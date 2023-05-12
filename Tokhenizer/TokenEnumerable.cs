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
            public readonly ref Token Current => ref current;

            Lexer lexer;
            ReadOnlySpan<char> text;
            TokenRule[] rules;

            ref Token current;

            public Enumerator(Lexer lexer, TokenRule[] rules, ReadOnlySpan<char> text, ref Token token)
            {
                this.lexer = lexer;
                this.rules = rules;
                this.text = text;
                this.current = ref token;
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