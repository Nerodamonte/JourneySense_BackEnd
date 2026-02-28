using JSEA_Application.DTOs.Request.MicroExperience;
using JSEA_Application.Models;

namespace JSEA_Application.Interfaces;

public interface IMicroExperienceRepository
{
    Task<List<MicroExperience>> FindAllAsync(MicroExperienceFilter filter, CancellationToken cancellationToken = default);
    Task<MicroExperience?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<MicroExperience> SaveAsync(MicroExperience entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
