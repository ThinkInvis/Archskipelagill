from __future__ import annotations

from typing import TYPE_CHECKING

from BaseClasses import Item, ItemClassification

if TYPE_CHECKING:
    from .world import SkigillWorld

from .scraped_game_data import WEAPON_NAMES, STARTER_WEAPON_NAMES, UNLOCK_WEAPON_NAMES

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
    "Trap: Scramble Stats": ItemClassification.trap,
    "Trap: Flash Mob": ItemClassification.trap,
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
        return world.random.choices(
            ["Trap: Damage", "Trap: Pull Enemies", "Trap: Weapon Jam", "Trap: Drain Ski", "Trap: Scramble Stats", "Trap: Flash Mob", "Trap: Stronger Enemies"],
            [
                world.options.trap_weight_damage,
                world.options.trap_weight_pull_enemies,
                world.options.trap_weight_weapon_jam,
                world.options.trap_weight_drain_ski,
                world.options.trap_weight_scramble_stats,
                world.options.trap_weight_flash_mob,
                world.options.trap_weight_stronger_enemies
            ])[0]
    return "Bonus Gill"

def create_item_with_correct_classification(world: SkigillWorld, name: str) -> SkigillItem:
    classification = DEFAULT_ITEM_CLASSIFICATIONS[name]

    if world.options.goal_type > 3 and name == "Progressive Difficulty":
        classification = ItemClassification.progression | ItemClassification.useful

    return SkigillItem(name, classification, ITEM_NAME_TO_ID[name], world.player)


def create_all_items(world: SkigillWorld) -> None:
    world.starting_character_name = "Mage"
    if world.options.random_start_char:
        world.starting_character_name = world.random.choice(["Mage", "Strongman", "Fox", "Prototype", "Dragon", "Dwarves"])
    
    reglist = [
        world.create_item("Skigill Region: Mage"),
        world.create_item("Skigill Region: Strongman"),
        world.create_item("Skigill Region: Fox"),
        world.create_item("Skigill Region: Prototype"),
        world.create_item("Skigill Region: Dwarves"),
        world.create_item("Skigill Region: Dragon")];
        
    charlist = [
        world.create_item("Character: Mage"),
        world.create_item("Character: Strongman"),
        world.create_item("Character: Fox"),
        world.create_item("Character: Prototype"),
        world.create_item("Character: Dwarves"),
        world.create_item("Character: Dragon")];
        
    itempool: list[Item] = [
        world.create_item("Skigill Region: Bosses"),
        world.create_item("Final Boss Key")
    ]
    
    for item in reglist:
        if world.starting_character_name in item.name:
            world.push_precollected(item)
        else:
            itempool.extend([item])
    
    for item in charlist:
        if not world.options.itemize_characters or world.starting_character_name in item.name:
            world.push_precollected(item)
        else:
            itempool.extend([item])
    
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
        
    if world.options.random_start_weapon:
        i = 0
        rwnames = WEAPON_NAMES.copy()
        world.random.shuffle(rwnames)
        for wn in rwnames:
            if i >= 30 or not world.options.itemize_weapons:
                world.push_precollected(world.create_item(f"Weapon: {wn}"))
            else:
                itempool.extend([world.create_item(f"Weapon: {wn}")])
            i += 1
    else:
        for wn in DEFAULT_STARTER_WEAPON_BY_CHARACTER: # first 6 are used to replace character starter weapons by client
            world.push_precollected(world.create_item(f"Weapon: {wn}"))
        for wn in STARTER_WEAPON_NAMES:
            if wn in DEFAULT_STARTER_WEAPON_BY_CHARACTER:
                continue
            world.push_precollected(world.create_item(f"Weapon: {wn}"))
                
        weaponlist = []
        
        for wn in UNLOCK_WEAPON_NAMES:
            weaponlist.append(world.create_item(f"Weapon: {wn}"))
            
        if world.options.itemize_weapons:
            itempool.extend(weaponlist)
        else:
            for item in weaponlist:
                world.push_precollected(item)
            
    number_of_items = len(itempool)
    number_of_unfilled_locations = len(world.multiworld.get_unfilled_locations(world.player))
    needed_number_of_filler_items = number_of_unfilled_locations - number_of_items
    itempool += [world.create_filler() for _ in range(needed_number_of_filler_items)]
    world.multiworld.itempool += itempool