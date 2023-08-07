using BenchmarkDotNet.Running;
using Runner;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using TemplateLanguage;
using LightLexer;

namespace Runner
{
	public static class Program
	{
		public static void Main()
		{
#if RELEASE
			BenchmarkRunner.Run<Perf>();
			return;
#endif


			//TemplateTest.Run();
			//return;
			/*
			*/

			/*
			IModel[] models = new IModel[10];
			for (int i = 0; i < models.Length; i++)
			{
				models[i] = new Model();
				models[i].Set("name", Parameter.Create("array"));
			}

			var model2 = new Model();
			model2.Set("shaba", new Parameter<float>(24));

			var model = new Model();
			model.Set("a", new Parameter<float>(2));
			model.Set("testVar", new Parameter<float>(6));
			model.Set("testVar2", new Parameter<float>(23));
			model.Set("testVar3", new Parameter<bool>(false));
			model.Set("result", new Parameter<string>("taper"));
			model.Set("vari", new ModelParameter(model2));
			model.Set("en", new EnumerableParameter<IModel>(models));

			var stack = new ModelStack();
			stack.Push(model);
			*/

			IModel[] members = new IModel[4];
			for (int i = 0; i < 2; i++)
			{
				members[i] = new Model();
				members[i].Set("name".AsSpan(), Parameter.Create("array"));
				members[i].Set("isReadonly".AsSpan(), Parameter.Create(true));
				members[i].Set("type".AsSpan(), Parameter.Create("int"));
				members[i].Set("size".AsSpan(), Parameter.Create(4f));
				members[i].Set("intSize".AsSpan(), Parameter.Create(4f));
				members[i].Set("length".AsSpan(), Parameter.Create(10f));
			}

			for (int i = 2; i < 4; i++)
			{
				members[i] = new Model();
				members[i].Set("name".AsSpan(), Parameter.Create("field"));
				members[i].Set("isReadonly".AsSpan(), Parameter.Create(true));
				members[i].Set("type".AsSpan(), Parameter.Create("int"));
				members[i].Set("size".AsSpan(), Parameter.Create(4f));
			}

			var model = new Model();
			model.Set("namespace".AsSpan(), Parameter.Create("TestNamespace"));
			model.Set("name".AsSpan(), Parameter.Create("TestClass"));
			model.Set("members".AsSpan(), Parameter.CreateEnum(members));

			var stack = new ModelStack();
			stack.Push(model);

			var str = File.ReadAllText("Templates/simple.tcs").AsMemory();
			TemplateDebugger.Parse(str);

			TemplateRules templateRules = new TemplateRules();
			var template = new ParsedTemplate(str.Span, templateRules.GetEnumerable(str.Span));

			Console.WriteLine();
			Console.WriteLine();
			Console.WriteLine("------- Model -------");

			Console.WriteLine($"a = 2");
			Console.WriteLine($"testVar = 6");
			Console.WriteLine($"testVar2 = 23");
			Console.WriteLine($"testVar3 = false");
			Console.WriteLine($"result = 'taper'");

			Console.WriteLine();
			Console.WriteLine("--- Parser Output ---");

			var sb = new StringBuilder();
			template.RenderTo(sb, stack);
			Console.WriteLine();
			Console.WriteLine("--- Render Output ---");

			Console.WriteLine();

			Console.WriteLine(sb);
		}
	}
}