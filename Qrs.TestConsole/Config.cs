using Qrid.Common.Crypto;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using CC = Qrid.Common.Proto.CanonicalCrypto;
using CR = Qrid.Common.Proto.Crypto;
using CryptoUtils = Qrid.Common.Crypto.CryptoUtils;
using GWK = Google.Protobuf.WellKnownTypes;

namespace Qrs.TestConsole
{
    public static class Config
    {
        public static string TunnelServerBaseUrl; // URL of the QRS Tunnel Server — set from QRS_TEST_TUNNEL_URL env var, defaults to http://localhost:4999/
        public static string Domain; // our Domain - initialized automatically from loaded Certs
        public static string RootDir;
        public static string TestMasterKeyPassword = "123456"; // hardcoded password for all Master Keys, to make initializing easier
        
        static Config()
        {
            Init();
        }

        public static void Init(/*IConfiguration configuration*/)
        {
            Domain = Environment.GetEnvironmentVariable("QRS_TEST_DOMAIN") ?? "localhost";
            RootDir = Environment.GetEnvironmentVariable("QRS_TEST_ROOT") ?? GetDefaultRootDir();
            TunnelServerBaseUrl = Environment.GetEnvironmentVariable("QRS_TEST_TUNNEL_URL") ?? "http://localhost:4999/";

            if (!Directory.Exists(RootDir) )
            {
                LogDebug($"Creating RootDir at '{RootDir}'");
                Directory.CreateDirectory(RootDir);
            }
            LogDebug($"RootDir = '{RootDir}'");
            LogDebug($"Domain = '{Domain}'");
        }

        private static string GetDefaultRootDir()
        {
            return "Root";
            //return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "QridTestConsole", ""); // ENV_NAME;
        }

        [Conditional("DEBUG")]
        public static void LogDebug(string message)
        {
            Console.WriteLine($"{DateTime.Now.ToString()} - {message}");
        }

    }
}
