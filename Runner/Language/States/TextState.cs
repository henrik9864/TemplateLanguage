﻿using System;
using System.Text;
using LightLexer;
using LightParser;
using Runner;

namespace Runner
{
	public class TextState : IState<NodeType, EngineState>
	{
		public ExitCode OnStep(ref Parser<NodeType, EngineState> sm, ref AbstractSyntaxTree<NodeType> ast, ref Token token)
		{
			if (token.Is(TokenType.Bracket, BracketType.AccessorClose) || token.Is(TokenType.Bracket, BracketType.EnumerableAccessorClose))
				return sm.PopState();

			if (token.Is(TokenType.Bracket, BracketType.Code))
			{
				int codeBlock = ast.InsertRight(NodeType.CodeBlock, token);
				ast.SetLeft(codeBlock);

				ast.BracketOpen();
				sm.Transition(EngineState.Code, ref ast);
				ast.BracketClose();
			}
			else if (token.Is(TokenType.Operator, OperatorType.Variable))
			{
				int codeBlock = ast.InsertRight(NodeType.VariableBlock, token);
				ast.SetLeft(codeBlock);

				ast.BracketOpen();
				sm.Transition(EngineState.Variable, ref ast, repeatToken: true);
				ast.BracketClose();

				if (sm.IsEnd() && (token.Is(TokenType.Bracket, BracketType.AccessorClose) || token.Is(TokenType.Bracket, BracketType.EnumerableAccessorClose)))
					return sm.PopState();

                return OnStep(ref sm, ref ast, ref token);
			}
			else if (token.Is(TokenType.Operator, OperatorType.NewLine))
			{
				ast.InsertRight(NodeType.NewLineBlock, token);
			}
			else
			{
				ast.InsertRight(NodeType.TextBlock, token);
			}

			return sm.Continue();
		}
	}

	public class VariableState : IState<NodeType, EngineState>
	{
		public ExitCode OnStep(ref Parser<NodeType, EngineState> sm, ref AbstractSyntaxTree<NodeType> ast, ref Token token)
		{
			if (token.Is(TokenType.Operator, OperatorType.Variable))
			{
				if (!sm.Consume())
					return ExitCode.Exit;

				int var = ast.InsertRight(NodeType.Variable);
				ast.SetRight(var);
				ast.InsertNode(NodeType.String, token);

				return sm.Continue();
			}
			else if (token.Is(TokenType.Operator, OperatorType.Accessor))
			{
				if (!sm.Consume())
					return ExitCode.Exit;

				int accessor = ast.TakeLeft(NodeType.Accessor);
				ast.SetRight(accessor);
				ast.InsertNode(NodeType.String, token);

				return sm.Continue();
			}
			else if (token.Is(TokenType.Bracket, BracketType.AccessorOpen))
			{
				int accessorBlock = ast.TakeLeft(NodeType.AccessorBlock);
				ast.SetRight(accessorBlock);

				ast.BracketOpen();
				sm.Transition(EngineState.TextState, ref ast, repeatToken: false);
				ast.BracketClose();

				sm.Consume();

				return sm.PopState();
			}
			else if (token.Is(TokenType.Bracket, BracketType.EnumerableAccessorOpen))
			{
				int enumAccessor = ast.TakeLeft(NodeType.EnumerableAccessorBlock);
				ast.SetRight(enumAccessor);

				ast.BracketOpen();

				int repeatIdx = ast.TakeLeft(NodeType.RepeatCodeBlock);
				ast.SetRight(repeatIdx);

				ast.BracketOpen();
				sm.Transition(EngineState.TextState, ref ast, repeatToken: false);
				ast.BracketClose();

				sm.Consume();

                if (token.Is(TokenType.Bracket, BracketType.EnumerableAccessorOpen))
				{
                    ast.SetLeft(repeatIdx);
					ast.BracketOpen();
					sm.Transition(EngineState.TextState, ref ast, repeatToken: false);
					ast.BracketClose();
					sm.Consume();
				}

				ast.BracketClose();

				return sm.PopState();
			}
			else if (token.Is(TokenType.Bracket, BracketType.Code))
			{
                int filter = ast.InsertMiddle(NodeType.Filter);
				ast.SetRight(filter);

				ast.BracketOpen();
				sm.Transition(EngineState.Code, ref ast, repeatToken: false);
				ast.BracketClose();

				return sm.Continue();
			}

			return sm.PopState();
		}
	}
}