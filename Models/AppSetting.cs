using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Campaign.Models
{
    public class AppSetting
    {
        public string VisualiserSeriesUri { get; set; }
        public string VisualiserApiKey { get; set; }
        public string CaseManagementQueueCountUrl { get; set; }
        public string CaseManagementAuthToken { get; set; }
        public string CountEndpoint { get; set; }
    }
}
