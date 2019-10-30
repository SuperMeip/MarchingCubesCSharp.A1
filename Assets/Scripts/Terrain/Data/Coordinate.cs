
using System.Linq;
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Direction constants
/// </summary>
public static class Directions {

  /// <summary>
  /// A valid direction
  /// </summary>
  public class Direction {

    /// <summary>
    /// The id of the direction
    /// </summary>
    public int Value {
      get;
      private set;
    }

    /// <summary>
    /// The name of this direction
    /// </summary>
    public string Name {
      get;
      private set;
    }

    /// <summary>
    /// The x y z offset of this direction from the origin
    /// </summary>
    public Coordinate Offset {
      get => Offsets[Value];
    }

    /// <summary>
    /// Get the oposite of this direction
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    public Direction Reverse {
      get {
        if (Equals(North)) {
          return South;
        }
        if (Equals(South)) {
          return North;
        }
        if (Equals(East)) {
          return West;
        }
        if (Equals(West)) {
          return East;
        }
        if (Equals(Below)) {
          return Above;
        }

        return Below;
      }
    }

    internal Direction(int value, string name) {
      Value = value;
      Name = name;
    }

    /// <summary>
    /// Get the 4 octants for a given direction
    /// </summary>
    /// <returns></returns>
    public Octants.Octant[] getOctants() {
      if (Equals(North)) {
        return Octants.North;
      }
      if (Equals(South)) {
        return Octants.All.Except(Octants.North).ToArray();
      }
      if (Equals(East)) {
        return Octants.East;
      }
      if (Equals(West)) {
        return Octants.All.Except(Octants.East).ToArray();
      }
      if (Equals(Below)) {
        return Octants.All.Except(Octants.Top).ToArray();
      }

      return Octants.Top;
    }

    /// <summary>
    /// To string
    /// </summary>
    /// <returns></returns>
    public override string ToString() {
      return Name;
    }

    /// <summary>
    /// Override equals
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public override bool Equals(object obj) {
      return (obj != null) 
        && !GetType().Equals(obj.GetType()) 
        && ((Direction)obj).Value == Value;
    }
  }

  /// <summary>
  /// Z+
  /// </summary>
  public static Direction North = new Direction(0, "North");

  /// <summary>
  /// X+
  /// </summary>
  public static Direction East = new Direction(1, "East");

  /// <summary>
  /// Z-
  /// </summary>
  public static Direction South = new Direction(2, "South");

  /// <summary>
  /// X-
  /// </summary>
  public static Direction West = new Direction(3, "West");

  /// <summary>
  /// Y+
  /// </summary>
  public static Direction Above = new Direction(4, "Above");

  /// <summary>
  /// Y-
  /// </summary>
  public static Direction Below = new Direction(5, "Below");

  /// <summary>
  /// All the directions in order
  /// </summary>
  public static Direction[] All = new Direction[6] {
    North,
    East,
    South,
    West,
    Above,
    Below
  };

  /// <summary>
  /// The cardinal directions. Non Y related
  /// </summary>
  public static Direction[] Cardinal = new Direction[4] {
    North,
    East,
    South,
    West
  };

  /// <summary>
  /// The coordinate directional offsets
  /// </summary>
  public static Coordinate[] Offsets = new Coordinate[6] {
    (0,0,1),
    (1,0,0),
    (0,0,-1),
    (-1, 0, 0),
    (0, 1, 0),
    (0, -1, 0)
  };
}

/// <summary>
/// Valid octant constants
/// </summary>
public static class Octants {

  /// <summary>
  /// One of 8 cubes making up a larger cube
  /// </summary>
  public class Octant {

    /// <summary>
    /// The id value of the octant
    /// </summary>
    public int Value { get; private set; }

    /// <summary>
    /// if this is an eastern octant
    /// </summary>
    /// <returns></returns>
    public bool IsEastern {
      get {
        return Value == EastBottomSouth.Value
          || Value == EastBottomNorth.Value
          || Value == EastTopSouth.Value
          || Value == EastTopNorth.Value;
      }
    }

    /// <summary>
    /// if this is a nothern octant
    /// </summary>
    /// <returns></returns>
    public bool IsNorthern {
      get {
        return Value == EastTopNorth.Value
        || Value == EastBottomNorth.Value
        || Value == WestTopNorth.Value
        || Value == WestBottomNorth.Value;
      }
    }

    /// <summary>
    /// if this is an upper/top octant
    /// </summary>
    /// <returns></returns>
    public bool IsUpper {
      get {
        return Value == EastTopNorth.Value
        || Value == EastTopSouth.Value
        || Value == WestTopNorth.Value
        || Value == WestTopSouth.Value;
      }
    }

