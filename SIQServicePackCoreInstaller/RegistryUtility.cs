using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace SIQServicePackCoreInstaller
{
    public static class RegistryUtility
    {
        public static string GetServicePath(string serviceName)
        {

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
