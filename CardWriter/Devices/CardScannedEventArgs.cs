using System;

namespace CardWriter.Devices
{
    /// <summary>Đầu đọc báo có thẻ / UID (giả lập hoặc thật).</summary>
    public sealed class CardScannedEventArgs : EventArgs
    {
        public CardScannedEventArgs(string uid)
        {
            Uid = uid ?? string.Empty;
        }

        public string Uid { get; }
    }
}
