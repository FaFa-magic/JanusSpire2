using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace JanusSpire2.JanusSpire2Code.Relics;

public sealed class Voucher : JanusRelicModel
{
    private const string PurchasesVar = "Purchases";

    private int _purchasesUsed;
    private bool _skipPurchaseThatObtainedThisRelic;

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override bool IsUsedUp => PurchasesUsed >= MaxPurchases;

    public override bool ShowCounter => !IsUsedUp;

    public override int DisplayAmount => Math.Max(0, MaxPurchases - PurchasesUsed);

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar(PurchasesVar, 2M)
    ];

    private int MaxPurchases => DynamicVars[PurchasesVar].IntValue;

    [SavedProperty]
    public int PurchasesUsed
    {
        get => _purchasesUsed;
        set
        {
            AssertMutable();
            _purchasesUsed = Math.Clamp(value, 0, MaxPurchases);
            Status = IsUsedUp ? RelicStatus.Disabled : RelicStatus.Normal;
            InvokeDisplayAmountChanged();
        }
    }

    public override Task AfterObtained()
    {
        var historyEntry = Owner.RunState.CurrentMapPointHistoryEntry?.GetEntry(Owner.NetId);
        _skipPurchaseThatObtainedThisRelic = historyEntry?.BoughtRelics.Count > 0 &&
                                             historyEntry.BoughtRelics[^1] == Id;
        return Task.CompletedTask;
    }

    public override decimal ModifyMerchantPrice(
        Player player,
        MerchantEntry entry,
        decimal originalPrice)
    {
        if (player != Owner || !LocalContext.IsMe(Owner) || IsUsedUp)
        {
            return originalPrice;
        }

        return 0M;
    }

    public override Task AfterItemPurchased(
        Player player,
        MerchantEntry itemPurchased,
        int goldSpent)
    {
        if (player != Owner || IsUsedUp)
        {
            return Task.CompletedTask;
        }

        if (_skipPurchaseThatObtainedThisRelic)
        {
            _skipPurchaseThatObtainedThisRelic = false;
            return Task.CompletedTask;
        }

        Flash();
        PurchasesUsed++;
        return Task.CompletedTask;
    }
}
