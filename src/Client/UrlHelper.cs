using System;

namespace Morph.Server.Sdk.Client
{
    internal static class UrlHelper
    {
        internal static string JoinUrl(params string[] urlParts)
        {
            var result = string.Empty;
            for (var i = 0; i < urlParts.Length; i++)
            {
                var p = urlParts[i];
                if (p == null)
                    continue;

                p = p.Replace('\\', '/');
                p = p.Trim(new[] { '/' });
                if (string.IsNullOrWhiteSpace(p))
                    continue;
                var t = p.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var u in t)
                {
                    if (result != string.Empty)
                        result += "/";
                    result += Uri.EscapeDataString(u);
                }
            }
            return result;
        }
    }


}
