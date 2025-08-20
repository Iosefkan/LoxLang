namespace sLox;

public class Parser
{
    private class ParseException : Exception;
    
    private readonly List<Token> _tokens;
    private int current = 0;
    private bool IsAtEnd => Peek.Type == TokenType.Eof;
    private Token Peek => _tokens[current];
    private Token Previous => _tokens[current - 1];

    public Parser(List<Token> tokens)
    {
        _tokens = tokens;
    }

    public List<Stmt> Parse()
    {
        List<Stmt> statements = new();
        while (!IsAtEnd)
        {
            statements.Add(Declaration());
        }
        return statements;
    }

    private Stmt? Declaration()
    {
        try
        {
            if (Match(TokenType.Class)) return ClassDeclaration();
            if (Match(TokenType.Fun)) return Function("function");
            if (Match(TokenType.Var)) return VarDeclaration();

            return Statement();
        }
        catch (ParseException)
        {
            Synchronize();
            return null;
        }
    }

    private Stmt ClassDeclaration()
    {
        Token name = Consume(TokenType.Identifier, "Expect class name.");

        Expr.Variable? superclass = null;
        if (Match(TokenType.Less))
        {
            Consume(TokenType.Identifier, "Expect class superclass name.");
            superclass = new Expr.Variable(Previous);
        }
        
        Consume(TokenType.LeftBrace, "Expect '{' before class body.");
        List<Stmt.Function> methods = new();
        while (!Check(TokenType.RightBrace) && !IsAtEnd)
        {
            methods.Add(Function("method"));
        }
        Consume(TokenType.RightBrace, "Expect '}' after class body.");
        
        return new Stmt.Class(name, superclass, methods);
    }

    private Stmt.Function Function(string kind)
    {
        Token name = Consume(TokenType.Identifier, $"Expect {kind} name.");
        Consume(TokenType.LeftParen, $"Expect '(' after {kind} name.");
        List<Token> parameters = new();
        if (!Check(TokenType.RightParen))
        {
            do
            {
                if (parameters.Count >= 255)
                {
                    Error(Peek, "Can't have more than 255 parameters.");
                }

                parameters.Add(Consume(TokenType.Identifier, "Expect parameter name."));
            } while (Match(TokenType.Comma));
        }

        Consume(TokenType.RightParen, "Expect ')' after parameters.");
        Consume(TokenType.LeftBrace, $"Expect '{{' before {kind} body.");
        List<Stmt?> body = Block();
        return new Stmt.Function(name, parameters, body);
    }

    private Stmt VarDeclaration()
    {
        Token name = Consume(TokenType.Identifier, "Expect a variable name");

        Expr? initializer = null;
        if (Match(TokenType.Equal))
        {
            initializer = Expression();
        }

        Consume(TokenType.Semicolon, "Expect ';' after variable declaration");
        return new Stmt.Var(name, initializer);
    }
    
    private Stmt Statement()
    {
        if (Match(TokenType.Return)) return ReturnStatement();
        if (Match(TokenType.For)) return ForStatement();
        if (Match(TokenType.While)) return WhileStatement();
        if (Match(TokenType.If)) return IfStatement();
        if (Match(TokenType.Print)) return PrintStatement();
        if (Match(TokenType.LeftBrace)) return new Stmt.Block(Block());

        return ExpressionStatement();
    }

    private Stmt ReturnStatement()
    {
        Token keyword = Previous;
        Expr? value = null;
        if (!Check(TokenType.Semicolon)) value = Expression();
        
        Consume(TokenType.Semicolon, "Expect ';' after return value.");
        return new Stmt.Return(keyword, value);
    }

    private Stmt ForStatement()
    {
        Consume(TokenType.LeftParen, "Expect '(' after 'for'.");

        Stmt? initializer;
        if (Match(TokenType.Semicolon)) initializer = null;
        else if (Match(TokenType.Var)) initializer = VarDeclaration();
        else initializer = ExpressionStatement();

        Expr? condition = null;
        if (!Check(TokenType.Semicolon)) condition = Expression();
        Consume(TokenType.Semicolon, "Expect ';' after loop condition.");
        
        Expr? increment = null;
        if (!Check(TokenType.RightParen)) increment = Expression();
        Consume(TokenType.RightParen, "Expect ')' after for clauses.");
        Stmt body = Statement();

        if (increment is not null)
        {
            body = new Stmt.Block([body, new Stmt.Expression(increment)]);
        }
        condition ??= new Expr.Literal(true);
        body = new Stmt.While(condition, body);
        if (initializer is not null)
        {
            body = new Stmt.Block([initializer, body]);
        }

        return body;
    }

    private Stmt WhileStatement()
    {
        Consume(TokenType.LeftParen, "Expect '(' after 'while'.");
        Expr condition = Expression();
        Consume(TokenType.RightParen, "Expect ')' after condition.");
        Stmt body = Statement();

        return new Stmt.While(condition, body);
    }

    private Stmt IfStatement()
    {
        Consume(TokenType.LeftParen, "Expect '(' after 'if'.");
        Expr condition = Expression();
        Consume(TokenType.RightParen, "Expect ')' after if condition.");

        Stmt thenBranch = Statement();
        Stmt? elseBranch = null;
        if (Match(TokenType.Else))
        {
            elseBranch = Statement();
        }
        
        return new Stmt.If(condition, thenBranch, elseBranch);
    }

    private List<Stmt?> Block()
    {
        List<Stmt?> statements = new();
        while (!Check(TokenType.RightBrace) && !IsAtEnd)
        {
            statements.Add(Declaration());
        }

        Consume(TokenType.RightBrace, "Expect '}' after block.");
        return statements;
    }

    private Stmt PrintStatement()
    {
        Expr value = Expression();
        Consume(TokenType.Semicolon, "Expect ';' after value.");
        return new Stmt.Print(value);
    }

