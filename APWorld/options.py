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

class RandomStartChar(DefaultOnToggle):
    """
    If enabled, the starting character/region will be randomized. If disabled, the starting character/region will always be Mage.
    """
    
    display_name = "Random Starting Character"

class RandomStartWeapon(DefaultOnToggle):
    """
    If enabled, the starting weapon set (and consequently all weapons in the meta tree) will be randomized. If disabled, the starting weapon set and meta tree weapons will remain unchanged from base game.
    """
    
    display_name = "Random Starting Weapons"

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
    
class CheckHeroSurvival(Toggle):
    """
    If enabled, a location will be added for surviving every 3 minutes up to 30 with each character; 60 total.
    """
    
    display_name = "Checks: Hero Survival"
    
class CheckDifficultySurvival(Toggle):
    """
    If enabled, a location will be added for surviving every 3 minutes up to 30 on each difficulty; 70 total.
    """
    
    display_name = "Checks: Difficulty Survival"
    
class CheckSkigillsanity(Toggle):
    """
    If enabled, a location will be added for activating each Stat-type Skigill node for the first time; 574 total.
    """
    
    display_name = "Checks: Skigillsanity"
    
class CheckSuperSkigillsanity(Toggle):
    """
    If enabled, a location will be added for activating each Chest- and Perk-type Skigill node on each character; 558 total.
    """
    
    display_name = "Checks: Super Skigillsanity"
    
class CheckUltraSkigillsanity(Toggle):
    """
    If enabled, a location will be added for activating each Stat-type Skigill node on each character; 3444 total! Not for the faint of heart.
    """
    
    display_name = "Checks: Ultra Skigillsanity"
    
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
    
class TrapWeightDamage(Range):
    """
    Weight for the given trap to be used when any trap is placed.
    """

    display_name = "Trap Weight: Damage"

    range_start = 0
    range_end = 100
    default = 50
    
class TrapWeightPullEnemies(Range):
    """
    Weight for the given trap to be used when any trap is placed.
    """

    display_name = "Trap Weight: Pull Enemies"

    range_start = 0
    range_end = 100
    default = 50
    
class TrapWeightWeaponJam(Range):
    """
    Weight for the given trap to be used when any trap is placed.
    """

    display_name = "Trap Weight: Weapon Jam"

    range_start = 0
    range_end = 100
    default = 50

    
class TrapWeightDrainSki(Range):
    """
    Weight for the given trap to be used when any trap is placed.
    """

    display_name = "Trap Weight: Drain Ski"

    range_start = 0
    range_end = 100
    default = 50
    
class TrapWeightScrambleStats(Range):
    """
    Weight for the given trap to be used when any trap is placed.
    """

    display_name = "Trap Weight: Scramble Stats"

    range_start = 0
    range_end = 100
    default = 50
    
class TrapWeightFlashMob(Range):
    """
    Weight for the given trap to be used when any trap is placed.
    """

    display_name = "Trap Weight: Flash Mob"

    range_start = 0
    range_end = 100
    default = 50
    
class TrapWeightStrongerEnemies(Range):
    """
    Weight for the given trap to be used when any trap is placed.
    """

    display_name = "Trap Weight: Stronger Enemies"

    range_start = 0
    range_end = 100
    default = 50
    
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
    random_start_char: RandomStartChar
    random_start_weapon: RandomStartWeapon
    check_chests: CheckChests
    check_perks: CheckPerks
    check_weapon_escapes: CheckWeaponEscapes
    check_hero_escapes: CheckHeroEscapes
    check_hero_survival: CheckHeroSurvival
    check_difficulty_survival: CheckDifficultySurvival
    check_skigillsanity: CheckSkigillsanity
    check_super_skigillsanity: CheckSuperSkigillsanity
    check_ultra_skigillsanity: CheckUltraSkigillsanity
    region_checks_need_character: RegionChecksNeedCharacter
    boss_region_last: BossRegionLast
    trap_chance: TrapChance
    trap_weight_damage: TrapWeightDamage
    trap_weight_pull_enemies: TrapWeightPullEnemies
    trap_weight_weapon_jam: TrapWeightWeaponJam
    trap_weight_drain_ski: TrapWeightDrainSki
    trap_weight_scramble_stats: TrapWeightScrambleStats
    trap_weight_flash_mob: TrapWeightFlashMob
    trap_weight_stronger_enemies: TrapWeightStrongerEnemies
    goal_type: GoalType


option_groups = [
    OptionGroup(
        "Item Distribution",
        [ItemizeCharacters, ItemizeWeapons, ItemizeDifficulty, ItemizeEndlessMode, TrapChance],
    ),
    OptionGroup(
        "Trap Weights",
        [TrapWeightDamage, TrapWeightPullEnemies, TrapWeightWeaponJam, TrapWeightDrainSki, TrapWeightScrambleStats, TrapWeightFlashMob, TrapWeightStrongerEnemies],
    ),
    OptionGroup(
        "Location Distribution",
        [CheckChests, CheckPerks, CheckWeaponEscapes, CheckHeroEscapes, CheckHeroSurvival, CheckDifficultySurvival, CheckSkigillsanity, CheckSuperSkigillsanity, CheckUltraSkigillsanity],
    ),
    OptionGroup(
        "Logic and Goals",
        [GoalType, RandomStartChar, RandomStartWeapon, RegionChecksNeedCharacter, BossRegionLast],
    ),
]

option_presets = {
    "default": {
        "itemize_characters": True,
        "itemize_weapons": True,
        "itemize_difficulty": False,
        "itemize_endless_mode": False,
        "random_start_char": True,
        "random_start_weapon": True,
        "check_chests": True,
        "check_perks": True,
        "check_weapon_escapes": True,
        "check_hero_escapes": True,
        "check_hero_survival": False,
        "check_difficulty_survival": False,
        "check_skigillsanity": False,
        "check_super_skigillsanity": False,
        "check_ultra_skigillsanity": False,
        "region_checks_need_character": False,
        "boss_region_last": True,
        "trap_chance": 0,
        "trap_weight_damage": 50,
        "trap_weight_pull_enemies": 50,
        "trap_weight_weapon_jam": 50,
        "trap_weight_drain_ski": 50,
        "trap_weight_scramble_stats": 50,
        "trap_weight_flash_mob": 50,
        "trap_weight_stronger_enemies": 50,
        "goal_type": GoalType.option_finalboss
    }
}
