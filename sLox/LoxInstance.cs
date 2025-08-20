namespace sLox;

public class LoxInstance(LoxClass klass)
{
    private readonly Dictionary<string, object?> _fields = new();
    
    public override string ToString()
    {
        return klass.Name + " instance";
    }

    public object? Get(Token name)
    {
        if (_fields.TryGetValue(name.Lexeme, out object? value))
        {
            return value;
        }

        LoxFunction? method = klass.FindMethod(name.Lexeme);
        if (method is not null) return method.Bind(this);
        
        throw new RuntimeException(name, $"Undefined property '{name.Lexeme}'.");
    }

    public void Set(Token name, object? value)
    {
        _fields[name.Lexeme] = value;
    }
}