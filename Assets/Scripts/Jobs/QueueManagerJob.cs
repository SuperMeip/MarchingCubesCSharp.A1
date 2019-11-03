using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

/// <summary>
/// A base job for managing chunk work queues
/// </summary>
public abstract class QueueManagerJob<QueueItemType> : ThreadedJob {

  /// <summary>
  /// An interface for a managed child queue job
  /// </summary>
  protected interface IChildQueueJob : IThreadedJob {

    /// <summary>
    /// If this job has been cancled
    bool isCanceled {
      get;
    }

    /// <summary>
    /// cancel a job and free the resources
    /// </summary>
    void cancel();
  }

  /// <summary>
  /// Base class for child jobs that manage chunk loading and unloading
  /// </summary>
  protected abstract class ChildQueueJob : ThreadedJob, IChildQueueJob {

    /// <summary>
    /// The parent resource pool managing this job
    /// </summary>
    protected SemaphoreSlim parentResourcePool;

    /// <summary>
    /// The cancel token this job is waiting on
    /// </summary>
    CancellationTokenSource cancelSource;

    /// <summary>
    /// If this job has been cancled
    public bool isCanceled {
      get;
      protected set;
    } = false;

    /// <summary>
    /// Make a new job
    /// </summary>
    /// <param name="parentResourcePool">The parent SemaphoreSlim for resource tracking</param>
    internal ChildQueueJob(SemaphoreSlim parentResourcePool, CancellationTokenSource cancelSource) {
      this.parentResourcePool = parentResourcePool;
      this.cancelSource = cancelSource;
    }

    /// <summary>
    /// Cancel the running job, this will abort it once it succesfully releases it's resources
    /// </summary>
    public void cancel() {
      isCanceled = true;
      cancelSource.Cancel();
    }

    /// <summary>
    /// Do the actual work on the given chunk for this type of job
    /// </summary>
    protected abstract void doWork();

    /// <summary>
    /// Threaded function, serializes this chunks block data and removes it from the level
    /// </summary>
    protected override void jobFunction() {
      try {
        parentResourcePool.Wait(cancelSource.Token);
        doWork();
      } finally {
        parentResourcePool.Release();
      }
    }
  }

  /// <summary>
  /// The resource pool for child jobs
  /// </summary>
  protected SemaphoreSlim childJobResourcePool;

  /// <summary>
  /// The queue this job is managing
  /// </summary>
  protected List<QueueItemType> queue;

  /// <summary>
  /// The cancelation sources for waiting jobs
  /// </summary>
  protected Dictionary<QueueItemType, CancellationTokenSource> cancelationSources;

  /// <summary>
  /// The dictionary containing the running child jobs
  /// </summary>
  protected Dictionary<QueueItemType, IChildQueueJob> runningChildJobs;

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
    runningChildJobs       = new Dictionary<QueueItemType, IChildQueueJob>();
    queue                  = new List<QueueItemType>();
    cancelationSources     = new Dictionary<QueueItemType, CancellationTokenSource>();
    childJobResourcePool   = new SemaphoreSlim(maxChildJobsCount, maxChildJobsCount);
  }

  /// <summary>
  /// Add a bunch of objects to the queue for processing
  /// </summary>
  /// <param name="queueObjects"></param>
  public void enQueue(QueueItemType[] queueObjects) {
    foreach (QueueItemType queueObject in queueObjects) {
      // if the chunk is already being loaded by a job, don't add it
      if (!runningChildJobs.ContainsKey(queueObject) && !queue.Contains(queueObject)) {
        queue.Add(queueObject);
      }
    }

    // if the queue manager job isn't running, start it
    if (!isRunning) {
      start();
    }
  }

  /// <summary>
  /// if there's a child job running for the given chunks dequeue and abort it.
  /// </summary>
  /// <param name="queueObject"></param>
  public void deQueue(QueueItemType[] queueObjects) {
    foreach (QueueItemType queueObject in queueObjects) {
      cancelChildJobWait(queueObject);
      if (runningChildJobs.ContainsKey(queueObject)) {
        IChildQueueJob job = runningChildJobs[queueObject];
        job.cancel();
      }
    }
  }

  /// <summary>
  /// Cancel any job that may be waiting to start
  /// </summary>
  /// <param name="queueObject"></param>
  public void cancelChildJobWait(QueueItemType queueObject) {
    if (cancelationSources.ContainsKey(queueObject)) {
      cancelationSources[queueObject].Cancel();
    }
  }

  /// <summary>
  /// Clear all the currently running jobs
  /// </summary>
  public void clearRunningJobs() {
    if (runningChildJobs.Count >= 1) {
      foreach (ChildQueueJob job in runningChildJobs.Values) {
        job.cancel();
      }
    }
  }

  /// <summary>
  /// Get the type of job we're managing in this queue
  /// </summary>
  /// <returns></returns>
  protected abstract ChildQueueJob getChildJob(QueueItemType queueObject, CancellationTokenSource cancelSource);

  /// <summary>
  /// The threaded function to run
  /// </summary>
  protected override void jobFunction() {
    while (queue.Count > 0) {
      queue.RemoveAll((queueObject) => {
        // if the chunk is already being loaded by a job
        if (runningChildJobs.ContainsKey(queueObject)) {
          IChildQueueJob chunkLoaderJob = runningChildJobs[queueObject];

          // if it's done, remove it from the running jobs and remove it from the queue
          if (chunkLoaderJob.isDone || chunkLoaderJob.isCanceled) {
            runningChildJobs.Remove(queueObject);
            cancelationSources.Remove(queueObject);
            return true;
          }

          // if it's not done yet, don't remove it
          return false;
        // if it's not being loaded yet by a job, and we have an open spot for a new job, start and add it
        } else {
          CancellationTokenSource cancelSource = new CancellationTokenSource();
          cancelationSources.Add(queueObject, cancelSource);
          IChildQueueJob chunkLoaderJob = getChildJob(queueObject, cancelSource);
          runningChildJobs[queueObject] = chunkLoaderJob;
          runningChildJobs[queueObject].start();

          // don't remove the running job from the queue yet
          return false;
        }
      });
    }
  }
}