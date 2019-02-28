using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace MEBetterSaves
{
    public partial class App : Application
    {
        private IConfigurationRoot Config;
        private MainWindow MainWindowRef;
        private string[] ChapterNames;
        private DateTime LastAutoSaveWriteTime;
        private DateTime LastQuickSaveWriteTime;
        private Timer AutoSaveTimer;
        private Timer QuickSaveTimer;
        private Hashtable CurrentFileHashes;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            Config = BuildConfig();
            ChapterNames = Config.GetSection("chapterNames").GetChildren().Select(x => x.Value).ToArray();
            LastAutoSaveWriteTime = new DateTime();
            LastQuickSaveWriteTime = new DateTime();
            CurrentFileHashes = new Hashtable();

            SetConfigDefaults();
            CreateFileWatcher();

            MainWindowRef = new MainWindow(this, Config);
            MainWindowRef.Show();
        }

        public void CreateFileWatcher()
        {
            AutoSaveTimer = new Timer(_ =>
            {
                var tasks = new List<Task>();
                tasks.Add(Task.Factory.StartNew(() => { CheckAutoSave(); }));
            },
                null,
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(5));

            QuickSaveTimer = new Timer(_ =>
            {
                var tasks = new List<Task>();
                tasks.Add(Task.Factory.StartNew(() => { CheckQuickSave(); }));
            },
                null,
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(5));
        }

        private void CheckAutoSave()
        {
            if (Config["metroSaveGamesFolder"] == "." || Config["betterSaveGamesFolder"] == ".")
                return;

            var autoSaveFilePath = Config["metroSaveGamesFolder"] + $"\\m3_auto_save";
            var autoSaveFileTempPath = Config["betterSaveGamesFolder"] + $"\\m3_auto_save";

            if (!File.Exists(autoSaveFilePath))
                return;

            var lastWriteTimeFile = File.GetLastWriteTime(autoSaveFilePath);

            if (LastAutoSaveWriteTime < lastWriteTimeFile)
            {
                LastAutoSaveWriteTime = File.GetLastWriteTime(autoSaveFilePath);

                while (!IsFileReady(autoSaveFilePath)) { }

                File.Copy(autoSaveFilePath, autoSaveFileTempPath, true);

                var save = File.ReadLines(autoSaveFileTempPath).Take(25).ToArray();
                var chapter = "";

                foreach (var chapterName in ChapterNames)
                {
                    for (int i = 0; i < save.Length; i++)
                    {
                        if (save[i].Contains(chapterName))
                        {
                            chapter = chapterName;
                            break;
                        }
                    }

                    if (chapter.Length > 0)
                        break;
                }

                var destinationFilePath = Config["betterSaveGamesFolder"] + $"\\{chapter}"+ $"\\{DateTime.Now.ToString("dd-MM-yyyy")}" + $"\\m3_auto_save_{DateTime.Now.ToString("dd-MM-yyyy--HH-mm-ss")}";

                if (!Directory.Exists(Config["betterSaveGamesFolder"] + $"\\{chapter}" + $"\\{DateTime.Now.ToString("dd-MM-yyyy")}"))
                    Directory.CreateDirectory(Config["betterSaveGamesFolder"] + $"\\{chapter}" + $"\\{DateTime.Now.ToString("dd-MM-yyyy")}");

                DirectoryInfo dinfo = new DirectoryInfo(Config["betterSaveGamesFolder"] + $"\\{chapter}" + $"\\{DateTime.Now.ToString("dd-MM-yyyy")}");
                FileInfo[] files = dinfo.GetFiles();

                foreach (var file in files)
                {
                    byte[] hash = MD5.Create().ComputeHash(File.ReadAllBytes(file.FullName));
                    var hashString = BitConverter.ToString(hash).Replace("-", "");

                    if (!file.FullName.Contains(".png") && !CurrentFileHashes.ContainsKey(hashString))
                        CurrentFileHashes.Add(hashString, file.Name);
                }

                dinfo = new DirectoryInfo(Config["betterSaveGamesFolder"]);
                files = dinfo.GetFiles();

                if (files.Length > 0)
                {
                    foreach (var file in files)
                    {
                        byte[] hash = MD5.Create().ComputeHash(File.ReadAllBytes(file.FullName));
                        var hashString = BitConverter.ToString(hash).Replace("-", "");

                        if (CurrentFileHashes.ContainsKey(hashString))
                        {
                            File.Delete(autoSaveFileTempPath);
                            return;
                        }
                    }

                    File.Copy(autoSaveFileTempPath, destinationFilePath);
                    File.Delete(autoSaveFileTempPath);

                    CaptureApplication(destinationFilePath);
                }
            }
        }

        private void CheckQuickSave()
        {
            if (Config["metroSaveGamesFolder"] == "." || Config["betterSaveGamesFolder"] == ".")
                return;

            var quickSaveFilePath = Config["metroSaveGamesFolder"] + $"\\m3_quick_save";
            var quickSaveFileTempPath = Config["betterSaveGamesFolder"] + $"\\m3_quick_save";

            if (!File.Exists(quickSaveFilePath))
                return;

            var lastWriteTimeFile = File.GetLastWriteTime(quickSaveFilePath);

            if (LastQuickSaveWriteTime < lastWriteTimeFile)
            {
                LastQuickSaveWriteTime = File.GetLastWriteTime(quickSaveFilePath);

                while (!IsFileReady(quickSaveFilePath)) { }

                File.Copy(quickSaveFilePath, quickSaveFileTempPath, true);

                var save = File.ReadLines(quickSaveFileTempPath).Take(25).ToArray();
                var chapter = "";

                foreach (var chapterName in ChapterNames)
                {
                    for (int i = 0; i < save.Length; i++)
                    {
                        if (save[i].Contains(chapterName))
                        {
                            chapter = chapterName;
                            break;
                        }
                    }

                    if (chapter.Length > 0)
                        break;
                }

                var destinationFilePath = Config["betterSaveGamesFolder"] + $"\\{chapter}" + $"\\{DateTime.Now.ToString("dd-MM-yyyy")}" + $"\\m3_quick_save_{DateTime.Now.ToString("dd-MM-yyyy--HH-mm-ss")}";

                if (!Directory.Exists(Config["betterSaveGamesFolder"] + $"\\{chapter}" + $"\\{DateTime.Now.ToString("dd-MM-yyyy")}"))
                    Directory.CreateDirectory(Config["betterSaveGamesFolder"] + $"\\{chapter}" + $"\\{DateTime.Now.ToString("dd-MM-yyyy")}");

                DirectoryInfo dinfo = new DirectoryInfo(Config["betterSaveGamesFolder"] + $"\\{chapter}" + $"\\{DateTime.Now.ToString("dd-MM-yyyy")}");
                FileInfo[] files = dinfo.GetFiles();

                foreach (var file in files)
                {
                    byte[] hash = MD5.Create().ComputeHash(File.ReadAllBytes(file.FullName));
                    var hashString = BitConverter.ToString(hash).Replace("-", "");

                    if (!file.FullName.Contains(".png") && !CurrentFileHashes.ContainsKey(hashString))
                        CurrentFileHashes.Add(hashString, file.Name);
                }

                dinfo = new DirectoryInfo(Config["betterSaveGamesFolder"]);
                files = dinfo.GetFiles();

                if (files.Length > 0)
                {
                    foreach (var file in files)
                    {
                        byte[] hash = MD5.Create().ComputeHash(File.ReadAllBytes(file.FullName));
                        var hashString = BitConverter.ToString(hash).Replace("-", "");

                        if (CurrentFileHashes.ContainsKey(hashString))
                        {
                            File.Delete(quickSaveFileTempPath);
                            return;
                        }
                    }

                    File.Copy(quickSaveFileTempPath, destinationFilePath);
                    File.Delete(quickSaveFileTempPath);

                    CaptureApplication(destinationFilePath);
                }
            }
        }

        public void CaptureApplication(string savePath)
        {
            var proc = Process.GetProcessesByName("MetroExodus")[0];
            var rect = new User32.Rect();
            User32.GetWindowRect(proc.MainWindowHandle, ref rect);

            int width = rect.right - rect.left;
            int height = rect.bottom - rect.top;

            var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            Graphics graphics = Graphics.FromImage(bmp);
            graphics.CopyFromScreen(rect.left, rect.top, 0, 0, new System.Drawing.Size(width, height), CopyPixelOperation.SourceCopy);

            bmp.Save($"{savePath}.png", ImageFormat.Png);
        }

        private class User32
        {
            [StructLayout(LayoutKind.Sequential)]
            public struct Rect
            {
                public int left;
                public int top;
                public int right;
                public int bottom;
            }

            [DllImport("user32.dll")]
            public static extern IntPtr GetWindowRect(IntPtr hWnd, ref Rect rect);
        }

        public static bool IsFileReady(string filename)
        {
            try
            {
                using (FileStream inputStream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.None))
                    return inputStream.Length > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void CreateFolders()
        {
            if (Config["metroSaveGamesFolder"] == "." || Config["betterSaveGamesFolder"] == ".")
                return;

            var chapterNames = Config.GetSection("chapterNames").GetChildren().Select(x => x.Value).ToArray();
            var betterSaveGamesDir = Config["betterSaveGamesFolder"];

            foreach (var chapter in chapterNames)
            {
                if (!Directory.Exists(betterSaveGamesDir + chapter.ToString()))
                    Directory.CreateDirectory(betterSaveGamesDir + "\\" + chapter.ToString());
            }
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

                if (!Directory.Exists(userFolder + Config["defaultSaveGamesFolder"]))
                    return;

               

                var metroSaveFilesDir = Directory.GetDirectories($"{userFolder + Config["defaultSaveGamesFolder"]}");

                if (metroSaveFilesDir.Length == 0)
                    return;

                configObj["metroSaveGamesFolder"] = metroSaveFilesDir[0];

                if (File.Exists(metroSaveFilesDir[0] + "\\user.cfg"))
                {
                    var userCfg = File.ReadAllLines(metroSaveFilesDir[0] + "\\user.cfg");

                    foreach (var line in userCfg)
                    {
                        if (line.Contains("quick_save"))
                        {
                            var lineItems = line.Split(' ');
                            configObj["quickSaveButton"] = lineItems[lineItems.Length - 1].Substring(1);
                            break;
                        }
                    }
                }

                configObj["defaultsLoaded"] = 1;

                File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "config.json"), configObj.ToString());
                Config.Reload();
            }
        }
    }
}
