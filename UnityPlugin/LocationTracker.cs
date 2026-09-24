using System;
using System.Collections.Generic;
using System.Text;

namespace Archskipelagill;

internal static class LocationTracker {
    public enum LocationState { Invalid, Unchecked, Checked }

    public static bool HasItem(string itemName) {
        return ArchiSaver.GetItemCount(itemName) > 0;
    }

    public static bool HasItem(Itemizers.AbilityUnlockItemizer.CharacterInIngameOrder character) {
        return ArchiSaver.GetItemCount($"Character: {Enum.GetName(typeof(Itemizers.AbilityUnlockItemizer.CharacterInIngameOrder), character)}") > 0;
    }

    public static LocationState HasLocation(string locationName) {
        if(!ArchiSaver.instance.allValidChecks.Contains(locationName)) return LocationState.Invalid;
        if(ArchiSaver.instance.sentChecks.Contains(locationName) || ArchiSaver.instance.unsentChecks.Contains(locationName)) return LocationState.Checked;
        return LocationState.Unchecked;
    }
}
