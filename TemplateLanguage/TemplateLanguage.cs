﻿using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace TemplateLanguage
{
	class ComputeResult
	{
		public static ComputeResult OK => new ComputeResult(true);

		public bool Ok;
		public List<string> Errors;
		public List<int> Lines;

		public ComputeResult(bool ok)
		{
			Ok = ok;
			Errors = new List<string>();
			Lines = new List<int>();
		}

		public ComputeResult(bool ok, string error, [CallerLineNumber] int lineNumber = 0)
		{
			Ok = ok;
			Errors = new List<string>() { error };
			Lines = new List<int>() { lineNumber };
		}

		public ComputeResult(bool ok, List<string> errors, List<int> lines)
		{
			Ok = ok;
			Errors = errors;
			Lines = lines;
		}

		public static ComputeResult Combine(ComputeResult a, ComputeResult b)
		{
			List<string> errors = new List<string>();
			errors.AddRange(a.Errors);
			errors.AddRange(b.Errors);

			List<int> lines = new List<int>();
			lines.AddRange(a.Lines);
			lines.AddRange(b.Lines);

			return new ComputeResult(a.Ok && b.Ok, errors, lines);
		}

		public static ComputeResult Combine(ComputeResult a, ComputeResult b, ComputeResult c)
		{
			List<string> errors = new List<string>();
			errors.AddRange(a.Errors);
			errors.AddRange(b.Errors);
			errors.AddRange(c.Errors);

			List<int> lines = new List<int>();
			lines.AddRange(a.Lines);
			lines.AddRange(b.Lines);
			lines.AddRange(c.Lines);

			return new ComputeResult(a.Ok && b.Ok && c.Ok, errors, lines);
		}
	}

	delegate ComputeResult TemplateMethod(ref TemplateContext container, in Node root, StringBuilder sb, ModelStack stack);
	delegate ComputeResult TemplateMethod<T>(ref TemplateContext container, in Node root, StringBuilder sb, ModelStack stack, out T result);

	static class TemplateLanguageRules
	{
		static MethodContainer<NodeType, ReturnType, TemplateMethod> voideMethods = new()
		{
			// -------- Start/Bracket --------
			{ NodeType.Start, ReturnType.Any, ComputeRight },
			{ NodeType.Bracket, ReturnType.Any, ComputeRight },
			{ NodeType.End, ReturnType.Any, End },

			// ------ Blocks ------
			{ NodeType.TextBlock, ReturnType.Any, TextBlock },
			{ NodeType.NewLineBlock, ReturnType.Any, NewLineBlock },
			{ NodeType.CodeBlock, ReturnType.Any, ReturnType.Any, CodeBlock },
			{ NodeType.RepeatCodeBlock, ReturnType.Any, ReturnType.None | ReturnType.None, ComputeRight },
			{ NodeType.NewLine, ReturnType.Any, ReturnType.Any, NewLine },
			{ NodeType.AccessorBlock, ReturnType.Any, ReturnType.Unknown | ReturnType.Bool, ReturnType.Variable, AccessorBlock },
			{ NodeType.EnumerableAccessorBlock, ReturnType.Any, ReturnType.Unknown | ReturnType.Bool, ReturnType.Variable, EnumerableAccessorBlock },

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
			{ NodeType.Variable, ReturnType.String, Variable },
			{ NodeType.Accessor, ReturnType.String, ReturnType.Variable, Accessor },
		};

		static MethodContainer<NodeType, ReturnType, TemplateMethod<bool>> boolMethods = new()
		{
			// ------ Bool ------
			{ NodeType.Bool, Bool },

			// ------ Operators ------
			{ NodeType.Equals, ReturnType.Number | ReturnType.Bool | ReturnType.Variable | ReturnType.String, ReturnType.Number | ReturnType.Bool | ReturnType.Variable | ReturnType.String, Equals },
			{ NodeType.Greater, ReturnType.Number | ReturnType.Variable, ReturnType.Number | ReturnType.Variable, Greater },
			{ NodeType.Less, ReturnType.Number | ReturnType.Variable, ReturnType.Number | ReturnType.Variable, Less },
			{ NodeType.And, ReturnType.Bool | ReturnType.Variable, ReturnType.Bool | ReturnType.Variable, And },
			{ NodeType.Or, ReturnType.Bool | ReturnType.Variable, ReturnType.Bool | ReturnType.Variable, Or },

			// ------ Variable ------
			{ NodeType.Variable, ReturnType.String, Variable },
			{ NodeType.Accessor, ReturnType.String, ReturnType.Variable, Accessor },
		};

		static MethodContainer<NodeType, ReturnType, TemplateMethod<string>> strMethods = new()
		{
			// ------ String ------
			{ NodeType.String, String },

			// ------ Variable ------
			{ NodeType.Variable, ReturnType.String, Variable },
			{ NodeType.Accessor, ReturnType.String, ReturnType.Variable, Accessor },

			// ------ Operators ------
			{ NodeType.Add, ReturnType.String | ReturnType.Variable, ReturnType.String | ReturnType.Variable, Cat },
		};

		static MethodContainer<NodeType, ReturnType, TemplateMethod<IParameter>> variableMethods = new()
		{
			// ------ Variable ------
			{ NodeType.Variable, ReturnType.String, VariableName },
			{ NodeType.Accessor, ReturnType.String, ReturnType.Variable, Accessor },
		};

		static TemplateLanguageRules()
		{
			// Add brackets to the containers
			numberMethods.Add(NodeType.Bracket, ReturnType.Any, Bracket(numberMethods));
			boolMethods.Add(NodeType.Bracket, ReturnType.Any, Bracket(boolMethods));
			boolMethods.Add(NodeType.Filter, ReturnType.Any, Bracket(boolMethods));
			strMethods.Add(NodeType.Bracket, ReturnType.Any, Bracket(strMethods));
			variableMethods.Add(NodeType.Bracket, ReturnType.Any, Bracket(variableMethods));

			numberMethods.Add(NodeType.Conditional, ReturnType.Bool | ReturnType.Variable, ReturnType.Number, Conditional(numberMethods));
			boolMethods.Add(NodeType.Conditional, ReturnType.Bool | ReturnType.Variable, ReturnType.Bool, Conditional(boolMethods));
			strMethods.Add(NodeType.Conditional, ReturnType.Bool | ReturnType.Variable, ReturnType.String, Conditional(strMethods));
		}

		public static ComputeResult Compute(ref TemplateContext context, int node, StringBuilder sb, ModelStack stack)
		{
			if (node == -1)
				return ComputeResult.OK;

			ref readonly Node rootNode = ref context.nodes[node];
			var cr = TryGetMethod(ref context, voideMethods, rootNode, out TemplateMethod method);

			if (!cr.Ok)
				return cr;

			return method(ref context, rootNode, sb, stack);
		}

		static ComputeResult TextBlock(ref TemplateContext context, in Node node, StringBuilder sb, ModelStack stack)
		{
            var result = Compute(ref context, node.right, sb, stack);
			sb.Append(node.token.GetSpan(context.txt).ToString());

			return result;
		}

		static ComputeResult NewLineBlock(ref TemplateContext context, in Node node, StringBuilder sb, ModelStack stack)
		{
            var result = Compute(ref context, node.right, sb, stack);
			sb.Append("\n");

			return result;
		}

		static ComputeResult CodeBlock(ref TemplateContext context, in Node node, StringBuilder sb, ModelStack stack)
		{
			var resultRight = Compute(ref context, node.right, sb, stack);
			var resultLeft = ComputeAny(ref context, node.left, sb, stack, appendResult: true);

			return ComputeResult.Combine(resultRight, resultLeft);
		}

		static ComputeResult AccessorBlock(ref TemplateContext context, in Node node, StringBuilder sb, ModelStack stack)
		{
			var resultLeft = Compute(ref context, node.left, sb, stack, variableMethods, out IParameter parameter);

			if (!resultLeft.Ok)
				return resultLeft;

			if (!parameter.TryGet(out IModel model))
				return new ComputeResult(false, "Variable cannot be made a model");

			ReturnType middleType = GetType(ref context, node.middle);
			if (middleType == ReturnType.Bool)
			{
				stack.Push(model);
				var resultMiddle = Compute(ref context, node.middle, sb, stack, boolMethods, out bool predicate);
				stack.Pop();

				if (!resultMiddle.Ok)
					return resultMiddle;

				if (!predicate)
					return ComputeResult.OK;
			}

			stack.Push(model);
			var resultRight = ComputeAny(ref context, node.right, sb, stack, appendResult: true);
			stack.Pop();

			return resultRight;
		}

		static ComputeResult EnumerableAccessorBlock(ref TemplateContext context, in Node node, StringBuilder sb, ModelStack stack)
		{
			var resultLeft = Compute(ref context, node.left, sb, stack, variableMethods, out IParameter parameter);

			if (!resultLeft.Ok)
				return resultLeft;

			if (!parameter.TryGet(out IEnumerable<IModel> models))
				return new ComputeResult(false, "Variable is not an enumerable model");

			IModel last = models.Last();
            foreach (IModel model in models)
			{
                ReturnType middleType = GetType(ref context, node.middle);
				if (middleType == ReturnType.Bool)
				{
					stack.Push(model);
					var resultMiddle = Compute(ref context, node.middle, sb, stack, boolMethods, out bool predicate);
					stack.Pop();

                    if (!resultMiddle.Ok)
						return resultMiddle;

					if (!predicate)
						continue;
				}

				stack.Push(model);

				var resultRight = ComputeAny(ref context, node.right, sb, stack, appendResult: true);
                if (model != last)
				{
					// TODO: Improve
                    ref readonly Node bracketNode = ref context.nodes[node.right];
                    ref readonly Node rightRightNode = ref context.nodes[bracketNode.right];
					var resultSeparator = ComputeAny(ref context, rightRightNode.left, sb, stack, appendResult: true);

					if (!resultSeparator.Ok)
						return resultSeparator;
				}

				stack.Pop();

                if (!resultRight.Ok)
					return resultRight;
			}

			return ComputeResult.OK;
		}

		static ComputeResult NewLine(ref TemplateContext context, in Node node, StringBuilder sb, ModelStack stack)
		{
			var resultRight = ComputeAny(ref context, node.left, sb, stack);
			var resultLeft = ComputeAny(ref context, node.right, sb, stack);

			return ComputeResult.Combine(resultRight, resultLeft);
		}

		static ComputeResult Assign(ref TemplateContext context, in Node node, StringBuilder sb, ModelStack stack)
		{
			ReturnType rightType = GetType(ref context, node.right);
			ReturnType leftType = GetType(ref context, node.left);

			if (rightType.HasFlag(ReturnType.Variable))
			{
				var rightResult = Compute(ref context, node.right, sb, stack, variableMethods, out IParameter parameter);

				if (!rightResult.Ok)
					return rightResult;

				rightType = parameter.GetType();
			}

			if (!leftType.HasFlag(ReturnType.Variable))
				return new ComputeResult(false, $"Cannot assign to type {leftType}");

			var varResult = Compute(ref context, node.left, sb, stack, variableMethods, out IParameter var);
			if (!varResult.Ok)
			{
				ref readonly Node varNode = ref context.nodes[node.left];
				ref readonly Node varNameNode = ref context.nodes[varNode.right];
				Compute(ref context, varNameNode.right, sb, stack, strMethods, out string varName);

				switch (rightType)
				{
					case ReturnType.Number:
						var = new Parameter<float>(0);
						break;
					case ReturnType.Bool:
						var = new Parameter<bool>(false);
						break;
					case ReturnType.String:
						var = new Parameter<string>("");
						break;
					default:
						return new ComputeResult(false, $"Assign does not support {rightType}");
				}

                stack.PeekBottom().Set(varName.AsSpan(), var);
			}

			if (var.GetType() != rightType)
				return new ComputeResult(false, $"Cannot assign to type {rightType} to variable of type {var.GetType()}");

			ComputeResult leftResult;
			switch (rightType)
			{
				case ReturnType.Number:
					{
						leftResult = Compute(ref context, node.right, sb, stack, numberMethods, out float val);
						var.TrySet(val);
					}
					break;
				case ReturnType.Bool:
					{
						leftResult = Compute(ref context, node.right, sb, stack, boolMethods, out bool val);
						var.TrySet(val);
					}
					break;
				case ReturnType.String:
					{
						leftResult = Compute(ref context, node.right, sb, stack, strMethods, out string val);
						var.TrySet(val);
					}
					break;
				default:
					return new ComputeResult(false, $"Assign does not support {rightType}");
			}

			return leftResult;
		}

		static ComputeResult Cat(ref TemplateContext context, in Node node, StringBuilder sb, ModelStack stack, out string result)
		{
			var resultRight = Compute(ref context, node.right, sb, stack, strMethods, out string right);
			var resultLeft = Compute(ref context, node.left, sb, stack, strMethods, out string left);

			result = left + right;
			return ComputeResult.Combine(resultRight, resultLeft);
		}

		static ComputeResult ComputeRight(ref TemplateContext context, in Node node, StringBuilder sb, ModelStack stack)
		 => Compute(ref context, node.right, sb, stack);

		static ComputeResult End(ref TemplateContext context, in Node nodeIdx, StringBuilder sb, ModelStack stack)
		{
			return ComputeResult.OK;
		}

		static ComputeResult Float(ref TemplateContext context, in Node node, StringBuilder sb, ModelStack stack, out float result)
		{
#if NETSTANDARD2_0
			bool r = float.TryParse(node.token.GetSpan(context.txt).ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out result);
#else
			bool r = float.TryParse(node.token.GetSpan(context.txt), System.Globalization.CultureInfo.InvariantCulture, out result);
#endif
			
			if (!r)
				return new ComputeResult(r, $"Cannot parse {node.token.GetSpan(context.txt).ToString()} as a float");

			return ComputeResult.OK;
		}

		static ComputeResult Integer(ref TemplateContext context, in Node node, StringBuilder sb, ModelStack stack, out float result)
		{
#if NETSTANDARD2_0
			bool r = int.TryParse(node.token.GetSpan(context.txt).ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out int integer);
#else
			bool r = int.TryParse(node.token.GetSpan(context.txt), System.Globalization.CultureInfo.InvariantCulture, out int integer);
#endif
			result = integer;

			if (!r)
				return new ComputeResult(r, $"Cannot parse {node.token.GetSpan(context.txt).ToString()} as an integer");

			return ComputeResult.OK;
		}

		static ComputeResult Bool(ref TemplateContext context, in Node node, StringBuilder sb, ModelStack stack, out bool result)
		{
			bool r = bool.TryParse(node.token.GetSpan(context.txt).ToString(), out result);

			if (!r)
				return new ComputeResult(r, $"Cannot parse {node.token.GetSpan(context.txt).ToString()} as a bool");

			return ComputeResult.OK;
		}

		static ComputeResult String(ref TemplateContext context, in Node node, StringBuilder sb, ModelStack stack, out string result)
		{
			result = node.token.GetSpan(context.txt).ToString();
			return ComputeResult.OK;
		}

		static ComputeResult Add(ref TemplateContext context, in Node node, StringBuilder sb, ModelStack stack, out float result)
		{
			var resultRight = Compute(ref context, node.right, sb, stack, numberMethods, out float right);
			var resultLeft = Compute(ref context, node.left, sb, stack, numberMethods, out float left);

			result = left + right;
			return ComputeResult.Combine(resultRight, resultLeft);
		}

		static ComputeResult Subtract(ref TemplateContext context, in Node node, StringBuilder sb, ModelStack stack, out float result)
		{
			var resultRight = Compute(ref context, node.right, sb, stack, numberMethods, out float right);
			var resultLeft = Compute(ref context, node.left, sb, stack, numberMethods, out float left);

			result = left - right;
			return ComputeResult.Combine(resultRight, resultLeft);
		}

		static ComputeResult Multiply(ref TemplateContext context, in Node node, StringBuilder sb, ModelStack stack, out float result)
		{
			var resultRight = Compute(ref context, node.right, sb, stack, numberMethods, out float right);
			var resultLeft = Compute(ref context, node.left, sb, stack, numberMethods, out float left);

			result = left * right;
			return ComputeResult.Combine(resultRight, resultLeft);
		}

		static ComputeResult Divide(ref TemplateContext context, in Node node, StringBuilder sb, ModelStack stack, out float result)
		{
			var resultRight = Compute(ref context, node.right, sb, stack, numberMethods, out float right);
			var resultLeft = Compute(ref context, node.left, sb, stack, numberMethods, out float left);

			result = left / right;
			return ComputeResult.Combine(resultRight, resultLeft);
		}

		static ComputeResult VariableName(ref TemplateContext context, in Node node, StringBuilder sb, ModelStack stack, out IParameter result)
		{
			var resultRight = Compute(ref context, node.right, sb, stack, strMethods, out string varName);

			if (!stack.TryGet(varName.AsSpan(), out result))
				return new ComputeResult(false, $"Variable with name {varName} does not exist");

			return resultRight;
		}

		static ComputeResult Variable<T>(ref TemplateContext context, in Node node, StringBuilder sb, ModelStack stack, out T result)
		{
			var resultRight = Compute(ref context, node.right, sb, stack, strMethods, out string varName);
			if (!resultRight.Ok)
			{
				result = default;
				return resultRight;
			}

			if (!stack.TryGet(varName.AsSpan(), out IParameter var))
			{
				result = default;
				return new ComputeResult(false, $"Variable with name {varName} does not exist");
			}

			if (!var.TryGet(out result))
				return new ComputeResult(false, $"Variable of type {varName} cannot be accessed as {typeof(T).Name}");

			return ComputeResult.OK;
		}

		static ComputeResult Accessor(ref TemplateContext context, in Node node, StringBuilder sb, ModelStack stack, out IParameter result)
		{
			var resultRight = Compute(ref context, node.right, sb, stack, strMethods, out string propName);
			var resultLeft = Compute(ref context, node.left, sb, stack, variableMethods, out IParameter var);

			if (!var.TryGet(propName.AsSpan(), out result))
				return new ComputeResult(false, $"Cannot access prop {propName}");
			
			return ComputeResult.Combine(resultRight, resultLeft);
		}

		static ComputeResult Accessor<T>(ref TemplateContext context, in Node node, StringBuilder sb, ModelStack stack, out T result)
		{
			var resultRight = Compute(ref context, node.right, sb, stack, strMethods, out string propName);
			var resultLeft = Compute(ref context, node.left, sb, stack, variableMethods, out IParameter var);

			if (!var.TryGet(propName.AsSpan(), out IParameter param))
			{
				result = default;
				return new ComputeResult(false, $"Cannot access prop {propName}");
			}

			if (!param.TryGet(out result))
				return new ComputeResult(false, $"Variable cannot be accessed as {nameof(T)}");

			return ComputeResult.Combine(resultRight, resultLeft);
		}

		static ComputeResult ResolveVariableType(ref TemplateContext context, int nodeIdx, StringBuilder sb, ModelStack stack, out ReturnType result)
		{
			result = GetType(ref context, nodeIdx);

			if (result.HasFlag(ReturnType.Variable))
			{
				var r = Compute(ref context, nodeIdx, sb, stack, variableMethods, out IParameter parameter);

				if (!r.Ok)
					return r;

				result = parameter.GetType();
			}

			return ComputeResult.OK;
		}

		static ComputeResult Equals(ref TemplateContext context, in Node node, StringBuilder sb, ModelStack stack, out bool result)
		{
			var resultRightType = ResolveVariableType(ref context, node.right, sb, stack, out ReturnType rightType);
			var resultLeftType = ResolveVariableType(ref context, node.right, sb, stack, out ReturnType leftType);

			if (!resultRightType.Ok || !resultLeftType.Ok)
			{
				result = false;
				return ComputeResult.Combine(resultRightType, resultLeftType);
			}

			ComputeResult resultRight, resultLeft;
			if (rightType == ReturnType.Number && leftType == ReturnType.Number)
			{
				resultRight = Compute(ref context, node.right, sb, stack, numberMethods, out float right);
				resultLeft = Compute(ref context, node.left, sb, stack, numberMethods, out float left);

				result = left == right;
			}
			else if (rightType == ReturnType.Bool && leftType == ReturnType.Bool)
			{
				resultRight = Compute(ref context, node.right, sb, stack, boolMethods, out bool right);
				resultLeft = Compute(ref context, node.left, sb, stack, boolMethods, out bool left);

				result = left == right;
			}
			else if (rightType == ReturnType.String && leftType == ReturnType.String)
			{
				resultRight = Compute(ref context, node.right, sb, stack, strMethods, out string right);
				resultLeft = Compute(ref context, node.left, sb, stack, strMethods, out string left);

				result = left == right;
			}
			else
			{
				result = false;
				return new ComputeResult(false, $"Cannot compare types {rightType} and {leftType}");
			}

			return ComputeResult.Combine(resultRight, resultLeft);
		}

		static ComputeResult Greater(ref TemplateContext context, in Node node, StringBuilder sb, ModelStack stack, out bool result)
		{
			var resultRightType = ResolveVariableType(ref context, node.right, sb, stack, out ReturnType rightType);
			var resultLeftType = ResolveVariableType(ref context, node.right, sb, stack, out ReturnType leftType);

			if (!resultRightType.Ok || !resultLeftType.Ok)
			{
				result = false;
				return ComputeResult.Combine(resultRightType, resultLeftType);
			}

			ComputeResult resultRight, resultLeft;
			if (rightType == ReturnType.Number && leftType == ReturnType.Number)
			{
				resultRight = Compute(ref context, node.right, sb, stack, numberMethods, out float right);
				resultLeft = Compute(ref context, node.left, sb, stack, numberMethods, out float left);

				result = left > right;
			}
			else
			{
				result = false;
				return new ComputeResult(false, $"Cannot compare types {rightType} and {leftType}");
			}

			return ComputeResult.Combine(resultRight, resultLeft);
		}

		static ComputeResult Less(ref TemplateContext context, in Node node, StringBuilder sb, ModelStack stack, out bool result)
		{
			var resultRightType = ResolveVariableType(ref context, node.right, sb, stack, out ReturnType rightType);
			var resultLeftType = ResolveVariableType(ref context, node.right, sb, stack, out ReturnType leftType);

			if (!resultRightType.Ok || !resultLeftType.Ok)
			{
				result = false;
				return ComputeResult.Combine(resultRightType, resultLeftType);
			}

			ComputeResult resultRight, resultLeft;
			if (rightType == ReturnType.Number && leftType == ReturnType.Number)
			{
				resultRight = Compute(ref context, node.right, sb, stack, numberMethods, out float right);
				resultLeft = Compute(ref context, node.left, sb, stack, numberMethods, out float left);

				result = left < right;
			}
			else
			{
				result = false;
				return new ComputeResult(false, $"Cannot compare types {rightType} and {leftType}");
			}

			return ComputeResult.Combine(resultRight, resultLeft);
		}

		static ComputeResult And(ref TemplateContext context, in Node node, StringBuilder sb, ModelStack stack, out bool result)
		{
			var resultRightType = ResolveVariableType(ref context, node.right, sb, stack, out ReturnType rightType);
			var resultLeftType = ResolveVariableType(ref context, node.right, sb, stack, out ReturnType leftType);

			if (!resultRightType.Ok || !resultLeftType.Ok)
			{
				result = false;
				return ComputeResult.Combine(resultRightType, resultLeftType);
			}

			ComputeResult resultRight, resultLeft;
			if (rightType == ReturnType.Bool && leftType == ReturnType.Bool)
			{
				resultRight = Compute(ref context, node.right, sb, stack, boolMethods, out bool right);
				resultLeft = Compute(ref context, node.left, sb, stack, boolMethods, out bool left);

				result = left && right;
			}
			else
			{
				result = false;
				return new ComputeResult(false, $"Cannot compare types {rightType} and {leftType}");
			}

			return ComputeResult.Combine(resultRight, resultLeft);
		}

		static ComputeResult Or(ref TemplateContext context, in Node node, StringBuilder sb, ModelStack stack, out bool result)
		{
			var resultRightType = ResolveVariableType(ref context, node.right, sb, stack, out ReturnType rightType);
			var resultLeftType = ResolveVariableType(ref context, node.right, sb, stack, out ReturnType leftType);

			if (!resultRightType.Ok || !resultLeftType.Ok)
			{
				result = false;
				return ComputeResult.Combine(resultRightType, resultLeftType);
			}

			ComputeResult resultRight, resultLeft;
			if (rightType == ReturnType.Bool && leftType == ReturnType.Bool)
			{
				resultRight = Compute(ref context, node.right, sb, stack, boolMethods, out bool right);
				resultLeft = Compute(ref context, node.left, sb, stack, boolMethods, out bool left);

				result = left || right;
			}
			else
			{
				result = false;
				return new ComputeResult(false, $"Cannot compare types {rightType} and {leftType}");
			}

			return ComputeResult.Combine(resultRight, resultLeft);
		}

		static ComputeResult If(ref TemplateContext context, in Node node, StringBuilder sb, ModelStack stack)
		{
			var cr = Compute(ref context, node.left, sb, stack, boolMethods, out bool condition);
			if (!cr.Ok)
				return cr;

			if (condition)
				return ComputeAny(ref context, node.middle, sb, stack, true);
			else
				return ComputeAny(ref context, node.right, sb, stack, true);
		}

		static TemplateMethod<T> Conditional<T>(MethodContainer<NodeType, ReturnType, TemplateMethod<T>> container)
		{
			return (ref TemplateContext context, in Node node, StringBuilder sb, ModelStack stack, out T result) =>
			{
				var cr = Compute(ref context, node.right, sb, stack, boolMethods, out bool condition);
				if (!cr.Ok)
				{
					result = default;
					return cr;
				}

				if (condition)
					return Compute(ref context, node.left, sb, stack, container, out result);

				result = default;
				return ComputeResult.OK;
			};
		}

		static TemplateMethod<T> Bracket<T>(MethodContainer<NodeType, ReturnType, TemplateMethod<T>> container)
		{
			return (ref TemplateContext context, in Node nodeIdx, StringBuilder sb, ModelStack stack, out T result) =>
			{
				return Compute(ref context, nodeIdx.right, sb, stack, container, out result);
			};
		}

		public static ComputeResult ComputeAny(ref TemplateContext context, int nodeIdx, StringBuilder sb, ModelStack stack, bool appendResult = false)
		{
			ReturnType type = GetType(ref context, nodeIdx);

			if (type.HasFlag(ReturnType.Variable))
			{
				ref readonly Node node = ref context.nodes[nodeIdx];
				var r = Compute(ref context, node.right, sb, stack, variableMethods, out IParameter parameter);

				if (!r.Ok)
					return r;

				type = parameter.GetType();
			}

			ComputeResult result;
			switch (type)
			{
				case ReturnType.Number:
					{
						result = Compute(ref context, nodeIdx, sb, stack, numberMethods, out float value);

						if (appendResult)
							sb.Append(value);
					}
					break;
				case ReturnType.Bool:
					{
						result = Compute(ref context, nodeIdx, sb, stack, boolMethods, out bool value);

						if (appendResult)
							sb.Append(value);
					}
					break;
				case ReturnType.String:
					{
						result = Compute(ref context, nodeIdx, sb, stack, strMethods, out string value);

						if (appendResult)
							sb.Append(value);
					}
					break;
				default:
					result = Compute(ref context, nodeIdx, sb, stack);
					break;
			}

			return result;
		}

		public static ComputeResult Compute<T>(ref TemplateContext context, int node, StringBuilder sb, ModelStack stack, MethodContainer<NodeType, ReturnType, TemplateMethod<T>> container, out T result)
		{
			ref readonly Node rootNode = ref context.nodes[node];
			var cr = TryGetMethod(ref context, container, rootNode, out TemplateMethod<T> method);

			if (!cr.Ok)
			{
				result = default;
				return cr;
			}

			return method(ref context, rootNode, sb, stack, out result);
		}

		static ReturnType GetType(ref TemplateContext context, int idx)
		{
			if (idx == -1)
				return ReturnType.Unknown;

			return context.returnTypes[idx];
		}

		static ComputeResult TryGetMethod<T>(ref TemplateContext context, MethodContainer<NodeType, ReturnType, T> methodContainer, in Node node, out T method)
		{
			ReturnType rightType = GetType(ref context, node.right);
			ReturnType middleType = GetType(ref context, node.middle);
			ReturnType leftType = GetType(ref context, node.left);

			if (!methodContainer.TryGet(node.nodeType, rightType, middleType, leftType, out method))
			{
				throw new Exception($"Node not found, Type: {node.nodeType}, Right: {rightType}, Middle: {middleType}, Left: {leftType}");
			}

			return ComputeResult.OK;
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

		public ComputeResult Compute(int root, StringBuilder sb, ModelStack stack)
			=> TemplateLanguageRules.Compute(ref container, root, sb, stack);
	}
}