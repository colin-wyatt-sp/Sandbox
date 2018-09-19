using System.Runtime.Serialization;

namespace ElasticSearchIndexer.Dto {

    [DataContract]
    public class IndexDoc {

        [DataMember(Name = "index")]
        public DocInfo doc = new DocInfo();
    }

}
