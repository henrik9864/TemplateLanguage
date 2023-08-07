using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using LightLexer;

namespace TemplateLanguage.Test
{
	[TestClass]
	public class TemplateLanguageTest
	{
		TemplateRules templateRules = new TemplateRules();
		Model model;
		ModelStack stack;

		[TestInitialize]
		public void Setup()
		{
			IModel[] models = new IModel[10];
			for (int i = 0; i < models.Length; i++)
			{
				models[i] = new Model();
				models[i].Set("i", new Parameter<float>(i));
			}

			var model2 = new Model();
			model2.Set("shaba", new Parameter<string>("wow"));
			model2.Set("shaba2", new Parameter<float>(25));

			model = new Model();
			model.Set("testVar", new Parameter<float>(6));
			model.Set("testVar2", new Parameter<string>("slfgh"));
			model.Set("testVar3", new Parameter<bool>(false));
			model.Set("testVar4", new Parameter<float>(7));
			model.Set("vari", new ModelParameter(model2));
			model.Set("en", new EnumerableParameter<IModel>(models));

			stack = new ModelStack();
			stack.Push(model);
		}

		[TestMethod]
		public void TestString()
		{
			Assert.AreEqual("", RunLanguage("| |", model));
			Assert.AreEqual("2+3", RunLanguage("2+3", model));
			Assert.AreEqual("if true then 1 end", RunLanguage("if true then 1 end", model));
			Assert.AreEqual("lmao 19.5123", RunLanguage("lmao |3*(4+2.5)|123", model));
			Assert.AreEqual("()*/+ 3123", RunLanguage("()*/+ |if 3*(4+2.5)==3*(4+2.5) then 1+2 end|123", model));
		}

		[TestMethod]
		public void TestNewLine()
		{
			Assert.AreEqual("2", RunLanguage("|2|", model));
			Assert.AreEqual("", RunLanguage("|\n|", model));
			Assert.AreEqual("", RunLanguage("|\n\n2\n\n|", model));
			Assert.AreEqual("tes\ng", RunLanguage("tes\n|\n\n2\n\n|g", model));
		}

		[TestMethod]
		public void TestMath()
		{
			Assert.AreEqual("5", RunLanguage("|2+3|", model));
			Assert.AreEqual("5.5", RunLanguage("|2+3.5|", model));
			Assert.AreEqual("12", RunLanguage("|3*4|", model));
			Assert.AreEqual("12", RunLanguage("|(3*4)|", model));
			Assert.AreEqual("14", RunLanguage("|2+3*4|", model));
			Assert.AreEqual("14", RunLanguage("|3*4+2|", model));
			Assert.AreEqual("18", RunLanguage("|3*(4+2)|", model));
			Assert.AreEqual("19.5", RunLanguage("|3*(4+2.5)|", model));
			Assert.AreEqual("24.5", RunLanguage("|3*(4+2.5)+5|", model));
		}

		[TestMethod]
		public void TestEquals()
		{
			Assert.AreEqual("True", RunLanguage("|2==2|", model));
			Assert.AreEqual("False", RunLanguage("|3==2|", model));
			Assert.AreEqual("False", RunLanguage("|3*(4+2.5)==2|", model));
			Assert.AreEqual("True", RunLanguage("|3*(4+2.5)==3*(4+2.5)|", model));
			Assert.AreEqual("True", RunLanguage("|$testVar3==false|", model));
			Assert.AreEqual("False", RunLanguage("|true==$testVar3|", model));
			Assert.AreEqual("True", RunLanguage("|$testVar3==$testVar3|", model));
		}

		[TestMethod]
		public void TestIf()
		{
			Assert.AreEqual("1", RunLanguage("|if true then 1 else 2 end|", model));
			Assert.AreEqual("2", RunLanguage("|if false then 1 else 2 end|", model));
			Assert.AreEqual("3", RunLanguage("|if 2==2 then 1+2 end|", model));
			Assert.AreEqual("", RunLanguage("|if 3==2 then 1+2 end|", model));
			Assert.AreEqual("3", RunLanguage("|if 3*(4+2.5)==19.5 then 1+2 end|", model));
			Assert.AreEqual("3", RunLanguage("|if 3*(4+2.5)==3*(4+2.5) then 1+2else3*(4+2.5) end|", model));
			Assert.AreEqual("19.5", RunLanguage("|if 3*(4+2.5)==3*(4+2.5)+5 then 1+2else3*(4+2.5) end|", model));
			Assert.AreEqual("4", RunLanguage("|if true then \r\n\t$testVar=3\r\n\t$testVar=4\r\nend\r\n||$testVar|", model));
		}

		[TestMethod]
		public void TestVariable()
		{
			Assert.AreEqual("6", RunLanguage("|$testVar|", model));
			Assert.AreEqual("slfgh", RunLanguage("|$testVar2|", model));
			Assert.AreEqual("slfgh ", RunLanguage("$testVar2 ", model));
			Assert.AreEqual("False", RunLanguage("|$testVar3|", model));
			Assert.AreEqual("wow ", RunLanguage("$vari.shaba ", model));
			Assert.AreEqual("1", RunLanguage("|if $testVar==6 then 1 end|", model));
			Assert.AreEqual("1", RunLanguage("|if 6==$testVar then 1 end|", model));
			Assert.AreEqual("30", RunLanguage("|3*(4+$testVar)|", model));
			Assert.AreEqual("30", RunLanguage("|3*($testVar+4)|", model));
			Assert.AreEqual("36", RunLanguage("|3*($testVar+$testVar)|", model));
		}

		[TestMethod]
		public void TestAssign()
		{
			Assert.AreEqual("1 ", RunLanguage("|$a=1|$a ", model));
			Assert.AreEqual("", RunLanguage("|$testVar=6|", model));
			Assert.AreEqual("7 ", RunLanguage("|$testVar=$testVar4|$testVar ", model));
			Assert.AreEqual("7  5 ", RunLanguage("$testVar |$testVar=5| $testVar ", model));
			Assert.AreEqual("56", RunLanguage("|$testVar||$testVar=6||$testVar|", model));
		}

		[TestMethod]
		public void TestBlock()
		{
			Assert.AreEqual("wow", RunLanguage("$vari->$shaba<-", model));
			Assert.AreEqual("25", RunLanguage("$vari->$shaba2<-", model));
			Assert.AreEqual("", RunLanguage("$vari|$shaba2 < 5|->$shaba2<-", model));
			Assert.AreEqual("25", RunLanguage("$vari|$shaba2 > 5|->$shaba2<-", model));
			Assert.AreEqual("{wow}", RunLanguage("{$vari|$shaba2 > 5|->$shaba<-}", model));
			Assert.AreEqual("6789", RunLanguage("$en|$i > 5|~>$i<~", model));
			Assert.AreEqual("{6789}", RunLanguage("{$en|$i > 5|~>$i<~}", model));
			Assert.AreEqual("{6 7 8 9 }", RunLanguage("{$en|$i > 5|~>$i <~}", model));
		}

		[TestMethod]
		public void TestConditional()
		{
			Assert.AreEqual("wow", RunLanguage("|\"wow\" ? true|", model));
			Assert.AreEqual("", RunLanguage("|\"wow\" ? $testVar3|", model));
			Assert.AreEqual("14", RunLanguage("|2+3*4 ? true|", model));
		}

		public string RunLanguage(ReadOnlySpan<char> txt, IModel model)
		{
			var template = new ParsedTemplate(txt, templateRules.GetEnumerable(txt));

			var sb = new StringBuilder();
			template.RenderTo(sb, stack);

			return sb.ToString();
		}
	}
}