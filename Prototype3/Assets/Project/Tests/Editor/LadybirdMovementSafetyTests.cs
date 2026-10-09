using System.Collections.Generic;
using NUnit.Framework;
using TinyWorlds;
using UnityEngine;

public sealed class LadybirdMovementSafetyTests
{
    private readonly List<GameObject> objects = new List<GameObject>();
    private LadybirdMovementSafety safety;
    private Transform owner;
    private Collider floor;
    private readonly Vector3 center = new Vector3(5000f, 100f, 5000f);

    [SetUp]
    public void SetUp()
    {
        safety = new LadybirdMovementSafety { bodyRadius = 0.1f, explorationRadius = 3f };
        owner = Create("Test Ladybird").transform;
        owner.position = center;
        floor = Box("Walkable surface", center - Vector3.up * 0.1f, new Vector3(4f, 0.2f, 4f));
        Physics.SyncTransforms();
    }

    [TearDown]
    public void TearDown()
    {
        safety.Dispose();
        foreach (GameObject item in objects) if (item != null) Object.DestroyImmediate(item);
        objects.Clear(); Physics.SyncTransforms();
    }

    [TestCase(0f)] [TestCase(0.1f)] [TestCase(0.15f)]
    public void DeadzoneSuppressesSmallInput(float input)
    { Assert.That(LadybirdMovementSafety.ApplyDeadzone(new Vector2(input, 0f), 0.15f), Is.EqualTo(Vector2.zero)); }

    [Test]
    public void FullStickRetainsDirectionAndMagnitude()
    {
        Vector2 result = LadybirdMovementSafety.ApplyDeadzone(Vector2.up, 0.15f);
        Assert.That(result.x, Is.EqualTo(0f)); Assert.That(result.y, Is.EqualTo(1f).Within(0.0001f));
    }

    [Test]
    public void BoundaryAccountsForBodyRatherThanOnlyPivot()
    {
        safety.explorationRadius = 1f;
        Assert.That(safety.InsideBoundary(center + Vector3.right * 0.89f, center), Is.True);
        Assert.That(safety.InsideBoundary(center + Vector3.right * 0.95f, center), Is.False);
    }

    [Test]
    public void GroundProbeReturnsConfiguredSurfaceHeight()
    {
        Assert.That(safety.TryPosition(new[] { floor }, center, center, center.y, out Vector3 ground, out string reason), Is.True, reason);
        Assert.That(ground.y, Is.EqualTo(center.y).Within(0.001f));
    }

    [Test]
    public void EdgeIsRejectedEvenWhileCentreStillHasGround()
    {
        Vector3 point = center + Vector3.right * 1.96f;
        Assert.That(safety.TryPosition(new[] { floor }, point, center, center.y, out _, out string reason), Is.False);
        StringAssert.Contains("footprint", reason);
    }

    [Test]
    public void UnconfiguredFloorCannotRescueAnUnsafeEdge()
    {
        Box("Lower floor", center - Vector3.up, new Vector3(20f, 0.2f, 20f)); Physics.SyncTransforms();
        Assert.That(safety.TryPosition(new[] { floor }, center + Vector3.right * 2.5f, center, center.y, out _, out _), Is.False);
        Assert.That(safety.TryFindStart(owner, new Collider[0], center, center, out _, out _), Is.False);
    }

    [Test]
    public void ExcessiveDropIsRejected()
    { Assert.That(safety.TryPosition(new[] { floor }, center, center, center.y + 0.2f, out _, out string reason), Is.False); }

    [Test]
    public void SolidObstacleBlocksSweptMotion()
    {
        Box("Obstacle", center + new Vector3(0.4f, 0.15f, 0f), Vector3.one * 0.1f); Physics.SyncTransforms();
        Assert.That(safety.PathIsClear(owner, center, Vector3.right, 0.5f, out string reason), Is.False);
        StringAssert.Contains("Obstacle", reason);
    }

    [Test]
    public void OwnInteractionColliderDoesNotBlockMovement()
    {
        Collider own = Box("Own body interaction collider", center + Vector3.up * 0.13f, Vector3.one * 0.1f);
        own.transform.SetParent(owner, true); Physics.SyncTransforms();
        Assert.That(safety.PathIsClear(owner, center, Vector3.right, 0.2f, out string reason), Is.True, reason);
    }

