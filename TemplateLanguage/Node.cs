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

		Integer,
		Float,

		Add,
		Subtract,
		Multiply,
		Divide,

		Bracket,
	}

	struct Node
	{
		public NodeType nodeType;
		public Token token;

		public int parent;
		public int right;
		public int left;

		public Node(NodeType nodeType, Token token, int right, int left, int parent)
		{
			this.nodeType = nodeType;
			this.token = token;
			this.right = right;
			this.left = left;
			this.parent = parent;
		}

		public static void CreateStart(ref Node node, int right)
		{
			node = new()
			{
				nodeType = NodeType.Start,
				right = right,
				left = -1,
				parent = -1
			};
		}

		public static void CreateEnd(ref Node node)
		{
			node = new()
			{
				nodeType = NodeType.End,
				right = -1,
				left = -1,
				parent = -1
			};
		}

		public static void CreateNumber(ref Node node, Token token, NodeType type, int parent)
		{
			node = new()
			{
				nodeType = type,
				token = token,
				left = -1,
				right = -1,
				parent = parent
			};
		}

		public static void CreateOperator(ref Node node, NodeType type, int right, int left, int parent)
		{
			node = new()
			{
				nodeType = type,
				left = left,
				right = right,
				parent = parent
			};
		}

		public static void CreateBracket(ref Node node, int right, int parent)
		{
			node = new()
			{
				nodeType = NodeType.Bracket,
				left = -1,
				right = right,
				parent = parent
			};
		}

		public static void CreateString(ref Node node, Token token, int right, int left, int parent)
		{
			node = new()
			{
				nodeType = NodeType.String,
				token = token,
				left = left,
				right = right,
				parent = parent
			};
		}
	}
}
