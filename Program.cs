using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;

namespace AcordStandaloneUpdater
{
    internal class Program
    {
        static string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        static string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        static void Main(string[] args)
        {

            string[] releases = new string[] { "Discord", "DiscordPTB", "DiscordCanary", "DiscordDevelopment" };

            string acordDataFolder = Path.Combine(appData, "Acord/data");
            Directory.CreateDirectory(acordDataFolder);

            string acordAsarPath = Path.Combine(acordDataFolder, "acord.asar");

            DownloadFile("https://github.com/acord-standalone/standalone/raw/main/dist/acord.asar", acordAsarPath);
            
            string discordRelease = "Discord";
            if (args.Length > 0) {
                switch (args[0])
                {
                    case "stable": discordRelease = "Discord"; break;
                    case "Discord": discordRelease = "Discord"; break;
                    case "ptb": discordRelease = "DiscordPTB"; break;
                    case "DiscordPTB": discordRelease = "DiscordPTB"; break;
                    case "canary": discordRelease = "DiscordCanary"; break;
                    case "DiscordCanary": discordRelease = "DiscordCanary"; break;
                    case "development": discordRelease = "DiscordDevelopment"; break;
                    case "DiscordDevelopment": discordRelease = "DiscordDevelopment"; break;
                    default: discordRelease = "Discord"; break;
                }
            }

            Console.WriteLine(discordRelease);

            string discordExePath = null;

            foreach (string release in releases)
            {
                Process[] processes = Process.GetProcessesByName(release.ToLower()).ToArray();
                for (int i = 0; i < processes.Length; i++)
                {
                    Process process = processes[i];

                    try
                    {
                        process.Kill();
                        if (discordExePath == null && release == discordRelease)
                        {
                            discordExePath = process.MainModule.FileName;
                        }
                    }
                    catch
                    {

                    }
                }
            }

            string settingsJsonPath = Path.Combine(appData, discordRelease.ToLower(), "settings.json");

            if (!File.Exists(settingsJsonPath)) return;

            File.WriteAllText(settingsJsonPath, @"{""openasar"":{""setup"":true},""DANGEROUS_ENABLE_DEVTOOLS_ONLY_ENABLE_IF_YOU_KNOW_WHAT_YOURE_DOING"":true}");

            string[] appPaths = Directory.GetDirectories(Path.Combine(localAppData, discordRelease)).Where(i => Path.GetFileName(i).StartsWith("app-")).ToArray();

            for (int i = 0; i < appPaths.Length; i++)
            {
                string discordAppPath = appPaths[i];

                string modulesPath = Path.Combine(discordAppPath, "modules");

                if (Directory.Exists(modulesPath))
                {
                    string[] desktopCoreModulePaths = Directory.GetDirectories(modulesPath).Where(k => Path.GetFileName(k).StartsWith("discord_desktop_core-")).ToArray();

                    for (int j = 0; j < desktopCoreModulePaths.Length; j++)
                    {
                        string modulePath = Path.Combine(desktopCoreModulePaths[j], "discord_desktop_core");

                        File.WriteAllText(Path.Combine(modulePath, "index.js"), $@"require(""{acordAsarPath.Replace("\\", "/")}"");module.exports = require(""./core.asar"");");
                        File.WriteAllText(Path.Combine(modulePath, "package.json"), "{\"name\":\"acord\",\"main\":\"index.js\",\"version\":\"0.0.0\"}");
                    }

                    Directory.CreateDirectory(Path.Combine(discordAppPath, "resources/app"));
                    DownloadFile("https://github.com/GooseMod/OpenAsar/releases/download/nightly/app.asar", Path.Combine(discordAppPath, "resources/app.asar"));
                }

            }

            if (discordExePath != null)
            {
                Process.Start(discordExePath);
            }

            void DownloadFile(string uri, string path)
        {
            using (var client = new WebClient())
            {
                client.DownloadFile(uri, path);
            }
        }
            
        }

    }
}
