using Morph.Server.Sdk.Dto.Commands;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Morph.Server.Sdk.Client
{

    public interface IJsonSerializer
    {
         T Deserialize<T>(string input)
            where T : new ();

         string Serialize<T>(T obj);
    }
}
