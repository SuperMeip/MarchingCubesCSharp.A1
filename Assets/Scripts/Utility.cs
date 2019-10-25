using UnityEngine;
using System.Collections;

public static class Utility {

  /// <summary>
  /// fast clamp a float to between 0 and 1
  /// </summary>
  /// <param name="value"></param>
  /// <param name="minValue"></param>
  /// <param name="maxValue"></param>
  /// <returns></returns>
  public static float ClampToFloat(float value, int minValue, int maxValue) {
    return (
      (value - minValue)
      / (maxValue - minValue)
    );
  }

  /// <summary>
  /// fast clamp float to short
  /// </summary>
  /// <param name="value"></param>
  /// <param name="minFloat"></param>
  /// <param name="maxFloat"></param>
  /// <returns></returns>
  public static short ClampToShort(float value, float minFloat = 0.0f, float maxFloat = 1.0f) {
    return (short)((short.MaxValue - short.MinValue)
      * ((value - minFloat) / (maxFloat - minFloat))
      + short.MinValue);
  }

  /// <summary>
  /// Clamp a value between two numbers
  /// </summary>
  /// <param name="value"></param>
  /// <param name="startingMin"></param>
  /// <param name="startingMax"></param>
  /// <param name="targetMin"></param>
  /// <param name="targetMax"></param>
  /// <returns></returns>
  public static double Clamp(double value, double startingMin, double startingMax, double targetMin, double targetMax) {
    return (targetMax - targetMin)
      * ((value - startingMin) / (startingMax - startingMin))
      + targetMin;
  }

  /// <summary>
  /// Clamp the values between these numbers in a non scaling way.
  /// </summary>
  /// <param name="number"></param>
  /// <param name="min"></param>
  /// <param name="max"></param>
  /// <returns></returns>
  public static float Box(this float number, float min, float max) {
    if (number < min)
      return min;
    else if (number > max)
      return max;
    else
      return number;
  }

  /// <summary>
  /// Box a float between 0 and 1
  /// </summary>
  /// <param name="number"></param>
  /// <returns></returns>
  public static float Box01(this float number) {
    return Box(number, 0, 1);
  }

  /// <summary>
  /// Map values for terrain generation
  /// </summary>
  /// <param name="value"></param>
  /// <param name="x1"></param>
  /// <param name="y1"></param>
  /// <param name="x2"></param>
  /// <param name="y2"></param>
  /// <returns></returns>
  public static float GenMap(this float value, float x1, float y1, float x2, float y2) {
    return (value - x1) / (y1 - x1) * (y2 - x2) + x2;
  }

  /// <summary>
  /// Speedy absolute value function
  /// </summary>
  /// <param name="n"></param>
  /// <returns></returns>
  public static float Abs(this float n) {
    if (n < 0)
      return -n;
    else
      return n;
  }
}