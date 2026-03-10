// #Misfits Add - disarm verb with DoAfter timer for landmines
using Content.Server.DoAfter;
using Content.Server.Explosion.EntitySystems;
using Content.Shared._Misfits.LandMines;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.StepTrigger.Systems;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Utility;

namespace Content.Server.LandMines;

public sealed class LandMineSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly TriggerSystem _trigger = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<LandMineComponent, StepTriggeredOnEvent>(HandleStepOnTriggered);
        SubscribeLocalEvent<LandMineComponent, StepTriggeredOffEvent>(HandleStepOffTriggered);
        SubscribeLocalEvent<LandMineComponent, StepTriggerAttemptEvent>(HandleStepTriggerAttempt);
        // #Misfits Add - disarm via interaction verb with DoAfter timer
        SubscribeLocalEvent<LandMineComponent, GetVerbsEvent<AlternativeVerb>>(AddDisarmVerb);
        SubscribeLocalEvent<LandMineComponent, LandMineDisarmDoAfterEvent>(OnDisarmDoAfter);
    }

    private void HandleStepOnTriggered(EntityUid uid, LandMineComponent component, ref StepTriggeredOnEvent args)
    {
        _popupSystem.PopupCoordinates(
            Loc.GetString("land-mine-triggered", ("mine", uid)),
            Transform(uid).Coordinates,
            args.Tripper,
            PopupType.LargeCaution);

        _audioSystem.PlayPvs(component.Sound, uid);
    }

    private void HandleStepOffTriggered(EntityUid uid, LandMineComponent component, ref StepTriggeredOffEvent args)
    {
        _trigger.Trigger(uid, args.Tripper);
    }

    private static void HandleStepTriggerAttempt(EntityUid uid, LandMineComponent component, ref StepTriggerAttemptEvent args)
    {
        args.Continue = true;
    }

    // #Misfits Add - add "Disarm" alternative verb to landmines so players can defuse them
    private void AddDisarmVerb(EntityUid uid, LandMineComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess || args.Hands == null)
            return;

        AlternativeVerb verb = new()
        {
            Text = Loc.GetString("land-mine-verb-disarm"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/pickup.svg.192dpi.png")),
            Act = () =>
            {
                _popupSystem.PopupEntity(
                    Loc.GetString("land-mine-disarm-start", ("mine", uid)),
                    uid,
                    args.User);

                var doAfterArgs = new DoAfterArgs(EntityManager, args.User, TimeSpan.FromSeconds(4), new LandMineDisarmDoAfterEvent(), uid, target: uid)
                {
                    BreakOnDamage = true,
                    BreakOnMove = true,
                    NeedHand = true,
                    BreakOnHandChange = true,
                };

                _doAfter.TryStartDoAfter(doAfterArgs);
            },
            Priority = 1,
        };

        args.Verbs.Add(verb);
    }

    // #Misfits Add - on successful disarm DoAfter, pick up the mine
    private void OnDisarmDoAfter(EntityUid uid, LandMineComponent component, LandMineDisarmDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || Deleted(uid))
            return;

        args.Handled = true;

        _popupSystem.PopupEntity(
            Loc.GetString("land-mine-disarm-success", ("mine", uid)),
            uid,
            args.User);

        _hands.TryPickupAnyHand(args.User, uid);
    }
}
