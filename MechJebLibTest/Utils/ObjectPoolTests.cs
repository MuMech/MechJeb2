/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
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
        }

        [Fact]
        public void DoubleRelease_Throws_AndDoesNotResetOrDuplicate()
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
            pool.Release(b);

            Assert.Throws<InvalidOperationException>(() => pool.Release(b));
            Assert.Equal(1, b.ResetCount);

            Assert.Same(b, pool.Borrow());
            Assert.NotSame(b, pool.Borrow()); // it was only pooled once, so this one comes from the factory
            Assert.Equal(2, created);
        }

        // This documents a weakness in the ObjectPool, where a double-release is accepted if
        // the returned object is borrowed first.  We don't catch every double-release.
        [Fact]
        public void Release_AfterReborrow_IsAccepted()
        {
            var pool = new ObjectPool<Box>(
                () => new Box(),
                b => b.ResetCount++);

            Box b = pool.Borrow();
            pool.Release(b);
            Assert.Same(b, pool.Borrow());
            pool.Release(b);

            Assert.Equal(2, b.ResetCount);
            Assert.Same(b, pool.Borrow());
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
