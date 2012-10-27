using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using MongoDB.Driver.GridFS;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using System.IO;
using System.Security.Cryptography;

namespace ResourceService
{
    public class Asset
    {
        /// <summary>
        /// 
        /// </summary>
        public ObjectId Id
        {
            get;
            protected set;
        }

        /// <summary>
        /// 
        /// </summary>
        public String FileName
        {
            get;
            protected set;
        }

        /// <summary>
        /// 
        /// </summary>
        public String ServerMD5
        {
            get;
            protected set;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="assetId"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        internal static Asset GetFile(ObjectId assetId, out Asset result)
        {
            result = new Asset();

            MongoGridFS gridFs = new MongoGridFS(Program.Database, 
                new MongoGridFSSettings(MongoGridFSSettings.Defaults.ChunkSize, "Assets", SafeMode.True));
            MongoGridFSFileInfo file = gridFs.FindOneById(assetId);

            if (file == null || !file.Exists)
                return null;
    
            result.Id = file.Id.AsObjectId;
            result.FileName = file.Name;
            result.ServerMD5 = file.MD5;

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="assetId"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        internal static ObjectId[] GetChunks(ObjectId assetId, out Int32 chunkSize, out Int32 length)
        {
            ObjectId[] result = null;
            chunkSize = 0;
            length = 0;

            MongoGridFS gridFs = new MongoGridFS(Program.Database, 
                new MongoGridFSSettings(MongoGridFSSettings.Defaults.ChunkSize, "Assets", SafeMode.True));
            MongoGridFSFileInfo file = gridFs.FindOneById(assetId);

            if (file == null || !file.Exists)
                return null;

            chunkSize = file.ChunkSize;
            length = (int)file.Length;

            var numberOfChunks = (length + chunkSize - 1) / chunkSize;
            result = new ObjectId[numberOfChunks];
            for (int n = 0; n < numberOfChunks; n++)
            {
                var query = Query.And(
                    Query.EQ("files_id", file.Id), 
                    Query.EQ("n", n)
                );

                var chunk = gridFs.Chunks.FindOne(query);

                if (chunk == null)
                {
                    //String errorMessage = String.Format("Chunk {0} missing for GridFS file '{1}'.", n, file.Name);
                    //throw new MongoGridFSException(errorMessage);

                    return null;
                }

                result[n] = chunk["_id"].AsObjectId;
            }

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="chunkId"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        internal static BsonBinaryData GetChunk(ObjectId chunkId)
        {
            MongoGridFS gridFs = new MongoGridFS(Program.Database, 
                new MongoGridFSSettings(MongoGridFSSettings.Defaults.ChunkSize, "Assets", SafeMode.True));
            var chunk = gridFs.Chunks.FindOne(Query.EQ("_id", chunkId));

            if (chunk == null)
                return null;

            return chunk["data"].AsBsonBinaryData;
        }

        /// <summary>
        /// Get file
        /// </summary>
        /// <param name="type"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        internal static Asset GetFile(String fileName)
        {
            var result = new Asset();

            MongoGridFS gridFs = new MongoGridFS(Program.Database, 
                new MongoGridFSSettings(MongoGridFSSettings.Defaults.ChunkSize, "Assets", SafeMode.True));
            MongoGridFSFileInfo file = gridFs.FindOne(fileName) ?? gridFs.FindOne(Query.EQ("aliases", fileName));

            if (file == null || !file.Exists)
                return null;

            result.Id = file.Id.AsObjectId;
            result.FileName = file.Name;
            result.ServerMD5 = file.MD5;

            return result;
        }

        /// <summary>
        /// Save file
        /// </summary>
        /// <param name="LocalImage">local file path</param>
        internal void SaveFile(String fileName, String previousName)
        {
            MongoGridFS gridFs = new MongoGridFS(Program.Database,
                new MongoGridFSSettings(MongoGridFSSettings.Defaults.ChunkSize, "Assets", SafeMode.True));

            //String name = FileName.EndsWith(".png") ? FileName.Remove(FileName.LastIndexOf('.')) : FileName;

            // Get MD5
            String md5Local = String.Empty;

            try
            {
                using (FileStream file = new FileStream(fileName, FileMode.Open))
                {
                    md5Local = FileMD5(gridFs.Settings, file);
                }
                this.ServerMD5 = md5Local;
            }
            catch (IOException)
            {
                System.Threading.Thread.Sleep(500);
                SaveFile(fileName, previousName);
                return;
            }

            // Find old file
            MongoGridFSFileInfo updateFile = gridFs.FindOne(previousName);

            if (updateFile == null) // Loaded file!
            {
                // Filename exists
                MongoGridFSFileInfo matchedFile = gridFs.FindOne(Query.And(Query.EQ("filename", FileName), Query.NE("md5", md5Local)));
                if (matchedFile != null)
                {
                    //MessageBox.Show("There already exists a file with that name but different graphic. Please edit <" + matchedFile.Name + "> instead!", "Name already exists", MessageBoxButtons.OK);
                    return;
                }

                // Create new file
                ObjectId id = gridFs.Upload(fileName, FileName).Id.AsObjectId;
                MongoGridFSFileInfo fileInfo = gridFs.FindOneById(id);

                return;
            }

            if (updateFile.Name == FileName && updateFile.MD5 == md5Local)
            {
                return;
            }

            if (updateFile.MD5 == md5Local)
            {
                // only name changed
                MongoGridFSFileInfo matchedFile = gridFs.FindOne(Query.And(Query.EQ("filename", FileName), Query.NE("md5", md5Local)));
                if (matchedFile != null)
                {
                    //MessageBox.Show("There already exists a file with that name but different graphic. Please edit <" + matchedFile.Name + "> instead!", "Name already exists", MessageBoxButtons.OK);
                    return;
                }

                gridFs.MoveTo(previousName, FileName);
                return;
            }

            if (updateFile.Name == FileName)
            {
                // Delete previous version
                gridFs.Delete(FileName);

                MongoGridFSFileInfo newFile = gridFs.Upload(fileName, FileName);
                return;
            }

            MongoGridFSFileInfo updatedPeekFile = gridFs.FindOne(Query.Or(Query.EQ("filename", FileName), Query.EQ("md5", md5Local)));
            if (updatedPeekFile != null)
            {
                //MessageBox.Show("There already exists a file with that name or that graphic. Please edit <" + updatedPeekFile.Name + "> instead!", "Graphic already exists", MessageBoxButtons.OK);
                return;
            }

            gridFs.Delete(updateFile.Name);

            MongoGridFSFileInfo newPeekFile = gridFs.Upload(fileName, FileName);
            return;
        }

        /// <summary>
        /// Downloads a file
        /// </summary>
        /// <param name="fileName">destination</param>
        public void Download(String fileName)
        {
            try
            {
                String dir = Path.GetDirectoryName(fileName);

                if (String.IsNullOrWhiteSpace(dir) == false)
                    Directory.CreateDirectory(dir);

                if (File.Exists(fileName))
                {
                    // Get MD5
                    String md5Local;
                    using (FileStream file = new FileStream(fileName, FileMode.Open))
                    {
                        md5Local = FileMD5(MongoGridFSSettings.Defaults, file);
                    }

                    Asset remoteCopy = GetFile(this.FileName);

                    if (remoteCopy == null)
                    {
                        return; // inner error
                    }

                    if (md5Local == remoteCopy.ServerMD5)
                    {
                        return;
                    }
                }

                MongoGridFS gridFs = new MongoGridFS(Program.Database, 
                    new MongoGridFSSettings(MongoGridFSSettings.Defaults.ChunkSize, "Assets", SafeMode.True));
                gridFs.Download(fileName, Query.EQ("_id", this.Id));
            }
            catch (IOException)
            {
                // File in use, update later
                return;
            }

            return;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override Boolean Equals(Object other)
        {
            if (other is Asset)
            {
                Asset otherAsset = (Asset)other;

                if (otherAsset.Id != ObjectId.Empty && this.Id != ObjectId.Empty &&
                    otherAsset.ServerMD5 != String.Empty && this.ServerMD5 != String.Empty)
                    return otherAsset.Id.Equals(this.Id);
            }
            else if (other is String)
            {
                String otherFilename = (String)other;

                Asset resultThere = Asset.GetFile(this.FileName);
                Asset resultHere = Asset.GetFile(otherFilename);

                if (resultThere == null || resultHere == null)
                    return false;

                return resultThere.Equals(resultHere);
            }

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return (this.Id.GetHashCode() * 63) ^ this.ServerMD5.GetHashCode();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static String FileMD5(Stream stream)
        {
            return FileMD5(MongoGridFSSettings.Defaults, stream);
        }

        /// <summary>
        /// Calculates the md5 value
        /// </summary>
        /// <param name="gridFs">Settings</param>
        /// <param name="stream">File Stream</param>
        /// <returns></returns>
        public static String FileMD5(MongoGridFSSettings gridFSSettings, Stream stream)
        {
            var chunkSize = gridFSSettings.ChunkSize;
            var buffer = new Byte[chunkSize];
            var length = 0;

            using (var md5Algorithm = MD5.Create())
            {
                for (int n = 0; true; n++)
                {
                    // might have to call Stream.Read several times to get a whole chunk
                    var bytesNeeded = chunkSize;
                    var bytesRead = 0;
                    while (bytesNeeded > 0)
                    {
                        var partialRead = stream.Read(buffer, bytesRead, bytesNeeded);
                        if (partialRead == 0)
                        {
                            break; // EOF may or may not have a partial chunk
                        }
                        bytesNeeded -= partialRead;
                        bytesRead += partialRead;
                    }
                    if (bytesRead == 0)
                    {
                        break; // EOF no partial chunk
                    }
                    length += bytesRead;

                    byte[] data = buffer;
                    if (bytesRead < chunkSize)
                    {
                        data = new byte[bytesRead];
                        Buffer.BlockCopy(buffer, 0, data, 0, bytesRead);
                    }

                    md5Algorithm.TransformBlock(data, 0, data.Length, null, 0);

                    if (bytesRead < chunkSize)
                    {
                        break; // EOF after partial chunk
                    }
                }

                md5Algorithm.TransformFinalBlock(new byte[0], 0, 0);
                return BsonUtils.ToHexString(md5Algorithm.Hash);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public String GetServerMD5()
        {
            Asset asset = GetFile(this.FileName);
            if (asset == null)
                return null;

            return asset.ServerMD5;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="getMessage"></param>
        internal void Pack(Lidgren.Network.NetOutgoingMessage msg)
        {
            msg.Write(this.Id.ToByteArray());
            msg.Write(this.FileName);
            msg.Write(this.ServerMD5);
        }
    }
}