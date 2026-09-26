using System.Linq;
using Archskipelagill.GameDataAccess;

namespace Archskipelagill.ArchipelagoCompat;

internal static class ArchipelagoDataUtils {

    ////// Public Static API //////
    
    public enum LocationState { Invalid, Unchecked, Checked }

    public static bool HasItem(string itemName) {
        return ArchipelagoSaver.GetItemCount(itemName) > 0;
    }

    public static bool HasRegionItem(GameData.Character character) {
        return HasItem($"Skigill Region: {character.name}");
    }

    public static bool HasItem(GameData.Character character) {
        return HasItem($"Character: {character.name}");
    }

    public static bool HasItem(GameData.Weapon weapon) {
        return HasItem($"Weapon: {weapon.prefabName}");
    }

    public static bool HasItem(CharaStats stats) {
        return HasItem(GameData.allCharacters.FirstOrDefault(n => n.id == stats.chara));
    }

    public static bool HasWeaponBySaveName(string name) {
        return HasItem(GameData.allWeapons.FirstOrDefault(n => n.saveName == name));
    }

    public static bool HasWeaponByPrefabName(string name) {
        return HasItem(GameData.allWeapons.FirstOrDefault(n => n.prefabName == name));
    }

    public static bool HasCharacterByInternalName(string name) {
        return HasItem(GameData.allCharacters.FirstOrDefault(n => n.internalName == name));
    }

    public static bool HasCharacterByEnName(string name) {
        return HasItem(GameData.allCharacters.FirstOrDefault(n => n.name == name));
    }

    public static bool HasCharacterById(int id) {
        return HasItem(GameData.allCharacters.FirstOrDefault(n => n.id == id));
    }

    public static bool HasRegionByCharacterId(int id) {
        return HasRegionItem(GameData.allCharacters.FirstOrDefault(n => n.id == id));
    }

    public static LocationState HasLocation(string locationName) {
        LocationState retv;
        if(!ArchipelagoSaver.Instance.AllValidChecks.Contains(locationName)) retv = LocationState.Invalid;
        else if(ArchipelagoSaver.Instance.SentChecks.Contains(locationName) || ArchipelagoSaver.Instance.UnsentChecks.Contains(locationName)) retv = LocationState.Checked;
        else retv = LocationState.Unchecked;
        return retv;
    }
}
