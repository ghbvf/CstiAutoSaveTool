using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.CompilerServices.RuntimeHelpers;
using UnityEngine;
using UnityEditor;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using TMPro;
using BepInEx.Configuration;
using static System.Net.Mime.MediaTypeNames;
using System.Xml.Linq;
using System.Runtime.Remoting.Contexts;

namespace CstiAutoSaveTool
{
    [BepInPlugin("CstiAutoSaveTool", "CstiAutoSaveTool", "1.0.1")]
    public class CstiAutoSaveTool : BaseUnityPlugin
    {
        private bool ShowGUI;
        private int CurrentPage = 0;
        private int MaxPages;
        private Vector2 FilesListScrollView;

        public static KeyCode SaveGameKey;
        public static KeyCode SaveAndBackupKey;
        public static KeyCode LoadGameKey;
        public static KeyCode GameLoadPageKey;
        public static ManualLogSource Log { get; set; }

        private static int currentGameDataIndex
        {
            get
            {
                return GameLoad.Instance.CurrentGameDataIndex;
            }
        }
        private static string GameFilesDirectoryPath
        {
            get
            {
                return string.Format("{0}/Games", UnityEngine.Application.persistentDataPath);
            }
        }

        private static string BackupsPath
        {
            get
            {
                return string.Format("{0}/Backups", UnityEngine.Application.persistentDataPath);
            }
        }
        private static string MainSaveFilePath
        {
            get
            {
                return string.Format("{0}/SaveData.json", UnityEngine.Application.persistentDataPath);
            }
        }

