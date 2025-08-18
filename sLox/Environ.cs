namespace sLox;

public class Environ
{
    public Environ? Enclosing { get; private set; }
    private readonly Dictionary<string, object?> _values = new();

    public Environ()
    {
        Enclosing = null;
    }

    public Environ(Environ enclosing)
    {
        Enclosing = enclosing;
    }
    
    public void Define(string name, object? value)
    {
        _values[name] = value;
    }

    public object? Get(Token name)
    {
        if (_values.TryGetValue(name.Lexeme, out object? value))
        {
            return value;
        }

        if (Enclosing is not null) return Enclosing.Get(name);
        
        throw new RuntimeException(name, $"Undefined variable '{name.Lexeme}'.");
    }

    public void Assign(Token name, object? value)
    {
        if (_values.ContainsKey(name.Lexeme))
        {
            _values[name.Lexeme] = value;
            return;
        }

        if (Enclosing is not null)
        {
            Enclosing.Assign(name, value);
            return;
        }
        
        throw new RuntimeException(name, $"Undefined variable '{name.Lexeme}'.");
    }
}