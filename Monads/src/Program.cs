using System;

var result = ProgramM.MaybeFunction();
Console.WriteLine($"Maybe: {result}");
var result2 = ProgramM.MaybeFunction2();
Console.WriteLine($"Maybe2: {result2}");

var sm = ProgramM.StateFunction();
var res = sm.Function(2);
Console.WriteLine($"State: {res}");
