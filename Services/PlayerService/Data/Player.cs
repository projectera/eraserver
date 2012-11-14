using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using Lidgren.Network;
using System.Net.Mail;
using Lidgren.Network.Authentication;
using Lidgren.Network.Lobby;

namespace PlayerService.Data
{
    internal static class Player
    {
        /// <summary>
        /// Generates a player
        /// </summary>
        /// <remarks>To store the newly created player call Put() on the player object</remarks>
        /// <param name="username">username</param>
        /// <param name="password">password</param>
        /// <param name="mailAdress">mailadress</param>
        /// <returns>Player object</returns>
        internal static PlayerProtocol.Player Generate(String username, String password, String mailAdress)
        {
            PlayerProtocol.Player result = Player.GetBlocking(username) ?? new PlayerProtocol.Player();
            if (result.Id.Equals(ObjectId.Empty) == false)
                throw new Exception("User already exists");

            Byte[] verifier, salt = null;
            verifier = Handshake.PasswordVerifier(result.Username.ToLower(), password, NetLobby.KeySize, out salt).ToByteArray(); 

            return PlayerProtocol.Player.Generate(
                ObjectId.GenerateNewId(), 
                0, 
                username, 
                verifier, 
                salt, 
                mailAdress, 
                PlayerProtocol.PermissionGroup.Registered);

            // Generate Salt and Verifier TODO
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="username"></param>
        /// <param name="verifier"></param>
        /// <param name="salt"></param>
        /// <returns></returns>
        internal static PlayerProtocol.Player Generate(String username, Byte[] verifier, Byte[] salt)
        {
            PlayerProtocol.Player result = Player.GetBlocking(username) ?? new PlayerProtocol.Player();
            if (result.Id.Equals(ObjectId.Empty) == false)
                throw new Exception("User already exists");

            return PlayerProtocol.Player.Generate(
                ObjectId.GenerateNewId(),
                username,
                verifier,
                salt,
                PlayerProtocol.PermissionGroup.None
                );
        }

        /// <summary>
        /// Bans user with reason
        /// </summary>
        /// <param name="reason"></param>
        /// <returns>Update task</returns>
        internal static Task<Boolean> Ban(ref PlayerProtocol.Player player, String reason)
        {
            // Update locally
            if (String.IsNullOrEmpty(reason))
                reason = "No reason specified";

            // Update remotely
            ObjectId updateId = player.Id;

            // New player
            player.Ban(reason);

            return Task.Factory.StartNew(() =>
            {
                // Second, try updating the username to the new username
                SafeModeResult sfr = GetCollection().Update(
                    Query.EQ("_id", updateId), Update.Set("BannedReason", reason), UpdateFlags.None, SafeMode.True);
                // Return succession flag
                return sfr.UpdatedExisting;
            });
        }

        /// <summary>
        /// Changes Username of player
        /// </summary>
        /// <remarks>Even though locally changes always succeeded, you should inspect the Task.Result value to see if the update succeeded remotely</remarks>
        /// <param name="username">new username</param>
        /// <returns>Update Task</returns>
        internal static Task<Boolean> ChangeUsername(ref PlayerProtocol.Player player, String username)
        {
            // Copy old value and update (local) value
            String oldname = player.Username;

            // New player
            player = PlayerProtocol.Player.Generate(player.Id, player.ForumId, username, player.Verifier, player.Salt, player.EmailAddress, player.PermissionGroup);

            // Start updating
            return Task.Factory.StartNew(() =>
            {
                // First, find a player with the new username
                var otherplayer = GetCollection().FindOne(Query.EQ("Username", username));
                // If we found one, we can not change to this username
                if (!otherplayer.Id.Equals(ObjectId.Empty))
                    return false;
                // Second, try updating the username to the new username
                SafeModeResult sfr = GetCollection().Update(Query.EQ("Username", oldname), Update.Set("Username", username), SafeMode.True);
                // Return succession flag
                return sfr.UpdatedExisting;
            });
        }

        /// <summary>
        /// Changes Email of Player
        /// </summary>
        /// <param name="email">new Email</param>
        /// <returns></returns>
        internal static Task<Boolean> ChangeEmail(ref PlayerProtocol.Player player, String email)
        {
            // Copy old value and update (local) value
            String oldmail = player.EmailAddress;
            Int32 validationCode = NetRandom.Instance.NextInt();
            ObjectId updateId = player.Id;

            // New player
            player = PlayerProtocol.Player.Generate(player.Id, player.ForumId, player.Username, player.Verifier, player.Salt, email, player.PermissionGroup);
            String mail = player.EmailAddress;
            String username = player.Username;

            // Start updating
            return Task.Factory.StartNew(() =>
            {
                //player.ValidationMailCode = NetRandom.Instance.NextInt();

                FindAndModifyResult fmr = GetCollection().FindAndModify(
                    Query.And(
                        Query.EQ("_id", updateId),
                        Query.EQ("EmailAddress", oldmail)),
                    SortBy.Null,
                    Update.Set("EmailAddress", email).Set("ValidationMailCode", validationCode),
                    true, false);
                return fmr.Ok;
            }).ContinueWith<Boolean>((task) =>
            {
                try
                {
                    MailMessage message = new MailMessage(new MailAddress("derk-jan@projectera.org", "Derk-Jan [ProjectERA]"),
                        new MailAddress(mail, username + " [ProjectERA]"));
                    message.IsBodyHtml = true;
                    message.Subject = "Email address linked to ProjectERA account";

                    // HTML Body (remove HTML tags for plain text).
                    message.Body = "<HTML><BODY><B>Hello World! " + validationCode + "</B></BODY></HTML>";

                    SmtpClient smtp = new SmtpClient("mail.direct-adsl.nl");
                    //smtp.SendCompleted += new SendCompletedEventHandler(smtp_SendCompleted);
                    smtp.SendAsync(message, updateId);
                }
                catch (Exception)
                {

                }

                return task.Result;
            });

            // TODO: Sent Confirmation Email
        }

        /// <summary>
        /// Links Forum account with Player
        /// </summary>
        /// <param name="?"></param>
        /// <returns></returns>
        internal static Task<Boolean> LinkAccount(ref PlayerProtocol.Player player, UInt16 forumId)
        {
            // Copy old value and update (local) value
            ObjectId updateId = player.Id;
            Int32 validationCode = NetRandom.Instance.NextInt();

            // new player
            player = PlayerProtocol.Player.Generate(player.Id, forumId, player.Username, player.Verifier, player.Salt, player.EmailAddress, player.PermissionGroup);
            String mail = player.EmailAddress;
            String username = player.Username;

            // Start updating
            return Task.Factory.StartNew(() =>
            {
                FindAndModifyResult fmr = GetCollection().FindAndModify(
                    Query.EQ("_id", updateId),
                    SortBy.Null,
                    Update.Set("ForumId", forumId).Set("ValidationLinkCode", validationCode),
                    true, false);
                return fmr.Ok;
            }).ContinueWith<Boolean>((task) =>
            {
                try
                {
                    MailMessage message = new MailMessage(new MailAddress("derk-jan@projectera.org", "Derk-Jan [ProjectERA]"),
                        new MailAddress(mail, username + " [ProjectERA]"));
                    message.IsBodyHtml = true;
                    message.Subject = "Forum account linked to ProjectERA account";

                    // HTML Body (remove HTML tags for plain text).
                    message.Body = "<HTML><BODY><B>Hello World! " + validationCode + "</B></BODY></HTML>";

                    SmtpClient smtp = new SmtpClient("mail.direct-adsl.nl");
                    //smtp.SendCompleted += new SendCompletedEventHandler(smtp_SendCompleted);
                    smtp.SendAsync(message, updateId);
                }
                catch (Exception)
                {

                }

                return task.Result;
            });

        }

        /// <summary>
        /// Elevates the player to another permissionGroup
        /// </summary>
        /// <param name="permissionGroup">new permissiongroup</param>
        /// <returns>Update task</returns>
        internal static Task<Boolean> Elevate(ref PlayerProtocol.Player player, PlayerProtocol.PermissionGroup permissionGroup)
        {
            // New player
            player = PlayerProtocol.Player.Generate(player.Id, player.ForumId, player.Username, player.Verifier, player.Salt, player.EmailAddress, permissionGroup);
            
            // Update remotely
            ObjectId updateId = player.Id;

            return Task<Boolean>.Factory.StartNew(() =>
            {
                // Second, try updating the username to the new username
                SafeModeResult sfr = GetCollection().Update(
                    Query.EQ("_id", updateId), Update.Set("PermissionGroup", permissionGroup), UpdateFlags.None, SafeMode.True);
                // Return succession flag
                return sfr.UpdatedExisting;

            });
        }

        /// <summary>
        /// Gets a player from the db
        /// </summary>
        /// <param name="id">id of player to get</param>
        /// <returns></returns>
        internal static Task<PlayerProtocol.Player> Get(ObjectId id)
        {
            return Task.Factory.StartNew(() => { return GetBlocking(id); });
        }

        /// <summary>
        /// Gets a player from the db and recursive data
        /// </summary>
        /// <param name="id">id of player to get</param>
        /// <returns></returns>
        internal static Task<PlayerProtocol.Player> GetRecursive(ObjectId id)
        {
            return Task.Factory.StartNew(() => { return GetBlocking(id); }).ContinueWith<PlayerProtocol.Player>((task) =>
            {
                task.Result.Dialogues = new Dictionary<ObjectId, PlayerProtocol.Dialogue>();
                PlayerProtocol.Dialogue[] retrieved = Dialogue.GetBlockingFor(task.Result.Id);
                foreach (var dialogue in retrieved)
                    task.Result.Dialogues.Add(dialogue.Id, dialogue);

                return task.Result;
            });
        }

        /// <summary>
        /// Gets a player from the db
        /// </summary>
        /// <param name="username">username of player to get</param>
        /// <returns></returns>
        internal static Task<PlayerProtocol.Player> Get(String username)
        {
            return Task.Factory.StartNew(() => { return GetBlocking(username); });
        }

        /// <summary>
        /// Gets a player from the db,  blocks while retrieving
        /// </summary>
        /// <param name="id">id of player to get</param>
        /// <returns></returns>
        internal static PlayerProtocol.Player GetBlocking(ObjectId id)
        {
            return GetCollection().FindOneById(id) as PlayerProtocol.Player;
        }

        /// <summary>
        /// Gets a player from the db, blocks while retrieving
        /// </summary>
        /// <param name="username">username of player to get</param>
        /// <returns></returns>
        internal static PlayerProtocol.Player GetBlocking(String username)
        {
            return GetCollection().FindOneAs<PlayerProtocol.Player>(
                Query.Matches("Username", new BsonRegularExpression("^(?i)" + username + "$"))) as PlayerProtocol.Player;
        }

        /// <summary>
        /// Gets the players collection
        /// </summary>
        /// <returns></returns>
        internal static MongoCollection<PlayerProtocol.Player> GetCollection()
        {
            return ServiceProtocol.ServiceClient.Database.GetCollection<PlayerProtocol.Player>("Players");
        }

        /// <summary>
        /// Puts a player to the db
        /// </summary>
        internal static void Put(PlayerProtocol.Player player)
        {
            Put(player, SafeMode.False);
        }

        /// <summary>
        /// Puts a player to the db
        /// <param name="safemode">Sets the safemode on this query</param>
        /// </summary>
        internal static SafeModeResult Put(PlayerProtocol.Player player, SafeMode safemode)
        {
            return GetCollection().Save<PlayerProtocol.Player>(player, safemode);
        }
    }
}
