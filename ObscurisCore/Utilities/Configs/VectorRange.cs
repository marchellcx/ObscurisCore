using System.ComponentModel;

using UnityEngine;

namespace ObscurisCore.Utilities.Configs;

/// <summary>
/// Represents a range of Vector3 values with configurable minimum and maximum bounds.
/// </summary>
public class VectorRange
{
    /// <summary>
    /// The maximum X value of the range.
    /// </summary>
    [Description("The maximum X value of the range.")]
    public float X_Max { get; set; } = 1f;

    /// <summary>
    /// The minimum X value of the range.
    /// </summary>
    [Description("The minimum X value of the range.")]
    public float X_Min { get; set; } = 1f;

    /// <summary>
    /// The maximum Y value of the range.
    /// </summary>
    [Description("The maximum Y value of the range.")]
    public float Y_Max { get; set; } = 1f;

    /// <summary>
    /// The minimum Y value of the range.
    /// </summary>
    [Description("The minimum Y value of the range.")]
    public float Y_Min { get; set; } = 1f;

    /// <summary>
    /// The maximum Z value of the range.
    /// </summary>
    [Description("The maximum Z value of the range.")]
    public float Z_Max { get; set; } = 1f;

    /// <summary>
    /// The minimum Z value of the range.
    /// </summary>
    [Description("The minimum Z value of the range.")]
    public float Z_Min { get; set; } = 1f;

    /// <summary>
    /// Generates a random Vector3 with each component (X, Y, Z)
    /// within the defined ranges of the VectorRange properties.
    /// </summary>
    /// <returns>A Vector3 with random X, Y, and Z values within the specified ranges.</returns>
    public Vector3 GetRandom()
        => new(
            UnityEngine.Random.Range(X_Min, X_Max),
            UnityEngine.Random.Range(Y_Min, Y_Max),
            UnityEngine.Random.Range(Z_Min, Z_Max));
}