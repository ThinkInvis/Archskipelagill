from dataclasses import dataclass

from Options import Choice, OptionGroup, PerGameCommonOptions, Range, Toggle, DefaultOnToggle

class ItemizeCharacters(DefaultOnToggle):
    """
    If enabled, character unlocks will be itemized through Archipelago; unlocking them on the Meta Tree will do nothing until they are unlocked through Archipelago. If disabled, these items will be added to starting inventory.
    """
    
    display_name = "Items: Characters"

class ItemizeWeapons(DefaultOnToggle):
    """
    If enabled, weapon unlocks will be itemized through Archipelago; unlocking them on the Meta Tree will do nothing until they are unlocked through Archipelago. If disabled, these items will be added to starting inventory.
    """
    
    display_name = "Items: Weapons"

class ItemizeDifficulty(Toggle):
    """
    If enabled, difficulty unlocks will be itemized through Archipelago; unlocking them by winning runs will do nothing until they are unlocked through Archipelago. If disabled, these items will be added to starting inventory.
    """
    
    display_name = "Items: Difficulty"

class ItemizeEndlessMode(Toggle):
    """
    If enabled, Endless Mode will be itemized through Archipelago; it can only be used once unlocked through Archipelago. If disabled, this item will be added to starting inventory.
    """
    
    display_name = "Items: Endless Mode"

class CheckChests(DefaultOnToggle):
    """
    If enabled, a location will be added for activating each Chest-type Skigill node for the first time; 51 total.
    """
    
    display_name = "Checks: Chests"
    
class CheckPerks(DefaultOnToggle):
    """
    If enabled, a location will be added for activating each Perk-type Skigill node for the first time; 42 total.
    """
    
    display_name = "Checks: Perks"
    
class CheckWeaponEscapes(DefaultOnToggle):
    """
    If enabled, a location will be added for escaping the Skigill (timeout victory, not final boss kill) with each weapon; 60 total.
    """
    
    display_name = "Checks: Weapon Escapes"
    
class CheckHeroEscapes(DefaultOnToggle):
    """
    If enabled, a location will be added for escaping the Skigill (timeout victory, not final boss kill) with each character; 6 total.
    """
    
    display_name = "Checks: Hero Escapes"
    
class CheckTimeSurvived(Toggle):
    """
    If enabled, a location will be added for every 3 minutes survived up to 30; 10 total.
    """
    
    display_name = "Checks: Time Survived"
    
class CheckTimeSurvivedDifficulty(Toggle):
    """
    If enabled, a location will be added for every 3 minutes survived up to 30 on every difficulty; 70 total.
    """
    
    display_name = "Difficult Checks: Time Survived Per Difficulty"
    
class CheckTimeSurvivedCharacter(Toggle):
    """
    If enabled, a location will be added for every 3 minutes survived up to 30 on every character; 60 total.
    """
    
    display_name = "Difficult Checks: Time Survived Per Character"
    
class CheckTimeSurvivedBoth(Toggle):
    """
    If enabled, a location will be added for every 3 minutes survived up to 30 on every character-difficulty combination; 420 total.
    """
    
    display_name = "Difficult Checks: Survivalsanity"
    
class CheckTreesanity(Toggle):
    """
    If enabled, a location will be added for every single Skigill node which isn't a Chest, Perk, or Boss; 574 total.
    """
    
    display_name = "Difficult Checks: Treesanity"
    
class CheckSuperTreesanity(Toggle):
    """
    If enabled, a location will be added for every combination of Skigill Chest/Perk, character, and difficulty; 3906 total.
    """
    
    display_name = "Difficult Checks: Super Treesanity"
    
class CheckUltraTreesanity(Toggle):
    """
    If enabled, a location will be added for every single Skigill node-character-difficulty combination (except Chests/Perks/Bosses); 28014(!!!) total. Not for the faint of heart.
    """
    
    display_name = "Difficult Checks: Ultra Treesanity"
    
class RegionChecksNeedCharacter(Toggle):
    """
    If enabled, checks within a given region will be out of logic until you have the character for that region. If disabled, checking locations in distant regions may be much more difficult; but if enabled, it may be easy to get some out-of-logic items.
    """
    
    display_name = "Region Checks Need Character"
    
class BossRegionLast(DefaultOnToggle):
    """
    If enabled, the boss region will not unlock until all other regions are also unlocked.
    """
    
    display_name = "Boss Region Needs All Other Regions"

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
    itemize_endless_mode: ItemizeEndlessMode
    check_chests: CheckChests
    check_perks: CheckPerks
    check_weapon_escapes: CheckWeaponEscapes
    check_hero_escapes: CheckHeroEscapes
    check_time_survived: CheckTimeSurvived
    check_time_survived_character: CheckTimeSurvivedCharacter
    check_time_survived_difficulty: CheckTimeSurvivedDifficulty
    check_time_survived_both: CheckTimeSurvivedBoth
    check_treesanity: CheckTreesanity
    check_super_treesanity: CheckSuperTreesanity
    check_ultra_treesanity: CheckUltraTreesanity
    region_checks_need_character: RegionChecksNeedCharacter
    boss_region_last: BossRegionLast
    trap_chance: TrapChance
    goal_type: GoalType


option_groups = [
    OptionGroup(
        "Item Distribution",
        [ItemizeCharacters, ItemizeWeapons, ItemizeDifficulty, ItemizeEndlessMode, TrapChance],
    ),
    OptionGroup(
        "Location Distribution",
        [CheckChests, CheckPerks, CheckWeaponEscapes, CheckHeroEscapes, CheckTimeSurvived],
    ),
    OptionGroup(
        "Difficult Locations",
        [CheckTimeSurvivedCharacter, CheckTimeSurvivedDifficulty, CheckTimeSurvivedBoth, CheckTreesanity, CheckSuperTreesanity, CheckUltraTreesanity],
    ),
    OptionGroup(
        "Logic and Goals",
        [RegionChecksNeedCharacter, BossRegionLast, GoalType],
    ),
]

option_presets = {
    "default": {
        "itemize_characters": True,
        "itemize_weapons": True,
        "itemize_difficulty": False,
        "itemize_endless_mode": False,
        "check_chests": True,
        "check_perks": True,
        "check_weapon_escapes": True,
        "check_hero_escapes": True,
        "check_time_survived": False,
        "check_time_survived_character": False,
        "check_time_survived_difficulty": False,
        "check_time_survived_both": False,
        "check_treesanity": False,
        "check_super_treesanity": False,
        "check_ultra_treesanity": False,
        "region_checks_need_character": False,
        "boss_region_last": True,
        "trap_chance": 0,
        "goal_type": GoalType.option_finalboss
    }
}
