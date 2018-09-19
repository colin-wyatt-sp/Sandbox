using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SIQServicePackCoreInstaller.Model.Utility
{
    public static class FileUtility {

        public static void copy(string sourceDir, string targetDir, IEnumerable<string> fileExcludeList = null, bool overwrite = false) {

            Directory.CreateDirectory(targetDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                if (fileExcludeList != null && fileExcludeList.Any(x => file.ToLowerInvariant().Contains(x)))
                    continue;

                File.Copy(file, Path.Combine(targetDir, Path.GetFileName(file)), overwrite);
            }

            foreach (var directory in Directory.GetDirectories(sourceDir))
                copy(directory, Path.Combine(targetDir, Path.GetFileName(directory)), fileExcludeList, overwrite);
        }

    }
}
