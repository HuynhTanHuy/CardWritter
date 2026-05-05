using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;

namespace  Duali.Utils
{
    class DualCardUtils
    {
        private const String TRANSPARENT_COMMAND = "3A02";
        private const String LE = "00";

        public delegate void OnErrorReceived(String error);
        public event OnErrorReceived OnError;
        private static DualCardUtils instance;
        private List<String> readers;
        private int port;
        private byte[] response;
        private byte[] sessionKey;
        private int responseLength;
        private int sessionKeyLength;
        private byte flag;

        public KeySetting currentKeySetting = new KeySetting(ChangeKeyAccess.MASTER_REQUIRE, 0, true, false, false, true, CryptoMethod.DES, false, "0", 14);
        public FileSetting currentFileSetting { get; set; }

        DualCardUtils() { }

        public static DualCardUtils GetInstance()
        {
            if (instance == null)
            {
                instance = new DualCardUtils();
            }

            return instance;
        }

        public void Initialize()
        {
            GetDeviceList();
            if (readers.Count != 0)
            {
                ConnectDevice(readers[0]);
            }
        }

        public int Authenticate(int keyNumber, string key)
        {
            if (port == 0)
            {
                return -1;
            }

            if (keyNumber > 14 || keyNumber < 0)
            {
                return -1;
            }
            sessionKey = new byte[256];

            return Duali.DualCard.DE_DESFireAuthentication(port, keyNumber, DataUtils.HexStringToBytes(key), key.Length / 2, sessionKey, out sessionKeyLength);
        }

        public TransparentResponse CreateApplication(String applicationId, KeySetting keySetting) 
        {
            if (port == 0)
            {
                return new TransparentResponse(ReaderResponse.DE_NOT_CONNECTED, DesfireResponse.OPERATION_OK, "");
            }

            flag = 0;
            String createCommand = "CA" + applicationId + keySetting.ToHexString();
            return Transparent(createCommand, "");
        }

        public TransparentResponse DeleteApplication(String applicationId)
        {
            if (port == 0)
            {
                return new TransparentResponse(ReaderResponse.DE_NOT_CONNECTED, DesfireResponse.OPERATION_OK, "");
            }

            flag = 0;
            String deleteCommand = "DA" + applicationId;
            return Transparent(deleteCommand, "");
        }

        public TransparentResponse CreateFile(int fileNumber, FileSetting fileSetting)
        {
            flag = 0;
            String createFileCommand = "CD" + DataUtils.DecToHex(fileNumber) + fileSetting.ToHexString();
            return Transparent(createFileCommand, "");
        }

        public TransparentResponse DeleteFile(int fileNumber)
        {
            flag = 0;
            String deleteFileCommand = "DF" + DataUtils.DecToHex(fileNumber);
            return Transparent(deleteFileCommand, "");
        }

        public TransparentResponse Read(int fileNumber, int offset, int length)
        {
            String offsetString = DataUtils.ReverseHex(DataUtils.DecToHex(offset).PadLeft(6, '0'));
            String lengthString = DataUtils.ReverseHex(DataUtils.DecToHex(length).PadLeft(6, '0'));

            String readFileCommand = "BD" + DataUtils.DecToHex(fileNumber) + offsetString + lengthString;
            flag = currentKeySetting.ComputeFlag();
            return Transparent(readFileCommand, "");
        }

        public TransparentResponse Write(int fileNumber, int offset, string data)
        {
            String hexData = DataUtils.StringToHex(data);
            int length = hexData.Length / 2;
            String offsetString = DataUtils.ReverseHex(DataUtils.DecToHex(offset).PadLeft(6, '0'));
            String lengthString = DataUtils.ReverseHex(DataUtils.DecToHex(length).PadLeft(6, '0'));
            String writeFileCommand = "3D" + DataUtils.DecToHex(fileNumber) + offsetString + lengthString;
            flag = currentKeySetting.ComputeFlag();
            return Transparent(writeFileCommand, hexData);
        }

        public TransparentResponse GetKeySetting()
        {
            flag = 0;
            String getKeySettingCommand = "45";
            TransparentResponse response = Transparent(getKeySettingCommand, "");
            if (TransparentResponse.VerifyResponse(response))
            {
                currentKeySetting = new  KeySetting(response.desfireResponseData);
                flag = currentKeySetting.ComputeFlag();
            }
            return response;
        }

        public TransparentResponse GetApplicationIds()
        {
            flag = 0;
            String getApplicationIdsCommand = "6A";
            return Transparent(getApplicationIdsCommand, "");
        }

        public TransparentResponse GetVersion()
        {
            flag = 0;
            String getVersionCommand = "60";
            return Transparent(getVersionCommand, "");
        }

