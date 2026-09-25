using ArkBase.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ClosureMod.Powers;

[RegisterPower]
public sealed class SenseOfSecurityPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png");

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Amount <= 0 || Owner.Player is not { } player ||
            !ReferenceEquals(cardPlay.Card.Owner, player) ||
            cardPlay.Card is not SupportCardTemplate)
        {
            return;
        }

        Flash();
        await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, cardPlay, false);
    }
}
