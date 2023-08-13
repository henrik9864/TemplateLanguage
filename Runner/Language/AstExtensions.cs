using LightParser;
using LightLexer;
using System;

namespace Runner
{
	public static class AstExtensions
	{
		public static int InsertNode(this ref AbstractSyntaxTree<NodeType> ast, NodeType nodeType)
		{
			Node<NodeType>.Create(ref ast.CurrentNode, nodeType);
			return ast.Advance();
		}

		public static int InsertNode(this ref AbstractSyntaxTree<NodeType> ast, NodeType nodeType, in Token token)
		{
			Node<NodeType>.Create(ref ast.CurrentNode, nodeType, token: token);
			return ast.Advance();
		}

		public static int InsertRight(this ref AbstractSyntaxTree<NodeType> ast, NodeType nodeType)
		{
			ref Node<NodeType> root = ref ast.CurrentRoot;

			int right = root.right == ast.CurrentIdx ? -1 : root.right;

            Node<NodeType>.Create(ref ast.CurrentNode, nodeType, right: right, middle: root.middle);
			root.right = ast.CurrentIdx;
			root.middle = -1;

			return ast.Advance();
		}

		public static int InsertRight(this ref AbstractSyntaxTree<NodeType> ast, NodeType nodeType, in Token token)
		{
			ref Node<NodeType> root = ref ast.CurrentRoot;

			int right = root.right == ast.CurrentIdx ? -1 : root.right;

			Node<NodeType>.Create(ref ast.CurrentNode, nodeType, token: token, right: right, middle: root.middle);
			root.right = ast.CurrentIdx;
			root.middle = -1;

			return ast.Advance();
		}

		public static int TakeLeft(this ref AbstractSyntaxTree<NodeType> ast, NodeType nodeType)
		{
			ref Node<NodeType> root = ref ast.CurrentRoot;

			int left = root.right == ast.CurrentIdx ? -1 : root.right;

			Node<NodeType>.Create(ref ast.CurrentNode, nodeType, left: left, middle: root.middle);
			root.right = ast.CurrentIdx;
			root.middle = -1;

			return ast.Advance();
		}

		public static int InsertMiddle(this ref AbstractSyntaxTree<NodeType> ast, NodeType nodeType)
		{
			ref Node<NodeType> root = ref ast.CurrentRoot;

			int middle = root.middle == ast.CurrentIdx ? -1 : root.middle;

			Node<NodeType>.Create(ref ast.CurrentNode, nodeType, middle: middle);
			root.middle = ast.CurrentIdx;

			return ast.Advance();
		}

		public static int InsertMiddle(this ref AbstractSyntaxTree<NodeType> ast, NodeType nodeType, in Token token)
		{
			ref Node<NodeType> root = ref ast.CurrentRoot;

			int middle = root.middle == ast.CurrentIdx ? -1 : root.middle;

			Node<NodeType>.Create(ref ast.CurrentNode, nodeType, token: token, middle: middle);
			root.middle = ast.CurrentIdx;

			return ast.Advance();
		}

		public static int BracketOpen(this ref AbstractSyntaxTree<NodeType> ast)
		{
			ast.PushRoot();
			int bracket = InsertNode(ref ast, NodeType.Bracket);
			ast.SetRight(bracket);

			return ast.CurrentIdx;
		}

		public static void BracketClose(this ref AbstractSyntaxTree<NodeType> ast)
		{
			ast.PopRoot();
		}
	}

