using System;
using System.IO;
using System.Threading.Tasks;

namespace Expense_Flow.Services;

public interface IFileStorageService
{
    /// <summary>
    /// Saves an invoice file to local storage
    /// </summary>
    /// <param name="fileStream">The file stream</param>
    /// <param name="fileName">Original filename</param>
    /// <param name="maxSizeInMB">Maximum file size in MB (default 10)</param>
    /// <returns>Success status, File GUID, and error message if any</returns>
    Task<(bool Success, Guid? FileGuid, string? ErrorMessage)> SaveInvoiceFileAsync(
        Stream fileStream, 
        string fileName, 
        int maxSizeInMB = 10);

    /// <summary>
    /// Retrieves an invoice file from local storage
    /// </summary>
    /// <param name="fileGuid">The file GUID</param>
    /// <returns>Success status, File stream, filename, and error message if any</returns>
    Task<(bool Success, Stream? FileStream, string? FileName, string? ErrorMessage)> GetInvoiceFileAsync(Guid fileGuid);

    /// <summary>
    /// Deletes an invoice file from local storage
    /// </summary>
    /// <param name="fileGuid">The file GUID</param>
    /// <returns>Success status</returns>
    Task<bool> DeleteInvoiceFileAsync(Guid fileGuid);

    /// <summary>
    /// Gets the full path to an invoice file
    /// </summary>
    /// <param name="fileGuid">The file GUID</param>
    /// <returns>Full file path or null if not found</returns>
    string? GetInvoiceFilePath(Guid fileGuid);

    /// <summary>
    /// Validates if a file extension is allowed
    /// </summary>
    /// <param name="fileName">The filename</param>
    /// <returns>True if allowed</returns>
    bool IsFileTypeAllowed(string fileName);
}
