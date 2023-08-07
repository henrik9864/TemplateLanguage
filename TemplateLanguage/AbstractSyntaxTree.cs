﻿using System;
using System.Collections.Generic;
using System.Text;
using LightLexer;

namespace TemplateLanguage
{
	ref struct AbstractSyntaxTree
	{
		Span<Node> nodeTree;
		RefStack<int> currRoot;

		int currIdx = 0;

		public AbstractSyntaxTree(Span<Node> nodeTree)
		{
            this.nodeTree = nodeTree;
			currRoot = new RefStack<int>(64);
			currRoot.Push(0);
		}

		// --------- START ---------

		public void InsertStart()
		{
			Node.Create(ref nodeTree[currIdx++], NodeType.Start);
		}

		public void InsertEnd()
		{
			Node.Create(ref nodeTree[currIdx++], NodeType.End);
		}

		// --------- STRING ---------

		public void InsertTextBlock(in Token token)
		{
			int rootIdx = currRoot.Peek();
			ref Node rootNode = ref nodeTree[rootIdx];

			int right = rootNode.right == currIdx ? -1 : rootNode.right;

			Node.Create(ref nodeTree[currIdx], NodeType.TextBlock, token: token, right: right);
			rootNode.right = currIdx;

			currIdx++;
		}

		public void InsertNewLineBlock()
		{
			int rootIdx = currRoot.Peek();
			ref Node rootNode = ref nodeTree[rootIdx];

			int right = rootNode.right == currIdx ? -1 : rootNode.right;

			Node.Create(ref nodeTree[currIdx], NodeType.NewLineBlock, right: right);
			rootNode.right = currIdx;

			currIdx++;
		}

		public void InsertVariableBlock(in Token token)
		{
			int rootIdx = currRoot.Peek();
			ref Node rootNode = ref nodeTree[rootIdx];

			Node.Create(ref nodeTree[currIdx], NodeType.VariableBlock, token: token, right: rootNode.right);
			rootNode.right = currIdx;

			currIdx++;
		}

		public void StartCodeBlock()
		{
			int rootIdx = currRoot.Peek();
			ref Node rootNode = ref nodeTree[rootIdx];

			int right = rootNode.right == currIdx ? -1 : rootNode.right;

			Node.Create(ref nodeTree[currIdx], NodeType.CodeBlock, right: right, left: currIdx + 1);
			rootNode.right = currIdx;

			currIdx++;
		}

		public void InsertFilter()
		{
			int rootIdx = currRoot.Peek();
			ref Node rootNode = ref nodeTree[rootIdx];

			Node.Create(ref nodeTree[currIdx], NodeType.Filter, right: currIdx + 1);
			rootNode.middle = currIdx;

			currIdx++;
		}

		// --------- CODE ---------

		public int InsertIf()
		{
			Node.Create(ref nodeTree[currIdx], NodeType.If, left: ++currIdx);

			return currIdx - 1;
		}

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

		public void InsertBool(in Token token)
		{
			int rootIdx = currRoot.Peek();
			if (rootIdx == currIdx)
				throw new Exception("Bool cannot be a root.");

			Node.Create(ref nodeTree[currIdx++], NodeType.Bool, token: token);
		}

		// --------- EXPRESSION ---------

		public int InsertOperator(NodeType type)
		{
			int rootIdx = currRoot.Peek();
			ref Node rootNode = ref nodeTree[rootIdx];

			int left = rootNode.right == currIdx ? -1 : rootNode.right;

			Node.Create(ref nodeTree[currIdx], type, right: currIdx + 1, middle: rootNode.middle, left: left);
			rootNode.right = currIdx;
			rootNode.middle = -1;

			currIdx++;
			return currIdx - 1;
		}

		public void InsertNumber(in Token token, NodeType type)
		{
			int rootIdx = currRoot.Peek();
			if (rootIdx == currIdx)
				throw new Exception("Number cannot be a root.");

			Node.Create(ref nodeTree[currIdx++], type, token: token);
		}

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

		public void InsertVariable()
		{
			int rootIdx = currRoot.Peek();
			ref Node rootNode = ref nodeTree[rootIdx];

			Node.Create(ref nodeTree[currIdx], NodeType.Variable, right: currIdx - 1);
			rootNode.right = currIdx;

			currIdx++;
		}

		public void InsertString(in Token token)
		{
			Node.Create(ref nodeTree[currIdx++], NodeType.String, token: token);
		}

		// ------------------

		public Span<Node> GetTree()
		{
			return nodeTree.Slice(0, currIdx);
		}

		public int GetStackDepth()
		{
			return currRoot.Count;
		}

		public int GetRoot()
		{
			return currRoot.Peek();
		}
	}

	internal static class AbstractSyntaxTreeExtensions
	{
		public static void PrintStackDepth(this ref AbstractSyntaxTree ast)
		{
			Console.WriteLine($"Stack Depth: {ast.GetStackDepth()}");
			Console.WriteLine();
		}

		public static void PrintNodes(this ref AbstractSyntaxTree ast)
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

		public static void PrintTree(this ref AbstractSyntaxTree ast, in ReadOnlySpan<char> txt, scoped ReadOnlySpan<ReturnType> returnTypes, bool simplified)
		{
			var tree = ast.GetTree();

			List<int> visited = new List<int>();
			PrintTree(txt, tree, returnTypes, ast.GetRoot(), 0, visited, simplified);
		}

		static void PrintTree(in ReadOnlySpan<char> txt, in ReadOnlySpan<Node> nodeTree, in ReadOnlySpan<ReturnType> returnTypes, int node, int indent, List<int> visited, bool simplified)
		{
			if (node == -1)
				return;

			ref readonly Node nodeRef = ref nodeTree[node];
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

		static char GetChar(in Node node)
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
}