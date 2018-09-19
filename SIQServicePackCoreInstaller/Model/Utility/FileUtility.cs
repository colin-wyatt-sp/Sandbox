using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using SIQServicePackCoreInstaller.Model.Jobs;

namespace SIQServicePackCoreInstaller.Model.Utility
{
    public static class FileUtility {

        public static void copy(string sourceDir, string targetDir, IEnumerable<string> fileExcludeList = null, bool overwrite = false, bool unblockFiles=false) {

            Directory.CreateDirectory(targetDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                if (fileExcludeList != null && fileExcludeList.Any(x => file.ToLowerInvariant().Contains(x)))
                    continue;

                File.Copy(file, Path.Combine(targetDir, Path.GetFileName(file)), overwrite);
            }

            if (unblockFiles) {
                Logger.logToFile($"Unblocking files at {targetDir}");
                unblock(targetDir);
            }

            foreach (var directory in Directory.GetDirectories(sourceDir))
                copy(directory, Path.Combine(targetDir, Path.GetFileName(directory)), fileExcludeList, overwrite, unblockFiles);
        }

        public static void unblock(string targetDir) {

            var processStartInfo = new ProcessStartInfo();
            processStartInfo.WorkingDirectory = targetDir;
            processStartInfo.FileName = "powershell.exe";
            processStartInfo.Arguments = "-ExecutionPolicy Bypass powershell -Command 'dir .\\* | Unblock-File'";
            processStartInfo.UseShellExecute = false;
            processStartInfo.LoadUserProfile = true;
            processStartInfo.RedirectStandardError = true;
            var process = new Process { StartInfo = processStartInfo };
            process.ErrorDataReceived += processErrorDataReceived;
            process.Start();
            process.BeginErrorReadLine();
            process.WaitForExit();
            process.ErrorDataReceived -= processErrorDataReceived;
        }

        private static void processErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(e.Data)) {
                Logger.log("WARN: PS: " + e.Data);
            }
        }

    }
}
