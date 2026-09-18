from __future__ import annotations

from typing import TYPE_CHECKING

from BaseClasses import Entrance, Region

if TYPE_CHECKING:
    from .world import SkigillWorld

def create_and_connect_regions(world: SkigillWorld) -> None:
    create_all_regions(world)
    connect_regions(world)

def create_all_regions(world: SkigillWorld) -> None:
    mage = Region("Mage", world.player, world.multiworld)
    strongman = Region("Strongman", world.player, world.multiworld)
    fox = Region("Fox", world.player, world.multiworld)
    prototype = Region("Prototype", world.player, world.multiworld)
    dwarves = Region("Dwarves", world.player, world.multiworld)
    dragon = Region("Dragon", world.player, world.multiworld)
    bosses = Region("Bosses", world.player, world.multiworld)
    
    regions = [mage, strongman, fox, prototype, dwarves, dragon, bosses]
    
    world.multiworld.regions += regions

def connect_regions(world: SkigillWorld) -> None:
    mage = world.get_region("Mage")
    strongman = world.get_region("Strongman")
    fox = world.get_region("Fox")
    prototype = world.get_region("Prototype")
    dwarves = world.get_region("Dwarves")
    dragon = world.get_region("Dragon")
    bosses = world.get_region("Bosses")
    
    mage.connect(prototype, "Mage to Prototype")
    mage.connect(dragon, "Mage to Dragon")
    mage.connect(bosses, "Mage to Bosses")
    
    prototype.connect(strongman, "Prototype to Strongman")
    prototype.connect(bosses, "Prototype to Bosses")
    
    dragon.connect(fox, "Dragon to Fox")
    dragon.connect(bosses, "Dragon to Bosses")
    
    strongman.connect(dwarves, "Strongman to Dwarves")
    strongman.connect(bosses, "Strongman to Bosses")
    
    fox.connect(dwarves, "Fox to Dwarves")
    fox.connect(bosses, "Fox to Bosses")
    
    dwarves.connect(bosses, "Dwarves to Bosses")