using System.Collections.Concurrent;
using System.Threading.Channels;
using CardReaderSimulator.Contracts;
using CardReaderSimulator.Options;
using Microsoft.Extensions.Options;

namespace CardReaderSimulator.Readers;

/// <summary>
/// Mô phỏng quét RFID: chu kỳ ngẫu nhiên 3–5s, file hàng đợi, và/hoặc kích hoạt thủ công.
/// </summary>
public sealed class MockCardReader : ICardReader, IManualScanTrigger
{
    private readonly IOptions<CardReaderOptions> _options;
    private readonly ILogger<MockCardReader> _logger;
    private readonly Channel<string> _manualChannel = Channel.CreateUnbounded<string>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

    public MockCardReader(IOptions<CardReaderOptions> options, ILogger<MockCardReader> logger)
    {
        _options = options;
        _logger = logger;
    }

    public void EnqueueScan(string? rfidCardNumber)
    {
        var rfid = string.IsNullOrWhiteSpace(rfidCardNumber)
            ? GenerateRandomRfid()
            : rfidCardNumber.Trim();
        if (!_manualChannel.Writer.TryWrite(rfid))
            _logger.LogWarning("Không thể đưa RFID thủ công vào hàng đợi (writer đã đóng).");
    }

    public async IAsyncEnumerable<string> ReadScansAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var opts = _options.Value;
        var source = (opts.InputSource ?? "Random").Trim();

        if (source.Equals("Manual", StringComparison.OrdinalIgnoreCase)
            || source.Equals("ManualOnly", StringComparison.OrdinalIgnoreCase))
        {
            await foreach (var item in _manualChannel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
                yield return item;
            yield break;
        }

        var fileQueue = new Queue<string>();
        if (source.Equals("File", StringComparison.OrdinalIgnoreCase)
            || source.Equals("Queue", StringComparison.OrdinalIgnoreCase))
            LoadFileLines(fileQueue, opts.RfidQueueFilePath);

        while (!cancellationToken.IsCancellationRequested)
        {
            var minMs = Math.Max(1000, opts.IntervalSecondsMin * 1000);
            var maxMs = Math.Max(minMs, opts.IntervalSecondsMax * 1000);
            var delayMs = Random.Shared.Next(minMs, maxMs + 1);

            if (source.Equals("File", StringComparison.OrdinalIgnoreCase)
                || source.Equals("Queue", StringComparison.OrdinalIgnoreCase))
            {
                await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
                if (fileQueue.Count > 0)
                    yield return fileQueue.Dequeue();
                else
                {
                    _logger.LogWarning(
                        "Hàng đợi RFID từ file trống hoặc hết; dùng mã ngẫu nhiên.");
                    yield return GenerateRandomRfid();
                }
                continue;
            }

            // Random (mặc định): xen kẽ bộ đếm thời gian và kênh thủ công
            using var cycle = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var delayTask = Task.Delay(delayMs, cycle.Token);
            var manualTask = _manualChannel.Reader.ReadAsync(cycle.Token).AsTask();

            var winner = await Task.WhenAny(manualTask, delayTask).ConfigureAwait(false);
            if (winner == manualTask)
            {
                cycle.Cancel();
                try
                {
                    await delayTask.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    /* bỏ qua */
                }

                string value;
                try
                {
                    value = await manualTask.ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }

                yield return value;
            }
            else
            {
                cycle.Cancel();
                try
                {
                    await manualTask.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    /* huỷ đọc manual để chu kỳ sau đọc lại */
                }

                yield return GenerateRandomRfid();
            }
        }
    }

    private void LoadFileLines(Queue<string> queue, string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            _logger.LogWarning("CardReader:RfidQueueFilePath chưa cấu hình.");
            return;
        }

        if (!File.Exists(path))
        {
            _logger.LogWarning("File hàng đợi RFID không tồn tại: {Path}", path);
            return;
        }

        foreach (var line in File.ReadLines(path))
        {
            var t = line.Trim();
            if (t.Length > 0)
                queue.Enqueue(t);
        }

        _logger.LogInformation("Đã nạp {Count} mã RFID từ file.", queue.Count);
    }

    private static string GenerateRandomRfid() =>
        Random.Shared.NextInt64(long.MinValue, long.MaxValue).ToString("X16");
}
