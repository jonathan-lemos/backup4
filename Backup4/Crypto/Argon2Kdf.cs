using System;
using System.Collections.Generic;
using System.Linq;
using Backup4.Functional;
using Backup4.Misc;
using Sodium;

namespace Backup4.Crypto
{
    public class Argon2Kdf : IDisposable, IKdf
    {
        public static readonly byte[] MagicHeader = {0xB4, (byte) 'A', (byte) 'G', (byte) '2'};
        public const uint Version = 0x0;
        public byte[] Salt { get; set; } = Random.Bytes(16);
        public uint OpsLimit { get; set; } = 12;
        public uint MemLimit { get; set; } = 1024 * 1024 * 1024;


        public static readonly uint BaseLength =
            (uint) (MagicHeader.Length + sizeof(uint) + sizeof(uint) + sizeof(uint) +
                    sizeof(uint) + sizeof(uint));

        public uint HeaderLength => (uint) (BaseLength + Salt.Length);

        public void Dispose()
        {
            Array.Clear(Salt, 0, Salt.Length);
        }

        public byte[] Derive(byte[] bytes, int outputLen)
        {
            return PasswordHash.ArgonHashBinary(bytes, Salt, OpsLimit, (int) MemLimit, outputLen,
                PasswordHash.ArgonAlgorithm.Argon_2ID13);
        }

        public byte[] Serialize()
        {
            var bytes = new List<byte>();
            bytes.AddRange(MagicHeader);
            bytes.AddRange(BitConvert.From32(Version));
            bytes.AddRange(BitConvert.From32(HeaderLength));
            bytes.AddRange(BitConvert.From32(OpsLimit));
            bytes.AddRange(BitConvert.From32(MemLimit));
            bytes.AddRange(BitConvert.From32((uint) Salt.Length));
            bytes.AddRange(Salt);
            return bytes.ToArray();
        }

        public static Either<Argon2Kdf, int?> Deserialize(byte[] bytes)
        {
            var ret = new Argon2Kdf();

            if (bytes.Length < 8)
            {
                return 12;
            }

            var ptr = 0;

            if (new Slice(bytes, ptr, 4) != MagicHeader)
            {
                return (int?) null;
            }

            ptr = 4;

            if (!BitConvert.To32(new Slice(bytes, ptr, 4).ToArray(), out var version))
            {
                return 12;
            }

            if (version != 0)
            {
                return (int?) null;
            }

            ptr += 4;

            if (!BitConvert.To32(new Slice(bytes, ptr, 4).ToArray(), out var len))
            {
                return 12;
            }

            if (len < 12)
            {
                return (int?)null;
            }

            ptr += 4;

            if (!BitConvert.To32(new Slice(bytes, ptr, 4).ToArray(), out var opsLimit))
            {
                return (int?) len;
            }

            ptr += 4;
            ret.OpsLimit = opsLimit;

            if (!BitConvert.To32(new Slice(bytes, ptr, 4).ToArray(), out var memLimit))
            {
                return (int?) len;
            }

            ptr += 4;
            ret.MemLimit = memLimit;

            if (!BitConvert.To32(new Slice(bytes, ptr, 4).ToArray(), out var saltLen))
            {
                return (int?) len;
            }

            if (saltLen != len - BaseLength)
            {
                return (int?) null;
            }

            ptr += 4;

            var rest = new Slice(bytes, ptr, (int) saltLen);
            if (rest.Length != saltLen)
            {
                return (int?) len;
            }

            ret.Salt = rest.ToArray();

            return ret;
        }
    }
}