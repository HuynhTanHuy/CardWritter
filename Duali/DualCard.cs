using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace Duali
{
    public class DualCard
    {
        [DllImport("DualCardDll.dll")]
        public static extern int DE_InitPort(int nPort, int nBaud);

        [DllImport("DualCardDll.dll")]
        public static extern int DE_BuzzerOn(int nPort);

        [DllImport("DualCardDll.dll")]
        public static extern int DE_BuzzerOff(int nPort);

        [DllImport("DualCardDll.dll")]
        public static extern void DE_ClosePort(int nPort);

        [DllImport("DualCardDll.dll")]
        public static extern int DE_GetVersion(int nPort, out int outlen, byte[] lpRes);

        [DllImport("DualCardDll.dll")]
        public static extern int DEA_Idle_Req(int nPort, out int outlen, byte[] lpRes);

        [DllImport("DualCardDll.dll")]
        public static extern int DEA_Anticoll(int nPort, out int outlen, byte[] lpRes);

        [DllImport("DualCardDll.dll")]
        public static extern int DEA_AntiSelLevel(int nPort, out int outlen, byte[] lpRes);

        [DllImport("DualCardDll.dll")]
        public static extern int DEA_Select(int nPort, byte[] uid, out int outlen, byte[] lpRes);

        [DllImport("DualCardDll.dll")]
        public static extern int DEA_Authkey(int nPort, byte mode, byte[] keydata, byte blockno);

        [DllImport("DualCardDll.dll")]
        public static extern int DEA_Read(int nPort, byte blockno, out int outlen, byte[] lpRes);

        [DllImport("DualCardDll.dll")]
        public static extern int DEA_Write(int nPort, byte blockno, int datalen, byte[] data);

        [DllImport("DualCardDll.dll")]
        public static extern int GetErrMsg(int errcode, StringBuilder retmsg);

        [DllImport("DualCardDll.dll")]
        public static extern int DE_RFOn(int nPort);

        [DllImport("DualCardDll.dll")]
        public static extern int DE_RFOff(int nPort);

        [DllImport("DualCardDll.dll")]
        public static extern int DEB_Transparent(int nPort, byte datalen, byte[] data, byte TOUT, out byte outlen, byte[] lpRes);

        [DllImport("DualCardDll.dll")]
        public static extern int DEA_Loadkey(int nPort, byte mode, byte keyno, byte[] keydata);

        [DllImport("DualCardDll.dll")]
        public static extern int DEA_Req_AuthRead(int nPort, byte requestmode, byte authmode, byte keyno, byte blockno, out int outlen, byte[] lpRes);

        [DllImport("DualCardDll.dll")]
        public static extern int DE_NFC_TAG_CMD(int nPort, byte TagType, byte TagCMD, int optdatalen, byte[] optdata, out int outlen, byte[] lpRes);

        [DllImport("DualCardDll.dll")]
        public static extern int DE_Polling(int nPort, int datalen, byte[] data, out int outlen, byte[] lpRes, int timeout);

        //Get usb device list
        [DllImport("DualCardDll.dll")]
        public static extern int DE_GetUSBDeviceList(int useserial);
        [DllImport("DualCardDll.dll")]
        public static extern int DE_GetUSBDeviceName(int nIDX, StringBuilder devname);

        //Get PCSC device list
        [DllImport("DualCardDll.dll")]
        public static extern int DE_GetPSCSReaderList(int useserial);
        [DllImport("DualCardDll.dll")]
        public static extern int DE_GetPCSCReaderName(int nIDX, StringBuilder devname);
        [DllImport("DualCardDll.dll")]
        public static extern int DE_SCardConnect(int port);
        [DllImport("DualCardDll.dll")]
        public static extern int DE_SCardDisConnect(int port);

        //TYPE A/B Common Function
        [DllImport("DualCardDll.dll")]
        public static extern int DE_APDU(int nPort, int datalen, byte[] data, out int outlen, byte[] lpRes);
        [DllImport("DualCardDll.dll")]
        public static extern int DE_FindCard(int nPort, byte baud, byte cid, byte nad, byte option, out int outlen, byte[] lpRes);
    
        // Desfire
        [DllImport("DualCardDll.dll")]
        public static extern int DE_DESFireTransparent(int nPort, byte Flag, int CmdDataLen, int dataLen, byte[] pData, out int outlen, byte[] lpRes);
        [DllImport("DualCardDll.dll")]
        public static extern int DE_DESFire_SetConfig_Batch(int nPort, byte Flag, int CmdDataLen, int DataLen, byte[] pData, out int outlen, byte[] lpRes);
        [DllImport("DualCardDll.dll")]
        public static extern int DE_DESFireAuthentication(int nPort, int keyNo, byte[] Key, int KeyLen, byte[] lpRes, out int outlen);
        [DllImport("DualCardDll.dll")]
        public static extern int DE_DESFireAuthentication_AES(int nPort, byte keyNo, byte[] Key, int KeyLen, out int outlen, byte[] lpRes);

    }
}
