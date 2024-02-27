using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using Morph.Server.Sdk.Dto;
using Morph.Server.Sdk.Exceptions;

namespace Morph.Server.Sdk.Client
{
    public class MorphDataContractJsonSerializer: IJsonSerializer
    {
        public T Deserialize<T>(string input)
            where T: new()
        {
            try
            {
                var tType = typeof(T);
                if ( tType == typeof(NoContentResult))
                {
                    return new T();
                }
                var serializer = new DataContractJsonSerializer(typeof(T), 
                    new DataContractJsonSerializerSettings() { UseSimpleDictionaryFormat = true });
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
        
        public string Serialize<T>(T obj)
        {

            var serializer = new DataContractJsonSerializer(typeof(T));            
            using (var ms = new MemoryStream())
            {
                serializer.WriteObject(ms, obj);
                byte[] json = ms.ToArray();
                return Encoding.UTF8.GetString(json, 0, json.Length);
            }            

        }
        
        public string Serialize(Type type, object obj)
        {
            var serializer = new DataContractJsonSerializer(type);            
            using (var ms = new MemoryStream())
            {
                serializer.WriteObject(ms, obj);
                byte[] json = ms.ToArray();
                return Encoding.UTF8.GetString(json, 0, json.Length);
            }            
        }

        
    }
}