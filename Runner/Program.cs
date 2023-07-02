using System.Runtime.CompilerServices;
using System.Text;
using TemplateLanguage;
using Tokhenizer;

var model = new TestModel();
model.Add("testVar", "6");
model.Add("testVar2", "23");
model.Add("result", "taper");

var str = File.ReadAllText("simpler.tcs").AsMemory();
TemplateDebugger.Parse(str);

TemplateRules templateRules = new TemplateRules();
var template = new ParsedTemplate(str.Span, templateRules.GetEnumerable(str.Span));

Console.WriteLine();
Console.WriteLine();
Console.WriteLine("------- Model -------");

Console.WriteLine($"testVar = 6");
Console.WriteLine($"testVar2 = 23");
Console.WriteLine($"result = 'taper'");

Console.WriteLine();
Console.WriteLine("--- Parser Output ---");

var sb = new StringBuilder();
template.RenderTo(sb, model);
Console.WriteLine();
Console.WriteLine("--- Render Output ---");

Console.WriteLine();

Console.WriteLine(sb);

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

    public void Add(ReadOnlySpan<char> name, string var)
    {
        data.Add(string.GetHashCode(name), (var, ReturnType.String));
    }

	public void Add(ReadOnlySpan<char> name, int var)
	{
		data.Add(string.GetHashCode(name), (var.ToString(), ReturnType.Number));
	}

	public void Add(ReadOnlySpan<char> name, float var)
	{
		data.Add(string.GetHashCode(name), (var.ToString(), ReturnType.Number));
	}

	public void Set(ReadOnlySpan<char> name, string var, ReturnType type)
	{
		if (!data.TryAdd(string.GetHashCode(name), (var, type)))
			data[string.GetHashCode(name)] = (var, type);
	}

	public ReturnType GetType(ReadOnlySpan<char> name)
	{
		return ReturnType.Number;
	}
}