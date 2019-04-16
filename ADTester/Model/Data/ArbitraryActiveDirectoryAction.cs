using System;
using System.CodeDom.Compiler;
using System.DirectoryServices;
using System.Reflection;
using System.Text;
using System.Windows;
using ADTester.Interfaces;
using Microsoft.CSharp;

namespace ADTester.Model.Data
{
    public class ArbitraryActiveDirectoryAction : IArbitraryAction
    {

        public ArbitraryActiveDirectoryAction(string description, string codeText)
        {
            IsEnabled = true;
            Description = description;
            Code = codeText;
            
        }

        public void setParameters(string domain, string specificServer, string username, string password, bool isSsl)
        {
            Domain = domain;
            SpecificServer = specificServer;
            Username = username;
            Password = password;
            IsSsl = isSsl;
        }

        public string Description { get; }

        public bool IsEnabled { get; set; }

        public IActionResult executeAction()
        {
            MethodInfo methodInfo = CreateFunction(Code);

            ActionReturnStatus status = ActionReturnStatus.Success;
            string output = string.Empty;
            Exception exception = null;
            try
            {
                output = (string) methodInfo.Invoke(null, new object[] {Domain, SpecificServer, Username, Password, IsSsl});
            }
            catch (Exception e)
            {
                exception = e;
                status = ActionReturnStatus.ErrorDetected;
                output = e.ToString();
            }

            return new ActionResult(status, Description, output, exception);
        }

        private static MethodInfo CreateFunction(string body)
        {
            string code = @"
        using System;
        using System.Text;
        using System.DirectoryServices;
        using System.Text.RegularExpressions;
            
        namespace Foo
        {                
            public static class Bar
            {                
                public static string Function(string domain, string specificServer, string username, string password, bool isSsl)
                {
                    StringBuilder sb = new StringBuilder();
                    body_text;
                    return sb.ToString();
                }
            }
        }
    ";
            //DirectoryEntry entry;
            string finalCode = code.Replace("body_text", body);

            CSharpCodeProvider provider = new CSharpCodeProvider();
            CompilerParameters parameters = new CompilerParameters();
            parameters.ReferencedAssemblies.Add("System.dll");
            parameters.ReferencedAssemblies.Add("System.Runtime.dll");
            parameters.ReferencedAssemblies.Add("System.DirectoryServices.dll");
            parameters.ReferencedAssemblies.Add("System.Configuration.dll");
            parameters.ReferencedAssemblies.Add("System.Text.RegularExpressions.dll");
            // True - memory generation, false - external file generation
            parameters.GenerateInMemory = true;
            // True - exe file generation, false - dll file generation
            parameters.GenerateExecutable = false;

            CompilerResults results = provider.CompileAssemblyFromSource(parameters, finalCode);

            if (results.Errors.HasErrors)
            {
                StringBuilder sb = new StringBuilder();

                foreach (CompilerError error in results.Errors)
                {
                    sb.AppendLine(String.Format("Error ({0}): {1}", error.ErrorNumber, error.ErrorText));
                }

                MessageBox.Show("Compile error: " + sb.ToString());
                //throw new InvalidOperationException(sb.ToString());
            }

            Type binaryFunction = results.CompiledAssembly.GetType("Foo.Bar");
            return binaryFunction.GetMethod("Function");
        }

        public string Code { get; set; }
        public string Domain { get; set; }
        public string SpecificServer { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool IsSsl { get; set; }
    }

}