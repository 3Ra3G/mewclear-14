// #Misfits Add - WoundableComponent: placed on humanoids to enable wound tracking.
// When an entity with this component takes brute or burn damage above BleedMinDamage,
// a wound is created that bleeds over time until treated.
// Ported/simplified from RMC-14 — skills and RMC-specific systems removed.
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Misfits.Wounds;

/// <summary>
/// Add to a humanoid entity to enable the wound + bleed system.
/// Works alongside Shitmed surgery (which handles internal bleeders separately).
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class WoundableComponent : Component
{
    /// <summary>
    /// Minimum brute/burn damage in a single hit before a bleed wound is created.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 BleedMinDamage = 10;

    /// <summary>
    /// How much bloodloss per damage point (scales with wound severity).
    /// </summary>
    [DataField, AutoNetworkedField]
    public float BloodLossMultiplier = 0.0375f;

    /// <summary>
    /// How long (in seconds) a wound bleeds per point of damage.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan DurationMultiplier = TimeSpan.FromSeconds(2.5f);

    [DataField, AutoNetworkedField]
    public ProtoId<DamageGroupPrototype> BruteWoundGroup = "Brute";

    [DataField, AutoNetworkedField]
    public ProtoId<DamageGroupPrototype> BurnWoundGroup = "Burn";
}
