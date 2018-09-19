using System;
using System.Diagnostics;
using System.IO;

namespace SIQServicePackCoreInstaller.Model.Utility {

    public static class ProcessUtility {

        public static void tryKillRogueProcesses(DirectoryInfo serviceDirectory)
        {
            //Log("calling GetProcesses...");
            Process[] runningProcesses = Process.GetProcesses();
            //Log("iterating over running processes...");
            foreach (var runningProcess in runningProcesses)
            {

                string processFileName;

                try
                {
                    //Log("DEBUG: process : " + runningProcess.ProcessName);
                    processFileName = runningProcess.MainModule.FileName;
                }
                catch (Exception)
                {
                    // some processes don't like you looking at them.
                    //Log("ERROR: access denied. " + runningProcess.ProcessName);
                    //Log("WARN: test");
                    continue;
                }

                //Log("DEBUG: process: " + processFileName);
                var directory = new FileInfo(processFileName).DirectoryName;
                //Log("DEBUG: " + directory);
                if (string.Compare(serviceDirectory.FullName, directory,
                        StringComparison.InvariantCultureIgnoreCase) == 0)
                {

                    try
                    {
                        Logger.logToFile("Killing " + processFileName);
                        runningProcess.Kill();
                    }
                    catch (Exception e)
                    {
                        Logger.log("ERROR killing service process: " + processFileName + " : " + e.Message);
                    }
                }
            }
        }

    }
}
