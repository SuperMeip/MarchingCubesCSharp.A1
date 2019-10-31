using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Threading;

/// <summary>
/// A type of level loaded column by column
/// </summary>
/// <typeparam name="ChunkType"></typeparam>
public class ColumnLoadedLevel<ChunkType> : HashedChunkLevel<ChunkType> where ChunkType : IBlockStorage {

  /// <summary>
  /// The maximum number of chunk load jobs that can run for one queue manager simultaniously
  /// </summary>
  const int MaxChunkLoadingJobsCount = 10;

  /// <summary>
  /// The current parent job, in charge of loading the chunks in the load queue
  /// </summary>
  JLoadChunks chunkLoadQueueManagerJob;

  /// <summary>
  /// The current parent job, in charge of loading the chunks in the load queue
  /// </summary>
  JUnloadChunks chunkUnloadQueueManagerJob;

  /// <summary>
  /// The columns of chunks already loaded/being loaded
  /// </summary>
  HashSet<Coordinate> loadedChunkColumns;

  /// <summary>
  /// construct
  /// </summary>
  /// <param name="chunkBounds"></param>
  /// <param name="blockSource"></param>
  public ColumnLoadedLevel(Coordinate chunkBounds, IBlockSource blockSource) : base(chunkBounds, blockSource) {
    chunkLoadQueueManagerJob   = new JLoadChunks(this);
    chunkUnloadQueueManagerJob = new JUnloadChunks(this);
    loadedChunkColumns         = new HashSet<Coordinate>();
  }

  /// <summary>
  /// initialize this level with the center of loaded chunks fouced on the given location
  /// </summary>
  /// <param name="centerChunkLocation">the center point/focus of the loaded chunks, usually a player location</param>
  public override void initializeAround(Coordinate centerChunkLocation) {
    loadedChunkFocus = centerChunkLocation;
    loadedChunkBounds = getLoadedChunkBounds(loadedChunkFocus);
    Coordinate[] chunksToLoad = Coordinate.GetAllPointsBetween(loadedChunkBounds[0], loadedChunkBounds[1]);
    addChunkColumnsToLoadingQueue(chunksToLoad);
  }

