using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace MEBetterSaves
{
    public partial class MainWindow
    {
        private IConfigurationRoot Config;

        public MainWindow(IConfigurationRoot config)
        {
            Config = config;

            InitializeComponent();

            var windowPosX = int.Parse(Config["windowPosX"]);
            var windowPosY = int.Parse(Config["windowPosY"]);

            mainWindow.WindowStartupLocation = WindowStartupLocation.Manual;
            mainWindow.Left = windowPosX;
            mainWindow.Top = windowPosY;

            tabControl.SelectedIndex = 0;

            metroSaveGamesPath.Text = Config["metroSaveGamesFolder"];
            metroBetterSaveGamesPath.Text = Config["betterSaveGamesFolder"];

            Closing += OnWindowClosing;

            PopulateDatesListBox(datesListBox0, Config["betterSaveGamesFolder"] + $"{Config["chapterNames:0"]}");

            DirectoryInfo dinfo = new DirectoryInfo(Config["betterSaveGamesFolder"] + $"{Config["chapterNames:0"]}");
            DirectoryInfo[] dirs = dinfo.GetDirectories();
            PopulateListBox((ListBox)FindName("autoSavesListBox" + 0), dirs[0].FullName);
        }

        private void OnWindowClosing(object sender, CancelEventArgs e)
        {
            var windowPosX = Application.Current.MainWindow.Left;
            var windowPosY = Application.Current.MainWindow.Top;

            if (windowPosX < 0 || windowPosY < 0 || windowPosX > SystemParameters.PrimaryScreenWidth || windowPosY > SystemParameters.PrimaryScreenHeight)
                return;

            if (int.Parse(Config["windowPosX"]) != windowPosX || int.Parse(Config["windowPosY"]) != windowPosY)
            {
                JObject configObj = JObject.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "config.json")));
                configObj["windowPosX"] = windowPosX;
                configObj["windowPosY"] = windowPosY;
                File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "config.json"), configObj.ToString());
            }
        }

        private void PopulateDatesListBox(ListBox lsb, string Folder, int tabIndex = 0)
        {
            DirectoryInfo dinfo = new DirectoryInfo(Folder);
            DirectoryInfo[] dirs = dinfo.GetDirectories();
            foreach (var dir in dirs)
            {
                lsb.Items.Add(dir.Name);
            }
        }

        private void PopulateListBox(ListBox lsb, string Folder)
        {
            DirectoryInfo dinfo = new DirectoryInfo(Folder);
            FileInfo[] Files = dinfo.GetFiles();
            foreach (FileInfo file in Files)
            {
                lsb.Items.Add(file.Name);
            }
        }
    }
}
