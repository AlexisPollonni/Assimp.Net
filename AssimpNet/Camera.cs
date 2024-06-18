/*
 * Copyright (c) 2012-2020 AssimpNet - Nicholas Woodfield
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using System.Numerics;

namespace Assimp;

/// <summary>
/// Describes a right-handed camera in the scene. An important aspect is that
/// the camera itself is also part of the scenegraph, meaning any values such
/// as the direction vector are not *absolute*, they can be relative to the coordinate
/// system defined by the node which corresponds to the camera. This allows for camera
/// animations.
/// </summary>
public sealed class Camera : IMarshalable<Camera, AiCamera>
{
    /// <summary>
    /// Gets or sets the name of the camera. This corresponds to a node in the
    /// scenegraph with the same name. This node specifies the position of the
    /// camera in the scene hierarchy and can be animated.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the position of the camera relative to the coordinate space defined by
    /// the corresponding node. THe default value is 0|0|0.
    /// </summary>
    public Vector3 Position { get; set; }

    /// <summary>
    /// Gets or sets the 'up' vector of the camera, relative to the coordinate space defined by the
    /// corresponding node. The 'right' vector of the camera is the cross product of the up
    /// and direction vectors. The default value is 0|1|0.
    /// </summary>
    public Vector3 Up { get; set; }

    /// <summary>
    /// Gets or sets the viewing direction of the camera, relative to the coordinate space defined by the corresponding node.
    /// The default value is 0|0|1.
    /// </summary>
    public Vector3 Direction { get; set; }

    /// <summary>
    /// Gets or sets the half horizontal field of view angle, in radians. The FoV angle is
    /// the angle between the center line of the screen and the left or right border. The default
    /// value is 1/4PI.
    /// </summary>
    public float FieldOfview { get; set; }

    /// <summary>
    /// Gets or sets the distance of the near clipping plane from the camera. The value may not
    /// be 0.0f for arithmetic reasons to prevent a division through zero. The default value is 0.1f;
    /// </summary>
    public float ClipPlaneNear { get; set; }

    /// <summary>
    /// Gets or sets the distance of the far clipping plane from the camera. The far clippling plane must
    /// be further than the near clippling plane. The default value is 1000.0f. The ratio between
    /// the near and far plane should not be too large (between 1000 - 10000 should be ok) to avoid
    /// floating-point inaccuracies which can lead to z-fighting.
    /// </summary>
    public float ClipPlaneFar { get; set; }

    /// <summary>
    /// Gets or sets the screen aspect ratio. This is the ratio between the width and height of the screen. Typical
    /// values are 4/3, 1/2, or 1/1. This value is 0 if the aspect ratio is not defined in the source file.
    /// The default value is zero.
    /// </summary>
    public float AspectRatio { get; set; }

    /// <summary>
    /// Gets a right-handed view matrix.
    /// </summary>
    public Matrix4x4 ViewMatrix
    {
        get
        {
            var zAxis = Direction;
            zAxis=Vector3.Normalize(zAxis);
            var yAxis = Up;
            yAxis=Vector3.Normalize(yAxis);
            var xAxis = Vector3.Cross(Up, Direction);
            xAxis = Vector3.Normalize(xAxis);

            //Assimp docs *say* they deal with Row major matrices,
            //but aiCamera.h has this calc done with translation in the 4th column
            Matrix4x4 mat;
            mat.M11 = xAxis.X;
            mat.M12 = xAxis.Y;
            mat.M13 = xAxis.Z;
            mat.M14 = 0;

            mat.M21 = yAxis.X;
            mat.M22 = yAxis.Y;
            mat.M23 = yAxis.Z;
            mat.M24 = 0;

            mat.M31 = zAxis.X;
            mat.M32 = zAxis.Y;
            mat.M33 = zAxis.Z;
            mat.M34 = 0;

            mat.M41 = -Vector3.Dot(xAxis, Position);
            mat.M42 = -Vector3.Dot(yAxis, Position);
            mat.M43 = -Vector3.Dot(zAxis, Position);
            mat.M44 = 1.0f;

            return mat;
        }
    }

    /// <summary>
    /// Constructs a new instance of the <see cref="Camera"/> class.
    /// </summary>
    public Camera()
    {
        Name = string.Empty;
    }

    #region IMarshalable Implementation

    /// <summary>
    /// Gets if the native value type is blittable (that is, does not require marshaling by the runtime, e.g. has MarshalAs attributes).
    /// </summary>
    bool IMarshalable<Camera, AiCamera>.IsNativeBlittable => true;

    /// <summary>
    /// Writes the managed data to the native value.
    /// </summary>
    /// <param name="thisPtr">Optional pointer to the memory that will hold the native value.</param>
    /// <param name="nativeValue">Output native value</param>
    void IMarshalable<Camera, AiCamera>.ToNative(nint thisPtr, out AiCamera nativeValue)
    {
        nativeValue = new()
        {
            MName = new(Name),
            MPosition = Position,
            MLookAt = Direction,
            MUp = Up,
            MHorizontalFOV = FieldOfview,
            MClipPlaneFar = ClipPlaneFar,
            MClipPlaneNear = ClipPlaneNear,
            MAspect = AspectRatio
        };
    }

    /// <summary>
    /// Reads the unmanaged data from the native value.
    /// </summary>
    /// <param name="nativeValue">Input native value</param>
    void IMarshalable<Camera, AiCamera>.FromNative(in AiCamera nativeValue)
    {
        Name = nativeValue.MName;
        Position = nativeValue.MPosition;
        Direction = nativeValue.MLookAt;
        Up = nativeValue.MUp;
        FieldOfview = nativeValue.MHorizontalFOV;
        ClipPlaneFar = nativeValue.MClipPlaneFar;
        ClipPlaneNear = nativeValue.MClipPlaneNear;
        AspectRatio = nativeValue.MAspect;
    }

    /// <summary>
    /// Frees unmanaged memory created by <see cref="IMarshalable{Camera, AiCamera}.ToNative"/>.
    /// </summary>
    /// <param name="nativeValue">Native value to free</param>
    /// <param name="freeNative">True if the unmanaged memory should be freed, false otherwise.</param>
    public static void FreeNative(nint nativeValue, bool freeNative)
    {
        if(nativeValue != nint.Zero && freeNative)
            MemoryHelper.FreeMemory(nativeValue);
    }

    #endregion
}