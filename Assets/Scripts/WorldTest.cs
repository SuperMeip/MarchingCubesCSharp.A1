using UnityEngine;

public class WorldTest : MonoBehaviour {

  public GameObject chunkPrefab;

  // Use this for initialization
  void Start() {
    IBlockSource source = new TestSource();
    MarchingPointDictionary blockData = new MarchingPointDictionary((64, 64, 64));
    source.generateAll(blockData);

    ChunkRenderer chunk = Instantiate(
      chunkPrefab,
      Vector3.zero,
      Quaternion.identity
    ).GetComponent<ChunkRenderer>();
    chunk.blockData = blockData;
  }

  // Update is called once per frame
  void Update() {

  }
}
