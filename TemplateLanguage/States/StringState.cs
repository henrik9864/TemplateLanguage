using System.Text;
using Tokhenizer;

namespace TemplateLanguage
{
	internal class StringState : IState
	{
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