using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Monads.SourceGenerator;

public class ExpressionBinder : CSharpSyntaxRewriter
{
	private readonly LinkedList<TempBind> tempBinds = new LinkedList<TempBind>();

	public override SyntaxNode? VisitPostfixUnaryExpression(PostfixUnaryExpressionSyntax postfixUnaryExpression)
	{
		if (!postfixUnaryExpression.OperatorToken.IsKind(SyntaxKind.ExclamationToken))
		{
			return base.VisitPostfixUnaryExpression(postfixUnaryExpression);
		}

		var innerExpression = HandleExpressionInBind(postfixUnaryExpression.Operand);
		return AddTempBind(innerExpression);
	}

	private ExpressionSyntax HandleExpressionInBind(ExpressionSyntax expression)
	{
		return (Visit(expression) as ExpressionSyntax)!;
	}

	private IdentifierNameSyntax AddTempBind(ExpressionSyntax expression)
	{
		var name = $"___monads_cs_temp_ident_{tempBinds.Count}";
		var identifier = SyntaxFactory.IdentifierName(name);
		tempBinds.AddLast(new TempBind(expression, identifier));
		return identifier;
	}

	public bool NeedsBinding() =>
		tempBinds.Count > 0;

	public StatementSyntax Bind(BlockSyntax finalBlock)
	{
		if (!(tempBinds.First?.Value is TempBind tempBind))
		{
			return finalBlock;
		}
		tempBinds.RemoveFirst();

		var thenStatement = Bind(finalBlock);
		var thenBlock = SyntaxFactory.Block(thenStatement);
		return BuildMonadicBind(tempBind.Expression, tempBind.Identifier, thenBlock);
	}

	public static StatementSyntax BuildMonadicBind(ExpressionSyntax expression, IdentifierNameSyntax identifier, BlockSyntax thenBlock)
	{
		var bindMethod = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, expression, SyntaxFactory.IdentifierName("Bind"));
		var bindLambda = SyntaxFactory.SimpleLambdaExpression(SyntaxFactory.Parameter(identifier.Identifier), thenBlock);
		var bindInvocation = SyntaxFactory.InvocationExpression(bindMethod, SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(bindLambda))))
			.WithLeadingTrivia(SyntaxFactory.Whitespace(" "));
		return SyntaxFactory.ReturnStatement(bindInvocation);
	}
}

class TempBind
{
	public ExpressionSyntax Expression { get; }
	public IdentifierNameSyntax Identifier { get; }

	public TempBind(ExpressionSyntax expression, IdentifierNameSyntax identifier)
	{
		Expression = expression;
		Identifier = identifier;
	}
}
