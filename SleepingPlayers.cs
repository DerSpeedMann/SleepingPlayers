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

namespace SpeedMann.SleepingPlayers
{
	public class SleepingPlayers : RocketPlugin<SleepingPlayerConfiguration>
	{
		public static SleepingPlayers Instance;

		private InventoryHelper inventoryHelper;

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
			Instance = this;
			inventoryHelper = new InventoryHelper();

			connectingPlayerInventorys = new Dictionary<CSteamID, Dictionary<ushort, List<Item>>>();

			U.Events.OnPlayerConnected += OnPlayerConnected;
			U.Events.OnPlayerDisconnected += OnPlayerDisconnected;
			Provider.onLoginSpawning += OnLoginPlayerSpawning;
		}

		protected override void Unload()
		{
			Logger.LogWarning("Unloading...");

			U.Events.OnPlayerConnected -= OnPlayerConnected;
			U.Events.OnPlayerDisconnected -= OnPlayerDisconnected;
			Provider.onLoginSpawning -= OnLoginPlayerSpawning;
		}
		private void Update()
		{
		}
		private void OnLoginPlayerSpawning(SteamPlayerID playerID, ref Vector3 point, ref float yaw, ref EPlayerStance initialStance, ref bool needsNewSpawnpoint)
        {
            if (needsNewSpawnpoint)
            {
				float radius = 1;

				Transform sleepingPlayerTransform = findSleepingPlayer(point, radius);

				if (sleepingPlayerTransform == null)
					sleepingPlayerTransform = findSleepingPlayer(getGroundedPosition(point), radius);

				// loading Inventory
				if (sleepingPlayerTransform != null)
				{ 			
					Dictionary<ushort, List<Item>> savedItems;
					InteractableStorage storage = sleepingPlayerTransform.transform.GetComponent<InteractableStorage>();
					if(storage != null)
                    {
						savedItems = getItemsFromStorage(storage);
						connectingPlayerInventorys.Add(playerID.steamID, savedItems);

						byte x;
						byte y;
						ushort plant;
						BarricadeRegion barricadeRegion;
						if (!BarricadeManager.tryGetRegion(sleepingPlayerTransform, out x, out y, out plant, out barricadeRegion))
						{
							return;
						}
						BarricadeDrop barricadeDrop = barricadeRegion.FindBarricadeByRootTransform(sleepingPlayerTransform);

						BarricadeManager.destroyBarricade(barricadeDrop, x, y, plant);

						needsNewSpawnpoint = !isValidSpawnPoint(point, ref initialStance);
						return;
					}
                    else
                    {
						Logger.LogError("Error loading SleepingPlayer Storage at: "+ point.ToString() +" for player: " + playerID);
					}
				}

				connectingPlayerInventorys.Add(playerID.steamID, null);
			}
        }

		private void OnPlayerConnected(UnturnedPlayer player)
		{
			bool sucess = false;
			Dictionary<ushort, List<Item>> savedItems;
			
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
			connectingPlayerInventorys.Remove(player.CSteamID);

		}

		private void OnPlayerDisconnected(UnturnedPlayer player)
		{
			Vector3 position = new Vector3(player.Position.x, player.Position.y + 1, player.Position.z);
			Transform barricadeTransform = BarricadeManager.dropBarricade(new Barricade(Configuration.Instance.SleepingPlayerStorageId), null, position, 0, 0, 0, 0, 0);

			Interactable2SalvageBarricade barricade = barricadeTransform.transform.GetComponent<Interactable2SalvageBarricade>();
			InteractableStorage storage = barricadeTransform.transform.GetComponent<InteractableStorage>();

			if (barricade != null && storage != null)
			{
				List<Item> items = new List<Item>();
				inventoryHelper.GetAllItems(player, ref items);

				foreach (var item in items)
				{
					storage.items.tryAddItem(item);
				}
			}

		}

		private Dictionary<ushort, List<Item>> getItemsFromStorage(InteractableStorage storage)
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
		private Transform findSleepingPlayer(Vector3 center, float radius)
		{
			byte x;
			byte y;

			Regions.tryGetCoordinate(center, out x, out y);
			List<RegionCoordinate> coordinates = new List<RegionCoordinate>() { new RegionCoordinate(x, y) };
			List<Transform> transforms = new List<Transform>();
			BarricadeManager.getBarricadesInRadius(center, radius, coordinates, transforms);

			foreach (var transform in transforms)
			{
				if (transform.position.x == center.x && transform.position.z == center.z)
				{

					ItemBarricadeAsset barricadeAsset = getAssetFromBarricadeTransform(transform);

					if (barricadeAsset != null && barricadeAsset.id == Configuration.Instance.SleepingPlayerStorageId)
						return transform;
				}

			}
			return null;
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

		private static Vector3 getGroundedPosition(Vector3 point, float offset = 0)
		{
			return new Vector3(point.x, LevelGround.getHeight(point) + offset, point.z);
		}

	}
}
