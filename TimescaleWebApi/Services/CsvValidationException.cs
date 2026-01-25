namespace TimescaleWebApi.Services;

public class CsvValidationException : Exception
{
    public int LineNumber { get; }

    public CsvValidationException(int lineNumber, string message)
        : base(message)
    {
        LineNumber = lineNumber;
    }
}