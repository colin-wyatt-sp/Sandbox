using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SIQServicePackCoreInstaller.Xml
{
    public class XmlUpdateJobFactory : IUpdateJobFactory {

        private string jsonFile;
        private string sourceDirPath;
        private string destinationDirectoryPath;

        public XmlUpdateJobFactory(string destinationDirectoryPath, string jsonFile) {
            this.destinationDirectoryPath = destinationDirectoryPath;
            this.sourceDirPath = new FileInfo(jsonFile).Directory.FullName;
            this.jsonFile = jsonFile;
        }

        public string Name => "XML";

        public IEnumerable<IUpdateJob> GetJobs() {
            try {
                return GetJobsFromJson();
            }
            catch  {
                return new List<IUpdateJob>();
            }
        }

        private IEnumerable<IUpdateJob> GetJobsFromJson() {

            var jobs = new List<IUpdateJob>();
            dynamic json = JsonConvert.DeserializeObject(File.ReadAllText(jsonFile));
            foreach (dynamic jsonVal in json.xmlMerge) {
                jobs.Add(new XmlUpdateJob(
                            Path.Combine(sourceDirPath, jsonVal.sourceName.ToString()), 
                            Path.Combine(destinationDirectoryPath, jsonVal.destinationName.ToString()),
                            jsonVal.textToReplace.ToString()
                                        )
                );
            }

            return jobs;
        }
    }
}
