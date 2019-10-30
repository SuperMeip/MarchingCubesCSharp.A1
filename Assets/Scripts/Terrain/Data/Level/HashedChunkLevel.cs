using System.Collections.Generic;

/// <summary>
/// A level that uses a dictionary to organize it's chunks
/// </summary>
/// <typeparam name="ChunkType"></typeparam>
public abstract class HashedChunkLevel<ChunkType> : Level<ChunkType> where ChunkType : IBlockStorage {

  /// <summary>
  /// The active chunks, stored by coordinate location
  /// </summary>
  Dictionary<long, ChunkType> loadedChunks;

  /// <summary>
  /// Construct
  /// </summary>
  /// <param name="chunkBounds"></param>
  /// <param name="blockSource"></param>
  public HashedChunkLevel(Coordinate chunkBounds, IBlockSource blockSource) : base(chunkBounds, blockSource) {
    loadedChunks = new Dictionary<long, ChunkType>(
      chunkBounds.x * chunkBounds.y * chunkBounds.z
    );
  }

  /// <summary>
  /// Get the chunk from the hash map
  /// </summary>
  /// <param name="chunkLocation"></param>
  /// <returns></returns>
  public override ChunkType getChunk(Coordinate chunkLocation) {
    return chunkLocation.isWithin(chunkBounds)
      && chunkIsWithinkLoadedBounds(chunkLocation)
      && loadedChunks.ContainsKey(getChunkHash(chunkLocation))
        ? loadedChunks[getChunkHash(chunkLocation)]
        : default;
  }

  /// <summary>
  /// Set the given set of block data to the given chunk location
  /// </summary>
  /// <param name="chunkLocation"></param>
  /// <param name="blockData"></param>
  internal override void setChunk(Coordinate chunkLocation, ChunkType blockData) {
    loadedChunks[getChunkHash(chunkLocation)] = blockData;
  }

  /// <summary>
  /// Remove the chunk at the given location
  /// </summary>
  /// <param name="chunkLocation"></param>
  internal override void removeChunk(Coordinate chunkLocation) {
    loadedChunks.Remove(getChunkHash(chunkLocation));
  }

  /// <summary>
  /// Get the hash key for the chunk's location
  /// </summary>
  /// <returns></returns>
  long getChunkHash(Coordinate chunkLocation) {
    long hash = 0;
    hash |= ((ushort)chunkLocation.x);
    hash |= (((ushort)chunkLocation.y) << 16);
    hash |= (((ushort)chunkLocation.z) << 24);

    return hash;
  }
}
