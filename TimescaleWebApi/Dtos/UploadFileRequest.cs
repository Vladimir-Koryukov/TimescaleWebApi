using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace TimescaleWebApi.Dtos;

public class UploadFileRequest
{
    [Required]
    public IFormFile File { get; set; } = null!;
}