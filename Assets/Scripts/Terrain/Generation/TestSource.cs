using Block;
using UnityEngine;

public class TestSource : BlockSource {

  protected override float getNoiseValueAt(Coordinate coordinate) {
    /*return (float)Utility.Clamp(
      noise.GetNoise(coordinate.x, coordinate.y, coordinate.z),
      -1.0f,
      1.0f,
      0.0f,
      1.0f
    );*/
    return getSphereNoiseValueAt(coordinate);
  }

  protected override void setUpNoise() {
    noise.SetNoiseType(Noise.FastNoise.NoiseType.Cellular);
  }

  protected float getSphereNoiseValueAt(Coordinate coordinate) {
    int sphereRadius = 1;
    float distance = Mathf.Abs(new Coordinate(sphereRadius + 3).distance(coordinate));
    return Utility.ClampToFloat(distance, 0, (int)((sphereRadius) * 1.5f));
  }
}
