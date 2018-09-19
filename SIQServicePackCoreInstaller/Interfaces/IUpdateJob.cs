namespace SIQServicePackCoreInstaller.Interfaces {
    public interface IUpdateJob {

        string Name { get; }

        void performUpdate();

    }
}