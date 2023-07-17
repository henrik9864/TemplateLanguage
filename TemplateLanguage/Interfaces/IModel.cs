using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TemplateLanguage
{
	public interface IModel
	{
		IParameter this[ReadOnlySpan<char> name] { get; }

		void Set(ReadOnlySpan<char> name, IParameter parameter);

		bool TryGet(ReadOnlySpan<char> name, out IParameter parameter);

		IEnumerable<IParameter> GetEnumerable();
	}

	public interface IParameter
	{
		ReturnType GetType();

		bool TryGet<T>(out IEnumerable<T> value);

		bool TryGet<T>(out T value);

		bool TryGet(ReadOnlySpan<char> name, out IParameter parameter);

		bool TrySet<T>(T value);
	}
}
