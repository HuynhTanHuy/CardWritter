namespace CardWriter.Devices
{
    /// <summary>Thao tác ghi/đọc Mifare qua driver — tách khỏi Form và CardService (dễ mock test).</summary>
    public interface IRfidWriter
    {
        int CardDetect();

        bool AuthMifare(string keyHex, int block, Duali.DualCardUtils.KeyType keyType);

        string ReadMifare(int block);

        bool WriteMifare(string data, int block);

        bool WriteMifareHex(string dataHex, int block);
    }
}
