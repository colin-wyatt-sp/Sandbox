using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using SIQServicePackCoreInstaller.Interfaces;
using SIQServicePackCoreInstaller.Model.Jobs;

namespace SIQServicePackCoreInstaller.Model.Factories  {
    public class XmlUpdateJobFactory : IUpdateJobFactory {

        private readonly string _jsonFile;
        private readonly string _sourceDirPath;
        private readonly string _destinationDirectoryPath;

        public XmlUpdateJobFactory(string destinationDirectoryPath, string jsonFile) {
            this._destinationDirectoryPath = destinationDirectoryPath;
            this._sourceDirPath = new FileInfo(jsonFile).Directory.FullName;
            this._jsonFile = jsonFile;
        }

        public string Name => "XML";

        public IEnumerable<IUpdateJob> getJobs() {
            try {
                return getJobsFromJson();
            }
            catch  {
                return new List<IUpdateJob>();
            }
        }

        private IEnumerable<IUpdateJob> getJobsFromJson() {

            var jobs = new List<IUpdateJob>();
            dynamic json = JsonConvert.DeserializeObject(File.ReadAllText(_jsonFile));
            foreach (dynamic jsonVal in json.xmlMerge) {
                jobs.Add(new XmlUpdateJob(
                            Path.Combine(_sourceDirPath, jsonVal.sourceName.ToString()), 
                            Path.Combine(_destinationDirectoryPath, jsonVal.destinationName.ToString()),
                            jsonVal.textToReplace.ToString()
                                        )
                );
            }

            return jobs;
        }
    }
}
