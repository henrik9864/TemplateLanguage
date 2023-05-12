using System.Runtime.CompilerServices;
using System.Text;
using TemplateLanguage;
using Tokhenizer;

var model = new TestModel();
model.Add("testVar", "25");

var str = File.ReadAllText("simpler.tcs").AsMemory();
TemplateDebugger.Parse(str);

var template = ParsedTemplate.Tokenize(str.Span);

Console.WriteLine();
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
    Dictionary<int, string> data = new Dictionary<int, string>();

    public void Add(ReadOnlySpan<char> name, string var)
    {
        data.Add(string.GetHashCode(name), var);
    }

    public ReadOnlySpan<char> this[ReadOnlySpan<char> name]
    {
        get
        {
            return data[string.GetHashCode(name)];
        }
    }
}