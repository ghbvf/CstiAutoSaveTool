using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.CompilerServices.RuntimeHelpers;
using UnityEngine;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using BepInEx.Configuration;
using static System.Net.Mime.MediaTypeNames;
using System.Xml.Linq;

namespace CstiAutoSaveTool
{
    [BepInPlugin("CstiAutoSaveTool", "CstiAutoSaveTool", "1.0.0")]
    public class CstiAutoSaveTool: BaseUnityPlugin
    {
        public static KeyCode SaveGameKey;
        public static KeyCode SaveAndBackup;
        public static KeyCode LoadGameKey;
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
            CstiAutoSaveTool.Log = Logger;
            CstiAutoSaveTool.SaveGameKey = Config.Bind<KeyCode>("Keys", "Save Game", KeyCode.F5, "Save Game").Value;
            CstiAutoSaveTool.LoadGameKey = Config.Bind<KeyCode>("Keys", "Load Game", KeyCode.F6, "Load Game").Value;
            CstiAutoSaveTool.SaveAndBackup = Config.Bind<KeyCode>("Keys", "Save And Backup", KeyCode.F4, "Save And Backup").Value;
            Log.LogInfo("Plugin CstiAutoSaveTool is loaded!");
        }

        [HarmonyPostfix, HarmonyPatch(typeof(GameManager), "Update")]
        private static void GameManagerUpdatePostfix(GameManager __instance)
        {
            if (Input.GetKeyDown(CstiAutoSaveTool.SaveGameKey))
            {
                GameLoad.Instance.AutoSaveGame(false);
                Log.LogInfo("Save Game");
            }
            if (Input.GetKeyDown(CstiAutoSaveTool.SaveAndBackup))
            {
                GameLoad.Instance.AutoSaveGame(false);
                Log.LogInfo("Save Game");

                string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH_mm_ss");
                string daytimeToHour = GameManager.TotalTicksToHourOfTheDayString(GameManager.HoursToTick((float)__instance.DaySettings.DayStartingHour) + __instance.CurrentTickInfo.z, __instance.CurrentMiniTicks);
                string currentGameDay = __instance.CurrentDay.ToString();
                string currentGameHour = daytimeToHour.Split(':')[0].ToString();
                string currentGameMinute = daytimeToHour.Split(':')[1].ToString();
                string currentGameTime = string.Format("{0}_{1}_{2}", currentGameDay, currentGameHour, currentGameMinute);
                string backupPath = string.Format("{0}/{1}__{2}", CstiAutoSaveTool.BackupsPath, currentTime, currentGameTime);
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
            }
            if (Input.GetKeyDown(CstiAutoSaveTool.LoadGameKey))
            {
                GameLoad.Instance.AutoLoadGame();
                Log.LogInfo("Load Last Game");
            }
        }
    }
}
