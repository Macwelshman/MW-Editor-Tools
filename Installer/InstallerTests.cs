using System;
using System.IO;
using System.Linq;
namespace MWEditorToolsInstaller {
    internal static class InstallerTests {
        private static int checks;
        private static void Check(bool ok,string label){if(!ok)throw new Exception(label);checks++;}
        private static void Refuses(Action action,string label){try{action();}catch(InvalidOperationException){checks++;return;}throw new Exception(label);}
        private static string Target(string root,string name){return Path.Combine(root,name,"Mods","MWEditorTools");}
        private static string Tree(string path){return Directory.Exists(path)?String.Join("|",Directory.GetFiles(path,"*",SearchOption.AllDirectories).OrderBy(p=>p).Select(p=>p.Substring(path.Length)+":"+InstallerCore.Hash(File.ReadAllBytes(p)))):"missing";}
        public static int Main(string[] args){try{
            if(args.Length!=2||!Path.IsPathRooted(args[0]))throw new Exception("Supply a new absolute test directory and linked-target fixture.");
            var root=Path.GetFullPath(args[0]);if(Directory.Exists(root))throw new Exception("Test directory must not exist.");Directory.CreateDirectory(root);
            var payload=InstallerCore.ReadPayload();Check(payload.Length==17,"actual embedded payload count");
            var linkedBefore=Tree(args[1]);Refuses(()=>InstallerCore.Install(args[1],payload,()=>false),"junction refusal");Check(Tree(args[1])==linkedBefore,"junction target changed");
            var clean=Target(root,"fresh");var result=InstallerCore.Install(clean,payload,()=>false);Check(result.Backup==null&&result.Files==17,"fresh install");foreach(var file in payload)Check(InstallerCore.Hash(File.ReadAllBytes(Path.Combine(clean,file.Name)))==file.Hash,"fresh checksum "+file.Name);
            Directory.CreateDirectory(Path.Combine(clean,"unrelated"));File.WriteAllText(Path.Combine(clean,"unrelated","keep.txt"),"untouched");File.WriteAllText(Path.Combine(clean,"MW.EditorTools.dll"),"old version");var old=Tree(clean);
            result=InstallerCore.Install(clean,payload,()=>false);Check(result.Backup!=null&&Tree(result.Backup)==old,"complete verified backup");Check(File.ReadAllText(Path.Combine(clean,"unrelated","keep.txt"))=="untouched","preserve unrelated files");Check(!result.Backup.StartsWith(Path.GetDirectoryName(clean)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase),"backup outside Mods");
            var before=Tree(clean);Refuses(()=>InstallerCore.Install(clean,payload,()=>true),"game-running refusal");Check(Tree(clean)==before,"running game changed files");
            var corrupt=payload.Select(p=>new PayloadFile{Name=p.Name,Bytes=(byte[])p.Bytes.Clone(),Hash=p.Hash}).ToArray();corrupt[0].Bytes[0]^=1;Refuses(()=>InstallerCore.Install(clean,corrupt,()=>false),"corrupt payload refusal");Check(Tree(clean)==before,"corrupt payload wrote files");
            Refuses(()=>InstallerCore.Install(Path.Combine(root,"wrong"),payload,()=>false),"wrong destination refusal");
            var fakeGame=Path.Combine(root,"game");Directory.CreateDirectory(fakeGame);File.WriteAllText(Path.Combine(fakeGame,"Cities2.exe"),"fixture");Refuses(()=>InstallerCore.Install(Path.Combine(fakeGame,"Mods","MWEditorTools"),payload,()=>false),"game-installation refusal");
            File.WriteAllText(Path.Combine(clean,payload[0].Name),"prior contents");before=Tree(clean);var writes=0;Refuses(()=>InstallerCore.Install(clean,payload,()=>false,name=>{if(++writes==3)throw new IOException("Injected write failure");}),"partial write rollback");Check(Tree(clean)==before,"existing installation not restored");
            var freshFail=Target(root,"fresh-failure");writes=0;Refuses(()=>InstallerCore.Install(freshFail,payload,()=>false,name=>{if(++writes==3)throw new IOException("Injected write failure");}),"fresh partial failure");Check(Directory.GetFiles(freshFail).Length==0,"fresh failure left payload files");
            var late=Target(root,"late-start");var calls=0;Refuses(()=>InstallerCore.Install(late,payload,()=>++calls>=4),"late process-start refusal");Check(!Directory.Exists(late)||Directory.GetFiles(late).Length==0,"late pre-write game start wrote files");
            var during=Target(root,"mid-start");Directory.CreateDirectory(during);File.WriteAllText(Path.Combine(during,payload[0].Name),"old");calls=0;Refuses(()=>InstallerCore.Install(during,payload,()=>++calls>=5),"mid-install game-start refusal");Check(InstallerCore.Hash(File.ReadAllBytes(Path.Combine(during,payload[0].Name)))==payload[0].Hash&&Directory.GetFiles(during).Length==1,"game-start path continued writes or rolled back while running");
            var verify=Target(root,"final-verify");Directory.CreateDirectory(verify);File.WriteAllText(Path.Combine(verify,payload[0].Name),"old");var verifyBefore=Tree(verify);writes=0;Refuses(()=>InstallerCore.Install(verify,payload,()=>false,name=>{if(++writes==payload.Length)File.WriteAllText(Path.Combine(verify,payload[0].Name),"corrupt after copy");}),"final verification catches late corruption");Check(Tree(verify)==verifyBefore,"final verification failure did not roll back");
            var readOnly=Target(root,"read-only");Directory.CreateDirectory(readOnly);var readOnlyFile=Path.Combine(readOnly,payload[1].Name);File.WriteAllText(readOnlyFile,"prior read-only");File.SetAttributes(readOnlyFile,FileAttributes.ReadOnly);var readBefore=Tree(readOnly);Refuses(()=>InstallerCore.Install(readOnly,payload,()=>false),"read-only write failure");Check(Tree(readOnly)==readBefore,"read-only failure changed prior files");
            var concurrent=Target(root,"concurrent");Directory.CreateDirectory(Path.GetDirectoryName(Path.GetDirectoryName(concurrent)));var lockFile=Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(concurrent)),".MWEditorTools-install.lock");using(var held=new FileStream(lockFile,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None)){bool blocked=false;try{InstallerCore.Install(concurrent,payload,()=>false);}catch(IOException){blocked=true;}Check(blocked&&!Directory.Exists(concurrent),"concurrent install lock");}
            Console.WriteLine("PASS: "+checks+" installer checks against actual embedded release payload; production mod directory untouched.");return 0;
        }catch(Exception error){Console.Error.WriteLine(error);return 1;}}
    }
}
