using System.Collections.Generic;

namespace SIQServicePackCoreInstaller {
    public interface IUpdateJobFactory {
        
        string Name { get; }

        IEnumerable<IUpdateJob> GetJobs();
    }
}