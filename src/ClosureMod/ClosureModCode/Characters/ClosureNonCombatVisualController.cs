using Godot;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;

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
        InstallIdleVisual(this);
    }

    internal static void InstallIdleVisual(Node root)
    {
        if (root.FindChild("SpineIdleVisuals", true, false) is not null)
        {
            return;
        }

        Node2D? fallback = root.FindChild("Visuals", true, false) as Node2D;
        if (fallback is null)
        {
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
            spineBody.Position = fallback.Position;
            spineBody.Scale = new Vector2(ModelScale, ModelScale);
            spineBody.Call("set_skeleton_data_res", skeletonData);
            new MegaSprite(spineBody).GetAnimationState().SetAnimation("Idle", true, 0);

            Node fallbackParent = fallback.GetParent();
            fallbackParent.AddChild(spineBody);
            fallbackParent.MoveChild(spineBody, fallback.GetIndex());
            fallback.Visible = false;
        }
        catch (Exception ex)
        {
            Entry.Logger.Warn($"[Animation] Using static non-combat Closure visuals: {ex}");
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
}
