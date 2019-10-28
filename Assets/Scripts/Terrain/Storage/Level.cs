﻿using System.Collections.Generic;
using System.IO;
using System;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using System.Threading;

/// <summary>
/// A collection of chunks, making an enclosed world in game
/// </summary>
public class Level<ChunkType> where ChunkType : BlockStorage {

  /// <summary>
  /// The block diameter, x y and z, of a chunk in this level
  /// </summary>
  const int ChunkDiameter = 64;

  /// <summary>
  /// The maximum number of chunk load jobs that can run for one queue manager simultaniously
  /// </summary>
  const int MaxChunkLoadingJobsCount = 10;

  /// <summary>
  /// The width of the active chunk area in chunks
  /// </summary>
  const int LoadedChunkDiameter = 24;

  /// <summary>
  /// The height of the active chunk area in chunks
  /// </summary>
  int LoadedChunkHeight {
    get => chunkBounds.y;
  }

  /// <summary>
  /// The coordinates indicating the two chunks the extreems of what columns are loaded from memmory:
  ///   0: southwest most loaded chunk column
  ///   1: northeast most loaded chunk column
  /// </summary>
  Coordinate[] loadedChunkBounds;

  /// <summary>
  /// The current center of all loaded chunks, usually based on player location
  /// </summary>
  Coordinate loadedChunkFocus;

  /// <summary>
  /// The active chunks, stored by coordinate location
  /// </summary>
  Dictionary<Coordinate, ChunkType> loadedChunks;

  /// <summary>
  /// The columns of chunks already loaded/being loaded
  /// @TODO: impliment this
  /// </summary>
  HashSet<Coordinate> loadedChunkColumns;

  /// <summary>
  /// The current parent job, in charge of loading the chunks in the load queue
  /// </summary>
  JLoadChunks chunkLoadQueueManagerJob;

  /// <summary>
  /// The current parent job, in charge of loading the chunks in the load queue
  /// </summary>
  JUnloadChunks chunkUnloadQueueManagerJob;

  /// <summary>
  /// The source used to load blocks for new chunks in this level
  /// </summary>
  IBlockSource blockSource;

  /// <summary>
  /// The level seed
  /// </summary>
  int seed;

  /// <summary>
  /// The overall bounds of the level, max x y and z
  /// </summary>
  public Coordinate chunkBounds {
    get;
    protected set;
  }

  /// <summary>
  /// Create a new level
  /// </summary>
  /// <param name="seed"></param>
  /// <param name="chunkBounds">the max x y and z chunk sizes of the world</param>
  public Level(int seed, Coordinate chunkBounds) {
    this.chunkBounds           = chunkBounds;
    this.seed                  = seed;
    chunkLoadQueueManagerJob   = new JLoadChunks(this);
    chunkUnloadQueueManagerJob = new JUnloadChunks(this);
    loadedChunkColumns         = new HashSet<Coordinate>();
    blockSource                = new WaveSource(seed);
    loadedChunks               = new Dictionary<Coordinate, ChunkType>(
      chunkBounds.x * chunkBounds.y * chunkBounds.z
    );
  }

  /// <summary>
  /// initialize this level with the center of loaded chunks fouced on the given location
  /// </summary>
  /// <param name="centerChunkLocation">the center point/focus of the loaded chunks, usually a player location</param>
  public void initializeAround(Coordinate centerChunkLocation) {
    loadChunksAround(centerChunkLocation);
  }

