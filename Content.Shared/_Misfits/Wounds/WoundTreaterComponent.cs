// #Misfits Add - WoundTreaterComponent: placed on items that can treat wounds (bandages, etc).
// Simplified from RMC-14 — skills and CanUseUnskilled removed; all characters can treat wounds.
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Misfits.Wounds;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class WoundTreaterComponent : Component
{
    // --- Core ---

    /// <summary>
    /// Which wound type this item treats (Brute or Burn).
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public WoundType Wound;

    /// <summary>
    /// If true, this item *treats* wounds (stops bleed). If false it only diagnoses / records.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public bool Treats;

    /// <summary>
    /// If true, the item is consumed (deleted) after a successful treatment.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Consumable = true;

    /// <summary>
    /// Damage group to reduce when this item finishes treatment.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public ProtoId<DamageGroupPrototype> Group;

    // --- Timing ---

    /// <summary>
    /// Base do-after duration. Scaled by total wound damage: duration += WoundDamage * ScalingDoAfter.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan ScalingDoAfter = TimeSpan.FromSeconds(0.1);

    // --- Healing ---

    /// <summary>
    /// Amount of damage in <see cref="Group"/> to heal on treatment finish. Null = heal all.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2? Damage;

    // --- Audio ---

    [DataField, AutoNetworkedField]
    public SoundSpecifier? TreatBeginSound;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? TreatEndSound;

    // --- Popups (LocId keys) ---

    [DataField, AutoNetworkedField]
    public LocId UserPopup = "wounds-treat-begin-user";

    [DataField, AutoNetworkedField]
    public LocId TargetPopup = "wounds-treat-begin-target";

    [DataField, AutoNetworkedField]
    public LocId OthersPopup = "wounds-treat-begin-others";

    [DataField, AutoNetworkedField]
    public LocId UserFinishPopup = "wounds-treat-finish-user";

    [DataField, AutoNetworkedField]
    public LocId TargetFinishPopup = "wounds-treat-finish-target";

    [DataField, AutoNetworkedField]
    public LocId NoneSelfPopup = "wounds-treat-none-self";

    [DataField, AutoNetworkedField]
    public LocId NoneOtherPopup = "wounds-treat-none-other";

    /// <summary>
    /// If false, skips the "no wounds found" popup when the target has no active wounds.
    /// Set to false on stack-based combo items (e.g. N14Bandage) that also have a Healing
    /// component — those items will still heal HP even if there are no wounds to treat.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ShowNoWoundsPopup = true;
}
