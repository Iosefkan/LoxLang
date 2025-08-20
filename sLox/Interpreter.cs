namespace sLox;

public class Interpreter : Expr.IVisitor<object?>, Stmt.IVisitor<object?>
{
    private static readonly Environ Globals = new();
    private Environ _environment = Globals;
    private readonly Dictionary<Expr, int> _locals = new();

    public Interpreter()
    {
        Globals.Define("clock", new LoxCallable(
            () => 0, 
            (inter,  args) => (double)DateTimeOffset.Now.ToUnixTimeMilliseconds() / 1000.0,
            () => "<native fn>"
            ));
    }
    public void Interpret(List<Stmt> statements)
    {
        try
        {
            foreach (Stmt statement in statements)
            {
                Execute(statement);
            }
        }
        catch (RuntimeException ex)
        {
            Lox.RuntimeError(ex);
        }
    }

    public object? VisitIfStmt(Stmt.If stmt)
    {
        if (IsTruthy(Evaluate(stmt.Condition)))
        {
            Execute(stmt.ThenBranch);
        }
        else if (stmt.ElseBranch is not null)
        {
            Execute(stmt.ElseBranch);
        }

        return null;
    }

    public object? VisitBlockStmt(Stmt.Block stmt)
    {
        ExecuteBlock(stmt.Statements, new Environ(_environment));
        return null;
    }

    public object? VisitSuperExpr(Expr.Super expr)
    {
        int distance = _locals[expr];
        LoxClass? superclass = _environment.GetAt(distance, "super") as LoxClass;
        LoxInstance? obj = _environment.GetAt(distance - 1, "this") as LoxInstance;

        LoxFunction? method = superclass?.FindMethod(expr.Method.Lexeme);

        if (method is null)
        {
            throw new RuntimeException(expr.Method, $"Undefined property '{expr.Method.Lexeme}'.");
        }
        
        return method.Bind(obj!);
    }

    public object? VisitClassStmt(Stmt.Class stmt)
    {
        object? superclass = null;
        if (stmt.Superclass is not null)
        {
            superclass = Evaluate(stmt.Superclass);
            if (superclass is not LoxClass)
            {
                throw new RuntimeException(stmt.Superclass.Name, "Superclass must be a class.");
            }
        }
        
        _environment.Define(stmt.Name.Lexeme, null);

        if (stmt.Superclass is not null)
        {
            _environment = new Environ(_environment);
            _environment.Define("super", superclass);
        }
        
        Dictionary<string, LoxFunction> methods = new();
        foreach (var method in stmt.Methods)
        {
            LoxFunction function = new(method, _environment, method.Name.Lexeme.Equals("init"));
            methods[method.Name.Lexeme] = function;
        }
        
        LoxClass klass = new(stmt.Name.Lexeme, superclass as LoxClass, methods);

        if (superclass is not null)
        {
            _environment = _environment.Enclosing!;
        }
        
        _environment.Assign(stmt.Name, klass);
        return null;
    }

    public object? VisitGetExpr(Expr.Get expr)
    {
        object? obj = Evaluate(expr.Obj);
        if (obj is LoxInstance instance)
        {
            return instance.Get(expr.Name);
        }

        throw new RuntimeException(expr.Name, "Only instances have properties.");
    }

    public object? VisitAssignExpr(Expr.Assign expr)
    {
        object? value = Evaluate(expr.Value);

        if (_locals.TryGetValue(expr, out var distance))
        {
            _environment.AssignAt(distance, expr.Name, value);
        }
        else
        {
            Globals.Assign(expr.Name, value);
        }

        return value;
    }

    public object? VisitThisExpr(Expr.This expr)
    {
        return LookUpVariable(expr.Keyword, expr);
    }
    
    public object? VisitExpressionStmt(Stmt.Expression stmt)
    {
        Evaluate(stmt.Expr);
        return null;
    }

    public object? VisitPrintStmt(Stmt.Print stmt)
    {
        object? value = Evaluate(stmt.Expr);
        Console.WriteLine(Stringify(value));
        return null;
    }

    public object? VisitVarStmt(Stmt.Var stmt)
    {
        object? value = null;
        if (stmt.Initializer is not null)
        {
            value = Evaluate(stmt.Initializer);
        }
        
        _environment.Define(stmt.Name.Lexeme, value);
        return null;
    }

    public object? VisitWhileStmt(Stmt.While stmt)
    {
        while (IsTruthy(Evaluate(stmt.Condition)))
        {
            Execute(stmt.Body);
        }

        return null;
    }

    public object? VisitFunctionStmt(Stmt.Function stmt)
    {
        LoxFunction function = new(stmt, _environment, false);
        _environment.Define(stmt.Name.Lexeme, function);
        return null;
    }

    public object? VisitReturnStmt(Stmt.Return stmt)
    {
        object? value = null;
        if (stmt.Value is not null) value = Evaluate(stmt.Value);
        throw new Return(value);
    }

    public object? VisitSetExpr(Expr.Set expr)
    {
        object? obj = Evaluate(expr.Obj);
        if (obj is not LoxInstance instance)
        {
            throw new RuntimeException(expr.Name, "Only instances have fields.");
        }
        object? value = Evaluate(expr.Value);
        instance.Set(expr.Name, value);
        return value;
    }

