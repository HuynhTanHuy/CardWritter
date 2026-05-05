using Duali;

namespace CardWriter.Devices
{
    public sealed class FakeRfidWriter : IRfidWriter
    {
        public int CardDetect() => ReaderResponse.DE_OK;

        public bool AuthMifare(string keyHex, int block, DualCardUtils.KeyType keyType) => true;

        public string ReadMifare(int block) => "FAKE-CARD";

        public bool WriteMifare(string data, int block) => true;

        public bool WriteMifareHex(string dataHex, int block) => true;
    }
}
