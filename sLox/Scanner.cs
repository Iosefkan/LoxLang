namespace sLox;

public class Scanner
{
    private string _source;
    private List<Token> _tokens = new();
    private int _start = 0;
    private int _current = 0;
    private int _line = 1;
    private static readonly Dictionary<string, TokenType> Keywords = new Dictionary<string, TokenType>()
    {
        ["and"] = TokenType.And,
        ["class"] = TokenType.Class,
        ["else"] = TokenType.Else,
        ["false"] = TokenType.False,
        ["for"] = TokenType.For,
        ["fun"] = TokenType.Fun,
        ["if"] = TokenType.If,
        ["nil"] = TokenType.Nil,
        ["or"] = TokenType.Or,
        ["print"] = TokenType.Print,
        ["return"] = TokenType.Return,
        ["super"] = TokenType.Super,
        ["this"] = TokenType.This,
        ["true"] = TokenType.True,
        ["var"] = TokenType.Var,
        ["while"] = TokenType.While
    };

    public Scanner(string source)
    {
        _source = source;
    }

    public List<Token> ScanTokens()
    {
        while (!IsAtEnd)
        {
            _start = _current;
            ScanToken();
        }
        
        _tokens.Add(new Token(TokenType.Eof, "", null, _line));
        return _tokens;
    }

    private bool IsAtEnd => _current >= _source.Length;

    private void ScanToken()
    {
        char c = Advance();
        switch (c)
        {
            case '(': AddToken(TokenType.LeftParen); break;
            case ')': AddToken(TokenType.RightParen); break;
            case '{': AddToken(TokenType.LeftBrace); break;
            case '}': AddToken(TokenType.RightBrace); break;
            case ',': AddToken(TokenType.Comma); break;
            case '.': AddToken(TokenType.Dot); break;
            case '-': AddToken(TokenType.Minus); break;
            case '+': AddToken(TokenType.Plus); break;
            case ';': AddToken(TokenType.Semicolon); break;
            case '*': AddToken(TokenType.Star); break; 
            case '!':
                AddToken(Match('=') ? TokenType.BangEqual : TokenType.Bang);
                break;
            case '=':
                AddToken(Match('=') ? TokenType.EqualEqual : TokenType.Equal);
                break;
            case '<':
                AddToken(Match('=') ? TokenType.LessEqual : TokenType.Less);
                break;
            case '>':
                AddToken(Match('=') ? TokenType.GreaterEqual : TokenType.Greater);
                break;
            case '/':
                if (Match('/'))
                {
                    while (Peek != '\n' && !IsAtEnd)
                        Advance();
                }
                else if (Match('*'))
                {
                    while (Peek != '*' && PeekNext != '/' && !IsAtEnd)
                    {
                        if (Peek == '\n') _line++;
                        Advance();
                    }
                    Advance();
                    Advance();
                }
                else
                {
                    AddToken(TokenType.Slash);
                }

                break;
            case ' ':
            case '\r':
            case '\t':
                break;
            case '\n':
                _line++;
                break;
            case '"':
                String();
                break;
            default:
                if (IsDigit(c))
                {
                    Number();
                }
                else if (IsAlpha(c))
                {
                    Identifier();
                }
                else
                {
                    Lox.Error(_line, "Unexpected character.");
                }

                break;
        }
    }

    private char Advance()
    {
        _current++;
        return _source[_current - 1];
    }

    private void AddToken(TokenType tokenType)
    {
        AddToken(tokenType, null);
    }

    private void AddToken(TokenType tokenType, object literal)
    {
        string text = _source.Substring(_start, _current - _start);
        _tokens.Add(new Token(tokenType, text, literal, _line));
    }

    private bool Match(char expected)
    {
        if (IsAtEnd) return false;
        if (_source[_current] != expected) return false;
        _current++;
        return true;
    }

    private void String()
    {
        while (Peek != '"' && !IsAtEnd)
        {
            if (Peek == '\n') _line++;
            Advance();
        }

        if (IsAtEnd)
        {
            Lox.Error(_line, "Unterminated string.");
            return;
        }

        Advance();
        string value = _source.Substring(_start + 1, _current - _start - 2);
        AddToken(TokenType.String, value);
    }

    private void Number()
    {
        while (IsDigit(Peek)) Advance();
        if (Peek == '.' && IsDigit(PeekNext))
        {
            Advance();
            while (IsDigit(Peek)) Advance();
        }
        
        var num = _source.Substring(_start, _current - _start);
        AddToken(TokenType.Number, double.Parse(num));
    }
    
    private void Identifier()
    {
        while (IsAlphaNumeric(Peek)) Advance();
        
        string text = _source.Substring(_start, _current - _start);
        if (!Keywords.TryGetValue(text, out TokenType tokenType))
        {
            tokenType = TokenType.Identifier;
        }
        
        AddToken(tokenType);
    }
    
    private char Peek => IsAtEnd ? '\0': _source[_current];
    
    private char PeekNext => _current + 1 >= _source.Length
                                ? '\0'
                                : _source[_current + 1];

    private bool IsDigit(char c)
    {
        return c >= '0' && c <= '9';
    }
    
    private bool IsAlpha(char c) {
        return (c >= 'a' && c <= 'z') ||
               (c >= 'A' && c <= 'Z') ||
               c == '_';
    }

    private bool IsAlphaNumeric(char c) => IsAlpha(c) || IsDigit(c);
}