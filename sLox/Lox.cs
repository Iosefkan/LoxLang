using System.Text;

namespace sLox;

class Lox
{
    public static bool HadError = false;
    public static bool HadRuntimeError = false;
    
    private static readonly Interpreter Interpreter = new Interpreter();
    
    
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

    /*public class GenerateAst
    {
        public static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.Error.WriteLine("Usage: generate_ast <output directory>");
                Environment.Exit(64);
            }
            string outputDir = args[0];
            DefineAst(outputDir, "Stmt",
            [
                "Block : List<Stmt?> Statements",
                "Expression : Expr Expr",
                "Function : Token Name, List<Token> Params, List<Stmt?> Body",
                "If : Expr Condition, Stmt ThenBranch, Stmt? ElseBranch",
                "Print : Expr Expr",
                "Return : Token Keyword, Expr? Value",
                "Var : Token Name, Expr? Initializer",
                "While : Expr Condition, Stmt Body",
            ]);
            
            DefineAst(outputDir, "Expr",
            [
                "Assign : Token Name, Expr Value",
                "Binary : Expr Left, Token Operator, Expr Right",
                "Call : Expr Callee, Token Paren, List<Expr> Arguments",
                "Grouping : Expr Expression",
                "Literal : object? Value",
                "Logical : Expr Left, Token Oper, Expr Right",
                "Unary : Token Operator, Expr Right",
                "Variable : Token Name"
            ]);
        }

        static void DefineAst(string outputDir, string baseName, string[] types)
        {
            string path = outputDir + Path.DirectorySeparatorChar + baseName + ".cs";
            using StreamWriter writer = new StreamWriter(path);
            writer.WriteLine("namespace sLox;");
            writer.WriteLine("");
            writer.WriteLine($"public abstract record {baseName}");
            writer.WriteLine("{");
            DefineVisitor(writer, baseName, types);
            foreach (string type in types)
            {
                var split = type.Split(":", StringSplitOptions.TrimEntries);
                string className = split[0];
                string fields = split[1];
                writer.WriteLine($"\tpublic record {className}({fields}) : {baseName}");
                writer.WriteLine("\t{");
                writer.WriteLine("\t\tpublic override T Accept<T>(IVisitor<T>  visitor)");
                writer.WriteLine("\t\t{");
                writer.WriteLine($"\t\t\treturn visitor.Visit{className}{baseName}(this);");
                writer.WriteLine("\t\t}");
                writer.WriteLine("\t}");
            }
            writer.WriteLine("\tpublic abstract T Accept<T>(IVisitor<T> visitor);");
            writer.WriteLine("}");  
        }

        static void DefineVisitor(StreamWriter writer, string baseName, string[] types)
        {
            writer.WriteLine("\tpublic interface IVisitor<T>");
            writer.WriteLine("\t{");
            foreach (string type in types)
            {
                var typeName = type.Split(":", StringSplitOptions.TrimEntries)[0];
                writer.WriteLine($"\t\tT Visit{typeName}{baseName}({typeName} {baseName.ToLower()});");
            }
            writer.WriteLine("\t}");
        }
    }*/

    private static void RunFile(string path)
    {
        var str = File.ReadAllText(path);
        Run(str);
        if (HadError) Environment.Exit(65);
        if (HadRuntimeError) Environment.Exit(70);
    }

    private static void RunPrompt()
    {
        for (;;)
        {
            Console.WriteLine("> ");
            var line = Console.ReadLine();
            if (line is null) break;
            Run(line);
            HadError = false;
        }
    }

    private static void Run(string source)
    {
        Scanner scanner = new(source);
        List<Token> tokens = scanner.ScanTokens();

        Parser parser = new Parser(tokens);
        List<Stmt> statements = parser.Parse();
        
        if (HadError) return;
        
        Interpreter.Interpret(statements);
    }

    public static void Error(int line, string message)
    {
        Report(line, "", message);
    }

    public static void Error(Token token, string message)
    {
        if (token.Type == TokenType.EOF)
        {
            Report(token.Line, "at end", message);
            return;
        }
        Report(token.Line, $"at '{token.Lexeme}'", message);
    }

    public static void RuntimeError(RuntimeException exception)
    {
        Console.Error.WriteLine($"{exception.Message} \n[line {exception.Token.Line}]");
        HadRuntimeError = true;
    }

    private static void Report(int line, string where, string message)
    {
        Console.Error.WriteLine($"[line {line}] Error {where}: {message}");
        HadError = true;
    }
}