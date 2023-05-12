using Newtonsoft.Json.Linq;

namespace Tokhenizer.Test
{
    public class LexerTests
    {
        const string TEST_STRING = "This is a test";

        [Fact]
        public void ConsumeEmptyLexerTest()
        {
            Lexer lexer = new Lexer("", 0);

            try
            {
                lexer.ConsumeAndCreateToken<TokenType>(out Token _, TokenType.String);
                Assert.True(false);
            }
            catch (Exception)
            {
                Assert.True(true);
            }
        }

        [Fact]
        public void ConsumeLexerTest()
        {
            Lexer lexer = new Lexer(TEST_STRING, 0);

            lexer.ConsumeAndCreateToken(4, out Token token, TokenType.String);
            Assert.Equal(4, lexer.Index);
            Assert.Equal(TokenType.String, token.Get<TokenType>(0));

            Assert.Equal(' ', lexer.Current);

            lexer.ConsumeAndCreateToken(out token, TokenType.Whitespace);
            Assert.Equal(5, lexer.Index);
            Assert.Equal(TokenType.Whitespace, token.Get<TokenType>(0));

            Assert.Equal('i', lexer.Current);

            var str = token.GetSpan(TEST_STRING);
            Assert.Equal(TEST_STRING.AsSpan().Slice(4, 1).ToString(), str.ToString());
        }

        [Fact]
        public void ConsumeAndCreateTest()
        {
            Lexer lexer = new Lexer(TEST_STRING, 10);

            lexer.Consume(4);
            lexer.TryCreateToken(out _, TokenType.String);

            Assert.Equal(14, lexer.Index);
            Assert.True(lexer.IsEnd());

            lexer.Consume(1);
            Assert.False(lexer.TryCreateToken(out _, TokenType.String));
        }

        [Fact]
        public void PeekTest()
        {
            Lexer lexer = new Lexer(TEST_STRING, 4);

            Assert.Equal('s', lexer.Peek(2));
        }

        [Fact]
        public void IsStringTest()
        {
            Lexer lexer = new Lexer(TEST_STRING, 4);

            Assert.True(lexer.IsString(TEST_STRING.AsSpan().Slice(4)));
        }
    }
}