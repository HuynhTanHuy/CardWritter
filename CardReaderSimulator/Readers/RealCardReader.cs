using CardReaderSimulator.Contracts;

namespace CardReaderSimulator.Readers;

/// <summary>
/// Placeholder cho tích hợp SDK thật: hiện chặn vô hạn tới khi hủy host.
/// Sau này thay bằng đọc từ phần cứng/SDK mà không đổi <see cref="ICardReader"/>.
/// </summary>
public sealed class RealCardReader : ICardReader
{
    public async IAsyncEnumerable<string> ReadScansAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
        yield break;
    }
}
