using Harmony;
using RimWorld;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Verse;

namespace TheThirdAge
{
    /// <summary>
    /// Filtered backstories file already added.
    ///   string[] filteredWords = {"world", "planet",
    ///             "universe", "research", "space", "galaxy",
    ///             "genetic", "communications", "gun", "ceti", "tech", "machine",
    ///             "addiction"};
    /// </summary>
public static class BackstoryHandler
    {
        public static void RemoveIncompatibleBackstories(StringBuilder DebugString)
        {
            DebugString.AppendLine("BackstoryDef Removal List");
            //StringBuilder listOfBackstoriesToRemove = new StringBuilder();
            var tempBackstoryKeys = BackstoryDatabase.allBackstories.Keys;
            foreach (string badId in BackstoryHandler.GetIncompatibleBackstories())
            {
                //listOfBackstoriesToRemove.AppendLine(badId);
                foreach (string existingId in tempBackstoryKeys)
                {
                    var properId = BackstoryHandler.RemoveNumbers(existingId);
                    //listOfBackstoriesToRemove.AppendLine(":: " + properId);
                    if (properId == BackstoryHandler.RemoveNumbers(badId))
                    {
                        BackstoryDatabase.allBackstories.Remove(existingId);
                        //listOfBackstoriesToRemove.AppendLine("::::::::::::: ");
                        //listOfBackstoriesToRemove.AppendLine(":: REMOVED :: ");
                        //listOfBackstoriesToRemove.AppendLine(existingId);
                        //listOfBackstoriesToRemove.AppendLine("::::::::::::: ");
                        break;
                    }
                }
            }
            //Log.Message(listOfBackstoriesToRemove.ToString());
            var shuffle = (Dictionary<Pair<BackstorySlot, string>, List<Backstory>>)AccessTools.Field(typeof(BackstoryDatabase), "shuffleableBackstoryList").GetValue(null);
            shuffle.Clear();
        }

        public static void ListIncompatibleBackstories()
        {

            StringBuilder listOfBackstoriesToRemove = new StringBuilder();
            foreach (var bsy in BackstoryDatabase.allBackstories)
            {
                var bs = bsy.Value;
                var bsTitle = bs.title.ToLowerInvariant();
                var bsDesc = bs.baseDesc.ToLowerInvariant();
                string[] filteredWords = {"world", "planet", "vat", "robot", "organ",
                    "universe", "research", "midworld", "space", "galaxy", "star system",
                    "genetic", "communications", "gun", "ceti", "tech", "machine",
                    "addiction", "starship", "pilot"};
                foreach (string subString in filteredWords)
                {
                    if (bsTitle.Contains(subString))
                    {
                        listOfBackstoriesToRemove.AppendLine(bsy.Key);
                        break;
                    }
                    if (bsDesc.Contains(subString))
                    {
                        listOfBackstoriesToRemove.AppendLine(bsy.Key);
                        break;
                    }
                }
            }
            Log.Message(listOfBackstoriesToRemove.ToString());
        }


        public static IEnumerable<string> GetIncompatibleBackstories()
        {
            if (Translator.TryGetTranslatedStringsForFile("Static/IncompatibleBackstories", out List<string> list))
            {
                foreach (string item in list)
                {
                    yield return item;
                }
            }
        }

        public static string GetIdentifier(string s)
        {
            return RemoveNumbers(s);
        }

        public static string RemoveNumbers(string s)
        {
            StringBuilder result = new StringBuilder();
            for (int i = 0; i < s.Length; i++)
            {
                if (char.IsLetter(s[i]))
                {
                    result.Append(s[i]);
                }
            }
            return result.ToString();
        }
    }
}
