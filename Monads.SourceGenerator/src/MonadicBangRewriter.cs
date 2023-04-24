using System.Linq;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Monads.SourceGenerator;

public class MonadicBangRewriter : CSharpSyntaxRewriter
{
	public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax methodDeclaration)
	{
		if (methodDeclaration.AttributeLists.SelectMany(al => al.Attributes).Any(a => a.Name.ToString() == "Monadic"))
		{
			if (methodDeclaration.ExpressionBody is ArrowExpressionClauseSyntax arrowExpression)
			{
				methodDeclaration = ConvertToBlockBody(methodDeclaration, arrowExpression);
			}
			if (methodDeclaration.Body is BlockSyntax bodyBlock)
			{
				return methodDeclaration.WithBody(BindInBlock(bodyBlock, methodDeclaration.ReturnType));
			}
		}

		return methodDeclaration;
	}

	private static MethodDeclarationSyntax ConvertToBlockBody(MethodDeclarationSyntax methodDeclaration, ArrowExpressionClauseSyntax arrowExpression)
	{
		var returnStatement = SyntaxFactory.ReturnStatement(arrowExpression.Expression.WithLeadingTrivia(SyntaxFactory.Whitespace(" ")));
		var block = SyntaxFactory.Block(returnStatement);
		return methodDeclaration.WithExpressionBody(null)
			.WithSemicolonToken(default)
			.WithBody(block);
	}

	public static BlockSyntax BindInBlock(BlockSyntax block, TypeSyntax returnType)
	{
		var statements = new LinkedList<StatementSyntax>(block.Statements);
		statements = StatementBinder.BindInStatements(statements, returnType);
		return block.WithStatements(new SyntaxList<StatementSyntax>(statements));
	}
}
