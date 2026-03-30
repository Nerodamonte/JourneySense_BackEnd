using JSEA_Application.Enums;

namespace JSEA_Application.Interfaces;

public interface ITravelStyleTextGenerator
{
    Task<string?> GenerateAsync(IReadOnlyList<VibeType> travelStyle, CancellationToken cancellationToken = default);
}
