using KLogger.Types;

namespace KLogger.Cores.Loggers
{
    /// <summary>
    ///     Logger의 상태 변경.
    /// </summary>
    internal partial class Logger
    {
        internal void Pause()
        {
            lock (_lock)
            {
                if (State != StateType.Start)
                {
                    return;
                }

                State = StateType.Pause;
            }

            Reporter.Fatal($"*Pause Logger!*\nInstanceID: `{InstanceID}`");
        }

        internal void Resume()
        {
            lock (_lock)
            {
                if (State != StateType.Pause)
                {
                    return;
                }

                State = StateType.Start;
            }

            Reporter.Fatal($"*Resume Logger!*\nInstanceID: `{InstanceID}`");
        }
    }
}
