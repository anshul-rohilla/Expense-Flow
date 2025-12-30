using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Expense_Flow.Data;

namespace Expense_Flow.Services;

public class DatabaseService
{
    private readonly ExpenseFlowDbContext _context;

    public DatabaseService(ExpenseFlowDbContext context)
    {
        _context = context;
    }

    public async Task InitializeAsync()
    {
        try
        {
            // Ensure the database directory exists
            var dbPath = GetDatabasePath();
            if (!string.IsNullOrEmpty(dbPath))
            {
                var directory = Path.GetDirectoryName(dbPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
            }

            // Check if database exists and has all required tables
            bool databaseExists = await _context.Database.CanConnectAsync();
            
            if (databaseExists)
            {
                // Check if ExpenseTypes table exists
                try
                {
                    await _context.ExpenseTypes.AnyAsync();
                }
                catch (Microsoft.Data.Sqlite.SqliteException)
                {
                    // Table doesn't exist, need to recreate database
                    System.Diagnostics.Debug.WriteLine("ExpenseTypes table missing. Recreating database...");
                    await _context.Database.EnsureDeletedAsync();
                    databaseExists = false;
                }
                
                // Check if Subscriptions table has Reference column (renamed from Username)
                if (databaseExists)
                {
                    try
                    {
                        // Try to query the Reference column
                        await _context.Subscriptions.Select(s => s.Reference).FirstOrDefaultAsync();
                    }
                    catch (Microsoft.Data.Sqlite.SqliteException ex) when (ex.Message.Contains("no such column"))
                    {
                        // Column doesn't exist, need to recreate database
                        System.Diagnostics.Debug.WriteLine("Reference column missing in Subscriptions. Recreating database...");
                        await _context.Database.EnsureDeletedAsync();
                        databaseExists = false;
                    }
                }
            }

            // Create database if it doesn't exist
            if (!databaseExists)
            {
                await _context.Database.EnsureCreatedAsync();
                System.Diagnostics.Debug.WriteLine("Database created successfully with all tables.");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Database initialization error: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            return await _context.Database.CanConnectAsync();
        }
        catch
        {
            return false;
        }
    }

    public string GetDatabasePath()
    {
        var connection = _context.Database.GetConnectionString();
        if (connection != null && connection.Contains("Data Source="))
        {
            var startIndex = connection.IndexOf("Data Source=") + "Data Source=".Length;
            var path = connection[startIndex..];
            var endIndex = path.IndexOf(';');
            if (endIndex > 0)
            {
                path = path[..endIndex];
            }
            return path.Trim();
        }
        return string.Empty;
    }
}
