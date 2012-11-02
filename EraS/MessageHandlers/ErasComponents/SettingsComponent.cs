using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EraS.Connections;
using ServiceProtocol;
using EraS.Services;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.IO;

namespace EraS.MessageHandlers.ErasComponents
{
    internal class SettingsComponent : DefaultComponent
    {
        protected Settings Data { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public SettingsComponent() : base("Settings")
        {
            LoadSettings();
            Functions.Add("GetMongo", GetMongo);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="c"></param>
        /// <param name="m"></param>
        protected void GetMongo(MessageClient c, Message m)
        {
            var ans = m.Answer(c);
            ans.Packet.Write(HeartBeatService.ServerAddress.Host);
            ans.Packet.Write(HeartBeatService.ServerAddress.Port);
            c.SendMessage(ans);
        }

        /// <summary>
        /// 
        /// </summary>
        protected void LoadSettings()
        {
            //var doc = XElement.Load("config.xml");
            XmlSerializer deserializer = new XmlSerializer(typeof(Settings));
            try
            {
                using (TextReader textReader = new StreamReader(@"config.xml"))
                {
                    Data = (Settings)deserializer.Deserialize(textReader);
                }
            }
            catch (FileNotFoundException)
            {
                Data = new Settings();
                SaveSettings();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        protected void SaveSettings()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Settings));
            using (TextWriter textWriter = new StreamWriter(@"config.xml"))
            {
                serializer.Serialize(textWriter, Data);
            }
        }
    }

    public class Settings {
        /// <summary>
        /// 
        /// </summary>
        public String MongoConnectionString;
    }
}
