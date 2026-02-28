using Avalonia.Controls.Shapes;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;

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

        public byte[] thumbnailArray;

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

        public bool Save(string fileName)
        {
            try
            {
                FileStream output = File.Open(fileName, FileMode.OpenOrCreate, FileAccess.Write);
                BinaryWriter binaryWriter = new BinaryWriter(output);
                binaryWriter.Write(ver);
                binaryWriter.Write(map);
                binaryWriter.Write(alias);
                binaryWriter.Write(developer);
                binaryWriter.Write(regDate.Year);
                binaryWriter.Write((sbyte)regDate.Month);
                binaryWriter.Write((sbyte)regDate.Day);
                binaryWriter.Write((sbyte)regDate.Hour);
                binaryWriter.Write((sbyte)regDate.Minute);
                binaryWriter.Write((sbyte)regDate.Second);
                binaryWriter.Write(modeMask);
                binaryWriter.Write(clanMatchable);
                binaryWriter.Write(officialMap);
                if (null == thumbnail)
                {
                    binaryWriter.Write(0);
                }
                else
                {
                    binaryWriter.Write(thumbnailArray.Length);
                    for (int i = 0; i < thumbnailArray.Length; i++)
                    {
                        binaryWriter.Write(thumbnailArray[i]);
                    }
                }
                binaryWriter.Close();
            }
            catch (Exception ex)
            {
                Global.PrintLine(ex.Message.ToString());
                return false;
            IL_0144:;
            }
            return true;
        }
    }
}
