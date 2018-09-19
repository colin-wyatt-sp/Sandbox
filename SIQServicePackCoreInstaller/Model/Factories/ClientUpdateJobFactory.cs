using System.Collections.Generic;
using System.IO;
using SIQServicePackCoreInstaller.Interfaces;
using SIQServicePackCoreInstaller.Model.DataTypes;
using SIQServicePackCoreInstaller.Model.Jobs;

namespace SIQServicePackCoreInstaller.Model.Factories {
    public class ClientUpdateJobFactory : IUpdateJobFactory {

        private readonly string _servicePackLocation;

        private static string SecurityIqHomeEnvKey = "SECURITYIQ_HOME";
        private static string ClientFolderName = "Client";
        private static string DefaultClientParentFolderPath = "C:\\Program Files\\SailPoint";


        public ClientUpdateJobFactory(string servicePackLocation) {
            _servicePackLocation = servicePackLocation;
        }

        public string Name => "Client";

        public IEnumerable<IUpdateJob> getJobs() {

            var servicePackClientLocation = Path.Combine(_servicePackLocation, ClientFolderName);
            if (!Directory.Exists(servicePackClientLocation)) {
                Logger.log("No client files found to update.");
                return new List<IUpdateJob>();
            }

            string securityIQHome = System.Environment.GetEnvironmentVariable(SecurityIqHomeEnvKey);
            if (string.IsNullOrWhiteSpace(securityIQHome)) {
                securityIQHome = DefaultClientParentFolderPath;
            }
            var candidateClientPath = Path.Combine(securityIQHome, ClientFolderName);
            if (Directory.Exists(candidateClientPath)) {
                return new[] {
                    new DirectoryUpdateJob(new DirectoryUpdateJobInfo {
                        Name = "Client",
                        LocationToUpdate = candidateClientPath,
                        DirectoryWithFileUpdates = servicePackClientLocation
                    })
                };
            }

            Logger.log($"WARN: Unable to find installed client to update.");
            return new List<IUpdateJob>();
        }
    }
}