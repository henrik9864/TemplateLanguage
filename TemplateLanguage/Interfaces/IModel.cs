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
		IParameter this[ReadOnlySpan<char> name] { get; }

		void Set(ReadOnlySpan<char> name, IParameter parameter);

		bool TryGet(ReadOnlySpan<char> name, out IParameter parameter);
	}

	public interface IParameter
	{
		IParameter this[ReadOnlySpan<char> name] { get; }

		ReturnType GetType();

		bool TryGet<T>(out T value);

		bool TrySet<T>(T value);

		/*
		float GetFloat();

		int GetInt();

		bool GetBool();

		string GetString();

		void Set(float value);

		void Set(int value);

		void Set(bool value);

		void Set(string value);
		*/
	}
}
