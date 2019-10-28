using UnityEngine;
using System.Collections.Generic;

public class LevelManager : MonoBehaviour {

  public const int RenderedChunkDiameter = 30;

  Level<MarchingPointDictionary> level;

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

  public void Initialize(Coordinate focusChunkLocation) {

  }
}
