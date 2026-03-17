// ================================================================
// File: JSEA.Presentation/Controllers/AdminController.cs
//
// Thêm vào Program.cs:
//   builder.Services.AddScoped<EmbeddingGeneratorService>();
// ================================================================

using JSEA_Application.Services.Journey;
using Microsoft.AspNetCore.Mvc;

namespace JSEA_Presentation.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly EmbeddingGeneratorService _embeddingGenerator;

    public AdminController(EmbeddingGeneratorService embeddingGenerator)
    {
        _embeddingGenerator = embeddingGenerator;
    }

    /// <summary>
    /// Generate embeddings cho tất cả experiences chưa có embedding.
    /// Gọi một lần sau khi seed data.
    /// </summary>
    [HttpPost("embeddings/generate")]
    public async Task<IActionResult> GenerateEmbeddings(CancellationToken cancellationToken)
    {
        var (success, failed, errors) = await _embeddingGenerator.GenerateForAllAsync(cancellationToken);

        return Ok(new
        {
            success,
            failed,
            errors
        });
    }
}