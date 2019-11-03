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
  /// construct
  /// </summary>
  /// <param name="chunkBounds"></param>
  /// <param name="blockSource"></param>
  public ColumnLoadedLevel(Coordinate chunkBounds, IBlockSource blockSource) : base(chunkBounds, blockSource) {
    chunkLoadQueueManagerJob = new JLoadChunks(this);
    chunkUnloadQueueManagerJob = new JUnloadChunks(this);
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
    chunkLoadQueueManagerJob.enQueue(chunkLocations);
  }

  /// <summary>
  /// Add multiple chunk column locations to the unload queue and run it
  /// </summary>
  /// <param name="chunkLocations">the x,z values of the chunk columns to unload</param>
  protected void addChunkColumnsToUnloadingQueue(Coordinate[] chunkLocations) {
    chunkLoadQueueManagerJob.deQueue(chunkLocations);
    chunkUnloadQueueManagerJob.enQueue(chunkLocations);
  }

  /// <summary>
  /// A job to load all chunks from the loading queue
  /// </summary>
  class JLoadChunks : LevelQueueManagerJob {

    /// <summary>
    /// A Job for loading the data for a column of chunks into a level from file
    /// </summary>
    class JLoadChunkColumnFromFile : ChunkColumnLoadingJob {

      /// <summary>
      /// Constructor
      /// </summary>
      /// <param name="level"></param>
      /// <param name="chunkColumnLocation"></param>
      /// <param name="parentCancellationSources"></param>
      internal JLoadChunkColumnFromFile(
        Level<ChunkType> level,
        Coordinate chunkColumnLocation,
        Dictionary<Coordinate, CancellationTokenSource> parentCancellationSources
      ) : base(level, chunkColumnLocation, parentCancellationSources) {
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
    /// The job for generating chunks from scratch
    /// </summary>
    JGenerateChunks chunkGenerationManagerJob;

    /// <summary>
    /// Create a new job, linked to the level
    /// </summary>
    /// <param name="level"></pa
    public JLoadChunks(Level<ChunkType> level) : base(level) {
      threadName = "Load Chunk Manager";
      chunkGenerationManagerJob = new JGenerateChunks(level);
    }

    /// <summary>
    /// Override to shift generational items to their own queue
    /// </summary>
    /// <param name="chunkColumnLocation"></param>
    /// <returns></returns>
    protected override bool isAValidQueueItem(Coordinate chunkColumnLocation) {
      // if this doesn't have a loaded file, remove it from this queue and load it in the generation one
      if (!File.Exists(level.getChunkFileName(chunkColumnLocation))) {
        chunkGenerationManagerJob.enQueue(new Coordinate[] { chunkColumnLocation });
        return false;
      }

      return base.isAValidQueueItem(chunkColumnLocation);
    }

    /// <summary>
    /// Get the correct child job
    /// </summary>
    /// <param name="chunkColumnLocation"></param>
    /// <param name="parentCancellationSources"></param>
    /// <returns></returns>
    protected override QueueTaskChildJob<Coordinate> getChildJob(
       Coordinate chunkColumnLocation,
       Dictionary<Coordinate, CancellationTokenSource> parentCancellationSources
     ) {
      return new JLoadChunkColumnFromFile(level, chunkColumnLocation, parentCancellationSources);
    }
  }

  /// <summary>
  /// A job to load all chunks from the loading queue
  /// </summary>
  class JGenerateChunks : LevelQueueManagerJob {

    /// <summary>
    /// A Job for generating a new column of chunks into a level
    /// </summary>
    class JGenerateChunkColumn : ChunkColumnLoadingJob {

      /// <summary>
      /// constructor
      /// </summary>
      /// <param name="level"></param>
      /// <param name="chunkColumnLocation"></param>
      /// <param name="parentCancellationSources"></param>
      internal JGenerateChunkColumn(
        Level<ChunkType> level,
        Coordinate chunkColumnLocation,
        Dictionary<Coordinate, CancellationTokenSource> parentCancellationSources
      ) : base(level, chunkColumnLocation, parentCancellationSources) {
        threadName = "Generate Column: " + chunkColumnLocation;
      }

      /// <summary>
      /// Threaded function, loads all the block data for this chunk
      /// </summary>
      protected override void doWorkOnChunk(Coordinate chunkLocation) {
        if (level.getChunk(chunkLocation) == null) {
          Debug.Log("Generating: " + chunkLocation.ToString());
          ChunkType chunkData = level.generateChunkData(chunkLocation);
          Debug.Log("Finished: " + chunkLocation.ToString());
          level.setChunk(chunkLocation, chunkData);
        }
      }
    }

    /// <summary>
    /// Create a new job, linked to the level
    /// </summary>
    /// <param name="level"></param>
    public JGenerateChunks(Level<ChunkType> level) : base(level) {
      threadName = "Generate Chunk Manager";
    }

    /// <summary>
    /// Get the correct child job
    /// </summary>
    /// <param name="chunkColumnLocation"></param>
    /// <param name="parentCancellationSources"></param>
    /// <returns></returns>
    protected override QueueTaskChildJob<Coordinate> getChildJob(
       Coordinate chunkColumnLocation,
       Dictionary<Coordinate, CancellationTokenSource> parentCancellationSources
     ) {
      return new JGenerateChunkColumn(level, chunkColumnLocation, parentCancellationSources);
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
      /// <param name="chunkColumnLocation"></param>
      /// <param name="resourcePool"></param>
      internal JUnloadChunkColumn(
        Level<ChunkType> level,
        Coordinate chunkColumnLocation,
        Dictionary<Coordinate, CancellationTokenSource> parentCancellationSources
      ) : base(level, chunkColumnLocation, parentCancellationSources) {
        threadName = "Unload Column: " + queueItem.ToString();
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
    /// Get the child job
    /// </summary>
    /// <param name="chunkColumnLocation"></param>
    /// <param name="parentCancellationSources"></param>
    /// <returns></returns>
    protected override QueueTaskChildJob<Coordinate> getChildJob(
      Coordinate chunkColumnLocation,
      Dictionary<Coordinate, CancellationTokenSource> parentCancellationSources
    ) {
      return new JUnloadChunkColumn(level, chunkColumnLocation, parentCancellationSources);
    }
  }

  /// <summary>
  /// A base job for managing chunk work queues
  /// </summary>
  abstract class LevelQueueManagerJob : QueueManagerJob<Coordinate> {

    /// <summary>
    /// Base class for child jobs that manage chunk loading and unloading
    /// </summary>
    protected abstract class ChunkColumnLoadingJob : QueueTaskChildJob<Coordinate> {

      /// <summary>
      /// The level we're loading for
      /// </summary>
      protected Level<ChunkType> level;

      /// <summary>
      /// Make a new job
      /// </summary>
      /// <param name="level"></param>
      /// <param name="chunkColumnLocation"></param>
      protected ChunkColumnLoadingJob(
        Level<ChunkType> level,
        Coordinate chunkColumnLocation,
        Dictionary<Coordinate, CancellationTokenSource> parentCancellationSources
      ) : base(chunkColumnLocation, parentCancellationSources) {
        this.level = level;
      }

      /// <summary>
      /// Do the actual work on the given chunk for this type of job
      /// </summary>
      protected abstract void doWorkOnChunk(Coordinate chunkLocation);

      /// <summary>
      /// Do work
      /// </summary>
      protected override void doWork(Coordinate chunkColumnLocation, CancellationToken cancellationToken) {
        Coordinate columnTop = (chunkColumnLocation.x, level.chunkBounds.y, chunkColumnLocation.z);
        Coordinate columnBottom = (chunkColumnLocation.x, 0, chunkColumnLocation.z);
        columnBottom.until(columnTop, chunkLocation => {
          if (!cancellationToken.IsCancellationRequested) {
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
