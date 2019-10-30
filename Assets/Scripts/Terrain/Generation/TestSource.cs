/// <summary>
/// just some test noise
/// </summary>
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
}
