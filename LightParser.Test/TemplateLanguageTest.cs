using System.Buffers;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using LightLexer;
using LightParser;
using Newtonsoft.Json.Linq;
using Runner;

namespace TemplateLanguage.Test
{
	[TestClass]
	public class TemplateLanguageTest
	{
		static Dictionary<EngineState, IState<NodeType, EngineState>> stateDict = new()
		{
			{ EngineState.TextState,  new TextState() },
			{ EngineState.Expression, new ExpressionState() },
			{ EngineState.Code,       new CodeState() },
			{ EngineState.Variable,   new VariableState() },
		};

		TemplateRules templateRules = new TemplateRules();
		Model<ReturnType> model;
		ModelStack<ReturnType> stack;

		[TestInitialize]
		public void Setup()
		{
			IModel<ReturnType>[] models = new IModel<ReturnType>[10];
			for (int i = 0; i < models.Length; i++)
			{
				models[i] = new Model<ReturnType>();
				models[i].Set("i", new Parameter<float>(i));
			}

			var model2 = new Model<ReturnType>();
			model2.Set("shaba", new Parameter<string>("wow"));
			model2.Set("shaba2", new Parameter<float>(25));

			model = new Model<ReturnType>();
			model.Set("testVar", new Parameter<float>(6));
			model.Set("testVar2", new Parameter<string>("slfgh"));
			model.Set("testVar3", new Parameter<bool>(false));
			model.Set("testVar4", new Parameter<float>(7));
			model.Set("vari", new ModelParameter(model2));
			model.Set("en", new EnumerableParameter<IModel<ReturnType>>(models));

			stack = new ModelStack<ReturnType>();
			stack.Push(model);
		}

		[TestMethod]
		public void TestString()
		{
			Assert.AreEqual("", RunLanguage("| |"));
			Assert.AreEqual("2+3", RunLanguage("2+3"));
			Assert.AreEqual("if true then 1 end", RunLanguage("if true then 1 end"));
			Assert.AreEqual("lmao 19.5123", RunLanguage("lmao |3*(4+2.5)|123"));
			Assert.AreEqual("()+ 3123", RunLanguage("()+ |if 3*(4+2.5)==3*(4+2.5) then 1+2 end|123"));
		}

		[TestMethod]
		public void TestNewLine()
		{
			Assert.AreEqual("2", RunLanguage("|2|"));
			Assert.AreEqual("", RunLanguage("|\n|"));
			Assert.AreEqual("", RunLanguage("|\n\n2\n\n|"));
			Assert.AreEqual("tes\ng", RunLanguage("tes\n|\n\n2\n\n|g"));
		}

		[TestMethod]
		public void TestMath()
		{
			Assert.AreEqual("5", RunLanguage("|2+3|"));
			Assert.AreEqual("5.5", RunLanguage("|2+3.5|"));
			Assert.AreEqual("12", RunLanguage("|3*4|"));
			Assert.AreEqual("12", RunLanguage("|(3*4)|"));
			Assert.AreEqual("14", RunLanguage("|2+3*4|"));
			Assert.AreEqual("14", RunLanguage("|3*4+2|"));
			Assert.AreEqual("18", RunLanguage("|3*(4+2)|"));
			Assert.AreEqual("19.5", RunLanguage("|3*(4+2.5)|"));
			Assert.AreEqual("24.5", RunLanguage("|3*(4+2.5)+5|"));
		}

		[TestMethod]
		public void TestEquals()
		{
			Assert.AreEqual("True", RunLanguage("|2==2|"));
			Assert.AreEqual("False", RunLanguage("|3==2|"));
			Assert.AreEqual("False", RunLanguage("|3*(4+2.5)==2|"));
			Assert.AreEqual("True", RunLanguage("|3*(4+2.5)==3*(4+2.5)|"));
			Assert.AreEqual("True", RunLanguage("|$testVar3==false|"));
			Assert.AreEqual("False", RunLanguage("|true==$testVar3|"));
			Assert.AreEqual("True", RunLanguage("|$testVar3==$testVar3|"));
		}

		[TestMethod]
		public void TestIf()
		{
			Assert.AreEqual("1", RunLanguage("|if true then 1 else 2 end|"));
			Assert.AreEqual("2", RunLanguage("|if false then 1 else 2 end|"));
			Assert.AreEqual("3", RunLanguage("|if 2==2 then 1+2 end|"));
			Assert.AreEqual("", RunLanguage("|if 3==2 then 1+2 end|"));
			Assert.AreEqual("3", RunLanguage("|if 3*(4+2.5)==19.5 then 1+2 end|"));
			Assert.AreEqual("3", RunLanguage("|if 3*(4+2.5)==3*(4+2.5) then 1+2else3*(4+2.5) end|"));
			Assert.AreEqual("19.5", RunLanguage("|if 3*(4+2.5)==3*(4+2.5)+5 then 1+2else3*(4+2.5) end|"));
			Assert.AreEqual("4", RunLanguage("|if true then \r\n\t$testVar=3\r\n\t$testVar=4\r\nend\r\n||$testVar|"));
		}

