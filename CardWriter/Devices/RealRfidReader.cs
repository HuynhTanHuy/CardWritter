using System;
using System.Threading;
using System.Threading.Tasks;
using Duali;

namespace CardWriter.Devices
{
    /// <summary>Chờ thẻ thật qua DualCard — chạy vòng lặp trên thread nền, không chặn UI.</summary>
    public sealed class RealRfidReader : IRfidReader
    {
        private readonly object _sync = new object();
        private CancellationTokenSource _cts;
        private Task _listenTask;

        public event EventHandler<CardScannedEventArgs> CardScanned;

        public void StartListening(CancellationToken cancellationToken)
        {
            StopListening();

            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var token = _cts.Token;
            _listenTask = Task.Run(() => ListenLoop(token), token);
        }

        public void StopListening()
        {
            lock (_sync)
            {
                _cts?.Cancel();
                try { _listenTask?.Wait(TimeSpan.FromSeconds(2)); } catch { /* ignore */ }
                _cts?.Dispose();
                _cts = null;
                _listenTask = null;
            }
        }

        private void ListenLoop(CancellationToken ct)
        {
            var device = DualCardUtils.GetInstance();

            while (!ct.IsCancellationRequested)
            {
                while (device.CardDetect() != ReaderResponse.DE_OK)
                {
                    if (ct.IsCancellationRequested)
                        return;
                    Thread.Sleep(1000);
                }

                RaiseCardScanned(string.Empty);

                while (device.CardDetect() == ReaderResponse.DE_OK)
                {
                    if (ct.IsCancellationRequested)
                        return;
                    Thread.Sleep(300);
                }
            }
        }

        private void RaiseCardScanned(string uid)
        {
            var h = CardScanned;
            if (h == null)
                return;
            h(this, new CardScannedEventArgs(uid));
        }
    }
}
