using System;
using System.Collections.Generic;
using System.Linq;

namespace TemplateLanguage
{
	public class Model : IModel
	{
		Dictionary<int, IParameter> data = new();

#if NETSTANDARD2_0
		public IParameter this[ReadOnlySpan<char> name] => data[name.ToString().GetHashCode()];
#else
		public IParameter this[ReadOnlySpan<char> name] => data[string.GetHashCode(name)];
#endif

#if NETSTANDARD2_0
		public void Set(ReadOnlySpan<char> name, IParameter parameter)
		{
			if (data.ContainsKey(name.ToString().GetHashCode()))
			{
				data.Add(name.ToString().GetHashCode(), parameter);
			}
			else
			{
				data[name.ToString().GetHashCode()] = parameter;
			}
		}
#else
		public void Set(ReadOnlySpan<char> name, IParameter parameter)
		{
			if (!data.TryAdd(string.GetHashCode(name), parameter))
				data[string.GetHashCode(name)] = parameter;
		}
#endif

		public bool TryGet(ReadOnlySpan<char> name, out IParameter parameter)
		{
#if NETSTANDARD2_0
			return data.TryGetValue(name.ToString().GetHashCode(), out parameter);
#else
			return data.TryGetValue(string.GetHashCode(name), out parameter);
#endif
		}

		public IEnumerable<IParameter> GetEnumerable()
		{
			return data.Select(x => x.Value);
		}
	}
}
