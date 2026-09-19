from __future__ import annotations

from typing import TYPE_CHECKING

from BaseClasses import Item, ItemClassification

if TYPE_CHECKING:
    from .world import SkigillWorld

from .skilltree_data import WEAPON_NAMES, STARTER_WEAPON_NAMES, UNLOCK_WEAPON_NAMES

# Note: do not use | or ; characters in item names, used as delimiters by client plugin save/load script

ITEM_NAME_TO_ID = {
    "Bonus Gill": 1,
    "Skigill Region: Mage": 2,
    "Skigill Region: Strongman": 3,
    "Skigill Region: Fox": 4,
    "Skigill Region: Prototype": 5,
    "Skigill Region: Dwarves": 6,
    "Skigill Region: Dragon": 7,
    "Skigill Region: Bosses": 8,
    "Final Boss Key": 9,
    "Progressive Difficulty": 10,
    "Character: Mage": 11, # NYI, always available
    "Character: Strongman": 12,
    "Character: Fox": 13,
    "Character: Prototype": 14,
    "Character: Dwarves": 15,
    "Character: Dragon": 16,
    "Endless Mode": 17,
    "Trap: Damage": 18,
    "Trap: Pull Enemies": 19,
    "Trap: Weapon Jam": 20,
    "Trap: Drain Ski": 21,
    "Trap: Scramble Stats": 22,
    "Trap: Flash Mob": 23,
    "Trap: Stronger Enemies": 24
}
    
DEFAULT_ITEM_CLASSIFICATIONS = {
    "Bonus Gill": ItemClassification.filler,
    "Skigill Region: Mage": ItemClassification.progression,
    "Skigill Region: Strongman": ItemClassification.progression,
    "Skigill Region: Fox": ItemClassification.progression,
    "Skigill Region: Prototype": ItemClassification.progression,
    "Skigill Region: Dwarves": ItemClassification.progression,
    "Skigill Region: Dragon": ItemClassification.progression,
    "Skigill Region: Bosses": ItemClassification.progression,
    "Final Boss Key": ItemClassification.progression,
    "Progressive Difficulty": ItemClassification.useful,
    "Character: Mage": ItemClassification.progression | ItemClassification.useful, # NYI, always available
    "Character: Strongman": ItemClassification.progression | ItemClassification.useful,
    "Character: Fox": ItemClassification.progression | ItemClassification.useful,
    "Character: Prototype": ItemClassification.progression | ItemClassification.useful,
    "Character: Dwarves": ItemClassification.progression | ItemClassification.useful,
    "Character: Dragon": ItemClassification.progression | ItemClassification.useful,
    "Endless Mode": ItemClassification.filler,
    "Trap: Damage": ItemClassification.trap,
    "Trap: Pull Enemies": ItemClassification.trap,
    "Trap: Weapon Jam": ItemClassification.trap,
    "Trap: Drain Ski": ItemClassification.trap,
    "Trap: Drain Gill": ItemClassification.trap,
    "Trap: Slow Movement": ItemClassification.trap,
    "Trap: Stronger Enemies": ItemClassification.trap
}

locNameInd = len(ITEM_NAME_TO_ID)

for wn in WEAPON_NAMES:
    wns = f"Weapon: {wn}"
    ITEM_NAME_TO_ID[wns] = locNameInd
    DEFAULT_ITEM_CLASSIFICATIONS[wns] = ItemClassification.progression | ItemClassification.useful
    locNameInd += 1
    
class SkigillItem(Item):
    game = "Skigill"

def get_random_filler_item_name(world: SkigillWorld) -> str:
    if world.random.randint(0, 99) < world.options.trap_chance:
        return list(ITEM_NAME_TO_ID.keys())[world.random.randrange(18, 24)]
    return "Bonus Gill"

def create_item_with_correct_classification(world: SkigillWorld, name: str) -> SkigillItem:
    classification = DEFAULT_ITEM_CLASSIFICATIONS[name]

    if world.options.goal_type > 3 and name == "Progressive Difficulty":
        classification = ItemClassification.progression | ItemClassification.useful

    return SkigillItem(name, classification, ITEM_NAME_TO_ID[name], world.player)


def create_all_items(world: SkigillWorld) -> None:
    itempool: list[Item] = [
        world.create_item("Skigill Region: Strongman"),
        world.create_item("Skigill Region: Fox"),
        world.create_item("Skigill Region: Prototype"),
        world.create_item("Skigill Region: Dwarves"),
        world.create_item("Skigill Region: Dragon"),
        world.create_item("Skigill Region: Bosses"),
        world.create_item("Final Boss Key")
    ]
    
    if world.options.itemize_endless_mode:
        itempool.extend([world.create_item("Endless Mode")])
    else:
        world.push_precollected(world.create_item("Endless Mode"))
        
    if world.options.itemize_difficulty:
        itempool.extend([
            world.create_item("Progressive Difficulty"),
            world.create_item("Progressive Difficulty"),
            world.create_item("Progressive Difficulty"),
            world.create_item("Progressive Difficulty"),
            world.create_item("Progressive Difficulty"),
            world.create_item("Progressive Difficulty")])
    else:
        world.push_precollected(world.create_item("Progressive Difficulty"))
        world.push_precollected(world.create_item("Progressive Difficulty"))
        world.push_precollected(world.create_item("Progressive Difficulty"))
        world.push_precollected(world.create_item("Progressive Difficulty"))
        world.push_precollected(world.create_item("Progressive Difficulty"))
        world.push_precollected(world.create_item("Progressive Difficulty"))
        
    for wn in STARTER_WEAPON_NAMES:
        world.push_precollected(world.create_item(f"Weapon: {wn}"))
            
    weaponlist = []
    
    for wn in UNLOCK_WEAPON_NAMES:
        weaponlist.append(world.create_item(f"Weapon: {wn}"))
        
    if world.options.itemize_weapons:
        itempool.extend(weaponlist)
    else:
        for item in weaponlist:
            world.push_precollected(item)
        
    charlist = [
        world.create_item("Character: Strongman"),
        world.create_item("Character: Fox"),
        world.create_item("Character: Prototype"),
        world.create_item("Character: Dwarves"),
        world.create_item("Character: Dragon")];
        
    world.push_precollected(world.create_item("Character: Mage"))
    world.push_precollected(world.create_item("Skigill Region: Mage"))
    
    if world.options.itemize_characters:
        itempool.extend(charlist)
    else:
        for item in charlist:
            world.push_precollected(item)
            
    number_of_items = len(itempool)
    number_of_unfilled_locations = len(world.multiworld.get_unfilled_locations(world.player))
    needed_number_of_filler_items = number_of_unfilled_locations - number_of_items
    itempool += [world.create_filler() for _ in range(needed_number_of_filler_items)]
    world.multiworld.itempool += itempool