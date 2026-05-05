namespace CardReaderSimulator.Contracts;

/// <summary>
/// Đầu đọc thẻ (mock hoặc SDK thật). Phát luồng mã RFID khi có “quét”.
/// </summary>
public interface ICardReader
{
    IAsyncEnumerable<string> ReadScansAsync(CancellationToken cancellationToken);
}
