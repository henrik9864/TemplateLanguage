using System.Runtime.CompilerServices;

namespace TemplateLanguage
{
	public class Parameter<T> : IParameter
	{
		static Dictionary<Type, ReturnType> types = new()
		{
			{typeof(int), ReturnType.Number},
			{typeof(float), ReturnType.Number},
			{typeof(bool), ReturnType.Bool},
			{typeof(string), ReturnType.String},
		};

		public IParameter this[ReadOnlySpan<char> name] => throw new NotImplementedException();

		T value;

		public Parameter(T value)
		{
			this.value = value;
		}

		ReturnType IParameter.GetType()
		{
			return types[typeof(T)];
		}

		public bool GetBool()
		{
			if (typeof(T) != typeof(bool))
				throw new NotImplementedException();

			return Unsafe.As<T, bool>(ref value);
		}

		public float GetFloat()
		{
			if (typeof(T) != typeof(float))
				throw new NotImplementedException();

			return Unsafe.As<T, float>(ref value);
		}

		public int GetInt()
		{
			if (typeof(T) != typeof(int))
				throw new NotImplementedException();

			return Unsafe.As<T, int>(ref value);
		}

		public string GetString()
		{
			if (typeof(T) != typeof(string))
				throw new NotImplementedException();

			return Unsafe.As<T, string>(ref value);
		}

		public void Set(float value)
		{
			if (typeof(T) != typeof(float))
				throw new NotImplementedException();

			this.value = Unsafe.As<float, T>(ref value);
		}

		public void Set(int value)
		{
			if (typeof(T) != typeof(int))
				throw new NotImplementedException();

			this.value = Unsafe.As<int, T>(ref value);
		}

		public void Set(bool value)
		{
			if (typeof(T) != typeof(bool))
				throw new NotImplementedException();

			this.value = Unsafe.As<bool, T>(ref value);
		}

		public void Set(string value)
		{
			if (typeof(T) != typeof(string))
				throw new NotImplementedException();

			this.value = Unsafe.As<string, T>(ref value);
		}
	}
}
