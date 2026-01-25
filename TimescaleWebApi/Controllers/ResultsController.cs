using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TimescaleWebApi.Data;
using TimescaleWebApi.Dtos;

namespace TimescaleWebApi.Controllers;

[ApiController]
[Route("api/results")]
public class ResultsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ResultsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponseDto<FileResultDto>>> Get(
        [FromQuery] string? fileName,
        [FromQuery] DateTime? minDateFrom,
        [FromQuery] DateTime? minDateTo,
        [FromQuery] double? avgValueMin,
        [FromQuery] double? avgValueMax,
        [FromQuery] double? avgExecutionTimeMin,
        [FromQuery] double? avgExecutionTimeMax,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        // Валидация диапазонов
        if (minDateFrom.HasValue && minDateTo.HasValue && minDateFrom > minDateTo)
            return Problem(title: "Bad Request", detail: "minDateFrom must be <= minDateTo.", statusCode: 400);

        if (avgValueMin.HasValue && avgValueMax.HasValue && avgValueMin > avgValueMax)
            return Problem(title: "Bad Request", detail: "avgValueMin must be <= avgValueMax.", statusCode: 400);

        if (avgExecutionTimeMin.HasValue && avgExecutionTimeMax.HasValue && avgExecutionTimeMin > avgExecutionTimeMax)
            return Problem(title: "Bad Request", detail: "avgExecutionTimeMin must be <= avgExecutionTimeMax.", statusCode: 400);

        if (page < 1)
            return Problem(title: "Bad Request", detail: "page must be >= 1.", statusCode: 400);

        if (pageSize < 1 || pageSize > 200)
            return Problem(title: "Bad Request", detail: "pageSize must be between 1 and 200.", statusCode: 400);

        IQueryable<Entities.FileResult> query = _db.Results.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(fileName))
            query = query.Where(r => EF.Functions.ILike(r.FileName, $"%{fileName}%"));

        if (minDateFrom.HasValue)
            query = query.Where(r => r.MinDate >= minDateFrom.Value);

        if (minDateTo.HasValue)
            query = query.Where(r => r.MinDate <= minDateTo.Value);

        if (avgValueMin.HasValue)
            query = query.Where(r => r.AvgValue >= avgValueMin.Value);

        if (avgValueMax.HasValue)
            query = query.Where(r => r.AvgValue <= avgValueMax.Value);

        if (avgExecutionTimeMin.HasValue)
            query = query.Where(r => r.AvgExecutionTime >= avgExecutionTimeMin.Value);

        if (avgExecutionTimeMax.HasValue)
            query = query.Where(r => r.AvgExecutionTime <= avgExecutionTimeMax.Value);

        var total = await query.LongCountAsync(ct);

        var items = await query
            .OrderByDescending(r => r.MinDate)
            .ThenBy(r => r.FileName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new FileResultDto(
                r.FileName,
                r.MinDate,
                r.DeltaTimeSeconds,
                r.AvgExecutionTime,
                r.AvgValue,
                r.MedianValue,
                r.MinValue,
                r.MaxValue
            ))
            .ToListAsync(ct);

        return Ok(new PagedResponseDto<FileResultDto>(
            Page: page,
            PageSize: pageSize,
            Total: total,
            Items: items
        ));
    }
}