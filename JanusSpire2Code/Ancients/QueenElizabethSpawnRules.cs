using HarmonyLib;
using JanusSpire2.JanusSpire2Code.Configs;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib;
using STS2RitsuLib.RunData;

namespace JanusSpire2.JanusSpire2Code.Ancients;

public sealed class QueenElizabethRunSettings
{
    public bool Enabled { get; set; } = true;
}

internal static class QueenElizabethSpawnRules
{
    private static readonly AccessTools.FieldRef<ActModel, RoomSet> Rooms =
        AccessTools.FieldRefAccess<ActModel, RoomSet>("_rooms");

    private static RunSavedData<QueenElizabethRunSettings> _settings = null!;
    private static WeakReference<RunState>? _currentRun;

    internal static void Register()
    {
        using (RitsuLibFramework.BeginModDataRegistration(MainFile.ModId))
        {
            _settings = RitsuLibFramework.GetRunSavedDataStore(MainFile.ModId)
                .Register("queen_elizabeth_spawn", () => new QueenElizabethRunSettings());
        }

        RitsuLibFramework.SubscribeLifecycle<RunSavedDataLobbyStagingEvent>(evt =>
        {
            if (evt.Reason == RunSavedDataLobbyStagingReason.Committing &&
                (!evt.IsMultiplayer || evt.IsHost))
            {
                _settings.Lobby.Set(evt.Lobby, new QueenElizabethRunSettings
                {
                    Enabled = JanusConfigPage.QueenElizabethEnabledBinding.Read()
                });
            }
        }, replayCurrentState: false);

        // Single-player can start without a lobby. Never fill missing multiplayer data from local settings.
        RitsuLibFramework.SubscribeLifecycle<RunSavedDataPreparingEvent>(evt =>
        {
            if (!evt.IsMultiplayer && !_settings.TryGet(evt.RunState, out _))
            {
                _settings.Set(evt.RunState, new QueenElizabethRunSettings
                {
                    Enabled = JanusConfigPage.QueenElizabethEnabledBinding.Read()
                });
            }
        }, replayCurrentState: false);

        RitsuLibFramework.SubscribeLifecycle<RunStartedEvent>(evt =>
            _currentRun = new(evt.RunState), replayCurrentState: false);
        RitsuLibFramework.SubscribeLifecycle<RunLoadedEvent>(evt =>
            _currentRun = new(evt.RunState), replayCurrentState: false);
        RitsuLibFramework.SubscribeLifecycle<RunEndedEvent>(_ =>
            _currentRun = null, replayCurrentState: false);
    }

    internal static bool IsValidForAct(ActModel act, ModelId ancientId)
    {
        if (act is not (Hive or Glory))
        {
            return false;
        }

        // Canonical acts are also queried by the compendium, outside any run.
        if (!act.IsMutable)
        {
            return true;
        }

        if (_currentRun == null || !_currentRun.TryGetTarget(out RunState? runState) ||
            !runState.Acts.Contains(act) ||
            !_settings.TryGet(runState, out QueenElizabethRunSettings settings) ||
            !settings.Enabled)
        {
            return false;
        }

        foreach (ActModel otherAct in runState.Acts)
        {
            // Read only: Ancient throws until that act has finished choosing its rooms.
            if (!ReferenceEquals(otherAct, act) && Rooms(otherAct) is { HasAncient: true } rooms &&
                rooms.Ancient.Id == ancientId)
            {
                return false;
            }
        }

        return true;
    }
}
