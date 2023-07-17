using System.Buffers;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Tokhenizer;

namespace TemplateLanguage
{
    enum ExitCode
    {
        Continue,
        Exit,
    }

	internal ref struct TemplateState
	{
		public ref Token token;
		public AbstractSyntaxTree ast;
	}

	public ref struct ParsedTemplate
    {
		static Dictionary<EngineState, IState> stateDict = new()
		{
			{ EngineState.TextState,  new TextState() },
			{ EngineState.Expression, new ExpressionState() },
			{ EngineState.Code,       new CodeState() },
			{ EngineState.Variable,   new VariableState() },
		};

		ReadOnlySpan<char> txt;
        TokenEnumerable.Enumerator enumerator;

		public ParsedTemplate(ReadOnlySpan<char> txt, TokenEnumerable tokens)
		{
			this.txt = txt;
			this.enumerator = tokens.GetEnumerator();
		}

		public void RenderTo(StringBuilder sb, ModelStack stack)
        {
			var nodeArr = ArrayPool<Node>.Shared.Rent(4096);
            var ast = new AbstractSyntaxTree(nodeArr);

            ast.InsertStart();
            CalculateAst(EngineState.TextState, ref ast);

			ComputeAst(sb, ref ast, stack);

			ArrayPool<Node>.Shared.Return(nodeArr);
		}

		void CalculateAst(EngineState engineState, ref AbstractSyntaxTree ast)
        {
			ref Token token = ref enumerator.Current;

			while (true)
            {
                if (!enumerator.MoveNext())
                    return;

                var code = stateDict[engineState].OnStep(ref this, ref ast, ref token);
				if (code == ExitCode.Exit)
					return;
			}
		}

		void ComputeAst(StringBuilder sb, ref AbstractSyntaxTree ast, ModelStack stack)
        {
            Span<Node> nodeTree = ast.GetTree();
			Span<ReturnType> returnTypes = stackalloc ReturnType[nodeTree.Length];

            //ast.PrintStackDepth();
            //ast.PrintNodes();

            TemplateLanguageTypeResolver.ResolveTypes(ast.GetRoot(), nodeTree, returnTypes);

			//ast.PrintTree(txt, returnTypes, false);

            var language = new TemplateLanguage(txt, nodeTree, returnTypes);

			var result = language.Compute(ast.GetRoot(), sb, stack);
            if (!result.Ok)
            {
                for (int i = 0; i < result.Errors.Count; i++)
                {
                    Console.WriteLine($"Error at line {result.Lines[i]}\n\t{result.Errors[i]}");
                }

                throw new Exception("Errors!");
            }
		}

        internal void Transition(EngineState newState, ref AbstractSyntaxTree ast, bool repeatToken = false)
        {
			ref Token token = ref enumerator.Current;

			if (repeatToken)
			{
				var code = stateDict[newState].OnStep(ref this, ref ast, ref token);
				if (code == ExitCode.Exit)
					return;
			}

			CalculateAst(newState, ref ast);
		}

        internal bool Consume()
        {
            return enumerator.MoveNext();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ExitCode PopState()
        {
			return ExitCode.Exit;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal ExitCode Continue()
		{
            return ExitCode.Continue;
		}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal bool IsEnd()
		 => enumerator.IsEnd();
	}

    public class TemplateDebugger
    {
        public static void Parse(ReadOnlyMemory<char> str)
        {
            TemplateRules tokenizer = new TemplateRules();
            var r = tokenizer.GetEnumerable(str.Span);

            PrintColors();
                
            StringBuilder sb = new StringBuilder();
            TokenEnumerable.Enumerator enumerator = r.GetEnumerator();
            while (enumerator.MoveNext())
            {
                sb.Append(enumerator.Current.GetSpan(str.Span));

                SetColor(enumerator.Current);
                
                Console.Write(sb);
                sb.Clear();
            }
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;
        }

        static void PrintToken(in Token token, ReadOnlySpan<char> str)
        {
            switch (token.Get<TokenType>(0))
            {
                case TokenType.Bracket:
                    Console.WriteLine($"{token.Get<TokenType>(0)}-{token.Get<BracketType>(1)}: {token.GetSpan(str)}");
                    break;
                case TokenType.Operator:
                    Console.WriteLine($"{token.Get<TokenType>(0)}-{token.Get<OperatorType>(1)}: {token.GetSpan(str)}");
                    break;
                case TokenType.Snippet:
                    Console.WriteLine($"{token.Get<TokenType>(0)}-{token.Get<int>(1)}: {token.GetSpan(str)}");
                    break;
                case TokenType.Number:
                    Console.WriteLine($"{token.Get<TokenType>(0)}-{token.Get<NumberType>(1)}: {token.GetSpan(str)}");
                    break;
                case TokenType.Whitespace:
                    if (token.GetSpan(str).Contains('\r'))
                        return;

                    Console.WriteLine($"{token.Get<TokenType>(0)}: '{token.GetSpan(str)}'");
                    break;
                case TokenType.String:
                case TokenType.LooseString:
                    Console.WriteLine($"{token.Get<TokenType>(0)}: '{token.GetSpan(str)}'");
                    break;
                default:
                    break;
            }
        }

        static void PrintColors()
        {
            Console.WriteLine("------------");
            PrintColor(ConsoleColor.DarkYellow, "Bracket");
            PrintColor(ConsoleColor.Yellow, "Operator");
            PrintColor(ConsoleColor.Red, "Snippet");
            PrintColor(ConsoleColor.Magenta, "Number");
            PrintColor(ConsoleColor.Black, "WhiteSpace");
            PrintColor(ConsoleColor.Blue, "String");
            PrintColor(ConsoleColor.Cyan, "Bool");
            Console.WriteLine("-------------");
            Console.WriteLine();
        }

        static void PrintColor(ConsoleColor color, string type)
        {
            Console.Write(" ");
            Console.BackgroundColor = color;
            Console.Write(" ");
            Console.BackgroundColor = ConsoleColor.Black;
            Console.WriteLine($" = {type}");
        }

        static void SetColor(in Token token)
        {
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;

            switch (token.Get<TokenType>(0))
            {
                case TokenType.Bracket:
                    Console.BackgroundColor = ConsoleColor.DarkYellow;
                    Console.ForegroundColor = ConsoleColor.Black;
                    break;
                case TokenType.Operator:
                    Console.BackgroundColor = ConsoleColor.Yellow;
                    Console.ForegroundColor = ConsoleColor.Black;
                    break;
                case TokenType.Snippet:
                    Console.BackgroundColor = ConsoleColor.Red;
                    break;
                case TokenType.Number:
                    Console.BackgroundColor = ConsoleColor.Magenta;
                    break;
                case TokenType.Whitespace:
                    Console.BackgroundColor = ConsoleColor.Black;
                    break;
                case TokenType.String:
                    Console.BackgroundColor = ConsoleColor.Blue;
                    Console.ForegroundColor = ConsoleColor.Black;
                    break;
                case TokenType.LooseString:
                    Console.BackgroundColor = ConsoleColor.DarkBlue;
                    break;
                case TokenType.Bool:
                    Console.BackgroundColor = ConsoleColor.Cyan;
					Console.ForegroundColor = ConsoleColor.Black;
					break;
                default:
                    break;
            }
        }
    }
}