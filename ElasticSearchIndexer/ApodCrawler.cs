using System;
using System.Diagnostics;
using System.IO;

namespace ElasticSearchIndexer {

    public class ApodCrawler {

        public void crawl(string destinationDirectory) {

            Console.WriteLine("Crawling NASA Astronomy Picture Of the Day archives for HTML docs...");
            string strCmdText;
            strCmdText = Path.Combine(Environment.CurrentDirectory, @"GetApod.ps1");// -destination " + "\"" + destinationDirectory + "\"" ;
            var processStartInfo = new ProcessStartInfo();
            processStartInfo.FileName = "powershell.exe ";
            processStartInfo.Arguments = "-ExecutionPolicy Bypass -File \"" + strCmdText + "\"";
            processStartInfo.UseShellExecute = false;
            processStartInfo.LoadUserProfile = true;
            var process = Process.Start(processStartInfo);

            process.WaitForExit();

        }

    }
}
