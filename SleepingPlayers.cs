using System.Collections.Generic;
using Rocket.API.Collections;
using Rocket.Core.Plugins;
using Rocket.Unturned;
using Rocket.Unturned.Events;
using Rocket.Unturned.Player;
using Logger = Rocket.Core.Logging.Logger;

using SDG.Unturned;

using UnityEngine;
using Rocket.Unturned.Enumerations;
using Steamworks;
using System;
using Rocket.Unturned.Chat;
using SDG.NetTransport;
using Rocket.API;
using System.Linq;

namespace SpeedMann.SleepingPlayers
{
	public class SleepingPlayers : RocketPlugin<SleepingPlayerConfiguration>
	{
		public static SleepingPlayers Inst;
		public static SleepingPlayerConfiguration Conf;

		private InventoryHelper inventoryHelper;
		private string Version;
		

		private float SleepingPlayerSearchRadius = 2;

		private Dictionary<CSteamID, Dictionary<ushort, List<Item>>> connectingPlayerInventorys;
		private List<CSteamID> newPlayers;
		public override TranslationList DefaultTranslations
		{
			get
			{
				return new TranslationList() {
				};
			}
		}

		protected override void Load()
		{
			Inst = this;
			Conf = Configuration.Instance; 
			Version = readFileVersion();

			inventoryHelper = new InventoryHelper();
			connectingPlayerInventorys = new Dictionary<CSteamID, Dictionary<ushort, List<Item>>>();
			newPlayers = new List<CSteamID>();

			Logger.Log($"Loading SleepingPlayer {Version} by SpeedMann");

			UnturnedPrivateFields.init();
			UnturnedPatches.Init();

			Provider.onServerConnected += OnPlayerConnected;
			Provider.onServerDisconnected += OnPlayerDisconnected;
			Provider.onLoginSpawning += OnLoginPlayerSpawning;
			UnturnedPlayerEvents.OnPlayerInventoryAdded += OnInventoryUpdated;
			Level.onPreLevelLoaded += OnPreLevelLoaded;
			UnturnedPatches.OnPostSleepingPlayerStorageUpdate += OnSleepingPlayerStorageUpdated;

			if (Level.isLoaded) {
				setSleeperAssetStorageSize();

				foreach (var region in BarricadeManager.regions)
				{
					foreach (var drop in region.drops)
					{
						if (drop.interactable is InteractableStorage storage && drop.asset is ItemStorageAsset storageAsset && Conf.SleepingPlayerStorageId == storageAsset.id)
						{
							resizeSleeperStorage(storage, storageAsset);
						}
					}
				}
			}
		}

		protected override void Unload()
		{
			Logger.LogWarning("Unloading...");
			UnturnedPatches.Cleanup();

			Provider.onServerConnected -= OnPlayerConnected;
			Provider.onServerDisconnected -= OnPlayerDisconnected;
			Provider.onLoginSpawning -= OnLoginPlayerSpawning;
			UnturnedPlayerEvents.OnPlayerInventoryAdded -= OnInventoryUpdated;
			Level.onPreLevelLoaded -= OnPreLevelLoaded;
			UnturnedPatches.OnPostSleepingPlayerStorageUpdate -= OnSleepingPlayerStorageUpdated;
		}

        #region Events
        private void Update()
		{
		}
		private void OnLoginPlayerSpawning(SteamPlayerID playerID, ref Vector3 point, ref float yaw, ref EPlayerStance initialStance, ref bool needsNewSpawnpoint)
        {
            Dictionary<ushort, List<Item>> savedItems = tryGetItemsFromSleepingPlayer(playerID.steamID, point, ref needsNewSpawnpoint, ref initialStance);
            if(!connectingPlayerInventorys.ContainsKey(playerID.steamID)){
				connectingPlayerInventorys.Add(playerID.steamID, savedItems);
			}
			if(!PlayerSavedata.fileExists(playerID, "/Player/Player.dat"))
            {
				newPlayers.Add(playerID.steamID);
            }
        }

