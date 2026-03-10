using Content.Server.Carrying;
using Content.Shared.Carrying;
using Content.Shared.Stunnable;

namespace Content.Server._N14.Carrying;

/// <summary>
/// Applies a timed stun to an entity when it is successfully carried (grabbed)
/// by another entity with <see cref="GrabStunComponent"/>.
/// </summary>
public sealed class GrabStunSystem : EntitySystem
{
    [Dependency] private readonly SharedStunSystem _stun = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CarriableComponent, CarryDoAfterEvent>(OnCarryDoAfter);
    }

    private void OnCarryDoAfter(EntityUid uid, CarriableComponent _, CarryDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        // Confirm the carry actually completed (BeingCarriedComponent is added by CarryingSystem).
        if (!HasComp<BeingCarriedComponent>(uid))
            return;

        // Only stun if the carrier has GrabStunComponent.
        if (!TryComp<GrabStunComponent>(args.Args.User, out var grabStun))
            return;

        _stun.TryStun(uid, grabStun.StunTime, refresh: true);
    }
}
