using System;

namespace Monads;

public class State<S, A, B> : IMonad<A, B, State<S, A, B>, State<S, B, B>>
{
	public static State<S, A, B> Return(A a) =>
		new(s => (s, a));

	public Func<S, (S, A)> Function { get; }

	internal State(Func<S, (S, A)> function) =>
		Function = function;

	public State<S, B, B> Bind(Func<A, State<S, B, B>> f) =>
		new(state =>
		{
			var (newState, a) = Function(state);
			var next = f(a);
			return next.Function(newState);
		});
}

public static class State
{
	public static State<S, A, B> Return<S, A, B>(A a) =>
		State<S, A, B>.Return(a);

	public static State<S, S, B> Get<S, B>() =>
		new(s => (s, s));

	public static State<S, object, B> Set<S, B>(S state) =>
		new(_ => (state, new()));
}
