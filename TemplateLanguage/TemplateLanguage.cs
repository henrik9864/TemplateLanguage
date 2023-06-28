using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace TemplateLanguage
{
	internal ref struct TemplateLanguage
	{
		public ReadOnlySpan<char> txt;
		public ReadOnlySpan<Node> nodes;

		public void Compute(int root, StringBuilder sb, IModel model)
		{
			if (root == -1)
				return;

            ref readonly Node rootNode = ref nodes[root];

			switch (rootNode.nodeType)
			{
				case NodeType.Integer:
					sb.Append(GetInt(rootNode.token.GetSpan(txt)));
					break;
				case NodeType.Float:
					sb.Append(GetFloat(rootNode.token.GetSpan(txt)));
					break;
				case NodeType.Add:
				case NodeType.Subtract:
				case NodeType.Multiply:
				case NodeType.Divide:
					sb.Append(ComputeNumber(root, model));
					break;
				case NodeType.Bracket:
					Compute(rootNode.right, sb, model);
					break;
				case NodeType.String:
					Compute(rootNode.right, sb, model);
                    sb.Append(rootNode.token.GetSpan(txt));
					break;
				case NodeType.CodeBlock:
					Compute(rootNode.right, sb, model);
					Compute(rootNode.left, sb, model);
					break;
				case NodeType.Variable:
					{
						ref readonly Node rightNode = ref nodes[rootNode.right];
						sb.Append(model[rightNode.token.GetSpan(txt)]);
						break;
					}
				case NodeType.If:
					{
						if (ComputeBool(rootNode.left, model))
							Compute(rootNode.middle, sb, model);
						else
							Compute(rootNode.right, sb, model);

						break;
					}
				case NodeType.Equals:
					{
						float leftNode = ComputeNumber(rootNode.left, model);
						float rightNode = ComputeNumber(rootNode.right, model);

						sb.Append(leftNode == rightNode);
						break;
					}
				case NodeType.Assign:
					{
                        float rightNode = ComputeNumber(rootNode.right, model);
						ref readonly Node variableNode = ref nodes[ComputeVariable(rootNode.left)];
						ref readonly Node nameNode = ref nodes[variableNode.right];

                        Console.WriteLine($"Name: {nameNode.nodeType} Var: {variableNode.nodeType}");

                        model.Set(nameNode.token.GetSpan(txt), rightNode.ToString());
						break;
					}
				case NodeType.Start:
					Compute(rootNode.right, sb, model);
					break;
				case NodeType.End:
					break;
				default:
					throw new Exception("WTF!");
			}
		}

		float ComputeNumber(int root, IModel model)
		{
            ref readonly Node rootNode = ref nodes[root];

			switch (rootNode.nodeType)
			{
				case NodeType.Add:
					{
						float leftNode = ComputeNumber(rootNode.left, model);
						float rightNode = ComputeNumber(rootNode.right, model);
						return leftNode + rightNode;
					}
				case NodeType.Subtract:
					{
						float leftNode = ComputeNumber(rootNode.left, model);
						float rightNode = ComputeNumber(rootNode.right, model);
                    return leftNode - rightNode;
					}
				case NodeType.Multiply:
					{
						float leftNode = ComputeNumber(rootNode.left, model);
						float rightNode = ComputeNumber(rootNode.right, model);
						return leftNode * rightNode;
					}
				case NodeType.Divide:
					{
					float leftNode = ComputeNumber(rootNode.left, model);
					float rightNode = ComputeNumber(rootNode.right, model);
                    return leftNode / rightNode;
					}
				case NodeType.Integer:
					return GetInt(rootNode.token.GetSpan(txt));
				case NodeType.Float:
					return GetFloat(rootNode.token.GetSpan(txt));
				case NodeType.Variable:
					{
						ref readonly Node rightNode = ref nodes[rootNode.right];
						return GetFloat(model[rightNode.token.GetSpan(txt)]);
					}
				case NodeType.Bracket:
					return ComputeNumber(rootNode.right, model);
				default:
					throw new Exception("WTF!");
			}
		}

		bool ComputeBool(int root, IModel model)
		{
			ref readonly Node rootNode = ref nodes[root];

			switch (rootNode.nodeType)
			{
				case NodeType.Bool:
					{
						return GetBool(rootNode.token.GetSpan(txt));
					}
				case NodeType.Variable:
					{
						ref readonly Node rightNode = ref nodes[rootNode.right];
						return GetBool(model[rightNode.token.GetSpan(txt)]);
					}
				case NodeType.Equals:
					{
						float leftNode = ComputeNumber(rootNode.left, model);
						float rightNode = ComputeNumber(rootNode.right, model);

						return leftNode == rightNode;
					}
				case NodeType.Bracket:
					{
						return ComputeBool(rootNode.right, model);
					}
				default:
					throw new Exception("WTF!");
			}
		}

		int ComputeVariable(int root)
		{
			ref readonly Node rootNode = ref nodes[root];

			switch (rootNode.nodeType)
			{
				case NodeType.Bracket:
					{
						return ComputeVariable(rootNode.right);
					}
				case NodeType.Variable:
					{
						return root;
					}
				default:
					throw new Exception("WTF!");
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static float GetInt(ReadOnlySpan<char> txt)
		{
			return int.Parse(txt, System.Globalization.CultureInfo.InvariantCulture);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static float GetFloat(ReadOnlySpan<char> txt)
		{
			return float.Parse(txt, System.Globalization.CultureInfo.InvariantCulture);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static bool GetBool(ReadOnlySpan<char> txt)
		{
			return bool.Parse(txt);
		}
	}
}