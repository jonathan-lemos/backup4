using System;
using System.Linq;

namespace Backup4.Misc
{
    public static class BitConvert
    {
        public static bool To64(byte[] bytes, out long res)
        {
            if (bytes.Length != 8)
            {
                res = 0;
                return false;
            }

            if (BitConverter.IsLittleEndian)
            {
                bytes = bytes.Reverse().ToArray();
            }

            res = BitConverter.ToInt64(bytes);
            return true;
        }

        public static bool To32(byte[] bytes, out int res)
        {
            if (bytes.Length != 4)
            {
                res = 0;
                return false;
            }

            if (BitConverter.IsLittleEndian)
            {
                bytes = bytes.Reverse().ToArray();
            }

            res = BitConverter.ToInt32(bytes);
            return true;
        }

        public static bool To16(byte[] bytes, out short res)
        {
            if (bytes.Length != 2)
            {
                res = 0;
                return false;
            }

            if (BitConverter.IsLittleEndian)
            {
                bytes = bytes.Reverse().ToArray();
            }

            res = BitConverter.ToInt16(bytes);
            return true;
        }

        public static byte[] From64(long arg)
        {
            var tmp = BitConverter.GetBytes(arg);
            return BitConverter.IsLittleEndian ? tmp.Reverse().ToArray() : tmp;
        }

        public static byte[] From32(int arg)
        {
            var tmp = BitConverter.GetBytes(arg);
            return BitConverter.IsLittleEndian ? tmp.Reverse().ToArray() : tmp;
        }

        public static byte[] From16(short arg)
        {
            var tmp = BitConverter.GetBytes(arg);
            return BitConverter.IsLittleEndian ? tmp.Reverse().ToArray() : tmp;
        }
    }
}