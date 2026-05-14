using MechJebLib.Utils;
using Xunit;

namespace MechJebLibTest.Utils
{
    public class ObjectPoolTests
    {
        private sealed class Box
        {
            public int Value;
            public int ResetCount;
        }

        [Fact]
        public void Borrow_FromEmptyPool_InvokesFactory()
        {
            int created = 0;
            var pool = new ObjectPool<Box>(
                () =>
                {
                    created++;
                    return new Box();
                },
                b => b.ResetCount++);

            Box b = pool.Borrow();
            Assert.NotNull(b);
            Assert.Equal(1, created);
            Assert.Equal(0, b.ResetCount); // reset only fires on Release
        }

        [Fact]
        public void Release_ThenBorrow_ReturnsSameInstance_NoNewAllocation()
        {
            int created = 0;
            var pool = new ObjectPool<Box>(
                () =>
                {
                    created++;
                    return new Box();
                },
                _ => { });

            Box first = pool.Borrow();
            first.Value = 42;
            pool.Release(first);

            Box second = pool.Borrow();
            Assert.Equal(42, second.Value);
            Assert.Same(first, second);
            Assert.Equal(1, created); // only the initial allocation
        }

        [Fact]
        public void Release_RunsResetAction()
        {
            var pool = new ObjectPool<Box>(
                () => new Box(),
                b => b.ResetCount++);

            Box b = pool.Borrow();
            pool.Release(b);
            Assert.Equal(1, b.ResetCount);

            pool.Release(b); // explicit double-release: still bumps reset
            Assert.Equal(2, b.ResetCount);
        }

        [Fact]
        public void RepeatedRoundTrips_AllocateOnceWhenPoolHasCapacity()
        {
            int created = 0;
            var pool = new ObjectPool<Box>(
                () =>
                {
                    created++;
                    return new Box();
                },
                _ => { });

            for (int i = 0; i < 1000; i++)
            {
                Box b = pool.Borrow();
                pool.Release(b);
            }

            Assert.Equal(1, created);
        }
    }
}
