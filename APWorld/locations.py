from __future__ import annotations

from typing import TYPE_CHECKING

from BaseClasses import ItemClassification, Location, Region

from itertools import islice

from . import items
from .game_data import SkillNodeType, SkillNodeRegion, SkillNode
from .scraped_game_data import WEAPON_NAMES, SKILL_TREE

if TYPE_CHECKING:
    from .world import SkigillWorld

LOCATION_NAME_TO_ID = {
    "Defeated Gari": 1,
    "Defeated Bouboul": 2,
    "Defeated Jello": 3,
    "Defeated Roger": 4,
    "Defeated Pilpou": 5,
    "Defeated Rosa": 6,
    "Defeated Final Boss": 7,
    "I'm The Boss Now": 8,
    "Defeated Gari on Difficulty 7": 9,
    "Defeated Bouboul on Difficulty 7": 10,
    "Defeated Jello on Difficulty 7": 11,
    "Defeated Roger on Difficulty 7": 12,
    "Defeated Pilpou on Difficulty 7": 13,
    "Defeated Rosa on Difficulty 7": 14,
    "Defeated Final Boss on Difficulty 7": 15,
    "I'm The Boss Now on Difficulty 7": 16,
    "Escaped with Mage": 17,
    "Escaped with Strongman": 18,
    "Escaped with Fox": 19,
    "Escaped with Prototype": 20,
    "Escaped with Dwarves": 21,
    "Escaped with Dragon": 22
}

locNameInd = len(LOCATION_NAME_TO_ID) + 1

for node in [n for n in SKILL_TREE if n.type == SkillNodeType.CHEST]:
    LOCATION_NAME_TO_ID[f"Skigill Chest #{node.chest_index + 1} ({node.region.name})"] = locNameInd
    locNameInd += 1
    
for node in [n for n in SKILL_TREE if n.type == SkillNodeType.PERK]:
    LOCATION_NAME_TO_ID[f"Skigill Perk #{node.perk_index + 1} ({node.region.name})"] = locNameInd
    locNameInd += 1
    
for i in range(len(WEAPON_NAMES)):
    LOCATION_NAME_TO_ID[f"Escaped with Weapon {WEAPON_NAMES[i]}"] = locNameInd
    locNameInd += 1

class SkigillLocation(Location):
    game = "Skigill"

def get_location_names_with_ids(location_names: list[str]) -> dict[str, int | None]:
    return {location_name: LOCATION_NAME_TO_ID[location_name] for location_name in location_names}

def create_all_locations(world: SkigillWorld) -> None:
    create_regular_locations(world)
    create_events(world)

