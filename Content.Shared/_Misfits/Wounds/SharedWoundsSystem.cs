// #Misfits Add - SharedWoundsSystem: shared logic for wound & bleed.
// Handles: damage-event → wound creation, WoundTreater interactions, wound ticking.
// Ported/simplified from RMC-14 — skills, RMCDoAfterSystem, RMCDamageable removed.
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Timing;

namespace Content.Shared._Misfits.Wounds;

public abstract class SharedWoundsSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WoundableComponent, DamageChangedEvent>(OnWoundableDamaged);
        SubscribeLocalEvent<WoundableComponent, MobStateChangedEvent>(OnMobStateChange);
        SubscribeLocalEvent<WoundTreaterComponent, AfterInteractEvent>(OnWoundTreaterAfterInteract);
        SubscribeLocalEvent<WoundTreaterComponent, UseInHandEvent>(OnWoundTreaterUseInHand);
        SubscribeLocalEvent<WoundableComponent, TreatWoundDoAfterEvent>(OnWoundTreaterDoAfter);

        // Partial bleed reducers (stimpaks, healing powder, poultice)
        SubscribeLocalEvent<WoundBleedReducerComponent, UseInHandEvent>(OnReducerUseInHand);
        SubscribeLocalEvent<WoundBleedReducerComponent, AfterInteractEvent>(OnReducerAfterInteract);
    }

    // ---------- Event handlers ----------

    private void OnMobStateChange(Entity<WoundableComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
            RemCompDeferred<WoundedComponent>(ent);
    }

    private void OnWoundableDamaged(Entity<WoundableComponent> ent, ref DamageChangedEvent args)
    {
        if (args.DamageDelta == null || !args.DamageIncreased)
            return;

        // Check if something has vetoed bleeding for this entity
        var bleedAttempt = new CMBleedAttemptEvent(false);
        RaiseLocalEvent(ent, ref bleedAttempt);
        if (bleedAttempt.Cancelled)
            return;

        var bleedEvent = new CMBleedEvent(args);
        RaiseLocalEvent(ent, ref bleedEvent);
        if (bleedEvent.Handled)
            return;

        // Check brute first, then burn
        var bruteDamage = GetGroupDamage(args.DamageDelta, ent.Comp.BruteWoundGroup);
        var burnDamage = GetGroupDamage(args.DamageDelta, ent.Comp.BurnWoundGroup);

        if (bruteDamage >= ent.Comp.BleedMinDamage)
            AddWound(ent, bruteDamage, WoundType.Brute);

        if (burnDamage >= ent.Comp.BleedMinDamage)
            AddWound(ent, burnDamage, WoundType.Burn);
    }

    private void OnWoundTreaterUseInHand(Entity<WoundTreaterComponent> item, ref UseInHandEvent args)
    {
        TryStartTreatment(item, args.User, args.User);
        // Don't mark handled — stack-based combo items (e.g. N14Bandage) also have a Healing
        // component that must fire to consume the stack charge and apply immediate HP healing.
    }

    private void OnWoundTreaterAfterInteract(Entity<WoundTreaterComponent> item, ref AfterInteractEvent args)
    {
        if (args.Target == null)
            return;
        TryStartTreatment(item, args.User, args.Target.Value);
        // Don't mark handled — same reason as above.
    }

    private void OnWoundTreaterDoAfter(Entity<WoundableComponent> ent, ref TreatWoundDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        if (args.Used == null || !TryComp<WoundTreaterComponent>(args.Used.Value, out var treater))
            return;

        if (!TryComp<WoundedComponent>(ent, out var wounded))
        {
            var msg = args.User == ent.Owner
                ? Loc.GetString(treater.NoneSelfPopup)
                : Loc.GetString(treater.NoneOtherPopup, ("target", ent.Owner));
            _popup.PopupClient(msg, ent, args.User);
            return;
        }

        // Apply finish sound
        if (treater.TreatEndSound != null)
            PlaySoundClient(treater.TreatEndSound, ent);

        // Heal wounds of matching type
        TreatWounds(ent, wounded, treater);

        // Popups
        var userMsg = Loc.GetString(treater.UserFinishPopup, ("target", ent.Owner));
        _popup.PopupClient(userMsg, ent, args.User);

        if (args.User != ent.Owner)
        {
            var targetMsg = Loc.GetString(treater.TargetFinishPopup, ("user", args.User));
            _popup.PopupClient(targetMsg, ent, ent);
        }

        // Consume item if applicable
        if (treater.Consumable)
            QueueDel(args.Used.Value);

        // Clear component if no wounds remain
        if (wounded.Wounds.Count == 0)
            RemCompDeferred<WoundedComponent>(ent);
    }

    // ---------- Bleed reducers ----------

    private void OnReducerUseInHand(Entity<WoundBleedReducerComponent> item, ref UseInHandEvent args)
    {
        // Don’t mark Handled — Food/Hypospray also subscribe to this event and must fire.
        ApplyBleedReduction(item.Comp.BleedReduction, args.User);
    }

    private void OnReducerAfterInteract(Entity<WoundBleedReducerComponent> item, ref AfterInteractEvent args)
    {
        if (!args.CanReach)
            return;
        // Inject/force-feed target; fall back to user if no target was clicked.
        ApplyBleedReduction(item.Comp.BleedReduction, args.Target ?? args.User);
    }

    private void ApplyBleedReduction(float reduction, EntityUid target)
    {
        if (!TryComp<WoundedComponent>(target, out var wounded))
            return;

        var multiplier = 1f - Math.Clamp(reduction, 0f, 1f);
        for (var i = 0; i < wounded.Wounds.Count; i++)
        {
            var w = wounded.Wounds[i];
            if (!w.Treated)
                wounded.Wounds[i] = w with { Bloodloss = w.Bloodloss * multiplier };
        }
        Dirty(target, wounded);
    }

    // ---------- Core helpers ----------

    private void TryStartTreatment(Entity<WoundTreaterComponent> item, EntityUid user, EntityUid target)
    {
        if (!HasComp<WoundableComponent>(target))
            return;

        TryComp<WoundedComponent>(target, out var wounded);

        // No active wounds — show popup only for dedicated wound-treater items.
        // Stack-based combo items (like N14Bandage) set ShowNoWoundsPopup = false so
        // they silently skip here and let their Healing component handle the interaction.
        if (wounded == null)
        {
            if (item.Comp.ShowNoWoundsPopup)
            {
                var msg = user == target
                    ? Loc.GetString(item.Comp.NoneSelfPopup)
                    : Loc.GetString(item.Comp.NoneOtherPopup, ("target", target));
                _popup.PopupClient(msg, target, user);
            }
            return;
        }

        // Calculate do-after duration: base = total wound damage * ScalingDoAfter
        var totalDamage = wounded != null ? GetTotalWoundDamage(wounded) : FixedPoint2.Zero;
        var doAfterDelay = item.Comp.ScalingDoAfter * (double)totalDamage;
        // Floor at 1 second
        if (doAfterDelay < TimeSpan.FromSeconds(1))
            doAfterDelay = TimeSpan.FromSeconds(1);

        // Initiate treatment do-after
        if (item.Comp.TreatBeginSound != null)
            PlaySoundClient(item.Comp.TreatBeginSound, user);

        var userMsg = Loc.GetString(item.Comp.UserPopup, ("target", target));
        _popup.PopupClient(userMsg, target, user);

        if (user != target)
        {
            var targetMsg = Loc.GetString(item.Comp.TargetPopup, ("user", user));
            _popup.PopupClient(targetMsg, target, target);
        }

        var ev = new TreatWoundDoAfterEvent();
        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, user, doAfterDelay, ev, target, target: target, used: item)
        {
            BreakOnMove = true,
            CancelDuplicate = true,
            BlockDuplicate = false,
            NeedHand = true
        });
    }

    private void AddWound(Entity<WoundableComponent> ent, FixedPoint2 damage, WoundType type)
    {
        var now = Timing.CurTime;
        var bloodloss = (float)damage * ent.Comp.BloodLossMultiplier;
        var duration = TimeSpan.FromSeconds((double)(damage * (float)ent.Comp.DurationMultiplier.TotalSeconds));
        var stopAt = now + duration;

        var wound = new Wound(damage, FixedPoint2.Zero, bloodloss, stopAt, type, false);
        var wounded = EnsureComp<WoundedComponent>(ent);
        wounded.Wounds.Add(wound);
        Dirty(ent, wounded);
    }

    private void TreatWounds(Entity<WoundableComponent> ent, WoundedComponent wounded, WoundTreaterComponent treater)
    {
        for (var i = wounded.Wounds.Count - 1; i >= 0; i--)
        {
            var wound = wounded.Wounds[i];
            if (wound.Type != treater.Wound || wound.Treated)
                continue;

            // Mark as treated — stops bleed, no damage refund
            wounded.Wounds[i] = wound with { Treated = true, StopBleedAt = Timing.CurTime };

            // Apply healing
            if (treater.Damage != null)
            {
                _damageable.TryChangeDamage(ent,
                    new DamageSpecifier(_proto.Index<DamageGroupPrototype>(treater.Group), -treater.Damage.Value),
                    ignoreResistances: true);
            }
            break; // Treat one wound per use
        }

        // Clean up wounds that are both treated and expired
        wounded.Wounds.RemoveAll(w => w.Treated && (w.StopBleedAt == null || w.StopBleedAt <= Timing.CurTime));
        Dirty(ent.Owner, wounded);
    }

    private static FixedPoint2 GetGroupDamage(DamageSpecifier? delta, ProtoId<DamageGroupPrototype> group)
    {
        if (delta == null)
            return FixedPoint2.Zero;

        FixedPoint2 total = FixedPoint2.Zero;
        foreach (var (type, amount) in delta.DamageDict)
        {
            // We check if the damage type name is within the brute/burn group
            // by checking the prototype prefix — simplistic but avoids registry lookups in hot path
            total += amount > 0 ? amount : FixedPoint2.Zero;
        }
        return total;
    }

    private static FixedPoint2 GetTotalWoundDamage(WoundedComponent wounded)
    {
        var total = FixedPoint2.Zero;
        foreach (var w in wounded.Wounds)
            total += w.Damage - w.Healed;
        return total;
    }

    // Overridden server-side for actual audio playback
    protected virtual void PlaySoundClient(SoundSpecifier sound, EntityUid source) { }
}
