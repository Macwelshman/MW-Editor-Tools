using System;
using System.Linq;
using MW.EditorTools.Services;
using MW.EditorTools.History;
using MW.EditorTools.Settings;
namespace MW.EditorTools { internal static class Mod { internal static readonly Logger Log = new Logger(); internal sealed class Logger { public void Warn(string value) {} } } }
internal static class Program {
    private static int checks;
    private static void Check(bool result, string name) { if(!result) throw new Exception(name); checks++; }
    private static int Shortcut(bool enabled=true,bool available=true,bool typing=false,bool dialog=false,bool repeat=false,bool ctrl=true,bool shift=false,bool alt=false,bool meta=false,bool undo=true,bool redo=true) =>
        ShortcutPolicy.Resolve(enabled,available,typing,dialog,repeat,ctrl,shift,alt,meta,undo,redo);
    public static void Main() {
        var namedHide=new EditorToolsSettings {hideUIKey="Home",hideUIModifiers=0};namedHide.Normalize();
        Check(namedHide.hideUIKey=="HOME"&&namedHide.hideUIModifiers==0,"standalone named key survives normalization");
        using(var namedJson=System.Text.Json.JsonDocument.Parse(namedHide.ToJson()))Check(namedJson.RootElement.GetProperty("hideUIKey").GetString()=="HOME"&&namedJson.RootElement.GetProperty("hideUIModifiers").GetInt32()==0,"standalone key persists");
        var migrated=new EditorToolsSettings {hideUIKey="H",hideUIModifiers=1};
        migrated.MigrateModifiers("{\"hideUIKey\":\"H\",\"hideUIModifiers\":1}");Check(migrated.hideUIKey=="BACKQUOTE"&&migrated.hideUIModifiers==0,"previous default migrates");
        migrated.hideUIKey="HOME";migrated.MigrateModifiers("{\"hideUIKey\":\"HOME\",\"hideUIModifiers\":0}");Check(migrated.hideUIKey=="HOME","custom binding retained");
        var hide=new EditorToolsSettings();
        Check(hide.hideUIShortcut&&hide.hideUIKey=="BACKQUOTE"&&hide.hideUIModifiers==0&&!hide.HideUIConflict(),"hide UI defaults");
        hide.hideUIKey="Z";hide.hideUIModifiers=1;Check(hide.HideUIConflict()&&hide.BrowserConflict("Z",1),"Undo collision suppresses both");
        hide.hideUIKey="B";hide.hideUIModifiers=4;hide.browserShortcuts=true;Check(hide.HideUIConflict()&&hide.BrowserConflict("B",4,0),"browser collision suppresses both");
        hide.hideUIKey="V";hide.hideUIModifiers=3;Check(hide.HideUIConflict(),"clipboard collision refused");
        hide.hideUIKey="invalid";hide.hideUIModifiers=99;hide.Normalize();Check(hide.hideUIKey=="BACKQUOTE"&&hide.hideUIModifiers==0,"invalid hide binding normalizes");
        hide.hideUIShortcut=false;hide.MigrateModifiers("{}");Check(hide.hideUIShortcut,"legacy settings receive default");
        hide.hideUIShortcut=false;hide.MigrateModifiers("{\"hideUIShortcut\":false,\"hideUIKey\":\"J\",\"hideUIModifiers\":4}");Check(!hide.hideUIShortcut,"saved disabled preference retained");
        using(var saved=System.Text.Json.JsonDocument.Parse(hide.ToJson()))Check(!saved.RootElement.GetProperty("hideUIShortcut").GetBoolean()&&saved.RootElement.GetProperty("hideUIKey").GetString()=="BACKQUOTE","hide settings serialize");
        Check(Shortcut()==0,"Undo"); Check(Shortcut(shift:true)==1,"Redo");
        Check(Shortcut(enabled:false)==-1,"disabled"); Check(Shortcut(available:false)==-1,"outside editor");
        Check(Shortcut(typing:true)==-1,"typing"); Check(Shortcut(dialog:true)==-1,"dialog");
        Check(Shortcut(repeat:true)==-1,"repeat"); Check(Shortcut(ctrl:false)==-1,"control required");
        Check(Shortcut(alt:true)==-1,"alt rejected"); Check(Shortcut(meta:true)==-1,"meta rejected");
        Check(Shortcut(undo:false)==-1,"wrong undo key"); Check(Shortcut(shift:true,redo:false)==-1,"wrong redo key");
        var settings = new EditorToolsSettings {historyDepth=900,undoKey="a",redoKey="F12",windowX=float.NaN,windowY=9};
        settings.Normalize();
        Check(settings.historyDepth==50,"limit upper bound"); Check(settings.undoKey=="A" && settings.redoKey=="Z","key validation");
        Check(settings.windowX==.4f && settings.windowY==1,"position validation");
        settings.historyDepth=0; settings.Normalize(); Check(settings.historyDepth==20,"limit lower bound");
        var previousCulture = System.Globalization.CultureInfo.CurrentCulture;
        try {
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("fr-FR");
            settings.windowX=.4f; settings.undoKey="\""; settings.confirmation=false;
            using var json = System.Text.Json.JsonDocument.Parse(settings.ToJson());
            var root = json.RootElement;
            Check(root.EnumerateObject().Count()==27,"all frontend settings included");
            Check(root.GetProperty("windowX").GetSingle()==.4f,"JSON decimal invariant");
            Check(root.GetProperty("undoKey").GetString()=="Z","unsafe key normalized before JSON");
            Check(!root.GetProperty("confirmation").GetBoolean() && root.GetProperty("recovery").GetBoolean(),"JSON boolean values");
        } finally { System.Globalization.CultureInfo.CurrentCulture=previousCulture; }
        Check(settings.ApplyOptions(false,"37"," x ","r"),"text limit change clears history");
        Check(settings.historyDepth==37 && settings.undoKey=="X" && settings.redoKey=="R" && !settings.shortcuts,"text options apply normalized");
        Check(!settings.ApplyOptions(true,"37","X","R"),"unchanged limit retains history");
        foreach (var bad in new[]{"19","51","abc","20.5","999999999999"}) {
            Check(!settings.ApplyOptions(true,bad,"F12","Ctrl+Z") && settings.historyDepth==37 && settings.undoKey=="X" && settings.redoKey=="R","invalid text retains settings");
        }
        var browserDefaults=new[]{"B","D","F","A","E","S"};var freshBrowser=new EditorToolsSettings();
        for(int i=0;i<6;i++)Check(freshBrowser.BrowserKey(i)==browserDefaults[i]&&freshBrowser.BrowserModifiers(i)==4,"new Alt-only defaults");
        for(int i=0;i<6;i++)freshBrowser.SetBrowserShortcut(i,i==1?"P":browserDefaults[i],5);
        freshBrowser.MigrateModifiers(freshBrowser.ToJson());
        for(int i=0;i<6;i++)Check(freshBrowser.BrowserKey(i)==browserDefaults[i]&&freshBrowser.BrowserModifiers(i)==4,"legacy defaults migrate");
        freshBrowser.SetBrowserShortcut(1,"Q",7);freshBrowser.MigrateModifiers(freshBrowser.ToJson());Check(freshBrowser.BrowserKey(1)=="Q"&&freshBrowser.BrowserModifiers(1)==7,"custom browser shortcut retained");
        settings.propsKey="\"";settings.propsModifiers=99;settings.Normalize();Check(settings.propsKey=="D"&&settings.propsModifiers==4,"invalid browser settings normalised"); settings.SetBrowserShortcut(1,"Q",7);using(var browserJson=System.Text.Json.JsonDocument.Parse(settings.ToJson()))Check(browserJson.RootElement.GetProperty("propsKey").GetString()=="Q"&&browserJson.RootElement.GetProperty("propsModifiers").GetInt32()==7,"browser JSON saves complete combination"); settings.MigrateModifiers("{}");Check(settings.propsKey=="D"&&settings.propsModifiers==4,"old file migrates browser defaults");
        var conflicts=new EditorToolsSettings{browserShortcuts=true};Check(!conflicts.BrowserConflict("D",4,1),"distinct browser shortcut allowed");conflicts.SetBrowserShortcut(2,"D",4);Check(conflicts.BrowserConflict("D",4,1)&&conflicts.BrowserConflict("D",4,2),"both duplicate browser shortcuts suppressed");conflicts.SetBrowserShortcut(1,"Z",1);Check(conflicts.BrowserConflict("Z",1,1)&&conflicts.BrowserConflict("Z",1),"recovery and browser conflict suppressed both ways");conflicts.shortcuts=false;Check(!conflicts.BrowserConflict("Z",1,1),"disabled recovery does not reserve its binding");conflicts.browserShortcuts=false;Check(!conflicts.BrowserConflict("Z",1),"disabled browser does not reserve its binding");
        var history = new TransactionHistory<int>(50); for(int i=0;i<50;i++) history.Record(i);
        for(int expected=0;expected<16;expected++) for(int actual=0;actual<16;actual++) {
            var result=ShortcutPolicy.Resolve(true,true,false,false,false,(actual&1)!=0,(actual&2)!=0,(actual&4)!=0,(actual&8)!=0,true,false,expected,3);
            Check(result==(expected==actual?0:-1),"exact configurable modifier mask");
        }
        Check(ShortcutPolicy.Resolve(true,true,false,false,false,true,false,true,false,true,true,5,5)==-1,"identical shortcut suppressed");
        Check(ShortcutPolicy.Resolve(true,true,true,false,false,false,false,true,false,true,false,4,3)==-1,"custom shortcut typing suppressed");
        settings.undoModifiers=0;settings.redoModifiers=0;settings.MigrateModifiers("{\"historyDepth\":37}");
        Check(settings.undoModifiers==1 && settings.redoModifiers==3,"old settings get default modifiers");
        settings.undoModifiers=0;settings.redoModifiers=8;settings.MigrateModifiers("{\"undoModifiers\":0,\"redoModifiers\":8}");
        Check(settings.undoModifiers==0 && settings.redoModifiers==8,"configured modifiers preserved");
        history.SetDepth(20); Check(history.UndoCount==20 && history.Recent[0]==30,"retains newest undo");
        for(int i=0;i<20;i++) Check(history.Undo(_=>true),"undo after resize");
        history.SetDepth(5); Check(history.RedoCount==5 && history.NextRedo==30,"retains immediate redo");
        Check(!history.Redo(_=>false) && history.RedoCount==5,"failed redo preserves cursor");
        var queue=new DeferredShortcut();int executions=0,last=-1;
        Action<int> dispatch=value=>{executions++;last=value;};
        Check(!queue.Request(-1,10)&&!queue.Request(2,10),"invalid shortcut rejected");
        Check(queue.Request(0,10),"undo queued");
        Check(!queue.Request(1,10),"repeated or competing request cannot overwrite pending undo");
        queue.Drain(10,true,dispatch);Check(executions==0,"no structural operation in input update frame");
        queue.Drain(11,true,dispatch);Check(executions==1&&last==0,"undo dispatched next frame");
        queue.Drain(12,true,dispatch);Check(executions==1,"consumed request cannot repeat");
        queue.Request(1,20);queue.Drain(21,false,dispatch);queue.Drain(22,true,dispatch);Check(executions==1,"invalid focus or context discards request permanently");
        queue.Request(0,30);queue.Cancel();queue.Drain(31,true,dispatch);Check(executions==1,"settings or asset invalidation cancels queued shortcut");
        queue.Request(1,40);queue.Drain(44,true,dispatch);Check(executions==1,"stale request expires without execution");
        queue.Request(1,50);queue.Drain(51,true,dispatch);Check(executions==2&&last==1,"redo uses same deferred path");
        queue.Request(0,60);try{queue.Drain(61,true,_=>throw new InvalidOperationException());}catch(InvalidOperationException){}queue.Drain(62,true,dispatch);Check(executions==2,"failed dispatch is removed before callback");
        Console.WriteLine("Phase 4 safety: "+checks+" assertions passed.");
    }
}





