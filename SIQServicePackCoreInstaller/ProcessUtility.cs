using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIQServicePackCoreInstaller
{
    public class ProcessUtility
    {
        public void TryKillRogueProcesses(DirectoryInfo serviceDirectory)
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
                        Logger.Log("Killing " + processFileName);
                        runningProcess.Kill();
                    }
                    catch (Exception e)
                    {
                        Logger.Log("ERROR killing service process: " + processFileName + " : " + e.Message);
                    }
                }
            }
        }
    }
}
