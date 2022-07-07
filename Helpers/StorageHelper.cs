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
		public static void fitStorage(InteractableStorage storage, ItemStorageAsset asset)
		{
			byte sizeX = asset.storage_x;
			byte sizeY = 1;

			byte foundX;
			byte foundY;
			byte foundRot;

			while (storage.items.tryFindSpace(sizeX, sizeY, out foundX, out foundY, out foundRot))
			{
				if (foundRot == 0)
				{
					if(foundY == 0)
                    {
						foundY = 1;
                    }
					sizeX = SleepingPlayers.Conf.StorageWidth;
					StateUpdated backup = storage.items.onStateUpdated;
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
	}
}
