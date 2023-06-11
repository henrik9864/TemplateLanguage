using Tokhenizer;

namespace TemplateLanguage
{
	internal class CodeState : IState
	{
		public void OnEnterAbove(ref ParsedTemplate sm, ref ParsedTemplate.State state, EngineState prevState)
		{
		}

		public void OnEnterBelow(ref ParsedTemplate sm, ref ParsedTemplate.State state, EngineState prevState)
		{
			OnStep(ref sm, ref state);
		}

		public void OnExitAbove(ref ParsedTemplate sm, ref ParsedTemplate.State state, EngineState newState)
		{
		}

		public void OnExitBelow(ref ParsedTemplate sm, ref ParsedTemplate.State state, EngineState newState)
		{
		}

		public void OnStep(ref ParsedTemplate sm, ref ParsedTemplate.State state)
		{
			ref AbstractSyntaxTree ast = ref state.ast;
			ref readonly Token token = ref state.token;

			if (token.Get<TokenType>(0) == TokenType.Number)
			{
				sm.Transition(EngineState.Term);
			}
			else if (token.Get<TokenType>(0) == TokenType.Operator)
			{
				if (token.Get<OperatorType>(1) == OperatorType.Variable)
				{
					ast.InsertVariable();
				}
				else if (token.Get<OperatorType>(1) == OperatorType.If)
				{
					ast.InsertIf();
				}
				else if (token.Get<OperatorType>(1) == OperatorType.Comparer)
				{
					ast.InsertComparer();
				}
			}
			else if (token.Get<TokenType>(0) == TokenType.String)
			{
				ast.InsertName(token);
			}
			else if (token.Get<TokenType>(0) == TokenType.Bracket && token.Get<BracketType>(1) == BracketType.Code)
			{
				sm.PopState();
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

	internal class IfState : IState
	{
		public void OnEnterAbove(ref ParsedTemplate parsedTemplate, ref ParsedTemplate.State state, EngineState prevState)
		{
		}

		public void OnEnterBelow(ref ParsedTemplate parsedTemplate, ref ParsedTemplate.State state, EngineState prevState)
		{
		}

		public void OnExitAbove(ref ParsedTemplate parsedTemplate, ref ParsedTemplate.State state, EngineState newState)
		{
		}

		public void OnExitBelow(ref ParsedTemplate parsedTemplate, ref ParsedTemplate.State state, EngineState newState)
		{
		}

		public void OnStep(ref ParsedTemplate parsedTemplate, ref ParsedTemplate.State state)
		{
		}
	}
}