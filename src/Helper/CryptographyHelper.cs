using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Morph.Server.Sdk.Helper
{
    internal static class CryptographyHelper
    {
        public static byte[] GenerateRandomSequence(int len)
        {
            if (len <= 0)
            {
                throw new ArgumentOutOfRangeException("{0} must be positive", nameof(len));
            }
            using (RandomNumberGenerator rng = new RNGCryptoServiceProvider())
            {
                byte[] randomData = new byte[len];
                rng.GetBytes(randomData);
                return randomData;
            }

        }

        public static string CalculateSha256HEX(string input)
        {
            using (var sha256 = CryptographyAlgorithms.CreateSHA256())
            {
                Encoding enc = Encoding.UTF8;
                Byte[] result = sha256.ComputeHash(enc.GetBytes(input));
                return ConvertHelper.ByteArrayToHexString(result);
            }
        }
    }
}
