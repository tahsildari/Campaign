namespace Campaign.Models
{
    public class Response<T>
    {
        public bool IsSuccessful { get; set; }
        public T Data { get; set; }

        public static Response<T> Success(T data)
            => new Response<T>
            {
                IsSuccessful = true,
                Data = data
            };

        public static Response<T> Fail()
            => new Response<T>
            {
                IsSuccessful = false,
                Data = default
            };
    }
}
