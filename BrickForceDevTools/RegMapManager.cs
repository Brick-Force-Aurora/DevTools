using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
namespace BrickForceDevTools
{
    public class RegMapManager
    {
        public static RegMap curRegMap; //Currently selected map

        public static List<RegMap> regMaps = new List<RegMap>(); //Collection of all loaded maps

        public static List<Brick> bricks = new List<Brick>(); //General collection of brick type metadata dumped from game

        //Modified map file parsing logic ported from decompiled game code
        public static RegMap Load(string fileName)
        {
            RegMap regMap = new RegMap();
            string fileName2 = fileName.Replace("regmap", "geometry");

            //Skip file if the corresponding geometry file is missing (recommended usage)
            if (Global.SkipMissingGeometry && !File.Exists(fileName2))
            {
                Global.PrintLine($"Missing Geometry File: {Path.GetFileName(fileName2)}");
                return null;
            }

            //.regmap file parsing
            try
            {
                FileStream input = File.Open(fileName, FileMode.Open, FileAccess.Read);
                BinaryReader binaryReader = new BinaryReader(input);
                regMap.ver = binaryReader.ReadInt32();
                regMap.map = binaryReader.ReadInt32();
                regMap.alias = binaryReader.ReadString();
                regMap.developer = binaryReader.ReadString();
                int year = binaryReader.ReadInt32();
                sbyte month = binaryReader.ReadSByte();
                sbyte day = binaryReader.ReadSByte();
                sbyte hour = binaryReader.ReadSByte();
                sbyte minute = binaryReader.ReadSByte();
                sbyte second = binaryReader.ReadSByte();
                regMap.regDate = new DateTime(year, month, day, hour, minute, second);
                regMap.modeMask = ((regMap.ver > 2) ? binaryReader.ReadUInt16() : binaryReader.ReadByte());
                regMap.clanMatchable = binaryReader.ReadBoolean();
                regMap.officialMap = (regMap.ver >= 2 && binaryReader.ReadBoolean());

                //Create thumbnail from byte array
                int num = binaryReader.ReadInt32();
                if (num <= 0)
                {
                    regMap.thumbnail = null;
                }
                else
                {
                    byte[] array = new byte[num];
                    for (int i = 0; i < num; i++)
                    {
                        array[i] = binaryReader.ReadByte();
                    }

                    using (var memoryStream = new MemoryStream(array))
                    {
                        regMap.thumbnail = new Avalonia.Media.Imaging.Bitmap(memoryStream);
                    }
                }
                binaryReader.Close();
                input.Close();
            }

            catch
            {
            }

            //.geometry file parsing
            if (File.Exists(fileName2))
            {
                try
                {
                    FileStream fileStream = File.Open(fileName2, FileMode.Open, FileAccess.Read);
                    BinaryReader reader = new BinaryReader(fileStream);

                    int num = reader.ReadInt32();
                    regMap.geometry = new Geometry();
                    regMap.geometry.map = reader.ReadInt32();
                    regMap.geometry.skybox = reader.ReadInt32();
                    int num2 = reader.ReadInt32();
                    regMap.geometry.brickCount = num2;
                    int num3 = 99999;
                    int num4 = -99999;
                    int num5 = 99999;
                    int num6 = -99999;
                    int num7 = 99999;
                    int num8 = -99999;
                    for (int i = 0; i < num2; i++)
                    {
                        int seq = reader.ReadInt32();
                        byte template = reader.ReadByte();
                        byte x = reader.ReadByte();
                        byte y = reader.ReadByte();
                        byte z = reader.ReadByte();
                        ushort meshCode = reader.ReadUInt16();
                        byte rot = reader.ReadByte();
                        if (num3 > x)
                        {
                            num3 = x;
                        }
                        if (num5 > y)
                        {
                            num5 = y;
                        }
                        if (num7 > z)
                        {
                            num7 = z;
                        }
                        if (num4 < x)
                        {
                            num4 = x;
                        }
                        if (num6 < y)
                        {
                            num6 = y;
                        }
                        if (num8 < z)
                        {
                            num8 = z;
                        }

                        /*if (template == 136)
                        {
                            //GlobalVars.Instance.vDefenseEnd = new Vector3((float)(int)x, (float)(int)y, (float)(int)z);
                        }
                        if (template == 191)
                        {
                            //BrickManager.Instance.AddDoorTDic(seq, new Vector3((float)(int)x, (float)(int)y, (float)(int)z));
                        }
                        switch (template)
                        {
                            case 163:
                                regMap.geometry.portalReds.Add(new SpawnerDesc(seq, new Vector3(x, y, z), rot));
                                break;
                            case 164:
                                regMap.geometry.portalBlues.Add(new SpawnerDesc(seq, new Vector3(x, y, z), rot));
                                break;
                            case 178:
                                regMap.geometry.portalNeutrals.Add(new SpawnerDesc(seq, new Vector3(x, y, z), rot));
                                break;
                        }
                        if (template == 196)
                        {
                            regMap.geometry.railSpawners.Add(new SpawnerDesc(seq, new Vector3(x, y, z), rot));
                        }*/

                        Brick brick = bricks.Find(t => t.seq == template);
                        BrickInst brickInst = new BrickInst(seq, template, x, y, z, meshCode, rot, brick);
                        regMap.geometry.dic.Add(i, brickInst);

                        if (brickInst == null || brick == null)
                        {
                            Global.PrintLine("Could Not Find Brick " + template);
                        }
                        //Never had this executed so far, might not be the correct behaviour
                        else if (num >= 1 && brick.function == Brick.FUNCTION.SCRIPT)
                        {
                            string alias = reader.ReadString();
                            bool enableOnAwake = reader.ReadBoolean();
                            bool visibleOnAwake = reader.ReadBoolean();
                            string commands = reader.ReadString();
                        }
                    }
                    regMap.geometry.cenX = (num3 + num4) * 0.5f;
                    regMap.geometry.cenZ = (num7 + num8) * 0.5f;
                    regMap.geometry.min.X = num3;
                    regMap.geometry.min.Y = num5;
                    regMap.geometry.min.Z = num7;
                    regMap.geometry.max.X = num4;
                    regMap.geometry.max.Y = num6;
                    regMap.geometry.max.Z = num8;
                    reader.Close();
                    fileStream.Close();
                }
                catch
                {
                }
            }
            //Global.Print("Loaded " + regMap.alias);
            return regMap;
        }
    }
}
