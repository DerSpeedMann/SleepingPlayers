# SleepingPlayers

**When logging off your character with all its items will stay on the server.**  
Your Inventory can be looted and you can be killed when not logging out in a secure spot.

This Plugin was created to stop people from logging off with their best gear or store it on multiple characters to not lose it when being raided. This makes secure bases a must have!

The new version also supports plugin reloading to a certain degree and prevents item loss on server crash or unproper shutdown!

The plugin should be used with this [Mod](https://steamcommunity.com/sharedfiles/filedetails/?id=2684315468)!

If you find any bugs, please contact me on [Discord](https://discord.gg/G3Bj2f4SFE) SpeedMann#7437

Permissions
----------------
"sleepingplayer.bypass" allows to bypass SleepingPlayers (players with this permission do not spawn a SleepingPlayer)

General Settings
----------------

**SleepingPlayerStorageId**: Allows selecting the preferred Barricade by id  
The SleepingPlayerStorageId should only be changed on whipe!

If you use your own barricade make sure that it has:  
- Useable Barricade
- Build Storage
-  Storage_X 10 (10 is max to still be visible on most resolutions)
- Storage_Y 25 (storage size 250 is recommended for Vanilla)
- Vulnerable (to allow damage by any means)
- **DONT ADD!** Unpickupable (this would break the plugin because owner would not be set)

**StorageWidth**: Sets the default width of all SleepingPlayers  
**StorageHeight**: Sets the default / max height of all SleepingPlayers  
This needs to be big enough to hold all items of a player including primary / secondary weapon and all clothing items!

**AutoResize**: AutoResize fits the storage size to the stored items  
This reduces the height of the storage to the minimum considering the stored items

**AllowEmptySleepingPlayers**: If players with empty inventory (no items and no clothing) stay on the server as SleepingPlayers  
**AllowSleepingPlayersInSafezone**: If players logging off in a safezone should stay on the server as SleepingPlayers
**IgnoreAdmins**: If admins should spawn SleepingPlayers
**UnsavedPlayers**: this list is automatically updated by the plugin and keeps track off all players that did disconnect witout a SleepingPlayer  
This also prevents item loss on server crash!

**Debug**: Enables debug output.
