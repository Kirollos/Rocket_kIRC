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
    enum mIRC_Colours
    {
        White = 0,  Black,  Blue,   Green,  LRed,   Brown,  Purple, 
        Orange,     Yellow, LGreen, Cyan,   LCyan,  LBlue,  Pink,   Grey,   LGrey
    }

    class kIRCTranslate
    {
        public static string Translate(string key, Dictionary<string, string> parameters)
        {
            if (!kIRC.dis.Translations.ContainsKey(key))
                return "";
            string retval = kIRC.dis.Translate(key, new object[] {});

            foreach(var lekey in parameters)
            {
                if (retval.Contains("{"+lekey.Key+"}"))
                    retval = retval.Replace("{"+lekey.Key+"}", lekey.Value);
            }

            int idx;
            while((idx = retval.IndexOf("{irccolor:")) > 0)
            {
                int idxc = retval.IndexOf(':', idx);
                int idxend = retval.IndexOf('}', idxc);
                string colourname = retval.Substring(idxc + 1, idxend-idxc);

                mIRC_Colours colourid;

                try
                {
                    colourid = (mIRC_Colours) Enum.Parse(typeof(mIRC_Colours), colourname);
                    if(!Enum.IsDefined(typeof(mIRC_Colours), colourid))
                    {
                        Rocket.Unturned.Logging.Logger.LogWarning("kIRC Warning: IRC colour (" + colourname + ") is invalid. Using colour ID 0.");
                        colourid = mIRC_Colours.Black; // 0
                    }
                }
                catch
                {
                    Rocket.Unturned.Logging.Logger.LogWarning("kIRC Warning: IRC colour (" + colourname + ") is invalid. Using colour ID 0.");
                    colourid = mIRC_Colours.Black; // 0
                }

                retval.Replace("{irccolor:" + colourname + "}", String.Format("{0}{1:D2}", Convert.ToChar(3), (int)colourid));

            }

            return retval;
        }

        public static void IRC_SayTranslation(kIRCCore irc, string target, string key, Dictionary<string, string> _parameters)
        {
            irc.Say(target, kIRCTranslate.Translate(key, _parameters));
            return;
        }

        public static void IRC_NoticeTranslation(kIRCCore irc, string target, string key, Dictionary<string, string> _parameters)
        {
            irc.Notice(target, kIRCTranslate.Translate(key, _parameters));
            return;
        }

        public static void Rocket_ChatSay(string key, Color color, Dictionary<string, string> _parameters)
        {
            RocketChat.Say(kIRCTranslate.Translate(key, _parameters), color);
        }

        public static void Rocket_ChatSay(string key, Dictionary<string, string> _parameters)
        {
            RocketChat.Say(kIRCTranslate.Translate(key, _parameters));
        }
    }
}
