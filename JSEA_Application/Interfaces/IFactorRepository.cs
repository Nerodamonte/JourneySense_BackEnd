using JSEA_Application.Models;

namespace JSEA_Application.Interfaces;

public interface IFactorRepository
{
    /// <summary>
    /// Lấy factor type = 'mood' theo tên (case-insensitive). Trả về null nếu không tìm thấy.
    /// </summary>
    Task<Factor?> GetMoodFactorByNameAsync(string name, CancellationToken cancellationToken = default);
}

