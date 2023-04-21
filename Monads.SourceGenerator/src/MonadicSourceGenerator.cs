using System.IO;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Monads.SourceGenerator;

[Generator]
public class MonadicSourceGenerator : ISourceGenerator
{
	public void Initialize(GeneratorInitializationContext context)
	{
	}

	public void Execute(GeneratorExecutionContext context)
	{
		foreach (var file in context.AdditionalFiles)
		{
			if (!file.Path.EndsWith(".mcs"))
			{
				continue;
			}

			var source = file.GetText(context.CancellationToken);
			if (source is null)
			{
				continue;
			}

			var tree = CSharpSyntaxTree.ParseText(source);
			if (tree is null)
			{
				continue;
			}

			var rewriter = new MonadicBangRewriter();
			var newTree = rewriter.Visit(tree.GetRoot());
			var newSource = newTree.GetText(Encoding.UTF8);
			context.AddSource($"{Path.GetFileNameWithoutExtension(file.Path)}.g.cs", newSource);
		}
	}
}
