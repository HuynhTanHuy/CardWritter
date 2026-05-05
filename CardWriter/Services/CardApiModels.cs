namespace CardWriter.Services
{
    public sealed class CardApiResult
    {
        public bool Success { get; set; }
        public int StatusCode { get; set; }
        public string Message { get; set; }
    }
}
