using System.Runtime.CompilerServices;

namespace TemplateLanguage
{
	public class ModelParameter : IParameter
	{
		IModel value;

		public ModelParameter(IModel value)
		{
			this.value = value;
		}

		public bool TryGet<T>(out T value)
		{
			if (typeof(T) != typeof(IModel))
			{
				value = default;
				return false;
			}

			value = Unsafe.As<IModel, T>(ref this.value);
			return true;
		}

		public bool TryGet(ReadOnlySpan<char> name, out IParameter parameter)
		{
			return value.TryGet(name, out parameter);
		}

		public bool TrySet<T>(T value)
		{
			if (typeof(T) != typeof(IModel))
			{
				return false;
			}

			this.value = Unsafe.As<T, IModel>(ref value);
			return true;
		}

		ReturnType IParameter.GetType()
		{
			return ReturnType.Unknown;
		}
	}
}
