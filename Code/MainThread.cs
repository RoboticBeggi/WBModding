using System;
using System.Collections.Concurrent;

namespace TeyvatChronicle
{
    /// <summary>
    /// Game hooks (trait attack actions, trait ticks) may run inside the simulation's job system.
    /// Anything touching Unity objects is queued here and run from the mod's Update on the main thread.
    /// </summary>
    public static class MainThread
    {
        private const int MaxQueued = 400;
        private const int MaxPerFrame = 80;
        private static readonly ConcurrentQueue<Action> _queue = new ConcurrentQueue<Action>();

        public static void Enqueue(Action pAction)
        {
            if (_queue.Count < MaxQueued) _queue.Enqueue(pAction);
        }

        public static void Drain()
        {
            for (int i = 0; i < MaxPerFrame && _queue.TryDequeue(out Action action); i++)
            {
                try
                {
                    action();
                }
                catch (Exception e)
                {
                    ModClass.LogWarning("[Teyvat] " + e.Message);
                }
            }
        }
    }
}
