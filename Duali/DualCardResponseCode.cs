using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace  Duali
{
    public class ReaderResponse
    {
        public static readonly int DE_NOT_CONNECTED = -1;
        public static readonly int DE_OK = 0;
        public static readonly int DE_NO_TAG_ERROR = 2;
        public static readonly int DE_CRC_ERROR = 3;
        public static readonly int DE_EMPTY = 4;
        public static readonly int DE_AUTHENTICATION_ERROR = 5;
        public static readonly int DE_NO_POWER = 5;
        public static readonly int DE_PARITY_ERROR = 6;
        public static readonly int DE_CODE_ERROR = 7;
        public static readonly int DE_SERIAL_NUMBER_ERROR = 8;
        public static readonly int DE_KEY_ERROR = 9;
        public static readonly int DE_NOT_AUTHENTICATION_ERROR = 10;
        public static readonly int DE_BIT_COUNT_ERROR = 11;
        public static readonly int DE_BYTE_COUNT_ERROR = 12;
        public static readonly int DE_TRANSFER_ERROR = 14;
        public static readonly int DE_WRITE_ERROR = 15;
        public static readonly int DE_INCREMENT_ERROR = 16;
        public static readonly int DE_DECREMENT_ERROR = 17;
        public static readonly int DE_READ_ERROR = 18;
        public static readonly int DE_OVERFLOW_ERROR = 19;
        public static readonly int DE_POLLING_ERROR = 20;
        public static readonly int DE_FRAMING_ERROR = 21;
        public static readonly int DE_ACCESS_ERROR = 22;
        public static readonly int DE_UNKNOWN_COMMAND_ERROR = 23;
        public static readonly int DE_ANTICOLLISION_ERROR = 24;
        public static readonly int DE_INITIALIZATION_ERROR = 25;
        public static readonly int DE_INTERFACE_ERROR = 26;
        public static readonly int DE_ACCESS_TIMEOUT_ERROR = 27;
        public static readonly int DE_NO_BITWISE_ANTICOLLISION_ERROR = 28;
        public static readonly int DE_FILE_ERROR = 29;
        public static readonly int DE_INVAILD_BLOCK_ERROR = 32;
        public static readonly int DE_ACK_COUNT_ERROR = 33;
        public static readonly int DE_NACK_DESELECT_ERROR = 34;
        public static readonly int DE_NACK_COUNT_ERROR = 35;
        public static readonly int DE_SAME_FRAME_COUNT_ERROR = 36;
        public static readonly int DE_RCV_BUFFER_TOO_SMALL_ERROR = 49;
        public static readonly int DE_RCV_BUFFER_OVERFLOW_ERROR = 50;
        public static readonly int DE_RF_ERROR = 51;
        public static readonly int DE_PROTOCOL_ERROR = 52;
        public static readonly int DE_USER_BUFFER_FULL_ERROR = 53;
        public static readonly int DE_BUADRATE_NOT_SUPPORTED = 54;
        public static readonly int DE_INVAILD_FORMAT_ERROR = 55;
        public static readonly int DE_LRC_ERROR = 56;
        public static readonly int DE_FRAMERR = 57;
        public static readonly int DE_WRONG_PARAMETER_VALUE = 60;
        public static readonly int DE_INVAILD_PARAMETER_ERROR = 61;
        public static readonly int DE_UNSUPPORTED_PARAMETER = 62;
        public static readonly int DE_UNSUPPORTED_COMMAND = 63;
        public static readonly int DE_INTERFACE_NOT_ENABLED = 64;
        public static readonly int DE_ACK_SUPPOSED = 65;
        public static readonly int DE_NACK_RECEVIED = 66;
        public static readonly int DE_BLOCKNR_NOT_EQUAL = 67;
        public static readonly int DE_TARGET_SET_TOX = 68;
        public static readonly int DE_TARGET_RESET_TOX = 69;
        public static readonly int DE_TARGET_DESELECTED = 70;
        public static readonly int DE_TARGET_RELEASED = 71;
        public static readonly int DE_ID_ALREADY_IN_USE = 72;
        public static readonly int DE_INSTANCE_ALREADY_IN_USE = 73;
        public static readonly int DE_ID_NOT_IN_USE = 74;
        public static readonly int DE_NO_ID_AVAILABLE = 75;
        public static readonly int DE_OTHER_ERROR = 76;
        public static readonly int DE_INVALID_READER_STATE = 77;
        public static readonly int DE_MI_JOINER_TEMP_ERROR = 78;
        public static readonly int DE_NOTYET_IMPLEMENTED = 100;
        public static readonly int DE_FIFO_ERROR = 109;
        public static readonly int DE_WRONG_SELECT_COUNT = 114;
        public static readonly int DE_WRONG_VALUE = 123;
        public static readonly int DE_VALERR = 124;
        public static readonly int DE_RE_INIT = 126;
        public static readonly int DE_NO_INIT = 127;
        public static readonly int APP_INVALID_PORT = 1000;
        public static readonly int APP_STX_ERROR = 1001;
        public static readonly int APP_INVALID_LENGTH_ERROR = 1002;
        public static readonly int APP_TIMEOUT_ERROR = 1003;
        public static readonly int APP_CRC_ERROR = 1004;
        public static readonly int APP_LRC_ERROR = 1005;
        public static readonly int APP_RW_ERROR = 1006;
        public static readonly int APP_ETX_ERROR = 1007;
        public static readonly int APP_USB_WRITE_ERROR = 1008;
        public static readonly int APP_USB_READ_ERROR = 1009;
        public static readonly int APP_INVALID_SENDDATA_LEN = 1010;
        public static readonly int APP_INVALID_SENDBUF_SIZE = 1011;
        public static readonly int APP_TOO_SMALL_RECVBUF = 1012;
        public static readonly int APP_SENDBUF_OVERFLOW = 1013;
    }

    class DesfireResponse
    {
        public static readonly int OPERATION_OK = 0x00;
        public static readonly int NO_CHANGES = 0x0C;
        public static readonly int OUT_OF_EEPROM_ERROR = 0x0E;
        public static readonly int ILLEGAL_COMMAND_CODE = 0x1C;
        public static readonly int INTEGRITY_ERROR = 0x1E;
        public static readonly int NO_SUCH_KEY = 0x40;
        public static readonly int LENGTH_ERROR = 0x7E;
        public static readonly int PERMISSION_DENIED = 0x9D;
        public static readonly int PARAMETER_ERROR = 0x9E;
        public static readonly int APPLICATION_NOT_FOUND = 0xA0;
        public static readonly int APPL_INTEGRITY_ERROR = 0xA1;
        public static readonly int AUTHENTICATION_ERROR = 0xAE;
        public static readonly int ADDITIONAL_FRAME = 0xAF;
        public static readonly int BOUNDARY_ERROR = 0xBE;
        public static readonly int PICC_INTEGRITY_ERROR = 0xC1;
        public static readonly int COMMAND_ABBORTED = 0xCA;
        public static readonly int PICC_DISSABLE_ERROR = 0xCD;
        public static readonly int COUNT_ERROR = 0xCE;
        public static readonly int DUPLICATE_ERROR = 0xDE;
        public static readonly int EEPROM_ERROR = 0xEE;
        public static readonly int FILE_NOT_FOUND = 0xF0;
        public static readonly int FILE_INTEGRITY_ERROR = 0xF1;

    }
}
