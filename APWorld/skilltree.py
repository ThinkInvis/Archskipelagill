from enum import Enum

class SkillNodeType(Enum):
    UNKNOWN = 0
    STAT = 1
    CHEST = 2
    PERK = 3
    BOSS = 4
    BOSS_FINAL = 5
    
class SkillNodeSpawnId(Enum):
    NONE = 0
    MAGE = 1
    STRONGMAN = 2
    FOX = 3
    DWARVES = 4
    PROTOTYPE = 5
    DRAGON = 6
    
class SkillNodeRegion(Enum):
    MAGE = 1
    STRONGMAN = 2
    FOX = 3
    DWARVES = 4
    PROTOTYPE = 5
    DRAGON = 6
    BOSS = 7

class SkillNode:
    def __init__(self, type, neighbors, spawn_id, region, original_index, chest_index, perk_index):
        self.type = type
        self.neighbors = neighbors
        self.spawn_id = spawn_id 
        self.region = region
        self.original_index = original_index
        self.chest_index = chest_index
        self.perk_index = perk_index