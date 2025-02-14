using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;

class Program
{
    static List<IPlugin> plugins = new List<IPlugin>();
    static string macroFolder = "macros"; // 🔹 マクロフォルダー

    static void Main()
    {
        LoadPlugins(); // 🔹 プラグインをロード

        var psi = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        Console.WriteLine(); // 🔹 起動時のみ改行

        using (var process = new Process { StartInfo = psi })
        {
            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    Console.WriteLine(e.Data); // 🔹 コンソールに出力

                    // 🔹 出力に反応するプラグインを実行
                    ExecuteOutputPlugins(e.Data, cmd => process.StandardInput.WriteLine(cmd));
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            using (StreamWriter writer = process.StandardInput)
            {
                while (true)
                {
                    string input = Console.ReadLine();
                    if (input == null) continue;

                    if (input.StartsWith("!"))
                    {
                        // 🔹 `!` で始まる場合はマクロを実行
                        string macroName = input.Substring(1) + ".txt"; // `!test` → `test.txt`
                        ExecuteMacro(macroName, writer);
                    }
                    else if (input.StartsWith("/"))
                    {
                        // 🔹 `/` で始まる場合はプラグインに処理を委託
                        ExecuteInputPlugins(input.Substring(1), cmd => writer.WriteLine(cmd));
                    }
                    else
                    {
                        // 🔹 何もついていない場合はそのまま `cmd.exe` に送信
                        writer.WriteLine(input);
                    }
                }
            }
        }
    }

    static void LoadPlugins()
    {
        plugins.AddRange(PluginLoader.LoadPluginsFromDll("plugins")); // 🔹 DLL からプラグインをロード
    }

    static void ExecuteInputPlugins(string input, Action<string> sendCommand)
    {
        foreach (var plugin in plugins)
        {
            if (plugin.Type == "input")
            {
                plugin.Execute(input, sendCommand);
            }
        }
    }

    static void ExecuteOutputPlugins(string output, Action<string> sendCommand)
    {
        foreach (var plugin in plugins)
        {
            if (plugin.Type == "output" && output.Contains(plugin.Trigger))
            {
                plugin.Execute(output, sendCommand);
            }
        }
    }

    static void ExecuteMacro(string macroFileName, StreamWriter writer)
    {
        string macroPath = Path.Combine(macroFolder, macroFileName);

        if (File.Exists(macroPath))
        {
            Console.WriteLine($"[MACRO] Executing {macroFileName}");

            string[] commands = File.ReadAllLines(macroPath);
            foreach (var command in commands)
            {
                writer.WriteLine(command);
            }
        }
        else
        {
            Console.WriteLine($"[ERROR] Macro file not found: {macroFileName}");
        }
    }
}
