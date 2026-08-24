namespace JanusSpire2.Scripts.Telemetry;

internal static class JanusTelemetryConfiguration
{
    // Fill this with the Cloudflare proxy host or PostHog host before enabling telemetry.
    internal const string PostHogHost = "";

    // Keep "proxy" when a Cloudflare Worker injects the real PostHog project token.
    internal const string PostHogProjectApiKey = "proxy";
}
