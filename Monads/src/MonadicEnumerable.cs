using System;
using System.Collections.Generic;
using System.Linq;

namespace Monads;

public static class MonadicEnumerable
{
	public static IEnumerable<T> Return<T>(T value) =>
		new[] { value };

	public static IEnumerable<U> Bind<T, U>(this IEnumerable<T> source, Func<T, IEnumerable<U>> selector) =>
		source.SelectMany(selector);
}
