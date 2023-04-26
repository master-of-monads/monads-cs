using System;
using System.Collections.Generic;

namespace Monads.SourceGenerator;

public static class Loops
{
	public static M BindForEachStatement<T, A, M>(IEnumerable<T> values, Func<T, M> body)
		where M : IMonad<A, A, M, M>
	{
		var enumerator = values.GetEnumerator();
		return RecurseForEachStatement<T, A, M>(enumerator, body);
	}

	public static M RecurseForEachStatement<T, A, M>(IEnumerator<T> enumerator, Func<T, M> body)
		where M : IMonad<A, A, M, M>
	{
		if (!enumerator.MoveNext())
		{
			enumerator.Dispose();
			return M.Return(default!);
		}

		var head = enumerator.Current;
		return body(head).Bind(_ => RecurseForEachStatement<T, A, M>(enumerator, body));
	}
}
