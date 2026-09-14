using ClosureMod.Summons;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace ClosureMod.Patches;

[HarmonyPatch(typeof(NCreature), nameof(NCreature._Ready))]
internal static class DroneModuleVisualPatch
{
    private const string ControllerName = "ClosureDroneModuleVisualController";

    private static void Postfix(NCreature __instance)
    {
        if (__instance.GetNodeOrNull<DroneModuleVisualController>(ControllerName) is not null)
        {
            return;
        }

        DroneModuleVisualController controller = new()
        {
            Name = ControllerName,
            CreatureNode = __instance
        };
        __instance.AddChild(controller);
    }
}
