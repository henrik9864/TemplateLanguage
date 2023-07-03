using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Tokhenizer;

namespace TemplateLanguage.Test
{
	[TestClass]
	public class TemplateLanguageTest
	{
		TemplateRules templateRules = new TemplateRules();
		TestModel model;

		[TestInitialize]
		public void Setup()
		{
			model = new TestModel();
			model.Set("testVar", 6);
		}

		[TestMethod]
		public void TestString()
		{
			Assert.AreEqual("", RunLanguage("||", model));
			Assert.AreEqual("2+3", RunLanguage("2+3", model));
			Assert.AreEqual("if true:1", RunLanguage("if true:1", model));
			Assert.AreEqual("lmao 19.5123", RunLanguage("lmao |3*(4+2.5)|123", model));
			Assert.AreEqual("()*/+ 3123", RunLanguage("()*/+ |if 3*(4+2.5)==3*(4+2.5):1+2|123", model));
		}

		[TestMethod]
		public void TestNewLine()
		{
			Assert.AreEqual("", RunLanguage("|\n|", model));
			Assert.AreEqual("2", RunLanguage("|\n\n2\n\n|", model));
			Assert.AreEqual("tes\n2g", RunLanguage("tes\n|\n\n2\n\n|g", model));
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
		}

		[TestMethod]
		public void TestIf()
		{
			Assert.AreEqual("1", RunLanguage("|if true:1else2|", model));
			Assert.AreEqual("2", RunLanguage("|if false:1else2|", model));
			Assert.AreEqual("3", RunLanguage("|if 2==2:1+2|", model));
			Assert.AreEqual("", RunLanguage("|if 3==2:1+2|", model));
			Assert.AreEqual("3", RunLanguage("|if 3*(4+2.5)==19.5:1+2|", model));
			Assert.AreEqual("3", RunLanguage("|if 3*(4+2.5)==3*(4+2.5):1+2else3*(4+2.5)|", model));
			Assert.AreEqual("19.5", RunLanguage("|if 3*(4+2.5)==3*(4+2.5)+5:1+2else3*(4+2.5)|", model));
		}

		[TestMethod]
		public void TestVariable()
		{
			Assert.AreEqual("6", RunLanguage("|$testVar|", model));
			Assert.AreEqual("1", RunLanguage("|if $testVar==6:1|", model));
			Assert.AreEqual("30", RunLanguage("|3*(4+$testVar)|", model));
		}

		[TestMethod]
		public void TestAssign()
		{
			Assert.AreEqual("", RunLanguage("|$testVar=6|", model));
			Assert.AreEqual("65", RunLanguage("|$testVar||$testVar=5||$testVar|", model));
		}

		public string RunLanguage(ReadOnlySpan<char> txt, IModel model)
		{
			var template = new ParsedTemplate(txt, templateRules.GetEnumerable(txt));

			var sb = new StringBuilder();
			template.RenderTo(sb, model);

			return sb.ToString();
		}
	}

	class TestModel : IModel
	{
		public ReadOnlySpan<char> this[ReadOnlySpan<char> name]
		{
			get
			{
				return data[string.GetHashCode(name)].Item1;
			}
		}

		Dictionary<int, (string, ReturnType)> data = new();

		public void Set(ReadOnlySpan<char> name, string value)
		{
			Set(name, value, ReturnType.String);
		}

		public void Set(ReadOnlySpan<char> name, float value)
		{
			Set(name, value.ToString(), ReturnType.Number);
		}

		public void Set(ReadOnlySpan<char> name, int value)
		{
			Set(name, value.ToString(), ReturnType.Number);
		}

		public void Set(ReadOnlySpan<char> name, bool value)
		{
			Set(name, value.ToString(), ReturnType.Bool);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Set(ReadOnlySpan<char> name, string var, ReturnType type)
		{
			if (!data.TryAdd(string.GetHashCode(name), (var, type)))
				data[string.GetHashCode(name)] = (var, type);
		}

		public ReturnType GetType(ReadOnlySpan<char> name)
		{
			return data[string.GetHashCode(name)].Item2;
		}
	}
}