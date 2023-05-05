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
using NUnit.Framework;
using TK = OpenTK;

namespace Assimp.Test;

[TestFixture]
public class Matrix3x3TestFixture
{
    [Test]
    public void TestIndexer()
    {
        var values = new[] { 1.0f, 2.0f, 3.0f, 0.0f, -5.0f, .5f, .3f, .35f, .025f };

        var m = Matrix3x3.Identity;
        for(var i = 0; i < 3; i++)
        {
            for(var j = 0; j < 3; j++)
            {
                var value = values[i * 3 + j];
                //Matrix indices are one-based.
                m[i + 1, j + 1] = value;
                TestHelper.AssertEquals(value, m[i + 1, j + 1], $"Testing [{i + 1},{j + 1}] indexer.");
            }
        }
    }

    [Test]
    public void TestEquals()
    {
        var m1 = new Matrix3x3(1.0f, 2.0f, 3.0f, 0.0f, -5.0f, .5f, .3f, .35f, .025f);
        var m2 = new Matrix3x3(1.0f, 2.0f, 3.0f, 0.0f, -5.0f, .5f, .3f, .35f, .025f);
        var m3 = new Matrix3x3(0.0f, 2.0f, 25.0f, 1.0f, 5.0f, 5.5f, 1.25f, 8.5f, 2.25f);

        //Test IEquatable Equals
        Assert.That(m1, Is.EqualTo(m2), "Test IEquatable equals");
        Assert.That(m1, Is.Not.EqualTo(m3), "Test IEquatable equals");

        //Test object equals override
        Assert.That(m1, Is.EqualTo(m2), "Tests object equals");
        Assert.That(m1, Is.Not.EqualTo(m3), "Tests object equals");

        //Test op equals
        Assert.That(m1, Is.EqualTo(m2), "Testing OpEquals");
        Assert.That(m1, Is.Not.EqualTo(m3), "Testing OpEquals");

        //Test op not equals
        Assert.That(m1, Is.Not.EqualTo(m3), "Testing OpNotEquals");
        Assert.That(m1, Is.EqualTo(m2), "Testing OpNotEquals");
    }

    [Test]
    public void TestDeterminant()
    {
        var x = TK.MathHelper.Pi;
        var y = TK.MathHelper.PiOver3;

        var tkM = TK.Matrix4.CreateRotationX(x) * TK.Matrix4.CreateRotationY(y);
        var m = Matrix3x3.FromRotationX(x) * Matrix3x3.FromRotationY(y);

        var tkDet = tkM.Determinant;
        var det = m.Determinant();
        TestHelper.AssertEquals(tkDet, det, "Testing determinant");
    }

    [Test]
    public void TestFromAngleAxis()
    {
        var tkM = TK.Matrix4.CreateFromAxisAngle(TK.Vector3.UnitY, TK.MathHelper.Pi);
        var m = Matrix3x3.FromAngleAxis(TK.MathHelper.Pi, new(0, 1, 0));

        TestHelper.AssertEquals(tkM, m, "Testing from angle axis");
    }

    [Test]
    public void TestFromEulerAnglesXYZ()
    {
        var x = TK.MathHelper.Pi;
        var y = 0.0f;
        var z = TK.MathHelper.PiOver4;

        var tkM = TK.Matrix4.CreateRotationX(x) * TK.Matrix4.CreateRotationZ(z);
        var m = Matrix3x3.FromEulerAnglesXYZ(x, y, z);
        var m2 = Matrix3x3.FromEulerAnglesXYZ(new(x, y, z));

        TestHelper.AssertEquals(tkM, m, "Testing create from euler angles");
        Assert.That(m, Is.EqualTo(m2), "Testing if create from euler angle as a vector is the same as floats.");
    }

    [Test]
    public void TestFromRotationX()
    {
        var x = TK.MathHelper.Pi;

        var tkM = TK.Matrix4.CreateRotationX(x);
        var m = Matrix3x3.FromRotationX(x);

        TestHelper.AssertEquals(tkM, m, "Testing from rotation x");
    }

    [Test]
    public void TestFromRotationY()
    {
        var y = TK.MathHelper.Pi;

        var tkM = TK.Matrix4.CreateRotationY(y);
        var m = Matrix3x3.FromRotationY(y);

        TestHelper.AssertEquals(tkM, m, "Testing from rotation y");
    }

    [Test]
    public void TestFromRotationZ()
    {
        var z = TK.MathHelper.Pi;

        var tkM = TK.Matrix4.CreateRotationZ(z);
        var m = Matrix3x3.FromRotationZ(z);

        TestHelper.AssertEquals(tkM, m, "Testing from rotation z");
    }

    [Test]
    public void TestFromScaling()
    {
        var x = 1.0f;
        var y = 2.0f;
        var z = 3.0f;

        var tkM = TK.Matrix4.CreateScale(x, y, z);
        var m = Matrix3x3.FromScaling(new(x, y, z));

        TestHelper.AssertEquals(tkM, m, "Testing from scaling");
    }

    [Test]
    public void TestFromToMatrix()
    {
        var from = new Vector3D(1, 0, 0);
        var to = new Vector3D(0, 1, 0);

        var tkM = TK.Matrix4.CreateRotationZ(-TK.MathHelper.PiOver2);
        var m = Matrix3x3.FromToMatrix(to, from);

        TestHelper.AssertEquals(tkM, m, "Testing From-To rotation matrix");
    }

    [Test]
    public void TestToFromQuaternion()
    {
        var axis = new Vector3D(.25f, .5f, 0.0f);
        axis.Normalize();

        var angle = (float) Math.PI;

        var q = new Quaternion(axis, angle);
        var m = q.GetMatrix();
        var q2 = new Quaternion(m);

        TestHelper.AssertEquals(q.X, q.Y, q.Z, q.W, q2, "Testing Quaternion->Matrix->Quaternion");
    }

    [Test]
    public void TestInverse()
    {
        var x = TK.MathHelper.PiOver6;
        var y = TK.MathHelper.Pi;

        var tkM = TK.Matrix4.CreateRotationX(x) * TK.Matrix4.CreateRotationY(y);
        var m = Matrix3x3.FromRotationX(x) * Matrix3x3.FromRotationY(y);

        tkM.Invert();
        m.Inverse();

        TestHelper.AssertEquals(tkM, m, "Testing inverse");
    }

    [Test]
    public void TestIdentity()
    {
        var tkM = TK.Matrix4.Identity;
        var m = Matrix3x3.Identity;

        Assert.That(m.IsIdentity, "Testing IsIdentity");
        TestHelper.AssertEquals(tkM, m, "Testing is identity to baseline");
    }

    [Test]
    public void TestTranspose()
    {
        var x = TK.MathHelper.Pi;
        var y = TK.MathHelper.PiOver4;

        var tkM = TK.Matrix4.CreateRotationX(x) * TK.Matrix4.CreateRotationY(y);
        var m = Matrix3x3.FromRotationX(x) * Matrix3x3.FromRotationY(y);

        tkM.Transpose();
        m.Transpose();
        TestHelper.AssertEquals(tkM, m, "Testing transpose");
    }

    [Test]
    public void TestOpMultiply()
    {
        var x = TK.MathHelper.Pi;
        var y = TK.MathHelper.PiOver3;

        var tkM = TK.Matrix4.CreateRotationX(x) * TK.Matrix4.CreateRotationY(y);
        var m = Matrix3x3.FromRotationX(x) * Matrix3x3.FromRotationY(y);

        TestHelper.AssertEquals(tkM, m, "Testing Op multiply");
    }
}