/// <summary>
/// An interface for a level, used to load the block data for a level around a player/focus point
/// </summary>
public interface ILevel<ChunkType> where ChunkType : IBlockStorage {

  /// <summary>
  /// The overall bounds of the level, max x y and z
  /// </summary>
  public Coordinate chunkBounds {
    get;
  }

  /// <summary>
  /// Get the chunk at the given location (if it's loaded)
  /// </summary>
  /// <param name="location"></param>
  /// <returns>the chunk data or null if there's none loaded</returns>
  ChunkType getChunk(Coordinate chunkLocation);

  /// <summary>
  /// initialize this level with the center of loaded chunks fouced on the given location
  /// </summary>
  /// <param name="centerChunkLocation">the center point/focus of the loaded chunks, usually a player location</param>
  void initializeAround(Coordinate centerChunkLocation);

  /// <summary>
  /// Move the focus/central loaded point of the level
  /// </summary>
  /// <param name="direction">the direction the focus has moved</param>
  /// <param name="magnitude">the number of chunks in the direction that the foucs moved</param>
  void adjustFocus(Directions.Direction direction);
}
