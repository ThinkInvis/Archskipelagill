using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace Archskipelagill.MiscModules;

public class AchievementsDisabler : Module<AchievementsDisabler> {

    ////// Initializer/Fields/Properties //////

    public bool DisableAchievements => _cfgDisableAchievements.Value;

    private readonly ConfigEntry<bool> _cfgDisableAchievements;

    private AchievementsDisabler() {
        _cfgDisableAchievements = Plugin.Instance.MainConfig.Bind<bool>(new ConfigDefinition("Misc.", "Disable Achievements"), true, new ConfigDescription("If true, Steam achievements will not be unlocked while this mod is active."));

        On.Achievements.unlockAchievement += Achievements_unlockAchievement;
    }


    ////// MonoMod Hooks //////
    #region MonoMod Hooks
    private void Achievements_unlockAchievement(On.Achievements.orig_unlockAchievement orig, Achievements self, string name) {
        if(!DisableAchievements)
            orig(self, name);
    }
    #endregion
}
