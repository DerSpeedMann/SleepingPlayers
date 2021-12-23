using System;
using System.Collections.Generic;
using Rocket.API;
using Rocket.Core.Logging;
using Rocket.Core.Plugins;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using Rocket.Unturned.Plugins;
using SDG.Unturned;

namespace SpeedMann.SleepingPlayers
{
	public class InventoryHelper
	{
		public bool UpdateInventory(UnturnedPlayer player, Dictionary<ushort, List<Item>> savedItems)
        {
			bool returnv = false;
			if (savedItems == null) return returnv;

			try
			{
                // Check Clothing Items and remove removed Clothing
                #region checkClothing
                List<KeyValuePair<ClothingType, Item>> clothingItems = new List<KeyValuePair<ClothingType, Item>>();
				if (!GetClothingItems(player, ref clothingItems))
					return false;

				Logger.Log("Found " + clothingItems.Count + " Clothing items");
				foreach (KeyValuePair<ClothingType, Item> item in clothingItems)
				{
					List<Item> itemList;
					bool found = false;
					if (savedItems.TryGetValue(item.Value.id, out itemList) && itemList.Count > 0)
					{
						for (int i = 0; i < itemList.Count; i++)
						{
							if (ItemEquality(item.Value, itemList[i]))
							{
								itemList.RemoveAt(i);
								found = true;
								break;
							}
						}
					}
					if (!found)
					{
						Logger.Log("Removed " + item.Key);
						//TODO: Fix Remove, currently only deequip
						switch (item.Key)
						{
							case ClothingType.Backpack:
								clearInventoryPage(player, 3);
								player.Player.clothing.thirdClothes.backpack = 0;
								player.Player.clothing.askWearBackpack(0, 0, new byte[0], true);
								break;
							case ClothingType.Vest:
								clearInventoryPage(player, 4);
								player.Player.clothing.thirdClothes.vest = 0;
								player.Player.clothing.askWearVest(0, 0, new byte[0], true);
								break;
							case ClothingType.Shirt:
								clearInventoryPage(player, 5);
								player.Player.clothing.thirdClothes.shirt = 0;
								player.Player.clothing.askWearShirt(0, 0, new byte[0], true);
								break;
							case ClothingType.Pants:
								clearInventoryPage(player, 6);
								player.Player.clothing.thirdClothes.pants = 0;
								player.Player.clothing.askWearPants(0, 0, new byte[0], true);
								break;
							case ClothingType.Glasses:
								player.Player.clothing.thirdClothes.glasses = 0;
								player.Player.clothing.askWearGlasses(0, 0, new byte[0], true);
								break;
							case ClothingType.Hat:
								player.Player.clothing.thirdClothes.hat = 0;
								player.Player.clothing.askWearHat(0, 0, new byte[0], true);
								break;
							case ClothingType.Mask:
								player.Player.clothing.thirdClothes.mask = 0;
								player.Player.clothing.askWearMask(0, 0, new byte[0], true);
								break;
						}
					}
				}
                #endregion

                // Check Inventory Pages and remove removed items
                #region checkItems
                for (byte p = 0; p < PlayerInventory.PAGES; p++)
				{
					if (player.Inventory.items[p] == null) continue;
					byte itemc = player.Inventory.getItemCount(p);
					if (itemc > 0)
					{
						List<byte> removedIndexes = new List<byte>();
						for (byte p1 = 0; p1 < itemc; p1++)
                        {
							ItemJar itemJ = player.Inventory.items[p].items[p1];
							List<Item> itemList;
							bool found = false;
							if (itemJ != null && savedItems.TryGetValue(itemJ.item.id, out itemList) && itemList.Count > 0)
							{
								for (int i = 0; i < itemList.Count; i++)
								{
									if (ItemEquality(itemJ.item, itemList[i]))
									{
										itemList.RemoveAt(i);
										found = true;
										break;
									}
								}
							}
							if (!found)
							{
								removedIndexes.Add(p1);
							}
						}
						Logger.Log("Removed " + removedIndexes.Count + " in page " + p);
						removedIndexes.Sort((a, b) => b.CompareTo(a));
						foreach (byte i in removedIndexes)
                        {
							player.Inventory.removeItem(p, i);
						}
					}
				}
                #endregion

                // Add added items to inventory
                #region addNewItems
                int counter = 0;
				foreach (KeyValuePair<ushort, List<Item>> savedItem in savedItems)
				{
					foreach (Item item in savedItem.Value)
					{
						counter++;
						if (!player.Inventory.tryAddItem(item, true))
							player.Inventory.forceAddItem(item, false);
					}
				}
				Logger.Log("Added " + counter + " new items");
                #endregion
                returnv = true;
			}
			catch (Exception e)
			{
				Logger.Log("There was an error getting items from " + player.CharacterName + "'s inventory.  Here is the error.");
				Console.Write(e);
			}
			return returnv;
		}

