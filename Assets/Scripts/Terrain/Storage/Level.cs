using System.Collections.Generic;
using UnityEngine;

public class Level {

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
  /// The coordinates indicating the two chunksat the extreems of what is loaded from memmory:
  ///   0: west-bottom-southmost loaded chunk
  ///   1: east-top-north most loaded chunk
  /// </summary>
  Coordinate[] loadedChunkBounds;

  /// <summary>
  /// The active chunks, store by coordinate location
  /// </summary>
  Dictionary<Coordinate, IBlockStorage> activeChunks;

  /// <summary>
  /// The source used to load blocks for new chunks in this level
  /// </summary>
  IBlockSource blockSource;

  /// <summary>
  /// The level seed
  /// </summary>
  protected int seed;

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
  public Level(int seed, Coordinate chunkBounds) {
    this.chunkBounds = chunkBounds;
    this.seed = seed;
    activeChunks = new Dictionary<Coordinate, IBlockStorage>(
      chunkBounds.x * chunkBounds.y * chunkBounds.z
    ); 
  }

  /// <summary>
  /// 
  /// </summary>
  /// <param name="location"></param>
  public IBlockStorage getChunk(Coordinate chunkLocation) {
    return chunkLocation.isWithin(chunkBounds) && chunkIsActive(chunkLocation) && activeChunks.ContainsKey(chunkLocation)
      ? activeChunks[chunkLocation]
      : null;
  }

  /// <summary>
  /// Load the active chunks around the given location
  /// </summary>
  /// <param name="location"></param>
  void loadChunksAround(Coordinate location) {
    Coordinate[] newbounds = getLoadedChunkBounds(location);
    
  }

  /// <summary>
  /// Get the loaded chunk bounds for a given center point
  /// </summary>
  /// <param name="centerLocation"></param>
  Coordinate[] getLoadedChunkBounds(Coordinate centerLocation) {
    return new Coordinate[] {
      (
        Mathf.Max(centerLocation.x - LoadedChunkDiameter / 2, 0),
        Mathf.Max(centerLocation.y - LoadedChunkHeight, 0),
        Mathf.Max(centerLocation.z - LoadedChunkDiameter / 2, 0)
      ),
      (
        Mathf.Min(centerLocation.x + LoadedChunkDiameter / 2, chunkBounds.x - 1),
        Mathf.Min(centerLocation.y + LoadedChunkHeight, chunkBounds.y - 1),
        Mathf.Min(centerLocation.z + LoadedChunkDiameter / 2, chunkBounds.z - 1)
      )
    };
  }

  /// <summary>
  /// 
  /// </summary>
  /// <param name="chunkLocation"></param>
  /// <returns></returns>
  void loadChunk(Coordinate chunkLocation) {
    //@todo try to find a saved chunk first
    IBlockStorage chunk = new MarchingPointDictionary(new Coordinate(Chunk.Diameter));
    blockSource.generateAllAt(chunkLocation, chunk);
    activeChunks[chunkLocation] = chunk;
  }

  /// <summary>
  /// 
  /// </summary>
  /// <param name="location"></param>
  /// <returns></returns>
  bool chunkIsActive(Coordinate location) {
    return location.isWithin(loadedChunkBounds[1]) && location.isBeyond(loadedChunkBounds[1]);
  }
}
