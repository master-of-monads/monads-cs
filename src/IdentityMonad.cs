using System;

namespace Monads;

public class IdentityMonad<A, B> : IMonad<A, B, IdentityMonad<A, B>, IdentityMonad<B, B>>
{
	public static IdentityMonad<A, B> Return(A a) =>
		new(a);

	public A Value { get; }

	public IdentityMonad(A value) =>
		Value = value;

	public IdentityMonad<B, B> Bind(Func<A, IdentityMonad<B, B>> f) =>
		f(Value);
}
