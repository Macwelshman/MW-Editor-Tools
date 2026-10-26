using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

static class Probe {
    public static bool RegisterFails, UnregisterFails, RemoveFails, MultiSaveFails,ColoursFails,ConfirmationFails,IconFails,TargetingFails,OutlineFails;
    public static int Adds, Removes, Registered, Unregistered, UiReleases, RecoveryReleases, ConfirmationReleases, Destroyed, Errors, MultiSaveReleases,ColoursReleases,IconReleases,TargetingReleases,OutlineReleases;
    public static void Reset() { RegisterFails=UnregisterFails=RemoveFails=MultiSaveFails=ColoursFails=ConfirmationFails=IconFails=TargetingFails=OutlineFails=false; Adds=Removes=Registered=Unregistered=UiReleases=RecoveryReleases=ConfirmationReleases=Destroyed=Errors=MultiSaveReleases=ColoursReleases=IconReleases=TargetingReleases=OutlineReleases=0; }
}
class Program {
    static int checks;
    static void Check(bool result,string message) { if(!result) throw new Exception(message); checks++; }
    static MW.EditorTools.Mod Uninitialized() => (MW.EditorTools.Mod)RuntimeHelpers.GetUninitializedObject(typeof(MW.EditorTools.Mod));
    static void Main() {
        Probe.Reset(); var empty=Uninitialized(); empty.OnDispose(); empty.OnDispose(); Check(Probe.Errors==0,"dispose before load tolerates skipped constructor");
        Probe.Reset(); Probe.ConfirmationFails=true; var conflict=Uninitialized(); conflict.OnLoad(new Game.UpdateSystem()); Check(Probe.Errors==1 && Probe.Registered==1 && Probe.Destroyed==0 && Probe.ConfirmationReleases==1,"confirmation conflict isolated; partial hooks released and other tools loaded"); conflict.OnDispose(); Check(Probe.UiReleases==1 && Probe.RecoveryReleases==1 && Probe.MultiSaveReleases==1 && Probe.ColoursReleases==1,"all independent modules survived confirmation conflict"); Probe.ConfirmationFails=false; Probe.Reset(); Probe.IconFails=true; var iconConflict=Uninitialized(); iconConflict.OnLoad(new Game.UpdateSystem()); Check(Probe.Errors==1&&Probe.Registered==1&&Probe.Destroyed==0&&Probe.IconReleases==1,"thumbnail adapter startup failure releases its hooks without disabling other modules"); iconConflict.OnDispose(); Check(Probe.RecoveryReleases==1&&Probe.UiReleases==1&&Probe.ColoursReleases==1,"Undo and other modules survive thumbnail adapter failure"); Probe.Reset(); Probe.TargetingFails=true; var targetingFailure=Uninitialized(); targetingFailure.OnLoad(new Game.UpdateSystem()); Check(Probe.Errors==1&&Probe.Registered==1&&Probe.Destroyed==0&&Probe.TargetingReleases==1,"targeting startup failure releases its resources without disabling Undo or other modules");targetingFailure.OnDispose();Check(Probe.RecoveryReleases==1&&Probe.UiReleases==1,"Undo and UI dispose after targeting failure");Probe.Reset();Probe.OutlineFails=true;var outlineFailure=Uninitialized();outlineFailure.OnLoad(new Game.UpdateSystem());Check(Probe.Errors==1&&Probe.OutlineReleases==1&&Probe.Registered==1,"outline startup failure is isolated and partial renderer released");outlineFailure.OnDispose();Check(Probe.UiReleases==1&&Probe.RecoveryReleases==1&&Probe.TargetingReleases==1,"Undo, targeting and UI survive optional renderer failure");Probe.Reset(); var mod=Uninitialized(); mod.OnLoad(new Game.UpdateSystem());
        Check(Probe.Adds==2 && Probe.Registered==1 && Probe.Errors==0,"loader bypass still registers all locales and Options");
        Check(MW.EditorTools.Modules.ColourVariations.PaletteWindow.Instance!=null,"Colours controller registered on startup");
        mod.OnDispose(); Check(MW.EditorTools.Modules.ColourVariations.PaletteWindow.Instance==null&&Probe.ColoursReleases==1,"Colours static state and UI released"); Check(Probe.Removes==2 && Probe.Unregistered==1,"registered resources released");
        Check(Probe.UiReleases==1 && Probe.RecoveryReleases==1 && Probe.ConfirmationReleases==1 && Probe.Destroyed==1,"all editor resources released");
        mod.OnDispose(); Check(Probe.Unregistered==1 && Probe.Destroyed==1,"repeat dispose is idempotent");Check(Probe.ColoursReleases==1,"Colours releases once on repeat disposal");
        Probe.Reset(); Probe.RegisterFails=true; var partial=Uninitialized(); partial.OnLoad(new Game.UpdateSystem());
        Check(Probe.Errors==1 && Probe.Removes==2 && Probe.Unregistered==1,"partial registration rolled back without throwing");
        Check(Probe.UiReleases==0 && Probe.Destroyed==0,"Options failure leaves working editor services active");
        partial.OnDispose(); Check(Probe.UiReleases==1 && Probe.Destroyed==1,"partial failure subsequently disposes editor services");
        Probe.Reset(); var fault=Uninitialized(); fault.OnLoad(new Game.UpdateSystem()); Probe.UnregisterFails=Probe.RemoveFails=true;
        fault.OnDispose(); Check(Probe.Removes==2 && Probe.Unregistered==1,"failed unregister does not skip locale cleanup");
        Check(Probe.Errors==3 && Probe.UiReleases==1 && Probe.Destroyed==1,"cleanup errors cannot interrupt other mods via throw");
        fault.OnDispose(); Check(Probe.Errors==3,"failed cleanup is not retried on repeat disposal");
        Check(Probe.MultiSaveReleases==1,"Multi-Save releases once even when other cleanup fails");Probe.Reset();Probe.MultiSaveFails=true;var isolated=Uninitialized();isolated.OnLoad(new Game.UpdateSystem());Check(Probe.Errors==1&&Probe.Registered==1,"Multi-Save startup failure preserves Options and existing tools");isolated.OnDispose();Check(Probe.UiReleases==1&&Probe.RecoveryReleases==1,"Existing tools still dispose after Multi-Save failure");Probe.Reset();Probe.ColoursFails=true;var colourFailure=Uninitialized();colourFailure.OnLoad(new Game.UpdateSystem());Check(Probe.Errors==1&&Probe.Registered==1,"Colours startup failure preserves Options and existing modules");Check(MW.EditorTools.Modules.ColourVariations.PaletteWindow.Instance==null,"Failed Colours startup clears static controller");colourFailure.OnDispose();Check(Probe.UiReleases==1&&Probe.MultiSaveReleases==1&&Probe.RecoveryReleases==1,"Other modules dispose after Colours startup failure");Console.WriteLine("PASS: "+checks+" lifecycle assertions against actual Mod source with constructor bypass and fault injection. Native Options runtime test pending.");
    }
}
namespace Colossal.Logging {
    public interface ILog { ILog SetShowsErrorsInUI(bool value); void Error(Exception error,string message); void Info(string message); }
    class Logger:ILog { public ILog SetShowsErrorsInUI(bool value)=>this; public void Error(Exception error,string message){Probe.Errors++;} public void Info(string message){} }
    public static class LogManager { public static ILog GetLogger(string name)=>new Logger(); }
}
namespace UnityEngine {
    public class Object { public static void DontDestroyOnLoad(Object value){} public static void Destroy(Object value){Probe.Destroyed++;} }
    public class GameObject:Object {
        readonly Dictionary<Type,object> components=new Dictionary<Type,object>();
        public GameObject(string name){}
        public T AddComponent<T>() where T:new(){var v=new T();components[typeof(T)]=v;return v;}
        public T GetComponent<T>() where T:class=>components.TryGetValue(typeof(T),out var value)?(T)value:null;
    }
}
namespace Game {
    public enum SystemUpdatePhase {UIUpdate,ToolUpdate,Rendering,PreCulling}
    public class TestWorld {public T GetOrCreateSystemManaged<T>() where T:new(){if(typeof(T)==typeof(MW.EditorTools.Modules.ColourVariations.MWUISystem)&&Probe.ColoursFails)throw new Exception("colours init");if(typeof(T)==typeof(MW.EditorTools.Modules.MultiSave.MultiSaveSystem)&&Probe.MultiSaveFails)throw new Exception("module init");return new T();}}
    public class UpdateSystem {public TestWorld World=new TestWorld();public void UpdateAt<T>(SystemUpdatePhase phase){} public void UpdateAfter<T,U>(SystemUpdatePhase phase){}public void UpdateBefore<T,U>(SystemUpdatePhase phase){} }
}
namespace Game.Modding {public interface IMod {void OnLoad(Game.UpdateSystem system);void OnDispose();}}
namespace Game.SceneFlow {
    public class Localization {public string[] GetSupportedLocales()=>new[]{"en-US","fr-FR"};public void AddSource(string locale,object source){Probe.Adds++;}public void RemoveSource(string locale,object source){Probe.Removes++;if(Probe.RemoveFails)throw new Exception("remove");}}
    public class GameManager {public static GameManager instance=new GameManager();public Localization localizationManager=new Localization();}
}
namespace MW.EditorTools.Settings {
    public class EditorToolsSettings {public static EditorToolsSettings Load()=>new EditorToolsSettings();}
    public class RecordingAction {public bool shouldBeEnabled {get;set;}}
    public class EditorToolsOptions {public EditorToolsOptions(Game.Modding.IMod mod,EditorToolsSettings settings,Modules.DeletionProtection.ListRecovery history,Action reset){}public void RegisterKeyBindings(){}public RecordingAction[] GetActions()=>new[]{new RecordingAction()};public void RegisterInOptionsUI(){Probe.Registered++;if(Probe.RegisterFails)throw new Exception("register");}public void UnregisterInOptionsUI(){Probe.Unregistered++;if(Probe.UnregisterFails)throw new Exception("unregister");}}
    public class OptionsLocale {public OptionsLocale(EditorToolsOptions options){} }
}
namespace MW.EditorTools.Services {public class EditorContextService {public bool DialogBusy=>false;public EditorContextService(Game.TestWorld world){}}}
namespace MW.EditorTools.Modules.DeletionProtection {
    public class ComponentConfirmation {public void Initialize(Settings.EditorToolsSettings settings){if(Probe.ConfirmationFails)throw new NotSupportedException("patch conflict");}public void Release(){Probe.ConfirmationReleases++;}}
    public class ListRecovery {public bool OperationBusy=>false;public bool CanRecordReplacementUndo=>true;public void RecordReplacementUndo(object action){}public void ClearHistory(){}public void SetSaveBusy(Func<bool> value){}public void Initialize(Game.TestWorld world,Settings.EditorToolsSettings settings,Services.EditorContextService context){}public void Release(){Probe.RecoveryReleases++;}}
}
namespace MW.EditorTools.UI {public class EditorToolsUISystem {public bool ClipboardShortcutAllowed=>true;public bool ComponentCommandAllowed=>true;public void Initialize(Settings.EditorToolsSettings settings,Services.EditorContextService context,Modules.DeletionProtection.ListRecovery history,Modules.DeletionProtection.ComponentConfirmation protection){}public void ResetWindowPosition(){}public void Release(){Probe.UiReleases++;}}}

