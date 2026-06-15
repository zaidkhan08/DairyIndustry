
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace DairyIndustry.Services
{
    public class FileUploadService
    {
        private readonly IWebHostEnvironment _env;
        public string GetWebRootPath() => _env.WebRootPath;
        public FileUploadService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public string SaveFile(IFormFile file, string folderName)
        {
            if (file == null || file.Length == 0)
                return null;

            HashSet<string> allowedExtensions =
                new(StringComparer.OrdinalIgnoreCase)
                {
                    ".jpg",
                    ".jpeg",
                    ".png",
                    ".pdf"
                };

            string extension =
                Path.GetExtension(file.FileName).ToLower();

            if (!allowedExtensions.Contains(extension))
                throw new Exception("Invalid file type");

            if (file.Length > 5 * 1024 * 1024)
                throw new Exception("File size cannot exceed 5 MB");

            string folder = Path.Combine(
                _env.WebRootPath,
                "uploads","documents",
                folderName);

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            string fileName =
                Guid.NewGuid() + extension;

            string fullPath =
                Path.Combine(folder, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                file.CopyTo(stream);
            }

            return "/uploads/documents/" + folderName + "/" + fileName;
        }
    }
}