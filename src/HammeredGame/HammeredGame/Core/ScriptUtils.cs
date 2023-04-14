using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace HammeredGame.Core
{
    /// <summary>
    /// Utilities for waiting in game scripts. Scripts are C# code that can execute across multiple
    /// frames, for example to induce choreographed camera movements or show help UI. Update()
    /// should be called on every game update.
    ///
    /// The scheduled delays provided by this class make sure that the continuations run
    /// synchronously during an Update() call, unlike what happens when you use the native
    /// Task.Delay(), which causes the continuations to execute completely asynchronously and
    /// independent of the game loop. The latter is fine if the operation is not related to the
    /// game, but otherwise can cause unwanted game state changes during Draw() or during access
    /// from another Update().
    /// </summary>
    public class ScriptUtils
    {
        /// <summary>
        /// A ScheduledScript is a single script with a scheduled time to start executing at since
        /// creation. When the elapsed time since instantiation exceeds the scheduled time at any
        /// Update() call, the contained script is executed (its waiting function return value is
        /// set to null).
        /// </summary>
        private class ScheduledScript
        {
            private TimeSpan elapsedTime = TimeSpan.Zero;
            private readonly TimeSpan executionTime;
            public readonly TaskCompletionSource<object> Task;

            /// <summary>
            /// Create a new scheduled script, with the desired delay. With the executionTime
            /// parameter set to 0, the script will execute on the next call to Update(). Otherwise,
            /// on every Update() call the class will check if the delay has been exceeded, and
            /// execute the script if so. A script cannot execute more than once.
            /// </summary>
            /// <param name="executionTime"></param>
            /// <param name="task"></param>
            public ScheduledScript(TimeSpan executionTime, TaskCompletionSource<object> task)
            {
                this.executionTime = executionTime;
                this.Task = task;
            }

            public void Update(GameTime gameTime)
            {
                elapsedTime += gameTime.ElapsedGameTime;
                if (elapsedTime >= executionTime)
                {
                    // Set the return value for the task. This causes the continuation to start
                    // executing synchronously here. This is desired since we want synchronous
                    // execution of scripts that might change the game state, instead of it
                    // executing during another Update() computation or during Draw(). More details
                    // on the "asynchronous"-ness of
                    // TaskCompletionSources: https://devblogs.microsoft.com/premier-developer/the-danger-of-taskcompletionsourcet-class/
                    Task.SetResult(null);
                }
            }
        }

        /// <summary>
        /// The manager has access to a list of scheduled scripts that are active.
        /// </summary>
        private readonly List<ScheduledScript> scheduledScripts = new();

        /// <summary>
        /// On every game update, call Update() on all scheduled scripts. If any completed, then
        /// remove them from the list.
        /// </summary>
        /// <param name="gameTime"></param>
        public void Update(GameTime gameTime)
        {
            for (int i = scheduledScripts.Count - 1; i >= 0; i--)
            {
                scheduledScripts[i].Update(gameTime);

                if (scheduledScripts[i].Task.Task.IsCompleted)
                {
                    scheduledScripts.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Create a new await-able Task that waits for the specified seconds. The continuation code
        /// after this completes is executed synchronously within an Update() call in the future.
        /// </summary>
        /// <param name="secs"></param>
        /// <returns></returns>
        public Task WaitSeconds(int secs)
        {
            TaskCompletionSource<object> completionSource = new();
            scheduledScripts.Add(new ScheduledScript(TimeSpan.FromSeconds(secs), completionSource));
            return completionSource.Task;
        }

        /// <summary>
        /// Create a new await-able Task that waits for the next call to Update(). The continuation code
        /// after this completes is executed synchronously within an Update() call in the future.
        /// </summary>
        /// <returns></returns>
        public Task WaitNextUpdate()
        {
            TaskCompletionSource<object> completionSource = new();
            scheduledScripts.Add(new ScheduledScript(TimeSpan.FromMilliseconds(0), completionSource));
            return completionSource.Task;
        }

        /// <summary>
        /// Create a new await-able Task that waits for the specified milliseconds. The continuation code
        /// after this completes is executed synchronously within an Update() call in the future.
        /// </summary>
        /// <param name="milliSecs"></param>
        /// <returns></returns>
        public Task WaitMilliseconds(int milliSecs)
        {
            TaskCompletionSource<object> completionSource = new();
            scheduledScripts.Add(new ScheduledScript(TimeSpan.FromMilliseconds(milliSecs), completionSource));
            return completionSource.Task;
        }

        /// <summary>
        /// Create a new await-able Task that waits until the specified event property of an object
        /// is triggered, and then removes itself from the handler. Essentially, this changes an
        /// EventHandler that you have to use callbacks for, into an async/await pattern.
        /// </summary>
        /// <param name="sender">Object that has the event</param>
        /// <param name="eventName">Event property name, found using reflection</param>
        /// <param name="cancellationToken">Token to cancel the listener if any</param>
        /// <returns></returns>
        public Task<EventArgs> WaitEvent(object sender, string eventName, CancellationToken cancellationToken = default)
        {
            // Adapted from https://learn.microsoft.com/en-us/previous-versions/windows/silverlight/dotnet-windows-silverlight/ms228976(v=vs.95)
            TaskCompletionSource<EventArgs> completionSource = new();
            EventInfo target = sender.GetType().GetEvent(eventName);
            cancellationToken.Register(() => completionSource.SetCanceled());
            EventHandler handler = null;
            handler = (s, args) =>
            {
                completionSource.SetResult(args);
                target.RemoveEventHandler(sender, handler);
            };
            target.AddEventHandler(sender, handler);
            return completionSource.Task;
        }
    }
}
