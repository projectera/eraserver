using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace PlayerProtocol
{
    public class Friend
    {
        /// <summary>
        /// 
        /// </summary>
        [BsonId]
        public ObjectId Id
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        [BsonRequired]
        public ObjectId PlayerId
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        [BsonIgnore]
        public DateTime FriendSince
        {
            get { return Id.CreationTime; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static Friend Generate(ObjectId player)
        {
            Friend result = new Friend();
            result.Id = ObjectId.GenerateNewId();
            result.PlayerId = player;

            return result;
        }
    }
}