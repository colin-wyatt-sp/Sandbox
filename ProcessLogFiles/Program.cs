using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
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
            
            foreach (FileInfo fileInfo in new DirectoryInfo(new FileInfo(assembly.Location).DirectoryName).EnumerateFiles("*.log*")) {
                Console.WriteLine("File: " + fileInfo.Name);
                using(StreamReader stream = fileInfo.OpenText()) {
                    while (!stream.EndOfStream) {
                        var line = stream.ReadLine();
                        //Console.WriteLine("line: " + line);
                        const string search = "Begin Parallel processing BRs count: ";
                        int length = search.Length;
                        if (line.Contains(search)) {

                            var index = line.IndexOf(search);

                            var commaIndex = line.IndexOf(',', index + length);
                            var countStr = line.Substring(index + length, commaIndex - (index + length));
                            //Console.WriteLine("countStr: \"" + countStr + "\"");
                            var count = Int32.Parse(countStr);
                            Console.WriteLine("count: " + count);
                            totalCount += count;
                        }
                    }
                }
            }

            Console.WriteLine("TOTAL: " + totalCount);
        }
    }
}
