namespace TimescaleWebApi.Dtos;

public record ParsedRow(DateTime Date, double ExecutionTime, double Value);