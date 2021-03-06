using HarmonyLib;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Logger = Rocket.Core.Logging.Logger;

namespace SpeedMann.SleepingPlayers
{
    class UnturnedPatches
    {
        private static Harmony harmony;
        private static string harmonyId = "SpeedMann.SleepingPlayers";
        public class StorageState
        {
            public InteractableStorage interactableStorage;
            public Asset asset;
        }

        public static void Init()
        {
            try
            {
                harmony = new Harmony(harmonyId);
                harmony.PatchAll();
                if (SleepingPlayers.Conf.Debug)
                {
                    var myOriginalMethods = harmony.GetPatchedMethods();
                    Logger.Log("Patched Methods:");
                    foreach (var method in myOriginalMethods)
                    {
                        Logger.Log(" " + method.ToString());
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogError($"EventLoad: {e.Message}");
            }
        }
        public static void Cleanup()
        {
            try
            {
                harmony.UnpatchAll(harmonyId);

                if (SleepingPlayers.Conf.Debug)
                {
                    var myOriginalMethods = harmony.GetPatchedMethods();
                    Logger.Log("Patched Methods:");
                    foreach (var method in myOriginalMethods)
                    {
                        Logger.Log(" " + method.ToString());
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogError($"Unturnov patches: {e.Message}");
            }
        }

        #region Events
        public delegate void PostSleepingPlayerStorageUpdate(InteractableStorage storage, ItemStorageAsset asset);

        public static event PostSleepingPlayerStorageUpdate OnPostSleepingPlayerStorageUpdate;
        #endregion

        #region Patches
        [HarmonyPatch(typeof(InteractableStorage), nameof(InteractableStorage.updateState))]
        class SleepingPlayerStorageUpdate
        {
            [HarmonyPrefix]
            internal static bool OnPreSleepingPlayerStorageUpdateInvoker(InteractableStorage __instance, ref Asset asset, byte[] state, out StorageState __state)
            {
                __state = null;
                if (asset?.id == SleepingPlayers.Conf.SleepingPlayerStorageId)
                {
                    __state = new StorageState() { asset = asset, interactableStorage = __instance };
                }
                
                return true;
            }

            [HarmonyPostfix]
            internal static void OnPostSleepingPlayerStorageUpdateInvoker(StorageState __state)
            {
                if(__state != null)
                {
                    OnPostSleepingPlayerStorageUpdate?.Invoke(__state.interactableStorage, (ItemStorageAsset)__state.asset);
                    
                }
            }
        }

        #endregion
    }
}
