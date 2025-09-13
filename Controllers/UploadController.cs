using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
public class UploadController : ControllerBase
{
    private readonly BackblazeS3Service _s3Service;

    public UploadController(BackblazeS3Service s3Service)
    {
        _s3Service = s3Service;
    }

    [HttpPost]
    public async Task<IActionResult> UploadFiles(List<IFormFile> files)
    {
        if (files == null || files.Count == 0)
            return BadRequest("No files uploaded.");

        var bucketName = "your-bucket-name";
        var uploadedFiles = new List<string>();

        foreach (var file in files)
        {
            var fileName = $"{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            using var stream = file.OpenReadStream();
            var uploadedFileName = await _s3Service.UploadFileAsync(bucketName, fileName, stream);
            uploadedFiles.Add(uploadedFileName);
        }

        return Ok(new { Files = uploadedFiles });
    }
}