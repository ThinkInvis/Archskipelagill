from __future__ import annotations

from typing import TYPE_CHECKING

from rule_builder.options import OptionFilter
from rule_builder.rules import Has, HasAll, HasAny, Rule, False_

from .skilltree import SkillNodeType, SkillNodeRegion, SkillNode
from .skilltree_data import SKILL_TREE, WEAPON_NAMES

if TYPE_CHECKING:
    from .world import SkigillWorld

def set_all_rules(world: SkigillWorld) -> None:
    set_all_entrance_rules(world)
    set_all_location_rules(world)
    set_completion_condition(world)

def set_all_entrance_rules(world: SkigillWorld) -> None:
    has_mage = Has("Character: Mage")
    has_strongman = Has("Character: Strongman")
    has_fox = Has("Character: Fox")
    has_prototype = Has("Character: Prototype")
    has_dragon = Has("Character: Dragon")
    has_dwarves = Has("Character: Dwarves")
    
    has_r_mage = Has("Skigill Region: Mage")
    has_r_strongman = Has("Skigill Region: Strongman")
    has_r_fox = Has("Skigill Region: Fox")
    has_r_prototype = Has("Skigill Region: Prototype")
    has_r_dragon = Has("Skigill Region: Dragon")
    has_r_dwarves = Has("Skigill Region: Dwarves")
    has_r_boss = Has("Skigill Region: Bosses")
    
    if world.options.region_checks_need_character:
        has_r_mage &= has_mage
        has_r_strongman &= has_strongman
        has_r_fox &= has_fox
        has_r_prototype &= has_prototype
        has_r_dragon &= has_dragon
        has_r_dwarves &= has_dwarves
    
    has_r_all = has_r_mage & has_r_strongman & has_r_fox & has_r_prototype & has_r_dragon & has_r_dwarves
    
    world.set_rule(world.get_entrance("Mage to Dragon"), has_r_mage & has_r_dragon)
    world.set_rule(world.get_entrance("Mage to Prototype"), has_r_mage & has_r_prototype)
    world.set_rule(world.get_entrance("Mage to Bosses"), has_r_all & has_r_boss)
    
    world.set_rule(world.get_entrance("Prototype to Strongman"), has_r_prototype & has_r_strongman)
    world.set_rule(world.get_entrance("Prototype to Bosses"), has_r_all & has_r_boss)

    world.set_rule(world.get_entrance("Dragon to Fox"), has_r_dragon & has_r_fox)
    world.set_rule(world.get_entrance("Dragon to Bosses"), has_r_all & has_r_boss)
    
    world.set_rule(world.get_entrance("Strongman to Dwarves"), has_r_strongman & has_r_dwarves)
    world.set_rule(world.get_entrance("Strongman to Bosses"), has_r_all & has_r_boss)
    
    world.set_rule(world.get_entrance("Fox to Dwarves"), has_r_fox & has_r_dwarves)
    world.set_rule(world.get_entrance("Fox to Bosses"), has_r_all & has_r_boss)
    
    world.set_rule(world.get_entrance("Dwarves to Bosses"), has_r_all & has_r_boss)

def set_all_location_rules(world: SkigillWorld) -> None:
    if world.options.check_weapon_escapes:
        for weapon in WEAPON_NAMES:
            world.set_rule(world.get_location(f"Escaped with Weapon {weapon}"), Has(f"Weapon: {weapon}"))
        
    has_bosses = Has("Skigill Region: Bosses")
    has_finalboss = has_bosses & Has("Final Boss Key")
    has_diff7 = Has("Progressive Difficulty", 6)
    has_bosses_diff7 = has_bosses & has_diff7
    has_finalboss_diff7 = has_finalboss & has_diff7
    
    world.set_rule(world.get_location("Defeated Gari"), has_bosses)
    world.set_rule(world.get_location("Defeated Bouboul"), has_bosses)
    world.set_rule(world.get_location("Defeated Jello"), has_bosses)
    world.set_rule(world.get_location("Defeated Roger"), has_bosses)
    world.set_rule(world.get_location("Defeated Pilpou"), has_bosses)
    world.set_rule(world.get_location("Defeated Rosa"), has_bosses)
    if world.options.goal_type == 1:
        world.set_rule(world.get_location("Defeated Final Boss"), has_finalboss)
    if world.options.goal_type == 2:
        world.set_rule(world.get_location("I'm The Boss Now"), has_bosses)
    if world.options.goal_type > 2:
        world.set_rule(world.get_location("Defeated Gari on Difficulty 7"), has_bosses_diff7)
        world.set_rule(world.get_location("Defeated Bouboul on Difficulty 7"), has_bosses_diff7)
        world.set_rule(world.get_location("Defeated Jello on Difficulty 7"), has_bosses_diff7)
        world.set_rule(world.get_location("Defeated Roger on Difficulty 7"), has_bosses_diff7)
        world.set_rule(world.get_location("Defeated Pilpou on Difficulty 7"), has_bosses_diff7)
        world.set_rule(world.get_location("Defeated Rosa on Difficulty 7"), has_bosses_diff7)
    if world.options.goal_type == 4:
        world.set_rule(world.get_location("Defeated Final Boss on Difficulty 7"), has_finalboss_diff7)
    if world.options.goal_type == 5:
        world.set_rule(world.get_location("I'm The Boss Now on Difficulty 7"), has_bosses_diff7)
    world.set_rule(world.get_location("Escaped with Mage"), Has("Character: Mage"))
    world.set_rule(world.get_location("Escaped with Strongman"), Has("Character: Strongman"))
    world.set_rule(world.get_location("Escaped with Fox"), Has("Character: Fox"))
    world.set_rule(world.get_location("Escaped with Prototype"), Has("Character: Prototype"))
    world.set_rule(world.get_location("Escaped with Dwarves"), Has("Character: Dwarves"))
    world.set_rule(world.get_location("Escaped with Dragon"), Has("Character: Dragon"))

def set_completion_condition(world: SkigillWorld) -> None:
    world.set_completion_rule(Has("Victory"))