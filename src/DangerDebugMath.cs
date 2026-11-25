using System;
namespace KnightInCradle {
 internal static class DangerDebugMath {
  internal static int Adjust(int value,int delta,bool uncapped) => (int)Math.Max(0L, Math.Min(uncapped ? ushort.MaxValue : 160L,(long)value+delta));
 }
}
