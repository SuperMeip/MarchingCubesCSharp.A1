
/// <summary>
/// A source of blocks, usually using nosie
/// </summary>
public interface IBlockSource {

  /// <summary>
  /// Generate all given blocks
  /// </summary>
  /// <param name="blockData"></param>
  void generateAll(IBlockStorage blockData);

  /// <summary>
  /// Generate all given blocks, using a location offset
  /// </summary>
  /// <param name="location">the offset of the blocks</param>
  /// <param name="blockData">the empty block collection</param>
  void generateAllAt(Coordinate location, IBlockStorage blockData);
}
