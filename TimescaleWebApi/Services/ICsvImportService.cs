using TimescaleWebApi.Dtos;

namespace TimescaleWebApi.Services;

public interface ICsvImportService
{
    Task<UploadResponseDto> ImportAsync(string fileName, Stream csvStream, CancellationToken ct);
}