# SleepingPlayers

When logging off your character with all its items will stay on the server.

Your Inventory can be looted and you can be killed when not logging out in a secure spot.


This Plugin was created to stop people from logging off with theyr best gear or store it on multiple characters to not lose it when being raided.

This makes secure bases a must have!


The plugin should be used with this [mod](https://steamcommunity.com/sharedfiles/filedetails/?id=2684315468)

## Settings

- __SleepingPlayerStorageId__: Allows selecting the preferred Barricade by id
  - If you use your own barricade make sure that it has:

    - Useable Barricade
    - Build Storage
    - Storage_X 10 (10 is max to still be visible on most resolutions)
    - Storage_Y 25 (storage size 250 is recommended for Vanilla)
    - Vulnerable (to allow damage by any means)
    - __DONT ADD!__ Unpickupable (this would break the plugin because owner would not be set)

- __StorageHeight__: Sets the default / max height of all new SleepingPlayers
 
  - This needs to be big enough to hold all items of a player including primary / secondary weapon and all clothing items

- __AutoResize__: AutoResize reduces the height of the storage to the minimum considering the stored items
 
  - This fits the storage size to the stored items

- __Debug__: Enables debug output.
