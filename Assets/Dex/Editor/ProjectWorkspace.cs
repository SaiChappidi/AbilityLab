using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dex.Editor
{
    [Serializable] public sealed class SourceStamp {public string path,hash;}
    [Serializable] public sealed class SourceEdit {public string path,find,replace;public bool create;}
    [Serializable] public sealed class PatchProposal {public string analysis;public SourceEdit[] edits;}
    [Serializable] public sealed class InspectRequest {public string tool,path,query;public int start,count;}
    public static class ProjectWorkspace
    {
        public static string Hash(string text){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-","").ToLowerInvariant();}
        public static bool Editable(string path)
        {
            if(string.IsNullOrWhiteSpace(path)||path.Contains("\\")||Path.IsPathRooted(path)||path.Contains(":")||path.Split('/').Any(p=>p==".."||p=="."||p.Length==0))return false;
            return path.StartsWith("Assets/",StringComparison.Ordinal)&&path.EndsWith(".cs",StringComparison.Ordinal)&&!path.Split('/').Any(p=>p.Equals("Editor",StringComparison.OrdinalIgnoreCase))&&new[]{"Diagnostics","Tests","Verification","ScenarioDriver"}.All(p=>path.IndexOf(p,StringComparison.OrdinalIgnoreCase)<0);
        }
        static string[] Sources(){return Directory.GetFiles("Assets","*.cs",SearchOption.AllDirectories).Select(p=>p.Replace('\\','/')).Where(Editable).OrderBy(p=>p,StringComparer.Ordinal).ToArray();}
        public static string ProtectedHash()
        {
            var files=Directory.GetFiles("Assets","*.cs",SearchOption.AllDirectories).Select(p=>p.Replace('\\','/')).Where(p=>!Editable(p)).OrderBy(p=>p,StringComparer.Ordinal);
            return Hash(string.Join("\n",files.Select(p=>p+"\n"+File.ReadAllText(p))));
        }
        public static SourceStamp[] Capture(string directory)
        {
            foreach(string path in Sources())foreach(string root in new[]{"baseline","workspace"}){string destination=Path.Combine(directory,root,path);Directory.CreateDirectory(Path.GetDirectoryName(destination));File.Copy(path,destination,true);}
            string workspace=Path.Combine(directory,"workspace");File.WriteAllText(Path.Combine(workspace,"PROJECT_CONTEXT.txt"),Context());File.WriteAllText(Path.Combine(workspace,"TEST_API.md"),ProjectProtocol.Tools);
            return Sources().Select(p=>new SourceStamp{path=p,hash=Hash(File.ReadAllText(p))}).ToArray();
        }
        public static string Inspect(string directory,InspectRequest[] requests)
        {
            if(requests==null||requests.Length==0||requests.Length>8)throw new ArgumentException("Inspection needs 1..8 requests.");
            string root=Path.GetFullPath(Path.Combine(directory,"workspace"));var output=new StringBuilder();
            foreach(var request in requests)
            {
                if(request==null||request.path==null||request.query==null||request.path.Contains("\\")||request.path.Contains(":")||request.path.Split('/').Any(p=>p==".."))throw new ArgumentException("Invalid inspection path.");
                string path=string.IsNullOrEmpty(request.path)?root:Path.GetFullPath(Path.Combine(root,request.path));
                if(path!=root&&!path.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Inspection escaped the working copy.");
                int count=request.count>0?Math.Min(400,request.count):120;int start=Math.Max(1,request.start);output.AppendLine("TOOL "+request.tool+" "+request.path+" query="+request.query);
                if(request.tool=="read")
                {
                    if(!File.Exists(path))throw new ArgumentException("Workspace file not found: "+request.path);
                    var lines=File.ReadAllLines(path);for(int i=start-1;i<Math.Min(lines.Length,start-1+count);i++)output.AppendLine((i+1)+": "+lines[i]);
                }
                else if(request.tool=="search")
                {
                    if(request.query.Length==0||request.query.Length>200)throw new ArgumentException("Search needs a literal query of 1..200 characters.");
                    var files=Directory.Exists(path)?Directory.GetFiles(path,"*",SearchOption.AllDirectories):new[]{path};int found=0;
                    foreach(var file in files.Where(f=>f.EndsWith(".cs")||f.EndsWith(".txt")||f.EndsWith(".md")))
                    {var lines=File.ReadAllLines(file);for(int i=0;i<lines.Length&&found<count;i++)if(lines[i].IndexOf(request.query,StringComparison.OrdinalIgnoreCase)>=0){output.AppendLine(file.Substring(root.Length+1).Replace('\\','/')+":"+(i+1)+": "+lines[i]);found++;}if(found>=count)break;}
                    output.AppendLine("Matches returned: "+found);
                }
                else throw new ArgumentException("Inspection tool must be read or search.");
                if(output.Length>80000)throw new ArgumentException("Inspection output budget exceeded; request smaller ranges.");
            }
            return output.ToString();
        }
        public static string SnapshotText(string directory)
        {
            string root=Path.Combine(directory,"workspace");var result=new StringBuilder();
            foreach(var file in Directory.GetFiles(Path.Combine(root,"Assets"),"*.cs",SearchOption.AllDirectories).OrderBy(p=>p))
            {result.AppendLine("FILE "+file.Substring(root.Length+1).Replace('\\','/'));result.AppendLine(File.ReadAllText(file));}
            if(result.Length>350000)throw new InvalidOperationException("Gameplay source exceeds the current repair context budget.");return result.ToString();
        }
        public static string Context()
        {
            var b=new StringBuilder();b.AppendLine("Unity "+Application.unityVersion+" / "+EditorUserBuildSettings.activeBuildTarget+" / scene "+SceneManager.GetActiveScene().path);
            b.AppendLine("The runtime boot creates DexArena and its units/map; these objects need Play to inspect. No items, AI, waves or practice regeneration are enabled in deterministic scenarios. All operations exercise the real arena, not the old Combat.ResolveCast imitation.");
            foreach(var root in SceneManager.GetActiveScene().GetRootGameObjects().Take(30))b.AppendLine("Scene root: "+root.name+"; components: "+string.Join(",",root.GetComponents<Component>().Where(c=>c!=null).Select(c=>c.GetType().Name)));
            b.AppendLine("Gameplay C# files:\n"+string.Join("\n",Sources()));
            b.AppendLine("Existing protected regressions: Verification.Evaluate() (224 assertions), temporary-state lifetime invariants (32 checks), and DexArena.RunSmokeChecks() (38 actual arena checks). The host runs these after repairs. Their implementation is not editable or copied into this workspace. Existing saved declarative regressions are also run.");
            b.AppendLine("Recent Editor Console (may include unrelated packages):\n"+ConsoleContext());
            if(File.Exists("Packages/manifest.json"))b.AppendLine("Packages:\n"+File.ReadAllText("Packages/manifest.json"));
            return b.ToString();
        }
        static string ConsoleContext()
        {
            try
            {
                var assembly=typeof(EditorWindow).Assembly;var entries=assembly.GetType("UnityEditor.LogEntries");var entryType=assembly.GetType("UnityEditor.LogEntry");var flags=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
                int count=(int)entries.GetMethod("GetCount",flags).Invoke(null,null);var result=new StringBuilder();entries.GetMethod("StartGettingEntries",flags).Invoke(null,null);
                try{for(int i=Math.Max(0,count-12);i<count;i++){var entry=Activator.CreateInstance(entryType);entries.GetMethod("GetEntryInternal",flags).Invoke(null,new[]{(object)i,entry});var field=entryType.GetField("message",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);if(field!=null)result.AppendLine(Convert.ToString(field.GetValue(entry)));}}
                finally{entries.GetMethod("EndGettingEntries",flags).Invoke(null,null);}return result.Length==0?"No current entries.":result.ToString();
            }
            catch(Exception e){return "Console reflection unavailable: "+e.GetType().Name+". Compiler diagnostics are still captured directly.";}
        }
        public static void AssertLive(SourceStamp[] expected)
        {
            var paths=Sources();if(!paths.SequenceEqual(expected.Select(s=>s.path).OrderBy(p=>p,StringComparer.Ordinal)))throw new IOException("Gameplay file inventory changed outside this run. External edits preserved.");
            foreach(var stamp in expected)if(!File.Exists(stamp.path)||Hash(File.ReadAllText(stamp.path))!=stamp.hash)throw new IOException("Source changed outside this run: "+stamp.path+". External edits preserved.");
        }
        public static string Stage(string directory,PatchProposal patch)
        {
            if(patch==null||patch.edits==null||patch.edits.Length<1||patch.edits.Length>60)throw new ArgumentException("A patch needs 1..60 ordinary source edits.");
            string root=Path.GetFullPath(Path.Combine(directory,"workspace"));var proposed=new Dictionary<string,string>();
            foreach(var edit in patch.edits)
            {
                if(edit==null||!Editable(edit.path))throw new ArgumentException("Protected or invalid edit path: "+(edit==null?"null":edit.path));
                string path=Path.GetFullPath(Path.Combine(root,edit.path));if(!path.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Edit escaped workspace.");
                string original;if(!proposed.TryGetValue(edit.path,out original))original=File.Exists(path)?File.ReadAllText(path):null;
                if(edit.replace==null||edit.replace.Length>150000)throw new ArgumentException("Missing or oversized replacement.");
                if(edit.create){if(original!=null||!string.IsNullOrEmpty(edit.find))throw new ArgumentException("Create cannot overwrite an existing source file.");original=edit.replace;}
                else
                {
                    if(original==null||string.IsNullOrEmpty(edit.find))throw new ArgumentException("An existing file edit requires exact nonempty find text.");
                    int at=original.IndexOf(edit.find,StringComparison.Ordinal);if(at<0||original.IndexOf(edit.find,at+edit.find.Length,StringComparison.Ordinal)>=0)throw new ArgumentException("Find text must occur exactly once in "+edit.path);
                    original=original.Substring(0,at)+edit.replace+original.Substring(at+edit.find.Length);
                }
                // Gameplay source is not a host automation API.
                if(Regex.IsMatch(original,@"\b(UnityEditor|System\s*\.\s*(IO|Diagnostics|Reflection|Net)|DllImport|LibraryImport|Microsoft\s*\.\s*Win32|ProcessStartInfo|Environment\s*\.)\b"))throw new ArgumentException("Gameplay patch contains a restricted host/OS API: "+edit.path);
                proposed[edit.path]=original;
            }
            foreach(var pair in proposed){string destination=Path.Combine(root,pair.Key);Directory.CreateDirectory(Path.GetDirectoryName(destination));File.WriteAllText(destination,pair.Value,new UTF8Encoding(false));}
            return Diff(directory);
        }
        public static string Diff(string directory)
        {
            string root=Path.Combine(directory,"workspace");var b=new StringBuilder();
            foreach(var path in Directory.GetFiles(Path.Combine(root,"Assets"),"*.cs",SearchOption.AllDirectories).Select(p=>p.Substring(root.Length+1).Replace('\\','/')).OrderBy(p=>p))
            {
                string baseline=Path.Combine(directory,"baseline",path);string before=File.Exists(baseline)?File.ReadAllText(baseline):"",after=File.ReadAllText(Path.Combine(root,path));if(before==after)continue;
                b.AppendLine("--- a/"+path);b.AppendLine("+++ b/"+path);var a=before.Replace("\r","").Split('\n');var c=after.Replace("\r","").Split('\n');
                if((long)a.Length*c.Length>2000000){b.AppendLine("- "+before);b.AppendLine("+ "+after);continue;}
                var lcs=new int[a.Length+1,c.Length+1];for(int i=a.Length-1;i>=0;i--)for(int j=c.Length-1;j>=0;j--)lcs[i,j]=a[i]==c[j]?lcs[i+1,j+1]+1:Math.Max(lcs[i+1,j],lcs[i,j+1]);
                int x=0,y=0;while(x<a.Length||y<c.Length){if(x<a.Length&&y<c.Length&&a[x]==c[y]){if(x+1<a.Length&&y+1<c.Length&&a[x+1]!=c[y+1])b.AppendLine("  "+a[x]);x++;y++;}else if(y<c.Length&&(x==a.Length||lcs[x,y+1]>=lcs[x+1,y]))b.AppendLine("+ "+c[y++]);else b.AppendLine("- "+a[x++]);}
            }
            return b.ToString();
        }
        public static SourceStamp[] Apply(string directory,SourceStamp[] expected)
        {
            AssertLive(expected);string root=Path.Combine(directory,"workspace");
            var files=Directory.GetFiles(Path.Combine(root,"Assets"),"*.cs",SearchOption.AllDirectories);
            foreach(string file in files)
            {
                string path=file.Substring(root.Length+1).Replace('\\','/');if(!Editable(path))throw new InvalidOperationException("Workspace has a protected source path.");
                string source=File.ReadAllText(file);if(File.Exists(path)&&File.ReadAllText(path)==source)continue;Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,source,new UTF8Encoding(false));
            }
            return Sources().Select(p=>new SourceStamp{path=p,hash=Hash(File.ReadAllText(p))}).ToArray();
        }
        public static SourceStamp[] Rollback(string directory,SourceStamp[] expected,SourceStamp[] baseline)
        {
            AssertLive(expected);var originals=new HashSet<string>(baseline.Select(s=>s.path));
            foreach(var stamp in expected)
            {
                if(!originals.Contains(stamp.path)){if(!Editable(stamp.path))throw new InvalidOperationException("Invalid rollback target");AssetDatabase.DeleteAsset(stamp.path);}
                else{string original=File.ReadAllText(Path.Combine(directory,"baseline",stamp.path));if(File.ReadAllText(stamp.path)!=original)File.WriteAllText(stamp.path,original,new UTF8Encoding(false));}
            }
            return baseline;
        }
    }
}
