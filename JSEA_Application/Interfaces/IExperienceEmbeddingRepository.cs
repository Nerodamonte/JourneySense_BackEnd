using JSEA_Application.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pgvector;

namespace JSEA_Application.Interfaces
{
    public interface IExperienceEmbeddingRepository
    {
        /// <summary>
        /// Lấy embedding theo experience_id. Trả về null nếu chưa có.
        /// </summary>
        Task<ExperienceEmbedding?> GetByExperienceIdAsync(Guid experienceId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Upsert embedding: insert nếu chưa có, update nếu đã có.
        /// Gọi sau khi Gemini embed xong metadata string của experience.
        /// </summary>
        Task UpsertAsync(Guid experienceId, string metadataString, Vector embedding, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cosine search: tìm top-k experiences gần nhất với user vector,
        /// chỉ trong tập candidateIds (đã qua hard filter).
        /// Trả về list (experienceId, cosineScore) sắp xếp theo score giảm dần.
        /// </summary>
        Task<List<(Guid ExperienceId, float CosineScore)>> SearchAsync(Vector userVector, IEnumerable<Guid> candidateIds, int topK, CancellationToken cancellationToken = default);

        /// <summary>
        /// Kiểm tra experience đã có embedding chưa.
        /// </summary>
        Task<bool> ExistsAsync(Guid experienceId, CancellationToken cancellationToken = default);
    }
}
