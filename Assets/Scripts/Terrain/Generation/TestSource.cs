using Block;
using UnityEngine;

public class TestSource : BlockSource {

  /// <summary>
  /// Get the noise value
  /// </summary>
  /// <param name="coordinate"></param>
  /// <returns></returns>
  protected override float getNoiseValueAt(Coordinate coordinate) {
    return (float)Utility.Clamp(
      noise.GetNoise(coordinate.x, coordinate.y, coordinate.z),
      -1.0f,
      1.0f,
      0.0f,
      1.0f
    );
  }

  /// <summary>
  /// set up the noise type
  /// </summary>
  protected override void setUpNoise() {
    //noise.SetNoiseType(Noise.FastNoise.NoiseType.Cellular);
  }
}
