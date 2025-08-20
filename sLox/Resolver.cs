namespace sLox;

public class Resolver : Expr.IVisitor<object?>, Stmt.IVisitor<object?>
{
    private readonly Interpreter _interpreter;
    private readonly Stack<Dictionary<string, bool>> _scopes = new();
    private FunctionType _currentFunction = FunctionType.None;
    private ClassType _currentClass = ClassType.None;

    public Resolver(Interpreter interpreter)
    {
        _interpreter = interpreter;
    }

    public object? VisitClassStmt(Stmt.Class stmt)
    {
        ClassType enclosingClass = _currentClass;
        _currentClass = ClassType.Class;
        
        Declare(stmt.Name);
        Define(stmt.Name);

        if (stmt.Superclass is not null && stmt.Name.Lexeme.Equals(stmt.Superclass.Name.Lexeme))
        {
            Lox.Error(stmt.Superclass.Name, "A class can't inherit from itself.");
        }
        
        if (stmt.Superclass is not null)
        {
            _currentClass = ClassType.Subclass;
            Resolve(stmt.Superclass);
            
            BeginScope();
            _scopes.Peek()["super"] = true;
        }
        
        BeginScope();
        _scopes.Peek()["this"] = true;

        foreach (var method in stmt.Methods)
        {
            var declaration = FunctionType.Method;
            if (method.Name.Lexeme.Equals("init"))
            {
                declaration = FunctionType.Initializer;
            }
            ResolveFunction(method, declaration);
        }
        
        EndScope();
        if (stmt.Superclass is not null) EndScope();
        _currentClass = enclosingClass;
        
        return null;
    }

    public object? VisitBlockStmt(Stmt.Block stmt)
    {
        BeginScope();
        Resolve(stmt.Statements);
        EndScope();
        return null;
    }

    public object? VisitVarStmt(Stmt.Var stmt)
    {
        Declare(stmt.Name);
        if (stmt.Initializer is not null)
        {
            Resolve(stmt.Initializer);
        }
        Define(stmt.Name);
        return null;
    }

    private void Declare(Token name)
    {
        if (_scopes.Count == 0) return;
        var scope = _scopes.Peek();
        if (scope.ContainsKey(name.Lexeme))
        {
            Lox.Error(name, "Already variable with this name in this scope.");
        }
        scope.Add(name.Lexeme, false);
    }

    private void Define(Token name)
    {
        if (_scopes.Count == 0) return;
        _scopes.Peek()[name.Lexeme] = true;
    }

    public object? VisitVariableExpr(Expr.Variable expr)
    {
        if (_scopes.Count != 0 && 
            _scopes.Peek().TryGetValue(expr.Name.Lexeme, out var val) && 
            val is false)
        {
            Lox.Error(expr.Name, "Can't read local variable in its own initializer.");
        }

        ResolveLocal(expr, expr.Name);
        return null;
    }

    public object? VisitAssignExpr(Expr.Assign expr)
    {
        Resolve(expr.Value);
        ResolveLocal(expr, expr.Name);
        return null;
    }

    public object? VisitFunctionStmt(Stmt.Function stmt)
    {
        Declare(stmt.Name);
        Define(stmt.Name);
        ResolveFunction(stmt, FunctionType.Function);
        return null;
    }

    public object? VisitExpressionStmt(Stmt.Expression stmt)
    {
        Resolve(stmt.Expr);
        return null;
    }

    public object? VisitIfStmt(Stmt.If stmt)
    {
        Resolve(stmt.Condition);
        Resolve(stmt.ThenBranch);
        if (stmt.ElseBranch is not null) Resolve(stmt.ElseBranch);
        return null;
    }

    public object? VisitPrintStmt(Stmt.Print stmt)
    {
        Resolve(stmt.Expr);
        return null;
    }

    public object? VisitReturnStmt(Stmt.Return stmt)
    {
        if (_currentFunction == FunctionType.None)
        {
            Lox.Error(stmt.Keyword, "Can't return from a top-level code.");
        }
        
        if (stmt.Value is not null)
        {
            if (_currentFunction is FunctionType.Initializer)
            {
                Lox.Error(stmt.Keyword, "Can't return a value from an initializer.");
            }
            Resolve(stmt.Value);
        }
        return null;
    }

    public object? VisitWhileStmt(Stmt.While stmt)
    {
        Resolve(stmt.Condition);
        Resolve(stmt.Body);
        return null;
    }

    public object? VisitSuperExpr(Expr.Super expr)
    {
        if (_currentClass is ClassType.None)
        {
            Lox.Error(expr.Keyword, "Can't use 'super' outside of a class.");
        }
        else if (_currentClass is not ClassType.Subclass)
        {
            Lox.Error(expr.Keyword, "Can't use 'super' in a class with no superclass.");
        }
        
        ResolveLocal(expr, expr.Keyword);
        return null;
    }

    public object? VisitThisExpr(Expr.This expr)
    {
        if (_currentClass is ClassType.None)
        {
            Lox.Error(expr.Keyword, "Can't use 'this' outside of a class.");
            return null;
        }
        
        ResolveLocal(expr, expr.Keyword);
        return null;
    }

    public object? VisitSetExpr(Expr.Set expr)
    {
        Resolve(expr.Value);
        Resolve(expr.Obj);
        return null;
    }

    public object? VisitGetExpr(Expr.Get expr)
    {
        Resolve(expr.Obj);
        return null;
    }

    public object? VisitBinaryExpr(Expr.Binary expr)
    {
        Resolve(expr.Left);
        Resolve(expr.Right);
        return null;
    }

    public object? VisitCallExpr(Expr.Call expr)
    {
        Resolve(expr.Callee);
        foreach (Expr argument in expr.Arguments)
        {
            Resolve(argument);
        }
        return null;
    }

    public object? VisitGroupingExpr(Expr.Grouping expr)
    {
        Resolve(expr.Expression);
        return null;
    }

    public object? VisitLiteralExpr(Expr.Literal expr)
    {
        return null;
    }

    public object? VisitLogicalExpr(Expr.Logical expr)
    {
        Resolve(expr.Left);
        Resolve(expr.Right);
        return null;
    }

    public object? VisitUnaryExpr(Expr.Unary expr)
    {
        Resolve(expr.Right);
        return null;
    }

    private void ResolveFunction(Stmt.Function stmt, FunctionType type)
    {
        var enclosingFunction = _currentFunction;
        _currentFunction = type;
        
        BeginScope();
        foreach (Token param in stmt.Params)
        {
            Declare(param);
            Define(param);
        }
        Resolve(stmt.Body);
        EndScope();
        _currentFunction = enclosingFunction;
    }

    private void ResolveLocal(Expr expr, Token name)
    {
        for (int i = _scopes.Count - 1; i >= 0; i--)
        {
            if (_scopes.ElementAt(i).ContainsKey(name.Lexeme))
            {
                _interpreter.Resolve(expr, i);
                return;
            }
        }
    }

    public void Resolve(List<Stmt?> statements)
    {
        foreach (var statement in statements)
        {
            Resolve(statement!);
        }
    }

    private void BeginScope()
    {
        _scopes.Push(new Dictionary<string, bool>());
    }

    private void EndScope()
    {
        _scopes.Pop();
    }

    private void Resolve(Stmt stmt)
    {
        stmt.Accept(this);
    }

    private void Resolve(Expr expr)
    {
        expr.Accept(this);
    }
}