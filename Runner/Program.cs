using BenchmarkDotNet.Running;
using Runner;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using LightLexer;
using LightParser;
using System.Buffers;

namespace Runner
{
	public static class Program
	{
		static Dictionary<EngineState, IState<NodeType, EngineState>> stateDict = new()
		{
			{ EngineState.TextState,  new TextState() },
			{ EngineState.Expression, new ExpressionState() },
			{ EngineState.Code,       new CodeState() },
			{ EngineState.Variable,   new VariableState() },
		};

		public static void Main()
		{
#if RELEASE
			BenchmarkRunner.Run<Perf>();
			return;
#endif
			IModel<ReturnType>[] models = new IModel<ReturnType>[10];
			for (int i = 0; i < models.Length; i++)
			{
				models[i] = new Model<ReturnType>();
				models[i].Set("i", Parameter.Create<float>(i));
			}

			var model2 = new Model<ReturnType>();
			model2.Set("shaba2", new Parameter<float>(24));

			var model = new Model<ReturnType>();
			model.Set("a", new Parameter<float>(2));
			model.Set("testVar", new Parameter<float>(6));
			model.Set("testVar2", new Parameter<float>(23));
			model.Set("testVar3", new Parameter<bool>(false));
			model.Set("result", new Parameter<string>("taper"));
			model.Set("vari", new ModelParameter(model2));
			model.Set("en", new EnumerableParameter<IModel<ReturnType>>(models));

			var stack = new ModelStack<ReturnType>();
			stack.Push(model);

			var str = File.ReadAllText("Templates/simple.tcs").AsMemory();

            Console.WriteLine(str);
            Console.WriteLine();

            TokenEnumerable tokens = new TemplateRules().GetEnumerable(str.Span);

			Parser<NodeType, EngineState> parser = new(stateDict, tokens);
			TypeResolver<NodeType, ReturnType> resolver = new(TypeResolver.ResolveType);

			var nodeArr = ArrayPool<Node<NodeType>>.Shared.Rent(4096);
			var typeArr = ArrayPool<ReturnType>.Shared.Rent(4096);

			var ast = parser.GetAst(nodeArr.AsSpan());
			int start = ast.InsertNode(NodeType.Start);
			ast.SetRight(start);

			parser.CalculateAst(ref ast, EngineState.TextState);

			//ast.PrintNodes();

			var types = resolver.ResolveTypes(ast.GetRoot(), ast.GetTree(), typeArr);
			ast.PrintTree(str.Span, typeArr, false);

			TemplateContext<NodeType, ReturnType> context = new()
			{
				txt = str.Span,
				nodes = ast.GetTree(),
				returnTypes = typeArr
			};

			StringBuilder sb = new();
			var result = TemplateLanguageRules.Compute(ref context, 0, sb, stack);
			foreach (var item in result.Errors)
			{
                Console.WriteLine(item);
            }
			/*
			*/

			Console.WriteLine(sb);

			ArrayPool<Node<NodeType>>.Shared.Return(nodeArr);
			ArrayPool<ReturnType>.Shared.Return(typeArr);
		}
	}

	internal static class AbstractSyntaxTreeExtensions
	{
		public static void PrintStackDepth(this ref AbstractSyntaxTree<NodeType> ast)
		{
			Console.WriteLine($"Stack Depth: {ast.GetStackDepth()}");
			Console.WriteLine();
		}

		public static void PrintNodes(this ref AbstractSyntaxTree<NodeType> ast)
		{
			var tree = ast.GetTree();

			Console.Write('|');
			for (int i = 0; i < tree.Length; i++)
			{
				SetColor(tree[i]);
				Console.Write($" {GetChar(tree[i])} ");
			}
			Console.BackgroundColor = ConsoleColor.Black;
			Console.ForegroundColor = ConsoleColor.White;
			Console.Write('|');
			Console.WriteLine();
			Console.Write('|');
			for (int i = 0; i < tree.Length; i++)
			{
				if (i % 2 == 0)
				{
					Console.BackgroundColor = ConsoleColor.Black;
					Console.ForegroundColor = ConsoleColor.White;
				}
				else
				{
					Console.BackgroundColor = ConsoleColor.White;
					Console.ForegroundColor = ConsoleColor.Black;
				}
				Console.Write($"{i}".PadRight(3));
			}
			Console.BackgroundColor = ConsoleColor.Black;
			Console.ForegroundColor = ConsoleColor.White;
			Console.Write('|');

			Console.WriteLine();
			Console.WriteLine();
		}

		public static void PrintTree(this ref AbstractSyntaxTree<NodeType> ast, in ReadOnlySpan<char> txt, scoped ReadOnlySpan<ReturnType> returnTypes, bool simplified)
		{
			var tree = ast.GetTree();

			List<int> visited = new List<int>();
			PrintTree(txt, tree, returnTypes, ast.GetRoot(), 0, visited, simplified);
		}

