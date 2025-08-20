namespace sLox;

public class LoxClass : LoxCallable
{
    public string Name { get; private set; }
    public LoxClass? Superclass { get; private set; }
    private readonly Dictionary<string, LoxFunction> _methods;
    public LoxClass(string name, LoxClass? superclass, Dictionary<string, LoxFunction> methods) :
        base(() => name)
    {
        Name = name;
        Superclass = superclass;
        _methods = methods;
        LoxFunction? initializer = FindMethod("init");
        
        base.Arity = () => initializer?.Arity() ?? 0;
        base.Call = (inter, args) => _call(inter, args, this);
    }

    private static readonly Func<
        Interpreter,
        List<object?>,
        LoxClass,
        object?> _call = (inter, args, inst) =>
    {
        LoxInstance instance = new(inst);
        LoxFunction? initializer = inst.FindMethod("init");
        if (initializer is not null)
        {
            initializer.Bind(instance).Call(inter, args);
        }
        
        return instance;
    };

    public LoxFunction? FindMethod(string name)
    {
        if (_methods.TryGetValue(name, out LoxFunction? method))
        {
            return method;
        }

        if (Superclass is not null)
        {
            return Superclass.FindMethod(name);
        }

        return null;
    }
}