using System;
using System.Threading;

namespace CardWriter.Devices
{
    public sealed class FakeRfidReader : IRfidReader
    {
        private readonly object _sync = new object();
        private Timer _timer;
        private bool _started;

        public event EventHandler<CardScannedEventArgs> CardScanned;

        public void StartListening(CancellationToken cancellationToken)
        {
            StopListening();
            lock (_sync)
            {
                _started = true;
                _timer = new Timer(_ => RaiseScan(), null, TimeSpan.FromMilliseconds(200), TimeSpan.FromSeconds(1.8));
                cancellationToken.Register(StopListening);
            }
        }

        public void StopListening()
        {
            lock (_sync)
            {
                _started = false;
                _timer?.Dispose();
                _timer = null;
            }
        }

        private void RaiseScan()
        {
            if (!_started)
                return;

            var h = CardScanned;
            if (h == null)
                return;

            var uid = "RFID-" + DateTime.UtcNow.Ticks.ToString("X");
            h(this, new CardScannedEventArgs(uid));
        }
    }
}