		private void OnPlayerConnected(CSteamID playerId)
		{
			if (playerId == CSteamID.Nil)
			{
				return;
			}

			UnturnedPlayer player = UnturnedPlayer.FromCSteamID(playerId);

			bool success = false;
			Dictionary<ushort, List<Item>> savedItems;
			ITransportConnection transportConnection = Provider.findTransportConnection(player.CSteamID);
			if (transportConnection == null)
			{
				Logger.LogError("Error in tranport connection for player: "+ player.CSteamID);
				connectingPlayerInventorys.Remove(player.CSteamID);
				newPlayers.Remove(player.CSteamID);
				return;
			}
            if (Conf.UnsavedPlayers.Contains(player.CSteamID.m_SteamID))
            {
				List<Item> items = new List<Item>();
				if ((player.IsAdmin && Conf.IgnoreAdmins) || player.GetPermissions().Any(x => x.Name == "sleepingplayer.bypass"))
				{
					Logger.Log($"{player.DisplayName} [{player.CSteamID}] with permission to bypass SleepingPlayer returned");
				}
				else if (!Conf.AllowEmptySleepingPlayers && inventoryHelper.GetAllItems(player, ref items) && items.Count <= 0)
                {
					Logger.Log($"Empty player {player.DisplayName} [{player.CSteamID}] returned");
				}
                else if (!Conf.AllowSleepingPlayersInSafezone && (player.Player.movement.isSafe || (player.Player.movement?.isSafeInfo?.noWeapons ?? false)))
                {
					//TODO: fix safezone check (not yet in safezone)
					Logger.Log($"Player {player.DisplayName} [{player.CSteamID}] returned to safezone");
                }
                else
                {
					Logger.Log($"Unsaved player {player.DisplayName} [{player.CSteamID}] returned");
				}
				
				connectingPlayerInventorys.Remove(player.CSteamID);
				newPlayers.Remove(player.CSteamID);
				return;
            }

			if (Provider.findPlayer(transportConnection) != null && !newPlayers.Contains(player.CSteamID))
            {
				if (connectingPlayerInventorys.TryGetValue(player.CSteamID, out savedItems) && savedItems != null)
				{
					success = inventoryHelper.UpdateInventory(player, savedItems);
					if (success)
					{
						Logger.Log($"Loading Inventory for {player.DisplayName} was successful");
					}
					else
					{
						Logger.LogError($"Loading Inventory for {player.DisplayName} was not successful!");
					}
				}
				else
				{
					success = inventoryHelper.ClearAll(player);

					// to kill player in safezone
					player.Player.life.askDamage(255, Vector3.up, EDeathCause.SUICIDE, ELimb.SKULL, CSteamID.Nil, out EPlayerKill eplayerKill, false, ERagdollEffect.NONE, false, true);

                    if (success)
                    {
						Logger.Log($"Clearing Inventory for {player.DisplayName} was successful");
					}
                    else
                    {
						Logger.LogError($"Clearing Inventory for {player.DisplayName} was not successful!");
					}
					
				}
            }

			newPlayers.Remove(player.CSteamID);
			connectingPlayerInventorys.Remove(player.CSteamID);

			Conf.UnsavedPlayers.Add(player.CSteamID.m_SteamID);
			Inst.Configuration.Save();

		}

