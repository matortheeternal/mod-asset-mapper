﻿using IniParser;
using IniParser.Model;
using ModAnalyzer.Analysis.Services;
using ModAnalyzer.Domain.Models;
using ModAnalyzer.Utils;
using Newtonsoft.Json;
using System;
using System.IO;

namespace ModAnalyzer.Domain.Services {
    public static class SettingsService {
        public static ProgramSetting Settings { get; set; }
        public static bool NewSettings { get; set; }

        public static void DefaultSettings() {
            Settings = new ProgramSetting();
        }

        public static string GamePath(string gameName) {
            return (string) Settings.GetType().GetProperty(gameName + "Path").GetValue(Settings, null);
        }

        public static void LoadSettings() {
            if (File.Exists("settings.json")) {
                LogService.GroupMessage("settings", "Found settings file.");
                try {
                    string json = File.ReadAllText("settings.json");
                    Settings = JsonConvert.DeserializeObject<ProgramSetting>(json);
                }
                catch (Exception x) {
                    LogService.GroupMessage("settings", "Exception loading settings: " + x.Message);
                    DialogUtils.ShowError(x.Message, "Exception loading settings");
                    DefaultSettings();
                }
            }
            else {
                LogService.GroupMessage("settings", "No settings file found, initializing defaults.");
                NewSettings = true;
                DefaultSettings();
            }
        }

        public static void SaveSettings() {
            if (NewSettings) NewSettings = false;
            LogService.GroupMessage("settings", "Saved.");
            string json = JsonConvert.SerializeObject(Settings);
            File.WriteAllText("settings.json", json);
            SaveIniSettings();
        }

        public static void SaveIniSettings() {
            try {
                if (!File.Exists("settings.ini")) {
                    LogService.GroupMessage("settings", "Initializing ModDump setting.ini");
                    ModDump.InitSettings();
                }
                FileIniDataParser parser = new FileIniDataParser();
                IniData data = parser.ReadFile("settings.ini");
                data.Sections["Games"]["skyrimPath"] = PathExtensions.AppendDelimiter(Settings.SkyrimPath);
                data.Sections["Games"]["skyrimSEPath"] = PathExtensions.AppendDelimiter(Settings.SkyrimSEPath);
                parser.WriteFile("settings.ini", data);
            } catch (Exception x) {
                LogService.GroupMessage("settings", "Error settings ModDump settings: " + x.Message);
            }
        }
    }
}
