using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Morph.Server.Sdk.Helper
{
    internal static class ConvertHelper
    {
        public static string ByteArrayToHexString(byte[] input)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            var sb = new StringBuilder(input.Length * 2);
            foreach (byte b in input)
            {
                sb.AppendFormat("{0:x2}", b);
            }
            return sb.ToString();
        }
    }
}
