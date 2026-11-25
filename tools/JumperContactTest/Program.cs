using System;
using KnightInCradle;
int checks=0;
void Check(bool result, string message) { if(!result) throw new Exception(message); checks++; }
Check(JumperContact.Lands(20,20.5f,9,10.1f,19,22,10), "translated board catches falling feet");
Check(!JumperContact.Lands(20,20.5f,9,10.1f,1,4,10), "original position is not a platform");
Check(JumperContact.Lands(20,20.5f,8,12,19,22,10), "fast downward crossing");
Check(!JumperContact.Lands(20,20.5f,12,8,19,22,10), "rising through underside");
Check(!JumperContact.Lands(20,20.5f,11,12,19,22,10), "already below surface");
Check(!JumperContact.Lands(22,22.5f,9,10,19,22,10), "no overlap at edge");
Check(!JumperContact.Lands(20,20.5f,7,8,19,22,10), "too far above surface");
for(int i=-100;i<=100;i++) {
 float shift=i*0.17f;
 Check(JumperContact.Lands(20+shift,20.5f+shift,9+shift,10.1f+shift,19+shift,22+shift,10+shift), "translation invariance");
}
Console.WriteLine($"{checks} checks passed");
