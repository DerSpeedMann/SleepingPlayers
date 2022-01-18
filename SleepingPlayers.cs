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

namespace SpeedMann.SleepingPlayers
{
	public class SleepingPlayers : RocketPlugin<SleepingPlayerConfiguration>
	{
		public static SleepingPlayers Inst;
		public static SleepingPlayerConfiguration Conf;
		private InventoryHelper inventoryHelper;

		private float SleepingPlayerSearchRadius = 2;

		private Dictionary<CSteamID, Dictionary<ushort, List<Item>>> connectingPlayerInventorys;
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

			inventoryHelper = new InventoryHelper();
			connectingPlayerInventorys = new Dictionary<CSteamID, Dictionary<ushort, List<Item>>>();
			
			UnturnedPrivateFields.init();
			UnturnedPatches.Init();

			U.Events.OnPlayerConnected += OnPlayerConnected;
			U.Events.OnPlayerDisconnected += OnPlayerDisconnected;
			Provider.onLoginSpawning += OnLoginPlayerSpawning;
			UnturnedPlayerEvents.OnPlayerInventoryAdded += OnInventoryUpdated;
			Level.onPreLevelLoaded += OnPreLevelLoaded;
			UnturnedPatches.OnPostSleepingPlayerStorageUpdate += OnSleepingPlayerStorageUpdated;
		}

		protected override void Unload()
		{
			Logger.LogWarning("Unloading...");

			U.Events.OnPlayerConnected -= OnPlayerConnected;
			U.Events.OnPlayerDisconnected -= OnPlayerDisconnected;
			Provider.onLoginSpawning -= OnLoginPlayerSpawning;
			UnturnedPlayerEvents.OnPlayerInventoryAdded -= OnInventoryUpdated;
			Level.onPreLevelLoaded -= OnPreLevelLoaded;
			UnturnedPatches.OnPostSleepingPlayerStorageUpdate += OnSleepingPlayerStorageUpdated;
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
        }

		private void OnPlayerConnected(UnturnedPlayer player)
		{
			bool sucess = false;
			Dictionary<ushort, List<Item>> savedItems;
			ITransportConnection transportConnection = Provider.findTransportConnection(player.CSteamID);
			if (transportConnection == null)
			{
				Logger.LogError("Error in tranport connection for player: "+ player.CSteamID);
				return;
			}

			if (Provider.findPlayer(transportConnection) != null)
            {
				if (connectingPlayerInventorys.TryGetValue(player.CSteamID, out savedItems) && savedItems != null)
				{
					sucess = inventoryHelper.UpdateInventory(player, savedItems);
					Logger.Log("Loading Inventory was " + (!sucess ? "not " : "") + "successful");
				}
				else
				{
					sucess = inventoryHelper.ClearAll(player);
					player.Damage(255, player.Position, EDeathCause.SUICIDE, ELimb.SKULL, player.CSteamID);
					Logger.Log("Clearing Inventory was " + (!sucess ? "not " : "") + "successful");
				}
			}
			
			connectingPlayerInventorys.Remove(player.CSteamID);

		}

		private void OnPlayerDisconnected(UnturnedPlayer player)
		{
            if (!player.Dead)
            {
				spawnSleepingPlayer(player);
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
			if (Conf.StorageHeight > 0)
			{
				Asset SleepingPalyerAsset = Assets.find(EAssetType.ITEM, Conf.SleepingPlayerStorageId);
				if (SleepingPalyerAsset != null)
                {
					UnturnedPrivateFields.setStorageY((ItemStorageAsset)SleepingPalyerAsset, Conf.StorageHeight);
					Logger.Log("Resized SleepingPlayer Asset: "+ SleepingPalyerAsset.name +" new size is: ["+ ((ItemStorageAsset)SleepingPalyerAsset).storage_x + ", "+ ((ItemStorageAsset)SleepingPalyerAsset).storage_y +"]");
					return;
				}
				Logger.LogError("Resize SleepingPlayer Asset Failed!");
			}
		}
		private void OnSleepingPlayerStorageUpdated(InteractableStorage interactableStorage, ItemStorageAsset asset)
        {
			StorageHelper.fitStorage(interactableStorage, asset);
		}
        #endregion

        #region SleepingPlayerLogic
        public void spawnSleepingPlayer(UnturnedPlayer player)
        {
			ItemStorageAsset asset = (Assets.find(EAssetType.ITEM, Conf.SleepingPlayerStorageId) as ItemStorageAsset);
			Vector3 position = new Vector3(player.Position.x, player.Position.y + asset.offset, player.Position.z);

			Transform barricadeTransform = BarricadeManager.dropBarricade(new Barricade(asset), null, position, 0, 0, 0, player.CSteamID.m_SteamID, 0);

			InteractableStorage storage = barricadeTransform.transform.GetComponent<InteractableStorage>();

			if (storage != null)
			{
				List<Item> items = new List<Item>();
				inventoryHelper.GetAllItems(player, ref items);
				storage.items.resize(asset.storage_x, asset.storage_y);

				foreach (var item in items)
				{
					storage.items.tryAddItem(item);
				}
				Logger.Log("Spawned SleepingPlayer for: " + player.CSteamID + " at " + position.ToString());

                if (Conf.AutoResize)
                {
					StorageHelper.fitStorage(storage, (ItemStorageAsset)asset);
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
					if(Configuration.Instance.Debug)
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
        #endregion
    }
}
