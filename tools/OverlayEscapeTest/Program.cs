using System;
using KnightInCradle.CharmUi;
class Program {
 static int n;
 static void Check(bool value){if(!value)throw new Exception("Check "+n);n++;}
 static void Main(){
  UnityEngine.Time.frameCount=1; UnityEngine.Input.Down=true; UnityEngine.Input.Held=true;
  Check(!OverlayEscape.Handle());
  KeyRebindLayer.Open=true;Check(OverlayEscape.Handle()&&!KeyRebindLayer.Open);
  Check(OverlayEscape.Consumed&&OverlayEscape.Handle());
  UnityEngine.Time.frameCount+=10;Check(OverlayEscape.Handle());
  UnityEngine.Input.Down=false;UnityEngine.Input.Held=false;
  UnityEngine.Time.frameCount++;Check(OverlayEscape.Handle());
  UnityEngine.Time.frameCount+=3;Check(!OverlayEscape.Handle());
  var charm=CharmUiController.Instance=new CharmUiController{IsOpen=true};
  KeyRebindLayer.Open=true;UnityEngine.Input.Down=true;UnityEngine.Input.Held=true;
  Check(OverlayEscape.Handle()&&!KeyRebindLayer.Open&&charm.IsOpen);
  Check(OverlayEscape.Handle()&&charm.IsOpen);
  UnityEngine.Input.Down=false;UnityEngine.Input.Held=false;UnityEngine.Time.frameCount+=3;
  Check(!OverlayEscape.Handle()&&charm.IsOpen);
  UnityEngine.Time.frameCount++;UnityEngine.Input.Down=true;UnityEngine.Input.Held=true;
  Check(OverlayEscape.Handle()&&!charm.IsOpen);
  UnityEngine.Input.Down=false;UnityEngine.Input.Held=false;UnityEngine.Time.frameCount+=3;
  Check(!OverlayEscape.Consumed);
  Console.WriteLine("OverlayEscapeTest: "+n+" checks passed");
 }
}
namespace UnityEngine {enum KeyCode{Escape} static class Time{public static int frameCount;} static class Input{public static bool Down,Held;public static bool GetKey(KeyCode key)=>Held;public static bool GetKeyDown(KeyCode key)=>Down;}}
namespace KnightInCradle.CharmUi {
 static class KeyRebindLayer{public static bool Open;public static bool CloseForEscape(){if(!Open)return false;Open=false;return true;}}
 class CharmUiController{public static CharmUiController Instance;public bool IsOpen;public void CloseAndHide(){IsOpen=false;}}
}
