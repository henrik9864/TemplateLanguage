using System;
using System.Runtime.InteropServices;
using Tokhenizer;

namespace TemplateLanguage
{
	internal class TermState : IState
	{
		public void OnEnter(ref ParsedTemplate sm, ref ParsedTemplate.State state)
		{
			ref readonly Token token = ref state.token;
			ref AbstractSyntaxTree ast = ref state.ast;
			ast.BracketOpen();

			// TODO: Not the best but find a way to prefor step when tranitioning from term but not from string.
			if (token.Get<TokenType>(0) != TokenType.Bracket || token.Get<BracketType>(1) != BracketType.Code)
				OnStep(ref sm, ref state);
		}

		public void OnExit(ref ParsedTemplate sm, ref ParsedTemplate.State state)
		{
			ref AbstractSyntaxTree ast = ref state.ast;
			ast.BracketClose();
		}

		public void OnStep(ref ParsedTemplate sm, ref ParsedTemplate.State state)
		{
			ref readonly Token token = ref state.token;
			ref AbstractSyntaxTree ast = ref state.ast;

			if (token.Get<TokenType>(0) == TokenType.Bracket && token.Get<BracketType>(1) == BracketType.Code)
			{
				//ast.StopAllFactors();
				sm.Transition(EngineState.String);
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