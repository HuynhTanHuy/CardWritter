using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace  Duali
{
    public enum CommunicationType { PLAIN, PLAIN_WITH_DES_MAC, FULL_DES_ENCIPHERED }

    public class FileSetting
    {
        private int changeAccessKey;
        private int rwAccessKey;
        private int readAccessKey;
        private int writeAccessKey;
        private int fileSize;
        private CommunicationType communicationType;

        public FileSetting(int numOfKey, CommunicationType communicationType, int readAccessKey, int writeAccessKey, int rwAccessKey, int changeAccessKey, int fileSize)
        {
            this.communicationType = communicationType;
            this.fileSize = fileSize;

            if (changeAccessKey >= numOfKey)
            {
                this.changeAccessKey = 0;
            } else
            {
                this.changeAccessKey = changeAccessKey;
            }
            if (rwAccessKey >= numOfKey)
            {
                this.rwAccessKey = 0;
            }
            else
            {
                this.rwAccessKey = rwAccessKey;
            }
            if (readAccessKey >= numOfKey)
            {
                this.readAccessKey = 0;
            }
            else
            {
                this.readAccessKey = readAccessKey;
            }
            if (writeAccessKey >= numOfKey)
            {
                this.writeAccessKey = 0;
            }
            else
            {
                this.writeAccessKey = writeAccessKey;
            }
        }

        public FileSetting(int numOfKey, string hex)
        {
            string communicationTypeString = hex.Substring(0, 2);
            switch (communicationTypeString)
            {
                case "00":
                   this.communicationType = CommunicationType.PLAIN;
                    break;
                case "01":
                   this.communicationType = CommunicationType.PLAIN_WITH_DES_MAC;
                    break;
                case "03":
                   this.communicationType = CommunicationType.FULL_DES_ENCIPHERED;
                    break;
            }

            string fileSize = hex.Substring(6, 6);
            this.fileSize = GetFileSizeInt(fileSize);

            this.changeAccessKey = hex[3];
            this.rwAccessKey = hex[2];
            this.readAccessKey = hex[4];
            this.writeAccessKey = hex[5];
        }

        public String ToHexString()
        {
            String setting = "";
            switch (communicationType)
            {
                case CommunicationType.PLAIN:
                    setting += "00";
                    break;
                case CommunicationType.PLAIN_WITH_DES_MAC:
                    setting += "01";
                    break;
                case CommunicationType.FULL_DES_ENCIPHERED:
                    setting += "03";
                    break;
            }
            setting += DataUtils.DecToHex(rwAccessKey)[1];
            setting += DataUtils.DecToHex(changeAccessKey)[1];
            setting += DataUtils.DecToHex(readAccessKey)[1];
            setting += DataUtils.DecToHex(writeAccessKey)[1];
            setting += GetFileSizeHex(fileSize);

            return setting;
        }

        private string GetFileSizeHex(int fileSize)
        {
            string hexString = DataUtils.DecToHex(fileSize).PadLeft(6, '0');
            return hexString.Substring(4, 2) + hexString.Substring(2, 2) + hexString.Substring(0, 2);
        }

        private int GetFileSizeInt(string fileSizeString)
        {
            string fileSizeHex = fileSizeString.Substring(4, 2) + fileSizeString.Substring(2, 2) + fileSizeString.Substring(0, 2);
            return DataUtils.HexToDec(fileSizeHex);
        }
    }
}