using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Characters;
using STS2RitsuLib.Scaffolding.Godot;

namespace ClosureMod.Characters;

[RegisterCharacter]
public sealed partial class ClosureModCharacter : ModCharacterTemplate<ClosureModCardPool, ClosureModRelicPool, ClosureModPotionPool>
{
    public static readonly Color ThemeColor = new(0.78f, 0.35f, 0.48f);

    private const string SceneRoot = $"{Entry.ResPath}/scenes/characters";
    private const string ImageRoot = $"{Entry.ResPath}/images/characters";
    private const string ModelRoot = $"{Entry.ResPath}/models";
    private const string CharacterScenePath = $"{SceneRoot}/ClosureMod_character.tscn";
    private const string CombatAtlasPath = $"{ModelRoot}/char_4228_closur.atlas";
    private const string CombatSkeletonPath = $"{ModelRoot}/char_4228_closur_4.2.43.skel";
    private const string EnergyCounterScenePath = $"{SceneRoot}/ClosureMod_energy_counter.tscn";
    private const string MerchantScenePath = $"{SceneRoot}/ClosureMod_merchant.tscn";
    private const string RestSiteScenePath = $"{SceneRoot}/ClosureMod_rest_site.tscn";
    private const string CharacterSelectBgScenePath = $"{SceneRoot}/ClosureMod_character_select_bg.tscn";
    private const float CombatModelScale = 0.6f;
    private const float AttackAnimationTimeScale = 1.25f;

    // 角色名称颜色。
    public override Color NameColor => ThemeColor;
    // 能量图标轮廓颜色。
    public override Color EnergyLabelOutlineColor => new(0.08f, 0.18f, 0.24f);
    // 地图绘制颜色。
    public override Color MapDrawingColor => new(0.55f, 0.02f, 0.04f);

    // 人物性别（男女中立）。
    public override CharacterGender Gender => CharacterGender.Neutral;

    // 初始血量和金币。
    public override int StartingHp => 70;
    public override int StartingGold => 99;

    public override CharacterAssetProfile AssetProfile => new(
        Scenes: new CharacterSceneAssetSet(
            VisualsPath: CharacterScenePath,
            EnergyCounterPath: EnergyCounterScenePath,
            MerchantAnimPath: MerchantScenePath,
            RestSiteAnimPath: RestSiteScenePath),
        Ui: new CharacterUiAssetSet(
            IconTexturePath: $"{ImageRoot}/ClosureMod_character_icon.png",
            IconOutlineTexturePath: $"{ImageRoot}/ClosureMod_character_icon_outline.png",
            IconPath: $"{ImageRoot}/ClosureMod_character_icon.png",
            CharacterSelectBgPath: CharacterSelectBgScenePath,
            CharacterSelectIconPath: $"{ImageRoot}/ClosureMod_avatar.png",
            CharacterSelectLockedIconPath: $"{ImageRoot}/ClosureMod_avatar_locked.png",
            MapMarkerPath: $"{ImageRoot}/ClosureMod_map_marker.png"));

    // 某个字段没写时，RitsuLib 会从占位角色配置里补齐。
    public override string? PlaceholderCharacterId => "ironclad";
    // 如果你的人物不需要时间线小故事，加上这句。
    public override bool RequiresEpochAndTimeline => false;
    // 攻击和施法动画延迟，以对齐动画。静态占位资源不需要延迟。
    public override float AttackAnimDelay => 0f;
    public override float CastAnimDelay => 0f;

    protected override NCreatureVisuals? TryCreateCreatureVisuals()
    {
        NCreatureVisuals? visuals = RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(
            CharacterScenePath);
        TryInstallRuntimeSpineVisuals(visuals);
        return visuals;
    }

    private static void TryInstallRuntimeSpineVisuals(NCreatureVisuals? root)
    {
        if (root is null)
        {
            return;
        }

        Node? oldBody = root.GetNodeOrNull("%Visuals");
        if (oldBody is null)
        {
            return;
        }

        Node2D? spineBody = TryCreateSpineBody();
        if (spineBody is null)
        {
            return;
        }

        string visualName = oldBody.Name;
        oldBody.Name = "StaticVisualsFallback";
        oldBody.UniqueNameInOwner = false;

        spineBody.Name = visualName;
        spineBody.Position = Vector2.Zero;
        spineBody.Scale = new Vector2(CombatModelScale, CombatModelScale);
        spineBody.AddChild(new AttackAnimationSpeedController());

        Node? parent = oldBody.GetParent();
        int oldIndex = oldBody.GetIndex();
        parent?.AddChild(spineBody);
        parent?.MoveChild(spineBody, oldIndex);
        spineBody.Owner = root;
        spineBody.UniqueNameInOwner = true;

        if (oldBody is CanvasItem fallback)
        {
            fallback.Visible = false;
        }

        Entry.Logger.Info("[Animation] Closure Spine visuals installed with Idle, Attack_Begin/Loop/End and Die.");
    }

