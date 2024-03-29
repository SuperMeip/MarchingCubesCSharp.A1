﻿using UnityEngine;

public class WorldTest : MonoBehaviour {

  public GameObject chunkPrefab;

  // Use this for initialization
  void Start() {
    RunLevelTest();
  }

  // Update is called once per frame
  void Update() {

  }

  void RunLevelTest() {
    Level<MarchingPointDictionary> level = 
      new ColumnLoadedLevel<MarchingPointDictionary>(
        (100, 16, 100),
        new WaveSource()
    );
    level.initializeAround((50, 0, 50));
  }

  void RunSingleTest() {
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
}
