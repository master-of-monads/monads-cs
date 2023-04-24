using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Monads.SourceGenerator;

public class StatementBinder
{
	public static LinkedList<StatementSyntax> BindInStatements(LinkedList<StatementSyntax> statements, TypeSyntax returnType)
	{
		var statementBinder = new StatementBinder(returnType);
		return statementBinder.BindInStatements(statements);
	}

	private readonly TypeSyntax returnType;

	private StatementBinder(TypeSyntax returnType)
	{
		this.returnType = returnType;
	}

	public LinkedList<StatementSyntax> BindInStatements(LinkedList<StatementSyntax> statements)
	{
		if (!(statements.First?.Value is StatementSyntax statement))
		{
			return statements;
		}

		// Debugger.Launch();
		statements.RemoveFirst();
		return BindStatement(statement, statements);
	}

	private LinkedList<StatementSyntax> BindStatement(StatementSyntax statement, LinkedList<StatementSyntax> statements) =>
		BindBlockStatement(statement, statements) ??
			BindSimpleStatement(statement, statements) ??
			NoBind(statement, statements);

	private LinkedList<StatementSyntax>? BindSimpleStatement(StatementSyntax statement, LinkedList<StatementSyntax> statements)
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

	private LinkedList<StatementSyntax>? BindBlockStatement(StatementSyntax statement, LinkedList<StatementSyntax> statements)
	{
		if (!IsBlockStatement(statement, statements))
		{
			return null;
		}
		var monadicKeyword = statement;
		var block = (BlockSyntax)statements.First!.Value;
		statements.RemoveFirst();

		var rest = BindInStatements(statements);

		var unitReturn = SyntaxFactory.ReturnStatement(
				SyntaxFactory.InvocationExpression(
					SyntaxFactory.MemberAccessExpression(
						SyntaxKind.SimpleMemberAccessExpression,
						returnType,
						SyntaxFactory.IdentifierName("Return")),
					SyntaxFactory.ArgumentList(
						SyntaxFactory.SingletonSeparatedList(
							SyntaxFactory.Argument(
								SyntaxFactory.IdentifierName("default")))))
				.WithLeadingTrivia(SyntaxFactory.Whitespace(" ")));
		var boundBlock = MonadicBangRewriter.BindInBlock(block.AddStatements(unitReturn), returnType);
		var thenBlock = SyntaxFactory.Block(rest)!;
		return new LinkedList<StatementSyntax>(new[] { BindBlockToReturn(boundBlock, thenBlock) });

		static bool IsBlockStatement(StatementSyntax statement, LinkedList<StatementSyntax> statements) =>
			statement is ExpressionStatementSyntax expressionStatement &&
				expressionStatement.Expression is IdentifierNameSyntax { Identifier: SyntaxToken { Text: "monadic" } } &&
				expressionStatement.SemicolonToken.IsMissing &&
				statements.First?.Value is BlockSyntax;
	}

	private LinkedList<StatementSyntax> NoBind(StatementSyntax statement, LinkedList<StatementSyntax> statements)
	{
		var rest = BindInStatements(statements);
		rest.AddFirst(statement);
		return rest;
	}

	private (ExpressionBinder, StatementSyntax) BindExpressionsInStatement(StatementSyntax expression)
	{
		var binder = new ExpressionBinder();
		var boundExpr = binder.Visit(expression) as StatementSyntax;
		return (binder, boundExpr!);
	}

	private BlockSyntax BindBlockToReturn(BlockSyntax block, BlockSyntax thenBlock)
	{
		var returnBinder = new ReturnBinder(thenBlock);
		return (BlockSyntax)returnBinder.Visit(block);
	}

	private bool ShouldBeReInserted(StatementSyntax statement) =>
		statement is not ExpressionStatementSyntax { Expression: IdentifierNameSyntax };
}
