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
using Rocket.API;
using Rocket.Unturned;
using Rocket.Unturned.Plugins;
using Rocket.Unturned.Player;
using UnityEngine;
using SDG;

namespace kIRCPlugin
{
    public class kIRC : RocketPlugin<kIRCConfig>
    {
        public static kIRCCore myirc;
        Thread ircthread;

        public Dictionary<string, byte> _playershealth = new Dictionary<string, byte>();

        public bool do_save;

        protected override void Load()
        {
            do_save = false;
            if(this.Configuration.server == "EDITME")
            {
                Rocket.Unturned.RocketConsole.print("kIRC Error: You did not configure the plugin! Unloading now...");
                this.Unload();
                return;
            }
            if(this.Configuration.parameter_delimiter == " ")
            {
                Rocket.Unturned.RocketConsole.print("kIRC Warning: parameter delimiter cannot be \" \"! Therefore, setting it to \"/\"");
            }
            myirc = new kIRCCore(this.Configuration.server, this.Configuration.port, this.Configuration.nick, this.Configuration.user, this.Configuration.realname, this.Configuration.channel, this.Configuration.password);
            
            // Command Prefix
            if(!String.IsNullOrEmpty(this.Configuration.command_prefix))
            {
                if(this.Configuration.command_prefix.Length == 1)
                {
                    myirc.SetCommandPrefix(System.Convert.ToChar(this.Configuration.command_prefix));
                }
                else
                {
                    Rocket.Unturned.RocketConsole.print("kIRC Error: command_prefix is not a character! Setting default to '!'");
                    this.Configuration.command_prefix = "!";
                    myirc.SetCommandPrefix('!');
                }
            }
            else
            {
                Rocket.Unturned.RocketConsole.print("kIRC Error: command_prefix is not set! Setting default to '!'");
                this.Configuration.command_prefix = "!";
                myirc.SetCommandPrefix('!');
            }

            // Parameter Delimiter
            if (!String.IsNullOrEmpty(this.Configuration.parameter_delimiter))
            {
                
                if (this.Configuration.parameter_delimiter.Length == 1)
                {
                    if (this.Configuration.parameter_delimiter == " ")
                    {
                        Rocket.Unturned.RocketConsole.print("kIRC Warning: parameter delimiter cannot be \" \"! Therefore, setting it to \"/\"");
                        this.Configuration.parameter_delimiter = "/";
                        myirc.SetParameterDelimiter('/');
                    }
                    else
                        myirc.SetParameterDelimiter(System.Convert.ToChar(this.Configuration.parameter_delimiter));
                }
                else
                {
                    Rocket.Unturned.RocketConsole.print("kIRC Error: parameter_delimiter is not set! Setting default to \"/\"");
                    this.Configuration.parameter_delimiter = "/";
                    myirc.SetParameterDelimiter('/');
                }
            }
            else
            {
                Rocket.Unturned.RocketConsole.print("kIRC Error: parameter_delimiter is not set! Setting default to \"/\"");
                this.Configuration.parameter_delimiter = "/";
                myirc.SetParameterDelimiter('/');
            }

            myirc.SetAllowAdminOwner(this.Configuration.allow_adminowner);

            ircthread = new Thread( () => myirc.loopparsing(this));
            ircthread.Start();

            Rocket.Unturned.Events.RocketServerEvents.OnServerShutdown += Unturned_OnServerShutdown;
            Rocket.Unturned.Events.RocketPlayerEvents.OnPlayerChatted += Unturned_OnPlayerChatted;
            Rocket.Unturned.Events.RocketServerEvents.OnPlayerConnected += Unturned_OnPlayerConnected;
            Rocket.Unturned.Events.RocketServerEvents.OnPlayerDisconnected += Unturned_OnPlayerDisconnected;
            //Rocket.Unturned.Events.RocketPlayerEvents.OnPlayerDeath += Unturned_OnPlayerDeath;
            Rocket.Unturned.Events.RocketPlayerEvents.OnPlayerUpdateHealth += Unturned_OnPlayerUpdateHealth;
        }

