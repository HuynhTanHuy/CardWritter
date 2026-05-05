using Duali;

namespace CardWriter.Devices
{
    public sealed class DualCardRfidWriter : IRfidWriter
    {
        private DualCardUtils Device => DualCardUtils.GetInstance();

        public int CardDetect() => Device.CardDetect();

        public bool AuthMifare(string keyHex, int block, DualCardUtils.KeyType keyType) =>
            Device.AuthMifare(keyHex, block, keyType);

        public string ReadMifare(int block) => Device.ReadMifare(block);

        public bool WriteMifare(string data, int block) => Device.WriteMifare(data, block);

        public bool WriteMifareHex(string dataHex, int block) => Device.WriteMifareHex(dataHex, block);
    }
}
