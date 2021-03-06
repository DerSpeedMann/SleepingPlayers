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
                List<KeyValuePair<StorageType, Item>> clothingItems = new List<KeyValuePair<StorageType, Item>>();
				if (!GetClothingItems(player, ref clothingItems))
					return false;

				if(SleepingPlayers.Inst.Configuration.Instance.Debug)
					Logger.Log("Found " + clothingItems.Count + " Clothing items");
				foreach (KeyValuePair<StorageType, Item> item in clothingItems)
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

						switch (item.Key)
						{
							case StorageType.Backpack:
								clearInventoryPage(player, 3);
								player.Player.clothing.thirdClothes.backpack = 0;
								player.Player.clothing.askWearBackpack(0, 0, new byte[0], true);
								break;
							case StorageType.Vest:
								clearInventoryPage(player, 4);
								player.Player.clothing.thirdClothes.vest = 0;
								player.Player.clothing.askWearVest(0, 0, new byte[0], true);
								break;
							case StorageType.Shirt:
								clearInventoryPage(player, 5);
								player.Player.clothing.thirdClothes.shirt = 0;
								player.Player.clothing.askWearShirt(0, 0, new byte[0], true);
								break;
							case StorageType.Pants:
								clearInventoryPage(player, 6);
								player.Player.clothing.thirdClothes.pants = 0;
								player.Player.clothing.askWearPants(0, 0, new byte[0], true);
								break;
							case StorageType.Glasses:
								player.Player.clothing.thirdClothes.glasses = 0;
								player.Player.clothing.askWearGlasses(0, 0, new byte[0], true);
								break;
							case StorageType.Hat:
								player.Player.clothing.thirdClothes.hat = 0;
								player.Player.clothing.askWearHat(0, 0, new byte[0], true);
								break;
							case StorageType.Mask:
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
						if (removedIndexes.Count > 0)
						{
							if (p <= 1)
                            {
								Logger.Log("Removed " + (StorageType)p);
								player.Player.inventory.channel.send("tellSlot", ESteamCall.ALL, ESteamPacket.UPDATE_RELIABLE_BUFFER, new object[]
								{
									p,
									(byte)0,
									new byte[0]
								});
							}	
							else if (p <= 6)
								Logger.Log("Removed " + removedIndexes.Count + " Items in " + (StorageType)p);
							else
								Logger.Log("Removed " + removedIndexes.Count + " Items in page" + p);
						}

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
				if(counter > 0)
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
		public void GetAllSavedItems(Player player, out List<Item> items)
        {

			items = new List<Item>();

			if (PlayerSavedata.fileExists(player.channel.owner.playerID, "/Player/Inventory.dat"))
			{
				
				Block block = PlayerSavedata.readBlock(player.channel.owner.playerID, "/Player/Inventory.dat", 0);
				byte b = block.readByte();
				if (b > 3)
				{
					for (byte b2 = 0; b2 < PlayerInventory.PAGES - 2; b2 += 1)
					{
						// two read byte for storage size which we dont need
						byte width = block.readByte();
						byte height = block.readByte();
						byte b3 = block.readByte();
						for (byte b4 = 0; b4 < b3; b4 += 1)
						{
							byte x = block.readByte();
							byte y = block.readByte();
							byte rot = 0;
							if (b > 4)
							{
								rot = block.readByte();
							}
							ushort num = block.readUInt16();
							byte newAmount = block.readByte();
							byte newQuality = block.readByte();
							byte[] newState = block.readByteArray();
							if (Assets.find(EAssetType.ITEM, num) is ItemAsset)
							{
								items.Add(new Item(num, newAmount, newQuality, newState));
							}
						}
					}
				}
			}

			if (PlayerSavedata.fileExists(player.channel.owner.playerID, "/Player/Clothing.dat"))
			{
				Block block = PlayerSavedata.readBlock(player.channel.owner.playerID, "/Player/Clothing.dat", 0);
				byte b = block.readByte();
				if (b > 1)
				{
					List<Item> clothing = new List<Item>();
					

					for(int i = 0; i < 7; i++)
                    {
						ushort id = 0;
						if (b > 6)
						{
							Guid guid = block.readGUID();
							id = Assets.find(guid)?.id ?? 0;
						}
						else
						{
							id = block.readUInt16();
						}
						byte quality = block.readByte();
						clothing.Add(new Item(id, true, quality));
					}
					
					if (b > 2)
					{
						bool isVisual = block.readBoolean();
					}
					if (b > 5)
					{
						bool isSkinned = block.readBoolean();
						bool isMythic = block.readBoolean();
					}

					if (b > 4)
					{
						foreach(Item item in clothing)
                        {
							item.state = block.readByteArray();
						}
					}
					else
					{
						if (clothing[clothing.Count-1].id == 334)
						{
							clothing[clothing.Count - 1].state = new byte[1];
						}
					}
					foreach (Item item in clothing)
					{
						if(item.id != 0)
                        {
							items.Add(item);
                        }
					}
				}
			}
		}
		public bool GetAllItems(UnturnedPlayer player, ref List<Item> foundItems)
        {
			List<KeyValuePair<StorageType, Item>> foundClothings = new List<KeyValuePair<StorageType, Item>>();
			if(GetClothingItems(player, ref foundClothings))
            {
				foreach (KeyValuePair<StorageType, Item> item in foundClothings)
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
				Logger.Log("There was an error getting items from " + player.CharacterName + "'s inventory.  Here is the error.");
				Console.Write(e);
			}
            return returnv;
        }
		public bool GetClothingItems(UnturnedPlayer player, ref List<KeyValuePair<StorageType, Item>> foundItems)
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
						StorageType type = StorageType.Unknown;

						switch (item)
                        {
							case ItemHatAsset hat:
								quality = player.Player.clothing.hatQuality;
								type = StorageType.Hat;
								break;
							case ItemMaskAsset mask:
								quality = player.Player.clothing.maskQuality;
								type = StorageType.Mask;
								break;
							case ItemGlassesAsset glasses:
								quality = player.Player.clothing.glassesQuality;
								type = StorageType.Glasses;
								break;
							case ItemBackpackAsset backpack:
								quality = player.Player.clothing.backpackQuality;
								type = StorageType.Backpack;
								break;
							case ItemVestAsset vest:
								quality = player.Player.clothing.vestQuality;
								type = StorageType.Vest;
								break;
							case ItemShirtAsset shirt:
								quality = player.Player.clothing.shirtQuality;
								type = StorageType.Shirt;
								break;
							case ItemPantsAsset pants:
								quality = player.Player.clothing.pantsQuality;
								type = StorageType.Pants;
								break;
                        }
						foundItems.Add(new KeyValuePair<StorageType, Item>(type, new Item(item.id, true, quality)));
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
			byte oldWidth = player.Inventory.items[2].width;
			byte oldHeight = player.Inventory.items[2].height;

			player.Inventory.items[2].resize(10, 10);

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
			finally
			{
				player.Inventory.items[2].resize(oldWidth, oldHeight);
			}

			return returnv;
		}
		public enum StorageType
        {
			PrimaryWeapon = 0,
			SecondaryWeapon = 1,
			Hands = 2,
			Backpack = 3,
			Vest = 4,
			Shirt = 5,
			Pants = 6,
			Hat = 7,
			Mask = 8,
			Glasses = 9,
			Unknown = 10,
		}
	}
}