  /// <summary>
  /// Move the focus/central loaded point of the level
  /// </summary>
  /// <param name="direction">the direction the focus has moved</param>
  /// <param name="magnitude">the number of chunks in the direction that the foucs moved</param>
  public void adjustFocus(Directions.Direction direction) {
    List<Coordinate> chunksToLoad   = new List<Coordinate>();
    List<Coordinate> chunksToUnload = new List<Coordinate>();

    if (Array.IndexOf(Directions.Cardinal, direction) > -1) {
      // add new chunks to the load queue in the given direction
      // NS
      if (direction.Value == Directions.North.Value || direction.Value == Directions.South.Value) {
        // grab the chunks one to the direction of the current loaded ones
        for (int i = 0; i < LoadedChunkDiameter; i++) {
          // the z comes from the extreem bound, either the northern or southern one, y is 0
          Coordinate chunkToLoad = loadedChunkBounds[direction.Value == Directions.North.Value ? 1 : 0] + direction.Offset;
          // the x is calculated from the SW corner's W, plust the current i
          chunkToLoad.x = i + loadedChunkBounds[1].x - LoadedChunkDiameter;

          // calculate the chunk to unload on the opposite side
          Coordinate chunkToUnload = loadedChunkBounds[direction.Value == Directions.North.Value ? 0 : 1] + direction.Offset;
          chunkToUnload.x = i + loadedChunkBounds[1].x - LoadedChunkDiameter;

          // add the values
          chunksToLoad.Add(chunkToLoad);
          chunksToUnload.Add(chunkToUnload);
        }
      // EW
      } else {
        for (int i = 0; i < LoadedChunkDiameter; i++) {
          Coordinate chunkToLoad = loadedChunkBounds[direction.Value == Directions.East.Value ? 1 : 0] + direction.Offset;
          chunkToLoad.z = i + loadedChunkBounds[0].z;

          Coordinate chunkToUnload = loadedChunkBounds[direction.Value == Directions.East.Value ? 0 : 1] + direction.Offset;
          chunkToUnload.z = i + loadedChunkBounds[0].z;

          chunksToLoad.Add(chunkToLoad);
          chunksToUnload.Add(chunkToUnload);
        }
      }
    }

    // queue the collected values
    addChunkColumnsToLoadingQueue(chunksToLoad.ToArray());
    addChunkColumnsToUnloadingQueue(chunksToUnload.ToArray());
  }

  /// <summary>
  /// Get the chunk at the given location (if it's loaded)
  /// </summary>
  /// <param name="location"></param>
  public ChunkType getChunk(Coordinate chunkLocation) {
    return chunkLocation.isWithin(chunkBounds)
      && chunkIsWithinLoadedBounds(chunkLocation)
      && loadedChunks.ContainsKey(chunkLocation)
        ? loadedChunks[chunkLocation]
        : null;
  }

  /// <summary>
  /// Re-load the active chunks around the given XZ chunk location
  /// </summary>
  /// <param name="location">the X,Z chunk location to load around</param>
  void loadChunksAround(Coordinate centerChunkLocation) {
    loadedChunkFocus = centerChunkLocation;
    loadedChunkBounds = getLoadedChunkBounds(loadedChunkFocus);
    Coordinate[] chunksToLoad = Coordinate.GetAllPointsBetween(loadedChunkBounds[0], loadedChunkBounds[1]);
    addChunkColumnsToLoadingQueue(chunksToLoad);
  }

  /// <summary>
  /// Add multiple chunk column locations to the load queue and run it
  /// </summary>
  /// <param name="chunkLocations">the x,z values of the chunk columns to load</param>
  void addChunkColumnsToLoadingQueue(Coordinate[] chunkLocations) {
    foreach (Coordinate chunkLocation in chunkLocations) {
      addChunkColumnToLoadingQueue(chunkLocation.xz);
    }

    // if the load queue manager job isn't running, start it
    if (!chunkLoadQueueManagerJob.isRunning) {
      chunkLoadQueueManagerJob.start();
    }
  }

  /// <summary>
  /// Add multiple chunk column locations to the unload queue and run it
  /// </summary>
  /// <param name="chunkLocations">the x,z values of the chunk columns to unload</param>
  void addChunkColumnsToUnloadingQueue(Coordinate[] chunkLocations) {
    foreach (Coordinate chunkLocation in chunkLocations) {
      addChunkColumnToUnloadingQueue(chunkLocation.xz);
    }

    // if the unload queue manager job isn't running, start it
    if (!chunkUnloadQueueManagerJob.isRunning) {
      chunkUnloadQueueManagerJob.start();
    }
  }

