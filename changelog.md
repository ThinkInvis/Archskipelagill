## Changelog

### 1.0.0-alpha1
- The client can now remember received items while disconnected and closed, and remember checks to send next time connection is restored.
- The client will no longer attempt to send checks multiple times if completed multiple times (unsure if this was having any effect, but better safe than sorry).
- The client now causes the game to use an alternate save file location while installed. You can add a suffix to this file via the mod's BepInEx config file, e.g. for participating in multiple Archipelago runs at once.
- The Bonus Gill filler item now has an implemented effect ingame. Traps are still NYI.
- Fixed an issue causing the game to crash/stall on close while the client is installed.
- Fixed an incorrect plugin GUID on the client.

### 1.0.0-alpha
- Initial release. A Minimum Working Example of the client and apworld.
