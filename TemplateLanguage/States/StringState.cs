using System.Text;
using Tokhenizer;

namespace TemplateLanguage
{
	internal class StringState : IState
	{
		/*
		public void OnEnterAbove(ref ParsedTemplate sm, ref TemplateState state, EngineState prevState)
		{
		}

		public void OnEnterBelow(ref ParsedTemplate sm, ref TemplateState state, EngineState prevState)
		{
			ref AbstractSyntaxTree ast = ref state.ast;

            if (prevState == EngineState.Code)
				ast.BracketClose();
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
			if (token.Get<TokenType>(0) == TokenType.Bracket && token.Get<BracketType>(1) == BracketType.Code)
			{
                ast.StartCodeBlock();
				var result = sm.Transition(EngineState.Code, ref ast);
				ast.BracketClose();

				return result;
			}
			else
			{
				ast.InsertString(token);
			}

			return sm.Continue();
		}
	}
}