		private void OnPlayerDisconnected(CSteamID playerId)
		{
			if(playerId == CSteamID.Nil)
            {
				return;
            }
			// add player if not previously added
            if(!Conf.UnsavedPlayers.Contains(playerId.m_SteamID))
			{
				Conf.UnsavedPlayers.Add(playerId.m_SteamID);
				Inst.Configuration.Save();
			}
			UnturnedPlayer player = UnturnedPlayer.FromCSteamID(playerId);

			bool allowSleeper = true;
			// check admin of bypass
			if ((player.IsAdmin && Conf.IgnoreAdmins) || player.GetPermissions().Any(x => x.Name == "sleepingplayer.bypass"))
            {
				allowSleeper = false;
				Logger.Log($"{player.DisplayName} [{player.CSteamID}] has permission to bypass SleepingPlayer");
			}
			
			// check empty inventory
			if (!Conf.AllowEmptySleepingPlayers)
            {
				List<Item> items = new List<Item>();
				if(inventoryHelper.GetAllItems(player, ref items) && items.Count <= 0)
                {
					allowSleeper = false;
					Logger.Log($"Inventory of {player.DisplayName} [{player.CSteamID}] was empty no SleepingPlayer was spawned");
				}
            }
			// check in safezone
			if (!Conf.AllowSleepingPlayersInSafezone && (player.Player.movement.isSafe || (player.Player.movement?.isSafeInfo?.noWeapons ?? false)))
            {
				allowSleeper = false;
				Logger.Log($"{player.DisplayName} [{player.CSteamID}] was in safezone no SleepingPlayer was spawned");
			}

			if (allowSleeper)
            {
				if (!player.Dead)
				{
					spawnSleepingPlayer(player);
				}

				Conf.UnsavedPlayers.Remove(player.CSteamID.m_SteamID);
				Inst.Configuration.Save();
            }
		}
		private void OnInventoryUpdated(UnturnedPlayer player, InventoryGroup inventoryGroup, byte inventoryIndex, ItemJar P)
		{
			if(P.item.id == Conf.SleepingPlayerStorageId)
            {
				player.Inventory.removeItem((byte)inventoryGroup, inventoryIndex);
				UnturnedChat.Say(player, "You should not have this!", Color.red);
			}
		}
		private void OnPreLevelLoaded(int level) 
		{
			setSleeperAssetStorageSize();
		}

		private void OnSleepingPlayerStorageUpdated(InteractableStorage interactableStorage, ItemStorageAsset asset)
        {
			resizeSleeperStorage(interactableStorage, asset);
		}
        #endregion

        #region SleepingPlayerLogic
        public void spawnSleepingPlayer(UnturnedPlayer player)
        {
			ItemStorageAsset asset = (Assets.find(EAssetType.ITEM, Conf.SleepingPlayerStorageId) as ItemStorageAsset);
			Vector3 position = new Vector3(player.Position.x, player.Position.y + asset.offset, player.Position.z);
			Barricade barricade = new Barricade(asset);
			Transform barricadeTransform = BarricadeManager.dropBarricade(barricade, null, position, 0, 0, 0, player.CSteamID.m_SteamID, 0);

			if(barricadeTransform == null)
            {
                if (Conf.Debug)
                {
					Logger.Log($"Placement of SleepingPlayer at {position} was restricted, trying to force place SleepingPlayer");
				}
				
				// force drop barricade
				Quaternion rotation = BarricadeManager.getRotation(barricade.asset, 0, 0, 0);
				barricadeTransform = BarricadeManager.dropNonPlantedBarricade(barricade, position, rotation, player.CSteamID.m_SteamID, 0);
			}
			if(barricadeTransform == null){
				Logger.LogError($"Could not place SleepingPlayer for: {player.DisplayName} [{player.CSteamID}] at {position}!");
				return;
            }
			InteractableStorage storage = barricadeTransform.transform.GetComponent<InteractableStorage>();

			if (storage != null)
			{
				inventoryHelper.GetAllSavedItems(player.Player, out List<Item> items);
				storage.items.resize(Conf.StorageWidth, Conf.StorageHeight);

				foreach (var item in items)
				{
					storage.items.tryAddItem(item);
				}
				Logger.Log($"Spawned SleepingPlayer for: {player.DisplayName} [{player.CSteamID}] at {position}");
				//TODO: add backpack baricade to store items > 200

                if (Conf.AutoResize)
                {
					StorageHelper.fitStorageSize(storage, asset);
				}
			}
		}
       