	ref struct AbstractSyntaxTree2<T> where T : Enum
	{
		// --------- START ---------

		public void InsertStart()
		{
			// InsertNode(NodeType.Start);
			//Node.Create(ref nodeTree[currIdx++], NodeType.Start);
		}

		public void InsertEnd()
		{
			// InsertNode(NodeType.End);
			//Node.Create(ref nodeTree[currIdx++], NodeType.End);
		}

		// --------- STRING ---------

		public void InsertTextBlock(in Token token)
		{
			/*
			int rootIdx = currRoot.Peek();
			ref Node rootNode = ref nodeTree[rootIdx];

			int right = rootNode.right == currIdx ? -1 : rootNode.right;

			Node.Create(ref nodeTree[currIdx], NodeType.TextBlock, token: token, right: right);
			rootNode.right = currIdx;

			currIdx++;
			*/
			// InsertRight(NodeType.TextBlock, token);
		}

		public void InsertNewLineBlock()
		{
			/*
			int rootIdx = currRoot.Peek();
			ref Node rootNode = ref nodeTree[rootIdx];

			int right = rootNode.right == currIdx ? -1 : rootNode.right;

			Node.Create(ref nodeTree[currIdx], NodeType.NewLineBlock, right: right);
			rootNode.right = currIdx;

			currIdx++; 
			*/
			// InsertRight(NodeType.NewLineBlock, token);
		}

		public void InsertVariableBlock(in Token token)
		{
			/*
			int rootIdx = currRoot.Peek();
			ref Node rootNode = ref nodeTree[rootIdx];

			Node.Create(ref nodeTree[currIdx], NodeType.VariableBlock, token: token, right: rootNode.right);
			rootNode.right = currIdx;

			currIdx++;
			*/
			// InsertRight(NodeType.VariableBlock, token);
		}

		public void StartCodeBlock()
		{
			/*
			int rootIdx = currRoot.Peek();
			ref Node rootNode = ref nodeTree[rootIdx];

			int right = rootNode.right == currIdx ? -1 : rootNode.right;

			Node.Create(ref nodeTree[currIdx], NodeType.CodeBlock, right: right, left: currIdx + 1);
			rootNode.right = currIdx;

			currIdx++;
			*/
			//int codeBlock = InsertRight(NodeType.CodeBlock, token);
			// SetLeft(codeBlock);
		}

		public void InsertFilter()
		{
			/*
			int rootIdx = currRoot.Peek();
			ref Node rootNode = ref nodeTree[rootIdx];

			Node.Create(ref nodeTree[currIdx], NodeType.Filter, right: currIdx + 1);
			rootNode.middle = currIdx;

			currIdx++;
			*/
			// InsertMiddle(NodeType.Filter);
		}

		// --------- CODE ---------

		public int InsertIf()
		{
			/*
			Node.Create(ref nodeTree[currIdx], NodeType.If, left: ++currIdx);

			return currIdx - 1;
			*/
			//int if = InsertNode(NodeType.If)
			// SetLeft(if)
			// return ast.Advance();
			return 0;
		}

		/*
		public void SetMiddle(int idx)
		{
			nodeTree[idx].middle = currIdx;
		}

		public void SetRight(int idx)
		{
			nodeTree[idx].right = currIdx;
		}

		public void SetLeft(int idx)
		{
			nodeTree[idx].left = currIdx;
		}
		*/

		public void InsertBool(in Token token)
		{
			/*
			int rootIdx = currRoot.Peek();
			if (rootIdx == currIdx)
				throw new Exception("Bool cannot be a root.");

			Node.Create(ref nodeTree[currIdx++], NodeType.Bool, token: token);
			*/
			// InsertNode(NodeType.Bool, token)
		}

		// --------- EXPRESSION ---------

		public int InsertOperator(NodeType type)
		{
			/*
			int rootIdx = currRoot.Peek();
			ref Node rootNode = ref nodeTree[rootIdx];

			int left = rootNode.right == currIdx ? -1 : rootNode.right;

			Node.Create(ref nodeTree[currIdx], type, right: currIdx + 1, middle: rootNode.middle, left: left);
			rootNode.right = currIdx;
			rootNode.middle = -1;

			currIdx++;
			return currIdx - 1;
			*/
			// int take = TakeLeft(type)
			// SetRight(take)

			return 0;
		}

		public void InsertNumber(in Token token, NodeType type)
		{
			/*
			int rootIdx = currRoot.Peek();
			if (rootIdx == currIdx)
				throw new Exception("Number cannot be a root.");

			Node.Create(ref nodeTree[currIdx++], type, token: token);
			*/
			// InsertNode(type, token)
		}

		/*
		public void BracketOpen()
		{
			Node.Create(ref nodeTree[currIdx], NodeType.Bracket, right: currIdx + 1);

			currRoot.Push(currIdx);
			currIdx++;
		}

		public void BracketClose()
		{
			if (currRoot.Count == 1)
				throw new Exception("Root stack must never be empty.");

			currRoot.Pop();
		}
		*/

		public void InsertVariable()
		{
			/*
			int rootIdx = currRoot.Peek();
			ref Node rootNode = ref nodeTree[rootIdx];

			Node.Create(ref nodeTree[currIdx], NodeType.Variable, right: currIdx - 1);
			rootNode.right = currIdx;

			currIdx++;
			*/
			//int var = InsertRight(NodeType.Variable)
			//SetRight(var)
			//return ast.Advance();
		}

		public void InsertString(in Token token)
		{
			//Node.Create(ref nodeTree[currIdx++], NodeType.String, token: token);
			// InsertNode(NodeType.String, token)
		}
	}
}