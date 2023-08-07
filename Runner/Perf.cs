using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TemplateLanguage;
using LightLexer;

namespace Runner
{
	[SimpleJob(RuntimeMoniker.Net80)]
	public class Perf
	{
		ModelStack stack = new ModelStack();

		[GlobalSetup]
		public void Setup()
		{
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
				members[i].Set("name_stop".AsSpan(), Parameter.Create("array_stop"));
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
			model.Set("container".AsSpan(), Parameter.Create("TestContainer"));
			model.Set("members".AsSpan(), Parameter.CreateEnum(members));

			stack.Push(model);
		}

		[Benchmark]
		public void Test1()
		{
			TemplateRules templateRules = new TemplateRules();
			var input = File.ReadAllText("Templates/template.tcs").AsMemory();
			var template = new ParsedTemplate(input.Span, templateRules.GetEnumerable(input.Span));

			var sb = new StringBuilder();
			template.RenderTo(sb, stack);
		}
	}
}
