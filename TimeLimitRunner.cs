using System;
using System.Diagnostics;
using System.Threading;

namespace ASD
{
    public static class TimeLimitRunner
    {
        public static double CalculateSpeedFactor(Action action = null)
        {
            var stopwatch = new Stopwatch();
            if (action == null) action = () => Fibonacci(37);
            action();
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
            GC.WaitForPendingFinalizers();
            stopwatch.Start();
            action();
            stopwatch.Stop();
            return stopwatch.Elapsed.TotalMilliseconds / 1000.0;
        }

        public static double Run(double timeLimit, bool checkTimeLimit, Exception expectedException, out bool timeout, out Exception exception, Action action, int stackSize = 1)
        {
            if (timeLimit <= 0.0)
                throw new ArgumentException("non-positive timeLimit is incorrect");
            Exception internalException = null;
            var internalTimeout = false;
            var millisecondsTimeout = (!checkTimeLimit || timeLimit > 86400.0) ? -1 : ((int)Math.Ceiling(timeLimit * 1000.0));
            var sw = new Stopwatch();
            var thread = new Thread(() =>
            {
                try
                {
                    GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
                    GC.WaitForPendingFinalizers();
                    sw.Start();
                    action();
                    sw.Stop();
                }
                catch (ThreadAbortException)
                {
                    sw.Stop();
                    internalTimeout = true;
                    Thread.ResetAbort();
                }
                catch (Exception ex) when (checkTimeLimit ||
                                           (expectedException != null &&
                                            ex.GetType() == expectedException.GetType()))
                {
                    sw.Stop();
                    internalException = ex;
                }
            }, stackSize * 1048576);
            thread.Start();
            if (!thread.Join(millisecondsTimeout))
            {
                thread.Abort();
                thread.Join();
            }
            exception = internalException;
            timeout = internalTimeout;
            return sw.Elapsed.TotalMilliseconds / 1000.0;
        }

        private static double Fibonacci(int n)
        {
            if (n >= 2)
            {
                return Fibonacci(n - 1) + Fibonacci(n - 2);
            }
            return n;
        }
    }
}
