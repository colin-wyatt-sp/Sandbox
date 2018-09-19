using System;
using System.IO;

namespace ElasticSearchIndexer {

    class Program {

        static string ApodHtmlDocsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @"Scratch\APOD_html");
        static string ApodJsonDocsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),  @"Scratch\APOD_docs");
        static string ApodImagesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @"Pictures\APOD");

        static void Main(string[] args) {

            try {

                ProcessArguments(args);
            }
            catch (Exception e) {
                Console.WriteLine("Error: " + e.Message);
            }
        }

        private static void ProcessArguments(string[] args) {

            int batchSize = 250;
            foreach (var arg in args) {

                if (string.Compare(arg, "crawl", true) == 0) {
                    new ApodCrawler().crawl(ApodHtmlDocsPath);
                }
                if (string.Compare(arg, "updateJson", true) == 0) {
                    new ApodHtmlToJsonConverter().convertApodHtmlToJson(ApodHtmlDocsPath, ApodJsonDocsPath, reprocess: false);
                }
                if (string.Compare(arg, "recreateJson", true) == 0) {
                    new ApodHtmlToJsonConverter().convertApodHtmlToJson(ApodHtmlDocsPath, ApodJsonDocsPath, reprocess: true);
                }
                if (arg.ToLower().StartsWith("batchsize=") && arg.Length > 10) {
                    int.TryParse(arg.Substring(10), out batchSize);
                }
                if (string.Compare(arg, "index", true) == 0) {
                    new ElasticSearchApodDocHelper(ApodJsonDocsPath).index(batchSize, recreateIndex: false);
                }
                if (string.Compare(arg, "reindex", true) == 0) {
                    new ElasticSearchApodDocHelper(ApodJsonDocsPath).index(batchSize, recreateIndex: true);
                }
                if (string.Compare(arg, "images", true) == 0) {
                    new ApodImageDownloader().getImages(ApodJsonDocsPath, ApodImagesPath, delayMillis: 400);
                }
                if (string.Compare(arg, "doall", true) == 0) {
                    new ApodCrawler().crawl(ApodHtmlDocsPath);
                    new ApodHtmlToJsonConverter().convertApodHtmlToJson(ApodHtmlDocsPath, ApodJsonDocsPath, reprocess: true);
                    new ElasticSearchApodDocHelper(ApodJsonDocsPath).index( batchSize, recreateIndex: true);
                    new ApodImageDownloader().getImages(ApodJsonDocsPath, ApodImagesPath, delayMillis: 400);
                }

                if (arg.ToLower().StartsWith("query=")) {
                    var query = arg.Substring(arg.IndexOf("=") + 1);
                    var result = new ElasticSearchApodDocHelper(ApodJsonDocsPath).performSimpleQuery(query);
                    Console.WriteLine("Query: " + query);
                    Console.WriteLine("Result: " + result);
                }

                if (arg.ToLower().StartsWith("putquery=")) {
                    var query = arg.Substring(arg.IndexOf("=") + 1);
                    new ElasticSearchApodDocHelper(ApodJsonDocsPath).putSimpleQuery(query.Replace(" ","_").Replace(",","_"), query);
                }
                if (arg.ToLower().StartsWith("percolatedoc=")) {
                    var docName = arg.Substring(arg.IndexOf("=") + 1);
                    new ElasticSearchApodDocHelper(ApodJsonDocsPath).percolateQuery(docName);
                }
                if (arg.ToLower().StartsWith("highlight=")) {
                    var query = arg.Substring(arg.IndexOf("=") + 1);
                    new ElasticSearchApodDocHelper(ApodJsonDocsPath).highlightQuery(query);
                }
            }
        }
    }
}
