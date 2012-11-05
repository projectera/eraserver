using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MongoDB.Bson
{
    public static class ObjectIdExtensions
    {
        /// <summary>
        /// Creates a new MongoObjectId from an Integer
        /// </summary>
        /// <param name="value"></param>
        public static ObjectId CreateFrom(Int32 value)
        {
            var data = new Byte[12];
            Array.Copy(BitConverter.GetBytes(value), 0, data, 8, 4);
            
            return new ObjectId(data);
        }

        /// <summary>
        /// Creates a new MongoObjectId from an Unsigned Integer
        /// </summary>
        /// <param name="value"></param>
        public static ObjectId CreateFrom(UInt32 value)
        {
            var data = new Byte[12];
            Array.Copy(BitConverter.GetBytes(value), 0, data, 8, 4);
            
            return new ObjectId(data);
        }

        /// <summary>
        /// Creates a new MongoObjectId from a Long
        /// </summary>
        /// <param name="value"></param>
        public static ObjectId CreateFrom(Int64 value)
        {
            var data = new Byte[12];
            Array.Copy(BitConverter.GetBytes(value), 0, data, 4, 8);

            return new ObjectId(data);
        }

        /// <summary>
        /// Creates a new MongoObjectId from an Unsigned Long
        /// </summary>
        /// <param name="value"></param>
        public static ObjectId CreateFrom(UInt64 value)
        {
            var data = new Byte[12];
            Array.Copy(BitConverter.GetBytes(value), 0, data, 4, 8);

            return new ObjectId(data);
        }
    }
}
