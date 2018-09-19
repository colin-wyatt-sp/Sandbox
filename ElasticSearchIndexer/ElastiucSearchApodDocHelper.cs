using ElasticSearchIndexer.Dto;
using Nest;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace ElasticSearchIndexer {

    public class ElasticSearchApodDocHelper {

        const string EsScheme = "http";
        const string EsHost = @"192.168.32.136";
        const int EsPort = 9200;

        const string NasaIndexName = "nasa";
        const string ApodDocTypeName = "apod";
        const string ApodBulkPath = "nasa/apod/_bulk";

        private static readonly HttpClient client = new HttpClient();
        private ElasticClient elasticClient;
        private string jsonDocsPath;

        public ElasticSearchApodDocHelper(string jsonDocsPath) {
            this.jsonDocsPath = jsonDocsPath;
        }

        internal void index(int batchSize, bool recreateIndex=false) {

            try {
                if (!Directory.Exists(jsonDocsPath))
                    Directory.CreateDirectory(jsonDocsPath);

                if (recreateIndex)
                    deleteIndex(NasaIndexName);
                
                var filesToIndex = Directory.GetFiles(jsonDocsPath, "*.json").ToList();
                Console.WriteLine("Beginning indexing of " + filesToIndex.Count() + " documents.  batchSize: " + batchSize);
                int numDocsProcessed = 0;
                var docs = new List<ApodDoc>();
                foreach (var jsonDoc in filesToIndex) {
                    ApodDoc apodObj = ApodDoc.fromJsonFile(jsonDoc);
                    docs.Add(apodObj);
                    ++numDocsProcessed;
                    if (numDocsProcessed % batchSize == 0) {
                        performIndex(docs);
                        docs.Clear();
                        //return;
                        Console.Write(".");
                    }
                }
                performIndex(docs);
            } catch (Exception e) {
                Console.WriteLine("Error indexing documents: " + e.Message);
            }
            Console.WriteLine("Finished indexing.");
        }

        public void deleteIndex(string index) {

            Console.WriteLine("deleting index: " + index);
            UriBuilder uriBuilder = new UriBuilder(EsScheme, EsHost, EsPort);
            var uri = uriBuilder.Uri;

            elasticClient = new ElasticClient(uri);
            elasticClient.DeleteIndex(new DeleteIndexRequest(index));

            Console.WriteLine("Creating index: " + index);
            elasticClient.CreateIndex(index, c => c
                            .Settings(s => s
                                .NumberOfShards(3)
                            )
                            .Mappings(m => m
                                .Map<ApodDoc>(d => d
                                    .AutoMap(3)
                                )
                            )
            );
        }

        public string performSimpleQuery(string matchText) {

            Console.WriteLine("index: " + NasaIndexName);
            UriBuilder uriBuilder = new UriBuilder(EsScheme, EsHost, EsPort);
            var uri = uriBuilder.Uri;

            elasticClient = new ElasticClient(uri);
            var response = elasticClient.Search<ApodDoc>(s => s
                                .Index(NasaIndexName)
                                .Query(q => q
                                    .Match(m => m
                                        .Field(f => f.body)
                                            .Query(matchText))));

            if (response.Hits.Count > 0)
                return string.Join(" \r\n", response.Hits.Select(x => x.Source.title));

            return response.ToString();
        }

        public void highlightQuery(string query) {

            Console.WriteLine("index: " + NasaIndexName);
            UriBuilder uriBuilder = new UriBuilder(EsScheme, EsHost, EsPort);
            var uri = uriBuilder.Uri;

            elasticClient = new ElasticClient(uri);
            var response = elasticClient.Search<ApodDoc>(s => s
                                .Index(NasaIndexName)
                                .Query(q => q
                                    .Match(m => m
                                        .Field(f => f.body)
                                            .Query(query)))
                                .Highlight(h => h
                                    .Encoder(HighlighterEncoder.Html)
                                    .FragmentSize(85)
                                    .Fields(fs => fs
                                                .Field(p => p.body))));

            foreach (IHit<ApodDoc> hit in response.Hits) {
                Console.WriteLine("HIT: " + hit.Source.title.Replace("\n"," "));
                foreach (var highlightDictionary in hit.Highlights) {
                    Console.WriteLine("\t" + highlightDictionary.Key + " : ");
                    foreach (var highlight in highlightDictionary.Value.Highlights) {
                        Console.WriteLine("\t\t" + highlight);

                    }
                    Console.WriteLine();
                }   
            }
            //if (response.Hits.Count > 0)
            //    Console.WriteLine(string.Join(" \r\n", response.Hits.Select(x => x.Source.title)) );

        }

        public void putSimpleQuery(string queryName, string matchText) {

            UriBuilder uriBuilder = new UriBuilder(EsScheme, EsHost, EsPort);
            uriBuilder.Path = "nasa/apod/" + queryName;
            var uri = uriBuilder.Uri;
            //Console.WriteLine("URI: " + uri.ToString());
            var queryStr = "{ \"query\": { \"match\": { \"body\" : \"" + matchText + "\" } } }";
            var task = client.PutAsync(uri, new StringContent(queryStr, Encoding.UTF8, "application/json"));
            task.Wait();
            task.Result.EnsureSuccessStatusCode();
            Console.WriteLine("Status: " + task.Result.StatusCode + " , content: " + task.Result.Content);
        }

        public void percolateQuery(string docName) {
            UriBuilder uriBuilder = new UriBuilder(EsScheme, EsHost, EsPort);
            uriBuilder.Path = "nasa/_search";
            uriBuilder.Query = "pretty";
            var uri = uriBuilder.Uri;
            //Console.WriteLine("URI: " + uri.ToString());
            var queryStr = "{ \"query\" : { \"percolate\" : { \"field\" : \"query\", \"document\" : "
                + ApodDoc.fromJsonFile(Path.Combine(jsonDocsPath, docName + ".json")).ToString()
                + "} } }";
            var task = client.PostAsync(uri, new StringContent(queryStr, Encoding.UTF8, "application/json"));
            task.Wait();
            task.Result.EnsureSuccessStatusCode();
            var strTask = task.Result.Content.ReadAsStringAsync();
            strTask.Wait();
            Console.WriteLine("Status: " + task.Result.StatusCode + " , content: " + strTask.Result);
        }

        private void performIndex(List<ApodDoc> docs) {

            var uri = new UriBuilder(EsScheme, EsHost, EsPort).Uri;
            elasticClient = new ElasticClient(uri);

            var response = elasticClient.Bulk(br => br
                            .IndexMany(docs, (descriptor, s) => descriptor.Index(NasaIndexName).Type(ApodDocTypeName).Id(s.image_url))
                            );

            //string data = convertJsonDocsToString(docs, typeof(IndexDoc));
            //Console.WriteLine(data);
            //UriBuilder uriBuilder = new UriBuilder(EsScheme, EsHost, EsPort);
            //uriBuilder.Path = ApodBulkPath;
            //var uri = uriBuilder.Uri;
            ////Console.WriteLine("URI: " + uri.ToString());
            //var response = await client.PutAsync(uri, new StringContent(data, Encoding.UTF8, "application/json"));

            //response.EnsureSuccessStatusCode();
            ////string responseContent = await response.Content.ReadAsStringAsync();
            ////Console.WriteLine("response: isSuccess" + response.IsSuccessStatusCode + " , statusCode: " + response.StatusCode, "  , content: " + responseContent);
        }

        //private string convertJsonDocsToString(IEnumerable<ApodDoc> docs, Type docType) {

        //    var serializer = new DataContractJsonSerializer(docType, new DataContractJsonSerializerSettings {
        //        KnownTypes = new[] { typeof(IndexDoc), typeof(ApodDoc), typeof(DocInfo) },
        //        EmitTypeInformation = EmitTypeInformation.Never
        //    });
        //    //var writer = JsonReaderWriterFactory.CreateJsonWriter(stream, Encoding.UTF8, true, true, "    ");
        //    StringBuilder sb = new StringBuilder();

        //    foreach (var doc in docs) {

        //        writeToSB(serializer, new IndexDoc { doc = new DocInfo { _id = doc.source_url } }, sb);
        //        writeToSB(serializer, doc, sb);
        //    }

        //    return sb.ToString();
        //}

        //private void writeToSB(DataContractJsonSerializer serializer, object doc, StringBuilder sb) {
        //    var stream = new MemoryStream();
        //    var writer = JsonReaderWriterFactory.CreateJsonWriter(stream, Encoding.UTF8);//, true, true, "    ");
        //    serializer.WriteObject(writer, doc);
        //    writer.Flush();

        //    stream.Position = 0;
        //    var reader = new StreamReader(stream);
        //    sb.AppendLine(reader.ReadToEnd());

        //    reader.Close();
        //    stream.Close();
        //}

    }
}
