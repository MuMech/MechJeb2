/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

using AssertExtensions;
using MechJebLib.Primitives;
using Xunit;

namespace MechJebLibTest.Structs
{
    public class HTests
    {
        private readonly H1 _interpolant1 = H1.Get();
        private readonly H3 _interpolant3 = H3.Get();
        //private readonly CN _interpolantN1 = new CN(1);

        [Fact]
        public void H1Linear()
        {
            _interpolant1.Add(0, 0, 1);
            _interpolant1.Add(1, 1, 1);
            _interpolant1.Evaluate(0.5).ShouldEqual(0.5, 1e-15);
            _interpolant1.Evaluate(0.5).ShouldEqual(0.5, 1e-15);
        }

        [Fact]
        public void H1ZeroSlope()
        {
            _interpolant1.Add(0, 0, 0);
            _interpolant1.Add(1, 0, 0);
            _interpolant1.Evaluate(0.5).ShouldEqual(0, 1e-15);
            _interpolant1.Evaluate(0.5).ShouldEqual(0, 1e-15);

        }

        [Fact]
        public void H1SupportsDerivativeJump()
        {
            _interpolant1.Add(0, 0, 1);
            _interpolant1.Add(1, 1, 1);
            _interpolant1.Add(1, 1, -1);
            _interpolant1.Add(2, 0, -1);

            _interpolant1.Evaluate(0.5).ShouldEqual(0.5, 1e-15);
            _interpolant1.Evaluate(1.5).ShouldEqual(0.5, 1e-15);
        }

        [Fact]
        public void H1SingleEntry()
        {
            _interpolant1.Add(2, 1, 0);

            _interpolant1.Evaluate(1.5).ShouldEqual(1.0, 1e-15);

            _interpolant1.Evaluate(2.0).ShouldEqual(1.0, 1e-15);

            _interpolant1.Evaluate(2.5).ShouldEqual(1.0, 1e-15);
       }

        [Fact]
        public void H1SingleEntry2()
        {
            _interpolant1.Add(2, 1, 1);

            _interpolant1.Evaluate(1.5).ShouldEqual(0.5, 1e-15);

            _interpolant1.Evaluate(2.0).ShouldEqual(1.0, 1e-15);

            _interpolant1.Evaluate(2.5).ShouldEqual(1.5, 1e-15);
        }

        [Fact]
        public void H3Linear()
        {
            _interpolant3.Add(0, V3.zero, V3.one);
            _interpolant3.Add(1, V3.one, V3.one);

            _interpolant3.Evaluate(0.5)[0].ShouldEqual(0.5, 1e-15);
            _interpolant3.Evaluate(0.5)[1].ShouldEqual(0.5, 1e-15);
            _interpolant3.Evaluate(0.5)[2].ShouldEqual(0.5, 1e-15);
        }

        [Fact]
        public void H3ZeroSlope()
        {
            _interpolant3.Add(0, V3.zero, V3.zero);
            _interpolant3.Add(1, V3.zero, V3.zero);

            _interpolant3.Evaluate(0.5)[0].ShouldEqual(0, 1e-15);
            _interpolant3.Evaluate(0.5)[1].ShouldEqual(0, 1e-15);
            _interpolant3.Evaluate(0.5)[2].ShouldEqual(0, 1e-15);
        }

        [Fact]
        public void H3SupportsDerivativeJump()
        {
            _interpolant3.Add(0, V3.zero, V3.one);
            _interpolant3.Add(1, V3.one, V3.one);
            _interpolant3.Add(1, V3.one, -V3.one);
            _interpolant3.Add(2, V3.zero, -V3.one);

            _interpolant3.Evaluate(0.5).ShouldEqual(V3.one*0.5, 1e-15);
            _interpolant3.Evaluate(1.5).ShouldEqual(V3.one*0.5, 1e-15);
        }

        [Fact]
        public void H3SingleEntry()
        {
            _interpolant3.Add(2, V3.one, -V3.zero);

            _interpolant3.Evaluate(1.5)[0].ShouldEqual(1.0, 1e-15);
            _interpolant3.Evaluate(1.5)[1].ShouldEqual(1.0, 1e-15);
            _interpolant3.Evaluate(1.5)[2].ShouldEqual(1.0, 1e-15);

            _interpolant3.Evaluate(2.0)[0].ShouldEqual(1.0, 1e-15);
            _interpolant3.Evaluate(2.0)[1].ShouldEqual(1.0, 1e-15);
            _interpolant3.Evaluate(2.0)[2].ShouldEqual(1.0, 1e-15);

            _interpolant3.Evaluate(2.5)[0].ShouldEqual(1.0, 1e-15);
            _interpolant3.Evaluate(2.5)[1].ShouldEqual(1.0, 1e-15);
            _interpolant3.Evaluate(2.5)[2].ShouldEqual(1.0, 1e-15);
        }

        [Fact]
        public void H3SingleEntry2()
        {
            _interpolant3.Add(2, V3.one, V3.one);

            _interpolant3.Evaluate(1.5).ShouldEqual(new V3(0.5, 0.5, 0.5), 1e-15);

            _interpolant3.Evaluate(2.0).ShouldEqual(V3.one, 1e-15);

            _interpolant3.Evaluate(2.5).ShouldEqual(new V3(1.5, 1.5, 1.5), 1e-15);
        }

        /*

        [Fact]
        public void CN1Linear()
        {
            _interpolantN1.Add(0, new double[] {0}, new double[] {1});
            _interpolantN1.Add(1, new double[] {1}, new double[] {1});
            _interpolantN1.Evaluate(0.5)[0].ShouldEqual(0.5, 1e-15);
        }

        [Fact]
        public void CN1ZeroSlope()
        {
            _interpolantN1.Add(0, new double[] {0}, new double[] {0});
            _interpolantN1.Add(1, new double[] {0}, new double[] {0});
            _interpolantN1.Evaluate(0.5)[0].ShouldEqual(0, 1e-15);
        }

        [Fact]
        public void CN1SupportsDerivativeJump()
        {
            _interpolantN1.Add(0, new double[] {0}, new double[] {1});
            _interpolantN1.Add(1, new double[] {1}, new double[] {1});
            _interpolantN1.Add(1, new double[] {1}, new double[] {-1});
            _interpolantN1.Add(2, new double[] {0}, new double[] {-1});

            _interpolantN1.Evaluate(0.5)[0].ShouldEqual(0.5, 1e-15);
            _interpolantN1.Evaluate(1.5)[0].ShouldEqual(0.5, 1e-15);
        }

        [Fact]
        public void CN1SingleEntry()
        {
            _interpolantN1.Add(2, new double[] {1}, new double[] {0});

            _interpolantN1.Evaluate(1.5)[0].ShouldEqual(1.0, 1e-15);
            _interpolantN1.Evaluate(2.0)[0].ShouldEqual(1.0, 1e-15);
            _interpolantN1.Evaluate(2.5)[0].ShouldEqual(1.0, 1e-15);
        }
        */
    }
}
