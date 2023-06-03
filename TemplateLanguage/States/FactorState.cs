using Tokhenizer;

namespace TemplateLanguage
{
	internal class FactorState : IState
	{
		public void OnEnter(ref ParsedTemplate sm, ref ParsedTemplate.State state)
		{
			ref AbstractSyntaxTree ast = ref state.ast;
			ast.BracketOpenBetween();

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
			if (token.Get<TokenType>(0) == TokenType.Operator && token.Get<OperatorType>(1) == OperatorType.Multiply)
			{
				ast.InsertOperator(NodeType.Multiply);
			}
			else if (token.Get<TokenType>(0) == TokenType.Operator && token.Get<OperatorType>(1) == OperatorType.Divide)
			{
				ast.InsertOperator(NodeType.Divide);
			}
			else if (token.Get<TokenType>(0) == TokenType.Operator)
			{
				ref OperatorType type = ref token.Get<OperatorType>(1);
				if (type == OperatorType.Add || type == OperatorType.Subtract)
					sm.Transition(EngineState.Term);

			}
		}
	}
}