/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

using AssertExtensions;
using MechJebLib.Structs;
using Xunit;

namespace MechJebLibTest.Structs
{
    public class HTests
    {
        private readonly H1 _interpolant1  = new H1();
        private readonly H3 _interpolant3  = new H3();
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
        public void H1SupportsInternalJump()
        {
            _interpolant1.Add(0, 0, 0);
            _interpolant1.Add(1, 0, 0);
            _interpolant1.Add(1, 1, 0);
            _interpolant1.Add(2, 1, 0);

            _interpolant1.Evaluate(0.5).ShouldEqual(0, 1e-15);
            _interpolant1.Evaluate(0.5).ShouldEqual(0, 1e-15);
            _interpolant1.Evaluate(1.5).ShouldEqual(1, 1e-15);
            _interpolant1.Evaluate(1.5).ShouldEqual(1, 1e-15);
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
        public void H1SingleEntry3()
        {
            _interpolant1.Add(2, 1, -1);
            _interpolant1.Add(2, 2, 1);

            _interpolant1.Evaluate(1.5).ShouldEqual(1.5, 1e-15);

            _interpolant1.Evaluate(2.0).ShouldEqual(2.0, 1e-15);

            _interpolant1.Evaluate(2.5).ShouldEqual(2.5, 1e-15);
        }

        [Fact]
        public void H3Linear()
        {
            _interpolant3.Add(0, Vector3d.zero, Vector3d.one);
            _interpolant3.Add(1, Vector3d.one, Vector3d.one);

            _interpolant3.Evaluate(0.5)[0].ShouldEqual(0.5, 1e-15);
            _interpolant3.Evaluate(0.5)[1].ShouldEqual(0.5, 1e-15);
            _interpolant3.Evaluate(0.5)[2].ShouldEqual(0.5, 1e-15);
        }

        [Fact]
        public void H3ZeroSlope()
        {
            _interpolant3.Add(0, Vector3d.zero, Vector3d.zero);
            _interpolant3.Add(1, Vector3d.zero, Vector3d.zero);

            _interpolant3.Evaluate(0.5)[0].ShouldEqual(0, 1e-15);
            _interpolant3.Evaluate(0.5)[1].ShouldEqual(0, 1e-15);
            _interpolant3.Evaluate(0.5)[2].ShouldEqual(0, 1e-15);
        }

        [Fact]
        public void H3SupportsInternalJump()
        {
            _interpolant3.Add(0, Vector3d.zero, Vector3d.zero);
            _interpolant3.Add(1, Vector3d.zero, Vector3d.zero);
            _interpolant3.Add(1, Vector3d.one, Vector3d.zero);
            _interpolant3.Add(2, Vector3d.one, Vector3d.zero);

            _interpolant3.Evaluate(0.5).ShouldEqual(Vector3d.zero, 1e-15);

            _interpolant3.Evaluate(1.5)[0].ShouldEqual(1, 1e-15);
            _interpolant3.Evaluate(1.5)[1].ShouldEqual(1, 1e-15);
            _interpolant3.Evaluate(1.5)[2].ShouldEqual(1, 1e-15);
        }

        [Fact]
        public void H3SupportsDerivativeJump()
        {
            _interpolant3.Add(0, Vector3d.zero, Vector3d.one);
            _interpolant3.Add(1, Vector3d.one, Vector3d.one);
            _interpolant3.Add(1, Vector3d.one, -Vector3d.one);
            _interpolant3.Add(2, Vector3d.zero, -Vector3d.one);

            _interpolant3.Evaluate(0.5).ShouldEqual(Vector3d.one*0.5, 1e-15);
            _interpolant3.Evaluate(1.5).ShouldEqual(Vector3d.one*0.5, 1e-15);
        }

        [Fact]
        public void H3SingleEntry()
        {
            _interpolant3.Add(2, Vector3d.one, -Vector3d.zero);

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
            _interpolant3.Add(2, Vector3d.one, Vector3d.one);

            _interpolant3.Evaluate(1.5).ShouldEqual(new Vector3d(0.5, 0.5, 0.5), 1e-15);

            _interpolant3.Evaluate(2.0).ShouldEqual(Vector3d.one, 1e-15);

            _interpolant3.Evaluate(2.5).ShouldEqual(new Vector3d(1.5, 1.5, 1.5), 1e-15);
        }

        [Fact]
        public void H3SingleEntry3()
        {
            _interpolant3.Add(2, Vector3d.one, new Vector3d(-1, -1, -1));
            _interpolant3.Add(2, new Vector3d(2.0, 2.0, 2.0), Vector3d.one);

            _interpolant3.Evaluate(1.5).ShouldEqual(new Vector3d(1.5, 1.5, 1.5), 1e-15);

            _interpolant3.Evaluate(2.0).ShouldEqual(new Vector3d(2.0, 2.0, 2.0), 1e-15);

            _interpolant3.Evaluate(2.5).ShouldEqual(new Vector3d(2.5, 2.5, 2.5), 1e-15);
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
        public void CN1SupportsInternalJump()
        {
            _interpolantN1.Add(0, new double[] {0}, new double[] {0});
            _interpolantN1.Add(1, new double[] {0}, new double[] {0});
            _interpolantN1.Add(1, new double[] {1}, new double[] {0});
            _interpolantN1.Add(2, new double[] {1}, new double[] {0});

            _interpolantN1.Evaluate(0.5)[0].ShouldEqual(0, 1e-15);
            _interpolantN1.Evaluate(1.5)[0].ShouldEqual(1, 1e-15);
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
