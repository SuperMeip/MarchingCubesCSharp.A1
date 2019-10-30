using System.Collections;

/// <summary>
/// Interface for a bit buffer
/// </summary>
interface IBitBuffer {

  /// <summary>
  /// Set a bit of the given length to the given bit values
  /// </summary>
  /// <param name="bitIndex"></param>
  /// <param name="bitLength"></param>
  /// <param name="bits"></param>
  void set(int bitIndex, int bitLength, int bits);

  /// <summary>
  /// Get a bit of the given length from the bit values
  /// </summary>
  /// <param name="bitIndex"></param>
  /// <param name="bitLength"></param>
  /// <returns></returns>
  int get(int bitIndex, int bitLength);
}

/*public class BitBuffer : IBitBuffer  {

  BitArray bits;

  public BitBuffer(int size) {
    bits = new BitArray(size);
  }

  public int get(int bitIndex, int bitLength) {
    throw new System.NotImplementedException();
  }

  public void set(int bitIndex, int bitLength, int bits) {
    /// Make a mask of 1s of the length of the data at the given index.
    /// This represents what we want to fill in with the new data
    BitArray fillInMask;
    // reverse it so we have 0s in only the spots we want to nullify
    BitArray oneMask;
    // nullify the area containing the values we want to replace,
    // leaving only the other data
    BitArray otherValues;

    // trim the bit to add to the proper size using bitLength
    BitArray bitsToAdd;

    // shift the values we want to add in to the correct position
    BitArray valuesToAdd = bitsToAdd.LeftShift();
  }

  public static BitArray ShiftRight(this BitArray instance) {
    return new BitArray(new bool[] { false }.Concat(instance.Cast<bool>().Take(instance.Length - 1)).ToArray());
  }

  public static BitArray ShiftLeft(this BitArray instance) {
    return new BitArray((instance.Cast<bool>().Take(instance.Length - 1).ToArray()).Concat(new bool[] { newState }).ToArray());
  }
}
*/