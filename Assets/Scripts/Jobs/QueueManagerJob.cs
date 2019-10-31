using System.Collections.Generic;
using System.Threading;
using UnityEngine;

/// <summary>
/// A base job for managing chunk work queues
/// </summary>
public abstract class QueueManagerJob<QueueObjectType> : ThreadedJob {

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
    protected Semaphore parentResourcePool;

    /// <summary>
    /// If this job has been cancled
    public bool isCanceled {
      get;
      protected set;
    } = false;

    /// <summary>
    /// Make a new job
    /// </summary>
    /// <param name="parentResourcePool">The parent semaphore for resource tracking</param>
    internal ChildQueueJob(Semaphore parentResourcePool) {
      this.parentResourcePool = parentResourcePool;
    }

    /// <summary>
    /// Cancel the running job, this will abort it once it succesfully releases it's resources
    /// </summary>
    public void cancel() {
      isCanceled = true;
    }

    /// <summary>
    /// Do the actual work on the given chunk for this type of job
    /// </summary>
    protected abstract void doWork();

    /// <summary>
    /// Threaded function, serializes this chunks block data and removes it from the level
    /// </summary>
    protected override void jobFunction() {
      // wait until we have a resouces, or the job is canceled.
      if (parentResourcePool.WaitOne(-1, isCanceled)) {
        doWork();

        parentResourcePool.Release();
        // if the job is canceled, abort after releasing the resource
      } else {
        abort();
      }
    }
  }

  /// <summary>
  /// The resource pool for child jobs
  /// </summary>
  protected Semaphore childJobResourcePool;

  /// <summary>
  /// The queue this job is managing
  /// </summary>
  protected List<QueueObjectType> queue;

  /// <summary>
  /// The dictionary containing the running child jobs
  /// </summary>
  protected Dictionary<QueueObjectType, IChildQueueJob> runningChildJobs;

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
    runningChildJobs = new Dictionary<QueueObjectType, IChildQueueJob>();
    childJobResourcePool = new Semaphore(maxChildJobsCount, maxChildJobsCount);
  }

  /// <summary>
  /// Add a bunch of objects to the queue for processing
  /// </summary>
  /// <param name="queueObjects"></param>
  public void enQueue(QueueObjectType[] queueObjects) {
    foreach (QueueObjectType queueObject in queueObjects) {
      // if the chunk is already being loaded by a job, don't add it
      if (!runningChildJobs.ContainsKey(queueObject)) {
        IChildQueueJob chunkLoaderJob = getChildJob(queueObject);
        runningChildJobs[queueObject] = chunkLoaderJob;
        runningChildJobs[queueObject].start();
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
  public void deQueue(QueueObjectType[] queueObjects) {
    foreach (QueueObjectType queueObject in queueObjects) {
      if (runningChildJobs.ContainsKey(queueObject)) {
        IChildQueueJob job = runningChildJobs[queueObject];
        job.cancel();
      }
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
  protected abstract ChildQueueJob getChildJob(QueueObjectType queueObject);

  /// <summary>
  /// The threaded function to run
  /// </summary>
  protected override void jobFunction() {
    // Manage and remove finished jobs
    List<QueueObjectType> toRemove = new List<QueueObjectType>();
    while (runningChildJobs.Count > 0) {
      foreach (KeyValuePair<QueueObjectType, IChildQueueJob> runningJob in runningChildJobs) {
        if (runningJob.Value.isDone || runningJob.Value.isCanceled) {
          toRemove.Add(runningJob.Key);
        }
      }

      toRemove.ForEach(queueObject => {
        runningChildJobs.Remove(queueObject);
      });
    }
  }
}