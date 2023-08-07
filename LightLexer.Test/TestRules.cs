using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightLexer.Test
{
    public static class TestRules
    {
        public static bool WhitespaceRule(ref Lexer lexer, out Token token)
        {
            while (!lexer.IsEnd() && char.IsWhiteSpace(lexer.Current))
                lexer.Consume();

            return lexer.TryCreateToken(out token, TokenType.Whitespace);
        }

        public static bool StringRule(ref Lexer lexer, out Token token)
        {
            while (!lexer.IsEnd() && !char.IsWhiteSpace(lexer.Current))
                lexer.Consume();

            return lexer.TryCreateToken(out token, TokenType.String);
        }
    }

    public enum TokenType
    {
        String,
        Whitespace
    }

    public enum TokenType2
    {

    }
}
