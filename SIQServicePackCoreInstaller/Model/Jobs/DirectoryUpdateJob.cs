using System;
using System.IO;
using SIQServicePackCoreInstaller.Interfaces;
using SIQServicePackCoreInstaller.Model.DataTypes;
using SIQServicePackCoreInstaller.Model.Utility;

namespace SIQServicePackCoreInstaller.Model.Jobs
{
    public class DirectoryUpdateJob : IUpdateJob
    {
        private readonly DirectoryUpdateJobInfo _info;

        public DirectoryUpdateJob(DirectoryUpdateJobInfo info) {
            _info = info;
        }

        public void performUpdate() {
            try {
                updateDirectories();
            }
            catch (Exception e) {
                Logger.log($"ERROR: while updating files from {_info.DirectoryWithFileUpdates} to {_info.LocationToUpdate}. Msg: {e.Message}");
            }
        }

        private void updateDirectories() {
            Logger.log($"Getting {_info.Name} path...");
            var destinationDirectoryPath = _info.LocationToUpdate;
            var destinationBackupPath = destinationDirectoryPath + "_BAK-" + Logger.Timestamp;
            var sourceDirectoryPath = _info.DirectoryWithFileUpdates;

            Logger.log("Stop any processes still running...");
            ProcessUtility.tryKillRogueProcesses(new DirectoryInfo(destinationDirectoryPath));

            Logger.log(
                $"Backing up {_info.Name} folder \"{destinationDirectoryPath}\" to \"{new DirectoryInfo(destinationBackupPath).Name}\"");

            var backupDirectoryInfo = Directory.CreateDirectory(destinationBackupPath);
            FileUtility.copy(destinationDirectoryPath, backupDirectoryInfo.FullName, null, overwrite: false);

            Logger.log($"Copying service pack files from \"{sourceDirectoryPath}\" to \"{destinationDirectoryPath}\"");
            FileUtility.copy(sourceDirectoryPath, destinationDirectoryPath, _info.FileExcludeList, overwrite: true);
        }
    }
}
