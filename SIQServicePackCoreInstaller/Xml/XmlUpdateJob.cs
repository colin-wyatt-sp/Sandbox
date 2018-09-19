using System.IO;

namespace SIQServicePackCoreInstaller {
    public class XmlUpdateJob : IUpdateJob {

        private readonly string _xmlWithUpdatesDocPath;
        private readonly string _xmlDocToUpdatePath;
        private string _textToReplace;

        public XmlUpdateJob(string xmlWithUpdatesDocPath, string xmlDocToUpdatePath, string textToReplace) {

            _xmlWithUpdatesDocPath = xmlWithUpdatesDocPath;
            _xmlDocToUpdatePath = xmlDocToUpdatePath;
            _textToReplace = textToReplace;
        }

        public void PerformUpdate() {

            string textToUpdate = File.ReadAllText(_xmlDocToUpdatePath);
            string updatedText = File.ReadAllText(_xmlWithUpdatesDocPath);

            if (!textToUpdate.Contains(updatedText)) {
                textToUpdate = textToUpdate.Replace(_textToReplace, updatedText);
            }
            else {
                Logger.Log($"WARN: Document {_xmlDocToUpdatePath} seems to already have updates applied. skipping.");
            }
            
            File.WriteAllText(_xmlDocToUpdatePath, textToUpdate);
        }
    }
}