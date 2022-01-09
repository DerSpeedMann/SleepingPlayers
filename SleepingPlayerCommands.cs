using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SpeedMann.SleepingPlayers
{
    class SleepingPlayerCommands : IRocketCommand
    {
        public string Help
        {
            get { return "SleepingPlayer"; }
        }

        public string Name
        {
            get { return "SleepingPlayer"; }
        }

        public string Syntax
        {
            get { return "<SleepingPlayer>"; }
        }

        public List<string> Aliases
        {
            get { return new List<string>(); }
        }

        public AllowedCaller AllowedCaller
        {
            get { return AllowedCaller.Player; }
        }

        public List<string> Permissions
        {
            get
            {
                return new List<string>() { "SleepingPlayer" };
            }
        }

        public void Execute(IRocketPlayer caller, string[] command)
        {
            UnturnedPlayer player = (UnturnedPlayer)caller;
            if (command.Length < 1)
            {
                UnturnedChat.Say(caller, "Invalid!", UnityEngine.Color.red);
                return;
            }
            else
            {
                switch (command[0].ToLower())
                {
                    case "sleep":
                        SleepingPlayers.Inst.spawnSleepingPlayer(player);
                        break;
                    case "wakeup":
                        bool needsNewSpawnpoint = true;
                        EPlayerStance stance = EPlayerStance.STAND;

                        if(SleepingPlayers.Inst.tryGetItemsFromSleepingPlayer(player.CSteamID, player.Position, ref needsNewSpawnpoint, ref stance) != null)
                            UnturnedChat.Say(caller, "Found and removed your SleepingPlayer", UnityEngine.Color.green);
                        else
                            UnturnedChat.Say(caller, "Could not find your SleepingPlayer", UnityEngine.Color.red);
                        break;
                    default:
                        UnturnedChat.Say(caller, "Invalid Command parameters", UnityEngine.Color.red);
                        throw new WrongUsageOfCommandException(caller, this);
                }
            }
        }
    }
}
