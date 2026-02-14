using System;

namespace BrickForceDevTools
{
    [Flags]
    public enum MODE_MASK : ushort
    {
        NONE = 0,

        TEAM_MATCH_MASK = 1 << 0,
        INDIVIDUAL_MATCH_MASK = 1 << 1,
        CAPTURE_THE_FALG_MATCH = 1 << 2,
        EXPLOSION_MATCH = 1 << 3,
        MISSION_MASK = 1 << 4,
        BND_MASK = 1 << 5,
        BUNGEE_MASK = 1 << 6,
        ESCAPE_MASK = 1 << 7,
        ZOMBIE_MASK = 1 << 8
    }
}
