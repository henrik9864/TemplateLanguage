using System.Buffers;
using System.Text;
using Tokhenizer;

namespace TemplateLanguage
{
	ref struct RefStack<T>
	{
		public int Count
		{
			get
			{
				return currIdx;
			}
		}

		IMemoryOwner<T> buff;
		int currIdx;

        public RefStack(int capacity)
        {
			this.buff = MemoryPool<T>.Shared.Rent(capacity);
			this.currIdx = 0;
        }

		public void Push(in T item)
		{
            buff.Memory.Span[currIdx++] = item;
		}

		public T Pop()
		{
			return buff.Memory.Span[--currIdx];
		}

		public ref T Peek(int offset = 0)
		{
			return ref buff.Memory.Span[currIdx + offset - 1];
		}

		public bool TryPeek(int offset, out T val)
		{
			if (currIdx + offset - 1 < buff.Memory.Length)
			{
				val = default;
				return false;
			}

			val = Peek(offset);
			return true;
		}

		public void Dispose()
		{
			buff.Dispose();
		}
    }

	ref struct AbstractSyntaxTree
	{
		Span<Node> nodeTree;
		RefStack<int> root;

		int currIdx = 0;

		public AbstractSyntaxTree(Span<Node> nodeTree)
		{
            this.nodeTree = nodeTree;
			root = new RefStack<int>(64);
			root.Push(0);
		}

		public void InsertNumber(in Token token, NodeType type)
		{
			int rootIdx = root.Peek();
			if (rootIdx == currIdx)
				throw new Exception("Number cannot be a root.");

			Node.CreateNumber(ref nodeTree[currIdx], token, type);

			currIdx++;
		}

		// --------- STRING ---------

		public void InsertString(in Token token)
		{
			int rootIdx = root.Peek();
			ref Node rootNode = ref nodeTree[rootIdx];

			if (rootNode.right == currIdx)
				rootNode.right = -1;

			Node.CreateString(ref nodeTree[currIdx], token, rootNode.right, -1);
			rootNode.right = currIdx;

			currIdx++;
		}

		public void StartCodeBlock()
		{
			int rootIdx = root.Peek();
			ref Node rootNode = ref nodeTree[rootIdx];
			ref Node parentNode = ref nodeTree[rootNode.right];
			parentNode.left = currIdx;

			BracketOpen();
			//root.Push(currIdx);
		}

		// --------- CODE ---------

		public void InsertVariable()
		{
			int rootIdx = root.Peek();
			ref Node rootNode = ref nodeTree[rootIdx];
			rootNode.right = currIdx;

			Node.CreateVariable(ref nodeTree[currIdx], currIdx - 1);
			currIdx++;
		}

		public void InsertName(in Token token)
		{
			Node.CreateName(ref nodeTree[currIdx++], token);
		}

		public int InsertIf()
		{
			Node.CreateIf(ref nodeTree[currIdx], ++currIdx);

			return currIdx - 1;
		}

		public void SetRight(int ifIdx)
		{
			nodeTree[ifIdx].right = currIdx;
		}

		public void InsertEquals()
		{
			int rootIdx = root.Peek();
			ref Node rootNode = ref nodeTree[rootIdx];

			Node.CreateOperator(ref nodeTree[currIdx], NodeType.Equals, currIdx + 1, rootNode.right);
			rootNode.right = currIdx;

			currIdx++;
		}

		public void InsertBool(in Token token)
		{
			Node.CreateBool(ref nodeTree[currIdx++], token);
		}

		// --------- EXPRESSION ---------

		public void InsertOperator(NodeType type)
		{
			int rootIdx = root.Peek();
			ref Node rootNode = ref nodeTree[rootIdx];

			Node.CreateOperator(ref nodeTree[currIdx], type, currIdx + 1, rootNode.right);
			rootNode.right = currIdx;

			currIdx++;
		}

		public void BracketOpen()
		{
			Node.CreateBracket(ref nodeTree[currIdx], currIdx + 1, -1);

			root.Push(currIdx);
			currIdx++;
		}

		public void BracketClose()
		{
			if (root.Count == 1)
				throw new Exception("Root stack must never be empty.");

			root.Pop();
		}

		// ------------------

		public void InsertStart()
		{
			Node.CreateStart(ref nodeTree[currIdx], ++currIdx);
		}
		
		public void InsertEnd()
		{
			Node.CreateEnd(ref nodeTree[currIdx++]);
		}

		public Span<Node> GetTree()
		{
			return nodeTree.Slice(0, currIdx);
		}

		public int GetStackDepth()
		{
			return root.Count;
		}

		public int GetRoot()
		{
			return root.Peek();
		}
	}

	internal static class AbstractSyntaxTreeExtensions
	{
		public static void PrintTree(this ref AbstractSyntaxTree ast, in ReadOnlySpan<char> txt, bool simplified)
		{
			var tree = ast.GetTree();

			Console.WriteLine($"Stack Depth: {ast.GetStackDepth()}");
            Console.WriteLine();

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

            List<int> visited = new List<int>();
            PrintTree(txt, tree, ast.GetRoot(), 0, visited, simplified);
		}

		static void PrintTree(in ReadOnlySpan<char> txt, in ReadOnlySpan<Node> nodeTree, int node, int indent, List<int> visited, bool simplified)
		{
			if (node == -1)
				return;

			ref readonly Node nodeRef = ref nodeTree[node];
			string nodeInfo;

			// Simplify printout
			if (simplified && nodeRef.nodeType == NodeType.Bracket)
			{
				PrintTree(txt, nodeTree, nodeTree[node].right, indent, visited, simplified);
				return;
			}

			if (nodeRef.nodeType == NodeType.Integer)
			{
				nodeInfo = $"{nodeRef.nodeType} - I: {node} V: {nodeRef.token.GetSpan(txt)} L: {nodeRef.left} R: {nodeRef.right}";
			}
			else if (nodeRef.nodeType == NodeType.String)
			{
				nodeInfo = $"{nodeRef.nodeType} - I: {node} C: {nodeRef.token.GetSpan(txt).Length} L: {nodeRef.left} R: {nodeRef.right}";
			}
			else
			{
				nodeInfo = $"{nodeRef.nodeType} - I: {node} L: {nodeRef.left} R: {nodeRef.right}";
			}

			var line = $"{new string('│', int.Max(indent - 1, 0))}{(indent == 0 ? "" : "├")}{nodeInfo}";
			Console.WriteLine(line);

			if (visited.Contains(node))
				throw new Exception("Loop found");

			visited.Add(node);

			PrintTree(txt, nodeTree, nodeTree[node].right, indent + 1, visited, simplified);
			PrintTree(txt, nodeTree, nodeTree[node].left, indent + 1, visited, simplified);
		}

		static void SetColor(in Node node)
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
				case NodeType.String:
					Console.BackgroundColor = ConsoleColor.Blue;
					Console.ForegroundColor = ConsoleColor.Black;
					break;
				case NodeType.Name:
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
				default:
					break;
			}
		}

		static char GetChar(in Node node)
		{
			switch (node.nodeType)
			{
				case NodeType.Start:
					return 'B';
				case NodeType.End:
					return 'E';
				case NodeType.String:
					return 'S';
				case NodeType.Name:
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
				default:
					return ' ';
			}
		}
	}
}