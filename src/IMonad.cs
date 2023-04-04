using System;

namespace Monads;

public interface IMonad<A, B, This, Next>
	where This : IMonad<A, B, This, Next>
	where Next : IMonad<B, B, Next, Next>
{
	static abstract This Return(A a);

	Next Bind(Func<A, Next> f);
}

public static class Monad
{
	public static Next FMap<A, B, This, Next>(this This m, Func<A, B> f)
		where This : IMonad<A, B, This, Next>
		where Next : IMonad<B, B, Next, Next> =>
	m.Bind(a => Next.Return(f(a)));
}
