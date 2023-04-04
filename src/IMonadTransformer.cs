namespace Monads;

public interface IMonadTransformer<A, B, This, Next, Inner, InnerNext>
	where A : class
	where B : class
	where This : IMonad<A, B, This, Next>
	where Next : IMonad<B, B, Next, Next>
	where Inner : IMonad<A, B, Inner, InnerNext>
	where InnerNext : IMonad<B, B, InnerNext, InnerNext>
{
	// static abstract This Lift<Inner, InnerNext>(Inner inner)
	// 	where Inner : IMonad<A, B, Inner, InnerNext>
	// 	where InnerNext : IMonad<B, B, InnerNext, InnerNext>;
}
