using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Backup4.Functional;
using Backup4.Misc;
using Sodium;

namespace Backup4.Crypto
{
    public class Argon2Kdf : IDisposable, IKdf
    {
        public const string KdfName = "argon2id";

        public static readonly byte[] MagicHeader = {0xB4, (byte) 'A', (byte) 'G', (byte) '2'};
        public const int Version = 0x0;
        public byte[] Salt { get; set; } = Random.Bytes(16);
        public int OpsLimit { get; set; } = 12;
        public int MemLimit { get; set; } = 1024 * 1024 * 1024;

        public void Dispose()
        {
            Array.Clear(Salt, 0, Salt.Length);
        }

        public byte[] Derive(byte[] bytes, int outputLen)
        {
            return PasswordHash.ArgonHashBinary(bytes, Salt, OpsLimit, MemLimit, outputLen,
                PasswordHash.ArgonAlgorithm.Argon_2ID13);
        }

        public IDictionary<string, object> Properties =>
            new Dictionary<string, object>
            {
                ["kdf"] = KdfName,
                ["version"] = Version,
                ["opslimit"] = OpsLimit,
                ["memlimit"] = MemLimit,
                ["salt"] = Salt.ToBase64()
            };

        public static Result<Argon2Kdf, InvalidDataException> Deserialize(IDictionary<string, object> properties)
        {
            var ret = new Argon2Kdf();

            if (!properties.TryGet<string>("kdf", out var kdf))
            {
                return new InvalidDataException("The kdf dictionary does not have the required 'kdf' field.");
            }

            if (kdf != KdfName)
            {
                return new InvalidDataException($"The kdf needs to be '{KdfName}' (was '{kdf}').");
            }

            if (!properties.TryGet<int>("version", out var version))
            {
                return new InvalidDataException("The kdf dictionary does not have the required 'version' field.");
            }

            if (version != Version)
            {
                return new InvalidDataException(
                    $"Only version 0x{Version:X4} is supported at the moment (was 0x{version:X4})");
            }

            if (!properties.TryGet<int>("opslimit", out var opslimit))
            {
                return new InvalidDataException("The kdf dictionary does not have the required 'opslimit' field.");
            }

            ret.OpsLimit = opslimit;

            if (!properties.TryGet<int>("memlimit", out var memlimit))
            {
                return new InvalidDataException("The kdf dictionary does not have the required 'memlimit' field.");
            }

            ret.MemLimit = memlimit;

            if (!properties.TryGet<string>("salt", out var salt))
            {
                return new InvalidDataException("The kdf dictionary does not have the required 'salt' field.");
            }

            var fb64 = salt.FromBase64();
            if (!fb64)
            {
                return new InvalidDataException("The salt is not a valid base64 string.");
            }

            ret.Salt = fb64.Value;

            return ret;
        }
    }
}