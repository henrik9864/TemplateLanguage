using Tokhenizer;

namespace TemplateLanguage
{
	internal class CodeState : IState
	{
		public ExitCode OnStep(ref ParsedTemplate sm, ref AbstractSyntaxTree ast, ref Token token)
		{
            if (token.Is(TokenType.Bracket, BracketType.Code) || token.Is(TokenType.Bracket, BracketType.Operator))
				return sm.PopState();

			if (token.Is(TokenType.Whitespace) || token.Is(TokenType.NewLine))
				return sm.Continue();

			if (token.Is(TokenType.Number) || token.Is(TokenType.Operator, OperatorType.Variable))
			{
				ast.BracketOpen();
				sm.Transition(EngineState.Expression, ref ast, repeatToken: true);
				ast.BracketClose();
			}

			if (token.Is(TokenType.Operator))
			{
				OperatorType type = token.Get<OperatorType>(1);
				switch (type)
				{
					case OperatorType.Asssign:
						ast.InsertOperator(NodeType.Assign);
						break;
					case OperatorType.Equals:
						ast.InsertOperator(NodeType.Equals);
						break;
					default:
						break;
				}

				return sm.Continue();
			}

			ast.AddStartPoint();
			return sm.Continue();
			/*
            if (token.Is(TokenType.Number) || token.Is(TokenType.Operator, OperatorType.Variable))
			{
				ast.BracketOpen();
				sm.Transition(EngineState.Expression, ref ast, repeatToken: true);
				ast.BracketClose();

				ast.AddRoot();

                return OnStep(ref sm, ref ast, ref token);
			}
			else if (token.Is(TokenType.Operator))
			{
                if (token.Get<OperatorType>(1) == OperatorType.If)
				{
					var ifIdx = ast.InsertIf();
					var compareIdx = ast.InsertCompare();

					ast.BracketOpen();
					sm.Transition(EngineState.Code, ref ast, repeatToken: false);
					ast.BracketClose();

					ast.SetRight(compareIdx);

					ast.BracketOpen();
					sm.Transition(EngineState.Code, ref ast, repeatToken: false);
					ast.BracketClose();

					if (token.Is(TokenType.Operator, OperatorType.Else))
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
				else if (token.Get<OperatorType>(1) == OperatorType.Else)
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
					case BracketType.Code:
					case BracketType.Operator:
						return sm.PopState(false);
					case BracketType.Close:
						return sm.Continue();
					default:
						return sm.PopState(true);
				}
			}
			else if (token.Is(TokenType.Bool))
			{
				ast.InsertBool(token);
			}

			return sm.Continue();
			*/
		}
	}
}