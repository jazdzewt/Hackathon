using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Hackathon.Api.Models;
using Hackathon.Api.DTOs;
using Supabase;
using System.Security.Cryptography;
using System.Text;

namespace Hackathon.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SubmissionsController : ControllerBase
{
    private readonly Client _supabase;
    private readonly ILogger<SubmissionsController> _logger;
    private const string SUBMISSIONS_BUCKET = "submissions";

    public SubmissionsController(Client supabase, ILogger<SubmissionsController> logger)
    {
        _supabase = supabase;
        _logger = logger;
    }

    /// <summary>
    /// Przesyła rozwiązanie dla wyzwania
    /// TO JEST NAJWAŻNIEJSZY ENDPOINT - TU ŁĄCZY SIĘ STORAGE Z BAZĄ!
    /// </summary>
    [HttpPost]
    [EnableRateLimiting("submissions")]
    [Consumes("multipart/form-data")]
    [ApiExplorerSettings(IgnoreApi = true)] // Ukryj w Swaggerze - użyj Postmana
    public async Task<IActionResult> Submit([FromForm] IFormFile file, [FromForm] string challengeId)
    {
        try
        {
            // 1. SPRAWDŹ CZY UŻYTKOWNIK JEST ZALOGOWANY
            var userId = HttpContext.Items["UserId"]?.ToString();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "Musisz być zalogowany" });
            }

            _logger.LogInformation($"Zgłoszenie od użytkownika {userId} dla wyzwania {challengeId}");

            // 2. SPRAWDŹ CZY PLIK ISTNIEJE
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "Plik jest wymagany" });
            }

            // 3. SPRAWDŹ CZY WYZWANIE ISTNIEJE I JEST AKTYWNE
            var challenge = await _supabase
                .From<Challenge>()
                .Where(c => c.Id == challengeId)
                .Single();

            if (challenge == null)
            {
                return NotFound(new { error = "Wyzwanie nie znalezione" });
            }

            if (!challenge.IsActive)
            {
                return BadRequest(new { error = "Wyzwanie nie jest aktywne" });
            }

            if (DateTime.UtcNow > challenge.SubmissionDeadline)
            {
                return BadRequest(new { error = "Termin zgłoszeń minął" });
            }

            // 4. SPRAWDŹ ROZMIAR PLIKU
            var fileSizeMb = (decimal)file.Length / (1024 * 1024);
            if (fileSizeMb > challenge.MaxFileSizeMb)
            {
                return BadRequest(new { 
                    error = $"Plik jest za duży. Maksymalny rozmiar: {challenge.MaxFileSizeMb} MB" 
                });
            }

            // 5. OBLICZ HASH PLIKU (do wykrywania duplikatów)
            string fileHash;
            using (var stream = file.OpenReadStream())
            {
                var hashBytes = MD5.HashData(stream);
                fileHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }

            // 6. SPRAWDŹ CZY NIE JEST TO DUPLIKAT
            var existingSubmission = await _supabase
                .From<Submission>()
                .Where(s => s.UserId == userId)
                .Where(s => s.ChallengeId == challengeId)
                .Where(s => s.FileHash == fileHash)
                .Get();

            if (existingSubmission.Models.Any())
            {
                return BadRequest(new { error = "Ten plik został już wcześniej przesłany" });
            }

            // 7. UPLOAD PLIKU DO STORAGE
            var fileName = $"{userId}_{challengeId}_{Guid.NewGuid()}_{file.FileName}";
            
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            var fileBytes = memoryStream.ToArray();

            var uploadPath = $"{userId}/{challengeId}/{fileName}";
            
            var uploadResponse = await _supabase.Storage
                .From(SUBMISSIONS_BUCKET)
                .Upload(fileBytes, uploadPath);

            if (string.IsNullOrEmpty(uploadResponse))
            {
                return BadRequest(new { error = "Nie udało się przesłać pliku do Storage" });
            }

            // 8. POBIERZ PUBLICZNY URL DO PLIKU
            var fileUrl = _supabase.Storage
                .From(SUBMISSIONS_BUCKET)
                .GetPublicUrl(uploadPath);

            // 9. ZAPISZ INFORMACJE O ZGŁOSZENIU DO BAZY
            var submission = new Submission
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId ?? string.Empty,
                ChallengeId = challengeId,
                FileName = file.FileName,
                FileUrl = fileUrl,
                FileSizeMb = fileSizeMb,
                FileHash = fileHash,
                Status = "pending", // będzie oceniane później
                Score = null, // zostanie obliczone
                SubmittedAt = DateTime.UtcNow
            };

            var dbResponse = await _supabase
                .From<Submission>()
                .Insert(submission);

            var created = dbResponse.Models.FirstOrDefault();
            if (created == null)
            {
                // Jeśli zapis do bazy się nie powiódł, usuń plik ze Storage
                await _supabase.Storage
                    .From(SUBMISSIONS_BUCKET)
                    .Remove(uploadPath);
                
                return BadRequest(new { error = "Nie udało się zapisać zgłoszenia" });
            }

            _logger.LogInformation($"✅ Zgłoszenie zapisane: {created.Id}");

            // 10. ZWRÓĆ INFORMACJE O ZGŁOSZENIU
            return Ok(new 
            {
                message = "✅ Zgłoszenie zostało przyjęte!",
                submission = new SubmissionResponse(
                    created.Id,
                    created.UserId,
                    created.ChallengeId,
                    created.FileName,
                    created.Score,
                    created.Status,
                    created.SubmittedAt
                )
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd przesyłania zgłoszenia");
            return BadRequest(new { error = "Błąd przesyłania zgłoszenia", details = ex.Message });
        }
    }

    /// <summary>
    /// Pobiera historię zgłoszeń zalogowanego użytkownika
    /// </summary>
    [HttpGet("my")]
    public async Task<ActionResult<IEnumerable<SubmissionResponse>>> GetMySubmissions()
    {
        try
        {
            var userId = HttpContext.Items["UserId"]?.ToString();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "Musisz być zalogowany" });
            }

            var response = await _supabase
                .From<Submission>()
                .Where(s => s.UserId == userId)
                .Get();

            var submissions = response.Models.Select(s => new SubmissionResponse(
                s.Id,
                s.UserId,
                s.ChallengeId,
                s.FileName,
                s.Score,
                s.Status,
                s.SubmittedAt
            ));

            return Ok(submissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd pobierania zgłoszeń");
            return BadRequest(new { error = "Błąd pobierania zgłoszeń", details = ex.Message });
        }
    }

    /// <summary>
    /// Pobiera zgłoszenia dla konkretnego wyzwania (wszyscy użytkownicy)
    /// </summary>
    [HttpGet("challenge/{challengeId}")]
    public async Task<ActionResult<IEnumerable<SubmissionResponse>>> GetByChallengeId(string challengeId)
    {
        try
        {
            var response = await _supabase
                .From<Submission>()
                .Where(s => s.ChallengeId == challengeId)
                .Where(s => s.Status == "completed")
                .Get();

            var submissions = response.Models.Select(s => new SubmissionResponse(
                s.Id,
                s.UserId,
                s.ChallengeId,
                s.FileName,
                s.Score,
                s.Status,
                s.SubmittedAt
            ));

            return Ok(submissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd pobierania zgłoszeń");
            return BadRequest(new { error = "Błąd pobierania zgłoszeń", details = ex.Message });
        }
    }
}
