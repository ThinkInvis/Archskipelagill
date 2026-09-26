using System;
using System.Linq;
using System.Reflection;

namespace Archskipelagill;

public abstract class Module<T>:Module where T:Module<T> {
    public static T Instance {get; private set;}
    protected Module() {
        if(Instance != null) throw new InvalidOperationException("Singleton class \"" + typeof(T).Name + "\" inheriting Module was instantiated twice");
        Instance = this as T;
    }
}
public abstract class Module {
    public static void InitAllModules() {
        foreach(Type type in Assembly.GetCallingAssembly().GetTypes().Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(Module)))) {
            var newModule = Activator.CreateInstance(type, nonPublic: true);
        }
    }
}