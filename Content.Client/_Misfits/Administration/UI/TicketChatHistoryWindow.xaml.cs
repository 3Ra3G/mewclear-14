// Misfits — Ticket Chat History Window
// Displays the full persisted bwoink/mhelp conversation for a specific ticket so admins
// can review what was said in any past ticket directly from the Audit Log.
using System.Linq;
using Content.Client.Administration.Systems;
using Content.Shared._Misfits.Administration;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Localization;
using Robust.Shared.Maths;

namespace Content.Client._Misfits.Administration.UI;

public sealed class TicketChatHistoryWindow : DefaultWindow
{
    private readonly BwoinkSystem _bwoinkSys;
    private readonly int _ticketId;
    private readonly HelpTicketType _ticketType;
    private readonly Guid _playerId;

    private readonly BoxContainer _messageList;
    private readonly Label _statusLabel;

    public TicketChatHistoryWindow(int ticketId, HelpTicketType ticketType, Guid playerId, string playerName)
    {
        _ticketId = ticketId;
        _ticketType = ticketType;
        _playerId = playerId;

        Title = Loc.GetString("ticket-chat-history-window-title",
            ("id", ticketId), ("player", playerName));
        MinWidth = 480;
        MinHeight = 320;

        var entMan = IoCManager.Resolve<IEntityManager>();
        _bwoinkSys = entMan.System<BwoinkSystem>();
        _bwoinkSys.OnTicketChatReceived += OnChatReceived;

        // Layout
        var root = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical, Margin = new Thickness(4) };
        Contents.AddChild(root);

        _statusLabel = new Label
        {
            Text = Loc.GetString("ticket-chat-history-loading"),
            HorizontalAlignment = HAlignment.Center,
            Margin = new Thickness(0, 8),
        };
        root.AddChild(_statusLabel);

        var scroll = new ScrollContainer { VerticalExpand = true, HScrollEnabled = false };
        root.AddChild(scroll);

        _messageList = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
        };
        scroll.AddChild(_messageList);

        // Request messages from server on open
        _bwoinkSys.RequestTicketChat(_ticketId, _ticketType, _playerId);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _bwoinkSys.OnTicketChatReceived -= OnChatReceived;
    }

    private void OnChatReceived(HelpTicketChatResponseMessage msg)
    {
        // Only handle responses for the ticket this window is showing
        if (msg.TicketId != _ticketId || msg.TicketType != _ticketType)
            return;

        _messageList.RemoveAllChildren();
        _statusLabel.Visible = false;

        if (msg.Messages.Count == 0)
        {
            var empty = new Label
            {
                Text = Loc.GetString("ticket-chat-history-empty"),
                HorizontalAlignment = HAlignment.Center,
                Margin = new Thickness(0, 16),
            };
            _messageList.AddChild(empty);
            return;
        }

        foreach (var entry in msg.Messages)
        {
            // Each message: "[HH:mm] SenderName: text"
            var timeStr = entry.SentAt.ToLocalTime().ToString("HH:mm");
            var prefix = entry.SenderIsStaff ? "[Staff]" : "[Player]";
            var color = entry.SenderIsStaff ? Color.CornflowerBlue : Color.LightGreen;

            var row = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Horizontal,
                HorizontalExpand = true,
                Margin = new Thickness(0, 1),
            };

            row.AddChild(new Label
            {
                Text = $"{timeStr} {prefix} ",
                FontColorOverride = color,
                MinWidth = 130,
            });

            row.AddChild(new Label
            {
                Text = $"{entry.SenderName}: {entry.MessageText}",
                HorizontalExpand = true,
                ClipText = false,
            });

            _messageList.AddChild(row);
        }
    }
}
