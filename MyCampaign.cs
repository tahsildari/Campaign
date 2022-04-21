using Campaign.Common;
using Campaign.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Campaign
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
            Task.Run(async () =>
            {
                await new MyCampaign(new RestClient()).Run();
            });
        }
    }
    public class MyCampaign
    {
        private readonly IRestClient restClient;
        private readonly AppSetting appSetting;
        public MyCampaign(IRestClient client)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("config.json", optional: false)
                .Build();

            appSetting = config.GetSection("AppSetting").Get<AppSetting>();
            restClient = client;
        }

        public async Task Run()
        {
            var epochTimestamp = (int)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            short[] serverIds = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 };

            var fetchers = new Dictionary<short, Task<Response<int>>>();
            var senders = new List<Task>();
            foreach (var serverId in serverIds)
            {
                fetchers.Add(serverId, FetchNewCountFromServer(serverId));
            }

            await Task.WhenAll(fetchers.Values);

            foreach (var task in fetchers)
            {
                var result = task.Value.Result;
                if (result.IsSuccessful)
                {
                    short serverId = task.Key;
                    senders.Add(SendData(
                        string.Format(Constants.CAMPAIN_KEY, serverId),
                        result.Data,
                        epochTimestamp));
                }
            }

            await Task.WhenAll(senders);
            var zendeskResult = await FetchZendeskQueueCount();
            if (zendeskResult.IsSuccessful)
            {
                await SendData(Constants.ZENDESK_METRIC, zendeskResult.Data, epochTimestamp);
            }
        }
        private async Task SendData(string metric, int value, int epochTimestamp)
        {
            var url = $"{appSetting.VisualiserSeriesUri}?api_key={appSetting.VisualiserApiKey}";
            IRestRequest request = new RestRequest(url, Method.POST);
            request.AddJsonBody(new
            {
                series = new[] {
                    new {
                        metric = metric,
                        points = new []{
                                new []{epochTimestamp,value}
                        },
                        type = "count"
                    }
                }
            });
            var result = await restClient.PostAsync<IRestResponse>(request);
            Console.WriteLine(result.StatusCode.ToString() + " " + result.Content);
        }

        private async Task<Response<int>> FetchNewCountFromServer(short serverId)
        {
            try
            {
                using var client = new WebClient();
                var url = string.Format(appSetting.CountEndpoint, serverId);
                var htmlCode = await client.DownloadStringTaskAsync(url);
                const string newCount = "new count: (.*)";
                var match = new Regex(newCount, RegexOptions.IgnoreCase).Match(htmlCode);
                var campaignCount = int.Parse(match.Groups[1].Value);
                Console.WriteLine($"Server: {serverId}   Campaign Queue Size: {campaignCount}");
                return Response<int>.Success(campaignCount);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                //better to log exception to a logger
                return Response<int>.Fail();
            }
        }

        private async Task<Response<int>> FetchZendeskQueueCount()
        {
            try
            {
                using var client = new WebClient();
                client.Headers.Add("Authorization", appSetting.CaseManagementAuthToken);
                var json = await client.DownloadStringTaskAsync(appSetting.CaseManagementQueueCountUrl);
                var queueCount = JObject.Parse(json)["count"].ToString();
                Console.WriteLine($"Zendesk Engineering Ticket count: {queueCount}");
                return Response<int>.Success(int.Parse(queueCount));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Response<int>.Fail();
            }
        }
    }
}
