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
        public string Stock_ID { get; set; }
        public string location_ID { get; set; }
        public string Change_Amount { get; set; }
        public DateTime Change_Time { get; set; }

    }
}
