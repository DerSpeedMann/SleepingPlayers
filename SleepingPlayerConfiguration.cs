using System;
using Rocket.API;

namespace SpeedMann.SleepingPlayers
{
	public class SleepingPlayerConfiguration : IRocketPluginConfiguration
	{
		public ushort SleepingPlayerStorageId;
		public bool Debug;
		public byte StorageHeight;
		public bool AutoResize;

		public SleepingPlayerConfiguration ()
		{

		}

		public void LoadDefaults()
		{
			SleepingPlayerStorageId = 52200;
			StorageHeight = 25;
			AutoResize = true;
			Debug = false;
		}
	}
}