		public Dictionary<ushort, List<Item>> tryGetItemsFromSleepingPlayer(CSteamID steamID, Vector3 point, ref bool needsNewSpawnpoint, ref EPlayerStance initialStance)
        {
			Transform sleepingPlayerTransform = findSleepingPlayer(point, SleepingPlayerSearchRadius, steamID);

			if (sleepingPlayerTransform == null)
				sleepingPlayerTransform = findSleepingPlayer(getGroundedPosition(point), SleepingPlayerSearchRadius, steamID);

			// loading Inventory
			if (sleepingPlayerTransform != null)
			{
				Dictionary<ushort, List<Item>> savedItems;
				InteractableStorage storage = sleepingPlayerTransform.transform.GetComponent<InteractableStorage>();
				if (storage != null)
				{
					savedItems = getItemsFromStorage(storage);

					byte x;
					byte y;
					ushort plant;
					BarricadeRegion barricadeRegion;
					if (BarricadeManager.tryGetRegion(sleepingPlayerTransform, out x, out y, out plant, out barricadeRegion))
					{
						BarricadeDrop barricadeDrop = barricadeRegion.FindBarricadeByRootTransform(sleepingPlayerTransform);

						BarricadeManager.destroyBarricade(barricadeDrop, x, y, plant);

						needsNewSpawnpoint = !isValidSpawnPoint(point, ref initialStance);

						// better spawn check
						if (needsNewSpawnpoint)
                        {
							initialStance = EPlayerStance.CROUCH;
							needsNewSpawnpoint = !isValidSpawnPoint(point, ref initialStance);

							if (needsNewSpawnpoint)
							{
								initialStance = EPlayerStance.PRONE;
								needsNewSpawnpoint = !isValidSpawnPoint(point, ref initialStance);
							}	
						}
						
						return savedItems;
					}
					Logger.LogError("Error destroying Barricade for player: " + steamID);
					return null;
				}
				else
				{
					Logger.LogError("Error loading SleepingPlayer Storage at: " + point.ToString() + " for player: " + steamID);
				}
			}
			return null;
        }

		private Transform findSleepingPlayer(Vector3 center, float radius, CSteamID playerID)
		{
			byte x;
			byte y;

			Regions.tryGetCoordinate(center, out x, out y);
			List<RegionCoordinate> coordinates = new List<RegionCoordinate>() { new RegionCoordinate(x, y) };
			List<Transform> transforms = new List<Transform>();
			BarricadeManager.getBarricadesInRadius(center, radius, coordinates, transforms);

			foreach (Transform barricadeTransform in transforms)
			{
				if (NearlyEqual(barricadeTransform.position.x, center.x, SleepingPlayerSearchRadius) && NearlyEqual(barricadeTransform.position.z, center.z, SleepingPlayerSearchRadius))
				{

					Interactable2SalvageBarricade salvageBarricade = barricadeTransform.transform.GetComponent<Interactable2SalvageBarricade>();
					ItemBarricadeAsset barricadeAsset = getAssetFromBarricadeTransform(barricadeTransform);
					if(Conf.Debug)
						Logger.Log("Found potential SleepingPlayer, owner: " + (salvageBarricade != null ? salvageBarricade.owner.ToString() : "null") +  " PlayerId: " + playerID.ToString() + 
							" Location: " + barricadeTransform.position.ToString() + " SearchCenter: " + center);

					if (barricadeAsset != null && barricadeAsset.id == Configuration.Instance.SleepingPlayerStorageId && salvageBarricade != null && salvageBarricade.owner == playerID.m_SteamID)
                    {
						return barricadeTransform;
					}

				}

			}
			return null;
		}
        #endregion

