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
            return new TokenEnumerable(text, BracketRule, NumberRule, BoolRule, OperatorRule, NewLineRule, SnippetRule, WhitespaceRule, StringRule);
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
                return lexer.ConsumeAndCreateToken(2, out token, TokenType.Operator, OperatorType.Equals);

			if (lexer.Current == '<')
				return lexer.ConsumeAndCreateToken(out token, TokenType.Operator, OperatorType.Less);

			if (lexer.Current == '>')
				return lexer.ConsumeAndCreateToken(out token, TokenType.Operator, OperatorType.Greater);

			if (lexer.IsString("elseif"))
				return lexer.ConsumeAndCreateToken(6, out token, TokenType.Operator, OperatorType.Elseif);

			if (lexer.IsString("if"))
				return lexer.ConsumeAndCreateToken(2, out token, TokenType.Operator, OperatorType.If);

			if (lexer.IsString("then"))
				return lexer.ConsumeAndCreateToken(4, out token, TokenType.Operator, OperatorType.Then);

			if (lexer.IsString("else"))
				return lexer.ConsumeAndCreateToken(4, out token, TokenType.Operator, OperatorType.Else);

			if (lexer.IsString("end"))
				return lexer.ConsumeAndCreateToken(3, out token, TokenType.Operator, OperatorType.End);

			if (lexer.Current == '$')
                return lexer.ConsumeAndCreateToken(out token, TokenType.Operator, OperatorType.Variable);

            if (lexer.Current == '=')
                return lexer.ConsumeAndCreateToken(out token, TokenType.Operator, OperatorType.Asssign);

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

        bool BoolRule(ref Lexer lexer, out Token token)
        {
			if (lexer.IsString("true"))
				return lexer.ConsumeAndCreateToken(4, out token, TokenType.Bool);

			if (lexer.IsString("false"))
				return lexer.ConsumeAndCreateToken(5, out token, TokenType.Bool);

			return lexer.Fail(out token);
		}

		bool NewLineRule(ref Lexer lexer, out Token token)
		{
			if (lexer.Current == '\n')
				return lexer.ConsumeAndCreateToken(1, out token, TokenType.NewLine);

			if (lexer.IsString(Environment.NewLine))
				return lexer.ConsumeAndCreateToken(Environment.NewLine.Length, out token, TokenType.NewLine);

			return lexer.Fail(out token);
		}

		bool WhitespaceRule(ref Lexer lexer, out Token token)
        {
            while (!lexer.IsEnd() && char.IsWhiteSpace(lexer.Current))
                lexer.Consume();

            return lexer.TryCreateToken(out token, TokenType.Whitespace);
        }

        bool StringRule(ref Lexer lexer, out Token token)
        {
            while (!lexer.IsEnd() && char.IsLetterOrDigit(lexer.Current) && !OperatorRule(ref lexer, out _) && !BracketRule(ref lexer, out _))
				lexer.Consume();

			return lexer.TryCreateToken(out token, TokenType.String);
        }
    }
}