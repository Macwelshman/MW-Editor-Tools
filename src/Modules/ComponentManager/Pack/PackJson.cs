using System;
using System.IO;
using Newtonsoft.Json;
namespace MW.EditorTools.Modules.ComponentManager.Pack {
    internal static class PackJson {
        private static JsonSerializer Serializer()=>JsonSerializer.Create(new JsonSerializerSettings{TypeNameHandling=TypeNameHandling.None,MaxDepth=16});
        internal static T Read<T>(string json){
            if(string.IsNullOrEmpty(json)||json.Length>100000)throw new ArgumentException("Pack request is empty or too large.");
            using(var text=new StringReader(json))using(var reader=new JsonTextReader(text){MaxDepth=16,DateParseHandling=DateParseHandling.None}){
                var result=Serializer().Deserialize<T>(reader);
                if(reader.Read())throw new ArgumentException("Unexpected data after pack request.");
                return result;
            }
        }
        internal static string Write(object value){using(var text=new StringWriter(System.Globalization.CultureInfo.InvariantCulture)){using(var writer=new JsonTextWriter(text))Serializer().Serialize(writer,value);return text.ToString();}}
    }
}
