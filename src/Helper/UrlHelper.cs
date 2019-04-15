using System;
using System.Linq;

namespace Morph.Server.Sdk.Helper
{
    public static class UrlHelper
    {
        public static string JoinUrl(params string[] urlParts)
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

                    // url path can't contains '.' because in will break url 
                    if (u.All(x => x == '.'))
                    {
                        throw new Exception("Wrong characters sequence. You can't use only '.' in the path segment.");
                    }

                    result += Uri.EscapeDataString(u);
                }
            }
            return result;
        }
    }


}
