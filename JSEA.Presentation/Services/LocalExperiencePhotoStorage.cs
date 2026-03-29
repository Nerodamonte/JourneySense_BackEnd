using JSEA_Application.Interfaces;
using Microsoft.AspNetCore.Hosting;

namespace JSEA_Presentation.Services;

public class LocalExperiencePhotoStorage : IExperiencePhotoStorage
{
    public const long MaxFileBytes = 10 * 1024 * 1024;

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp", "image/gif"
    };

    private readonly IWebHostEnvironment _env;

    public LocalExperiencePhotoStorage(IWebHostEnvironment env)
    {
        _env = env;
    }

    public async Task<string> SavePhotoAsync(
        Guid experienceId,
        Stream content,
        string contentType,
        string originalFileName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        var ct = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType.Trim();
        if (!AllowedContentTypes.Contains(ct))
            throw new InvalidOperationException("Chỉ chấp nhận ảnh JPEG, PNG, WebP hoặc GIF.");

        var ext = ResolveExtension(ct, originalFileName);
        if (ext == null)
            throw new InvalidOperationException("Không xác định được phần mở rộng file.");

        var webRoot = GetWebRootFullPath();
        var expDir = Path.Combine(webRoot, "uploads", "experiences", experienceId.ToString("N"));
        Directory.CreateDirectory(expDir);

        var fileName = $"{Guid.NewGuid():N}{ext}";
        var fullPath = Path.GetFullPath(Path.Combine(expDir, fileName));
        if (!fullPath.StartsWith(Path.GetFullPath(expDir), StringComparison.Ordinal))
            throw new InvalidOperationException("Đường dẫn file không hợp lệ.");

        await using (var fs = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 65536, useAsync: true))
        {
            long len = 0;
            var buffer = new byte[81920];
            int read;
            while ((read = await content.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
            {
                len += read;
                if (len > MaxFileBytes)
                    throw new InvalidOperationException($"Ảnh vượt quá {MaxFileBytes / 1024 / 1024}MB.");
                await fs.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            }
        }

        return $"/uploads/experiences/{experienceId:N}/{fileName}";
    }

    public Task TryDeleteStoredFileAsync(string photoUrl, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(photoUrl))
            return Task.CompletedTask;

        var url = photoUrl.Trim();
        var prefix = "/uploads/experiences/";
        if (!url.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return Task.CompletedTask;

        var relative = url[prefix.Length..].Replace('/', Path.DirectorySeparatorChar);
        if (relative.Contains("..", StringComparison.Ordinal))
            return Task.CompletedTask;

        var webRoot = GetWebRootFullPath();
        var full = Path.GetFullPath(Path.Combine(webRoot, "uploads", "experiences", relative));
        if (!full.StartsWith(Path.GetFullPath(Path.Combine(webRoot, "uploads", "experiences")), StringComparison.Ordinal))
            return Task.CompletedTask;

        if (File.Exists(full))
        {
            try
            {
                File.Delete(full);
            }
            catch
            {
                /* best effort */
            }
        }

        return Task.CompletedTask;
    }

    private string GetWebRootFullPath()
    {
        var root = _env.WebRootPath;
        if (string.IsNullOrEmpty(root))
            root = Path.Combine(_env.ContentRootPath, "wwwroot");
        Directory.CreateDirectory(root);
        return Path.GetFullPath(root);
    }

    private static string? ResolveExtension(string contentType, string originalFileName)
    {
        var extFromCt = contentType.ToLowerInvariant() switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            "image/gif" => ".gif",
            _ => null
        };
        if (extFromCt != null)
            return extFromCt;

        var fromName = Path.GetExtension(originalFileName);
        if (string.IsNullOrEmpty(fromName))
            return null;
        fromName = fromName.ToLowerInvariant();
        return fromName is ".jpg" or ".jpeg" or ".png" or ".webp" or ".gif" ? (fromName == ".jpeg" ? ".jpg" : fromName) : null;
    }
}
