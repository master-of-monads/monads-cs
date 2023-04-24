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
				return methodDeclaration.WithBody(BindInBlock(bodyBlock));
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

	private static BlockSyntax BindInBlock(BlockSyntax block)
	{
		var statements = new LinkedList<StatementSyntax>(block.Statements);
		statements = BindInStatements(statements);
		return block.WithStatements(new SyntaxList<StatementSyntax>(statements));
	}

	private static LinkedList<StatementSyntax> BindInStatements(LinkedList<StatementSyntax> statements)
	{
		if (!(statements.First?.Value is StatementSyntax statement))
		{
			return statements;
		}
		statements.RemoveFirst();

		if (ShouldBeBound(statement, statements))
		{
			return BindStatement(statement, statements);
		}

		statements.AddFirst(statement);
		return statements;
	}

	private static LinkedList<StatementSyntax> BindStatement(StatementSyntax statement, LinkedList<StatementSyntax> statements)
	{
		var rest = BindInStatements(statements);

		var (binder, boundStatement) = BindExpressionsInStatement(statement);
		if (ShouldBeReInserted(boundStatement))
		{
			rest.AddFirst(boundStatement);
		}

		if (!binder.NeedsBinding())
		{
			return rest;
		}

		var thenBlock = SyntaxFactory.Block(rest)!;
		var blockStatement = binder.Bind(thenBlock);
		return new LinkedList<StatementSyntax>(new[] { blockStatement });
	}

	private static (ExpressionBinder, StatementSyntax) BindExpressionsInStatement(StatementSyntax expression)
	{
		var binder = new ExpressionBinder();
		var boundExpr = binder.Visit(expression) as StatementSyntax;
		return (binder, boundExpr!);
	}

	private static bool ShouldBeBound(StatementSyntax statement, LinkedList<StatementSyntax> statements) =>
		statement is ExpressionStatementSyntax or
				LocalDeclarationStatementSyntax or
				ReturnStatementSyntax;

	private static bool ShouldBeReInserted(StatementSyntax statement) =>
		statement is not ExpressionStatementSyntax { Expression: IdentifierNameSyntax };
}
