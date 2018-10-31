﻿using System;
using System.IO;
using System.IO.Compression;
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

        public string Name => _info.Name;

        public void performUpdate() {

            Logger.logToFile($"Getting {_info.Name} path");
            var destinationDirectoryPath = _info.LocationToUpdate;
            var destinationBackupPath = destinationDirectoryPath + "_BAK-" + Logger.Timestamp;
            var sourceDirectoryPath = _info.DirectoryWithFileUpdates;

            Logger.log("Stop any processes still running");
            ProcessUtility.tryKillRogueProcesses(new DirectoryInfo(destinationDirectoryPath));

            Logger.log(
                $"Backing up \"{destinationDirectoryPath}\" to \"{new DirectoryInfo(destinationBackupPath).Name}\"");

            var backupDirectoryInfo = Directory.CreateDirectory(destinationBackupPath);
            FileUtility.copy(destinationDirectoryPath, backupDirectoryInfo.FullName, fileExcludeList: null, overwrite: false);

            if (_info.CreateBackupAsZip) {
                try {
                    var backupZip = backupDirectoryInfo.FullName + ".zip";
                    ZipFile.CreateFromDirectory(backupDirectoryInfo.FullName, backupZip);
                    if (File.Exists(backupZip)) {
                        Directory.Delete(backupDirectoryInfo.FullName, recursive: true);
                    }
                    else {
                        Logger.log(
                            "WARN: Unable to create zip file from backup directory: " + backupDirectoryInfo.FullName +
                            ". You may need to create the zip of this folder manually, and remove the folder so it does not conflict with source.");
                    }
                }
                catch (Exception e) {
                    Logger.log(
                        "ERROR: Unable to create zip file from backup directory: " + backupDirectoryInfo.FullName +
                        ". You may need to create the zip of this folder manually, and remove the folder so it does not conflict with source.");
                }
            }

            Logger.log($"Copying service pack files from \"{sourceDirectoryPath}\" to \"{destinationDirectoryPath}\"");
            FileUtility.copy(sourceDirectoryPath, destinationDirectoryPath, _info.FileExcludeList, overwrite: true, unblockFiles: true);

        }

        public override string ToString() {
            return $"DirectoryUpdateJob {Name}, source:{_info.DirectoryWithFileUpdates}, dest:{_info.LocationToUpdate}";
        }

    }
}
