using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ERA.Services.InteractableService.Data
{
    internal class ServerInteractable
    {
        [BsonId]
        public ObjectId Id;

        [BsonRequired]
        public ERA.Protocols.InteractableProtocol.Interactable PublicData { get; set; }
    }
}
