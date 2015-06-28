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
        public const string VERSION = "v1.6";
        public const string update_checkerurl = "https://raw.githubusercontent.com/Kirollos/Rocket_kIRC/master/VERSION";
        static HttpWebRequest Updater;
        static HttpWebResponse Updater_Response;
        public static DateTime lastchecked;

        private static bool inited = false;

        public static void Init()
        {
            Updater = null;
            Updater_Response = null;
            if (inited) return;
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
            inited = true;
            return;
        }

        public static void CheckUpdate()
        {
            kIRCVersionChecker.Init();
            Updater = (HttpWebRequest)HttpWebRequest.Create(update_checkerurl);
            Updater_Response = (HttpWebResponse)Updater.GetResponse();

            if (Updater_Response.StatusCode == HttpStatusCode.OK)
            {
                Rocket.Unturned.Logging.Logger.Log("kIRC: Contacting updater...");
                Stream reads = Updater_Response.GetResponseStream();
                byte[] buff = new byte[10];
                reads.Read(buff, 0, 10);
                string ver = Encoding.UTF8.GetString(buff);
                ver = ver.ToLower().Trim(new[] { ' ', '\r', '\n', '\t' }).TrimEnd(new[] { '\0' });

                if (ver == VERSION.ToLower().Trim())
                {
                    Rocket.Unturned.Logging.Logger.Log("kIRC: This plugin is using the latest version!");
                }
                else
                {
                    Rocket.Unturned.Logging.Logger.LogWarning("kIRC Warning: Plugin version mismatch!");
                    Rocket.Unturned.Logging.Logger.LogWarning("Current version: "+VERSION+", Latest version on repository is " + ver + ".");
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

        public static void CheckUpdate(kIRCCore irc, string irc_target)
        {
            Updater = (HttpWebRequest)HttpWebRequest.Create(update_checkerurl);
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
            Updater_Response = (HttpWebResponse)Updater.GetResponse();

            if (Updater_Response.StatusCode == HttpStatusCode.OK)
            {
                irc.Say(irc_target, "kIRC: Contacting updater...");
                Stream reads = Updater_Response.GetResponseStream();
                byte[] buff = new byte[10];
                reads.Read(buff, 0, 10);
                string ver = Encoding.UTF8.GetString(buff);
                ver = ver.ToLower().Trim(new[] { ' ', '\r', '\n', '\t' }).TrimEnd(new[] { '\0' });

                if (ver == VERSION.ToLower().Trim())
                {
                    irc.Say(irc_target, "kIRC: This plugin is using the latest version!");
                }
                else
                {
                    irc.Say(irc_target, "kIRC Warning: Plugin version mismatch!");
                    irc.Say(irc_target, "Current version: " + VERSION + ", Latest version on repository is " + ver + ".");
                }
            }
            else
            {
                irc.Say(irc_target, "kIRC Error: Failed to contact updater.");
            }
            Updater.Abort();
            Updater = null;
            Updater_Response = null;
            lastchecked = DateTime.Now;
        }
    }
}
