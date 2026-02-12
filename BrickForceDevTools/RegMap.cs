using System;
using System.Collections.ObjectModel;
using System.Drawing;

namespace BrickForceDevTools
{
    public class RegMap
    {
        public const byte TAG_ABUSE = 16;

        public int latestFileVer = 3;

        public int ver = 3;

        public int map { get; set; }

        public string developer { get; set; }

        public string alias { get; set; }

        public DateTime regDate;

        public ushort modeMask;

        public int release;

        public int latestRelease;

        public int Rank;

        public int RankChg;

        public byte tagMask;

        //public byte[] thumbnailArray;

        public Avalonia.Media.Imaging.Bitmap thumbnail { get; set; }

        public bool clanMatchable;

        public bool officialMap;

        public bool blocked;

        public int likes;

        public int disLikes;

        public int downloadCount;

        public int downloadFee;

        public bool isSelected {  get; set; }

        public Geometry geometry { get; set; }
    }
}
