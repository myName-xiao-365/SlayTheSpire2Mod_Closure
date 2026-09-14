using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib.Content;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace ClosureMod.Keywords;

[RegisterOwnedCardKeyword(nameof(Sluggish), IconPath = "res://ClosureMod/images/powers/SluggishPower.png")]
[RegisterOwnedCardKeyword(nameof(Debt), IconPath = "res://ClosureMod/images/powers/EnergyDebtPower.png")]
public sealed class ClosureKeywords
{
    public const string SluggishId = "CLOSURE_MOD_KEYWORD_SLUGGISH";
    public const string DebtId = "CLOSURE_MOD_KEYWORD_DEBT";

    public static readonly CardKeyword Sluggish =
        ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(Sluggish)).GetModCardKeyword();

    public static readonly CardKeyword Debt =
        ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(Debt)).GetModCardKeyword();
}