    /// <summary>
    /// Get the opposite/reversed octant around the origin
    /// </summary>
    public Octant Reverse {
      get {
        return Get(!IsEastern, !IsUpper, !IsNorthern);
      }
    }

    /// <summary>
    /// Get the coordinate plane offset from the 000 vertex of the cube of this octant
    /// </summary>
    public Coordinate Offset {
      get {
        return (
          IsEastern ? 1 : 0,
          IsUpper ? 1 : 0,
          IsNorthern ? 1 : 0
        );
      }
    }

    internal Octant(int value) {
      Value = value;
    }

    /// <summary>
    /// Make the correct octant based on given directions.
    /// </summary>
    /// <param name="isNorthern"></param>
    /// <param name="isEastern"></param>
    /// <param name="isUpper"></param>
    /// <returns></returns>
    public static Octant Get(bool isEastern = true, bool isUpper = true, bool isNorthern = true) {
      return isNorthern
        ? isEastern
          ? isUpper
            ? EastTopNorth
            : EastBottomNorth
          : isUpper
            ? WestTopNorth
            : WestBottomNorth
        : isEastern
          ? isUpper
            ? EastTopSouth
            : EastBottomSouth
          : isUpper
            ? WestTopSouth
            : WestBottomSouth;
    }

    /// <summary>
    /// Get the octant to the direction of the current octant
    /// </summary>
    /// <param name="direction"></param>
    /// <returns>the octant to the direction, or null if it's out of the current bounds</returns>
    public Octant toThe(Directions.Direction direction) {
      if (direction.Equals(Directions.North)) {
        if (IsNorthern) {
          return null;
        } else {
          return Get(IsEastern, IsUpper, true);
        }
      }
      if (direction.Equals(Directions.South)) {
        if (!IsNorthern) {
          return null;
        } else {
          return Get(IsEastern, IsUpper, false);
        }
      }
      if (direction.Equals(Directions.East)) {
        if (IsEastern) {
          return null;
        } else {
          return Get(true, IsUpper, IsNorthern);
        }
      }
      if (direction.Equals(Directions.West)) {
        if (!IsEastern) {
          return null;
        } else {
          return Get(false, IsUpper, IsNorthern);
        }
      }
      if (direction.Equals(Directions.Above)) {
        if (IsUpper) {
          return null;
        } else {
          return Get(IsEastern, true, IsNorthern);
        }
      }
      if (direction.Equals(Directions.Below)) {
        if (!IsUpper) {
          return null;
        } else {
          return Get(IsEastern, false, IsNorthern);
        }
      }

      return null;
    }

    /// <summary>
    /// To string
    /// </summary>
    /// <returns></returns>
    public override string ToString() {
      return (IsEastern ? "East" : "West") +
        (IsUpper ? "Top" : "Bottom") +
        (IsNorthern ? "North" : "South") +
        " {" +
        (IsEastern ? "+" : "-") +
        (IsUpper ? "+" : "-") +
        (IsNorthern ? "+" : "-") +
        "}";
    }
  }

  /// <summary>
  /// X-Y-Z-
  /// </summary>
  public static Octant WestBottomSouth = new Octant(0);

  /// <summary>
  /// X+Y-Z-
  /// </summary>
  public static Octant EastBottomSouth = new Octant(/*4*/1);

  /// <summary>
  /// X+Y-Z+
  /// </summary>
  public static Octant EastBottomNorth = new Octant(/*5*/2);

  /// <summary>
  /// X-Y-Z+
  /// </summary>
  public static Octant WestBottomNorth = new Octant(/*1*/3);

  /// <summary>
  /// X-Y+Z-
  /// </summary>
  public static Octant WestTopSouth = new Octant(/*2*/4);

  /// <summary>
  /// X+Y+Z-
  /// </summary>
  public static Octant EastTopSouth = new Octant(/*6*/5);

  /// <summary>
  /// X+Y+Z+
  /// </summary>
  public static Octant EastTopNorth = new Octant(/*7*/6);

  /// <summary>
  /// X-Y+Z+
  /// </summary>
  public static Octant WestTopNorth = new Octant(/*3*/7);

  /// <summary>
  /// All of the octants in order
  /// </summary>
  public static Octant[] All = new Octant[8] {
    WestBottomSouth,
    EastBottomSouth,
    EastBottomNorth,
    WestBottomNorth,
    WestTopSouth,
    EastTopSouth,
    EastTopNorth,
    WestTopNorth
  };

  /// <summary>
  /// All eastern octants
  /// </summary>
  public static Octant[] East = new Octant[4] {
    EastBottomSouth,
    EastBottomNorth,
    EastTopSouth,
    EastTopNorth
  };

