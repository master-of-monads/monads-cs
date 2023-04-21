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
		if (methodDeclaration.AttributeLists.SelectMany(al => al.Attributes).Any(a => a.Name.ToString() == "Monadic") && methodDeclaration.Body is BlockSyntax bodyBlock)
		{
			return methodDeclaration.WithBody(BindInBlock(bodyBlock));
		}
		else
		{
			return methodDeclaration;
		}
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

		if (statement is ExpressionStatementSyntax expressionStatement)
		{
			return BindStatement(expressionStatement, statements);
		}
		else if (statement is LocalDeclarationStatementSyntax localDeclarationStatement)
		{
			return BindLocalDeclaration(localDeclarationStatement, statements);
		}
		else
		{
			statements.AddFirst(statement);
			return statements;
		}
	}

	private static LinkedList<StatementSyntax> BindStatement(ExpressionStatementSyntax expressionStatement, LinkedList<StatementSyntax> statements)
	{
		var rest = BindInStatements(statements);

		var (binder, boundExpr) = BindExpression(expressionStatement.Expression);
		if (boundExpr is not IdentifierNameSyntax)
		{
			rest.AddFirst(expressionStatement.WithExpression(boundExpr));
		}

		if (!binder.NeedsBinding())
		{
			return rest;
		}

		var thenBlock = SyntaxFactory.Block(rest)!;
		var statement = binder.Bind(thenBlock);
		return new LinkedList<StatementSyntax>(new[] { statement });
	}

	private static LinkedList<StatementSyntax> BindLocalDeclaration(LocalDeclarationStatementSyntax localDeclarationStatement, LinkedList<StatementSyntax> statements)
	{
		var rest = BindInStatements(statements);

		var (binder, boundExpr) = BindExpression(localDeclarationStatement.Declaration.Variables[0].Initializer!.Value);
		var newDeclaration = localDeclarationStatement.Declaration.WithVariables(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.VariableDeclarator(localDeclarationStatement.Declaration.Variables[0].Identifier, null, SyntaxFactory.EqualsValueClause(boundExpr))));
		rest.AddFirst(localDeclarationStatement.WithDeclaration(newDeclaration));

		if (!binder.NeedsBinding())
		{
			return rest;
		}

		var thenBlock = SyntaxFactory.Block(rest)!;
		var statement = binder.Bind(thenBlock);
		return new LinkedList<StatementSyntax>(new[] { statement });
	}

	private static (ExpressionBinder, ExpressionSyntax) BindExpression(ExpressionSyntax expression)
	{
		var binder = new ExpressionBinder();
		var boundExpr = binder.Visit(expression) as ExpressionSyntax;
		return (binder, boundExpr!);
	}
}
