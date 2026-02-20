using Microsoft.Win32;
using SmartAlloc.Data;
using System.IO;
using System.Windows;

namespace SmartAlloc.Services;

public class BackupService
{
    private readonly DatabaseContext _db;

    public BackupService(DatabaseContext db) => _db = db;

    public bool Backup()
    {
        var dlg = new SaveFileDialog
        {
            Title = "Export SmartAlloc Backup",
            Filter = "SmartAlloc Backup (*.smartbackup)|*.smartbackup",
            FileName = $"SmartAlloc_Backup_{DateTime.Now:yyyyMMdd_HHmm}.smartbackup"
        };

        if (dlg.ShowDialog() != true) return false;

        try
        {
            _db.CloseConnection();
            File.Copy(DatabaseContext.DbPath, dlg.FileName, overwrite: true);
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Backup failed:\n{ex.Message}", "Backup Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
        finally
        {
            _ = _db.GetConnection();
        }
    }

    public bool Restore()
    {
        var dlg = new OpenFileDialog
        {
            Title = "Import SmartAlloc Backup",
            Filter = "SmartAlloc Backup (*.smartbackup)|*.smartbackup"
        };

        if (dlg.ShowDialog() != true) return false;

        var confirm = MessageBox.Show(
            "This will replace ALL current data with the backup.\n\nAre you sure?",
            "Restore Backup",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes) return false;

        try
        {
            _db.CloseConnection();
            File.Copy(dlg.FileName, DatabaseContext.DbPath, overwrite: true);

            MessageBox.Show(
                "Backup restored successfully.\nThe application will now restart.",
                "Restore Complete",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            var exe = Environment.ProcessPath ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
            if (exe != null)
                System.Diagnostics.Process.Start(exe);

            Application.Current.Shutdown();
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Restore failed:\n{ex.Message}", "Restore Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            _ = _db.GetConnection(); // re-open even on failure
            return false;
        }
    }
}
