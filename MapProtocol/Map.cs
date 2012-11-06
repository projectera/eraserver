using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using Lidgren.Network;

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

        /// <summary>
        /// Reads a map from a buffer
        /// </summary>
        /// <param name="buffer"></param>
        public static Map Read(NetBuffer buffer)
        {
            var result = new Map();
            result.Id = new ObjectId(buffer.ReadBytes(12));
            result.TilesetId  = new ObjectId(buffer.ReadBytes(12));
            result.RegionId  = new ObjectId(buffer.ReadBytes(12));
            result.Name = buffer.ReadString();
            result.Width = buffer.ReadUInt16();
            result.Height = buffer.ReadUInt16();
            result.Type = (MapType)buffer.ReadByte();

            result.Data = new UInt16[result.Width][][];
            for (Int32 x = 0; x < result.Width; x++)
            {
                result.Data[x] = new UInt16[result.Height][];
                for (Int32 y = 0; y < result.Height; y++)
                {
                    result.Data[x][y] = new UInt16[Layers];
                    for (Int32 z = 0; z < Layers; z++)
                        result.Data[x][y][z] = buffer.ReadUInt16();
                }
            }

            result.Version = buffer.ReadVariableUInt32();
            return result;
        }

        /// <summary>
        /// Writes a map to a buffer
        /// </summary>
        /// <param name="buffer"></param>
        public void Write(NetBuffer buffer)
        {
            buffer.Write(this.Id.ToByteArray());
            buffer.Write(this.TilesetId.ToByteArray());
            buffer.Write(this.RegionId.ToByteArray());
            buffer.Write(this.Name);
            buffer.Write(this.Width);
            buffer.Write(this.Height);
            buffer.Write((Byte)this.Type);

            for(Int32 x = 0; x < this.Width; x++)
                for(Int32 y = 0; y < this.Height; y++)
                    for (Int32 z = 0; z < Layers; z++)
                        buffer.Write(this.Data[x][y][z]);
           
            buffer.WriteVariableUInt32(this.Version);
        }
    }
}