  /// <summary>
  /// all northern octants
  /// </summary>
  public static Octant[] North = new Octant[4] {
    WestBottomNorth,
    WestTopNorth,
    EastBottomNorth,
    EastTopNorth
  };

  /// <summary>
  /// all upper/top octants
  /// </summary>
  public static Octant[] Top = new Octant[4] {
    WestTopSouth,
    WestTopNorth,
    EastTopSouth,
    EastTopNorth
  };
}

/// <summary>
/// A block position in a level
/// </summary>
public struct Coordinate {

  /// <summary>
  /// The coordinate for 0, 0, 0
  /// </summary>
  public static readonly Coordinate Zero = (0, 0, 0);

  // The coordinate values
  /// <summary>
  /// east west
  /// </summary>
  public int x;

  /// <summary>
  /// up down
  /// </summary>
  public int y;

  /// <summary>
  /// north south
  /// </summary>
  public int z;

  /// <summary>
  /// If this coordinate is valid and was properly initialized
  /// </summary>
  public bool isInitialized {
    get;
    private set;
  }

  /// <summary>
  /// This as a vector 3
  /// </summary>
  public Vector3 vec3 {
    get {
      var _vec3 = new Vector3(x, y, z);
      return _vec3;
    }
  }

  /// <summary>
  /// shortcut for geting just he X and Z of a coordinate
  /// </summary>
  public Coordinate xz {
    get => (x, 0, z);
  }

  /// <summary>
  /// Create a coordinate with one value for all 3
  /// </summary>
  /// <param name="xyz"></param>
  public Coordinate(int xyz) {
    x = y = z = xyz;
    isInitialized = true;
  }

  /// <summary>
  /// Create a 3d coordinate
  /// </summary>
  /// <param name="x"></param>
  /// <param name="y"></param>
  /// <param name="z"></param>
  public Coordinate(int x, int y = 0, int z = 0) {
    this.x = x;
    this.y = y;
    this.z = z;
    isInitialized = true;
  }

  /// <summary>
  /// Turn a set of coordinates into a coordinate.
  /// </summary>
  /// <param name="coordinates"></param>
  public static implicit operator Coordinate((int, int, int) coordinates) {
    return new Coordinate(coordinates.Item1, coordinates.Item2, coordinates.Item3);
  }

  /// <summary>
  /// Turn a set of coordinates into a coordinate.
  /// </summary>
  /// <param name="coordinates"></param>
  public static implicit operator Coordinate(Vector3 vector3) {
    return new Coordinate((int)vector3.x, (int)vector3.y, (int)vector3.z);
  }

  public static Coordinate operator +(Coordinate a, Coordinate b) {
    return (
      a.x + b.x,
      a.y + b.y,
      a.z + b.z
    );
  }

  public static Coordinate operator -(Coordinate a, Coordinate b) {
    return (
      a.x - b.x,
      a.y - b.y,
      a.z - b.z
    );
  }

  public static Coordinate operator +(Coordinate a, int b) {
    return (
      a.x + b,
      a.y + b,
      a.z + b
    );
  }

  public static Coordinate operator *(Coordinate a, int b) {
    return (
      a.x * b,
      a.y * b,
      a.z * b
    );
  }

  public static Coordinate operator -(Coordinate a, int b) {
    return a + (-b);
  }

  /// <summary>
  /// The unity/world position for this level location
  /// </summary>
  /*public Vector3 worldPosition {
    get {
      return new Vector3(
        x * Level.BLOCK_SIZE,
        y * Level.BLOCK_SIZE,
        z * Level.BLOCK_SIZE
      );
    }
  }*/

  /// <summary>
  /// Get the coordinate one over in another direction.
  /// </summary>
  /// <param name="direction">The direction to move in</param>
  /// <param name="magnitude">The distance to move</param>
  /// <returns>The coordinate one over in the requested direction</returns>
  public Coordinate go(Directions.Direction direction, byte magnitude) {
    if (direction.Equals(Directions.North)) {
      return (x, y, z + magnitude);
    }
    if (direction.Equals(Directions.South)) {
      return (x, y, z - magnitude);
    }
    if (direction.Equals(Directions.East)) {
      return (x + magnitude, y, z);
    }
    if (direction.Equals(Directions.West)) {
      return (x - magnitude, y, z);
    }
    if (direction.Equals(Directions.Below)) {
      return (x, y - magnitude, z);
    }
    if (direction.Equals(Directions.Above)) {
      return (x, y + magnitude, z);
    }

    return this;
  }

