﻿using System.Runtime.CompilerServices;
using System.Text;

namespace TemplateLanguage
{
	internal ref struct TemplateLanguage
	{
		public ReadOnlySpan<char> txt;
		public ReadOnlySpan<Node> nodes;

		public void Compute(int root, StringBuilder sb)
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
					sb.Append(ComputeNumber(root));
					break;
				case NodeType.Bracket:
					Compute(rootNode.right, sb);
					break;
				case NodeType.String:
					Compute(rootNode.right, sb);
                    sb.Append(rootNode.token.GetSpan(txt));
					Compute(rootNode.left, sb);
					break;
				case NodeType.Start:
					Compute(rootNode.right, sb);
					break;
				case NodeType.End:
					break;
				default:
					throw new Exception("WTF!");
			}
		}

		float ComputeNumber(int root)
		{
            ref readonly Node rootNode = ref nodes[root];

			switch (rootNode.nodeType)
			{
				case NodeType.Add:
					float leftNode = ComputeNumber(rootNode.left);
					float rightNode = ComputeNumber(rootNode.right);
					return leftNode + rightNode;
				case NodeType.Subtract:
					leftNode = ComputeNumber(rootNode.left);
					rightNode = ComputeNumber(rootNode.right);
                    return leftNode - rightNode;
				case NodeType.Multiply:
					leftNode = ComputeNumber(rootNode.left);
					rightNode = ComputeNumber(rootNode.right);
                    return leftNode * rightNode;
				case NodeType.Divide:
					leftNode = ComputeNumber(rootNode.left);
					rightNode = ComputeNumber(rootNode.right);
                    return leftNode / rightNode;
				case NodeType.Integer:
					return GetInt(rootNode.token.GetSpan(txt));
				case NodeType.Float:
					return GetFloat(rootNode.token.GetSpan(txt));
				case NodeType.Bracket:
					return ComputeNumber(rootNode.right);
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
	}
}