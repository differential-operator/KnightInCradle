using System;
using KnightInCradle;
int checks=0;
void Check(bool pass) { if(!pass) throw new Exception("Flutter check failed"); checks++; }
for(int id=0;id<10;id++) {
 float p=LeafFlutter.Period(id), delay=3f+((id*3)%11)*.2f;
 Check(p>=3f && p<=5f);
 Check(LeafFlutter.Amount(delay-.01f,id)==0);
 Check(LeafFlutter.Amount(delay+LeafFlutter.Duration*.5f,id)>.999f);
 Check(LeafFlutter.Amount(delay+LeafFlutter.Duration+.01f,id)==0);
 Check(Math.Abs(LeafFlutter.Amount(delay+.2f,id)-LeafFlutter.Amount(delay+p+.2f,id))<.0001f);
 for(int f=0;f<3600;f++) {float value=LeafFlutter.Amount(f/60f,id); Check(value>=0 && value<=1);}
}
Console.WriteLine($"{checks} timing and range checks passed");
