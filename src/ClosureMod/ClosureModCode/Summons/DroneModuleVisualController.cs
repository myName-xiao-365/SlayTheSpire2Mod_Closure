using ClosureMod.Powers;
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace ClosureMod.Summons;

public partial class DroneModuleVisualController : Node
{
    private static readonly Vector2 AttackPosition = new(-160f, -250f);
    private static readonly Vector2 DefensePosition = new(0f, -305f);
    private static readonly Vector2 SupportPosition = new(160f, -250f);
    private static readonly Vector2 BarOffset = new(55f, 45f);
    private static readonly Vector2 BarSize = new(90f, 16f);
    private static readonly Vector2 BarScale = new(0.62f, 0.62f);
    private const string HealthBarScenePath = "res://scenes/combat/health_bar.tscn";

    private Sprite2D? _attackModule;
    private Sprite2D? _defenseModule;
    private Sprite2D? _supportModule;
    private ModuleHealthBar? _attackBar;
    private ModuleHealthBar? _defenseBar;
    private ModuleHealthBar? _supportBar;
    private double _time;
    private bool _loggedHealthBarException;

    public NCreature? CreatureNode { get; set; }

    public override void _Process(double delta)
    {
        _time += delta;

        if (!ResolveSprites() || CreatureNode?.Entity is not { } creature)
        {
            HideAll();
            return;
        }

        DroneSwarmPower? swarm = creature.Powers.OfType<DroneSwarmPower>().FirstOrDefault();
        SetModule(_attackModule, ref _attackBar, "AttackModuleHealthBar", swarm is { HasAttackModule: true }, swarm?.AttackModuleMaxHp ?? 0, AttackPosition, 0f);
        SetModule(_defenseModule, ref _defenseBar, "DefenseModuleHealthBar", swarm is { HasDefenseModule: true }, swarm?.DefenseModuleMaxHp ?? 0, DefensePosition, 1.4f);
        SetModule(_supportModule, ref _supportBar, "SupportModuleHealthBar", swarm is { HasSupportModule: true }, swarm?.SupportModuleMaxHp ?? 0, SupportPosition, 2.8f);
    }

    private bool ResolveSprites()
    {
        if (CreatureNode is null)
        {
            return false;
        }

        _attackModule ??= CreatureNode.FindChild("AttackModule", true, false) as Sprite2D;
        _defenseModule ??= CreatureNode.FindChild("DefenseModule", true, false) as Sprite2D;
        _supportModule ??= CreatureNode.FindChild("SupportModule", true, false) as Sprite2D;

        return _attackModule is not null || _defenseModule is not null || _supportModule is not null;
    }

    private void HideAll()
    {
        SetVisible(_attackModule, false);
        SetVisible(_defenseModule, false);
        SetVisible(_supportModule, false);
        _attackBar?.SetVisible(false);
        _defenseBar?.SetVisible(false);
        _supportBar?.SetVisible(false);
    }

    private void SetModule(
        Sprite2D? sprite,
        ref ModuleHealthBar? bar,
        string barName,
        bool visible,
        int maxHp,
        Vector2 basePosition,
        float phase)
    {
        if (sprite is null)
        {
            return;
        }

        SetVisible(sprite, visible);
        if (!visible)
        {
            bar?.SetVisible(false);
            return;
        }

        float bob = Mathf.Sin((float)_time * 2.6f + phase) * 7f;
        sprite.Position = basePosition + new Vector2(0f, bob);
        sprite.ZIndex = 1;
        bar ??= TryEnsureHealthBar(sprite, barName);
        try
        {
            bar?.Update(maxHp, sprite);
        }
        catch (Exception exception)
        {
            LogHealthBarException(exception);
            bar?.SetVisible(false);
            bar = null;
        }
    }

    private static void SetVisible(Sprite2D? sprite, bool visible)
    {
        if (sprite is not null)
        {
            sprite.Visible = visible;
        }
    }

