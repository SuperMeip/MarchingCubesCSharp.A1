using System;
using System.Collections;

public struct BlockGridArray {

  /// <summary>
  /// block data
  /// </summary>
  int[][][] blockData;

  /// <summary>
  /// The max X, Y, and Z of this grid of blocks
  /// </summary>
  Coordinate bounds;

  public BlockGridArray(Coordinate bounds) {
    this.bounds = bounds;
    blockData = null;
  }

  public int this[Coordinate coordinate] {
    get {
      return coordinate.isWithin(bounds)
        ? get(coordinate)
        : throw new IndexOutOfRangeException();
    }
    set {
      if (coordinate.isWithin(bounds)) {
        set(coordinate, value);
      } else {
        throw new IndexOutOfRangeException();
      }
    }
  }

  public int this[int x, int y, int z] {
    get {
      return new Coordinate(x, y, z).isWithin(bounds) 
        ? get((x, y, z))
        : throw new IndexOutOfRangeException();
    }
    set {
      if (new Coordinate(x, y, z).isWithin(bounds)) {
        set((x, y, z), value);
      } else {
        throw new IndexOutOfRangeException();
      }
    }
  }

  /// <summary>
  /// Set the value at the given point
  /// </summary>
  /// <param name="location"></param>
  /// <param name="value"></param>
  /// <returns></returns>
  void set(Coordinate location, int value) {
    if (blockData == null) {
      initilizeJaggedArray(location.x);
    }
    // If this is beyond our current X, resize the x array
    if (blockData.Length < location.x) {
      Array.Resize(ref blockData, location.x);
    }
    // if there's no Y array at the X location, add one
    if (blockData[location.x] == null) {
      blockData[location.x] = new int[location.y][];
    }
    // if the Y array is too small, resize it
    if (blockData[location.x].Length < location.y) {
      Array.Resize(ref blockData[location.x], location.y);
    }
    // if there's no Z array at our location, add one
    if (blockData[location.x][location.y] == null) {
      blockData[location.x][location.y] = new int[location.z];
    }
    // if the Z array is too small, resize it
    if (blockData[location.x][location.y].Length < location.z) {
      Array.Resize(ref blockData[location.x][location.y], location.z);
    }

    blockData[location.x][location.y][location.z] = value;
  }

  /// <summary>
  /// Get the block data at a location
  /// </summary>
  /// <param name="location"></param>
  /// <returns></returns>
  int get(Coordinate location) {
    return blockData != null
      ? location.x < blockData.Length
        ? location.y < blockData[location.x].Length
          ? location.z < blockData[location.x][location.y].Length
            ? blockData[location.x][location.y][location.z]
            : 0
          : 0
        : 0
      : 0;
  }

  /// <summary>
  /// Create the first row of the jagged array
  /// </summary>
  /// <param name="x"></param>
  void initilizeJaggedArray(int x = -1) {
    blockData = new int[x == -1 ? bounds.x : x][][]; 
  }
}
