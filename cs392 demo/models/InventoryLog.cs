using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace cs392_demo.models
{
    public class InventoryLog
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]

        public string Log_ID { get; set; } = string.Empty;
        public string Stock_ID_Log { get; set; } = string.Empty;
        public string BusinessId { get; set; } = string.Empty;
        public int Quantity_Before { get; set; }
        public int Quantity_After { get; set; }
        public string Changed_By { get; set; } = string.Empty;
        public DateTime Changed_At { get; set; }

    }
}
