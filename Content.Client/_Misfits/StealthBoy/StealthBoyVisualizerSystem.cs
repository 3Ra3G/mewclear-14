// #Misfits Add - Client-side Stealth Boy visualizer. Reads StealthBoyActiveComponent.Opacity
// and applies it to the SpriteComponent color alpha so the player fades in/out visually.
using Content.Shared._Misfits.StealthBoy;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Client._Misfits.StealthBoy;

/// <summary>
/// Reads StealthBoyActiveComponent.Opacity (networked) and sets the entity's
/// sprite colour alpha to match, producing the cloaking visual effect.
/// </summary>
public sealed class StealthBoyVisualizerSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StealthBoyActiveComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<StealthBoyActiveComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<StealthBoyActiveComponent, AfterAutoHandleStateEvent>(OnStateChanged);
    }

    private void OnInit(Entity<StealthBoyActiveComponent> ent, ref ComponentInit args)
    {
        ApplyOpacity(ent, ent.Comp.Opacity);
    }

    private void OnRemove(Entity<StealthBoyActiveComponent> ent, ref ComponentRemove args)
    {
        // Restore full visibility
        ApplyOpacity(ent, 1f);
    }

    private void OnStateChanged(Entity<StealthBoyActiveComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        ApplyOpacity(ent, ent.Comp.Opacity);
    }

    private void ApplyOpacity(EntityUid uid, float opacity)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        // Preserve RGB, only modify alpha channel
        var color = sprite.Color;
        sprite.Color = color.WithAlpha(opacity);
    }
}
