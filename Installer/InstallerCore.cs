using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;

namespace MWEditorToolsInstaller {
    internal sealed class PayloadFile {
        internal string Name, Hash;
        internal byte[] Bytes;
    }
    internal sealed class InstallResult {
        internal string Destination, Backup, EnvironmentBackup;
        internal int Files;
    }
    internal static class InstallerCore {
        internal const string Version="0.4.259";
        internal static string DefaultDestination {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),@"AppData\LocalLow\Colossal Order\Cities Skylines II\Mods\MWEditorTools"); }
        }
        internal static bool GameRunning() {
            var processes=Process.GetProcessesByName("Cities2");
            try { return processes.Length>0; } finally { foreach(var process in processes)process.Dispose(); }
        }
        internal static string Hash(byte[] bytes) {
            using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","");
        }
        internal static PayloadFile[] ReadPayload() {
            var assembly=Assembly.GetExecutingAssembly();
            string manifest;
            using(var stream=assembly.GetManifestResourceStream("payload.manifest")) {
                if(stream==null)throw new InvalidOperationException("The installer payload manifest is missing.");
                using(var reader=new StreamReader(stream))manifest=reader.ReadToEnd();
            }
            var result=new List<PayloadFile>();
            foreach(var line in manifest.Split(new[]{'\r','\n'},StringSplitOptions.RemoveEmptyEntries)) {
                var parts=line.Split('|');
                if(parts.Length!=2)throw new InvalidOperationException("Invalid installer manifest.");
                using(var stream=assembly.GetManifestResourceStream("payload."+parts[0])) {
                    if(stream==null)throw new InvalidOperationException("Missing payload: "+parts[0]);
                    using(var buffer=new MemoryStream()){stream.CopyTo(buffer);result.Add(new PayloadFile{Name=parts[0],Hash=parts[1],Bytes=buffer.ToArray()});}
                }
            }
            ValidatePayload(result.ToArray());return result.ToArray();
        }
        private static void ValidatePayload(PayloadFile[] files) {
            var expected=new[]{"MW.EditorTools.dll","MW.EditorTools.mjs","MW.EditorTools.css","0Harmony.dll","USER_MANUAL.md","USER_MANUAL.pdf","THIRD_PARTY_NOTICES.md","RELEASE_NOTES.md","INSTALL.txt","manual-replacer.png","manual-components.png","manual-fields.png","manual-priorities.png","manual-localisation.png","manual-colours.png","manual-multisave.png","manual-options.png"};
            if(files==null||files.Length!=expected.Length||files.Select(f=>f.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=expected.Length||expected.Except(files.Select(f=>f.Name),StringComparer.Ordinal).Any())throw new InvalidOperationException("Unexpected installer payload.");
            foreach(var file in files)if(file.Bytes==null||!String.Equals(Hash(file.Bytes),file.Hash,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Payload checksum failed: "+file.Name);
        }
        // Refuse junctions/symlinks on every existing path component. Never follow
        // a selected folder into the game installation or another filesystem tree.
        private static void CheckPath(string path) {
            for(var current=Path.GetFullPath(path);!String.IsNullOrEmpty(current);current=Path.GetDirectoryName(current)) {
                if((Directory.Exists(current)||File.Exists(current))&&(File.GetAttributes(current)&FileAttributes.ReparsePoint)!=0)throw new InvalidOperationException("Linked folders/files are not supported: "+current);
                if(Directory.Exists(current)&&(File.Exists(Path.Combine(current,"Cities2.exe"))||Directory.Exists(Path.Combine(current,"Cities2_Data"))))throw new InvalidOperationException("Choose the user Mods folder, not the game installation.");
            }
        }
        private static string Destination(string path) {
            if(String.IsNullOrWhiteSpace(path)||!Path.IsPathRooted(path))throw new InvalidOperationException("Select an absolute Mods folder path.");
            var full=Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);
            var parent=Path.GetDirectoryName(full);
            if(!String.Equals(Path.GetFileName(full),"MWEditorTools",StringComparison.OrdinalIgnoreCase)||!String.Equals(Path.GetFileName(parent),"Mods",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException(@"The destination must end in Mods\MWEditorTools.");
            CheckPath(full);return full;
        }
        private static void CopyTree(string source,string target) {
            CheckPath(source);CheckPath(target);Directory.CreateDirectory(target);
            foreach(var file in Directory.GetFiles(source)) {
                CheckPath(file);var copy=Path.Combine(target,Path.GetFileName(file));CheckPath(copy);File.Copy(file,copy,false);
                if(Hash(File.ReadAllBytes(file))!=Hash(File.ReadAllBytes(copy)))throw new IOException("Backup checksum failed: "+file);
            }
            foreach(var folder in Directory.GetDirectories(source))CopyTree(folder,Path.Combine(target,Path.GetFileName(folder)));
        }
        private static string TreeHash(string path) {
            CheckPath(path);
            var entries=new List<string>();
            foreach(var file in Directory.GetFiles(path)){CheckPath(file);entries.Add(Path.GetFileName(file)+":"+Hash(File.ReadAllBytes(file)));}
            foreach(var folder in Directory.GetDirectories(path)){CheckPath(folder);entries.Add(Path.GetFileName(folder)+"/"+TreeHash(folder));}
            return Hash(System.Text.Encoding.UTF8.GetBytes(String.Join("|",entries.OrderBy(s=>s,StringComparer.Ordinal))));
        }
        internal static InstallResult Install(string path,PayloadFile[] files,Func<bool> running=null,Action<string> beforeWrite=null,Action afterEnvironmentMove=null) {
            ValidatePayload(files);var target=Destination(path);running=running??GameRunning;
            if(running())throw new InvalidOperationException("Close Cities: Skylines II before installing.");
            var mods=Path.GetDirectoryName(target);var home=Path.GetDirectoryName(mods);
            CheckPath(home);Directory.CreateDirectory(home);
            var lockPath=Path.Combine(home,".MWEditorTools-install.lock");CheckPath(lockPath);
            using(var installLock=new FileStream(lockPath,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None)) {
                if(running())throw new InvalidOperationException("Close Cities: Skylines II before installing.");
                string backup=null;
                var backupRoot=Path.Combine(home,"MWEditorTools-backups",DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")+"-"+Guid.NewGuid().ToString("N").Substring(0,8));
                // Only the known sibling standalone mod is migrated. All endpoints
                // are canonical paths outside the game installation; backups are
                // outside Mods and preserve every standalone file/subdirectory.
                var legacy=Path.Combine(mods,"MWEditorEnvironment");CheckPath(legacy);
                var migrate=File.Exists(Path.Combine(legacy,"MWEditorEnvironment.dll"));
                string legacyCopy=null,legacyMoved=null;
                if(Directory.Exists(target)) {
                    backup=Path.Combine(backupRoot,"MWEditorTools");
                    CopyTree(target,backup);
                }
                if(migrate){legacyCopy=Path.Combine(backupRoot,"MWEditorEnvironment");CopyTree(legacy,legacyCopy);}
                var written=new List<string>();
                try {
                    if(running())throw new InvalidOperationException("Cities2 started. Close it before installing.");
                    CheckPath(target);Directory.CreateDirectory(target);
                    foreach(var file in files) {
                        if(running())throw new InvalidOperationException("Cities2 started during installation.");
                        var destination=Path.Combine(target,file.Name);CheckPath(destination);
                        if(beforeWrite!=null)beforeWrite(file.Name);
                        written.Add(file.Name);File.WriteAllBytes(destination,file.Bytes);
                        if(Hash(File.ReadAllBytes(destination))!=file.Hash.ToUpperInvariant())throw new IOException("Installed checksum failed: "+file.Name);
                    }
                    foreach(var file in files)if(Hash(File.ReadAllBytes(Path.Combine(target,file.Name)))!=file.Hash.ToUpperInvariant())throw new IOException("Final installed checksum failed: "+file.Name);
                    if(migrate){
                        if(running())throw new InvalidOperationException("Cities2 started before standalone Environment migration.");
                        CheckPath(legacy);var disabled=Path.Combine(backupRoot,"disabled");CheckPath(disabled);Directory.CreateDirectory(disabled);
                        var destination=Path.Combine(disabled,"MWEditorEnvironment");CheckPath(destination);
                        if(TreeHash(legacy)!=TreeHash(legacyCopy))throw new IOException("Standalone Environment changed after backup.");
                        Directory.Move(legacy,destination);legacyMoved=destination;
                        if(afterEnvironmentMove!=null)afterEnvironmentMove();
                        if(running())throw new InvalidOperationException("Cities2 started during standalone Environment migration.");
                        if(TreeHash(legacyMoved)!=TreeHash(legacyCopy))throw new IOException("Archived Environment checksum failed.");
                    }
                    return new InstallResult{Destination=target,Backup=backup,EnvironmentBackup=legacyMoved,Files=files.Length};
                } catch(Exception error) {
                    if(written.Count==0)throw;
                    // Do not write rollback files while the game is running either.
                    if(running())throw new InvalidOperationException("Installation stopped because Cities2 started. Close the game before restoring the backup: "+(backup??"No previous installation; remove the newly created mod files with the game closed.")+" Standalone Environment archive: "+(legacyMoved??legacyCopy??"none"),error);
                    var failures=new List<string>();
                    if(legacyMoved!=null)try{
                        CheckPath(legacyMoved);CheckPath(legacy);
                        if(Directory.Exists(legacy))throw new IOException("Standalone destination changed; original remains archived at "+legacyMoved);
                        Directory.Move(legacyMoved,legacy);
                        if(TreeHash(legacy)!=TreeHash(legacyCopy))throw new IOException("Standalone rollback checksum mismatch.");
                    }catch(Exception rollback){failures.Add("MWEditorEnvironment: "+rollback.Message);}
                    foreach(var name in written.AsEnumerable().Reverse())try {
                        var destination=Path.Combine(target,name);CheckPath(destination);
                        var prior=backup==null?null:Path.Combine(backup,name);
                        if(prior!=null&&File.Exists(prior)){if(!File.Exists(destination)||Hash(File.ReadAllBytes(prior))!=Hash(File.ReadAllBytes(destination)))File.Copy(prior,destination,true);if(Hash(File.ReadAllBytes(prior))!=Hash(File.ReadAllBytes(destination)))throw new IOException("Rollback checksum mismatch.");}
                        else if(File.Exists(destination))File.Delete(destination);
                    }catch(Exception rollback){failures.Add(name+": "+rollback.Message);}
                    throw new InvalidOperationException("Installation failed. "+(failures.Count==0?"Changed files were rolled back.":"Rollback needs attention: "+String.Join("; ",failures))+" Backup: "+(backup??"none (new install)")+". "+error.Message,error);
                }
            }
        }
    }
}
