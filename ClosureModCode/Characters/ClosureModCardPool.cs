using Godot;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Utils;

namespace ClosureMod.Characters;

public sealed class ClosureModCardPool : TypeListCardPoolModel
{
    private static readonly Material? PoolFrameTintMaterial =
        MaterialUtils.CreateRgbShaderMaterial(0.58f, 0.12f, 0.16f);

    // Title 和 EnergyColorName 是池子的稳定标识，不是玩家看到的角色名。
    // 自定义角色卡、遗物、药水池保持同一个 EnergyColorName，方便实验室和文本统一读取能量图标。
    public override string Title => "ClosureMod";
    public override string EnergyColorName => "ClosureMod";

    // 这里指定卡牌文本和大图使用的能量图标路径。
    // res://ClosureMod/... 里的 ClosureMod 是 PCK 资源目录，不是 C# namespace。
    public override string? BigEnergyIconPath => $"{Entry.ResPath}/images/characters/energy.png";
    public override string? TextEnergyIconPath => $"{Entry.ResPath}/images/characters/energy_text.png";

    public override Color DeckEntryCardColor => new(0.58f, 0.12f, 0.16f);
    public override Color EnergyOutlineColor => new(0.08f, 0.18f, 0.24f);
    public override Material? PoolFrameMaterial => PoolFrameTintMaterial;

    // false 表示这是角色专属卡池，不是事件/状态那类无色卡池。
    public override bool IsColorless => false;
}
