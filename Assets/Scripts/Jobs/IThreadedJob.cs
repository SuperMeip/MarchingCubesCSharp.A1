using System.Collections;

/// <summary>
/// Interface for jobs run on other threads
/// </summary>
public interface IThreadedJob {

  /// <summary>
  /// Get if the job is done running
  /// </summary>
  bool isDone {
    get;
  }

  /// <summary>
  /// Get if the job is currently running
  /// </summary>
  bool isRunning {
    get;
  }

  /// <summary>
  /// Start the job
  /// </summary>
  void start();

  /// <summary>
  /// A function to run the job syncronously, can be used as the job's 'task'
  /// </summary>
  void task();

  /// <summary>
  /// Allows you to easily wait in a coroutine for the thread to finish. Just use:
  ///  yield return StartCoroutine(myJob.WaitFor());
  /// </summary>
  /// <returns></returns>
  IEnumerator waitFor();

  /// <summary>
  /// Abort the job
  /// </summary>
  void abort();
}