  /// <summary>
  /// Clear chunks that are currently loaded from memory and stop running load jobs
  /// </summary>
  void clearLoadedChunks() {
    // abort the load job and abort all child jobs
    chunkLoadQueueManagerJob.clearRunningJobs();

    // Add all exstant chunks to the unload queue
    Coordinate[] chunksToUnload = new Coordinate[loadedChunkColumns.Count];
    loadedChunkColumns.CopyTo(chunksToUnload, 0);
    addChunkColumnsToUnloadingQueue(chunksToUnload);
  }

  /// <summary>
  /// Get the loaded chunk bounds for a given center point.
  /// Always trims to X,0,Z
  /// </summary>
  /// <param name="centerLocation"></param>
  Coordinate[] getLoadedChunkBounds(Coordinate centerLocation) {
    return new Coordinate[] {
      (
        Mathf.Max(centerLocation.x - LoadedChunkDiameter / 2, 0),
        0,
        Mathf.Max(centerLocation.z - LoadedChunkDiameter / 2, 0)
      ),
      (
        Mathf.Min(centerLocation.x + LoadedChunkDiameter / 2, chunkBounds.x - 1),
        0,
        Mathf.Min(centerLocation.z + LoadedChunkDiameter / 2, chunkBounds.z - 1)
      )
    };
  }

  /// <summary>
  /// Only to be used by jobs
  /// Save a chunk to file
  /// </summary>
  /// <param name="chunkLocation"></param>
  void saveChunkToFile(Coordinate chunkLocation) {
    ChunkType chunkData = getChunk(chunkLocation);
    if (chunkData != null) {
      IFormatter formatter = new BinaryFormatter();
      Stream stream = new FileStream(getChunkFileName(chunkLocation), FileMode.Create, FileAccess.Write, FileShare.None);
      formatter.Serialize(stream, chunkData);
      stream.Close();
    }
  }

  /// <summary>
  /// Get the blockdata for a chunk location from file
  /// </summary>
  /// <param name="chunkLocation"></param>
  /// <returns></returns>
  ChunkType loadChunkDataFromFile(Coordinate chunkLocation) {
    IFormatter formatter = new BinaryFormatter();
    Stream readStream = new FileStream(getChunkFileName(chunkLocation), FileMode.Open, FileAccess.Read, FileShare.Read);
    ChunkType chunkData = (ChunkType)formatter.Deserialize(readStream);
    readStream.Close();

    return chunkData;
  }

  /// <summary>
  /// Generate the chunk data for the chunk at the given location
  /// </summary>
  /// <param name="chunkLocation"></param>
  ChunkType generateChunkData(Coordinate chunkLocation) {
    ChunkType chunkData = (ChunkType)Activator.CreateInstance(typeof(ChunkType), ChunkDiameter);
    blockSource.generateAllAt(chunkLocation, chunkData);

    return chunkData;
  }

  /// <summary>
  /// Get the file name a chunk is saved to based on it's location
  /// </summary>
  /// <param name="chunkLocation">the location of the chunk</param>
  /// <returns></returns>
  string getChunkFileName(Coordinate chunkLocation) {
    return Application.persistentDataPath + "/" + seed + "/" + chunkLocation.ToString() + ".evxch";
  }

  /// <summary>
  /// Add the chunk to the queue of chunks to be loaded
  /// please make sure y = 0
  /// </summary>
  /// <param name="chunkColumn">Make sure y = 0</param>
  void addChunkColumnToLoadingQueue(Coordinate chunkColumn) {
    if (!loadedChunkColumns.Contains(chunkColumn) && chunkColumn.y == 0) {
      loadedChunkColumns.Add(chunkColumn);
      chunkUnloadQueueManagerJob.deQueue(chunkColumn);
      chunkLoadQueueManagerJob.enQueue(chunkColumn);
    }
  }

