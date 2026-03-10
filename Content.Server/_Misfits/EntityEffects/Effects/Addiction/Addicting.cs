// #Misfits Change - Ported from Delta-V addiction system
using Content.Shared._Misfits.Addictions;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server._Misfits.EntityEffects.Effects.Addiction;

/// <summary>
///     Reagent effect that applies an addiction to the entity.
///     Duration scales with reagent quantity.
///     Also registers per-drug withdrawal effects (mood, damage, speed, stamina)
///     that are applied by <see cref="Content.Server._Misfits.Addictions.AddictionSystem"/>
///     while the entity is in active withdrawal (not suppressed).
/// </summary>
[UsedImplicitly]
public sealed partial class Addicting : EntityEffect
{
    /// <summary>
    ///     Base addiction time in seconds per 1u of reagent metabolized.
    /// </summary>
    [DataField]
    public float Time = 5f;

    // #Misfits Change /Add:/ Per-drug withdrawal effect parameters.
    // These are authored in chems.yml on each !type:Addicting block.

    /// <summary>
    ///     Mood prototype ID to apply during active withdrawal.
    ///     Leave empty for drugs that handle their own mood via ChemAddMoodlet (e.g. Jet/MovespeedMixture).
    /// </summary>
    [DataField]
    public string WithdrawalMoodEffect = string.Empty;

    /// <summary>
    ///     Damage applied periodically while in active withdrawal.
    ///     Thematically opposite to the drug's benefit (e.g. a healing drug causes damage on withdrawal).
    /// </summary>
    [DataField]
    public DamageSpecifier? WithdrawalDamage;

    /// <summary>
    ///     Multiplicative movement speed penalty while in active withdrawal (1.0 = none).
    ///     Used for stimulant drugs whose absence causes lethargy.
    /// </summary>
    [DataField]
    public float WithdrawalSpeedPenalty = 1.0f;

    /// <summary>
    ///     Stamina damage applied periodically while in active withdrawal.
    ///     0.0 = none.
    /// </summary>
    [DataField]
    public float WithdrawalStaminaDrain = 0.0f;

    public override void Effect(EntityEffectBaseArgs args)
    {
        var addictionSys = args.EntityManager.EntitySysManager.GetEntitySystem<SharedAddictionSystem>();

        var time = Time;

        // #Misfits Change /Tweak:/ Pass the reagent's localized name so chat messages can name the specific drug
        var drugName = string.Empty;
        if (args is EntityEffectReagentArgs reagentArgs)
        {
            time *= reagentArgs.Scale.Float();
            drugName = reagentArgs.Reagent?.LocalizedName ?? string.Empty;
        }

        addictionSys.TryApplyAddiction(args.TargetEntity, time, drugName);

        // #Misfits Change /Add:/ Register withdrawal parameters (strongest values win on multi-drug)
        addictionSys.SetWithdrawalEffects(
            args.TargetEntity,
            WithdrawalMoodEffect,
            WithdrawalDamage,
            WithdrawalSpeedPenalty,
            WithdrawalStaminaDrain);
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("reagent-effect-guidebook-addicted", ("chance", Probability));
    }
}
