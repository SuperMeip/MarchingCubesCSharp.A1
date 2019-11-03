using System.Collections.Generic;
using System.Threading;

/// <summary>
/// A base job for managing chunk work queues
/// </summary>
public abstract class QueueManagerJob<QueueItemType> : ThreadedJob {

  /// <summary>
  /// Child job for doing work on objects in the queue
  /// </summary>
  protected abstract class QueueTaskChildJob<ParentQueueItemType> : ThreadedJob {

    /// <summary>
    /// The queue item this job will do work on
    /// </summary>
    protected ParentQueueItemType queueItem;

    /// <summary>
    /// The cancelation sources for waiting jobs
    /// </summary>
    Dictionary<ParentQueueItemType, CancellationTokenSource> parentCancellationSources;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="queueItem"></param>
    /// <param name="parentCancellationSources"></param>
    internal QueueTaskChildJob(
      ParentQueueItemType queueItem,
      Dictionary<ParentQueueItemType, CancellationTokenSource> parentCancellationSources
    ) {
      this.queueItem = queueItem;
      this.parentCancellationSources = parentCancellationSources;
    }

    /// <summary>
    /// The do work function
    /// </summary>
    /// <param name="queueItem"></param>
    /// <param name="cancellationToken"></param>
    protected abstract void doWork(ParentQueueItemType queueItem, CancellationToken cancellationToken);

    /// <summary>
    /// Threaded function
    /// </summary>
    protected override void jobFunction() {
      doWork(queueItem, parentCancellationSources[queueItem].Token);
    }

    /// <summary>
    /// On job complete, remove from parent
    /// </summary>
    protected override void finallyDo() {
      if (parentCancellationSources.ContainsKey(queueItem)) {
        parentCancellationSources.Remove(queueItem);
      }
    }
  }

  /// <summary>
  /// The queue this job is managing
  /// </summary>
  protected List<QueueItemType> queue;

  /// <summary>
  /// The cancelation sources for waiting jobs
  /// </summary>
  protected Dictionary<QueueItemType, CancellationTokenSource> cancelationSources;

  /// <summary>
  /// The max number of child jobs allowed
  /// </summary>
  int maxChildJobsCount;

  /// <summary>
  /// Create a new job, linked to the level
  /// </summary>
  /// <param name="level"></param>
  protected QueueManagerJob(int maxChildJobsCount = 10) {
    this.maxChildJobsCount = maxChildJobsCount;
    queue                  = new List<QueueItemType>();
    cancelationSources     = new Dictionary<QueueItemType, CancellationTokenSource>();
  }

  /// <summary>
  /// Get the type of job we're managing in this queue
  /// </summary>
  /// <returns></returns>
  protected abstract QueueTaskChildJob<QueueItemType> getChildJob(
    QueueItemType queueObject,
    Dictionary<QueueItemType, CancellationTokenSource> parentCancelationSources
  );

  /// <summary>
  /// Add a bunch of objects to the queue for processing
  /// </summary>
  /// <param name="queueObjects"></param>
  public void enQueue(QueueItemType[] queueObjects) {
    foreach (QueueItemType queueObject in queueObjects) {
      // if the chunk is already being loaded by a job, don't add it
      if (!cancelationSources.ContainsKey(queueObject) && !queue.Contains(queueObject)) {
        queue.Add(queueObject);
      }
    }

    // if the queue manager job isn't running, start it
    if (!isRunning) {
      start();
    }
  }

  /// <summary>
  /// if there's any child jobs running for the given ojects, stop them and dequeue
  /// </summary>
  /// <param name="queueObject"></param>
  public void deQueue(QueueItemType[] queueObjects) {
    foreach (QueueItemType queueObject in queueObjects) {
      if (queue.Contains(queueObject)) {
        queue.Remove(queueObject);
      }
      if (cancelationSources.ContainsKey(queueObject)) {
        cancelationSources[queueObject].Cancel();
      }
    }
  }

  /// <summary>
  /// The threaded function to run
  /// </summary>
  protected override void jobFunction() {
    // run while we have a queue
    while (queue.Count > 0) {
      // validate
      if (!isAValidQueueItem(queue[0])) {
        queue.Remove(queue[0]);
        continue;
      }

      // if we have space, pop off the top of the queue and run it as a job.
      if (cancelationSources.Count < maxChildJobsCount) {
        QueueItemType queueItem = queue.Pop();
        CancellationTokenSource cancelationToken = new CancellationTokenSource();
        cancelationSources.Add(queueItem, cancelationToken);
        // The child job is responsible for removing itself from the sources dictionary when done 
        //    see QueueTaskChildJob.finallyDo()
        getChildJob(queueItem, cancelationSources).start();
      }

      // @TODO: sort the queue here by priority
    }
  }

  /// <summary>
  /// validate queue items
  /// </summary>
  /// <param name="queueItem"></param>
  /// <returns></returns>
  protected virtual bool isAValidQueueItem(QueueItemType queueItem) {
    return queueItem != null;
  }
}

/// <summary>
/// Add pop to lists
/// </summary>
static class ListExtension {
  public static T Pop<T>(this List<T> list) {
    T r = list[0];
    list.RemoveAt(0);
    return r;
  }
}