using Block;

public abstract class BlockStorage : IBlockStorage {

  /// <summary>
  /// The itteratable bounds of this collection of blocks, x, y, and z
  /// </summary>
  public Coordinate bounds {
    get;
    protected set;
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
  public abstract int getBlock(Coordinate location);

  /// <summary>
  /// Overwrite the entire block at the given location
  /// </summary>
  /// <param name="location">the x,y,z of the block to set</param>
  /// <param name="newBlockValue">The block data to set as a bitmask:
  ///   byte 1: the block type id
  ///   byte 2: the block vertex mask
  ///   byte 3 & 4: the block's scalar density float, compresed to a short
  /// </param>
  public abstract void setBlock(Coordinate location, int newBlockValue);

  /// <summary>
  /// Update the point at the given location with a new blcok type id, and potentially a new density value
  /// This also updates the bit mask for all the blocks around the point being updated.
  /// </summary>
  /// <param name="location">The xyz of the point to update</param>
  /// <param name="blockTypeId">The new block id</param>
  /// <param name="densityValue">the new density value</param>
  /// <returns>the updated block value with bitmask included</returns>
  public void updateBlock(Coordinate location, byte blockTypeId, float? densityValue = null) {
    updateBlockTypeId(location, blockTypeId);
    if (densityValue != null) {
      updateBlockScalarDensity(location, (float)densityValue);
    }

    // for all the blocks around this point, update their bitmasks
    foreach (Octants.Octant neightboringVertexDirection in Octants.All) {
      Coordinate blockLocation = location - 1 + neightboringVertexDirection.Offset;
      //update the vertex mask to a 1 for the neighbor
      updateBlockVertexMask(
        blockLocation,
        neightboringVertexDirection.Reverse,
        Block.Types.Get(blockTypeId).IsSolid
      );
    }
  }

  /// <summary>
  /// Helper function to update just the vertex mask of a given block
  /// </summary>
  /// <param name="location">the x,y,z of the block to set</param>
  /// <param name="vertex">the vertex to toggle</param>
  /// <param name="setSolid">whether to set the vertex flag to solid or empty</param>
  protected void updateBlockVertexMask(Coordinate location, Octants.Octant vertex, bool setSolid = true) {
    setBlock(location, getBlock(location).SetVertexMaskForOctant(vertex, setSolid));
  }

  /// <summary>
  /// Helper function to set just the id of a block at a given location
  /// </summary>
  /// <param name="location">the x,y,z of the block to set</param>
  /// <param name="blockTypeId">The new block id</param>
  protected void updateBlockTypeId(Coordinate location, byte blockTypeId) {
    setBlock(location, getBlock(location).SetBlockTypeId(blockTypeId));
  }

  /// <summary>
  /// Helper function to set just the scalar density of a block at a given location
  /// </summary>
  /// <param name="location">the x,y,z of the block to set</param>
  /// <param name="blockTypeId">The new block id</param>
  protected void updateBlockScalarDensity(Coordinate location, float scalarDensity) {
    setBlock(location, getBlock(location).SetBlockScalarDensity(scalarDensity));
  }
}
