using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;

static class Program
{
    const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static int checks;
    static void Check(bool ok, string name) { if (!ok) throw new Exception(name); checks++; }
    static void Set(object target, string name, object value) => target.GetType().GetField(name, Fields).SetValue(target, value);
    static object Get(object target, string name) => target.GetType().GetField(name, Fields).GetValue(target);
    static object Blank(Type type) => FormatterServices.GetUninitializedObject(type);
    static object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name).Invoke(target, args);
    static int Main(string[] args)
    {
        string repo = Path.GetFullPath(args[0]);
        string game = Path.GetFullPath(args.Length > 1 ? args[1] : Path.Combine(repo, "../_compat_current030/AliceInCradle_ver030"));
        AppDomain.CurrentDomain.AssemblyResolve += (_, e) => {
            foreach (string dir in new[] {Path.Combine(game,"AliceInCradle_Data/Managed"), Path.Combine(game,"BepInEx/core"), Path.Combine(repo,"bin/Release/net472")}) {
                string file=Path.Combine(dir,new AssemblyName(e.Name).Name+".dll");
                if(File.Exists(file)) return Assembly.LoadFrom(file);
            }
            return null;
        };
        try { Run(repo,game); Console.WriteLine("MapRevealTest: "+checks+" checks passed (native 030i classes)"); return 0; }
        catch(Exception e) { Console.Error.WriteLine(e); return 1; }
    }
    static void Run(string repo,string game)
    {
        var native=Assembly.LoadFrom(Path.Combine(game,"AliceInCradle_Data/Managed/Assembly-CSharp.dll"));
        var maps=Assembly.LoadFrom(Path.Combine(game,"AliceInCradle_Data/Managed/unsafeAssem.dll"));
        var mod=Assembly.LoadFrom(Path.Combine(repo,"bin/Release/net472/KnightInCradle.dll"));
        var wmType=native.GetType("nel.WholeMapItem",true);
        var roomType=native.GetType("nel.WholeMapItem+WMItem",true);
        var mapType=maps.GetType("m2d.Map2d",true);
        var wm=Blank(wmType); var other=Blank(wmType);
        var rooms=(IList)Activator.CreateInstance(typeof(System.Collections.Generic.List<>).MakeGenericType(roomType));
        var visited=(IList)Activator.CreateInstance(typeof(System.Collections.Generic.List<>).MakeGenericType(mapType));
        Set(wm,"AWmi",rooms); Set(wm,"Avisitted",visited); Set(wm,"completion_calc_count",3f); Set(wm,"completion_ratio_",1f/3f); Set(wm,"prepared_all",-1);
        var playerMap=Blank(mapType); Set(playerMap,"key","current"); Set(wm,"CurMap",playerMap);
        for(int i=0;i<3;i++) {
            var map= i==0 ? playerMap : Blank(mapType); Set(map,"key","room"+i);
            var room=Blank(roomType); Set(room,"SrcMap",map); Set(room,"visitted",i==0); rooms.Add(room);
            if(i==0) visited.Add(map);
        }
        Set(other,"completion_ratio_",0.25f);
        var reveal=mod.GetType("KnightInCradle.MapRevealDebug",true).GetMethod("Reveal",BindingFlags.Static|BindingFlags.NonPublic);
        Check((int)reveal.Invoke(null,new[]{wm})==2,"Only unexplored rooms are added");
        Check((float)Call(wm,"getCompletionRatio")==1f,"Native completion becomes 100%");
        foreach(var room in rooms) {
            Check((bool)Get(room,"visitted"),"Native drawing flag lit");
            Check((bool)wmType.GetMethod("isVisitted",new[]{mapType}).Invoke(wm,new[]{Get(room,"SrcMap")}),"Native persistent visited lookup");
        }
        Check(ReferenceEquals(Get(wm,"CurMap"),playerMap),"Player map unchanged");
        Check((float)Call(other,"getCompletionRatio")==0.25f,"Other region unchanged");
        Check((int)Get(wm,"prepared_all")==0,"Native incremental preparation restarted");
        Check((int)reveal.Invoke(null,new[]{wm})==0 && visited.Count==3,"Repeat operation is idempotent");
        // Initialize only unrelated empty collections needed by the original serializer.
        foreach(string field in new[]{"OMarker","OIconHiddenDeperture","OANoticedIcon","ATreasure"}) {
            var f=wmType.GetField(field,Fields); f.SetValue(wm,Activator.CreateInstance(f.FieldType));
        }
        var bytes=Call(wm,"createBinary");
        Check(bytes!=null,"Original game save serializer accepts unlocked region");
        var position=bytes.GetType().GetProperty("position");
        position.SetValue(bytes,Convert.ChangeType(0,position.PropertyType),null);
        Check(Convert.ToInt32(Call(bytes,"readByte"))==9,"Native save format unchanged");
        Check(Convert.ToInt32(Call(bytes,"readUShort"))==3,"All visited rooms serialized");
        for(int i=0;i<3;i++) Check((string)Call(bytes,"readString","utf-8",false)=="room"+i,"Saved room key "+i);
    }
}
