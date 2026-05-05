namespace CardWriter.Services
{
    internal static class CardMifareConstants
    {
        public const string DefaultMifareKeyA = "FFFFFFFFFFFF";
        public const string MifareKeyA = "6C6564616E67";
        public const string DefaultMifareKeyB = "FFFFFFFFFFFF";
        public const string AccessBit = "FF078069";
        public const int DataBlock = 14;
        public const int DataSectorTrailerBlock = 15;
    }
}
