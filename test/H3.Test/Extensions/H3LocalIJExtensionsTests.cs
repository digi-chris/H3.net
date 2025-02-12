﻿using System;
using System.Collections.Generic;
using System.Linq;
using H3.Extensions;
using H3.Model;
using NUnit.Framework;

namespace H3.Test.Extensions; 

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class H3LocalIJExtensionsTests {

    private static readonly H3Index BaseCell15 = H3Index.Create(0, 15, 0);      // bc1
    private static readonly H3Index BaseCell8 = H3Index.Create(0, 8, 0);        // bc2
    private static readonly H3Index BaseCell31 = H3Index.Create(0, 31, 0);      // bc3
    private static readonly H3Index PentagonIndex = H3Index.Create(0, 4, 0);    // pent1

    // result of select h3_experimental_h3_to_local_ij('8e48e1d7038d527'::h3index, '8e48e1d7038952f'::h3index)
    private static readonly CoordIJ TestLocalIJ = (-247608, -153923);

    private static readonly IEnumerable<object[]> ToLocalIJTestArgs = new List<object[]> {
        new object[] { BaseCell15, BaseCell15, 0, 0 },      // bc1 -> bc1
        new object[] { BaseCell15, PentagonIndex, 1, 0 },   // bc1 -> pent1
        new object[] { BaseCell15, BaseCell8, 0, -1 },      // bc1 -> bc2
        new object[] { BaseCell15, BaseCell31, -1, 0 }      // bc1 -> bc3
    };

    private static readonly CoordIJ[] IjDirections = {
        (0, 1),
        (-1, 0),
        (-1, -1),
        (0, -1),
        (1, 0),
        (1, 1)
    };
    private static readonly CoordIJ IjNextRing = (1, 0);

    [Test]
    [TestCase(0, 15, Direction.Center)]
    public void Test_H3IndexToLocalIJK_BaseCell(int resolution, int baseCell, Direction direction) {
        // Arrange
        var index = H3Index.Create(resolution, baseCell, direction);

        // Act
        var ijk = LocalCoordIJK.ToLocalIJK(PentagonIndex, index);

        // Assert
        Assert.IsTrue(ijk.IsValid, "should be valid");
        Assert.IsTrue(ijk == LookupTables.UnitVectors[2], "should be equal to UnitVectors[2]");
    }

    [Test]
    public void Test_H3IndexToLocalIJ_MatchesPg() {
        // Arrange
        H3Index start = 0x8e48e1d7038d527;
        H3Index end = 0x8e48e1d7038952f;

        // Act
        var localIj = start.CellToLocalIj(end);

        // Assert
        Assert.IsTrue(localIj == TestLocalIJ, "should be equal");
    }

    [Test]
    public void Test_Upstream_ToLocalIJK_BaseCells() {
        // Act
        var actual = PentagonIndex.CellToLocalIjk(BaseCell15);

        // Assert
        Assert.IsTrue(actual.IsValid, "should be valid");
        Assert.AreEqual(LookupTables.UnitVectors[2], actual, "should equal 0,1,0");
    }

    [Test]
    public void Test_Upstream_FromLocalIJ_BaseCells_OriginMatchesSelf() {
        // Arrange
        H3Index origin = 0x8029fffffffffff;
        CoordIJ zero = (0, 0);

        // Act
        var actual = origin.LocalIjToCell(zero);

        // Assert
        Assert.AreEqual(origin, actual, "should be equal");
    }

    [Test]
    public void Test_Upstream_FromLocalIJ_BaseCells_OffsetIndex() {
        // Arrange
        H3Index origin = 0x8029fffffffffff;
        CoordIJ offset = (1, 0);
        H3Index expectedIndex = 0x8051fffffffffff;

        // Act
        var actual = origin.LocalIjToCell(offset);

        // Assert
        Assert.AreEqual(expectedIndex, actual, "should be equal");
    }

    [Test]
    [TestCase(2, 0)]
    [TestCase(0, 2)]
    [TestCase(-2, -2)]
    public void Test_Upstream_FromLocalIJ_BaseCells_OutOfRange(int i, int j) {
        // Arrange
        H3Index origin = 0x8029fffffffffff;
        CoordIJ coord = (i, j);

        // Act
        var actual = origin.LocalIjToCell(coord);

        // Assert
        Assert.AreEqual(H3Index.Invalid, actual, "should equal H3_NULL");
    }

    [Test]
    [TestCase(0, 0, 0x81283ffffffffffUL)]
    [TestCase(1, 0, 0x81293ffffffffffUL)]
    [TestCase(2, 0, 0x8150bffffffffffUL)]
    [TestCase(3, 0, 0x8151bffffffffffUL)]
    [TestCase(4, 0, 0UL)]
    [TestCase(-4, 0, 0UL)]
    [TestCase(0, 4, 0UL)]
    public void Test_Upstream_FromLocalIJ(int i, int j, ulong expectedIndex) {
        // Arrange
        H3Index origin = 0x81283ffffffffff;
        CoordIJ coord = (i, j);
        H3Index expected = expectedIndex;

        // Act
        var actual = origin.LocalIjToCell(coord);

        // Assert
        Assert.AreEqual(expected, actual, "should be equal");
    }

    [Test]
    [TestCaseSource(nameof(ToLocalIJTestArgs))]
    public void Test_Upstream_ToLocalIJ(H3Index originIndex, H3Index destIndex, int expectedI, int expectedJ) {
        // Arrange
        CoordIJ expectedCoord = (expectedI, expectedJ);

        // Act
        var actual = originIndex.CellToLocalIj(destIndex);

        // Assert
        Assert.AreEqual(expectedCoord, actual, "should be equal");
    }

    [Test]
    public void Test_Upstream_ToLocakIJ_FailsIfNotNeighbours() {
        // Act
        Assert.Throws<ArgumentException>(() => PentagonIndex.CellToLocalIj(BaseCell31));
    }

    [Test]
    public void Test_Upstream_ToLocalIJ_Invalid_ResolutionMismatch() {
        // Arrange
        H3Index invalid = 0x7fffffffffffffff;
#if NET48
            const string expectedMessage = "must be same resolution as origin\r\nParameter name: index";
#else
        const string expectedMessage = "must be same resolution as origin (Parameter 'index')";
#endif

        // Act
        var actual = Assert.Throws<ArgumentOutOfRangeException>(() => invalid.CellToLocalIj(BaseCell15));

        // Assert
        Assert.AreEqual(expectedMessage, actual.Message, "same message");
    }

    [Test]
    public void Test_Upstream_ToLocalIJ_Invalid_OriginAndDestination() {
        // Arrange
        H3Index invalid = 0x7fffffffffffffff;

        // Act
        var actual = Assert.Throws<IndexOutOfRangeException>(() => invalid.CellToLocalIj(invalid));

        // Assert
        Assert.AreEqual("Index was outside the bounds of the array.", actual.Message, "same message");
    }

    [Test]
    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    public void Test_Upstream_ToLocalIJ_Identity(int resolution) {
        // Arrange
        var coords = TestHelpers.GetAllCellsForResolution(resolution)
            .Select(index => (Origin: index, LocalCoordIJ: index.CellToLocalIj(index)));

        // Act
        var actual = coords.Select(c => (Expected: c.Origin, Actual: c.Origin.LocalIjToCell(c.LocalCoordIJ)));

        // Assert
        foreach (var (Expected, Actual) in actual) {
            Assert.AreEqual(Expected, Actual, "should be equal");
        }
    }

    [Test]
    public void Test_Upstream_ToLocalIJ_Coordinates_Resolution0() {
        // Act
        var coords = TestHelpers.GetAllCellsForResolution(0)
            .Select(index => (
                Origin: index,
                LocalCoordIJK: index.CellToLocalIj(index).ToCoordIJK(),
                Expected: LookupTables.UnitVectors[0]
            ));

        // Assert
        foreach (var (Origin, LocalCoordIJK, Expected) in coords) {
            Assert.AreEqual(Expected, LocalCoordIJK, $"{Origin} should equal {Expected} not {LocalCoordIJK}");
        }
    }

    [Test]
    public void Test_Upstream_ToLocalIJ_Coordinates_Resolution1() {
        // Act
        var coords = TestHelpers.GetAllCellsForResolution(1)
            .Select(index => (
                Origin: index,
                LocalCoordIJK: index.CellToLocalIj(index).ToCoordIJK(),
                Expected: LookupTables.DirectionToUnitVector[index.GetDirectionForResolution(1)]
            ));

        // Assert
        foreach (var (Origin, LocalCoordIJK, Expected) in coords) {
            Assert.AreEqual(Expected, LocalCoordIJK, $"{Origin} ({Origin.GetDirectionForResolution(1)}) should equal {Expected} not {LocalCoordIJK}");
        }
    }

    [Test]
    public void Test_Upstream_ToLocalIJ_Coordinates_Resolution2() {
        // Act
        var coords = TestHelpers.GetAllCellsForResolution(2)
            .Select(index => {
                CoordIJK expected = new(LookupTables.DirectionToUnitVector[index.GetDirectionForResolution(1)]);
                expected.DownAperture7Clockwise().ToNeighbour(index.GetDirectionForResolution(2));

                return (
                    Origin: index,
                    LocalCoordIJK: index.CellToLocalIj(index).ToCoordIJK(),
                    Expected: expected
                );
            });

        // Assert
        foreach (var (Origin, LocalCoordIJK, Expected) in coords) {
            Assert.AreEqual(Expected, LocalCoordIJK, $"{Origin} should equal {Expected} not {LocalCoordIJK}");
        }
    }

    [Test]
    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void Test_Upstream_ToLocalIJ_Neighbours(int resolution) {
        // Act
        var coords = TestHelpers.GetAllCellsForResolution(resolution)
            .SelectMany(index =>
                Enumerable.Range((int)Direction.K, 6)
                    .Where(dir => !(index.IsPentagon && dir == (int)Direction.K))
                    .Select(dir => {
                        var offset = index.GetDirectNeighbour((Direction)dir).Item1;
                        return (
                            Origin: index,
                            OriginIJK: index.CellToLocalIjk(index),
                            Index: offset,
                            LocalCoordIJ: index.CellToLocalIj(offset),
                            Direction: (Direction)dir
                        );
                    }));

        // Assert
        foreach(var (Origin, OriginIJK, Index, LocalCoordIJ, Direction) in coords) {
            Assert.NotNull(LocalCoordIJ, "should not be null");
            var invertedIjk = new CoordIJK(0, 0, 0).ToNeighbour(Direction);
            for (var i = 0; i < 3; i += 1) {
                invertedIjk = invertedIjk.RotateCounterClockwise();
            }
            var ijk = (LocalCoordIJ.ToCoordIJK() + invertedIjk).Normalize();
            Assert.AreEqual(OriginIJK, ijk, $"should be {OriginIJK} not {ijk}");
        }
    }

}