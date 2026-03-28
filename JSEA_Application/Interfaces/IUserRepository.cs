using JSEA_Application.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JSEA_Application.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(Guid userId);
        Task<User?> GetByEmailAsync(string email);
        Task CreateAsync(User user);
        Task UpdateAsync(User user);

        /// <summary>Danh sách user cho admin portal (đã loại soft-delete).</summary>
        Task<(List<User> Items, int TotalCount)> GetPagedAsync(
            string? role,
            string? status,
            string? search,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);
    }
}
