using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Rocket.API;

namespace SpeedMann.SleepingPlayers
{
	public class SleepingPlayerConfiguration : IRocketPluginConfiguration
	{
		public ushort SleepingPlayerStorageId;
		public bool Debug;
		public byte StorageWidth = 10;
		public byte StorageHeight = 25;
		public bool AutoResize = true;
		public bool AllowEmptySleepingPlayers = true;
		public bool AllowSleepingPlayersInSafezone = true;
		public bool IgnoreAdmins = false;
		[XmlArrayItem(ElementName = "CSteamID")]
		public List<ulong> UnsavedPlayers = new List<ulong>();

		public SleepingPlayerConfiguration ()
		{

		}

		public void LoadDefaults()
		{
			Debug = false;
			SleepingPlayerStorageId = 52200;
			StorageWidth = 10;
			StorageHeight = 25;
			AutoResize = true;
			AllowEmptySleepingPlayers = true;
			AllowSleepingPlayersInSafezone = true;
			IgnoreAdmins = false;
		}
	}
}

