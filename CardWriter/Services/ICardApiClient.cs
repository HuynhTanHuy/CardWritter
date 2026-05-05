namespace CardWriter.Services
{
    public interface ICardApiClient
    {
        CardApiResult CreateOrUpdateCard(string hospitalId, string rfidCardNumber, string rfidCardBatchCode);
    }
}
