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

using NUnit.Framework;

namespace Assimp.Test;

[TestFixture]
public class MarshalingTestFixture
{
    [Test]
    public void TestMarshal_MeshMorphAnimationChannel()
    {
        var morph = new MeshMorphAnimationChannel
        {
            Name = "TestMorph"
        };

        var morphKey1 = new MeshMorphKey
        {
            Time = 0.0f
        };
        morphKey1.Values.Add(1);
        morphKey1.Values.Add(2);
        morphKey1.Weights.Add(0.5);
        morphKey1.Weights.Add(1.0);

        var morphKey2 = new MeshMorphKey
        {
            Time = 1.0f
        };
        morphKey2.Values.Add(1);
        morphKey2.Values.Add(2);
        morphKey2.Weights.Add(.25);
        morphKey2.Weights.Add(.25);

        morph.MeshMorphKeys.Add(morphKey1);
        morph.MeshMorphKeys.Add(morphKey2);

        var anim = new Animation();
        anim.MeshMorphAnimationChannels.Add(morph);

        var scene = new Scene();
        scene.Animations.Add(anim);

        var scenePtr = Scene.ToUnmanagedScene(scene);
        Assert.That(scenePtr, Is.Not.EqualTo(nint.Zero));

        var scene2 = Scene.FromUnmanagedScene(scenePtr);
        Scene.FreeUnmanagedScene(scenePtr);

        Assert.That(scene2.AnimationCount, Is.EqualTo(1));

        var otherAnim = scene2.Animations[0];
        Assert.That(otherAnim.MeshMorphAnimationChannelCount, Is.EqualTo(1));

        var otherMorph = otherAnim.MeshMorphAnimationChannels[0];
        Assert.Multiple(() =>
        {
            Assert.That(otherMorph.Name, Is.EqualTo(morph.Name));
            Assert.That(otherMorph.MeshMorphKeyCount, Is.EqualTo(2));
        });
        CompareMorphKey(otherMorph.MeshMorphKeys[0], morph.MeshMorphKeys[0]);
        CompareMorphKey(otherMorph.MeshMorphKeys[1], morph.MeshMorphKeys[1]);
    }

    private void CompareMorphKey(MeshMorphKey key1, MeshMorphKey key2)
    {
        TestHelper.AssertEquals(key1.Time, key2.Time);
        Assert.Multiple(() =>
        {
            Assert.That(key1.Values, Has.Count.EqualTo(key1.Weights.Count));
            Assert.That(key2.Values, Has.Count.EqualTo(key2.Weights.Count));
        });
        Assert.That(key1.Values, Has.Count.EqualTo(key2.Values.Count));

        for(var i = 0; i < key1.Values.Count; i++)
        {
            TestHelper.AssertEquals(key1.Weights[i], key2.Weights[i]);
            Assert.IsTrue(key1.Values[i] == key2.Values[i]);
        }
    }
}