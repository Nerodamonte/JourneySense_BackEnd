using JSEA_Application.DTOs.Request.MicroExperience;
using JSEA_Application.DTOs.Respone.MicroExperience;

namespace JSEA_Application.Interfaces;

public interface IMicroExperienceService
{
    Task<List<MicroExperienceListItemResponse>> GetListAsync(MicroExperienceFilter filter, CancellationToken cancellationToken = default);
    Task<MicroExperienceDetailResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<MicroExperienceDetailResponse?> CreateAsync(CreateMicroExperienceRequest request, CancellationToken cancellationToken = default);
    Task<MicroExperienceDetailResponse?> UpdateAsync(Guid id, UpdateMicroExperienceRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
