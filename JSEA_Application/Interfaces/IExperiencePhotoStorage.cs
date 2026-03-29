namespace JSEA_Application.Interfaces;

/// <summary>Lưu file ảnh experience lên disk và trả đường dẫn public (bắt đầu bằng /).</summary>
public interface IExperiencePhotoStorage
{
    /// <summary>Lưu stream → wwwroot/uploads/experiences/{experienceId}/... Trả URL tương đối (vd. /uploads/experiences/...).</summary>
    Task<string> SavePhotoAsync(
        Guid experienceId,
        Stream content,
        string contentType,
        string originalFileName,
        CancellationToken cancellationToken = default);

    /// <summary>Xóa file nếu URL trỏ tới thư mục upload nội bộ.</summary>
    Task TryDeleteStoredFileAsync(string photoUrl, CancellationToken cancellationToken = default);
}
