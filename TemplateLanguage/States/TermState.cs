using System;
using System.Runtime.InteropServices;
using Tokhenizer;

namespace TemplateLanguage
{

	internal class TermState : IState
	{
		public void OnEnterAbove(ref ParsedTemplate sm, ref ParsedTemplate.State state, EngineState prevState)
		{
			ref AbstractSyntaxTree ast = ref state.ast;
			ast.BracketOpen();

			OnStep(ref sm, ref state);
		}

		public void OnEnterBelow(ref ParsedTemplate sm, ref ParsedTemplate.State state, EngineState prevState)
		{
			OnStep(ref sm, ref state);
		}

		public void OnExitAbove(ref ParsedTemplate sm, ref ParsedTemplate.State state, EngineState newState)
		{
            ref AbstractSyntaxTree ast = ref state.ast;
			ast.BracketClose();
		}

		public void OnExitBelow(ref ParsedTemplate sm, ref ParsedTemplate.State state, EngineState newState)
		{
		}

		public void OnStep(ref ParsedTemplate sm, ref ParsedTemplate.State state)
		{
			ref readonly Token token = ref state.token;
			ref AbstractSyntaxTree ast = ref state.ast;

			if (token.Get<TokenType>(0) == TokenType.Bracket && token.Get<BracketType>(1) == BracketType.Code)
			{
				sm.PopState();
				//sm.Transition(EngineState.Code);
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
			else if (token.Get<TokenType>(0) == TokenType.Operator)
			{
				ref OperatorType type = ref token.Get<OperatorType>(1);
				if (type == OperatorType.Multiply || type == OperatorType.Divide)
					sm.Transition(EngineState.Factor);

			}
			else if (token.Get<TokenType>(0) == TokenType.Bracket && token.Get<BracketType>(1) == BracketType.Open)
			{
				ast.BracketOpen();
			}
			else if (token.Get<TokenType>(0) == TokenType.Bracket && token.Get<BracketType>(1) == BracketType.Close)
			{
				ast.BracketClose();
			}
		}
	}
}