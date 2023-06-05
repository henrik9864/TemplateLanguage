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
				//ast.StartCodeBlock();
				sm.Transition(EngineState.Term);
			}
			else if (token.Get<TokenType>(0) == TokenType.Operator && token.Get<OperatorType>(1) == OperatorType.Variable)
			{
                Console.WriteLine("Yay");
                ast.InsertVariable();
			}
			else if (token.Get<TokenType>(0) == TokenType.Bracket && token.Get<BracketType>(1) == BracketType.Code)
			{
				sm.PopState();
			}
		}
	}
}