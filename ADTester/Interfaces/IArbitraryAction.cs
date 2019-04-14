namespace ADTester.Interfaces
{
    public interface IArbitraryAction
    {
        string Description { get; }

        bool IsEnabled { get; set; }

        IActionResult executeAction();

        string Code { get; set; }
    }
}