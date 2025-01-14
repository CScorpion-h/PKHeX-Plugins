using System.IO;
using System.Linq;

namespace PKHeX.Core.AutoMod
{
    /// <summary>
    /// Logic to load <see cref="ITrainerInfo"/> from a saved text file.
    /// </summary>
    public static class TrainerSettings
    {
        private static readonly TrainerDatabase Database = new();
        private static readonly string TrainerPath = Path.Combine(Directory.GetCurrentDirectory(), "trainers");
        private static readonly SimpleTrainerInfo DefaultFallback8 = new(GameVersion.SW);
        private static readonly SimpleTrainerInfo DefaultFallback7 = new(GameVersion.UM);
        private static readonly GameVersion[] FringeVersions = { GameVersion.GG, GameVersion.BDSP, GameVersion.PLA };

        public static ITrainerInfo DefaultFallback(int gen = 8, LanguageID? lang = null)
        {
            var fallback = gen > 7 ? DefaultFallback8 : DefaultFallback7;
            if (lang == null)
                return fallback;
            return new SimpleTrainerInfo((GameVersion)fallback.Game) { Language = (int)lang };
        }

        public static ITrainerInfo DefaultFallback(GameVersion ver, LanguageID? lang = null)
        {
            if (!ver.IsValidSavedVersion())
                ver = GameUtil.GameVersions.First(z => ver.Contains(z));
            return lang == null ? new SimpleTrainerInfo(ver) : new SimpleTrainerInfo(ver) { Language = (int)lang };
        }

        static TrainerSettings() => LoadTrainerDatabaseFromPath(TrainerPath);

        /// <summary>
        /// Loads possible <see cref="PKM"/> data from the path, and registers them to the <see cref="Database"/>.
        /// </summary>
        /// <param name="path"></param>
        public static void LoadTrainerDatabaseFromPath(string path)
        {
            if (!Directory.Exists(path))
                return;
            var files = Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories);
            foreach (var f in files)
            {
                var len = new FileInfo(f).Length;
                if (!PKX.IsPKM(len))
                    return;
                var data = File.ReadAllBytes(f);
                var pk = PKMConverter.GetPKMfromBytes(data);
                if (pk != null)
                    Database.Register(new PokeTrainerDetails(pk.Clone()));
            }
        }

        /// <summary>
        /// Gets a possible Trainer Data for the requested <see cref="generation"/>.
        /// </summary>
        /// <param name="generation">Generation of origin requested.</param>
        /// <param name="fallback">Fallback trainer data if no new parent is found.</param>
        /// <param name="lang">Language to request for</param>
        /// <returns>Parent trainer data that originates from the <see cref="PKM.Version"/>. If none found, will return the <see cref="fallback"/>.</returns>
        public static ITrainerInfo GetSavedTrainerData(int generation, GameVersion ver = GameVersion.Any, ITrainerInfo? fallback = null, LanguageID? lang = null)
        {
            ITrainerInfo? trainer = null;
            var special_version = FringeVersions.Any(z => z.Contains(ver));
            if (!special_version)
                trainer = Database.GetTrainerFromGen(generation, lang);
            if (trainer != null)
                return trainer;

            if (fallback == null)
                return special_version ? DefaultFallback(ver, lang) : DefaultFallback(generation, lang);
            if (lang == null)
                return fallback;
            if (lang == (LanguageID)fallback.Language)
                return fallback;
            return special_version ? DefaultFallback(ver, lang) : DefaultFallback(generation, lang);
        }

        /// <summary>
        /// Gets a possible Trainer Data for the requested <see cref="version"/>.
        /// </summary>
        /// <param name="version">Version of origin requested.</param>
        /// <param name="gen">Generation of origin requested.</param>
        /// <param name="fallback">Fallback trainer data if no new parent is found.</param>
        /// <param name="lang">Language to request for</param>
        /// <returns>Parent trainer data that originates from the <see cref="PKM.Version"/>. If none found, will return the <see cref="fallback"/>.</returns>
        public static ITrainerInfo GetSavedTrainerData(GameVersion version, int gen, ITrainerInfo? fallback = null, LanguageID? lang = null)
        {
            var byVer = Database.GetTrainer(version, lang);
            if (byVer != null)
                return byVer;
            return GetSavedTrainerData(gen, version, fallback, lang);
        }

        /// <summary>
        /// Gets a possible Trainer Data for the provided <see cref="pk"/>.
        /// </summary>
        /// <param name="pk">Pok�mon that will receive the trainer details.</param>
        /// <param name="template_save">Fallback trainer data if no new parent is found.</param>
        /// <param name="lang">Language to request for</param>
        /// <returns>Parent trainer data that originates from the <see cref="PKM.Version"/>. If none found, will return the <see cref="fallback"/>.</returns>
        public static ITrainerInfo GetSavedTrainerData(PKM pk, ITrainerInfo template_save, LanguageID? lang = null)
        {
            int origin = pk.Generation;
            int format = pk.Format;
            if (format != origin)
                return GetSavedTrainerData(format, (GameVersion)template_save.Game, fallback:template_save, lang:lang);
            return GetSavedTrainerData((GameVersion)pk.Version, origin, template_save, lang);
        }

        /// <summary>
        /// Registers the Trainer Data to the <see cref="Database"/>.
        /// </summary>
        /// <param name="tr">Trainer Data</param>
        public static void Register(ITrainerInfo tr) => Database.Register(tr);

        /// <summary>
        /// Clears the Trainer Data in the <see cref="Database"/>.
        /// </summary>
        public static void Clear() => Database.Clear();
    }
}
