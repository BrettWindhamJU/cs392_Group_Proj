using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace cs392_demo.models
{
    public class InventoryLog
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]

        public string Log_ID { get; set; }
        public string Stock_ID_Log { get; set; }
        public string BusinessId { get; set; }
        public int Quantity_Before { get; set; }
        public int Quantity_After { get; set; }
        public string Changed_By { get; set; }
        public DateTime Changed_At { get; set; }

    }
}