        protected override void Unload()
        {
            myirc.Destruct();
            //ircthread.Join();
            ircthread.Abort();
            // Rocket unload/reload does not clear this anyway...
            Rocket.Unturned.Events.RocketServerEvents.OnServerShutdown -= Unturned_OnServerShutdown;
            Rocket.Unturned.Events.RocketPlayerEvents.OnPlayerChatted -= Unturned_OnPlayerChatted;
            Rocket.Unturned.Events.RocketServerEvents.OnPlayerConnected -= Unturned_OnPlayerConnected;
            Rocket.Unturned.Events.RocketServerEvents.OnPlayerDisconnected -= Unturned_OnPlayerDisconnected;
            //Rocket.Unturned.Events.RocketPlayerEvents.OnPlayerDeath -= Unturned_OnPlayerDeath;
            Rocket.Unturned.Events.RocketPlayerEvents.OnPlayerUpdateHealth -= Unturned_OnPlayerUpdateHealth;
            _playershealth.Clear();
        }

        private void Unturned_OnServerShutdown()
        {
            this.Unload();
            return;
        }

        private void FixedUpdate()
        {
            if (!this.Loaded)
                return;
            //myirc.parse(myirc.Read(), this); // Made a thread instead :(
            if(do_save == true)
            {
                InputText myinputtext = Steam.ConsoleInput.onInputText;

                // Getting response from console
                var stdout = Console.Out;
                string stdoutresponse = "";
                StringWriter tmpstdout = new StringWriter();
                Console.SetOut(tmpstdout);
                myinputtext("save");
                stdoutresponse = tmpstdout.ToString();
                myirc.Say(myirc._channel, "Save response: " + stdoutresponse);
                tmpstdout.Flush();
                Console.SetOut(stdout);
                Console.WriteLine(stdoutresponse);
                RocketChat.Say("[IRC] Server settings, Player items saved!");
                do_save = false;
            }
        }

        private void Unturned_OnPlayerChatted(RocketPlayer player, ref Color color, string message)
        {
            if (!myirc.isConnected) return;
            if (message[0] == '/') return; // Blocking commands from echoing in the channel
            myirc.Say(myirc._channel, "[CHAT] " + player.CharacterName + ": " + message);
        }
        
        private void Unturned_OnPlayerConnected(RocketPlayer player)
        {
            if (!myirc.isConnected) return;
            myirc.Say(myirc._channel, "[CONNECT] " + player.CharacterName + " has connected!");
            _playershealth.Add(player.CharacterName, 100);
        }

        private void Unturned_OnPlayerDisconnected(RocketPlayer player)
        {
            if (!myirc.isConnected) return;
            myirc.Say(myirc._channel, "[DISCONNECT] " + player.CharacterName + " has disconnected!");
            if(_playershealth.ContainsKey(player.CharacterName))
            {
                _playershealth.Remove(player.CharacterName);
            }
        }

        /*private void Unturned_OnPlayerDeath(RocketPlayer player, EDeathCause cause, ELimb limb, Steamworks.CSteamID murderer)
        {
            if (!myirc.isConnected) return;
            //myirc.Send("PRIVMSG " + myirc._channel + " :[DEATH] " + player.CharacterName + " has died! (Cause:"+cause+", Limb:"+limb+", murderer:"+(murderer != null ? RocketPlayer.FromCSteamID(murderer).CharacterName : "Unknown")+")");
            myirc.Say(myirc._channel, "[DEATH] " + player.CharacterName + " has died! (Cause:" + cause + ", Limb:" + limb + ", murderer:" + (murderer != null ? RocketPlayer.FromCSteamID(murderer).CharacterName : "Unknown") + ")");
        }*/

        private void Unturned_OnPlayerUpdateHealth(RocketPlayer player, byte health)
        {
            if (!myirc.isConnected) return;
            if(_playershealth.ContainsKey(player.CharacterName))
            {
                _playershealth[player.CharacterName] = health;
            }
            else
            {
                _playershealth.Add(player.CharacterName, health);
            }
        }

    }

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
                    allow_adminowner = true
                };
            }
        }
    }
}