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
using Lidgren.Network;

namespace ResourceService
{
    /// <summary>
    /// Assets are stored files in the GridFs section of the Database. They can be retrieved by quering for
    /// the unique filename, which is composed of an MD5 and SHA1 hash based on the contents of the file.
    /// Once you get the asset object with the file information, the chunks can be retrieved individually
    /// to compose the binary file on the remote location. Files are automatically replaced if updated.
    /// </summary>
    internal class Asset
    {
        /// <summary>
        /// File Id
        /// </summary>
        public ObjectId Id
        {
            get;
            protected set;
        }

        /// <summary>
        /// File Name
        /// </summary>
        public String FileName
        {
            get;
            protected set;
        }

        /// <summary>
        /// Server MD5
        /// </summary>
        public String ServerMD5
        {
            get;
            protected set;
        }

        /// <summary>
        /// Gets an asset by id
        /// </summary>
        /// <param name="assetId">Asset ID</param>
        /// <returns></returns>
        internal static Asset GetFileById(ObjectId assetId)
        {
            Asset result = new Asset();

            // Access Grid FS and find the file
            MongoGridFS gridFs = new MongoGridFS(ServiceProtocol.ServiceClient.Database, 
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
        /// Gets chunks (give id)
        /// </summary>
        /// <param name="assetId"></param>
        /// <param name="chunkSize"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        internal static ObjectId[] GetChunksById(ObjectId assetId, out Int32 chunkSize, out Int32 length)
        {
            ObjectId[] result = null;
            chunkSize = 0;
            length = 0;

            // Access Grid FS and get the file
            MongoGridFS gridFs = new MongoGridFS(ServiceProtocol.ServiceClient.Database, 
                new MongoGridFSSettings(MongoGridFSSettings.Defaults.ChunkSize, "Assets", SafeMode.True));
            MongoGridFSFileInfo file = gridFs.FindOneById(assetId);

            if (file == null || !file.Exists)
                return null;

            chunkSize = file.ChunkSize;
            length = (int)file.Length;

            var numberOfChunks = (length + chunkSize - 1) / chunkSize;
            result = new ObjectId[numberOfChunks];

            // Get those chunks
            for (int n = 0; n < numberOfChunks; n++)
            {
                var query = Query.And(
                    Query.EQ("files_id", file.Id), 
                    Query.EQ("n", n)
                );

                var chunk = gridFs.Chunks.FindOne(query);
                if (chunk == null)
                {
                    String errorMessage = String.Format("Chunk {0} missing for GridFS file '{1}'.", n, file.Name);
                    throw new MongoGridFSException(errorMessage);
                }

                result[n] = chunk["_id"].AsObjectId;
            }

            return result;
        }

        /// <summary>
        /// Gets a chunk (give chunk id)
        /// </summary>
        /// <param name="chunkId"></param>
        /// <returns></returns>
        internal static BsonBinaryData GetChunkById(ObjectId chunkId)
        {
            MongoGridFS gridFs = new MongoGridFS(ServiceProtocol.ServiceClient.Database, 
                new MongoGridFSSettings(MongoGridFSSettings.Defaults.ChunkSize, "Assets", SafeMode.True));
            var chunk = gridFs.Chunks.FindOne(Query.EQ("_id", chunkId));

            if (chunk == null)
                return null;

            return chunk["data"].AsBsonBinaryData;
        }

        /// <summary>
        /// Get file by filename
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        internal static Asset GetFile(String fileName)
        {
            var result = new Asset();

            MongoGridFS gridFs = new MongoGridFS(ServiceProtocol.ServiceClient.Database, 
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
        /// <param name="fileName">local file path</param>
        /// <param name="previousName">previous generated name</param>
        internal void SaveFile(String fileName, String previousName = null)
        {
            MongoGridFS gridFs = new MongoGridFS(ServiceProtocol.ServiceClient.Database,
                new MongoGridFSSettings(MongoGridFSSettings.Defaults.ChunkSize, "Assets", SafeMode.True));

            String md5Local = String.Empty, sha1 = String.Empty;

            try
            {
                // Generates the filename
                using (FileStream file = new FileStream(fileName, FileMode.Open))
                {
                    md5Local = FileMD5(gridFs.Settings, file);
                    file.Position = 0;
                    sha1 = FileSha1(gridFs.Settings, file);
                }

                this.ServerMD5 = md5Local;
                this.FileName = String.Format("{0}.{1}", md5Local, sha1);
            }
            catch (IOException)
            {
                System.Threading.Thread.Sleep(500);
                SaveFile(fileName, previousName);
                return;
            }

            // Find old file
            MongoGridFSFileInfo updateFile = gridFs.FindOne(previousName ?? String.Empty);

            if (updateFile == null) // New file!
            {
                // Filename exists
                MongoGridFSFileInfo matchedFile = gridFs.FindOne(
                    Query.And(
                        Query.EQ("filename", FileName), 
                        Query.NE("md5", md5Local)
                    )
                );

                // Impossible collision occured. There is a file with the same filename
                // but a different md5 value, which is impossible, because the filename
                // has both md5 and sha1.
                if (matchedFile != null)
                    return;

                // Create new file
                ObjectId id = gridFs.Upload(fileName, FileName).Id.AsObjectId;
                MongoGridFSFileInfo fileInfo = gridFs.FindOneById(id);

                return;
            }

            // Already exists in the database, no need to change
            if (updateFile.Name == FileName && updateFile.MD5 == md5Local)
                return;

            // We changed the name of the file, like with a different filename format. So 
            // we need to update that name now.
            if (updateFile.MD5 == md5Local)
            {
                // only name changed
                MongoGridFSFileInfo matchedFile = gridFs.FindOne(
                    Query.And(
                        Query.EQ("filename", FileName), 
                        Query.NE("md5", md5Local))
                    );

                // Impossible collision occured
                if (matchedFile != null)
                    return;

                // Save file under new name
                gridFs.MoveTo(previousName, FileName);
                return;
            }

            // The names match, but the MD5 does not
            if (updateFile.Name == FileName)
            {
                // Delete previous version
                gridFs.Delete(FileName);
                MongoGridFSFileInfo newFile = gridFs.Upload(fileName, FileName);
                return;
            }

            // Find this file in the database
            MongoGridFSFileInfo updatedPeekFile = gridFs.FindOne(
                Query.Or(
                    Query.EQ("filename", FileName), 
                    Query.EQ("md5", md5Local)
                    )
            );

            // Already exists in the database
            if (updatedPeekFile != null)
                return;

            // Delete previous version
            gridFs.Delete(updateFile.Name);
            MongoGridFSFileInfo newPeekFile = gridFs.Upload(fileName, FileName);
            return;
        }

        /// <summary>
        /// Downloads the current asset
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
                        md5Local = FileHash(MongoGridFSSettings.Defaults, file);
                    }

                    Asset remoteCopy = GetFile(this.FileName);

                    if (remoteCopy == null)
                        return; // inner error

                    if (md5Local == remoteCopy.ServerMD5)
                        return;
                }

                MongoGridFS gridFs = new MongoGridFS(ServiceProtocol.ServiceClient.Database, 
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
        /// Equality comparision
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override Boolean Equals(Object other)
        {
            if (other is Asset)
            {
                Asset otherAsset = (Asset)other;

                if (otherAsset.Id != ObjectId.Empty && this.Id != ObjectId.Empty &&
                    otherAsset.FileName != String.Empty && this.FileName != String.Empty)
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
        /// Gets the hashcode
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return (this.Id.GetHashCode() * 63) ^ this.FileName.GetHashCode();
        }

        /// <summary>
        /// Gets MD5 value from stream
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static String FileMD5(Stream stream)
        {
            return FileHash(MongoGridFSSettings.Defaults, stream);
        }

        /// <summary>
        /// Gets SHA1 value from stream
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static String FileSha1(Stream stream)
        {
            return FileHash(MongoGridFSSettings.Defaults, stream, SHA1.Create());
        }

        /// <summary>
        /// Gets MD5 value from stream
        /// </summary>
        /// <param name="gridFSSettings"></param>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static String FileMD5(MongoGridFSSettings gridFSSettings, Stream stream)
        {
            return FileHash(gridFSSettings, stream);
        }

        /// <summary>
        ///  Gets SHA1 value from stream
        /// </summary>
        /// <param name="gridFSSettings"></param>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static String FileSha1(MongoGridFSSettings gridFSSettings, Stream stream)
        {
            return FileHash(gridFSSettings, stream, SHA1.Create());
        }

        /// <summary>
        /// Calculates the md5/sha1 value
        /// </summary>
        /// <param name="gridFs">Settings</param>
        /// <param name="stream">File Stream</param>
        /// <returns></returns>
        public static String FileHash(MongoGridFSSettings gridFSSettings, Stream stream, HashAlgorithm algorithm = null)
        {
            var chunkSize = gridFSSettings.ChunkSize;
            var buffer = new Byte[chunkSize];
            var length = 0;

            if (algorithm == null)
                algorithm = MD5.Create();

            using (var md5Algorithm = algorithm)
            {
                while(true) { // for (int n = 0; true; n++)
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
        /// Gets server MD5 value
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
        internal void Pack(NetBuffer msg)
        {
            msg.Write(this.Id.ToByteArray());
            msg.Write(this.FileName);
            msg.Write(this.ServerMD5);
        }
    }
}