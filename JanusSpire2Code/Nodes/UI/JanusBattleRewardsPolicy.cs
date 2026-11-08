namespace JanusSpire2.JanusSpire2Code.Nodes.UI;

/// <summary>Pure presentation policy: no rewards, RNG, or multiplayer state changes.</summary>
internal static class JanusBattleRewardsPolicy
{
    public const int SkinCount = 7;

    public static bool ShouldDecorate(bool enabled, bool testMode, bool terminal, bool combatRoom, bool janus) =>
        enabled && !testMode && terminal && combatRoom && janus;

    public static int SkinIndex(int skin) => skin is >= 0 and < SkinCount ? skin : 0;
}
