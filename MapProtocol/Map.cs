using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace MapProtocol
{
    [BsonIgnoreExtraElements(true)]
    public class Map
    {
        /// <summary>
        /// 
        /// </summary>
        public static readonly Byte Layers = 3;

        /// <summary>
        /// Id of the map
        /// </summary>
        [BsonId]
        public ObjectId Id
        {
            get;
            set;
        }

        /// <summary>
        /// Id of the tileset
        /// </summary>
        [BsonRequired]
        public ObjectId TilesetId
        {
            get;
            set;
        }

        /// <summary>
        /// Id of the region
        /// </summary>
        [BsonRequired]
        public ObjectId RegionId
        {
            get;
            set;
        }

        /// <summary>
        /// Map name
        /// </summary>
        public String Name
        {
            get;
            set;
        }

        /// <summary>
        /// Map width in tiles
        /// </summary>
        [BsonRequired]
        public UInt16 Width
        {
            get;
            set;
        }

        /// <summary>
        /// Map height in tiles
        /// </summary>
        [BsonRequired]
        public UInt16 Height
        {
            get;
            set;
        }

        /// <summary>
        /// Map type
        /// </summary>
        [BsonRequired]
        public MapType Type
        {
            get;
            set;
        }

        /// <summary>
        /// Map data
        /// </summary>
        [BsonRequired]
        public UInt16[][][] Data
        {
            get;
            set;
        }

        /// <summary>
        /// Mapdata version
        /// </summary>
        [BsonRequired]
        public UInt32 Version
        {
            get;
            set;
        }

        /// <summary>
        /// Creates a new map
        /// </summary>
        public Map() { }

        /// <summary>
        /// Generates a new map object from data
        /// </summary>
        /// <param name="id">Map Id</param>
        /// <param name="tilesetId">Tileset Id</param>
        /// <param name="regionId">Region Id</param>
        /// <param name="name">Name of the Map</param>
        /// <param name="type">Map Type</param>
        /// <param name="width">Width of the map in tiles</param>
        /// <param name="height">Height of the map in tiles</param>
        /// <param name="data">Tile Id's</param>
        /// <param name="version">Version of the map data</param>
        public static Map Generate(ObjectId id, ObjectId tilesetId, ObjectId regionId, String name, MapType type,
            UInt16 width, UInt16 height, UInt16[][][] data, UInt32 version)
        {
            Map result = new Map();
            result.Id = id;
            result.TilesetId = tilesetId;
            result.RegionId = regionId;
            result.Name = name;
            result.Type = type;
            result.Width = width;
            result.Height = height;
            result.Data = data;
            result.Version = version;

            return result;
        }

        /// <summary>
        /// Clears the map data
        /// </summary>
        public void Clear()
        {
            this.Id = ObjectId.Empty;
            this.Name = String.Empty;
            this.TilesetId = ObjectId.Empty;
            this.RegionId = ObjectId.Empty;
            this.Type = MapType.NotSpecified;
            this.Width = 0;
            this.Height = 0;
            this.Data = new UInt16[0][][];
            this.Version = 0;
        }

        /// <summary>
        /// Get HashCode
        /// </summary>
        /// <returns>HashCode for this object</returns>
        public override Int32 GetHashCode()
        {
            // Hash the TileData
            Int32 code = 0;
            foreach (UInt16[][] ystack in this.Data)
                foreach (UInt16[] stack in ystack)
                    foreach (UInt16 item in stack)
                        code = (code * 31) ^ item;

            // Return the Hashcode
            return (Int32)(this.TilesetId.GetHashCode() * 127 ^//(MongoObjectId.GetHashCode(this.TilesetId.ToByteArray()) * 127) ^
                ((this.Name == null ? (String.Empty.GetHashCode()) : this.Name.GetHashCode()) * 63) ^
                (this.Width * 7) ^
                (this.Height * 15) ^
                ((Int32)this.Type * 3) ^
                (this.Version * 255) ^
                code);
        }
    }
}