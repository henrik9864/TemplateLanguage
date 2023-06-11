﻿using System.Buffers;
using System.Reflection;
using System.Text;
using Tokhenizer;

namespace TemplateLanguage
{
	public ref struct ParsedTemplate
    {
		internal ref struct State
		{
            public ref Token token;
			public AbstractSyntaxTree ast;
		}

		static Dictionary<EngineState, IState> stateDict = new()
        {
			{ EngineState.String,   new StringState() },
			{ EngineState.Term,     new TermState() },
			{ EngineState.Factor,   new FactorState() },
			{ EngineState.Code,     new CodeState() },
		};

		ReadOnlySpan<char> txt;
        TokenEnumerable tokens;

        RefStack<EngineState> engineStates;
        State state;

		public void RenderTo(StringBuilder sb, IModel model)
        {
            var nodeArr = ArrayPool<Node>.Shared.Rent(4096);
            engineStates = new RefStack<EngineState>(16);
            engineStates.Push(EngineState.String);

			TokenEnumerable.Enumerator enumerator = tokens.GetEnumerator();
            state = new State()
            {
                token = ref enumerator.Current,
                ast = new AbstractSyntaxTree(nodeArr)
			};

			state.ast.InsertStart();

            stateDict[engineStates.Peek()].OnEnterAbove(ref this, ref state, engineStates.Peek());
			while (enumerator.MoveNext())
                stateDict[engineStates.Peek()].OnStep(ref this, ref state);
			stateDict[engineStates.Peek()].OnExitAbove(ref this, ref state, engineStates.Peek());

			state.ast.InsertEnd();

			state.ast.PrintTree(txt, false);

			var language = new TemplateLanguage()
			{
				txt = txt,
				nodes = state.ast.GetTree()
			};

			language.Compute(state.ast.GetRoot(), sb, model);
			ArrayPool<Node>.Shared.Return(nodeArr);
        }

		internal void Transition(EngineState newState)
		{
            /*
            EngineState prevState = engineState;
            stateDict[engineState].OnExit(ref this, ref state, newState);
            engineState = newState;
            stateDict[engineState].OnEnter(ref this, ref state, prevState);
            */

			stateDict[engineStates.Peek()].OnExitBelow(ref this, ref state, newState);
            engineStates.Push(newState);
			stateDict[newState].OnEnterAbove(ref this, ref state, engineStates.Peek(1));
		}

        internal void PopState()
        {
			EngineState prevState = engineStates.Pop();

			stateDict[prevState].OnExitAbove(ref this, ref state, engineStates.Peek());
			stateDict[engineStates.Peek()].OnEnterBelow(ref this, ref state, prevState);
		}

        public static ParsedTemplate Tokenize(in ReadOnlySpan<char> str)
        {
            TemplateRules tokenizer = new TemplateRules();

            return new ParsedTemplate()
            {
                txt = str,
                tokens = tokenizer.GetEnumerable(str)
            };
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