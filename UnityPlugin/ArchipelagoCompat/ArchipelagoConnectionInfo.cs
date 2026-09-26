using System;

namespace Archskipelagill.ArchipelagoCompat;

public struct ArchipelagoConnectionInfo:IEquatable<ArchipelagoConnectionInfo> {

    ////// Initializer/Fields/Properties //////
    
    public string uri, slotName, password;

    public ArchipelagoConnectionInfo() {
        uri = "localhost";
        slotName = "Player1";
        password = "";
    }

    public ArchipelagoConnectionInfo(string uri, string slotName, string password) {
        this.uri = uri;
        this.slotName = slotName;
        this.password = password;
    }


    ////// Public API //////

    public readonly bool Equals(ArchipelagoConnectionInfo other) {
        return uri == other.uri &&
               slotName == other.slotName &&
               password == other.password;
    }

    public readonly override int GetHashCode() {
        return HashCode.Combine(uri, slotName, password);
    }
}