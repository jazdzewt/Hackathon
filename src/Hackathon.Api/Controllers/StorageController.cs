using Microsoft.AspNetCore.Mvc;
using Supabase;

namespace Hackathon.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StorageController : ControllerBase
{
    private readonly Supabase.Client _supabase;
    private readonly ILogger<StorageController> _logger;

    public StorageController(Supabase.Client supabase, ILogger<StorageController> logger)
    {
        _supabase = supabase;
        _logger = logger;
    }

    /// <summary>
    /// Lista wszystkich bucket'ów (pojemników na pliki)
    /// </summary>
    [HttpGet("buckets")]
    public async Task<IActionResult> GetBuckets()
    {
        try
        {
            var buckets = await _supabase.Storage.ListBuckets();
            
            return Ok(new
            {
                message = "✅ Lista bucket'ów",
                count = buckets?.Count ?? 0,
                buckets = buckets?.Select(b => new
                {
                    id = b.Id,
                    name = b.Name,
                    isPublic = b.Public,
                    createdAt = b.CreatedAt
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd pobierania bucket'ów");
            return BadRequest(new
            {
                error = "Błąd pobierania bucket'ów",
                details = ex.Message
            });
        }
    }

    /// <summary>
    /// Upload pliku do bucket'a
    /// </summary>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [ApiExplorerSettings(IgnoreApi = true)] // Ukryj w Swaggerze - użyj Postmana
    public async Task<IActionResult> UploadFile(
        [FromForm] IFormFile file,
        [FromForm] string bucketName = "uploads",
        [FromForm] string? folder = null)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "Brak pliku" });
            }

            _logger.LogInformation($"Upload pliku: {file.FileName} ({file.Length} bytes)");

            // Generuj unikalną nazwę pliku
            var fileExtension = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = string.IsNullOrEmpty(folder) ? fileName : $"{folder}/{fileName}";

            // Konwertuj IFormFile na byte array
            byte[] fileBytes;
            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                fileBytes = memoryStream.ToArray();
            }

            // Upload do Supabase Storage
            var response = await _supabase.Storage
                .From(bucketName)
                .Upload(fileBytes, filePath, new Supabase.Storage.FileOptions
                {
                    ContentType = file.ContentType,
                    Upsert = false
                });

            // Wygeneruj publiczny URL (jeśli bucket jest publiczny)
            var publicUrl = _supabase.Storage
                .From(bucketName)
                .GetPublicUrl(filePath);

            return Ok(new
            {
                message = "✅ Plik przesłany!",
                fileName = file.FileName,
                originalSize = file.Length,
                storedPath = filePath,
                publicUrl = publicUrl,
                bucket = bucketName
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd uploadu pliku");
            return BadRequest(new
            {
                error = "Błąd uploadu pliku",
                details = ex.Message
            });
        }
    }

    /// <summary>
    /// Lista plików w bucket'cie
    /// </summary>
    [HttpGet("files/{bucketName}")]
    public async Task<IActionResult> ListFiles(
        string bucketName,
        [FromQuery] string? folder = null)
    {
        try
        {
            var path = folder ?? "";
            var files = await _supabase.Storage
                .From(bucketName)
                .List(path);

            return Ok(new
            {
                message = "✅ Lista plików",
                bucket = bucketName,
                folder = folder,
                count = files?.Count ?? 0,
                files = files?.Select(f => new
                {
                    name = f.Name,
                    id = f.Id,
                    createdAt = f.CreatedAt,
                    updatedAt = f.UpdatedAt
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd pobierania listy plików");
            return BadRequest(new
            {
                error = "Błąd pobierania listy plików",
                details = ex.Message
            });
        }
    }

    /// <summary>
    /// Pobierz publiczny URL pliku
    /// </summary>
    [HttpGet("url/{bucketName}/{*filePath}")]
    public IActionResult GetPublicUrl(string bucketName, string filePath)
    {
        try
        {
            var publicUrl = _supabase.Storage
                .From(bucketName)
                .GetPublicUrl(filePath);

            return Ok(new
            {
                message = "✅ Publiczny URL",
                filePath = filePath,
                publicUrl = publicUrl
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd generowania URL");
            return BadRequest(new
            {
                error = "Błąd generowania URL",
                details = ex.Message
            });
        }
    }

    /// <summary>
    /// Pobierz plik (download)
    /// </summary>
    [HttpGet("download/{bucketName}/{*filePath}")]
    public async Task<IActionResult> DownloadFile(string bucketName, string filePath)
    {
        try
        {
            var fileBytes = await _supabase.Storage
                .From(bucketName)
                .Download(filePath, null);

            if (fileBytes == null || fileBytes.Length == 0)
            {
                return NotFound(new { error = "Plik nie został znaleziony" });
            }

            var fileName = Path.GetFileName(filePath);
            return File(fileBytes, "application/octet-stream", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd pobierania pliku");
            return BadRequest(new
            {
                error = "Błąd pobierania pliku",
                details = ex.Message
            });
        }
    }

    /// <summary>
    /// Usuń plik
    /// </summary>
    [HttpDelete("delete/{bucketName}/{*filePath}")]
    public async Task<IActionResult> DeleteFile(string bucketName, string filePath)
    {
        try
        {
            await _supabase.Storage
                .From(bucketName)
                .Remove(filePath);

            return Ok(new
            {
                message = "✅ Plik usunięty!",
                filePath = filePath,
                bucket = bucketName
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd usuwania pliku");
            return BadRequest(new
            {
                error = "Błąd usuwania pliku",
                details = ex.Message
            });
        }
    }

    /// <summary>
    /// Przenieś/zmień nazwę pliku
    /// </summary>
    [HttpPost("move")]
    public async Task<IActionResult> MoveFile(
        [FromBody] MoveFileRequest request)
    {
        try
        {
            await _supabase.Storage
                .From(request.BucketName)
                .Move(request.FromPath, request.ToPath);

            return Ok(new
            {
                message = "✅ Plik przeniesiony!",
                from = request.FromPath,
                to = request.ToPath,
                bucket = request.BucketName
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd przenoszenia pliku");
            return BadRequest(new
            {
                error = "Błąd przenoszenia pliku",
                details = ex.Message
            });
        }
    }

    /// <summary>
    /// Test połączenia z Storage
    /// </summary>
    [HttpGet("test")]
    public async Task<IActionResult> TestStorage()
    {
        try
        {
            var buckets = await _supabase.Storage.ListBuckets();
            
            return Ok(new
            {
                message = "✅ Połączenie z Supabase Storage działa!",
                bucketsCount = buckets?.Count ?? 0,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd połączenia ze Storage");
            return BadRequest(new
            {
                error = "❌ Błąd połączenia z Supabase Storage",
                details = ex.Message
            });
        }
    }
}

// Request models
public record MoveFileRequest(string BucketName, string FromPath, string ToPath);
