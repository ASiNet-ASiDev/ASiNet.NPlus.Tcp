namespace ASiNet.NPlus.Tcp.Exceptions;
public class GeneratorException : Exception
{
    public GeneratorException(string message) : base(message) { }

    public GeneratorException(string message, Exception inner) : base(message, inner) { }
}
