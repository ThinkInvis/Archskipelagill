from collections.abc import Mapping
from typing import Any

import uuid

from worlds.AutoWorld import World

from . import items, locations, regions, rules, web_world
from . import options as skigill_options  # rename due to a name conflict with World.options

class SkigillWorld(World):
    game = "Skigill"
    web = web_world.SkigillWebWorld()

    options_dataclass = skigill_options.SkigillOptions
    options: skigill_options.SkigillOptions

    location_name_to_id = locations.LOCATION_NAME_TO_ID
    item_name_to_id = items.ITEM_NAME_TO_ID

    origin_region_name = "Menu"

    def create_regions(self) -> None:
        regions.create_and_connect_regions(self)
        locations.create_all_locations(self)

    def set_rules(self) -> None:
        rules.set_all_rules(self)

    def create_items(self) -> None:
        items.create_all_items(self)

    def create_item(self, name: str) -> items.SkigillItem:
        return items.create_item_with_correct_classification(self, name)

    def get_filler_item_name(self) -> str:
        return items.get_random_filler_item_name(self)

    def fill_slot_data(self) -> Mapping[str, Any]:
        retv = self.options.as_dict("goal_type", "boss_region_last")
        retv["start_char"] = self.starting_character_name
        retv["world_uuid"] = str(uuid.uuid4())
        retv["trap_weights"] = f"{self.options.trap_weight_damage}|{self.options.trap_weight_pull_enemies}|{self.options.trap_weight_weapon_jam}|{self.options.trap_weight_drain_ski}|{self.options.trap_weight_scramble_stats}|{self.options.trap_weight_flash_mob}|{self.options.trap_weight_stronger_enemies}"
        return retv
