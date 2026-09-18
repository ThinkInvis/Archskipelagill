from BaseClasses import Tutorial
from worlds.AutoWorld import WebWorld

from .options import option_groups, option_presets

class SkigillWebWorld(WebWorld):
    game = "Skigill"
    theme = "ocean"

    setup_en = Tutorial(
        "Multiworld Setup Guide",
        "A guide to setting up Skigill for MultiWorld.",
        "English",
        "setup_en.md",
        "setup/en",
        ["ThinkInvisible"],
    )

    tutorials = [setup_en]

    option_groups = option_groups
    options_presets = option_presets
