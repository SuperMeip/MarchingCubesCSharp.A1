﻿using System.Collections;

/// <summary>
///  job to be executed in another thread
/// </summary>
public abstract class ThreadedJob : IThreadedJob {

  /// <summary>
  /// The job's handle
  /// </summary>
  private object handle = new object();

  /// <summary>
  /// The thread this job will run on
  /// </summary>
  protected System.Threading.Thread thread = null;

  /// <summary>
  /// The name to set as the thread name on start
  /// </summary>
  protected string threadName = "Job";

  /// <summary>
  /// if this job is done
  /// </summary>
  public bool isDone {
    get {
      bool tmp;
      lock (handle) {
        tmp = _isDone;
      }
      return tmp;
    }
    private set {
      lock (handle) {
        _isDone = value;
      }
    }
  }

  /// <summary>
  /// if this job is currently running
  /// </summary>
  public bool isRunning {
    get {
      bool tmp;
      lock (handle) {
        tmp = _isRunning;
      }
      return tmp;
    }
    private set {
      lock (handle) {
        _isRunning = value;
      }
    }
  }

  /// <summary>
  /// if the job has finished
  /// </summary>
  bool _isDone = false;

  /// <summary>
  /// if the job is currently running
  /// </summary>
  bool _isRunning = false;

  /// <summary>
  /// Start the job
  /// </summary>
  public void start() {
    thread = new System.Threading.Thread(run);
    thread.Name = threadName;
    thread.Start();
  }

  /// <summary>
  /// abort this job
  /// </summary>
  public void abort() {
    thread.Abort();
    isRunning = false;
  }

  /// <summary>
  /// Allows you to easily wait in a coroutine for the thread to finish. Just use:
  ///  yield return StartCoroutine(myJob.WaitFor());
  /// </summary>
  /// <returns></returns>
  public IEnumerator waitFor() {
    while (!isDone) {
      yield return null;
    }
  }

  /// <summary>
  /// The function to execute in another thread, you can't have unity stuff here.
  /// </summary>
  protected abstract void jobFunction();

  /// <summary>
  /// What to do on finished, you can have unity stuff here
  /// </summary>
  protected virtual void onFinished() { }

  /// <summary>
  /// threaded run function
  /// </summary>
  void run() {
    isRunning = true;
    jobFunction();
    isDone = true;
    // @TODO: make sure this works here:
    onFinished();
    isRunning = false;
  }
}