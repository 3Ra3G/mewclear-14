// #Misfits Change - Wasteland Map Viewer BUI
using Content.Shared._Misfits.WastelandMap;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.Client._Misfits.WastelandMap;

[UsedImplicitly]
public sealed class WastelandMapBoundUserInterface : BoundUserInterface
{
    private WastelandMapWindow? _window;

    public WastelandMapBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<WastelandMapWindow>();
        _window.OnAddAnnotation += annotation => SendMessage(new WastelandMapAddAnnotationMessage(annotation));
        _window.OnRemoveAnnotation += index => SendMessage(new WastelandMapRemoveAnnotationMessage(index));
        _window.OnClearAnnotations += () => SendMessage(new WastelandMapClearAnnotationsMessage());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not WastelandMapBoundUserInterfaceState mapState)
            return;

        var bounds = new Box2(mapState.BoundsLeft, mapState.BoundsBottom, mapState.BoundsRight, mapState.BoundsTop);
        var texturePath = new ResPath(mapState.MapTexturePath);
        _window?.SetMap(mapState.MapTitle, texturePath, bounds, mapState.TrackedBlips, mapState.SharedAnnotations, mapState.CompactHud);
    }
}

