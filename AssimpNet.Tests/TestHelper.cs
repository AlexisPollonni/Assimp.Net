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

using System;
using System.Collections.Generic;
using NUnit.Framework;
using TK = OpenTK;

namespace Assimp.Test;

/// <summary>
/// Helper for Assimp.NET testing.
/// </summary>
public static class TestHelper
{
    public const float DEFAULT_TOLERANCE = 0.000001f;
    public static readonly float Tolerance = DEFAULT_TOLERANCE;

    private static string m_rootPath;

    public static string RootPath
    {
        get
        {
            if(m_rootPath == null)
            {
                /*
                Assembly entryAssembly = Assembly.GetEntryAssembly();
                String dirPath = String.Empty;

                if(entryAssembly == null)
                    entryAssembly = Assembly.GetCallingAssembly();

                if(entryAssembly != null)
                    dirPath = Path.GetDirectoryName(entryAssembly.Location);

                m_rootPath = dirPath;*/

                m_rootPath = AppContext.BaseDirectory;
            }

            return m_rootPath;
        }
    }

    public static void AssertEquals(double expected, double actual)
    {
        Assert.That(Math.Abs(expected - actual), Is.LessThanOrEqualTo(Tolerance));
    }

    public static void AssertEquals(double expected, double actual, string msg)
    {
        Assert.That(Math.Abs(expected - actual), Is.LessThanOrEqualTo(Tolerance), msg);
    }

    public static void AssertEquals(float expected, float actual)
    {
        Assert.That(Math.Abs(expected - actual), Is.LessThanOrEqualTo(Tolerance));
    }

    public static void AssertEquals(float expected, float actual, string msg)
    {
        Assert.That(Math.Abs(expected - actual), Is.LessThanOrEqualTo(Tolerance), msg);
    }

    public static void AssertEquals(float x, float y, Vector2D v)
    {
        AssertEquals(x, v.X);
        AssertEquals(y, v.Y);
    }

    public static void AssertEquals(float x, float y, Vector2D v, string msg)
    {
        AssertEquals(x, v.X, msg + $" => checking X component ({x} == {v.X}");
        AssertEquals(y, v.Y, msg + $" => checking Y component ({y} == {v.Y}");
    }

    public static void AssertEquals(float x, float y, float z, Vector3D v)
    {
        AssertEquals(x, v.X);
        AssertEquals(y, v.Y);
        AssertEquals(z, v.Z);
    }

    public static void AssertEquals(float x, float y, float z, Vector3D v, string msg)
    {
        AssertEquals(x, v.X, msg + $" => checking X component ({x} == {v.X}");
        AssertEquals(y, v.Y, msg + $" => checking Y component ({y} == {v.Y}");
        AssertEquals(z, v.Z, msg + $" => checking Z component ({z} == {v.Z}");
    }

    public static void AssertEquals(float r, float g, float b, float a, Color4D c)
    {
        AssertEquals(r, c.R);
        AssertEquals(g, c.G);
        AssertEquals(b, c.B);
        AssertEquals(a, c.A);
    }

    public static void AssertEquals(float r, float g, float b, float a, Color4D c, string msg)
    {
        AssertEquals(r, c.R, msg + $" => checking R component ({r} == {c.R}");
        AssertEquals(g, c.G, msg + $" => checking G component ({g} == {c.G}");
        AssertEquals(b, c.B, msg + $" => checking B component ({b} == {c.B}");
        AssertEquals(a, c.A, msg + $" => checking A component ({a} == {c.A}");
    }

    public static void AssertEquals(float r, float g, float b, Color3D c)
    {
        AssertEquals(r, c.R);
        AssertEquals(g, c.G);
        AssertEquals(b, c.B);
    }

    public static void AssertEquals(float r, float g, float b, Color3D c, string msg)
    {
        AssertEquals(r, c.R, msg + $" => checking R component ({r} == {c.R}");
        AssertEquals(g, c.G, msg + $" => checking G component ({g} == {c.G}");
        AssertEquals(b, c.B, msg + $" => checking B component ({b} == {c.B}");
    }

    public static void AssertEquals(float x, float y, float z, float w, Quaternion q, string msg)
    {
        AssertEquals(x, q.X, msg + $" => checking X component ({x} == {q.X}");
        AssertEquals(y, q.Y, msg + $" => checking Y component ({y} == {q.Y}");
        AssertEquals(z, q.Z, msg + $" => checking Z component ({z} == {q.Z}");
        AssertEquals(w, q.W, msg + $" => checking W component ({w} == {q.W}");
    }

    public static void AssertEquals(TK.Matrix4 tkM, Matrix3x3 mat, string msg)
    {
        //Note: OpenTK 4x4 matrix is a row-vector matrix, so compare rows to AssimpNet Matrix3x3 columns
        var row0 = tkM.Row0;
        var row1 = tkM.Row1;
        var row2 = tkM.Row2;

        AssertEquals(row0.X, row0.Y, row0.Z, new Vector3D(mat.A1, mat.B1, mat.C1), msg + " => checking first column vector");
        AssertEquals(row1.X, row1.Y, row1.Z, new Vector3D(mat.A2, mat.B2, mat.C2), msg + " => checking second column vector");
        AssertEquals(row2.X, row2.Y, row2.Z, new Vector3D(mat.A3, mat.B3, mat.C3), msg + " => checking third column vector");
    }

    public static void AssertEquals(TK.Vector4 v1, TK.Vector4 v2, string msg)
    {
        AssertEquals(v1.X, v2.X, msg + $" => checking X component ({v1.X} == {v2.X}");
        AssertEquals(v1.Y, v2.Y, msg + $" => checking Y component ({v1.Y} == {v2.Y}");
        AssertEquals(v1.Z, v2.Z, msg + $" => checking Z component ({v1.Z} == {v2.Z}");
        AssertEquals(v1.W, v2.W, msg + $" => checking W component ({v1.W} == {v2.W}");
    }

    public static void AssertEquals(TK.Quaternion q1, Quaternion q2, string msg)
    {
        AssertEquals(q1.X, q2.X, msg + $" => checking X component ({q1.X} == {q2.X}");
        AssertEquals(q1.Y, q2.Y, msg + $" => checking Y component ({q1.Y} == {q2.Y}");
        AssertEquals(q1.Z, q2.Z, msg + $" => checking Z component ({q1.Z} == {q2.Z}");
        AssertEquals(q1.W, q2.W, msg + $" => checking W component ({q1.W} == {q2.W}");
    }

    public static void AssertEquals(TK.Matrix4 tkM, Matrix4x4 mat, string msg)
    {
        //Note: OpenTK 4x4 matrix is a row-vector matrix, so compare rows to AssimpNet Matrix4x4 columns
        AssertEquals(tkM.Row0, new(mat.A1, mat.B1, mat.C1, mat.D1), msg + " => checking first column vector");
        AssertEquals(tkM.Row1, new(mat.A2, mat.B2, mat.C2, mat.D2), msg + " => checking second column vector");
        AssertEquals(tkM.Row2, new(mat.A3, mat.B3, mat.C3, mat.D3), msg + " => checking third column vector");
        AssertEquals(tkM.Row3, new(mat.A4, mat.B4, mat.C4, mat.D4), msg + " => checking third column vector");
    }
        
    public static void Shuffle<T>(this IList<T> list, Random rng)
    {
        var n = list.Count;
        while (n > 1) {
            n--;
            var k = rng.Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }
}