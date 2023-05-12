using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TemplateLanguage
{
    public interface IModel
    {
        ReadOnlySpan<char> this[ReadOnlySpan<char> name] { get; }
    }

    public interface IParameter
    {

    }
}
