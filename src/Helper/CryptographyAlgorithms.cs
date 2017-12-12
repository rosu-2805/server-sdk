using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Morph.Server.Sdk.Helper
{
    internal static class CryptographyAlgorithms
    {
        public static SHA256 CreateSHA256()
        {
            try
            {
                return SHA256.Create();
            }

            catch (System.Reflection.TargetInvocationException)
            {

                return new SHA256CryptoServiceProvider();
            }
        }
    }
}
