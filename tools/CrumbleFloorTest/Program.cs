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
        try { Run(repo,game); Console.WriteLine("CrumbleFloorTest: "+checks+" checks passed (native 030i classes)"); return 0; }
        catch(Exception e) { Console.Error.WriteLine(e); return 1; }
    }
    static void Run(string repo,string game)
    {
        var native=Assembly.LoadFrom(Path.Combine(game,"AliceInCradle_Data/Managed/Assembly-CSharp.dll"));
        var maps=Assembly.LoadFrom(Path.Combine(game,"AliceInCradle_Data/Managed/unsafeAssem.dll"));
        var wallType=native.GetType("nel.M2BreakableWallMover",true);
        var labelType=native.GetType("nel.M2LpBreakable",true);
        var typeField=labelType.GetField("type",Fields);
        var ride=wallType.GetMethod("initRideBy",Fields);
        Check(ride!=null,"Native contact entry exists");
        foreach(string kind in new[]{"FALL","FALLPUNCH","BOMB","UNBREAK"})
        {
            var wall=Blank(wallType); var label=Blank(labelType);
            typeField.SetValue(label,Enum.Parse(typeField.FieldType,kind));
            Set(wall,"Lp",label);
            bool fall=kind=="FALL"||kind=="FALLPUNCH";
            Check((bool)labelType.GetProperty("fallbreak").GetValue(label,null)==fall,"Native floor-type gating "+kind);
            // Uninitialized Unity movers have no native object handle and compare equal to null.
            // Test the real null-host guard rather than treating fake movers as live game objects.
            ride.Invoke(wall,new object[]{null});
            Check(Get(wall,"FallInit")==null,"Null host cannot arm floor "+kind);
        }
    }
}
