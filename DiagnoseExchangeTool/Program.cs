using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Runtime.InteropServices;
using System.Threading;

namespace DiagnoseExchangeTool
{
    class Program
    {

        private static string OutPath;

        static bool isSsl;
        static bool hideTypedPassword;
        static string netbios = string.Empty;
        static string serverName;
        static string userName;
        static string password;
        private static Assembly assembly;
        private static string outputText;
        static bool isExchangeOnline;

        static void Main(string[] args) {

            assembly = Assembly.GetExecutingAssembly();
            //foreach (var manifestResourceName in assembly.GetManifestResourceNames()) {
            //    Console.WriteLine("manifestResourceName: " + manifestResourceName);
            //}

            processIsExchangeOnlineParameter();

            if (isExchangeOnline) {
                
                processUserNameParameter();
                processPasswordParameter();
                serverName = "ps.outlook.com";
                isSsl = true;
            }
            else {

                processServerNameParameter();
                processNetbiosParameter();
                var port = processPortParameter();

                if (!string.IsNullOrWhiteSpace(port)) {
                    serverName = serverName + ":" + port;
                }

                processUserNameParameter();
                processPasswordParameter();

                if (!string.IsNullOrWhiteSpace(netbios) && !userName.Contains("\\"))
                {
                    userName = netbios + "\\" + userName;
                }

                processIsSslParameter();

            }

            int scriptNumber = processScriptNumber();

            var scriptName = RunExchangePowershell(isExchangeOnline, scriptNumber);

            if (ConfigurationManager.AppSettings["deleteScriptOnExit"] == "true") {
                File.Delete(".\\" + scriptName);
            }

            writeOutputAndCleanup();

            return;
        }

        private static int processScriptNumber() 
        {
            if (string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings["scriptNumber"]))
                return 1;

            string scriptNumberStr = ConfigurationManager.AppSettings["scriptNumber"];
            int scriptNum;
            return Int32.TryParse(scriptNumberStr, out scriptNum) ? scriptNum : 1;
        }

        private static void processIsExchangeOnlineParameter()
        {
            string isExchangeOnlineStr;
            if (string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings["isExchangeOnline"]))
            {
                Console.WriteLine("Is Exchange Online?");
                isExchangeOnlineStr = Console.ReadLine();
            }
            else
            {
                isExchangeOnlineStr = ConfigurationManager.AppSettings["isExchangeOnline"];
            }

            isExchangeOnline = getBoolFrom(isExchangeOnlineStr);
        }

        private static void processUserNameParameter()
        {
            if (string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings["userName"]))
            {
                Console.WriteLine(isExchangeOnline ? "Enter Monitoring User:" : "Enter User Name:");
                userName = Console.ReadLine();
            }
            else
            {
                userName = ConfigurationManager.AppSettings["userName"];
            }
        }

        private static void processPasswordParameter()
        {
            processShouldTypeObfuscatedPasswordParameter();

            if (string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings["password"]))
            {
                Console.WriteLine("Enter Password:");
                if (hideTypedPassword) {
                    password = null;
                    while (true)
                    {
                        var key = Console.ReadKey(true);
                        if (key.Key == ConsoleKey.Enter)
                            break;
                        password += key.KeyChar;
                    }
                    Console.WriteLine();
                }
                else {
                    password = Console.ReadLine();
                }
            }
            else
            {
                password = ConfigurationManager.AppSettings["password"];
            }
        }

        private static void processShouldTypeObfuscatedPasswordParameter()
        {
            string hideTypedPasswordStr;
            if (string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings["hideTypedPassword"]))
            {
                Console.WriteLine("Hide typed password?");
                hideTypedPasswordStr = Console.ReadLine();
            }
            else
            {
                hideTypedPasswordStr = ConfigurationManager.AppSettings["hideTypedPassword"];
            }

            hideTypedPassword = getBoolFrom(hideTypedPasswordStr);
        }

        private static void processServerNameParameter()
        {
            if (string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings["serverName"]))
            {
                Console.WriteLine("Enter Exchange Server Name:");
                serverName = Console.ReadLine();
            }
            else
            {
                serverName = ConfigurationManager.AppSettings["serverName"];
            }
        }


        private static void processNetbiosParameter() {
            if (string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings["netbios"])) {
                Console.WriteLine("Enter NETBIOS:");
                netbios = Console.ReadLine();
            }
            else {
                netbios = ConfigurationManager.AppSettings["netbios"];
            }
        }

        private static string processPortParameter()
        {
            string port;
            if (string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings["port"]))
            {
                Console.WriteLine("Enter Port (optional):");
                port = Console.ReadLine();
            }
            else
            {
                port = ConfigurationManager.AppSettings["port"];
                int i;
                if (!int.TryParse(port, out i))
                    port = string.Empty;
            }

            return port;
        }

        private static void processIsSslParameter()
        {
            string isSslStr;
            if (string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings["isSsl"]))
            {
                Console.WriteLine("Is SSL?");
                isSslStr = Console.ReadLine();
            }
            else
            {
                isSslStr = ConfigurationManager.AppSettings["isSsl"];
            }

            isSsl = getBoolFrom(isSslStr);
        }

        

        private static bool getBoolFrom(string boolStr) {
            return boolStr.Trim().ToLowerInvariant().StartsWith("y")
                   || boolStr.Trim().ToLowerInvariant().StartsWith("t");
        }

        private static string RunExchangePowershell(bool isExchangeOnline, int scriptNumber) {

            var currentDateTime = DateTime.Now;
            var timeStamp = currentDateTime.ToString("yyyyMMddHHmmss");

            var scriptName = $"DiagnoseExchangeTool.iterateMailboxes{scriptNumber}.ps1";
            string scriptContents = string.Empty;

            using (Stream stream = assembly.GetManifestResourceStream(scriptName))
            using (StreamReader reader = new StreamReader(stream)) {
                scriptContents = reader.ReadToEnd();
            }

            File.WriteAllText(".\\" + scriptName, scriptContents);
            Console.WriteLine("Checking mailboxes. Please wait...");
            string strCmdText;
            strCmdText =
                Path.Combine(Environment.CurrentDirectory, scriptName); // -destination " + "\"" + destinationDirectory + "\"" ;

            var processStartInfo = new ProcessStartInfo();
            processStartInfo.FileName = "powershell.exe ";
            processStartInfo.Arguments = string.Format(
                "-ExecutionPolicy Bypass -File \"" + strCmdText + "\" -serverName {0} -userName {1} -password {2} {3} {4}",
                serverName, userName, password, isSsl ? "-isSsl" : "", isExchangeOnline ? "-isOnline" : "");
            processStartInfo.UseShellExecute = false;
            processStartInfo.LoadUserProfile = true;
            processStartInfo.RedirectStandardOutput = true;
            var process = Process.Start(processStartInfo);

            OutPath = Path.Combine(Environment.CurrentDirectory, "mailboxes-output-" + timeStamp + ".txt");

            outputText = process.StandardOutput.ReadToEnd();

            process.WaitForExit();
            return scriptName;
        }

        private static void writeOutputAndCleanup() {

            Console.WriteLine("Writing output file \"" + OutPath + "\"...");
            File.WriteAllText(OutPath, outputText);

            Console.WriteLine("Diagnostic complete.");
            Console.WriteLine("Output file at: " + OutPath);
            //System.Diagnostics.Process.Start(OutPath);
            //Console.WriteLine("Press any key to exit");
            //Console.ReadKey();
            if (File.Exists(OutPath)) {
                string argument = "/select, \"" + OutPath + "\"";
                System.Diagnostics.Process.Start("explorer.exe", argument);
            }
        }

    }
}
