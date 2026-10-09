using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace ClosureMod.Patches;

public partial class ClosureIdleVoiceController : Node
{
    private const double IdleSeconds = 10;
    private static ClosureIdleVoiceController? _instance;

    private CombatState? _combat;
    private int _turnNumber = -1;
    private double _inactiveSeconds;
    private bool _played;

    internal static void EnsureAttached()
    {
        if (GodotObject.IsInstanceValid(_instance))
        {
            return;
        }

        _instance = new ClosureIdleVoiceController { Name = "ClosureIdleVoice" };
        ((SceneTree)Engine.GetMainLoop()).Root.AddChild(_instance);
    }

    public override void _Ready() => SetProcessInput(true);

    public override void _Input(InputEvent @event) => _inactiveSeconds = 0;

    public override void _Process(double delta)
    {
        if (RunManager.Instance.State?.CurrentRoom is not CombatRoom room ||
            room.CombatState.Players.FirstOrDefault(ClosureVoicePatch.IsLocalClosure) is not Player player ||
            player.PlayerCombatState is not { TurnNumber: > 0 } turn)
        {
            Reset();
            return;
        }

        if (!ReferenceEquals(_combat, room.CombatState) || _turnNumber != turn.TurnNumber)
        {
            _combat = room.CombatState;
            _turnNumber = turn.TurnNumber;
            _inactiveSeconds = 0;
            _played = false;
        }

        if (turn.Phase != PlayerTurnPhase.Play || CombatManager.Instance.IsOverOrEnding ||
            CombatManager.Instance.PlayerActionsDisabled)
        {
            _inactiveSeconds = 0;
            return;
        }

        if (!_played && (_inactiveSeconds += delta) >= IdleSeconds)
        {
            _played = true;
            ClosureVoicePatch.Play("Poke.wav");
        }
    }

    private void Reset()
    {
        _combat = null;
        _turnNumber = -1;
        _inactiveSeconds = 0;
        _played = false;
    }
}
