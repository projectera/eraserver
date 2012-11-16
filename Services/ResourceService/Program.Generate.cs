using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceProtocol;
using ERA.Services.Resource.Generators;

namespace ERA.Services.Resource
{
    partial class Program
    {
        public static Dictionary<String, Action<Message>> GenerateFunctions = new Dictionary<String, Action<Message>>() {
            { "Names", GetNames },
            { "Werd", GetWerd },
        };

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        public static void Generate(Message msg)
        {
            var function = msg.Packet.ReadString();
            lock (GenerateFunctions)
                if (GenerateFunctions.ContainsKey(function))
                    GenerateFunctions[function](msg);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        public static void GetNames(Message msg)
        {
            var sub = msg.Packet.ReadString();
            var count = msg.Packet.ReadInt32();

            try {
                var answer = msg.Answer(EraSClient);
                var result = LanguageConfluxer.Run(sub, count);
                answer.Packet.Write(result.Length);
                foreach (var res in result)
                    answer.Packet.Write(res);
                EraSClient.SendMessage(answer);

            } catch (Exception) {
            
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        public static void GetWerd(Message msg)
        {
            var sub = msg.Packet.ReadString();
            var count = msg.Packet.ReadInt32();

            try
            {
                var answer = msg.Answer(EraSClient);
                var result = Werd.Run(sub, count);
                answer.Packet.Write(result.Length);
                foreach (var res in result)
                    answer.Packet.Write(res);
                EraSClient.SendMessage(answer);

            }
            catch (Exception)
            {

            }
        }
    }
}
