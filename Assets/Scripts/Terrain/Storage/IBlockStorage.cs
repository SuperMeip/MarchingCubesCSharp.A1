public interface IBlockStorage {
  int getBlockAt(Coordinate location);

  int updateBlockAt(Coordinate location, int newIdAndDensity);

  int updateBlockAt(Coordinate location, byte blockTypeId, float? densityValue);
}
