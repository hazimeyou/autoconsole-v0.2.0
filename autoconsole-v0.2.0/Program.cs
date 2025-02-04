using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        List<IPlugin> plugins = LoadPlugins("Plugins");

        var psi = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (var process = new Process { StartInfo = psi })
        {
            process.Start();

            // 非同期で出力を読み取る
            Task outputTask = ReadOutputAsync(process.StandardOutput, process.StandardInput, plugins);
            Task errorTask = ReadOutputAsync(process.StandardError, process.StandardInput, plugins);

            using (StreamWriter writer = process.StandardInput)
            {
                if (writer.BaseStream.CanWrite)
                {
                    while (true)
                    {
                        Console.Write("> ");
                        string? input = Console.ReadLine();
                        if (input == null) continue;

                        // マクロ処理（!コマンド）
                        if (input.StartsWith("!"))
                        {
                            string command = input.Substring(1);
                            Console.WriteLine($"[MACRO] {command}");
                            await writer.WriteLineAsync(command);
                            continue;
                        }

                        // プラグイン処理（/コマンド）
                        if (input.StartsWith("/"))
                        {
                            ProcessPluginInput(writer, input, plugins);
                            continue;
                        }

                        await writer.WriteLineAsync(input);
                    }
                }
            }

            await Task.WhenAll(outputTask, errorTask);
            process.WaitForExit();
        }
    }

    static List<IPlugin> LoadPlugins(string path)
    {
        List<IPlugin> plugins = new();

        if (!Directory.Exists(path)) Directory.CreateDirectory(path);

        foreach (string file in Directory.GetFiles(path, "*.dll"))
        {
            try
            {
                Assembly asm = Assembly.LoadFrom(file);
                foreach (Type type in asm.GetTypes().Where(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsInterface))
                {
                    if (Activator.CreateInstance(type) is IPlugin plugin)
                    {
                        plugins.Add(plugin);
                        Console.WriteLine($"[PLUGIN LOADED] {plugin.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to load {file}: {ex.Message}");
            }
        }

        return plugins;
    }

    static async Task ReadOutputAsync(StreamReader reader, StreamWriter writer, List<IPlugin> plugins)
    {
        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            Console.WriteLine(line);
            ProcessPluginOutput(writer, line, plugins);
        }
    }

    static void ProcessPluginInput(StreamWriter writer, string input, List<IPlugin> plugins)
    {
        foreach (var plugin in plugins)
        {
            if (input.StartsWith(plugin.Trigger) && plugin.Type == "send")
            {
                plugin.Execute(input, cmd =>
                {
                    Console.WriteLine($"[PLUGIN] {plugin.Name} executed → Sending: {cmd}");
                    writer.WriteLine(cmd);
                });
                return;
            }
        }
    }

    static void ProcessPluginOutput(StreamWriter writer, string output, List<IPlugin> plugins)
    {
        foreach (var plugin in plugins)
        {
            if (output.Contains(plugin.Trigger) && plugin.Type == "send")
            {
                plugin.Execute(output, cmd =>
                {
                    Console.WriteLine($"[PLUGIN] {plugin.Name} triggered → Sending: {cmd}");
                    writer.WriteLine(cmd);
                });
            }
        }
    }
}
public interface IPlugin
{
    string Name { get; }
    string Trigger { get; }
    string Type { get; }
    void Execute(string input, Action<string> sendCommand);
}
