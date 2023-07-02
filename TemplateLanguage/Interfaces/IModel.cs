using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace TemplateLanguage
{
    public interface IModel
    {
        ReadOnlySpan<char> this[ReadOnlySpan<char> name] { get; }

        void Set(ReadOnlySpan<char> name, string var, ReturnType type);

		ReturnType GetType(ReadOnlySpan<char> name);
	}
}
