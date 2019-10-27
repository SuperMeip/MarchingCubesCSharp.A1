/// <summary>
/// A set if blocks in the world
/// </summary>
public class Chunk {

  /// <summary>
  /// The diameter of a chunk (x y or z) in blocks
  /// </summary>
  public const int Diameter = 64;

  /// <summary>
  /// The chunk location of this chunk.
  /// </summary>
  public Coordinate location;

  /// <summary>
  /// The chunk's blocks
  /// </summary>
  public IBlockStorage blocks;

  /// <summary>
  /// Make a new chunk at the given location
  /// </summary>
  /// <param name="location"></param>
  public Chunk(Coordinate location) {
    this.location = location;
    blocks = new MarchingPointDictionary(new Coordinate(Diameter));
  }

  /// <summary>
  /// Generate all the blocks for this chunk given the blocksource
  /// </summary>
  /// <param name="blockSource">The source used generate the blockdata for this chunk</param>
  public void generateBlocks(IBlockSource blockSource) {
    blockSource.generateAllAt(location, blocks);
  }
}
