using System;
using System.Collections.Generic;
using System.Threading.Tasks;

sealed class PrioritySemaphore : IDisposable {
  public enum Priority {
    High,
    Normal,
    Low
  }

  /// <summary>
  /// The finished task
  /// </summary>
  readonly static Task CompletedTask = Task.FromResult<object>(null);

  /// <summary>
  /// The root object used for locking
  /// </summary>
  readonly object syncRoot;

  /// <summary>
  /// The wait queue for high priority
  /// </summary>
  readonly Queue<TaskCompletionSource<object>> waitersHigh;

  /// <summary>
  /// the wait queue for low priority
  /// </summary>
  readonly Queue<TaskCompletionSource<object>> waitersNormal;

  /// <summary>
  /// the wait queue for low priority
  /// </summary>
  readonly Queue<TaskCompletionSource<object>> waitersLow;

  /// <summary>
  /// The current resource count
  /// </summary>
  int currentCount;

  /// <summary>
  /// Make a new priority based semaphore
  /// </summary>
  /// <param name="initialCount">Initial amount of jobs in the queue</param>
  public PrioritySemaphore(int initialCount) {
    if (initialCount < 0)
      throw new ArgumentOutOfRangeException("initialCount");

    syncRoot = new object();
    waitersHigh = new Queue<TaskCompletionSource<object>>();
    waitersNormal = new Queue<TaskCompletionSource<object>>();
    waitersLow = new Queue<TaskCompletionSource<object>>();
    currentCount = initialCount;
  }

  /// <summary>
  /// Wait for a thread to open up at the given priority
  /// </summary>
  /// <param name="priority"></param>
  /// <returns></returns>
  public Task waitAsync(Priority priority) {
    lock (syncRoot) {
      if (currentCount > 0) {
        --currentCount;
        return CompletedTask;
      } else {
        var waiter = new TaskCompletionSource<object>();
        var waiters = default(Queue<TaskCompletionSource<object>>);
        switch (priority) {
          case Priority.High:
            waiters = waitersHigh;
            break;
          case Priority.Normal:
            waiters = waitersNormal;
            break;
          case Priority.Low:
            waiters = waitersLow;
            break;
          default:
            throw new ArgumentOutOfRangeException("unknown semaphore priority");
        }
        waiters.Enqueue(waiter);
        return waiter.Task;
      }
    }
  }

  /// <summary>
  /// Release the semaphore
  /// </summary>
  public void release() {
    TaskCompletionSource<object> toRelease = null;
    lock (syncRoot) {
      if (waitersHigh.Count > 0)
        toRelease = waitersHigh.Dequeue();
      else if (waitersNormal.Count > 0)
        toRelease = waitersNormal.Dequeue();
      else
        ++currentCount;
    }
    if (toRelease != null) {
      // separate task to avoid stack overflow on continuations
      Task.Factory.StartNew(o => (o as TaskCompletionSource<object>)
        .SetResult(null), toRelease, TaskCreationOptions.HideScheduler).Wait();
    }
  }

  /// <summary>
  /// for primative support
  /// </summary>
  public void Dispose() { }
}