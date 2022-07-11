using Logger = Rocket.Core.Logging.Logger;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SpeedMann.SleepingPlayers
{
    public class StorageHelper
    {
		public static void fitStorageSize(InteractableStorage storage, ItemStorageAsset asset)
		{
			// resize to fix error with too small storages
			StateUpdated backup = storage.items.onStateUpdated;
			storage.items.onStateUpdated = null;
			storage.items.resize(SleepingPlayers.Conf.StorageWidth, SleepingPlayers.Conf.StorageHeight);
			storage.items.onStateUpdated = backup;

			byte sizeX = SleepingPlayers.Conf.StorageWidth;
			byte sizeY = 1;

			while (storage.items.tryFindSpace(sizeX, sizeY, out byte foundX, out byte foundY, out byte foundRot))
			{
				if (foundRot == 0)
				{
					if(foundY == 0)
                    {
						foundY = 1;
                    }
					sizeX = SleepingPlayers.Conf.StorageWidth;
					storage.items.onStateUpdated = null;
					storage.items.resize(sizeX, foundY);
					storage.items.onStateUpdated = backup;

					if (SleepingPlayers.Conf.Debug)
					{
						Logger.Log("SleepingPlayer storage resized to: [" + sizeX + ", " + foundY + "]");
					}
					break;
				}
				sizeY++;
			}
		}
		public static void setStorageSize(InteractableStorage storage, ItemStorageAsset asset)
		{
			byte sizeX = SleepingPlayers.Conf.StorageWidth;
			byte sizeY = SleepingPlayers.Conf.StorageHeight;

			storage.items.resize(sizeX, sizeY);

			if (SleepingPlayers.Conf.Debug)
			{
				Logger.Log("SleepingPlayer storage resized to: [" + sizeX + ", " + sizeY + "]");
			}
		}
	}
}
