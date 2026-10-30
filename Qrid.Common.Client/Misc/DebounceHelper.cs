using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Qrid.Common.Client.Misc
{
    public class DebounceHelper
    {
        private CancellationTokenSource debounceToken = null;

        public async Task DebounceAsync(Func<CancellationToken, Task> func, int milliseconds = 1000)
        {
            try
            {
                // Cancel previous task
                if (debounceToken != null) { debounceToken.Cancel(); }

                // Assign new token
                debounceToken = new CancellationTokenSource();

                // Debounce delay
                await Task.Delay(milliseconds, debounceToken.Token);

                // Throw if canceled
                debounceToken.Token.ThrowIfCancellationRequested();

                // Run function
                await func(debounceToken.Token);
            }
            catch (TaskCanceledException) { }
        }
    }
}
