using System;
using System.Threading;

namespace CardWriter.Devices
{
    /// <summary>Lớp thiết bị đọc thẻ — UI chỉ subscribe event, không poll DualCard trực tiếp.</summary>
    public interface IRfidReader
    {
        event EventHandler<CardScannedEventArgs> CardScanned;

        /// <summary>Bắt đầu lắng nghe thẻ thật (vòng lặp nền). Fake reader có thể bỏ qua.</summary>
        void StartListening(CancellationToken cancellationToken);

        void StopListening();
    }
}
