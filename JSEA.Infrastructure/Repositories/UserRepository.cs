using JSEA_Application.Interfaces;
using JSEA_Application.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JSEA_Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users
                .FirstOrDefaultAsync(x => x.Email == email);
        }

        public async Task<User?> GetByIdAsync(Guid userId)
        {
            return await _context.Users
                .FirstOrDefaultAsync(x => x.Id == userId);
        }

        public async Task CreateAsync(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }

        public async Task<(List<User> Items, int TotalCount)> GetPagedAsync(
            string? role,
            string? status,
            string? search,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var q = _context.Users.AsNoTracking().Where(u => u.DeletedAt == null);

            if (!string.IsNullOrWhiteSpace(role))
                q = q.Where(u => u.Role == role.Trim().ToLowerInvariant());

            if (!string.IsNullOrWhiteSpace(status))
                q = q.Where(u => u.Status == status.Trim().ToLowerInvariant());

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                q = q.Where(u =>
                    u.Email.ToLower().Contains(s) ||
                    (u.Phone != null && u.Phone.ToLower().Contains(s)));
            }

            var total = await q.CountAsync(cancellationToken);
            var items = await q
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, total);
        }
    }
}
