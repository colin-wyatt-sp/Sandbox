using ElasticSearchIndexer.Dto;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace ElasticSearchIndexer {

    public class ApodImageDownloader {

        public  void getImages(string apodJsonDocsPath, string apodImagesPath, int delayMillis=1300) {

            Console.WriteLine("Downloading NASA's Astronomy Picture of the Day's to: " + apodImagesPath);
            try {

                int i = 0;
                int numSkipped = 0;
                if (!Directory.Exists(apodImagesPath))
                    Directory.CreateDirectory(apodImagesPath);

                var proc = new Process();
                foreach (string docFilePath in Directory.GetFiles(apodJsonDocsPath, "*.json")) {
                    ApodDoc apodObj = ApodDoc.fromJsonFile(docFilePath);
                    if (string.IsNullOrWhiteSpace(apodObj.image_url)) {
                        Console.WriteLine("image_url is empty for: " + apodObj.source_url + " , skipping.");
                        continue;
                    }
                    var imgOutputPath = Path.Combine(apodImagesPath, Path.GetFileNameWithoutExtension(docFilePath) + "_" + apodObj.image_url.Substring(apodObj.image_url.LastIndexOf('/') + 1));
                    if (imgOutputPath.LastIndexOf(".") < 0) {
                        Console.WriteLine("ImgOutputPath: " + imgOutputPath + " , image_url: " + apodObj.image_url);
                        System.Diagnostics.Debugger.Break();
                    }
                    if (File.Exists(imgOutputPath)) {
                        if (numSkipped == 0) {
                            Console.WriteLine("Skipping already processed files.");
                        } else if (numSkipped % 50 == 0) {
                            Console.Write(".");
                        }
                        ++numSkipped;
                        continue;
                    }
                    if (numSkipped > 0) {
                        numSkipped = 0;
                        Console.WriteLine();
                        Console.WriteLine("Processing missing pages...");
                    }

                    var processStartInfo = new ProcessStartInfo(@"C:\Program Files\Git\mingw64\bin\wget.exe", apodObj.image_url + " -O " + imgOutputPath);
                    processStartInfo.CreateNoWindow = true;
                    processStartInfo.UseShellExecute = false;


                    proc.StartInfo = processStartInfo;

                    proc.Start();
                    proc.WaitForExit();

                    Thread.Sleep(delayMillis);

                    ++i;
                    // if (i == 5) return;
                }
            } catch (Exception e) {
                Console.WriteLine("Error getting images: " + e.Message);
                return;
            }

        }
  
    }
}
