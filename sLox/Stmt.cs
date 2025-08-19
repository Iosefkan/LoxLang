namespace sLox;

public abstract record Stmt
{
	public interface IVisitor<T>
	{
		T VisitBlockStmt(Block stmt);
		T VisitExpressionStmt(Expression stmt);
		T VisitFunctionStmt(Function stmt);
		T VisitIfStmt(If stmt);
		T VisitPrintStmt(Print stmt);
		T VisitReturnStmt(Return stmt);
		T VisitVarStmt(Var stmt);
		T VisitWhileStmt(While stmt);
	}
	public record Block(List<Stmt?> Statements) : Stmt
	{
		public override T Accept<T>(IVisitor<T>  visitor)
		{
			return visitor.VisitBlockStmt(this);
		}
	}
	public record Expression(Expr Expr) : Stmt
	{
		public override T Accept<T>(IVisitor<T>  visitor)
		{
			return visitor.VisitExpressionStmt(this);
		}
	}
	public record Function(Token Name, List<Token> Params, List<Stmt?> Body) : Stmt
	{
		public override T Accept<T>(IVisitor<T>  visitor)
		{
			return visitor.VisitFunctionStmt(this);
		}
	}
	public record If(Expr Condition, Stmt ThenBranch, Stmt? ElseBranch) : Stmt
	{
		public override T Accept<T>(IVisitor<T>  visitor)
		{
			return visitor.VisitIfStmt(this);
		}
	}
	public record Print(Expr Expr) : Stmt
	{
		public override T Accept<T>(IVisitor<T>  visitor)
		{
			return visitor.VisitPrintStmt(this);
		}
	}
	public record Return(Token Keyword, Expr? Value) : Stmt
	{
		public override T Accept<T>(IVisitor<T>  visitor)
		{
			return visitor.VisitReturnStmt(this);
		}
	}
	public record Var(Token Name, Expr? Initializer) : Stmt
	{
		public override T Accept<T>(IVisitor<T>  visitor)
		{
			return visitor.VisitVarStmt(this);
		}
	}
	public record While(Expr Condition, Stmt Body) : Stmt
	{
		public override T Accept<T>(IVisitor<T>  visitor)
		{
			return visitor.VisitWhileStmt(this);
		}
	}
	public abstract T Accept<T>(IVisitor<T> visitor);
}
