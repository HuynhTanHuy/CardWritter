namespace CardReaderSimulator.Api;

public interface IRfidCardsApiClient
{
    Task<(bool Success, int StatusCode, string? ReasonPhrase)> PostCardAsync(string rfidCardNumber, CancellationToken cancellationToken);
}
