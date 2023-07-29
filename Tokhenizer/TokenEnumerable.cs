﻿using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace Tokhenizer
{
    public delegate bool TokenRule(ref Lexer lexer, out Token token);

    public ref struct TokenEnumerable
    {
        ReadOnlySpan<char> text;
        Lexer lexer;

        TokenRule[] rules;

        public TokenEnumerable(ReadOnlySpan<char> text, params TokenRule[] rules)
        {
            this.text = text;
            lexer = new Lexer(text, 0);

            this.rules = rules;
        }

        public unsafe ref struct Enumerator
        {
#if NETSTANDARD2_0
            public unsafe readonly ref Token Current
            {
                get
                {
                    fixed(Token* pCurrent = &current)
                    {
                        return ref pCurrent[0];
                    }
                }
            }
#else
			public readonly ref Token Current => ref current;
#endif

            Lexer lexer;
            ReadOnlySpan<char> text;
            TokenRule[] rules;

#if NETSTANDARD2_0
            Token current;
#else
            ref Token current;
#endif

            public Enumerator(Lexer lexer, TokenRule[] rules, ReadOnlySpan<char> text, ref Token token)
            {
                this.lexer = lexer;
                this.rules = rules;
                this.text = text;
#if NETSTANDARD2_0
				this.current = token;
#else
				this.current = ref token;
#endif
            }

            public bool MoveNext()
            {
                return TryNext(out Current);
            }

            bool TryNext(out Token token)
            {
                Unsafe.SkipInit(out token);

                for (int i = 0; i < rules.Length; i++)
                {
                    if (lexer.IsEnd())
                        return false;

                    if (rules[i](ref lexer, out token))
                        return true;
                }

                throw new Exception($"Invalid character '{text[lexer.Index]}-({(byte)text[lexer.Index]})' at char {lexer.Index}");
            }

            public bool IsEnd()
                => lexer.IsEnd();
        }

        public Enumerator GetEnumerator()
        {
            using (var pool = MemoryPool<Token>.Shared.Rent(1))
            {
                return new Enumerator(lexer, rules, text, ref pool.Memory.Span[0]);
            }
        }
    }
}