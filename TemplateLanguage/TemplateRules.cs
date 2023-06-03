using System.Runtime.CompilerServices;
using TemplateLanguage;

namespace Tokhenizer
{
    public class TemplateRules
    {
        List<ReadOnlyMemory<char>> snippets = new List<ReadOnlyMemory<char>>();

        public TemplateRules(params string[] snippets)
        {
            this.snippets.AddRange(snippets.Select(x => x.AsMemory()));
        }

        public TokenEnumerable GetEnumerable(ReadOnlySpan<char> text)
        {
            return new TokenEnumerable(text, BracketRule, NumberRule, OperatorRule, SnippetRule, WhitespaceRule, StringRule);
        }

        bool BracketRule(ref Lexer lexer, out Token token)
        {
            if (lexer.Current == '|')
                return lexer.ConsumeAndCreateToken(out token, TokenType.Bracket, BracketType.Code);

            if (lexer.Current == '"')
                return lexer.ConsumeAndCreateToken(out token, TokenType.Bracket, BracketType.String);

			if (lexer.Current == '(')
				return lexer.ConsumeAndCreateToken(out token, TokenType.Bracket, BracketType.Open);

			if (lexer.Current == ')')
				return lexer.ConsumeAndCreateToken(out token, TokenType.Bracket, BracketType.Close);

			if (lexer.IsString("->"))
				return lexer.ConsumeAndCreateToken(2, out token, TokenType.Bracket, BracketType.AccessorOpen);

			if (lexer.IsString("<-"))
				return lexer.ConsumeAndCreateToken(2, out token, TokenType.Bracket, BracketType.AccessorClose);

			if (lexer.IsString("~>"))
				return lexer.ConsumeAndCreateToken(2, out token, TokenType.Bracket, BracketType.EnumerableAccessorOpen);

			if (lexer.IsString("<~"))
				return lexer.ConsumeAndCreateToken(2, out token, TokenType.Bracket, BracketType.EnumerableAccessorOpen);

			return lexer.Fail(out token);
        }

        bool OperatorRule(ref Lexer lexer, out Token token)
        {
            if (lexer.IsString("=="))
                return lexer.ConsumeAndCreateToken(2, out token, TokenType.Operator, OperatorType.Comparer);

            if (lexer.Current == '$')
                return lexer.ConsumeAndCreateToken(out token, TokenType.Operator, OperatorType.Variable);

            if (lexer.Current == '=')
                return lexer.ConsumeAndCreateToken(out token, TokenType.Operator, OperatorType.Setter);

            if (lexer.Current == '?')
                return lexer.ConsumeAndCreateToken(out token, TokenType.Operator, OperatorType.Conditional);

            if (lexer.Current == '+')
                return lexer.ConsumeAndCreateToken(out token, TokenType.Operator, OperatorType.Add);

			if (lexer.Current == '-')
				return lexer.ConsumeAndCreateToken(out token, TokenType.Operator, OperatorType.Subtract);

			if (lexer.Current == '*')
				return lexer.ConsumeAndCreateToken(out token, TokenType.Operator, OperatorType.Multiply);

			if (lexer.Current == '/')
				return lexer.ConsumeAndCreateToken(out token, TokenType.Operator, OperatorType.Divide);

			return lexer.Fail(out token);
        }

        bool SnippetRule(ref Lexer lexer, out Token token)
        {
            for (int i = 0; i < snippets.Count; i++)
            {
                ReadOnlyMemory<char> snippet = snippets[i];

                if (lexer.IsString(snippet.Span))
                    return lexer.ConsumeAndCreateToken(snippet.Length, out token, TokenType.Snippet, i);
            }

            return lexer.Fail(out token);
        }

        bool NumberRule(ref Lexer lexer, out Token token)
        {
            NumberType numberType = NumberType.Integer;

            if (lexer.Current == '.')
                return lexer.Fail(out token);

            while (!lexer.IsEnd() && (char.IsDigit(lexer.Current) || lexer.Current == '.'))
            {
                if (lexer.Current == '.')
                    numberType = NumberType.Float;

                lexer.Consume();
            }

            return lexer.TryCreateToken(out token, TokenType.Number, numberType);
        }

        bool WhitespaceRule(ref Lexer lexer, out Token token)
        {
            while (!lexer.IsEnd() && char.IsWhiteSpace(lexer.Current))
                lexer.Consume();

            return lexer.TryCreateToken(out token, TokenType.Whitespace);
        }

        bool StringRule(ref Lexer lexer, out Token token)
        {
            while (!lexer.IsEnd() && !char.IsWhiteSpace(lexer.Current) && char.IsLetterOrDigit(lexer.Current) && !OperatorRule(ref lexer, out _) && !BracketRule(ref lexer, out _))
                lexer.Consume();

            return lexer.TryCreateToken(out token, TokenType.String);
        }
    }
}