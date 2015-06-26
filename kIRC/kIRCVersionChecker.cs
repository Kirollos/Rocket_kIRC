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
using SDG.Unturned;
using UnityEngine;
using SDG;

namespace kIRCPlugin
{
    class kIRCVersionChecker
    {
        public const string VERSION = "v1.5";
        public const string update_checkerurl = "https://raw.githubusercontent.com/Kirollos/Rocket_kIRC/master/VERSION";
        static HttpWebRequest Updater;
        static HttpWebResponse Updater_Response;
        public static DateTime lastchecked;

        public static void CheckUpdate()
        {
            Updater = (HttpWebRequest)HttpWebRequest.Create(update_checkerurl);
            Updater_Response = (HttpWebResponse)Updater.GetResponse();

            if (Updater_Response.StatusCode == HttpStatusCode.OK)
            {
                Rocket.Unturned.Logging.Logger.Log("kIRC: Contacting updater...");
                Stream reads = Updater_Response.GetResponseStream();
                byte[] buff = new byte[reads.Length + 1];
                reads.Read(buff, 0, (int)reads.Length);

                if (buff.ToString().ToLower() == VERSION.ToLower())
                {
                    Rocket.Unturned.Logging.Logger.Log("kIRC: This plugin is using the latest version!");
                }
                else
                {
                    Rocket.Unturned.Logging.Logger.LogWarning("kIRC Warning: This plugin is outdated! Latest version on repository is " + VERSION + ".");
                }
            }
            else
            {
                Rocket.Unturned.Logging.Logger.LogError("kIRC Error: Failed to contact updater.");
            }
            Updater.Abort();
            Updater = null;
            Updater_Response = null;
            lastchecked = DateTime.Now;
        }
    }
}
