using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MEBetterSaves
{
    public partial class MainWindow : Window
    {
        

        private IConfigurationRoot Config;
        private string[] ChapterNames;
        private App MainApp;
        private bool FirstActivated;

        private int DateListSelectedIndex;
        private int AutoSavesSelectedIndex;
        private int QuickSaveSelectedIndex;
        private int RadioButtonSelectedIndex;
        private ImageSource AutoSaveImageSource;
        private ImageSource QuickSaveImageSource;
        private Thickness QuickSaveImageMargin;

        public MainWindow(App app, IConfigurationRoot config)
        {
            MainApp = app;
            Config = config;
            ChapterNames = Config.GetSection("chapterNames").GetChildren().Select(x => x.Value).ToArray();

            DateListSelectedIndex = 0;
            AutoSavesSelectedIndex = 0;
            QuickSaveSelectedIndex = 0;
            RadioButtonSelectedIndex = 0;
            AutoSaveImageSource = null;
            QuickSaveImageSource = null;

            InitializeComponent();
            RestoreScreenPosition();
            SetPathsAndInitialPopulate();

            QuickSaveImageMargin = quickSaveImage.Margin;

            Closing += OnWindowClosing;
            Activated += OnActivated;
            Deactivated += OnDeactivated;

            chapter0.IsChecked = true;

            datesListBox.SelectionChanged += OnDateSelectionChangedEvent;
            autoSavesListBox.MouseDoubleClick += OnAutoSavesMouseDoubleClickHandler;
            quickSavesListBox.MouseDoubleClick += OnQuickSavesMouseDoubleClickHandler;
        }

        private void OnActivated(object sender, EventArgs e)
        {
            if (FirstActivated)
            {
                FirstActivated = false;
                RefreshListBoxes();
            }
        }

        private void OnDeactivated(object sender, EventArgs e)
        {
            FirstActivated = true;
        }

        public void RefreshListBoxes()
        {
            datesListBox.Items.Clear();
            quickSavesListBox.Items.Clear();
            autoSavesListBox.Items.Clear();

            if (Config["metroSaveGamesFolder"] == "." || Config["betterSaveGamesFolder"] == ".")
                return;

            PopulateDatesListBox(datesListBox, Config["betterSaveGamesFolder"] + $"\\{Config[$"chapterNames:{RadioButtonSelectedIndex}"]}");

           datesListBox.SelectedIndex = DateListSelectedIndex;
            
            var chapterName = ChapterNames[RadioButtonSelectedIndex];

            if (datesListBox.SelectedItem == null)
                return;

            var folderPath = Config["betterSaveGamesFolder"] + "\\" + chapterName + "\\" + datesListBox.SelectedItem.ToString();

            PopulateListBox(quickSavesListBox, folderPath, "quick");
            PopulateListBox(autoSavesListBox, folderPath, "auto");
        }

        private void OnDateSelectionChangedEvent(object sender, SelectionChangedEventArgs e)
        {
            var chapterName = ChapterNames[RadioButtonSelectedIndex];

            if (datesListBox.SelectedIndex >= 0)
                DateListSelectedIndex = datesListBox.SelectedIndex;

            if (datesListBox.SelectedItem == null)
                return;

            var folderPath = Config["betterSaveGamesFolder"] + "\\" + chapterName + "\\" + datesListBox.SelectedItem.ToString();

            AutoSavesSelectedIndex = 0;
            QuickSaveSelectedIndex = 0;

            PopulateListBox(autoSavesListBox, folderPath, "auto");
            PopulateListBox(quickSavesListBox, folderPath, "quick");
        }

        private void OnTabSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshListBoxes();
        }

        private void RestoreScreenPosition()
        {
            var windowPosX = int.Parse(Config["windowPosX"]);
            var windowPosY = int.Parse(Config["windowPosY"]);

            mainWindow.WindowStartupLocation = WindowStartupLocation.Manual;
            mainWindow.Left = windowPosX;
            mainWindow.Top = windowPosY;
        }

        private void SetPathsAndInitialPopulate()
        {
            if (Config["metroSaveGamesFolder"] != ".")
                metroSaveGamesPath.Text = Config["metroSaveGamesFolder"];
            else
                metroSaveGamesPath.Text = "Metro save games folder could not be found. Please set location.";

            if (Config["betterSaveGamesFolder"] != ".")
            {
                metroBetterSaveGamesPath.Text = Config["betterSaveGamesFolder"];

                PopulateDatesListBox(datesListBox, Config["betterSaveGamesFolder"] + $"\\{Config[$"chapterNames:{RadioButtonSelectedIndex}"]}");

                if (Directory.Exists(Config["betterSaveGamesFolder"] + $"\\{Config[$"chapterNames:{RadioButtonSelectedIndex}"]}"))
                {
                    DirectoryInfo dinfo = new DirectoryInfo(Config["betterSaveGamesFolder"] + $"\\{Config[$"chapterNames:{RadioButtonSelectedIndex}"]}");
                    DirectoryInfo[] dirs = dinfo.GetDirectories();

                    if (dirs.Length > 0)
                    {
                        PopulateListBox(autoSavesListBox, dirs[0].FullName, "auto");
                        PopulateListBox(quickSavesListBox, dirs[0].FullName, "quick");
                    }
                }
            }
            else
            {
                metroBetterSaveGamesPath.Text = "MEBetterSaves save games folder not set. Please provide a location.";
            }
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
       

        private void PopulateDatesListBox(ListBox lsb, string folder)
        {
            lsb.Items.Clear();

            if (!Directory.Exists(folder))
                return;

            DirectoryInfo dinfo = new DirectoryInfo(folder);
            DirectoryInfo[] dirs = dinfo.GetDirectories();
            
            if (dirs.Length > 0)
            {
                Array.Sort(dirs, CompareDirectoryInfo);
                Array.Reverse(dirs);
            }

            foreach (var dir in dirs)
            {
                lsb.Items.Add(dir.Name);
            }

            datesListBox.SelectedIndex = DateListSelectedIndex;

            if (datesListBox.Items.Count > 0 && datesListBox.Items.Count > DateListSelectedIndex)
                datesListBox.SelectedItem = datesListBox.Items[DateListSelectedIndex];
        }

        private void PopulateListBox(ListBox lsb, string folder, string saveType)
        {
            lsb.Items.Clear();

            if (!Directory.Exists(folder))
                return;

            DirectoryInfo dinfo = new DirectoryInfo(folder);
            FileInfo[] Files = dinfo.GetFiles();

            if (Files.Length > 0)
            {
                Array.Sort(Files, CompareFileInfo);
                Array.Reverse(Files);
                
            }

            foreach (FileInfo file in Files)
            {
                if (file.Name.Contains(saveType) && !file.Name.Contains(".png"))
                    lsb.Items.Add(file.Name);
            }

            if (saveType == "auto")
            {
                autoSavesListBox.SelectedIndex = AutoSavesSelectedIndex;

                if (autoSavesListBox.Items.Count > 0 && autoSavesListBox.Items.Count > AutoSavesSelectedIndex)
                    autoSavesListBox.SelectedItem = autoSavesListBox.Items[AutoSavesSelectedIndex];
            }
            else
            {
                quickSavesListBox.SelectedIndex = QuickSaveSelectedIndex;

                if (quickSavesListBox.Items.Count > 0 && quickSavesListBox.Items.Count > QuickSaveSelectedIndex)
                    quickSavesListBox.SelectedItem = quickSavesListBox.Items[QuickSaveSelectedIndex];
            }
        }

        public static int CompareFileInfo(FileInfo s1, FileInfo s2)
        {
            return s1.Name.CompareTo(s2.Name);
        }

        public static int CompareDirectoryInfo(DirectoryInfo s1, DirectoryInfo s2)
        {
            return s1.Name.CompareTo(s2.Name);
        }

        private void SetMetroSavesGamesPathButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            var result = dialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK && dialog.SelectedPath.CompareTo(Config["betterSaveGamesFolder"]) != 0)
            {
                JObject configObj = JObject.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "config.json")));
                configObj["metroSaveGamesFolder"] = dialog.SelectedPath;
                File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "config.json"), configObj.ToString());
                Config.Reload();

                metroSaveGamesPath.Text = dialog.SelectedPath;

                RefreshListBoxes();
            }
            else
            {
                metroSaveGamesPath.Text = "Either a folder was not selected or folder is the same as this applications save games folder.";
                SystemSounds.Exclamation.Play();
            }
        }

        private void SetMetroBetterSavesGamesPathButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            var result = dialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK && dialog.SelectedPath.CompareTo(Config["metroSaveGamesFolder"]) != 0)
            {
                JObject configObj = JObject.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "config.json")));
                configObj["betterSaveGamesFolder"] = dialog.SelectedPath;
                File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "config.json"), configObj.ToString());
                Config.Reload();

                metroBetterSaveGamesPath.Text = dialog.SelectedPath;

                MainApp.CreateFolders();
                RefreshListBoxes();
            }
            else
            {
                metroBetterSaveGamesPath.Text = "Either a folder was not selected or folder is the same as metro save games folder.";
                SystemSounds.Exclamation.Play();
            }
        }

        private void OnAutoSavesMouseDoubleClickHandler(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                var item = (sender as ListBox).SelectedItem;
                if (item != null)
                {
                    if (autoSavesListBox == null)
                        return;

                    if (autoSavesListBox.SelectedItem == null)
                        return;

                    var sourceFile = Config["betterSaveGamesFolder"] + $"\\{ChapterNames[RadioButtonSelectedIndex]}\\{datesListBox.SelectedItem.ToString()}\\{item.ToString()}";
                    var destinationFile = Config["metroSaveGamesFolder"] + "\\m3_auto_save";

                    if (File.Exists(destinationFile))
                        File.Delete(destinationFile);

                    File.Copy(sourceFile, destinationFile, true);
                }
            }
        }

        private void OnQuickSavesMouseDoubleClickHandler(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                var item = (sender as ListBox).SelectedItem;
                if (item != null)
                {
                    if (datesListBox == null)
                        return;

                    if (datesListBox.SelectedItem == null)
                        return;

                    var sourceFile = Config["betterSaveGamesFolder"] + $"\\{ChapterNames[RadioButtonSelectedIndex]}\\{datesListBox.SelectedItem.ToString()}\\{item.ToString()}";
                    var destinationFile = Config["metroSaveGamesFolder"] + "\\m3_quick_save";

                    if (File.Exists(destinationFile))
                        File.Delete(destinationFile);

                    File.Copy(sourceFile, destinationFile, true);
                }
            }
            
        }

        private void OnRadioButtonChecked(object sender, RoutedEventArgs e)
        {
            var radioButton = (RadioButton)sender;
            var checkedIndex = int.Parse(radioButton.Tag.ToString());

            if (RadioButtonSelectedIndex != checkedIndex)
            {
                DateListSelectedIndex = 0;
                AutoSavesSelectedIndex = 0;
                QuickSaveSelectedIndex = 0;
                AutoSaveImageSource = null;
                QuickSaveImageSource = null;
                autoSaveImage.Source = null;
                quickSaveImage.Source = null;
                RadioButtonSelectedIndex = checkedIndex;
                RefreshListBoxes();
            }
            
        }

        private void OnQuickSaveSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (datesListBox.SelectedItem != null && quickSavesListBox.SelectedItem != null)
            {
                if (quickSavesListBox.SelectedIndex >= 0)
                    QuickSaveSelectedIndex = quickSavesListBox.SelectedIndex;

                var imageFile = Config["betterSaveGamesFolder"] + $"\\{ChapterNames[RadioButtonSelectedIndex]}\\{datesListBox.SelectedItem.ToString()}\\{quickSavesListBox.SelectedItem.ToString()}.png";

                BitmapImage b = new BitmapImage();

                try
                {
                    b.BeginInit();
                    b.UriSource = new Uri(imageFile);
                    b.EndInit();

                    quickSaveImage.Source = b;
                }
                catch
                {
                    quickSaveImage.Source = null;
                }
            }
        }

        private void OnAutoSaveSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (datesListBox.SelectedItem != null && autoSavesListBox.SelectedItem != null)
            {
                if (autoSavesListBox.SelectedIndex >= 0)
                    AutoSavesSelectedIndex = autoSavesListBox.SelectedIndex;

                var imageFile = Config["betterSaveGamesFolder"] + $"\\{ChapterNames[RadioButtonSelectedIndex]}\\{datesListBox.SelectedItem.ToString()}\\{autoSavesListBox.SelectedItem.ToString()}.png";

                BitmapImage b = new BitmapImage();

                try
                {
                    b.BeginInit();
                    b.UriSource = new Uri(imageFile);
                    b.EndInit();

                    autoSaveImage.Source = b;
                }
                catch
                {
                    autoSaveImage.Source = null;
                }
            }
        }

        private void AutoSaveImageLoaded(object sender, RoutedEventArgs e)
        {
            if (datesListBox.SelectedItem == null || autoSavesListBox.SelectedItem == null)
                return;

            var imageFile = Config["betterSaveGamesFolder"] + $"\\{ChapterNames[RadioButtonSelectedIndex]}\\{datesListBox.SelectedItem.ToString()}\\{autoSavesListBox.SelectedItem.ToString()}.png";

            BitmapImage b = new BitmapImage();
            var image = sender as Image;

            try
            {
                b.BeginInit();
                b.UriSource = new Uri(imageFile);
                b.EndInit();

                image.Source = b;
            }
            catch
            {
                image.Source = null;
            }

           
        }

        private void QuickSaveImageLoaded(object sender, RoutedEventArgs e)
        {
            if (datesListBox.SelectedItem == null || quickSavesListBox.SelectedItem == null)
                return;

            var imageFile = Config["betterSaveGamesFolder"] + $"\\{ChapterNames[RadioButtonSelectedIndex]}\\{datesListBox.SelectedItem.ToString()}\\{quickSavesListBox.SelectedItem.ToString()}.png";

            BitmapImage b = new BitmapImage();
            var image = sender as Image;

            try
            {
                b.BeginInit();
                b.UriSource = new Uri(imageFile);
                b.EndInit();

                image.Source = b;
            }
            catch
            {
                image.Source = null;
            }
        }

        private void AutoSaveImageOnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var image = sender as Image;

            if (image == null)
                return;

            if (QuickSaveImageSource == null)
            {
                image.Width = image.Width * 2;
                image.Height = image.Height * 2;
                QuickSaveImageSource = quickSaveImage.Source;
                quickSaveImage.Source = null;
            }
            else
            {

                image.Width = image.Width / 2;
                image.Height = image.Height / 2;
                quickSaveImage.Source = QuickSaveImageSource;
                QuickSaveImageSource = null;
            }
        }

        private void QuickSaveImageOnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var image = sender as Image;

            if (image == null)
                return;

            if (AutoSaveImageSource == null)
            {
                image.Width = image.Width * 2;
                image.Height = image.Height * 2;
                AutoSaveImageSource = autoSaveImage.Source;
                autoSaveImage.Source = null;
                quickSaveImage.Margin = autoSaveImage.Margin;
            }
            else
            {

                image.Width = image.Width / 2;
                image.Height = image.Height / 2;
                autoSaveImage.Source = AutoSaveImageSource;
                AutoSaveImageSource = null;
                quickSaveImage.Margin = QuickSaveImageMargin;
            }
        }

        private void LoadButtonClick(object sender, RoutedEventArgs e)
        {
            CopySaveGameFiles();
        }

        private void RestartButtonClick(object sender, RoutedEventArgs e)
        {
            CopySaveGameFiles();

            var processes = Process.GetProcessesByName("MetroExodus");
            if (processes.Length != 0)
                processes[0].Kill();

            Process.Start("com.epicgames.launcher://apps/Snapdragon?action=launch&silent=true");
        }

        private void CopySaveGameFiles()
        {
            bool autoSaveCopied = false;
            bool quickSaveCopied = false;

            var item = autoSavesListBox.SelectedItem;
            if (item != null)
            {
                if (autoSavesListBox == null)
                    return;

                if (autoSavesListBox.SelectedItem == null)
                    return;

                var sourceFile = Config["betterSaveGamesFolder"] + $"\\{ChapterNames[RadioButtonSelectedIndex]}\\{datesListBox.SelectedItem.ToString()}\\{item.ToString()}";
                var destinationFile = Config["metroSaveGamesFolder"] + "\\m3_auto_save";

                if (File.Exists(destinationFile))
                    File.Delete(destinationFile);

                File.Copy(sourceFile, destinationFile, true);

                autoSaveCopied = true;
            }

            item = quickSavesListBox.SelectedItem;
            if (item != null)
            {
                if (datesListBox == null)
                    return;

                if (datesListBox.SelectedItem == null)
                    return;

                var sourceFile = Config["betterSaveGamesFolder"] + $"\\{ChapterNames[RadioButtonSelectedIndex]}\\{datesListBox.SelectedItem.ToString()}\\{item.ToString()}";
                var destinationFile = Config["metroSaveGamesFolder"] + "\\m3_quick_save";

                if (File.Exists(destinationFile))
                    File.Delete(destinationFile);

                File.Copy(sourceFile, destinationFile, true);

                quickSaveCopied = true;
            }

            if (!autoSaveCopied && !quickSaveCopied)
                loadedLabel.Content = "Save games could not be copied.";
            else if (!autoSaveCopied)
                loadedLabel.Content = "Auto save game could not be copied.";
            else if (!quickSaveCopied)
                loadedLabel.Content = "Quick save game could not be copied.";
            else
                loadedLabel.Content = "Save games have been copied.";
        }
    }
}
