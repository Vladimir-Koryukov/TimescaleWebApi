namespace TimescaleWebApi.Dtos;

public record FileValueDto(
    DateTime Date,
    double ExecutionTime,
    double Value
);