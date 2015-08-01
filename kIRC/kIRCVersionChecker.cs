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
    class kIRCVersionChecker
    {
        public const string VERSION = "v1.6.5";
        public const string update_checkerurl = "https://raw.githubusercontent.com/Kirollos/Rocket_kIRC/master/VERSION";
        public static DateTime lastchecked;
        static bool plswaitimchecking = false;

        private static bool inited = false;

        public static void Init()
        {
            if (inited) return;
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
            inited = true;
            return;
        }

        public static void CheckUpdate()
        {
            try {
                kIRCVersionChecker.Init();
                HttpWebRequest Updater;
                HttpWebResponse Updater_Response;
                Updater = (HttpWebRequest)HttpWebRequest.Create(update_checkerurl);
                Updater_Response = (HttpWebResponse)Updater.GetResponse();

                if (Updater_Response.StatusCode == HttpStatusCode.OK)
                {
                    Logger.Log("kIRC: Contacting updater...");
                    Stream reads = Updater_Response.GetResponseStream();
                    byte[] buff = new byte[10];
                    reads.Read(buff, 0, 10);
                    string ver = Encoding.UTF8.GetString(buff);
                    ver = ver.ToLower().Trim(new[] { ' ', '\r', '\n', '\t' }).TrimEnd(new[] { '\0' });

                    if (ver == VERSION.ToLower().Trim())
                    {
                        Logger.Log("kIRC: This plugin is using the latest version!");
                    }
                    else
                    {
                        Logger.LogWarning("kIRC Warning: Plugin version mismatch!");
                        Logger.LogWarning("Current version: " + VERSION + ", Latest version on repository is " + ver + ".");
                    }
                }
                else
                {
                    Logger.LogError("kIRC Error: Failed to contact updater.");
                }
                Updater.Abort();
            }
            catch(Exception ex)
            {
                Logger.LogError("Failed to check for update!");
                Logger.LogException(ex);
            }
            lastchecked = DateTime.Now;
        }

        public static void CheckUpdate(kIRCCore irc, string irc_target)
        {
            try {
                HttpWebRequest Updater;
                HttpWebResponse Updater_Response;
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
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to check for update!");
                Logger.LogException(ex);
            }
            lastchecked = DateTime.Now;
        }
    }
}