        #region HelperFunctions
        private static Dictionary<ushort, List<Item>> getItemsFromStorage(InteractableStorage storage)
		{
			Dictionary<ushort, List<Item>> savedItems = new Dictionary<ushort, List<Item>>();
			while (storage.items.items.Count > 0)
			{
				var firstItem = storage.items.items[0];
				List<Item> itemList;
				if (!savedItems.ContainsKey(firstItem.item.id))
					savedItems.Add(firstItem.item.id, new List<Item>() { firstItem.item });
				else if (savedItems.TryGetValue(firstItem.item.id, out itemList))
				{
					itemList.Add(firstItem.item);
				}
				storage.items.items.RemoveAt(0);
			}
			return savedItems;
		}
		private static ItemBarricadeAsset getAssetFromBarricadeTransform(Transform transform)
		{
			ThreadUtil.assertIsGameThread();
			byte x;
			byte y;
			ushort num;
			BarricadeRegion barricadeRegion;

			if (!BarricadeManager.tryGetRegion(transform, out x, out y, out num, out barricadeRegion))
				return null;

			BarricadeDrop barricadeDrop = barricadeRegion.FindBarricadeByRootTransform(transform);
			if (barricadeDrop == null) return null;

			ItemBarricadeAsset asset = barricadeDrop.asset;
			if (asset == null) return null;

			return asset;
		}
		private static ItemStructureAsset getAssetFromStructureTransform(Transform transform)
		{
			ThreadUtil.assertIsGameThread();
			byte x;
			byte y;
			StructureRegion structureRegion;

			if (!StructureManager.tryGetRegion(transform, out x, out y, out structureRegion))
				return null;

			StructureDrop structureDrop = structureRegion.FindStructureByRootTransform(transform);
			if (structureDrop == null) return null;

			ItemStructureAsset asset = structureDrop.asset;
			if (asset == null) return null;

			return asset;
		}

		private static bool isValidSpawnPoint(Vector3 point, ref EPlayerStance initialStance)
        {
			bool flag = true;
			/*
			 * TODO: FIX
			if (!point.IsFinite())
			{
				flag = true;
			}
			*/
			if (!PlayerStance.getStanceForPosition(point, ref initialStance))
			{
				initialStance = EPlayerStance.PRONE;
				if (!PlayerStance.getStanceForPosition(point, ref initialStance))
					flag = false;
			}
			return flag;
		}
		public static bool NearlyEqual(double a, double b, double epsilon)
		{
			const double MinNormal = 2.2250738585072014E-308d;
			double absA = Math.Abs(a);
			double absB = Math.Abs(b);
			double diff = Math.Abs(a - b);

			if (a.Equals(b))
			{ // shortcut, handles infinities
				return true;
			}
			else if (a == 0 || b == 0 || absA + absB < MinNormal)
			{
				// a or b is zero or both are extremely close to it
				// relative error is less meaningful here
				return diff < (epsilon * MinNormal);
			}
			else
			{ // use relative error
				return diff / (absA + absB) < epsilon;
			}
		}

		private static Vector3 getGroundedPosition(Vector3 point, float offset = 0)
		{
			return new Vector3(point.x, LevelGround.getHeight(point) + offset, point.z);
		}

		private static string readFileVersion()
        {
			System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
			System.Diagnostics.FileVersionInfo fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location);
			return fvi.FileVersion;
		}
        private static void setSleeperAssetStorageSize()
        {
			if (Conf.StorageHeight > 0 && Conf.StorageWidth > 0)
			{
				ItemStorageAsset SleepingPlayerAsset = Assets.find(EAssetType.ITEM, Conf.SleepingPlayerStorageId) as ItemStorageAsset;
				if (SleepingPlayerAsset != null)
				{
					UnturnedPrivateFields.setStorageX(SleepingPlayerAsset, Conf.StorageWidth);
					UnturnedPrivateFields.setStorageY(SleepingPlayerAsset, Conf.StorageHeight);

					if (Conf.Debug)
					{
						Logger.Log("Resized SleepingPlayer Asset: " + SleepingPlayerAsset.name + " new size is: [" + (SleepingPlayerAsset).storage_x + ", " + (SleepingPlayerAsset).storage_y + "]");
					}
					return;
				}
				Logger.LogError($"Resize SleepingPlayer Asset Failed!" +
					$"Asset with Id: {Conf.SleepingPlayerStorageId} might not be a ItemStorageAsset");
				return;
			}
			Logger.LogError($"Resize SleepingPlayer Asset Failed!" +
					$"StorageWidth or Height <= 0");
		}

		private static void resizeSleeperStorage(InteractableStorage interactableStorage, ItemStorageAsset asset)
        {
			if (!Conf.AutoResize)
			{
				StorageHelper.setStorageSize(interactableStorage, asset);
			}
			else
			{
				StorageHelper.fitStorageSize(interactableStorage, asset);
			}
		}
		#endregion
	}
}
