namespace sLox;

class Lox
{
    private static bool _hadError;
    private static bool _hadRuntimeError;
    
    private static readonly Interpreter Interpreter = new();
    
    
    static void Main(string[] args)
    {
        if (args.Length > 1)
        {
            Console.WriteLine("Usage: sLox [script]");
            Environment.Exit(64);
        }
        else if (args.Length == 1)
        {
            RunFile(args[0]);
        }
        else
        {
            RunPrompt();
        }
    }

    private static void RunFile(string path)
    {
        var str = File.ReadAllText(path);
        Run(str);
        if (_hadError) Environment.Exit(65);
        if (_hadRuntimeError) Environment.Exit(70);
    }

    private static void RunPrompt()
    {
        for (;;)
        {
            Console.WriteLine("> ");
            var line = Console.ReadLine();
            if (line is null) break;
            Run(line);
            _hadError = false;
        }
    }

    private static void Run(string source)
    {
        Scanner scanner = new(source);
        List<Token> tokens = scanner.ScanTokens();

        Parser parser = new Parser(tokens);
        List<Stmt> statements = parser.Parse();
        
        if (_hadError) return;

        Resolver resolver = new(Interpreter);
        resolver.Resolve(statements!);
        
        if (_hadError) return;
        
        Interpreter.Interpret(statements);
    }

    public static void Error(int line, string message)
    {
        Report(line, "", message);
    }

    public static void Error(Token token, string message)
    {
        if (token.Type == TokenType.Eof)
        {
            Report(token.Line, "at end", message);
            return;
        }
        Report(token.Line, $"at '{token.Lexeme}'", message);
    }

    public static void RuntimeError(RuntimeException exception)
    {
        Console.Error.WriteLine($"{exception.Message} \n[line {exception.Token.Line}]");
        _hadRuntimeError = true;
    }

    private static void Report(int line, string where, string message)
    {
        Console.Error.WriteLine($"[line {line}] Error {where}: {message}");
        _hadError = true;
    }
}