  /// <summary>
  /// Add the chunk to the queue of chunks to be unloaded
  /// </summary>
  /// <param name="chunkColumn">make sure y = 0</param>
  void addChunkColumnToUnloadingQueue(Coordinate chunkColumn) {
    if (loadedChunkColumns.Contains(chunkColumn) && chunkColumn.y == 0) {
      chunkUnloadQueueManagerJob.enQueue(chunkColumn);
      loadedChunkColumns.Remove(chunkColumn);
    }
  }

  /// <summary>
  /// Get if the given chunkLocation is loaded
  /// </summary>
  /// <param name="chunkLocation"></param>
  /// <returns></returns>
  bool chunkIsWithinLoadedBounds(Coordinate chunkLocation) {
    return chunkLocation.isWithin(loadedChunkBounds[1]) && chunkLocation.isBeyond(loadedChunkBounds[0]);
  }

  /// <summary>
  /// A job to load all chunks from the loading queue
  /// </summary>
  class JLoadChunks : LevelQueueManagerJob {

    /// <summary>
    /// A Job for generating a new column of chunks into a level
    /// @TODO: update this to load the entire Y column of chunks, make a base job that runs a function once for each y chunk.
    /// </summary>
    class JGenerateChunkColumn : ChunkColumnLoadingJob {

      /// <summary>
      /// Make a new job
      /// </summary>
      /// <param name="level"></param>
      /// <param name="chunkLocation"></param>
      /// <param name="resourcePool"></param>
      internal JGenerateChunkColumn(Level<ChunkType> level, Coordinate chunkLocation, Semaphore resourcePool)
        : base(level, chunkLocation, resourcePool) { }

      /// <summary>
      /// Threaded function, loads all the block data for this chunk
      /// </summary>
      protected override void doWorkOnChunk(Coordinate chunkLocation) {
        if (level.getChunk(chunkLocation) == null) {
          ChunkType chunkData = level.generateChunkData(chunkLocation);
          level.loadedChunks[chunkLocation] = chunkData;
        }
      }
    }

    /// <summary>
    /// A Job for loading the data for a column of chunks into a level from file
    /// @TODO: update this to load the entire Y column of chunks, make a base job that runs a function once for each y chunk.
    /// </summary>
    class JLoadChunkColumnFromFile : ChunkColumnLoadingJob {

      /// <summary>
      /// Make a new job
      /// </summary>
      /// <param name="level"></param>
      /// <param name="chunkLocation"></param>
      /// <param name="resourcePool"></param>
      internal JLoadChunkColumnFromFile(Level<ChunkType> level, Coordinate chunkLocation, Semaphore resourcePool)
        : base(level, chunkLocation, resourcePool) { }

      /// <summary>
      /// Threaded function, loads all the block data for this chunk
      /// </summary>
      protected override void doWorkOnChunk(Coordinate chunkLocation) {
        if (level.getChunk(chunkLocation) == null) {
          ChunkType chunkData = level.loadChunkDataFromFile(chunkLocation);
          level.loadedChunks[chunkLocation] = chunkData;
        }
      }
    }

    /// <summary>
    /// A seperate resource pool for generating chunks
    /// </summary>
    Semaphore chunkGenerationResourcePool;

    /// <summary>
    /// Create a new job, linked to the level
    /// </summary>
    /// <param name="level"></param>
    public JLoadChunks(Level<ChunkType> level) : base(level) {
      chunkGenerationResourcePool = new Semaphore(0, MaxChunkLoadingJobsCount);
    }

