namespace sLox;

public class GenerateAst
{
    public static void MMain(string[] args)
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
            "Class : Token Name, Expr.Variable? Superclass, List<Stmt.Function> Methods",
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
            "Get : Expr Obj, Token Name",
            "Grouping : Expr Expression",
            "Literal : object? Value",
            "Logical : Expr Left, Token Oper, Expr Right",
            "Set : Expr Obj, Token Name, Expr Value",
            "Super : Token Keyword, Token Method",
            "This : Token Keyword",
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
}