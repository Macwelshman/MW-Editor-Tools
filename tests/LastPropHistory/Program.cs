using System;using System.Collections.Generic;using System.Linq;
namespace Game.Prefabs {
 class ComponentBase {public bool active=true;public int GetInstanceID()=>GetHashCode();}
 class ObjectSubObjects:ComponentBase {}
 class PrefabBase {public string name="Test";public List<ComponentBase> components=new List<ComponentBase>();public T AddComponent<T>() where T:ComponentBase,new(){var value=new T();components.Add(value);return value;}}
}
namespace Game.Tools {static class ApplyPrefabsSystem {public static void RemoveComponent<T>(Game.Prefabs.PrefabBase asset){asset.components.RemoveAll(value=>value is T);}}}
namespace MW.EditorTools {static class Mod {public static Logger Log=new Logger();public class Logger{public void Info(string value){}}}}
namespace MW.EditorTools.Modules.BulldozerRecovery {
 using Game.Prefabs;
 internal sealed partial class BulldozerTrace {
 class Snapshot {public bool ReferencesValid=true;public object Materialize()=>null;public bool Matches(object value)=>true;}
 class FieldValue {public ComponentBase Owner;}
 class Payload {public ComponentBase[] Components;public bool[] Active;public int Objects;public List<FieldValue> Fields=new List<FieldValue>();public Dictionary<string,Snapshot> Values=new Dictionary<string,Snapshot>();public bool Shape(PrefabBase asset)=>asset.components.SequenceEqual(Components);}
 class Record {public PrefabBase Asset;public Payload Before,After;}
 readonly MW.EditorTools.History.TransactionHistory<Record> records=new MW.EditorTools.History.TransactionHistory<Record>(20);
 bool CanArm=true;PrefabBase armedAsset;string Status;void Arm(){}
 static int checks;static void Check(bool value){checks++;if(!value)throw new Exception("Assertion "+checks);}
 static Payload Image(ComponentBase retained,ObjectSubObjects component){var p=new Payload{Components=component==null?new[]{retained}:new ComponentBase[]{retained,component},Active=component==null?new[]{true}:new[]{true,true}};if(component!=null)p.Fields.Add(new FieldValue{Owner=component});return p;}
 public static void Main(){
  var t=new BulldozerTrace();var asset=new PrefabBase();var retained=new ComponentBase();var old=new ObjectSubObjects();asset.components.Add(retained);
  var first=new Record{Asset=asset,Before=Image(retained,old),After=Image(retained,old)};var last=new Record{Asset=asset,Before=Image(retained,old),After=Image(retained,null)};
  var unrelated=new Record{Asset=new PrefabBase(),Before=Image(retained,old),After=Image(retained,old)};
  t.records.Record(unrelated);t.records.Record(first);t.records.Record(last);t.records.Undo(_=>true);
  Check(t.records.All.Length==3&&t.records.UndoCount==2&&t.records.RedoCount==1);
  var copy=t.records.All;copy[0]=null;Check(t.records.All[0]!=null);
  t.AlignLast(last.Before,asset);var created=asset.components.OfType<ObjectSubObjects>().Single();Check(!ReferenceEquals(old,created));
  Check(first.Before.Shape(asset)&&first.After.Shape(asset)&&last.Before.Shape(asset));
  Check(ReferenceEquals(first.Before.Fields[0].Owner,created)&&ReferenceEquals(first.After.Fields[0].Owner,created));
  Check(ReferenceEquals(unrelated.Before.Components[1],old)&&ReferenceEquals(unrelated.Before.Fields[0].Owner,old));
  Check(last.After.Components.Length==1&&last.After.Fields.Count==0);
  t.AlignLast(last.After,asset);Check(asset.components.Count==1&&ReferenceEquals(asset.components[0],retained));
  t.AlignLast(last.Before,asset);var next=asset.components.OfType<ObjectSubObjects>().Single();Check(!ReferenceEquals(next,created));
  Check(first.Before.Shape(asset)&&first.After.Shape(asset)&&last.Before.Shape(asset));
  Check(ReferenceEquals(first.Before.Fields[0].Owner,next)&&ReferenceEquals(last.Before.Fields[0].Owner,next));
  Check(t.records.UndoCount==2&&t.records.RedoCount==1);
  Console.WriteLine("PASS: "+checks+" actual-source last-component history rebinding assertions; native timing requires runtime validation.");
 }
 }
}

