using System;
using Monads;

var result = Maybe.None<string, string>()
	.Bind(s => Maybe.Some<string, string>(s.ToUpper()));

Console.WriteLine($"Maybe: {result}");

State<int, string, string> sm = State.Get<int, string>()
	.Bind(i => State.Set<int, string>(i + 2)
		.Bind(_ => State.Get<int, string>()
			.Bind(i => State.Return<int, string, string>(i.ToString()))));

var res = sm.Function(2);
Console.WriteLine($"State: {res}");
