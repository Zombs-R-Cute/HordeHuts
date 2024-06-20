using System.Collections.Generic;
using Rocket.API;
using Rocket.Unturned.Chat;
using UnityEngine;

namespace Zombs_R_Cute_Horde_Huts
{
    public class CommandSetHutDoor:IRocketCommand
    {
        public void Execute(IRocketPlayer caller, string[] command)
        {
            HordeHuts.SetCommandRun = !HordeHuts.SetCommandRun;

            UnturnedChat.Say(caller, HordeHuts.SetCommandRun?"Selecting door":"Selecting canceled", Color.yellow);
        }

        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "sethutdoor";
        public string Help => "after running this command, the next door you open will be set as a horde hut door, run this command again to cancel";
        public string Syntax => "sethutdoor";
        public List<string> Aliases => new List<string>(){ "shd" };
        public List<string> Permissions => new List<string>() { "hordehuts.sethutdoor" };
    }
}