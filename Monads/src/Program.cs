using System;

var result = ProgramM.MaybeFunction();
Console.WriteLine($"Maybe: {result}");
var result2 = ProgramM.MaybeFunction2();
Console.WriteLine($"Maybe2: {result2}");

var sm = ProgramM.StateFunction();
var res = sm.Function(2);
Console.WriteLine($"State: {res}");

var sm2 = ProgramM.StateFunction2();
var res2 = sm2.Function(2);
Console.WriteLine($"State2: {res2}");

var sm3 = ProgramM.StateFunction3();
var res30 = sm3.Function(0);
Console.WriteLine($"State30: {res30}");
var res31 = sm3.Function(1);
Console.WriteLine($"State31: {res31}");
var res32 = sm3.Function(2);
Console.WriteLine($"State32: {res32}");

var sm32 = ProgramM.StateFunction32();
var res320 = sm32.Function(0);
Console.WriteLine($"State320: {res320}");
var res321 = sm32.Function(1);
Console.WriteLine($"State321: {res321}");
var res322 = sm32.Function(2);
Console.WriteLine($"State322: {res322}");

var loop = ProgramM.LoopStateFunction(new int[] { 0, 1, 20, 300 });
var loopRes = loop.Function(0);
Console.WriteLine($"LoopState: {loopRes}");

var loop2 = ProgramM.LoopStateFunction2(5);
var loopRes2 = loop2.Function(0);
Console.WriteLine($"LoopState2: {loopRes2}");

var loop3 = ProgramM.LoopStateFunction3(5);
var loopRes3 = loop3.Function(0);
Console.WriteLine($"LoopState3: {loopRes3}");
