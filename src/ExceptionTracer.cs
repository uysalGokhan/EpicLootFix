using System;

namespace EpicLootFix
{
    /// <summary>
    /// Diagnostic only. Logs every exception the CLR raises (via
    /// AppDomain.FirstChanceException, which fires before any catch/swallow
    /// happens anywhere - including inside Unity's own UnityEvent.Invoke, which
    /// catches and logs per-listener exceptions itself and does NOT always show up
    /// clearly in LogOutput.log). This bypasses whatever layer has been hiding the
    /// real cause of the Enchant/Augment "stuck on Cancel" symptom - the earlier,
    /// narrower GetJoyRightStickY-only filter never caught it, and it turned out
    /// unrelated to the actual bug.
    /// </summary>
    internal static class ExceptionTracer
    {
        private static int _loggedCount;
        private const int MaxToLog = 40;

        internal static void Install()
        {
            AppDomain.CurrentDomain.FirstChanceException += OnFirstChanceException;
        }

        private static void OnFirstChanceException(object sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
        {
            // Skip the flood of already-diagnosed GetJoyRightStickY noise so anything
            // new is easy to spot in the log.
            if (e.Exception is MissingMethodException mme0 && mme0.Message.Contains("GetJoyRightStickY"))
                return;

            if (System.Threading.Interlocked.Increment(ref _loggedCount) <= MaxToLog)
            {
                Plugin.Log?.LogWarning(
                    $"[EpicLootFix] FirstChanceException: {e.Exception.GetType().FullName}: {e.Exception.Message}\n" +
                    $"TargetSite: {e.Exception.TargetSite}\n" +
                    $"StackTrace:\n{e.Exception.StackTrace}");
            }
        }
    }
}
