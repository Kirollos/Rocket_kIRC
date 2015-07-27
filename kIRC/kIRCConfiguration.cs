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
        public string server = "EDITME";
        public int port = 6667;
        public string
            nick = "EDITME",
            user = "EDITME",
            realname = "EDITME",
            password = "EDITME OR LEAVE IT BLANK",
            channel = "#EDITME",
            command_prefix = "!",
            parameter_delimiter = "/"
            ;
        public bool allow_adminowner = true;
        public kDeathEvent deathevent = new kDeathEvent(true, false);

        [XmlArrayItem(ElementName = "Perform")]
        public List<CPerform> perform = new List<CPerform>() { new CPerform("PRIVMSG #somechannel :I am a bot (EXAMPLE PERFORM)") };

        [XmlArrayItem(ElementName = "CCommand")]
        public List<kIRC_Commands> ccommands = new List<kIRC_Commands>() { new kIRC_Commands("experience", "experience {0}/{1}", "o", "[Player/SteamID]/[Experience]") };
        public bool Debug = false;

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
