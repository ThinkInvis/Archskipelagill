## Changelog

### 1.0.0-alpha4

- Added a new Archipelago connection panel and a new submenu on the main menu
- Added a new Archipelago console, replacing the previous nonfunctional implementation
- Skigill Regions that are unreachable but unlocked will now appear locked
- Added audiovisual notifications for receiving items, sending location checks, and sending goals
  - These notifications will be queued and only appear once in an unpaused run or the Meta Tree
- Added visual indicators for when game start options or meta tree weapons are locked behind an unobtained Archipelago item
- Added visual indicators for unchecked locations (chests/perks, weapons)
- Fixed incorrect characters being identified for unlock/escape conditions
- Backend:
  - The client will no longer even attempt to send checks that aren't active/valid on the server
  - Fixed a probably harmful error that could sometimes occur because skill tree itemization was trying to activate on the meta tree
  - Client will no longer send all checks, including previously unsent checks twice, every single time it connects; now only previously unsent checks will be sent once
  - Slightly improved performance by replacing/caching some uses of GameObject.Find

### 1.0.0-alpha3

- Implemented client effects for Trap items:
  - Damage
  - Pull Enemies
  - Weapon Jam
  - Drain Ski
  - Scramble Stats
  - Flash Mob
  - Stronger Enemies
  - (Note: Some planned traps already implemented by name in the APWorld have been removed/replaced)
- Implemented the Endless Mode item
- Added APWorld options to en/disable each category of locations
- Added an APWorld option to limit boss region access behind having every other region, enabled by default
- Fixed Skigill nodes being assigned to incorrect regions (region index order was wrong)
- Fixed access to final boss node not being blocked behind Final Boss Key item
- Improved appearance of locked Skigill nodes and connections
- Reduced/removed some unnecessary console logs
- Backend:
  - Made retrieving saved item counts much easier
  - Removed a lot of redundant code in SkillTreeItemizer
  - Vast improvements to game data scraper
  - Suppressed an intermittent "Remove unnecessary suppression" message applying to private methods used by UnityEngine
  - Added a primary readme to the repo

### 1.0.0-alpha2

- Fixed goals not being sent
- Fixed boss region not unlocking after obtaining the relevant region item
- Added a tool to generate zipped apworld files automatically
- Added some extra debug tools to the client panel

### 1.0.0-alpha1
- The client can now remember received items while disconnected and closed, and remember checks to send next time connection is restored.
- The client will no longer attempt to send checks multiple times if completed multiple times (unsure if this was having any effect, but better safe than sorry).
- The client now causes the game to use an alternate save file location while installed. You can add a suffix to this file via the mod's BepInEx config file, e.g. for participating in multiple Archipelago runs at once.
- The Bonus Gill filler item now has an implemented effect ingame. Traps are still NYI.
- Fixed an issue causing the game to crash/stall on close while the client is installed.
- Fixed an incorrect plugin GUID on the client.

### 1.0.0-alpha
- Initial release. A Minimum Working Example of the client and apworld.
