using UnityEngine;

namespace ObscurisCore.Extensions
{
    /// <summary>
    /// Extensions targeting the Unity Engine.
    /// </summary>
    public static class UnityExtensions
    {
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
    }
}
