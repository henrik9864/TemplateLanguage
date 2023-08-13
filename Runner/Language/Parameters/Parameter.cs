﻿using LightParser;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Runner
{
	public class Parameter<T> : IParameter<ReturnType>
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

		ReturnType IParameter<ReturnType>.GetType()
		{
			return types[typeof(T)];
		}

		public bool TryGet<T1>(out T1 value)
		{
			if (typeof(T) == typeof(T1) || typeof(T).IsAssignableFrom(typeof(T1)))
			{
				value = Unsafe.As<T, T1>(ref this.value);
				return true;
			}

			value = default;
			return false;
		}

		public bool TryGet(ReadOnlySpan<char> name, out IParameter<ReturnType> value)
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
			if (typeof(T) == typeof(T1) || typeof(T).IsAssignableFrom(typeof(T1)))
			{
				this.value = Unsafe.As<T1, T>(ref value);
				return true;
			}

			return false;
		}
	}

	public static class Parameter
	{
		public static EnumerableParameter<T> CreateEnum<T>(IEnumerable<T> value)
		{
			return new EnumerableParameter<T>(value);
		}

		public static ModelParameter Create(IModel<ReturnType> value)
		{
			return new ModelParameter(value);
		}

		public static Parameter<T> Create<T>(T value)
		{
			return new Parameter<T>(value);
		}
	}
}
