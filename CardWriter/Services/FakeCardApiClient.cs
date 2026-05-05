namespace CardWriter.Services
{
    public sealed class FakeCardApiClient : ICardApiClient
    {
        public CardApiResult CreateOrUpdateCard(string hospitalId, string rfidCardNumber, string rfidCardBatchCode)
        {
            return new CardApiResult
            {
                Success = true,
                StatusCode = 200,
                Message = "Fake API OK"
            };
        }
    }
}
