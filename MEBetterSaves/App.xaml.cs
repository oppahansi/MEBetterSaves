using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Windows;

namespace MEBetterSaves
{
    public partial class App : Application
    {
        private IConfigurationRoot Config;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            Config = BuildConfig();

            SetConfigDefaults();
            CreateFolders();

            // Create the startup window
            MainWindow wnd = new MainWindow(Config);



            wnd.Show();
        }

        private IConfigurationRoot BuildConfig()
        {
            try
            {
                var builder = new ConfigurationBuilder()
                    .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("config.json", optional: false, reloadOnChange: true);

                return builder.Build();
            }
            catch
            {
                throw new FileNotFoundException();
            }
        }

        private void SetConfigDefaults()
        {
            var defaultsLoaded = int.Parse(Config["defaultsLoaded"]);
            var userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            if (defaultsLoaded == 1)
                return;
            else
            {
                JObject configObj = JObject.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "config.json")));

                if (!Directory.Exists($"{userFolder + "\\Saved Games\\metro exodus\\"}"))
                    configObj["metroSaveGamesFolderExists"] = 0;
                else
                {
                    var path = userFolder + Config["defaultSaveGamesFolder"];
                    var metroSaveFilesDir = Directory.GetDirectories($"{userFolder + Config["defaultSaveGamesFolder"]}");

                    if (metroSaveFilesDir.Length == 0 || metroSaveFilesDir.Length > 1)
                        configObj["metroSaveGamesFolderExists"] = 0;
                    else
                    {
                        configObj["metroSaveGamesFolder"] = metroSaveFilesDir[0];
                        configObj["betterSaveGamesFolder"] = userFolder + Config["defaultBetterSaveGamesFolder"];
                    }
                }

                configObj["defaultsLoaded"] = 1;

                File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "config.json"), configObj.ToString());
            }
        }

        private void CreateFolders()
        {
            JObject configObj = JObject.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "config.json")));
            var chapterNames = configObj["chapterNames"].ToArray();
            var betterSaveGamesDir = configObj["betterSaveGamesFolder"];

            foreach (var chapter in chapterNames)
            {
                if (!Directory.Exists(betterSaveGamesDir + chapter.ToString()))
                    Directory.CreateDirectory(betterSaveGamesDir + chapter.ToString());
            }
        }
    }
}