		[TestMethod]
		public void TestVariable()
		{
			Assert.AreEqual("6", RunLanguage("|$testVar|"));
			Assert.AreEqual("slfgh", RunLanguage("|$testVar2|"));
			Assert.AreEqual("slfgh ", RunLanguage("$testVar2 "));
			Assert.AreEqual("False", RunLanguage("|$testVar3|"));
			Assert.AreEqual("wow ", RunLanguage("$vari.shaba "));
			Assert.AreEqual("1", RunLanguage("|if $testVar==6 then 1 end|"));
			Assert.AreEqual("1", RunLanguage("|if 6==$testVar then 1 end|"));
			Assert.AreEqual("30", RunLanguage("|3*(4+$testVar)|"));
			Assert.AreEqual("30", RunLanguage("|3*($testVar+4)|"));
			Assert.AreEqual("36", RunLanguage("|3*($testVar+$testVar)|"));
		}

		[TestMethod]
		public void TestAssign()
		{
			Assert.AreEqual("1 ", RunLanguage("|$a=1|$a "));
			Assert.AreEqual("", RunLanguage("|$testVar=6|"));
			Assert.AreEqual("7 ", RunLanguage("|$testVar=$testVar4|$testVar "));
			Assert.AreEqual("7  5 ", RunLanguage("$testVar |$testVar=5| $testVar "));
			Assert.AreEqual("56", RunLanguage("|$testVar||$testVar=6||$testVar|"));
		}

		[TestMethod]
		public void TestOperator()
		{
			Assert.AreEqual("False", RunLanguage("|$vari.shaba2 < 5|"));
			Assert.AreEqual("True", RunLanguage("|$vari.shaba2 > 5|"));
		}

		[TestMethod]
		public void TestBlock()
		{
			Assert.AreEqual("wow", RunLanguage("$vari->$shaba<-"));
			Assert.AreEqual("25", RunLanguage("$vari->$shaba2<-"));
			Assert.AreEqual("", RunLanguage("$vari|$shaba2 < 5|->$shaba2<-"));
			Assert.AreEqual("25", RunLanguage("$vari|$shaba2 > 5|->$shaba2<-"));
			Assert.AreEqual("{wow}", RunLanguage("{$vari|$shaba2 > 5|->$shaba<-}"));
			Assert.AreEqual("6789", RunLanguage("$en|$i > 5|~>$i<~"));
			Assert.AreEqual("{6789}", RunLanguage("{$en|$i > 5|~>$i<~}"));
			Assert.AreEqual("{6 7 8 9 }", RunLanguage("{$en|$i > 5|~>$i <~}"));
		}

		[TestMethod]
		public void TestConditional()
		{
			Assert.AreEqual("wow", RunLanguage("|\"wow\" ? true|"));
			Assert.AreEqual("", RunLanguage("|\"wow\" ? $testVar3|"));
			Assert.AreEqual("14", RunLanguage("|2+3*4 ? true|"));
		}

		public string RunLanguage(ReadOnlySpan<char> txt)
		{
			TokenEnumerable tokens = new TemplateRules().GetEnumerable(txt);

			Parser<NodeType, EngineState> parser = new(stateDict, tokens);
			TypeResolver<NodeType, ReturnType> resolver = new(TypeResolver.ResolveType);

			var nodeArr = ArrayPool<Node<NodeType>>.Shared.Rent(4096);
			var typeArr = ArrayPool<ReturnType>.Shared.Rent(4096);

			var ast = parser.GetAst(nodeArr.AsSpan());
			int start = ast.InsertNode(NodeType.Start);
			ast.SetRight(start);

			parser.CalculateAst(ref ast, EngineState.TextState);

			var types = resolver.ResolveTypes(ast.GetRoot(), ast.GetTree(), typeArr);

			TemplateContext<NodeType, ReturnType> context = new()
			{
				txt = txt,
				nodes = ast.GetTree(),
				returnTypes = typeArr
			};

			var sb = new StringBuilder();
			var result = TemplateLanguageRules.Compute(ref context, 0, sb, stack);

			if (!result.Ok)
				throw new Exception("Template language was not ok!");

			ArrayPool<Node<NodeType>>.Shared.Return(nodeArr);
			ArrayPool<ReturnType>.Shared.Return(typeArr);

			return sb.ToString();
		}
	}
}