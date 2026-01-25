using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TimescaleWebApi.Data;
using TimescaleWebApi.Dtos;
using TimescaleWebApi.Services;

namespace TimescaleWebApi.Controllers;

[ApiController]
[Route("api/files")]
public class FilesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICsvImportService _importService;

    public FilesController(AppDbContext db, ICsvImportService importService)
    {
        _db = db;
        _importService = importService;
    }

    [HttpPost("upload")]
    [RequestSizeLimit(10_000_000)]
    public async Task<ActionResult<UploadResponseDto>> Upload([FromForm] UploadFileRequest request, CancellationToken ct)
    {
        if (request.File is null || request.File.Length == 0)
            return Problem(title: "Bad Request", detail: "File is required.", statusCode: 400);

        var safeFileName = Path.GetFileName(request.File.FileName);

        if (string.IsNullOrWhiteSpace(safeFileName))
            return Problem(title: "Bad Request", detail: "Invalid file name.", statusCode: 400);

        await using var stream = request.File.OpenReadStream();
        var result = await _importService.ImportAsync(safeFileName, stream, ct);
        return Ok(result);
    }

    [HttpGet("{fileName}/values/latest")]
    public async Task<ActionResult<List<FileValueDto>>> GetLatestValues([FromRoute] string fileName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return BadRequestProblem("fileName is required.");

        var items = await _db.Values
            .AsNoTracking()
            .Where(v => v.FileName == fileName)
            .OrderByDescending(v => v.Date)
            .Take(10)
            .Select(v => new FileValueDto(v.Date, v.ExecutionTime, v.Value))
            .ToListAsync(ct);

        if (items.Count == 0)
            return Problem(
                title: "Not Found",
                detail: $"File '{fileName}' not found.",
                statusCode: StatusCodes.Status404NotFound);

        return Ok(items);

    }

    private ActionResult BadRequestProblem(string detail) =>
        Problem(title: "Bad Request", detail: detail, statusCode: StatusCodes.Status400BadRequest);
}