﻿using System.Text;
using Tokhenizer;

namespace TemplateLanguage
{
	internal class StringState : IState
	{
		public ExitCode OnStep(ref ParsedTemplate sm, ref AbstractSyntaxTree ast, ref Token token)
		{
			if (token.Get<TokenType>(0) == TokenType.Bracket && token.Get<BracketType>(1) == BracketType.Code)
			{
				ast.StartCodeBlock();
				ast.BracketOpen();
				sm.Transition(EngineState.Code, ref ast);
				ast.BracketClose();
            }
			else if(token.Get<TokenType>(0) == TokenType.Operator && token.Get<OperatorType>(1) == OperatorType.Variable)
			{
				sm.Consume();
				ast.InsertVariable(token);

				/*
				while (token.Is(TokenType.Bracket,BracketType.AccessorOpen))
				{

				}
				*/
			}
			else
			{
				ast.InsertString(token);
			}

			return sm.Continue();
		}
	}
}