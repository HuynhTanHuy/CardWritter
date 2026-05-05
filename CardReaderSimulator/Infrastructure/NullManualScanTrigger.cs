using CardReaderSimulator.Contracts;

namespace CardReaderSimulator.Infrastructure;

/// <summary>Khi Mode=Real — không có kích hoạt thủ công.</summary>
internal sealed class NullManualScanTrigger : IManualScanTrigger
{
    public static readonly NullManualScanTrigger Instance = new();

    private NullManualScanTrigger()
    {
    }

    public void EnqueueScan(string? rfidCardNumber)
    {
    }
}
