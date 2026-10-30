using System;
using System.Threading.Tasks;

namespace Qrs.TestConsole
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            Console.WriteLine("QRS Integration Test starting...");

            //var t = new Tests.NewUserTest();
            //var res = t.Run();

            //var t = new LegacyTests.WrapUnwrapTest();

            try
            {
                var t = new IntegrationTests.MainTest();
                var res = await t.Run();
                if (!res)
                {
                    Console.WriteLine("Integration test FAILED (MainTest.Run() returned false).");
                    return 1;
                }
                Console.WriteLine("MainTest PASSED.");

                var pwmTest = new IntegrationTests.PwmTest();
                var pwmRes = await pwmTest.Run();
                if (!pwmRes)
                {
                    Console.WriteLine("Integration test FAILED (PwmTest.Run() returned false).");
                    return 1;
                }
                Console.WriteLine("PwmTest PASSED.");

                var pwmNotesTest = new IntegrationTests.PwmTest();
                if (!await pwmNotesTest.RunEditSecretNotesTest())
                {
                    Console.WriteLine("Integration test FAILED (PwmTest.RunEditSecretNotesTest() returned false).");
                    return 1;
                }
                Console.WriteLine("PwmTest.RunEditSecretNotesTest PASSED.");

                var pwmUsernameTest = new IntegrationTests.PwmTest();
                if (!await pwmUsernameTest.RunEditUsernameSecretClosedTest())
                {
                    Console.WriteLine("Integration test FAILED (PwmTest.RunEditUsernameSecretClosedTest() returned false).");
                    return 1;
                }
                Console.WriteLine("PwmTest.RunEditUsernameSecretClosedTest PASSED.");

                var pwmUsernameNotesTest = new IntegrationTests.PwmTest();
                if (!await pwmUsernameNotesTest.RunEditUsernameAndSecretNotesTest())
                {
                    Console.WriteLine("Integration test FAILED (PwmTest.RunEditUsernameAndSecretNotesTest() returned false).");
                    return 1;
                }
                Console.WriteLine("PwmTest.RunEditUsernameAndSecretNotesTest PASSED.");

                Console.WriteLine("Integration test PASSED.");
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Integration test FAILED with exception: {ex}");
                return 1;
            }
        }

        
    }
}
