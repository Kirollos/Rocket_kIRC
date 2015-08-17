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
using Rocket.API.Collections;
using Rocket.API.Extensions;
using Rocket.Core.Plugins;
using Rocket.Core.Logging;
using Rocket.Unturned;
using Rocket.Unturned.Player;
using Rocket.Unturned.Plugins;
using Rocket.Unturned.Chat;
using SDG.Unturned;
using UnityEngine;
using SDG;

namespace kIRCPlugin
{
    public class kIRCConfig : IRocketPluginConfiguration
    {
        public string server;
        public int port;
        public string
            spassword,
            nick,
            user,
            realname,
            password,
            channel,
            command_prefix,
            parameter_delimiter
            ;
        public bool allow_adminowner;
        public kDeathEvent deathevent;

        [XmlArrayItem(ElementName = "Perform")]
        public List<CPerform> perform;

        [XmlArrayItem(ElementName = "CCommand")]
        public List<kIRC_Commands> ccommands;
        public bool Debug;

        public void LoadDefaults()
        {
            server = "EDITME";
            port = 6667;
            spassword = "";
            nick = "EDITME";
            user = "EDITME";
            realname = "EDITME";
            password = "EDITME OR LEAVE IT BLANK";
            channel = "#EDITME";
            command_prefix = "!";
            parameter_delimiter = "/";
            allow_adminowner = true;
            deathevent = new kDeathEvent(true, false);
            perform = new List<CPerform>() { new CPerform("PRIVMSG #somechannel :I am a bot (EXAMPLE PERFORM)") };
            ccommands = new List<kIRC_Commands>() { new kIRC_Commands("experience", "experience {0}/{1}", "o", "[Player/SteamID]/[Experience]") };
            Debug = false;
        }
    }

    public class kDeathEvent
    {
        [XmlAttribute("show")]
        public bool show;
        [XmlAttribute("suicides")]
        public bool suicides;

        public kDeathEvent() { }
        public kDeathEvent(bool _show, bool _suicides)
        {
            show = _show;
            suicides = _suicides;
        }
    }
}
