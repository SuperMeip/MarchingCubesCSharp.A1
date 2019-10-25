using System;
using System.Collections.Generic;
using Block;

public class MarchingPointDictionary {

  public float isoSurfaceLevel;

  /// <summary>
  /// The max boundries of this collection of blocks 0->xyz
  /// </summary>
  public Coordinate bounds {
    get => trueBounds - 1;
  }

  Coordinate trueBounds;

  /// <summary>
  /// The collection of points, a byte representing the material the point is made of
  /// </summary>
  IDictionary<Coordinate, int> points;

  public MarchingPointDictionary(Coordinate bounds) {
    trueBounds = bounds + 1;
    points = new Dictionary<Coordinate, int>(trueBounds.x * trueBounds.y * trueBounds.z);
  }

  public int this[Coordinate coordinate] {
    get {
      return points.ContainsKey(coordinate)
        ? points[coordinate]
        : 0;
    }
    set {
      if (coordinate.isWithin(trueBounds)) {
        points[coordinate] = value;
      } else {
        throw new IndexOutOfRangeException();
      }
    }
  }

  /// <summary>
  /// Update the block at the location, while also updating the related vertex bitmasks
  /// </summary>
  /// <param name="location"></param>
  /// <param name="blockTypeId"></param>
  public void updateBlock(Coordinate location, int blockData) {
    this[location] = this[location].SetBlockTypeId(blockData.GetBlockTypeId());
    this[location] = this[location].SetBlockScalarDensity(blockData.GetBlockScalarDensity());

     // for all the blocks around this point, update their bitmasks
    foreach (Octants.Octant neightboringVertexDirection in Octants.All) {
      Coordinate blockLocation = location - 1 + neightboringVertexDirection.Offset;
      //update the vertex mask to a 1 for the neighbor
      this[blockLocation] = this[blockLocation].SetVertexMaskForOctant(
        neightboringVertexDirection.Reverse,
        blockData.GetBlockType().IsSolid
      );
    }
  }
}