namespace MW.EditorTools.Modules.MultiSave {public class MultiSaveSystem {public bool Enabled;public bool IsBusy=>false;public Func<string> BlockReason;public void Release(){Probe.MultiSaveReleases++;}}}namespace MW.EditorTools.Modules.ColourVariations {
 public class MWUISystem {public bool Enabled;public void Release(){Probe.ColoursReleases++;}}
 public class PaletteWindow:UnityEngine.Object {public static PaletteWindow Instance;public bool enabled=true;public Func<string> BlockReason;public Action BeforeWrite;}
}
namespace MW.EditorTools.Modules.ComponentManager.Systems {public class ComponentToolSystem {public bool Enabled;public void Initialize(Action beforeWrite,Func<bool> allowWrite){}public void Release(){}}}
namespace MW.EditorTools.Modules.PropReplacer {public class PropReplacerSystem {public Func<bool> CanRecordUndo;public Action<object> RecordUndo;public bool Enabled;public void Initialize(MW.EditorTools.Services.EditorContextService context,Action beforeWrite,Func<bool> shortcutAllowed){}public void Release(){}}}


namespace MW.EditorTools.Modules.PropReplacer {public class SurfaceRenderSystem {public void Initialize(PropReplacerSystem replacer){} public void Release(){}}}
namespace Game.Rendering {public class AreaBatchSystem {}}
