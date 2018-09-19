using Nest;
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace ElasticSearchIndexer.Dto {

    [DataContract]
    [ElasticsearchType(Name="apod")]
    public class ApodDoc {
        [DataMember(Name="source_url")]
        [Text(Name = "source_url")]
        public string source_url { get; set; }

        [DataMember(Name = "created_on")]
        [Date(Name ="created_on")]
        public DateTime created_on { get; set; }

        [DataMember(Name = "name")]
        [Text(Name = "name")]
        public string name { get; set; }

        [DataMember(Name = "title")]
        [Text(Name = "title")]
        public string title { get; set; }

        [DataMember(Name = "body")]
        [Text(Name = "body")]
        public string body { get; set; }

        [DataMember(Name = "image_url")]
        [Text(Name = "image_url")]
        public string image_url { get; set; }

        [DataMember(Name = "keywords")]
        [Text(Name = "keywords")]
        public string keywords { get; set; }

        //[DataMember(Name = "query")]
        [Percolator(Name = "query")]
        public string query { get; set; }

        //[DataMember(Name = "suggest")]
        [Completion(Name = "suggest")]
        public string suggest { get; set; }

        public static ApodDoc fromJsonFile(string jsonFilePath) {
            var serializer = new DataContractJsonSerializer(typeof(ApodDoc), new DataContractJsonSerializerSettings {
                DateTimeFormat = new DateTimeFormat("yyyy-MM-ddTHH\\:mm\\:ss.fffffffzzz")
            });
            var stream = File.OpenRead(jsonFilePath);
            var apodObj = (ApodDoc)serializer.ReadObject(stream);
            stream.Close();
            return apodObj;
        }

        public override string ToString() {

            var serializer = new DataContractJsonSerializer(typeof(ApodDoc), new DataContractJsonSerializerSettings {
                DateTimeFormat = new DateTimeFormat("yyyy-MM-ddTHH\\:mm\\:ss.fffffffzzz"),
                KnownTypes = new[] { typeof(ApodDoc)},
                EmitTypeInformation = EmitTypeInformation.Never
            });
               
            StringBuilder sb = new StringBuilder();

            writeToSB(serializer, this, sb);
            return sb.ToString();
        }

    private void writeToSB(DataContractJsonSerializer serializer, object doc, StringBuilder sb) {
        var stream = new MemoryStream();
        var writer = JsonReaderWriterFactory.CreateJsonWriter(stream, Encoding.UTF8);//, true, true, "    ");
        serializer.WriteObject(writer, doc);
        writer.Flush();

        stream.Position = 0;
        var reader = new StreamReader(stream);
        sb.AppendLine(reader.ReadToEnd());

        reader.Close();
        stream.Close();
    }

}

}