		static void PrintTree(in ReadOnlySpan<char> txt, in ReadOnlySpan<Node<NodeType>> nodeTree, in ReadOnlySpan<ReturnType> returnTypes, int node, int indent, List<int> visited, bool simplified)
		{
			if (node == -1)
				return;

			ref readonly Node<NodeType> nodeRef = ref nodeTree[node];
			string nodeInfo;

			// Simplify printout
			if (simplified && nodeRef.nodeType == NodeType.Bracket)
			{
				PrintTree(txt, nodeTree, returnTypes, nodeTree[node].right, indent, visited, simplified);
				return;
			}

			if (nodeRef.nodeType == NodeType.Integer)
			{
				nodeInfo = $"{nodeRef.nodeType} - I: {node} V: {nodeRef.token.GetSpan(txt).ToString()} L: {nodeRef.left} R: {nodeRef.right} T: {returnTypes[node]}";
			}
			else if (nodeRef.nodeType == NodeType.String || nodeRef.nodeType == NodeType.TextBlock)
			{
				nodeInfo = $"{nodeRef.nodeType} - I: {node} C: {nodeRef.token.GetSpan(txt).Length} L: {nodeRef.left} R: {nodeRef.right} T: {returnTypes[node]}";
			}
			else
			{
				nodeInfo = $"{nodeRef.nodeType} - I: {node} L: {nodeRef.left} R: {nodeRef.right} T: {returnTypes[node]}";
			}

			var line = $"{new string('│', Math.Max(indent - 1, 0))}{(indent == 0 ? "" : "├")}{nodeInfo}";
			Console.WriteLine(line);

			if (visited.Contains(node))
				throw new Exception("Loop found");

			visited.Add(node);

			PrintTree(txt, nodeTree, returnTypes, nodeTree[node].right, indent + 1, visited, simplified);
			PrintTree(txt, nodeTree, returnTypes, nodeTree[node].middle, indent + 1, visited, simplified);
			PrintTree(txt, nodeTree, returnTypes, nodeTree[node].left, indent + 1, visited, simplified);
		}

		static void SetColor(in Node<NodeType> node)
		{
			Console.BackgroundColor = ConsoleColor.Black;
			Console.ForegroundColor = ConsoleColor.White;

			switch (node.nodeType)
			{
				case NodeType.Start:
					Console.BackgroundColor = ConsoleColor.Green;
					Console.ForegroundColor = ConsoleColor.Black;
					break;
				case NodeType.End:
					Console.BackgroundColor = ConsoleColor.DarkGreen;
					Console.ForegroundColor = ConsoleColor.Black;
					break;
				case NodeType.TextBlock:
					Console.BackgroundColor = ConsoleColor.Blue;
					Console.ForegroundColor = ConsoleColor.Black;
					break;
				case NodeType.String:
					Console.BackgroundColor = ConsoleColor.DarkBlue;
					Console.ForegroundColor = ConsoleColor.Black;
					break;
				case NodeType.Integer:
					Console.BackgroundColor = ConsoleColor.Magenta;
					break;
				case NodeType.Float:
					Console.BackgroundColor = ConsoleColor.Magenta;
					break;
				case NodeType.Add:
					Console.BackgroundColor = ConsoleColor.Yellow;
					Console.ForegroundColor = ConsoleColor.Black;
					break;
				case NodeType.Subtract:
					Console.BackgroundColor = ConsoleColor.Yellow;
					Console.ForegroundColor = ConsoleColor.Black;
					break;
				case NodeType.Multiply:
					Console.BackgroundColor = ConsoleColor.Yellow;
					Console.ForegroundColor = ConsoleColor.Black;
					break;
				case NodeType.Divide:
					Console.BackgroundColor = ConsoleColor.Yellow;
					Console.ForegroundColor = ConsoleColor.Black;
					break;
				case NodeType.Bracket:
					Console.BackgroundColor = ConsoleColor.Red;
					break;
				case NodeType.Variable:
					Console.BackgroundColor = ConsoleColor.Yellow;
					Console.ForegroundColor = ConsoleColor.Black;
					break;
				case NodeType.CodeBlock:
					Console.BackgroundColor = ConsoleColor.Green;
					Console.ForegroundColor = ConsoleColor.Black;
					break;
				default:
					break;
			}
		}

		static char GetChar(in Node<NodeType> node)
		{
			switch (node.nodeType)
			{
				case NodeType.Start:
					return 'B';
				case NodeType.End:
					return 'E';
				case NodeType.TextBlock:
					return 'S';
				case NodeType.String:
					return 'N';
				case NodeType.Integer:
					return 'I';
				case NodeType.Float:
					return 'F';
				case NodeType.Add:
					return 'A';
				case NodeType.Subtract:
					return 'S';
				case NodeType.Multiply:
					return 'M';
				case NodeType.Divide:
					return 'D';
				case NodeType.Bracket:
					return 'B';
				case NodeType.Variable:
					return '$';
				case NodeType.CodeBlock:
					return 'C';
				default:
					return ' ';
			}
		}
	}

	/*
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
                sb.Append(enumerator.Current.GetSpan(str.Span).ToString());

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
                    Console.WriteLine($"{token.Get<TokenType>(0)}-{token.Get<BracketType>(1)}: {token.GetSpan(str).ToString()}");
                    break;
                case TokenType.Operator:
                    Console.WriteLine($"{token.Get<TokenType>(0)}-{token.Get<OperatorType>(1)}: {token.GetSpan(str).ToString()}");
                    break;
                case TokenType.Snippet:
                    Console.WriteLine($"{token.Get<TokenType>(0)}-{token.Get<int>(1)}: {token.GetSpan(str).ToString()}");
                    break;
                case TokenType.Number:
                    Console.WriteLine($"{token.Get<TokenType>(0)}-{token.Get<NumberType>(1)}: {token.GetSpan(str).ToString()}");
                    break;
                case TokenType.Whitespace:
                    if (token.GetSpan(str).Contains('\r'))
                        return;

                    Console.WriteLine($"{token.Get<TokenType>(0)}: '{token.GetSpan(str).ToString()}'");
                    break;
                case TokenType.String:
                case TokenType.LooseString:
                    Console.WriteLine($"{token.Get<TokenType>(0)}: '{token.GetSpan(str).ToString()}'");
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
    */
}