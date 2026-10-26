using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using MW.EditorTools.Modules.ComponentManager.Pack;
class Program {
    private sealed class Request {public string action="",start="100";public int[] ids=null;}
    private sealed class Row {public int id=0,current=5;public string name="Test asset";public bool eligible=true;}
    private sealed class State {public string scanToken="scan1";public Row[] rows=new[]{new Row()};}
    static int count;
    static void Check(bool ok,string name){if(!ok)throw new Exception(name);count++;}
    static void Main(string[] args){
        var scan=PackJson.Read<Request>("{\"action\":\"scan\"}");Check(scan.action=="scan"&&scan.start=="100","scan request and native-independent defaults");
        var preview=PackJson.Read<Request>("{\"action\":\"priorityPreview\",\"ids\":[0,2],\"start\":\"101\"}");Check(preview.ids.SequenceEqual(new[]{0,2})&&preview.start=="101","preview request arrays");
        var wire=PackJson.Write(new State());var roundtrip=PackJson.Read<State>(wire);Check(roundtrip.rows.Length==1&&roundtrip.rows[0].name=="Test asset"&&roundtrip.rows[0].eligible,"nested private state row array reaches UI");
        Check(Newtonsoft.Json.Linq.JObject.Parse(wire)["rows"][0]["current"].Value<int>()==5,"priority row properties emitted");
        var choices=FieldChoiceReader.Read("{\"choices\":[{\"index\":0,\"names\":[\"m_Group\",\"m_Priority\"]},{\"index\":2,\"names\":[]}]}");
        Check(choices[0].SequenceEqual(new[]{"m_Group","m_Priority"}),"Group/Priority");Check(choices[2].Length==0,"explicit no fields");
        Check(FieldChoiceReader.Read("{\"choices\":[]}").Count==0,"empty overrides");
        foreach(var invalid in new[]{"", "{}", "null", "[]", "{\"choices\":null}","{\"choices\":[null]}","{\"choices\":[{\"index\":-1,\"names\":[]}]}","{\"choices\":[{\"index\":0.5,\"names\":[]}]}","{\"choices\":[{\"index\":\"0\",\"names\":[]}]}","{\"choices\":[{\"index\":2147483648,\"names\":[]}]}","{\"choices\":[{\"index\":0}]}","{\"choices\":[{\"index\":0,\"names\":[null]}]}","{\"choices\":[{\"index\":0,\"names\":[42]}]}","{\"choices\":[{\"index\":0,\"names\":[\"\"]}]}","{\"choices\":[{\"index\":0,\"names\":[\"m_Group\",\"m_Group\"]}]}","{\"choices\":[{\"index\":0,\"names\":[]},{\"index\":0,\"names\":[]}]}","{\"choices\":[],\"choices\":[]}","{\"choices\":[]} {}",new string(' ',100001)}){
            bool refused=false;try{FieldChoiceReader.Read(invalid);}catch(ArgumentException){refused=true;}catch(Newtonsoft.Json.JsonException){refused=true;}Check(refused,"malformed/oversized request refused");
        }
        if(args.Length>0){
            var payloads=Newtonsoft.Json.JsonConvert.DeserializeObject<string[]>(File.ReadAllText(args[0]));
            int masked=0;foreach(var command in payloads){var parts=command.Split('|');if(parts.Length==4){var parsed=FieldChoiceReader.Read(parts[3]);Check(parsed.Count>0,"actual UI payload parsed");masked++;}}
            Check(masked>=3,"Copy and Copy All payload coverage");
        }
        Console.WriteLine($"PASS: {count} actual-source field-choice parser checks.");
    }
}
