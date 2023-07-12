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
		NewLine,

		TextBlock,
		CodeBlock,
		VariableBlock,
		Bracket,

		Integer,
		Float,
		Bool,

		Add,
		Subtract,
		Multiply,
		Divide,

		Assign,
		Accessor,

		Variable,
		String,

		If,
		Else,
		Equals,
	}

	struct Node
	{
		public NodeType nodeType;
		public Token token;

		public int right;
		public int middle;
		public int left;

		public Node(NodeType nodeType, Token token, int right, int middle, int left)
		{
			this.nodeType = nodeType;
			this.token = token;
			this.right = right;
			this.middle = middle;
			this.left = left;
		}

		public static void Create(ref Node node, NodeType nodeType, Token token = default, int right = -1, int middle = -1, int left = -1)
		{
			node = new()
			{
				nodeType = nodeType,
				token = token,
				right = right,
				middle = middle,
				left = left
			};
		}
	}
}
