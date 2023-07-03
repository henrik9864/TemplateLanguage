using System.Runtime.CompilerServices;
using System.Text;
using TemplateLanguage;
using Tokhenizer;

var model = new TestModel();
model.Set("testVar", 6);
model.Set("testVar2", 23);
model.Set("result", "taper");

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