    /// <summary>
    /// Make a chunk loader job
    /// </summary>
    /// <param name="level"></param>
    /// <param name="chunkLocation"></param>
    /// <returns></returns>
    protected override ChunkColumnLoadingJob getChildJob(Level<ChunkType> level, Coordinate chunkLocation) {
      // create two queues for these
      if (File.Exists(level.getChunkFileName(chunkLocation))) { 
        return new JLoadChunkColumnFromFile(level, chunkLocation, childJobResourcePool);
      }

      return new JGenerateChunkColumn(level, chunkLocation, chunkGenerationResourcePool);
    }
  }

  /// <summary>
  /// A job to un-load and serialize all chunks from the unloading queue
  /// </summary>
  class JUnloadChunks : LevelQueueManagerJob {

    /// <summary>
    /// A Job for un-loading the data for a column of chunks into a serialized file
    /// </summary>
    class JUnloadChunkColumn : ChunkColumnLoadingJob {
      /// <summary>
      /// Make a new job
      /// </summary>
      /// <param name="level"></param>
      /// <param name="chunkLocation"></param>
      /// <param name="resourcePool"></param>
      internal JUnloadChunkColumn(Level<ChunkType> level, Coordinate chunkLocation, Semaphore resourcePool)
        : base(level, chunkLocation, resourcePool) { }

      /// <summary>
      /// Threaded function, serializes this chunks block data and removes it from the level
      /// </summary>
      protected override void doWorkOnChunk(Coordinate chunkLocation) {
        level.saveChunkToFile(chunkLocation);
        level.loadedChunks.Remove(chunkLocation);
      }
    }

    /// <summary>
    /// Create a new job, linked to the level
    /// </summary>
    /// <param name="level"></param>
    public JUnloadChunks(Level<ChunkType> level) : base(level) {}

    /// <summary>
    /// Make a new unload chunk job
    /// </summary>
    /// <param name="level"></param>
    /// <param name="chunkLocation"></param>
    /// <returns></returns>
    protected override ChunkColumnLoadingJob getChildJob(Level<ChunkType> level, Coordinate chunkLocation) {
      return new JUnloadChunkColumn(level, chunkLocation, childJobResourcePool);
    }
  }

  /// <summary>
  /// A base job for managing chunk work queues
  /// </summary>
  abstract class LevelQueueManagerJob : ThreadedJob {

    /// <summary>
    /// Base class for child jobs that manage chunk loading and unloading
    /// </summary>
    protected abstract class ChunkColumnLoadingJob : ThreadedJob {

      /// <summary>
      /// The level we're loading for
      /// </summary>
      protected Level<ChunkType> level;

      /// <summary>
      /// The location of the chunk that this job is loading.
      /// </summary>
      protected Coordinate chunkColumnLocation;

      /// <summary>
      /// The resource pool managing this job
      /// </summary>
      protected Semaphore resourcePool;

      /// <summary>
      /// If we have a resource that is provisioned and needs to be released
      /// </summary>
      bool resourceIsProvisioned = false;

      /// <summary>
      /// Make a new job
      /// </summary>
      /// <param name="level"></param>
      /// <param name="chunkColumnLocation"></param>
      protected ChunkColumnLoadingJob(Level<ChunkType> level, Coordinate chunkColumnLocation, Semaphore resourcePool) {
        this.level = level;
        this.chunkColumnLocation = chunkColumnLocation;
        this.resourcePool = resourcePool;
      }

      /// <summary>
      /// Do the actual work on the given chunk for this type of job
      /// </summary>
      protected abstract void doWorkOnChunk(Coordinate chunkLocation);

      /// <summary>
      /// Threaded function, serializes this chunks block data and removes it from the level
      /// </summary>
      protected override void jobFunction() {
        if (resourcePool.WaitOne()) {
          resourceIsProvisioned = true;
          Coordinate columnTop = (chunkColumnLocation.x, level.chunkBounds.y, chunkColumnLocation.z);
          Coordinate columnBottom = (chunkColumnLocation.x, 0, chunkColumnLocation.z);
          columnBottom.until(columnTop, chunkLocation => {
            doWorkOnChunk(chunkLocation);
          });
        }

        resourcePool.Release();
        resourceIsProvisioned = false;
      }

