using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Duali.Utils
{
    enum ChangeKeyAccess { MASTER_REQUIRE, SPECIFY_KEY, SAME_KEY, ALL_FROZEN }
    enum CryptoMethod { DES, THREEKTHREEDES, AES }
    class KeySetting
    {

        private ChangeKeyAccess access;
        private int specifyKey = -1;
        private bool configChange;
        private bool freeCD;
        private bool freeDirList;
        private bool masterKeyChange;

        private CryptoMethod cryptoMethod;
        private bool fileIdentifer;
        private String rfu;
        public int numOfKey { get; set; }

        public KeySetting(ChangeKeyAccess access, int specifyKey, bool configChange, bool freeCD, bool freeDirList, bool masterKeyChange, CryptoMethod method, bool fileIdentifer, String rfu, int numOfKey)
        {
            
            this.access = access;
            this.specifyKey = specifyKey;
            this.configChange = configChange;
            this.freeCD = freeCD;
            this.freeDirList = freeDirList;
            this.masterKeyChange = masterKeyChange;
            this.cryptoMethod = method;
            this.fileIdentifer = fileIdentifer;
            this.rfu = rfu;
            this.numOfKey = numOfKey;
        }

        public string ToHexString()
        {
            String setting = "";

            switch (access)
            {
                case ChangeKeyAccess.MASTER_REQUIRE:
                    setting += "0000";
                    break;
                case ChangeKeyAccess.SPECIFY_KEY:
                    setting += DataDuali.Utils.DecToBin(specifyKey);
                    break;
                case ChangeKeyAccess.SAME_KEY:
                    setting += "1110";
                    break;
                case ChangeKeyAccess.ALL_FROZEN:
                    setting += "1111";
                    break;
            }

            setting += configChange ? "1" : "0";
            setting += freeCD ? "1" : "0";
            setting += freeDirList ? "1" : "0";
            setting += masterKeyChange ? "1" : "0";

            switch (cryptoMethod)
            {
                case CryptoMethod.DES:
                    setting += "00";
                    break;
                case CryptoMethod.THREEKTHREEDES:
                    setting += "01";
                    break;
                case CryptoMethod.AES:
                    setting += "10";
                    break;
            }

            setting += fileIdentifer ? "1" : "0";
            setting += rfu;
            setting += DataDuali.Utils.DecToBin(numOfKey);
            return DataDuali.Utils.BinaryStringToHexString(setting);
        }

        public KeySetting(String hex)
        {
            String bin = DataDuali.Utils.HexToBin(hex);
            String access = bin.Substring(0, 4);
            String cryptoMethod = bin.Substring(8, 2);
            String numOfKey = bin.Substring(12, 4);

            switch(access)
            {
                case "0000":
                    this.access = ChangeKeyAccess.MASTER_REQUIRE;
                    break;
                case "1110":
                    this.access = ChangeKeyAccess.SAME_KEY;
                    break;
                case "1111":
                    this.access = ChangeKeyAccess.ALL_FROZEN;
                    break;
                default:
                    this.access = ChangeKeyAccess.SPECIFY_KEY;
                    this.specifyKey = DataDuali.Utils.BinToDec(access);
                    break;
            }

            this.configChange = bin[4] == '1';
            this.freeCD = bin[5] == '1';
            this.freeDirList = bin[6] == '1';
            this.masterKeyChange = bin[7] == '1';

            switch (cryptoMethod)
            {
                case "00":
                    this.cryptoMethod = CryptoMethod.DES;
                    break;
                case "01":
                    this.cryptoMethod = CryptoMethod.THREEKTHREEDES;
                    break;
                case "10":
                    this.cryptoMethod = CryptoMethod.AES;
                    break;
            }

            this.fileIdentifer = bin[10] == '1';
            this.rfu = bin[11].ToString();
            this.numOfKey = DataDuali.Utils.BinToDec(numOfKey);
        }

        public byte ComputeFlag()
        {
            String flag = "00001";

            switch (cryptoMethod)
            {
                case CryptoMethod.DES:
                    flag += "011";
                    break;
                case CryptoMethod.THREEKTHREEDES:
                case CryptoMethod.AES:
                    flag += "101";
                    break;
            }
            return DataDuali.Utils.BinaryStringToByte(flag);
        }
    }

   
}
