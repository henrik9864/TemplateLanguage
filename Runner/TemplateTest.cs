using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TemplateLanguage;
using Tokhenizer;

namespace Runner
{
	internal class TemplateTest
	{
		public static void Run()
		{
			IModel[] members = new IModel[4];
			for (int i = 0; i < 2; i++)
			{
				members[i] = new Model();
				members[i].Set("name", Parameter.Create("array"));
				members[i].Set("isReadonly", Parameter.Create(true));
				members[i].Set("type", Parameter.Create("int"));
				members[i].Set("size", Parameter.Create(4f));
				members[i].Set("intSize", Parameter.Create(4f));
				members[i].Set("length", Parameter.Create(10f));
				members[i].Set("name_stop", Parameter.Create("array_stop"));
			}

			for (int i = 2; i < 4; i++)
			{
				members[i] = new Model();
				members[i].Set("name", Parameter.Create("field"));
				members[i].Set("isReadonly", Parameter.Create(true));
				members[i].Set("type", Parameter.Create("int"));
				members[i].Set("size", Parameter.Create(4f));
			}

			var model = new Model();
			model.Set("namespace", Parameter.Create("TestNamespace"));
			model.Set("name", Parameter.Create("TestClass"));
			model.Set("container", Parameter.Create("TestContainer"));
			model.Set("members", Parameter.CreateEnum(members));

			var stack = new ModelStack();
			stack.Push(model);

			TemplateRules templateRules = new TemplateRules();
			var input = File.ReadAllText("template.tcs").AsMemory();
			var template = new ParsedTemplate(input.Span, templateRules.GetEnumerable(input.Span));

			var sb = new StringBuilder();
			template.RenderTo(sb, stack);

			Console.WriteLine(sb);
		}
	}
}
