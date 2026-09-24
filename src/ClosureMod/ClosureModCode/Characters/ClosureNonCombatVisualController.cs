using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Helpers;

namespace ClosureMod.Characters;

// Merchant and rest-site scenes use this controller because they are not NCreature
// scenes and therefore do not pass through ClosureModCharacter's combat visual hook.
public partial class ClosureNonCombatVisualController : Node2D
{
    private const string ModelRoot = "res://ClosureMod/models";
    private const string CombatAtlasPath = $"{ModelRoot}/char_4228_closur.atlas";
    private const string CombatSkeletonPath = $"{ModelRoot}/char_4228_closur_4.2.43.skel";
    private const float ModelScale = 0.6f;

    public override void _Ready()
    {
        CallDeferred(nameof(InstallIdleVisualDeferred));
    }

    private void InstallIdleVisualDeferred()
    {
        InstallIdleVisual(this);
    }

    internal static void InstallIdleVisual(Node root)
    {
        Node2D? existingSpine = FindSpineSprite(root);
        Node2D? fallback = root.FindChild("Visuals", true, false) as Node2D;

        if (existingSpine is not null)
        {
            HideStaticVisuals(root);
            PlayIdleWhenReady(root, existingSpine);
            return;
        }

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

            if (skeletonData.Call("find_animation", "Idle").AsGodotObject() is null)
            {
                throw new InvalidOperationException("Spine animation 'Idle' is missing.");
            }

            Node2D spineBody = (Node2D)ClassDB.Instantiate("SpineSprite").AsGodotObject();
            spineBody.Name = "SpineIdleVisuals";
            spineBody.Position = fallback?.Position ?? new Vector2(0f, -96f);
            spineBody.Scale = new Vector2(ModelScale, ModelScale);
            spineBody.Call("set_skeleton_data_res", skeletonData);

            Node parent = fallback?.GetParent() ?? root;
            parent.AddChild(spineBody);
            parent.MoveChild(spineBody, 0);
            HideStaticVisuals(root);
            PlayIdleWhenReady(parent, spineBody);
        }
        catch (Exception ex)
        {
            Entry.Logger.Warn($"[Animation] Using static non-combat Closure visuals: {ex}");
        }
    }

    private static Node2D? FindSpineSprite(Node root)
    {
        foreach (Node node in root.GetChildren())
        {
            if (node is Node2D node2D && node.GetClass() == "SpineSprite")
            {
                return node2D;
            }
        }

        return root.FindChild("SpineIdleVisuals", true, false) as Node2D;
    }

    private static void HideStaticVisuals(Node root)
    {
        foreach (Node node in root.GetChildren())
        {
            if (node is Sprite2D sprite && node.GetClass() != "SpineSprite")
            {
                sprite.Visible = false;
            }
        }
    }

    private static void PlayIdleWhenReady(Node host, Node2D spineBody)
    {
        SpineNodeExtensions.RunWhenSpineReady(
            host,
            new MegaSprite(spineBody),
            state => state.SetAnimation("Idle", true, 0));
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
}
