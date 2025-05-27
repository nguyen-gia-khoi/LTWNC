using System;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace LTWNC.Middleware
{
    public class HashPassword
    {
        private const int _iterationCount = 10000;

        public static string CreateHash(string password)
        {
            int saltSize = 128 / 8; // 16 bytes salt
            var salt = new byte[saltSize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            var subkey = KeyDerivation.Pbkdf2(
                password,
                salt,
                KeyDerivationPrf.HMACSHA512,
                _iterationCount,
                256 / 8); // 32 bytes subkey

            var outputBytes = new byte[13 + salt.Length + subkey.Length];
            outputBytes[0] = 0x01;
            WriteNetworkByteOrder(outputBytes, 1, (uint)KeyDerivationPrf.HMACSHA512);
            WriteNetworkByteOrder(outputBytes, 5, (uint)_iterationCount);
            WriteNetworkByteOrder(outputBytes, 9, (uint)saltSize);
            Buffer.BlockCopy(salt, 0, outputBytes, 13, salt.Length);
            Buffer.BlockCopy(subkey, 0, outputBytes, 13 + saltSize, subkey.Length);

            return Convert.ToBase64String(outputBytes);
        }

        public static bool VerifyHashedPassword(string password, string hashed)
        {
            try
            {
                var hashedPassword = Convert.FromBase64String(hashed);

                var prf = (KeyDerivationPrf)ReadNetworkByteOrder(hashedPassword, 1);
                var iterCount = (int)ReadNetworkByteOrder(hashedPassword, 5);
                var saltLength = (int)ReadNetworkByteOrder(hashedPassword, 9);

                if (saltLength < 128 / 8) return false;

                var salt = new byte[saltLength];
                Buffer.BlockCopy(hashedPassword, 13, salt, 0, salt.Length);

                var storedSubkey = new byte[hashedPassword.Length - 13 - salt.Length];
                Buffer.BlockCopy(hashedPassword, 13 + salt.Length, storedSubkey, 0, storedSubkey.Length);

                var generatedSubkey = KeyDerivation.Pbkdf2(password, salt, prf, iterCount, storedSubkey.Length);

                return CryptographicOperations.FixedTimeEquals(storedSubkey, generatedSubkey);
            }
            catch
            {
                return false;
            }
        }

        private static void WriteNetworkByteOrder(byte[] buffer, int offset, uint value)
        {
            buffer[offset + 0] = (byte)(value >> 24);
            buffer[offset + 1] = (byte)(value >> 16);
            buffer[offset + 2] = (byte)(value >> 8);
            buffer[offset + 3] = (byte)(value);
        }

        private static uint ReadNetworkByteOrder(byte[] buffer, int offset)
        {
            return ((uint)(buffer[offset + 0]) << 24)
                 | ((uint)(buffer[offset + 1]) << 16)
                 | ((uint)(buffer[offset + 2]) << 8)
                 | ((uint)(buffer[offset + 3]));
        }
    }
}