		public void clearInventoryPage(UnturnedPlayer player, byte page)
        {
			if (player.Player.inventory.items[page] == null)
				return;

			for (byte p2 = 0; p2 < player.Player.inventory.getItemCount(page); p2++)
			{
				player.Player.inventory.removeItem(page, 0);
			}
		}
		public bool ItemEquality(Item a, Item b)
		{
			bool equals = false;
			ItemAsset itemAssetA = Assets.find(EAssetType.ITEM, a.id) as ItemAsset;
			ItemAsset itemAssetB = Assets.find(EAssetType.ITEM, b.id) as ItemAsset;
			if (itemAssetA is ItemGunAsset && itemAssetB is ItemGunAsset)
            {
				equals = a != null && b != null
								&& a.id == b.id
								&& a.durability == b.durability
								&& a.quality == b.quality
								&& a.amount == b.amount
								&& a.state[0] == b.state[0] // Sight 1
								&& a.state[1] == b.state[1] // Sight 2
								&& a.state[2] == b.state[2] // Tactical 1
								&& a.state[3] == b.state[3] // Tactical 2
								&& a.state[4] == b.state[4] // Grip 1
								&& a.state[5] == b.state[5] // Grip 2
								&& a.state[6] == b.state[6] // Barrel 1
								&& a.state[7] == b.state[7] // Barrel 2
								&& a.state[8] == b.state[8] // Mag 1
								&& a.state[9] == b.state[9] // Mag 2
								&& a.state[10] == b.state[10]; // Mag Ammo
			}
            else
			{
				equals = a != null && b != null
								&& a.id == b.id
								&& a.durability == b.durability
								&& a.quality == b.quality
								&& a.amount == b.amount;
			}

			return equals;


		}
		public bool GetAllItems(UnturnedPlayer player, ref List<Item> foundItems)
        {
			List<KeyValuePair<ClothingType, Item>> foundClothings = new List<KeyValuePair<ClothingType, Item>>();
			if(GetClothingItems(player, ref foundClothings))
            {
				foreach (KeyValuePair<ClothingType, Item> item in foundClothings)
				{
					foundItems.Add(item.Value);
				}
				return GetInvItems(player, ref foundItems);
			}
			return false;
		}
		public bool GetInvItems(UnturnedPlayer player, ref List<Item> foundItems)
		{
            bool returnv = false;
			if (foundItems == null) return returnv;

			try
			{
				foreach (Items items in player.Inventory.items)
				{
					if (items == null) continue;

					foreach (ItemJar itemJ in items.items)
					{
						foundItems.Add(itemJ.item);
					}
					returnv = true;
				}
				
			}
			catch (Exception e)
			{
				Logger.Log("There was an error getting itmes from " + player.CharacterName + "'s inventory.  Here is the error.");
				Console.Write(e);
			}
            return returnv;
        }
		public bool GetClothingItems(UnturnedPlayer player, ref List<KeyValuePair<ClothingType, Item>> foundItems)
		{
			bool returnv = false;
			if (foundItems == null) return returnv;

			try
			{
				List<ItemClothingAsset> clothing = new List<ItemClothingAsset>();

				clothing.Add(player.Player.clothing.hatAsset);
				clothing.Add(player.Player.clothing.maskAsset);
				clothing.Add(player.Player.clothing.glassesAsset);
				clothing.Add(player.Player.clothing.backpackAsset);
				clothing.Add(player.Player.clothing.vestAsset);
				clothing.Add(player.Player.clothing.shirtAsset);
				clothing.Add(player.Player.clothing.pantsAsset);

				foreach(ItemClothingAsset item in clothing)
                {
					if(item != null)
                    {
						byte quality = 0;
						ClothingType type = ClothingType.None;

						switch (item)
                        {
							case ItemHatAsset hat:
								quality = player.Player.clothing.hatQuality;
								type = ClothingType.Hat;
								break;
							case ItemMaskAsset mask:
								quality = player.Player.clothing.maskQuality;
								type = ClothingType.Mask;
								break;
							case ItemGlassesAsset glasses:
								quality = player.Player.clothing.glassesQuality;
								type = ClothingType.Glasses;
								break;
							case ItemBackpackAsset backpack:
								quality = player.Player.clothing.backpackQuality;
								type = ClothingType.Backpack;
								break;
							case ItemVestAsset vest:
								quality = player.Player.clothing.vestQuality;
								type = ClothingType.Vest;
								break;
							case ItemShirtAsset shirt:
								quality = player.Player.clothing.shirtQuality;
								type = ClothingType.Shirt;
								break;
							case ItemPantsAsset pants:
								quality = player.Player.clothing.pantsQuality;
								type = ClothingType.Pants;
								break;
                        }
						foundItems.Add(new KeyValuePair<ClothingType, Item>(type, new Item(item.id, true, quality)));
					}
                }
				returnv = true;
			}
			catch (Exception e)
			{
				Logger.Log("There was an error getting clothes from " + player.CharacterName + "'.  Here is the error.");
				Console.Write(e);
			}
			return returnv;
		}
		
