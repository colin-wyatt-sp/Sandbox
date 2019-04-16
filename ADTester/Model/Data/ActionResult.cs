using System;
using ADTester.Interfaces;

namespace ADTester.Model.Data
{
    public class ActionResult : IActionResult
    {
        public ActionResult(ActionReturnStatus status, string description, string output, Exception exception)
        {
            ResturnStatus = status;
            Description = description;
            Output = output;
            Exception = exception;
        }

        public ActionReturnStatus ResturnStatus { get; }
        public string Description { get; }
        public string Output { get; }
        public Exception Exception { get; }
    }
}