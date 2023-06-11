using System.Text;
using Tokhenizer;
using static System.Net.Mime.MediaTypeNames;

namespace TemplateLanguage
{
	internal class StringState : IState
	{
		public void OnEnterAbove(ref ParsedTemplate sm, ref ParsedTemplate.State state, EngineState prevState)
		{
		}

		public void OnEnterBelow(ref ParsedTemplate sm, ref ParsedTemplate.State state, EngineState prevState)
		{
			ref AbstractSyntaxTree ast = ref state.ast;

            if (prevState == EngineState.Code)
				ast.BracketClose();
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

			if (token.Get<TokenType>(0) == TokenType.Bracket && token.Get<BracketType>(1) == BracketType.Code)
			{
				ast.StartCodeBlock();
				sm.Transition(EngineState.Code);
			}
			else
			{
				ast.InsertString(token);
			}
		}
	}
}