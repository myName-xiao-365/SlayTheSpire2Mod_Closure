using ClosureMod.Cards;
using ClosureMod.Characters;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ClosureMod.Relics;

[RegisterRelic(typeof(ClosureModRelicPool))]
public sealed class StrangeButton : ModRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Event;
    public override bool HasUponPickupEffect => true;
    public override bool IsAllowedInShops => false;

    public override RelicAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/relics/{nameof(ClosureModRelic)}.png",
        IconOutlinePath: $"{Entry.ResPath}/images/relics/{nameof(ClosureModRelic)}.png",
        BigIconPath: $"{Entry.ResPath}/images/relics/{nameof(ClosureModRelic)}.png");

    public override async Task AfterObtained()
    {
        if (Owner is null)
        {
            return;
        }

        DrawGame drawGame = Owner.RunState.CreateCard<DrawGame>(Owner);
        var result = await CardPileCmd.Add(drawGame, PileType.Deck);
        if (result.success)
        {
            CardCmd.PreviewCardPileAdd(result);
        }
    }
}
