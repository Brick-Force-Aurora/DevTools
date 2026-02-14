using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BrickForceDevTools.Patch
{
    public static class PatchBuilder
    {
        public sealed record DiffEntry(
            string BaseName,
            string RegMapPath,
            string GeometryPath,
            bool HasGeometry,
            string MapName,
            string Creator
        );

        /// <summary>
        /// Compute B - A by base filename (without extension).
        /// </summary>
        public static List<DiffEntry> ComputeDiff(string folderA, string folderB, bool skipMissingGeometry, Action<string>? log = null)
        {
            // Index A
            var aKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var regA in Directory.EnumerateFiles(folderA, "*.regmap"))
            {
                var baseName = Path.GetFileNameWithoutExtension(regA);
                var geoA = Path.Combine(folderA, baseName + ".geometry");

                if (skipMissingGeometry && !File.Exists(geoA))
                    continue;

                aKeys.Add(baseName);
            }

            // Scan B
            var diffs = new List<DiffEntry>();

            foreach (var regB in Directory.EnumerateFiles(folderB, "*.regmap"))
            {
                var baseName = Path.GetFileNameWithoutExtension(regB);
                if (aKeys.Contains(baseName))
                    continue;

                var geoB = Path.Combine(folderB, baseName + ".geometry");
                var hasGeo = File.Exists(geoB);

                if (skipMissingGeometry && !hasGeo)
                {
                    log?.Invoke($"[Patch] Missing Geometry (skipped): {Path.GetFileName(geoB)}");
                    continue;
                }

                // Load map metadata from regmap (adjust fields if needed)
                var regMap = RegMapManager.Load(regB);

                // These names come from your earlier regmap loader snippet: alias + developer
                string mapName = regMap.alias ?? baseName;
                string creator = regMap.developer ?? "Unknown";

                diffs.Add(new DiffEntry(baseName, regB, geoB, hasGeo, mapName, creator));
            }

            return diffs
                .OrderBy(d => d.MapName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(d => d.Creator, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// Copy files and generate changelog.txt and info.
        /// </summary>
        public static void BuildPatchOutput(IEnumerable<DiffEntry> diffs, string outputFolder, Action<string>? log = null)
        {
            Directory.CreateDirectory(outputFolder);

            var changelogLines = new List<string>();
            var infoLines = new List<string>();
            if (Global.IncludeAssemblyLineInPatchInfo)
            {
                infoLines.Add("Assembly-CSharp.dll=BrickForce_Data/Managed/");
            }

            int copiedMaps = 0;
            int copiedFiles = 0;

            foreach (var d in diffs)
            {
                var outReg = Path.Combine(outputFolder, d.BaseName + ".regmap");
                File.Copy(d.RegMapPath, outReg, overwrite: true);
                copiedFiles++;

                changelogLines.Add($"{d.MapName} by {d.Creator}");
                infoLines.Add($"{Path.GetFileName(outReg)}=BrickForce_Data/Resources/Cache/");

                if (d.HasGeometry)
                {
                    var outGeo = Path.Combine(outputFolder, d.BaseName + ".geometry");
                    File.Copy(d.GeometryPath, outGeo, overwrite: true);
                    copiedFiles++;

                    infoLines.Add($"{Path.GetFileName(outGeo)}=BrickForce_Data/Resources/Cache/");
                }

                copiedMaps++;
            }

            File.WriteAllLines(Path.Combine(outputFolder, "changelog.txt"), changelogLines);
            File.WriteAllLines(Path.Combine(outputFolder, "info"), infoLines);

            log?.Invoke($"[Patch] Done. Maps: {copiedMaps}, Files: {copiedFiles}, Output: {outputFolder}");
        }
    }
}
