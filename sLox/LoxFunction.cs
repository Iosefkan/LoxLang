namespace sLox;

public record LoxFunction : LoxCallable
{
    public LoxFunction(
        Stmt.Function declaration,
        Environ closure)
        : base(() => _arity(declaration),
            (inter, args) => _call(inter, args, declaration, closure),
            () => _toString(declaration))
    {
    }

    private static Func<Stmt.Function, string> _toString => (declar) => $"<fn {declar.Name.Lexeme}>";
    
    public override string ToString() => base.ToString();

    private static Func<Stmt.Function, int> _arity => (declar) => declar.Params.Count;
    
    private static readonly 
        Func<
            Interpreter, 
            List<object?>, 
            Stmt.Function, 
            Environ, 
            object?> _call = 
        (inter, args, declar, closure) =>
    {
        Environ environment = new Environ(closure);
        for (int i = 0; i < declar.Params.Count; i++)
        {
            environment.Define(declar.Params[i].Lexeme, args[i]);
        }

        try
        {
            inter.ExecuteBlock(declar.Body, environment);
        }
        catch (Return returnValue)
        {
            return returnValue.Value;
        }
        
        return null;
    };
}