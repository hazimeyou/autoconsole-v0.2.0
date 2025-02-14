using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

public static class PluginLoader
{
    public static List<IPlugin> LoadPluginsFromDll(string directory)
    {
        List<IPlugin> loadedPlugins = new List<IPlugin>();

        if (!Directory.Exists(directory))
        {
            Console.WriteLine($"[PLUGIN] プラグインディレクトリが存在しません: {directory}");
            return loadedPlugins;
        }

        foreach (var dll in Directory.GetFiles(directory, "*.dll"))
        {
            try
            {
                Assembly assembly = Assembly.LoadFrom(dll);
                foreach (var type in assembly.GetTypes().Where(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsInterface))
                {
                    if (Activator.CreateInstance(type) is IPlugin plugin)
                    {
                        loadedPlugins.Add(plugin);
                        Console.WriteLine($"[PLUGIN] 読み込み成功: {plugin.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PLUGIN] 読み込みエラー ({dll}): {ex.Message}");
            }
        }

        return loadedPlugins;
    }
}
