using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace  Duali
{
    public class DataUtils
    {
        private const int sizeOfIntInHalfBytes = 8;
        private const int numberOfBitsInAHalfByte = 4;
        private const int halfByte = 0x0F;
        private static readonly char[] hexDigits = {
            '0', '1', '2', '3', '4', '5', '6', '7',
            '8', '9', 'A', 'B', 'C', 'D', 'E', 'F'
          };

        public static String DecToHex(int dec)
        {
            String hex = Convert.ToString(dec, 16);
            return hex.PadLeft(hex.Length + hex.Length % 2, '0');
        }

        public static int HexToDec(String hex)
        {
            return Convert.ToInt32(hex, 16);
        }

        public static string BinaryStringToHexString(string binary)
        {
            StringBuilder result = new StringBuilder(binary.Length / 8 + 1);

            // TODO: check all 1's or 0's... Will throw otherwise

            int mod4Len = binary.Length % 8;
            if (mod4Len != 0)
            {
                // pad to length multiple of 8
                binary = binary.PadLeft(((binary.Length / 8) + 1) * 8, '0');
            }

            for (int i = 0; i < binary.Length; i += 8)
            {
                string eightBits = binary.Substring(i, 8);
                result.AppendFormat("{0:X2}", Convert.ToByte(eightBits, 2));
            }

            return result.ToString();
        }

        public static string DecToBin(int dec)
        {
            return Convert.ToString(dec, 2).PadLeft(4, '0');
        }

        public static string HexToBin(string hex)
        {
            string binarystring = String.Join(String.Empty, hex.Select(
                    c => Convert.ToString(Convert.ToInt32(c.ToString(), 16), 2).PadLeft(4, '0')));
            return binarystring;

        }

        public static byte[] HexStringToBytes(string str)
        {
            string[] hexValuesSplit = new string[str.Length / 2];
            byte[] byteArray = new byte[hexValuesSplit.Length];
            int i = 0;

            for (i = 0; i < str.Length / 2; i++)
            {
                hexValuesSplit[i] = str.Substring(i * 2, 2);
            }

            i = 0;
            foreach (String hex in hexValuesSplit)
            {
                // 16진수를 Integer 로 변환
                int value = Convert.ToInt32(hex, 16);
                // Integer 를 byte 로 변환
                byte bytevalue = Convert.ToByte(value);

                byteArray[i++] = bytevalue;
            }

            return byteArray;
        }

        public static string BytesToHex(byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", "");
        }

        public static byte HexStringToByte(string str)
        {
            int value = Convert.ToInt32(str, 16);
            return Convert.ToByte(value);
        }

        public static void ByteArrayCopy(byte[] Result, int ResultStart, byte[] Source, int SourceStart, int Count)
        {
            for (int i = 0; i < Count; i++)
            {
                Result[ResultStart + i] = Source[SourceStart + i];
            }
        }

        public static byte BinaryStringToByte(string flag)
        {
            return HexStringToByte(BinaryStringToHexString(flag));
        }

        public static int BinToDec(string access)
        {
            return Convert.ToInt32(access, 2);
        }

        public static string ReverseHex(string hexString)
        {
            hexString = hexString.PadLeft(hexString.Length + hexString.Length % 2, '0');
            string reverseString = "";
            int length = hexString.Length;
            for (int index = length; index > 0; )
            {
                reverseString += hexString.Substring(index - 2, 2);
                index -= 2;
            }
            return reverseString;
        }

        public static string UnReverseHex(string hexString)
        {
            hexString = hexString.PadRight(hexString.Length + hexString.Length % 2, '0');
            string reverseString = "";
            int length = hexString.Length;
            for (int index = length; index > 0;)
            {
                reverseString += hexString.Substring(index - 2, 2);
                index -= 2;
            }
            return reverseString;
        }

        public static string StringToHex(string str)
        {
            string reverseString = "";
            byte[] ba = Encoding.Default.GetBytes(str);
            var hexString = BitConverter.ToString(ba);
            reverseString = hexString.Replace("-", "");
            return reverseString;
        }

        public static string HexToString(string hex)
        {
            hex = hex.Replace("-", "");
            byte[] raw = new byte[hex.Length / 2];
            for (int i = 0; i < raw.Length; i++)
            {
                raw[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }

           return Encoding.ASCII.GetString(raw);
        }
    }

}
