using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson.Serialization.Attributes;

namespace ERA.Protocols.InteractableProtocol
{
    /// <summary>
    /// 
    /// </summary>
    [BsonDiscriminator(RootClass = true)]
    [BsonKnownTypes(typeof(InteractableAppearance), typeof(InteractableMovement))]
    public abstract class InteractableComponent
    {
        /// <summary>
        /// 
        /// </summary>
        public Interactable Root
        {
            get;
            set;
        }

        /// <summary>
        /// Encodes the component
        /// </summary>
        /// <param name="component"></param>
        /// <param name="msg"></param>
        public abstract void Pack(Lidgren.Network.NetBuffer buffer);
    }
}