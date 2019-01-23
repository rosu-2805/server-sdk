using Morph.Server.Sdk.Dto.Commands;
using Morph.Server.Sdk.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace Morph.Server.Sdk.Helper
{
    internal static class JsonSerializationHelper
    {
        public static T Deserialize<T>(string input)
        {
            try
            {
                var serializer = new DataContractJsonSerializer(typeof(T));
                var d = Encoding.UTF8.GetBytes(input);
                using (var ms = new MemoryStream(d))
                {
                    var data = (T)serializer.ReadObject(ms);
                    return data;
                }
            }
            catch (Exception ex)
            {
                throw new ResponseParseException("An error occurred while deserializing the response: "+ ex.Message, input);                
            }
        }
        public static string Serialize<T>(T obj)
        {

            var serializer = new DataContractJsonSerializer(typeof(T));            
            using (var ms = new MemoryStream())
            {
                serializer.WriteObject(ms, obj);
                byte[] json = ms.ToArray();
                return Encoding.UTF8.GetString(json, 0, json.Length);
            }            

        }

        
    }
}
