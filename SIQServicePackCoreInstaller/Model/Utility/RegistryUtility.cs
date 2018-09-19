using System;
using Microsoft.Win32;

namespace SIQServicePackCoreInstaller.Model.Utility
{
    public static class RegistryUtility {

        public static string getServicePath(string serviceName) {

            string registryPath = @"SYSTEM\CurrentControlSet\Services\" + serviceName;
            RegistryKey keyHKLM = Registry.LocalMachine;

            RegistryKey key;
            key = keyHKLM.OpenSubKey(registryPath);

            string value = key.GetValue("ImagePath").ToString();
            key.Close();

            return Environment.ExpandEnvironmentVariables(value).Replace("\\\"", "").Replace("\"", "");
        }

    }
}
