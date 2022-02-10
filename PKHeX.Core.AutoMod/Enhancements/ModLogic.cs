﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace PKHeX.Core.AutoMod
{
    /// <summary>
    /// Miscellaneous enhancement methods
    /// </summary>
    public static class ModLogic
    {
        // Living Dex Settings
        public static bool IncludeForms { get; set; }
        public static bool SetShiny { get; set; }
        public static bool SetAlpha { get; set; }

        /// <summary>
        /// Exports the <see cref="SaveFile.CurrentBox"/> to <see cref="ShowdownSet"/> as a single string.
        /// </summary>
        /// <param name="provider">Save File to export from</param>
        /// <returns>Concatenated string of all sets in the current box.</returns>
        public static string GetRegenSetsFromBoxCurrent(this ISaveFileProvider provider) => GetRegenSetsFromBox(provider.SAV, provider.CurrentBox);

        /// <summary>
        /// Exports the <see cref="box"/> to <see cref="ShowdownSet"/> as a single string.
        /// </summary>
        /// <param name="sav">Save File to export from</param>
        /// <param name="box">Box to export from</param>
        /// <returns>Concatenated string of all sets in the specified box.</returns>
        public static string GetRegenSetsFromBox(this SaveFile sav, int box)
        {
            var data = sav.GetBoxData(box);
            var sep = Environment.NewLine + Environment.NewLine;
            return data.GetRegenSets(sep);
        }

        /// <summary>
        /// Gets a living dex (one per species, not every form)
        /// </summary>
        /// <param name="sav">Save File to receive the generated <see cref="PKM"/>.</param>
        /// <param name="attempts"></param>
        /// <returns>Consumable list of newly generated <see cref="PKM"/> data.</returns>
        public static IEnumerable<PKM> GenerateLivingDex(this SaveFile sav, out int attempts)
        {
            var species = Enumerable.Range(1, sav.MaxSpeciesID);
            species = sav switch
            {
                SAV7b => species.Where(z => z is <= 151 or 808 or 809),
                SAV8 => species.Where(z => ((PersonalInfoSWSH)PersonalTable.SWSH.GetFormEntry(z, 0)).IsPresentInGame || SimpleEdits.Zukan8Additions.Contains(z)),
                SAV8BS => species.Where(z => ((PersonalInfoBDSP)PersonalTable.BDSP.GetFormEntry(z, 0)).IsPresentInGame),
                _ => species,
            };
            return sav.GenerateLivingDex(species, includeforms: IncludeForms, shiny: SetShiny, alpha: SetAlpha, out attempts);
        }

        /// <summary>
        /// Gets a living dex (one per species, not every form)
        /// </summary>
        /// <param name="sav">Save File to receive the generated <see cref="PKM"/>.</param>
        /// <param name="speciesIDs">Species IDs to generate</param>
        /// <returns>Consumable list of newly generated <see cref="PKM"/> data.</returns>
        public static IEnumerable<PKM> GenerateLivingDex(this SaveFile sav, params int[] speciesIDs) =>
            sav.GenerateLivingDex(speciesIDs, includeforms: IncludeForms, shiny: SetShiny, alpha: SetAlpha, out _);

        /// <summary>
        /// Gets a living dex (one per species, not every form)
        /// </summary>
        /// <param name="sav">Save File to receive the generated <see cref="PKM"/>.</param>
        /// <param name="speciesIDs">Species IDs to generate</param>
        /// <param name="includeforms">Include all forms in the resulting list of data</param>
        /// <param name="shiny"></param>
        /// <param name="attempts"></param>
        /// <returns>Consumable list of newly generated <see cref="PKM"/> data.</returns>
        public static IEnumerable<PKM> GenerateLivingDex(this SaveFile sav, IEnumerable<int> speciesIDs, bool includeforms, bool shiny, bool alpha, out int attempts)
        {
            attempts = 0;
            var tr = APILegality.UseTrainerData ? TrainerSettings.GetSavedTrainerData(sav.Version, sav.Generation, fallback: sav, lang: (LanguageID)sav.Language) : sav;
            var pt = sav.Personal;
            List<PKM> pklist = new();
            foreach (var id in speciesIDs)
            {
                if (!includeforms)
                {
                    AddPKM(sav, tr, pklist, id, null, shiny, alpha, ref attempts);
                    continue;
                }
                var num_forms = pt[id].FormCount;
                for (int i = 0; i < num_forms; i++)
                {
                    if (sav is SAV8SWSH && !((PersonalInfoSWSH)pt.GetFormEntry(id, i)).IsPresentInGame)
                        continue;
                    if (sav is SAV8LA && !((PersonalInfoLA)pt.GetFormEntry(id, i)).IsPresentInGame)
                        continue;
                    AddPKM(sav, tr, pklist, id, i, shiny, alpha, ref attempts);
                }
            }
            return pklist;
        }

        private static void AddPKM(SaveFile sav, ITrainerInfo tr, List<PKM> pklist, int species, int? form, bool shiny, bool alpha, ref int attempt)
        {
            if (tr.GetRandomEncounter(species, form, shiny, alpha, ref attempt, out var pk) && pk != null)
            {
                pk.Heal();
                pklist.Add(pk);
            }
            else if (sav is SAV2 && GetRandomEncounter(new SAV1(GameVersion.Y) { Language = tr.Language, OT = tr.OT, TID = tr.TID }, species, 0, shiny, false, ref attempt, out var pkm) && pkm is PK1 pk1)
            {
                pklist.Add(pk1.ConvertToPK2());
            }
        }

        /// <summary>
        /// Gets a legal <see cref="PKM"/> from a random in-game encounter's data.
        /// </summary>
        /// <param name="sav">Save File to receive the generated <see cref="PKM"/>.</param>
        /// <param name="species">Species ID to generate</param>
        /// <param name="form">Form to generate; if left null, picks first encounter</param>
        /// <param name="shiny"></param>
        /// <param name="attempt"></param>
        /// <param name="pk">Result legal pkm</param>
        /// <returns>True if a valid result was generated, false if the result should be ignored.</returns>
        public static bool GetRandomEncounter(this SaveFile sav, int species, int? form, bool shiny, bool alpha, ref int attempt, out PKM? pk) => ((ITrainerInfo)sav).GetRandomEncounter(species, form, shiny, alpha, ref attempt, out pk);

        /// <summary>
        /// Gets a legal <see cref="PKM"/> from a random in-game encounter's data.
        /// </summary>
        /// <param name="tr">Trainer Data to use in generating the encounter</param>
        /// <param name="species">Species ID to generate</param>
        /// <param name="form">Form to generate; if left null, picks first encounter</param>
        /// <param name="shiny"></param>
        /// <param name="attempt"></param>
        /// <param name="pk">Result legal pkm</param>
        /// <returns>True if a valid result was generated, false if the result should be ignored.</returns>
        public static bool GetRandomEncounter(this ITrainerInfo tr, int species, int? form, bool shiny, bool alpha, ref int attempt, out PKM? pk)
        {
            var blank = PKMConverter.GetBlank(tr.Generation, tr.Game);
            pk = GetRandomEncounter(blank, tr, species, form, shiny, alpha, ref attempt);
            if (pk == null)
                return false;

            pk = PKMConverter.ConvertToType(pk, blank.GetType(), out _);
            return pk != null;
        }

        /// <summary>
        /// Gets a legal <see cref="PKM"/> from a random in-game encounter's data.
        /// </summary>
        /// <param name="blank">Template data that will have its properties modified</param>
        /// <param name="tr">Trainer Data to use in generating the encounter</param>
        /// <param name="species">Species ID to generate</param>
        /// <param name="form">Form to generate; if left null, picks first encounter</param>
        /// <param name="shiny"></param>
        /// <param name="attempt"></param>
        /// <returns>Result legal pkm, null if data should be ignored.</returns>
        private static PKM? GetRandomEncounter(PKM blank, ITrainerInfo tr, int species, int? form, bool shiny, bool alpha, ref int attempt)
        {
            blank.Species = species;
            blank.Gender = blank.GetSaneGender();
            if (species is ((int)Species.Meowstic) or ((int)Species.Indeedee))
            {
                if (form == null)
                    blank.Form = blank.Gender;
                else
                    blank.Gender = (int)form;
            }

            var template = PKMConverter.GetBlank(tr.Generation, (GameVersion)tr.Game);
            if (form != null)
            {
                blank.Form = (int)form;
                var item = SetFormSpecificItem(tr.Game, blank.Species, (int)form);
                if (item != null) blank.HeldItem = (int)item;
                if (blank.Species == (int)Species.Keldeo && blank.Form == 1) blank.Move1 = (int)Move.SecretSword;
            }
            if (form == null)
            {
                var valid = SetAvailableForm(blank, tr);
                if (!valid)
                    return null;
            }
            if (blank.IgnoreForm(tr, blank.Form))
                return null;
            attempt++;
            var ssettext = new ShowdownSet(blank).Text.Split('\r')[0];
            if (shiny && !SimpleEdits.ShinyLockedSpeciesForm.Contains(new Tuple<Species, int>((Species)blank.Species, blank.Form)))
                ssettext += Environment.NewLine + "Shiny: Yes";
            if (template is IAlpha && alpha)
                ssettext += Environment.NewLine + "Alpha: Yes";
            var sset = new ShowdownSet(ssettext);
            var set = new RegenTemplate(sset);
            template.ApplySetDetails(set);
            var success = tr.TryAPIConvert(set, template, out PKM pk);
            if (success == LegalizationResult.Regenerated)
            {
                if (form == null) return pk;
                if (pk.Form == (int)form) return pk;
            }

            // just get a legal pkm and return. Only validate form and not shininess or alpha.
            var legalencs = EncounterMovesetGenerator.GeneratePKMs(blank, tr).Where(z => new LegalityAnalysis(z).Valid);
            var firstenc = GetFirstEncounter(legalencs, form);
            if (firstenc == null)
            {
                attempt--;
                return null;
            }
            var originspecies = firstenc.Species;
            if (originspecies != blank.Species)
            {
                firstenc.Species = blank.Species;
                firstenc.CurrentLevel = 100;
                if (!firstenc.IsNicknamed)
                    firstenc.Nickname = SpeciesName.GetSpeciesNameGeneration(firstenc.Species, firstenc.Language, firstenc.Format);
                firstenc.SetMoves(firstenc.GetMoveSet());
                firstenc.RefreshAbility(firstenc.AbilityNumber >> 1);
            }
            var second = PKMConverter.ConvertToType(firstenc, blank.GetType(), out _);
            if (second == null)
                return null;
            second.HeldItem = blank.HeldItem;
            if (second is IScaledSizeValue sv)
            {
                sv.HeightAbsolute = sv.CalcHeightAbsolute;
                sv.WeightAbsolute = sv.CalcWeightAbsolute;
            }
            if (form == null || second.Form == (int)form)
                return second;
            // force form and check legality as a last ditch effort.
            second.Form = (int)form;
            second.SetSuggestedFormArgument(originspecies);
            if (second is IScaledSizeValue sc)
            {
                sc.HeightAbsolute = sc.CalcHeightAbsolute;
                sc.WeightAbsolute = sc.CalcWeightAbsolute;
            }
            if (new LegalityAnalysis(second).Valid)
                return second;
            return null;
        }

        private static bool SetAvailableForm(this PKM pk, ITrainerInfo tr)
        {
            var species = pk.Species;
            pk.Version = tr.Game;
            var pi = GameData.GetPersonal((GameVersion)tr.Game);
            var formcount = pi.GetFormEntry(species, 0).FormCount;
            if (formcount == 0)
                return false;

            if (!(pk.SWSH || pk.BDSP || pk.LA))
                return true;
            static bool IsPresentInGameSWSH(ISpeciesForm pk) => ((PersonalInfoSWSH)PersonalTable.SWSH.GetFormEntry(pk.Species, pk.Form)).IsPresentInGame;
            static bool IsPresentInGameBDSP(ISpeciesForm pk) => ((PersonalInfoBDSP)PersonalTable.BDSP.GetFormEntry(pk.Species, pk.Form)).IsPresentInGame;
            static bool IsPresentInGameLA(ISpeciesForm pk) => ((PersonalInfoLA)PersonalTable.LA.GetFormEntry(pk.Species, pk.Form)).IsPresentInGame;
            for (int f = 0; f < formcount; f++)
            {
                pk.Form = f;
                if (pk.LA && IsPresentInGameLA(pk)) return true;
                if (pk.BDSP && IsPresentInGameBDSP(pk)) return true;
                if (pk.SWSH && IsPresentInGameSWSH(pk)) return true;
            }
            return false;
        }

        private static bool IgnoreForm(this PKM pk, ITrainerInfo tr, int form)
        {
            var generation = tr.Generation;
            var species = pk.Species;
            switch ((Species)species)
            {
                case Species.Unown when generation == 2 && form >= 26:
                    return true;
                case Species.Scatterbug or Species.Spewpa when form > Vivillon3DS.MaxWildFormID:
                    return true;
                case Species.Floette when form == 5:
                    return true;
                case Species.Shaymin or Species.Furfrou or Species.Hoopa when form != 0 && generation <= 6:
                    return true;
                case Species.Arceus when generation == 4 && form == 9: // ??? form
                    return true;
            }
            if (FormInfo.IsBattleOnlyForm(pk.Species, form, generation))
                return true;
            if (form == 0)
                return false;
            if (generation >= 7 && pk.Generation < 7 && pk.Generation != -1 && (species == 25 || SimpleEdits.AlolanOriginForms.Contains(species)))
                return true;
            return false;
        }

        private static int? SetFormSpecificItem(int game, int species, int form)
        {
            if (game == (int)GameVersion.PLA)
                return null;
            var generation = ((GameVersion)game).GetGeneration();
            return species switch
            {
                (int)Species.Arceus => generation != 4 || form < 9 ? SimpleEdits.GetArceusHeldItemFromForm(form) : SimpleEdits.GetArceusHeldItemFromForm(form - 1),
                (int)Species.Silvally => SimpleEdits.GetSilvallyHeldItemFromForm(form),
                (int)Species.Genesect => SimpleEdits.GetGenesectHeldItemFromForm(form),
                (int)Species.Giratina => form == 1 ? 112 : null, // Griseous Orb
                (int)Species.Zacian => form == 1 ? 1103 : null, // Rusted Sword
                (int)Species.Zamazenta => form == 1 ? 1104 : null, // Rusted Shield
                _ => null
            };
        }

        private static PKM? GetFirstEncounter(IEnumerable<PKM> legalencs, int? form)
        {
            if (form == null)
                return legalencs.FirstOrDefault();

            PKM? result = null;
            foreach (var pk in legalencs)
            {
                if (pk.Form == form)
                    return pk;
                result ??= pk;
            }
            return result;
        }

        /// <summary>
        /// Legalizes all <see cref="PKM"/> in the specified <see cref="box"/>.
        /// </summary>
        /// <param name="sav">Save File to legalize</param>
        /// <param name="box">Box to legalize</param>
        /// <returns>Count of Pokémon that are now legal.</returns>
        public static int LegalizeBox(this SaveFile sav, int box)
        {
            if ((uint)box >= sav.BoxCount)
                return -1;

            var data = sav.GetBoxData(box);
            var ctr = sav.LegalizeAll(data);
            if (ctr > 0)
                sav.SetBoxData(data, box);
            return ctr;
        }

        /// <summary>
        /// Legalizes all <see cref="PKM"/> in all boxes.
        /// </summary>
        /// <param name="sav">Save File to legalize</param>
        /// <returns>Count of Pokémon that are now legal.</returns>
        public static int LegalizeBoxes(this SaveFile sav)
        {
            if (!sav.HasBox)
                return -1;
            var ctr = 0;
            for (int i = 0; i < sav.BoxCount; i++)
            {
                var result = sav.LegalizeBox(i);
                if (result < 0)
                    return result;
                ctr += result;
            }
            return ctr;
        }

        /// <summary>
        /// Legalizes all <see cref="PKM"/> in the provided <see cref="data"/>.
        /// </summary>
        /// <param name="sav">Save File context to legalize with</param>
        /// <param name="data">Data to legalize</param>
        /// <returns>Count of Pokémon that are now legal.</returns>
        public static int LegalizeAll(this SaveFile sav, IList<PKM> data)
        {
            var ctr = 0;
            for (int i = 0; i < data.Count; i++)
            {
                var pk = data[i];
                if (pk.Species <= 0 || new LegalityAnalysis(pk).Valid)
                    continue;

                var result = sav.Legalize(pk);
                result.Heal();
                if (!new LegalityAnalysis(result).Valid)
                    continue; // failed to legalize

                data[i] = result;
                ctr++;
            }

            return ctr;
        }
    }
}
