// #Misfits Add - WoundedComponent: dynamically added when an entity has active wounds.
// Tracks multiple wounds simultaneously. Server ticks bleed damage each second.
// Ported/simplified from RMC-14 — skills and RMC-specific systems removed.
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Misfits.Wounds;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedWoundsSystem))]
public sealed partial class WoundedComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<DamageGroupPrototype> BruteWoundGroup = "Brute";

    [DataField, AutoNetworkedField]
    public ProtoId<DamageGroupPrototype> BurnWoundGroup = "Burn";

    /// <summary>
    /// All active wounds on this entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<Wound> Wounds = new();

    /// <summary>
    /// Passive healing rate per second for wounds (negative = heals wounds).
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 PassiveHealing = FixedPoint2.New(-0.05f);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    public TimeSpan UpdateAt;

    [DataField, AutoNetworkedField]
    public TimeSpan UpdateCooldown = TimeSpan.FromSeconds(1);
}

[DataRecord]
[Serializable, NetSerializable]
public partial record struct Wound(
    FixedPoint2 Damage,
    FixedPoint2 Healed,
    float Bloodloss,
    [field: DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    TimeSpan? StopBleedAt,
    WoundType Type,
    bool Treated
);

[Serializable, NetSerializable]
public enum WoundType
{
    Brute = 0,
    Burn,
    Surgery // Reserved for future Shitmed surgery wound integration
}