def create_regular_locations(world: SkigillWorld) -> None:
    regions: dict[SkillNodeRegion, Region] = {
        SkillNodeRegion.MAGE: world.get_region("Mage"),
        SkillNodeRegion.STRONGMAN: world.get_region("Strongman"),
        SkillNodeRegion.FOX: world.get_region("Fox"),
        SkillNodeRegion.PROTOTYPE: world.get_region("Prototype"),
        SkillNodeRegion.DWARVES: world.get_region("Dwarves"),
        SkillNodeRegion.DRAGON: world.get_region("Dragon"),
        SkillNodeRegion.BOSSES: world.get_region("Bosses"),
    }
    
    if world.options.check_chests:
        for node in [n for n in SKILL_TREE if n.type == SkillNodeType.CHEST]:
            regions[node.region].add_locations(get_location_names_with_ids([f"Skigill Chest #{node.chest_index + 1} ({node.region.name})"]), SkigillLocation)
        
    if world.options.check_perks:
        for node in [n for n in SKILL_TREE if n.type == SkillNodeType.PERK]:
            regions[node.region].add_locations(get_location_names_with_ids([f"Skigill Perk #{node.perk_index + 1} ({node.region.name})"]), SkigillLocation)
        
    if world.options.check_weapon_escapes:
        regions[SkillNodeRegion.MAGE].add_locations(get_location_names_with_ids([f"Escaped with Weapon {n}" for n in WEAPON_NAMES]), SkigillLocation)
        
    if world.options.check_hero_escapes:
        regions[SkillNodeRegion.MAGE].add_locations(get_location_names_with_ids(["Escaped with Mage"]), SkigillLocation)
        regions[SkillNodeRegion.STRONGMAN].add_locations(get_location_names_with_ids(["Escaped with Strongman"]), SkigillLocation)
        regions[SkillNodeRegion.FOX].add_locations(get_location_names_with_ids(["Escaped with Fox"]), SkigillLocation)
        regions[SkillNodeRegion.PROTOTYPE].add_locations(get_location_names_with_ids(["Escaped with Prototype"]), SkigillLocation)
        regions[SkillNodeRegion.DWARVES].add_locations(get_location_names_with_ids(["Escaped with Dwarves"]), SkigillLocation)
        regions[SkillNodeRegion.DRAGON].add_locations(get_location_names_with_ids(["Escaped with Dragon"]), SkigillLocation)
            
    lnidList = list(LOCATION_NAME_TO_ID.keys())
    regions[SkillNodeRegion.BOSSES].add_locations(get_location_names_with_ids(lnidList[0:6]), SkigillLocation)

    if world.options.goal_type > 0:
        regions[SkillNodeRegion.BOSSES].add_locations(get_location_names_with_ids(["Defeated Final Boss"]), SkigillLocation)

    if world.options.goal_type > 1:
        regions[SkillNodeRegion.BOSSES].add_locations(get_location_names_with_ids(["I'm The Boss Now"]), SkigillLocation)

    if world.options.goal_type > 2:
        regions[SkillNodeRegion.BOSSES].add_locations(get_location_names_with_ids(lnidList[9:16]), SkigillLocation)

    if world.options.goal_type > 3:
        regions[SkillNodeRegion.BOSSES].add_locations(get_location_names_with_ids(["Defeated Final Boss on Difficulty 7"]), SkigillLocation)

    if world.options.goal_type > 4:
        regions[SkillNodeRegion.BOSSES].add_locations(get_location_names_with_ids(["I'm The Boss Now on Difficulty 7"]), SkigillLocation)

def create_events(world: SkigillWorld) -> None:
    boss_region = world.get_region("Bosses")

    match world.options.goal_type:
        case 0:
            boss_region.add_event("Defeated Rosa", "Victory", location_type=SkigillLocation, item_type=items.SkigillItem)
            boss_region.add_event("Defeated Roger", "Victory", location_type=SkigillLocation, item_type=items.SkigillItem)
            boss_region.add_event("Defeated Jello", "Victory", location_type=SkigillLocation, item_type=items.SkigillItem)
            boss_region.add_event("Defeated Pilpou", "Victory", location_type=SkigillLocation, item_type=items.SkigillItem)
            boss_region.add_event("Defeated Bouboul", "Victory", location_type=SkigillLocation, item_type=items.SkigillItem)
            boss_region.add_event("Defeated Gari", "Victory", location_type=SkigillLocation, item_type=items.SkigillItem)
        case 1:
            boss_region.add_event("Defeated Final Boss", "Victory", location_type=SkigillLocation, item_type=items.SkigillItem)
        case 2:
            boss_region.add_event("I'm The Boss Now", "Victory", location_type=SkigillLocation, item_type=items.SkigillItem)
        case 3:
            boss_region.add_event("Defeated Rosa on Difficulty 7", "Victory", location_type=SkigillLocation, item_type=items.SkigillItem)
            boss_region.add_event("Defeated Roger on Difficulty 7", "Victory", location_type=SkigillLocation, item_type=items.SkigillItem)
            boss_region.add_event("Defeated Jello on Difficulty 7", "Victory", location_type=SkigillLocation, item_type=items.SkigillItem)
            boss_region.add_event("Defeated Pilpou on Difficulty 7", "Victory", location_type=SkigillLocation, item_type=items.SkigillItem)
            boss_region.add_event("Defeated Bouboul on Difficulty 7", "Victory", location_type=SkigillLocation, item_type=items.SkigillItem)
            boss_region.add_event("Defeated Gari on Difficulty 7", "Victory", location_type=SkigillLocation, item_type=items.SkigillItem)
        case 4:
            boss_region.add_event("Defeated Final Boss on Difficulty 7", "Victory", location_type=SkigillLocation, item_type=items.SkigillItem)
        case 5:
            boss_region.add_event("I'm The Boss Now on Difficulty 7", "Victory", location_type=SkigillLocation, item_type=items.SkigillItem)