using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content;

namespace ClosureMod.Cards;

public abstract class SupportCardTemplate(
    int energyCost,
    CardType cardType,
    CardRarity rarity,
    TargetType targetType)
    : ModCardTemplate(energyCost, cardType, rarity, targetType, showInCardLibrary: true)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Retain, CardKeyword.Exhaust];

    public override bool CanBeGeneratedInCombat => false;
    public override bool CanBeGeneratedByModifiers => false;
}