        public TransparentResponse ChangeKeySetting(KeySetting keySetting)
        {
            flag = currentKeySetting.ComputeFlag();
            String changeKeySettingCommand = "54";
            return Transparent(changeKeySettingCommand, keySetting.ToHexString().Substring(0, 2));
        }

        public TransparentResponse ChangeKey(int keyNumber, int keyVersion, string oldKey, string newKey)
        {
            flag = 0;
            String changeKeyCommand = "C4" + DataUtils.DecToHex(keyNumber);
            return Transparent(changeKeyCommand, DataUtils.DecToHex(keyVersion) + "10" + "10" + oldKey + newKey);
        }

        public TransparentResponse FormatCard()
        {
            flag = 0;
            String changeKeyCommand = "FC";
            return Transparent(changeKeyCommand, "");
        }

        public TransparentResponse GetFileSetting(int fileNumber)
        {
            flag = 0;
            String getKeySettingCommand = "F5" + DataUtils.DecToHex(fileNumber);
            TransparentResponse response = Transparent(getKeySettingCommand, "");
            if (TransparentResponse.VerifyResponse(response))
            {
                currentFileSetting = new FileSetting(currentKeySetting.numOfKey, response.desfireResponseData);
            }

            return response;
        }

        public TransparentResponse GetFileIds()
        {
            flag = 0;
            String getFileIdsCommand = "6F";
            return Transparent(getFileIdsCommand, "");
        }

        private void RefreshReaderList()
        {
            GetDeviceList();
        }

        public void ConnectDevice(String readerName)
        {
            int readerIndex = readers.IndexOf(readerName);
            int responseCode;


            if (readerName.Substring(0, 3) == "COM")
            {
                String temp;
                temp = readerName.Substring(3);
                port = Convert.ToInt32(temp);
            }
            else
            {
                port = 100 + readerIndex;
            }

            responseCode = Duali.DualCard.DE_InitPort(port, 115200); // 115200 is baudrate of reader
            if (responseCode == port)
            {
                Duali.DualCard.DE_BuzzerOn(port);
                Thread.Sleep(100);
                Duali.DualCard.DE_BuzzerOff(port);
            }
            else
            {
                OnError("Failed to connect device");
            }
        }

        public void DisConnectDevice()
        {
            Duali.DualCard.DE_BuzzerOn(port);
            Thread.Sleep(100);
            Duali.DualCard.DE_BuzzerOff(port);
            Duali.DualCard.DE_BuzzerOn(port);
            Thread.Sleep(100);
            Duali.DualCard.DE_BuzzerOff(port);
            port = 0;
            Duali.DualCard.DE_ClosePort(port);
        }


        public void ResetReader()
        {

        }

        public TransparentResponse SelectApplication(String applicationId)
        {
            flag = 0;
            if (port == 0)
            {
                return new TransparentResponse(ReaderResponse.DE_NOT_CONNECTED, DesfireResponse.OPERATION_OK, "");
            }

            String selectCommand = "5A"+ applicationId ;
            return Transparent(selectCommand, "");
        }

        private TransparentResponse Transparent(String command, String data)
        {
            int commandLength = CalculateCommandLength(command);
            int commandBufferLength = CalculateCommandLength(command + data);
            byte[] commandBuffer = DataUtils.HexStringToBytes(command + data);
            CleanResponse();
            int readerResponse = Duali.DualCard.DE_DESFireTransparent(
                port,
                flag,
                commandLength,
                commandBufferLength,
                commandBuffer,
                out responseLength,
                response
            );
            return new TransparentResponse(readerResponse, response[1], DataUtils.BytesToHex(response).Substring(4));
        }

        private void CleanResponse()
        {
            this.responseLength = 0;
            this.response = new byte[1024];
        }

        public int CalculateCommandLength(String command)
        {
            return command.Length / 2;
        }

        public void GetDeviceList()
        {
            int i, cnt;
            string deviceName;
            StringBuilder stringBuilder = new StringBuilder();

            cnt = Duali.DualCard.DE_GetUSBDeviceList(1);
            this.readers = new List<string>();

            for (i = 0; i < cnt; i++)
            {
                Duali.DualCard.DE_GetUSBDeviceName(i, stringBuilder);
                deviceName = stringBuilder.ToString();
                this.readers.Add(deviceName);
            }
        }

        public int CardDetect()
        {
            if (port == 0)
            {
                return -1;
            }
            DeviceBuzzerRing();
            CleanResponse();
            return DualCard.DE_FindCard(port, 0, 0, 0, 0, out responseLength, response);
        }



        private void DeviceBuzzerRing()
        {
            Duali.DualCard.DE_BuzzerOn(port);
            Thread.Sleep(100);
            Duali.DualCard.DE_BuzzerOff(port);
        }

    }
}
