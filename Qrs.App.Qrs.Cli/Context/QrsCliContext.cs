using Qrid.Common.Client.Cookie;
using Qrid.Common.Client.UI;
using Qrid.Common.Http;
using Qrid.Common.Proto.Crypto;
using Qrid.Common.Utils;
using Qrs.App.Qrs.Cli.Models;
using Qrs.App.Qrs.Cli.Payload;
using Qrs.Client.Context;
using Qrs.Proto;
using Qrs.Proto.Extensions;
using Qrs.Shared.Data;
using Qrs.Tunnel.Client;
using Spectre.Console;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Qrs.App.Qrs.Cli.Context
{
    public partial class QrsCliContext : ClientContext
    {
        public const string APP_NAME = "PROCESS LAUNCHER";
        public string TunnelServiceUri { get; set; }
        public string TunnelServiceDomain { get; set; }

        public CommandKeyWrapper CommandKeyWrapper { get; set; }

        public QrsCliContext(IUserInteraction ui, string tunnelServiceDomain, string tunnelServiceUri, IAsymKeyItemProvider asymKeyItemProvider, ICookieProvider cookieProvider, string apiKey = null) :
            base(APP_NAME, ui, asymKeyItemProvider, cookieProvider)
        {
            TunnelServiceDomain = tunnelServiceDomain;
            TunnelServiceUri = tunnelServiceUri;
            ApiKey = apiKey;
        }

        public void WriteHeader(string header)
        {
            var rule = new Rule(header).LeftJustified();
            AnsiConsole.Write(rule);
        }

        public void WriteError(string message)
        {
            
            AnsiConsole.MarkupLine($"[red]ERROR[/] - {message}");
        }

        public void WriteInfo(string message)
        {

            AnsiConsole.MarkupLine($"{message}");
        }

        public void WriteLine(string message)
        {

            AnsiConsole.WriteLine($"{message}");
        }


        public async Task<HttpResponseModel<StringResponse>> InitFromConfigAsync()
        {
            //var tunnelServiceDomain = ConfigurationManager.AppSettings["TunnelServiceDomain"];
            //var tunnelServiceUriTemplate = "http://%localip%:81/qrs-tunnel-api/"; //ConfigurationManager.AppSettings["TunnelServiceUriTemplate"];
            //var tunnelServiceUriTemplate = "http://localhost:81/qrs-tunnel-api/"; 

            // QrsContext - init using Config Params 
            var initResponse = await InitFromConfigAsync(TunnelServiceDomain, TunnelServiceUri, ApiKey);

            WriteInfo("Tunnel Server: " + TunnelClient?.BaseUrl ?? "not initialized");

            return initResponse;
        }

        public CommandFile GetCommandFile(string file)
        {
            var yml = File.ReadAllText(file);

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(PascalCaseNamingConvention.Instance) 
                .Build();

            return deserializer.Deserialize<CommandFile>(yml);
        }

        public void SaveCommandFile(CommandFile commandFile, string file)
        {
            var serializer = new SerializerBuilder()
                .WithNamingConvention(PascalCaseNamingConvention.Instance)
                .Build();
            var yml = serializer.Serialize(commandFile);

            File.WriteAllText(file, yml);
        }

        public string GetKeyString(QrsEnvelope wrappedKey)
        {
            var keyString = new KeyString();
            keyString.KeyType = KeyString.KEY_TYPE_QRS;
            keyString.WrappedKey = wrappedKey.GetBase64String();
            return keyString.ToQrString();
        }

        public KeyString GetKeyString(string keyString)
        {
            var res = KeyString.Create(keyString);
            return res;
        }

    }
}
