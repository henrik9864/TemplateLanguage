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
			{ NodeType.Start, ReturnType.Any, Start },
			{ NodeType.Bracket, ReturnType.Any, Start },
			{ NodeType.End, ReturnType.Any, End },

			// ------ Blocks ------
			{ NodeType.TextBlock, ReturnType.Any, TextBlock },
			{ NodeType.CodeBlock, ReturnType.Any, ReturnType.Any, CodeBlock },
			{ NodeType.VariableBlock, ReturnType.Any, ReturnType.Any, VariableBlock },
			{ NodeType.NewLine, ReturnType.Any, ReturnType.Any, NewLine },

			// ------ Operators ------
			{ NodeType.If, ReturnType.Any, ReturnType.Any, ReturnType.Bool | ReturnType.Variable, If },
			{ NodeType.Assign, ReturnType.Any, ReturnType.Any, Assign },
		};

		static MethodContainer<NodeType, ReturnType, TemplateMethod<float>> numberMethods = new()
		{
			// ------ Number ------
			{ NodeType.Float, Float },
			{ NodeType.Integer, Integer },

			// ------ Operators ------
			{ NodeType.Add, ReturnType.Number | ReturnType.Variable, ReturnType.Number | ReturnType.Variable, Add },
			{ NodeType.Subtract, ReturnType.Number | ReturnType.Variable, ReturnType.Number | ReturnType.Variable, Subtract },
			{ NodeType.Multiply, ReturnType.Number | ReturnType.Variable, ReturnType.Number | ReturnType.Variable, Multiply },
			{ NodeType.Divide, ReturnType.Number | ReturnType.Variable, ReturnType.Number | ReturnType.Variable, Divide },
			
			// ------ Variable ------
			{ NodeType.Variable, ReturnType.String, VariableNumber },
		};

		static MethodContainer<NodeType, ReturnType, TemplateMethod<bool>> boolMethods = new()
		{
			// ------ Bool ------
			{ NodeType.Bool, Bool },

			// ------ Operators ------
			{ NodeType.Equals, ReturnType.Number | ReturnType.Bool | ReturnType.Variable, ReturnType.Number | ReturnType.Bool | ReturnType.Variable, Equals },

			// ------ Variable ------
			{ NodeType.Variable, ReturnType.String, VariableBool },
		};

		static MethodContainer<NodeType, ReturnType, TemplateMethod<string>> strMethods = new()
		{
			// ------ String ------
			{ NodeType.String, String },

			// ------ Variable ------
			{ NodeType.Variable, ReturnType.String, VariableString },

			// ------ Operators ------
			{ NodeType.Add, ReturnType.String | ReturnType.Variable, ReturnType.String | ReturnType.Variable, Cat },
		};

		static MethodContainer<NodeType, ReturnType, TemplateMethod<string>> varNameMethods = new()
		{
			// ------ String ------
			{ NodeType.String, String },

			// ------ Variable ------
			{ NodeType.Variable, ReturnType.String, VariableName },
		};

		static TemplateLanguageRules()
		{
			// Add brackets to the containers
			numberMethods.Add(NodeType.Bracket, ReturnType.Any, ReturnType.Unknown, ReturnType.Unknown, Bracket(numberMethods));
			boolMethods.Add(NodeType.Bracket, ReturnType.Any, ReturnType.Unknown, ReturnType.Unknown, Bracket(boolMethods));
			strMethods.Add(NodeType.Bracket, ReturnType.Any, ReturnType.Unknown, ReturnType.Unknown, Bracket(strMethods));
			varNameMethods.Add(NodeType.Bracket, ReturnType.Any, ReturnType.Unknown, ReturnType.Unknown, Bracket(varNameMethods));
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
			ComputeAny(ref context, node.left, sb, model, appendResult: true);
		}

		static void VariableBlock(ref TemplateContext context, int nodeIdx, StringBuilder sb, IModel model)
		{
			ref readonly Node node = ref context.nodes[nodeIdx];
			Compute(ref context, node.right, sb, model);

			var parameter = model[node.token.GetSpan(context.txt)];
			switch (parameter.GetType())
			{
				case ReturnType.Number:
					sb.Append(parameter.GetFloat());
					break;
				case ReturnType.Bool:
					sb.Append(parameter.GetBool());
					break;
				case ReturnType.String:
					sb.Append(parameter.GetString());
					break;
				default:
					throw new Exception("Unsupported type");
			}
		}

		static void NewLine(ref TemplateContext context, int nodeIdx, StringBuilder sb, IModel model)
		{
			ref readonly Node node = ref context.nodes[nodeIdx];

			ComputeAny(ref context, node.left, sb, model);
			ComputeAny(ref context, node.right, sb, model);
		}

		static void Assign(ref TemplateContext context, int nodeIdx, StringBuilder sb, IModel model)
		{
			ref readonly Node node = ref context.nodes[nodeIdx];

			ReturnType rightType = GetType(ref context, node.right);
			ReturnType leftType = GetType(ref context, node.left);

			if (rightType.HasFlag(ReturnType.Variable))
				rightType = model[Compute(ref context, node.right, sb, model, varNameMethods)].GetType();

			if (!leftType.HasFlag(ReturnType.Variable))
				throw new Exception($"Cannot assign to type {leftType}");

			string varName = Compute(ref context, node.left, sb, model, varNameMethods);

			if (model.TryGet(varName, out IParameter parameter) && parameter.GetType() != rightType)
				throw new Exception($"Cannot assign to type {rightType} to variable of type {parameter}");

			switch (rightType)
			{
				case ReturnType.Number:
					{
						float val = Compute(ref context, node.right, sb, model, numberMethods);
						model.Set(varName, new Parameter<float>(val));
					}
					break;
				case ReturnType.Bool:
					{
						bool val = Compute(ref context, node.right, sb, model, boolMethods);
						model.Set(varName, new Parameter<bool>(val));
					}
					break;
				case ReturnType.String:
					{
						string val = Compute(ref context, node.right, sb, model, strMethods);
						model.Set(varName, new Parameter<string>(val));
					}
					break;
				case ReturnType.Variable:
					{
						string val = Compute(ref context, node.right, sb, model, varNameMethods);
						model.Set(varName, model[val]);
					}
					break;
				default:
					throw new Exception($"Assign does not support {rightType}");
			}
		}

		static string Cat(ref TemplateContext context, int nodeIdx, StringBuilder sb, IModel model)
		{
			ref readonly Node node = ref context.nodes[nodeIdx];
			return Compute(ref context, node.right, sb, model, strMethods) + Compute(ref context, node.left, sb, model, strMethods);
		}

		static void Start(ref TemplateContext context, int nodeIdx, StringBuilder sb, IModel model)
		{
			ref readonly Node node = ref context.nodes[nodeIdx];
			Compute(ref context, node.right, sb, model);
		}

		static void End(ref TemplateContext context, int nodeIdx, StringBuilder sb, IModel model)
		{
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

		static string String(ref TemplateContext context, int nodeIdx, StringBuilder sb, IModel model)
		{
			return context.nodes[nodeIdx].token.GetSpan(context.txt).ToString();
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

		static string VariableName(ref TemplateContext context, int nodeIdx, StringBuilder sb, IModel model)
		{
			ref readonly Node node = ref context.nodes[nodeIdx];
			return Compute(ref context, node.right, sb, model, strMethods);
		}

		static string VariableString(ref TemplateContext context, int nodeIdx, StringBuilder sb, IModel model)
		{
			ref readonly Node node = ref context.nodes[nodeIdx];
			string varName = Compute(ref context, node.right, sb, model, strMethods);

			return model[varName].GetString();
		}

		static float VariableNumber(ref TemplateContext context, int nodeIdx, StringBuilder sb, IModel model)
		{
			ref readonly Node node = ref context.nodes[nodeIdx];
			string varName = Compute(ref context, node.right, sb, model, strMethods);

			return model[varName].GetFloat();
		}

		static bool VariableBool(ref TemplateContext context, int nodeIdx, StringBuilder sb, IModel model)
		{
			ref readonly Node node = ref context.nodes[nodeIdx];
			string varName = Compute(ref context, node.right, sb, model, strMethods);

			return model[varName].GetBool();
		}

		static bool Equals(ref TemplateContext context, int nodeIdx, StringBuilder sb, IModel model)
		{
			ref readonly Node node = ref context.nodes[nodeIdx];

			ReturnType rightType = GetType(ref context, node.right);
			ReturnType leftType = GetType(ref context, node.left);

			if (rightType.HasFlag(ReturnType.Variable))
			{
				rightType = model[Compute(ref context, node.right, sb, model, varNameMethods)].GetType();
			}

			if (leftType.HasFlag(ReturnType.Variable))
			{
				leftType = model[Compute(ref context, node.left, sb, model, varNameMethods)].GetType();
			}

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

			if (type.HasFlag(ReturnType.Variable))
			{
				ref readonly Node node = ref context.nodes[nodeIdx];
				type = model[Compute(ref context, node.right, sb, model, varNameMethods)].GetType();
			}

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
					{
						string value = Compute(ref context, nodeIdx, sb, model, strMethods);

						if (appendResult)
							sb.Append(value);
					}
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

	internal ref struct TemplateLanguage
	{
		TemplateContext container;

		public TemplateLanguage(ReadOnlySpan<char> txt, ReadOnlySpan<Node> nodes, ReadOnlySpan<ReturnType> returnTypes)
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
}