using System;

var result = ProgramM.MaybeFunction();
Console.WriteLine($"Maybe: {result}");

var sm = ProgramM.StateFunction();
var res = sm.Function(2);
Console.WriteLine($"State: {res}");
