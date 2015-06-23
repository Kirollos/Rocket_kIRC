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
    public class kIRCCore
    {
        public string _host;
        public int _port;
        public string _nick;
        public string _user;
        public string _realname;
        public string _password;
        public string _channel;
        public char _command_prefix;
        public char _parameter_delimiter;
        public bool Registered = false;
        private TcpClient ircsock;
        private Stream mystream;
        private ASCIIEncoding _encoding;
        public List<string[]> userlist = new List<string[]>();

        public bool allow_adminowner;

        private bool __NAMES = false;

        public bool isConnected = false;

        public List<CPerform> cperform;
        public List<kIRC_Commands> custom_commands;

        public kIRCCore(string host, int port, string nick, string user, string realname, string channel, string password)
        {
            ircsock = new TcpClient();
            ircsock.Connect(host, port);
            mystream = ircsock.GetStream();
            ircsock.NoDelay = true;
            _encoding = new ASCIIEncoding();
            isConnected = true;
            _nick = nick;
            _host = host;
            _port = port;
            _user = user;
            _realname = realname;
            _password = password;
            _channel = channel;
            this.Send("NICK " + this._nick);
            this.Send("USER " + this._user + " - - :" + this._realname);
        }
        public void Destruct()
        {
            try
            {
                this.Send("QUIT :Bye!");
                ircsock.Close();
            }
            catch
            {
            }
            //ircsock.Close();
            isConnected = false;
        }

        public void Send(string data)
        {
            if (!data.Contains("\n"))
                data += "\r\n";
            byte[] _data = _encoding.GetBytes(data);

            mystream.Write(_data, 0, _data.Length);
        }
        public String Read()
        {
            string data = "";
            byte[] _data = new byte[1];
            while (true)
            {
                int k = mystream.Read(_data, 0, 1);
                if (k == 0) return "";
                char kk = Convert.ToChar(_data[0]);
                data += kk;
                if (kk == '\n')
                    break;
            }

            return data;
        }

        public void Say(string target, string text)
        {
            string[] splitted_text = text.Split(new string[] {"\r", "\n"}, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < splitted_text.Length; i++)
                this.Send("PRIVMSG " + target + " :" + /*text*/splitted_text[i]);
            return;
        }

        public void Notice(string target, string text)
        {
            string[] splitted_text = text.Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < splitted_text.Length; i++)
                this.Send("NOTICE " + target + " :" + /*text*/splitted_text[i]);
            return;
        }

        public void parse(string data, kIRC unturnedclass)
        {
            if (data == "") return;
            if(data.Substring(0, 6) == "ERROR ")
            {
                RocketConsole.print("Error: IRC socket has closed. Reload the plugin for reconnection.");
                this.Destruct();
            }
            // Regex taken from (http://calebdelnay.com/blog/2010/11/parsing-the-irc-message-format-as-a-client)
            string
                prefix = "",
                command = "",
                //parameters,
                trailing = ""
                ;
            string[] parameters = new string[] { };
            Regex parsingRegex = new Regex(@"^(:(?<prefix>\S+) )?(?<command>\S+)( (?!:)(?<params>.+?))?( :(?<trail>.+))?$", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
            Match messageMatch = parsingRegex.Match(data);

            if (messageMatch.Success)
            {
                prefix = messageMatch.Groups["prefix"].Value;
                command = messageMatch.Groups["command"].Value;
                parameters = messageMatch.Groups["params"].Value.Split(' ');
                trailing = messageMatch.Groups["trail"].Value;

                if (!String.IsNullOrEmpty(trailing))
                    parameters = parameters.Concat(new string[] { trailing }).ToArray();
            }

            if (command == "PING")
            {
                this.Send("PONG :" + trailing);
                this.Send("NAMES " + this._channel);
            }

            if (command == "001")
            {
                this.Registered = true;
            }
            if (command == "005")
            {
                if(!String.IsNullOrEmpty(this._password))
                    this.Say("NickServ", "IDENTIFY " + this._password);
                this.Send("JOIN " + _channel);
                // Perform
                for(int i = 0; i < this.cperform.Count; i++)
                {
                    this.Send(this.cperform[i].pcommand);
                }
                // -------
            }
            if (command == "353")
            {
                // Names list

                if(!this.__NAMES)
                {
                    this.__NAMES = true;
                    this.userlist.Clear();
                }
                
                string[] _userlist = trailing.Trim().Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < _userlist.Length; i++)
                {
                    string lerank = Convert.ToString(_userlist[i][0]);
                    string lename = _userlist[i].Remove(0, 1);
                    if (lerank == "~" || lerank == "&" || lerank == "@" || lerank == "%" || lerank == "+")
                    {
                        lename = _userlist[i].Remove(0, 1);
                    }
                    else
                    {
                        lename = _userlist[i];
                        lerank = "";
                    }

                    this.userlist.Add(new[] { lename, lerank });
                }
            }
            if(command == "JOIN")
            {
                string user = prefix.Split('!')[0];
                RocketChat.Say("[IRC JOIN] "+ user +" has joined IRC channel.", Color.gray);
                this.userlist.Add(new[] { user, "" });
            }
            else if(command == "PART")
            {
                string user = prefix.Split('!')[0];
                RocketChat.Say("[IRC PART] " + user + " has left IRC channel.", Color.gray);
                
                for(int i = 0; i < this.userlist.Count; i++)
                {
                    if(this.userlist[i][0] == user)
                    {
                        this.userlist.Remove(new[] { user, this.userlist[i][1]});
                        break;
                    }
                }
            }
            if (command == "366")
            { // End of /NAMES
                this.__NAMES = false;
            }
            if (command == "MODE")
            {
                if (String.IsNullOrEmpty(trailing))
                {
                    this.Send("NAMES "+this._channel);
                }
            }
            if (command == "PRIVMSG")
            {
                if (trailing[0] == this._command_prefix)
                {
                    string cmd;
                    try
                    {
                        cmd = trailing.Split(' ')[0].ToLower();
                        if (String.IsNullOrEmpty(cmd.Trim()))
                            cmd = trailing.Trim().ToLower();
                    }
                    catch
                    {
                        cmd = trailing.Trim().ToLower();
                    }
                    string msg = "";
                    string user = prefix.Split('!')[0];
                    cmd = cmd.Remove(0, 1); // remove the prefix pls
                    
                    msg = trailing.Remove(0, 1 + cmd.Length).Trim();
                    cmd = cmd.Trim();

                    if (cmd == "help")
                    {
                        if (!IsVoice(user, false))
                        {
                            this.Say(this._channel, "Error: You need voice to use the commands.");
                            return;
                        }
                        this.Say(this._channel, "====Unturned IRC Commands====");
                        this.Say(this._channel, "- " + this._command_prefix+"help => This command");
                        this.Say(this._channel, "- " + this._command_prefix + "say <text> => Send a message to ingame users");
                        this.Say(this._channel, "- " + this._command_prefix + "players => Shows a list of online players");
                        this.Say(this._channel, "- " + this._command_prefix + "pm => Sends a personal message to a specific player");
                        // Custom commands
                        for(int i = 0; i < this.custom_commands.Count; i++)
                        {
                            if(this.custom_commands[i].FlagNeeded == "v" || this.custom_commands[i].FlagNeeded == "")
                            {
                                this.Say(this._channel, "+ " + this._command_prefix + this.custom_commands[i].BotCommand + this.custom_commands[i].BotSyntax);
                            }
                        }
                        // ---------------
                        if (IsHalfOp(user, false))
                        {
                            this.Say(this._channel, "- " + this._command_prefix + "info <player name> => Show information about given username");
                            this.Say(this._channel, "- " + this._command_prefix + "broadcast <text> => Sends a broadcast to the players");
                            // Custom commands
                            for (int i = 0; i < this.custom_commands.Count; i++)
                            {
                                if (this.custom_commands[i].FlagNeeded == "h")
                                {
                                    this.Say(this._channel, "+ " + this._command_prefix + this.custom_commands[i].BotCommand + this.custom_commands[i].BotSyntax);
                                }
                            }
                            // ---------------
                        }
                        if (IsOp(user, false))
                        {
                            this.Say(this._channel, "- " + this._command_prefix + "kick <player name> <reason> => Kicks a player from the server with a given reason");
                            // Custom commands
                            for (int i = 0; i < this.custom_commands.Count; i++)
                            {
                                if (this.custom_commands[i].FlagNeeded == "o")
                                {
                                    this.Say(this._channel, "+ " + this._command_prefix + this.custom_commands[i].BotCommand + this.custom_commands[i].BotSyntax);
                                }
                            }
                            // ---------------
                        }
                        if (IsAdmin(user, false))
                        {
                            this.Say(this._channel, "- " + this._command_prefix + "ban <player name|SteamID> <duration in seconds> <reason> => Bans a player from the server with a given duration and reason");
                            this.Say(this._channel, "- " + this._command_prefix + "unban <SteamID> => Unbans a player from the server with a given SteamID");
                            this.Say(this._channel, "- " + this._command_prefix + "bans => Shows ban list");
                            this.Say(this._channel, "- " + this._command_prefix + "save => Saves the game data.");
                            this.Say(this._channel, "- " + this._command_prefix + "shutdown => shuts down the server.");
                            // Custom commands
                            for (int i = 0; i < this.custom_commands.Count; i++)
                            {
                                if (this.custom_commands[i].FlagNeeded == "a")
                                {
                                    this.Say(this._channel, "+ " + this._command_prefix + this.custom_commands[i].BotCommand + this.custom_commands[i].BotSyntax);
                                }
                            }
                            // ---------------
                        }
                        if (IsOwner(user, false))
                        {
                            // Custom commands
                            for (int i = 0; i < this.custom_commands.Count; i++)
                            {
                                if (this.custom_commands[i].FlagNeeded == "q")
                                {
                                    this.Say(this._channel, "+ " + this._command_prefix + this.custom_commands[i].BotCommand + this.custom_commands[i].BotSyntax);
                                }
                            }
                            // ---------------
                        }
                        this.Say(this._channel, "=============================");
                    }
                    else if (cmd == "say" && IsVoice(user, false))
                    {
                        if(String.IsNullOrEmpty(msg))
                        {
                            string[] ParamSyntax = { "text" };
                            this.SendSyntax(this._channel, cmd, ParamSyntax);
                            return;
                        }
                        RocketChat.Say("[IRC] " + user + ": " + msg, Color.yellow);
                    }
                    else if (cmd == "players" && IsVoice(user, false))
                    {
                        string playerlist = "";
                        for(int i = 0; i < Steam.Players.Count; i++)
                        {
                            playerlist += Steam.Players[i].SteamPlayerID.CharacterName/* + ", "*/;
                            if (i != (Steam.Players.Count - 1))
                                playerlist += ", ";
                        }
                        this.Say(this._channel, "Connected Players[" + Steam.Players.Count + "/"+Steam.MaxPlayers+"]: " + playerlist);
                    }
                    else if (cmd == "pm" && IsVoice(user, false))
                    {
                        if (msg.Split(this._parameter_delimiter).Length < 2)
                        {
                            string[] ParamSyntax = { "Player Name", "Message" };
                            this.SendSyntax(this._channel, cmd, ParamSyntax);
                            return;
                        }
                        else
                        {
                            string pname = msg.Split(this._parameter_delimiter)[0];
                            string message = msg.Split(this._parameter_delimiter)[1];

                            RocketPlayer pPointer = RocketPlayer.FromName(pname);
                            if (pPointer == null || pPointer.CharacterName != pname)
                                this.Say(this._channel, "[ERROR] Player " + pname + " not found.");
                            else
                            {
                                RocketChat.Say(pPointer, "[IRC PM] " + user + ": " + message, Color.magenta);
                            }
                        }
                    }
                    else if (cmd == "info" && IsHalfOp(user, false))
                    {
                        if (String.IsNullOrEmpty(msg))
                        {
                            string[] ParamSyntax = {"Player Name"};
                            this.SendSyntax(this._channel, cmd, ParamSyntax);
                        }
                        else
                        {
                            string pname = msg.Trim();
                            RocketPlayer pPointer = RocketPlayer.FromName(pname);

                            if (pPointer == null || pPointer.CharacterName != pname)
                            {
                                this.Say(this._channel, "[ERROR] Player " + pname + " not found.");
                                return;
                            }
                            this.Notice(user, "Info about {" + pname + "}");
                            this.Notice(user, "Character name: " + pPointer.CharacterName);
                            this.Notice(user, "Steam name: " + pPointer.SteamName);
                            this.Notice(user, "SteamID: " + pPointer.CSteamID.ToString());
                            //this.Notice(user, "Health: " + pPointer.Health + "%"); // pPointer.Health ALWAYS returns zero for some reason..
                                                                                     // Therefore using another method to update player health
                                                                                     // on each health update event.
                            this.Notice(user, "Health: " + unturnedclass._playershealth[pPointer.CharacterName] + "%");
                            this.Notice(user, "Hunger: " + pPointer.Hunger + "%");
                            this.Notice(user, "Thirst: " + pPointer.Thirst + "%");
                            this.Notice(user, "Infection: " + pPointer.Infection + "%");
                            this.Notice(user, "Stamina: " + pPointer.Stamina + "%");
                            this.Notice(user, "Experience: " + pPointer.Experience);
                            this.Notice(user, "Admin: " + (pPointer.IsAdmin == true ? "Yes" : "No"));
                            this.Notice(user, "Dead: " + (pPointer.Dead == true ? "Yes" : "No"));
                            this.Notice(user, "Godmode: " + (pPointer.Features.GodMode == true ? "Enabled" : "Disabled"));
                            this.Notice(user, "Position: X:" + pPointer.Position.x + ", Y:" + pPointer.Position.y + ", Z:" + pPointer.Position.z);

                        }
                    }
                    else if (cmd == "broadcast" && IsHalfOp(user, false))
                    {
                        if (String.IsNullOrEmpty(msg))
                        {
                            string[] ParamSyntax = { "text" };
                            this.SendSyntax(this._channel, cmd, ParamSyntax);
                            return;
                        }
                        RocketChat.Say("[IRC Broadcast]: " + msg, Color.red);
                    }
                    else if (cmd == "kick" && IsOp(user, false))
                    {
                        if (msg.Split(this._parameter_delimiter).Length < 2)
                        {
                            string[] ParamSyntax = { "Player Name", "Reason" };
                            this.SendSyntax(this._channel, cmd, ParamSyntax);
                        }
                        else
                        {
                            string pname = msg.Split(this._parameter_delimiter)[0];
                            string reason = msg.Split(this._parameter_delimiter)[1];
                            RocketPlayer pPointer = RocketPlayer.FromName(pname);
                            if (pPointer == null || pPointer.CharacterName!=pname)
                                this.Say(this._channel, "[ERROR] Player " + pname + " not found.");
                            else
                            {
                                RocketPlayer.FromName(pname).Kick(reason);
                                this.Say(this._channel, "[SUCCESS] Player " + pname + " is kicked!");
                            }
                        }
                    }
                    else if (cmd == "ban" && IsAdmin(user, false))
                    {
                        if (msg.Split(this._parameter_delimiter).Length < 3)
                        {
                            string[] ParamSyntax = { "Player Name|SteamID", "Duration in seconds", "Reason" };
                            this.SendSyntax(this._channel, cmd, ParamSyntax);
                        }
                        else
                        {
                            string pname = msg.Split(this._parameter_delimiter)[0];
                            string durationstr = msg.Split(this._parameter_delimiter)[1];
                            int duration = 0;
                            if (!int.TryParse(durationstr, out duration))
                            {
                                this.Say(this._channel, "[ERROR] duration \"" + durationstr + "\" is not valid.");
                                return;
                            }
                            
                            string reason = msg.Split(this._parameter_delimiter/*' '*/)[2];
                            if (!Regex.IsMatch(pname, @"^\d+$")) // If not SteamID
                            {
                                RocketPlayer pPointer = RocketPlayer.FromName(pname);
                                if (pPointer == null || pPointer.CharacterName != pname)
                                {
                                    this.Say(this._channel, "[ERROR] Player " + pname + " not found.");
                                    return;
                                }
                                else
                                {
                                    // pPointer.Ban(reason, (uint) duration); // Doesn't work (https://github.com/RocketFoundation/Rocket/issues/173)
                                    this.Say(this._channel, "[SUCCESS] Player " + pname + " is banned!");
                                }
                            }
                            else
                            {
                                this.Say(this._channel, "[SUCCESS] SteamID "+pname+" is banned.");
                            }

                            kIRC_PushCommand pcmd = new kIRC_PushCommand();
                            pcmd.command = "ban {0}/{1}/{2}";
                            pcmd.parameters = new string[] { pname, reason, durationstr };
                            pcmd.onfire(() => { });
                            pcmd.onexec((string response) => { });

                            pcmd.execute = true;

                            pcmd.push(unturnedclass); // Sends it to the main unturned thread

                            /*InputText myinputtext = Steam.ConsoleInput.onInputText;
                            myinputtext("ban " + pname + "/" + reason + "/" + durationstr + "");*/
                        }
                    }
                    else if (cmd == "unban" && IsAdmin(user, false))
                    {
                        if(String.IsNullOrEmpty(msg))
                        {
                            string[] ParamSyntax = { "SteamID" };
                            this.SendSyntax(this._channel, cmd, ParamSyntax);
                            return;
                        }
                        else
                        {

                            kIRC_PushCommand pcmd = new kIRC_PushCommand();
                            pcmd.command = "unban {0}";
                            pcmd.parameters = new string[] { msg };
                            pcmd.onfire(() => { } );
                            pcmd.onexec((string response) => {
                                this.Say(this._channel, "Unban response: " + response);
                            });

                            pcmd.execute = true;

                            pcmd.push(unturnedclass); // Sends it to the main unturned thread

                            /*InputText myinputtext = Steam.ConsoleInput.onInputText;
                            
                            // Getting response from console
                            var stdout = Console.Out;
                            string stdoutresponse = "";
                            StringWriter tmpstdout = new StringWriter();
                            Console.SetOut(tmpstdout);
                            myinputtext("unban " + msg + "");
                            stdoutresponse = tmpstdout.ToString();
                            this.Say(this._channel, "Unban response: " + stdoutresponse);
                            tmpstdout.Flush();
                            Console.SetOut(stdout);
                            Console.WriteLine(stdoutresponse);*/
                        }
                    }
                    else if (cmd == "bans" && IsAdmin(user, false))
                    {
                        kIRC_PushCommand pcmd = new kIRC_PushCommand();
                        pcmd.command = "bans";
                        pcmd.parameters = new string[] { };
                        pcmd.onfire(() => 
                        {
                            this.Say(this._channel, user + ": Response is sent to your query.");
                        });
                        pcmd.onexec((string response) =>
                        {
                            this.Say(user, "Response from bans:");
                            string[] bans = response.Split('\n');
                            for (int i = 0; i < bans.Length; i++)
                            {
                                this.Say(user, bans[i]);
                            }
                        });

                        pcmd.execute = true;

                        pcmd.push(unturnedclass); // Sends it to the main unturned thread

                        /*this.Say(this._channel, user+": Response is sent to your query.");
                        InputText myinputtext = Steam.ConsoleInput.onInputText;

                        // Getting response from console
                        var stdout = Console.Out;
                        string stdoutresponse = "";
                        StringWriter tmpstdout = new StringWriter();
                        Console.SetOut(tmpstdout);
                        myinputtext("bans");
                        stdoutresponse = tmpstdout.ToString();
                        this.Say(user, "Response from bans:");
                        string[] bans = stdoutresponse.Split('\n');
                        for (int i = 0; i < bans.Length; i++)
                        {
                            this.Say(user, bans[i]);
                        }
                        tmpstdout.Flush();
                        Console.SetOut(stdout);
                        Console.WriteLine(stdoutresponse);*/
                    }
                    else if (cmd == "save" && IsAdmin(user, false))
                    {
                        //unturnedclass.do_save = true;

                        kIRC_PushCommand pcmd = new kIRC_PushCommand();
                        pcmd.command = "save";
                        pcmd.parameters = new string[] { };
                        pcmd.onfire(() => {
                            this.Say(this._channel, "Saving server settings...");
                            RocketChat.Say("[IRC] Saving server settings...");
                        });
                        pcmd.onexec((string response) => {
                            this.Say(this._channel, "Save response: " + response);
                            RocketChat.Say("[IRC] Server settings, Player items saved!");
                        });

                        pcmd.execute = true;

                        pcmd.push(unturnedclass); // Sends it to the main unturned thread

                        /*InputText myinputtext = Steam.ConsoleInput.onInputText;

                        // Getting response from console
                        var stdout = Console.Out;
                        string stdoutresponse = "";
                        StringWriter tmpstdout = new StringWriter();
                        Console.SetOut(tmpstdout);
                        myinputtext("save");
                        stdoutresponse = tmpstdout.ToString();
                        this.Say(this._channel, "Save response: " + stdoutresponse);
                        tmpstdout.Flush();
                        Console.SetOut(stdout);
                        Console.WriteLine(stdoutresponse);
                        RocketChat.Say("[IRC] Server settings, Player items saved!");*/
                    }
                    else if (cmd == "shutdown" && IsAdmin(user, false))
                    {
                        kIRC_PushCommand pcmd = new kIRC_PushCommand();
                        pcmd.command = "save";
                        pcmd.parameters = new string[] { };
                        pcmd.onfire(() =>
                        {
                            RocketChat.Say("[IRC WARNING]: SERVER IS SHUTTING DOWN IN 5 SECONDS!", Color.red);
                            this.Say(this._channel, "Shutting down in 5 seconds");
                            Thread.Sleep(1000);
                        });
                        pcmd.onexec((string response) =>
                        {
                            RocketChat.Say("[IRC] Server settings, Player items saved!");

                            kIRC_PushCommand pcmd2 = new kIRC_PushCommand();
                            pcmd2.command = "shutdown";
                            pcmd2.parameters = new string[] { };

                            pcmd2.onfire(() => 
                            {
                                RocketChat.Say("[IRC WARNING]: SERVER IS SHUTTING DOWN IN 4 SECONDS!", Color.red);
                                this.Say(this._channel, "Shutting down in 4 seconds");
                                Thread.Sleep(1000);
                                RocketChat.Say("[IRC WARNING]: SERVER IS SHUTTING DOWN IN 3 SECONDS!", Color.red);
                                this.Say(this._channel, "Shutting down in 3 seconds");
                                Thread.Sleep(1000);
                                RocketChat.Say("[IRC WARNING]: SERVER IS SHUTTING DOWN IN 2 SECONDS!", Color.red);
                                this.Say(this._channel, "Shutting down in 2 seconds");
                                Thread.Sleep(1000);
                                RocketChat.Say("[IRC WARNING]: SERVER IS SHUTTING DOWN IN 1 SECOND!", Color.red);
                                this.Say(this._channel, "Shutting down in 1 second");
                                Thread.Sleep(1000);
                            });

                            pcmd2.onexec((string response2) => { });

                            pcmd2.execute = true;
                            pcmd2.push(unturnedclass);

                        });

                        pcmd.execute = true;

                        pcmd.push(unturnedclass); // Sends it to the main unturned thread



                        /*InputText myinputtext = Steam.ConsoleInput.onInputText;


                        RocketChat.Say("[IRC WARNING]: SERVER IS SHUTTING DOWN IN 5 SECONDS!", Color.red);
                        this.Say(this._channel, "Shutting down in 5 seconds");
                        Thread.Sleep(1000);
                        myinputtext("save");
                        RocketChat.Say("[IRC] Server settings, Player items saved!");
                        RocketChat.Say("[IRC WARNING]: SERVER IS SHUTTING DOWN IN 4 SECONDS!", Color.red);
                        this.Say(this._channel, "Shutting down in 4 seconds");
                        Thread.Sleep(1000);
                        RocketChat.Say("[IRC WARNING]: SERVER IS SHUTTING DOWN IN 3 SECONDS!", Color.red);
                        this.Say(this._channel, "Shutting down in 3 seconds");
                        Thread.Sleep(1000);
                        RocketChat.Say("[IRC WARNING]: SERVER IS SHUTTING DOWN IN 2 SECONDS!", Color.red);
                        this.Say(this._channel, "Shutting down in 2 seconds");
                        Thread.Sleep(1000);
                        RocketChat.Say("[IRC WARNING]: SERVER IS SHUTTING DOWN IN 1 SECOND!", Color.red);
                        this.Say(this._channel, "Shutting down in 1 second");
                        Thread.Sleep(1000);

                        myinputtext("shutdown");*/
                    }
                    else
                    {
                        for(int i = 0; i < this.custom_commands.Count; i++)
                        {
                            kIRC_Commands refer = this.custom_commands[i];
                            
                            if(refer.BotCommand.ToLower().Trim() == cmd)
                            {
                                bool hasperms = false;
                                if (refer.FlagNeeded == "q")
                                    hasperms = IsOwner(user);
                                else if (refer.FlagNeeded == "a")
                                    hasperms = IsAdmin(user);
                                else if (refer.FlagNeeded == "o")
                                    hasperms = IsOp(user);
                                else if (refer.FlagNeeded == "h")
                                    hasperms = IsHalfOp(user);
                                else if (refer.FlagNeeded == "v")
                                    hasperms = IsVoice(user);
                                else
                                    hasperms = false;

                                if (hasperms)
                                {
                                    kIRC_PushCommand pcmd = new kIRC_PushCommand();
                                    pcmd.command = refer.ConsoleCommand;
                                    pcmd.parameters = (!String.IsNullOrEmpty(msg) && msg.Split(this._parameter_delimiter).Length>0) ? msg.Split(this._parameter_delimiter) : new string[] { };
                                    pcmd.extradata.Add("Syntax", refer.BotSyntax);
                                    pcmd.onfire(() =>
                                    {
                                        if (!String.IsNullOrEmpty(refer.IRCmsg_onfire.Trim()))
                                        {
                                            this.Say(this._channel, refer.IRCmsg_onfire);
                                        }
                                        if (!String.IsNullOrEmpty(refer.GAMEmsg_onfire.Trim()))
                                        {
                                            RocketChat.Say(refer.GAMEmsg_onfire);
                                        }
                                    });
                                    pcmd.onexec((string response) =>
                                    {
                                        if (!String.IsNullOrEmpty(refer.IRCmsg_onexec.Trim()))
                                        {
                                            this.Say(this._channel, refer.IRCmsg_onexec);
                                        }
                                        if (!String.IsNullOrEmpty(refer.GAMEmsg_onexec.Trim()))
                                        {
                                            RocketChat.Say(refer.GAMEmsg_onexec);
                                        }

                                        if(refer.printresponse == true)
                                            this.Say(this._channel, "Response from ("+cmd+"): "+response);
                                    });

                                    pcmd.execute = true;

                                    pcmd.push(unturnedclass); // Sends it to the main unturned thread

                                }

                                break;
                            }
                        }
                    }
                }
            }
        }

        public bool IsOwner(string name, bool actualrank = true)
        {
            for (int i = 0; i < this.userlist.Count; i++)
            {
                if (this.userlist[i][0] == name)
                {
                    if (this.allow_adminowner || actualrank)
                        return userlist[i][1] == "~";
                    else
                        return userlist[i][1] == "@";
                }
            }
            return false;
        }

        public bool IsAdmin(string name, bool actualrank = true)
        {
            for (int i = 0; i < this.userlist.Count; i++)
            {
                if (this.userlist[i][0] == name)
                {
                    if (this.allow_adminowner || actualrank)
                        return userlist[i][1] == "&" || userlist[i][1] == "~";
                    else
                        return userlist[i][1] == "@";
                }
            }
            return false;
        }

        public bool IsOp(string name, bool actualrank = true)
        {
            for (int i = 0; i < this.userlist.Count; i++)
            {
                if (this.userlist[i][0] == name)
                {
                    if (this.allow_adminowner || actualrank)
                        return userlist[i][1] == "@" || userlist[i][1] == "&" || userlist[i][1] == "~";
                    else
                        return userlist[i][1] == "%" || userlist[i][1] == "@";
                }
            }
            return false;
        }

        public bool IsHalfOp(string name, bool actualrank = true)
        {
            for (int i = 0; i < this.userlist.Count; i++)
            {
                if (this.userlist[i][0] == name)
                {
                    if (this.allow_adminowner || actualrank)
                        return userlist[i][1] == "%" || userlist[i][1] == "@" || userlist[i][1] == "&" || userlist[i][1] == "~";
                    else
                        return userlist[i][1] == "%" || userlist[i][1] == "@";
                }
            }
            return false;
        }

        public bool IsVoice(string name, bool actualrank = true)
        {
            for (int i = 0; i < this.userlist.Count; i++)
            {
                if (this.userlist[i][0] == name)
                {
                    if (this.allow_adminowner || actualrank)
                        return userlist[i][1] == "+" || userlist[i][1] == "%" || userlist[i][1] == "@" || userlist[i][1] == "&" || userlist[i][1] == "~";
                    else
                        return userlist[i][1] == "+" || userlist[i][1] == "%" || userlist[i][1] == "@";
                }
            }
            return false;
        }

        public void SetCommandPrefix(char prefix)
        {
            this._command_prefix = prefix;
        }

        public void SetParameterDelimiter(char delimiter)
        {
            this._parameter_delimiter = delimiter;
        }

        public void SetAllowAdminOwner(bool allow)
        {
            this.allow_adminowner = allow;
        }

        public void SetCustomCommands(List<kIRC_Commands> cc)
        {
            this.custom_commands = cc;
        }

        public void SendSyntax(string channel, string command, string[] parameters)
        {
            string _parameters = "";
            for(int i = 0; i < parameters.Length; i++)
            {
                _parameters += "<" + parameters[i] + ">";
                if (i != (parameters.Length - 1))
                    _parameters += this._parameter_delimiter;
            }

            this.Say(channel, "Syntax: " + this._command_prefix + command + " " + _parameters);
            return;
        }

        public void loopparsing(kIRC uclass)
        {
            while (isConnected)
            {
                this.parse(this.Read(), uclass);
            }
        }
    }
}
