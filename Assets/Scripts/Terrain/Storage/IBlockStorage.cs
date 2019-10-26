public interface IBlockStorage {

  /// <summary>
  /// The itteratable bounds of this collection of blocks, x, y, and z
  /// </summary>
  Coordinate bounds {
    get;
  }

  /// <summary>
  /// Get the block data as an int bitmask at the given x,y,z
  /// </summary>
  /// <param name="location">the x,y,z of the block/point data to get</param>
  /// <returns>The block data as a bitmask:
  ///   byte 1: the block type id
  ///   byte 2: the block vertex mask
  ///   byte 3 & 4: the block's scalar density float, compresed to a short
  /// </returns>
  int getBlock(Coordinate location);

  /// <summary>
  /// Update the point at the given location with a new blcok type id, and potentially a new density value
  /// This also updates the bit mask for all the blocks around the point being updated.
  /// </summary>
  /// <param name="location">The xyz of the point to update</param>
  /// <param name="newIdAndDensity">The new block id and density value as a bitmask on an int
  ///   byte 1: the block type id
  ///   byte 2: 00000000 (left blank for vertex bitmask)
  ///   bytes 3 & 4: the compresed surface density float
  /// </param>
  /// <returns>the updated block value with bitmask included</returns>
  void updateBlock(Coordinate location, byte blockTypeId, float? densityValue = null);
}
