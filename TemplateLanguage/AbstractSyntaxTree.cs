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

		public void Dispose()
		{
			buff.Dispose();
		}
    }

	ref struct AbstractSyntaxTree
	{
		struct NodeState
		{
			public int idx;
			public bool isFactor;
		}

		Span<Node> nodeTree;
		RefStack<NodeState> root;

		int currIdx = 0;

		public AbstractSyntaxTree(Span<Node> nodeTree)
		{
            this.nodeTree = nodeTree;
			root = new RefStack<NodeState>(64);
			root.Push(new NodeState() { idx = 0, isFactor = false });

			// Put everything inside a bracket som no operators is exposed to the cold dead void outside those brackets
			int rootIdx = root.Peek().idx;
			Node.CreateBracket(ref nodeTree[currIdx], -1, rootIdx);
			currIdx++;
		}

		public void InsertAbove(in Token token)
		{
			int rootIdx = root.Peek().idx;
			ref Node rootNode = ref nodeTree[rootIdx];

			Node.CreateString(ref nodeTree[currIdx], token, rootIdx, -1);
			rootNode.parent = currIdx;

			SetRoot(currIdx, root.Peek().isFactor);
			currIdx++;
		}

		public void InsertRight(in Token token, NodeType type)
		{
			int rootIdx = root.Peek().idx;
			Node.CreateNumber(ref nodeTree[currIdx], token, type, rootIdx);

			// First node will have root be itself
			if (rootIdx != currIdx)
				nodeTree[rootIdx].right = currIdx;

			currIdx++;
		}

		public void InsertOperator(in Token token, NodeType type)
		{
			int rootIdx = root.Peek().idx;
			ref Node rootNode = ref nodeTree[rootIdx];

			Node.CreateOperator(ref nodeTree[currIdx], type, rootIdx, -1, -1);
			rootNode.parent = currIdx;

			SetRoot(currIdx, root.Peek().isFactor);
			currIdx++;
		}

		public void BracketOpen(bool isFactor)
		{
			int rootIdx = root.Peek().idx;
			Node.CreateBracket(ref nodeTree[currIdx], -1, rootIdx);

			nodeTree[rootIdx].right = currIdx;

			root.Push(new NodeState { idx = currIdx, isFactor = isFactor });
			currIdx++;
			root.Push(new NodeState { idx = currIdx, isFactor = isFactor });
		}

		public void BracketClose()
		{
			int prevRoot = root.Pop().idx;
			int bracketRoot = root.Pop().idx;
			int rootIdx = root.Peek().idx;

			ref Node rootNode = ref nodeTree[rootIdx];
			rootNode.right = bracketRoot;

			ref Node bracketNode = ref nodeTree[bracketRoot];
			bracketNode.right = prevRoot;
		}

		public void StartFactor()
		{
			int prevRootIdx = root.Peek().idx;
			ref Node prevRootNode = ref nodeTree[prevRootIdx];
			int afterNewRoot = prevRootNode.right; // BracketOpen will change prevRootNode right int

			BracketOpen(true);
			SetRoot(afterNewRoot, true);

			ref Node newRootNode = ref nodeTree[prevRootNode.right];
			newRootNode.parent = -1;
		}

		public void StopFactor()
		{
			if (root.Peek().isFactor)
				BracketClose();
		}

		public void StopAllFactors()
		{
			while (root.Peek().isFactor)
				BracketClose();
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
			return root.Peek().idx;
		}

		void SetRoot(int root, bool isFactor)
		{
			this.root.Pop();
			this.root.Push(new NodeState { idx = root, isFactor = isFactor });
		}
	}

	internal static class AbstractSyntaxTreeExtensions
	{
		public static void PrintTree(this AbstractSyntaxTree ast, in ReadOnlySpan<char> txt)
		{
			List<int> visited = new List<int>();
            Console.WriteLine($"Stack Depth: {ast.GetStackDepth()}");
            PrintTree(txt, ast.GetTree(), ast.GetRoot(), 0, visited);
		}

		static void PrintTree(in ReadOnlySpan<char> txt, in ReadOnlySpan<Node> nodeTree, int node, int indent, List<int> visited)
		{
			if (node == -1)
				return;

			ref readonly Node nodeRef = ref nodeTree[node];
			string nodeInfo;

			// Simplify printout
			/*
			if (nodeRef.nodeType == NodeType.Bracket)
			{
				PrintTree(txt, nodeTree, nodeTree[node].right, indent, visited);
				return;
			}
			*/

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

			PrintTree(txt, nodeTree, nodeTree[node].right, indent + 1, visited);
			PrintTree(txt, nodeTree, nodeTree[node].left, indent + 1, visited);
		}
	}
}