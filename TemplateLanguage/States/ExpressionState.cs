﻿using System;
using System.Runtime.InteropServices;
using LightLexer;

namespace TemplateLanguage
{

	internal class ExpressionState : IState
	{
		public ExitCode OnStep(ref ParsedTemplate sm, ref AbstractSyntaxTree ast, ref Token token)
		{
            if (token.Is(TokenType.Whitespace))
				return sm.Continue();

			if (token.Is(TokenType.Number))
			{
				if (token.Is(TokenType.Number, NumberType.Integer))
					ast.InsertNumber(token, NodeType.Integer);

				if (token.Is(TokenType.Number, NumberType.Float))
					ast.InsertNumber(token, NodeType.Float);

				return sm.Continue();
			}
			else if (token.Is(TokenType.Operator))
			{
				switch (token.Get<OperatorType>(1))
				{
					case OperatorType.Add:
						ast.InsertOperator(NodeType.Add);

						ast.BracketOpen();
						sm.Transition(EngineState.Expression, ref ast);
						ast.BracketClose();

						return OnStep(ref sm, ref ast, ref token);
					case OperatorType.Subtract:
						ast.InsertOperator(NodeType.Subtract);

						ast.BracketOpen();
						sm.Transition(EngineState.Expression, ref ast);
						ast.BracketClose();

						return OnStep(ref sm, ref ast, ref token);
					case OperatorType.Multiply:
						ast.InsertOperator(NodeType.Multiply);

						break;
					case OperatorType.Divide:
						ast.InsertOperator(NodeType.Divide);

						break;
					case OperatorType.Variable:
						if (!sm.Consume())
							return ExitCode.Exit;

						ast.InsertString(token);
						ast.InsertVariable();

						break;
					default:
						return sm.PopState();
				}

				return sm.Continue();
			}
			else if (token.Is(TokenType.Bracket))
			{
				switch (token.Get<BracketType>(1))
				{
					case BracketType.Open:
                        ast.BracketOpen();
						sm.Transition(EngineState.Expression, ref ast, repeatToken: false);
						ast.BracketClose();

						return sm.Continue();
					default:
						return sm.PopState();
				}
			}

            return sm.PopState();
		}
	}
}