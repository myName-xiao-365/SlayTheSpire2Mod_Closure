using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using ClosureMod.Characters;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using STS2RitsuLib.Localization;

namespace ClosureMod.Patches;

internal static class ClosureAncientDialoguePatch
{
    private static readonly ConditionalWeakTable<AncientDialogueSet, string> AncientSets = new();

    [HarmonyPatch(typeof(Neow), "DefineDialogues")]
    private static class TrackNeowDialogueSet
    {
        private static void Postfix(AncientDialogueSet __result)
        {
            AncientSets.GetValue(__result, _ => "NEOW");
        }
    }

    [HarmonyPatch(typeof(Orobas), "DefineDialogues")]
    private static class TrackOrobasDialogueSet
    {
        private static void Postfix(AncientDialogueSet __result)
        {
            AncientSets.GetValue(__result, _ => "OROBAS");
        }
    }

    [HarmonyPatch(typeof(Pael), "DefineDialogues")]
    private static class TrackPaelDialogueSet
    {
        private static void Postfix(AncientDialogueSet __result)
        {
            AncientSets.GetValue(__result, _ => "PAEL");
        }
    }

    [HarmonyPatch(typeof(Tezcatara), "DefineDialogues")]
    private static class TrackTezcataraDialogueSet
    {
        private static void Postfix(AncientDialogueSet __result)
        {
            AncientSets.GetValue(__result, _ => "TEZCATARA");
        }
    }

    [HarmonyPatch(typeof(Darv), "DefineDialogues")]
    private static class TrackDarvDialogueSet
    {
        private static void Postfix(AncientDialogueSet __result)
        {
            AncientSets.GetValue(__result, _ => "DARV");
        }
    }

    [HarmonyPatch(typeof(Nonupeipe), "DefineDialogues")]
    private static class TrackNonupeipeDialogueSet
    {
        private static void Postfix(AncientDialogueSet __result)
        {
            AncientSets.GetValue(__result, _ => "NONUPEIPE");
        }
    }

    [HarmonyPatch(typeof(Tanx), "DefineDialogues")]
    private static class TrackTanxDialogueSet
    {
        private static void Postfix(AncientDialogueSet __result)
        {
            AncientSets.GetValue(__result, _ => "TANX");
        }
    }

    [HarmonyPatch(typeof(TheArchitect), "DefineDialogues")]
    private static class TrackArchitectDialogueSet
    {
        private static void Postfix(AncientDialogueSet __result)
        {
            AncientSets.GetValue(__result, _ => "THE_ARCHITECT");
        }
    }

    [HarmonyPatch(typeof(AncientDialogueSet), nameof(AncientDialogueSet.PopulateLocKeys))]
    private static class PopulateClosureDialogue
    {
        private static void Postfix(AncientDialogueSet __instance, string __0)
        {
            if (!AncientSets.TryGetValue(__instance, out string? ancientEntry) || __0 != ancientEntry)
            {
                return;
            }

            ClosureModCharacter? closure = ModelDb.AllCharacters.OfType<ClosureModCharacter>().FirstOrDefault();
            if (closure is null)
            {
                Entry.Logger.Warn($"Could not find Closure character for {ancientEntry} dialogue.");
                return;
            }

            List<AncientDialogue> dialogues = AncientDialogueLocalization.GetDialoguesForCharacter(ancientEntry, closure);
            if (dialogues.Count < 3)
            {
                Entry.Logger.Warn($"Expected 3 Closure {ancientEntry} dialogues, found {dialogues.Count}.");
                return;
            }

            string characterEntry = closure.Id.Entry;
            for (int i = 0; i < 3; i++)
            {
                dialogues[i].PopulateLines(ancientEntry, characterEntry, i);
            }

            __instance.CharacterDialogues[characterEntry] = dialogues.Take(3).ToArray();
            Entry.Logger.Info($"Registered Closure {ancientEntry} dialogues.");
        }
    }

    [HarmonyPatch(typeof(AncientDialogueSet), nameof(AncientDialogueSet.GetValidDialogues))]
    private static class SelectClosureDialogue
    {
        private static bool Prefix(AncientDialogueSet __instance, ModelId __0,
            ref IEnumerable<AncientDialogue> __result)
        {
            if (!AncientSets.TryGetValue(__instance, out _))
            {
                return true;
            }

            ClosureModCharacter? closure = ModelDb.AllCharacters.OfType<ClosureModCharacter>().FirstOrDefault();
            if (closure is null || __0.Entry != closure.Id.Entry ||
                !__instance.CharacterDialogues.TryGetValue(__0.Entry, out IReadOnlyList<AncientDialogue>? dialogues) ||
                dialogues.Count < 3)
            {
                return true;
            }

            __result = dialogues;
            return false;
        }
    }
}
