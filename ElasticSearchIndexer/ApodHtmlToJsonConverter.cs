using ElasticSearchIndexer.Dto;
using HtmlAgilityPack;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace ElasticSearchIndexer {

    class ApodHtmlToJsonConverter {

        const string BaseUrl = @"http://apod.nasa.gov/apod/";

        public void convertApodHtmlToJson(string sourcePath, string destinationPath, bool reprocess= false) {

            Console.WriteLine("Converting APOD html files to JSON document files. Source: " + sourcePath + ", destination: " + destinationPath);
            prepareDestinationFolder(destinationPath, reprocess);

            try {
                performConvert(sourcePath, destinationPath);
            } catch (Exception e) {
                Console.WriteLine("Error: " + e.Message);
            }
        }

        private static void prepareDestinationFolder(string destinationPath, bool reprocess) {
            if (!Directory.Exists(destinationPath)) {
                Directory.CreateDirectory(destinationPath);
            } else if (reprocess) {
                ClearExistingJsonFiles(destinationPath);
            }
        }

        private void performConvert(string sourcePath, string destinationPath) {

            foreach (string f in Directory.GetFiles(sourcePath, "*.html")) {

                var destFilePath = Path.Combine(destinationPath, Path.GetFileNameWithoutExtension(f) + ".json");
                if (File.Exists(destFilePath))
                    continue;

                var apodDoc = processApodHtmlFile(f);
                if (apodDoc == null)
                    continue;

                WriteApodAsJson(destFilePath, apodDoc, typeof(ApodDoc));
            }
        }

        private static void ClearExistingJsonFiles(string destinationPath) {
            if (!Directory.Exists(destinationPath))
                return;

            foreach (var jsonFile in Directory.GetFiles(destinationPath, "*.json")) {
                File.Delete(jsonFile);
            }
        }

        private ApodDoc processApodHtmlFile(string filename) {

            ApodDoc apodDoc = new ApodDoc();
            try {

                processHtmlToApodDocObj(filename, out apodDoc);

            } catch (Exception e) {

                Console.WriteLine();
                Console.WriteLine("Error processing: " + Path.GetFileNameWithoutExtension(filename) + " , msg: " + e.Message);
                throw;
                ////if (apodDoc != null && apodDoc.source_url != null && apodDoc.body != null)
                ////    return apodDoc;
                ////else
                //    return null;
            }
            return apodDoc;
        }

        private void processHtmlToApodDocObj(string filename, out ApodDoc apodDoc) {
            
            HtmlDocument htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(File.ReadAllText(filename));
            apodDoc = new ApodDoc();
            apodDoc.source_url = BaseUrl + Path.GetFileName(filename);
            apodDoc.created_on = getTimestampStringFrom(Path.GetFileName(filename));
            apodDoc.title = htmlDocument.DocumentNode.SelectNodes("//title").Single().InnerText;
            var boldNodes = htmlDocument.DocumentNode.SelectNodes("//b");
            var firstBoldNode = boldNodes?.First();
            apodDoc.name = firstBoldNode != null ? firstBoldNode.InnerText : apodDoc.title.Substring(apodDoc.title.IndexOf("-") + 2) ;

            try {
                apodDoc.body = htmlDocument.DocumentNode.SelectSingleNode("/html[1]/body[1]").InnerText;
            } catch (Exception) {
                apodDoc.body = htmlDocument.DocumentNode.InnerText;
            }

            var metaNodes = htmlDocument.DocumentNode.SelectNodes("//meta[@name]");
            var keywordsNode = metaNodes?.FirstOrDefault(x => x.GetAttributeValue("name", null) == "keywords");
            if (keywordsNode != null) {
                apodDoc.keywords = keywordsNode.Attributes["content"].Value;
            } 

            var imgHrefNode = htmlDocument.DocumentNode.SelectNodes("//a[@href]").ToList().Where(x => x.LastChild != null && x.LastChild.Name == "img").FirstOrDefault();
            if (imgHrefNode != null)
                apodDoc.image_url = imgHrefNode.Attributes.First().Value;
            else {
                var imgSrcNodes = htmlDocument.DocumentNode.SelectNodes("//img[@src]");
                if (imgSrcNodes != null) {
                    apodDoc.image_url = imgSrcNodes.ToList().First().Attributes.First().Value;
                }
            }
            if (apodDoc.image_url != null)
                apodDoc.image_url = BaseUrl + apodDoc.image_url;
            else
                Console.WriteLine("Unable to find image for: " + apodDoc.source_url);
        }

        private static void WriteApodAsJson(string destFilePath, object doc, Type docType) {

            var serializer = new DataContractJsonSerializer(docType, new DataContractJsonSerializerSettings {
                DateTimeFormat = new DateTimeFormat("yyyy-MM-ddTHH\\:mm\\:ss.fffffffzzz"),
                KnownTypes = new[] { typeof(IndexDoc), typeof(ApodDoc), typeof(DocInfo) },
                EmitTypeInformation = EmitTypeInformation.Never
            });

            var stream = new MemoryStream();
            var writer = JsonReaderWriterFactory.CreateJsonWriter(stream, Encoding.UTF8, true, true, "    ");
            serializer.WriteObject(writer, doc);
            writer.Flush();

            stream.Position = 0;
            var reader = new StreamReader(stream);

            Console.Write(Path.GetFileName(destFilePath) + " ");

            stream.Position = 0;
            File.WriteAllText(destFilePath, reader.ReadToEnd());
            reader.Close();
            stream.Close();
        }

        private DateTime getTimestampStringFrom(string filename) {

            if (!filename.StartsWith("ap")) {
                return default(DateTime);
            }
            var dtStr = filename.Substring(2);
            var year = dtStr.Substring(0, 2);
            int intYear;
            if (int.Parse(year) > 30) {
                intYear = 1900 + int.Parse(year);
            } else {
                intYear = 2000 + int.Parse(year);
            }
            int month = int.Parse(dtStr.Substring(2, 2));
            int day = int.Parse(dtStr.Substring(4, 2));

            return new DateTime(intYear, month, day);
        }


        //private static void WriteJsonDocToConsole(object doc, Type docType) {
        //    var stream = new MemoryStream();
        //    var serializer = new DataContractJsonSerializer(docType, new DataContractJsonSerializerSettings {
        //        KnownTypes = new[] { typeof(IndexDoc), typeof(ApodDoc), typeof(DocInfo) },
        //        EmitTypeInformation = EmitTypeInformation.Never
        //    });
        //    //var writer = JsonReaderWriterFactory.CreateJsonWriter(stream, Encoding.UTF8, true, true, "    ");
        //    var writer = JsonReaderWriterFactory.CreateJsonWriter(stream, Encoding.UTF8);//, true, true, "    ");
        //    serializer.WriteObject(writer, doc);
        //    writer.Flush();

        //    stream.Position = 0;
        //    var reader = new StreamReader(stream);
        //    Console.WriteLine(reader.ReadToEnd());

        //    reader.Close();
        //    stream.Close();
        //}
    }
}
