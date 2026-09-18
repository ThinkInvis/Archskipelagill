from dataclasses import dataclass

from Options import Choice, OptionGroup, PerGameCommonOptions, Range, Toggle, DefaultOnToggle

class ItemizeCharacters(DefaultOnToggle):
    """
    If enabled, character unlocks will be itemized through Archipelago; unlocking them on the Meta Tree will do nothing until they are unlocked through Archipelago. If disabled, these items will be added to starting inventory.
    """
    
    display_name = "Itemize Characters"

class ItemizeWeapons(DefaultOnToggle):
    """
    If enabled, weapon unlocks will be itemized through Archipelago; unlocking them on the Meta Tree will do nothing until they are unlocked through Archipelago. If disabled, these items will be added to starting inventory.
    """
    
    display_name = "Itemize Weapons"

class ItemizeDifficulty(Toggle):
    """
    If enabled, difficulty unlocks will be itemized through Archipelago; unlocking them by winning runs will do nothing until they are unlocked through Archipelago. If disabled, these items will be added to starting inventory.
    """
    
    display_name = "Itemize Difficulty"

class RegionChecksNeedCharacter(Toggle):
    """
    If enabled, checks within a given region will be out of logic until you have the character for that region. If disabled, checking locations in distant regions may be much more difficult; but if enabled, it may be easy to get some out-of-logic items.
    """
    
    display_name = "Region Checks Need Character"

class TrapChance(Range):
    """
    Percentage chance that any filler item will be replaced by a trap.
    """

    display_name = "Trap Chance"

    range_start = 0
    range_end = 100
    default = 0

class GoalType(Choice):
    """
    Which win condition to use.
    """

    display_name = "Goal Type"

    option_anyboss = 0
    option_finalboss = 1
    option_allbossonerun = 2
    option_anybossdiff7 = 3
    option_finalbossdiff7 = 4
    option_allbossonerundiff7 = 5

    default = option_finalboss

@dataclass
class SkigillOptions(PerGameCommonOptions):
    itemize_characters: ItemizeCharacters
    itemize_weapons: ItemizeWeapons
    itemize_difficulty: ItemizeDifficulty
    region_checks_need_character: RegionChecksNeedCharacter
    trap_chance: TrapChance
    goal_type: GoalType


option_groups = [
    OptionGroup(
        "Item Distribution",
        [ItemizeCharacters, ItemizeWeapons, ItemizeDifficulty, TrapChance],
    ),
    OptionGroup(
        "Logic and Goals",
        [RegionChecksNeedCharacter, GoalType],
    ),
]

option_presets = {
    "default": {
        "itemize_characters": True,
        "itemize_weapons": True,
        "itemize_difficulty": False,
        "region_checks_need_character": False,
        "trap_chance": 0,
        "goal_type": GoalType.option_finalboss
    }
}
