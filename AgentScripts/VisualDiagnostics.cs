using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

public static class VisualDiagnostics
{
    public static string ImportPortraits()
    {
        foreach(string champion in new[]{"Ahri","Morgana","Ezreal","Mel"})
        {
            string path="Assets/Resources/Dex/Portraits/"+champion+".png";
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.spritePixelsPerUnit=120;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.filterMode=FilterMode.Bilinear;
            importer.SaveAndReimport();
        }
        return "Imported four official portraits as sprites.";
    }
    public static string AssertVisible()
    {
        var sprites=UnityEngine.Object.FindObjectsByType<SpriteRenderer>();
        int missing=sprites.Count(r=>r.sprite==null);int portraits=Dex.ChampionKit.Roster.Count(c=>Resources.Load<Sprite>("Dex/Portraits/"+c)!=null);
        bool ok=sprites.Length>20&&missing==0&&portraits==4;
        string result=(ok?"PASS":"FAIL")+" visual lifecycle: "+sprites.Length+" renderers; missing sprites="+missing+"; official portraits="+portraits+"/4\n"+Inspect();
        Directory.CreateDirectory("DexReports/evidence");File.AppendAllText("DexReports/evidence/visual-lifecycle.txt",DateTime.UtcNow.ToString("O")+"\n"+result+"\n");
        return result;
    }
    public static string Inspect()
    {
        var sprites=UnityEngine.Object.FindObjectsByType<SpriteRenderer>();
        var camera=Camera.main;
        string result="Sprites="+sprites.Length+" missing sprites="+sprites.Count(r=>r.sprite==null)+" enabled="+sprites.Count(r=>r.enabled)+" cameraMask="+(camera!=null?camera.cullingMask:0)+"\n";
        foreach(var r in sprites.Where(r=>r.name=="Ezreal"||r.name=="Morgana"||r.name=="Ahri"||r.name=="Minion").Take(12))
            result+=r.name+" sprite="+(r.sprite!=null?r.sprite.name:"MISSING")+" material="+(r.sharedMaterial!=null?r.sharedMaterial.shader.name:"MISSING")+" enabled="+r.enabled+" position="+r.transform.position+"\n";
        return result;
    }
}
