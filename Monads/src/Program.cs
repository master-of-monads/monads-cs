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
