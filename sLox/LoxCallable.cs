namespace sLox;

public class LoxCallable
{
    public Func<int> Arity { get; protected set; } = null!;
    public Func<Interpreter, List<object?>, object?> Call { get; protected set; } = null!;
    private readonly Func<string> _conv;
    
    public LoxCallable(
        Func<int> arity,
        Func<Interpreter, List<object?>, object> call,
        Func<string> conv)
    {
        Arity = arity;
        Call = call;
        _conv = conv;
    }
    protected LoxCallable(Func<string> conv)
    {
        _conv = conv;
    }
    
    public override string ToString()
    {
        return _conv();
    } 
};