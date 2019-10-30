using UnityEngine;
using System.Collections;

public class Octree<TType> {

#pragma warning disable CS0693 // Type parameter has the same name as the type parameter from outer type
  public class OctreeNode<TType> {
#pragma warning restore CS0693 // Type parameter has the same name as the type parameter from outer type
    
    /// <summary>
    /// The diameter of this node
    /// </summary>
    //byte diameter = 0;

    /// <summary>
    /// The center of this node
    /// </summary>
    //Coordinate center;

    /// <summary>
    /// the child nodes of this node
    /// </summary>
    //OctreeNode<TType>[] children;
  }
}
