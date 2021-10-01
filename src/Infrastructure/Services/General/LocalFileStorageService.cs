using DN.WebApi.Application.Abstractions.Services.General;
using DN.WebApi.Domain.Enums;
using DN.WebApi.Infrastructure.Extensions;
using DN.WebApi.Shared.DTOs.General.Requests;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DN.WebApi.Infrastructure.Services.General
{
    public class LocalFileStorageService : IFileStorageService
    {
        public Task<string> UploadAsync<T>(FileUploadRequest request, FileType supportedFileType)
        where T : class
        {
            if (request == null || request.Data == null)
            {
                return Task.FromResult(string.Empty);
            }

            if (!supportedFileType.GetDescriptionList().Contains(request.Extension))
                throw new Exception("File Format Not Supported.");

            string base64Data = Regex.Match(request.Data, "data:image/(?<type>.+?),(?<data>.+)").Groups["data"].Value;

            var streamData = new MemoryStream(Convert.FromBase64String(base64Data));
            if (streamData.Length > 0)
            {
                string folder = typeof(T).Name;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    folder = folder.Replace(@"\", "/");
                }

                string folderName = string.Empty;
                switch (supportedFileType)
                {
                    case FileType.Image:
                        folderName = Path.Combine("Files", "Images", folder);
                        break;
                    default:
                        folderName = Path.Combine("Files", "Others", folder);
                        break;
                }

                string pathToSave = Path.Combine(Directory.GetCurrentDirectory(), folderName);
                bool exists = Directory.Exists(pathToSave);
                if (!exists)
                {
                    Directory.CreateDirectory(pathToSave);
                }

                string fileName = request.Name.Trim('"');
                fileName = RemoveSpecialCharacters(fileName);
                fileName = RegexExtensions.ReplaceWhitespace(fileName, "-");
                fileName = fileName + request.Extension.Trim();
                string fullPath = Path.Combine(pathToSave, fileName);
                string dbPath = Path.Combine(folderName, fileName);
                if (File.Exists(dbPath))
                {
                    dbPath = NextAvailableFilename(dbPath);
                    fullPath = NextAvailableFilename(fullPath);
                }

                using var stream = new FileStream(fullPath, FileMode.Create);
                streamData.CopyTo(stream);
                dbPath = dbPath.Replace("\\", "/");
                return Task.FromResult("{server_url}/" + dbPath);
            }
            else
            {
                return Task.FromResult(string.Empty);
            }
        }

        public static string RemoveSpecialCharacters(string str)
        {
            return Regex.Replace(str, "[^a-zA-Z0-9_.]+", string.Empty, RegexOptions.Compiled);
        }

        private static string numberPattern = "-{0}";

        private static string NextAvailableFilename(string path)
        {
            if (!File.Exists(path))
            {
                return path;
            }

            if (Path.HasExtension(path))
            {
                return GetNextFilename(path.Insert(path.LastIndexOf(Path.GetExtension(path), StringComparison.Ordinal), numberPattern));
            }

            return GetNextFilename(path + numberPattern);
        }

        private static string GetNextFilename(string pattern)
        {
            string tmp = string.Format(pattern, 1);

            if (!File.Exists(tmp))
            {
                return tmp;
            }

            int min = 1, max = 2;

            while (File.Exists(string.Format(pattern, max)))
            {
                min = max;
                max *= 2;
            }

            while (max != min + 1)
            {
                int pivot = (max + min) / 2;
                if (File.Exists(string.Format(pattern, pivot)))
                {
                    min = pivot;
                }
                else
                {
                    max = pivot;
                }
            }

            return string.Format(pattern, max);
        }
    }
}