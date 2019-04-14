using System;

namespace ADTester.Interfaces
{
    public interface IActionResult
    {
        ActionReturnStatus ResturnStatus { get; }

        string Description { get; }

        string Output { get; }

        Exception Exception { get; }
    }
}