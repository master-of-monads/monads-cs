using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Monads.SourceGenerator;

public static class StatementBinder
{
	public static LinkedList<StatementSyntax> BindInStatements(LinkedList<StatementSyntax> statements)
	{
		if (!(statements.First?.Value is StatementSyntax statement))
		{
			return statements;
		}

		statements.RemoveFirst();
		return BindStatement(statement, statements);
	}

	private static LinkedList<StatementSyntax> BindStatement(StatementSyntax statement, LinkedList<StatementSyntax> statements) =>
		BindSimpleStatement(statement, statements) ??
			NoBind(statement, statements);

	private static LinkedList<StatementSyntax>? BindSimpleStatement(StatementSyntax statement, LinkedList<StatementSyntax> statements)
	{
		if (!IsSimpleStatement(statement))
		{
			return null;
		}

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

		static bool IsSimpleStatement(StatementSyntax statement) =>
			statement is ExpressionStatementSyntax or
					LocalDeclarationStatementSyntax or
					ReturnStatementSyntax;
	}

	private static LinkedList<StatementSyntax> NoBind(StatementSyntax statement, LinkedList<StatementSyntax> statements)
	{
		statements.AddFirst(statement);
		return statements;
	}

	private static (ExpressionBinder, StatementSyntax) BindExpressionsInStatement(StatementSyntax expression)
	{
		var binder = new ExpressionBinder();
		var boundExpr = binder.Visit(expression) as StatementSyntax;
		return (binder, boundExpr!);
	}

	private static bool ShouldBeReInserted(StatementSyntax statement) =>
		statement is not ExpressionStatementSyntax { Expression: IdentifierNameSyntax };
}
