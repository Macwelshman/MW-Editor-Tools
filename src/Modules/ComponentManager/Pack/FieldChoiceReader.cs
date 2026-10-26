using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MW.EditorTools.Modules.ComponentManager.Pack {
    internal static class FieldChoiceReader {
        internal static Dictionary<int,string[]> Read(string json) {
            if(string.IsNullOrEmpty(json)||json.Length>100000)throw new ArgumentException("Field choices are empty or too large.");
            JObject payload;
            using(var text=new StringReader(json))using(var reader=new JsonTextReader(text){MaxDepth=8,DateParseHandling=DateParseHandling.None}){
                payload=JObject.Load(reader,new JsonLoadSettings{DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error});
                if(reader.Read())throw new ArgumentException("Unexpected data after field choices.");
            }
            var choices=payload["choices"] as JArray;
            if(choices==null||choices.Count>256)throw new ArgumentException("Expected a field choices array.");
            var result=new Dictionary<int,string[]>();
            foreach(var item in choices){
                var choice=item as JObject;var index=choice?["index"];var names=choice?["names"] as JArray;
                if(index==null||index.Type!=JTokenType.Integer||names==null||names.Count>256)throw new ArgumentException("Invalid component field choice.");
                long number=index.Value<long>();
                if(number<0||number>int.MaxValue||result.ContainsKey((int)number))throw new ArgumentException("Invalid or repeated component index.");
                if(names.Any(name=>name.Type!=JTokenType.String||string.IsNullOrEmpty(name.Value<string>())||name.Value<string>().Length>256))throw new ArgumentException("Invalid field name.");
                var fields=names.Values<string>().ToArray();
                if(fields.Distinct(StringComparer.Ordinal).Count()!=fields.Length)throw new ArgumentException("Repeated field name.");
                result.Add((int)number,fields);
            }
            return result;
        }
    }
}
