using System;
using System.CodeDom.Compiler;
using System.Collections;
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
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Collections;
            
namespace Foo
{                
    public static class Bar
    {                
        public static void Function(StringBuilder sb, string domain, string domainNetbios, string specificServer, string username, string password, bool isSsl)
        {
            body_text;
        }
    }
}";
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
            parameters.ReferencedAssemblies.Add("System.Collections.Concurrent.dll");
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
                    sb.AppendLine(String.Format("Error ({0}): {1} - LineNum: {2}", error.ErrorNumber, error.ErrorText, error.Line - 16));
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





        public void getObjectSidByDN(StringBuilder sb, string domain, string domainNetbios, string specificServer,
            string username, string password, bool isSsl)
        {
            int port = isSsl ? 636 : 389;
            string ldapBaseAddr = !string.IsNullOrWhiteSpace(specificServer) ? specificServer : domain;
            string ldapPath = string.Format("LDAP://{0}:{1}/RootDSE", ldapBaseAddr, port);
            sb.AppendLine(ldapPath);
            AuthenticationTypes authType = AuthenticationTypes.Secure;
            if (isSsl)
                authType |= AuthenticationTypes.SecureSocketsLayer;
            if (!string.IsNullOrWhiteSpace(specificServer))
                authType |= AuthenticationTypes.ServerBind;

            sb.AppendLine("creating DirectoryEntry obj with username=" + username);
            DirectoryEntry directoryEntry = new DirectoryEntry(ldapPath, username, password, authType);

            string forestDN;
            using (directoryEntry)
            {
                sb.AppendLine("Getting property 'rootDomainNamingContext'");
                forestDN = directoryEntry.Properties["rootDomainNamingContext"].Value as string;
                sb.AppendLine("forestDN = " + forestDN);
                //forestFQDN = getForestFqdn(forestDN);
            }

            directoryEntry = new DirectoryEntry();

            // Set username and password
            if (
                username.Contains(@"\") ||
                username.Contains(@"@")
            )
            {
                directoryEntry.Username = username;
            }
            else
            {
                directoryEntry.Username = String.Format(@"{0}\{1}", domainNetbios, username);
            }

            directoryEntry.Password = password;

            string domainDN = domain;
            domainDN = Regex.Replace(
                domainDN,
                @"\.",
                ",dc="
            );
            domainDN =
                String.Format(
                    "dc={0}",
                    domainDN
                );

            StringBuilder path = new StringBuilder();
            path.AppendFormat(
                "LDAP://{0}:{1}/{2}",
                string.IsNullOrEmpty(specificServer) ? domain : specificServer,
                port.ToString(),
                "CN=Configuration," + forestDN
            );

            directoryEntry.Path = path.ToString();
            sb.AppendLine(path.ToString() + "  ");

            // setting the authentication type.
            directoryEntry.AuthenticationType = AuthenticationTypes.Secure;

            // adding to the base auth type
            if (isSsl)
            {
                directoryEntry.AuthenticationType = directoryEntry.AuthenticationType |
                                                    AuthenticationTypes.SecureSocketsLayer;
            }

            // adding to the base auth type
            if (!string.IsNullOrEmpty(specificServer))
            {
                directoryEntry.AuthenticationType = directoryEntry.AuthenticationType |
                                                    AuthenticationTypes.ServerBind;
            }

            string returnedSid = null;

            // creating a temp directory searcher.
            DirectorySearcher searcher = new DirectorySearcher(directoryEntry);

            // setting the search scope.
            searcher.SearchScope = SearchScope.Subtree;

            // setting the timeout.
            searcher.ClientTimeout = new TimeSpan(0, 0, seconds: 10);
            searcher.PageSize = 100;

            string dn = domainDN;

            searcher.SearchScope = SearchScope.OneLevel;
            searcher.PageSize = 512;

            searcher.SearchRoot.Path = path.ToString();
            searcher.Filter = null;

            searcher.PropertiesToLoad.Clear();
            searcher.PropertiesToLoad.Add("name");
            searcher.PropertiesToLoad.Add("objectClass");
            searcher.PropertiesToLoad.Add("distinguishedName");

            sb.AppendLine("getting permission..");

            string[] extendedRightsProperties = new string[]
            {
                "rightsGuid",
                "displayName"
            };

            // Configure the directory searcher
            searcher.Filter = "(objectCategory=controlAccessRight)";
            searcher.SearchScope = SearchScope.Subtree;
            searcher.PropertiesToLoad.AddRange(extendedRightsProperties);
            // Larger page sizes may cause the search to get only one page of objects
            searcher.PageSize = 512;

            SearchResultCollection extendedRightsFound;

            using (extendedRightsFound = searcher.FindAll())
            {
                foreach (SearchResult searchResult in extendedRightsFound)
                {
                    // Get the extended right's wanted properties
                    Guid rightsGuid = new Guid(searchResult.Properties["rightsGuid"][0].ToString());
                    String displayName = searchResult.Properties["displayName"][0].ToString();
                    // Some default extended rights have the same GUID, and we need to avoid those duplicates

                    sb.AppendLine(string.Format("{0} - {1}", rightsGuid.ToString(), displayName));
                    
                }
            }
        }


    }


}