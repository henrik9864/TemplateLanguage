﻿using System;
using System.Runtime.InteropServices;
using Tokhenizer;

namespace TemplateLanguage
{

	internal class TermState : IState
	{
		/*
		public void OnEnterAbove(ref ParsedTemplate sm, ref TemplateState state, EngineState prevState)
		{
			ref AbstractSyntaxTree ast = ref state.ast;
			ast.BracketOpen();

			OnStep(ref sm, ref state);
		}

		public void OnEnterBelow(ref ParsedTemplate sm, ref TemplateState state, EngineState prevState)
		{
			OnStep(ref sm, ref state);
		}

		public void OnExitAbove(ref ParsedTemplate sm, ref TemplateState state, EngineState newState)
		{
            ref AbstractSyntaxTree ast = ref state.ast;
			ast.BracketClose();
		}

		public void OnExitBelow(ref ParsedTemplate sm, ref TemplateState state, EngineState newState)
		{
		}
		*/

		public ExitCode OnStep(ref ParsedTemplate sm, ref AbstractSyntaxTree ast, ref Token token)
		{
			if (token.Get<TokenType>(0) == TokenType.Bracket && token.Get<BracketType>(1) == BracketType.Code)
			{
				return sm.PopState(true);
			}
			else if (token.Get<TokenType>(0) == TokenType.Number)
			{
				if (token.Get<NumberType>(1) == NumberType.Integer)
				{
					ast.InsertNumber(token, NodeType.Integer);
				}
				else if (token.Get<NumberType>(1) == NumberType.Float)
				{
					ast.InsertNumber(token, NodeType.Float);
				}
			}
			else if (token.Get<TokenType>(0) == TokenType.Operator && token.Get<OperatorType>(1) == OperatorType.Add)
			{
				ast.InsertOperator(NodeType.Add);
			}
			else if (token.Get<TokenType>(0) == TokenType.Operator && token.Get<OperatorType>(1) == OperatorType.Subtract)
			{
				ast.InsertOperator(NodeType.Subtract);
			}
			else if (token.Get<TokenType>(0) == TokenType.Operator && (token.Get<OperatorType>(1) == OperatorType.Multiply || token.Get<OperatorType>(1) == OperatorType.Divide))
			{
				ast.BracketOpenBetween();
				sm.Transition(EngineState.Factor, ref ast, repeatToken: true);
				ast.BracketClose();

				return OnStep(ref sm, ref ast, ref token);
			}
			else if (token.Get<TokenType>(0) == TokenType.Bracket && token.Get<BracketType>(1) == BracketType.Open)
			{
				ast.BracketOpen();
			}
			else if (token.Get<TokenType>(0) == TokenType.Bracket && token.Get<BracketType>(1) == BracketType.Close)
			{
				ast.BracketClose();
			}
			else if (token.Get<TokenType>(0) == TokenType.Whitespace)
			{
				return sm.Continue();
			}
			else
			{
                return sm.PopState(true);
			}

			return sm.Continue();
		}
	}
}