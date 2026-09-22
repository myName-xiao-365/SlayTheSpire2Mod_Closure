using ClosureMod.Characters;
using ClosureMod.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ClosureMod.Events;

[RegisterSharedEvent]
public sealed class RhodesIslandWorkshop : ModEventTemplate
{
    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath: $"{Entry.ResPath}/images/events/RhodesIslandWorkshopPortrait.png");

    public override LocString InitialDescription => PageDescription("INITIAL");

    public override bool IsAllowed(IRunState runState)
    {
        return runState is RunState run &&
            run.Players.Any(player => player.Character is ClosureModCharacter) &&
            IsFirstUnknownRoom(run);
    }

    public static bool IsFirstUnknownRoom(RunState run)
    {
        return !run.VisitedEventIds.Contains(ModelDb.Event<RhodesIslandWorkshop>().Id) &&
            !run.MapPointHistory.SelectMany(act => act)
                .Any(point => point.MapPointType == MapPointType.Unknown && point.Rooms.Count > 0 &&
                    !ReferenceEquals(point, run.CurrentMapPointHistoryEntry));
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return
        [
            Choice("INITIAL", "ENTER", Owner?.Gold >= 42 ? async () =>
            {
                await PlayerCmd.LoseGold(42, Owner!, MegaCrit.Sts2.Core.Entities.Gold.GoldLossType.Spent);
                Show("WORKSHOP", Choice("WORKSHOP", "LEFT", () => ShowAsync("COLLECTION", CollectionOptions())),
                    Choice("WORKSHOP", "RIGHT", () => ShowAsync("FRIENDS", FriendOptions())));
            } : null),
            Choice("INITIAL", "LEAVE", async () =>
            {
                await PlayerCmd.GainGold(42, Owner!);
                SetEventFinished(PageDescription("LEAVE"));
            })
        ];
    }

    private EventOption[] CollectionOptions() =>
    [
        TakeOption<GaulCheque>("COLLECTION", "CHEQUE"),
        TakeOption<GiftCard>("COLLECTION", "GIFT_CARD")
    ];

    private EventOption[] FriendOptions() =>
    [
        TakeOption<StructuralPrinciple>("FRIENDS", "STRUCTURE"),
        TakeOption<SniperScope>("FRIENDS", "SCOPE")
    ];

    private EventOption TakeOption<T>(string page, string option) where T : RelicModel
    {
        return Choice(page, option, async () =>
        {
            await RelicCmd.Obtain<T>(Owner!);
            SetEventFinished(PageDescription("ENDING"));
        });
    }

    private Task ShowAsync(string page, params EventOption[] options)
    {
        Show(page, options);
        return Task.CompletedTask;
    }

    private void Show(string page, params EventOption[] options)
    {
        SetEventState(PageDescription(page), options);
    }

    private EventOption Choice(string page, string name, Func<Task>? action)
    {
        return new EventOption(this, action!, ModOptionKey(page, name), []);
    }
}