  /// <summary>
  /// Get the octant the given point would be in assuming this is the center.
  /// </summary>
  /// <param name="otherPoint"></param>
  /// <returns></returns>
  public Octants.Octant octantToDirectionOf(Coordinate otherPoint) {
    return Octants.Octant.Get(
      otherPoint.x >= x,
      otherPoint.y >= y,
      otherPoint.z >= z
    );
  }

  /// <summary>
  /// Get the distance between this and otherPoint
  /// </summary>
  /// <param name="otherPoint"></param>
  /// <returns></returns>
  public float distance(Coordinate otherPoint) {
    return Mathf.Sqrt(
      Mathf.Pow(x - otherPoint.x, 2)
      + Mathf.Pow(y - otherPoint.y, 2)
      + Mathf.Pow(z - otherPoint.z, 2)
    );
  }

  /// <summary>
  /// Add the value to x y and z of this coordinate
  /// </summary>
  /// <param name="i">the value to add to all axis</param>
  /// <returns></returns>
  public Coordinate plus(int i) {
    return new Coordinate(
      x + i,
      y + i,
      z + i
    );
  }

  /// <summary>
  /// Checks if this coordinate is within a bounds coodinate 
  /// </summary>
  public bool isWithin(Coordinate bounds) {
    return x < bounds.x
      && y < bounds.y
      && z < bounds.z;
  }
  
  /// <summary>
  /// Checks if this coordinate is greater than a lower bounds coordinate
  /// </summary>
  public bool isBeyond(Coordinate bounds) {
    return x > bounds.x
      && y > bounds.y
      && z > bounds.z;
  }

  /// <summary>
  /// preform the acton on all coordinates between this one and the end coordinate
  /// </summary>
  /// <param name="end"></param>
  /// <param name="action"></param>
  /// <param name="step">the value by which the coordinate values are incrimented</param>
  public void until(Coordinate end, Action<Coordinate> action, int step = 1) {
    Coordinate current = (x, y, z);
    for (current.x = x; current.x < end.x; current.x += step) {
      for (current.y = y; current.y < end.y; current.y += step) {
        for (current.z = z; current.z < end.z; current.z += step) {
          action(current);
        }
      }
    }
  }

  /// <summary>
  /// preform the acton on all coordinates between this one and the end coordinate
  /// </summary>
  /// <param name="end"></param>
  /// <param name="action"></param>
  /// <param name="step">the value by which the coordinate values are incrimented</param>
  public void until(Coordinate end, Func<Coordinate, bool> action, int step = 1) {
    Coordinate current = (x, y, z);
    for (current.x = x; current.x < end.x; current.x += step) {
      for (current.y = y; current.y < end.y; current.y += step) {
        for (current.z = z; current.z < end.z; current.z += step) {
          if (!action(current)) {
            return;
          }
        }
      }
    }
  }

  /// <summary>
  /// Get all the points within two sets of bounds
  /// </summary>
  /// <param name="westBottomSouthBound"> the lesser bound, -,-,-</param>
  /// <param name="eastTopNorthBound">the greater bound, +,+,+</param>
  /// <returns>All points between these bounds</returns>
  public static Coordinate[] GetAllPointsBetween(Coordinate westBottomSouthBound, Coordinate eastTopNorthBound) {
    List<Coordinate> points = new List<Coordinate>();
    westBottomSouthBound.until(eastTopNorthBound, (coordinate) => {
      points.Add(coordinate);
    });

    return points.ToArray();
  }

  public override string ToString() {
    return "{" + x.ToString() + ", " + y.ToString() + ", " + z.ToString() + "}";
  }

  /// <summary>
  /// Hash this coord, only works up to 256,256,256
  /// </summary>
  /// <returns></returns>
  public override int GetHashCode() {
    int hash = 0;
    hash |= (((byte)x) << 8);
    hash |= (((byte)y) << 16);
    hash |= (((byte)z) << 24);

    return hash;
  }

  public override bool Equals(object obj) {
    Coordinate otherCoordinate = (Coordinate)obj;
    return otherCoordinate.x == x
      && otherCoordinate.y == y
      && otherCoordinate.z == z
      && otherCoordinate.isInitialized == isInitialized;
  }
}

public static class Vector3Extension {

  /// <summary>
  /// Get the level coordinate given a unity vector 3 position.
  /// </summary>
  /// <param name="v"></param>
  /// <returns></returns>
  /*public static Coordinate levelCoordinate(this Vector3 v) {
    return (v / Level.BLOCK_SIZE).toCoordinate();
  }*/

  /// <summary>
  /// Get a coordinate for the exact values in this vector 3.
  /// </summary>
  /// <param name="v"></param>
  /// <returns></returns>
  private static Coordinate toCoordinate(this Vector3 v) {
    return new Coordinate((int)v.x, (int)v.y, (int)v.z);
  }
}
