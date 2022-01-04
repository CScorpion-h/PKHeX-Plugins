﻿using System;
using System.Collections.Generic;

namespace PKHeX.Core.Injection
{
    public static class LiveHeXBlocks
    {
        // WinForms function (<value>) to invoke for editing saveblocks of type <key>
        public static readonly Dictionary<Type, string> BlockFormMapping = new()
        {
            { typeof(MyItem), "B_OpenItemPouch_Click" },
            { typeof(RaidSpawnList8), "B_OpenRaids_Click" },
            { typeof(UndergroundItemList8b), "B_OpenUGSEditor_Click" },
        };

        /// <summary>
        /// Dictionary that does a mapping from livehexversion to a dictionary of SCBlock Keys in the savefile (not ram) to the WinForms function to invoke
        /// </summary>
        public static readonly Dictionary<LiveHeXVersion, Dictionary<uint, string>> SCBlockFormMapping = new()
        {
            {
                LiveHeXVersion.SWSH_Rigel2,
                new Dictionary<uint, string>
                {
                    { 0x4716c404, "B_OpenPokedex_Click" }, // KZukan
                    { 0x3F936BA9, "B_OpenPokedex_Click" }, // KZukanR1
                    { 0x3C9366F0, "B_OpenPokedex_Click" }, // KZukanR2
                }
            },
        };

        /// <summary>
        /// Check if a special form needs to be open to handle the block
        /// </summary>
        /// <param name="sb">saveblock</param>
        /// <param name="lv">LiveHeX version being edited</param>
        /// <param name="value">string value of the form to open</param>
        /// <returns>Boolean indicating if a special form needs to be opened</returns>
        public static bool IsSpecialBlock(this object sb, LiveHeXVersion lv, out string value)
        {
            value = string.Empty;
            if (sb is SCBlock scb)
            {
                // only keys exist here
                if (!SCBlockFormMapping.TryGetValue(lv, out var forms))
                    return false;
                return forms.TryGetValue(scb.Key, out value);
            }
            foreach (Type k in BlockFormMapping.Keys)
            {
                if (!k.IsInstanceOfType(sb))
                    continue;
                value = BlockFormMapping[k];
                return true;
            }
            return false;
        }
    }
}