    [Test]
    public void TriggerIsNotASolidObstacle()
    {
        Collider trigger = Box("Trigger", center + Vector3.up * 0.13f, Vector3.one * 0.1f);
        trigger.isTrigger = true; Physics.SyncTransforms();
        Assert.That(safety.PathIsClear(owner, center, Vector3.right, 0.2f, out string reason), Is.True, reason);
    }

    [Test]
    public void ExplicitDecorationExclusionDoesNotDisableSolidObstacles()
    {
        Collider decoration = Box("Decorative grass", center + Vector3.up * 0.13f, Vector3.one * 0.1f);
        safety.ignoredObstacles = new[] { decoration }; Physics.SyncTransforms();
        Assert.That(safety.PathIsClear(owner, center, Vector3.right, 0.2f, out string reason), Is.True, reason);
        Box("Solid rock", center + new Vector3(0.4f, 0.15f, 0f), Vector3.one * 0.1f); Physics.SyncTransforms();
        Assert.That(safety.PathIsClear(owner, center, Vector3.right, 0.5f, out reason), Is.False);
        StringAssert.Contains("Solid rock", reason);
    }

    [Test]
    public void SafeStartHasFullSupportAndNoObstacleOverlap()
    {
        Assert.That(safety.TryFindStart(owner, new[] { floor }, center, center, out Vector3 start, out string reason), Is.True, reason);
        Assert.That(safety.TryPosition(new[] { floor }, start, center, start.y, out _, out reason), Is.True, reason);
        Assert.That(safety.PathIsClear(owner, start, Vector3.forward, 0f, out reason), Is.True, reason);
    }

    [Test] public void RectangularBoundaryUsesLongSurfaceAxisAndFootprintInset()
    {
        safety.boundaryShape=LadybirdMovementSafety.BoundaryShape.WorldRectangle;
        safety.boundaryHalfExtents=new Vector2(4f,2f);
        Assert.IsTrue(safety.InsideBoundary(center+Vector3.right*3.8f,center));
        Assert.IsFalse(safety.InsideBoundary(center+Vector3.right*3.95f,center));
        Assert.IsFalse(safety.InsideBoundary(center+Vector3.forward*1.95f,center));
    }
    [Test] public void RectangularAreaCannotRescueUnsupportedGround()
    {
        safety.boundaryShape=LadybirdMovementSafety.BoundaryShape.WorldRectangle;
        safety.boundaryHalfExtents=new Vector2(4f,2f);
        Assert.IsFalse(safety.TryPosition(new[]{floor},center+Vector3.right*2.5f,center,center.y,out _,out string reason));
        StringAssert.Contains("ground",reason);
    }
    [Test] public void BoundaryRejectionDoesNotPreventReverseStep()
    {
        safety.explorationRadius=1f; var from=center+Vector3.right*.89f;
        Assert.IsFalse(safety.TryMovementPosition(new[]{floor},from,from+Vector3.right*.02f,center,out _,out _));
        Assert.IsTrue(safety.TryMovementPosition(new[]{floor},from,from-Vector3.right*.02f,center,out _,out _));
    }
    [Test] public void BoundaryShrinkAllowsOnlySupportedInwardRecovery()
    {
        safety.explorationRadius=1f; var from=center+Vector3.right*1.1f;
        Assert.IsTrue(safety.TrySupportedPosition(new[]{floor},from,from.y,out _,out _));
        Assert.IsTrue(safety.TryMovementPosition(new[]{floor},from,from-Vector3.right*.02f,center,out _,out _));
        Assert.IsFalse(safety.TryMovementPosition(new[]{floor},from,from+Vector3.right*.02f,center,out _,out _));
        Assert.IsFalse(safety.TryMovementPosition(new Collider[0],from,from-Vector3.right*.02f,center,out _,out _));
    }
    [Test] public void ExpandedRectangleRetainsVerifiedResetStart()
    {
        safety.boundaryShape=LadybirdMovementSafety.BoundaryShape.WorldRectangle;
        safety.boundaryHalfExtents=new Vector2(1.8f,1.5f);
        Assert.IsTrue(safety.TryFindStart(owner,new[]{floor},center,center,out var start,out string reason),reason);
        Assert.IsTrue(safety.TryPosition(new[]{floor},start,center,start.y,out _,out reason),reason);
    }

    private GameObject Create(string name) { GameObject item = new GameObject(name); objects.Add(item); return item; }
    private Collider Box(string name, Vector3 position, Vector3 size)
    { GameObject item = Create(name); item.transform.position = position; BoxCollider box = item.AddComponent<BoxCollider>(); box.size = size; return box; }
}
