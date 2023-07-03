using Tokhenizer;

namespace TemplateLanguage
{
	internal class CodeState : IState
	{
		public ExitCode OnStep(ref ParsedTemplate sm, ref AbstractSyntaxTree ast, ref Token token)
		{
            if (
				token.Is(TokenType.Bracket, BracketType.Code) ||
				token.Is(TokenType.Bracket, BracketType.Operator) ||
				token.Is(TokenType.Operator, OperatorType.Else) ||
				token.Is(TokenType.Operator, OperatorType.End))
			{
				ast.InsertEnd();
				return sm.PopState();
			}

			if (token.Is(TokenType.Whitespace))
				return sm.Continue();

			if (token.Is(TokenType.Number) || token.Is(TokenType.Operator, OperatorType.Variable) || token.Is(TokenType.Bracket, BracketType.Open))
			{
				ast.BracketOpen();
				sm.Transition(EngineState.Expression, ref ast, repeatToken: true);
				ast.BracketClose();

				return OnStep(ref sm, ref ast, ref token);
			}
			else if (token.Is(TokenType.Operator))
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
					case OperatorType.If:
						var ifIdx = ast.InsertIf();

						ast.BracketOpen();
						sm.Transition(EngineState.Code, ref ast, repeatToken: false);
						ast.BracketClose();

						ast.SetMiddle(ifIdx);

						ast.BracketOpen();
						sm.Transition(EngineState.Code, ref ast, repeatToken: false);
						ast.BracketClose();

						if (token.Is(TokenType.Operator, OperatorType.Else))
						{
                            ast.SetRight(ifIdx);

							ast.BracketOpen();
							sm.Transition(EngineState.Code, ref ast, repeatToken: false);
							ast.BracketClose();
						}

						if (token.Is(TokenType.Operator, OperatorType.End))
						{
							return sm.Continue();
						}

						return OnStep(ref sm, ref ast, ref token);
					default:
						break;
				}
			}
			else if (token.Is(TokenType.NewLine))
			{
                ast.InsertOperator(NodeType.CodeBlock);

				ast.BracketOpen();
				sm.Transition(EngineState.Code, ref ast, repeatToken: false);
				ast.BracketClose();

				return OnStep(ref sm, ref ast, ref token);
			}
			else if (token.Is(TokenType.Bool))
			{
				ast.InsertBool(token);
			}
			else if (token.Is(TokenType.String))
			{
				ast.InsertName(token);
			}

			return sm.Continue();
		}
	}
}