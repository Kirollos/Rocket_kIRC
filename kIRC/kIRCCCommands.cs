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
    public class kIRC_Commands
    {
        [XmlArrayItem(ElementName="CCommand")]
        [XmlAttribute("BotCommand")]
        public string BotCommand = "";
        public string ConsoleCommand = "";
        public string BotSyntax = "";
        public string FlagNeeded = ""; // q for ~, a for &, o for @, h for %, v for +, or empty for none.
        public string IRCmsg_onfire;
        public string GAMEmsg_onfire;
        public string IRCmsg_onexec;
        public string GAMEmsg_onexec;
        public bool printresponse;

        public kIRC_Commands() { }

        public kIRC_Commands(string botcommand, string consolecommand, string flagneeded, string botsyntax)
        {
            BotCommand = botcommand;
            ConsoleCommand = consolecommand;
            BotSyntax = botsyntax;
            FlagNeeded = flagneeded;
            IRCmsg_onfire = " ";
            IRCmsg_onexec = " ";
            GAMEmsg_onfire = " ";
            GAMEmsg_onexec = " ";
            IRCmsg_onfire = " ";
            printresponse = true;
        }
    }
}
