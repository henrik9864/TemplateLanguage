using System.Text;
using Tokhenizer;
using static System.Net.Mime.MediaTypeNames;

namespace TemplateLanguage
{
	internal class StringState : IState
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

			if (token.Get<TokenType>(0) == TokenType.Operator && token.Get<OperatorType>(1) == OperatorType.Variable)
			{
				//state.enumerator.MoveNext();
				//state.sb.Append(state.model[state.enumerator.Current.GetSpan(state.txt)]);
			}
			else if (token.Get<TokenType>(0) == TokenType.Bracket && token.Get<BracketType>(1) == BracketType.Code)
			{
				sm.Transition(EngineState.Code);
			}
			else
			{
				state.ast.InsertAbove(token);
			}
		}
	}
}