using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using MongoDB.Driver;
using System.Threading.Tasks;
using ERA.Protocols.ServiceProtocol;

namespace ERA.Services.PlayerService.Data
{
    class Dialogue
    {
        /// <summary>
        /// Generates a new Dialogue
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="receiver"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        internal static ERA.Protocols.PlayerProtocol.Dialogue Generate(ObjectId sender, ObjectId receiver, String message)
        {
            return ERA.Protocols.PlayerProtocol.Dialogue.Generate(sender, receiver, message);
        }

        /// <summary>
        /// Generates a new Dialogue
        /// </summary>
        /// <param name="message"></param>
        /// <param name="participants"></param>
        /// <returns></returns>
        internal static ERA.Protocols.PlayerProtocol.Dialogue Generate(ERA.Protocols.PlayerProtocol.DialogueMessage message, HashSet<ObjectId> participants)
        {
            return ERA.Protocols.PlayerProtocol.Dialogue.Generate(message, participants);
        }

        /// <summary>
        /// Adds a message to the conversation
        /// </summary>
        /// <param name="message"></param>
        internal static void AddMessage(ERA.Protocols.PlayerProtocol.Dialogue dialogue, ERA.Protocols.PlayerProtocol.DialogueMessage message)
        {
            var result = dialogue.GetMessages();
            // Page limit reached? Page count can only increase so no need to lock
            if (result.Count >= ERA.Protocols.PlayerProtocol.Dialogue.DialogueSize)
            {
                if (dialogue.FollowUp == null)
                {
                    ArchiveDialogue(dialogue, message);
                    return;
                }

                // Get FollowUp and append
                Dialogue.Get(dialogue.FollowUp ?? ObjectId.Empty).
                    ContinueWith(r =>
                    {
                        if (r != null) { r.Result.AddMessage(message); }
                    });
                return;
            }

            dialogue.AddMessage(message);
            result.Add(message);

            // Adds message
            GetCollection().Update(Query.EQ("_id", dialogue.Id), 
                Update.AddToSet("Messages", message.ToBsonDocument<ERA.Protocols.PlayerProtocol.DialogueMessage>()));

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        internal static void AddMessage(ERA.Protocols.PlayerProtocol.Dialogue dialogue, ObjectId sender, String message)
        {
            AddMessage(dialogue, ERA.Protocols.PlayerProtocol.DialogueMessage.Generate(sender, message));
        }

        /// <summary>
        /// Adds a participant to the conversation
        /// </summary>
        /// <param name="id"></param>
        internal static void AddParticipant(ERA.Protocols.PlayerProtocol.Dialogue dialogue, ObjectId id)
        {
            var participants = dialogue.GetParticipants();
            var got = (GetCollection().
                FindAndModify(
                    Query.EQ("_id", dialogue.Id),
                    SortBy.Null, Update.AddToSet("Participants", id), true).
                    GetModifiedDocumentAs<ERA.Protocols.PlayerProtocol.Dialogue>()
                ).GetParticipants();

            var diff = got.Except(participants);
            foreach (var participant in diff)
                dialogue.AddParticipant(participant);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        internal static void ArchiveDialogue(ERA.Protocols.PlayerProtocol.Dialogue dialogue, ERA.Protocols.PlayerProtocol.DialogueMessage message)
        {
            var replacement = ERA.Protocols.PlayerProtocol.Dialogue.Generate(message, dialogue.GetParticipants());
            Dialogue.Put(replacement);

            FindAndModifyResult atomicResult = Dialogue.GetCollection().FindAndModify(
                Query.And(Query.EQ("_id", dialogue.Id), Query.EQ("FollowUp", BsonNull.Value)),
                SortBy.Null, Update.Set("FollowUp", replacement.Id));

            if (!atomicResult.Ok)
            {
                Dialogue.GetCollection().Remove(Query.EQ("_id", replacement.Id));
                Dialogue.Get(dialogue.Id).
                    ContinueWith(result =>
                    {
                        dialogue.FollowUp = result.Result.FollowUp;
                        dialogue.AddMessage(message);
                    });
            }
            else
            {
                dialogue.FollowUp = replacement.Id;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dialogueMessageId"></param>
        /// <param name="readerId"></param>
        /// <returns></returns>
        internal static void MarkAsRead(ERA.Protocols.PlayerProtocol.Dialogue dialogue, ObjectId dialogueMessageId, ObjectId readerId, Boolean asRead = true)
        {
            foreach (ERA.Protocols.PlayerProtocol.DialogueMessage msg in dialogue.GetMessages())
            {

                if (asRead)
                {
                    msg.MarkAsRead(readerId);
                }
                else
                {
                    msg.MarkAsUnread(readerId);
                }

                if (msg.Id == dialogueMessageId)
                    break;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dialogueMessageId"></param>
        /// <param name="readerId"></param>
        internal static void MarkAsUnread(ERA.Protocols.PlayerProtocol.Dialogue dialogue, ObjectId dialogueMessageId, ObjectId readerId)
        {
            MarkAsRead(dialogue, dialogueMessageId, readerId, false);
        }

        /// <summary>
        /// 
        /// </summary>
        public static void Put(ERA.Protocols.PlayerProtocol.Dialogue dialogue)
        {
            Put(dialogue, SafeMode.False);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sm"></param>
        /// <returns></returns>
        public static SafeModeResult Put(ERA.Protocols.PlayerProtocol.Dialogue dialogue, SafeMode sm)
        {
            return GetCollection().Save(dialogue, sm);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static Task<ERA.Protocols.PlayerProtocol.Dialogue> Get(ObjectId id)
        {
            return Task<ERA.Protocols.PlayerProtocol.Dialogue>.Factory.StartNew(() => { return GetBlocking(id); });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static Task<ERA.Protocols.PlayerProtocol.Dialogue[]> GetFor(ObjectId id)
        {
            return Task<ERA.Protocols.PlayerProtocol.Dialogue[]>.Factory.StartNew(() => { return GetBlockingFor(id); });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static ERA.Protocols.PlayerProtocol.Dialogue GetBlocking(ObjectId id)
        {
            ERA.Protocols.PlayerProtocol.Dialogue result = GetCollection().FindOneById(id);

            foreach (ERA.Protocols.PlayerProtocol.DialogueMessage message in result.GetMessages())
                message.Parent = result;

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static ERA.Protocols.PlayerProtocol.Dialogue[] GetBlockingFor(ObjectId id, Boolean archived = false, Boolean unread = true)
        {
            IMongoQuery query = Query.EQ("Participants", id);

            if (!archived)
            {
                // Only find those with no followups
                IMongoQuery innerQuery = Query.EQ("FollowUp", BsonNull.Value);

                // Only find those with unread messages
                if (unread)
                {
                    innerQuery = Query.Or(innerQuery, Query.ElemMatch("Messages", Query.NE("Messages.Read", id)));
                }

                query = Query.And(query, innerQuery);
            }

            ERA.Protocols.PlayerProtocol.Dialogue[] result = GetCollection().Find(query).ToArray();

            foreach (ERA.Protocols.PlayerProtocol.Dialogue dialogue in result)
                foreach (ERA.Protocols.PlayerProtocol.DialogueMessage message in dialogue.GetMessages())
                    message.Parent = dialogue;

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        internal static MongoCollection<ERA.Protocols.PlayerProtocol.Dialogue> GetCollection()
        {
            return ServiceClient.Database.GetCollection<ERA.Protocols.PlayerProtocol.Dialogue>("Players.Dialogues");
        }
    }
}
