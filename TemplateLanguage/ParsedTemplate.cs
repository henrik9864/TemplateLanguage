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
        ExitAndRepeat,
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
			{ EngineState.String,     new StringState() },
			{ EngineState.Expression, new ExpressionState() },
			{ EngineState.Code,       new CodeState() },
		};

		ReadOnlySpan<char> txt;
        TokenEnumerable.Enumerator enumerator;

		public ParsedTemplate(ReadOnlySpan<char> txt, TokenEnumerable tokens)
		{
			this.txt = txt;
			this.enumerator = tokens.GetEnumerator();
		}

		public void RenderTo(StringBuilder sb, IModel model)
        {
			var nodeArr = ArrayPool<Node>.Shared.Rent(4096);
            var ast = new AbstractSyntaxTree(nodeArr);

			ast.InsertStart();
            CalculateAst(EngineState.String, ref ast);
			ast.InsertEnd();

			ComputeAst(sb, ref ast, model);

			ArrayPool<Node>.Shared.Return(nodeArr);
		}

		ExitCode CalculateAst(EngineState engineState, ref AbstractSyntaxTree ast)
        {
			ref Token token = ref enumerator.Current;
            while (true)
            {
                if (!enumerator.MoveNext())
                    return ExitCode.Exit;

                var code = stateDict[engineState].OnStep(ref this, ref ast, ref token);
				if (code != ExitCode.Continue)
					return ExitCode.Continue;
			}
		}

		void ComputeAst(StringBuilder sb, ref AbstractSyntaxTree ast, IModel model)
        {
			var language = new TemplateLanguage()
			{
				txt = txt,
				nodes = ast.GetTree()
			};

			ast.PrintTree(txt, false);
			language.Compute(ast.GetRoot(), sb, model);
		}

        internal ExitCode Transition(EngineState newState, ref AbstractSyntaxTree ast, bool repeatToken = false)
        {
            if (repeatToken)
            {
				ref Token token = ref enumerator.Current;
                var code = stateDict[newState].OnStep(ref this, ref ast, ref token);

				if (code != ExitCode.Continue)
					return ExitCode.Continue;
			}

            return CalculateAst(newState, ref ast);
		}

        internal ref Token Consume()
        {
            enumerator.MoveNext();
            return ref enumerator.Current;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ExitCode PopState(bool repeatToken)
        {
			return repeatToken ? ExitCode.ExitAndRepeat : ExitCode.Exit;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal ExitCode Continue()
		{
            return ExitCode.Continue;
		}
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