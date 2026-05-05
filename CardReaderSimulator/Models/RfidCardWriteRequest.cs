namespace CardReaderSimulator.Models;

/// <summary>
/// Body cố định khi gọi POST /api/rfid/cards — không đổi theo nghiệp vụ backend.
/// </summary>
public sealed class RfidCardWriteRequest
{
    public string HospitalId { get; set; } = "";

    public string RfidCardNumber { get; set; } = "";

    public Guid RfidCardTypeId { get; set; }

    public Guid? RfidCardBatchId { get; set; }

    public int Status { get; set; } = 4;

    public bool IsActive { get; set; } = true;
}