    public object? VisitCallExpr(Expr.Call expr)
    {
        object? callee = Evaluate(expr.Callee);

        List<object?> arguments = new();
        foreach (Expr argument in expr.Arguments)
        {
            arguments.Add(Evaluate(argument));
        }
        
        if (callee is not LoxCallable function)
        {
            throw new RuntimeException(expr.Paren, "Can only call functions and classes.");
        }

        if (arguments.Count != function.Arity())
        {
            throw new RuntimeException(expr.Paren,
                $"Expected {function.Arity()} arguments but got {arguments.Count}.");
        }
        
        return function.Call(this, arguments);
    }

    public object? VisitLogicalExpr(Expr.Logical expr)
    {
        object? left = Evaluate(expr.Left);

        if (expr.Oper.Type is TokenType.Or)
        {
            if (IsTruthy(left)) return left;
        }
        else
        {
            if (!IsTruthy(left)) return left;
        }
        
        return Evaluate(expr.Right);
    }

    public object? VisitVariableExpr(Expr.Variable expr)
    {
        return LookUpVariable(expr.Name, expr);
    }

    private object? LookUpVariable(Token name, Expr expr)
    {
        if (_locals.TryGetValue(expr, out var distance))
        {
            return _environment.GetAt(distance, name.Lexeme);
        }
        return Globals.Get(name);
    }
    
    public object? VisitLiteralExpr(Expr.Literal expr)
    {
        return expr.Value;
    }

    public object? VisitGroupingExpr(Expr.Grouping expr)
    {
        return Evaluate(expr.Expression);
    }

    public object? VisitUnaryExpr(Expr.Unary expr)
    {
        object? right = Evaluate(expr.Right);

        switch (expr.Operator.Type)
        {
            case TokenType.Bang:
                return !IsTruthy(right);
            case TokenType.Minus:
                CheckNumberOperand(expr.Operator, right);
                return -(double)right!;
        }

        return null;
    }

    public object? VisitBinaryExpr(Expr.Binary expr)
    {
        object? left = Evaluate(expr.Left);
        object? right = Evaluate(expr.Right);

        switch (expr.Operator.Type)
        {
            case TokenType.BangEqual: return !IsEqual(left, right);
            case TokenType.EqualEqual: return IsEqual(left, right);
            case TokenType.Greater:
                CheckNumberOperands(expr.Operator, left, right);
                return (double)left! > (double)right!;
            case TokenType.GreaterEqual:
                CheckNumberOperands(expr.Operator, left, right);
                return (double)left! >= (double)right!;
            case TokenType.Less:
                CheckNumberOperands(expr.Operator, left, right);
                return (double)left! < (double)right!;
            case TokenType.LessEqual:
                CheckNumberOperands(expr.Operator, left, right);
                return (double)left! <= (double)right!;
            case TokenType.Minus:
                CheckNumberOperands(expr.Operator, left, right);
                return (double)left! - (double)right!;
            case TokenType.Slash:
                CheckNumberOperands(expr.Operator, left, right);
                return (double)left! / (double)right!;
            case TokenType.Star:
                CheckNumberOperands(expr.Operator, left, right);
                return (double)left! * (double)right!;
            case TokenType.Plus:
                if (left is double dl && right is double dr)
                {
                    return dl + dr;
                }

                if (left is string sl && right is string sr)
                {
                    return sl + sr;
                }

                throw new RuntimeException(expr.Operator, 
                    "Operands must be two numbers or two strings.");
        }

        return null;
    }

    private void CheckNumberOperand(Token oper, object? operand)
    {
        if (operand is double) return;
        throw new RuntimeException(oper, "Operand must be a number.");
    }

    private void CheckNumberOperands(Token oper, object? left, object? right)
    {
        if (left is double && right is double) return;

        throw new RuntimeException(oper, "Operands must be numbers.");
    }

    private bool IsEqual(object? a, object? b)
    {
        if (a is null && b is null) return true;
        if (a is null) return false;

        return a.Equals(b);
    }
    
    private bool IsTruthy(object? obj)
    {
        if (obj is null) return false;
        if (obj is bool boolVal) return boolVal;
        if (obj is string strVal && string.IsNullOrEmpty(strVal)) return false;
        return true;
    }

    private object? Evaluate(Expr expr)
    {
        return expr.Accept(this);
    }

    private void Execute(Stmt stmt)
    {
        stmt.Accept(this);
    }

    public void Resolve(Expr expr, int depth)
    {
        _locals.Add(expr, depth);
    }

    public void ExecuteBlock(List<Stmt?> statements, Environ env)
    {
        Environ previous = this._environment;
        try
        {
            _environment = env;

            foreach (var statement in statements)
            {
                Execute(statement!);
            }
        }
        finally
        {
            _environment = previous;
        }
    }

    private string Stringify(object? obj)
    {
        if (obj is null) return "nil";

        if (obj is double)
        {
            string text = obj.ToString()!;
            if (text.EndsWith(".0")) {
                text = text.Substring(0, text.Length - 2);
            }
            return text;
        }
        
        return obj.ToString()!;
    }
}