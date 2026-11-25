using System;
namespace KnightInCradle {
 internal static class DangerHealthMath {
  internal static int Maximum(int basis,int danger) => (int)Math.Min(int.MaxValue, Math.Max(1L, ((long)basis*(100L+Math.Max(0,danger))+99L)/100L));
 }
}
