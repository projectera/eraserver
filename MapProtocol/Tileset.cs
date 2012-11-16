using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using Lidgren.Network;

namespace ERA.Protocols.MapProtocol
{
    public class Tileset
    {
        /// <summary>
        /// TilesetId
        /// </summary>
        [BsonId]
        public ObjectId Id
        {
            get;
            set;
        }

        /// <summary>
        /// Tileset Name
        /// </summary>
        [BsonRequired]
        public String Name
        {
            get;
            set;
        }

        /// <summary>
        /// Tileset Asset Id
        /// </summary>
        [BsonRequired]
        public String AssetName
        {
            get;
            set;
        }

        /// <summary>
        /// Autotile Asset Id's
        /// </summary>
        [BsonRequired]
        public List<String> AutotileAssetNames
        {
            get;
            set;
        }

        /// <summary>
        /// Autotile Animation Flags
        /// </summary>
        public List<Boolean> AutotileAnimationFlags
        {
            get;
            set;
        }

        /// <summary>
        /// Passage Bytes
        /// </summary>
        public Byte[] Passages
        {
            get;
            set;
        }

        /// <summary>
        /// Priority Bytes
        /// </summary>
        public Byte[] Priorities
        {
            get;
            set;
        }

        /// <summary>
        /// Flag Bytes
        /// </summary>
        public Byte[] Flags
        {
            get;
            set;
        }

        /// <summary>
        /// Tag Bytes
        /// </summary>
        public Byte[] Tags
        {
            get;
            set;
        }

        /// <summary>
        /// Number of Tiles
        /// </summary>
        [BsonIgnoreIfDefault]
        public Int32 Tiles
        {
            get;
            set;
        }

        /// <summary>
        /// Version number
        /// </summary>
        public UInt32 Version
        {
            get;
            set;
        }

        /// <summary>
        /// Creates a new tileset
        /// </summary>
        public Tileset() { }

        /// <summary>
        /// Generates a  tileset from data
        /// </summary>
        /// <param name="id">Tileset id</param>
        /// <param name="name">Name</param>
        /// <param name="assetName">Asset File Id</param>
        /// <param name="autotileNames">Autotile File Ids</param>
        /// <param name="autotileAnimation">Autotile Animation flags</param>
        /// <param name="passages">Passage Bytes</param>
        /// <param name="priorities">Priority Bytes</param>
        /// <param name="flags">Flag Bytes</param>
        /// <param name="tags">Tag Bytes</param>
        /// <returns></returns>
        public static Tileset Generate(ObjectId id, String name, String assetName, List<String> autotileNames, List<Boolean> autotileAnimation,
            Byte[] passages, Byte[] priorities, Byte[] flags, Byte[] tags, Int32 tiles, UInt32 version)
        {
            Tileset result = new Tileset();
            result.Id = id;
            result.Name = name;
            result.AssetName = assetName;
            result.AutotileAssetNames = autotileNames;
            result.AutotileAnimationFlags = autotileAnimation;
            result.Passages = passages;
            result.Priorities = priorities;
            result.Flags = flags;
            result.Tags = tags;
            result.Tiles = tiles;
            result.Version = version;

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="count"></param>
        public void Resize(Int32 count)
        {
            Byte[] temp1 = Passages;
            Byte[] temp2 = Priorities;
            Byte[] temp3 = Flags;
            Byte[] temp4 = Tags;

            Array.Resize(ref temp1, count + 384);
            Array.Resize(ref temp2, count + 384);
            Array.Resize(ref temp3, count + 384);
            Array.Resize(ref temp4, count + 384);

            Passages = temp1;
            Priorities = temp2;
            Flags = temp3;
            Tags = temp4;

            Tiles = count;
        }

        /// <summary>
        /// Compute Hashcode
        /// </summary>
        /// <returns>Hashcode</returns>
        public override Int32 GetHashCode()
        {
            Int32 code = 0;

            if (this.AutotileAssetNames != null)
                foreach (String item in this.AutotileAssetNames)
                    code = (code * 3 + item.Length) ^ item.GetHashCode();

            if (this.Passages != null)
                foreach (Byte item in this.Passages)
                    code = (code * 7 + item) ^ item.GetHashCode();

            if (this.Priorities != null)
                foreach (Byte item in this.Priorities)
                    code = (code * 19 + item) ^ item.GetHashCode();

            if (this.Flags != null)
                foreach (Byte item in this.Flags)
                    code = (code * 31 + item) ^ item.GetHashCode();

            if (this.Tags != null)
                foreach (Byte item in this.Tags)
                    code = (code * 61 + item) ^ item.GetHashCode();

            if (this.AssetName != null)
                code = this.AssetName.GetHashCode() ^ (code * 127);

            code = (Int32)(this.Version * 255) ^ code;

            return code;
        }

        /// <summary>
        /// Read the tileset from buffer
        /// </summary>
        /// <param name="netBuffer"></param>
        /// <returns></returns>
        public static Tileset Read(NetBuffer buffer)
        {
            Tileset result = new Tileset();
            result.Id = new ObjectId(buffer.ReadBytes(12));
            result.Name = buffer.ReadString();
            result.AssetName = buffer.ReadString();

            var count = buffer.ReadByte();
            result.AutotileAssetNames = new List<String>(count);
            result.AutotileAnimationFlags = new List<Boolean>(count);

            for (int i = 0; i < count; i++)
                result.AutotileAssetNames.Add(buffer.ReadString());
            for (int i = 0; i < count; i++)
                result.AutotileAnimationFlags.Add(buffer.ReadBoolean());

            result.Tiles = buffer.ReadInt32();
            result.Passages = new Byte[result.Tiles];
            result.Priorities = new Byte[result.Tiles];
            result.Flags = new Byte[result.Tiles];
            result.Tags = new Byte[result.Tiles];
            for (Int32 i = 0; i < result.Tiles; i++)
            {
                result.Passages[i] = buffer.ReadByte();
                result.Priorities[i] = buffer.ReadByte();
                result.Flags[i] = buffer.ReadByte();
                result.Tags[i] = buffer.ReadByte();
            }

            return result;
        }
        
        /// <summary>
        /// Write the tileset to buffer
        /// </summary>
        /// <param name="netBuffer"></param>
        public void Write(NetBuffer buffer)
        {
            buffer.Write(this.Id.ToByteArray());
            buffer.Write(this.Name ?? String.Empty);
            buffer.Write(this.AssetName ?? String.Empty);
            buffer.Write((Byte)this.AutotileAssetNames.Count);

            foreach (var name in this.AutotileAssetNames)
                buffer.Write(name ?? String.Empty);
            foreach (var flag in this.AutotileAnimationFlags)
                buffer.Write(flag);

            buffer.Write(this.Tiles);
            for (Int32 i = 0; i < this.Tiles; i++)
            {
                buffer.Write(this.Passages[i]);
                buffer.Write(this.Priorities[i]);
                buffer.Write(this.Flags[i]);
                buffer.Write(this.Tags[i]);
            }
        }
    }
}
