using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TimescaleWebApi.Data;
using TimescaleWebApi.Dtos;
using TimescaleWebApi.Entities;

namespace TimescaleWebApi.Services;

public class CsvImportService : ICsvImportService
{
    private readonly AppDbContext _db;

    public CsvImportService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<UploadResponseDto> ImportAsync(string fileName, Stream csvStream, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new CsvValidationException(0, "FileName is required.");

        var readResult = await ReadValidateAndAggregateAsync(csvStream, ct);

        var rows = readResult.Rows;
        var count = rows.Count;

        var minDate = readResult.MinDate;
        var maxDate = readResult.MaxDate;
        var deltaSeconds = (maxDate - minDate).TotalSeconds;

        var avgExecution = readResult.SumExecutionTime / count;
        var avgValue = readResult.SumValue / count;

        var minValue = readResult.MinValue;
        var maxValue = readResult.MaxValue;

        var medianValue = CalculateMedianInPlace(readResult.ValuesForMedian);

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        try
        {
            await _db.Values.Where(v => v.FileName == fileName).ExecuteDeleteAsync(ct);
            await _db.Results.Where(r => r.FileName == fileName).ExecuteDeleteAsync(ct);

            var oldAutoDetect = _db.ChangeTracker.AutoDetectChangesEnabled;
            _db.ChangeTracker.AutoDetectChangesEnabled = false;

            try
            {
                var valueEntities = rows.Select(r => new FileValue
                {
                    FileName = fileName,
                    Date = r.Date,
                    ExecutionTime = r.ExecutionTime,
                    Value = r.Value
                }).ToList();

                const int batchSize = 1000;
                for (int i = 0; i < valueEntities.Count; i += batchSize)
                {
                    var batch = valueEntities.Skip(i).Take(batchSize);
                    await _db.Values.AddRangeAsync(batch, ct);
                }

                _db.Results.Add(new FileResult
                {
                    FileName = fileName,
                    MinDate = minDate,
                    DeltaTimeSeconds = deltaSeconds,
                    AvgExecutionTime = avgExecution,
                    AvgValue = avgValue,
                    MedianValue = medianValue,
                    MinValue = minValue,
                    MaxValue = maxValue
                });

                await _db.SaveChangesAsync(ct);
            }
            finally
            {
                _db.ChangeTracker.AutoDetectChangesEnabled = oldAutoDetect;
            }

            await tx.CommitAsync(ct);

            return new UploadResponseDto(fileName, count);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    private sealed record ReadAggregateResult(
        List<ParsedRow> Rows,
        DateTime MinDate,
        DateTime MaxDate,
        double SumExecutionTime,
        double SumValue,
        double MinValue,
        double MaxValue,
        List<double> ValuesForMedian
    );

    private static async Task<ReadAggregateResult> ReadValidateAndAggregateAsync(Stream csvStream, CancellationToken ct)
    {
        var rows = new List<ParsedRow>(capacity: 1024);
        var valuesForMedian = new List<double>(capacity: 1024);

        var minDateAllowed = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var nowUtc = DateTime.UtcNow;

        DateTime? minDate = null;
        DateTime? maxDate = null;

        double sumExec = 0;
        double sumValue = 0;

        double minValue = double.PositiveInfinity;
        double maxValue = double.NegativeInfinity;

        using var reader = new StreamReader(csvStream);

        string? line;
        int lineNumber = 0;

        while ((line = await reader.ReadLineAsync(ct)) is not null)
        {
            lineNumber++;

            if (string.IsNullOrWhiteSpace(line))
                throw new CsvValidationException(lineNumber, "empty line.");

            var parts = line.Split(';');
            if (parts.Length != 3)
                throw new CsvValidationException(lineNumber, "expected 3 columns 'Date;ExecutionTime;Value'.");

            if (rows.Count >= 10000)
                throw new CsvValidationException(lineNumber, "file has more than 10000 rows.");

            if (!DateTime.TryParse(parts[0], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var date))
                throw new CsvValidationException(lineNumber, $"invalid Date '{parts[0]}'.");

            if (!double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var execTime))
                throw new CsvValidationException(lineNumber, $"invalid ExecutionTime '{parts[1]}'.");

            if (!double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                throw new CsvValidationException(lineNumber, $"invalid Value '{parts[2]}'.");

            var dateUtc = date.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(date, DateTimeKind.Utc)
                : date.ToUniversalTime();

            if (dateUtc < minDateAllowed)
                throw new CsvValidationException(lineNumber, "Date must be >= 2000-01-01.");

            if (dateUtc > nowUtc)
                throw new CsvValidationException(lineNumber, "Date must be <= current time.");

            if (execTime < 0)
                throw new CsvValidationException(lineNumber, "ExecutionTime must be >= 0.");

            if (value < 0)
                throw new CsvValidationException(lineNumber, "Value must be >= 0.");

            rows.Add(new ParsedRow(dateUtc, execTime, value));
            valuesForMedian.Add(value);

            sumExec += execTime;
            sumValue += value;

            if (!minDate.HasValue || dateUtc < minDate.Value) minDate = dateUtc;
            if (!maxDate.HasValue || dateUtc > maxDate.Value) maxDate = dateUtc;

            if (value < minValue) minValue = value;
            if (value > maxValue) maxValue = value;
        }

        if (rows.Count == 0)
            throw new CsvValidationException(0, "file must contain at least 1 row.");

        return new ReadAggregateResult(
            Rows: rows,
            MinDate: minDate!.Value,
            MaxDate: maxDate!.Value,
            SumExecutionTime: sumExec,
            SumValue: sumValue,
            MinValue: minValue,
            MaxValue: maxValue,
            ValuesForMedian: valuesForMedian
        );
    }

    private static double CalculateMedianInPlace(List<double> values)
    {
        if (values.Count == 0) return 0;

        values.Sort();

        int n = values.Count;
        int mid = n / 2;

        return (n % 2 == 1)
            ? values[mid]
            : (values[mid - 1] + values[mid]) / 2.0;
    }
}