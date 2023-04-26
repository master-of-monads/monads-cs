using System;
using System.Collections.Generic;
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

		statements.RemoveFirst();
		return BindStatement(statement, statements);
	}

	private LinkedList<StatementSyntax> BindStatement(StatementSyntax statement, LinkedList<StatementSyntax> statements) =>
		BindIfStatement(statement, statements) ??
			BindMonadicForEachStatement(statement, statements) ??
			BindMonadicWhileStatement(statement, statements) ??
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
		var block = (BlockSyntax)statement;

		var rest = BindInStatements(statements);

		var defaultReturn = BuildDefaultReturn();
		var boundBlock = MonadicBangRewriter.BindInBlock(block.AddStatements(defaultReturn), returnType);
		var thenBlock = SyntaxFactory.Block(rest)!;
		return new LinkedList<StatementSyntax>(new[] { BindBlockToReturn(boundBlock, thenBlock) });

		static bool IsBlockStatement(StatementSyntax statement, LinkedList<StatementSyntax> statements) =>
			statement is BlockSyntax;
	}

	private LinkedList<StatementSyntax>? BindIfStatement(StatementSyntax statement, LinkedList<StatementSyntax> statements)
	{
		if (!IsIfStatement(statement, statements))
		{
			return null;
		}
		var ifStatement = (IfStatementSyntax)statement;

		var rest = BindInStatements(statements);
		var thenBlock = SyntaxFactory.Block(rest)!;

		var (conditionBinder, boundCondition) = BindExpression(ifStatement.Condition);
		ifStatement = ifStatement.WithCondition(boundCondition);

		if (ifStatement.Statement is not BlockSyntax)
		{
			ifStatement = ifStatement.WithStatement(SyntaxFactory.Block(ifStatement.Statement));
		}
		var statementBlock = (BlockSyntax)ifStatement.Statement;
		statementBlock = statementBlock.AddStatements(BuildDefaultReturn());
		var boundStatementBlock = MonadicBangRewriter.BindInBlock(statementBlock, returnType);
		ifStatement = ifStatement.WithStatement(BindBlockToReturn(boundStatementBlock, thenBlock));

		if (ifStatement.Else is null)
		{
			var elseStatement = BuildDefaultReturn();
			ifStatement = ifStatement.WithElse(SyntaxFactory.ElseClause(ExpressionBinder.BuildMonadicBind(elseStatement.Expression!, thenBlock)));
		}
		else if (ifStatement.Else is ElseClauseSyntax { Statement: IfStatementSyntax elseIfStatement } elseClause)
		{
			var boundElseBlock = MonadicBangRewriter.BindInBlock(SyntaxFactory.Block(elseIfStatement), returnType);
			ifStatement = ifStatement.WithElse(elseClause.WithStatement(BindBlockToReturn(boundElseBlock, thenBlock)));
		}
		else
		{
			if (ifStatement.Else is ElseClauseSyntax { Statement: not BlockSyntax })
			{
				ifStatement = ifStatement.WithElse(ifStatement.Else.WithStatement(SyntaxFactory.Block(ifStatement.Else.Statement)));
			}
			var elseBlock = (BlockSyntax)ifStatement.Else!.Statement;
			elseBlock = elseBlock.AddStatements(BuildDefaultReturn());
			var boundElseBlock = MonadicBangRewriter.BindInBlock(elseBlock, returnType);
			ifStatement = ifStatement.WithElse(ifStatement.Else.WithStatement(BindBlockToReturn(boundElseBlock, thenBlock)));
		}

		if (!conditionBinder.NeedsBinding())
		{
			return new LinkedList<StatementSyntax>(new[] { ifStatement });
		}

		var ifBlock = SyntaxFactory.Block(ifStatement)!;
		var blockStatement = conditionBinder.Bind(ifBlock);
		return new LinkedList<StatementSyntax>(new[] { blockStatement });

		static bool IsIfStatement(StatementSyntax statement, LinkedList<StatementSyntax> statements) =>
			statement is IfStatementSyntax;
	}

	public LinkedList<StatementSyntax>? BindMonadicForEachStatement(StatementSyntax statement, LinkedList<StatementSyntax> statements)
	{
		if (!IsForEachStatement(statement, statements))
		{
			return null;
		}
		var forEachStatement = (ForEachStatementSyntax)statements.First!.Value;
		statements.RemoveFirst();

		var rest = BindInStatements(statements);
		var thenBlock = SyntaxFactory.Block(rest)!;

		var (collectionBinder, boundCollection) = BindExpression(forEachStatement.Expression);

		if (forEachStatement.Statement is not BlockSyntax)
		{
			forEachStatement = forEachStatement.WithStatement(SyntaxFactory.Block(forEachStatement.Statement));
		}
		var statementBlock = (BlockSyntax)forEachStatement.Statement;
		statementBlock = statementBlock.AddStatements(BuildDefaultReturn());
		var boundStatementBlock = MonadicBangRewriter.BindInBlock(statementBlock, returnType);

		var boundForEachStatement = SyntaxFactory.ReturnStatement(
				SyntaxFactory.InvocationExpression(
					SyntaxFactory.GenericName(
						SyntaxFactory.Identifier("global::Monads.SourceGenerator.Loops.BindForEachStatement"),
						SyntaxFactory.TypeArgumentList(
							SyntaxFactory.SeparatedList(new[]
							{
								forEachStatement.Type,
								GetMonadReturnType(),
								returnType
							}))
					),
					SyntaxFactory.ArgumentList(
						SyntaxFactory.SeparatedList(new[]
						{
							SyntaxFactory.Argument(boundCollection),
							SyntaxFactory.Argument(
								SyntaxFactory.SimpleLambdaExpression(
									SyntaxFactory.Parameter(forEachStatement.Identifier),
									boundStatementBlock))
						})))
			.WithLeadingTrivia(forEachStatement.GetLeadingTrivia()
				.Add(SyntaxFactory.Whitespace(" "))));

		if (collectionBinder.NeedsBinding())
		{
			boundForEachStatement = (ReturnStatementSyntax)collectionBinder.Bind(SyntaxFactory.Block(boundForEachStatement));
		}

		return new LinkedList<StatementSyntax>(new[] { ExpressionBinder.BuildMonadicBind(boundForEachStatement.Expression!, thenBlock) });

		static bool IsForEachStatement(StatementSyntax statement, LinkedList<StatementSyntax> statements) =>
			statement is ExpressionStatementSyntax expressionStatement &&
				expressionStatement.Expression is IdentifierNameSyntax { Identifier: SyntaxToken { Text: "monadic" } } &&
				expressionStatement.SemicolonToken.IsMissing &&
				statements.First?.Value is ForEachStatementSyntax;
	}

	private LinkedList<StatementSyntax>? BindMonadicWhileStatement(StatementSyntax statement, LinkedList<StatementSyntax> statements)
	{
		if (!IsWhileStatement(statement, statements))
		{
			return null;
		}
		var whileStatement = (WhileStatementSyntax)statements.First!.Value;
		statements.RemoveFirst();

		var rest = BindInStatements(statements);
		var thenBlock = SyntaxFactory.Block(rest)!;

		if (whileStatement.Statement is not BlockSyntax)
		{
			whileStatement = whileStatement.WithStatement(SyntaxFactory.Block(whileStatement.Statement));
		}
		var statementBlock = (BlockSyntax)whileStatement.Statement;
		statementBlock = statementBlock.AddStatements(BuildDefaultReturn());
		var boundStatementBlock = MonadicBangRewriter.BindInBlock(statementBlock, returnType);

		var boundWhileStatement = SyntaxFactory.ReturnStatement(
				SyntaxFactory.InvocationExpression(
					SyntaxFactory.GenericName(
						SyntaxFactory.Identifier("global::Monads.SourceGenerator.Loops.BindWhileStatement"),
						SyntaxFactory.TypeArgumentList(
							SyntaxFactory.SeparatedList(new[]
							{
								GetMonadReturnType(),
								returnType
							}))
					),
					SyntaxFactory.ArgumentList(
						SyntaxFactory.SeparatedList(new[]
						{
							SyntaxFactory.Argument(
								SyntaxFactory.ParenthesizedLambdaExpression(
									SyntaxFactory.ParameterList(),
									block: null,
									expressionBody: whileStatement.Condition!)),
							SyntaxFactory.Argument(
								SyntaxFactory.ParenthesizedLambdaExpression(
									SyntaxFactory.ParameterList(),
									block: boundStatementBlock,
									expressionBody: null))
						})))
			.WithLeadingTrivia(whileStatement.GetLeadingTrivia()
				.Add(SyntaxFactory.Whitespace(" "))));

		return new LinkedList<StatementSyntax>(new[] { ExpressionBinder.BuildMonadicBind(boundWhileStatement.Expression!, thenBlock) });

		static bool IsWhileStatement(StatementSyntax statement, LinkedList<StatementSyntax> statements) =>
			statement is ExpressionStatementSyntax expressionStatement &&
				expressionStatement.Expression is IdentifierNameSyntax { Identifier: SyntaxToken { Text: "monadic" } } &&
				expressionStatement.SemicolonToken.IsMissing &&
				statements.First?.Value is WhileStatementSyntax;
	}

	private LinkedList<StatementSyntax> NoBind(StatementSyntax statement, LinkedList<StatementSyntax> statements)
	{
		var rest = BindInStatements(statements);
		rest.AddFirst(statement);
		return rest;
	}

	private ReturnStatementSyntax BuildDefaultReturn() =>
		SyntaxFactory.ReturnStatement(
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

	private (ExpressionBinder, StatementSyntax) BindExpressionsInStatement(StatementSyntax expression)
	{
		var binder = new ExpressionBinder();
		var boundExpr = binder.Visit(expression) as StatementSyntax;
		return (binder, boundExpr!);
	}

	private (ExpressionBinder, ExpressionSyntax) BindExpression(ExpressionSyntax expression)
	{
		var binder = new ExpressionBinder();
		var boundExpr = binder.Visit(expression) as ExpressionSyntax;
		return (binder, boundExpr!);
	}

	private BlockSyntax BindBlockToReturn(BlockSyntax block, BlockSyntax thenBlock)
	{
		if (thenBlock.Statements.Count == 0)
		{
			return block;
		}

		var returnBinder = new ReturnBinder(thenBlock);
		return (BlockSyntax)returnBinder.Visit(block);
	}

	private bool ShouldBeReInserted(StatementSyntax statement) =>
		statement is not ExpressionStatementSyntax { Expression: IdentifierNameSyntax };

	private TypeSyntax GetMonadReturnType() =>
		returnType switch
		{
			GenericNameSyntax { TypeArgumentList: TypeArgumentListSyntax { Arguments: { Count: > 0 } genericArguments } } =>
				genericArguments.Last(),
			_ => throw new Exception("Invalid monad return type"),
		};
}
