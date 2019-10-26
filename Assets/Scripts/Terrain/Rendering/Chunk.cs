using UnityEngine;
using System.Collections;

[RequireComponent(typeof(MeshCollider))]
[RequireComponent(typeof(MeshFilter))]
public class Chunk : MonoBehaviour {

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

  public float isoSurfaceLevelOverride = 0.5f;

  public float clippingLevel = 0.00001f;

  /// <summary>
  /// The block data
  /// </summary>
  public IBlockStorage blockData;

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
    Mesh mesh = blockRenderer.generateMesh(blockData, isoSurfaceLevelOverride, clippingLevel);

    meshFilter.sharedMesh = mesh;
    meshCollider.sharedMesh = mesh;
  }
}
