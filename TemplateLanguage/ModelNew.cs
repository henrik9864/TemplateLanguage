namespace TemplateLanguage
{
	public class ModelNew : IModel
	{
		Dictionary<int, IParameter> data = new();

		public IParameter this[ReadOnlySpan<char> name] => data[string.GetHashCode(name)];

		public void Set(ReadOnlySpan<char> name, IParameter parameter)
		{
			if (!data.TryAdd(string.GetHashCode(name), parameter))
				data[string.GetHashCode(name)] = parameter;
		}

		public bool TryGet(ReadOnlySpan<char> name, out IParameter parameter)
		{
			return data.TryGetValue(string.GetHashCode(name), out parameter);
		}
	}
}
