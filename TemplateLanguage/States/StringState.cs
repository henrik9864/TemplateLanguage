using System.Text;
using Tokhenizer;

namespace TemplateLanguage
{
	internal class StringState : IState
	{
		public ExitCode OnStep(ref ParsedTemplate sm, ref AbstractSyntaxTree ast, ref Token token)
		{
			if (token.Is(TokenType.Bracket, BracketType.Code))
			{
				ast.StartCodeBlock();
				ast.BracketOpen();
				sm.Transition(EngineState.Code, ref ast);
				ast.BracketClose();
			}
			else if (token.Is(TokenType.Operator, OperatorType.Variable))
			{
				ast.StartCodeBlock();
				ast.BracketOpen();
				sm.Transition(EngineState.Variable, ref ast, repeatToken: true);
				ast.BracketClose();

				if (sm.IsEnd())
					return sm.Continue();

				return OnStep(ref sm, ref ast, ref token);
			}
			else
			{
				ast.InsertString(token);
			}

			return sm.Continue();
		}
	}

	internal class VariableState : IState
	{
		public ExitCode OnStep(ref ParsedTemplate sm, ref AbstractSyntaxTree ast, ref Token token)
		{
			if (token.Is(TokenType.Operator, OperatorType.Variable))
			{
				if (!sm.Consume())
					return ExitCode.Exit;

				ast.InsertName(token);
				ast.InsertVariable();

				return sm.Continue();
			}
			else if (token.Is(TokenType.Operator, OperatorType.Accessor))
			{
				if (!sm.Consume())
					return ExitCode.Exit;

				ast.InsertOperator(NodeType.Accessor);
				ast.InsertName(token);

				return sm.Continue();
			}

			return sm.PopState();
		}
	}
}