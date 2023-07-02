using System.Buffers;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace TemplateLanguage
{
	delegate void TemplateMethod(ref TemplateContext container, int root, StringBuilder sb, IModel model);
	delegate T TemplateMethod<T>(ref TemplateContext container, int root, StringBuilder sb, IModel model);

	static class TemplateLanguageRules
	{
		static MethodContainer<NodeType, ReturnType, TemplateMethod> voideMethods = new()
		{
			// -------- Start/Bracket --------
			{ NodeType.Start, ReturnType.Any, ReturnType.Unknown, ReturnType.Unknown, Start },
			{ NodeType.Bracket, ReturnType.Any, ReturnType.Unknown, ReturnType.Unknown, Start },

			// ------ Text Block ------
			{ NodeType.TextBlock, ReturnType.Any, ReturnType.Unknown, ReturnType.Unknown, TextBlock },

			// ------ Code Block ------
			{ NodeType.CodeBlock, ReturnType.Any, ReturnType.Unknown, ReturnType.Any, CodeBlock },

			// ------ Operators ------
			{ NodeType.If, ReturnType.Any, ReturnType.Any, ReturnType.Bool, If },
		};

		static MethodContainer<NodeType, ReturnType, TemplateMethod<float>> numberMethods = new()
		{
			// ------ Number ------
			{ NodeType.Float, ReturnType.Unknown, ReturnType.Unknown, ReturnType.Unknown, Float },
			{ NodeType.Integer, ReturnType.Unknown, ReturnType.Unknown, ReturnType.Unknown, Integer },

			// ------ Operators ------
			{ NodeType.Add, ReturnType.Number, ReturnType.Unknown, ReturnType.Number, Add },
			{ NodeType.Subtract, ReturnType.Number, ReturnType.Unknown, ReturnType.Number, Subtract },
			{ NodeType.Multiply, ReturnType.Number, ReturnType.Unknown, ReturnType.Number, Multiply },
			{ NodeType.Divide, ReturnType.Number, ReturnType.Unknown, ReturnType.Number, Divide },
			
			// ------ Operators ------
			{ NodeType.Variable, ReturnType.String, ReturnType.Unknown, ReturnType.Unknown, VariableNumber },
		};

		static MethodContainer<NodeType, ReturnType, TemplateMethod<bool>> boolMethods = new()
		{
			// ------ Bool ------
			{ NodeType.Bool, ReturnType.Unknown, ReturnType.Unknown, ReturnType.Unknown, Bool },

			// ------ Operators ------
			{ NodeType.Equals, ReturnType.Number | ReturnType.Bool, ReturnType.Unknown, ReturnType.Number | ReturnType.Bool, Equals },
		};

		static MethodContainer<NodeType, ReturnType, TemplateMethod<Range>> strMethods = new()
		{
			// ------ String ------
			{ NodeType.String, ReturnType.Unknown, ReturnType.Unknown, ReturnType.Unknown, String },
		};

		static TemplateLanguageRules()
		{
			// Add brackets to the containers
			numberMethods.Add(NodeType.Bracket, ReturnType.Any, ReturnType.Unknown, ReturnType.Unknown, Bracket(numberMethods));
			boolMethods.Add(NodeType.Bracket, ReturnType.Any, ReturnType.Unknown, ReturnType.Unknown, Bracket(boolMethods));
			strMethods.Add(NodeType.Bracket, ReturnType.Any, ReturnType.Unknown, ReturnType.Unknown, Bracket(strMethods));
		}

		public static void Compute(ref TemplateContext context, int node, StringBuilder sb, IModel model)
		{
			if (node == -1)
				return;

			ref readonly Node rootNode = ref context.nodes[node];
			GetMethod(ref context, voideMethods, rootNode)(ref context, node, sb, model);
		}

		static void TextBlock(ref TemplateContext context, int nodeIdx, StringBuilder sb, IModel model)
		{
			ref readonly Node node = ref context.nodes[nodeIdx];

			Compute(ref context, node.right, sb, model);
			sb.Append(node.token.GetSpan(context.txt));
		}

		static void CodeBlock(ref TemplateContext context, int nodeIdx, StringBuilder sb, IModel model)
		{
			ref readonly Node node = ref context.nodes[nodeIdx];
			Compute(ref context, node.right, sb, model);

			ReturnType type = GetType(ref context, node.left);
			switch (type)
			{
				case ReturnType.Number:
					{
						float value = Compute(ref context, node.left, sb, model, numberMethods);
						sb.Append(value);
					}
					break;
				case ReturnType.Bool:
					{
						bool value = Compute(ref context, node.left, sb, model, boolMethods);
						sb.Append(value);
					}
					break;
				case ReturnType.String:
					break;
				case ReturnType.Variable:
					break;
				default:
					Compute(ref context, node.left, sb, model);
					break;
			}
		}

		static void Start(ref TemplateContext context, int nodeIdx, StringBuilder sb, IModel model)
		{
			ref readonly Node node = ref context.nodes[nodeIdx];
			Compute(ref context, node.right, sb, model);
		}

		static float Float(ref TemplateContext context, int nodeIdx, StringBuilder sb, IModel model)
		{
			ref readonly Node node = ref context.nodes[nodeIdx];
			return float.Parse(node.token.GetSpan(context.txt), System.Globalization.CultureInfo.InvariantCulture);
		}

		static float Integer(ref TemplateContext context, int nodeIdx, StringBuilder sb, IModel model)
		{
			ref readonly Node node = ref context.nodes[nodeIdx];
			return int.Parse(node.token.GetSpan(context.txt), System.Globalization.CultureInfo.InvariantCulture);
		}

		static bool Bool(ref TemplateContext context, int nodeIdx, StringBuilder sb, IModel model)
		{
			ref readonly Node node = ref context.nodes[nodeIdx];
			return bool.Parse(node.token.GetSpan(context.txt));
		}

		static Range String(ref TemplateContext context, int nodeIdx, StringBuilder sb, IModel model)
		{
			return context.nodes[nodeIdx].token.range;
		}

		static float Add(ref TemplateContext context, int nodeIdx, StringBuilder sb, IModel model)
		{
			ref readonly Node node = ref context.nodes[nodeIdx];
			return Compute(ref context, node.right, sb, model, numberMethods) + Compute(ref context, node.left, sb, model, numberMethods);
		}

		static float Subtract(ref TemplateContext context, int nodeIdx, StringBuilder sb, IModel model)
		{
			ref readonly Node node = ref context.nodes[nodeIdx];
			return Compute(ref context, node.right, sb, model, numberMethods) - Compute(ref context, node.left, sb, model, numberMethods);
		}

		static float Multiply(ref TemplateContext context, int nodeIdx, StringBuilder sb, IModel model)
		{
			ref readonly Node node = ref context.nodes[nodeIdx];
			return Compute(ref context, node.right, sb, model, numberMethods) * Compute(ref context, node.left, sb, model, numberMethods);
		}

		static float Divide(ref TemplateContext context, int nodeIdx, StringBuilder sb, IModel model)
		{
			ref readonly Node node = ref context.nodes[nodeIdx];
			return Compute(ref context, node.right, sb, model, numberMethods) / Compute(ref context, node.left, sb, model, numberMethods);
		}

		static float VariableNumber(ref TemplateContext context, int nodeIdx, StringBuilder sb, IModel model)
		{
			ref readonly Node node = ref context.nodes[nodeIdx];
			Range range = Compute(ref context, node.right, sb, model, strMethods);

			return float.Parse(model[context.txt[range]], System.Globalization.CultureInfo.InvariantCulture);
		}

		static bool Equals(ref TemplateContext context, int nodeIdx, StringBuilder sb, IModel model)
		{
			ref readonly Node node = ref context.nodes[nodeIdx];

			ReturnType rightType = GetType(ref context, node.right);
			ReturnType leftType = GetType(ref context, node.left);

			return (rightType, leftType) switch
			{
				(ReturnType.Number, ReturnType.Number) => Compute(ref context, node.right, sb, model, numberMethods) == Compute(ref context, node.left, sb, model, numberMethods),
				(ReturnType.Bool, ReturnType.Bool) => Compute(ref context, node.right, sb, model, boolMethods) == Compute(ref context, node.left, sb, model, boolMethods),
				_ => throw new Exception($"Cannot compare types {rightType} and {leftType}"),
			};
		}

		static void If(ref TemplateContext context, int nodeIdx, StringBuilder sb, IModel model)
		{
			ref readonly Node node = ref context.nodes[nodeIdx];

			if (Compute(ref context, node.left, sb, model, boolMethods))
				ComputeAny(ref context, node.middle, sb, model, true);
			else
				ComputeAny(ref context, node.right, sb, model, true);
		}

		static TemplateMethod<T> Bracket<T>(MethodContainer<NodeType, ReturnType, TemplateMethod<T>> container)
		{
			return (ref TemplateContext context, int nodeIdx, StringBuilder sb, IModel model) =>
			{
				ref readonly Node node = ref context.nodes[nodeIdx];
				return Compute(ref context, node.right, sb, model, container);
			};
		}

		public static void ComputeAny(ref TemplateContext context, int nodeIdx, StringBuilder sb, IModel model, bool appendResult = false)
		{
			ReturnType type = GetType(ref context, nodeIdx);
			switch (type)
			{
				case ReturnType.Number:
					{
						float value = Compute(ref context, nodeIdx, sb, model, numberMethods);

						if (appendResult)
							sb.Append(value);
					}
					break;
				case ReturnType.Bool:
					{
						bool value = Compute(ref context, nodeIdx, sb, model, boolMethods);

						if (appendResult)
							sb.Append(value);
					}
					break;
				case ReturnType.String:
					break;
				case ReturnType.Variable:
					break;
				default:
					Compute(ref context, nodeIdx, sb, model);
					break;
			}
		}

		public static T Compute<T>(ref TemplateContext context, int node, StringBuilder sb, IModel model, MethodContainer<NodeType, ReturnType, TemplateMethod<T>> container)
		{
			ref readonly Node rootNode = ref context.nodes[node];
			return GetMethod(ref context, container, rootNode)(ref context, node, sb, model);
		}

		static ReturnType GetType(ref TemplateContext context, int idx)
		{
			if (idx == -1)
				return ReturnType.Unknown;

			return context.returnTypes[idx];
		}

		static T GetMethod<T>(ref TemplateContext context, MethodContainer<NodeType, ReturnType, T> methodContainer, in Node node)
		{
			ReturnType rightType = GetType(ref context, node.right);
			ReturnType middleType = GetType(ref context, node.middle);
			ReturnType leftType = GetType(ref context, node.left);

			return methodContainer.Get(node.nodeType, rightType, middleType, leftType);
		}
	}

	ref struct TemplateContext
	{
		public ReadOnlySpan<char> txt;
		public ReadOnlySpan<Node> nodes;
		public ReadOnlySpan<ReturnType> returnTypes;
	};

	internal ref struct TemplateLanguageNew
	{
		TemplateContext container;

		public TemplateLanguageNew(ReadOnlySpan<char> txt, ReadOnlySpan<Node> nodes, ReadOnlySpan<ReturnType> returnTypes)
		{
			this.container = new TemplateContext()
			{
				txt = txt,
				nodes = nodes,
				returnTypes = returnTypes
			};
		}

		public void Compute(int root, StringBuilder sb, IModel model)
			=> TemplateLanguageRules.Compute(ref container, root, sb, model);
	}

	internal ref struct TemplateLanguage
	{
		ReadOnlySpan<char> txt;
		ReadOnlySpan<Node> nodes;
		ReadOnlySpan<ReturnType> returnTypes;

		public TemplateLanguage(ReadOnlySpan<char> txt, ReadOnlySpan<Node> nodes, ReadOnlySpan<ReturnType> returnTypes)
		{
			this.txt = txt;
			this.nodes = nodes;
			this.returnTypes = returnTypes;
		}

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
				case NodeType.TextBlock:
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
						ReturnType type = returnTypes[rootNode.left];

						switch (type)
						{
							case ReturnType.Number:
								{
									float leftNode = ComputeNumber(rootNode.left, model);
									float rightNode = ComputeNumber(rootNode.right, model);

									sb.Append(leftNode == rightNode);
								}
								break;
							case ReturnType.Bool:
								{
									bool leftNode = ComputeBool(rootNode.left, model);
									bool rightNode = ComputeBool(rootNode.right, model);

									sb.Append(leftNode == rightNode);
								}
								break;
							default:
								throw new Exception($"Equals does not support type {type}");
						}
						break;
					}
				case NodeType.Assign:
					{
                        float rightNode = ComputeNumber(rootNode.right, model);
						ref readonly Node variableNode = ref nodes[ComputeVariable(rootNode.left)];
						ref readonly Node nameNode = ref nodes[variableNode.right];

                        model.Set(nameNode.token.GetSpan(txt), rightNode.ToString(), ReturnType.Number);
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
						ReturnType type = returnTypes[rootNode.left];

						switch (type)
						{
							case ReturnType.Number:
								{
									float leftNode = ComputeNumber(rootNode.left, model);
									float rightNode = ComputeNumber(rootNode.right, model);

									return leftNode == rightNode;
								}
							case ReturnType.Bool:
								{
									bool leftNode = ComputeBool(rootNode.left, model);
									bool rightNode = ComputeBool(rootNode.right, model);

									return leftNode == rightNode;
								}
							default:
								throw new Exception($"Equals does not support type {type}");
						}
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