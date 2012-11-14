using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Lidgren.Network;

namespace InteractableProtocol
{
    [Serializable]
    public partial class Interactable
    {
        /// <summary>
        /// Interactable ID
        /// </summary>
        [BsonId]
        public ObjectId Id
        {
            get;
            private set;
        }

        /// <summary>
        /// Interactable Name
        /// </summary>
        public String Name
        {
            get;
            private set;
        }

        /// <summary>
        /// Interactable State Flags
        /// </summary>
        [BsonRequired]
        public InteractableStateFlags StateFlags
        {
            get;
            private set;
        }

        /// <summary>
        /// Map Id
        /// </summary>
        [BsonRequired]
        public ObjectId MapId
        {
            get;
            private set;
        }

        /// <summary>
        /// Map X coord
        /// </summary>
        [BsonRequired]
        public Int32 MapX
        {
            get;
            set;
        }

        /// <summary>
        /// Map Y coord
        /// </summary>
        [BsonRequired]
        public Int32 MapY
        {
            get;
            set;
        }

        protected Boolean _blockingFlag = false;
        /// <summary>
        /// 
        /// </summary>
        public Boolean IsBlocking
        {
            get { return !StateFlags.HasFlag(InteractableStateFlags.Through) && _blockingFlag; }
            set { _blockingFlag = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public Boolean IsTransparantBlocking
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public Byte PassagesBits
        {
            get;
            set;
        }

        /// <summary>
        /// Holds the Interactable Components
        /// </summary>
        /// <remarks>Key is String because JSON requires Dictionairies to have String Keys</remarks>
        public Dictionary<String, InteractableComponent> Components
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public Boolean HasAppearance
        {
            get { return this.Components.ContainsKey(typeof(InteractableAppearance).Name); }
        }

        /// <summary>
        /// 
        /// </summary>
        public Boolean HasMovement
        {
            get { return this.Components.ContainsKey(typeof(InteractableMovement).Name); }
        }

        /// <summary>
        /// 
        /// </summary>
        public InteractableAppearance Appearance
        {
            get { return GetComponent(typeof(InteractableAppearance)) as InteractableAppearance; }
        }

        /// <summary>
        /// 
        /// </summary>
        public InteractableMovement Movement
        {
            get { return GetComponent(typeof(InteractableMovement)) as InteractableMovement; }
        }


        /// <summary>
        /// Constructor
        /// </summary>
        public Interactable()
        {
            this.Components = new Dictionary<String, InteractableComponent>();
        }

        /// <summary>
        /// Add an AI component
        /// </summary>
        /// <param name="component"></param>
        public InteractableComponent AddComponent(InteractableComponent component)
        {
            this.Components.Add(component.GetType().Name, component);
            return component;
        }

        /// <summary>
        /// Gets an AI component
        /// </summary>
        /// <param name="component"></param>
        /// <returns></returns>
        public InteractableComponent GetComponent(Type component)
        {
            InteractableComponent result;

            if (this.Components.TryGetValue(component.Name, out result))
                return result;

            return null;
        }

        /// <summary>
        /// Clears for pool
        /// </summary>
        public void Clear()
        {
            this.Id = ObjectId.Empty;
            this.Components.Clear();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        public void Pack(NetBuffer buffer)
        {
            buffer.Write(this.Id.ToByteArray());
            buffer.Write(this.Name);
            buffer.Write((Byte)this.StateFlags);
            buffer.Write(this.MapId.ToByteArray());
            buffer.Write(this.MapX);
            buffer.Write(this.MapY);
            buffer.Write(this.IsTransparantBlocking);
            buffer.Write(this.PassagesBits);

            buffer.Write(this.Components.Count);
            foreach (var component in this.Components.Values)
                component.Pack(buffer);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static Interactable Unpack(NetBuffer buffer)
        {
            var result = new Interactable();
            result.Id = new ObjectId(buffer.ReadBytes(12));
            result.Name = buffer.ReadString();
            result.StateFlags = (InteractableStateFlags)buffer.ReadByte();
            result.MapId = new ObjectId(buffer.ReadBytes(12));
            result.MapX = buffer.ReadInt32();
            result.MapY = buffer.ReadInt32();
            result.IsTransparantBlocking = buffer.ReadBoolean();
            result.PassagesBits = buffer.ReadByte();

            var components = buffer.ReadInt32();
            for (; components > 0; components--)
            {
                var type = (InteractableComponentType)buffer.ReadRangedInteger(0, (Int32)(InteractableComponentType.Max));
                switch (type)
                {
                    case InteractableComponentType.Appearance:
                        result.AddComponent(InteractableAppearance.Unpack(buffer));
                        break;
                    case InteractableComponentType.Movement:
                        result.AddComponent(InteractableMovement.Unpack(buffer));
                        break;
                }
            }
            return result;
        }
    }
}
