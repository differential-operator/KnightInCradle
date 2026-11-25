using System; using KnightInCradle;
int checks=0;
void Check(int actual,int wanted) {if(actual!=wanted)throw new Exception($"{actual} != {wanted}"); checks++;}
Check(DangerDebugMath.Adjust(0,16,false),16);Check(DangerDebugMath.Adjust(16,-16,false),0);
Check(DangerDebugMath.Adjust(0,-16,false),0);Check(DangerDebugMath.Adjust(150,16,false),160);
Check(DangerDebugMath.Adjust(160,16,true),176);Check(DangerDebugMath.Adjust(176,-16,true),160);
Check(DangerDebugMath.Adjust(65530,16,true),65535);Check(DangerDebugMath.Adjust(int.MaxValue,16,true),65535);
Console.WriteLine($"{checks} danger boundary checks passed");
