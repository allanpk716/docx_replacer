using System;
using System.Threading.Tasks;
using DocuFiller.Models.Update;

namespace DocuFiller.Services.Update
{
    public interface IUpdateService
    {
        Task<VersionInfo?> CheckForUpdateAsync(string currentVersion, string channel);
        Task<string> DownloadUpdateAsync(VersionInfo version, IProgress<DownloadProgress> progress);
        Task<bool> InstallUpdateAsync(string packagePath);
        event EventHandler<UpdateAvailableEventArgs>? UpdateAvailable;
    }

    public class UpdateAvailableEventArgs : EventArgs
    {
        public required VersionInfo Version { get; set; }
        public bool IsDownloaded { get; set; }
    }
}
