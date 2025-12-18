using Microsoft.AspNetCore.Hosting;

public interface IFileStorageService
{
    Task<string> SavePhotoAsync(IFormFile file, string subfolder);
    Task<(string storedName, string contentType, long size)> SaveDocumentAsync(IFormFile file, string subfolder);
    void DeleteIfExists(string relativeOrPhysicalPath, bool isRelativeWebPath = true);
}

public class FileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _env;

    public FileStorageService(IWebHostEnvironment env) => _env = env;

    public async Task<string> SavePhotoAsync(IFormFile file, string subfolder)
    {
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { ".jpg", ".jpeg", ".png", ".webp" };

        var ext = Path.GetExtension(file.FileName);
        if (!allowed.Contains(ext)) throw new InvalidOperationException("Invalid image type.");

        if (file.Length > 2 * 1024 * 1024) throw new InvalidOperationException("Image too large (max 2MB).");

        var uploadsRoot = Path.Combine(_env.WebRootPath, "uploads", subfolder);
        Directory.CreateDirectory(uploadsRoot);

        var storedName = $"{Guid.NewGuid():N}{ext}";
        var physicalPath = Path.Combine(uploadsRoot, storedName);

        using var stream = new FileStream(physicalPath, FileMode.Create);
        await file.CopyToAsync(stream);

        // web path for DB
        return $"/uploads/{subfolder}/{storedName}".Replace("\\", "/");
    }

    public async Task<(string storedName, string contentType, long size)> SaveDocumentAsync(IFormFile file, string subfolder)
    {
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { ".pdf", ".doc", ".docx" };

        var ext = Path.GetExtension(file.FileName);
        if (!allowed.Contains(ext)) throw new InvalidOperationException("Invalid document type.");

        if (file.Length > 15 * 1024 * 1024) throw new InvalidOperationException("File too large (max 15MB).");

        // пример: OUTSIDE wwwroot (посигурно)
        var root = Path.Combine(_env.ContentRootPath, "App_Data", "uploads", subfolder);
        Directory.CreateDirectory(root);

        var storedName = $"{Guid.NewGuid():N}{ext}";
        var physicalPath = Path.Combine(root, storedName);

        using var stream = new FileStream(physicalPath, FileMode.Create);
        await file.CopyToAsync(stream);

        return (storedName, file.ContentType, file.Length);
    }

    public void DeleteIfExists(string path, bool isRelativeWebPath = true)
    {
        string physical = path;

        if (isRelativeWebPath)
        {
            // "/uploads/..." -> wwwroot/uploads/...
            physical = Path.Combine(_env.WebRootPath, path.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));
        }

        if (File.Exists(physical)) File.Delete(physical);
    }
}
