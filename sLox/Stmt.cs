namespace sLox;

public abstract record Stmt
{
	public interface IVisitor<T>
	{
		T VisitBlockStmt(Block stmt);
		T VisitExpressionStmt(Expression stmt);
		T VisitPrintStmt(Print stmt);
		T VisitVarStmt(Var stmt);
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
	public record Print(Expr Expr) : Stmt
	{
		public override T Accept<T>(IVisitor<T>  visitor)
		{
			return visitor.VisitPrintStmt(this);
		}
	}
	public record Var(Token Name, Expr? Initializer) : Stmt
	{
		public override T Accept<T>(IVisitor<T>  visitor)
		{
			return visitor.VisitVarStmt(this);
		}
	}
	public abstract T Accept<T>(IVisitor<T> visitor);
}
