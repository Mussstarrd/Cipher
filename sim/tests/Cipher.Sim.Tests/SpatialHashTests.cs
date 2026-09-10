using System.Collections.Generic;
using Cipher.Sim.Core;
using Cipher.Sim.Spatial;
using Xunit;

namespace Cipher.Sim.Tests
{
    public class SpatialHashTests
    {
        [Fact]
        public void QueryCircle_ReturnsExactlyTheItemsInRadius()
        {
            var hash = new SpatialHash(cellSize: 1f);
            hash.Insert(0, new Vec2(0f, 0f));     // inside
            hash.Insert(1, new Vec2(1.9f, 0f));   // inside (dist 1.9)
            hash.Insert(2, new Vec2(2.1f, 0f));   // outside (dist 2.1)
            hash.Insert(3, new Vec2(0f, -1.5f));  // inside
            hash.Insert(4, new Vec2(5f, 5f));     // far outside

            var results = new List<int>();
            hash.QueryCircle(new Vec2(0f, 0f), 2f, results);
            results.Sort();

            Assert.Equal(new[] { 0, 1, 3 }, results);
        }

        [Fact]
        public void QueryCircle_BoundaryIsInclusive()
        {
            var hash = new SpatialHash(cellSize: 0.5f);
            hash.Insert(0, new Vec2(3f, 4f)); // dist exactly 5 from origin

            var results = new List<int>();
            hash.QueryCircle(new Vec2(0f, 0f), 5f, results);

            Assert.Equal(new[] { 0 }, results);
        }

        [Fact]
        public void Clear_EmptiesTheHash_AndAllowsReinsertFromZero()
        {
            var hash = new SpatialHash(cellSize: 1f);
            hash.Insert(0, new Vec2(1f, 1f));
            hash.Clear();
            hash.Insert(0, new Vec2(9f, 9f));

            var atOld = new List<int>();
            hash.QueryCircle(new Vec2(1f, 1f), 0.5f, atOld);
            Assert.Empty(atOld);

            var atNew = new List<int>();
            hash.QueryCircle(new Vec2(9f, 9f), 0.5f, atNew);
            Assert.Equal(new[] { 0 }, atNew);
        }

        [Fact]
        public void NegativeCoordinates_HashCorrectly()
        {
            var hash = new SpatialHash(cellSize: 1f);
            hash.Insert(0, new Vec2(-3.5f, -7.25f));
            hash.Insert(1, new Vec2(-3.6f, -7.3f));

            var results = new List<int>();
            hash.QueryCircle(new Vec2(-3.5f, -7.25f), 0.5f, results);
            results.Sort();

            Assert.Equal(new[] { 0, 1 }, results);
        }
    }
}
