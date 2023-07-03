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
		Any = 31,
	}

	internal static class TemplateLanguageTypeResolver
	{
		public static void ResolveTypes(int root, ReadOnlySpan<Node> nodes, Span<ReturnType> returnTypes, ReadOnlySpan<char> txt, IModel model)
		{
			ComputeReturnTypes(root, nodes, returnTypes, txt, model);
		}

		static ReturnType ComputeReturnTypes(int root, ReadOnlySpan<Node> nodes, Span<ReturnType> returnTypes, ReadOnlySpan<char> txt, IModel model)
		{
			if (root == -1)
				return ReturnType.None;

			ref readonly Node rootNode = ref nodes[root];

            var rightType = ComputeReturnTypes(rootNode.right, nodes, returnTypes, txt, model);
			var middleType = ComputeReturnTypes(rootNode.middle, nodes, returnTypes, txt, model);
			var leftType = ComputeReturnTypes(rootNode.left, nodes, returnTypes, txt, model);

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
				case NodeType.CodeBlock:
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
				case NodeType.Compare:
					returnTypes[root] = rightType;
					break;
				case NodeType.Equals:
					returnTypes[root] = ReturnType.Bool;
					break;
				case NodeType.Assign:
					returnTypes[root] = ReturnType.None;
					break;
				case NodeType.Variable:
					// TODO: Bit dirty
					ref readonly Node rightNode = ref nodes[rootNode.right];

					returnTypes[root] = model.GetType(rightNode.token.GetSpan(txt)) | ReturnType.Variable;
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
				default:
					throw new Exception("WTF!");
			}

			return returnTypes[root];
		}
    }
}