using Tokhenizer;

namespace TemplateLanguage
{
	internal class CodeState : IState
	{
		public ExitCode OnStep(ref ParsedTemplate sm, ref AbstractSyntaxTree ast, ref Token token)
		{
            if (
				token.Is(TokenType.Bracket, BracketType.Code) ||
				token.Is(TokenType.Operator, OperatorType.Then) ||
				token.Is(TokenType.Operator, OperatorType.Else) ||
				token.Is(TokenType.Operator, OperatorType.Elseif) ||
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
			else if (token.Is(TokenType.Bracket, BracketType.String))
			{
				Range strRange = new Range(token.range.Start.Value + 1, token.range.End.Value + 1);
				sm.Consume();

				while (!token.Is(TokenType.Bracket, BracketType.String))
				{
					strRange = new Range(strRange.Start, token.range.End);
					sm.Consume();
				}

				ast.InsertString(new Token(strRange));
				return sm.Continue();
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
					case OperatorType.And:
						ast.InsertOperator(NodeType.And);
						break;
					case OperatorType.Or:
						ast.InsertOperator(NodeType.Or);
						break;
					case OperatorType.Greater:
						ast.InsertOperator(NodeType.Greater);
						break;
					case OperatorType.Less:
						ast.InsertOperator(NodeType.Less);
						break;
					case OperatorType.Conditional:
						ast.InsertOperator(NodeType.Conditional);
						break;
					case OperatorType.If:
						int ifIdx = 0;

						do
						{
							// If token is elseif it means there is a previous if statement
							if (token.Is(TokenType.Operator, OperatorType.Elseif))
								ast.SetRight(ifIdx);

							ifIdx = ast.InsertIf();

							ast.BracketOpen();
							sm.Transition(EngineState.Code, ref ast, repeatToken: false);
							ast.BracketClose();

							if (!token.Is(TokenType.Operator, OperatorType.Then))
								throw new Exception("If condition must be closed by then");

							ast.SetMiddle(ifIdx);

							ast.BracketOpen();
							sm.Transition(EngineState.Code, ref ast, repeatToken: false);
							ast.BracketClose();
						}
						while (token.Is(TokenType.Operator, OperatorType.Elseif));

						if (token.Is(TokenType.Operator, OperatorType.Else))
						{
                            ast.SetRight(ifIdx);

							ast.BracketOpen();
							sm.Transition(EngineState.Code, ref ast, repeatToken: false);
							ast.BracketClose();
						}

						if (!token.Is(TokenType.Operator, OperatorType.End))
							throw new Exception("If statement must be closed with end.");

						return sm.Continue();
					default:
						break;
				}
			}
			else if (token.Is(TokenType.NewLine))
			{
                ast.InsertOperator(NodeType.NewLine);

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
				ast.InsertString(token);
			}

			return sm.Continue();
		}
	}
}