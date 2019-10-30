// a group of blocks
/*class Chunk {
  public static final int CHUNK_SIZE = ...;
  private BlockStorage storage = new BlockStorage(CHUNK_SIZE pow 3);

  public void setBlock(x, y, z, BlockType type) {
    int index = /* turn x/y/z into a index */
    /*storage.setBlock(index, type);
  }

  public BlockType getBlock(x, y, z) {
    int index = /* turn x/y/z into a index */
    /*return storage.getBlock(index);
  }
}*/
using System.Collections;
using UnityEngine;

// the actual storage for blocks
/*public class BlockPalette : BlockStorage {
  /// <summary>
  /// An entry in the Palette structure
  /// </summary>
  struct PaletteEntry {
    /// <summary>
    /// The ref count
    /// </summary>
    public int refCount;

    /// <summary>
    /// The type of block this entry is for
    /// </summary>
    public Block.Type type;

    public PaletteEntry(int refCount, Block.Type blockType) {
      this.refCount = refCount;
      type = blockType;
    }
  }

  /// <summary>
  /// The max size of the block storage
  /// </summary>
  private readonly int size;

  /// <summary>
  /// The data array
  /// </summary>
  private IBitBuffer data;

  /// <summary>
  /// The data palette
  /// </summary>
  private PaletteEntry[] palette;

  /// <summary>
  /// The palettes stored
  /// </summary>
  private int paletteCount;

  /// <summary>
  /// The size of a single block index in bits
  /// </summary>
  private int indicesLength;

  public BlockPalette(int size) {
    this.size = size;
    indicesLength = 1;
    paletteCount = 0;
    palette = new PaletteEntry[(int)Mathf.Pow(2, indicesLength)];
    data = new BitArray(size * indicesLength); // the length is in bits, not bytes!
  }

  public void setBlock(int index, Block.Type type) {
    int paletteIndex = data.get(index * indicesLength, indicesLength);
    PaletteEntry current = palette[paletteIndex];

    // Whatever block is there will cease to exist in but a moment...
    current.refCount -= 1;

    // The following steps/choices *must* be ordered like they are.

    // --- Is the block-type already in the palette?
    int replace = palette.indexOf((entry)-> { entry.type.equals(type)});
    if (replace != -1) {
      // YES: Use the existing palette entry.
      data.set(index * indicesLength, indicesLength, replace);
      palette[replace].refCount += 1;
      return;
    }

    // --- Can we overwrite the current palette entry?
    if (current.refCount == 0) {
      // YES, we can!
      current.type = type;
      current.refCount = 1;
      return;
    }

    // --- A new palette entry is needed!

    // Get the first free palette entry, possibly growing the palette!
    int newEntry = newPaletteEntry();

    palette[newEntry] = new PaletteEntry() { refCount = 1, type = type };
    data.set(index * indicesLength, indicesLength, newEntry);
    paletteCount += 1;
  }

  public BlockType getBlock(int index) {
    int paletteIndex = data.get(index * indicesLength, indicesLength);
    return palette[paletteIndex].type;
  }

  private int newPaletteEntry() {
    int firstFree = palette.indexOf((entry)-> { entry == null || entry.refcount == 0});

    if (firstFree != -1) {
      return firstFree;
    }

    // No free entry?
    // Grow the palette, and thus the BitArray
    growPalette();

    // Just try again now!
    return newPaletteEntry();
  }

  private void growPalette() {
    // decode the indices
    int[] indices = new int[size];
    for (int i = 0; i < indices.length; i++) {
      indices[i] = data.get(i * indicesLength, indicesLength);
    }

    // Create new palette, doubling it in size
    indicesLength = indicesLength << 1;
    PaletteEntry[] newPalette = new PaletteEntry[2 pow indicesLength];
    System.arrayCopy(palette, 0, newPalette, 0, paletteCount);
    palette = newPalette;

    // Allocate new BitArray
    data = new BitArray(size * indicesLength); // the length is in bits, not bytes!

    // Encode the indices
    for (int i = 0; i < indices.length; i++) {
      data.set(i * indicesLength, indicesLength, indices[i]);
    }
  }

  // Shrink the palette (and thus the BitArray) every now and then.
  // You may need to apply heuristics to determine when to do this.
  public void fitPalette() {
    // Remove old entries...
    for (int i = 0; i < palette.length; i++) {
      if (palette[i].refCount == 0) {
        palette[i] = null;
        paletteCount -= 1;
      }
    }

    // Is the palette less than half of its closest power-of-two?
    if (paletteCount > powerOfTwo(paletteCount) / 2) {
      // NO: The palette cannot be shrunk!
      return;
    }

    // decode all indices
    int[] indices = new int[size];
    for (int i = 0; i < indices.length; i++) {
      indices[i] = data.get(i * indicesLength, indicesLength);
    }

    // Create new palette, halfing it in size
    indicesLength = indicesLength >> 1;
    PaletteEntry[] newPalette = new PaletteEntry[2 pow indicesLength];

    // We gotta compress the palette entries!
    int paletteCounter = 0;
    for (int pi = 0; pi < palette.length; pi++, paletteCounter++) {
      PaletteEntry entry = newPalette[paletteCounter] = palette[pi];

      // Re-encode the indices (find and replace; with limit)
      for (int di = 0, fc = 0; di < indices.length && fc < entry.refCount; di++) {
        if (pi == indices[di]) {
          indices[di] = paletteCounter;
          fc += 1;
        }
      }
    }

    // Allocate new BitArray
    data = new BitArray(size * indicesLength); // the length is in bits, not bytes!

    // Encode the indices
    for (int i = 0; i < indices.length; i++) {
      data.set(i * indicesLength, indicesLength, indices[i]);
    }
  }
}*/