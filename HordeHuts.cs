using System;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using Newtonsoft.Json;
using Rocket.Core.Plugins;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using UnityEngine;
using Logger = Rocket.Core.Logging.Logger;

namespace Zombs_R_Cute_Horde_Huts
{
    [HarmonyPatch]
    public class HordeHuts : RocketPlugin<HordeHutsConfiguration>
    {
        public static HordeHutsConfiguration ConfigInstance;
        public static bool SetCommandRun;

        private static string filename = "Plugins/Zombs_R_Cute_Horde_Huts/DoorLocations.json";

        public static HashSet<Vector3> hordeDoors = new HashSet<Vector3>();
        public static Dictionary<Vector3, CSteamID> doorLockedByPlayer = new Dictionary<Vector3, CSteamID>();
        public static Dictionary<Vector3, TimerPlus> doorToTimer = new Dictionary<Vector3, TimerPlus>();

        protected override void Load()
        {
            ConfigInstance = Configuration.Instance;
            Harmony harmony = new Harmony("HordeHuts");
            harmony.PatchAll();
            LoadDoors();
        }

        private static void AddHutDoor(Vector3 position)
        {
            hordeDoors.Add(position);
            doorLockedByPlayer[position] = CSteamID.Nil;
            doorToTimer[position] = null;
        }

        private void LoadDoors()
        {
            if (!File.Exists(filename))
            {
                return;
            }
            string text = File.ReadAllText(filename);
            hordeDoors = JsonConvert.DeserializeObject<HashSet<Vector3>>(text);

            Logger.Log("Loaded Horde Hut Doors: " + hordeDoors.Count + " Entries");
            foreach (var position in hordeDoors)
            {
                AddHutDoor(position);
            }
        }

        private static void SaveDoors()
        {
            string output = "";
            int count = 0;
            
            lock (hordeDoors)
            {
                output = JsonConvert.SerializeObject(hordeDoors, new JsonSerializerSettings(){ReferenceLoopHandling = ReferenceLoopHandling.Ignore});
                count = hordeDoors.Count;
            }
            File.WriteAllText(filename, output);
            Logger.Log("Saved Horde hut door positions: " + count + " Entries");
        }


        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(InteractableDoor), nameof(InteractableDoor.ReceiveToggleRequest))]
        public static bool ReceiveToggleRequest(in ServerInvocationContext context, bool desiredOpen,
            InteractableDoor __instance)
        {
            var position = __instance.transform.position;
            
            if (hordeDoors.Contains(position) == false && !SetCommandRun)  // if it's not a horde hut return immediately
            {
                return true;
            }
            CSteamID lockedBy = CSteamID.Nil;
            if(doorLockedByPlayer.ContainsKey(position))
                lockedBy = doorLockedByPlayer[position];

            UnturnedPlayer player = UnturnedPlayer.FromPlayer(context.GetPlayer());
            if (player.IsAdmin && SetCommandRun)
            {
                SetCommandRun = false;
                if (hordeDoors.Contains(position))
                {
                    UnturnedChat.Say(player, $"Removed door at {position}", Color.yellow);
                    hordeDoors.Remove(position);
                    SaveDoors();
                    return false;
                }                    

                UnturnedChat.Say(player, $"Set door at {position}", Color.yellow);
                AddHutDoor(position);
                SaveDoors();
                return false;
            }
                
            var timer = new TimerPlus();
            timer.AutoReset = false;
            if (lockedBy == player.CSteamID || lockedBy == CSteamID.Nil)
            {
                if (doorToTimer[position] != null) // is there a running timer?
                {
                    doorToTimer[position].Stop();
                    doorToTimer[position].Dispose();
                }
                                
                doorToTimer[position] = timer;
                doorLockedByPlayer[position] = player.CSteamID;
                timer.Elapsed += (sender, args) => TimerOnElapsed((TimerPlus)sender, position); 
                
                if (!__instance.isOpen)// if the door is not open
                {
                    timer.Interval = ConfigInstance.TimeToUnlockOpenedDoor * 1000;
                    timer.Start();
                    UnturnedChat.Say(player, $"The door will remain locked by you for {timer.Interval/1000} seconds", Color.yellow);
                    return true; //allow the door to be closed
                }

                timer.Interval = ConfigInstance.TimeToUnlockClosedDoor * 1000;
                UnturnedChat.Say(player, $"The door will remain locked by you for {timer.Interval/1000} seconds", Color.yellow);
                timer.Start();
                return true;
            }
            
            timer.Dispose();
            var remainingSeconds = (int)doorToTimer[position].TimeLeft / 1000;
            UnturnedChat.Say(player,$"Locked by \"{UnturnedPlayer.FromCSteamID(lockedBy).DisplayName}\"" +
                             $" and will be unlocked in {remainingSeconds} seconds.", Color.red);
            
            return false; // stop the door from opening
        }

     
        private static void TimerOnElapsed(TimerPlus timer, Vector3 position)
        {
            doorToTimer[position] = null;
            doorLockedByPlayer[position] = CSteamID.Nil;
            timer.Dispose();
        }
    }
}