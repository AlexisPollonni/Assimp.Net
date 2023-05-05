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

using Assimp.Unmanaged;
using NUnit.Framework;
using TK = OpenTK;

namespace Assimp.Test;

[TestFixture]
public class Matrix4x4TestFixture
{
    [Test]
    public void TestIndexer()
    {
        var values = new[] { 1.0f, 2.0f, 3.0f, 5.0f, 0.0f, -5.0f, .5f, 100.25f, .3f, .35f, .025f, 1.0f, 0.0f, 0.0f, 0.0f, 1.0f };

        var m = Matrix4x4.Identity;
        for(var i = 0; i < 4; i++)
        {
            for(var j = 0; j < 4; j++)
            {
                var value = values[i * 4 + j];
                //Matrix indices are one-based.
                m[i + 1, j + 1] = value;
                TestHelper.AssertEquals(value, m[i + 1, j + 1], $"Testing [{i + 1},{j + 1}] indexer.");
            }
        }
    }

    [Test]
    public void TestEquals()
    {
        var m1 = new Matrix4x4(1.0f, 2.0f, 3.0f, 5.0f, 0.0f, -5.0f, .5f, 100.25f, .3f, .35f, .025f, 1.0f, 0.0f, 0.0f, 0.0f, 1.0f);
        var m2 = new Matrix4x4(1.0f, 2.0f, 3.0f, 5.0f, 0.0f, -5.0f, .5f, 100.25f, .3f, .35f, .025f, 1.0f, 0.0f, 0.0f, 0.0f, 1.0f);
        var m3 = new Matrix4x4(0.0f, 2.0f, 25.0f, 5.0f, 1.0f, 5.0f, 5.5f, 100.25f, 1.25f, 8.5f, 2.25f, 1.0f, 0.0f, 0.0f, 0.0f, 1.0f);

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
    public void TestDecompose()
    {
        var axis = new Vector3D(.25f, .5f, 0.0f);
        axis.Normalize();

        var rot = new Quaternion(axis, TK.MathHelper.Pi);
        var x = 50.0f;
        var y = 100.0f;
        var z = -50.0f;

        var scale = 2.0f;

        var m = Matrix4x4.FromScaling(new(scale, scale, scale)) * Matrix4x4.FromAngleAxis(TK.MathHelper.Pi, axis) * Matrix4x4.FromTranslation(new(x, y, z));

        AssimpLibrary.Instance.DecomposeMatrix(ref m, out var scaling1, out var rotation1, out var translation1);

        m.Decompose(out var scaling2, out var rotation2, out var translation2);

        TestHelper.AssertEquals(scaling1.X, scaling1.Y, scaling1.Z, scaling2, "Testing decomposed scaling output");
        TestHelper.AssertEquals(rotation1.X, rotation1.Y, rotation1.Z, rotation1.W, rotation2, "Testing decomposed rotation output");
        TestHelper.AssertEquals(translation1.X, translation1.Y, translation1.Z, translation2, "Testing decomposed translation output");

        m = Matrix4x4.FromAngleAxis(TK.MathHelper.Pi, axis) * Matrix4x4.FromTranslation(new(x, y, z));

        m.DecomposeNoScaling(out rotation2, out translation2);

        TestHelper.AssertEquals(rot.X, rot.Y, rot.Z, rot.W, rotation2, "Testing no scaling decomposed rotation output");
        TestHelper.AssertEquals(x, y, z, translation2, "Testing no scaling decomposed translation output");
    }

    [Test]
    public void TestDeterminant()
    {
        var x = TK.MathHelper.Pi;
        var y = TK.MathHelper.PiOver3;

        var tkM = TK.Matrix4.CreateRotationX(x) * TK.Matrix4.CreateRotationY(y);
        var m = Matrix4x4.FromRotationX(x) * Matrix4x4.FromRotationY(y);

        var tkDet = tkM.Determinant;
        var det = m.Determinant();
        TestHelper.AssertEquals(tkDet, det, "Testing determinant");
    }

    [Test]
    public void TestFromAngleAxis()
    {
        var tkM = TK.Matrix4.CreateFromAxisAngle(TK.Vector3.UnitY, TK.MathHelper.Pi);
        var m = Matrix4x4.FromAngleAxis(TK.MathHelper.Pi, new(0, 1, 0));

        TestHelper.AssertEquals(tkM, m, "Testing from angle axis");
    }

    [Test]
    public void TestFromEulerAnglesXYZ()
    {
        var x = TK.MathHelper.Pi;
        var y = 0.0f;
        var z = TK.MathHelper.PiOver4;

        var tkM = TK.Matrix4.CreateRotationX(x) * TK.Matrix4.CreateRotationZ(z);
        var m = Matrix4x4.FromEulerAnglesXYZ(x, y, z);
        var m2 = Matrix4x4.FromEulerAnglesXYZ(new(x, y, z));

        TestHelper.AssertEquals(tkM, m, "Testing create from euler angles");
        Assert.That(m, Is.EqualTo(m2), "Testing if create from euler angle as a vector is the same as floats.");
    }

    [Test]
    public void TestFromRotationX()
    {
        var x = TK.MathHelper.Pi;

        var tkM = TK.Matrix4.CreateRotationX(x);
        var m = Matrix4x4.FromRotationX(x);

        TestHelper.AssertEquals(tkM, m, "Testing from rotation x");
    }

    [Test]
    public void TestFromRotationY()
    {
        var y = TK.MathHelper.Pi;

        var tkM = TK.Matrix4.CreateRotationY(y);
        var m = Matrix4x4.FromRotationY(y);

        TestHelper.AssertEquals(tkM, m, "Testing from rotation y");
    }

    [Test]
    public void TestFromRotationZ()
    {
        var z = TK.MathHelper.Pi;

        var tkM = TK.Matrix4.CreateRotationZ(z);
        var m = Matrix4x4.FromRotationZ(z);

        TestHelper.AssertEquals(tkM, m, "Testing from rotation z");
    }

    [Test]
    public void TestFromScaling()
    {
        var x = 1.0f;
        var y = 2.0f;
        var z = 3.0f;

        var tkM = TK.Matrix4.CreateScale(x, y, z);
        var m = Matrix4x4.FromScaling(new(x, y, z));

        TestHelper.AssertEquals(tkM, m, "Testing from scaling");
    }

    [Test]
    public void TestFromToMatrix()
    {
        var from = new Vector3D(1, 0, 0);
        var to = new Vector3D(0, 1, 0);

        var tkM = TK.Matrix4.CreateRotationZ(TK.MathHelper.PiOver2);
        var m = Matrix4x4.FromToMatrix(from, to);

        TestHelper.AssertEquals(tkM, m, "Testing From-To rotation matrix");
    }

    [Test]
    public void TestFromTranslation()
    {
        var x = 52.0f;
        var y = -100.0f;
        var z = 5.0f;

        var tkM = TK.Matrix4.CreateTranslation(x, y, z);
        var m = Matrix4x4.FromTranslation(new(x, y, z));

        TestHelper.AssertEquals(tkM, m, "Testing from translation");
    }

    [Test]
    public void TestInverse()
    {
        var x = TK.MathHelper.PiOver6;
        var y = TK.MathHelper.Pi;

        var tkM = TK.Matrix4.CreateRotationX(x) * TK.Matrix4.CreateRotationY(y);
        var m = Matrix4x4.FromRotationX(x) * Matrix4x4.FromRotationY(y);

        tkM.Invert();
        m.Inverse();

        TestHelper.AssertEquals(tkM, m, "Testing inverse");
    }

    [Test]
    public void TestIdentity()
    {
        var tkM = TK.Matrix4.Identity;
        var m = Matrix4x4.Identity;

        Assert.That(m.IsIdentity, "Testing IsIdentity");
        TestHelper.AssertEquals(tkM, m, "Testing is identity to baseline");
    }

    [Test]
    public void TestTranspose()
    {
        var x = TK.MathHelper.Pi;
        var y = TK.MathHelper.PiOver4;

        var tkM = TK.Matrix4.CreateRotationX(x) * TK.Matrix4.CreateRotationY(y);
        var m = Matrix4x4.FromRotationX(x) * Matrix4x4.FromRotationY(y);

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
        var m = Matrix4x4.FromRotationX(x) * Matrix4x4.FromRotationY(y);

        TestHelper.AssertEquals(tkM, m, "Testing Op multiply");
    }
}