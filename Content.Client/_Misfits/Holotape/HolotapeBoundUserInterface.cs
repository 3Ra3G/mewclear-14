using Content.Shared._Misfits.Holotape;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

// #Misfits Add - Client BUI for the holotape/terminal viewer.
// Creates the green-on-black terminal window and receives content state from server.
// #Misfits Add - Wires up notes submit/delete/request events for the notebook tab.
// #Misfits Add - Wires up link port invoke events for the LINKS tab.

namespace Content.Client._Misfits.Holotape;

/// <summary>
/// Bound user interface that bridges server state to the HolotapeWindow display.
/// Sends note submit/delete/request messages and link port invoke messages to the server.
/// </summary>
[UsedImplicitly]
public sealed class HolotapeBoundUserInterface : BoundUserInterface
{
    private HolotapeWindow? _window;

    public HolotapeBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<HolotapeWindow>();

        // Wire up notebook events to send BUI messages to the server
        _window.OnSubmitNote += (author, text) =>
        {
            SendMessage(new SubmitTerminalNoteMessage(author, text));
        };

        _window.OnDeleteNote += noteId =>
        {
            SendMessage(new DeleteTerminalNoteMessage(noteId));
        };

        _window.OnRequestNotes += () =>
        {
            SendMessage(new RequestTerminalNotesMessage());
        };

        // #Misfits Add - Wire link port invocation to send BUI message to server
        _window.OnInvokeLinkPort += portId =>
        {
            SendMessage(new InvokeTerminalLinkPortMessage(portId));
        };
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_window == null || state is not HolotapeBoundUserInterfaceState cast)
            return;

        // Update the DATA tab content
        // #Misfits Add - Set header/title based on whether holotape item or built-in terminal
        _window.SetHolotapeMode(cast.IsHolotapeItem);
        _window.UpdateContent(cast.Title, cast.Content);

        // Update the NOTES tab (null notes means no notebook on this terminal)
        _window.UpdateNotes(cast.Notes, cast.ViewerUserId);

        // #Misfits Add - Update the LINKS tab (shows device link port buttons when terminal has links)
        _window.UpdateLinks(cast.HasLinkSource, cast.LinkPorts);
    }
}
