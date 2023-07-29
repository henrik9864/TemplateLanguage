using System;
using System.Collections.Generic;
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

		public bool TryGet<T>(out IEnumerable<T> value)
		{
			if (typeof(T) != typeof(IParameter))
			{
				value = default;
				return false;
			}

			var enumerable = this.value.GetEnumerable();
			value = Unsafe.As<IEnumerable<IParameter>, IEnumerable<T>>(ref enumerable);
			return true;
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
			return ReturnType.Model;
		}
	}

	public class EnumerableParameter<T> : IParameter
	{
		IEnumerable<T> value;

		public EnumerableParameter(IEnumerable<T> value)
		{
			this.value = value;
		}

		public bool TryGet<T1>(out IEnumerable<T1> value)
		{
			if (typeof(T1) != typeof(T))
			{
				value = default;
				return false;
			}

			value = Unsafe.As<IEnumerable<T>, IEnumerable<T1>>(ref this.value);
			return true;
		}

		public bool TryGet<T1>(out T1 value)
		{
			if (typeof(T1) != typeof(IEnumerable<T>))
			{
				value = default;
				return false;
			}

			value = Unsafe.As<IEnumerable<T>, T1>(ref this.value);
			return true;
		}

		public bool TryGet(ReadOnlySpan<char> name, out IParameter parameter)
		{
			parameter = default;
			return false;
		}

		public bool TrySet<T1>(T1 value)
		{
			if (typeof(T1) != typeof(IEnumerable<T>))
			{
				value = default;
				return false;
			}

			this.value = Unsafe.As<T1, IEnumerable<T>>(ref value);
			return true;
		}

		ReturnType IParameter.GetType()
		{
			return ReturnType.Enumerable;
		}
	}
}
