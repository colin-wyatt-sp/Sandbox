using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Assembly = System.Reflection.Assembly;

namespace ProcessLogFiles
{
    class Program
    {
        static void Main(string[] args)
        {
            var assembly = Assembly.GetExecutingAssembly();

            int totalCount = 0;

            var dictionary = new Dictionary<string, string>();

            foreach (FileInfo fileInfo in new DirectoryInfo(new FileInfo(assembly.Location).DirectoryName).EnumerateFiles("*.log*").OrderBy(x => x.LastWriteTimeUtc)) {
                Console.WriteLine("File: " + fileInfo.Name);
                using(StreamReader stream = fileInfo.OpenText()) {
                    while (!stream.EndOfStream) {
                        var line = stream.ReadLine();
                        //Console.WriteLine("line: " + line);
                        //const string search = @"Begin Parallel processing BRs count: (\d+)";
                        //const string search = "Requesting BR processing for chunk count: ";
                        //const string search = @"Adding (\d+) messages to collector ActionBlock processing";
                        //const string search = @"BEGIN Try getting permissions of BR";
                        //const string search = @"BRs waiting in line: (\d+), vanilla ACL queue:";
                        const string beginSearch = @"BEGIN Try getting permissions of BR .*, \(ID: (\d+)\)";
                        const string endSearch = @"END Try getting permissions of BR .*, \(ID: (\d+)\)";

                        var match = Regex.Match(line, beginSearch);
                        if (match.Success) {
                            var resourceId = match.Groups[1].Value;
                            dictionary[resourceId] = match.Value;
                        }
                        match = Regex.Match(line, endSearch);
                        if (match.Success) {
                            var resourceId = match.Groups[1].Value;
                            if (dictionary.ContainsKey(resourceId))
                                dictionary.Remove(resourceId);
                            else {
                                Console.WriteLine("ERROR: found end without a previous begin for: " + resourceId +
                                                  ", " + match.Value);
                            }
                        }


                        //var match = Regex.Match(line, beginSearch);
                        //if (match.Success) {
                        //    if (match.Groups.Count > 1) {
                        //        string resultString = match.Groups[1].Value;
                        //        Console.WriteLine("resultString: " + match.Groups[1].Value);
                        //        var count = Int32.Parse(resultString);
                        //        Console.WriteLine("count: " + count);
                        //        totalCount += count;
                        //    }
                        //    else {
                        //        ++totalCount;
                        //    }
                        //}

                    }
                }
            }

            foreach (string key in dictionary.Keys) {
                Console.WriteLine("Unmatched: " + dictionary[key]);
            }

            Console.WriteLine("TOTAL: " + totalCount);
        }
    }
}
