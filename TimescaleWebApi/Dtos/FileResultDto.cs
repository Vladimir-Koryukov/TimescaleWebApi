namespace TimescaleWebApi.Dtos;

public record FileResultDto(
    string FileName,
    DateTime MinDate,
    double DeltaTimeSeconds,
    double AvgExecutionTime,
    double AvgValue,
    double MedianValue,
    double MinValue,
    double MaxValue
);