using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using TemplateLanguage;
using Tokhenizer;

var model = new ModelNew();
model.Set("a", new Parameter<float>(2));
model.Set("testVar", new Parameter<float>(6));
model.Set("testVar2", new Parameter<float>(23));
model.Set("testVar3", new Parameter<bool>(false));
model.Set("result", new Parameter<string>("taper"));
model.Set("vari", new Parameter<string>("tpt"));

var str = File.ReadAllText("simpler.tcs").AsMemory();
TemplateDebugger.Parse(str);

TemplateRules templateRules = new TemplateRules();
var template = new ParsedTemplate(str.Span, templateRules.GetEnumerable(str.Span));

Console.WriteLine();
Console.WriteLine();
Console.WriteLine("------- Model -------");

Console.WriteLine($"testVar = 6");
Console.WriteLine($"testVar2 = 23");
Console.WriteLine($"testVar3 = false");
Console.WriteLine($"result = 'taper'");

Console.WriteLine();
Console.WriteLine("--- Parser Output ---");

var sb = new StringBuilder();
template.RenderTo(sb, model);
Console.WriteLine();
Console.WriteLine("--- Render Output ---");

Console.WriteLine();

Console.WriteLine(sb);