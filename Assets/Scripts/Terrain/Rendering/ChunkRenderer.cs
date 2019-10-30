using UnityEngine;

[RequireComponent(typeof(MeshCollider))]
[RequireComponent(typeof(MeshFilter))]
public class ChunkRenderer : MonoBehaviour {

  /// <summary>
  /// The unity mesh filter
  /// </summary>
  MeshFilter meshFilter;

  /// <summary>
  /// The unity mesh collider
  /// </summary>
  MeshCollider meshCollider;

  /// <summary>
  /// The renderer algorhytm used to generate the mesh
  /// </summary>
  MarchRenderer blockRenderer;

  /// <summary>
  /// Override for iso surface level
  /// </summary>
  public float isoSurfaceLevelOverride = 0.5f;

  /// <summary>
  /// The block data for the chunk
  /// </summary>
  public IBlockStorage blockData;

  /// <summary>
  ///  set up the mesh filter
  /// </summary>
  private void Awake() {
    meshFilter = GetComponent<MeshFilter>();
    meshCollider = GetComponent<MeshCollider>();
  }

  // Use this for initialization
  void Start() {
    blockRenderer = new MarchRenderer();
    Mesh mesh = blockRenderer.generateMesh(blockData);

    meshFilter.sharedMesh = mesh;
    meshCollider.sharedMesh = mesh;
  }

  // Update is called once per frame
  void Update() {

  }

  private void OnMouseDown() {
    blockRenderer = new MarchRenderer();
    Mesh mesh = blockRenderer.generateMesh(blockData, isoSurfaceLevelOverride);

    meshFilter.sharedMesh = mesh;
    meshCollider.sharedMesh = mesh;
  }
}
