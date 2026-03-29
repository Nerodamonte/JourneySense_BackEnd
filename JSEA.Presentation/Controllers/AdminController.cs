using System.Security.Claims;
using JSEA_Application.Constants;
using JSEA_Application.Enums;
using JSEA_Application.Interfaces;
using JSEA_Application.Services.Journey;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JSEA_Presentation.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly EmbeddingGeneratorService _embeddingGenerator;
    private readonly IPortalAuditLogger _audit;

    public AdminController(EmbeddingGeneratorService embeddingGenerator, IPortalAuditLogger audit)
    {
        _embeddingGenerator = embeddingGenerator;
        _audit = audit;
    }

    /// <summary>
    /// 11) Generate embeddings cho tất cả experiences chưa có embedding. Chỉ admin.
    /// </summary>
    [HttpPost("embeddings/generate")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> GenerateEmbeddings(CancellationToken cancellationToken)
    {
        var (success, failed, errors) = await _embeddingGenerator.GenerateForAllAsync(cancellationToken);

        var actor = User.FindFirstValue(ClaimTypes.NameIdentifier);
        _ = Guid.TryParse(actor, out var actorId);
        await _audit.LogAsync(
            actorId == Guid.Empty ? null : actorId,
            ActionType.AdminEmbeddingBatchRun,
            "ExperienceEmbedding",
            null,
            null,
            new { success, failed, errorCount = errors.Count },
            HttpContext.Connection.RemoteIpAddress,
            Request.Headers.UserAgent.ToString(),
            cancellationToken);

        return Ok(new
        {
            success,
            failed,
            errors
        });
    }
}