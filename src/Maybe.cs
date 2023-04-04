using System;

namespace Monads;

public class Maybe<A, B> : IMonad<A, B, Maybe<A, B>, Maybe<B, B>>
	where A : class
	where B : class
{
	public static Maybe<A, B> Return(A value) =>
		new(value);

	public A? Value { get; }

	internal Maybe(A? value) =>
		Value = value;

	public Maybe<B, B> Bind(Func<A, Maybe<B, B>> f) => Value switch
	{
		A value => f(value),
		null => new(null),
	};

	public override string ToString() => Value switch
	{
		A value => $"Some({value})",
		null => "None",
	};
}

public static class Maybe
{
	public static Maybe<A, B> Some<A, B>(A value)
		where A : class
		where B : class
	=> Maybe<A, B>.Return(value);

	public static Maybe<A, B> None<A, B>()
		where A : class
		where B : class
	=> new(null);
}
