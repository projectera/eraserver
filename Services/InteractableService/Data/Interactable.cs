using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using MongoDB.Driver;
using ERA.Protocols.ServiceProtocol;

namespace ERA.Services.InteractableService.Data
{
    internal static class Interactable
    {
        /// <summary>
        /// Gets a interactable from the db
        /// </summary>
        /// <param name="id">id of interactable to get</param>
        /// <returns></returns>
        internal static Task<ERA.Protocols.InteractableProtocol.Interactable> Get(ObjectId id)
        {
            return Task.Factory.StartNew(() => { return GetBlocking(id); });
        }

        /// <summary>
        /// Gets a interactable from the db
        /// </summary>
        /// <param name="name">name of interactable to get</param>
        /// <returns></returns>
        internal static Task<ERA.Protocols.InteractableProtocol.Interactable> Get(String name)
        {
            return Task.Factory.StartNew(() => { return GetBlocking(name); });
        }

        /// <summary>
        /// Gets a Interactable from the db,  blocks while retrieving
        /// </summary>
        /// <param name="id">id of Interactable to get</param>
        /// <returns></returns>
        internal static ERA.Protocols.InteractableProtocol.Interactable GetBlocking(ObjectId id)
        {
            ERA.Protocols.InteractableProtocol.Interactable result = GetCollection().FindOneById(id) as ERA.Protocols.InteractableProtocol.Interactable;
            foreach (var component in result.Components.Values)
                component.Root = result;
            return result;
        }

        /// <summary>
        /// Gets a Interactable from the db, blocks while retrieving
        /// </summary>
        /// <param name="name">name of Interactable to get</param>
        /// <returns></returns>
        internal static ERA.Protocols.InteractableProtocol.Interactable GetBlocking(String name)
        {
            return GetCollection().FindOneAs<ERA.Protocols.InteractableProtocol.Interactable>(
                Query.Matches("Name", new BsonRegularExpression("^(?i)" + name + "$"))) as ERA.Protocols.InteractableProtocol.Interactable;
        }

        /// <summary>
        /// Gets the players collection
        /// </summary>
        /// <returns></returns>
        internal static MongoCollection<ERA.Protocols.InteractableProtocol.Interactable> GetCollection()
        {
            return ServiceClient.Database.GetCollection<ERA.Protocols.InteractableProtocol.Interactable>("Interactables");
        }

        /// <summary>
        /// Puts a Interactable to the db
        /// </summary>
        internal static void Put(ERA.Protocols.InteractableProtocol.Interactable interactable)
        {
            Put(interactable, SafeMode.False);
        }

        /// <summary>
        /// Puts a Interactable to the db
        /// <param name="safemode">Sets the safemode on this query</param>
        /// </summary>
        internal static SafeModeResult Put(ERA.Protocols.InteractableProtocol.Interactable interactable, SafeMode safemode)
        {
            return GetCollection().Save<ERA.Protocols.InteractableProtocol.Interactable>(interactable, safemode);
        }
    }
}
