using CardReaderSimulator.Api;
using CardReaderSimulator.Contracts;

namespace CardReaderSimulator.Services;

/// <summary>
/// HostedService: lấy mã từ <see cref="ICardReader"/> và POST tới API ghi thẻ.
/// </summary>
public sealed class CardScanPostingHostedService : BackgroundService
{
    private readonly ICardReader _reader;
    private readonly IRfidCardsApiClient _api;
    private readonly ILogger<CardScanPostingHostedService> _logger;

    public CardScanPostingHostedService(
        ICardReader reader,
        IRfidCardsApiClient api,
        ILogger<CardScanPostingHostedService> logger)
    {
        _reader = reader;
        _api = api;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var rfid in _reader.ReadScansAsync(stoppingToken).ConfigureAwait(false))
            {
                _logger.LogInformation("Sự kiện quét RFID (mô phỏng hoặc thật): {Rfid}", rfid);

                try
                {
                    var (success, status, reason) = await _api.PostCardAsync(rfid, stoppingToken).ConfigureAwait(false);
                    if (success)
                        _logger.LogInformation(
                            "POST /api/rfid/cards thành công. RFID={Rfid}, HTTP {Status}",
                            rfid, status);
                    else
                        _logger.LogWarning(
                            "POST /api/rfid/cards thất bại. RFID={Rfid}, HTTP {Status} {Reason}",
                            rfid, status, reason);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi gọi API ghi thẻ cho RFID={Rfid}", rfid);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            /* shutdown */
        }
    }
}
