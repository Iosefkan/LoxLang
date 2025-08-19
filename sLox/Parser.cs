namespace sLox;

public class Parser
{
    private class ParseException : Exception;
    
    private readonly List<Token> _tokens;
    private int current = 0;
    private bool IsAtEnd => Peek.Type == TokenType.EOF;
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
            if (Match(TokenType.FUN)) return Function("function");
            if (Match(TokenType.VAR)) return VarDeclaration();

            return Statement();
        }
        catch (ParseException)
        {
            Synchronize();
            return null;
        }
    }

    private Stmt.Function Function(string kind)
    {
        Token name = Consume(TokenType.IDENTIFIER, $"Expect {kind} name.");
        Consume(TokenType.LEFT_PAREN, $"Expect '(' after {kind} name.");
        List<Token> parameters = new();
        if (!Check(TokenType.RIGHT_PAREN))
        {
            do
            {
                if (parameters.Count >= 255)
                {
                    Error(Peek, "Can't have more than 255 parameters.");
                }

                parameters.Add(Consume(TokenType.IDENTIFIER, "Expect parameter name."));
            } while (Match(TokenType.COMMA));
        }

        Consume(TokenType.RIGHT_PAREN, "Expect ')' after parameters.");
        Consume(TokenType.LEFT_BRACE, $"Expect '{{' before {kind} body.");
        List<Stmt?> body = Block();
        return new Stmt.Function(name, parameters, body);
    }

    private Stmt VarDeclaration()
    {
        Token name = Consume(TokenType.IDENTIFIER, "Expect a variable name");

        Expr? initializer = null;
        if (Match(TokenType.EQUAL))
        {
            initializer = Expression();
        }

        Consume(TokenType.SEMICOLON, "Expect ';' after variable declaration");
        return new Stmt.Var(name, initializer);
    }
    
    private Stmt Statement()
    {
        if (Match(TokenType.RETURN)) return ReturnStatement();
        if (Match(TokenType.FOR)) return ForStatement();
        if (Match(TokenType.WHILE)) return WhileStatement();
        if (Match(TokenType.IF)) return IfStatement();
        if (Match(TokenType.PRINT)) return PrintStatement();
        if (Match(TokenType.LEFT_BRACE)) return new Stmt.Block(Block());

        return ExpressionStatement();
    }

    private Stmt ReturnStatement()
    {
        Token keyword = Previous;
        Expr? value = null;
        if (!Check(TokenType.SEMICOLON)) value = Expression();
        
        Consume(TokenType.SEMICOLON, "Expect ';' after return value.");
        return new Stmt.Return(keyword, value);
    }

    private Stmt ForStatement()
    {
        Consume(TokenType.LEFT_PAREN, "Expect '(' after 'for'.");

        Stmt? initializer;
        if (Match(TokenType.SEMICOLON)) initializer = null;
        else if (Match(TokenType.VAR)) initializer = VarDeclaration();
        else initializer = ExpressionStatement();

        Expr? condition = null;
        if (!Check(TokenType.SEMICOLON)) condition = Expression();
        Consume(TokenType.SEMICOLON, "Expect ';' after loop condition.");
        
        Expr? increment = null;
        if (!Check(TokenType.RIGHT_PAREN)) increment = Expression();
        Consume(TokenType.RIGHT_PAREN, "Expect ')' after for clauses.");
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
        Consume(TokenType.LEFT_PAREN, "Expect '(' after 'while'.");
        Expr condition = Expression();
        Consume(TokenType.RIGHT_PAREN, "Expect ')' after condition.");
        Stmt body = Statement();

        return new Stmt.While(condition, body);
    }

    private Stmt IfStatement()
    {
        Consume(TokenType.LEFT_PAREN, "Expect '(' after 'if'.");
        Expr condition = Expression();
        Consume(TokenType.RIGHT_PAREN, "Expect ')' after if condition.");

        Stmt thenBranch = Statement();
        Stmt? elseBranch = null;
        if (Match(TokenType.ELSE))
        {
            elseBranch = Statement();
        }
        
        return new Stmt.If(condition, thenBranch, elseBranch);
    }

    private List<Stmt?> Block()
    {
        List<Stmt?> statements = new();
        while (!Check(TokenType.RIGHT_BRACE) && !IsAtEnd)
        {
            statements.Add(Declaration());
        }

        Consume(TokenType.RIGHT_BRACE, "Expect '}' after block.");
        return statements;
    }

    private Stmt PrintStatement()
    {
        Expr value = Expression();
        Consume(TokenType.SEMICOLON, "Expect ';' after value.");
        return new Stmt.Print(value);
    }

    private Stmt ExpressionStatement()
    {
        Expr expr = Expression();
        Consume(TokenType.SEMICOLON, "Expect ';' after expression.");
        return new Stmt.Expression(expr);
    }

    private Expr Expression()
    {
        return Assignment();
    }

    private Expr Assignment()
    {
        Expr expr = Or();

        if (Match(TokenType.EQUAL))
        {
            Token equals = Previous;
            Expr value = Assignment();
            if (expr is Expr.Variable variable)
            {
                Token name = variable.Name;
                return new Expr.Assign(name, value);
            }

            Error(equals, "Invalid assignment target.");
        }

        return expr;
    }

    private Expr Or()
    {
        Expr expr = And();

        while (Match(TokenType.OR))
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

        while (Match(TokenType.AND))
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

        while (Match(TokenType.BANG_EQUAL, TokenType.EQUAL_EQUAL))
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

        while (Match(TokenType.GREATER, TokenType.GREATER_EQUAL, TokenType.LESS, TokenType.LESS_EQUAL))
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

        while (Match(TokenType.MINUS, TokenType.PLUS))
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

        while (Match(TokenType.SLASH, TokenType.STAR))
        {
            Token oper = Previous;
            Expr right = Unary();
            expr = new Expr.Binary(expr, oper, right);
        }
        
        return expr;
    }

    private Expr Unary()
    {
        if (Match(TokenType.BANG, TokenType.MINUS))
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
            if (Match(TokenType.LEFT_PAREN))
            {
                expr = FinishCall(expr);
            }
            else break;
        }

        return expr;
    }

    private Expr FinishCall(Expr callee)
    {
        List<Expr> arguments = new();
        if (!Check(TokenType.RIGHT_PAREN))
        {
            do
            {
                if (arguments.Count >= 255)
                {
                    Error(Peek, "Can't have more than 255 arguments.");
                }
                arguments.Add(Expression());
            } while (Match(TokenType.COMMA));
        }

        Token paren = Consume(TokenType.RIGHT_PAREN, "Expect ')' after arguments.");
        
        return new Expr.Call(callee, paren, arguments);
    }

    private Expr Primary()
    {
        if (Match(TokenType.FALSE)) return new Expr.Literal(false);
        if (Match(TokenType.TRUE)) return new Expr.Literal(true);
        if (Match(TokenType.NIL)) return new Expr.Literal(null);

        if (Match(TokenType.NUMBER, TokenType.STRING))
        {
            return new Expr.Literal(Previous.Literal);
        }

        if (Match(TokenType.IDENTIFIER))
        {
            return new Expr.Variable(Previous);
        }

        if (Match(TokenType.LEFT_PAREN))
        {
            Expr expr = Expression();
            Consume(TokenType.RIGHT_PAREN, "Expect ')' after expression.");
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
            if (Previous.Type == TokenType.SEMICOLON) return;

            switch (Peek.Type)
            {
                case TokenType.CLASS:
                case TokenType.FUN:
                case TokenType.VAR:
                case TokenType.FOR:
                case TokenType.IF:
                case TokenType.WHILE:
                case TokenType.PRINT:
                case TokenType.RETURN:
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