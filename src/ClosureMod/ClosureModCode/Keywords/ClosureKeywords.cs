using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib.Content;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace ClosureMod.Keywords;

[RegisterOwnedCardKeyword(nameof(Sluggish), IconPath = "res://ClosureMod/images/powers/SluggishPower.png")]
[RegisterOwnedCardKeyword(nameof(Debt), IconPath = "res://ClosureMod/images/powers/EnergyDebtPower.png")]
[RegisterOwnedCardKeyword(nameof(OverflowDamage))]
[RegisterOwnedCardKeyword(nameof(Block), IncludeInCardHoverTip = false)]
[RegisterOwnedCardKeyword(nameof(Vulnerable))]
[RegisterOwnedCardKeyword(nameof(Weak))]
[RegisterOwnedCardKeyword(nameof(Artifact))]
public sealed class ClosureKeywords
{
    public const string SluggishId = "CLOSURE_MOD_KEYWORD_SLUGGISH";
    public const string DebtId = "CLOSURE_MOD_KEYWORD_DEBT";
    public const string OverflowDamageId = "CLOSURE_MOD_KEYWORD_OVERFLOW_DAMAGE";
    public const string BlockId = "CLOSURE_MOD_KEYWORD_BLOCK";
    public const string VulnerableId = "CLOSURE_MOD_KEYWORD_VULNERABLE";
    public const string WeakId = "CLOSURE_MOD_KEYWORD_WEAK";
    public const string ArtifactId = "CLOSURE_MOD_KEYWORD_ARTIFACT";

    public static readonly CardKeyword Sluggish =
        ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(Sluggish)).GetModCardKeyword();

    public static readonly CardKeyword Debt =
        ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(Debt)).GetModCardKeyword();

    public static readonly CardKeyword OverflowDamage =
        ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(OverflowDamage)).GetModCardKeyword();

    public static readonly CardKeyword Block =
        ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(Block)).GetModCardKeyword();

    public static readonly CardKeyword Vulnerable =
        ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(Vulnerable)).GetModCardKeyword();

    public static readonly CardKeyword Weak =
        ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(Weak)).GetModCardKeyword();

    public static readonly CardKeyword Artifact =
        ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(Artifact)).GetModCardKeyword();
}
