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
    public class kIRC_PushCommand
    {
        public kIRC_PushCommand() { }

        public string command;
        public string[] parameters;

        public bool execute = false;
        public Action onfireev;
        public Action<string> onexecev;

        public Dictionary<string, string> extradata = new Dictionary<string, string>();

        public void onfire(Action onfireevent) // onfireev()
        {
            onfireev = onfireevent;
            return;
        }

        public void onexec(Action<string> onexecevent) // onexecev(response)
        {
            onexecev = onexecevent;
            return;
        }

        public void Destruct()
        {
            command = "";
            parameters = new string[] { };
            execute = false;
            onfireev = null;
            onexecev = null;
        }

        public void push(kIRC __ref)
        {
            __ref.do_command.Add(this);
            return;
        }
    }
}