        private void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(CstiAutoSaveTool));
            this.ShowGUI = false;
            CstiAutoSaveTool.Log = Logger;
            CstiAutoSaveTool.SaveAndBackupKey = Config.Bind<KeyCode>("Keys", "Save And Backup", KeyCode.F4, "Save And Backup").Value;
            CstiAutoSaveTool.SaveGameKey = Config.Bind<KeyCode>("Keys", "Save Game", KeyCode.F5, "Save Game").Value;
            CstiAutoSaveTool.LoadGameKey = Config.Bind<KeyCode>("Keys", "Load Game", KeyCode.F6, "Load Game").Value;
            CstiAutoSaveTool.GameLoadPageKey = Config.Bind<KeyCode>("Keys", "Load Page", KeyCode.F3, "Load Page").Value;
            Log.LogInfo("Plugin CstiAutoSaveTool is loaded!");
        }

        private void Update()
        {
            if (Input.GetKeyDown(CstiAutoSaveTool.GameLoadPageKey))
            {
                this.ShowGUI = !this.ShowGUI;
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(GameManager), "Update")]
        private static void GameManagerUpdatePostfix(GameManager __instance)
        {
            if (Input.GetKeyDown(CstiAutoSaveTool.SaveGameKey))
            {
                SaveGame(__instance);
            }
            if (Input.GetKeyDown(CstiAutoSaveTool.SaveAndBackupKey))
            {
                SaveAndBackup(__instance);
            }
            if (Input.GetKeyDown(CstiAutoSaveTool.LoadGameKey))
            {
                LoadGame(__instance);
            }
        }

        public static void SaveGame(GameManager __instance)
        {
            GameLoad.Instance.AutoSaveGame(false);
            Log.LogInfo("Save Game");
        }

        public static void SaveAndBackup(GameManager __instance)
        {
            SaveGame(__instance);

            string currentTime = DateTime.Now.ToString("yyyyMMddHHmmss");
            string daytimeToHour = GameManager.TotalTicksToHourOfTheDayString(GameManager.HoursToTick((float)__instance.DaySettings.DayStartingHour) + __instance.CurrentTickInfo.z, __instance.CurrentMiniTicks);
            string currentGameDay = __instance.CurrentDay.ToString();
            string currentGameHour = daytimeToHour.Split(':')[0].ToString();
            string currentGameMinute = daytimeToHour.Split(':')[1].ToString();
            string currentGameTime = string.Format("{0}Days{1}{2}", currentGameDay, currentGameHour, currentGameMinute);
            string backupPath = string.Format("{0}/csti__{1}__{2}", BackupsPath, currentTime, currentGameTime);
            string backupGamePath = string.Format("{0}/{1}", backupPath, "Games");
            if (!Directory.Exists(backupPath))
            {
                Directory.CreateDirectory(backupPath);
            }
            if (!Directory.Exists(backupGamePath))
            {
                Directory.CreateDirectory(backupGamePath);
            }

            string backupMainSaveFilePath = string.Format("{0}/SaveData.json", backupPath);
            if (File.Exists(MainSaveFilePath))
            {
                File.Copy(MainSaveFilePath, backupMainSaveFilePath);
                Log.LogInfo(string.Format("Copy File \"{0}\" To \"{1}\"", MainSaveFilePath, backupMainSaveFilePath));
            }

            string gameFileName = string.Format("Slot_{0}.json", currentGameDataIndex + 1);
            string gameFilePath = string.Format("{0}/{1}", GameFilesDirectoryPath, gameFileName);
            string backupGameFilePath = string.Format("{0}/{1}", backupGamePath, gameFileName);
            if (File.Exists(gameFilePath))
            {
                File.Copy(gameFilePath, backupGameFilePath);
                Log.LogInfo(string.Format("Copy File \"{0}\" To \"{1}\"", gameFilePath, backupGameFilePath));
            }
            string gameInfo = string.Format("GameTime:Slot_{0} {1}Days{2}Hour{3}Minute  ", currentGameDataIndex + 1, currentGameDay, currentGameHour, currentGameMinute, currentTime);
            File.WriteAllText(string.Format("{0}/index", backupPath), currentGameDataIndex.ToString());
            File.WriteAllText(string.Format("{0}/game.info", backupPath), gameInfo);
            File.AppendAllText(string.Format("{0}/game.info", backupPath), string.Format("BackupTime:{0}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
        }

        public static void LoadGame(GameManager __instance)
        {
            GameLoad.Instance.AutoLoadGame();
            Log.LogInfo("Load Last Game");
        }

        private void OnGUI()
        {
            if (!ShowGUI)
            {
                return;
            }
            GUILayout.BeginArea(new Rect((float)Screen.width * 0.45f, (float)Screen.height * 0.2f, (float)Screen.width * 0.25f, (float)Screen.height * 0.68f));
            //this.GeneralOptionsGUI();
            this.FilesGUI();
            this.MenuGui();
            GUILayout.EndArea();
        }

        private void FilesGUI()
        {
            GUILayout.BeginVertical("box", Array.Empty<GUILayoutOption>());
            GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
            GUILayout.Label("CstiAutoSaveTool", Array.Empty<GUILayoutOption>());
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("X", new GUILayoutOption[] { }))
            {
                this.ShowGUI = false;
            }
            GUILayout.EndHorizontal();
            this.FilesListScrollView = GUILayout.BeginScrollView(this.FilesListScrollView, new GUILayoutOption[] { GUILayout.ExpandHeight(true) });
            DirectoryInfo folder = new DirectoryInfo(BackupsPath);
            DirectoryInfo[] directoryInfos = folder.GetDirectories();

            MaxPages = directoryInfos.Length / 10 + 1;
            for (int i = 0; i < directoryInfos.Length; i++)
            {
                if (i <= 10 * this.CurrentPage - 1)
                {
                    continue;
                }
                if (i / 10 != this.CurrentPage && i >= 10 * this.CurrentPage)
                {
                    break;
                }
                DirectoryInfo dir = directoryInfos[i];
                string gameInfoPath = string.Format("{0}/game.info", dir.FullName);
                try
                {
                    if (File.Exists(gameInfoPath))
                    {
                        string gameInfo = File.ReadAllText(gameInfoPath);
                        GUILayout.BeginHorizontal("box", Array.Empty<GUILayoutOption>());
                        GUILayout.Label(string.Format("{0}\r\nDirectoryPath:{1}", gameInfo, dir.Name), Array.Empty<GUILayoutOption>());
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Load", Array.Empty<GUILayoutOption>()))
                        {
                            LoadBackupGame(dir.FullName);
                        }
                        else if (GUILayout.Button("Delete", new GUILayoutOption[] { }))
                        {
                            if(dir.Exists)
                            {
                                DeleteBackupFile(dir.FullName);
                            }
                        }
                        GUILayout.EndHorizontal();
                    }
                }
                catch 
                { 

                }
            }
            GUILayout.EndScrollView();

            GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
            if (this.CurrentPage == 0)
            {
                GUILayout.Box("<", new GUILayoutOption[]
                {
                GUILayout.Width(25f)
                });
            }
            else if (GUILayout.Button("<", new GUILayoutOption[]
            {
            GUILayout.Width(25f)
            }))
            {
                this.CurrentPage--;
            }
            GUILayout.FlexibleSpace();
            GUILayout.Label(string.Format("{0}/{1}", (this.CurrentPage + 1).ToString(), this.MaxPages.ToString()), Array.Empty<GUILayoutOption>());
            GUILayout.FlexibleSpace();
            if (this.CurrentPage == this.MaxPages - 1)
            {
                GUILayout.Box(">", new GUILayoutOption[]
                {
                GUILayout.Width(25f)
                });
            }
            else if (GUILayout.Button(">", new GUILayoutOption[]
            {
            GUILayout.Width(25f)
            }))
            {
                this.CurrentPage++;
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }
   
        private void MenuGui()
        {
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
            if (GUILayout.Button("Save And Backup", new GUILayoutOption[] { }))
            {
                GameManager __instance = GameManager.Instance;
                SaveAndBackup(__instance);
            }
            if (GUILayout.Button("Load Last Game", new GUILayoutOption[] { }))
            {
                //this.ShowGUI = false;
                GameManager __instance = GameManager.Instance;
                LoadGame(__instance);
            }
            if (GUILayout.Button("Close", new GUILayoutOption[]{ }))
            {
                this.ShowGUI = false;
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private static void  DeleteBackupFile(string backupPath)
        {
            string backupGamePath = string.Format("{0}/{1}", backupPath, "Games");
            try
            {
                if (Directory.Exists(backupGamePath))
                {
                    DirectoryInfo dir = new DirectoryInfo(backupGamePath);
                    foreach(FileInfo file in dir.GetFiles())
                    {
                        file.Delete();
                    }
                    dir.Delete();
                }

                if (Directory.Exists(backupPath))
                {
                    DirectoryInfo dir = new DirectoryInfo(backupPath);
                    foreach (FileInfo file in dir.GetFiles())
                    {
                        file.Delete();
                    }
                    dir.Delete();
                }
            }
            catch
            { 
                
            }

        }

        private static void LoadBackupGame(string backupPath)
        {
            string indexFilePath = string.Format("{0}/index", backupPath);
            int backupGameDataIndex = Convert.ToInt32(File.ReadAllText(indexFilePath));
            string backupGamePath = string.Format("{0}/{1}", backupPath, "Games");
            string backupMainSaveFilePath = string.Format("{0}/SaveData.json", backupPath);
            string gameFileName = string.Format("Slot_{0}.json", backupGameDataIndex + 1);
            string gameFilePath = string.Format("{0}/{1}", GameFilesDirectoryPath, gameFileName);
            string backupGameFilePath = string.Format("{0}/{1}", backupGamePath, gameFileName);

            if (File.Exists(backupMainSaveFilePath))
            {
                File.Copy(backupMainSaveFilePath, MainSaveFilePath, true);
                Log.LogInfo(string.Format("Copy File \"{0}\" To \"{1}\"", backupMainSaveFilePath, MainSaveFilePath));
            }
            if (File.Exists(backupGameFilePath))
            {
                File.Copy(backupGameFilePath, gameFilePath, true);
                Log.LogInfo(string.Format("Copy File \"{0}\" To \"{1}\"", backupGameFilePath, gameFilePath));
            }
            GameManager __instance = GameManager.Instance;
            GameLoad.Instance.LoadGame(backupGameDataIndex);
        }

    }
}