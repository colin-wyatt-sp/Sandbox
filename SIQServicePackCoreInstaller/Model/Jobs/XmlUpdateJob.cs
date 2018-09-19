using System.IO;
using SIQServicePackCoreInstaller.Interfaces;

namespace SIQServicePackCoreInstaller.Model.Jobs {

    public class XmlUpdateJob : IUpdateJob {

        private readonly string _xmlWithUpdatesDocPath;
        private readonly string _xmlDocToUpdatePath;
        private readonly string _textToReplace;

        public XmlUpdateJob(string xmlWithUpdatesDocPath, string xmlDocToUpdatePath, string textToReplace) {

            _xmlWithUpdatesDocPath = xmlWithUpdatesDocPath;
            _xmlDocToUpdatePath = xmlDocToUpdatePath;
            _textToReplace = textToReplace;
        }

        public void performUpdate() {

            string textToUpdate = File.ReadAllText(_xmlDocToUpdatePath);
            string updatedText = File.ReadAllText(_xmlWithUpdatesDocPath);

            if (!textToUpdate.Contains(updatedText)) {
                textToUpdate = textToUpdate.Replace(_textToReplace, updatedText);
            }
            else {
                Logger.log($"WARN: Document {_xmlDocToUpdatePath} seems to already have updates applied. skipping.");
            }
            
            File.WriteAllText(_xmlDocToUpdatePath, textToUpdate);
        }
    }
}