      /// <summary>
      /// If we abort, make sure we release the resource
      /// </summary>
      protected override void onAborted() {
        if (resourceIsProvisioned) {
          resourcePool.Release();
        }

        base.onAborted();
      }
    }

    /// <summary>
    /// The level we're loading for
    /// </summary>
    Level<ChunkType> level;

    /// <summary>
    /// The resource pool for child jobs
    /// </summary>
    protected Semaphore childJobResourcePool;

    /// <summary>
    /// The queue this job is managing
    /// </summary>
    protected List<Coordinate> queue;

    /// <summary>
    /// The dictionary containing the running child jobs
    /// </summary>
    protected Dictionary<Coordinate, IThreadedJob> runningChildJobs;

    /// <summary>
    /// Create a new job, linked to the level
    /// </summary>
    /// <param name="level"></param>
    protected LevelQueueManagerJob(Level<ChunkType> level) {
      this.level           = level;
      queue                = new List<Coordinate>();
      runningChildJobs     = new Dictionary<Coordinate, IThreadedJob>();
      childJobResourcePool = new Semaphore(0, MaxChunkLoadingJobsCount);
    }

    /// <summary>
    /// Enqueue a column of chunks for management/loading/unloading
    /// </summary>
    /// <param name="chunkColumnLocation"></param>
    public void enQueue(Coordinate chunkColumnLocation) {
      queue.Add(chunkColumnLocation);
    }

    /// <summary>
    /// if there's a child job running for the given chunks dequeue and abort it.
    /// </summary>
    /// <param name="chunkColumnLocation"></param>
    public void deQueue(Coordinate chunkColumnLocation) {
      queue.Remove(chunkColumnLocation);
      if (runningChildJobs.ContainsKey(chunkColumnLocation)) {
        runningChildJobs[chunkColumnLocation].abort();
        runningChildJobs.Remove(chunkColumnLocation);
      }
    }

    /// <summary>
    /// Clear all the currently running jobs
    /// </summary>
    public void clearRunningJobs() {
      if (runningChildJobs.Count >= 1) {
        foreach (IThreadedJob job in runningChildJobs.Values) {
          job.abort();
        }
      }

      // create a new load pipeline
      runningChildJobs = new Dictionary<Coordinate, IThreadedJob>();
      queue            = new List<Coordinate>();
    }

    /// <summary>
    /// Get the type of job we're managing in this queue
    /// </summary>
    /// <returns></returns>
    protected abstract ChunkColumnLoadingJob getChildJob(Level<ChunkType> level, Coordinate chunkLocation);

    /// <summary>
    /// The threaded function to run
    /// </summary>
    protected override void jobFunction() {
      // This job will continue until all queue'd chunks are loaded
      while (queue.Count > 0) {
        queue.RemoveAll((chunkLocation) => {
          // if the chunk is already being loaded by a job
          if (runningChildJobs.ContainsKey(chunkLocation)) {
            IThreadedJob chunkLoaderJob = runningChildJobs[chunkLocation];

            // if it's done, remove it from the running jobs and remove it from the queue
            if (chunkLoaderJob.isDone) {
              runningChildJobs.Remove(chunkLocation);
              return true;
            }

            // if it's not done yet, don't remove it
            return false;
            // if it's not being loaded yet by a job, and we have an open spot for a new job, start and add it
          } else {
            IThreadedJob chunkLoaderJob = getChildJob(level, chunkLocation);
            runningChildJobs[chunkLocation] = chunkLoaderJob;
            runningChildJobs[chunkLocation].start();

            // don't remove the running job from the queue yet
            return false;
          }
        });
      }
    }
  }
}
