from enum import Enum

class SkillNodeType(Enum):
    UNKNOWN = 0
    STAT = 1
    CHEST = 2
    PERK = 3
    BOSS = 4
    BOSS_FINAL = 5
    
class SkillNodeRegion(Enum):
    MAGE = 1
    STRONGMAN = 2
    FOX = 3
    DWARVES = 4
    PROTOTYPE = 5
    DRAGON = 6
    BOSSES = 7

class SkillNode:
    def __init__(self, type, neighbors, region, original_index, index_in_type):
        self.type = type
        self.neighbors = neighbors
        self.region = region
        self.original_index = original_index
        self.index_in_type = index_in_type