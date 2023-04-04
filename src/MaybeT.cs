using System;

namespace Monads;

public class MaybeT<A, B, M, N>
	: IMonad<A, B, MaybeT<A, B, M, N>, MaybeT<B, B, N, N>>,
		IMonadTransformer<A, B, MaybeT<A, B, M, N>, MaybeT<B, B, N, N>>
	where A : class
	where B : class
	where M : IMonad<Maybe<A, B>, Maybe<B, B>, M, N>
	where N : IMonad<Maybe<B, B>, Maybe<B, B>, N, N>
{
	public static MaybeT<A, B, M, N> Return(A a) =>
		new(M.Return(Maybe.Some<A, B>(a)));

	public M Inner { get; }

	internal MaybeT(M inner) =>
		Inner = inner;

	public MaybeT<B, B, N, N> Bind(Func<A, MaybeT<B, B, N, N>> f) =>
		new(Inner.Bind(maybe => maybe.Value switch
		{
			A value => f(value).Inner,
			null => N.Return(Maybe.None<B, B>()),
		}));

	public static MaybeT<A, B, M, N> Lift<Inner, InnerNext>(Inner inner)
		where Inner : IMonad<A, B, Inner, InnerNext>
		where InnerNext : IMonad<Maybe<B, B>, Maybe<B, B>, InnerNext, InnerNext>
		=> new(inner.FMap<Maybe<A>, Maybe<B>, Inner, InnerNext>(maybe => maybe.Value switch
		{
			A value => Maybe.Some<A, B>(value),
			null => Maybe.None<A, B>(),
		}));
}
