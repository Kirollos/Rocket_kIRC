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
    public class kIRC : RocketPlugin<kIRCConfig>
    {
        public static kIRC dis;
        public kIRCCore myirc;
        Thread ircthread;

        public List<kIRC_PushCommand> do_command;

        protected override void Load()
        {
            dis = this;
            this.do_command = new List<kIRC_PushCommand>();
            if(this.Configuration.Instance.server == "EDITME")
            {
                Logger.LogError("kIRC Error: You did not configure the plugin! Unloading now...");
                this.Unload();
                return;
            }
            if(this.Configuration.Instance.parameter_delimiter == " ")
            {
                Logger.LogWarning("kIRC Warning: parameter delimiter cannot be \" \"! Therefore, setting it to \"/\"");
            }
            myirc = new kIRCCore(this.Configuration.Instance.server, this.Configuration.Instance.port, this.Configuration.Instance.nick, this.Configuration.Instance.user, this.Configuration.Instance.realname, this.Configuration.Instance.channel, this.Configuration.Instance.password, this.Configuration.Instance.spassword);
            
            // Command Prefix
            if (!String.IsNullOrEmpty(this.Configuration.Instance.command_prefix))
            {
                if (this.Configuration.Instance.command_prefix.Length == 1)
                {
                    myirc.SetCommandPrefix(System.Convert.ToChar(this.Configuration.Instance.command_prefix));
                }
                else
                {
                    Logger.LogWarning("kIRC Warning: command_prefix is not a character! Setting default to '!'");
                    this.Configuration.Instance.command_prefix = "!";
                    myirc.SetCommandPrefix('!');
                }
            }
            else
            {
                Logger.LogWarning("kIRC Warning: command_prefix is not set! Setting default to '!'");
                this.Configuration.Instance.command_prefix = "!";
                myirc.SetCommandPrefix('!');
            }

            // Parameter Delimiter
            if (!String.IsNullOrEmpty(this.Configuration.Instance.parameter_delimiter))
            {

                if (this.Configuration.Instance.parameter_delimiter.Length == 1)
                {
                    if (this.Configuration.Instance.parameter_delimiter == " ")
                    {
                        Logger.LogWarning("kIRC Warning: parameter delimiter cannot be \" \"! Therefore, setting it to \"/\"");
                        this.Configuration.Instance.parameter_delimiter = "/";
                        myirc.SetParameterDelimiter('/');
                    }
                    else
                        myirc.SetParameterDelimiter(System.Convert.ToChar(this.Configuration.Instance.parameter_delimiter));
                }
                else
                {
                    Logger.LogWarning("kIRC Warning: parameter_delimiter is not set! Setting default to \"/\"");
                    this.Configuration.Instance.parameter_delimiter = "/";
                    myirc.SetParameterDelimiter('/');
                }
            }
            else
            {
                Logger.LogWarning("kIRC Warning: parameter_delimiter is not set! Setting default to \"/\"");
                this.Configuration.Instance.parameter_delimiter = "/";
                myirc.SetParameterDelimiter('/');
            }

            myirc.SetAllowAdminOwner(this.Configuration.Instance.allow_adminowner);
            myirc.cperform = this.Configuration.Instance.perform;
            myirc.SetCustomCommands(this.Configuration.Instance.ccommands);

            ircthread = new Thread( () => myirc.loopparsing(this));
            ircthread.Start();

            U.Events.OnShutdown += Unturned_OnServerShutdown;
            Rocket.Unturned.Events.UnturnedPlayerEvents.OnPlayerChatted += Unturned_OnPlayerChatted;
            U.Events.OnPlayerConnected += Unturned_OnPlayerConnected;
            U.Events.OnPlayerDisconnected += Unturned_OnPlayerDisconnected;
            Rocket.Unturned.Events.UnturnedPlayerEvents.OnPlayerDeath += Unturned_OnPlayerDeath;

            Logger.Log("kIRC Loaded! Version: " + kIRCVersionChecker.VERSION);

            kIRCVersionChecker.CheckUpdate();

        }

        protected override void Unload()
        {
            do_command.Clear();
            myirc.Destruct();
            if(ircthread.IsAlive)
                ircthread.Abort();
            // Rocket unload/reload does not clear this anyway...
            U.Events.OnShutdown -= Unturned_OnServerShutdown;
            Rocket.Unturned.Events.UnturnedPlayerEvents.OnPlayerChatted -= Unturned_OnPlayerChatted;
            U.Events.OnPlayerConnected -= Unturned_OnPlayerConnected;
            U.Events.OnPlayerDisconnected -= Unturned_OnPlayerDisconnected;
            Rocket.Unturned.Events.UnturnedPlayerEvents.OnPlayerDeath -= Unturned_OnPlayerDeath;
        }

        private void Unturned_OnServerShutdown()
        {
            this.Unload();
            return;
        }

        private void FixedUpdate()
        {
            if (this.State != PluginState.Loaded)
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

        private void Unturned_OnPlayerChatted(UnturnedPlayer player, ref Color color, string message, EChatMode chatMode)
        {
            if (!myirc.isConnected) return;
            if (message[0] == '/') return; // Blocking commands from echoing in the channel
            //myirc.Say(myirc._channel, "[CHAT] " + player.CharacterName + ": " + message);
            if (chatMode != EChatMode.GLOBAL) return;
            kIRCTranslate.IRC_SayTranslation(myirc, myirc._channel, "irc_onchat", new Dictionary<string, string>()
            {
                {"playername", player.CharacterName},
                {"steamid", player.CSteamID.ToString()},
                {"message", message}
            });
        }
        
        private void Unturned_OnPlayerConnected(UnturnedPlayer player)
        {
            if (!myirc.isConnected) return;
            //myirc.Say(myirc._channel, "[CONNECT] " + player.CharacterName + " has connected!");
            kIRCTranslate.IRC_SayTranslation(myirc, myirc._channel, "irc_onplayerconnect", new Dictionary<string, string>()
            {
                {"playername", player.CharacterName},
                {"steamid", player.CSteamID.ToString()},
                {"time", DateTime.Now.ToString()}
            });
        }

        private void Unturned_OnPlayerDisconnected(UnturnedPlayer player)
        {
            if (!myirc.isConnected) return;
            //myirc.Say(myirc._channel, "[DISCONNECT] " + player.CharacterName + " has disconnected!");
            kIRCTranslate.IRC_SayTranslation(myirc, myirc._channel, "irc_onplayerdisconnect", new Dictionary<string, string>()
            {
                {"playername", player.CharacterName},
                {"steamid", player.CSteamID.ToString()},
                {"time", DateTime.Now.ToString()}
            });
        }

        private void Unturned_OnPlayerDeath(UnturnedPlayer player, EDeathCause cause, ELimb limb, Steamworks.CSteamID murderer)
        {
            if (!myirc.isConnected) return;
            if (Configuration.Instance.deathevent.show)
            {
                if(UnturnedPlayer.FromCSteamID(murderer) == null)
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
                                {"killername", UnturnedPlayer.FromCSteamID(murderer).CharacterName},
                                {"killersteamid", murderer.ToString()}
                            });
                            break;
                        case EDeathCause.SUICIDE:
                            if (this.Configuration.Instance.deathevent.suicides)
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

        /*
         * Any translation key that starts with 'game' goes to game chat,
         * while any translation key that starts with 'irc' goes to the 
         * main IRC channel.
         * Placeholders can be found in the repository wiki.
         */

        public override TranslationList DefaultTranslations
        {
            get
            {
                return new TranslationList()
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