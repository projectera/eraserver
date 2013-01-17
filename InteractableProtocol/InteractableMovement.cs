using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;

namespace ERA.Protocols.InteractableProtocol
{
    [Serializable]
    public class InteractableMovement : InteractableComponent
    {
        /// <summary>
        /// 
        /// </summary>
        public Byte MoveSpeed
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public Byte MoveFrequency
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public Byte StopFrequency
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        internal static InteractableMovement Generate()
        {
            InteractableMovement movement = new InteractableMovement();
            movement.MoveSpeed = 3;
            movement.MoveFrequency = 3;
            movement.StopFrequency = 0;
            return movement;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="root"></param>
        /// <returns></returns>
        internal static InteractableMovement Generate(Interactable root)
        {
            InteractableMovement result = Generate();
            result.Root = root;
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Clear()
        {
            this.MoveFrequency = 0;
            this.MoveSpeed = 0;
            this.StopFrequency = 0;
            this.Root = null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        public override void Pack(NetBuffer msg)
        {
            // Write header
            msg.WriteRangedInteger(0, (Int32)InteractableComponentType.Max, (Int32)InteractableComponentType.Movement);

            // Write data
            msg.Write(this.MoveFrequency);
            msg.Write(this.StopFrequency);
            msg.Write(this.MoveSpeed);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static InteractableComponent Unpack(NetBuffer msg)
        {
            // Header should already be read out!

            var result = InteractableMovement.Generate();
            result.MoveFrequency = msg.ReadByte();
            result.StopFrequency = msg.ReadByte();
            result.MoveSpeed = msg.ReadByte();
            return result;
        }


    }
}
