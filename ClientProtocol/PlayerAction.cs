using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClientProtocol
{
    public enum PlayerAction : byte
    {
        /// <summary>
        /// 
        /// </summary>
        None = 0,

        /// <summary>
        /// 
        /// </summary>
        Max = 32,

        /// <summary>
        /// 1: Actively Requesting Player Data
        /// </summary>
        Get = 1,

        /// <summary>
        /// 2: Gets the interactable id's for this player
        /// </summary>
        GetAvatars = 2,

        /// <summary>
        /// 6: Requests action key processing
        /// </summary>
        RequestActionKey = 6,

        /// <summary>
        /// 7: Requests accepting choice
        /// </summary>
        RequestAccept = 7,

        /// <summary>
        /// 8: Requests declining choice
        /// </summary>
        RequestCancel = 8,

        /// <summary>
        /// 9: Requests movement
        /// </summary>
        RequestMovement = 9,

        /// <summary>
        /// 10: Sends a message
        /// </summary>
        Message = 10,

        /// <summary>
        /// 11: Attachement to message
        /// </summary>
        MessageAttachment = 11,

        /// <summary>
        /// 12: Sets a message to read/unread
        /// </summary>
        MessageStatus = 12,

        /// <summary>
        /// 13: Starts a new dialogue
        /// </summary>
        MessageStart = 13,

        /// <summary>
        /// 14: Adds a participant to the conversation
        /// </summary>
        MessageParticipant = 14,

        /// <summary>
        /// 15: When Picking the avatar
        /// </summary>
        PickAvatar = 15,

        /// <summary>
        /// 31: Transfers player to other server
        /// </summary>
        Transfer = 31,

    }
}
