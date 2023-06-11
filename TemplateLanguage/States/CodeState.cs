using Tokhenizer;

namespace TemplateLanguage
{
	internal class CodeState : IState
	{
		/*
		public void OnEnterAbove(ref ParsedTemplate sm, ref TemplateState state, EngineState prevState)
		{
		}

		public void OnEnterBelow(ref ParsedTemplate sm, ref TemplateState state, EngineState prevState)
		{
			OnStep(ref sm, ref state);
		}

		public void OnExitAbove(ref ParsedTemplate sm, ref TemplateState state, EngineState newState)
		{
		}

		public void OnExitBelow(ref ParsedTemplate sm, ref TemplateState state, EngineState newState)
		{
		}
		*/

		public ExitCode OnStep(ref ParsedTemplate sm, ref AbstractSyntaxTree ast, ref Token token)
		{
			if (token.Get<TokenType>(0) == TokenType.Number)
			{
				ast.BracketOpen();
				sm.Transition(EngineState.Term, ref ast, repeatToken: true);
				ast.BracketClose();

                return OnStep(ref sm, ref ast, ref token);
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
				return sm.PopState(false);
			}
			else if (token.Get<TokenType>(0) == TokenType.Bracket && token.Get<BracketType>(1) == BracketType.Open)
			{
				ast.BracketOpen();
			}
			else if (token.Get<TokenType>(0) == TokenType.Bracket && token.Get<BracketType>(1) == BracketType.Close)
			{
				ast.BracketClose();
			}

			return sm.Continue();
		}
	}
}