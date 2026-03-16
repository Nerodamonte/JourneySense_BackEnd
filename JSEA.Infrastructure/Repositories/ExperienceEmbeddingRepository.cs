using JSEA_Application.Interfaces;
using JSEA_Application.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace JSEA_Infrastructure.Repositories
{
    public class ExperienceEmbeddingRepository : IExperienceEmbeddingRepository
    {
        private readonly AppDbContext _context;

        public ExperienceEmbeddingRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ExperienceEmbedding?> GetByExperienceIdAsync(Guid experienceId, CancellationToken cancellationToken = default)
        {
            return await _context.ExperienceEmbeddings
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.ExperienceId == experienceId, cancellationToken);
        }

        public async Task UpsertAsync(Guid experienceId, string metadataString, Vector embedding, CancellationToken cancellationToken = default)
        {
            var existing = await _context.ExperienceEmbeddings
                .FirstOrDefaultAsync(e => e.ExperienceId == experienceId, cancellationToken);

            if (existing == null)
            {
                _context.ExperienceEmbeddings.Add(new ExperienceEmbedding
                {
                    Id = Guid.NewGuid(),
                    ExperienceId = experienceId,
                    MetadataString = metadataString,
                    Embedding = embedding,
                    EmbeddedAt = DateTime.UtcNow
                });
            }
            else
            {
                existing.MetadataString = metadataString;
                existing.Embedding = embedding;
                existing.UpdatedAt = DateTime.UtcNow;
                _context.ExperienceEmbeddings.Update(existing);
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<List<(Guid ExperienceId, float CosineScore)>> SearchAsync(
            Vector userVector,
            IEnumerable<Guid> candidateIds,
            int topK,
            CancellationToken cancellationToken = default)
        {
            var candidateList = candidateIds.ToList();
            if (candidateList.Count == 0)
                return new List<(Guid, float)>();

            var results = await _context.ExperienceEmbeddings
                .AsNoTracking()
                .Where(e => candidateList.Contains(e.ExperienceId))
                .Select(e => new
                {
                    e.ExperienceId,
                    CosineScore = 1f - (float)e.Embedding.CosineDistance(userVector)
                })
                .OrderByDescending(e => e.CosineScore)
                .Take(topK)
                .ToListAsync(cancellationToken);

            return results
                .Select(e => (e.ExperienceId, e.CosineScore))
                .ToList();
        }

        public async Task<List<(Guid ExperienceId, float CosineScore)>> GetCosineScoresAsync(
            Vector userVector,
            IEnumerable<Guid> candidateIds,
            CancellationToken cancellationToken = default)
        {
            var candidateList = candidateIds.ToList();
            if (candidateList.Count == 0)
                return new List<(Guid, float)>();

            var results = await _context.ExperienceEmbeddings
                .AsNoTracking()
                .Where(e => candidateList.Contains(e.ExperienceId))
                .Select(e => new
                {
                    e.ExperienceId,
                    CosineScore = 1f - (float)e.Embedding.CosineDistance(userVector)
                })
                .ToListAsync(cancellationToken);

            return results
                .Select(e => (e.ExperienceId, e.CosineScore))
                .ToList();
        }

        public async Task<int> CountExistingAsync(IEnumerable<Guid> candidateIds, CancellationToken cancellationToken = default)
        {
            var candidateList = candidateIds.Distinct().ToList();
            if (candidateList.Count == 0)
                return 0;

            return await _context.ExperienceEmbeddings
                .AsNoTracking()
                .CountAsync(e => candidateList.Contains(e.ExperienceId), cancellationToken);
        }

        public async Task<bool> ExistsAsync(Guid experienceId, CancellationToken cancellationToken = default)
        {
            return await _context.ExperienceEmbeddings
                .AnyAsync(e => e.ExperienceId == experienceId, cancellationToken);
        }
    }
}
