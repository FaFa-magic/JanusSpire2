using JanusSpire2.JanusSpire2Code.Cards;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Models;
using STS2RitsuLib.Scaffolding.Content;

namespace JanusSpire2.JanusSpire2Code.Singleton;

[RegisterSingleton]
public class JanusSingleton : HookedSingletonModel
{
    public static JanusSingleton? Instance { get; private set; }

    public JanusSingleton() : base(HookType.Combat)
    {
    }
    
}