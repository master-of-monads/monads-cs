using System.Linq;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Monads.SourceGenerator;

public class ReturnBinder : CSharpSyntaxRewriter
{
	private readonly BlockSyntax thenBlock;

	public ReturnBinder(BlockSyntax thenBlock)
	{
		this.thenBlock = thenBlock;
	}

	public override SyntaxNode? VisitReturnStatement(ReturnStatementSyntax returnStatement)
	{
		if (returnStatement.Expression is not null)
		{
			return ExpressionBinder.BuildMonadicBind(returnStatement.Expression, thenBlock);
		}
		return base.VisitReturnStatement(returnStatement);
	}

	public override SyntaxNode? VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax parenthesizedLambdaExpression) =>
		parenthesizedLambdaExpression;

	public override SyntaxNode? VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax simpleLambdaExpression) =>
		simpleLambdaExpression;
}
