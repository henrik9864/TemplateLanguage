using Tokhenizer;

namespace TemplateLanguage
{
	internal class CodeState : IState
	{
		public ExitCode OnStep(ref ParsedTemplate sm, ref AbstractSyntaxTree ast, ref Token token)
		{
            if (token.Is(TokenType.Number) || token.Is(TokenType.Operator, OperatorType.Variable))
			{
				ast.BracketOpen();
				sm.Transition(EngineState.Expression, ref ast, repeatToken: true);
				ast.BracketClose();

                return OnStep(ref sm, ref ast, ref token);
			}
			else if (token.Is(TokenType.Operator))
			{
                if (token.Get<OperatorType>(1) == OperatorType.If)
				{
					var ifIdx = ast.InsertIf();

					ast.BracketOpen();
					sm.Transition(EngineState.Code, ref ast, repeatToken: false);
					ast.BracketClose();

					ast.SetRight(ifIdx);
				}
				else if (token.Get<OperatorType>(1) == OperatorType.Equals)
				{
					ast.InsertOperator(NodeType.Equals);
				}
				else if (token.Get<OperatorType>(1) == OperatorType.Asssign)
				{
					ast.InsertOperator(NodeType.Assign);
				}
			}
			else if (token.Is(TokenType.Bracket))
			{
				if (token.Get<BracketType>(1) == BracketType.Code)
				{
					return sm.PopState(false);
				}
				else if (token.Get<BracketType>(1) == BracketType.Operator)
				{
					return sm.PopState(false);
				}
			}
			else if (token.Is(TokenType.Bracket))
			{
				switch (token.Get<BracketType>(1))
				{
					case BracketType.Open:
						ast.BracketOpen();
						sm.Transition(EngineState.Code, ref ast, repeatToken: false);
						ast.BracketClose();

						return OnStep(ref sm, ref ast, ref token);
					default:
						return sm.PopState(true);
				}
			}
			else if (token.Is(TokenType.Bool))
			{
				ast.InsertBool(token);
			}

			return sm.Continue();
		}
	}
}