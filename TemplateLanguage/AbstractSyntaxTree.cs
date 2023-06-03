using System.Buffers;
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

			Node.CreateNumber(ref nodeTree[currIdx], token, type, -1);

			currIdx++;
		}

		public void InsertString(in Token token)
		{
			int rootIdx = root.Peek();
			ref Node rootNode = ref nodeTree[rootIdx];

			if (rootNode.right == currIdx)
				rootNode.right = -1;

			Node.CreateString(ref nodeTree[currIdx], token, rootNode.right, -1, -1);
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
		}

		public void InsertOperator(NodeType type)
		{
			int rootIdx = root.Peek();
			ref Node rootNode = ref nodeTree[rootIdx];

			Node.CreateOperator(ref nodeTree[currIdx], type, currIdx + 1, rootNode.right, -1);
			rootNode.right = currIdx;

			currIdx++;
		}

		public void BracketOpen()
		{
			Node.CreateBracket(ref nodeTree[currIdx], currIdx + 1, -1);

			root.Push(currIdx);
			currIdx++;
		}

		public void BracketOpenBetween()
		{
			nodeTree[currIdx] = nodeTree[currIdx - 1];
			Node.CreateBracket(ref nodeTree[currIdx - 1], currIdx, -1);

			root.Push(currIdx - 1);
			currIdx++;
		}

		public void InsertStart()
		{
			Node.CreateStart(ref nodeTree[currIdx], ++currIdx);
		}
		
		public void InsertEnd()
		{
			Node.CreateEnd(ref nodeTree[currIdx++]);
		}

		public void BracketClose()
		{
			root.Pop();
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

		void SetRoot(int root)
		{
			this.root.Pop();
			this.root.Push(root);
		}
	}

	internal static class AbstractSyntaxTreeExtensions
	{
		public static void PrintTree(this ref AbstractSyntaxTree ast, in ReadOnlySpan<char> txt, bool simplified)
		{
			List<int> visited = new List<int>();
            Console.WriteLine($"Stack Depth: {ast.GetStackDepth()}");
            PrintTree(txt, ast.GetTree(), ast.GetRoot(), 0, visited, simplified);
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
				nodeInfo = $"{nodeRef.nodeType} - V: {nodeRef.token.GetSpan(txt)} L: {nodeRef.left} R: {nodeRef.right}";
			}
			else if (nodeRef.nodeType == NodeType.String)
			{
				nodeInfo = $"{nodeRef.nodeType} - C: {nodeRef.token.GetSpan(txt).Length} L: {nodeRef.left} R: {nodeRef.right}";
			}
			else
			{
				nodeInfo = $"{nodeRef.nodeType} - L: {nodeRef.left} R: {nodeRef.right}";
			}

			var line = $"{new string('│', int.Max(indent - 1, 0))}{(indent == 0 ? "" : "├")}{nodeInfo}";
			Console.WriteLine(line);

			if (visited.Contains(node))
				throw new Exception("Loop found");

			visited.Add(node);

			PrintTree(txt, nodeTree, nodeTree[node].right, indent + 1, visited, simplified);
			PrintTree(txt, nodeTree, nodeTree[node].left, indent + 1, visited, simplified);
		}
	}
}