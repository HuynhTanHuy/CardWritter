namespace CardReaderSimulator.Contracts;

/// <summary>
/// Chỉ dùng với <see cref="Readers.MockCardReader"/>: đưa một lần quét vào hàng đợi (POST /simulate/scan).
/// </summary>
public interface IManualScanTrigger
{
    void EnqueueScan(string? rfidCardNumber);
}
