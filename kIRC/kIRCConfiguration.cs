/*
    See LICENSE file for license info.
*/

using System;
using System.Threading;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using Rocket.API;
using Rocket.Unturned;
using Rocket.Unturned.Plugins;
using Rocket.Unturned.Player;
using UnityEngine;
using SDG;

namespace kIRCPlugin
{
    public class kIRCConfig : IRocketPluginConfiguration
    {
        public string server;
        public int port;
        public string
            nick,
            user,
            realname,
            password,
            channel,
            command_prefix,
            parameter_delimiter
            ;
        public bool allow_adminowner;
        public bool deathevent;

        [XmlArrayItem(ElementName = "Perform")]
        public List<CPerform> perform;

        [XmlArrayItem(ElementName = "CCommand")]
        public List<kIRC_Commands> ccommands;

        public IRocketPluginConfiguration DefaultConfiguration
        {
            get
            {
                return new kIRCConfig()
                {
                    server = "EDITME",
                    port = 6667,
                    nick = "EDITME",
                    user = "EDITME",
                    realname = "EDITME",
                    password = "EDITME OR LEAVE IT BLANK",
                    channel = "#EDITME",
                    command_prefix = "!",
                    parameter_delimiter = "/",
                    allow_adminowner = true,
                    deathevent = true,
                    perform = new List<CPerform>() { new CPerform("PRIVMSG #somechannel :I am a bot (EXAMPLE PERFORM)") },
                    ccommands = new List<kIRC_Commands>() { new kIRC_Commands("experience", "experience {0} {1}", "o", "[Player/SteamID] [Experience]") }
                };
            }
        }
    }
}
