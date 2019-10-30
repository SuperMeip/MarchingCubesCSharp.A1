using System.Collections.Generic;
using System.Threading;

/// <summary>
/// A base job for managing chunk work queues
/// </summary>
public abstract class QueueManagerJob<QueueObjectType> : ThreadedJob {

  /// <summary>
  /// An interface for a managed child queue job
  /// </summary>
  protected interface IChildQueueJob : IThreadedJob {

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
    /// </summary>
    protected bool isCanceled = false;

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
  protected Dictionary<QueueObjectType, IThreadedJob> runningChildJobs;

  /// <summary>
  /// Create a new job, linked to the level
  /// </summary>
  /// <param name="level"></param>
  protected QueueManagerJob(int maxChildJobsCount = 10) {
    queue = new List<QueueObjectType>();
    runningChildJobs = new Dictionary<QueueObjectType, IThreadedJob>();
    childJobResourcePool = new Semaphore(0, maxChildJobsCount);
  }

  /// <summary>
  /// Enqueue a column of chunks for management/loading/unloading
  /// </summary>
  /// <param name="queueObject"></param>
  public void enQueue(QueueObjectType queueObject) {
    queue.Add(queueObject);
  }

  /// <summary>
  /// if there's a child job running for the given chunks dequeue and abort it.
  /// </summary>
  /// <param name="queueObject"></param>
  public void deQueue(QueueObjectType queueObject) {
    queue.Remove(queueObject);
    if (runningChildJobs.ContainsKey(queueObject)) {
      ChildQueueJob job = (ChildQueueJob)runningChildJobs[queueObject];
      job.cancel();
      runningChildJobs.Remove(queueObject);
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

    // create a new load pipeline
    runningChildJobs = new Dictionary<QueueObjectType, IThreadedJob>();
    queue = new List<QueueObjectType>();
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
    // This job will continue until all queue'd chunks are loaded
    while (queue.Count > 0) {
      queue.RemoveAll((queueObject) => {
        // if the chunk is already being loaded by a job
        if (runningChildJobs.ContainsKey(queueObject)) {
          IThreadedJob chunkLoaderJob = runningChildJobs[queueObject];

          // if it's done, remove it from the running jobs and remove it from the queue
          if (chunkLoaderJob.isDone) {
            runningChildJobs.Remove(queueObject);
            return true;
          }

          // if it's not done yet, don't remove it
          return false;
          // if it's not being loaded yet by a job, and we have an open spot for a new job, start and add it
        } else {
          IThreadedJob chunkLoaderJob = getChildJob(queueObject);
          runningChildJobs[queueObject] = chunkLoaderJob;
          runningChildJobs[queueObject].start();

          // don't remove the running job from the queue yet
          return false;
        }
      });
    }
  }
}