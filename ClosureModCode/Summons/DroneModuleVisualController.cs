using ClosureMod.Powers;
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.addons.mega_text;

namespace ClosureMod.Summons;

public partial class DroneModuleVisualController : Node
{
    private static readonly Vector2 AttackPosition = new(-195f, -280f);
    private static readonly Vector2 DefensePosition = new(0f, -350f);
    private static readonly Vector2 SupportPosition = new(195f, -280f);
    private static readonly Vector2 BarOffset = new(15.5f, 45f);
    private static readonly Vector2 BarSize = new(215f, 22f);
    private static readonly Vector2 BarScale = new(0.62f, 0.62f);
    private static readonly Vector2 StatusIconOffset = new(-49f, 78.5f);
    private const string HealthBarScenePath = "res://scenes/combat/health_bar.tscn";

    private Sprite2D? _attackModule;
    private Sprite2D? _defenseModule;
    private Sprite2D? _supportModule;
    private ModuleHealthBar? _attackBar;
    private ModuleHealthBar? _defenseBar;
    private ModuleHealthBar? _supportBar;
    private ModuleStatusView? _attackStatus;
    private ModuleStatusView? _defenseStatus;
    private ModuleStatusView? _supportStatus;
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
        SetModule(_attackModule, ref _attackBar, ref _attackStatus, DroneModuleKind.Attack,
            "AttackModuleHealthBar", swarm is { HasAttackModule: true }, swarm?.AttackModuleMaxHp ?? 0, AttackPosition, 0f);
        SetModule(_defenseModule, ref _defenseBar, ref _defenseStatus, DroneModuleKind.Defense,
            "DefenseModuleHealthBar", swarm is { HasDefenseModule: true }, swarm?.DefenseModuleMaxHp ?? 0, DefensePosition, 1.4f);
        SetModule(_supportModule, ref _supportBar, ref _supportStatus, DroneModuleKind.Support,
            "SupportModuleHealthBar", swarm is { HasSupportModule: true }, swarm?.SupportModuleMaxHp ?? 0, SupportPosition, 2.8f);
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
        _attackStatus?.SetVisible(false);
        _defenseStatus?.SetVisible(false);
        _supportStatus?.SetVisible(false);
    }

    private void SetModule(
        Sprite2D? sprite,
        ref ModuleHealthBar? bar,
        ref ModuleStatusView? status,
        DroneModuleKind kind,
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
            status?.SetVisible(false);
            return;
        }

        float bob = Mathf.Sin((float)_time * 2.6f + phase) * 7f;
        sprite.Position = basePosition + new Vector2(0f, bob);
        sprite.ZIndex = 1;
        bar ??= TryEnsureHealthBar(sprite, barName);
        status ??= new ModuleStatusView(this, sprite, kind);
        status.Update(sprite);
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
            existing.SetHpBarContainerSizeWithOffsetsImmediately(BarSize);
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

        // Kept outside combat's creature lists so neither side can target, damage, or buff it.
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
        if (healthBar.GetNodeOrNull<Label>("HpBarContainer/HpLabel") is { } hpLabel)
        {
            hpLabel.AddThemeFontSizeOverride("font_size", 39);
            hpLabel.AddThemeConstantOverride("outline_size", 26);
            hpLabel.OffsetTop = -12f;
            hpLabel.OffsetBottom = 13f;
        }
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
            _root.SetHpBarContainerSizeWithOffsetsImmediately(BarSize);
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
            _root.SetHpBarContainerSizeWithOffsetsImmediately(BarSize);
            _root.RefreshValues();
            _initialized = true;
            return true;
        }
    }

    private sealed class ModuleStatusView
    {
        private static readonly Vector2 HitboxSize = new(170f, 165f);
        private readonly DroneModuleVisualController _controller;
        private readonly DroneModuleKind _kind;
        private readonly Control _hitbox;
        private readonly Sprite2D _icon;
        private readonly Control _nameplate;

        public ModuleStatusView(DroneModuleVisualController controller, Sprite2D sprite, DroneModuleKind kind)
        {
            _controller = controller;
            _kind = kind;
            string name = kind switch
            {
                DroneModuleKind.Attack => "AttackAssistModulePower",
                DroneModuleKind.Defense => "DefenseAssistModulePower",
                DroneModuleKind.Support => "SupportAssistModulePower",
                _ => throw new ArgumentOutOfRangeException(nameof(kind))
            };

            _icon = new Sprite2D
            {
                Name = $"{kind}StatusIcon",
                Texture = GD.Load<Texture2D>($"{Entry.ResPath}/images/powers/{name}.png"),
                ZIndex = 3
            };
            sprite.AddChild(_icon);

            _nameplate = (Control)controller.CreatureNode!._stateDisplay._nameplateContainer.Duplicate();
            _nameplate.Name = $"{kind}Nameplate";
            _nameplate.Visible = false;
            _nameplate.Modulate = Colors.White;
            _nameplate.Size = new Vector2(176f, 28f);
            _nameplate.MouseFilter = Control.MouseFilterEnum.Ignore;
            _nameplate.MouseBehaviorRecursive = Control.MouseBehaviorRecursiveEnum.Disabled;
            _nameplate.ZIndex = 3;
            sprite.AddChild(_nameplate);
            string titleKey = $"CLOSURE_MOD_POWER_{kind.ToString().ToUpperInvariant()}_ASSIST_MODULE_POWER.title";
            _nameplate.GetNode<MegaLabel>("NameplateLabel").SetTextAutoSize(new LocString("powers", titleKey).GetFormattedText());

            _hitbox = new Control
            {
                Name = $"{kind}HoverArea",
                Size = HitboxSize,
                MouseFilter = Control.MouseFilterEnum.Stop,
                ZIndex = 4
            };
            _hitbox.MouseEntered += ShowHoverTip;
            _hitbox.MouseExited += HideHoverTip;
            _hitbox.TreeExiting += HideHoverTip;
            sprite.AddChild(_hitbox);
        }

        public void Update(Sprite2D sprite)
        {
            float scaleX = Math.Abs(sprite.Scale.X) > 0.0001f ? Math.Abs(sprite.Scale.X) : 1f;
            float scaleY = Math.Abs(sprite.Scale.Y) > 0.0001f ? Math.Abs(sprite.Scale.Y) : 1f;
            Vector2 iconPosition = new(StatusIconOffset.X / scaleX, StatusIconOffset.Y / scaleY);
            Vector2 iconScale = new(1f / scaleX, 1f / scaleY);
            _icon.Position = iconPosition;
            _icon.Scale = iconScale * 0.68f;
            _nameplate.Position = new Vector2(-54f / scaleX, 67.5f / scaleY);
            _nameplate.Scale = iconScale * 0.75f;
            _hitbox.Position = new Vector2(-85f / scaleX, -50f / scaleY);
            _hitbox.Scale = iconScale;
            if (NCombatRoom.Instance?.Ui?.Hand.InCardPlay == true)
            {
                HideHoverTip();
            }
            SetVisible(true);
        }

        public void SetVisible(bool visible)
        {
            _icon.Visible = visible;
            _hitbox.Visible = visible;
            if (!visible)
            {
                HideHoverTip();
            }
        }

        private void ShowHoverTip()
        {
            if (NCombatRoom.Instance?.Ui?.Hand.InCardPlay == true ||
                _controller.CreatureNode?.Entity?.Powers.OfType<DroneSwarmPower>().FirstOrDefault() is not { } swarm ||
                !swarm.HasModule(_kind) || _icon.Texture is not { } icon)
            {
                return;
            }

            int maxHp = _kind switch
            {
                DroneModuleKind.Attack => swarm.AttackModuleMaxHp,
                DroneModuleKind.Defense => swarm.DefenseModuleMaxHp,
                DroneModuleKind.Support => swarm.SupportModuleMaxHp,
                _ => 0
            };
            string powerName = $"CLOSURE_MOD_POWER_{_kind.ToString().ToUpperInvariant()}_ASSIST_MODULE_POWER";
            string descriptionKey = _kind == DroneModuleKind.Attack && swarm.AttackModuleHitsAllEnemies
                ? "allDescription" : "description";
            LocString description = new("powers", $"{powerName}.{descriptionKey}");
            description.Add("Amount", maxHp);

            HoverTip tip = new(new LocString("powers", $"{powerName}.title"), description, icon);
            NHoverTipSet.CreateAndShow(_hitbox, tip, HoverTip.GetHoverTipAlignment(_hitbox, 0.5f));
            _nameplate.Visible = true;
            _icon.Modulate = new Color(1f, 1f, 1f, 0.5f);
        }

        private void HideHoverTip()
        {
            NHoverTipSet.Remove(_hitbox);
            if (GodotObject.IsInstanceValid(_nameplate))
            {
                _nameplate.Visible = false;
            }
            if (GodotObject.IsInstanceValid(_icon))
            {
                _icon.Modulate = Colors.White;
            }
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
