using System;
using System.Runtime.InteropServices;
using Tokhenizer;

namespace TemplateLanguage
{
	internal class CodeState : IState
	{
		public void OnEnter(ref ParsedTemplate sm, ref ParsedTemplate.State state)
		{
		}

		public void OnExit(ref ParsedTemplate sm, ref ParsedTemplate.State state)
		{
		}

		public void OnStep(ref ParsedTemplate sm, ref ParsedTemplate.State state)
		{
			ref readonly Token token = ref state.token;
			ref AbstractSyntaxTree ast = ref state.ast;

			if (token.Get<TokenType>(0) == TokenType.Bracket && token.Get<BracketType>(1) == BracketType.Code)
			{
				ast.StopAllFactors();
				sm.Transition(EngineState.String);
			}
			else if (token.Get<TokenType>(0) == TokenType.Number)
			{
				if (token.Get<NumberType>(1) == NumberType.Integer)
				{
					ast.InsertRight(token, NodeType.Integer);
				}
				else if (token.Get<NumberType>(1) == NumberType.Float)
				{
					ast.InsertRight(token, NodeType.Float);
				}
			}
			else if (token.Get<TokenType>(0) == TokenType.Operator && token.Get<OperatorType>(1) == OperatorType.Add)
			{
				ast.StopFactor();
				ast.InsertOperator(token, NodeType.Add);
			}
			else if (token.Get<TokenType>(0) == TokenType.Operator && token.Get<OperatorType>(1) == OperatorType.Subtract)
			{
				ast.StopFactor();
				ast.InsertOperator(token, NodeType.Subtract);
			}
			else if (token.Get<TokenType>(0) == TokenType.Operator && token.Get<OperatorType>(1) == OperatorType.Multiply)
			{
				ast.StartFactor();
				ast.InsertOperator(token, NodeType.Multiply);
			}
			else if (token.Get<TokenType>(0) == TokenType.Operator && token.Get<OperatorType>(1) == OperatorType.Divide)
			{
				ast.StartFactor();
				ast.InsertOperator(token, NodeType.Divide);
			}
			else if (token.Get<TokenType>(0) == TokenType.Bracket && token.Get<BracketType>(1) == BracketType.Open)
			{
				ast.BracketOpen(false);
			}
			else if (token.Get<TokenType>(0) == TokenType.Bracket && token.Get<BracketType>(1) == BracketType.Close)
			{
				ast.StopFactor();
				ast.BracketClose();
			}
		}
	}
}