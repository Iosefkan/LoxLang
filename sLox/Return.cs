namespace sLox;

public class Return : Exception
{
    public object? Value { get; private set; }

    public Return(object? value)
    {
        Value = value;
    } 
}