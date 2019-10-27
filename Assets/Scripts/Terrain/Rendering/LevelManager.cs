using UnityEngine;
using System.Collections.Generic;

public class LevelManager : MonoBehaviour {

  public const int RenderedChunkDiameter = 30;

  Level level;

  Dictionary<Coordinate, ChunkRenderer> chunkRendererPool;

  /// <summary>
  /// The queue of chunks that need to be rendered.
  /// </summary>
  List<Coordinate> chunkRenderQueue;

  /// <summary>
  /// The chunks that still need to be loaded.
  /// </summary>
  List<Coordinate> chunkLoadingQueue;

  public void Awake() {
    chunkRendererPool = new Dictionary<Coordinate, ChunkRenderer>();
    chunkRenderQueue  = new List<Coordinate>();
  }

  void queChunkForRender(Coordinate location) {
    IBlockStorage chunkBlockData = level.getChunk(location);
  }
}
