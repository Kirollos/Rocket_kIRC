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
    public class kIRC : RocketPlugin<kIRCConfig>
    {
        public static kIRC dis;
        public kIRCCore myirc;
        Thread ircthread;

        public Dictionary<string, byte> _playershealth = new Dictionary<string, byte>();

        public List<kIRC_PushCommand> do_command;

        protected override void Load()
        {
            dis = this;
            this.do_command = new List<kIRC_PushCommand>();
            if(this.Configuration.server == "EDITME")
            {
                Rocket.Unturned.Logging.Logger.LogError("kIRC Error: You did not configure the plugin! Unloading now...");
                this.Unload();
                return;
            }
            if(this.Configuration.parameter_delimiter == " ")
            {
                Rocket.Unturned.Logging.Logger.LogWarning("kIRC Warning: parameter delimiter cannot be \" \"! Therefore, setting it to \"/\"");
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
                    Rocket.Unturned.Logging.Logger.LogWarning("kIRC Warning: command_prefix is not a character! Setting default to '!'");
                    this.Configuration.command_prefix = "!";
                    myirc.SetCommandPrefix('!');
                }
            }
            else
            {
                Rocket.Unturned.Logging.Logger.LogWarning("kIRC Warning: command_prefix is not set! Setting default to '!'");
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
                        Rocket.Unturned.Logging.Logger.LogWarning("kIRC Warning: parameter delimiter cannot be \" \"! Therefore, setting it to \"/\"");
                        this.Configuration.parameter_delimiter = "/";
                        myirc.SetParameterDelimiter('/');
                    }
                    else
                        myirc.SetParameterDelimiter(System.Convert.ToChar(this.Configuration.parameter_delimiter));
                }
                else
                {
                    Rocket.Unturned.Logging.Logger.LogWarning("kIRC Warning: parameter_delimiter is not set! Setting default to \"/\"");
                    this.Configuration.parameter_delimiter = "/";
                    myirc.SetParameterDelimiter('/');
                }
            }
            else
            {
                Rocket.Unturned.Logging.Logger.LogWarning("kIRC Warning: parameter_delimiter is not set! Setting default to \"/\"");
                this.Configuration.parameter_delimiter = "/";
                myirc.SetParameterDelimiter('/');
            }

            myirc.SetAllowAdminOwner(this.Configuration.allow_adminowner);
            myirc.cperform = this.Configuration.perform;
            myirc.SetCustomCommands(this.Configuration.ccommands);

            ircthread = new Thread( () => myirc.loopparsing(this));
            ircthread.Start();

            Rocket.Unturned.Events.RocketServerEvents.OnServerShutdown += Unturned_OnServerShutdown;
            Rocket.Unturned.Events.RocketPlayerEvents.OnPlayerChatted += Unturned_OnPlayerChatted;
            Rocket.Unturned.Events.RocketServerEvents.OnPlayerConnected += Unturned_OnPlayerConnected;
            Rocket.Unturned.Events.RocketServerEvents.OnPlayerDisconnected += Unturned_OnPlayerDisconnected;
            Rocket.Unturned.Events.RocketPlayerEvents.OnPlayerDeath += Unturned_OnPlayerDeath;
            Rocket.Unturned.Events.RocketPlayerEvents.OnPlayerUpdateHealth += Unturned_OnPlayerUpdateHealth;

            Rocket.Unturned.Logging.Logger.Log("kIRC Loaded! Version: " + kIRCVersionChecker.VERSION);

            kIRCVersionChecker.CheckUpdate();

        }

        protected override void Unload()
        {
            do_command.Clear();
            myirc.Destruct();
            if(ircthread.IsAlive)
                ircthread.Abort();
            // Rocket unload/reload does not clear this anyway...
            Rocket.Unturned.Events.RocketServerEvents.OnServerShutdown -= Unturned_OnServerShutdown;
            Rocket.Unturned.Events.RocketPlayerEvents.OnPlayerChatted -= Unturned_OnPlayerChatted;
            Rocket.Unturned.Events.RocketServerEvents.OnPlayerConnected -= Unturned_OnPlayerConnected;
            Rocket.Unturned.Events.RocketServerEvents.OnPlayerDisconnected -= Unturned_OnPlayerDisconnected;
            Rocket.Unturned.Events.RocketPlayerEvents.OnPlayerDeath -= Unturned_OnPlayerDeath;
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

            /*if (kIRCVersionChecker.lastchecked.AddHours(1) < DateTime.Now)
            {
                kIRCVersionChecker.CheckUpdate();
            }*/
            
            if(this.do_command.Count > 0)
            {
                for(int i = 0; i < this.do_command.Count; i++)
                {
                    kIRC_PushCommand reff = this.do_command[i];

                    if (!reff.execute)
                        continue;

                    InputText myinputtext = Steam.ConsoleInput.onInputText;

                    reff.onfireev();
                    // Getting response from console
                    var stdout = Console.Out;
                    string stdoutresponse = "";
                    StringWriter tmpstdout = new StringWriter();
                    Console.SetOut(tmpstdout);
                    if (reff.parameters.Length > 0)
                    {
                        try
                        {
                            myinputtext(String.Format(reff.command, reff.parameters));
                        }
                        catch (Exception)
                        {
                            myirc.Say(myirc._channel, "Error: Parameters do not match!\nSyntax: "+myirc._command_prefix+reff.command.Split(new string[]{" "}, StringSplitOptions.RemoveEmptyEntries)[0]+" "+reff.extradata["Syntax"]);
                        }
                        //myinputtext(String.Format(reff.command, reff.parameters));
                    }
                    else
                        myinputtext(reff.command);

                    stdoutresponse = tmpstdout.ToString();
                    tmpstdout.Flush();
                    Console.SetOut(stdout);
                    Console.WriteLine(stdoutresponse);

                    reff.onexecev(stdoutresponse);
                    this.do_command.Remove(this.do_command[i]);
                    break;
                }
            }
        }

        private void Unturned_OnPlayerChatted(RocketPlayer player, ref Color color, string message)
        {
            if (!myirc.isConnected) return;
            if (message[0] == '/') return; // Blocking commands from echoing in the channel
            //myirc.Say(myirc._channel, "[CHAT] " + player.CharacterName + ": " + message);
            kIRCTranslate.IRC_SayTranslation(myirc, myirc._channel, "irc_onchat", new Dictionary<string, string>()
            {
                {"playername", player.CharacterName},
                {"steamid", player.CSteamID.ToString()},
                {"message", message}
            });
        }
        
        private void Unturned_OnPlayerConnected(RocketPlayer player)
        {
            if (!myirc.isConnected) return;
            //myirc.Say(myirc._channel, "[CONNECT] " + player.CharacterName + " has connected!");
            kIRCTranslate.IRC_SayTranslation(myirc, myirc._channel, "irc_onplayerconnect", new Dictionary<string, string>()
            {
                {"playername", player.CharacterName},
                {"steamid", player.CSteamID.ToString()},
                {"time", DateTime.Now.ToString()}
            });
            _playershealth.Add(player.CharacterName, 100);
        }

        private void Unturned_OnPlayerDisconnected(RocketPlayer player)
        {
            if (!myirc.isConnected) return;
            //myirc.Say(myirc._channel, "[DISCONNECT] " + player.CharacterName + " has disconnected!");
            kIRCTranslate.IRC_SayTranslation(myirc, myirc._channel, "irc_onplayerdisconnect", new Dictionary<string, string>()
            {
                {"playername", player.CharacterName},
                {"steamid", player.CSteamID.ToString()},
                {"time", DateTime.Now.ToString()}
            });
            if(_playershealth.ContainsKey(player.CharacterName))
            {
                _playershealth.Remove(player.CharacterName);
            }
        }

        private void Unturned_OnPlayerDeath(RocketPlayer player, EDeathCause cause, ELimb limb, Steamworks.CSteamID murderer)
        {
            if (!myirc.isConnected) return;
            if (Configuration.deathevent.show)
            {
                if(RocketPlayer.FromCSteamID(murderer) == null)
                    //myirc.Say(myirc._channel, "[DEATH] " + player.CharacterName + " has died.");
                    kIRCTranslate.IRC_SayTranslation(myirc, myirc._channel, "irc_onplayerdeath_unknown", new Dictionary<string, string>()
                    {
                        {"playername", player.CharacterName},
                        {"steamid", player.CSteamID.ToString()},
                        {"time", DateTime.Now.ToString()}
                    });
                else
                {
                    switch(cause)
                    {
                        case EDeathCause.GUN:
                        case EDeathCause.KILL:
                        case EDeathCause.ROADKILL:
                        case EDeathCause.PUNCH:
                        case EDeathCause.MELEE:
                            //myirc.Say(myirc._channel, "[DEATH] " + player.CharacterName + " has been killed by " + RocketPlayer.FromCSteamID(murderer).CharacterName + ". (Cause:" + cause + ", Limb:" + limb + ")");
                            kIRCTranslate.IRC_SayTranslation(myirc, myirc._channel, "irc_onplayerdeath_killed", new Dictionary<string, string>()
                            {
                                {"playername", player.CharacterName},
                                {"steamid", player.CSteamID.ToString()},
                                {"time", DateTime.Now.ToString()},
                                {"cause", cause.ToString()},
                                {"limb", limb.ToString()},
                                {"killername", RocketPlayer.FromCSteamID(murderer).CharacterName},
                                {"killersteamid", murderer.ToString()}
                            });
                            break;
                        case EDeathCause.SUICIDE:
                            if(this.Configuration.deathevent.suicides)
                                //myirc.Say(myirc._channel, "[DEATH] " + player.CharacterName + " has suicided.");
                                kIRCTranslate.IRC_SayTranslation(myirc, myirc._channel, "irc_onplayerdeath_suicided", new Dictionary<string, string>()
                                {
                                    {"playername", player.CharacterName},
                                    {"steamid", player.CSteamID.ToString()},
                                    {"time", DateTime.Now.ToString()}
                                });
                            break;
                        case EDeathCause.ZOMBIE:
                            //myirc.Say(myirc._channel, "[DEATH] " + player.CharacterName + " has been killed by a zombie.");
                            kIRCTranslate.IRC_SayTranslation(myirc, myirc._channel, "irc_onplayerdeath_byzombie", new Dictionary<string, string>()
                            {
                                {"playername", player.CharacterName},
                                {"steamid", player.CSteamID.ToString()},
                                {"time", DateTime.Now.ToString()}
                            });
                            break;
                        default:
                            //myirc.Say(myirc._channel, "[DEATH] " + player.CharacterName + " has died (Cause: "+cause+").");
                            kIRCTranslate.IRC_SayTranslation(myirc, myirc._channel, "irc_onplayerdeath_other", new Dictionary<string, string>()
                            {
                                {"playername", player.CharacterName},
                                {"steamid", player.CSteamID.ToString()},
                                {"time", DateTime.Now.ToString()},
                                {"cause", cause.ToString()}
                            });
                            break;
                    }
                }
                    //myirc.Say(myirc._channel, "[DEATH] " + player.CharacterName + " has been killed by "+RocketPlayer.FromCSteamID(murderer).CharacterName+". (Cause:" + cause + ", Limb:" + limb + ")");
            }
        }

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

        /*
         * Any translation key that starts with 'game' goes to game chat,
         * while any translation key that starts with 'irc' goes to the 
         * main IRC channel.
         * Placeholders can be found in the repository wiki.
         */

        public override Dictionary<string, string> DefaultTranslations
        {
            get
            {
                return new Dictionary<string, string>()
                {
                    {"game_ircjoin","[IRC JOIN] {irc_usernick} has joined IRC channel."},
                    {"game_ircpart","[IRC PART] {irc_usernick} has parted IRC channel."},
                    {"game_ircsay","[IRC] {irc_usernick}: {irc_message}"},
                    {"irc_playerslist","Connected Players[{players_amount}/{players_max}]: {players_list}"},
                    {"game_ircpm","[IRC PM] {irc_usernick}: {irc_message}"},
                    {"game_ircbroadcast","[IRC Broadcast]: {irc_message}"},
                    {"irc_kicksuccess", "[SUCCESS] Player {irc_targetnick} is kicked!"},
                    {"irc_pbansuccess", "[SUCCESS] Player {irc_targetnick} is banned!"},
                    {"irc_sbansuccess", "[SUCCESS] SteamID {irc_targetsteamid} is banned."},
                    {"irc_unbanresponse", "Unban response: {irc_response}"},
                    {"irc_onsave", "Saving server settings..."},
                    {"game_onsave", "[IRC] Saving server settings..."},
                    {"irc_saveexec", "Save response: {irc_response}"},
                    {"game_saveexec", "[IRC] Server settings, Player items saved!"},
                    {"game_shutdownwarning", "[IRC WARNING]: SERVER IS SHUTTING DOWN IN {shutdown_secs} SECONDS!"},
                    {"irc_shutdownwarning", "Shutting down in {shutdown_secs} seconds"},
                    {"irc_onchat", "[CHAT] {playername}: {message}"},
                    {"irc_onplayerconnect", "[CONNECT] {playername} has connected!"},
                    {"irc_onplayerdisconnect", "[DISCONNECT] {playername} has disconnected!"},
                    {"irc_onplayerdeath_unknown", "[DEATH] {playername} has died."},
                    {"irc_onplayerdeath_killed", "[DEATH] {playername} has been killed by {killername}. (Cause:{cause}, Limb:{limb})"},
                    {"irc_onplayerdeath_suicided", "[DEATH] {playername} has suicided."},
                    {"irc_onplayerdeath_byzombie", "[DEATH] {playername} has been killed by a zombie."},
                    {"irc_onplayerdeath_other", "[DEATH] {playername} has died (Cause: {cause})."}
                };
            }
        }

    }
}