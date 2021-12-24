using System;
using Rocket.API;

namespace SpeedMann.SleepingPlayers
{
	public class SleepingPlayerConfiguration : IRocketPluginConfiguration
	{
		public ushort SleepingPlayerStorageId;
		public bool Debug;
		public SleepingPlayerConfiguration ()
		{

		}

		public void LoadDefaults()
		{
			SleepingPlayerStorageId = 52200;
			Debug = false;
		}
	}
}

