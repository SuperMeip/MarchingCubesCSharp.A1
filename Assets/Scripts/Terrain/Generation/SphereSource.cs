using Block;
using UnityEngine;

/// <summary>
/// Block source for a single sphere
/// </summary>
public class SphereSource : BlockSource {

  /// <summary>
  /// the radius of the sphere to generate
  /// </summary>
  int sphereRadius;

  public SphereSource(int sphereRadius = 10) : base() {
    this.sphereRadius = sphereRadius;
  }

  /// <summary>
  /// Get values for sphere distance
  /// </summary>
  /// <param name="coordinate"></param>
  /// <returns></returns>
  protected override float getNoiseValueAt(Coordinate coordinate) {
    float distance = Mathf.Abs(new Coordinate(sphereRadius + 3).distance(coordinate));
    return Utility.ClampToFloat(distance, 0, (int)((sphereRadius) * 1.5f));
  }
}
