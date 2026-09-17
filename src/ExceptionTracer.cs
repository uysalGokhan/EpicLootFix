using System;

namespace EpicLootFix
{
    /// <summary>
    /// Diagnostic only. Unity's own error log for the GetJoyRightStickY
    /// MissingMethodException shows an empty stack trace, so we can't tell from
    /// LogOutput.log which method actually calls it. AppDomain.FirstChanceException
    /// fires the instant an exception is thrown, before any unwinding/handling, so
    /// its StackTrace should be fully populated even for exceptions Unity itself logs
    /// poorly. This should tell us definitively where the call is really coming from -
    /// static IL scans of every installed mod DLL found zero direct call sites for
    /// GetJoyRightStickY, which points at a Harmony transpiler injecting the call into
    /// a vanilla method at runtime (that wouldn't show up in any DLL's own IL).
    /// </summary>
    internal static class ExceptionTracer
    {
        private static int _loggedCount;
        private const int MaxToLog = 5;

        internal static void Install()
        {
            AppDomain.CurrentDomain.FirstChanceException += OnFirstChanceException;
        }

        private static void OnFirstChanceException(object sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
        {
            if (e.Exception is MissingMethodException mme && mme.Message.Contains("GetJoyRightStickY"))
            {
                if (System.Threading.Interlocked.Increment(ref _loggedCount) <= MaxToLog)
                {
                    Plugin.Log?.LogWarning("[EpicLootFix] FirstChanceException full trace:\n" +
                                           mme.StackTrace + "\n---TargetSite: " + mme.TargetSite + "---");
                }
            }
        }
    }
}
