using System;
using System.Linq;
using System.Globalization;
using System.Text.RegularExpressions;
namespace MW.EditorTools.Modules.ComponentManager.Pack {
 internal static class PriorityOrder {
  internal const string DefaultPattern=@"^(?<pack>\S+) (?<zone>[^_]+)_(?<house>\d+)_L(?<level>[1-5])_(?<width>\d+)x(?<depth>\d+)$";
  internal sealed class Key {internal string Pack; internal int House,Level,Width,Depth;}
  internal static Key Parse(string name,string pattern){
   return Parse(name,CreatePattern(pattern));
  }
  private static Regex CreatePattern(string pattern){
   if(string.IsNullOrWhiteSpace(pattern)||pattern.Length>500)throw new ArgumentException("Enter a naming pattern (maximum 500 characters).");
   return new Regex(pattern,RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(100));
  }
  private static Key Parse(string name,Regex regex){
   var match=regex.Match(name??"");if(!match.Success)throw new ArgumentException("Name does not match the numbering pattern: "+name);
   Func<string,int> read=g=>{int value;if(!int.TryParse(match.Groups[g].Value,NumberStyles.None,CultureInfo.InvariantCulture,out value)||value<1)throw new ArgumentException("Pattern requires positive house, level, width and depth groups.");return value;};
   return new Key{Pack=match.Groups["pack"].Value,House=read("house"),Level=read("level"),Width=read("width"),Depth=read("depth")};
  }
  internal static int[] Sort(string[] names,string pattern){
   if(names==null||names.Length==0||names.Length>1000)throw new ArgumentException("Choose between 1 and 1000 assets.");
   if(names.Distinct(StringComparer.Ordinal).Count()!=names.Length)throw new ArgumentException("Duplicate asset names; choose a unique asset for each name.");
   if(pattern==null)return Enumerable.Range(0,names.Length).ToArray();
   var budget=System.Diagnostics.Stopwatch.StartNew();
   var regex=CreatePattern(pattern);
   var keys=names.Select(n=>{if(budget.ElapsedMilliseconds>1000)throw new ArgumentException("Naming pattern took too long. Use a simpler pattern.");return Parse(n,regex);}).ToArray();
   return Enumerable.Range(0,names.Length).OrderBy(i=>keys[i].Pack,StringComparer.OrdinalIgnoreCase).ThenBy(i=>keys[i].House).ThenBy(i=>keys[i].Level).ThenBy(i=>keys[i].Width).ThenBy(i=>keys[i].Depth).ThenBy(i=>names[i],StringComparer.Ordinal).ToArray();
  }
  internal static int[] Values(int count,int start,int step){if(count<1||count>1000||step<=0)throw new ArgumentException("Choose assets and a positive increment.");return Enumerable.Range(0,count).Select(i=>checked(start+checked(i*step))).ToArray();}
 }
}
