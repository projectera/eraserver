using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System.Threading.Tasks;

namespace PlayerProtocol
{
    [Serializable, BsonIgnoreExtraElements(true)]
    public class Player
    {
        /// <summary>
        /// Player ID (readonly)
        /// </summary>
        [BsonId]
        public ObjectId 
            Id
        {
            get;
            protected set;
        }

        /// <summary>
        /// Forum ID
        /// </summary>
        public UInt16 ForumId
        {
            get;
            protected set;
        }
        
        /// <summary>
        /// Player Username
        /// </summary>
        /// <remarks>Can be changed through ChangeUsername()</remarks>
        [BsonRequired]
        public String Username
        {
            get;
            protected set;
        }
        
        /// <summary>
        /// Player Logon Verifier
        /// </summary>
        [BsonRequired]
        public Byte[] Verifier;

        /// <summary>
        /// Player Logon Salt
        /// </summary>
        [BsonRequired]
        public Byte[] Salt;

        /// <summary>
        /// Players Email Adress
        /// <remarks>System.Net.MailAddress is not Serializble</remarks>
        /// </summary>
        [BsonRequired]
        public String EmailAddress
        {
            get;
            protected set;
        }

        /// <summary>
        /// Players Permission Group
        /// </summary>
        public PermissionGroup PermissionGroup
        {
            get;
            protected set;
        }

        /// <summary>
        /// CurrentInteractable
        /// </summary>
        [BsonIgnore]
        public ObjectId ActiveInteractable
        {
            get;
            protected set;
        }

        /// <summary>
        /// Banned Reason (not banned if empty)
        /// </summary>
        [BsonIgnoreIfNull]
        public String BannedReason
        {
            get;
            protected set;
        }

        /// <summary>
        /// Banned Flag (not stored)
        /// </summary>
        [BsonIgnore]
        public Boolean IsBanned
        {
            get { return !String.IsNullOrEmpty(this.BannedReason); }
        }

        /// <summary>
        /// Friends
        /// </summary>
        [BsonRequired]
        public HashSet<Friend> Friends
        {
            get;
            protected set;
        }

        /// <summary>
        /// Registration Time
        /// </summary>
        [BsonIgnore]
        public DateTime RegistrationDate
        {
            get { return this.Id.CreationTime; }
        }

        /// <summary>
        /// 
        /// </summary>
        public Player()
        {
            this.Id = ObjectId.Empty;
            this.ActiveInteractable = ObjectId.Empty;
            this.Friends = new HashSet<Friend>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="forumid"></param>
        /// <param name="username"></param>
        /// <param name="verifier"></param>
        /// <param name="salt"></param>
        /// <param name="email"></param>
        /// <param name="permissions"></param>
        /// <returns></returns>
        public static Player Generate(ObjectId id, UInt16 forumid, String username, Byte[] verifier, Byte[] salt, String email, PermissionGroup permissions)
        {
            var result = new Player();
            result.Id = id;
            result.ForumId = forumid;
            result.Username = username;
            result.Verifier = verifier;
            result.Salt = salt;
            result.EmailAddress = email;
            result.PermissionGroup = permissions;
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="username"></param>
        /// <param name="verifier"></param>
        /// <param name="salt"></param>
        /// <param name="permissions"></param>
        /// <returns></returns>
        public static Player Generate(ObjectId id, String username, Byte[] verifier, Byte[] salt, PermissionGroup permissions)
        {
            return Player.Generate(id, 0, username, verifier, salt, String.Empty, permissions);
        }

        /// <summary>
        /// Clears for pool
        /// </summary>
        public void Clear()
        {
            this.Id = ObjectId.Empty;
            this.ForumId = 0;
            this.Username = String.Empty;
            this.EmailAddress = String.Empty;

            this.PermissionGroup = 0;
            this.ActiveInteractable = ObjectId.Empty;

            this.BannedReason = null;
            this.Verifier = null;
            this.Salt = null;
        }
    }
}
