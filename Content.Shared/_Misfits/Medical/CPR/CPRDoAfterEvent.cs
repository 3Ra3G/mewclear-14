// #Misfits Add - CPR DoAfter event for the cardiopulmonary resuscitation mechanic.
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Misfits.Medical.CPR;

/// <summary>
/// Raised on the CPR performer when the do-after completes or is cancelled.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class CPRDoAfterEvent : SimpleDoAfterEvent
{
}
