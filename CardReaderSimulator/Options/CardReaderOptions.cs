namespace CardReaderSimulator.Options;

/// <summary>
/// Cấu hình đầu đọc thẻ (mock / thật) và endpoint API ghi thẻ.
/// </summary>
public sealed class CardReaderOptions
{
    public const string SectionName = "CardReader";

    /// <summary>Mock | Real</summary>
    public string Mode { get; set; } = "Mock";

    /// <summary>Base URL backend (ví dụ http://localhost:5000). POST /api/rfid/cards</summary>
    public string ApiBaseUrl { get; set; } = "http://localhost:5000";

    public string HospitalId { get; set; } = "";

    /// <summary>
    /// Bearer token — ưu tiên biến môi trường <c>CardReader__BearerToken</c> (ASP.NET Core bind sẵn).
    /// </summary>
    public string? BearerToken { get; set; }

    public Guid RfidCardTypeId { get; set; } = Guid.Parse("4866945e-51a3-48f7-b6a8-eb5e51ffbc8c");

    /// <summary>
    /// Random: lặp 3–5s, RFID ngẫu nhiên; có thể xen kẽ POST /simulate/scan.<br/>
    /// File|Queue: đọc hàng đợi từ file, mỗi chu kỳ lấy một dòng.<br/>
    /// Manual: chỉ kích hoạt thủ công qua /simulate/scan.
    /// </summary>
    public string InputSource { get; set; } = "Random";

    /// <summary>Đường dẫn file: mỗi dòng một mã RFID (khi InputSource = File hoặc Queue).</summary>
    public string? RfidQueueFilePath { get; set; }

    public int IntervalSecondsMin { get; set; } = 3;

    public int IntervalSecondsMax { get; set; } = 5;

    /// <summary>Bật POST /simulate/scan trên host simulator.</summary>
    public bool EnableManualSimulateEndpoint { get; set; } = true;
}
