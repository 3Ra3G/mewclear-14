// #Misfits Add - WoundBleedReducerComponent: placed on items that partially slow active bleeding.
// Unlike WoundTreaterComponent (which stops wounds completely), this reduces the bloodloss
// rate of all active wounds immediately when used — stimpaks, healing powder, poultice, etc.
// Stacks multiplicatively if applied multiple times.
using Robust.Shared.GameStates;

namespace Content.Shared._Misfits.Wounds;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class WoundBleedReducerComponent : Component
{
    /// <summary>
    /// Fraction to reduce all active wound bloodloss rates by on use.
    /// 0.5 = halves the bleed rate. Applies multiplicatively if used multiple times.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float BleedReduction = 0.5f;
}
