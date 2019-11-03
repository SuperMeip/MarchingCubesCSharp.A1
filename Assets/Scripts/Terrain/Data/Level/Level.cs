using System.IO;
using System;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

/// <summary>
/// A collection of chunks, making an enclosed world in game
/// </summary>
public abstract class Level<ChunkType> : ILevel<ChunkType> where ChunkType : IBlockStorage {

  /// <summary>
  /// The size of a block, in engine
  /// </summary>
  public const float BlockSize = 1.0f;

  /// <summary>
  /// The block diameter, x y and z, of a chunk in this level
  /// </summary>
  public const int ChunkDiameter = 64;

  /// <summary>
  /// The width of the active chunk area in chunks
  /// </summary>
  protected const int LoadedChunkDiameter = 24;

  /// <summary>
  /// The save path for levels.
  /// </summary>
  readonly string SavePath = Application.persistentDataPath + "/leveldata/";

  /// <summary>
  /// The height of the active chunk area in chunks
  /// </summary>
  protected int LoadedChunkHeight {
    get => chunkBounds.y;
  }

  /// <summary>
  /// The source used to load blocks for new chunks in this level
  /// </summary>
  IBlockSource blockSource;

  /// <summary>
  /// The level seed
  /// </summary>
  int seed;

  /// <summary>
  /// The coordinates indicating the two chunks the extreems of what columns are loaded from memmory:
  ///   0: south bottom west most loaded chunk
  ///   1: north top east most loaded chunk 
  /// </summary>
  protected Coordinate[] loadedChunkBounds;

  /// <summary>
  /// The current center of all loaded chunks, usually based on player location
  /// </summary>
  protected Coordinate loadedChunkFocus;

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
  public Level(Coordinate chunkBounds, IBlockSource blockSource) {
    this.blockSource           = blockSource;
    this.chunkBounds           = chunkBounds;
    seed                       = blockSource.seed;
  }

  /// <summary>
  /// initialize this level with the center of loaded chunks fouced on the given location
  /// </summary>
  /// <param name="centerChunkLocation">the center point/focus of the loaded chunks, usually a player location</param>
  public abstract void initializeAround(Coordinate centerChunkLocation);

  /// <summary>
  /// Move the focus/central loaded point of the level by one chunk in the given direction
  /// </summary>
  /// <param name="direction">the direction the focus has moved</param>
  /// <param name="magnitude">the number of chunks in the direction that the foucs moved</param>
  public abstract void adjustFocus(Directions.Direction direction);

  /// <summary>
  /// Get the chunk at the given location (if it's loaded)
  /// </summary>
  /// <param name="location"></param>
  /// <returns>the chunk data or null if there's none loaded</returns>
  public abstract ChunkType getChunk(Coordinate chunkLocation);

  /// <summary>
  /// Set the given blockdata to the given chunk location in this level's active storage/memmory
  /// </summary>
  /// <param name="chunkLocation"></param>
  /// <param name="blockData"></param>
  internal abstract void setChunk(Coordinate chunkLocation, ChunkType blockData);

  /// <summary>
  /// Remove/nullify data for the chunk at the given location
  /// </summary>
  /// <param name="chunkLocation"></param>
  internal abstract void removeChunk(Coordinate chunkLocation);

  /// <summary>
  /// Get the loaded chunk bounds for a given center point.
  /// Always trims to X,0,Z
  /// </summary>
  /// <param name="centerLocation"></param>
  protected abstract Coordinate[] getLoadedChunkBounds(Coordinate centerLocation);

  /// <summary>
  /// Get if the given chunkLocation is loaded
  /// </summary>
  /// <param name="chunkLocation"></param>
  /// <returns></returns>
  protected bool chunkIsWithinkLoadedBounds(Coordinate chunkLocation) {
    return chunkLocation.isWithin(loadedChunkBounds[1]) && chunkLocation.isBeyond(loadedChunkBounds[0]);
  }

  /// <summary>
  /// Only to be used by jobs
  /// Save a chunk to file
  /// </summary>
  /// <param name="chunkLocation"></param>
  internal void saveChunkToFile(Coordinate chunkLocation) {
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
  internal ChunkType getChunkDataFromFile(Coordinate chunkLocation) {
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
  internal ChunkType generateChunkData(Coordinate chunkLocation) {
    ChunkType chunkData = (ChunkType)Activator.CreateInstance(typeof(ChunkType), ChunkDiameter);
    blockSource.generateAllAt(chunkLocation, chunkData);

    return chunkData;
  }

  /// <summary>
  /// Get the file name a chunk is saved to based on it's location
  /// </summary>
  /// <param name="chunkLocation">the location of the chunk</param>
  /// <returns></returns>
  internal string getChunkFileName(Coordinate chunkLocation) {
    return SavePath + "/" + seed + "/" + chunkLocation.ToString() + ".evxch";
  }
}