using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using Lidgren.Network;

namespace PlayerProtocol
{
    /// <summary>
    /// Dialogue message
    /// </summary>
    public class DialogueMessage
    {
        /// <summary>
        /// Message id
        /// </summary>
        [BsonRequired]
        public ObjectId Id
        {
            get;
            private set;
        }

        /// <summary>
        /// Sender of message
        /// </summary>
        [BsonRequired]
        public ObjectId Sender
        {
            get;
            private set;
        }

        /// <summary>
        /// Contents of message
        /// </summary>
        [BsonRequired]
        public String Contents
        {
            get;
            private set;
        }

        /// <summary>
        /// Timestamp of message
        /// </summary>
        [BsonIgnore]
        public DateTime TimeStamp
        {
            get
            {
                return Id.CreationTime;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [BsonRequired]
        public IAttachment Attachment
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        [BsonRequired]
        public HashSet<ObjectId> Read
        {
            get;
            private set;
        }

        [BsonIgnore]
        public Dialogue Parent
        {
            get;
            set;
        }

        /// <summary>
        /// Attachment interface
        /// </summary>
        [BsonDiscriminator(RootClass = true)]
        [BsonKnownTypes(typeof(AchievementAttachment), typeof(EffortAttachment), typeof(ItemAttachment), typeof(LocationAttachment))]
        public abstract class IAttachment
        {
            /// <summary>
            /// 
            /// </summary>
            [BsonId]
            public ObjectId Id
            {
                get;
                protected set;
            }


            /// <summary>
            /// Attachment id
            /// </summary>
            [BsonRequired]
            public ObjectId Sender
            {
                get;
                protected set;
            }

            /// <summary>
            /// 
            /// </summary>
            [BsonIgnore]
            public DateTime TimeStamp
            {
                get { return Id.CreationTime; }
            }

            internal abstract NetBuffer Pack(ref NetBuffer msg);
        }

        /// <summary>
        /// Attachment in message to show an interactables location
        /// </summary>
        public class LocationAttachment : IAttachment
        {
            internal const Byte AttachmentType = 1;

            /// <summary>
            /// Map id
            /// </summary>
            [BsonRequired]
            public ObjectId MapId
            {
                get;
                private set;
            }

            /// <summary>
            /// Map x coord
            /// </summary>
            [BsonRequired]
            public Int32 MapX
            {
                get;
                private set;
            }

            /// <summary>
            /// Map y coord
            /// </summary>
            [BsonRequired]
            public Int32 MapY
            {
                get;
                private set;
            }

            /// <summary>
            /// Generates location attachment
            /// </summary>
            /// <param name="source"></param>
            /// <returns></returns>
            public static IAttachment Generate(ObjectId sender, ObjectId mapId, Int32 mapX, Int32 mapY)
            {
                LocationAttachment result = new LocationAttachment();
                result.Id = ObjectId.GenerateNewId();
                result.Sender = sender;
                result.MapId = mapId;
                result.MapX = mapX;
                result.MapY = mapY;

                return result;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="msg"></param>
            /// <returns></returns>
            internal override NetBuffer Pack(ref NetBuffer msg)
            {
                msg.Write(AttachmentType); // 1
                msg.Write(this.Id.ToByteArray()); // 13
                msg.Write(this.MapId.ToByteArray()); // 25
                msg.Write(this.MapX); // 29
                msg.Write(this.MapY); // 33
                msg.Write(this.Sender.ToByteArray()); // 45
                return msg;
            }

            internal static IAttachment Unpack(NetBuffer msg)
            {
                var result = new LocationAttachment();
                result.Id = new ObjectId(msg.ReadBytes(12));
                result.MapId = new ObjectId(msg.ReadBytes(12));
                result.MapX = msg.ReadInt32();
                result.MapY = msg.ReadInt32();
                result.Sender = new ObjectId(msg.ReadBytes(12));
                return result;
            }
        }

        /// <summary>
        /// Attachment in message to show an item
        /// </summary>
        public class ItemAttachment : IAttachment
        {
            internal const Byte AttachmentType = 2;

            /// <summary>
            /// Item id
            /// </summary>
            [BsonRequired]
            public ObjectId ItemId
            {
                get;
                private set;
            }

            /// <summary>
            /// Blueprint id
            /// </summary>
            public Int32 BlueprintId
            {
                get;
                private set;
            }

            /// <summary>
            /// Generates item attachment
            /// </summary>
            /// <param name="item"></param>
            /// <returns></returns>
            public static IAttachment Generate(ObjectId sender, ObjectId itemId, Int32 blueprintId)
            {
                ItemAttachment result = new ItemAttachment();
                result.Id = ObjectId.GenerateNewId();
                result.ItemId = itemId;
                result.Sender = sender;
                result.BlueprintId = blueprintId;

                return result;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="msg"></param>
            /// <returns></returns>
            internal override NetBuffer Pack(ref NetBuffer msg)
            {
                msg.Write(AttachmentType); // 1
                msg.Write(this.Id.ToByteArray()); // 13
                msg.Write(this.BlueprintId); // 17
                msg.Write(this.Sender.ToByteArray()); // 29
                return msg;
            }

            internal static IAttachment Unpack(NetBuffer msg)
            {
                var result = new ItemAttachment();
                result.Id = new ObjectId(msg.ReadBytes(12));
                result.BlueprintId = msg.ReadInt32();
                result.Sender = new ObjectId(msg.ReadBytes(12));
                return result;
            }
        }

        /// <summary>
        /// Attachment in message to show an achievement
        /// </summary>
        public class AchievementAttachment : IAttachment
        {
            internal const Byte AttachmentType = 3;

            /// <summary>
            /// 
            /// </summary>
            /// <param name="msg"></param>
            /// <returns></returns>
            internal override NetBuffer Pack(ref NetBuffer msg)
            {
                msg.Write(AttachmentType); // 1
                msg.Write(this.Id.ToByteArray()); // 13
                msg.Write(this.Sender.ToByteArray()); // 25
                return msg;
            }

            internal static IAttachment Unpack(NetBuffer msg)
            {
                var result = new AchievementAttachment();
                result.Id = new ObjectId(msg.ReadBytes(12));
                result.Sender = new ObjectId(msg.ReadBytes(12));
                return result;
            }
        }

        /// <summary>
        /// Attachment in message to show an effort
        /// </summary>
        public class EffortAttachment : IAttachment
        {
            internal const Byte AttachmentType = 4;

            /// <summary>
            /// 
            /// </summary>
            /// <param name="msg"></param>
            /// <returns></returns>
            internal override NetBuffer Pack(ref NetBuffer msg)
            {
                msg.Write(AttachmentType); // 1
                msg.Write(this.Id.ToByteArray()); // 13
                msg.Write(this.Sender.ToByteArray()); // 25
                return msg;
            }

            internal static IAttachment Unpack(NetBuffer msg)
            {
                var result = new EffortAttachment();
                result.Id = new ObjectId(msg.ReadBytes(12));
                result.Sender = new ObjectId(msg.ReadBytes(12));
                return result;
            }
        }

        /// <summary>
        /// Generates a new dialog message
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static DialogueMessage Generate(ObjectId sender, String message)
        {
            DialogueMessage result = new DialogueMessage();
            result.Id = ObjectId.GenerateNewId();
            result.Sender = sender;
            result.Contents = message;
            result.Read = new HashSet<ObjectId> { sender };
            result.Read.Remove(ObjectId.Empty);
            result.Parent = null;

            return result;
        }

        /// <summary>
        /// Attaches attachment to a message
        /// </summary>
        /// <param name="attachment"></param>
        public void Attach(DialogueMessage.IAttachment attachment)
        {
            this.Attachment = attachment;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="receipientId"></param>
        public Boolean MarkAsRead(ObjectId readerId)
        {
            return readerId != ObjectId.Empty && this.Read.Add(readerId);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="readerId"></param>
        public Boolean MarkAsUnread(ObjectId readerId)
        {
            return this.Read.Remove(readerId);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public NetBuffer Pack(ref NetBuffer msg)
        {
            msg.Write(this.Id.ToByteArray()); // 12
            msg.Write(this.Contents); // 12 + x
            msg.Write(this.Sender.ToByteArray()); // 24 + x
            msg.Write(this.Attachment != null); // 33 + x
            if (this.Attachment != null)
                this.Attachment.Pack(ref msg); // (33 || 45) + x

            return msg;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public DialogueMessage Unpack(NetBuffer msg)
        {
            var result = new DialogueMessage();
            result.Id = new ObjectId(msg.ReadBytes(12));
            result.Contents = msg.ReadString();
            result.Sender = new ObjectId(msg.ReadBytes(12));
            if (msg.ReadBoolean())
            {
                var type = msg.ReadByte();
                switch (type)
                {
                    case AchievementAttachment.AttachmentType:
                        result.Attachment = AchievementAttachment.Unpack(msg);
                        break;
                    case EffortAttachment.AttachmentType:
                        result.Attachment = EffortAttachment.Unpack(msg);
                        break;
                    case ItemAttachment.AttachmentType:
                        result.Attachment = ItemAttachment.Unpack(msg);
                        break;
                    case LocationAttachment.AttachmentType:
                        result.Attachment = LocationAttachment.Unpack(msg);
                        break;
                }
            }

            return result;
        }
    }
}