		public bool ClearAll(UnturnedPlayer player)
        {
			return ClearInv(player) && ClearClothes(player);
		}
		public bool ClearInv(UnturnedPlayer player)
		{
			bool returnv = false;
			try
			{
				player.Player.equipment.dequip();
				for (byte p = 0; p < PlayerInventory.PAGES; p++)
				{
					if (player.Inventory.items[p] == null) continue;
					byte itemc = player.Player.inventory.getItemCount(p);
					if (itemc > 0)
					{
						for (byte p1 = 0; p1 < itemc; p1++)
						{
							player.Player.inventory.removeItem(p, 0);
						}
					}
				}
				player.Player.inventory.channel.send("tellSlot", ESteamCall.ALL, ESteamPacket.UPDATE_RELIABLE_BUFFER, new object[]
												{
					(byte)0,
					(byte)0,
					new byte[0]
				});
				player.Player.inventory.channel.send("tellSlot", ESteamCall.ALL, ESteamPacket.UPDATE_RELIABLE_BUFFER, new object[]
												{
					(byte)1,
					(byte)0,
					new byte[0]
				});
				returnv = true;
			}
			catch (Exception e)
			{
				Logger.Log("There was an error clearing " + player.CharacterName + "'s inventory.  Here is the error.");
				Console.Write(e);
			}
			return returnv;
		}
		public bool ClearClothes(UnturnedPlayer player)
		{
			bool returnv = false;
			try
			{
				player.Player.clothing.askWearBackpack(0, 0, new byte[0],true);
				for (byte p2 = 0; p2 < player.Player.inventory.getItemCount(2); p2++)
				{
					player.Player.inventory.removeItem(2, 0);
				}
				player.Player.clothing.askWearGlasses(0, 0, new byte[0], true);
                for (byte p2 = 0; p2 < player.Player.inventory.getItemCount(2); p2++)
				{
					player.Player.inventory.removeItem(2, 0);
				}
				player.Player.clothing.askWearHat(0, 0, new byte[0], true);
                for (byte p2 = 0; p2 < player.Player.inventory.getItemCount(2); p2++)
				{
					player.Player.inventory.removeItem(2, 0);
				}
				player.Player.clothing.askWearMask(0, 0, new byte[0], true);
                for (byte p2 = 0; p2 < player.Player.inventory.getItemCount(2); p2++)
				{
					player.Player.inventory.removeItem(2, 0);
				}
				player.Player.clothing.askWearPants(0, 0, new byte[0], true);
                for (byte p2 = 0; p2 < player.Player.inventory.getItemCount(2); p2++)
				{
					player.Player.inventory.removeItem(2, 0);
				}
				player.Player.clothing.askWearShirt(0, 0, new byte[0], true);
                for (byte p2 = 0; p2 < player.Player.inventory.getItemCount(2); p2++)
				{
					player.Player.inventory.removeItem(2, 0);
				}
				player.Player.clothing.askWearVest(0, 0, new byte[0], true);
                for (byte p2 = 0; p2 < player.Player.inventory.getItemCount(2); p2++)
				{
					player.Player.inventory.removeItem(2, 0);
				}
				returnv = true;
			}
			catch (Exception e)
			{
				Logger.Log("There was an error clearing " + player.CharacterName + "'s inventory.  Here is the error.");
				Console.Write(e);
			}
			return returnv;
		}
		public enum ClothingType
        {
			None,
			Hat,
			Mask,
			Glasses,
			Backpack,
			Vest,
			Shirt,
			Pants
        }
	}
}

