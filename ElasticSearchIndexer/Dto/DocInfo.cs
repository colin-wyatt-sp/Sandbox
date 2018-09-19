using System.Runtime.Serialization;

namespace ElasticSearchIndexer.Dto {

    [DataContract]
    public class DocInfo {
        //[DataMember]
        //public string _index;
        //[DataMember]
        //public string _type;
        [DataMember]
        public string _id;
    }

}
