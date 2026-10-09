using System.Runtime.CompilerServices;
using ArkBase.Audio;
using ArkBase.Cards;
using ClosureMod.Characters;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Audio;

namespace ClosureMod.Patches;

internal static class ClosureVoicePatch
{
    private const string VoiceRoot = $"{Entry.ResPath}/voice/";
    private static readonly string[] CombatLines =
        ["Attack1.wav", "Attack2.wav", "Skill.wav", "Power.wav"];
    private static readonly ConditionalWeakTable<RunState, object> NewRuns = new();
    private static readonly ConditionalWeakTable<CombatState, object> DeploymentsPlayed = new();
    private static readonly ConditionalWeakTable<CombatManager, object> SubscribedCombatManagers = new();
    private static readonly ConditionalWeakTable<RunState, VictoryVoiceState> VictoryStates = new();
    private static readonly ConditionalWeakTable<Player, TurnVoiceState> TurnStates = new();
    private static readonly HashSet<string> FailedClips = new();

    [HarmonyPatch(typeof(RunManager), "InitializeNewRun")]
    private static class MarkNewRun
    {
        private static void Postfix(RunManager __instance)
        {
            if (__instance.State is { } run)
            {
                NewRuns.GetValue(run, _ => new object());
            }
        }
    }

    [HarmonyPatch(typeof(RunManager), "Launch")]
    private static class PlayRunStart
    {
        private static void Postfix(RunState __result)
        {
            if (NewRuns.Remove(__result) && __result.Players.Any(IsLocalClosure))
            {
                Play("RunStart.wav");
            }
        }
    }

    [HarmonyPatch(typeof(CombatManager), nameof(CombatManager.SetUpCombat))]
    private static class SubscribeToCombatWon
    {
        private static void Postfix(CombatManager __instance)
        {
            if (SubscribedCombatManagers.TryGetValue(__instance, out _))
            {
                return;
            }

            SubscribedCombatManagers.Add(__instance, new object());
            __instance.CombatWon += OnCombatWon;
        }
    }

    [HarmonyPatch(typeof(CombatManager), nameof(CombatManager.AfterCombatRoomLoaded))]
    private static class PlayDeployment
    {
        private static void Postfix()
        {
            if (RunManager.Instance.State?.CurrentRoom is CombatRoom room &&
                room.CombatState.Players.Any(IsLocalClosure) &&
                !DeploymentsPlayed.TryGetValue(room.CombatState, out _))
            {
                DeploymentsPlayed.Add(room.CombatState, new object());
                ClosureIdleVoiceController.EnsureAttached();
                Play(Random.Shared.Next(2) == 0 ? "Deploy1.wav" : "Deploy2.wav");
            }
        }
    }

    [HarmonyPatch(typeof(CardModel), nameof(CardModel.OnPlayWrapper))]
    private static class PlayFirstCardOfTurn
    {
        private static void Prefix(CardModel __instance)
        {
            Player? player = __instance.Owner;
            if (player is null || !IsLocalClosure(player) ||
                player.PlayerCombatState is not { TurnNumber: > 0 } combatState ||
                __instance.CombatState is not { } combat ||
                __instance is SupportCardTemplate)
            {
                return;
            }

            TurnVoiceState voiceState = TurnStates.GetValue(player, _ => new TurnVoiceState());
            if (!ReferenceEquals(voiceState.Combat, combat))
            {
                voiceState.LastLineIndex = -1;
            }

            if (!ReferenceEquals(voiceState.Combat, combat) || voiceState.TurnNumber != combatState.TurnNumber)
            {
                voiceState.Combat = combat;
                voiceState.TurnNumber = combatState.TurnNumber;
                voiceState.Handled = false;
            }

            if (!voiceState.Handled)
            {
                voiceState.Handled = true;
                if (VoiceAudioPlayer.IsSupportVoicePlaying)
                {
                    return;
                }

                int lineIndex = Random.Shared.Next(CombatLines.Length - (voiceState.LastLineIndex >= 0 ? 1 : 0));
                if (voiceState.LastLineIndex >= 0 && lineIndex >= voiceState.LastLineIndex)
                {
                    lineIndex++;
                }

                voiceState.LastLineIndex = lineIndex;
                Play(CombatLines[lineIndex]);
            }
        }
    }

    [HarmonyPatch(typeof(CombatManager), nameof(CombatManager.HandlePlayerDeath))]
    private static class PlayDefeat
    {
        private static void Prefix(Player __0)
        {
            if (IsLocalClosure(__0))
            {
                Play("Defeat.wav");
            }
        }
    }

    private static void OnCombatWon(CombatRoom room)
    {
        if (!room.CombatState.Players.Any(IsLocalClosure))
        {
            return;
        }

        if (room.RoomType == RoomType.Boss)
        {
            if (!VoiceAudioPlayer.IsSupportVoicePlaying)
            {
                Play("VictoryMajor.wav");
            }

            return;
        }

        if (RunManager.Instance.State is not { } run)
        {
            return;
        }

        VictoryVoiceState state = VictoryStates.GetValue(run, _ => new VictoryVoiceState());
        bool selected = Random.Shared.Next(100) < state.ChancePercent;
        state.ChancePercent = selected ? 0 : Math.Min(100, state.ChancePercent + 20);
        if (selected && !VoiceAudioPlayer.IsSupportVoicePlaying)
        {
            Play(room.RoomType == RoomType.Elite ? "VictoryMajor.wav" : "Victory.wav");
        }
    }

    internal static bool IsLocalClosure(Player player) =>
        player.Character is ClosureModCharacter &&
        LocalContext.NetId is ulong localId && player.NetId == localId;

    internal static void Play(string fileName)
    {
        try
        {
            AudioPlayResult result = VoiceAudioPlayer.PlayResource(VoiceRoot + fileName);
            if (!result.Succeeded && FailedClips.Add(fileName))
            {
                Entry.Logger.Warn($"Could not play Closure voice {fileName}: {result.Status} {result.Message}");
            }
        }
        catch (Exception ex)
        {
            if (FailedClips.Add(fileName))
            {
                Entry.Logger.Warn($"Could not play Closure voice {fileName}: {ex}");
            }
        }
    }

    private sealed class TurnVoiceState
    {
        public object? Combat;
        public int TurnNumber = -1;
        public bool Handled;
        public int LastLineIndex = -1;
    }

    private sealed class VictoryVoiceState
    {
        public int ChancePercent = 100;
    }
}
