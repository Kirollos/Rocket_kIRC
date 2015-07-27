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
    public class CommandIRCPM : IRocketCommand
    {
        public bool AllowFromConsole
        {
            get {return false;}
        }

        public string Name
        {
            get {return "ircpm";}
        }

        public string Help
        {
            get {return "Sends a personal message (IRC Notice) to an IRC user";}
        }

        public List<string> Aliases
        {
            get {return new List<string>() {"irc"};}
        }

        public string Syntax
        {
            get {return "<username> <message>";}
        }

        public List<string> Permissions
        {
            get { return new List<string>(){}; }
        }

        public void Execute(Rocket.API.IRocketPlayer caller, string[] command)
        {
            if (!kIRC.dis.myirc.isConnected) return;

            //if (String.IsNullOrEmpty(command) || command.Split(' ').Length < 2)
            if (command.Length < 2)
            {
                UnturnedChat.Say(caller, "Syntax: /ircpm [user name] [message]", Color.yellow);
                return;
            }
            else
            {
                //string username = command.Split(' ')[0];
                //string message = command.Remove(0, username.Length + 1);
                string username = command[0];
                string message = String.Join(" ", command, 1, command.Length - 1);
                bool useronline = false;

                for (int i = 0; i < kIRC.dis.myirc.userlist.Count; i++)
                {
                    if (kIRC.dis.myirc.userlist[i][0] == username)
                    {
                        useronline = true;
                        break;
                    }
                    useronline = false;
                }
                
                if (!useronline)
                {
                    UnturnedChat.Say(caller, "Error: Username \"" + username + "\" is not online.", Color.red);
                    return;
                }
                else
                {
                    kIRC.dis.myirc.Notice(username, "[Unturned PM] " + caller.DisplayName + ": " + message);
                }
            }
        }
    }
}
