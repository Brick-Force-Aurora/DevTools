using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.Collections.Generic;

namespace BrickForceDevTools
{
    public static class ModeIconDecoder
    {
        // IMPORTANT: Replace these with your real mask values from Room.MODE_MASK
        private static readonly (ushort bit, string assetPath)[] Map =
        {
            // example names/paths – adjust to your assets
            ((ushort)MODE_MASK.TEAM_MATCH_MASK,         "avares://BrickForceDevTools/Assets/Modes/icon_teamMode.png"),
            ((ushort)MODE_MASK.INDIVIDUAL_MATCH_MASK,   "avares://BrickForceDevTools/Assets/Modes/icon_survivalMode.png"),
            ((ushort)MODE_MASK.CAPTURE_THE_FALG_MATCH,  "avares://BrickForceDevTools/Assets/Modes/icon_ctfMode.png"),
            ((ushort)MODE_MASK.EXPLOSION_MATCH,         "avares://BrickForceDevTools/Assets/Modes/icon_blastMode.png"),
            ((ushort)MODE_MASK.MISSION_MASK,            "avares://BrickForceDevTools/Assets/Modes/icon_defenceMode.png"),
            ((ushort)MODE_MASK.BND_MASK,                "avares://BrickForceDevTools/Assets/Modes/icon_BND.png"),
            ((ushort)MODE_MASK.BUNGEE_MASK,             "avares://BrickForceDevTools/Assets/Modes/icon_bungeeMode.png"),
            ((ushort)MODE_MASK.ESCAPE_MASK,             "avares://BrickForceDevTools/Assets/Modes/icon_runMode.png"),
            ((ushort)MODE_MASK.ZOMBIE_MASK,             "avares://BrickForceDevTools/Assets/Modes/icon_zombieMode.png"),
        };

        // cache loaded bitmaps so we don’t reload files repeatedly
        private static readonly Dictionary<string, Bitmap> Cache = new();

        public static List<Bitmap> Decode(ushort modeMask)
        {
            var list = new List<Bitmap>(4);

            foreach (var (bit, asset) in Map)
            {
                if ((modeMask & bit) == 0)
                    continue;

                list.Add(GetBitmap(asset));
            }

            return list;
        }

        public static Bitmap GetBitmap(string assetUri)
        {
            if (Cache.TryGetValue(assetUri, out var bmp))
                return bmp;

            using var stream = AssetLoader.Open(new Uri(assetUri));
            bmp = new Bitmap(stream);
            Cache[assetUri] = bmp;
            return bmp;
        }
    }
}
