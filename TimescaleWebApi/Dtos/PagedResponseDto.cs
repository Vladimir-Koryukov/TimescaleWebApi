namespace TimescaleWebApi.Dtos;

public record PagedResponseDto<T>(
    int Page,
    int PageSize,
    long Total,
    IReadOnlyList<T> Items
);