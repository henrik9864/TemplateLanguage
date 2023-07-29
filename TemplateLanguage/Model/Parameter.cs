using System;
using System.Collections.Generic;
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

		T value;

		public Parameter(T value)
		{
			this.value = value;
		}

		ReturnType IParameter.GetType()
		{
			return types[typeof(T)];
		}

		public bool TryGet<T1>(out T1 value)
		{
			if (typeof(T) != typeof(T1))
			{
				value = default;
				return false;
			}

			value = Unsafe.As<T, T1>(ref this.value);
			return true;
		}

		public bool TryGet(ReadOnlySpan<char> name, out IParameter value)
		{
			value = default;
			return false;
		}

		public bool TryGet<T1>(out IEnumerable<T1> value)
		{
			value = default;
			return false;
		}

		public bool TrySet<T1>(T1 value)
		{
			if (typeof(T) != typeof(T1))
			{
				return false;
			}

			this.value = Unsafe.As<T1, T>(ref value);
			return true;
		}
	}

	public static class Parameter
	{
		public static EnumerableParameter<T> CreateEnum<T>(IEnumerable<T> value)
		{
			return new EnumerableParameter<T>(value);
		}

		public static ModelParameter Create(IModel value)
		{
			return new ModelParameter(value);
		}

		public static Parameter<T> Create<T>(T value)
		{
			return new Parameter<T>(value);
		}
	}
}
