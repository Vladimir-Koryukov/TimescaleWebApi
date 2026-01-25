using System.ComponentModel.DataAnnotations;

namespace TimescaleWebApi.Entities;

public class FileValue
{
    public long Id { get; set; }

    [Required]
    [MaxLength(260)]
    public string FileName { get; set; } = null!;

    public DateTime Date { get; set; }

    public double ExecutionTime { get; set; }

    public double Value { get; set; }
}