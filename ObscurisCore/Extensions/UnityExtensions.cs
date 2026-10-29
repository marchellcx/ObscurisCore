using UnityEngine;

namespace ObscurisCore.Extensions;

/// <summary>
/// Extensions targeting the Unity Engine.
/// </summary>
public static class UnityExtensions
{
    /// <summary>
    /// Rotates the given Quaternion by a specified angle around the Y axis.
    /// </summary>
    /// <param name="quaternion">The original Quaternion to be rotated.</param>
    /// <param name="rotationAngle">The angle, in degrees, by which to rotate the Quaternion around the Y axis. This value is multiplied by Time.deltaTime to ensure frame-rate independence.</param>
    /// <returns>A new Quaternion representing the original Quaternion after being rotated by the specified angle around the Y axis.</returns>
    public static Quaternion Rotate(this Quaternion quaternion, float rotationAngle)
    {
        return quaternion *
               Quaternion.Inverse(quaternion)
               * Quaternion.Euler(0f, rotationAngle * Time.deltaTime, 0f)
               * quaternion;
    }
    
    /// <summary>
    /// Makes the Y component of the given Vector3 oscillate over time based on a sine wave function.
    /// </summary>
    /// <param name="vector">The original Vector3 to be adjusted.</param>
    /// <param name="initialYPosition">The initial Y position to serve as the base for oscillation.</param>
    /// <param name="floatSpeed">The speed of the oscillation, determining how quickly the value oscillates over time.</param>
    /// <param name="floatAmplitude">The amplitude of the oscillation, determining the maximum displacement of the Y component.</param>
    /// <returns>A new Vector3 where the Y component oscillates between initialYPosition ± floatAmplitude, while other components remain unchanged.</returns>
    public static Vector3 Float(this Vector3 vector, float initialYPosition, float floatSpeed, float floatAmplitude)
    {
        vector.y = initialYPosition + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        return vector;
    }
    
    /// <summary>
    /// Returns a new Vector3 with its Y component adjusted by the specified value.
    /// </summary>
    /// <param name="vector">The Vector3 instance whose Y component is to be adjusted.</param>
    /// <param name="y">The value to add to the Y component of the vector.</param>
    /// <returns>A Vector3 with its Y component increased by the specified value, while the X and Z components remain unchanged.</returns>
    public static Vector3 AdjustY(this Vector3 vector, float y)
        => new(vector.x, vector.y + y, vector.z);

    /// <summary>
    /// Returns a new Vector3 in which the specified value is added to each non-zero axis of the input vector.
    /// </summary>
    /// <param name="vector">The Vector3 instance to which the value will be added. Components with a value of 0 are not modified.</param>
    /// <param name="num">The value to add to each non-zero component of the vector.</param>
    /// <returns>A Vector3 whose non-zero components are increased by the specified value. Components that are zero remain
    /// unchanged.</returns>
    public static Vector3 NotNullAdd(this Vector3 vector, float num)
    {
        if (vector.x != 0f) vector.x += num;
        if (vector.y != 0f) vector.y += num;
        if (vector.z != 0f) vector.z += num;

        return vector;
    }

    /// <summary>
    /// Returns a new Vector3 whose non-zero components are multiplied by the specified scalar value.
    /// </summary>
    /// <remarks>This method does not modify the original vector. Only components that are not equal
    /// to zero are affected by the multiplication.</remarks>
    /// <param name="vector">The Vector3 instance whose components are to be conditionally multiplied.</param>
    /// <param name="num">The scalar value by which to multiply each non-zero component of the vector.</param>
    /// <returns>A Vector3 with each non-zero component multiplied by the specified scalar value. Components with a value of
    /// zero remain unchanged.</returns>
    public static Vector3 NotNullMultiply(this Vector3 vector, float num)
    {
        if (vector.x != 0f) vector.x *= num;
        if (vector.y != 0f) vector.y *= num;
        if (vector.z != 0f) vector.z *= num;

        return vector;
    }

    /// <summary>
    /// Attempts to find a component of type T on the given GameObject or its child/parent objects.
    /// </summary>
    /// <param name="gameObject">The GameObject instance on which the search for the component is performed.</param>
    /// <param name="component">The output parameter that will hold the reference to the found component, if any.</param>
    /// <typeparam name="T">The type of the component to search for.</typeparam>
    /// <returns>True if a component of type T is found; otherwise, false.</returns>
    public static bool TryFindComponent<T>(this GameObject gameObject, out T component)
    {
        if (gameObject.TryGetComponent<T>(out component))
            return true;

        if ((component = gameObject.GetComponentInChildren<T>()) != null)
            return true;

        return (component = gameObject.GetComponentInParent<T>()) != null;
    }
}
