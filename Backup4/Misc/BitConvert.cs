using System;
using System.Linq;

namespace Backup4.Misc
{
    public static class BitConvert
    {
        public static bool To64(byte[] bytes, out ulong res)
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

            res = BitConverter.ToUInt64(bytes);
            return true;
        }

        public static bool To32(byte[] bytes, out uint res)
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

            res = BitConverter.ToUInt32(bytes);
            return true;
        }

        public static bool To16(byte[] bytes, out ushort res)
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

            res = BitConverter.ToUInt16(bytes);
            return true;
        }

        public static byte[] From64(ulong arg)
        {
            var tmp = BitConverter.GetBytes(arg);
            return BitConverter.IsLittleEndian ? tmp.Reverse().ToArray() : tmp;
        }

        public static byte[] From32(uint arg)
        {
            var tmp = BitConverter.GetBytes(arg);
            return BitConverter.IsLittleEndian ? tmp.Reverse().ToArray() : tmp;
        }

        public static byte[] From16(ushort arg)
        {
            var tmp = BitConverter.GetBytes(arg);
            return BitConverter.IsLittleEndian ? tmp.Reverse().ToArray() : tmp;
        }
    }
}