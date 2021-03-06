﻿using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using ModAnalyzer.Utils;
using SevenZipExtractor;

namespace ModAnalyzer.Domain.Services {
    public static class BainArchiveService {

        // CONSTANTS
        private static readonly string[] dataDirectories = {
            "distantlod", "facegen", "fonts", "interface", "menus", "meshes", "music", "scripts", "shaders", "sound", "strings",
            "textures", "trees", "video", "skse", "obse", "nvse", "fose", "asi", "SkyProc Patchers", "Docs", "INI Tweaks"
        };
        private static readonly string[] dataExtensions = { ".BA2", ".BSA", ".ESP", ".ESM" };

        // METHODS
        private static bool IsDataDirectory(ArchiveFile archive, string directory) {
            List<string> childrenDirectories = archive.GetImmediateChildren(directory, true);
            foreach (string childDirectory in childrenDirectories) {
                if (dataDirectories.Contains(Path.GetFileName(childDirectory), StringComparer.OrdinalIgnoreCase)) {
                    return true;
                }
            }

            List<string> childrenFiles = archive.GetImmediateChildren(directory, false);
            foreach (string childFile in childrenFiles) {
                if (dataExtensions.Contains(Path.GetExtension(childFile), StringComparer.OrdinalIgnoreCase)) {
                    return true;
                }
            }

            return false;
        }

        private static bool SkipBainDirectory(string dirName) {
            return dirName == "fomod" || dirName == "omod conversion data" || dirName.StartsWith("--");
        }

        public static List<string> GetDirectories(ArchiveFile archive, string directoryPath, bool data = true) {
            List<string> bainDirectories;
            if (directoryPath == "") {
                bainDirectories = archive.GetLevelDirectories(1);
            }
            else {
                string directoryName = Path.GetDirectoryName(directoryPath);
                bainDirectories = archive.GetImmediateChildren(directoryName, true);
            }

            return bainDirectories.FindAll(d => {
                return !SkipBainDirectory(Path.GetFileName(d)) && (!data || IsDataDirectory(archive, d));
            });
        }

        public static bool IsBainDirectory(ArchiveFile archive, string directoryPath) {
            int validDirectories = 0;
            int invalidDirectories = 0;
            foreach (string childDirectory in archive.GetImmediateChildren(directoryPath, true)) {
                if (SkipBainDirectory(childDirectory)) continue;
                if (IsDataDirectory(archive, childDirectory)) {
                    validDirectories++;
                }
                else {
                    invalidDirectories++;
                }
            }

            return validDirectories > 1;
        }

        public static string GetBasePath(ArchiveFile archive) {
            // check if top level directory is a BAIN installer
            if (IsBainDirectory(archive, "")) {
                return "";
            }

            // else try to find a top level directory which is a BAIN installer
            return archive.GetLevelDirectories(1).Find(x => IsBainDirectory(archive, x));
        }

    }
}