    private Stmt ExpressionStatement()
    {
        Expr expr = Expression();
        Consume(TokenType.Semicolon, "Expect ';' after expression.");
        return new Stmt.Expression(expr);
    }

    private Expr Expression()
    {
        return Assignment();
    }

    private Expr Assignment()
    {
        Expr expr = Or();

        if (Match(TokenType.Equal))
        {
            Token equals = Previous;
            Expr value = Assignment();
            if (expr is Expr.Variable variable)
            {
                Token name = variable.Name;
                return new Expr.Assign(name, value);
            }

            if (expr is Expr.Get get)
            {
                return new Expr.Set(get.Obj, get.Name, value);
            }

            Error(equals, "Invalid assignment target.");
        }

        return expr;
    }

    private Expr Or()
    {
        Expr expr = And();

        while (Match(TokenType.Or))
        {
            Token oper = Previous;
            Expr right = And();
            expr = new Expr.Logical(expr, oper, right);
        }

        return expr;
    }

    private Expr And()
    {
        Expr expr = Equality();

        while (Match(TokenType.And))
        {
            Token oper = Previous;
            Expr right = Equality();
            expr = new Expr.Logical(expr, oper, right);
        }

        return expr;
    }

    private Expr Equality()
    {
        Expr expr = Comparison();

        while (Match(TokenType.BangEqual, TokenType.EqualEqual))
        {
            Token oper = Previous;
            Expr right = Comparison();
            expr = new Expr.Binary(expr, oper, right);
        }

        return expr;
    }

    private Expr Comparison()
    {
        Expr expr = Term();

        while (Match(TokenType.Greater, TokenType.GreaterEqual, TokenType.Less, TokenType.LessEqual))
        {
            Token oper = Previous;
            Expr right = Term();
            expr = new Expr.Binary(expr, oper, right);
        }
        
        return expr;
    }

    private Expr Term()
    {
        Expr expr = Factor();

        while (Match(TokenType.Minus, TokenType.Plus))
        {
            Token oper = Previous;
            Expr right = Factor();
            expr = new Expr.Binary(expr, oper, right);
        }
        
        return expr;
    }

    private Expr Factor()
    {
        Expr expr = Unary();

        while (Match(TokenType.Slash, TokenType.Star))
        {
            Token oper = Previous;
            Expr right = Unary();
            expr = new Expr.Binary(expr, oper, right);
        }
        
        return expr;
    }

    private Expr Unary()
    {
        if (Match(TokenType.Bang, TokenType.Minus))
        {
            Token oper = Previous;
            Expr right = Unary();
            return new Expr.Unary(oper, right);
        }

        return Call();
    }

    private Expr Call()
    {
        Expr expr = Primary();

        while (true)
        {
            if (Match(TokenType.LeftParen))
            {
                expr = FinishCall(expr);
            }
            else if (Match(TokenType.Dot))
            {
                Token name = Consume(TokenType.Identifier, "Expect property name after '.'.");
                expr = new Expr.Get(expr, name);
            }
            else break;
        }

        return expr;
    }

    private Expr FinishCall(Expr callee)
    {
        List<Expr> arguments = new();
        if (!Check(TokenType.RightParen))
        {
            do
            {
                if (arguments.Count >= 255)
                {
                    Error(Peek, "Can't have more than 255 arguments.");
                }
                arguments.Add(Expression());
            } while (Match(TokenType.Comma));
        }

        Token paren = Consume(TokenType.RightParen, "Expect ')' after arguments.");
        
        return new Expr.Call(callee, paren, arguments);
    }

    private Expr Primary()
    {
        if (Match(TokenType.False)) return new Expr.Literal(false);
        if (Match(TokenType.True)) return new Expr.Literal(true);
        if (Match(TokenType.Nil)) return new Expr.Literal(null);

        if (Match(TokenType.Number, TokenType.String))
        {
            return new Expr.Literal(Previous.Literal);
        }

        if (Match(TokenType.Super))
        {
            Token keyword = Previous;
            Consume(TokenType.Dot, "Expect '.' after 'super'.");
            Token method = Consume(TokenType.Identifier, "Expect superclass method name.");
            return new Expr.Super(keyword, method);
        }

        if (Match(TokenType.This)) return new Expr.This(Previous);

        if (Match(TokenType.Identifier))
        {
            return new Expr.Variable(Previous);
        }

        if (Match(TokenType.LeftParen))
        {
            Expr expr = Expression();
            Consume(TokenType.RightParen, "Expect ')' after expression.");
            return new Expr.Grouping(expr);
        }
        
        throw Error(Peek, "Expect expression.");
    }

    private bool Match(params TokenType[] types)
    {
        foreach (TokenType type in types)
        {
            if (Check(type))
            {
                Advance();
                return true;
            }
        }

        return false;
    }

    private Token Consume(TokenType type, string message)
    {
        if (Check(type)) return Advance();

        throw Error(Peek, message);
    }

    private ParseException Error(Token token, string message)
    {
        Lox.Error(token, message);
        return new ParseException();
    }

    private void Synchronize()
    {
        Advance();
        while (!IsAtEnd)
        {
            if (Previous.Type == TokenType.Semicolon) return;

            switch (Peek.Type)
            {
                case TokenType.Class:
                case TokenType.Fun:
                case TokenType.Var:
                case TokenType.For:
                case TokenType.If:
                case TokenType.While:
                case TokenType.Print:
                case TokenType.Return:
                    return;
            }
            
            Advance();
        }
    }

    private Token Advance()
    {
        if (!IsAtEnd) current++;
        return Previous;
    }

    private bool Check(TokenType type)
    {
        if (IsAtEnd) return false;
        return Peek.Type == type;
    }
}