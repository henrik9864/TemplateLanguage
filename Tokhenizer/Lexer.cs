using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;

namespace Tokhenizer
{
    public ref struct Lexer
    {
        public char Current
        {
            get
            {
                return Peek(0);
            }
        }

        public int Index
        {
            get
            {
                return index;
            }
        }

        public char this[int index]
        {
            get
            {
                return Peek(index);
            }
        }

        ReadOnlySpan<char> text;

        int index;
        int span;

        public Lexer(ReadOnlySpan<char> text, int start)
        {
            this.text = text;
            this.index = start;
            this.span = start;
        }

        public void Consume()
        {
            span++;
        }

        public void Consume(int characters)
        {
            span += characters;
        }

        public char Peek(int index)
        {
            if (span + index >= text.Length)
                return char.MinValue;

            return text[span + index];
        }

        public bool IsEnd()
        {
            return span >= text.Length;
        }

        public bool IsString(ReadOnlySpan<char> str)
        {
            if (span + str.Length > text.Length)
                return false;

            return text.Slice(span, str.Length).SequenceEqual(str);
        }

        public bool Fail(out Token token)
        {
			Unsafe.SkipInit(out token);
			return false;
        }

		public bool TryCreateToken<T1>(out Token token, in T1 item1) where T1 : unmanaged
        {
			Unsafe.SkipInit(out token);
			if (span > text.Length || index == span)
                return false;

            token = new Token(index..span);
            token.Get<T1>(0) = item1;
            Reset();
            return true;
        }

        public bool TryCreateToken<T1, T2>(out Token token, in T1 item1, in T2 item2) where T1 : unmanaged where T2 : unmanaged
        {
			Unsafe.SkipInit(out token);
			if (index == span)
                return false;

            token = new Token(index..span);
            token.Get<T1>(0) = item1;
            token.Get<T2>(1) = item2;
            Reset();
            return true;
        }

        void Reset()
        {
            index = span;
        }
    }

    public static class LexerExtensions
    {
        public static bool ConsumeAndCreateToken<T1>(this ref Lexer lexer, out Token token, in T1 item1) where T1 : unmanaged
        {
            lexer.Consume();
            return lexer.TryCreateToken(out token, item1);
        }

        public static bool ConsumeAndCreateToken<T1>(this ref Lexer lexer, int characters, out Token token, in T1 item1) where T1 : unmanaged
        {
            lexer.Consume(characters);
            return lexer.TryCreateToken(out token, item1);
        }

        public static bool ConsumeAndCreateToken<T1, T2>(this ref Lexer lexer, out Token token, in T1 item1, in T2 item2) where T1 : unmanaged where T2 : unmanaged
        {
            lexer.Consume();
            return lexer.TryCreateToken(out token, item1, item2);
        }

        public static bool ConsumeAndCreateToken<T1, T2>(this ref Lexer lexer, int characters, out Token token, in T1 item1, in T2 item2) where T1 : unmanaged where T2 : unmanaged
        {
            lexer.Consume(characters);
            return lexer.TryCreateToken(out token, item1, item2);
        }

		public static bool Dict<T1>(this Lexer lexer, Dictionary<char, T1> dict, out Token token) where T1 : unmanaged
		{
			Unsafe.SkipInit(out token);
			return dict.TryGetValue(lexer.Current, out T1 type) && lexer.ConsumeAndCreateToken(out token, type);
		}

		public static bool Dict<T1, T2>(this Lexer lexer, Dictionary<char, (T1, T2)> dict, out Token token) where T1 : unmanaged where T2 : unmanaged
		{
			Unsafe.SkipInit(out token);
			return dict.TryGetValue(lexer.Current, out (T1, T2) type) && lexer.ConsumeAndCreateToken(out token, type.Item1, type.Item2);
		}
	}
}