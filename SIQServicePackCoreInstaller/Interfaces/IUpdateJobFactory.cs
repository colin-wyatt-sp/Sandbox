using System.Collections.Generic;

namespace SIQServicePackCoreInstaller.Interfaces {
    public interface IUpdateJobFactory {
        
        string Name { get; }

        IEnumerable<IUpdateJob> getJobs();
    }
}