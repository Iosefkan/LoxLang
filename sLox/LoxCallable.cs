namespace sLox;

public record LoxCallable(
    Func<int> Arity, 
    Func<Interpreter, List<object?>, object?> Call, 
    Func<string>? Conv = null)
{
    public override string ToString()
    {
        if (Conv is null) return "<fn>";
        return Conv();
    } 
};