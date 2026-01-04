using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Expense_Flow.Services;

public class FileStorageService : IFileStorageService
{
    private readonly string _invoicesDirectory;
    private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".pdf" };

    public FileStorageService()
    {
        // Store invoices in LocalApplicationData\ExpenseFlow\Invoices
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _invoicesDirectory = Path.Combine(localAppData, "ExpenseFlow", "Invoices");

        // Create directory if it doesn't exist
        if (!Directory.Exists(_invoicesDirectory))
        {
            Directory.CreateDirectory(_invoicesDirectory);
        }
    }

    public async Task<(bool Success, Guid? FileGuid, string? ErrorMessage)> SaveInvoiceFileAsync(
        Stream fileStream, 
        string fileName, 
        int maxSizeInMB = 10)
    {
        try
        {
            // Validate file type
            if (!IsFileTypeAllowed(fileName))
            {
                return (false, null, $"File type not allowed. Allowed types: {string.Join(", ", _allowedExtensions)}");
            }

            // Validate file size
            var maxSizeInBytes = maxSizeInMB * 1024 * 1024;
            if (fileStream.Length > maxSizeInBytes)
            {
                return (false, null, $"File size exceeds {maxSizeInMB}MB limit. Current size: {fileStream.Length / 1024 / 1024:F2}MB");
            }

            // Generate GUID and get extension
            var fileGuid = Guid.NewGuid();
            var extension = Path.GetExtension(fileName);
            var newFileName = $"{fileGuid}{extension}";
            var filePath = Path.Combine(_invoicesDirectory, newFileName);

            // Save file
            using (var fileOutputStream = File.Create(filePath))
            {
                fileStream.Position = 0; // Reset stream position
                await fileStream.CopyToAsync(fileOutputStream);
            }

            return (true, fileGuid, null);
        }
        catch (Exception ex)
        {
            return (false, null, $"Error saving file: {ex.Message}");
        }
    }

    public async Task<(bool Success, Stream? FileStream, string? FileName, string? ErrorMessage)> GetInvoiceFileAsync(Guid fileGuid)
    {
        try
        {
            var filePath = GetInvoiceFilePath(fileGuid);
            
            if (filePath == null || !File.Exists(filePath))
            {
                return (false, null, null, "File not found");
            }

            var fileStream = new MemoryStream();
            using (var fileInputStream = File.OpenRead(filePath))
            {
                await fileInputStream.CopyToAsync(fileStream);
            }
            fileStream.Position = 0;

            var fileName = Path.GetFileName(filePath);
            return (true, fileStream, fileName, null);
        }
        catch (Exception ex)
        {
            return (false, null, null, $"Error reading file: {ex.Message}");
        }
    }

    public async Task<bool> DeleteInvoiceFileAsync(Guid fileGuid)
    {
        try
        {
            var filePath = GetInvoiceFilePath(fileGuid);
            
            if (filePath != null && File.Exists(filePath))
            {
                await Task.Run(() => File.Delete(filePath));
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    public string? GetInvoiceFilePath(Guid fileGuid)
    {
        try
        {
            // Search for file with any allowed extension
            foreach (var ext in _allowedExtensions)
            {
                var filePath = Path.Combine(_invoicesDirectory, $"{fileGuid}{ext}");
                if (File.Exists(filePath))
                {
                    return filePath;
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    public bool IsFileTypeAllowed(string fileName)
    {
        var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
        return !string.IsNullOrEmpty(extension) && _allowedExtensions.Contains(extension);
    }
}