  /// <summary>
  /// Move the focus/central loaded point of the level
  /// </summary>
  /// <param name="direction">the direction the focus has moved</param>
  /// <param name="magnitude">the number of chunks in the direction that the foucs moved</param>
  public override void adjustFocus(Directions.Direction direction) {
    List<Coordinate> chunksToLoad = new List<Coordinate>();
    List<Coordinate> chunksToUnload = new List<Coordinate>();

    // add new chunks to the load queue in the given direction
    if (Array.IndexOf(Directions.Cardinal, direction) > -1) {
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
  /// Get the loaded chunk bounds for a given center point.
  /// Always trims to X,0,Z
  /// </summary>
  /// <param name="centerLocation"></param>
  protected override Coordinate[] getLoadedChunkBounds(Coordinate centerLocation) {
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
  /// Add multiple chunk column locations to the load queue and run it
  /// </summary>
  /// <param name="chunkLocations">the x,z values of the chunk columns to load</param>
  protected void addChunkColumnsToLoadingQueue(Coordinate[] chunkLocations) {
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
  protected void addChunkColumnsToUnloadingQueue(Coordinate[] chunkLocations) {
    foreach (Coordinate chunkLocation in chunkLocations) {
      addChunkColumnToUnloadingQueue(chunkLocation.xz);
    }

    // if the unload queue manager job isn't running, start it
    if (!chunkUnloadQueueManagerJob.isRunning) {
      chunkUnloadQueueManagerJob.start();
    }
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
  /// A job to load all chunks from the loading queue
  /// </summary>
  class JLoadChunks : LevelQueueManagerJob {

    /// <summary>
    /// A Job for generating a new column of chunks into a level
    /// </summary>
    class JGenerateChunkColumn : ChunkColumnLoadingJob {

      /// <summary>
      /// Make a new job
      /// </summary>
      /// <param name="level"></param>
      /// <param name="chunkLocation"></param>
      /// <param name="resourcePool"></param>
      internal JGenerateChunkColumn(Level<ChunkType> level, Coordinate chunkLocation, Semaphore resourcePool)
        : base(resourcePool, level, chunkLocation) {
        threadName = "Generate Column: " + chunkColumnLocation;
      }

      /// <summary>
      /// Threaded function, loads all the block data for this chunk
      /// </summary>
      protected override void doWorkOnChunk(Coordinate chunkLocation) {
        if (level.getChunk(chunkLocation) == null) {
          ChunkType chunkData = level.generateChunkData(chunkLocation);
          level.setChunk(chunkLocation, chunkData);
        }
      }
    }

    /// <summary>
    /// A Job for loading the data for a column of chunks into a level from file
    /// </summary>
    class JLoadChunkColumnFromFile : ChunkColumnLoadingJob {

      /// <summary>
      /// Make a new job
      /// </summary>
      /// <param name="level"></param>
      /// <param name="chunkLocation"></param>
      /// <param name="resourcePool"></param>
      internal JLoadChunkColumnFromFile(Level<ChunkType> level, Coordinate chunkLocation, Semaphore resourcePool)
        : base(resourcePool, level, chunkLocation) {
        threadName = "Load Column: " + chunkColumnLocation;
      }

      /// <summary>
      /// Threaded function, loads all the block data for this chunk
      /// </summary>
      protected override void doWorkOnChunk(Coordinate chunkLocation) {
        if (level.getChunk(chunkLocation) == null) {
          ChunkType chunkData = level.getChunkDataFromFile(chunkLocation);
          level.setChunk(chunkLocation, chunkData);
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
      threadName = "Load Chunk Manager";
    }

    /// <summary>
    /// Make a chunk loader job
    /// </summary>
    /// <param name="level"></param>
    /// <param name="chunkLocation"></param>
    /// <returns></returns>
    protected override ChildQueueJob getChildJob(Coordinate chunkLocation) {
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
        : base(resourcePool, level, chunkLocation) {
        threadName = "Unload Column: " + chunkColumnLocation;
      }

      /// <summary>
      /// Threaded function, serializes this chunks block data and removes it from the level
      /// </summary>
      protected override void doWorkOnChunk(Coordinate chunkLocation) {
        level.saveChunkToFile(chunkLocation);
        level.removeChunk(chunkLocation);
      }
    }

    /// <summary>
    /// Create a new job, linked to the level
    /// </summary>
    /// <param name="level"></param>
    public JUnloadChunks(Level<ChunkType> level) : base(level) {
      threadName = "Unload Chunk Manager";
    }

    /// <summary>
    /// Make a new unload chunk job
    /// </summary>
    /// <param name="level"></param>
    /// <param name="chunkLocation"></param>
    /// <returns></returns>
    protected override ChildQueueJob getChildJob(Coordinate chunkLocation) {
      return new JUnloadChunkColumn(level, chunkLocation, childJobResourcePool);
    }
  }

  /// <summary>
  /// A base job for managing chunk work queues
  /// </summary>
  abstract class LevelQueueManagerJob : QueueManagerJob<Coordinate> {

    /// <summary>
    /// Base class for child jobs that manage chunk loading and unloading
    /// </summary>
    protected abstract class ChunkColumnLoadingJob : ChildQueueJob {

      /// <summary>
      /// The level we're loading for
      /// </summary>
      protected Level<ChunkType> level;

      /// <summary>
      /// The location of the chunk that this job is loading.
      /// </summary>
      protected Coordinate chunkColumnLocation;

      /// <summary>
      /// Make a new job
      /// </summary>
      /// <param name="level"></param>
      /// <param name="chunkColumnLocation"></param>
      protected ChunkColumnLoadingJob(
        Semaphore parentResourcePool,
        Level<ChunkType> level,
        Coordinate chunkColumnLocation
      ) : base(parentResourcePool) {
        this.level = level;
        this.chunkColumnLocation = chunkColumnLocation;
      }

      /// <summary>
      /// Do the actual work on the given chunk for this type of job
      /// </summary>
      protected abstract void doWorkOnChunk(Coordinate chunkLocation);

      /// <summary>
      /// Do work
      /// </summary>
      protected override void doWork() {
        Coordinate columnTop = (chunkColumnLocation.x, level.chunkBounds.y, chunkColumnLocation.z);
        Coordinate columnBottom = (chunkColumnLocation.x, 0, chunkColumnLocation.z);
        columnBottom.until(columnTop, chunkLocation => {
          if (!isCanceled) {
            doWorkOnChunk(chunkLocation);
            return true;
          }

          return false;
        });
      }
    }

    /// <summary>
    /// The level we're loading for
    /// </summary>
    protected Level<ChunkType> level;

    /// <summary>
    /// Create a new job, linked to the level
    /// </summary>
    /// <param name="level"></param>
    protected LevelQueueManagerJob(Level<ChunkType> level) : base(MaxChunkLoadingJobsCount) {
      this.level = level;
    }
  }
}
