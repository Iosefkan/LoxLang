namespace sLox;

public class LoxFunction : LoxCallable
{
    private readonly Environ _closure;
    private readonly Stmt.Function _declaration;
    private readonly bool _isInitializer = false;
    
    public LoxFunction(
        Stmt.Function declaration,
        Environ closure,
        bool isInitializer)
        : base(() => _toString(declaration))
    {
        _isInitializer = isInitializer;
        _closure = closure;
        _declaration = declaration;

        base.Arity = () => _arity(declaration);
        base.Call = (inter, args) => _call(inter, args, declaration, closure, this);
    }

    private static readonly Func<Stmt.Function, string> _toString = (declar) => $"<fn {declar.Name.Lexeme}>";

    private static readonly Func<Stmt.Function, int> _arity = (declar) => declar.Params.Count;
    
    private static readonly 
        Func<
            Interpreter, 
            List<object?>, 
            Stmt.Function, 
            Environ,
            LoxFunction,
            object?> _call = 
        (inter, args, declar, closure, inst) =>
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
            if (inst._isInitializer) return closure.GetAt(0, "this");
            return returnValue.Value;
        }

        if (inst._isInitializer) return closure.GetAt(0, "this");
        return null;
    };

    public LoxFunction Bind(LoxInstance instance)
    {
        Environ environment = new Environ(_closure);
        environment.Define("this", instance);
        return new LoxFunction(_declaration, environment, _isInitializer);
    }
}