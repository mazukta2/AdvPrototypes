using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Postica.Common
{
    internal class SmartAwaiter : IDisposable
    {
        private readonly float _maxSliceInMs;
        private readonly Stopwatch _stopwatch;
        
        public SmartAwaiter(float maxSliceInMs = 10)
        {
            _maxSliceInMs = maxSliceInMs;
            _stopwatch = new Stopwatch();
            _stopwatch.Start();
        }

        public async ValueTask Await()
        {
            if (_stopwatch.ElapsedMilliseconds > _maxSliceInMs)
            {
                // GC.Collect();
                await Task.Yield();
                _stopwatch.Restart();
            }
        }

        public void Dispose()
        {
        }
    }
}