    private ModuleHealthBar? EnsureHealthBar(Sprite2D? sprite, string name)
    {
        if (sprite is null || CreatureNode?.Entity is not { } creature || GetDisplayPlayer() is not { } player)
        {
            return null;
        }

        if (sprite.GetNodeOrNull<NHealthBar>(name) is { } existing)
        {
            Creature existingDisplayCreature = new(player, 1, 1)
            {
                HpDisplay = HpDisplay.Normal,
                CombatState = creature.CombatState
            };
            ConfigureHealthBar(existing, sprite);
            existing.SetCreature(existingDisplayCreature);
            existing.SetHpBarContainerSizeWithOffsets(BarSize);
            return new ModuleHealthBar(existing, existingDisplayCreature, true);
        }

        if (sprite.GetNodeOrNull<Node>(name) is { } staleNode)
        {
            staleNode.QueueFree();
        }
        if (sprite.GetParent()?.GetNodeOrNull<Node>(name) is { } oldParentNode)
        {
            oldParentNode.QueueFree();
        }

        Creature displayCreature = new(player, 1, 1)
        {
            HpDisplay = HpDisplay.Normal,
            CombatState = creature.CombatState
        };

        NHealthBar? root = ResourceLoader
            .Load<PackedScene>(HealthBarScenePath)?
            .Instantiate<NHealthBar>();
        if (root is null)
        {
            return null;
        }

        root.Name = name;
        root.Position = Vector2.Zero;
        ConfigureHealthBar(root, sprite);
        root.Visible = false;
        sprite.AddChild(root);

        return new ModuleHealthBar(root, displayCreature, false);
    }

    private static void ConfigureHealthBar(NHealthBar healthBar, Sprite2D moduleSprite)
    {
        healthBar.Scale = GetInverseModuleScale(moduleSprite);
        healthBar.ZIndex = 2;
        healthBar.ZAsRelative = true;
        healthBar.MouseFilter = Control.MouseFilterEnum.Ignore;
        healthBar.MouseBehaviorRecursive = Control.MouseBehaviorRecursiveEnum.Disabled;
    }

    private static Vector2 GetInverseModuleScale(Sprite2D moduleSprite)
    {
        float scaleX = Math.Abs(moduleSprite.Scale.X) > 0.0001f ? Math.Abs(moduleSprite.Scale.X) : 1f;
        float scaleY = Math.Abs(moduleSprite.Scale.Y) > 0.0001f ? Math.Abs(moduleSprite.Scale.Y) : 1f;
        return new Vector2(BarScale.X / scaleX, BarScale.Y / scaleY);
    }

    private static Vector2 GetModuleLocalBarOffset(Sprite2D moduleSprite)
    {
        float scaleX = Math.Abs(moduleSprite.Scale.X) > 0.0001f ? moduleSprite.Scale.X : 1f;
        float scaleY = Math.Abs(moduleSprite.Scale.Y) > 0.0001f ? moduleSprite.Scale.Y : 1f;
        return new Vector2(BarOffset.X / scaleX, BarOffset.Y / scaleY);
    }

    private Player? GetDisplayPlayer()
    {
        if (CreatureNode?.Entity.Player is { } player)
        {
            return player;
        }

        return CreatureNode?.Entity.CombatState?.Players.FirstOrDefault();
    }

    private sealed class ModuleHealthBar
    {
        private readonly NHealthBar _root;
        private readonly Creature _displayCreature;
        private bool _initialized;

        public ModuleHealthBar(NHealthBar root, Creature displayCreature, bool initialized)
        {
            _root = root;
            _displayCreature = displayCreature;
            _initialized = initialized;
        }

        public void SetVisible(bool visible)
        {
            _root.Visible = visible;
        }

        public void Update(int maxHp, Sprite2D moduleSprite)
        {
            maxHp = Math.Max(0, maxHp);
            _root.Visible = maxHp > 0;
            _root.Position = GetModuleLocalBarOffset(moduleSprite);
            if (maxHp <= 0)
            {
                return;
            }

            if (!EnsureInitialized())
            {
                _root.Visible = false;
                return;
            }

            _displayCreature.MaxHp = maxHp;
            _displayCreature.CurrentHp = maxHp;
            _displayCreature.Block = 0;
            _root.SetHpBarContainerSizeWithOffsets(BarSize);
            _root.UpdateWidthRelativeToReferenceValue(maxHp, maxHp);
            _root.RefreshValues();
        }

        private bool EnsureInitialized()
        {
            if (_initialized)
            {
                return true;
            }

            if (!_root.IsNodeReady())
            {
                return false;
            }

            _root.SetCreature(_displayCreature);
            _root.SetHpBarContainerSizeWithOffsets(BarSize);
            _root.RefreshValues();
            _initialized = true;
            return true;
        }
    }

    private ModuleHealthBar? TryEnsureHealthBar(Sprite2D? sprite, string name)
    {
        try
        {
            return EnsureHealthBar(sprite, name);
        }
        catch (Exception exception)
        {
            LogHealthBarException(exception);
            return null;
        }
    }

    private void LogHealthBarException(Exception exception)
    {
        if (_loggedHealthBarException)
        {
            return;
        }

        _loggedHealthBarException = true;
        Entry.Logger.Error($"Failed to create or update drone module health bar. Drone sprites will stay visible. {exception}");
    }
}
