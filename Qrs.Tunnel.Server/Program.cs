using System.Collections.Generic;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Qrid.Common.Const;
using Qrid.Common.Server.Extensions;
using Qrs.Tunnel.Server.Json;
using Qrs.Tunnel.Server.Models;
using Qrs.Tunnel.Server.Services;
using Qrs.Tunnel.Shared;
using Model = Qrs.Tunnel.Shared.Models;

namespace Qrs.Tunnel.Server
{
    public class Program
    {
        private const string BasePath = "/api/QrsTunnel";

        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateSlimBuilder(args);

            // Register the source-generated JSON metadata so that request/response
            // (de)serialization for the Minimal API endpoints below never relies on
            // runtime reflection. This is what makes Native AOT (PublishAot=true) work.
            builder.Services.ConfigureHttpJsonOptions(options =>
            {
                options.SerializerOptions.TypeInfoResolverChain.Insert(0, TunnelJsonContext.Default);
            });

            var app = builder.Build();

            // read the configurable session cap once at startup (see QrsTunnel:MaxSessionCount
            // in appsettings.json) rather than per-request, to avoid races across concurrent requests.
            TunnelService.Configure(app.Configuration);

            // start the background timer that periodically removes expired tunnel sessions.
            TunnelService.StartCleanupTimer();

            app.MapGet($"{BasePath}/Test", Test);
            // Admin/diagnostic endpoints - only accessible with a valid admin api key cookie
            // (see TunnelService.IsApiKeyValidForAdmin). Blocked when no api key is configured.
            app.MapGet($"{BasePath}/PrintHeaders", PrintHeaders);
            app.MapGet($"{BasePath}/Stats", Stats);
            app.MapGet($"{BasePath}/Cleanup", Cleanup);
            app.MapGet($"{BasePath}/XyzGetSessionLog", GetSessionLog);
            app.MapPost($"{BasePath}/XyzClearSessionLog", ClearSessionLog);
            app.MapPost($"{BasePath}/MessageSendPoll", MessageSendPollAsync);
            app.MapGet($"{BasePath}/Version", Version);
            app.MapMethods("/error", new[] { "GET", "POST" }, Error);

            app.Run();
        }

        private static IResult Test()
            => TypedResults.Json(
                TunnelResponse<string>.Success(message: "Test - Success"),
                TunnelJsonContext.Default);

        private static bool IsAdmin(HttpContext ctx)
        {
            var apiKey = ctx.Request.Cookies[CookieName.QrsTunnelApiKey]?.Trim() ?? string.Empty;
            return TunnelService.IsApiKeyValidForAdmin(apiKey);
        }

        private static IResult AdminApiKeyRequired()
            => TypedResults.Json(
                TunnelResponse<string>.Error($"Access denied. Please provide a valid API key in the '{CookieName.QrsTunnelApiKey}' cookie to use this endpoint."),
                TunnelJsonContext.Default);

        private static IResult PrintHeaders(HttpContext ctx)
        {
            if (!IsAdmin(ctx))
                return AdminApiKeyRequired();

            var headers = new Dictionary<string, string>();
            foreach (var header in ctx.Request.Headers)
                headers[header.Key] = header.Value.ToString();

            return TypedResults.Json(headers, TunnelJsonContext.Default);
        }

        private static IResult Stats(HttpContext ctx)
        {
            if (!IsAdmin(ctx))
                return AdminApiKeyRequired();

            return TypedResults.Json(
                TunnelResponse<Stats>.Success(TunnelService.Stats),
                TunnelJsonContext.Default);
        }

        private static IResult Cleanup(HttpContext ctx)
        {
            if (!IsAdmin(ctx))
                return AdminApiKeyRequired();

            TunnelService.RunCleanup();
            return TypedResults.Json(
                TunnelResponse<Stats>.Success(TunnelService.Stats),
                TunnelJsonContext.Default);
        }

        private static IResult GetSessionLog(HttpContext ctx)
        {
            if (!IsAdmin(ctx))
                return AdminApiKeyRequired();

            return TypedResults.Json(
                TunnelResponse<List<Qrid.Common.Analytics.SessionLogItem>>.Success(TunnelService.GetSessionLogs()),
                TunnelJsonContext.Default);
        }

        private static IResult ClearSessionLog(HttpContext ctx)
        {
            if (!IsAdmin(ctx))
                return AdminApiKeyRequired();

            var clearedCount = TunnelService.ClearSessionLogs();
            return TypedResults.Json(
                TunnelResponse<string>.Success(message: clearedCount.ToString()),
                TunnelJsonContext.Default);
        }

        private static IResult Version()
            => TypedResults.Json(
                new Model.VersionResponse
                {
                    ServerProtocolVersion = QrsTunnelProtocol.ServerProtocolVersion,
                    BuildVersion = BuildInfo.BuildVersion,
                    BuildTime = BuildInfo.BuildTime
                },
                TunnelJsonContext.Default);

        private static async Task<IResult> MessageSendPollAsync(Model.MessageSendPollRequest request, HttpContext ctx, ILogger<Program> logger)
        {
            var instanceId = ctx.Request.Cookies[CookieName.InstanceIdt]?.Trim() ?? string.Empty;
            var apiKey = ctx.Request.Cookies[CookieName.QrsTunnelApiKey]?.Trim() ?? string.Empty;
            var clientIp = ctx.Request.GetHeader("X-Forwarded-For") ?? string.Empty;

            var response = await TunnelService.MessageSendPollAsync(request, instanceId, clientIp, apiKey, logger);
            return TypedResults.Json(response, TunnelJsonContext.Default);
        }

        private static IResult Error(HttpContext ctx, ILogger<Program> logger)
        {
            var feature = ctx.Features.Get<IExceptionHandlerFeature>();
            if (feature?.Error != null)
            {
                logger.LogError(feature.Error, "Unhandled exception");
                return TypedResults.Json(
                    TunnelResponse<string>.Error("Unhandled server error"),
                    TunnelJsonContext.Default);
            }

            return TypedResults.Json(
                TunnelResponse<string>.Success(message: "There is no error"),
                TunnelJsonContext.Default);
        }
    }
}
