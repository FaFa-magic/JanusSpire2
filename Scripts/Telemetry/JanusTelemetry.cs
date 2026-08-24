using System.Text.Json.Nodes;
using JanusSpire2.JanusSpire2Code;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Telemetry;

namespace JanusSpire2.Scripts.Telemetry;

public static class JanusTelemetry
{
    public const string BalanceRequestId = "balance_event";

    private const string ApplicantId = MainFile.ModId;

    private static ITelemetryClient? _client;

    public static bool IsRegistered { get; private set; }

    public static void Register()
    {
        if (IsRegistered)
        {
            return;
        }

        if (!TryCreateAdapter(out ITelemetryAdapter? adapter) || adapter is null)
        {
            MainFile.Logger.Info(
                "[JanusTelemetry] Backend is not configured; telemetry applicant registration was skipped.");
            return;
        }

        TelemetryRegistry.RegisterApplicant(new TelemetryApplicant
        {
            ApplicantId = ApplicantId,
            OwnerModId = MainFile.ModId,
            DisplayName = "JanusSpire2",
            DisplayNameText = ModSettingsText.Literal("JanusSpire2"),
            Adapter = adapter,
            Requests =
            [
                TelemetryRequest.BasicUsage(
                    ModSettingsText.Literal(
                        "发送模组版本、游戏平台、游戏语言和匿名安装ID，用于分析版本兼容性。")),
                TelemetryRequest.RunHistory(
                    ModSettingsText.Literal(
                        "发送已结束的原版跑局历史，用于分析Janus的卡牌与遗物平衡性。")),
                TelemetryRequest.Diagnostics(
                    ModSettingsText.Literal(
                        "发送异常类型、堆栈和诊断上下文，用于定位JanusSpire2运行错误。")),
                TelemetryRequest.Custom(
                    BalanceRequestId,
                    ModSettingsText.Literal(
                        "发送JanusSpire2的平衡性事件及数值，不包含玩家昵称、本地路径或账号标识。"))
            ]
        });

        _client = TelemetryApi.GetClient(ApplicantId);
        IsRegistered = true;
    }

    public static bool IsRequestEnabled(string requestId)
    {
        return IsRegistered && _client?.IsEnabled(requestId) == true;
    }

    public static void CaptureBalanceEvent(
        string eventName,
        IReadOnlyDictionary<string, object?>? properties = null)
    {
        if (!IsRegistered)
        {
            return;
        }

        _client?.Capture(eventName, BalanceRequestId, properties);
    }

    public static void CaptureBalancePayload(
        string eventName,
        JsonNode payload,
        IReadOnlyDictionary<string, object?>? properties = null)
    {
        ArgumentNullException.ThrowIfNull(payload);
        if (!IsRegistered)
        {
            return;
        }

        _client?.CapturePayload(eventName, BalanceRequestId, payload, properties);
    }

    public static void CaptureException(Exception exception, string operation)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);
        if (!IsRegistered)
        {
            return;
        }

        _client?.CaptureException(
            exception,
            new Dictionary<string, object?>
            {
                ["operation"] = operation
            });
    }

    private static bool TryCreateAdapter(out ITelemetryAdapter? adapter)
    {
        adapter = null;
        if (!Uri.TryCreate(
                JanusTelemetryConfiguration.PostHogHost,
                UriKind.Absolute,
                out Uri? host) ||
            host.Scheme is not ("http" or "https") ||
            string.IsNullOrWhiteSpace(JanusTelemetryConfiguration.PostHogProjectApiKey))
        {
            return false;
        }

        adapter = new PostHogTelemetryAdapter(
            host.AbsoluteUri,
            JanusTelemetryConfiguration.PostHogProjectApiKey);
        return true;
    }
}
