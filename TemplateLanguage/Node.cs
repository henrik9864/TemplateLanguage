using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokhenizer;

namespace TemplateLanguage
{
	enum NodeType
	{
		Start,
		End,

		String,
		Bracket,

		Integer,
		Float,

		Add,
		Subtract,
		Multiply,
		Divide,

		Variable,
		Name,

		If,
		Else,
		Comparer,
	}

	struct Node
	{
		public NodeType nodeType;
		public Token token;

		public int right;
		public int left;

		public Node(NodeType nodeType, Token token, int right, int left)
		{
			this.nodeType = nodeType;
			this.token = token;
			this.right = right;
			this.left = left;
		}

		public static void CreateStart(ref Node node, int right)
		{
			node = new()
			{
				nodeType = NodeType.Start,
				right = right,
				left = -1
			};
		}

		public static void CreateEnd(ref Node node)
		{
			node = new()
			{
				nodeType = NodeType.End,
				right = -1,
				left = -1
			};
		}

		public static void CreateNumber(ref Node node, Token token, NodeType type)
		{
			node = new()
			{
				nodeType = type,
				token = token,
				left = -1,
				right = -1
			};
		}

		public static void CreateOperator(ref Node node, NodeType type, int right, int left)
		{
			node = new()
			{
				nodeType = type,
				left = left,
				right = right
			};
		}

		public static void CreateBracket(ref Node node, int right, int parent)
		{
			node = new()
			{
				nodeType = NodeType.Bracket,
				left = -1,
				right = right
			};
		}

		public static void CreateString(ref Node node, Token token, int right, int left)
		{
			node = new()
			{
				nodeType = NodeType.String,
				token = token,
				left = left,
				right = right
			};
		}

		public static void CreateVariable(ref Node node, int right)
		{
			node = new()
			{
				nodeType = NodeType.Variable,
				left = -1,
				right = right
			};
		}

		public static void CreateName(ref Node node, Token token)
		{
			node = new()
			{
				nodeType = NodeType.Name,
				token = token,
				left = -1,
				right = -1
			};
		}

		public static void CreateIf(ref Node node, int right)
		{
			node = new()
			{
				nodeType = NodeType.If,
				left = -1,
				right = right
			};
		}

		public static void CreateComparer(ref Node node, int right, int left)
		{
			node = new()
			{
				nodeType = NodeType.Comparer,
				left = left,
				right = right
			};
		}
	}
}
