using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Used for interfacing with blocks
/// </summary>
namespace Block {

  /// <summary>
  /// A class for storing the values of each type of block
  /// </summary>
  public abstract class Type {

    /// <summary>
    /// The ID of the block
    /// </summary>
    public byte Id {
      get;
      protected set;
    }

    /// <summary>
    /// If this block type is solid block or not
    /// </summary>
    public bool IsSolid {
      get;
      protected set;
    } = true;

    /// <summary>
    /// How hard/solid this block is. 0 is air.
    /// </summary>
    public byte Density {
      get;
      protected set;
    } = 64;

    internal Type(byte id) {
      Id = id;
    }

    public static Type Get(byte id) {
      return Types.All[id];
    }
  }

  /// <summary>
  /// An air block, empty
  /// </summary>
  public class Air : Type {

    internal Air() : base(0) {
      Density = 0;
      IsSolid = false;
    }
  }

  /// <summary>
  /// An empty block that's not air.
  /// Counts as solid but doesn't render
  /// </summary>
  public class Placeholder : Type {
    internal Placeholder() : base(1) { }
  }

  /// <summary>
  /// Stone, a solid rock block
  /// </summary>
  public class Stone : Type {

    internal Stone() : base(2) {
      Density = 128;
    }
  }

  public static class Types {

    /// <summary>
    /// Air, an empty block
    /// </summary>
    public static Type Air = new Air();

    /// <summary>
    /// Stone, a solid rock block
    /// </summary>
    public static Type Stone = new Stone();

    /// <summary>
    /// Stone, a solid rock block
    /// </summary>
    public static Type Placeholder = new Placeholder();

    /// <summary>
    /// All block types by id
    /// </summary>
    public static Type[] All = {
      Air,
      Placeholder,
      Stone
    };

    public static Type Get(byte id) {
      return All[id];
    }
  }

  /// <summary>
  /// Block data is stored in ints:
  /// 00110100 00001011
  /// The first byte is used for the block type id byte.
  /// the second byte is used for the block's vertex solidity mask.
  /// the last 2 bytes (3 and 4) are used for the solidity value
  /// </summary>
  public static class IntBlockExtensions {

    /// <summary>
    /// Get the block type for this int
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static Block.Type GetBlockType(this int value) {
      return Block.Type.Get(value.GetBlockTypeId());
    }

    /// <summary>
    /// Get the block type id from a int value representing a block
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static byte GetBlockTypeId(this int value) {
      return (byte)(value);
    }

    /// <summary>
    /// Get the id of this block type
    /// </summary>
    /// <param name="value"></param>
    /// <param name="blockId"></param>
    /// <returns></returns>
    public static int SetBlockTypeId(this int value, byte blockId) {
      int otherValues = value & ~0xFF;
      return otherValues | blockId;
    }

    /// <summary>
    /// Get if this block's given vertex is within the isosurface and is 'solid'
    /// </summary>
    /// <param name="value"></param>
    /// <param name="octant"></param>
    /// <returns>If the vertex's value is solid</returns>
    public static bool BlockVertexIsSolid(this int value, Octants.Octant octant) {
      // Get the mask of neighboring block data from the int
      return (
        // trim the neighbor's mask off of the full block data int value
        ((byte)(value >> 8))
          // create a mask (ex: 000100) with the 1 in the spot = to the value of the 
          // direction we want to test for, and & compare them.
          & (1 << (octant.Value))
      // if it is not = 000000 after the and, the bit is set.
      ) != 0;
    }

    /// <summary>
    /// Set this block's vertex 'is solid' flag for the given octant
    /// </summary>
    /// <param name="value"></param>
    /// <param name="octant"></param>
    /// <param name="toTrue"></param>
    /// <returns></returns>
    public static int SetVertexMaskForOctant(this int value, Octants.Octant octant, bool toTrue = true) {
      int mask = 1 << (octant.Value + 8);
      if (toTrue) {
        return value | mask;
      }

      return value & ~mask;
    }

    /// <summary>
    /// Get the entire block vertex mask from the block data
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static byte GetBlockVertexMask(this int value) {
      return (byte)((value & 0x0000FF00) >> 8);
    }

    /// <summary>
    /// Get the scalar density from the block data compressed as a short
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static short GetBlockScalarDensity(this int value) {
      return (short)((value & 0xFFFF0000) >> 16);
    }

    /// <summary>
    /// Get the scalar density as a float from the block data
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static float GetBlockScalarDensityFloat(this int value) {
      return Utility.ClampToFloat(value.GetBlockScalarDensity(), short.MinValue, short.MaxValue);
    }

    /// <summary>
    /// Set just the scalar density short from a float
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static int SetBlockScalarDensity(this int value, float scalarDensity) {
      int otherValueMask = ((0xFF << 8) | 0xFF) << 16;
      int otherValues = value & ~(otherValueMask);
      return otherValues | (int)Utility.ClampToShort(scalarDensity) << 16;
    }

    /// <summary>
    /// Set just the scalar density short on the blockdata
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static int SetBlockScalarDensity(this int value, short scalarDensity) {
      int otherValueMask = ((0xFF << 8) | 0xFF) << 16;
      int otherValues = value & ~(otherValueMask);
      return otherValues | (int)scalarDensity << 16;
    }

    /// <summary>
    /// Get if this block's face in the given direction is exposed to air/transparent blocks
    /// </summary>
    /// <param name="value"></param>
    /// <param name="direction"></param>
    /// <returns>If the neighbor to the given direction is solid</returns>
    public static bool BlockFaceIsExposed(this int value, Directions.Direction direction) {
      // Get the mask of neighboring block data from the int
      return (
        // trim the neighbor's mask off of the full block data int value
        ((byte)(value >> 8))
          // create a mask (ex: 000100) with the 1 in the spot = to the value of the 
          // direction we want to test for, and & compare them.
          & (1 << (direction.Value))
      // if it is not = 000000 after the and, the bit is set.
      ) != 0;
    }
  }
}