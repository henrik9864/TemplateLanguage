using Newtonsoft.Json.Linq;

namespace LightLexer.Test
{
    public class TokenEnumerableTests
    {
        const string TEST_STRING = "This is a test";

        [Fact]
        public void TokenEnumerableWithNoRules()
        {
            TokenEnumerable tokens = new TokenEnumerable(TEST_STRING);

            try
            {
                tokens.GetEnumerator().MoveNext();
                Assert.True(false);
            }
            catch (Exception)
            {
                Assert.True(true);
            }
        }

        [Fact]
        public void ParseSimpleTextTest()
        {
            TokenEnumerable tokens = new TokenEnumerable(TEST_STRING, TestRules.WhitespaceRule, TestRules.StringRule);
            TokenType[] TEST_TOKENS = { TokenType.String, TokenType.Whitespace, TokenType.String, TokenType.Whitespace, TokenType.String, TokenType.Whitespace, TokenType.String };

            int i = 0;
            foreach (ref Token token in tokens)
            {
                Assert.True(TEST_TOKENS[i] == token.Get<TokenType>(0));

                i++;
            }
        }

        [Fact]
        public void AccesingEmptySlotThrowsTest()
        {
            TokenEnumerable tokens = new TokenEnumerable(TEST_STRING, TestRules.WhitespaceRule, TestRules.StringRule);

            try
            {
                var e = tokens.GetEnumerator();
                e.MoveNext();
                Assert.True(true);

                var o = e.Current.Get<TokenType>(1);
                Assert.True(false);
            }
            catch (Exception)
            {
                Assert.True(true);
            }
        }

        [Fact]
        public void AccesingWrongTypeThrowsTest()
        {
            TokenEnumerable tokens = new TokenEnumerable(TEST_STRING, TestRules.WhitespaceRule, TestRules.StringRule);

            try
            {
                var e = tokens.GetEnumerator();
                e.MoveNext();
                Assert.True(true);

                var o = e.Current.Get<TokenType2>(0);
                Assert.True(false);
            }
            catch (Exception)
            {
                Assert.True(true);
            }
        }
    }
}