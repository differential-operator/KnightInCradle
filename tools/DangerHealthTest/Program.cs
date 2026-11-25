using System; using KnightInCradle;
void Check(int a,int b){if(a!=b)throw new Exception($"{a} != {b}");}
Check(DangerHealthMath.Maximum(60,0),60);Check(DangerHealthMath.Maximum(60,160),156);
Check(DangerHealthMath.Maximum(60,1),61);Check(DangerHealthMath.Maximum(60,176),166);
Check(DangerHealthMath.Maximum(120,160),312);Check(DangerHealthMath.Maximum(int.MaxValue,int.MaxValue),int.MaxValue);
Check(DangerHealthMath.Maximum(60,-10),60);Console.WriteLine("7 enemy health scaling checks passed");
