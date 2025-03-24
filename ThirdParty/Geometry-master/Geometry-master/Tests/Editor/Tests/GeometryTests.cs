using iShape.Geometry;
using NUnit.Framework;
using UnityEngine;

public class GeometryTests {

    [Test]
    public void Test_00() {
        var point = new IntVector(0, 0);
        Assert.AreEqual(point.BitPack, 0);
    }

    [Test]
    public void Test_01() {
        var point = new IntVector(0, 1);
        Assert.AreEqual(point.BitPack, 1);
    }

    [Test]
    public void Test_02() {
        var point = new IntVector(1, 0);
        Assert.AreEqual(point.BitPack, (long)1<<IntGeom.maxBits);
    }

    [Test]
    public void Test_03() {
        for(int x = -10; x <=10; ++x) {
            for(int y = -10; y <= 10; ++y) {
                var origin = new Vector2(x, y);
                var intPoint = IntGeom.DefGeom.Int(origin);
                var point = IntGeom.DefGeom.Float(intPoint);
                Assert.AreEqual((int)point.x, x);
                Assert.AreEqual((int)point.y, y);
            }
        }
    }

    [Test]
    public void Test_04() {
        var a = new Vector2(-100, 100);
        var b = new Vector2(100, -100);
        var iPoints = IntGeom.DefGeom.Int(new Vector2[] { a, b });
        var points = IntGeom.DefGeom.Float(iPoints);
        Assert.AreEqual(points, new Vector2[] { a, b });
    }
}