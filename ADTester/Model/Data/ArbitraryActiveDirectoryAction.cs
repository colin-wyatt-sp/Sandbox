using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.DirectoryServices;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using ADTester.Annotations;
using ADTester.Interfaces;
using Microsoft.CSharp;

namespace ADTester.Model.Data
{
    public class ArbitraryActiveDirectoryAction : IArbitraryAction, INotifyPropertyChanged
    {
        private bool _isEnabled;

        public ArbitraryActiveDirectoryAction(string description, string codeText)
        {
            IsEnabled = true;
            Description = description;
            Code = codeText;
            
        }

        public void setParameters(string domain, string domainNetbios, string specificServer, string username, string password, bool isSsl)
        {
            Domain = domain;
            DomainNetbios = domainNetbios;
            SpecificServer = specificServer;
            Username = username;
            Password = password;
            IsSsl = isSsl;
        }

        public string DomainNetbios { get; set; }

        public string Description { get; }

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                _isEnabled = value; 
                onPropertyChanged();
            }
        }

        public IActionResult executeAction()
        {
            MethodInfo methodInfo = CreateFunction(Code);
            if (methodInfo == null) return null;

            ActionReturnStatus status = ActionReturnStatus.Success;
            StringBuilder sb = new StringBuilder();
            string output = string.Empty;
            Exception exception = null;
            try
            {
                methodInfo.Invoke(null, new object[] {sb, Domain, DomainNetbios, SpecificServer, Username, Password, IsSsl});
                output = sb.ToString();
            }
            catch (Exception e)
            {
                exception = e;
                status = ActionReturnStatus.ErrorDetected;
                output = sb.ToString()  + Environment.NewLine + e.ToString();
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
        using System.ServiceModel;
        using System.Security.Principal;  
            
        namespace Foo
        {                
            public static class Bar
            {                
                public static void Function(StringBuilder sb, string domain, string domainNetbios, string specificServer, string username, string password, bool isSsl)
                {
                    body_text;
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
            parameters.ReferencedAssemblies.Add("System.ServiceModel.dll");
            parameters.ReferencedAssemblies.Add("System.Security.dll");
            //parameters.ReferencedAssemblies.Add("System.Security.Principal.Windows.dll");
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
                return null;
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

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void onPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }





        //public string getObjectSidByDN(StringBuilder sb, string domain, string domainNetbios, string specificServer, string username, string password, bool isSsl)
        //{
        //    DirectoryEntry directoryEntry = new DirectoryEntry();
        //    int port = isSsl ? 636 : 389;
        //    // Set username and password
        //    if (
        //        username.Contains(@"\") ||
        //        username.Contains(@"@")
        //    )
        //    {
        //        directoryEntry.Username = username;
        //    }
        //    else
        //    {
        //        directoryEntry.Username = String.Format(@"{0}\{1}", domainNetbios, username);
        //    }

        //    directoryEntry.Password = password;

        //    string domainDN = domain;
        //    domainDN = Regex.Replace(
        //        domainDN,
        //        @"\.",
        //        ",dc="
        //    );
        //    domainDN =
        //        String.Format(
        //            "dc={0}",
        //            domainDN
        //        );

        //    StringBuilder path = new StringBuilder();
        //    path.AppendFormat(
        //        "LDAP://{0}:{1}/{2}",
        //        string.IsNullOrEmpty(specificServer) ? domain : specificServer,
        //        port.ToString(),
        //        domainDN
        //    );
        //    directoryEntry.Path = path.ToString();
            
        //    // setting the authentication type.
        //    directoryEntry.AuthenticationType = AuthenticationTypes.Secure;

        //    // adding to the base auth type
        //    if (isSsl)
        //    {
        //        directoryEntry.AuthenticationType = directoryEntry.AuthenticationType |
        //                                            AuthenticationTypes.SecureSocketsLayer;
        //    }

        //    // adding to the base auth type
        //    if (!string.IsNullOrEmpty(specificServer))
        //    {
        //        directoryEntry.AuthenticationType = directoryEntry.AuthenticationType |
        //                                            AuthenticationTypes.ServerBind;
        //    }
        //    string returnedSid = null;

        //    // creating a temp directory searcher.
        //    DirectorySearcher searcher = new DirectorySearcher(directoryEntry);

        //    // setting the search scope.
        //    searcher.SearchScope = SearchScope.Subtree;

        //    // setting the timeout.
        //    searcher.ClientTimeout = new TimeSpan(0, 0, seconds: 10);

        //    string dn = domainDN;

        //    if (searcher != null)
        //    {

        //        try
        //        {

        //            // Get all policies from the Active Directory domain
        //            searcher.SearchScope = SearchScope.Base;
        //            searcher.PropertiesToLoad.AddRange(
        //                new string[] { "objectSid" }
        //            );

        //            // Set the LDAP query filter
        //            searcher.Filter =
        //                string.Format(
        //                    "{0}={1}",
        //                    "distinguishedName",
        //                    dn
        //                );

        //            // Find the Sid
        //            SearchResult domainFound = searcher.FindOne();

        //            if (domainFound != null)
        //            {
        //                // Get the sid as a byte array
        //                byte[] sidBytes = (byte[])domainFound.Properties["objectSid"][0];

        //                // Create SecurityIdentifier
        //                System.Security.Principal.SecurityIdentifier sid = new System.Security.Principal.SecurityIdentifier(sidBytes, 0);

        //                // Return the Sid as string
        //                returnedSid = sid.Value;
        //                sb.AppendLine("Successfully got SID: " + returnedSid);
        //            }
        //            else
        //            {
        //                sb.AppendLine(string.Format("Attempt to get object's Sid failed. Object's DN: {0}", dn));
        //            }


        //        }
        //        catch (System.Runtime.InteropServices.COMException ex)
        //        {
        //            sb.AppendLine(string.Format("Error in '{0}'.", dn));
        //        }
        //        catch (Exception ex)
        //        {
        //            sb.AppendLine(
        //                string.Format(
        //                    "Error fetching object's sid. Object DN: {0}",
        //                    dn
        //                ));
        //        }
        //        finally
        //        {
        //            if (searcher != null)
        //            {
        //                searcher.Dispose();
        //            }
        //        }

        //    }

        //    return returnedSid;
        //}



    }

}