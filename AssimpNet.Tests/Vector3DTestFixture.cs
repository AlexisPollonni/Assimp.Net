﻿/*
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
using NUnit.Framework;

namespace Assimp.Test;

[TestFixture]
public class Vector3DTestFixture
{
    [Test]
    public void TestIndexer()
    {
        const float x = 1, y = 2, z = 3;
        var v = new Vector3D
        {
            [0] = x,
            [1] = y,
            [2] = z
        };
        TestHelper.AssertEquals(x, v[0], "Test Indexer, X");
        TestHelper.AssertEquals(y, v[1], "Test Indexer, Y");
        TestHelper.AssertEquals(z, v[2], "Test Indexer, Z");
    }

    [Test]
    public void TestSet()
    {
        const float x = 10.5f, y = 109.21f, z = 100;
        var v = new Vector3D();
        v.Set(x, y, z);

        TestHelper.AssertEquals(x, y, z, v, "Test v.Set()");
    }

    [Test]
    public void TestEquals()
    {
        float x = 1, y = 2, z = 5;
        float x2 = 3, y2 = 4, z2 = 10;

        var v1 = new Vector3D(x, y, z);
        var v2 = new Vector3D(x, y, z);
        var v3 = new Vector3D(x2, y2, z2);

        //Test IEquatable Equals
        Assert.That(v1, Is.EqualTo(v2), "Test IEquatable equals");
        Assert.That(v1, Is.Not.EqualTo(v3), "Test IEquatable equals");

        //Test object equals override
        Assert.That(v1, Is.EqualTo(v2), "Tests object equals");
        Assert.That(v1, Is.Not.EqualTo(v3), "Tests object equals");

        //Test op equals
        Assert.That(v1, Is.EqualTo(v2), "Testing OpEquals");
        Assert.That(v1, Is.Not.EqualTo(v3), "Testing OpEquals");

        //Test op not equals
        Assert.That(v1, Is.Not.EqualTo(v3), "Testing OpNotEquals");
        Assert.That(v1, Is.EqualTo(v2), "Testing OpNotEquals");
    }

    [Test]
    public void TestLength()
    {
        float x = -62, y = 5, z = 10;

        var v = new Vector3D(x, y, z);
        Assert.That(v.Length(), Is.EqualTo((float) Math.Sqrt(x * x + y * y + z * z)), "Testing v.Length()");
    }

    [Test]
    public void TestLengthSquared()
    {
        float x = -5, y = 25f, z = 7;

        var v = new Vector3D(x, y, z);
        Assert.That(v.LengthSquared(), Is.EqualTo(x * x + y * y + z * z), "Testing v.LengthSquared()");
    }

    [Test]
    public void TestNegate()
    {
        float x = 2, y = 5, z = -5;

        var v = new Vector3D(x, y, z);
        v.Negate();
        TestHelper.AssertEquals(-x, -y, -z, v, "Testing v.Negate()");
    }

    [Test]
    public void TestNormalize()
    {
        float x = 5, y = 12, z = 2;
        var v = new Vector3D(x, y, z);
        v.Normalize();
        var invLength = 1.0f / (float) Math.Sqrt(x * x + y * y + z * z);
        x *= invLength;
        y *= invLength;
        z *= invLength;

        TestHelper.AssertEquals(x, y, z, v, "Testing v.Normalize()");
    }

    [Test]
    public void TestOpAdd()
    {
        float x1 = 2, y1 = 5, z1 = 10;
        float x2 = 10, y2 = 15, z2 = 5.5f;
        var x = x1 + x2;
        var y = y1 + y2;
        var z = z1 + z2;

        var v1 = new Vector3D(x1, y1, z1);
        var v2 = new Vector3D(x2, y2, z2);

        var v = v1 + v2;

        TestHelper.AssertEquals(x, y, z, v, "Testing v1 + v2");
    }

    [Test]
    public void TestOpSubtract()
    {
        float x1 = 2, y1 = 5, z1 = 10;
        float x2 = 10, y2 = 15, z2 = 5.5f;
        var x = x1 - x2;
        var y = y1 - y2;
        var z = z1 - z2;

        var v1 = new Vector3D(x1, y1, z1);
        var v2 = new Vector3D(x2, y2, z2);

        var v = v1 - v2;

        TestHelper.AssertEquals(x, y, z, v, "Testing v1 - v2");
    }

    [Test]
    public void TestOpNegate()
    {
        float x = 22, y = 75, z = -5;

        var v = -new Vector3D(x, y, z);

        TestHelper.AssertEquals(-x, -y, -z, v, "Testing -v)");
    }

    [Test]
    public void TestOpMultiply()
    {
        float x1 = 2, y1 = 5, z1 = 10;
        float x2 = 10, y2 = 15, z2 = 5.5f;
        var x = x1 * x2;
        var y = y1 * y2;
        var z = z1 * z2;

        var v1 = new Vector3D(x1, y1, z1);
        var v2 = new Vector3D(x2, y2, z2);

        var v = v1 * v2;

        TestHelper.AssertEquals(x, y, z, v, "Testing v1 * v2");
    }

    [Test]
    public void TestOpMultiplyByScalar()
    {
        float x1 = 2, y1 = 5, z1 = -10;
        float scalar = 25;

        var x = x1 * scalar;
        var y = y1 * scalar;
        var z = z1 * scalar;

        var v1 = new Vector3D(x1, y1, z1);

        //Left to right
        var v = v1 * scalar;
        TestHelper.AssertEquals(x, y, z, v, "Testing v * scale");

        //Right to left
        v = scalar * v1;
        TestHelper.AssertEquals(x, y, z, v, "Testing scale * v");
    }

    [Test]
    public void TestOpDivide()
    {
        float x1 = 105f, y1 = 4.5f, z1 = -20;
        float x2 = 22f, y2 = 25.2f, z2 = 10;

        var x = x1 / x2;
        var y = y1 / y2;
        var z = z1 / z2;

        var v1 = new Vector3D(x1, y1, z1);
        var v2 = new Vector3D(x2, y2, z2);

        var v = v1 / v2;

        TestHelper.AssertEquals(x, y, z, v, "Testing v1 / v2");
    }

    [Test]
    public void TestOpDivideByFactor()
    {
        float x1 = 55f, y1 = 2f, z1 = 50f;
        var divisor = 5f;

        var x = x1 / divisor;
        var y = y1 / divisor;
        var z = z1 / divisor;

        var v = new Vector3D(x1, y1, z1) / divisor;

        TestHelper.AssertEquals(x, y, z, v, "Testing v / divisor");
    }

    [Test]
    public void TestOpTransformBy3X3()
    {
        float m11 = 2, m12 = .2f, m13 = 0;
        float m21 = .2f, m22 = 2, m23 = 0;
        float m31 = 0, m32 = 0, m33 = 2;

        float x1 = 2, y1 = 2.5f, z1 = 52;

        var m = new Matrix3x3(m11, m12, m13, m21, m22, m23, m31, m32, m33);
        var v = new Vector3D(x1, y1, z1);

        var transformedV = m * v;

        var x = x1 * m11 + y1 * m12 + z1 * m13;
        var y = x1 * m21 + y1 * m22 + z1 * m23;
        var z = x1 * m31 + y1 * m32 + z1 * m33;

        TestHelper.AssertEquals(x, y, z, transformedV, "Testing vector transform by Matrix 3x3");
    }

    [Test]
    public void TestOpTransformBy4X4()
    {
        float m11 = 2, m12 = .2f, m13 = 0, m14 = 0;
        float m21 = .2f, m22 = 2, m23 = 0, m24 = 0;
        float m31 = 0, m32 = 0, m33 = 2, m34 = 0;
        float m41 = 2, m42 = 3, m43 = 5, m44 = 1;

        float x1 = 2, y1 = 2.5f, z1 = 52;

        var m = new Matrix4x4(m11, m12, m13, m14, m21, m22, m23, m24, m31, m32, m33, m34, m41, m42, m43, m44);
        var v = new Vector3D(x1, y1, z1);

        var transformedV = m * v;

        var x = x1 * m11 + y1 * m12 + z1 * m13 + m14;
        var y = x1 * m21 + y1 * m22 + z1 * m23 + m24;
        var z = x1 * m31 + y1 * m32 + z1 * m33 + m34;

        TestHelper.AssertEquals(x, y, z, transformedV, "Testing vector transform by Matrix 4x4");
    }
}