// #Misfits Change - Ported from Delta-V addiction system
using Content.Shared.Damage;
using Content.Shared.StatusEffect;
using Robust.Shared.Prototypes;

namespace Content.Shared._Misfits.Addictions;

/// <summary>
///     Shared addiction system. Handles applying and suppressing addictions
///     via the StatusEffectsSystem. Server overrides provide update/popup logic.
/// </summary>
public abstract class SharedAddictionSystem : EntitySystem
{
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;

    /// <summary>
    ///     Status effect key used for the addiction status.
    /// </summary>
    public ProtoId<StatusEffectPrototype> StatusEffectKey = "Addicted";

    /// <summary>
    ///     Server-side time bookkeeping for suppression windows.
    /// </summary>
    protected abstract void UpdateTime(EntityUid uid);

    /// <summary>
    ///     Attempts to apply an addiction to the entity.
    ///     If the entity already has the effect, extends its duration.
    ///     Calls <see cref="OnAddictionApplied"/> so the server can send drug-specific chat messages.
    /// </summary>
    /// <param name="drugName">Localized name of the drug (e.g. "hydra"). Empty skips chat messages.</param>
    public virtual void TryApplyAddiction(EntityUid uid, float addictionTime, string drugName = "", StatusEffectsComponent? status = null)
    {
        if (!Resolve(uid, ref status, false))
            return;

        UpdateTime(uid);

        // #Misfits Change /Tweak:/ Track whether this is a new addiction or an existing one deepening
        var isNew = !_statusEffects.HasStatusEffect(uid, StatusEffectKey, status);

        if (isNew)
        {
            _statusEffects.TryAddStatusEffect<AddictedComponent>(
                uid,
                StatusEffectKey,
                TimeSpan.FromSeconds(addictionTime),
                false,
                status);
        }
        else
        {
            _statusEffects.TryAddTime(uid, StatusEffectKey, TimeSpan.FromSeconds(addictionTime), status);
        }

        // Store drug name and increment dose count on the component
        if (TryComp<AddictedComponent>(uid, out var addicted))
        {
            if (!string.IsNullOrEmpty(drugName))
                addicted.DrugName = drugName;

            addicted.DoseCount++;
        }

        OnAddictionApplied(uid, isNew);
    }

    // #Misfits Change /Add:/ Store per-drug withdrawal effect parameters on the component.
    /// <summary>
    ///     Stores withdrawal gameplay effect parameters on the entity's <see cref="AddictedComponent"/>.
    ///     Called by the <c>Addicting</c> reagent effect after <see cref="TryApplyAddiction"/>.
    ///     Only updates a field if the new value represents a stronger effect than what is already set,
    ///     preventing a milder drug from overriding a harsher one's withdrawal on a multi-drug user.
    /// </summary>
    public void SetWithdrawalEffects(
        EntityUid uid,
        string moodEffect = "",
        DamageSpecifier? damage = null,
        float speedPenalty = 1.0f,
        float staminaDrain = 0.0f)
    {
        if (!TryComp<AddictedComponent>(uid, out var addicted))
            return;

        // Only update mood if not already set (first drug setting mood wins across different drugs)
        if (!string.IsNullOrEmpty(moodEffect) && string.IsNullOrEmpty(addicted.WithdrawalMoodEffect))
            addicted.WithdrawalMoodEffect = moodEffect;

        // Take the highest damage value
        if (damage != null)
            addicted.WithdrawalDamage = damage;

        // Take the lowest (most punishing) speed penalty
        if (speedPenalty < addicted.WithdrawalSpeedPenalty)
            addicted.WithdrawalSpeedPenalty = speedPenalty;

        // Take the highest stamina drain
        if (staminaDrain > addicted.WithdrawalStaminaDrain)
            addicted.WithdrawalStaminaDrain = staminaDrain;
    }

    /// <summary>
    ///     Called after addiction is applied or extended.
    ///     Server override uses this to send drug-specific chat messages based on dose count / severity.
    /// </summary>
    protected virtual void OnAddictionApplied(EntityUid uid, bool isNew) { }

    /// <summary>
    ///     Suppresses active addiction symptoms for a duration.
    /// </summary>
    public virtual void TrySuppressAddiction(EntityUid uid, float duration)
    {
        if (!TryComp<AddictedComponent>(uid, out var addicted))
            return;

        UpdateAddictionSuppression(uid, addicted, duration);
    }

    /// <summary>
    ///     Marks the addiction as suppressed and updates the suppression end time.
    /// </summary>
    protected void UpdateAddictionSuppression(EntityUid uid, AddictedComponent component, float duration)
    {
        component.Suppressed = true;
        Dirty(uid, component);
    }
}
