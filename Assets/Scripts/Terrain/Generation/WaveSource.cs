using Block;

public class WaveSource : BlockSource {

  protected override float getNoiseValueAt(Coordinate location) {
    return location.y - noise.GetPerlin(location.x / 0.1f, location.z / 0.1f).GenMap(-1, 1, 0, 1) * 10 - 10;
  }
}
