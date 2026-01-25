using System.ComponentModel.DataAnnotations;

namespace TimescaleWebApi.Entities;

public class FileResult
{
    public long Id { get; set; }

    [Required]
    [MaxLength(260)]
    public string FileName { get; set; } = null!;

    public DateTime MinDate { get; set; }

    public double DeltaTimeSeconds { get; set; }

    public double AvgExecutionTime { get; set; }

    public double AvgValue { get; set; }

    public double MedianValue { get; set; }

    public double MinValue { get; set; }

    public double MaxValue { get; set; }
}