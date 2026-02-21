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
                // Check if core tables exist (including new architecture tables)
                try
                {
                    await _context.ExpenseTypes.AnyAsync();
                    await _context.Organizations.AnyAsync();
                    await _context.Vendors.AnyAsync();
                    await _context.Settlements.AnyAsync();
                    await _context.ProjectVendors.AnyAsync();
                    await _context.ProjectMembers.AnyAsync();
                    await _context.OrganizationMembers.AnyAsync();
                }
                catch (Microsoft.Data.Sqlite.SqliteException)
                {
                    // Tables don't exist, need to recreate database
                    System.Diagnostics.Debug.WriteLine("Required tables missing. Recreating database with new schema...");
                    await _context.Database.EnsureDeletedAsync();
                    databaseExists = false;
                }
                
                // Check if Subscriptions table has new schema columns
                if (databaseExists)
                {
                    try
                    {
                        // Try to query new schema columns
                        await _context.Subscriptions.Select(s => new { s.Reference, s.VendorId, s.BillingCycle }).FirstOrDefaultAsync();
                        await _context.Expenses.Select(e => new { e.VendorId, e.FundSource, e.ReimbursementStatus }).FirstOrDefaultAsync();
                        await _context.Contacts.Select(c => c.OrganizationId).FirstOrDefaultAsync();
                        await _context.Projects.Select(p => p.OrganizationId).FirstOrDefaultAsync();
                    }
                    catch (Microsoft.Data.Sqlite.SqliteException)
                    {
                        // Columns don't exist, need to recreate database with new schema
                        System.Diagnostics.Debug.WriteLine("Schema mismatch detected. Recreating database with updated schema...");
                        await _context.Database.EnsureDeletedAsync();
                        databaseExists = false;
                    }
                }

                // Check for new columns added incrementally (preserve existing data)
                if (databaseExists)
                {
                    try
                    {
                        await _context.PaymentModes.Select(pm => pm.RequiresSettlement).FirstOrDefaultAsync();
                    }
                    catch (Microsoft.Data.Sqlite.SqliteException)
                    {
                        // Add RequiresSettlement column via ALTER TABLE to preserve data
                        System.Diagnostics.Debug.WriteLine("Adding RequiresSettlement column to PaymentModes...");
                        await _context.Database.ExecuteSqlRawAsync(
                            "ALTER TABLE PaymentModes ADD COLUMN RequiresSettlement INTEGER NOT NULL DEFAULT 0");
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
