# Archskipelagill

## Where is the options page?

The [player options page for this game](../player-options) contains all the options you need to configure and export a
config file.

## What is Skigill?

[Skigill](https://store.steampowered.com/app/3657180/Skigill/) is a game released on Steam in early 2026 by Achromi. It's a Survivors-like -- a top-down single-stick shooter where aiming and shooting happens automatically, and enemies spawn in waves from the edges of the screen. What makes this game unique is that its map/playfield is a gigantic skill tree! Purchases are made by standing on nodes of the tree for a couple seconds (a possibly dangerous proposition when being constantly swarmed by enemies), granting you extra stats, weapons, and perks.

Archskipelagill currently exists as a Custom implementation and must be installed to Archipelago manually.

## What does Archskipelagill do?

This Archipelago mod currently implements the following changes to its game:

### Goals

- Defeat any boss once, excluding the final boss
- Defeat the final boss
- Defeat all bosses in one run ("I'm the boss now" achievement)
- Any of the above goals with an added Difficulty 7 requirement

### Items

Items cannot be configured; use Starting Inventory instead.

- (7) Unlock access to a region of the Skigill corresponding to each character (other than the starting character), the bosses, or the final boss node
- (5) Unlock access to each character (other than the starting character)
- (30) Unlock access to each weapon (other than the starting weapons)
- (6) Unlock access to each difficulty level
- (1) Unlock access to Endless Mode (NYI)
- (Filler) Gain bonus Gill based on progression (NYI)

### Locations/Checks

- (6) Escape on each character
- (60) Escape with each weapon equipped
- (51) Collect each Chest node on the Skigill
- (42) Collect each Perk node on the Skigill
- (6) Defeat each boss

The following checks are only included if an equal or harder goal is chosen:
- (1) Defeat the final boss
- (1) Defeat all 6 bosses in one run
- (6) Defeat each boss on Difficulty 7
- (1) Defeat the final boss on Difficulty 7
- (1) Defeat all 6 bosses in one run on Difficulty 7

### Traps

Traps are disabled by default and must be configured to appear.

- NYI! Traps are a work in progress and will do nothing if enabled.

### Credits

- The Archipelago project itself, and its excellent guides/examples for implementing an APWorld!
  - The APQuest world, used as a basis for this mod's APWorld implementation
- alwaysintreble's [ArchipelagoBepInExPluginTemplate](https://github.com/alwaysintreble/ArchipelagoBepInExPluginTemplate), which is the basis for this mod's Unity plugin