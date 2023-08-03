using System;

namespace TemplateLanguage
{
	[Flags]
	public enum ReturnType
	{
		Unknown = 0,
		None = 1,
		Number = 2,
		Bool = 4,
		String = 8,
		Variable = 16,
		Model = 32,
		Enumerable = 64,
		Any = 127,
	}

	internal static class TemplateLanguageTypeResolver
	{
		public static void ResolveTypes(int root, ReadOnlySpan<Node> nodes, Span<ReturnType> returnTypes)
		{
			ComputeReturnTypes(root, nodes, returnTypes);
		}

		static ReturnType ComputeReturnTypes(int root, ReadOnlySpan<Node> nodes, Span<ReturnType> returnTypes)
		{
			if (root == -1)
				return ReturnType.None;

			ref readonly Node rootNode = ref nodes[root];

            var rightType = ComputeReturnTypes(rootNode.right, nodes, returnTypes);
			var middleType = ComputeReturnTypes(rootNode.middle, nodes, returnTypes);
			var leftType = ComputeReturnTypes(rootNode.left, nodes, returnTypes);

			switch (rootNode.nodeType)
			{
				case NodeType.Start:
					returnTypes[root] = ReturnType.None;
					break;
				case NodeType.End:
					returnTypes[root] = ReturnType.None;
					break;
				case NodeType.NewLine:
					returnTypes[root] = ReturnType.None;
					break;
				case NodeType.TextBlock:
					returnTypes[root] = ReturnType.None;
					break;
				case NodeType.NewLineBlock:
					returnTypes[root] = ReturnType.None;
					break;
				case NodeType.CodeBlock:
					returnTypes[root] = ReturnType.None;
					break;
				case NodeType.RepeatCodeBlock:
					returnTypes[root] = ReturnType.None;
					break;
				case NodeType.VariableBlock:
					returnTypes[root] = ReturnType.None;
					break;
				case NodeType.Bracket:
					returnTypes[root] = rightType;
					break;
				case NodeType.Integer:
					returnTypes[root] = ReturnType.Number;
					break;
				case NodeType.Float:
					returnTypes[root] = ReturnType.Number;
					break;
				case NodeType.Bool:
					returnTypes[root] = ReturnType.Bool;
					break;
				case NodeType.Add:
				case NodeType.Subtract:
				case NodeType.Multiply:
				case NodeType.Divide:
					returnTypes[root] = ReturnType.Number;
					break;
				case NodeType.Equals:
				case NodeType.Greater:
				case NodeType.Less:
				case NodeType.And:
				case NodeType.Or:
					returnTypes[root] = ReturnType.Bool;
					break;
				case NodeType.Assign:
					returnTypes[root] = ReturnType.None;
					break;
				case NodeType.Variable:
					returnTypes[root] = ReturnType.Variable;
					break;
				case NodeType.String:
					returnTypes[root] = ReturnType.String;
					break;
				case NodeType.If:
					returnTypes[root] = ReturnType.None;
					break;
				case NodeType.Else:
					returnTypes[root] = ReturnType.None;
					break;
				case NodeType.Accessor:
					returnTypes[root] = ReturnType.Variable;
					break;
				case NodeType.AccessorBlock:
					returnTypes[root] = ReturnType.None;
					break;
				case NodeType.EnumerableAccessorBlock:
					returnTypes[root] = ReturnType.None;
					break;
				case NodeType.Filter:
					returnTypes[root] = rightType;
					break;
				case NodeType.Conditional:
					returnTypes[root] = leftType;
					break;
				default:
					throw new Exception("WTF!");
			}

			return returnTypes[root];
		}
    }
}