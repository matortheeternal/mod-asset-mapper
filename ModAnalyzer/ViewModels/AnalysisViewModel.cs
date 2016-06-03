﻿using BA2Lib;
using GalaSoft.MvvmLight;
using libbsa;
using Newtonsoft.Json;
using SharpCompress.Archive;
using SharpCompress.Common;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Text;

namespace ModAnalyzer.ViewModels
{
    public class AnalysisViewModel : ViewModelBase
    {
        private readonly BA2NET _ba2Manager;
        private readonly BSANET _bsaManager;
        private ModAnalysis _modAnalysis;

        public ObservableCollection<string> LogMessages { get; set; }

        public AnalysisViewModel()
        {
            _ba2Manager = new BA2NET();
            _bsaManager = new BSANET();
            _modAnalysis = new ModAnalysis();

            ModDump.StartModDump();

            LogMessages = new ObservableCollection<string>();
        }

        ~AnalysisViewModel()
        {
            _ba2Manager.Dispose();
            _bsaManager.bsa_close();
            ModDump.EndModDump();
        }

        private void GetEntryMap(string path)
        {
            IArchive archive = ArchiveFactory.Open(@path);

            ProgressMessage = "Analyzing archive entries...";

            foreach (IArchiveEntry entry in archive.Entries)
            {
                if (entry.IsDirectory)
                    continue;

                string entryPath = entry.Key.Replace('/', '\\');
                _modAnalysis.Assets.Add(entryPath);

                LogMessages.Add(entryPath);

                string extension = Path.GetExtension(entryPath).ToUpper();

                switch (extension)
                {
                    case ".BA2":
                        ProgressMessage = "Extracting BA2 at " + entryPath;
                        HandleBA2(entry);
                        ProgressMessage = "Analyzing archive entries...";
                        break;
                    case ".BSA":
                        ProgressMessage = "Extracting BSA at " + entryPath;
                        HandleBSA(entry);
                        ProgressMessage = "Analyzing archive entries...";
                        break;
                    case ".ESP":
                    case ".ESM":
                        ProgressMessage = "Extracting " + extension + " at " + entryPath;
                        HandlePlugin(entry);
                        ProgressMessage = "Analyzing plugin file...";
                        break;
                }
            }
        }

        public void HandleBA2(IArchiveEntry entry)
        {
            entry.WriteToDirectory(@".\\bsas", ExtractOptions.ExtractFullPath | ExtractOptions.Overwrite);

            ProgressMessage = "BSA extracted, Analyzing entries...";

            string rootPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string ba2Path = Path.Combine(rootPath, @"\bsas\", entry.Key);

            if (_ba2Manager.Open(ba2Path))
            {
                string[] entries = _ba2Manager.GetNameTable();

                for (int i = 0; i < entries.Length; i++)
                {
                    string entryPath = entry.Key + "\\" + entries[i];
                    _modAnalysis.Assets.Add(entryPath);
                    LogMessages.Add(entryPath);
                }
            }
        }

        public void HandleBSA(IArchiveEntry entry)
        {
            entry.WriteToDirectory(@".\\bsas", ExtractOptions.ExtractFullPath | ExtractOptions.Overwrite);

            string rootPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string bsaPath = Path.Combine(rootPath, @"\bsas\", entry.Key);

            if (_bsaManager.bsa_open(bsaPath) == 0)
            {
                string[] entries = _bsaManager.bsa_get_assets(".*");
                for (int i = 0; i < entries.Length; i++)
                {
                    string entryPath = entry.Key + "\\" + entries[i];
                    _modAnalysis.Assets.Add(entryPath);
                    LogMessages.Add(entryPath);
                }
            }
        }

        public void HandlePlugin(IArchiveEntry entry)
        {
            string dataPath = @"C:\SteamLibrary\steamapps\common\Skyrim\data";

            string pluginPath = Path.Combine(dataPath, entry.Key);

            bool deleteAfter = false;
            if (!File.Exists(pluginPath))
            {
                deleteAfter = true;
                entry.WriteToDirectory(dataPath, ExtractOptions.ExtractFullPath | ExtractOptions.Overwrite);
            }

            //TODO: This should be dynamic
            ModDump.SetGameMode(1);

            StringBuilder message = new StringBuilder(4 * 1024 * 1024);

            // prepare plugin file for dumping
            if (!ModDump.Prepare(pluginPath))
            {
                ModDump.GetBuffer(message, message.Capacity);
                LogMessages.Add(message.ToString());
                return;
            }

            // dump the plugin file
            int maxDumpSize = 4 * 1024 * 1024; // 4MB maximum dump size
            StringBuilder json = new StringBuilder(maxDumpSize);
            if (!ModDump.Dump(json, maxDumpSize))
            {
                ModDump.GetBuffer(message, message.Capacity);
                LogMessages.Add(message.ToString());
                return;
            }

            PluginDump pluginDump = JsonConvert.DeserializeObject<PluginDump>(json.ToString());
            _modAnalysis.Plugins.Add(pluginDump);

            // log the results
            // TODO: This should be handled better.
            ModDump.GetBuffer(message, message.Capacity);
            LogMessages.Add(message.ToString());

            if (deleteAfter)
            {
                File.Delete(pluginPath);
            }
        }
    }
}
