using System;
using System.Text;
using Tokhenizer;

namespace TemplateLanguage
{
	internal class TextState : IState
	{
		public ExitCode OnStep(ref ParsedTemplate sm, ref AbstractSyntaxTree ast, ref Token token)
		{
			if (token.Is(TokenType.Bracket, BracketType.AccessorClose) || token.Is(TokenType.Bracket, BracketType.EnumerableAccessorClose))
				return sm.PopState();

			if (token.Is(TokenType.Bracket, BracketType.Code))
			{
				ast.StartCodeBlock();
				ast.BracketOpen();
				sm.Transition(EngineState.Code, ref ast);
				ast.BracketClose();
			}
			else if (token.Is(TokenType.Operator, OperatorType.Variable))
			{
				ast.StartCodeBlock();
				ast.BracketOpen();
				sm.Transition(EngineState.Variable, ref ast, repeatToken: true);
				ast.BracketClose();

                return OnStep(ref sm, ref ast, ref token);
			}
			else if (token.Is(TokenType.Operator, OperatorType.NewLine))
			{
				ast.InsertNewLineBlock();
			}
			else
			{
				ast.InsertTextBlock(token);
			}

			return sm.Continue();
		}
	}

	internal class VariableState : IState
	{
		public ExitCode OnStep(ref ParsedTemplate sm, ref AbstractSyntaxTree ast, ref Token token)
		{
			if (token.Is(TokenType.Operator, OperatorType.Variable))
			{
				if (!sm.Consume())
					return ExitCode.Exit;
				
				ast.InsertString(token);
				ast.InsertVariable();

				return sm.Continue();
			}
			else if (token.Is(TokenType.Operator, OperatorType.Accessor))
			{
				if (!sm.Consume())
					return ExitCode.Exit;

				ast.InsertOperator(NodeType.Accessor);
				ast.InsertString(token);

				return sm.Continue();
			}
			else if (token.Is(TokenType.Bracket, BracketType.AccessorOpen))
			{
				ast.InsertOperator(NodeType.AccessorBlock);
				ast.BracketOpen();
				sm.Transition(EngineState.TextState, ref ast, repeatToken: false);
				ast.BracketClose();

				sm.Consume();
				return sm.PopState();
			}
			else if (token.Is(TokenType.Bracket, BracketType.EnumerableAccessorOpen))
			{
				ast.InsertOperator(NodeType.EnumerableAccessorBlock);
				ast.BracketOpen();
				int repeatIdx = ast.InsertOperator(NodeType.RepeatCodeBlock);
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
                ast.InsertFilter();
				ast.BracketOpen();
				sm.Transition(EngineState.Code, ref ast, repeatToken: false);
				ast.BracketClose();

				return sm.Continue();
			}

			return sm.PopState();
		}
	}
}