    private static Node2D? TryCreateSpineBody()
    {
        Node2D? spineBody = null;
        try
        {
            if (!ClassDB.ClassExists("SpineSprite"))
            {
                throw new InvalidOperationException("The Spine extension is unavailable.");
            }

            using Resource atlas = LoadSpineFileResource(
                "SpineAtlasResource", "load_from_atlas_file", CombatAtlasPath);
            using Resource skeleton = LoadSpineFileResource(
                "SpineSkeletonFileResource", "load_from_file", CombatSkeletonPath);
            using Resource skeletonData = (Resource)ClassDB.Instantiate("SpineSkeletonDataResource").AsGodotObject();
            skeletonData.Set("atlas_res", atlas);
            skeletonData.Set("skeleton_file_res", skeleton);

            if (!skeletonData.Call("is_skeleton_data_loaded").AsBool())
            {
                throw new InvalidOperationException("Spine could not parse the skeleton and atlas.");
            }

            foreach (string animation in new[] { "Idle", "Attack_Begin", "Attack_Loop", "Attack_End", "Die" })
            {
                if (skeletonData.Call("find_animation", animation).AsGodotObject() is null)
                {
                    throw new InvalidOperationException($"Spine animation '{animation}' is missing.");
                }
            }

            // Validate everything before replacing %Visuals; NCreatureVisuals caches it in _Ready.
            spineBody = (Node2D)ClassDB.Instantiate("SpineSprite").AsGodotObject();
            spineBody.Call("set_skeleton_data_res", skeletonData);
            new MegaSprite(spineBody).GetAnimationState().SetAnimation("Idle", true, 0);
            return spineBody;
        }
        catch (Exception ex)
        {
            spineBody?.Free();
            Entry.Logger.Warn($"[Animation] Using static Closure visuals: {ex}");
            return null;
        }
    }

    private static Resource LoadSpineFileResource(string className, string loadMethod, string path)
    {
        Resource resource = (Resource)ClassDB.Instantiate(className).AsGodotObject();
        try
        {
            Error error = (Error)resource.Call(loadMethod, path).AsInt64();
            if (error != Error.Ok)
            {
                throw new InvalidOperationException($"{className}.{loadMethod} failed for '{path}': {error}.");
            }

            return resource;
        }
        catch
        {
            resource.Dispose();
            throw;
        }
    }

    protected override CreatureAnimator? SetupCustomCreatureAnimator(MegaSprite controller)
    {
        AnimState idle = new("Idle", true);
        AnimState attackEnd = new("Attack_End", false) { NextState = idle };
        AnimState attackLoop = new("Attack_Loop", false) { NextState = attackEnd };
        AnimState attackBegin = new("Attack_Begin", false) { NextState = attackLoop };
        AnimState dead = new("Die", false);
        CreatureAnimator animator = new(idle, controller);
        animator.AddAnyState("Idle", idle);
        animator.AddAnyState("Attack", attackBegin);
        animator.AddAnyState("Cast", attackBegin);
        animator.AddAnyState("Hit", idle);
        animator.AddAnyState("Relaxed", idle);
        animator.AddAnyState("Dead", dead);
        return animator;
    }

    private sealed partial class AttackAnimationSpeedController : Node
    {
        private MegaAnimationState? _animationState;
        private string? _currentAnimationName;
        private float _currentTimeScale = 1f;

        public override void _Ready()
        {
            if (GetParent() is Node2D spineBody)
            {
                _animationState = new MegaSprite(spineBody).GetAnimationState();
            }
        }

        public override void _Process(double delta)
        {
            MegaTrackEntry? current = _animationState?.GetCurrent(0);
            if (current is null)
            {
                return;
            }

            string animationName = current.GetAnimationName();
            float timeScale = animationName.StartsWith("Attack_", StringComparison.Ordinal)
                ? AttackAnimationTimeScale
                : 1f;

            if (_currentAnimationName == animationName && Math.Abs(_currentTimeScale - timeScale) < 0.001f)
            {
                return;
            }

            current.SetTimeScale(timeScale);
            _currentAnimationName = animationName;
            _currentTimeScale = timeScale;
        }
    }

    // 攻击建筑师的攻击特效列表。
    public override List<string> GetArchitectAttackVfx()
    {
        return
        [
            "vfx/vfx_attack_blunt",
            "vfx/vfx_heavy_blunt",
            "vfx/vfx_attack_slash",
            "vfx/vfx_bloody_impact",
            "vfx/vfx_rock_shatter"
        ];
    }
}
