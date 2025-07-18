namespace sLox;

public class Token
{
    public TokenType Type { get; private set; }
    public string Lexeme { get; private set; }
    public object Literal { get; private set; }
    public int Line { get; private set; }

    public Token(TokenType type, string lexeme, object literal, int line)
    {
        Type = type;
        Lexeme = lexeme;
        Literal = literal;
        Line = line;
    }

    public override string ToString()
    {
        return $"{Type} {Lexeme} {Literal}";
    }
}