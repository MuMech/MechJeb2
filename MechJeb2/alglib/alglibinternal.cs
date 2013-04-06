/*************************************************************************
Copyright (c) Sergey Bochkanov (ALGLIB project).

>>> SOURCE LICENSE >>>
This program is free software; you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation (www.fsf.org); either version 2 of the
License, or (at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

A copy of the GNU General Public License is available at
http://www.fsf.org/licensing/licenses
>>> END OF LICENSE >>>
*************************************************************************/
#pragma warning disable 162
#pragma warning disable 219
using System;

public partial class alglib
{



}
public partial class alglib
{
    public class scodes
    {
        public static int getrdfserializationcode()
        {
            int result = 0;

            result = 1;
            return result;
        }


        public static int getkdtreeserializationcode()
        {
            int result = 0;

            result = 2;
            return result;
        }


        public static int getmlpserializationcode()
        {
            int result = 0;

            result = 3;
            return result;
        }


    }
    public class apserv
    {
        /*************************************************************************
        Buffers for internal functions which need buffers:
        * check for size of the buffer you want to use.
        * if buffer is too small, resize it; leave unchanged, if it is larger than
          needed.
        * use it.

        We can pass this structure to multiple functions;  after first run through
        functions buffer sizes will be finally determined,  and  on  a next run no
        allocation will be required.
        *************************************************************************/
        public class apbuffers
        {
            public int[] ia0;
            public int[] ia1;
            public int[] ia2;
            public int[] ia3;
            public double[] ra0;
            public double[] ra1;
            public double[] ra2;
            public double[] ra3;
            public apbuffers()
            {
                ia0 = new int[0];
                ia1 = new int[0];
                ia2 = new int[0];
                ia3 = new int[0];
                ra0 = new double[0];
                ra1 = new double[0];
                ra2 = new double[0];
                ra3 = new double[0];
            }
        };




        /*************************************************************************
        This  function  generates  1-dimensional  general  interpolation task with
        moderate Lipshitz constant (close to 1.0)

        If N=1 then suborutine generates only one point at the middle of [A,B]

          -- ALGLIB --
             Copyright 02.12.2009 by Bochkanov Sergey
        *************************************************************************/
        public static void taskgenint1d(double a,
            double b,
            int n,
            ref double[] x,
            ref double[] y)
        {
            int i = 0;
            double h = 0;

            x = new double[0];
            y = new double[0];

            ap.assert(n>=1, "TaskGenInterpolationEqdist1D: N<1!");
            x = new double[n];
            y = new double[n];
            if( n>1 )
            {
                x[0] = a;
                y[0] = 2*math.randomreal()-1;
                h = (b-a)/(n-1);
                for(i=1; i<=n-1; i++)
                {
                    if( i!=n-1 )
                    {
                        x[i] = a+(i+0.2*(2*math.randomreal()-1))*h;
                    }
                    else
                    {
                        x[i] = b;
                    }
                    y[i] = y[i-1]+(2*math.randomreal()-1)*(x[i]-x[i-1]);
                }
            }
            else
            {
                x[0] = 0.5*(a+b);
                y[0] = 2*math.randomreal()-1;
            }
        }


        /*************************************************************************
        This function generates  1-dimensional equidistant interpolation task with
        moderate Lipshitz constant (close to 1.0)

        If N=1 then suborutine generates only one point at the middle of [A,B]

          -- ALGLIB --
             Copyright 02.12.2009 by Bochkanov Sergey
        *************************************************************************/
        public static void taskgenint1dequidist(double a,
            double b,
            int n,
            ref double[] x,
            ref double[] y)
        {
            int i = 0;
            double h = 0;

            x = new double[0];
            y = new double[0];

            ap.assert(n>=1, "TaskGenInterpolationEqdist1D: N<1!");
            x = new double[n];
            y = new double[n];
            if( n>1 )
            {
                x[0] = a;
                y[0] = 2*math.randomreal()-1;
                h = (b-a)/(n-1);
                for(i=1; i<=n-1; i++)
                {
                    x[i] = a+i*h;
                    y[i] = y[i-1]+(2*math.randomreal()-1)*h;
                }
            }
            else
            {
                x[0] = 0.5*(a+b);
                y[0] = 2*math.randomreal()-1;
            }
        }


        /*************************************************************************
        This function generates  1-dimensional Chebyshev-1 interpolation task with
        moderate Lipshitz constant (close to 1.0)

        If N=1 then suborutine generates only one point at the middle of [A,B]

          -- ALGLIB --
             Copyright 02.12.2009 by Bochkanov Sergey
        *************************************************************************/
        public static void taskgenint1dcheb1(double a,
            double b,
            int n,
            ref double[] x,
            ref double[] y)
        {
            int i = 0;

            x = new double[0];
            y = new double[0];

            ap.assert(n>=1, "TaskGenInterpolation1DCheb1: N<1!");
            x = new double[n];
            y = new double[n];
            if( n>1 )
            {
                for(i=0; i<=n-1; i++)
                {
                    x[i] = 0.5*(b+a)+0.5*(b-a)*Math.Cos(Math.PI*(2*i+1)/(2*n));
                    if( i==0 )
                    {
                        y[i] = 2*math.randomreal()-1;
                    }
                    else
                    {
                        y[i] = y[i-1]+(2*math.randomreal()-1)*(x[i]-x[i-1]);
                    }
                }
            }
            else
            {
                x[0] = 0.5*(a+b);
                y[0] = 2*math.randomreal()-1;
            }
        }


        /*************************************************************************
        This function generates  1-dimensional Chebyshev-2 interpolation task with
        moderate Lipshitz constant (close to 1.0)

        If N=1 then suborutine generates only one point at the middle of [A,B]

          -- ALGLIB --
             Copyright 02.12.2009 by Bochkanov Sergey
        *************************************************************************/
        public static void taskgenint1dcheb2(double a,
            double b,
            int n,
            ref double[] x,
            ref double[] y)
        {
            int i = 0;

            x = new double[0];
            y = new double[0];

            ap.assert(n>=1, "TaskGenInterpolation1DCheb2: N<1!");
            x = new double[n];
            y = new double[n];
            if( n>1 )
            {
                for(i=0; i<=n-1; i++)
                {
                    x[i] = 0.5*(b+a)+0.5*(b-a)*Math.Cos(Math.PI*i/(n-1));
                    if( i==0 )
                    {
                        y[i] = 2*math.randomreal()-1;
                    }
                    else
                    {
                        y[i] = y[i-1]+(2*math.randomreal()-1)*(x[i]-x[i-1]);
                    }
                }
            }
            else
            {
                x[0] = 0.5*(a+b);
                y[0] = 2*math.randomreal()-1;
            }
        }


        /*************************************************************************
        This function checks that all values from X[] are distinct. It does more
        than just usual floating point comparison:
        * first, it calculates max(X) and min(X)
        * second, it maps X[] from [min,max] to [1,2]
        * only at this stage actual comparison is done

        The meaning of such check is to ensure that all values are "distinct enough"
        and will not cause interpolation subroutine to fail.

        NOTE:
            X[] must be sorted by ascending (subroutine ASSERT's it)

          -- ALGLIB --
             Copyright 02.12.2009 by Bochkanov Sergey
        *************************************************************************/
        public static bool aredistinct(double[] x,
            int n)
        {
            bool result = new bool();
            double a = 0;
            double b = 0;
            int i = 0;
            bool nonsorted = new bool();

            ap.assert(n>=1, "APSERVAreDistinct: internal error (N<1)");
            if( n==1 )
            {
                
                //
                // everything is alright, it is up to caller to decide whether it
                // can interpolate something with just one point
                //
                result = true;
                return result;
            }
            a = x[0];
            b = x[0];
            nonsorted = false;
            for(i=1; i<=n-1; i++)
            {
                a = Math.Min(a, x[i]);
                b = Math.Max(b, x[i]);
                nonsorted = nonsorted | (double)(x[i-1])>=(double)(x[i]);
            }
            ap.assert(!nonsorted, "APSERVAreDistinct: internal error (not sorted)");
            for(i=1; i<=n-1; i++)
            {
                if( (double)((x[i]-a)/(b-a)+1)==(double)((x[i-1]-a)/(b-a)+1) )
                {
                    result = false;
                    return result;
                }
            }
            result = true;
            return result;
        }


        /*************************************************************************
        If Length(X)<N, resizes X

          -- ALGLIB --
             Copyright 20.03.2009 by Bochkanov Sergey
        *************************************************************************/
        public static void bvectorsetlengthatleast(ref bool[] x,
            int n)
        {
            if( ap.len(x)<n )
            {
                x = new bool[n];
            }
        }


        /*************************************************************************
        If Length(X)<N, resizes X

          -- ALGLIB --
             Copyright 20.03.2009 by Bochkanov Sergey
        *************************************************************************/
        public static void ivectorsetlengthatleast(ref int[] x,
            int n)
        {
            if( ap.len(x)<n )
            {
                x = new int[n];
            }
        }


        /*************************************************************************
        If Length(X)<N, resizes X

          -- ALGLIB --
             Copyright 20.03.2009 by Bochkanov Sergey
        *************************************************************************/
        public static void rvectorsetlengthatleast(ref double[] x,
            int n)
        {
            if( ap.len(x)<n )
            {
                x = new double[n];
            }
        }


        /*************************************************************************
        If Cols(X)<N or Rows(X)<M, resizes X

          -- ALGLIB --
             Copyright 20.03.2009 by Bochkanov Sergey
        *************************************************************************/
        public static void rmatrixsetlengthatleast(ref double[,] x,
            int m,
            int n)
        {
            if( ap.rows(x)<m | ap.cols(x)<n )
            {
                x = new double[m, n];
            }
        }


        /*************************************************************************
        Resizes X and:
        * preserves old contents of X
        * fills new elements by zeros

          -- ALGLIB --
             Copyright 20.03.2009 by Bochkanov Sergey
        *************************************************************************/
        public static void rmatrixresize(ref double[,] x,
            int m,
            int n)
        {
            double[,] oldx = new double[0,0];
            int i = 0;
            int j = 0;
            int m2 = 0;
            int n2 = 0;

            m2 = ap.rows(x);
            n2 = ap.cols(x);
            ap.swap(ref x, ref oldx);
            x = new double[m, n];
            for(i=0; i<=m-1; i++)
            {
                for(j=0; j<=n-1; j++)
                {
                    if( i<m2 & j<n2 )
                    {
                        x[i,j] = oldx[i,j];
                    }
                    else
                    {
                        x[i,j] = 0.0;
                    }
                }
            }
        }


        /*************************************************************************
        This function checks that all values from X[] are finite

          -- ALGLIB --
             Copyright 18.06.2010 by Bochkanov Sergey
        *************************************************************************/
        public static bool isfinitevector(double[] x,
            int n)
        {
            bool result = new bool();
            int i = 0;

            ap.assert(n>=0, "APSERVIsFiniteVector: internal error (N<0)");
            for(i=0; i<=n-1; i++)
            {
                if( !math.isfinite(x[i]) )
                {
                    result = false;
                    return result;
                }
            }
            result = true;
            return result;
        }


        /*************************************************************************
        This function checks that all values from X[] are finite

          -- ALGLIB --
             Copyright 18.06.2010 by Bochkanov Sergey
        *************************************************************************/
        public static bool isfinitecvector(complex[] z,
            int n)
        {
            bool result = new bool();
            int i = 0;

            ap.assert(n>=0, "APSERVIsFiniteCVector: internal error (N<0)");
            for(i=0; i<=n-1; i++)
            {
                if( !math.isfinite(z[i].x) | !math.isfinite(z[i].y) )
                {
                    result = false;
                    return result;
                }
            }
            result = true;
            return result;
        }


        /*************************************************************************
        This function checks that all values from X[0..M-1,0..N-1] are finite

          -- ALGLIB --
             Copyright 18.06.2010 by Bochkanov Sergey
        *************************************************************************/
        public static bool apservisfinitematrix(double[,] x,
            int m,
            int n)
        {
            bool result = new bool();
            int i = 0;
            int j = 0;

            ap.assert(n>=0, "APSERVIsFiniteMatrix: internal error (N<0)");
            ap.assert(m>=0, "APSERVIsFiniteMatrix: internal error (M<0)");
            for(i=0; i<=m-1; i++)
            {
                for(j=0; j<=n-1; j++)
                {
                    if( !math.isfinite(x[i,j]) )
                    {
                        result = false;
                        return result;
                    }
                }
            }
            result = true;
            return result;
        }


        /*************************************************************************
        This function checks that all values from X[0..M-1,0..N-1] are finite

          -- ALGLIB --
             Copyright 18.06.2010 by Bochkanov Sergey
        *************************************************************************/
        public static bool apservisfinitecmatrix(complex[,] x,
            int m,
            int n)
        {
            bool result = new bool();
            int i = 0;
            int j = 0;

            ap.assert(n>=0, "APSERVIsFiniteCMatrix: internal error (N<0)");
            ap.assert(m>=0, "APSERVIsFiniteCMatrix: internal error (M<0)");
            for(i=0; i<=m-1; i++)
            {
                for(j=0; j<=n-1; j++)
                {
                    if( !math.isfinite(x[i,j].x) | !math.isfinite(x[i,j].y) )
                    {
                        result = false;
                        return result;
                    }
                }
            }
            result = true;
            return result;
        }


        /*************************************************************************
        This function checks that all values from upper/lower triangle of
        X[0..N-1,0..N-1] are finite

          -- ALGLIB --
             Copyright 18.06.2010 by Bochkanov Sergey
        *************************************************************************/
        public static bool isfinitertrmatrix(double[,] x,
            int n,
            bool isupper)
        {
            bool result = new bool();
            int i = 0;
            int j1 = 0;
            int j2 = 0;
            int j = 0;

            ap.assert(n>=0, "APSERVIsFiniteRTRMatrix: internal error (N<0)");
            for(i=0; i<=n-1; i++)
            {
                if( isupper )
                {
                    j1 = i;
                    j2 = n-1;
                }
                else
                {
                    j1 = 0;
                    j2 = i;
                }
                for(j=j1; j<=j2; j++)
                {
                    if( !math.isfinite(x[i,j]) )
                    {
                        result = false;
                        return result;
                    }
                }
            }
            result = true;
            return result;
        }


        /*************************************************************************
        This function checks that all values from upper/lower triangle of
        X[0..N-1,0..N-1] are finite

          -- ALGLIB --
             Copyright 18.06.2010 by Bochkanov Sergey
        *************************************************************************/
        public static bool apservisfinitectrmatrix(complex[,] x,
            int n,
            bool isupper)
        {
            bool result = new bool();
            int i = 0;
            int j1 = 0;
            int j2 = 0;
            int j = 0;

            ap.assert(n>=0, "APSERVIsFiniteCTRMatrix: internal error (N<0)");
            for(i=0; i<=n-1; i++)
            {
                if( isupper )
                {
                    j1 = i;
                    j2 = n-1;
                }
                else
                {
                    j1 = 0;
                    j2 = i;
                }
                for(j=j1; j<=j2; j++)
                {
                    if( !math.isfinite(x[i,j].x) | !math.isfinite(x[i,j].y) )
                    {
                        result = false;
                        return result;
                    }
                }
            }
            result = true;
            return result;
        }


        /*************************************************************************
        This function checks that all values from X[0..M-1,0..N-1] are  finite  or
        NaN's.

          -- ALGLIB --
             Copyright 18.06.2010 by Bochkanov Sergey
        *************************************************************************/
        public static bool apservisfiniteornanmatrix(double[,] x,
            int m,
            int n)
        {
            bool result = new bool();
            int i = 0;
            int j = 0;

            ap.assert(n>=0, "APSERVIsFiniteOrNaNMatrix: internal error (N<0)");
            ap.assert(m>=0, "APSERVIsFiniteOrNaNMatrix: internal error (M<0)");
            for(i=0; i<=m-1; i++)
            {
                for(j=0; j<=n-1; j++)
                {
                    if( !(math.isfinite(x[i,j]) | Double.IsNaN(x[i,j])) )
                    {
                        result = false;
                        return result;
                    }
                }
            }
            result = true;
            return result;
        }


        /*************************************************************************
        Safe sqrt(x^2+y^2)

          -- ALGLIB --
             Copyright by Bochkanov Sergey
        *************************************************************************/
        public static double safepythag2(double x,
            double y)
        {
            double result = 0;
            double w = 0;
            double xabs = 0;
            double yabs = 0;
            double z = 0;

            xabs = Math.Abs(x);
            yabs = Math.Abs(y);
            w = Math.Max(xabs, yabs);
            z = Math.Min(xabs, yabs);
            if( (double)(z)==(double)(0) )
            {
                result = w;
            }
            else
            {
                result = w*Math.Sqrt(1+math.sqr(z/w));
            }
            return result;
        }


        /*************************************************************************
        Safe sqrt(x^2+y^2)

          -- ALGLIB --
             Copyright by Bochkanov Sergey
        *************************************************************************/
        public static double safepythag3(double x,
            double y,
            double z)
        {
            double result = 0;
            double w = 0;

            w = Math.Max(Math.Abs(x), Math.Max(Math.Abs(y), Math.Abs(z)));
            if( (double)(w)==(double)(0) )
            {
                result = 0;
                return result;
            }
            x = x/w;
            y = y/w;
            z = z/w;
            result = w*Math.Sqrt(math.sqr(x)+math.sqr(y)+math.sqr(z));
            return result;
        }


        /*************************************************************************
        Safe division.

        This function attempts to calculate R=X/Y without overflow.

        It returns:
        * +1, if abs(X/Y)>=MaxRealNumber or undefined - overflow-like situation
              (no overlfow is generated, R is either NAN, PosINF, NegINF)
        *  0, if MinRealNumber<abs(X/Y)<MaxRealNumber or X=0, Y<>0
              (R contains result, may be zero)
        * -1, if 0<abs(X/Y)<MinRealNumber - underflow-like situation
              (R contains zero; it corresponds to underflow)

        No overflow is generated in any case.

          -- ALGLIB --
             Copyright by Bochkanov Sergey
        *************************************************************************/
        public static int saferdiv(double x,
            double y,
            ref double r)
        {
            int result = 0;

            r = 0;

            
            //
            // Two special cases:
            // * Y=0
            // * X=0 and Y<>0
            //
            if( (double)(y)==(double)(0) )
            {
                result = 1;
                if( (double)(x)==(double)(0) )
                {
                    r = Double.NaN;
                }
                if( (double)(x)>(double)(0) )
                {
                    r = Double.PositiveInfinity;
                }
                if( (double)(x)<(double)(0) )
                {
                    r = Double.NegativeInfinity;
                }
                return result;
            }
            if( (double)(x)==(double)(0) )
            {
                r = 0;
                result = 0;
                return result;
            }
            
            //
            // make Y>0
            //
            if( (double)(y)<(double)(0) )
            {
                x = -x;
                y = -y;
            }
            
            //
            //
            //
            if( (double)(y)>=(double)(1) )
            {
                r = x/y;
                if( (double)(Math.Abs(r))<=(double)(math.minrealnumber) )
                {
                    result = -1;
                    r = 0;
                }
                else
                {
                    result = 0;
                }
            }
            else
            {
                if( (double)(Math.Abs(x))>=(double)(math.maxrealnumber*y) )
                {
                    if( (double)(x)>(double)(0) )
                    {
                        r = Double.PositiveInfinity;
                    }
                    else
                    {
                        r = Double.NegativeInfinity;
                    }
                    result = 1;
                }
                else
                {
                    r = x/y;
                    result = 0;
                }
            }
            return result;
        }


        /*************************************************************************
        This function calculates "safe" min(X/Y,V) for positive finite X, Y, V.
        No overflow is generated in any case.

          -- ALGLIB --
             Copyright by Bochkanov Sergey
        *************************************************************************/
        public static double safeminposrv(double x,
            double y,
            double v)
        {
            double result = 0;
            double r = 0;

            if( (double)(y)>=(double)(1) )
            {
                
                //
                // Y>=1, we can safely divide by Y
                //
                r = x/y;
                result = v;
                if( (double)(v)>(double)(r) )
                {
                    result = r;
                }
                else
                {
                    result = v;
                }
            }
            else
            {
                
                //
                // Y<1, we can safely multiply by Y
                //
                if( (double)(x)<(double)(v*y) )
                {
                    result = x/y;
                }
                else
                {
                    result = v;
                }
            }
            return result;
        }


        /*************************************************************************
        This function makes periodic mapping of X to [A,B].

        It accepts X, A, B (A>B). It returns T which lies in  [A,B] and integer K,
        such that X = T + K*(B-A).

        NOTES:
        * K is represented as real value, although actually it is integer
        * T is guaranteed to be in [A,B]
        * T replaces X

          -- ALGLIB --
             Copyright by Bochkanov Sergey
        *************************************************************************/
        public static void apperiodicmap(ref double x,
            double a,
            double b,
            ref double k)
        {
            k = 0;

            ap.assert((double)(a)<(double)(b), "APPeriodicMap: internal error!");
            k = (int)Math.Floor((x-a)/(b-a));
            x = x-k*(b-a);
            while( (double)(x)<(double)(a) )
            {
                x = x+(b-a);
                k = k-1;
            }
            while( (double)(x)>(double)(b) )
            {
                x = x-(b-a);
                k = k+1;
            }
            x = Math.Max(x, a);
            x = Math.Min(x, b);
        }


        /*************************************************************************
        'bounds' value: maps X to [B1,B2]

          -- ALGLIB --
             Copyright 20.03.2009 by Bochkanov Sergey
        *************************************************************************/
        public static double boundval(double x,
            double b1,
            double b2)
        {
            double result = 0;

            if( (double)(x)<=(double)(b1) )
            {
                result = b1;
                return result;
            }
            if( (double)(x)>=(double)(b2) )
            {
                result = b2;
                return result;
            }
            result = x;
            return result;
        }


        /*************************************************************************
        Allocation of serializer: complex value
        *************************************************************************/
        public static void alloccomplex(alglib.serializer s,
            complex v)
        {
            s.alloc_entry();
            s.alloc_entry();
        }


        /*************************************************************************
        Serialization: complex value
        *************************************************************************/
        public static void serializecomplex(alglib.serializer s,
            complex v)
        {
            s.serialize_double(v.x);
            s.serialize_double(v.y);
        }


        /*************************************************************************
        Unserialization: complex value
        *************************************************************************/
        public static complex unserializecomplex(alglib.serializer s)
        {
            complex result = 0;

            result.x = s.unserialize_double();
            result.y = s.unserialize_double();
            return result;
        }


        /*************************************************************************
        Allocation of serializer: real array
        *************************************************************************/
        public static void allocrealarray(alglib.serializer s,
            double[] v,
            int n)
        {
            int i = 0;

            if( n<0 )
            {
                n = ap.len(v);
            }
            s.alloc_entry();
            for(i=0; i<=n-1; i++)
            {
                s.alloc_entry();
            }
        }


        /*************************************************************************
        Serialization: complex value
        *************************************************************************/
        public static void serializerealarray(alglib.serializer s,
            double[] v,
            int n)
        {
            int i = 0;

            if( n<0 )
            {
                n = ap.len(v);
            }
            s.serialize_int(n);
            for(i=0; i<=n-1; i++)
            {
                s.serialize_double(v[i]);
            }
        }


        /*************************************************************************
        Unserialization: complex value
        *************************************************************************/
        public static void unserializerealarray(alglib.serializer s,
            ref double[] v)
        {
            int n = 0;
            int i = 0;
            double t = 0;

            v = new double[0];

            n = s.unserialize_int();
            if( n==0 )
            {
                return;
            }
            v = new double[n];
            for(i=0; i<=n-1; i++)
            {
                t = s.unserialize_double();
                v[i] = t;
            }
        }


        /*************************************************************************
        Allocation of serializer: Integer array
        *************************************************************************/
        public static void allocintegerarray(alglib.serializer s,
            int[] v,
            int n)
        {
            int i = 0;

            if( n<0 )
            {
                n = ap.len(v);
            }
            s.alloc_entry();
            for(i=0; i<=n-1; i++)
            {
                s.alloc_entry();
            }
        }


        /*************************************************************************
        Serialization: Integer array
        *************************************************************************/
        public static void serializeintegerarray(alglib.serializer s,
            int[] v,
            int n)
        {
            int i = 0;

            if( n<0 )
            {
                n = ap.len(v);
            }
            s.serialize_int(n);
            for(i=0; i<=n-1; i++)
            {
                s.serialize_int(v[i]);
            }
        }


        /*************************************************************************
        Unserialization: complex value
        *************************************************************************/
        public static void unserializeintegerarray(alglib.serializer s,
            ref int[] v)
        {
            int n = 0;
            int i = 0;
            int t = 0;

            v = new int[0];

            n = s.unserialize_int();
            if( n==0 )
            {
                return;
            }
            v = new int[n];
            for(i=0; i<=n-1; i++)
            {
                t = s.unserialize_int();
                v[i] = t;
            }
        }


        /*************************************************************************
        Allocation of serializer: real matrix
        *************************************************************************/
        public static void allocrealmatrix(alglib.serializer s,
            double[,] v,
            int n0,
            int n1)
        {
            int i = 0;
            int j = 0;

            if( n0<0 )
            {
                n0 = ap.rows(v);
            }
            if( n1<0 )
            {
                n1 = ap.cols(v);
            }
            s.alloc_entry();
            s.alloc_entry();
            for(i=0; i<=n0-1; i++)
            {
                for(j=0; j<=n1-1; j++)
                {
                    s.alloc_entry();
                }
            }
        }


        /*************************************************************************
        Serialization: complex value
        *************************************************************************/
        public static void serializerealmatrix(alglib.serializer s,
            double[,] v,
            int n0,
            int n1)
        {
            int i = 0;
            int j = 0;

            if( n0<0 )
            {
                n0 = ap.rows(v);
            }
            if( n1<0 )
            {
                n1 = ap.cols(v);
            }
            s.serialize_int(n0);
            s.serialize_int(n1);
            for(i=0; i<=n0-1; i++)
            {
                for(j=0; j<=n1-1; j++)
                {
                    s.serialize_double(v[i,j]);
                }
            }
        }


        /*************************************************************************
        Unserialization: complex value
        *************************************************************************/
        public static void unserializerealmatrix(alglib.serializer s,
            ref double[,] v)
        {
            int i = 0;
            int j = 0;
            int n0 = 0;
            int n1 = 0;
            double t = 0;

            v = new double[0,0];

            n0 = s.unserialize_int();
            n1 = s.unserialize_int();
            if( n0==0 | n1==0 )
            {
                return;
            }
            v = new double[n0, n1];
            for(i=0; i<=n0-1; i++)
            {
                for(j=0; j<=n1-1; j++)
                {
                    t = s.unserialize_double();
                    v[i,j] = t;
                }
            }
        }


        /*************************************************************************
        Copy integer array
        *************************************************************************/
        public static void copyintegerarray(int[] src,
            ref int[] dst)
        {
            int i = 0;

            dst = new int[0];

            if( ap.len(src)>0 )
            {
                dst = new int[ap.len(src)];
                for(i=0; i<=ap.len(src)-1; i++)
                {
                    dst[i] = src[i];
                }
            }
        }


        /*************************************************************************
        Copy real array
        *************************************************************************/
        public static void copyrealarray(double[] src,
            ref double[] dst)
        {
            int i = 0;

            dst = new double[0];

            if( ap.len(src)>0 )
            {
                dst = new double[ap.len(src)];
                for(i=0; i<=ap.len(src)-1; i++)
                {
                    dst[i] = src[i];
                }
            }
        }


        /*************************************************************************
        Copy real matrix
        *************************************************************************/
        public static void copyrealmatrix(double[,] src,
            ref double[,] dst)
        {
            int i = 0;
            int j = 0;

            dst = new double[0,0];

            if( ap.rows(src)>0 & ap.cols(src)>0 )
            {
                dst = new double[ap.rows(src), ap.cols(src)];
                for(i=0; i<=ap.rows(src)-1; i++)
                {
                    for(j=0; j<=ap.cols(src)-1; j++)
                    {
                        dst[i,j] = src[i,j];
                    }
                }
            }
        }


        /*************************************************************************
        This function searches integer array. Elements in this array are actually
        records, each NRec elements wide. Each record has unique header - NHeader
        integer values, which identify it. Records are lexicographically sorted by
        header.

        Records are identified by their index, not offset (offset = NRec*index).

        This function searches A (records with indices [I0,I1)) for a record with
        header B. It returns index of this record (not offset!), or -1 on failure.

          -- ALGLIB --
             Copyright 28.03.2011 by Bochkanov Sergey
        *************************************************************************/
        public static int recsearch(ref int[] a,
            int nrec,
            int nheader,
            int i0,
            int i1,
            int[] b)
        {
            int result = 0;
            int mididx = 0;
            int cflag = 0;
            int k = 0;
            int offs = 0;

            result = -1;
            while( true )
            {
                if( i0>=i1 )
                {
                    break;
                }
                mididx = (i0+i1)/2;
                offs = nrec*mididx;
                cflag = 0;
                for(k=0; k<=nheader-1; k++)
                {
                    if( a[offs+k]<b[k] )
                    {
                        cflag = -1;
                        break;
                    }
                    if( a[offs+k]>b[k] )
                    {
                        cflag = 1;
                        break;
                    }
                }
                if( cflag==0 )
                {
                    result = mididx;
                    return result;
                }
                if( cflag<0 )
                {
                    i0 = mididx+1;
                }
                else
                {
                    i1 = mididx;
                }
            }
            return result;
        }


    }
    public class tsort
    {
        /*************************************************************************
        This function sorts array of real keys by ascending.

        Its results are:
        * sorted array A
        * permutation tables P1, P2

        Algorithm outputs permutation tables using two formats:
        * as usual permutation of [0..N-1]. If P1[i]=j, then sorted A[i]  contains
          value which was moved there from J-th position.
        * as a sequence of pairwise permutations. Sorted A[] may  be  obtained  by
          swaping A[i] and A[P2[i]] for all i from 0 to N-1.
          
        INPUT PARAMETERS:
            A       -   unsorted array
            N       -   array size

        OUPUT PARAMETERS:
            A       -   sorted array
            P1, P2  -   permutation tables, array[N]
            
        NOTES:
            this function assumes that A[] is finite; it doesn't checks that
            condition. All other conditions (size of input arrays, etc.) are not
            checked too.

          -- ALGLIB --
             Copyright 14.05.2008 by Bochkanov Sergey
        *************************************************************************/
        public static void tagsort(ref double[] a,
            int n,
            ref int[] p1,
            ref int[] p2)
        {
            apserv.apbuffers buf = new apserv.apbuffers();

            p1 = new int[0];
            p2 = new int[0];

            tagsortbuf(ref a, n, ref p1, ref p2, buf);
        }


        /*************************************************************************
        Buffered variant of TagSort, which accepts preallocated output arrays as
        well as special structure for buffered allocations. If arrays are too
        short, they are reallocated. If they are large enough, no memory
        allocation is done.

        It is intended to be used in the performance-critical parts of code, where
        additional allocations can lead to severe performance degradation

          -- ALGLIB --
             Copyright 14.05.2008 by Bochkanov Sergey
        *************************************************************************/
        public static void tagsortbuf(ref double[] a,
            int n,
            ref int[] p1,
            ref int[] p2,
            apserv.apbuffers buf)
        {
            int i = 0;
            int lv = 0;
            int lp = 0;
            int rv = 0;
            int rp = 0;

            
            //
            // Special cases
            //
            if( n<=0 )
            {
                return;
            }
            if( n==1 )
            {
                apserv.ivectorsetlengthatleast(ref p1, 1);
                apserv.ivectorsetlengthatleast(ref p2, 1);
                p1[0] = 0;
                p2[0] = 0;
                return;
            }
            
            //
            // General case, N>1: prepare permutations table P1
            //
            apserv.ivectorsetlengthatleast(ref p1, n);
            for(i=0; i<=n-1; i++)
            {
                p1[i] = i;
            }
            
            //
            // General case, N>1: sort, update P1
            //
            apserv.rvectorsetlengthatleast(ref buf.ra0, n);
            apserv.ivectorsetlengthatleast(ref buf.ia0, n);
            tagsortfasti(ref a, ref p1, ref buf.ra0, ref buf.ia0, n);
            
            //
            // General case, N>1: fill permutations table P2
            //
            // To fill P2 we maintain two arrays:
            // * PV (Buf.IA0), Position(Value). PV[i] contains position of I-th key at the moment
            // * VP (Buf.IA1), Value(Position). VP[i] contains key which has position I at the moment
            //
            // At each step we making permutation of two items:
            //   Left, which is given by position/value pair LP/LV
            //   and Right, which is given by RP/RV
            // and updating PV[] and VP[] correspondingly.
            //
            apserv.ivectorsetlengthatleast(ref buf.ia0, n);
            apserv.ivectorsetlengthatleast(ref buf.ia1, n);
            apserv.ivectorsetlengthatleast(ref p2, n);
            for(i=0; i<=n-1; i++)
            {
                buf.ia0[i] = i;
                buf.ia1[i] = i;
            }
            for(i=0; i<=n-1; i++)
            {
                
                //
                // calculate LP, LV, RP, RV
                //
                lp = i;
                lv = buf.ia1[lp];
                rv = p1[i];
                rp = buf.ia0[rv];
                
                //
                // Fill P2
                //
                p2[i] = rp;
                
                //
                // update PV and VP
                //
                buf.ia1[lp] = rv;
                buf.ia1[rp] = lv;
                buf.ia0[lv] = rp;
                buf.ia0[rv] = lp;
            }
        }


        /*************************************************************************
        Same as TagSort, but optimized for real keys and integer labels.

        A is sorted, and same permutations are applied to B.

        NOTES:
        1.  this function assumes that A[] is finite; it doesn't checks that
            condition. All other conditions (size of input arrays, etc.) are not
            checked too.
        2.  this function uses two buffers, BufA and BufB, each is N elements large.
            They may be preallocated (which will save some time) or not, in which
            case function will automatically allocate memory.

          -- ALGLIB --
             Copyright 11.12.2008 by Bochkanov Sergey
        *************************************************************************/
        public static void tagsortfasti(ref double[] a,
            ref int[] b,
            ref double[] bufa,
            ref int[] bufb,
            int n)
        {
            int i = 0;
            int j = 0;
            bool isascending = new bool();
            bool isdescending = new bool();
            double tmpr = 0;
            int tmpi = 0;

            
            //
            // Special case
            //
            if( n<=1 )
            {
                return;
            }
            
            //
            // Test for already sorted set
            //
            isascending = true;
            isdescending = true;
            for(i=1; i<=n-1; i++)
            {
                isascending = isascending & a[i]>=a[i-1];
                isdescending = isdescending & a[i]<=a[i-1];
            }
            if( isascending )
            {
                return;
            }
            if( isdescending )
            {
                for(i=0; i<=n-1; i++)
                {
                    j = n-1-i;
                    if( j<=i )
                    {
                        break;
                    }
                    tmpr = a[i];
                    a[i] = a[j];
                    a[j] = tmpr;
                    tmpi = b[i];
                    b[i] = b[j];
                    b[j] = tmpi;
                }
                return;
            }
            
            //
            // General case
            //
            if( ap.len(bufa)<n )
            {
                bufa = new double[n];
            }
            if( ap.len(bufb)<n )
            {
                bufb = new int[n];
            }
            tagsortfastirec(ref a, ref b, ref bufa, ref bufb, 0, n-1);
        }


        /*************************************************************************
        Same as TagSort, but optimized for real keys and real labels.

        A is sorted, and same permutations are applied to B.

        NOTES:
        1.  this function assumes that A[] is finite; it doesn't checks that
            condition. All other conditions (size of input arrays, etc.) are not
            checked too.
        2.  this function uses two buffers, BufA and BufB, each is N elements large.
            They may be preallocated (which will save some time) or not, in which
            case function will automatically allocate memory.

          -- ALGLIB --
             Copyright 11.12.2008 by Bochkanov Sergey
        *************************************************************************/
        public static void tagsortfastr(ref double[] a,
            ref double[] b,
            ref double[] bufa,
            ref double[] bufb,
            int n)
        {
            int i = 0;
            int j = 0;
            bool isascending = new bool();
            bool isdescending = new bool();
            double tmpr = 0;

            
            //
            // Special case
            //
            if( n<=1 )
            {
                return;
            }
            
            //
            // Test for already sorted set
            //
            isascending = true;
            isdescending = true;
            for(i=1; i<=n-1; i++)
            {
                isascending = isascending & a[i]>=a[i-1];
                isdescending = isdescending & a[i]<=a[i-1];
            }
            if( isascending )
            {
                return;
            }
            if( isdescending )
            {
                for(i=0; i<=n-1; i++)
                {
                    j = n-1-i;
                    if( j<=i )
                    {
                        break;
                    }
                    tmpr = a[i];
                    a[i] = a[j];
                    a[j] = tmpr;
                    tmpr = b[i];
                    b[i] = b[j];
                    b[j] = tmpr;
                }
                return;
            }
            
            //
            // General case
            //
            if( ap.len(bufa)<n )
            {
                bufa = new double[n];
            }
            if( ap.len(bufb)<n )
            {
                bufb = new double[n];
            }
            tagsortfastrrec(ref a, ref b, ref bufa, ref bufb, 0, n-1);
        }


        /*************************************************************************
        Same as TagSort, but optimized for real keys without labels.

        A is sorted, and that's all.

        NOTES:
        1.  this function assumes that A[] is finite; it doesn't checks that
            condition. All other conditions (size of input arrays, etc.) are not
            checked too.
        2.  this function uses buffer, BufA, which is N elements large. It may be
            preallocated (which will save some time) or not, in which case
            function will automatically allocate memory.

          -- ALGLIB --
             Copyright 11.12.2008 by Bochkanov Sergey
        *************************************************************************/
        public static void tagsortfast(ref double[] a,
            ref double[] bufa,
            int n)
        {
            int i = 0;
            int j = 0;
            bool isascending = new bool();
            bool isdescending = new bool();
            double tmpr = 0;

            
            //
            // Special case
            //
            if( n<=1 )
            {
                return;
            }
            
            //
            // Test for already sorted set
            //
            isascending = true;
            isdescending = true;
            for(i=1; i<=n-1; i++)
            {
                isascending = isascending & a[i]>=a[i-1];
                isdescending = isdescending & a[i]<=a[i-1];
            }
            if( isascending )
            {
                return;
            }
            if( isdescending )
            {
                for(i=0; i<=n-1; i++)
                {
                    j = n-1-i;
                    if( j<=i )
                    {
                        break;
                    }
                    tmpr = a[i];
                    a[i] = a[j];
                    a[j] = tmpr;
                }
                return;
            }
            
            //
            // General case
            //
            if( ap.len(bufa)<n )
            {
                bufa = new double[n];
            }
            tagsortfastrec(ref a, ref bufa, 0, n-1);
        }


        /*************************************************************************
        Heap operations: adds element to the heap

        PARAMETERS:
            A       -   heap itself, must be at least array[0..N]
            B       -   array of integer tags, which are updated according to
                        permutations in the heap
            N       -   size of the heap (without new element).
                        updated on output
            VA      -   value of the element being added
            VB      -   value of the tag

          -- ALGLIB --
             Copyright 28.02.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void tagheappushi(ref double[] a,
            ref int[] b,
            ref int n,
            double va,
            int vb)
        {
            int j = 0;
            int k = 0;
            double v = 0;

            if( n<0 )
            {
                return;
            }
            
            //
            // N=0 is a special case
            //
            if( n==0 )
            {
                a[0] = va;
                b[0] = vb;
                n = n+1;
                return;
            }
            
            //
            // add current point to the heap
            // (add to the bottom, then move up)
            //
            // we don't write point to the heap
            // until its final position is determined
            // (it allow us to reduce number of array access operations)
            //
            j = n;
            n = n+1;
            while( j>0 )
            {
                k = (j-1)/2;
                v = a[k];
                if( (double)(v)<(double)(va) )
                {
                    
                    //
                    // swap with higher element
                    //
                    a[j] = v;
                    b[j] = b[k];
                    j = k;
                }
                else
                {
                    
                    //
                    // element in its place. terminate.
                    //
                    break;
                }
            }
            a[j] = va;
            b[j] = vb;
        }


        /*************************************************************************
        Heap operations: replaces top element with new element
        (which is moved down)

        PARAMETERS:
            A       -   heap itself, must be at least array[0..N-1]
            B       -   array of integer tags, which are updated according to
                        permutations in the heap
            N       -   size of the heap
            VA      -   value of the element which replaces top element
            VB      -   value of the tag

          -- ALGLIB --
             Copyright 28.02.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void tagheapreplacetopi(ref double[] a,
            ref int[] b,
            int n,
            double va,
            int vb)
        {
            int j = 0;
            int k1 = 0;
            int k2 = 0;
            double v = 0;
            double v1 = 0;
            double v2 = 0;

            if( n<1 )
            {
                return;
            }
            
            //
            // N=1 is a special case
            //
            if( n==1 )
            {
                a[0] = va;
                b[0] = vb;
                return;
            }
            
            //
            // move down through heap:
            // * J  -   current element
            // * K1 -   first child (always exists)
            // * K2 -   second child (may not exists)
            //
            // we don't write point to the heap
            // until its final position is determined
            // (it allow us to reduce number of array access operations)
            //
            j = 0;
            k1 = 1;
            k2 = 2;
            while( k1<n )
            {
                if( k2>=n )
                {
                    
                    //
                    // only one child.
                    //
                    // swap and terminate (because this child
                    // have no siblings due to heap structure)
                    //
                    v = a[k1];
                    if( (double)(v)>(double)(va) )
                    {
                        a[j] = v;
                        b[j] = b[k1];
                        j = k1;
                    }
                    break;
                }
                else
                {
                    
                    //
                    // two childs
                    //
                    v1 = a[k1];
                    v2 = a[k2];
                    if( (double)(v1)>(double)(v2) )
                    {
                        if( (double)(va)<(double)(v1) )
                        {
                            a[j] = v1;
                            b[j] = b[k1];
                            j = k1;
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        if( (double)(va)<(double)(v2) )
                        {
                            a[j] = v2;
                            b[j] = b[k2];
                            j = k2;
                        }
                        else
                        {
                            break;
                        }
                    }
                    k1 = 2*j+1;
                    k2 = 2*j+2;
                }
            }
            a[j] = va;
            b[j] = vb;
        }


        /*************************************************************************
        Heap operations: pops top element from the heap

        PARAMETERS:
            A       -   heap itself, must be at least array[0..N-1]
            B       -   array of integer tags, which are updated according to
                        permutations in the heap
            N       -   size of the heap, N>=1

        On output top element is moved to A[N-1], B[N-1], heap is reordered, N is
        decreased by 1.

          -- ALGLIB --
             Copyright 28.02.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void tagheappopi(ref double[] a,
            ref int[] b,
            ref int n)
        {
            double va = 0;
            int vb = 0;

            if( n<1 )
            {
                return;
            }
            
            //
            // N=1 is a special case
            //
            if( n==1 )
            {
                n = 0;
                return;
            }
            
            //
            // swap top element and last element,
            // then reorder heap
            //
            va = a[n-1];
            vb = b[n-1];
            a[n-1] = a[0];
            b[n-1] = b[0];
            n = n-1;
            tagheapreplacetopi(ref a, ref b, n, va, vb);
        }


        /*************************************************************************
        Internal TagSortFastI: sorts A[I1...I2] (both bounds are included),
        applies same permutations to B.

          -- ALGLIB --
             Copyright 06.09.2010 by Bochkanov Sergey
        *************************************************************************/
        private static void tagsortfastirec(ref double[] a,
            ref int[] b,
            ref double[] bufa,
            ref int[] bufb,
            int i1,
            int i2)
        {
            int i = 0;
            int j = 0;
            int k = 0;
            int cntless = 0;
            int cnteq = 0;
            int cntgreater = 0;
            double tmpr = 0;
            int tmpi = 0;
            double v0 = 0;
            double v1 = 0;
            double v2 = 0;
            double vp = 0;

            
            //
            // Fast exit
            //
            if( i2<=i1 )
            {
                return;
            }
            
            //
            // Non-recursive sort for small arrays
            //
            if( i2-i1<=16 )
            {
                for(j=i1+1; j<=i2; j++)
                {
                    
                    //
                    // Search elements [I1..J-1] for place to insert Jth element.
                    //
                    // This code stops immediately if we can leave A[J] at J-th position
                    // (all elements have same value of A[J] larger than any of them)
                    //
                    tmpr = a[j];
                    tmpi = j;
                    for(k=j-1; k>=i1; k--)
                    {
                        if( a[k]<=tmpr )
                        {
                            break;
                        }
                        tmpi = k;
                    }
                    k = tmpi;
                    
                    //
                    // Insert Jth element into Kth position
                    //
                    if( k!=j )
                    {
                        tmpr = a[j];
                        tmpi = b[j];
                        for(i=j-1; i>=k; i--)
                        {
                            a[i+1] = a[i];
                            b[i+1] = b[i];
                        }
                        a[k] = tmpr;
                        b[k] = tmpi;
                    }
                }
                return;
            }
            
            //
            // Quicksort: choose pivot
            // Here we assume that I2-I1>=2
            //
            v0 = a[i1];
            v1 = a[i1+(i2-i1)/2];
            v2 = a[i2];
            if( v0>v1 )
            {
                tmpr = v1;
                v1 = v0;
                v0 = tmpr;
            }
            if( v1>v2 )
            {
                tmpr = v2;
                v2 = v1;
                v1 = tmpr;
            }
            if( v0>v1 )
            {
                tmpr = v1;
                v1 = v0;
                v0 = tmpr;
            }
            vp = v1;
            
            //
            // now pass through A/B and:
            // * move elements that are LESS than VP to the left of A/B
            // * move elements that are EQUAL to VP to the right of BufA/BufB (in the reverse order)
            // * move elements that are GREATER than VP to the left of BufA/BufB (in the normal order
            // * move elements from the tail of BufA/BufB to the middle of A/B (restoring normal order)
            // * move elements from the left of BufA/BufB to the end of A/B
            //
            cntless = 0;
            cnteq = 0;
            cntgreater = 0;
            for(i=i1; i<=i2; i++)
            {
                v0 = a[i];
                if( v0<vp )
                {
                    
                    //
                    // LESS
                    //
                    k = i1+cntless;
                    if( i!=k )
                    {
                        a[k] = v0;
                        b[k] = b[i];
                    }
                    cntless = cntless+1;
                    continue;
                }
                if( v0==vp )
                {
                    
                    //
                    // EQUAL
                    //
                    k = i2-cnteq;
                    bufa[k] = v0;
                    bufb[k] = b[i];
                    cnteq = cnteq+1;
                    continue;
                }
                
                //
                // GREATER
                //
                k = i1+cntgreater;
                bufa[k] = v0;
                bufb[k] = b[i];
                cntgreater = cntgreater+1;
            }
            for(i=0; i<=cnteq-1; i++)
            {
                j = i1+cntless+cnteq-1-i;
                k = i2+i-(cnteq-1);
                a[j] = bufa[k];
                b[j] = bufb[k];
            }
            for(i=0; i<=cntgreater-1; i++)
            {
                j = i1+cntless+cnteq+i;
                k = i1+i;
                a[j] = bufa[k];
                b[j] = bufb[k];
            }
            
            //
            // Sort left and right parts of the array (ignoring middle part)
            //
            tagsortfastirec(ref a, ref b, ref bufa, ref bufb, i1, i1+cntless-1);
            tagsortfastirec(ref a, ref b, ref bufa, ref bufb, i1+cntless+cnteq, i2);
        }


        /*************************************************************************
        Internal TagSortFastR: sorts A[I1...I2] (both bounds are included),
        applies same permutations to B.

          -- ALGLIB --
             Copyright 06.09.2010 by Bochkanov Sergey
        *************************************************************************/
        private static void tagsortfastrrec(ref double[] a,
            ref double[] b,
            ref double[] bufa,
            ref double[] bufb,
            int i1,
            int i2)
        {
            int i = 0;
            int j = 0;
            int k = 0;
            double tmpr = 0;
            double tmpr2 = 0;
            int tmpi = 0;
            int cntless = 0;
            int cnteq = 0;
            int cntgreater = 0;
            double v0 = 0;
            double v1 = 0;
            double v2 = 0;
            double vp = 0;

            
            //
            // Fast exit
            //
            if( i2<=i1 )
            {
                return;
            }
            
            //
            // Non-recursive sort for small arrays
            //
            if( i2-i1<=16 )
            {
                for(j=i1+1; j<=i2; j++)
                {
                    
                    //
                    // Search elements [I1..J-1] for place to insert Jth element.
                    //
                    // This code stops immediatly if we can leave A[J] at J-th position
                    // (all elements have same value of A[J] larger than any of them)
                    //
                    tmpr = a[j];
                    tmpi = j;
                    for(k=j-1; k>=i1; k--)
                    {
                        if( a[k]<=tmpr )
                        {
                            break;
                        }
                        tmpi = k;
                    }
                    k = tmpi;
                    
                    //
                    // Insert Jth element into Kth position
                    //
                    if( k!=j )
                    {
                        tmpr = a[j];
                        tmpr2 = b[j];
                        for(i=j-1; i>=k; i--)
                        {
                            a[i+1] = a[i];
                            b[i+1] = b[i];
                        }
                        a[k] = tmpr;
                        b[k] = tmpr2;
                    }
                }
                return;
            }
            
            //
            // Quicksort: choose pivot
            // Here we assume that I2-I1>=16
            //
            v0 = a[i1];
            v1 = a[i1+(i2-i1)/2];
            v2 = a[i2];
            if( v0>v1 )
            {
                tmpr = v1;
                v1 = v0;
                v0 = tmpr;
            }
            if( v1>v2 )
            {
                tmpr = v2;
                v2 = v1;
                v1 = tmpr;
            }
            if( v0>v1 )
            {
                tmpr = v1;
                v1 = v0;
                v0 = tmpr;
            }
            vp = v1;
            
            //
            // now pass through A/B and:
            // * move elements that are LESS than VP to the left of A/B
            // * move elements that are EQUAL to VP to the right of BufA/BufB (in the reverse order)
            // * move elements that are GREATER than VP to the left of BufA/BufB (in the normal order
            // * move elements from the tail of BufA/BufB to the middle of A/B (restoring normal order)
            // * move elements from the left of BufA/BufB to the end of A/B
            //
            cntless = 0;
            cnteq = 0;
            cntgreater = 0;
            for(i=i1; i<=i2; i++)
            {
                v0 = a[i];
                if( v0<vp )
                {
                    
                    //
                    // LESS
                    //
                    k = i1+cntless;
                    if( i!=k )
                    {
                        a[k] = v0;
                        b[k] = b[i];
                    }
                    cntless = cntless+1;
                    continue;
                }
                if( v0==vp )
                {
                    
                    //
                    // EQUAL
                    //
                    k = i2-cnteq;
                    bufa[k] = v0;
                    bufb[k] = b[i];
                    cnteq = cnteq+1;
                    continue;
                }
                
                //
                // GREATER
                //
                k = i1+cntgreater;
                bufa[k] = v0;
                bufb[k] = b[i];
                cntgreater = cntgreater+1;
            }
            for(i=0; i<=cnteq-1; i++)
            {
                j = i1+cntless+cnteq-1-i;
                k = i2+i-(cnteq-1);
                a[j] = bufa[k];
                b[j] = bufb[k];
            }
            for(i=0; i<=cntgreater-1; i++)
            {
                j = i1+cntless+cnteq+i;
                k = i1+i;
                a[j] = bufa[k];
                b[j] = bufb[k];
            }
            
            //
            // Sort left and right parts of the array (ignoring middle part)
            //
            tagsortfastrrec(ref a, ref b, ref bufa, ref bufb, i1, i1+cntless-1);
            tagsortfastrrec(ref a, ref b, ref bufa, ref bufb, i1+cntless+cnteq, i2);
        }


        /*************************************************************************
        Internal TagSortFastI: sorts A[I1...I2] (both bounds are included),
        applies same permutations to B.

          -- ALGLIB --
             Copyright 06.09.2010 by Bochkanov Sergey
        *************************************************************************/
        private static void tagsortfastrec(ref double[] a,
            ref double[] bufa,
            int i1,
            int i2)
        {
            int cntless = 0;
            int cnteq = 0;
            int cntgreater = 0;
            int i = 0;
            int j = 0;
            int k = 0;
            double tmpr = 0;
            int tmpi = 0;
            double v0 = 0;
            double v1 = 0;
            double v2 = 0;
            double vp = 0;

            
            //
            // Fast exit
            //
            if( i2<=i1 )
            {
                return;
            }
            
            //
            // Non-recursive sort for small arrays
            //
            if( i2-i1<=16 )
            {
                for(j=i1+1; j<=i2; j++)
                {
                    
                    //
                    // Search elements [I1..J-1] for place to insert Jth element.
                    //
                    // This code stops immediatly if we can leave A[J] at J-th position
                    // (all elements have same value of A[J] larger than any of them)
                    //
                    tmpr = a[j];
                    tmpi = j;
                    for(k=j-1; k>=i1; k--)
                    {
                        if( a[k]<=tmpr )
                        {
                            break;
                        }
                        tmpi = k;
                    }
                    k = tmpi;
                    
                    //
                    // Insert Jth element into Kth position
                    //
                    if( k!=j )
                    {
                        tmpr = a[j];
                        for(i=j-1; i>=k; i--)
                        {
                            a[i+1] = a[i];
                        }
                        a[k] = tmpr;
                    }
                }
                return;
            }
            
            //
            // Quicksort: choose pivot
            // Here we assume that I2-I1>=16
            //
            v0 = a[i1];
            v1 = a[i1+(i2-i1)/2];
            v2 = a[i2];
            if( v0>v1 )
            {
                tmpr = v1;
                v1 = v0;
                v0 = tmpr;
            }
            if( v1>v2 )
            {
                tmpr = v2;
                v2 = v1;
                v1 = tmpr;
            }
            if( v0>v1 )
            {
                tmpr = v1;
                v1 = v0;
                v0 = tmpr;
            }
            vp = v1;
            
            //
            // now pass through A/B and:
            // * move elements that are LESS than VP to the left of A/B
            // * move elements that are EQUAL to VP to the right of BufA/BufB (in the reverse order)
            // * move elements that are GREATER than VP to the left of BufA/BufB (in the normal order
            // * move elements from the tail of BufA/BufB to the middle of A/B (restoring normal order)
            // * move elements from the left of BufA/BufB to the end of A/B
            //
            cntless = 0;
            cnteq = 0;
            cntgreater = 0;
            for(i=i1; i<=i2; i++)
            {
                v0 = a[i];
                if( v0<vp )
                {
                    
                    //
                    // LESS
                    //
                    k = i1+cntless;
                    if( i!=k )
                    {
                        a[k] = v0;
                    }
                    cntless = cntless+1;
                    continue;
                }
                if( v0==vp )
                {
                    
                    //
                    // EQUAL
                    //
                    k = i2-cnteq;
                    bufa[k] = v0;
                    cnteq = cnteq+1;
                    continue;
                }
                
                //
                // GREATER
                //
                k = i1+cntgreater;
                bufa[k] = v0;
                cntgreater = cntgreater+1;
            }
            for(i=0; i<=cnteq-1; i++)
            {
                j = i1+cntless+cnteq-1-i;
                k = i2+i-(cnteq-1);
                a[j] = bufa[k];
            }
            for(i=0; i<=cntgreater-1; i++)
            {
                j = i1+cntless+cnteq+i;
                k = i1+i;
                a[j] = bufa[k];
            }
            
            //
            // Sort left and right parts of the array (ignoring middle part)
            //
            tagsortfastrec(ref a, ref bufa, i1, i1+cntless-1);
            tagsortfastrec(ref a, ref bufa, i1+cntless+cnteq, i2);
        }


    }
    public class basicstatops
    {
        /*************************************************************************
        Internal ranking subroutine
        *************************************************************************/
        public static void rankx(ref double[] x,
            int n,
            apserv.apbuffers buf)
        {
            int i = 0;
            int j = 0;
            int k = 0;
            int t = 0;
            double tmp = 0;
            int tmpi = 0;

            
            //
            // Prepare
            //
            if( n<1 )
            {
                return;
            }
            if( n==1 )
            {
                x[0] = 1;
                return;
            }
            if( ap.len(buf.ra1)<n )
            {
                buf.ra1 = new double[n];
            }
            if( ap.len(buf.ia1)<n )
            {
                buf.ia1 = new int[n];
            }
            for(i=0; i<=n-1; i++)
            {
                buf.ra1[i] = x[i];
                buf.ia1[i] = i;
            }
            
            //
            // sort {R, C}
            //
            if( n!=1 )
            {
                i = 2;
                do
                {
                    t = i;
                    while( t!=1 )
                    {
                        k = t/2;
                        if( (double)(buf.ra1[k-1])>=(double)(buf.ra1[t-1]) )
                        {
                            t = 1;
                        }
                        else
                        {
                            tmp = buf.ra1[k-1];
                            buf.ra1[k-1] = buf.ra1[t-1];
                            buf.ra1[t-1] = tmp;
                            tmpi = buf.ia1[k-1];
                            buf.ia1[k-1] = buf.ia1[t-1];
                            buf.ia1[t-1] = tmpi;
                            t = k;
                        }
                    }
                    i = i+1;
                }
                while( i<=n );
                i = n-1;
                do
                {
                    tmp = buf.ra1[i];
                    buf.ra1[i] = buf.ra1[0];
                    buf.ra1[0] = tmp;
                    tmpi = buf.ia1[i];
                    buf.ia1[i] = buf.ia1[0];
                    buf.ia1[0] = tmpi;
                    t = 1;
                    while( t!=0 )
                    {
                        k = 2*t;
                        if( k>i )
                        {
                            t = 0;
                        }
                        else
                        {
                            if( k<i )
                            {
                                if( (double)(buf.ra1[k])>(double)(buf.ra1[k-1]) )
                                {
                                    k = k+1;
                                }
                            }
                            if( (double)(buf.ra1[t-1])>=(double)(buf.ra1[k-1]) )
                            {
                                t = 0;
                            }
                            else
                            {
                                tmp = buf.ra1[k-1];
                                buf.ra1[k-1] = buf.ra1[t-1];
                                buf.ra1[t-1] = tmp;
                                tmpi = buf.ia1[k-1];
                                buf.ia1[k-1] = buf.ia1[t-1];
                                buf.ia1[t-1] = tmpi;
                                t = k;
                            }
                        }
                    }
                    i = i-1;
                }
                while( i>=1 );
            }
            
            //
            // compute tied ranks
            //
            i = 0;
            while( i<=n-1 )
            {
                j = i+1;
                while( j<=n-1 )
                {
                    if( (double)(buf.ra1[j])!=(double)(buf.ra1[i]) )
                    {
                        break;
                    }
                    j = j+1;
                }
                for(k=i; k<=j-1; k++)
                {
                    buf.ra1[k] = 1+(double)(i+j-1)/(double)2;
                }
                i = j;
            }
            
            //
            // back to x
            //
            for(i=0; i<=n-1; i++)
            {
                x[buf.ia1[i]] = buf.ra1[i];
            }
        }


    }
    public class ablasf
    {
        /*************************************************************************
        Fast kernel

          -- ALGLIB routine --
             19.01.2010
             Bochkanov Sergey
        *************************************************************************/
        public static bool cmatrixrank1f(int m,
            int n,
            ref complex[,] a,
            int ia,
            int ja,
            ref complex[] u,
            int iu,
            ref complex[] v,
            int iv)
        {
            bool result = new bool();

            result = false;
            return result;
        }


        /*************************************************************************
        Fast kernel

          -- ALGLIB routine --
             19.01.2010
             Bochkanov Sergey
        *************************************************************************/
        public static bool rmatrixrank1f(int m,
            int n,
            ref double[,] a,
            int ia,
            int ja,
            ref double[] u,
            int iu,
            ref double[] v,
            int iv)
        {
            bool result = new bool();

            result = false;
            return result;
        }


        /*************************************************************************
        Fast kernel

          -- ALGLIB routine --
             19.01.2010
             Bochkanov Sergey
        *************************************************************************/
        public static bool cmatrixmvf(int m,
            int n,
            complex[,] a,
            int ia,
            int ja,
            int opa,
            complex[] x,
            int ix,
            ref complex[] y,
            int iy)
        {
            bool result = new bool();

            result = false;
            return result;
        }


        /*************************************************************************
        Fast kernel

          -- ALGLIB routine --
             19.01.2010
             Bochkanov Sergey
        *************************************************************************/
        public static bool rmatrixmvf(int m,
            int n,
            double[,] a,
            int ia,
            int ja,
            int opa,
            double[] x,
            int ix,
            ref double[] y,
            int iy)
        {
            bool result = new bool();

            result = false;
            return result;
        }


        /*************************************************************************
        Fast kernel

          -- ALGLIB routine --
             19.01.2010
             Bochkanov Sergey
        *************************************************************************/
        public static bool cmatrixrighttrsmf(int m,
            int n,
            complex[,] a,
            int i1,
            int j1,
            bool isupper,
            bool isunit,
            int optype,
            ref complex[,] x,
            int i2,
            int j2)
        {
            bool result = new bool();

            result = false;
            return result;
        }


        /*************************************************************************
        Fast kernel

          -- ALGLIB routine --
             19.01.2010
             Bochkanov Sergey
        *************************************************************************/
        public static bool cmatrixlefttrsmf(int m,
            int n,
            complex[,] a,
            int i1,
            int j1,
            bool isupper,
            bool isunit,
            int optype,
            ref complex[,] x,
            int i2,
            int j2)
        {
            bool result = new bool();

            result = false;
            return result;
        }


        /*************************************************************************
        Fast kernel

          -- ALGLIB routine --
             19.01.2010
             Bochkanov Sergey
        *************************************************************************/
        public static bool rmatrixrighttrsmf(int m,
            int n,
            double[,] a,
            int i1,
            int j1,
            bool isupper,
            bool isunit,
            int optype,
            ref double[,] x,
            int i2,
            int j2)
        {
            bool result = new bool();

            result = false;
            return result;
        }


        /*************************************************************************
        Fast kernel

          -- ALGLIB routine --
             19.01.2010
             Bochkanov Sergey
        *************************************************************************/
        public static bool rmatrixlefttrsmf(int m,
            int n,
            double[,] a,
            int i1,
            int j1,
            bool isupper,
            bool isunit,
            int optype,
            ref double[,] x,
            int i2,
            int j2)
        {
            bool result = new bool();

            result = false;
            return result;
        }


        /*************************************************************************
        Fast kernel

          -- ALGLIB routine --
             19.01.2010
             Bochkanov Sergey
        *************************************************************************/
        public static bool cmatrixsyrkf(int n,
            int k,
            double alpha,
            complex[,] a,
            int ia,
            int ja,
            int optypea,
            double beta,
            ref complex[,] c,
            int ic,
            int jc,
            bool isupper)
        {
            bool result = new bool();

            result = false;
            return result;
        }


        /*************************************************************************
        Fast kernel

          -- ALGLIB routine --
             19.01.2010
             Bochkanov Sergey
        *************************************************************************/
        public static bool rmatrixsyrkf(int n,
            int k,
            double alpha,
            double[,] a,
            int ia,
            int ja,
            int optypea,
            double beta,
            ref double[,] c,
            int ic,
            int jc,
            bool isupper)
        {
            bool result = new bool();

            result = false;
            return result;
        }


        /*************************************************************************
        Fast kernel

          -- ALGLIB routine --
             19.01.2010
             Bochkanov Sergey
        *************************************************************************/
        public static bool rmatrixgemmf(int m,
            int n,
            int k,
            double alpha,
            double[,] a,
            int ia,
            int ja,
            int optypea,
            double[,] b,
            int ib,
            int jb,
            int optypeb,
            double beta,
            ref double[,] c,
            int ic,
            int jc)
        {
            bool result = new bool();

            result = false;
            return result;
        }


        /*************************************************************************
        Fast kernel

          -- ALGLIB routine --
             19.01.2010
             Bochkanov Sergey
        *************************************************************************/
        public static bool cmatrixgemmf(int m,
            int n,
            int k,
            complex alpha,
            complex[,] a,
            int ia,
            int ja,
            int optypea,
            complex[,] b,
            int ib,
            int jb,
            int optypeb,
            complex beta,
            ref complex[,] c,
            int ic,
            int jc)
        {
            bool result = new bool();

            result = false;
            return result;
        }


    }
    public class blas
    {
        public static double vectornorm2(double[] x,
            int i1,
            int i2)
        {
            double result = 0;
            int n = 0;
            int ix = 0;
            double absxi = 0;
            double scl = 0;
            double ssq = 0;

            n = i2-i1+1;
            if( n<1 )
            {
                result = 0;
                return result;
            }
            if( n==1 )
            {
                result = Math.Abs(x[i1]);
                return result;
            }
            scl = 0;
            ssq = 1;
            for(ix=i1; ix<=i2; ix++)
            {
                if( (double)(x[ix])!=(double)(0) )
                {
                    absxi = Math.Abs(x[ix]);
                    if( (double)(scl)<(double)(absxi) )
                    {
                        ssq = 1+ssq*math.sqr(scl/absxi);
                        scl = absxi;
                    }
                    else
                    {
                        ssq = ssq+math.sqr(absxi/scl);
                    }
                }
            }
            result = scl*Math.Sqrt(ssq);
            return result;
        }


        public static int vectoridxabsmax(double[] x,
            int i1,
            int i2)
        {
            int result = 0;
            int i = 0;
            double a = 0;

            result = i1;
            a = Math.Abs(x[result]);
            for(i=i1+1; i<=i2; i++)
            {
                if( (double)(Math.Abs(x[i]))>(double)(Math.Abs(x[result])) )
                {
                    result = i;
                }
            }
            return result;
        }


        public static int columnidxabsmax(double[,] x,
            int i1,
            int i2,
            int j)
        {
            int result = 0;
            int i = 0;
            double a = 0;

            result = i1;
            a = Math.Abs(x[result,j]);
            for(i=i1+1; i<=i2; i++)
            {
                if( (double)(Math.Abs(x[i,j]))>(double)(Math.Abs(x[result,j])) )
                {
                    result = i;
                }
            }
            return result;
        }


        public static int rowidxabsmax(double[,] x,
            int j1,
            int j2,
            int i)
        {
            int result = 0;
            int j = 0;
            double a = 0;

            result = j1;
            a = Math.Abs(x[i,result]);
            for(j=j1+1; j<=j2; j++)
            {
                if( (double)(Math.Abs(x[i,j]))>(double)(Math.Abs(x[i,result])) )
                {
                    result = j;
                }
            }
            return result;
        }


        public static double upperhessenberg1norm(double[,] a,
            int i1,
            int i2,
            int j1,
            int j2,
            ref double[] work)
        {
            double result = 0;
            int i = 0;
            int j = 0;

            ap.assert(i2-i1==j2-j1, "UpperHessenberg1Norm: I2-I1<>J2-J1!");
            for(j=j1; j<=j2; j++)
            {
                work[j] = 0;
            }
            for(i=i1; i<=i2; i++)
            {
                for(j=Math.Max(j1, j1+i-i1-1); j<=j2; j++)
                {
                    work[j] = work[j]+Math.Abs(a[i,j]);
                }
            }
            result = 0;
            for(j=j1; j<=j2; j++)
            {
                result = Math.Max(result, work[j]);
            }
            return result;
        }


        public static void copymatrix(double[,] a,
            int is1,
            int is2,
            int js1,
            int js2,
            ref double[,] b,
            int id1,
            int id2,
            int jd1,
            int jd2)
        {
            int isrc = 0;
            int idst = 0;
            int i_ = 0;
            int i1_ = 0;

            if( is1>is2 | js1>js2 )
            {
                return;
            }
            ap.assert(is2-is1==id2-id1, "CopyMatrix: different sizes!");
            ap.assert(js2-js1==jd2-jd1, "CopyMatrix: different sizes!");
            for(isrc=is1; isrc<=is2; isrc++)
            {
                idst = isrc-is1+id1;
                i1_ = (js1) - (jd1);
                for(i_=jd1; i_<=jd2;i_++)
                {
                    b[idst,i_] = a[isrc,i_+i1_];
                }
            }
        }


        public static void inplacetranspose(ref double[,] a,
            int i1,
            int i2,
            int j1,
            int j2,
            ref double[] work)
        {
            int i = 0;
            int j = 0;
            int ips = 0;
            int jps = 0;
            int l = 0;
            int i_ = 0;
            int i1_ = 0;

            if( i1>i2 | j1>j2 )
            {
                return;
            }
            ap.assert(i1-i2==j1-j2, "InplaceTranspose error: incorrect array size!");
            for(i=i1; i<=i2-1; i++)
            {
                j = j1+i-i1;
                ips = i+1;
                jps = j1+ips-i1;
                l = i2-i;
                i1_ = (ips) - (1);
                for(i_=1; i_<=l;i_++)
                {
                    work[i_] = a[i_+i1_,j];
                }
                i1_ = (jps) - (ips);
                for(i_=ips; i_<=i2;i_++)
                {
                    a[i_,j] = a[i,i_+i1_];
                }
                i1_ = (1) - (jps);
                for(i_=jps; i_<=j2;i_++)
                {
                    a[i,i_] = work[i_+i1_];
                }
            }
        }


        public static void copyandtranspose(double[,] a,
            int is1,
            int is2,
            int js1,
            int js2,
            ref double[,] b,
            int id1,
            int id2,
            int jd1,
            int jd2)
        {
            int isrc = 0;
            int jdst = 0;
            int i_ = 0;
            int i1_ = 0;

            if( is1>is2 | js1>js2 )
            {
                return;
            }
            ap.assert(is2-is1==jd2-jd1, "CopyAndTranspose: different sizes!");
            ap.assert(js2-js1==id2-id1, "CopyAndTranspose: different sizes!");
            for(isrc=is1; isrc<=is2; isrc++)
            {
                jdst = isrc-is1+jd1;
                i1_ = (js1) - (id1);
                for(i_=id1; i_<=id2;i_++)
                {
                    b[i_,jdst] = a[isrc,i_+i1_];
                }
            }
        }


        public static void matrixvectormultiply(double[,] a,
            int i1,
            int i2,
            int j1,
            int j2,
            bool trans,
            double[] x,
            int ix1,
            int ix2,
            double alpha,
            ref double[] y,
            int iy1,
            int iy2,
            double beta)
        {
            int i = 0;
            double v = 0;
            int i_ = 0;
            int i1_ = 0;

            if( !trans )
            {
                
                //
                // y := alpha*A*x + beta*y;
                //
                if( i1>i2 | j1>j2 )
                {
                    return;
                }
                ap.assert(j2-j1==ix2-ix1, "MatrixVectorMultiply: A and X dont match!");
                ap.assert(i2-i1==iy2-iy1, "MatrixVectorMultiply: A and Y dont match!");
                
                //
                // beta*y
                //
                if( (double)(beta)==(double)(0) )
                {
                    for(i=iy1; i<=iy2; i++)
                    {
                        y[i] = 0;
                    }
                }
                else
                {
                    for(i_=iy1; i_<=iy2;i_++)
                    {
                        y[i_] = beta*y[i_];
                    }
                }
                
                //
                // alpha*A*x
                //
                for(i=i1; i<=i2; i++)
                {
                    i1_ = (ix1)-(j1);
                    v = 0.0;
                    for(i_=j1; i_<=j2;i_++)
                    {
                        v += a[i,i_]*x[i_+i1_];
                    }
                    y[iy1+i-i1] = y[iy1+i-i1]+alpha*v;
                }
            }
            else
            {
                
                //
                // y := alpha*A'*x + beta*y;
                //
                if( i1>i2 | j1>j2 )
                {
                    return;
                }
                ap.assert(i2-i1==ix2-ix1, "MatrixVectorMultiply: A and X dont match!");
                ap.assert(j2-j1==iy2-iy1, "MatrixVectorMultiply: A and Y dont match!");
                
                //
                // beta*y
                //
                if( (double)(beta)==(double)(0) )
                {
                    for(i=iy1; i<=iy2; i++)
                    {
                        y[i] = 0;
                    }
                }
                else
                {
                    for(i_=iy1; i_<=iy2;i_++)
                    {
                        y[i_] = beta*y[i_];
                    }
                }
                
                //
                // alpha*A'*x
                //
                for(i=i1; i<=i2; i++)
                {
                    v = alpha*x[ix1+i-i1];
                    i1_ = (j1) - (iy1);
                    for(i_=iy1; i_<=iy2;i_++)
                    {
                        y[i_] = y[i_] + v*a[i,i_+i1_];
                    }
                }
            }
        }


        public static double pythag2(double x,
            double y)
        {
            double result = 0;
            double w = 0;
            double xabs = 0;
            double yabs = 0;
            double z = 0;

            xabs = Math.Abs(x);
            yabs = Math.Abs(y);
            w = Math.Max(xabs, yabs);
            z = Math.Min(xabs, yabs);
            if( (double)(z)==(double)(0) )
            {
                result = w;
            }
            else
            {
                result = w*Math.Sqrt(1+math.sqr(z/w));
            }
            return result;
        }


        public static void matrixmatrixmultiply(double[,] a,
            int ai1,
            int ai2,
            int aj1,
            int aj2,
            bool transa,
            double[,] b,
            int bi1,
            int bi2,
            int bj1,
            int bj2,
            bool transb,
            double alpha,
            ref double[,] c,
            int ci1,
            int ci2,
            int cj1,
            int cj2,
            double beta,
            ref double[] work)
        {
            int arows = 0;
            int acols = 0;
            int brows = 0;
            int bcols = 0;
            int crows = 0;
            int ccols = 0;
            int i = 0;
            int j = 0;
            int k = 0;
            int l = 0;
            int r = 0;
            double v = 0;
            int i_ = 0;
            int i1_ = 0;

            
            //
            // Setup
            //
            if( !transa )
            {
                arows = ai2-ai1+1;
                acols = aj2-aj1+1;
            }
            else
            {
                arows = aj2-aj1+1;
                acols = ai2-ai1+1;
            }
            if( !transb )
            {
                brows = bi2-bi1+1;
                bcols = bj2-bj1+1;
            }
            else
            {
                brows = bj2-bj1+1;
                bcols = bi2-bi1+1;
            }
            ap.assert(acols==brows, "MatrixMatrixMultiply: incorrect matrix sizes!");
            if( ((arows<=0 | acols<=0) | brows<=0) | bcols<=0 )
            {
                return;
            }
            crows = arows;
            ccols = bcols;
            
            //
            // Test WORK
            //
            i = Math.Max(arows, acols);
            i = Math.Max(brows, i);
            i = Math.Max(i, bcols);
            work[1] = 0;
            work[i] = 0;
            
            //
            // Prepare C
            //
            if( (double)(beta)==(double)(0) )
            {
                for(i=ci1; i<=ci2; i++)
                {
                    for(j=cj1; j<=cj2; j++)
                    {
                        c[i,j] = 0;
                    }
                }
            }
            else
            {
                for(i=ci1; i<=ci2; i++)
                {
                    for(i_=cj1; i_<=cj2;i_++)
                    {
                        c[i,i_] = beta*c[i,i_];
                    }
                }
            }
            
            //
            // A*B
            //
            if( !transa & !transb )
            {
                for(l=ai1; l<=ai2; l++)
                {
                    for(r=bi1; r<=bi2; r++)
                    {
                        v = alpha*a[l,aj1+r-bi1];
                        k = ci1+l-ai1;
                        i1_ = (bj1) - (cj1);
                        for(i_=cj1; i_<=cj2;i_++)
                        {
                            c[k,i_] = c[k,i_] + v*b[r,i_+i1_];
                        }
                    }
                }
                return;
            }
            
            //
            // A*B'
            //
            if( !transa & transb )
            {
                if( arows*acols<brows*bcols )
                {
                    for(r=bi1; r<=bi2; r++)
                    {
                        for(l=ai1; l<=ai2; l++)
                        {
                            i1_ = (bj1)-(aj1);
                            v = 0.0;
                            for(i_=aj1; i_<=aj2;i_++)
                            {
                                v += a[l,i_]*b[r,i_+i1_];
                            }
                            c[ci1+l-ai1,cj1+r-bi1] = c[ci1+l-ai1,cj1+r-bi1]+alpha*v;
                        }
                    }
                    return;
                }
                else
                {
                    for(l=ai1; l<=ai2; l++)
                    {
                        for(r=bi1; r<=bi2; r++)
                        {
                            i1_ = (bj1)-(aj1);
                            v = 0.0;
                            for(i_=aj1; i_<=aj2;i_++)
                            {
                                v += a[l,i_]*b[r,i_+i1_];
                            }
                            c[ci1+l-ai1,cj1+r-bi1] = c[ci1+l-ai1,cj1+r-bi1]+alpha*v;
                        }
                    }
                    return;
                }
            }
            
            //
            // A'*B
            //
            if( transa & !transb )
            {
                for(l=aj1; l<=aj2; l++)
                {
                    for(r=bi1; r<=bi2; r++)
                    {
                        v = alpha*a[ai1+r-bi1,l];
                        k = ci1+l-aj1;
                        i1_ = (bj1) - (cj1);
                        for(i_=cj1; i_<=cj2;i_++)
                        {
                            c[k,i_] = c[k,i_] + v*b[r,i_+i1_];
                        }
                    }
                }
                return;
            }
            
            //
            // A'*B'
            //
            if( transa & transb )
            {
                if( arows*acols<brows*bcols )
                {
                    for(r=bi1; r<=bi2; r++)
                    {
                        k = cj1+r-bi1;
                        for(i=1; i<=crows; i++)
                        {
                            work[i] = 0.0;
                        }
                        for(l=ai1; l<=ai2; l++)
                        {
                            v = alpha*b[r,bj1+l-ai1];
                            i1_ = (aj1) - (1);
                            for(i_=1; i_<=crows;i_++)
                            {
                                work[i_] = work[i_] + v*a[l,i_+i1_];
                            }
                        }
                        i1_ = (1) - (ci1);
                        for(i_=ci1; i_<=ci2;i_++)
                        {
                            c[i_,k] = c[i_,k] + work[i_+i1_];
                        }
                    }
                    return;
                }
                else
                {
                    for(l=aj1; l<=aj2; l++)
                    {
                        k = ai2-ai1+1;
                        i1_ = (ai1) - (1);
                        for(i_=1; i_<=k;i_++)
                        {
                            work[i_] = a[i_+i1_,l];
                        }
                        for(r=bi1; r<=bi2; r++)
                        {
                            i1_ = (bj1)-(1);
                            v = 0.0;
                            for(i_=1; i_<=k;i_++)
                            {
                                v += work[i_]*b[r,i_+i1_];
                            }
                            c[ci1+l-aj1,cj1+r-bi1] = c[ci1+l-aj1,cj1+r-bi1]+alpha*v;
                        }
                    }
                    return;
                }
            }
        }


    }
    public class hblas
    {
        public static void hermitianmatrixvectormultiply(complex[,] a,
            bool isupper,
            int i1,
            int i2,
            complex[] x,
            complex alpha,
            ref complex[] y)
        {
            int i = 0;
            int ba1 = 0;
            int ba2 = 0;
            int by1 = 0;
            int by2 = 0;
            int bx1 = 0;
            int bx2 = 0;
            int n = 0;
            complex v = 0;
            int i_ = 0;
            int i1_ = 0;

            n = i2-i1+1;
            if( n<=0 )
            {
                return;
            }
            
            //
            // Let A = L + D + U, where
            //  L is strictly lower triangular (main diagonal is zero)
            //  D is diagonal
            //  U is strictly upper triangular (main diagonal is zero)
            //
            // A*x = L*x + D*x + U*x
            //
            // Calculate D*x first
            //
            for(i=i1; i<=i2; i++)
            {
                y[i-i1+1] = a[i,i]*x[i-i1+1];
            }
            
            //
            // Add L*x + U*x
            //
            if( isupper )
            {
                for(i=i1; i<=i2-1; i++)
                {
                    
                    //
                    // Add L*x to the result
                    //
                    v = x[i-i1+1];
                    by1 = i-i1+2;
                    by2 = n;
                    ba1 = i+1;
                    ba2 = i2;
                    i1_ = (ba1) - (by1);
                    for(i_=by1; i_<=by2;i_++)
                    {
                        y[i_] = y[i_] + v*math.conj(a[i,i_+i1_]);
                    }
                    
                    //
                    // Add U*x to the result
                    //
                    bx1 = i-i1+2;
                    bx2 = n;
                    ba1 = i+1;
                    ba2 = i2;
                    i1_ = (ba1)-(bx1);
                    v = 0.0;
                    for(i_=bx1; i_<=bx2;i_++)
                    {
                        v += x[i_]*a[i,i_+i1_];
                    }
                    y[i-i1+1] = y[i-i1+1]+v;
                }
            }
            else
            {
                for(i=i1+1; i<=i2; i++)
                {
                    
                    //
                    // Add L*x to the result
                    //
                    bx1 = 1;
                    bx2 = i-i1;
                    ba1 = i1;
                    ba2 = i-1;
                    i1_ = (ba1)-(bx1);
                    v = 0.0;
                    for(i_=bx1; i_<=bx2;i_++)
                    {
                        v += x[i_]*a[i,i_+i1_];
                    }
                    y[i-i1+1] = y[i-i1+1]+v;
                    
                    //
                    // Add U*x to the result
                    //
                    v = x[i-i1+1];
                    by1 = 1;
                    by2 = i-i1;
                    ba1 = i1;
                    ba2 = i-1;
                    i1_ = (ba1) - (by1);
                    for(i_=by1; i_<=by2;i_++)
                    {
                        y[i_] = y[i_] + v*math.conj(a[i,i_+i1_]);
                    }
                }
            }
            for(i_=1; i_<=n;i_++)
            {
                y[i_] = alpha*y[i_];
            }
        }


        public static void hermitianrank2update(ref complex[,] a,
            bool isupper,
            int i1,
            int i2,
            complex[] x,
            complex[] y,
            ref complex[] t,
            complex alpha)
        {
            int i = 0;
            int tp1 = 0;
            int tp2 = 0;
            complex v = 0;
            int i_ = 0;
            int i1_ = 0;

            if( isupper )
            {
                for(i=i1; i<=i2; i++)
                {
                    tp1 = i+1-i1;
                    tp2 = i2-i1+1;
                    v = alpha*x[i+1-i1];
                    for(i_=tp1; i_<=tp2;i_++)
                    {
                        t[i_] = v*math.conj(y[i_]);
                    }
                    v = math.conj(alpha)*y[i+1-i1];
                    for(i_=tp1; i_<=tp2;i_++)
                    {
                        t[i_] = t[i_] + v*math.conj(x[i_]);
                    }
                    i1_ = (tp1) - (i);
                    for(i_=i; i_<=i2;i_++)
                    {
                        a[i,i_] = a[i,i_] + t[i_+i1_];
                    }
                }
            }
            else
            {
                for(i=i1; i<=i2; i++)
                {
                    tp1 = 1;
                    tp2 = i+1-i1;
                    v = alpha*x[i+1-i1];
                    for(i_=tp1; i_<=tp2;i_++)
                    {
                        t[i_] = v*math.conj(y[i_]);
                    }
                    v = math.conj(alpha)*y[i+1-i1];
                    for(i_=tp1; i_<=tp2;i_++)
                    {
                        t[i_] = t[i_] + v*math.conj(x[i_]);
                    }
                    i1_ = (tp1) - (i1);
                    for(i_=i1; i_<=i;i_++)
                    {
                        a[i,i_] = a[i,i_] + t[i_+i1_];
                    }
                }
            }
        }


    }
    public class reflections
    {
        /*************************************************************************
        Generation of an elementary reflection transformation

        The subroutine generates elementary reflection H of order N, so that, for
        a given X, the following equality holds true:

            ( X(1) )   ( Beta )
        H * (  ..  ) = (  0   )
            ( X(n) )   (  0   )

        where
                      ( V(1) )
        H = 1 - Tau * (  ..  ) * ( V(1), ..., V(n) )
                      ( V(n) )

        where the first component of vector V equals 1.

        Input parameters:
            X   -   vector. Array whose index ranges within [1..N].
            N   -   reflection order.

        Output parameters:
            X   -   components from 2 to N are replaced with vector V.
                    The first component is replaced with parameter Beta.
            Tau -   scalar value Tau. If X is a null vector, Tau equals 0,
                    otherwise 1 <= Tau <= 2.

        This subroutine is the modification of the DLARFG subroutines from
        the LAPACK library.

        MODIFICATIONS:
            24.12.2005 sign(Alpha) was replaced with an analogous to the Fortran SIGN code.

          -- LAPACK auxiliary routine (version 3.0) --
             Univ. of Tennessee, Univ. of California Berkeley, NAG Ltd.,
             Courant Institute, Argonne National Lab, and Rice University
             September 30, 1994
        *************************************************************************/
        public static void generatereflection(ref double[] x,
            int n,
            ref double tau)
        {
            int j = 0;
            double alpha = 0;
            double xnorm = 0;
            double v = 0;
            double beta = 0;
            double mx = 0;
            double s = 0;
            int i_ = 0;

            tau = 0;

            if( n<=1 )
            {
                tau = 0;
                return;
            }
            
            //
            // Scale if needed (to avoid overflow/underflow during intermediate
            // calculations).
            //
            mx = 0;
            for(j=1; j<=n; j++)
            {
                mx = Math.Max(Math.Abs(x[j]), mx);
            }
            s = 1;
            if( (double)(mx)!=(double)(0) )
            {
                if( (double)(mx)<=(double)(math.minrealnumber/math.machineepsilon) )
                {
                    s = math.minrealnumber/math.machineepsilon;
                    v = 1/s;
                    for(i_=1; i_<=n;i_++)
                    {
                        x[i_] = v*x[i_];
                    }
                    mx = mx*v;
                }
                else
                {
                    if( (double)(mx)>=(double)(math.maxrealnumber*math.machineepsilon) )
                    {
                        s = math.maxrealnumber*math.machineepsilon;
                        v = 1/s;
                        for(i_=1; i_<=n;i_++)
                        {
                            x[i_] = v*x[i_];
                        }
                        mx = mx*v;
                    }
                }
            }
            
            //
            // XNORM = DNRM2( N-1, X, INCX )
            //
            alpha = x[1];
            xnorm = 0;
            if( (double)(mx)!=(double)(0) )
            {
                for(j=2; j<=n; j++)
                {
                    xnorm = xnorm+math.sqr(x[j]/mx);
                }
                xnorm = Math.Sqrt(xnorm)*mx;
            }
            if( (double)(xnorm)==(double)(0) )
            {
                
                //
                // H  =  I
                //
                tau = 0;
                x[1] = x[1]*s;
                return;
            }
            
            //
            // general case
            //
            mx = Math.Max(Math.Abs(alpha), Math.Abs(xnorm));
            beta = -(mx*Math.Sqrt(math.sqr(alpha/mx)+math.sqr(xnorm/mx)));
            if( (double)(alpha)<(double)(0) )
            {
                beta = -beta;
            }
            tau = (beta-alpha)/beta;
            v = 1/(alpha-beta);
            for(i_=2; i_<=n;i_++)
            {
                x[i_] = v*x[i_];
            }
            x[1] = beta;
            
            //
            // Scale back outputs
            //
            x[1] = x[1]*s;
        }


        /*************************************************************************
        Application of an elementary reflection to a rectangular matrix of size MxN

        The algorithm pre-multiplies the matrix by an elementary reflection transformation
        which is given by column V and scalar Tau (see the description of the
        GenerateReflection procedure). Not the whole matrix but only a part of it
        is transformed (rows from M1 to M2, columns from N1 to N2). Only the elements
        of this submatrix are changed.

        Input parameters:
            C       -   matrix to be transformed.
            Tau     -   scalar defining the transformation.
            V       -   column defining the transformation.
                        Array whose index ranges within [1..M2-M1+1].
            M1, M2  -   range of rows to be transformed.
            N1, N2  -   range of columns to be transformed.
            WORK    -   working array whose indexes goes from N1 to N2.

        Output parameters:
            C       -   the result of multiplying the input matrix C by the
                        transformation matrix which is given by Tau and V.
                        If N1>N2 or M1>M2, C is not modified.

          -- LAPACK auxiliary routine (version 3.0) --
             Univ. of Tennessee, Univ. of California Berkeley, NAG Ltd.,
             Courant Institute, Argonne National Lab, and Rice University
             September 30, 1994
        *************************************************************************/
        public static void applyreflectionfromtheleft(ref double[,] c,
            double tau,
            double[] v,
            int m1,
            int m2,
            int n1,
            int n2,
            ref double[] work)
        {
            double t = 0;
            int i = 0;
            int vm = 0;
            int i_ = 0;

            if( ((double)(tau)==(double)(0) | n1>n2) | m1>m2 )
            {
                return;
            }
            
            //
            // w := C' * v
            //
            vm = m2-m1+1;
            for(i=n1; i<=n2; i++)
            {
                work[i] = 0;
            }
            for(i=m1; i<=m2; i++)
            {
                t = v[i+1-m1];
                for(i_=n1; i_<=n2;i_++)
                {
                    work[i_] = work[i_] + t*c[i,i_];
                }
            }
            
            //
            // C := C - tau * v * w'
            //
            for(i=m1; i<=m2; i++)
            {
                t = v[i-m1+1]*tau;
                for(i_=n1; i_<=n2;i_++)
                {
                    c[i,i_] = c[i,i_] - t*work[i_];
                }
            }
        }


        /*************************************************************************
        Application of an elementary reflection to a rectangular matrix of size MxN

        The algorithm post-multiplies the matrix by an elementary reflection transformation
        which is given by column V and scalar Tau (see the description of the
        GenerateReflection procedure). Not the whole matrix but only a part of it
        is transformed (rows from M1 to M2, columns from N1 to N2). Only the
        elements of this submatrix are changed.

        Input parameters:
            C       -   matrix to be transformed.
            Tau     -   scalar defining the transformation.
            V       -   column defining the transformation.
                        Array whose index ranges within [1..N2-N1+1].
            M1, M2  -   range of rows to be transformed.
            N1, N2  -   range of columns to be transformed.
            WORK    -   working array whose indexes goes from M1 to M2.

        Output parameters:
            C       -   the result of multiplying the input matrix C by the
                        transformation matrix which is given by Tau and V.
                        If N1>N2 or M1>M2, C is not modified.

          -- LAPACK auxiliary routine (version 3.0) --
             Univ. of Tennessee, Univ. of California Berkeley, NAG Ltd.,
             Courant Institute, Argonne National Lab, and Rice University
             September 30, 1994
        *************************************************************************/
        public static void applyreflectionfromtheright(ref double[,] c,
            double tau,
            double[] v,
            int m1,
            int m2,
            int n1,
            int n2,
            ref double[] work)
        {
            double t = 0;
            int i = 0;
            int vm = 0;
            int i_ = 0;
            int i1_ = 0;

            if( ((double)(tau)==(double)(0) | n1>n2) | m1>m2 )
            {
                return;
            }
            vm = n2-n1+1;
            for(i=m1; i<=m2; i++)
            {
                i1_ = (1)-(n1);
                t = 0.0;
                for(i_=n1; i_<=n2;i_++)
                {
                    t += c[i,i_]*v[i_+i1_];
                }
                t = t*tau;
                i1_ = (1) - (n1);
                for(i_=n1; i_<=n2;i_++)
                {
                    c[i,i_] = c[i,i_] - t*v[i_+i1_];
                }
            }
        }


    }
    public class creflections
    {
        /*************************************************************************
        Generation of an elementary complex reflection transformation

        The subroutine generates elementary complex reflection H of  order  N,  so
        that, for a given X, the following equality holds true:

             ( X(1) )   ( Beta )
        H' * (  ..  ) = (  0   ),   H'*H = I,   Beta is a real number
             ( X(n) )   (  0   )

        where

                      ( V(1) )
        H = 1 - Tau * (  ..  ) * ( conj(V(1)), ..., conj(V(n)) )
                      ( V(n) )

        where the first component of vector V equals 1.

        Input parameters:
            X   -   vector. Array with elements [1..N].
            N   -   reflection order.

        Output parameters:
            X   -   components from 2 to N are replaced by vector V.
                    The first component is replaced with parameter Beta.
            Tau -   scalar value Tau.

        This subroutine is the modification of CLARFG subroutines  from the LAPACK
        library. It has similar functionality except for the fact that it  doesn’t
        handle errors when intermediate results cause an overflow.

          -- LAPACK auxiliary routine (version 3.0) --
             Univ. of Tennessee, Univ. of California Berkeley, NAG Ltd.,
             Courant Institute, Argonne National Lab, and Rice University
             September 30, 1994
        *************************************************************************/
        public static void complexgeneratereflection(ref complex[] x,
            int n,
            ref complex tau)
        {
            int j = 0;
            complex alpha = 0;
            double alphi = 0;
            double alphr = 0;
            double beta = 0;
            double xnorm = 0;
            double mx = 0;
            complex t = 0;
            double s = 0;
            complex v = 0;
            int i_ = 0;

            tau = 0;

            if( n<=0 )
            {
                tau = 0;
                return;
            }
            
            //
            // Scale if needed (to avoid overflow/underflow during intermediate
            // calculations).
            //
            mx = 0;
            for(j=1; j<=n; j++)
            {
                mx = Math.Max(math.abscomplex(x[j]), mx);
            }
            s = 1;
            if( (double)(mx)!=(double)(0) )
            {
                if( (double)(mx)<(double)(1) )
                {
                    s = Math.Sqrt(math.minrealnumber);
                    v = 1/s;
                    for(i_=1; i_<=n;i_++)
                    {
                        x[i_] = v*x[i_];
                    }
                }
                else
                {
                    s = Math.Sqrt(math.maxrealnumber);
                    v = 1/s;
                    for(i_=1; i_<=n;i_++)
                    {
                        x[i_] = v*x[i_];
                    }
                }
            }
            
            //
            // calculate
            //
            alpha = x[1];
            mx = 0;
            for(j=2; j<=n; j++)
            {
                mx = Math.Max(math.abscomplex(x[j]), mx);
            }
            xnorm = 0;
            if( (double)(mx)!=(double)(0) )
            {
                for(j=2; j<=n; j++)
                {
                    t = x[j]/mx;
                    xnorm = xnorm+(t*math.conj(t)).x;
                }
                xnorm = Math.Sqrt(xnorm)*mx;
            }
            alphr = alpha.x;
            alphi = alpha.y;
            if( (double)(xnorm)==(double)(0) & (double)(alphi)==(double)(0) )
            {
                tau = 0;
                x[1] = x[1]*s;
                return;
            }
            mx = Math.Max(Math.Abs(alphr), Math.Abs(alphi));
            mx = Math.Max(mx, Math.Abs(xnorm));
            beta = -(mx*Math.Sqrt(math.sqr(alphr/mx)+math.sqr(alphi/mx)+math.sqr(xnorm/mx)));
            if( (double)(alphr)<(double)(0) )
            {
                beta = -beta;
            }
            tau.x = (beta-alphr)/beta;
            tau.y = -(alphi/beta);
            alpha = 1/(alpha-beta);
            if( n>1 )
            {
                for(i_=2; i_<=n;i_++)
                {
                    x[i_] = alpha*x[i_];
                }
            }
            alpha = beta;
            x[1] = alpha;
            
            //
            // Scale back
            //
            x[1] = x[1]*s;
        }


        /*************************************************************************
        Application of an elementary reflection to a rectangular matrix of size MxN

        The  algorithm  pre-multiplies  the  matrix  by  an  elementary reflection
        transformation  which  is  given  by  column  V  and  scalar  Tau (see the
        description of the GenerateReflection). Not the whole matrix  but  only  a
        part of it is transformed (rows from M1 to M2, columns from N1 to N2). Only
        the elements of this submatrix are changed.

        Note: the matrix is multiplied by H, not by H'.   If  it  is  required  to
        multiply the matrix by H', it is necessary to pass Conj(Tau) instead of Tau.

        Input parameters:
            C       -   matrix to be transformed.
            Tau     -   scalar defining transformation.
            V       -   column defining transformation.
                        Array whose index ranges within [1..M2-M1+1]
            M1, M2  -   range of rows to be transformed.
            N1, N2  -   range of columns to be transformed.
            WORK    -   working array whose index goes from N1 to N2.

        Output parameters:
            C       -   the result of multiplying the input matrix C by the
                        transformation matrix which is given by Tau and V.
                        If N1>N2 or M1>M2, C is not modified.

          -- LAPACK auxiliary routine (version 3.0) --
             Univ. of Tennessee, Univ. of California Berkeley, NAG Ltd.,
             Courant Institute, Argonne National Lab, and Rice University
             September 30, 1994
        *************************************************************************/
        public static void complexapplyreflectionfromtheleft(ref complex[,] c,
            complex tau,
            complex[] v,
            int m1,
            int m2,
            int n1,
            int n2,
            ref complex[] work)
        {
            complex t = 0;
            int i = 0;
            int vm = 0;
            int i_ = 0;

            if( (tau==0 | n1>n2) | m1>m2 )
            {
                return;
            }
            
            //
            // w := C^T * conj(v)
            //
            vm = m2-m1+1;
            for(i=n1; i<=n2; i++)
            {
                work[i] = 0;
            }
            for(i=m1; i<=m2; i++)
            {
                t = math.conj(v[i+1-m1]);
                for(i_=n1; i_<=n2;i_++)
                {
                    work[i_] = work[i_] + t*c[i,i_];
                }
            }
            
            //
            // C := C - tau * v * w^T
            //
            for(i=m1; i<=m2; i++)
            {
                t = v[i-m1+1]*tau;
                for(i_=n1; i_<=n2;i_++)
                {
                    c[i,i_] = c[i,i_] - t*work[i_];
                }
            }
        }


        /*************************************************************************
        Application of an elementary reflection to a rectangular matrix of size MxN

        The  algorithm  post-multiplies  the  matrix  by  an elementary reflection
        transformation  which  is  given  by  column  V  and  scalar  Tau (see the
        description  of  the  GenerateReflection). Not the whole matrix but only a
        part  of  it  is  transformed (rows from M1 to M2, columns from N1 to N2).
        Only the elements of this submatrix are changed.

        Input parameters:
            C       -   matrix to be transformed.
            Tau     -   scalar defining transformation.
            V       -   column defining transformation.
                        Array whose index ranges within [1..N2-N1+1]
            M1, M2  -   range of rows to be transformed.
            N1, N2  -   range of columns to be transformed.
            WORK    -   working array whose index goes from M1 to M2.

        Output parameters:
            C       -   the result of multiplying the input matrix C by the
                        transformation matrix which is given by Tau and V.
                        If N1>N2 or M1>M2, C is not modified.

          -- LAPACK auxiliary routine (version 3.0) --
             Univ. of Tennessee, Univ. of California Berkeley, NAG Ltd.,
             Courant Institute, Argonne National Lab, and Rice University
             September 30, 1994
        *************************************************************************/
        public static void complexapplyreflectionfromtheright(ref complex[,] c,
            complex tau,
            ref complex[] v,
            int m1,
            int m2,
            int n1,
            int n2,
            ref complex[] work)
        {
            complex t = 0;
            int i = 0;
            int vm = 0;
            int i_ = 0;
            int i1_ = 0;

            if( (tau==0 | n1>n2) | m1>m2 )
            {
                return;
            }
            
            //
            // w := C * v
            //
            vm = n2-n1+1;
            for(i=m1; i<=m2; i++)
            {
                i1_ = (1)-(n1);
                t = 0.0;
                for(i_=n1; i_<=n2;i_++)
                {
                    t += c[i,i_]*v[i_+i1_];
                }
                work[i] = t;
            }
            
            //
            // C := C - w * conj(v^T)
            //
            for(i_=1; i_<=vm;i_++)
            {
                v[i_] = math.conj(v[i_]);
            }
            for(i=m1; i<=m2; i++)
            {
                t = work[i]*tau;
                i1_ = (1) - (n1);
                for(i_=n1; i_<=n2;i_++)
                {
                    c[i,i_] = c[i,i_] - t*v[i_+i1_];
                }
            }
            for(i_=1; i_<=vm;i_++)
            {
                v[i_] = math.conj(v[i_]);
            }
        }


    }
    public class sblas
    {
        public static void symmetricmatrixvectormultiply(double[,] a,
            bool isupper,
            int i1,
            int i2,
            double[] x,
            double alpha,
            ref double[] y)
        {
            int i = 0;
            int ba1 = 0;
            int ba2 = 0;
            int by1 = 0;
            int by2 = 0;
            int bx1 = 0;
            int bx2 = 0;
            int n = 0;
            double v = 0;
            int i_ = 0;
            int i1_ = 0;

            n = i2-i1+1;
            if( n<=0 )
            {
                return;
            }
            
            //
            // Let A = L + D + U, where
            //  L is strictly lower triangular (main diagonal is zero)
            //  D is diagonal
            //  U is strictly upper triangular (main diagonal is zero)
            //
            // A*x = L*x + D*x + U*x
            //
            // Calculate D*x first
            //
            for(i=i1; i<=i2; i++)
            {
                y[i-i1+1] = a[i,i]*x[i-i1+1];
            }
            
            //
            // Add L*x + U*x
            //
            if( isupper )
            {
                for(i=i1; i<=i2-1; i++)
                {
                    
                    //
                    // Add L*x to the result
                    //
                    v = x[i-i1+1];
                    by1 = i-i1+2;
                    by2 = n;
                    ba1 = i+1;
                    ba2 = i2;
                    i1_ = (ba1) - (by1);
                    for(i_=by1; i_<=by2;i_++)
                    {
                        y[i_] = y[i_] + v*a[i,i_+i1_];
                    }
                    
                    //
                    // Add U*x to the result
                    //
                    bx1 = i-i1+2;
                    bx2 = n;
                    ba1 = i+1;
                    ba2 = i2;
                    i1_ = (ba1)-(bx1);
                    v = 0.0;
                    for(i_=bx1; i_<=bx2;i_++)
                    {
                        v += x[i_]*a[i,i_+i1_];
                    }
                    y[i-i1+1] = y[i-i1+1]+v;
                }
            }
            else
            {
                for(i=i1+1; i<=i2; i++)
                {
                    
                    //
                    // Add L*x to the result
                    //
                    bx1 = 1;
                    bx2 = i-i1;
                    ba1 = i1;
                    ba2 = i-1;
                    i1_ = (ba1)-(bx1);
                    v = 0.0;
                    for(i_=bx1; i_<=bx2;i_++)
                    {
                        v += x[i_]*a[i,i_+i1_];
                    }
                    y[i-i1+1] = y[i-i1+1]+v;
                    
                    //
                    // Add U*x to the result
                    //
                    v = x[i-i1+1];
                    by1 = 1;
                    by2 = i-i1;
                    ba1 = i1;
                    ba2 = i-1;
                    i1_ = (ba1) - (by1);
                    for(i_=by1; i_<=by2;i_++)
                    {
                        y[i_] = y[i_] + v*a[i,i_+i1_];
                    }
                }
            }
            for(i_=1; i_<=n;i_++)
            {
                y[i_] = alpha*y[i_];
            }
        }


        public static void symmetricrank2update(ref double[,] a,
            bool isupper,
            int i1,
            int i2,
            double[] x,
            double[] y,
            ref double[] t,
            double alpha)
        {
            int i = 0;
            int tp1 = 0;
            int tp2 = 0;
            double v = 0;
            int i_ = 0;
            int i1_ = 0;

            if( isupper )
            {
                for(i=i1; i<=i2; i++)
                {
                    tp1 = i+1-i1;
                    tp2 = i2-i1+1;
                    v = x[i+1-i1];
                    for(i_=tp1; i_<=tp2;i_++)
                    {
                        t[i_] = v*y[i_];
                    }
                    v = y[i+1-i1];
                    for(i_=tp1; i_<=tp2;i_++)
                    {
                        t[i_] = t[i_] + v*x[i_];
                    }
                    for(i_=tp1; i_<=tp2;i_++)
                    {
                        t[i_] = alpha*t[i_];
                    }
                    i1_ = (tp1) - (i);
                    for(i_=i; i_<=i2;i_++)
                    {
                        a[i,i_] = a[i,i_] + t[i_+i1_];
                    }
                }
            }
            else
            {
                for(i=i1; i<=i2; i++)
                {
                    tp1 = 1;
                    tp2 = i+1-i1;
                    v = x[i+1-i1];
                    for(i_=tp1; i_<=tp2;i_++)
                    {
                        t[i_] = v*y[i_];
                    }
                    v = y[i+1-i1];
                    for(i_=tp1; i_<=tp2;i_++)
                    {
                        t[i_] = t[i_] + v*x[i_];
                    }
                    for(i_=tp1; i_<=tp2;i_++)
                    {
                        t[i_] = alpha*t[i_];
                    }
                    i1_ = (tp1) - (i1);
                    for(i_=i1; i_<=i;i_++)
                    {
                        a[i,i_] = a[i,i_] + t[i_+i1_];
                    }
                }
            }
        }


    }
    public class rotations
    {
        /*************************************************************************
        Application of a sequence of  elementary rotations to a matrix

        The algorithm pre-multiplies the matrix by a sequence of rotation
        transformations which is given by arrays C and S. Depending on the value
        of the IsForward parameter either 1 and 2, 3 and 4 and so on (if IsForward=true)
        rows are rotated, or the rows N and N-1, N-2 and N-3 and so on, are rotated.

        Not the whole matrix but only a part of it is transformed (rows from M1 to
        M2, columns from N1 to N2). Only the elements of this submatrix are changed.

        Input parameters:
            IsForward   -   the sequence of the rotation application.
            M1,M2       -   the range of rows to be transformed.
            N1, N2      -   the range of columns to be transformed.
            C,S         -   transformation coefficients.
                            Array whose index ranges within [1..M2-M1].
            A           -   processed matrix.
            WORK        -   working array whose index ranges within [N1..N2].

        Output parameters:
            A           -   transformed matrix.

        Utility subroutine.
        *************************************************************************/
        public static void applyrotationsfromtheleft(bool isforward,
            int m1,
            int m2,
            int n1,
            int n2,
            double[] c,
            double[] s,
            ref double[,] a,
            ref double[] work)
        {
            int j = 0;
            int jp1 = 0;
            double ctemp = 0;
            double stemp = 0;
            double temp = 0;
            int i_ = 0;

            if( m1>m2 | n1>n2 )
            {
                return;
            }
            
            //
            // Form  P * A
            //
            if( isforward )
            {
                if( n1!=n2 )
                {
                    
                    //
                    // Common case: N1<>N2
                    //
                    for(j=m1; j<=m2-1; j++)
                    {
                        ctemp = c[j-m1+1];
                        stemp = s[j-m1+1];
                        if( (double)(ctemp)!=(double)(1) | (double)(stemp)!=(double)(0) )
                        {
                            jp1 = j+1;
                            for(i_=n1; i_<=n2;i_++)
                            {
                                work[i_] = ctemp*a[jp1,i_];
                            }
                            for(i_=n1; i_<=n2;i_++)
                            {
                                work[i_] = work[i_] - stemp*a[j,i_];
                            }
                            for(i_=n1; i_<=n2;i_++)
                            {
                                a[j,i_] = ctemp*a[j,i_];
                            }
                            for(i_=n1; i_<=n2;i_++)
                            {
                                a[j,i_] = a[j,i_] + stemp*a[jp1,i_];
                            }
                            for(i_=n1; i_<=n2;i_++)
                            {
                                a[jp1,i_] = work[i_];
                            }
                        }
                    }
                }
                else
                {
                    
                    //
                    // Special case: N1=N2
                    //
                    for(j=m1; j<=m2-1; j++)
                    {
                        ctemp = c[j-m1+1];
                        stemp = s[j-m1+1];
                        if( (double)(ctemp)!=(double)(1) | (double)(stemp)!=(double)(0) )
                        {
                            temp = a[j+1,n1];
                            a[j+1,n1] = ctemp*temp-stemp*a[j,n1];
                            a[j,n1] = stemp*temp+ctemp*a[j,n1];
                        }
                    }
                }
            }
            else
            {
                if( n1!=n2 )
                {
                    
                    //
                    // Common case: N1<>N2
                    //
                    for(j=m2-1; j>=m1; j--)
                    {
                        ctemp = c[j-m1+1];
                        stemp = s[j-m1+1];
                        if( (double)(ctemp)!=(double)(1) | (double)(stemp)!=(double)(0) )
                        {
                            jp1 = j+1;
                            for(i_=n1; i_<=n2;i_++)
                            {
                                work[i_] = ctemp*a[jp1,i_];
                            }
                            for(i_=n1; i_<=n2;i_++)
                            {
                                work[i_] = work[i_] - stemp*a[j,i_];
                            }
                            for(i_=n1; i_<=n2;i_++)
                            {
                                a[j,i_] = ctemp*a[j,i_];
                            }
                            for(i_=n1; i_<=n2;i_++)
                            {
                                a[j,i_] = a[j,i_] + stemp*a[jp1,i_];
                            }
                            for(i_=n1; i_<=n2;i_++)
                            {
                                a[jp1,i_] = work[i_];
                            }
                        }
                    }
                }
                else
                {
                    
                    //
                    // Special case: N1=N2
                    //
                    for(j=m2-1; j>=m1; j--)
                    {
                        ctemp = c[j-m1+1];
                        stemp = s[j-m1+1];
                        if( (double)(ctemp)!=(double)(1) | (double)(stemp)!=(double)(0) )
                        {
                            temp = a[j+1,n1];
                            a[j+1,n1] = ctemp*temp-stemp*a[j,n1];
                            a[j,n1] = stemp*temp+ctemp*a[j,n1];
                        }
                    }
                }
            }
        }


        /*************************************************************************
        Application of a sequence of  elementary rotations to a matrix

        The algorithm post-multiplies the matrix by a sequence of rotation
        transformations which is given by arrays C and S. Depending on the value
        of the IsForward parameter either 1 and 2, 3 and 4 and so on (if IsForward=true)
        rows are rotated, or the rows N and N-1, N-2 and N-3 and so on are rotated.

        Not the whole matrix but only a part of it is transformed (rows from M1
        to M2, columns from N1 to N2). Only the elements of this submatrix are changed.

        Input parameters:
            IsForward   -   the sequence of the rotation application.
            M1,M2       -   the range of rows to be transformed.
            N1, N2      -   the range of columns to be transformed.
            C,S         -   transformation coefficients.
                            Array whose index ranges within [1..N2-N1].
            A           -   processed matrix.
            WORK        -   working array whose index ranges within [M1..M2].

        Output parameters:
            A           -   transformed matrix.

        Utility subroutine.
        *************************************************************************/
        public static void applyrotationsfromtheright(bool isforward,
            int m1,
            int m2,
            int n1,
            int n2,
            double[] c,
            double[] s,
            ref double[,] a,
            ref double[] work)
        {
            int j = 0;
            int jp1 = 0;
            double ctemp = 0;
            double stemp = 0;
            double temp = 0;
            int i_ = 0;

            
            //
            // Form A * P'
            //
            if( isforward )
            {
                if( m1!=m2 )
                {
                    
                    //
                    // Common case: M1<>M2
                    //
                    for(j=n1; j<=n2-1; j++)
                    {
                        ctemp = c[j-n1+1];
                        stemp = s[j-n1+1];
                        if( (double)(ctemp)!=(double)(1) | (double)(stemp)!=(double)(0) )
                        {
                            jp1 = j+1;
                            for(i_=m1; i_<=m2;i_++)
                            {
                                work[i_] = ctemp*a[i_,jp1];
                            }
                            for(i_=m1; i_<=m2;i_++)
                            {
                                work[i_] = work[i_] - stemp*a[i_,j];
                            }
                            for(i_=m1; i_<=m2;i_++)
                            {
                                a[i_,j] = ctemp*a[i_,j];
                            }
                            for(i_=m1; i_<=m2;i_++)
                            {
                                a[i_,j] = a[i_,j] + stemp*a[i_,jp1];
                            }
                            for(i_=m1; i_<=m2;i_++)
                            {
                                a[i_,jp1] = work[i_];
                            }
                        }
                    }
                }
                else
                {
                    
                    //
                    // Special case: M1=M2
                    //
                    for(j=n1; j<=n2-1; j++)
                    {
                        ctemp = c[j-n1+1];
                        stemp = s[j-n1+1];
                        if( (double)(ctemp)!=(double)(1) | (double)(stemp)!=(double)(0) )
                        {
                            temp = a[m1,j+1];
                            a[m1,j+1] = ctemp*temp-stemp*a[m1,j];
                            a[m1,j] = stemp*temp+ctemp*a[m1,j];
                        }
                    }
                }
            }
            else
            {
                if( m1!=m2 )
                {
                    
                    //
                    // Common case: M1<>M2
                    //
                    for(j=n2-1; j>=n1; j--)
                    {
                        ctemp = c[j-n1+1];
                        stemp = s[j-n1+1];
                        if( (double)(ctemp)!=(double)(1) | (double)(stemp)!=(double)(0) )
                        {
                            jp1 = j+1;
                            for(i_=m1; i_<=m2;i_++)
                            {
                                work[i_] = ctemp*a[i_,jp1];
                            }
                            for(i_=m1; i_<=m2;i_++)
                            {
                                work[i_] = work[i_] - stemp*a[i_,j];
                            }
                            for(i_=m1; i_<=m2;i_++)
                            {
                                a[i_,j] = ctemp*a[i_,j];
                            }
                            for(i_=m1; i_<=m2;i_++)
                            {
                                a[i_,j] = a[i_,j] + stemp*a[i_,jp1];
                            }
                            for(i_=m1; i_<=m2;i_++)
                            {
                                a[i_,jp1] = work[i_];
                            }
                        }
                    }
                }
                else
                {
                    
                    //
                    // Special case: M1=M2
                    //
                    for(j=n2-1; j>=n1; j--)
                    {
                        ctemp = c[j-n1+1];
                        stemp = s[j-n1+1];
                        if( (double)(ctemp)!=(double)(1) | (double)(stemp)!=(double)(0) )
                        {
                            temp = a[m1,j+1];
                            a[m1,j+1] = ctemp*temp-stemp*a[m1,j];
                            a[m1,j] = stemp*temp+ctemp*a[m1,j];
                        }
                    }
                }
            }
        }


        /*************************************************************************
        The subroutine generates the elementary rotation, so that:

        [  CS  SN  ]  .  [ F ]  =  [ R ]
        [ -SN  CS  ]     [ G ]     [ 0 ]

        CS**2 + SN**2 = 1
        *************************************************************************/
        public static void generaterotation(double f,
            double g,
            ref double cs,
            ref double sn,
            ref double r)
        {
            double f1 = 0;
            double g1 = 0;

            cs = 0;
            sn = 0;
            r = 0;

            if( (double)(g)==(double)(0) )
            {
                cs = 1;
                sn = 0;
                r = f;
            }
            else
            {
                if( (double)(f)==(double)(0) )
                {
                    cs = 0;
                    sn = 1;
                    r = g;
                }
                else
                {
                    f1 = f;
                    g1 = g;
                    if( (double)(Math.Abs(f1))>(double)(Math.Abs(g1)) )
                    {
                        r = Math.Abs(f1)*Math.Sqrt(1+math.sqr(g1/f1));
                    }
                    else
                    {
                        r = Math.Abs(g1)*Math.Sqrt(1+math.sqr(f1/g1));
                    }
                    cs = f1/r;
                    sn = g1/r;
                    if( (double)(Math.Abs(f))>(double)(Math.Abs(g)) & (double)(cs)<(double)(0) )
                    {
                        cs = -cs;
                        sn = -sn;
                        r = -r;
                    }
                }
            }
        }


    }
    public class hsschur
    {
        /*************************************************************************
        Subroutine performing  the  Schur  decomposition  of  a  matrix  in  upper
        Hessenberg form using the QR algorithm with multiple shifts.

        The  source matrix  H  is  represented as  S'*H*S = T, where H - matrix in
        upper Hessenberg form,  S - orthogonal matrix (Schur vectors),   T - upper
        quasi-triangular matrix (with blocks of sizes  1x1  and  2x2  on  the main
        diagonal).

        Input parameters:
            H   -   matrix to be decomposed.
                    Array whose indexes range within [1..N, 1..N].
            N   -   size of H, N>=0.


        Output parameters:
            H   –   contains the matrix T.
                    Array whose indexes range within [1..N, 1..N].
                    All elements below the blocks on the main diagonal are equal
                    to 0.
            S   -   contains Schur vectors.
                    Array whose indexes range within [1..N, 1..N].

        Note 1:
            The block structure of matrix T could be easily recognized: since  all
            the elements  below  the blocks are zeros, the elements a[i+1,i] which
            are equal to 0 show the block border.

        Note 2:
            the algorithm  performance  depends  on  the  value  of  the  internal
            parameter NS of InternalSchurDecomposition  subroutine  which  defines
            the number of shifts in the QR algorithm (analog of  the  block  width
            in block matrix algorithms in linear algebra). If you require  maximum
            performance  on  your  machine,  it  is  recommended  to  adjust  this
            parameter manually.

        Result:
            True, if the algorithm has converged and the parameters H and S contain
                the result.
            False, if the algorithm has not converged.

        Algorithm implemented on the basis of subroutine DHSEQR (LAPACK 3.0 library).
        *************************************************************************/
        public static bool upperhessenbergschurdecomposition(ref double[,] h,
            int n,
            ref double[,] s)
        {
            bool result = new bool();
            double[] wi = new double[0];
            double[] wr = new double[0];
            int info = 0;

            s = new double[0,0];

            internalschurdecomposition(ref h, n, 1, 2, ref wr, ref wi, ref s, ref info);
            result = info==0;
            return result;
        }


        public static void internalschurdecomposition(ref double[,] h,
            int n,
            int tneeded,
            int zneeded,
            ref double[] wr,
            ref double[] wi,
            ref double[,] z,
            ref int info)
        {
            double[] work = new double[0];
            int i = 0;
            int i1 = 0;
            int i2 = 0;
            int ierr = 0;
            int ii = 0;
            int itemp = 0;
            int itn = 0;
            int its = 0;
            int j = 0;
            int k = 0;
            int l = 0;
            int maxb = 0;
            int nr = 0;
            int ns = 0;
            int nv = 0;
            double absw = 0;
            double ovfl = 0;
            double smlnum = 0;
            double tau = 0;
            double temp = 0;
            double tst1 = 0;
            double ulp = 0;
            double unfl = 0;
            double[,] s = new double[0,0];
            double[] v = new double[0];
            double[] vv = new double[0];
            double[] workc1 = new double[0];
            double[] works1 = new double[0];
            double[] workv3 = new double[0];
            double[] tmpwr = new double[0];
            double[] tmpwi = new double[0];
            bool initz = new bool();
            bool wantt = new bool();
            bool wantz = new bool();
            double cnst = 0;
            bool failflag = new bool();
            int p1 = 0;
            int p2 = 0;
            double vt = 0;
            int i_ = 0;
            int i1_ = 0;

            wr = new double[0];
            wi = new double[0];
            info = 0;

            
            //
            // Set the order of the multi-shift QR algorithm to be used.
            // If you want to tune algorithm, change this values
            //
            ns = 12;
            maxb = 50;
            
            //
            // Now 2 < NS <= MAXB < NH.
            //
            maxb = Math.Max(3, maxb);
            ns = Math.Min(maxb, ns);
            
            //
            // Initialize
            //
            cnst = 1.5;
            work = new double[Math.Max(n, 1)+1];
            s = new double[ns+1, ns+1];
            v = new double[ns+1+1];
            vv = new double[ns+1+1];
            wr = new double[Math.Max(n, 1)+1];
            wi = new double[Math.Max(n, 1)+1];
            workc1 = new double[1+1];
            works1 = new double[1+1];
            workv3 = new double[3+1];
            tmpwr = new double[Math.Max(n, 1)+1];
            tmpwi = new double[Math.Max(n, 1)+1];
            ap.assert(n>=0, "InternalSchurDecomposition: incorrect N!");
            ap.assert(tneeded==0 | tneeded==1, "InternalSchurDecomposition: incorrect TNeeded!");
            ap.assert((zneeded==0 | zneeded==1) | zneeded==2, "InternalSchurDecomposition: incorrect ZNeeded!");
            wantt = tneeded==1;
            initz = zneeded==2;
            wantz = zneeded!=0;
            info = 0;
            
            //
            // Initialize Z, if necessary
            //
            if( initz )
            {
                z = new double[n+1, n+1];
                for(i=1; i<=n; i++)
                {
                    for(j=1; j<=n; j++)
                    {
                        if( i==j )
                        {
                            z[i,j] = 1;
                        }
                        else
                        {
                            z[i,j] = 0;
                        }
                    }
                }
            }
            
            //
            // Quick return if possible
            //
            if( n==0 )
            {
                return;
            }
            if( n==1 )
            {
                wr[1] = h[1,1];
                wi[1] = 0;
                return;
            }
            
            //
            // Set rows and columns 1 to N to zero below the first
            // subdiagonal.
            //
            for(j=1; j<=n-2; j++)
            {
                for(i=j+2; i<=n; i++)
                {
                    h[i,j] = 0;
                }
            }
            
            //
            // Test if N is sufficiently small
            //
            if( (ns<=2 | ns>n) | maxb>=n )
            {
                
                //
                // Use the standard double-shift algorithm
                //
                internalauxschur(wantt, wantz, n, 1, n, ref h, ref wr, ref wi, 1, n, ref z, ref work, ref workv3, ref workc1, ref works1, ref info);
                
                //
                // fill entries under diagonal blocks of T with zeros
                //
                if( wantt )
                {
                    j = 1;
                    while( j<=n )
                    {
                        if( (double)(wi[j])==(double)(0) )
                        {
                            for(i=j+1; i<=n; i++)
                            {
                                h[i,j] = 0;
                            }
                            j = j+1;
                        }
                        else
                        {
                            for(i=j+2; i<=n; i++)
                            {
                                h[i,j] = 0;
                                h[i,j+1] = 0;
                            }
                            j = j+2;
                        }
                    }
                }
                return;
            }
            unfl = math.minrealnumber;
            ovfl = 1/unfl;
            ulp = 2*math.machineepsilon;
            smlnum = unfl*(n/ulp);
            
            //
            // I1 and I2 are the indices of the first row and last column of H
            // to which transformations must be applied. If eigenvalues only are
            // being computed, I1 and I2 are set inside the main loop.
            //
            i1 = 1;
            i2 = n;
            
            //
            // ITN is the total number of multiple-shift QR iterations allowed.
            //
            itn = 30*n;
            
            //
            // The main loop begins here. I is the loop index and decreases from
            // IHI to ILO in steps of at most MAXB. Each iteration of the loop
            // works with the active submatrix in rows and columns L to I.
            // Eigenvalues I+1 to IHI have already converged. Either L = ILO or
            // H(L,L-1) is negligible so that the matrix splits.
            //
            i = n;
            while( true )
            {
                l = 1;
                if( i<1 )
                {
                    
                    //
                    // fill entries under diagonal blocks of T with zeros
                    //
                    if( wantt )
                    {
                        j = 1;
                        while( j<=n )
                        {
                            if( (double)(wi[j])==(double)(0) )
                            {
                                for(i=j+1; i<=n; i++)
                                {
                                    h[i,j] = 0;
                                }
                                j = j+1;
                            }
                            else
                            {
                                for(i=j+2; i<=n; i++)
                                {
                                    h[i,j] = 0;
                                    h[i,j+1] = 0;
                                }
                                j = j+2;
                            }
                        }
                    }
                    
                    //
                    // Exit
                    //
                    return;
                }
                
                //
                // Perform multiple-shift QR iterations on rows and columns ILO to I
                // until a submatrix of order at most MAXB splits off at the bottom
                // because a subdiagonal element has become negligible.
                //
                failflag = true;
                for(its=0; its<=itn; its++)
                {
                    
                    //
                    // Look for a single small subdiagonal element.
                    //
                    for(k=i; k>=l+1; k--)
                    {
                        tst1 = Math.Abs(h[k-1,k-1])+Math.Abs(h[k,k]);
                        if( (double)(tst1)==(double)(0) )
                        {
                            tst1 = blas.upperhessenberg1norm(h, l, i, l, i, ref work);
                        }
                        if( (double)(Math.Abs(h[k,k-1]))<=(double)(Math.Max(ulp*tst1, smlnum)) )
                        {
                            break;
                        }
                    }
                    l = k;
                    if( l>1 )
                    {
                        
                        //
                        // H(L,L-1) is negligible.
                        //
                        h[l,l-1] = 0;
                    }
                    
                    //
                    // Exit from loop if a submatrix of order <= MAXB has split off.
                    //
                    if( l>=i-maxb+1 )
                    {
                        failflag = false;
                        break;
                    }
                    
                    //
                    // Now the active submatrix is in rows and columns L to I. If
                    // eigenvalues only are being computed, only the active submatrix
                    // need be transformed.
                    //
                    if( its==20 | its==30 )
                    {
                        
                        //
                        // Exceptional shifts.
                        //
                        for(ii=i-ns+1; ii<=i; ii++)
                        {
                            wr[ii] = cnst*(Math.Abs(h[ii,ii-1])+Math.Abs(h[ii,ii]));
                            wi[ii] = 0;
                        }
                    }
                    else
                    {
                        
                        //
                        // Use eigenvalues of trailing submatrix of order NS as shifts.
                        //
                        blas.copymatrix(h, i-ns+1, i, i-ns+1, i, ref s, 1, ns, 1, ns);
                        internalauxschur(false, false, ns, 1, ns, ref s, ref tmpwr, ref tmpwi, 1, ns, ref z, ref work, ref workv3, ref workc1, ref works1, ref ierr);
                        for(p1=1; p1<=ns; p1++)
                        {
                            wr[i-ns+p1] = tmpwr[p1];
                            wi[i-ns+p1] = tmpwi[p1];
                        }
                        if( ierr>0 )
                        {
                            
                            //
                            // If DLAHQR failed to compute all NS eigenvalues, use the
                            // unconverged diagonal elements as the remaining shifts.
                            //
                            for(ii=1; ii<=ierr; ii++)
                            {
                                wr[i-ns+ii] = s[ii,ii];
                                wi[i-ns+ii] = 0;
                            }
                        }
                    }
                    
                    //
                    // Form the first column of (G-w(1)) (G-w(2)) . . . (G-w(ns))
                    // where G is the Hessenberg submatrix H(L:I,L:I) and w is
                    // the vector of shifts (stored in WR and WI). The result is
                    // stored in the local array V.
                    //
                    v[1] = 1;
                    for(ii=2; ii<=ns+1; ii++)
                    {
                        v[ii] = 0;
                    }
                    nv = 1;
                    for(j=i-ns+1; j<=i; j++)
                    {
                        if( (double)(wi[j])>=(double)(0) )
                        {
                            if( (double)(wi[j])==(double)(0) )
                            {
                                
                                //
                                // real shift
                                //
                                p1 = nv+1;
                                for(i_=1; i_<=p1;i_++)
                                {
                                    vv[i_] = v[i_];
                                }
                                blas.matrixvectormultiply(h, l, l+nv, l, l+nv-1, false, vv, 1, nv, 1.0, ref v, 1, nv+1, -wr[j]);
                                nv = nv+1;
                            }
                            else
                            {
                                if( (double)(wi[j])>(double)(0) )
                                {
                                    
                                    //
                                    // complex conjugate pair of shifts
                                    //
                                    p1 = nv+1;
                                    for(i_=1; i_<=p1;i_++)
                                    {
                                        vv[i_] = v[i_];
                                    }
                                    blas.matrixvectormultiply(h, l, l+nv, l, l+nv-1, false, v, 1, nv, 1.0, ref vv, 1, nv+1, -(2*wr[j]));
                                    itemp = blas.vectoridxabsmax(vv, 1, nv+1);
                                    temp = 1/Math.Max(Math.Abs(vv[itemp]), smlnum);
                                    p1 = nv+1;
                                    for(i_=1; i_<=p1;i_++)
                                    {
                                        vv[i_] = temp*vv[i_];
                                    }
                                    absw = blas.pythag2(wr[j], wi[j]);
                                    temp = temp*absw*absw;
                                    blas.matrixvectormultiply(h, l, l+nv+1, l, l+nv, false, vv, 1, nv+1, 1.0, ref v, 1, nv+2, temp);
                                    nv = nv+2;
                                }
                            }
                            
                            //
                            // Scale V(1:NV) so that max(abs(V(i))) = 1. If V is zero,
                            // reset it to the unit vector.
                            //
                            itemp = blas.vectoridxabsmax(v, 1, nv);
                            temp = Math.Abs(v[itemp]);
                            if( (double)(temp)==(double)(0) )
                            {
                                v[1] = 1;
                                for(ii=2; ii<=nv; ii++)
                                {
                                    v[ii] = 0;
                                }
                            }
                            else
                            {
                                temp = Math.Max(temp, smlnum);
                                vt = 1/temp;
                                for(i_=1; i_<=nv;i_++)
                                {
                                    v[i_] = vt*v[i_];
                                }
                            }
                        }
                    }
                    
                    //
                    // Multiple-shift QR step
                    //
                    for(k=l; k<=i-1; k++)
                    {
                        
                        //
                        // The first iteration of this loop determines a reflection G
                        // from the vector V and applies it from left and right to H,
                        // thus creating a nonzero bulge below the subdiagonal.
                        //
                        // Each subsequent iteration determines a reflection G to
                        // restore the Hessenberg form in the (K-1)th column, and thus
                        // chases the bulge one step toward the bottom of the active
                        // submatrix. NR is the order of G.
                        //
                        nr = Math.Min(ns+1, i-k+1);
                        if( k>l )
                        {
                            p1 = k-1;
                            p2 = k+nr-1;
                            i1_ = (k) - (1);
                            for(i_=1; i_<=nr;i_++)
                            {
                                v[i_] = h[i_+i1_,p1];
                            }
                        }
                        reflections.generatereflection(ref v, nr, ref tau);
                        if( k>l )
                        {
                            h[k,k-1] = v[1];
                            for(ii=k+1; ii<=i; ii++)
                            {
                                h[ii,k-1] = 0;
                            }
                        }
                        v[1] = 1;
                        
                        //
                        // Apply G from the left to transform the rows of the matrix in
                        // columns K to I2.
                        //
                        reflections.applyreflectionfromtheleft(ref h, tau, v, k, k+nr-1, k, i2, ref work);
                        
                        //
                        // Apply G from the right to transform the columns of the
                        // matrix in rows I1 to min(K+NR,I).
                        //
                        reflections.applyreflectionfromtheright(ref h, tau, v, i1, Math.Min(k+nr, i), k, k+nr-1, ref work);
                        if( wantz )
                        {
                            
                            //
                            // Accumulate transformations in the matrix Z
                            //
                            reflections.applyreflectionfromtheright(ref z, tau, v, 1, n, k, k+nr-1, ref work);
                        }
                    }
                }
                
                //
                // Failure to converge in remaining number of iterations
                //
                if( failflag )
                {
                    info = i;
                    return;
                }
                
                //
                // A submatrix of order <= MAXB in rows and columns L to I has split
                // off. Use the double-shift QR algorithm to handle it.
                //
                internalauxschur(wantt, wantz, n, l, i, ref h, ref wr, ref wi, 1, n, ref z, ref work, ref workv3, ref workc1, ref works1, ref info);
                if( info>0 )
                {
                    return;
                }
                
                //
                // Decrement number of remaining iterations, and return to start of
                // the main loop with a new value of I.
                //
                itn = itn-its;
                i = l-1;
            }
        }


        private static void internalauxschur(bool wantt,
            bool wantz,
            int n,
            int ilo,
            int ihi,
            ref double[,] h,
            ref double[] wr,
            ref double[] wi,
            int iloz,
            int ihiz,
            ref double[,] z,
            ref double[] work,
            ref double[] workv3,
            ref double[] workc1,
            ref double[] works1,
            ref int info)
        {
            int i = 0;
            int i1 = 0;
            int i2 = 0;
            int itn = 0;
            int its = 0;
            int j = 0;
            int k = 0;
            int l = 0;
            int m = 0;
            int nh = 0;
            int nr = 0;
            int nz = 0;
            double ave = 0;
            double cs = 0;
            double disc = 0;
            double h00 = 0;
            double h10 = 0;
            double h11 = 0;
            double h12 = 0;
            double h21 = 0;
            double h22 = 0;
            double h33 = 0;
            double h33s = 0;
            double h43h34 = 0;
            double h44 = 0;
            double h44s = 0;
            double ovfl = 0;
            double s = 0;
            double smlnum = 0;
            double sn = 0;
            double sum = 0;
            double t1 = 0;
            double t2 = 0;
            double t3 = 0;
            double tst1 = 0;
            double unfl = 0;
            double v1 = 0;
            double v2 = 0;
            double v3 = 0;
            bool failflag = new bool();
            double dat1 = 0;
            double dat2 = 0;
            int p1 = 0;
            double him1im1 = 0;
            double him1i = 0;
            double hiim1 = 0;
            double hii = 0;
            double wrim1 = 0;
            double wri = 0;
            double wiim1 = 0;
            double wii = 0;
            double ulp = 0;

            info = 0;

            info = 0;
            dat1 = 0.75;
            dat2 = -0.4375;
            ulp = math.machineepsilon;
            
            //
            // Quick return if possible
            //
            if( n==0 )
            {
                return;
            }
            if( ilo==ihi )
            {
                wr[ilo] = h[ilo,ilo];
                wi[ilo] = 0;
                return;
            }
            nh = ihi-ilo+1;
            nz = ihiz-iloz+1;
            
            //
            // Set machine-dependent constants for the stopping criterion.
            // If norm(H) <= sqrt(OVFL), overflow should not occur.
            //
            unfl = math.minrealnumber;
            ovfl = 1/unfl;
            smlnum = unfl*(nh/ulp);
            
            //
            // I1 and I2 are the indices of the first row and last column of H
            // to which transformations must be applied. If eigenvalues only are
            // being computed, I1 and I2 are set inside the main loop.
            //
            i1 = 1;
            i2 = n;
            
            //
            // ITN is the total number of QR iterations allowed.
            //
            itn = 30*nh;
            
            //
            // The main loop begins here. I is the loop index and decreases from
            // IHI to ILO in steps of 1 or 2. Each iteration of the loop works
            // with the active submatrix in rows and columns L to I.
            // Eigenvalues I+1 to IHI have already converged. Either L = ILO or
            // H(L,L-1) is negligible so that the matrix splits.
            //
            i = ihi;
            while( true )
            {
                l = ilo;
                if( i<ilo )
                {
                    return;
                }
                
                //
                // Perform QR iterations on rows and columns ILO to I until a
                // submatrix of order 1 or 2 splits off at the bottom because a
                // subdiagonal element has become negligible.
                //
                failflag = true;
                for(its=0; its<=itn; its++)
                {
                    
                    //
                    // Look for a single small subdiagonal element.
                    //
                    for(k=i; k>=l+1; k--)
                    {
                        tst1 = Math.Abs(h[k-1,k-1])+Math.Abs(h[k,k]);
                        if( (double)(tst1)==(double)(0) )
                        {
                            tst1 = blas.upperhessenberg1norm(h, l, i, l, i, ref work);
                        }
                        if( (double)(Math.Abs(h[k,k-1]))<=(double)(Math.Max(ulp*tst1, smlnum)) )
                        {
                            break;
                        }
                    }
                    l = k;
                    if( l>ilo )
                    {
                        
                        //
                        // H(L,L-1) is negligible
                        //
                        h[l,l-1] = 0;
                    }
                    
                    //
                    // Exit from loop if a submatrix of order 1 or 2 has split off.
                    //
                    if( l>=i-1 )
                    {
                        failflag = false;
                        break;
                    }
                    
                    //
                    // Now the active submatrix is in rows and columns L to I. If
                    // eigenvalues only are being computed, only the active submatrix
                    // need be transformed.
                    //
                    if( its==10 | its==20 )
                    {
                        
                        //
                        // Exceptional shift.
                        //
                        s = Math.Abs(h[i,i-1])+Math.Abs(h[i-1,i-2]);
                        h44 = dat1*s+h[i,i];
                        h33 = h44;
                        h43h34 = dat2*s*s;
                    }
                    else
                    {
                        
                        //
                        // Prepare to use Francis' double shift
                        // (i.e. 2nd degree generalized Rayleigh quotient)
                        //
                        h44 = h[i,i];
                        h33 = h[i-1,i-1];
                        h43h34 = h[i,i-1]*h[i-1,i];
                        s = h[i-1,i-2]*h[i-1,i-2];
                        disc = (h33-h44)*0.5;
                        disc = disc*disc+h43h34;
                        if( (double)(disc)>(double)(0) )
                        {
                            
                            //
                            // Real roots: use Wilkinson's shift twice
                            //
                            disc = Math.Sqrt(disc);
                            ave = 0.5*(h33+h44);
                            if( (double)(Math.Abs(h33)-Math.Abs(h44))>(double)(0) )
                            {
                                h33 = h33*h44-h43h34;
                                h44 = h33/(extschursign(disc, ave)+ave);
                            }
                            else
                            {
                                h44 = extschursign(disc, ave)+ave;
                            }
                            h33 = h44;
                            h43h34 = 0;
                        }
                    }
                    
                    //
                    // Look for two consecutive small subdiagonal elements.
                    //
                    for(m=i-2; m>=l; m--)
                    {
                        
                        //
                        // Determine the effect of starting the double-shift QR
                        // iteration at row M, and see if this would make H(M,M-1)
                        // negligible.
                        //
                        h11 = h[m,m];
                        h22 = h[m+1,m+1];
                        h21 = h[m+1,m];
                        h12 = h[m,m+1];
                        h44s = h44-h11;
                        h33s = h33-h11;
                        v1 = (h33s*h44s-h43h34)/h21+h12;
                        v2 = h22-h11-h33s-h44s;
                        v3 = h[m+2,m+1];
                        s = Math.Abs(v1)+Math.Abs(v2)+Math.Abs(v3);
                        v1 = v1/s;
                        v2 = v2/s;
                        v3 = v3/s;
                        workv3[1] = v1;
                        workv3[2] = v2;
                        workv3[3] = v3;
                        if( m==l )
                        {
                            break;
                        }
                        h00 = h[m-1,m-1];
                        h10 = h[m,m-1];
                        tst1 = Math.Abs(v1)*(Math.Abs(h00)+Math.Abs(h11)+Math.Abs(h22));
                        if( (double)(Math.Abs(h10)*(Math.Abs(v2)+Math.Abs(v3)))<=(double)(ulp*tst1) )
                        {
                            break;
                        }
                    }
                    
                    //
                    // Double-shift QR step
                    //
                    for(k=m; k<=i-1; k++)
                    {
                        
                        //
                        // The first iteration of this loop determines a reflection G
                        // from the vector V and applies it from left and right to H,
                        // thus creating a nonzero bulge below the subdiagonal.
                        //
                        // Each subsequent iteration determines a reflection G to
                        // restore the Hessenberg form in the (K-1)th column, and thus
                        // chases the bulge one step toward the bottom of the active
                        // submatrix. NR is the order of G.
                        //
                        nr = Math.Min(3, i-k+1);
                        if( k>m )
                        {
                            for(p1=1; p1<=nr; p1++)
                            {
                                workv3[p1] = h[k+p1-1,k-1];
                            }
                        }
                        reflections.generatereflection(ref workv3, nr, ref t1);
                        if( k>m )
                        {
                            h[k,k-1] = workv3[1];
                            h[k+1,k-1] = 0;
                            if( k<i-1 )
                            {
                                h[k+2,k-1] = 0;
                            }
                        }
                        else
                        {
                            if( m>l )
                            {
                                h[k,k-1] = -h[k,k-1];
                            }
                        }
                        v2 = workv3[2];
                        t2 = t1*v2;
                        if( nr==3 )
                        {
                            v3 = workv3[3];
                            t3 = t1*v3;
                            
                            //
                            // Apply G from the left to transform the rows of the matrix
                            // in columns K to I2.
                            //
                            for(j=k; j<=i2; j++)
                            {
                                sum = h[k,j]+v2*h[k+1,j]+v3*h[k+2,j];
                                h[k,j] = h[k,j]-sum*t1;
                                h[k+1,j] = h[k+1,j]-sum*t2;
                                h[k+2,j] = h[k+2,j]-sum*t3;
                            }
                            
                            //
                            // Apply G from the right to transform the columns of the
                            // matrix in rows I1 to min(K+3,I).
                            //
                            for(j=i1; j<=Math.Min(k+3, i); j++)
                            {
                                sum = h[j,k]+v2*h[j,k+1]+v3*h[j,k+2];
                                h[j,k] = h[j,k]-sum*t1;
                                h[j,k+1] = h[j,k+1]-sum*t2;
                                h[j,k+2] = h[j,k+2]-sum*t3;
                            }
                            if( wantz )
                            {
                                
                                //
                                // Accumulate transformations in the matrix Z
                                //
                                for(j=iloz; j<=ihiz; j++)
                                {
                                    sum = z[j,k]+v2*z[j,k+1]+v3*z[j,k+2];
                                    z[j,k] = z[j,k]-sum*t1;
                                    z[j,k+1] = z[j,k+1]-sum*t2;
                                    z[j,k+2] = z[j,k+2]-sum*t3;
                                }
                            }
                        }
                        else
                        {
                            if( nr==2 )
                            {
                                
                                //
                                // Apply G from the left to transform the rows of the matrix
                                // in columns K to I2.
                                //
                                for(j=k; j<=i2; j++)
                                {
                                    sum = h[k,j]+v2*h[k+1,j];
                                    h[k,j] = h[k,j]-sum*t1;
                                    h[k+1,j] = h[k+1,j]-sum*t2;
                                }
                                
                                //
                                // Apply G from the right to transform the columns of the
                                // matrix in rows I1 to min(K+3,I).
                                //
                                for(j=i1; j<=i; j++)
                                {
                                    sum = h[j,k]+v2*h[j,k+1];
                                    h[j,k] = h[j,k]-sum*t1;
                                    h[j,k+1] = h[j,k+1]-sum*t2;
                                }
                                if( wantz )
                                {
                                    
                                    //
                                    // Accumulate transformations in the matrix Z
                                    //
                                    for(j=iloz; j<=ihiz; j++)
                                    {
                                        sum = z[j,k]+v2*z[j,k+1];
                                        z[j,k] = z[j,k]-sum*t1;
                                        z[j,k+1] = z[j,k+1]-sum*t2;
                                    }
                                }
                            }
                        }
                    }
                }
                if( failflag )
                {
                    
                    //
                    // Failure to converge in remaining number of iterations
                    //
                    info = i;
                    return;
                }
                if( l==i )
                {
                    
                    //
                    // H(I,I-1) is negligible: one eigenvalue has converged.
                    //
                    wr[i] = h[i,i];
                    wi[i] = 0;
                }
                else
                {
                    if( l==i-1 )
                    {
                        
                        //
                        // H(I-1,I-2) is negligible: a pair of eigenvalues have converged.
                        //
                        //        Transform the 2-by-2 submatrix to standard Schur form,
                        //        and compute and store the eigenvalues.
                        //
                        him1im1 = h[i-1,i-1];
                        him1i = h[i-1,i];
                        hiim1 = h[i,i-1];
                        hii = h[i,i];
                        aux2x2schur(ref him1im1, ref him1i, ref hiim1, ref hii, ref wrim1, ref wiim1, ref wri, ref wii, ref cs, ref sn);
                        wr[i-1] = wrim1;
                        wi[i-1] = wiim1;
                        wr[i] = wri;
                        wi[i] = wii;
                        h[i-1,i-1] = him1im1;
                        h[i-1,i] = him1i;
                        h[i,i-1] = hiim1;
                        h[i,i] = hii;
                        if( wantt )
                        {
                            
                            //
                            // Apply the transformation to the rest of H.
                            //
                            if( i2>i )
                            {
                                workc1[1] = cs;
                                works1[1] = sn;
                                rotations.applyrotationsfromtheleft(true, i-1, i, i+1, i2, workc1, works1, ref h, ref work);
                            }
                            workc1[1] = cs;
                            works1[1] = sn;
                            rotations.applyrotationsfromtheright(true, i1, i-2, i-1, i, workc1, works1, ref h, ref work);
                        }
                        if( wantz )
                        {
                            
                            //
                            // Apply the transformation to Z.
                            //
                            workc1[1] = cs;
                            works1[1] = sn;
                            rotations.applyrotationsfromtheright(true, iloz, iloz+nz-1, i-1, i, workc1, works1, ref z, ref work);
                        }
                    }
                }
                
                //
                // Decrement number of remaining iterations, and return to start of
                // the main loop with new value of I.
                //
                itn = itn-its;
                i = l-1;
            }
        }


        private static void aux2x2schur(ref double a,
            ref double b,
            ref double c,
            ref double d,
            ref double rt1r,
            ref double rt1i,
            ref double rt2r,
            ref double rt2i,
            ref double cs,
            ref double sn)
        {
            double multpl = 0;
            double aa = 0;
            double bb = 0;
            double bcmax = 0;
            double bcmis = 0;
            double cc = 0;
            double cs1 = 0;
            double dd = 0;
            double eps = 0;
            double p = 0;
            double sab = 0;
            double sac = 0;
            double scl = 0;
            double sigma = 0;
            double sn1 = 0;
            double tau = 0;
            double temp = 0;
            double z = 0;

            rt1r = 0;
            rt1i = 0;
            rt2r = 0;
            rt2i = 0;
            cs = 0;
            sn = 0;

            multpl = 4.0;
            eps = math.machineepsilon;
            if( (double)(c)==(double)(0) )
            {
                cs = 1;
                sn = 0;
            }
            else
            {
                if( (double)(b)==(double)(0) )
                {
                    
                    //
                    // Swap rows and columns
                    //
                    cs = 0;
                    sn = 1;
                    temp = d;
                    d = a;
                    a = temp;
                    b = -c;
                    c = 0;
                }
                else
                {
                    if( (double)(a-d)==(double)(0) & extschursigntoone(b)!=extschursigntoone(c) )
                    {
                        cs = 1;
                        sn = 0;
                    }
                    else
                    {
                        temp = a-d;
                        p = 0.5*temp;
                        bcmax = Math.Max(Math.Abs(b), Math.Abs(c));
                        bcmis = Math.Min(Math.Abs(b), Math.Abs(c))*extschursigntoone(b)*extschursigntoone(c);
                        scl = Math.Max(Math.Abs(p), bcmax);
                        z = p/scl*p+bcmax/scl*bcmis;
                        
                        //
                        // If Z is of the order of the machine accuracy, postpone the
                        // decision on the nature of eigenvalues
                        //
                        if( (double)(z)>=(double)(multpl*eps) )
                        {
                            
                            //
                            // Real eigenvalues. Compute A and D.
                            //
                            z = p+extschursign(Math.Sqrt(scl)*Math.Sqrt(z), p);
                            a = d+z;
                            d = d-bcmax/z*bcmis;
                            
                            //
                            // Compute B and the rotation matrix
                            //
                            tau = blas.pythag2(c, z);
                            cs = z/tau;
                            sn = c/tau;
                            b = b-c;
                            c = 0;
                        }
                        else
                        {
                            
                            //
                            // Complex eigenvalues, or real (almost) equal eigenvalues.
                            // Make diagonal elements equal.
                            //
                            sigma = b+c;
                            tau = blas.pythag2(sigma, temp);
                            cs = Math.Sqrt(0.5*(1+Math.Abs(sigma)/tau));
                            sn = -(p/(tau*cs)*extschursign(1, sigma));
                            
                            //
                            // Compute [ AA  BB ] = [ A  B ] [ CS -SN ]
                            //         [ CC  DD ]   [ C  D ] [ SN  CS ]
                            //
                            aa = a*cs+b*sn;
                            bb = -(a*sn)+b*cs;
                            cc = c*cs+d*sn;
                            dd = -(c*sn)+d*cs;
                            
                            //
                            // Compute [ A  B ] = [ CS  SN ] [ AA  BB ]
                            //         [ C  D ]   [-SN  CS ] [ CC  DD ]
                            //
                            a = aa*cs+cc*sn;
                            b = bb*cs+dd*sn;
                            c = -(aa*sn)+cc*cs;
                            d = -(bb*sn)+dd*cs;
                            temp = 0.5*(a+d);
                            a = temp;
                            d = temp;
                            if( (double)(c)!=(double)(0) )
                            {
                                if( (double)(b)!=(double)(0) )
                                {
                                    if( extschursigntoone(b)==extschursigntoone(c) )
                                    {
                                        
                                        //
                                        // Real eigenvalues: reduce to upper triangular form
                                        //
                                        sab = Math.Sqrt(Math.Abs(b));
                                        sac = Math.Sqrt(Math.Abs(c));
                                        p = extschursign(sab*sac, c);
                                        tau = 1/Math.Sqrt(Math.Abs(b+c));
                                        a = temp+p;
                                        d = temp-p;
                                        b = b-c;
                                        c = 0;
                                        cs1 = sab*tau;
                                        sn1 = sac*tau;
                                        temp = cs*cs1-sn*sn1;
                                        sn = cs*sn1+sn*cs1;
                                        cs = temp;
                                    }
                                }
                                else
                                {
                                    b = -c;
                                    c = 0;
                                    temp = cs;
                                    cs = -sn;
                                    sn = temp;
                                }
                            }
                        }
                    }
                }
            }
            
            //
            // Store eigenvalues in (RT1R,RT1I) and (RT2R,RT2I).
            //
            rt1r = a;
            rt2r = d;
            if( (double)(c)==(double)(0) )
            {
                rt1i = 0;
                rt2i = 0;
            }
            else
            {
                rt1i = Math.Sqrt(Math.Abs(b))*Math.Sqrt(Math.Abs(c));
                rt2i = -rt1i;
            }
        }


        private static double extschursign(double a,
            double b)
        {
            double result = 0;

            if( (double)(b)>=(double)(0) )
            {
                result = Math.Abs(a);
            }
            else
            {
                result = -Math.Abs(a);
            }
            return result;
        }


        private static int extschursigntoone(double b)
        {
            int result = 0;

            if( (double)(b)>=(double)(0) )
            {
                result = 1;
            }
            else
            {
                result = -1;
            }
            return result;
        }


    }
    public class trlinsolve
    {
        /*************************************************************************
        Utility subroutine performing the "safe" solution of system of linear
        equations with triangular coefficient matrices.

        The subroutine uses scaling and solves the scaled system A*x=s*b (where  s
        is  a  scalar  value)  instead  of  A*x=b,  choosing  s  so  that x can be
        represented by a floating-point number. The closer the system  gets  to  a
        singular, the less s is. If the system is singular, s=0 and x contains the
        non-trivial solution of equation A*x=0.

        The feature of an algorithm is that it could not cause an  overflow  or  a
        division by zero regardless of the matrix used as the input.

        The algorithm can solve systems of equations with  upper/lower  triangular
        matrices,  with/without unit diagonal, and systems of type A*x=b or A'*x=b
        (where A' is a transposed matrix A).

        Input parameters:
            A       -   system matrix. Array whose indexes range within [0..N-1, 0..N-1].
            N       -   size of matrix A.
            X       -   right-hand member of a system.
                        Array whose index ranges within [0..N-1].
            IsUpper -   matrix type. If it is True, the system matrix is the upper
                        triangular and is located in  the  corresponding  part  of
                        matrix A.
            Trans   -   problem type. If it is True, the problem to be  solved  is
                        A'*x=b, otherwise it is A*x=b.
            Isunit  -   matrix type. If it is True, the system matrix has  a  unit
                        diagonal (the elements on the main diagonal are  not  used
                        in the calculation process), otherwise the matrix is considered
                        to be a general triangular matrix.

        Output parameters:
            X       -   solution. Array whose index ranges within [0..N-1].
            S       -   scaling factor.

          -- LAPACK auxiliary routine (version 3.0) --
             Univ. of Tennessee, Univ. of California Berkeley, NAG Ltd.,
             Courant Institute, Argonne National Lab, and Rice University
             June 30, 1992
        *************************************************************************/
        public static void rmatrixtrsafesolve(double[,] a,
            int n,
            ref double[] x,
            ref double s,
            bool isupper,
            bool istrans,
            bool isunit)
        {
            bool normin = new bool();
            double[] cnorm = new double[0];
            double[,] a1 = new double[0,0];
            double[] x1 = new double[0];
            int i = 0;
            int i_ = 0;
            int i1_ = 0;

            s = 0;

            
            //
            // From 0-based to 1-based
            //
            normin = false;
            a1 = new double[n+1, n+1];
            x1 = new double[n+1];
            for(i=1; i<=n; i++)
            {
                i1_ = (0) - (1);
                for(i_=1; i_<=n;i_++)
                {
                    a1[i,i_] = a[i-1,i_+i1_];
                }
            }
            i1_ = (0) - (1);
            for(i_=1; i_<=n;i_++)
            {
                x1[i_] = x[i_+i1_];
            }
            
            //
            // Solve 1-based
            //
            safesolvetriangular(a1, n, ref x1, ref s, isupper, istrans, isunit, normin, ref cnorm);
            
            //
            // From 1-based to 0-based
            //
            i1_ = (1) - (0);
            for(i_=0; i_<=n-1;i_++)
            {
                x[i_] = x1[i_+i1_];
            }
        }


        /*************************************************************************
        Obsolete 1-based subroutine.
        See RMatrixTRSafeSolve for 0-based replacement.
        *************************************************************************/
        public static void safesolvetriangular(double[,] a,
            int n,
            ref double[] x,
            ref double s,
            bool isupper,
            bool istrans,
            bool isunit,
            bool normin,
            ref double[] cnorm)
        {
            int i = 0;
            int imax = 0;
            int j = 0;
            int jfirst = 0;
            int jinc = 0;
            int jlast = 0;
            int jm1 = 0;
            int jp1 = 0;
            int ip1 = 0;
            int im1 = 0;
            int k = 0;
            int flg = 0;
            double v = 0;
            double vd = 0;
            double bignum = 0;
            double grow = 0;
            double rec = 0;
            double smlnum = 0;
            double sumj = 0;
            double tjj = 0;
            double tjjs = 0;
            double tmax = 0;
            double tscal = 0;
            double uscal = 0;
            double xbnd = 0;
            double xj = 0;
            double xmax = 0;
            bool notran = new bool();
            bool upper = new bool();
            bool nounit = new bool();
            int i_ = 0;

            s = 0;

            upper = isupper;
            notran = !istrans;
            nounit = !isunit;
            
            //
            // these initializers are not really necessary,
            // but without them compiler complains about uninitialized locals
            //
            tjjs = 0;
            
            //
            // Quick return if possible
            //
            if( n==0 )
            {
                return;
            }
            
            //
            // Determine machine dependent parameters to control overflow.
            //
            smlnum = math.minrealnumber/(math.machineepsilon*2);
            bignum = 1/smlnum;
            s = 1;
            if( !normin )
            {
                cnorm = new double[n+1];
                
                //
                // Compute the 1-norm of each column, not including the diagonal.
                //
                if( upper )
                {
                    
                    //
                    // A is upper triangular.
                    //
                    for(j=1; j<=n; j++)
                    {
                        v = 0;
                        for(k=1; k<=j-1; k++)
                        {
                            v = v+Math.Abs(a[k,j]);
                        }
                        cnorm[j] = v;
                    }
                }
                else
                {
                    
                    //
                    // A is lower triangular.
                    //
                    for(j=1; j<=n-1; j++)
                    {
                        v = 0;
                        for(k=j+1; k<=n; k++)
                        {
                            v = v+Math.Abs(a[k,j]);
                        }
                        cnorm[j] = v;
                    }
                    cnorm[n] = 0;
                }
            }
            
            //
            // Scale the column norms by TSCAL if the maximum element in CNORM is
            // greater than BIGNUM.
            //
            imax = 1;
            for(k=2; k<=n; k++)
            {
                if( (double)(cnorm[k])>(double)(cnorm[imax]) )
                {
                    imax = k;
                }
            }
            tmax = cnorm[imax];
            if( (double)(tmax)<=(double)(bignum) )
            {
                tscal = 1;
            }
            else
            {
                tscal = 1/(smlnum*tmax);
                for(i_=1; i_<=n;i_++)
                {
                    cnorm[i_] = tscal*cnorm[i_];
                }
            }
            
            //
            // Compute a bound on the computed solution vector to see if the
            // Level 2 BLAS routine DTRSV can be used.
            //
            j = 1;
            for(k=2; k<=n; k++)
            {
                if( (double)(Math.Abs(x[k]))>(double)(Math.Abs(x[j])) )
                {
                    j = k;
                }
            }
            xmax = Math.Abs(x[j]);
            xbnd = xmax;
            if( notran )
            {
                
                //
                // Compute the growth in A * x = b.
                //
                if( upper )
                {
                    jfirst = n;
                    jlast = 1;
                    jinc = -1;
                }
                else
                {
                    jfirst = 1;
                    jlast = n;
                    jinc = 1;
                }
                if( (double)(tscal)!=(double)(1) )
                {
                    grow = 0;
                }
                else
                {
                    if( nounit )
                    {
                        
                        //
                        // A is non-unit triangular.
                        //
                        // Compute GROW = 1/G(j) and XBND = 1/M(j).
                        // Initially, G(0) = max{x(i), i=1,...,n}.
                        //
                        grow = 1/Math.Max(xbnd, smlnum);
                        xbnd = grow;
                        j = jfirst;
                        while( (jinc>0 & j<=jlast) | (jinc<0 & j>=jlast) )
                        {
                            
                            //
                            // Exit the loop if the growth factor is too small.
                            //
                            if( (double)(grow)<=(double)(smlnum) )
                            {
                                break;
                            }
                            
                            //
                            // M(j) = G(j-1) / abs(A(j,j))
                            //
                            tjj = Math.Abs(a[j,j]);
                            xbnd = Math.Min(xbnd, Math.Min(1, tjj)*grow);
                            if( (double)(tjj+cnorm[j])>=(double)(smlnum) )
                            {
                                
                                //
                                // G(j) = G(j-1)*( 1 + CNORM(j) / abs(A(j,j)) )
                                //
                                grow = grow*(tjj/(tjj+cnorm[j]));
                            }
                            else
                            {
                                
                                //
                                // G(j) could overflow, set GROW to 0.
                                //
                                grow = 0;
                            }
                            if( j==jlast )
                            {
                                grow = xbnd;
                            }
                            j = j+jinc;
                        }
                    }
                    else
                    {
                        
                        //
                        // A is unit triangular.
                        //
                        // Compute GROW = 1/G(j), where G(0) = max{x(i), i=1,...,n}.
                        //
                        grow = Math.Min(1, 1/Math.Max(xbnd, smlnum));
                        j = jfirst;
                        while( (jinc>0 & j<=jlast) | (jinc<0 & j>=jlast) )
                        {
                            
                            //
                            // Exit the loop if the growth factor is too small.
                            //
                            if( (double)(grow)<=(double)(smlnum) )
                            {
                                break;
                            }
                            
                            //
                            // G(j) = G(j-1)*( 1 + CNORM(j) )
                            //
                            grow = grow*(1/(1+cnorm[j]));
                            j = j+jinc;
                        }
                    }
                }
            }
            else
            {
                
                //
                // Compute the growth in A' * x = b.
                //
                if( upper )
                {
                    jfirst = 1;
                    jlast = n;
                    jinc = 1;
                }
                else
                {
                    jfirst = n;
                    jlast = 1;
                    jinc = -1;
                }
                if( (double)(tscal)!=(double)(1) )
                {
                    grow = 0;
                }
                else
                {
                    if( nounit )
                    {
                        
                        //
                        // A is non-unit triangular.
                        //
                        // Compute GROW = 1/G(j) and XBND = 1/M(j).
                        // Initially, M(0) = max{x(i), i=1,...,n}.
                        //
                        grow = 1/Math.Max(xbnd, smlnum);
                        xbnd = grow;
                        j = jfirst;
                        while( (jinc>0 & j<=jlast) | (jinc<0 & j>=jlast) )
                        {
                            
                            //
                            // Exit the loop if the growth factor is too small.
                            //
                            if( (double)(grow)<=(double)(smlnum) )
                            {
                                break;
                            }
                            
                            //
                            // G(j) = max( G(j-1), M(j-1)*( 1 + CNORM(j) ) )
                            //
                            xj = 1+cnorm[j];
                            grow = Math.Min(grow, xbnd/xj);
                            
                            //
                            // M(j) = M(j-1)*( 1 + CNORM(j) ) / abs(A(j,j))
                            //
                            tjj = Math.Abs(a[j,j]);
                            if( (double)(xj)>(double)(tjj) )
                            {
                                xbnd = xbnd*(tjj/xj);
                            }
                            if( j==jlast )
                            {
                                grow = Math.Min(grow, xbnd);
                            }
                            j = j+jinc;
                        }
                    }
                    else
                    {
                        
                        //
                        // A is unit triangular.
                        //
                        // Compute GROW = 1/G(j), where G(0) = max{x(i), i=1,...,n}.
                        //
                        grow = Math.Min(1, 1/Math.Max(xbnd, smlnum));
                        j = jfirst;
                        while( (jinc>0 & j<=jlast) | (jinc<0 & j>=jlast) )
                        {
                            
                            //
                            // Exit the loop if the growth factor is too small.
                            //
                            if( (double)(grow)<=(double)(smlnum) )
                            {
                                break;
                            }
                            
                            //
                            // G(j) = ( 1 + CNORM(j) )*G(j-1)
                            //
                            xj = 1+cnorm[j];
                            grow = grow/xj;
                            j = j+jinc;
                        }
                    }
                }
            }
            if( (double)(grow*tscal)>(double)(smlnum) )
            {
                
                //
                // Use the Level 2 BLAS solve if the reciprocal of the bound on
                // elements of X is not too small.
                //
                if( (upper & notran) | (!upper & !notran) )
                {
                    if( nounit )
                    {
                        vd = a[n,n];
                    }
                    else
                    {
                        vd = 1;
                    }
                    x[n] = x[n]/vd;
                    for(i=n-1; i>=1; i--)
                    {
                        ip1 = i+1;
                        if( upper )
                        {
                            v = 0.0;
                            for(i_=ip1; i_<=n;i_++)
                            {
                                v += a[i,i_]*x[i_];
                            }
                        }
                        else
                        {
                            v = 0.0;
                            for(i_=ip1; i_<=n;i_++)
                            {
                                v += a[i_,i]*x[i_];
                            }
                        }
                        if( nounit )
                        {
                            vd = a[i,i];
                        }
                        else
                        {
                            vd = 1;
                        }
                        x[i] = (x[i]-v)/vd;
                    }
                }
                else
                {
                    if( nounit )
                    {
                        vd = a[1,1];
                    }
                    else
                    {
                        vd = 1;
                    }
                    x[1] = x[1]/vd;
                    for(i=2; i<=n; i++)
                    {
                        im1 = i-1;
                        if( upper )
                        {
                            v = 0.0;
                            for(i_=1; i_<=im1;i_++)
                            {
                                v += a[i_,i]*x[i_];
                            }
                        }
                        else
                        {
                            v = 0.0;
                            for(i_=1; i_<=im1;i_++)
                            {
                                v += a[i,i_]*x[i_];
                            }
                        }
                        if( nounit )
                        {
                            vd = a[i,i];
                        }
                        else
                        {
                            vd = 1;
                        }
                        x[i] = (x[i]-v)/vd;
                    }
                }
            }
            else
            {
                
                //
                // Use a Level 1 BLAS solve, scaling intermediate results.
                //
                if( (double)(xmax)>(double)(bignum) )
                {
                    
                    //
                    // Scale X so that its components are less than or equal to
                    // BIGNUM in absolute value.
                    //
                    s = bignum/xmax;
                    for(i_=1; i_<=n;i_++)
                    {
                        x[i_] = s*x[i_];
                    }
                    xmax = bignum;
                }
                if( notran )
                {
                    
                    //
                    // Solve A * x = b
                    //
                    j = jfirst;
                    while( (jinc>0 & j<=jlast) | (jinc<0 & j>=jlast) )
                    {
                        
                        //
                        // Compute x(j) = b(j) / A(j,j), scaling x if necessary.
                        //
                        xj = Math.Abs(x[j]);
                        flg = 0;
                        if( nounit )
                        {
                            tjjs = a[j,j]*tscal;
                        }
                        else
                        {
                            tjjs = tscal;
                            if( (double)(tscal)==(double)(1) )
                            {
                                flg = 100;
                            }
                        }
                        if( flg!=100 )
                        {
                            tjj = Math.Abs(tjjs);
                            if( (double)(tjj)>(double)(smlnum) )
                            {
                                
                                //
                                // abs(A(j,j)) > SMLNUM:
                                //
                                if( (double)(tjj)<(double)(1) )
                                {
                                    if( (double)(xj)>(double)(tjj*bignum) )
                                    {
                                        
                                        //
                                        // Scale x by 1/b(j).
                                        //
                                        rec = 1/xj;
                                        for(i_=1; i_<=n;i_++)
                                        {
                                            x[i_] = rec*x[i_];
                                        }
                                        s = s*rec;
                                        xmax = xmax*rec;
                                    }
                                }
                                x[j] = x[j]/tjjs;
                                xj = Math.Abs(x[j]);
                            }
                            else
                            {
                                if( (double)(tjj)>(double)(0) )
                                {
                                    
                                    //
                                    // 0 < abs(A(j,j)) <= SMLNUM:
                                    //
                                    if( (double)(xj)>(double)(tjj*bignum) )
                                    {
                                        
                                        //
                                        // Scale x by (1/abs(x(j)))*abs(A(j,j))*BIGNUM
                                        // to avoid overflow when dividing by A(j,j).
                                        //
                                        rec = tjj*bignum/xj;
                                        if( (double)(cnorm[j])>(double)(1) )
                                        {
                                            
                                            //
                                            // Scale by 1/CNORM(j) to avoid overflow when
                                            // multiplying x(j) times column j.
                                            //
                                            rec = rec/cnorm[j];
                                        }
                                        for(i_=1; i_<=n;i_++)
                                        {
                                            x[i_] = rec*x[i_];
                                        }
                                        s = s*rec;
                                        xmax = xmax*rec;
                                    }
                                    x[j] = x[j]/tjjs;
                                    xj = Math.Abs(x[j]);
                                }
                                else
                                {
                                    
                                    //
                                    // A(j,j) = 0:  Set x(1:n) = 0, x(j) = 1, and
                                    // scale = 0, and compute a solution to A*x = 0.
                                    //
                                    for(i=1; i<=n; i++)
                                    {
                                        x[i] = 0;
                                    }
                                    x[j] = 1;
                                    xj = 1;
                                    s = 0;
                                    xmax = 0;
                                }
                            }
                        }
                        
                        //
                        // Scale x if necessary to avoid overflow when adding a
                        // multiple of column j of A.
                        //
                        if( (double)(xj)>(double)(1) )
                        {
                            rec = 1/xj;
                            if( (double)(cnorm[j])>(double)((bignum-xmax)*rec) )
                            {
                                
                                //
                                // Scale x by 1/(2*abs(x(j))).
                                //
                                rec = rec*0.5;
                                for(i_=1; i_<=n;i_++)
                                {
                                    x[i_] = rec*x[i_];
                                }
                                s = s*rec;
                            }
                        }
                        else
                        {
                            if( (double)(xj*cnorm[j])>(double)(bignum-xmax) )
                            {
                                
                                //
                                // Scale x by 1/2.
                                //
                                for(i_=1; i_<=n;i_++)
                                {
                                    x[i_] = 0.5*x[i_];
                                }
                                s = s*0.5;
                            }
                        }
                        if( upper )
                        {
                            if( j>1 )
                            {
                                
                                //
                                // Compute the update
                                // x(1:j-1) := x(1:j-1) - x(j) * A(1:j-1,j)
                                //
                                v = x[j]*tscal;
                                jm1 = j-1;
                                for(i_=1; i_<=jm1;i_++)
                                {
                                    x[i_] = x[i_] - v*a[i_,j];
                                }
                                i = 1;
                                for(k=2; k<=j-1; k++)
                                {
                                    if( (double)(Math.Abs(x[k]))>(double)(Math.Abs(x[i])) )
                                    {
                                        i = k;
                                    }
                                }
                                xmax = Math.Abs(x[i]);
                            }
                        }
                        else
                        {
                            if( j<n )
                            {
                                
                                //
                                // Compute the update
                                // x(j+1:n) := x(j+1:n) - x(j) * A(j+1:n,j)
                                //
                                jp1 = j+1;
                                v = x[j]*tscal;
                                for(i_=jp1; i_<=n;i_++)
                                {
                                    x[i_] = x[i_] - v*a[i_,j];
                                }
                                i = j+1;
                                for(k=j+2; k<=n; k++)
                                {
                                    if( (double)(Math.Abs(x[k]))>(double)(Math.Abs(x[i])) )
                                    {
                                        i = k;
                                    }
                                }
                                xmax = Math.Abs(x[i]);
                            }
                        }
                        j = j+jinc;
                    }
                }
                else
                {
                    
                    //
                    // Solve A' * x = b
                    //
                    j = jfirst;
                    while( (jinc>0 & j<=jlast) | (jinc<0 & j>=jlast) )
                    {
                        
                        //
                        // Compute x(j) = b(j) - sum A(k,j)*x(k).
                        //   k<>j
                        //
                        xj = Math.Abs(x[j]);
                        uscal = tscal;
                        rec = 1/Math.Max(xmax, 1);
                        if( (double)(cnorm[j])>(double)((bignum-xj)*rec) )
                        {
                            
                            //
                            // If x(j) could overflow, scale x by 1/(2*XMAX).
                            //
                            rec = rec*0.5;
                            if( nounit )
                            {
                                tjjs = a[j,j]*tscal;
                            }
                            else
                            {
                                tjjs = tscal;
                            }
                            tjj = Math.Abs(tjjs);
                            if( (double)(tjj)>(double)(1) )
                            {
                                
                                //
                                // Divide by A(j,j) when scaling x if A(j,j) > 1.
                                //
                                rec = Math.Min(1, rec*tjj);
                                uscal = uscal/tjjs;
                            }
                            if( (double)(rec)<(double)(1) )
                            {
                                for(i_=1; i_<=n;i_++)
                                {
                                    x[i_] = rec*x[i_];
                                }
                                s = s*rec;
                                xmax = xmax*rec;
                            }
                        }
                        sumj = 0;
                        if( (double)(uscal)==(double)(1) )
                        {
                            
                            //
                            // If the scaling needed for A in the dot product is 1,
                            // call DDOT to perform the dot product.
                            //
                            if( upper )
                            {
                                if( j>1 )
                                {
                                    jm1 = j-1;
                                    sumj = 0.0;
                                    for(i_=1; i_<=jm1;i_++)
                                    {
                                        sumj += a[i_,j]*x[i_];
                                    }
                                }
                                else
                                {
                                    sumj = 0;
                                }
                            }
                            else
                            {
                                if( j<n )
                                {
                                    jp1 = j+1;
                                    sumj = 0.0;
                                    for(i_=jp1; i_<=n;i_++)
                                    {
                                        sumj += a[i_,j]*x[i_];
                                    }
                                }
                            }
                        }
                        else
                        {
                            
                            //
                            // Otherwise, use in-line code for the dot product.
                            //
                            if( upper )
                            {
                                for(i=1; i<=j-1; i++)
                                {
                                    v = a[i,j]*uscal;
                                    sumj = sumj+v*x[i];
                                }
                            }
                            else
                            {
                                if( j<n )
                                {
                                    for(i=j+1; i<=n; i++)
                                    {
                                        v = a[i,j]*uscal;
                                        sumj = sumj+v*x[i];
                                    }
                                }
                            }
                        }
                        if( (double)(uscal)==(double)(tscal) )
                        {
                            
                            //
                            // Compute x(j) := ( x(j) - sumj ) / A(j,j) if 1/A(j,j)
                            // was not used to scale the dotproduct.
                            //
                            x[j] = x[j]-sumj;
                            xj = Math.Abs(x[j]);
                            flg = 0;
                            if( nounit )
                            {
                                tjjs = a[j,j]*tscal;
                            }
                            else
                            {
                                tjjs = tscal;
                                if( (double)(tscal)==(double)(1) )
                                {
                                    flg = 150;
                                }
                            }
                            
                            //
                            // Compute x(j) = x(j) / A(j,j), scaling if necessary.
                            //
                            if( flg!=150 )
                            {
                                tjj = Math.Abs(tjjs);
                                if( (double)(tjj)>(double)(smlnum) )
                                {
                                    
                                    //
                                    // abs(A(j,j)) > SMLNUM:
                                    //
                                    if( (double)(tjj)<(double)(1) )
                                    {
                                        if( (double)(xj)>(double)(tjj*bignum) )
                                        {
                                            
                                            //
                                            // Scale X by 1/abs(x(j)).
                                            //
                                            rec = 1/xj;
                                            for(i_=1; i_<=n;i_++)
                                            {
                                                x[i_] = rec*x[i_];
                                            }
                                            s = s*rec;
                                            xmax = xmax*rec;
                                        }
                                    }
                                    x[j] = x[j]/tjjs;
                                }
                                else
                                {
                                    if( (double)(tjj)>(double)(0) )
                                    {
                                        
                                        //
                                        // 0 < abs(A(j,j)) <= SMLNUM:
                                        //
                                        if( (double)(xj)>(double)(tjj*bignum) )
                                        {
                                            
                                            //
                                            // Scale x by (1/abs(x(j)))*abs(A(j,j))*BIGNUM.
                                            //
                                            rec = tjj*bignum/xj;
                                            for(i_=1; i_<=n;i_++)
                                            {
                                                x[i_] = rec*x[i_];
                                            }
                                            s = s*rec;
                                            xmax = xmax*rec;
                                        }
                                        x[j] = x[j]/tjjs;
                                    }
                                    else
                                    {
                                        
                                        //
                                        // A(j,j) = 0:  Set x(1:n) = 0, x(j) = 1, and
                                        // scale = 0, and compute a solution to A'*x = 0.
                                        //
                                        for(i=1; i<=n; i++)
                                        {
                                            x[i] = 0;
                                        }
                                        x[j] = 1;
                                        s = 0;
                                        xmax = 0;
                                    }
                                }
                            }
                        }
                        else
                        {
                            
                            //
                            // Compute x(j) := x(j) / A(j,j)  - sumj if the dot
                            // product has already been divided by 1/A(j,j).
                            //
                            x[j] = x[j]/tjjs-sumj;
                        }
                        xmax = Math.Max(xmax, Math.Abs(x[j]));
                        j = j+jinc;
                    }
                }
                s = s/tscal;
            }
            
            //
            // Scale the column norms by 1/TSCAL for return.
            //
            if( (double)(tscal)!=(double)(1) )
            {
                v = 1/tscal;
                for(i_=1; i_<=n;i_++)
                {
                    cnorm[i_] = v*cnorm[i_];
                }
            }
        }


    }
    public class safesolve
    {
        /*************************************************************************
        Real implementation of CMatrixScaledTRSafeSolve

          -- ALGLIB routine --
             21.01.2010
             Bochkanov Sergey
        *************************************************************************/
        public static bool rmatrixscaledtrsafesolve(double[,] a,
            double sa,
            int n,
            ref double[] x,
            bool isupper,
            int trans,
            bool isunit,
            double maxgrowth)
        {
            bool result = new bool();
            double lnmax = 0;
            double nrmb = 0;
            double nrmx = 0;
            int i = 0;
            complex alpha = 0;
            complex beta = 0;
            double vr = 0;
            complex cx = 0;
            double[] tmp = new double[0];
            int i_ = 0;

            ap.assert(n>0, "RMatrixTRSafeSolve: incorrect N!");
            ap.assert(trans==0 | trans==1, "RMatrixTRSafeSolve: incorrect Trans!");
            result = true;
            lnmax = Math.Log(math.maxrealnumber);
            
            //
            // Quick return if possible
            //
            if( n<=0 )
            {
                return result;
            }
            
            //
            // Load norms: right part and X
            //
            nrmb = 0;
            for(i=0; i<=n-1; i++)
            {
                nrmb = Math.Max(nrmb, Math.Abs(x[i]));
            }
            nrmx = 0;
            
            //
            // Solve
            //
            tmp = new double[n];
            result = true;
            if( isupper & trans==0 )
            {
                
                //
                // U*x = b
                //
                for(i=n-1; i>=0; i--)
                {
                    
                    //
                    // Task is reduced to alpha*x[i] = beta
                    //
                    if( isunit )
                    {
                        alpha = sa;
                    }
                    else
                    {
                        alpha = a[i,i]*sa;
                    }
                    if( i<n-1 )
                    {
                        for(i_=i+1; i_<=n-1;i_++)
                        {
                            tmp[i_] = sa*a[i,i_];
                        }
                        vr = 0.0;
                        for(i_=i+1; i_<=n-1;i_++)
                        {
                            vr += tmp[i_]*x[i_];
                        }
                        beta = x[i]-vr;
                    }
                    else
                    {
                        beta = x[i];
                    }
                    
                    //
                    // solve alpha*x[i] = beta
                    //
                    result = cbasicsolveandupdate(alpha, beta, lnmax, nrmb, maxgrowth, ref nrmx, ref cx);
                    if( !result )
                    {
                        return result;
                    }
                    x[i] = cx.x;
                }
                return result;
            }
            if( !isupper & trans==0 )
            {
                
                //
                // L*x = b
                //
                for(i=0; i<=n-1; i++)
                {
                    
                    //
                    // Task is reduced to alpha*x[i] = beta
                    //
                    if( isunit )
                    {
                        alpha = sa;
                    }
                    else
                    {
                        alpha = a[i,i]*sa;
                    }
                    if( i>0 )
                    {
                        for(i_=0; i_<=i-1;i_++)
                        {
                            tmp[i_] = sa*a[i,i_];
                        }
                        vr = 0.0;
                        for(i_=0; i_<=i-1;i_++)
                        {
                            vr += tmp[i_]*x[i_];
                        }
                        beta = x[i]-vr;
                    }
                    else
                    {
                        beta = x[i];
                    }
                    
                    //
                    // solve alpha*x[i] = beta
                    //
                    result = cbasicsolveandupdate(alpha, beta, lnmax, nrmb, maxgrowth, ref nrmx, ref cx);
                    if( !result )
                    {
                        return result;
                    }
                    x[i] = cx.x;
                }
                return result;
            }
            if( isupper & trans==1 )
            {
                
                //
                // U^T*x = b
                //
                for(i=0; i<=n-1; i++)
                {
                    
                    //
                    // Task is reduced to alpha*x[i] = beta
                    //
                    if( isunit )
                    {
                        alpha = sa;
                    }
                    else
                    {
                        alpha = a[i,i]*sa;
                    }
                    beta = x[i];
                    
                    //
                    // solve alpha*x[i] = beta
                    //
                    result = cbasicsolveandupdate(alpha, beta, lnmax, nrmb, maxgrowth, ref nrmx, ref cx);
                    if( !result )
                    {
                        return result;
                    }
                    x[i] = cx.x;
                    
                    //
                    // update the rest of right part
                    //
                    if( i<n-1 )
                    {
                        vr = cx.x;
                        for(i_=i+1; i_<=n-1;i_++)
                        {
                            tmp[i_] = sa*a[i,i_];
                        }
                        for(i_=i+1; i_<=n-1;i_++)
                        {
                            x[i_] = x[i_] - vr*tmp[i_];
                        }
                    }
                }
                return result;
            }
            if( !isupper & trans==1 )
            {
                
                //
                // L^T*x = b
                //
                for(i=n-1; i>=0; i--)
                {
                    
                    //
                    // Task is reduced to alpha*x[i] = beta
                    //
                    if( isunit )
                    {
                        alpha = sa;
                    }
                    else
                    {
                        alpha = a[i,i]*sa;
                    }
                    beta = x[i];
                    
                    //
                    // solve alpha*x[i] = beta
                    //
                    result = cbasicsolveandupdate(alpha, beta, lnmax, nrmb, maxgrowth, ref nrmx, ref cx);
                    if( !result )
                    {
                        return result;
                    }
                    x[i] = cx.x;
                    
                    //
                    // update the rest of right part
                    //
                    if( i>0 )
                    {
                        vr = cx.x;
                        for(i_=0; i_<=i-1;i_++)
                        {
                            tmp[i_] = sa*a[i,i_];
                        }
                        for(i_=0; i_<=i-1;i_++)
                        {
                            x[i_] = x[i_] - vr*tmp[i_];
                        }
                    }
                }
                return result;
            }
            result = false;
            return result;
        }


        /*************************************************************************
        Internal subroutine for safe solution of

            SA*op(A)=b
            
        where  A  is  NxN  upper/lower  triangular/unitriangular  matrix, op(A) is
        either identity transform, transposition or Hermitian transposition, SA is
        a scaling factor such that max(|SA*A[i,j]|) is close to 1.0 in magnutude.

        This subroutine  limits  relative  growth  of  solution  (in inf-norm)  by
        MaxGrowth,  returning  False  if  growth  exceeds MaxGrowth. Degenerate or
        near-degenerate matrices are handled correctly (False is returned) as long
        as MaxGrowth is significantly less than MaxRealNumber/norm(b).

          -- ALGLIB routine --
             21.01.2010
             Bochkanov Sergey
        *************************************************************************/
        public static bool cmatrixscaledtrsafesolve(complex[,] a,
            double sa,
            int n,
            ref complex[] x,
            bool isupper,
            int trans,
            bool isunit,
            double maxgrowth)
        {
            bool result = new bool();
            double lnmax = 0;
            double nrmb = 0;
            double nrmx = 0;
            int i = 0;
            complex alpha = 0;
            complex beta = 0;
            complex vc = 0;
            complex[] tmp = new complex[0];
            int i_ = 0;

            ap.assert(n>0, "CMatrixTRSafeSolve: incorrect N!");
            ap.assert((trans==0 | trans==1) | trans==2, "CMatrixTRSafeSolve: incorrect Trans!");
            result = true;
            lnmax = Math.Log(math.maxrealnumber);
            
            //
            // Quick return if possible
            //
            if( n<=0 )
            {
                return result;
            }
            
            //
            // Load norms: right part and X
            //
            nrmb = 0;
            for(i=0; i<=n-1; i++)
            {
                nrmb = Math.Max(nrmb, math.abscomplex(x[i]));
            }
            nrmx = 0;
            
            //
            // Solve
            //
            tmp = new complex[n];
            result = true;
            if( isupper & trans==0 )
            {
                
                //
                // U*x = b
                //
                for(i=n-1; i>=0; i--)
                {
                    
                    //
                    // Task is reduced to alpha*x[i] = beta
                    //
                    if( isunit )
                    {
                        alpha = sa;
                    }
                    else
                    {
                        alpha = a[i,i]*sa;
                    }
                    if( i<n-1 )
                    {
                        for(i_=i+1; i_<=n-1;i_++)
                        {
                            tmp[i_] = sa*a[i,i_];
                        }
                        vc = 0.0;
                        for(i_=i+1; i_<=n-1;i_++)
                        {
                            vc += tmp[i_]*x[i_];
                        }
                        beta = x[i]-vc;
                    }
                    else
                    {
                        beta = x[i];
                    }
                    
                    //
                    // solve alpha*x[i] = beta
                    //
                    result = cbasicsolveandupdate(alpha, beta, lnmax, nrmb, maxgrowth, ref nrmx, ref vc);
                    if( !result )
                    {
                        return result;
                    }
                    x[i] = vc;
                }
                return result;
            }
            if( !isupper & trans==0 )
            {
                
                //
                // L*x = b
                //
                for(i=0; i<=n-1; i++)
                {
                    
                    //
                    // Task is reduced to alpha*x[i] = beta
                    //
                    if( isunit )
                    {
                        alpha = sa;
                    }
                    else
                    {
                        alpha = a[i,i]*sa;
                    }
                    if( i>0 )
                    {
                        for(i_=0; i_<=i-1;i_++)
                        {
                            tmp[i_] = sa*a[i,i_];
                        }
                        vc = 0.0;
                        for(i_=0; i_<=i-1;i_++)
                        {
                            vc += tmp[i_]*x[i_];
                        }
                        beta = x[i]-vc;
                    }
                    else
                    {
                        beta = x[i];
                    }
                    
                    //
                    // solve alpha*x[i] = beta
                    //
                    result = cbasicsolveandupdate(alpha, beta, lnmax, nrmb, maxgrowth, ref nrmx, ref vc);
                    if( !result )
                    {
                        return result;
                    }
                    x[i] = vc;
                }
                return result;
            }
            if( isupper & trans==1 )
            {
                
                //
                // U^T*x = b
                //
                for(i=0; i<=n-1; i++)
                {
                    
                    //
                    // Task is reduced to alpha*x[i] = beta
                    //
                    if( isunit )
                    {
                        alpha = sa;
                    }
                    else
                    {
                        alpha = a[i,i]*sa;
                    }
                    beta = x[i];
                    
                    //
                    // solve alpha*x[i] = beta
                    //
                    result = cbasicsolveandupdate(alpha, beta, lnmax, nrmb, maxgrowth, ref nrmx, ref vc);
                    if( !result )
                    {
                        return result;
                    }
                    x[i] = vc;
                    
                    //
                    // update the rest of right part
                    //
                    if( i<n-1 )
                    {
                        for(i_=i+1; i_<=n-1;i_++)
                        {
                            tmp[i_] = sa*a[i,i_];
                        }
                        for(i_=i+1; i_<=n-1;i_++)
                        {
                            x[i_] = x[i_] - vc*tmp[i_];
                        }
                    }
                }
                return result;
            }
            if( !isupper & trans==1 )
            {
                
                //
                // L^T*x = b
                //
                for(i=n-1; i>=0; i--)
                {
                    
                    //
                    // Task is reduced to alpha*x[i] = beta
                    //
                    if( isunit )
                    {
                        alpha = sa;
                    }
                    else
                    {
                        alpha = a[i,i]*sa;
                    }
                    beta = x[i];
                    
                    //
                    // solve alpha*x[i] = beta
                    //
                    result = cbasicsolveandupdate(alpha, beta, lnmax, nrmb, maxgrowth, ref nrmx, ref vc);
                    if( !result )
                    {
                        return result;
                    }
                    x[i] = vc;
                    
                    //
                    // update the rest of right part
                    //
                    if( i>0 )
                    {
                        for(i_=0; i_<=i-1;i_++)
                        {
                            tmp[i_] = sa*a[i,i_];
                        }
                        for(i_=0; i_<=i-1;i_++)
                        {
                            x[i_] = x[i_] - vc*tmp[i_];
                        }
                    }
                }
                return result;
            }
            if( isupper & trans==2 )
            {
                
                //
                // U^H*x = b
                //
                for(i=0; i<=n-1; i++)
                {
                    
                    //
                    // Task is reduced to alpha*x[i] = beta
                    //
                    if( isunit )
                    {
                        alpha = sa;
                    }
                    else
                    {
                        alpha = math.conj(a[i,i])*sa;
                    }
                    beta = x[i];
                    
                    //
                    // solve alpha*x[i] = beta
                    //
                    result = cbasicsolveandupdate(alpha, beta, lnmax, nrmb, maxgrowth, ref nrmx, ref vc);
                    if( !result )
                    {
                        return result;
                    }
                    x[i] = vc;
                    
                    //
                    // update the rest of right part
                    //
                    if( i<n-1 )
                    {
                        for(i_=i+1; i_<=n-1;i_++)
                        {
                            tmp[i_] = sa*math.conj(a[i,i_]);
                        }
                        for(i_=i+1; i_<=n-1;i_++)
                        {
                            x[i_] = x[i_] - vc*tmp[i_];
                        }
                    }
                }
                return result;
            }
            if( !isupper & trans==2 )
            {
                
                //
                // L^T*x = b
                //
                for(i=n-1; i>=0; i--)
                {
                    
                    //
                    // Task is reduced to alpha*x[i] = beta
                    //
                    if( isunit )
                    {
                        alpha = sa;
                    }
                    else
                    {
                        alpha = math.conj(a[i,i])*sa;
                    }
                    beta = x[i];
                    
                    //
                    // solve alpha*x[i] = beta
                    //
                    result = cbasicsolveandupdate(alpha, beta, lnmax, nrmb, maxgrowth, ref nrmx, ref vc);
                    if( !result )
                    {
                        return result;
                    }
                    x[i] = vc;
                    
                    //
                    // update the rest of right part
                    //
                    if( i>0 )
                    {
                        for(i_=0; i_<=i-1;i_++)
                        {
                            tmp[i_] = sa*math.conj(a[i,i_]);
                        }
                        for(i_=0; i_<=i-1;i_++)
                        {
                            x[i_] = x[i_] - vc*tmp[i_];
                        }
                    }
                }
                return result;
            }
            result = false;
            return result;
        }


        /*************************************************************************
        complex basic solver-updater for reduced linear system

            alpha*x[i] = beta

        solves this equation and updates it in overlfow-safe manner (keeping track
        of relative growth of solution).

        Parameters:
            Alpha   -   alpha
            Beta    -   beta
            LnMax   -   precomputed Ln(MaxRealNumber)
            BNorm   -   inf-norm of b (right part of original system)
            MaxGrowth-  maximum growth of norm(x) relative to norm(b)
            XNorm   -   inf-norm of other components of X (which are already processed)
                        it is updated by CBasicSolveAndUpdate.
            X       -   solution

          -- ALGLIB routine --
             26.01.2009
             Bochkanov Sergey
        *************************************************************************/
        private static bool cbasicsolveandupdate(complex alpha,
            complex beta,
            double lnmax,
            double bnorm,
            double maxgrowth,
            ref double xnorm,
            ref complex x)
        {
            bool result = new bool();
            double v = 0;

            x = 0;

            result = false;
            if( alpha==0 )
            {
                return result;
            }
            if( beta!=0 )
            {
                
                //
                // alpha*x[i]=beta
                //
                v = Math.Log(math.abscomplex(beta))-Math.Log(math.abscomplex(alpha));
                if( (double)(v)>(double)(lnmax) )
                {
                    return result;
                }
                x = beta/alpha;
            }
            else
            {
                
                //
                // alpha*x[i]=0
                //
                x = 0;
            }
            
            //
            // update NrmX, test growth limit
            //
            xnorm = Math.Max(xnorm, math.abscomplex(x));
            if( (double)(xnorm)>(double)(maxgrowth*bnorm) )
            {
                return result;
            }
            result = true;
            return result;
        }


    }
    public class xblas
    {
        /*************************************************************************
        More precise dot-product. Absolute error of  subroutine  result  is  about
        1 ulp of max(MX,V), where:
            MX = max( |a[i]*b[i]| )
            V  = |(a,b)|

        INPUT PARAMETERS
            A       -   array[0..N-1], vector 1
            B       -   array[0..N-1], vector 2
            N       -   vectors length, N<2^29.
            Temp    -   array[0..N-1], pre-allocated temporary storage

        OUTPUT PARAMETERS
            R       -   (A,B)
            RErr    -   estimate of error. This estimate accounts for both  errors
                        during  calculation  of  (A,B)  and  errors  introduced by
                        rounding of A and B to fit in double (about 1 ulp).

          -- ALGLIB --
             Copyright 24.08.2009 by Bochkanov Sergey
        *************************************************************************/
        public static void xdot(double[] a,
            double[] b,
            int n,
            ref double[] temp,
            ref double r,
            ref double rerr)
        {
            int i = 0;
            double mx = 0;
            double v = 0;

            r = 0;
            rerr = 0;

            
            //
            // special cases:
            // * N=0
            //
            if( n==0 )
            {
                r = 0;
                rerr = 0;
                return;
            }
            mx = 0;
            for(i=0; i<=n-1; i++)
            {
                v = a[i]*b[i];
                temp[i] = v;
                mx = Math.Max(mx, Math.Abs(v));
            }
            if( (double)(mx)==(double)(0) )
            {
                r = 0;
                rerr = 0;
                return;
            }
            xsum(ref temp, mx, n, ref r, ref rerr);
        }


        /*************************************************************************
        More precise complex dot-product. Absolute error of  subroutine  result is
        about 1 ulp of max(MX,V), where:
            MX = max( |a[i]*b[i]| )
            V  = |(a,b)|

        INPUT PARAMETERS
            A       -   array[0..N-1], vector 1
            B       -   array[0..N-1], vector 2
            N       -   vectors length, N<2^29.
            Temp    -   array[0..2*N-1], pre-allocated temporary storage

        OUTPUT PARAMETERS
            R       -   (A,B)
            RErr    -   estimate of error. This estimate accounts for both  errors
                        during  calculation  of  (A,B)  and  errors  introduced by
                        rounding of A and B to fit in double (about 1 ulp).

          -- ALGLIB --
             Copyright 27.01.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void xcdot(complex[] a,
            complex[] b,
            int n,
            ref double[] temp,
            ref complex r,
            ref double rerr)
        {
            int i = 0;
            double mx = 0;
            double v = 0;
            double rerrx = 0;
            double rerry = 0;

            r = 0;
            rerr = 0;

            
            //
            // special cases:
            // * N=0
            //
            if( n==0 )
            {
                r = 0;
                rerr = 0;
                return;
            }
            
            //
            // calculate real part
            //
            mx = 0;
            for(i=0; i<=n-1; i++)
            {
                v = a[i].x*b[i].x;
                temp[2*i+0] = v;
                mx = Math.Max(mx, Math.Abs(v));
                v = -(a[i].y*b[i].y);
                temp[2*i+1] = v;
                mx = Math.Max(mx, Math.Abs(v));
            }
            if( (double)(mx)==(double)(0) )
            {
                r.x = 0;
                rerrx = 0;
            }
            else
            {
                xsum(ref temp, mx, 2*n, ref r.x, ref rerrx);
            }
            
            //
            // calculate imaginary part
            //
            mx = 0;
            for(i=0; i<=n-1; i++)
            {
                v = a[i].x*b[i].y;
                temp[2*i+0] = v;
                mx = Math.Max(mx, Math.Abs(v));
                v = a[i].y*b[i].x;
                temp[2*i+1] = v;
                mx = Math.Max(mx, Math.Abs(v));
            }
            if( (double)(mx)==(double)(0) )
            {
                r.y = 0;
                rerry = 0;
            }
            else
            {
                xsum(ref temp, mx, 2*n, ref r.y, ref rerry);
            }
            
            //
            // total error
            //
            if( (double)(rerrx)==(double)(0) & (double)(rerry)==(double)(0) )
            {
                rerr = 0;
            }
            else
            {
                rerr = Math.Max(rerrx, rerry)*Math.Sqrt(1+math.sqr(Math.Min(rerrx, rerry)/Math.Max(rerrx, rerry)));
            }
        }


        /*************************************************************************
        Internal subroutine for extra-precise calculation of SUM(w[i]).

        INPUT PARAMETERS:
            W   -   array[0..N-1], values to be added
                    W is modified during calculations.
            MX  -   max(W[i])
            N   -   array size
            
        OUTPUT PARAMETERS:
            R   -   SUM(w[i])
            RErr-   error estimate for R

          -- ALGLIB --
             Copyright 24.08.2009 by Bochkanov Sergey
        *************************************************************************/
        private static void xsum(ref double[] w,
            double mx,
            int n,
            ref double r,
            ref double rerr)
        {
            int i = 0;
            int k = 0;
            int ks = 0;
            double v = 0;
            double s = 0;
            double ln2 = 0;
            double chunk = 0;
            double invchunk = 0;
            bool allzeros = new bool();
            int i_ = 0;

            r = 0;
            rerr = 0;

            
            //
            // special cases:
            // * N=0
            // * N is too large to use integer arithmetics
            //
            if( n==0 )
            {
                r = 0;
                rerr = 0;
                return;
            }
            if( (double)(mx)==(double)(0) )
            {
                r = 0;
                rerr = 0;
                return;
            }
            ap.assert(n<536870912, "XDot: N is too large!");
            
            //
            // Prepare
            //
            ln2 = Math.Log(2);
            rerr = mx*math.machineepsilon;
            
            //
            // 1. find S such that 0.5<=S*MX<1
            // 2. multiply W by S, so task is normalized in some sense
            // 3. S:=1/S so we can obtain original vector multiplying by S
            //
            k = (int)Math.Round(Math.Log(mx)/ln2);
            s = xfastpow(2, -k);
            while( (double)(s*mx)>=(double)(1) )
            {
                s = 0.5*s;
            }
            while( (double)(s*mx)<(double)(0.5) )
            {
                s = 2*s;
            }
            for(i_=0; i_<=n-1;i_++)
            {
                w[i_] = s*w[i_];
            }
            s = 1/s;
            
            //
            // find Chunk=2^M such that N*Chunk<2^29
            //
            // we have chosen upper limit (2^29) with enough space left
            // to tolerate possible problems with rounding and N's close
            // to the limit, so we don't want to be very strict here.
            //
            k = (int)(Math.Log((double)536870912/(double)n)/ln2);
            chunk = xfastpow(2, k);
            if( (double)(chunk)<(double)(2) )
            {
                chunk = 2;
            }
            invchunk = 1/chunk;
            
            //
            // calculate result
            //
            r = 0;
            for(i_=0; i_<=n-1;i_++)
            {
                w[i_] = chunk*w[i_];
            }
            while( true )
            {
                s = s*invchunk;
                allzeros = true;
                ks = 0;
                for(i=0; i<=n-1; i++)
                {
                    v = w[i];
                    k = (int)(v);
                    if( (double)(v)!=(double)(k) )
                    {
                        allzeros = false;
                    }
                    w[i] = chunk*(v-k);
                    ks = ks+k;
                }
                r = r+s*ks;
                v = Math.Abs(r);
                if( allzeros | (double)(s*n+mx)==(double)(mx) )
                {
                    break;
                }
            }
            
            //
            // correct error
            //
            rerr = Math.Max(rerr, Math.Abs(r)*math.machineepsilon);
        }


        /*************************************************************************
        Fast Pow

          -- ALGLIB --
             Copyright 24.08.2009 by Bochkanov Sergey
        *************************************************************************/
        private static double xfastpow(double r,
            int n)
        {
            double result = 0;

            result = 0;
            if( n>0 )
            {
                if( n%2==0 )
                {
                    result = math.sqr(xfastpow(r, n/2));
                }
                else
                {
                    result = r*xfastpow(r, n-1);
                }
                return result;
            }
            if( n==0 )
            {
                result = 1;
            }
            if( n<0 )
            {
                result = xfastpow(1/r, -n);
            }
            return result;
        }


    }
    public class linmin
    {
        public class linminstate
        {
            public bool brackt;
            public bool stage1;
            public int infoc;
            public double dg;
            public double dgm;
            public double dginit;
            public double dgtest;
            public double dgx;
            public double dgxm;
            public double dgy;
            public double dgym;
            public double finit;
            public double ftest1;
            public double fm;
            public double fx;
            public double fxm;
            public double fy;
            public double fym;
            public double stx;
            public double sty;
            public double stmin;
            public double stmax;
            public double width;
            public double width1;
            public double xtrapf;
        };


        public class armijostate
        {
            public bool needf;
            public double[] x;
            public double f;
            public int n;
            public double[] xbase;
            public double[] s;
            public double stplen;
            public double fcur;
            public double stpmax;
            public int fmax;
            public int nfev;
            public int info;
            public rcommstate rstate;
            public armijostate()
            {
                x = new double[0];
                xbase = new double[0];
                s = new double[0];
                rstate = new rcommstate();
            }
        };




        public const double ftol = 0.001;
        public const double xtol = 100*math.machineepsilon;
        public const int maxfev = 20;
        public const double stpmin = 1.0E-50;
        public const double defstpmax = 1.0E+50;
        public const double armijofactor = 1.3;


        /*************************************************************************
        Normalizes direction/step pair: makes |D|=1, scales Stp.
        If |D|=0, it returns, leavind D/Stp unchanged.

          -- ALGLIB --
             Copyright 01.04.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void linminnormalized(ref double[] d,
            ref double stp,
            int n)
        {
            double mx = 0;
            double s = 0;
            int i = 0;
            int i_ = 0;

            
            //
            // first, scale D to avoid underflow/overflow durng squaring
            //
            mx = 0;
            for(i=0; i<=n-1; i++)
            {
                mx = Math.Max(mx, Math.Abs(d[i]));
            }
            if( (double)(mx)==(double)(0) )
            {
                return;
            }
            s = 1/mx;
            for(i_=0; i_<=n-1;i_++)
            {
                d[i_] = s*d[i_];
            }
            stp = stp/s;
            
            //
            // normalize D
            //
            s = 0.0;
            for(i_=0; i_<=n-1;i_++)
            {
                s += d[i_]*d[i_];
            }
            s = 1/Math.Sqrt(s);
            for(i_=0; i_<=n-1;i_++)
            {
                d[i_] = s*d[i_];
            }
            stp = stp/s;
        }


        /*************************************************************************
        THE  PURPOSE  OF  MCSRCH  IS  TO  FIND A STEP WHICH SATISFIES A SUFFICIENT
        DECREASE CONDITION AND A CURVATURE CONDITION.

        AT EACH STAGE THE SUBROUTINE  UPDATES  AN  INTERVAL  OF  UNCERTAINTY  WITH
        ENDPOINTS  STX  AND  STY.  THE INTERVAL OF UNCERTAINTY IS INITIALLY CHOSEN
        SO THAT IT CONTAINS A MINIMIZER OF THE MODIFIED FUNCTION

            F(X+STP*S) - F(X) - FTOL*STP*(GRADF(X)'S).

        IF  A STEP  IS OBTAINED FOR  WHICH THE MODIFIED FUNCTION HAS A NONPOSITIVE
        FUNCTION  VALUE  AND  NONNEGATIVE  DERIVATIVE,   THEN   THE   INTERVAL  OF
        UNCERTAINTY IS CHOSEN SO THAT IT CONTAINS A MINIMIZER OF F(X+STP*S).

        THE  ALGORITHM  IS  DESIGNED TO FIND A STEP WHICH SATISFIES THE SUFFICIENT
        DECREASE CONDITION

            F(X+STP*S) .LE. F(X) + FTOL*STP*(GRADF(X)'S),

        AND THE CURVATURE CONDITION

            ABS(GRADF(X+STP*S)'S)) .LE. GTOL*ABS(GRADF(X)'S).

        IF  FTOL  IS  LESS  THAN GTOL AND IF, FOR EXAMPLE, THE FUNCTION IS BOUNDED
        BELOW,  THEN  THERE  IS  ALWAYS  A  STEP  WHICH SATISFIES BOTH CONDITIONS.
        IF  NO  STEP  CAN BE FOUND  WHICH  SATISFIES  BOTH  CONDITIONS,  THEN  THE
        ALGORITHM  USUALLY STOPS  WHEN  ROUNDING ERRORS  PREVENT FURTHER PROGRESS.
        IN THIS CASE STP ONLY SATISFIES THE SUFFICIENT DECREASE CONDITION.


        :::::::::::::IMPORTANT NOTES:::::::::::::

        NOTE 1:

        This routine  guarantees that it will stop at the last point where function
        value was calculated. It won't make several additional function evaluations
        after finding good point. So if you store function evaluations requested by
        this routine, you can be sure that last one is the point where we've stopped.

        NOTE 2:

        when 0<StpMax<StpMin, algorithm will terminate with INFO=5 and Stp=0.0
        :::::::::::::::::::::::::::::::::::::::::


        PARAMETERS DESCRIPRION

        STAGE IS ZERO ON FIRST CALL, ZERO ON FINAL EXIT

        N IS A POSITIVE INTEGER INPUT VARIABLE SET TO THE NUMBER OF VARIABLES.

        X IS  AN  ARRAY  OF  LENGTH N. ON INPUT IT MUST CONTAIN THE BASE POINT FOR
        THE LINE SEARCH. ON OUTPUT IT CONTAINS X+STP*S.

        F IS  A  VARIABLE. ON INPUT IT MUST CONTAIN THE VALUE OF F AT X. ON OUTPUT
        IT CONTAINS THE VALUE OF F AT X + STP*S.

        G IS AN ARRAY OF LENGTH N. ON INPUT IT MUST CONTAIN THE GRADIENT OF F AT X.
        ON OUTPUT IT CONTAINS THE GRADIENT OF F AT X + STP*S.

        S IS AN INPUT ARRAY OF LENGTH N WHICH SPECIFIES THE SEARCH DIRECTION.

        STP  IS  A NONNEGATIVE VARIABLE. ON INPUT STP CONTAINS AN INITIAL ESTIMATE
        OF A SATISFACTORY STEP. ON OUTPUT STP CONTAINS THE FINAL ESTIMATE.

        FTOL AND GTOL ARE NONNEGATIVE INPUT VARIABLES. TERMINATION OCCURS WHEN THE
        SUFFICIENT DECREASE CONDITION AND THE DIRECTIONAL DERIVATIVE CONDITION ARE
        SATISFIED.

        XTOL IS A NONNEGATIVE INPUT VARIABLE. TERMINATION OCCURS WHEN THE RELATIVE
        WIDTH OF THE INTERVAL OF UNCERTAINTY IS AT MOST XTOL.

        STPMIN AND STPMAX ARE NONNEGATIVE INPUT VARIABLES WHICH SPECIFY LOWER  AND
        UPPER BOUNDS FOR THE STEP.

        MAXFEV IS A POSITIVE INTEGER INPUT VARIABLE. TERMINATION OCCURS WHEN THE
        NUMBER OF CALLS TO FCN IS AT LEAST MAXFEV BY THE END OF AN ITERATION.

        INFO IS AN INTEGER OUTPUT VARIABLE SET AS FOLLOWS:
            INFO = 0  IMPROPER INPUT PARAMETERS.

            INFO = 1  THE SUFFICIENT DECREASE CONDITION AND THE
                      DIRECTIONAL DERIVATIVE CONDITION HOLD.

            INFO = 2  RELATIVE WIDTH OF THE INTERVAL OF UNCERTAINTY
                      IS AT MOST XTOL.

            INFO = 3  NUMBER OF CALLS TO FCN HAS REACHED MAXFEV.

            INFO = 4  THE STEP IS AT THE LOWER BOUND STPMIN.

            INFO = 5  THE STEP IS AT THE UPPER BOUND STPMAX.

            INFO = 6  ROUNDING ERRORS PREVENT FURTHER PROGRESS.
                      THERE MAY NOT BE A STEP WHICH SATISFIES THE
                      SUFFICIENT DECREASE AND CURVATURE CONDITIONS.
                      TOLERANCES MAY BE TOO SMALL.

        NFEV IS AN INTEGER OUTPUT VARIABLE SET TO THE NUMBER OF CALLS TO FCN.

        WA IS A WORK ARRAY OF LENGTH N.

        ARGONNE NATIONAL LABORATORY. MINPACK PROJECT. JUNE 1983
        JORGE J. MORE', DAVID J. THUENTE
        *************************************************************************/
        public static void mcsrch(int n,
            ref double[] x,
            ref double f,
            ref double[] g,
            double[] s,
            ref double stp,
            double stpmax,
            double gtol,
            ref int info,
            ref int nfev,
            ref double[] wa,
            linminstate state,
            ref int stage)
        {
            double v = 0;
            double p5 = 0;
            double p66 = 0;
            double zero = 0;
            int i_ = 0;

            
            //
            // init
            //
            p5 = 0.5;
            p66 = 0.66;
            state.xtrapf = 4.0;
            zero = 0;
            if( (double)(stpmax)==(double)(0) )
            {
                stpmax = defstpmax;
            }
            if( (double)(stp)<(double)(stpmin) )
            {
                stp = stpmin;
            }
            if( (double)(stp)>(double)(stpmax) )
            {
                stp = stpmax;
            }
            
            //
            // Main cycle
            //
            while( true )
            {
                if( stage==0 )
                {
                    
                    //
                    // NEXT
                    //
                    stage = 2;
                    continue;
                }
                if( stage==2 )
                {
                    state.infoc = 1;
                    info = 0;
                    
                    //
                    //     CHECK THE INPUT PARAMETERS FOR ERRORS.
                    //
                    if( (double)(stpmax)<(double)(stpmin) & (double)(stpmax)>(double)(0) )
                    {
                        info = 5;
                        stp = 0.0;
                        return;
                    }
                    if( ((((((n<=0 | (double)(stp)<=(double)(0)) | (double)(ftol)<(double)(0)) | (double)(gtol)<(double)(zero)) | (double)(xtol)<(double)(zero)) | (double)(stpmin)<(double)(zero)) | (double)(stpmax)<(double)(stpmin)) | maxfev<=0 )
                    {
                        stage = 0;
                        return;
                    }
                    
                    //
                    //     COMPUTE THE INITIAL GRADIENT IN THE SEARCH DIRECTION
                    //     AND CHECK THAT S IS A DESCENT DIRECTION.
                    //
                    v = 0.0;
                    for(i_=0; i_<=n-1;i_++)
                    {
                        v += g[i_]*s[i_];
                    }
                    state.dginit = v;
                    if( (double)(state.dginit)>=(double)(0) )
                    {
                        stage = 0;
                        return;
                    }
                    
                    //
                    //     INITIALIZE LOCAL VARIABLES.
                    //
                    state.brackt = false;
                    state.stage1 = true;
                    nfev = 0;
                    state.finit = f;
                    state.dgtest = ftol*state.dginit;
                    state.width = stpmax-stpmin;
                    state.width1 = state.width/p5;
                    for(i_=0; i_<=n-1;i_++)
                    {
                        wa[i_] = x[i_];
                    }
                    
                    //
                    //     THE VARIABLES STX, FX, DGX CONTAIN THE VALUES OF THE STEP,
                    //     FUNCTION, AND DIRECTIONAL DERIVATIVE AT THE BEST STEP.
                    //     THE VARIABLES STY, FY, DGY CONTAIN THE VALUE OF THE STEP,
                    //     FUNCTION, AND DERIVATIVE AT THE OTHER ENDPOINT OF
                    //     THE INTERVAL OF UNCERTAINTY.
                    //     THE VARIABLES STP, F, DG CONTAIN THE VALUES OF THE STEP,
                    //     FUNCTION, AND DERIVATIVE AT THE CURRENT STEP.
                    //
                    state.stx = 0;
                    state.fx = state.finit;
                    state.dgx = state.dginit;
                    state.sty = 0;
                    state.fy = state.finit;
                    state.dgy = state.dginit;
                    
                    //
                    // NEXT
                    //
                    stage = 3;
                    continue;
                }
                if( stage==3 )
                {
                    
                    //
                    //     START OF ITERATION.
                    //
                    //     SET THE MINIMUM AND MAXIMUM STEPS TO CORRESPOND
                    //     TO THE PRESENT INTERVAL OF UNCERTAINTY.
                    //
                    if( state.brackt )
                    {
                        if( (double)(state.stx)<(double)(state.sty) )
                        {
                            state.stmin = state.stx;
                            state.stmax = state.sty;
                        }
                        else
                        {
                            state.stmin = state.sty;
                            state.stmax = state.stx;
                        }
                    }
                    else
                    {
                        state.stmin = state.stx;
                        state.stmax = stp+state.xtrapf*(stp-state.stx);
                    }
                    
                    //
                    //        FORCE THE STEP TO BE WITHIN THE BOUNDS STPMAX AND STPMIN.
                    //
                    if( (double)(stp)>(double)(stpmax) )
                    {
                        stp = stpmax;
                    }
                    if( (double)(stp)<(double)(stpmin) )
                    {
                        stp = stpmin;
                    }
                    
                    //
                    //        IF AN UNUSUAL TERMINATION IS TO OCCUR THEN LET
                    //        STP BE THE LOWEST POINT OBTAINED SO FAR.
                    //
                    if( (((state.brackt & ((double)(stp)<=(double)(state.stmin) | (double)(stp)>=(double)(state.stmax))) | nfev>=maxfev-1) | state.infoc==0) | (state.brackt & (double)(state.stmax-state.stmin)<=(double)(xtol*state.stmax)) )
                    {
                        stp = state.stx;
                    }
                    
                    //
                    //        EVALUATE THE FUNCTION AND GRADIENT AT STP
                    //        AND COMPUTE THE DIRECTIONAL DERIVATIVE.
                    //
                    for(i_=0; i_<=n-1;i_++)
                    {
                        x[i_] = wa[i_];
                    }
                    for(i_=0; i_<=n-1;i_++)
                    {
                        x[i_] = x[i_] + stp*s[i_];
                    }
                    
                    //
                    // NEXT
                    //
                    stage = 4;
                    return;
                }
                if( stage==4 )
                {
                    info = 0;
                    nfev = nfev+1;
                    v = 0.0;
                    for(i_=0; i_<=n-1;i_++)
                    {
                        v += g[i_]*s[i_];
                    }
                    state.dg = v;
                    state.ftest1 = state.finit+stp*state.dgtest;
                    
                    //
                    //        TEST FOR CONVERGENCE.
                    //
                    if( (state.brackt & ((double)(stp)<=(double)(state.stmin) | (double)(stp)>=(double)(state.stmax))) | state.infoc==0 )
                    {
                        info = 6;
                    }
                    if( ((double)(stp)==(double)(stpmax) & (double)(f)<=(double)(state.ftest1)) & (double)(state.dg)<=(double)(state.dgtest) )
                    {
                        info = 5;
                    }
                    if( (double)(stp)==(double)(stpmin) & ((double)(f)>(double)(state.ftest1) | (double)(state.dg)>=(double)(state.dgtest)) )
                    {
                        info = 4;
                    }
                    if( nfev>=maxfev )
                    {
                        info = 3;
                    }
                    if( state.brackt & (double)(state.stmax-state.stmin)<=(double)(xtol*state.stmax) )
                    {
                        info = 2;
                    }
                    if( (double)(f)<=(double)(state.ftest1) & (double)(Math.Abs(state.dg))<=(double)(-(gtol*state.dginit)) )
                    {
                        info = 1;
                    }
                    
                    //
                    //        CHECK FOR TERMINATION.
                    //
                    if( info!=0 )
                    {
                        stage = 0;
                        return;
                    }
                    
                    //
                    //        IN THE FIRST STAGE WE SEEK A STEP FOR WHICH THE MODIFIED
                    //        FUNCTION HAS A NONPOSITIVE VALUE AND NONNEGATIVE DERIVATIVE.
                    //
                    if( (state.stage1 & (double)(f)<=(double)(state.ftest1)) & (double)(state.dg)>=(double)(Math.Min(ftol, gtol)*state.dginit) )
                    {
                        state.stage1 = false;
                    }
                    
                    //
                    //        A MODIFIED FUNCTION IS USED TO PREDICT THE STEP ONLY IF
                    //        WE HAVE NOT OBTAINED A STEP FOR WHICH THE MODIFIED
                    //        FUNCTION HAS A NONPOSITIVE FUNCTION VALUE AND NONNEGATIVE
                    //        DERIVATIVE, AND IF A LOWER FUNCTION VALUE HAS BEEN
                    //        OBTAINED BUT THE DECREASE IS NOT SUFFICIENT.
                    //
                    if( (state.stage1 & (double)(f)<=(double)(state.fx)) & (double)(f)>(double)(state.ftest1) )
                    {
                        
                        //
                        //           DEFINE THE MODIFIED FUNCTION AND DERIVATIVE VALUES.
                        //
                        state.fm = f-stp*state.dgtest;
                        state.fxm = state.fx-state.stx*state.dgtest;
                        state.fym = state.fy-state.sty*state.dgtest;
                        state.dgm = state.dg-state.dgtest;
                        state.dgxm = state.dgx-state.dgtest;
                        state.dgym = state.dgy-state.dgtest;
                        
                        //
                        //           CALL CSTEP TO UPDATE THE INTERVAL OF UNCERTAINTY
                        //           AND TO COMPUTE THE NEW STEP.
                        //
                        mcstep(ref state.stx, ref state.fxm, ref state.dgxm, ref state.sty, ref state.fym, ref state.dgym, ref stp, state.fm, state.dgm, ref state.brackt, state.stmin, state.stmax, ref state.infoc);
                        
                        //
                        //           RESET THE FUNCTION AND GRADIENT VALUES FOR F.
                        //
                        state.fx = state.fxm+state.stx*state.dgtest;
                        state.fy = state.fym+state.sty*state.dgtest;
                        state.dgx = state.dgxm+state.dgtest;
                        state.dgy = state.dgym+state.dgtest;
                    }
                    else
                    {
                        
                        //
                        //           CALL MCSTEP TO UPDATE THE INTERVAL OF UNCERTAINTY
                        //           AND TO COMPUTE THE NEW STEP.
                        //
                        mcstep(ref state.stx, ref state.fx, ref state.dgx, ref state.sty, ref state.fy, ref state.dgy, ref stp, f, state.dg, ref state.brackt, state.stmin, state.stmax, ref state.infoc);
                    }
                    
                    //
                    //        FORCE A SUFFICIENT DECREASE IN THE SIZE OF THE
                    //        INTERVAL OF UNCERTAINTY.
                    //
                    if( state.brackt )
                    {
                        if( (double)(Math.Abs(state.sty-state.stx))>=(double)(p66*state.width1) )
                        {
                            stp = state.stx+p5*(state.sty-state.stx);
                        }
                        state.width1 = state.width;
                        state.width = Math.Abs(state.sty-state.stx);
                    }
                    
                    //
                    //  NEXT.
                    //
                    stage = 3;
                    continue;
                }
            }
        }


        /*************************************************************************
        These functions perform Armijo line search using  at  most  FMAX  function
        evaluations.  It  doesn't  enforce  some  kind  of  " sufficient decrease"
        criterion - it just tries different Armijo steps and returns optimum found
        so far.

        Optimization is done using F-rcomm interface:
        * ArmijoCreate initializes State structure
          (reusing previously allocated buffers)
        * ArmijoIteration is subsequently called
        * ArmijoResults returns results

        INPUT PARAMETERS:
            N       -   problem size
            X       -   array[N], starting point
            F       -   F(X+S*STP)
            S       -   step direction, S>0
            STP     -   step length
            STPMAX  -   maximum value for STP or zero (if no limit is imposed)
            FMAX    -   maximum number of function evaluations
            State   -   optimization state

          -- ALGLIB --
             Copyright 05.10.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void armijocreate(int n,
            double[] x,
            double f,
            double[] s,
            double stp,
            double stpmax,
            int fmax,
            armijostate state)
        {
            int i_ = 0;

            if( ap.len(state.x)<n )
            {
                state.x = new double[n];
            }
            if( ap.len(state.xbase)<n )
            {
                state.xbase = new double[n];
            }
            if( ap.len(state.s)<n )
            {
                state.s = new double[n];
            }
            state.stpmax = stpmax;
            state.fmax = fmax;
            state.stplen = stp;
            state.fcur = f;
            state.n = n;
            for(i_=0; i_<=n-1;i_++)
            {
                state.xbase[i_] = x[i_];
            }
            for(i_=0; i_<=n-1;i_++)
            {
                state.s[i_] = s[i_];
            }
            state.rstate.ia = new int[0+1];
            state.rstate.ra = new double[0+1];
            state.rstate.stage = -1;
        }


        /*************************************************************************
        This is rcomm-based search function

          -- ALGLIB --
             Copyright 05.10.2010 by Bochkanov Sergey
        *************************************************************************/
        public static bool armijoiteration(armijostate state)
        {
            bool result = new bool();
            double v = 0;
            int n = 0;
            int i_ = 0;

            
            //
            // Reverse communication preparations
            // I know it looks ugly, but it works the same way
            // anywhere from C++ to Python.
            //
            // This code initializes locals by:
            // * random values determined during code
            //   generation - on first subroutine call
            // * values from previous call - on subsequent calls
            //
            if( state.rstate.stage>=0 )
            {
                n = state.rstate.ia[0];
                v = state.rstate.ra[0];
            }
            else
            {
                n = -983;
                v = -989;
            }
            if( state.rstate.stage==0 )
            {
                goto lbl_0;
            }
            if( state.rstate.stage==1 )
            {
                goto lbl_1;
            }
            if( state.rstate.stage==2 )
            {
                goto lbl_2;
            }
            if( state.rstate.stage==3 )
            {
                goto lbl_3;
            }
            
            //
            // Routine body
            //
            if( ((double)(state.stplen)<=(double)(0) | (double)(state.stpmax)<(double)(0)) | state.fmax<2 )
            {
                state.info = 0;
                result = false;
                return result;
            }
            if( (double)(state.stplen)<=(double)(stpmin) )
            {
                state.info = 4;
                result = false;
                return result;
            }
            n = state.n;
            state.nfev = 0;
            
            //
            // We always need F
            //
            state.needf = true;
            
            //
            // Bound StpLen
            //
            if( (double)(state.stplen)>(double)(state.stpmax) & (double)(state.stpmax)!=(double)(0) )
            {
                state.stplen = state.stpmax;
            }
            
            //
            // Increase length
            //
            v = state.stplen*armijofactor;
            if( (double)(v)>(double)(state.stpmax) & (double)(state.stpmax)!=(double)(0) )
            {
                v = state.stpmax;
            }
            for(i_=0; i_<=n-1;i_++)
            {
                state.x[i_] = state.xbase[i_];
            }
            for(i_=0; i_<=n-1;i_++)
            {
                state.x[i_] = state.x[i_] + v*state.s[i_];
            }
            state.rstate.stage = 0;
            goto lbl_rcomm;
        lbl_0:
            state.nfev = state.nfev+1;
            if( (double)(state.f)>=(double)(state.fcur) )
            {
                goto lbl_4;
            }
            state.stplen = v;
            state.fcur = state.f;
        lbl_6:
            if( false )
            {
                goto lbl_7;
            }
            
            //
            // test stopping conditions
            //
            if( state.nfev>=state.fmax )
            {
                state.info = 3;
                result = false;
                return result;
            }
            if( (double)(state.stplen)>=(double)(state.stpmax) )
            {
                state.info = 5;
                result = false;
                return result;
            }
            
            //
            // evaluate F
            //
            v = state.stplen*armijofactor;
            if( (double)(v)>(double)(state.stpmax) & (double)(state.stpmax)!=(double)(0) )
            {
                v = state.stpmax;
            }
            for(i_=0; i_<=n-1;i_++)
            {
                state.x[i_] = state.xbase[i_];
            }
            for(i_=0; i_<=n-1;i_++)
            {
                state.x[i_] = state.x[i_] + v*state.s[i_];
            }
            state.rstate.stage = 1;
            goto lbl_rcomm;
        lbl_1:
            state.nfev = state.nfev+1;
            
            //
            // make decision
            //
            if( (double)(state.f)<(double)(state.fcur) )
            {
                state.stplen = v;
                state.fcur = state.f;
            }
            else
            {
                state.info = 1;
                result = false;
                return result;
            }
            goto lbl_6;
        lbl_7:
        lbl_4:
            
            //
            // Decrease length
            //
            v = state.stplen/armijofactor;
            for(i_=0; i_<=n-1;i_++)
            {
                state.x[i_] = state.xbase[i_];
            }
            for(i_=0; i_<=n-1;i_++)
            {
                state.x[i_] = state.x[i_] + v*state.s[i_];
            }
            state.rstate.stage = 2;
            goto lbl_rcomm;
        lbl_2:
            state.nfev = state.nfev+1;
            if( (double)(state.f)>=(double)(state.fcur) )
            {
                goto lbl_8;
            }
            state.stplen = state.stplen/armijofactor;
            state.fcur = state.f;
        lbl_10:
            if( false )
            {
                goto lbl_11;
            }
            
            //
            // test stopping conditions
            //
            if( state.nfev>=state.fmax )
            {
                state.info = 3;
                result = false;
                return result;
            }
            if( (double)(state.stplen)<=(double)(stpmin) )
            {
                state.info = 4;
                result = false;
                return result;
            }
            
            //
            // evaluate F
            //
            v = state.stplen/armijofactor;
            for(i_=0; i_<=n-1;i_++)
            {
                state.x[i_] = state.xbase[i_];
            }
            for(i_=0; i_<=n-1;i_++)
            {
                state.x[i_] = state.x[i_] + v*state.s[i_];
            }
            state.rstate.stage = 3;
            goto lbl_rcomm;
        lbl_3:
            state.nfev = state.nfev+1;
            
            //
            // make decision
            //
            if( (double)(state.f)<(double)(state.fcur) )
            {
                state.stplen = state.stplen/armijofactor;
                state.fcur = state.f;
            }
            else
            {
                state.info = 1;
                result = false;
                return result;
            }
            goto lbl_10;
        lbl_11:
        lbl_8:
            
            //
            // Nothing to be done
            //
            state.info = 1;
            result = false;
            return result;
            
            //
            // Saving state
            //
        lbl_rcomm:
            result = true;
            state.rstate.ia[0] = n;
            state.rstate.ra[0] = v;
            return result;
        }


        /*************************************************************************
        Results of Armijo search

        OUTPUT PARAMETERS:
            INFO    -   on output it is set to one of the return codes:
                        * 0     improper input params
                        * 1     optimum step is found with at most FMAX evaluations
                        * 3     FMAX evaluations were used,
                                X contains optimum found so far
                        * 4     step is at lower bound STPMIN
                        * 5     step is at upper bound
            STP     -   step length (in case of failure it is still returned)
            F       -   function value (in case of failure it is still returned)

          -- ALGLIB --
             Copyright 05.10.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void armijoresults(armijostate state,
            ref int info,
            ref double stp,
            ref double f)
        {
            info = state.info;
            stp = state.stplen;
            f = state.fcur;
        }


        private static void mcstep(ref double stx,
            ref double fx,
            ref double dx,
            ref double sty,
            ref double fy,
            ref double dy,
            ref double stp,
            double fp,
            double dp,
            ref bool brackt,
            double stmin,
            double stmax,
            ref int info)
        {
            bool bound = new bool();
            double gamma = 0;
            double p = 0;
            double q = 0;
            double r = 0;
            double s = 0;
            double sgnd = 0;
            double stpc = 0;
            double stpf = 0;
            double stpq = 0;
            double theta = 0;

            info = 0;
            
            //
            //     CHECK THE INPUT PARAMETERS FOR ERRORS.
            //
            if( ((brackt & ((double)(stp)<=(double)(Math.Min(stx, sty)) | (double)(stp)>=(double)(Math.Max(stx, sty)))) | (double)(dx*(stp-stx))>=(double)(0)) | (double)(stmax)<(double)(stmin) )
            {
                return;
            }
            
            //
            //     DETERMINE IF THE DERIVATIVES HAVE OPPOSITE SIGN.
            //
            sgnd = dp*(dx/Math.Abs(dx));
            
            //
            //     FIRST CASE. A HIGHER FUNCTION VALUE.
            //     THE MINIMUM IS BRACKETED. IF THE CUBIC STEP IS CLOSER
            //     TO STX THAN THE QUADRATIC STEP, THE CUBIC STEP IS TAKEN,
            //     ELSE THE AVERAGE OF THE CUBIC AND QUADRATIC STEPS IS TAKEN.
            //
            if( (double)(fp)>(double)(fx) )
            {
                info = 1;
                bound = true;
                theta = 3*(fx-fp)/(stp-stx)+dx+dp;
                s = Math.Max(Math.Abs(theta), Math.Max(Math.Abs(dx), Math.Abs(dp)));
                gamma = s*Math.Sqrt(math.sqr(theta/s)-dx/s*(dp/s));
                if( (double)(stp)<(double)(stx) )
                {
                    gamma = -gamma;
                }
                p = gamma-dx+theta;
                q = gamma-dx+gamma+dp;
                r = p/q;
                stpc = stx+r*(stp-stx);
                stpq = stx+dx/((fx-fp)/(stp-stx)+dx)/2*(stp-stx);
                if( (double)(Math.Abs(stpc-stx))<(double)(Math.Abs(stpq-stx)) )
                {
                    stpf = stpc;
                }
                else
                {
                    stpf = stpc+(stpq-stpc)/2;
                }
                brackt = true;
            }
            else
            {
                if( (double)(sgnd)<(double)(0) )
                {
                    
                    //
                    //     SECOND CASE. A LOWER FUNCTION VALUE AND DERIVATIVES OF
                    //     OPPOSITE SIGN. THE MINIMUM IS BRACKETED. IF THE CUBIC
                    //     STEP IS CLOSER TO STX THAN THE QUADRATIC (SECANT) STEP,
                    //     THE CUBIC STEP IS TAKEN, ELSE THE QUADRATIC STEP IS TAKEN.
                    //
                    info = 2;
                    bound = false;
                    theta = 3*(fx-fp)/(stp-stx)+dx+dp;
                    s = Math.Max(Math.Abs(theta), Math.Max(Math.Abs(dx), Math.Abs(dp)));
                    gamma = s*Math.Sqrt(math.sqr(theta/s)-dx/s*(dp/s));
                    if( (double)(stp)>(double)(stx) )
                    {
                        gamma = -gamma;
                    }
                    p = gamma-dp+theta;
                    q = gamma-dp+gamma+dx;
                    r = p/q;
                    stpc = stp+r*(stx-stp);
                    stpq = stp+dp/(dp-dx)*(stx-stp);
                    if( (double)(Math.Abs(stpc-stp))>(double)(Math.Abs(stpq-stp)) )
                    {
                        stpf = stpc;
                    }
                    else
                    {
                        stpf = stpq;
                    }
                    brackt = true;
                }
                else
                {
                    if( (double)(Math.Abs(dp))<(double)(Math.Abs(dx)) )
                    {
                        
                        //
                        //     THIRD CASE. A LOWER FUNCTION VALUE, DERIVATIVES OF THE
                        //     SAME SIGN, AND THE MAGNITUDE OF THE DERIVATIVE DECREASES.
                        //     THE CUBIC STEP IS ONLY USED IF THE CUBIC TENDS TO INFINITY
                        //     IN THE DIRECTION OF THE STEP OR IF THE MINIMUM OF THE CUBIC
                        //     IS BEYOND STP. OTHERWISE THE CUBIC STEP IS DEFINED TO BE
                        //     EITHER STPMIN OR STPMAX. THE QUADRATIC (SECANT) STEP IS ALSO
                        //     COMPUTED AND IF THE MINIMUM IS BRACKETED THEN THE THE STEP
                        //     CLOSEST TO STX IS TAKEN, ELSE THE STEP FARTHEST AWAY IS TAKEN.
                        //
                        info = 3;
                        bound = true;
                        theta = 3*(fx-fp)/(stp-stx)+dx+dp;
                        s = Math.Max(Math.Abs(theta), Math.Max(Math.Abs(dx), Math.Abs(dp)));
                        
                        //
                        //        THE CASE GAMMA = 0 ONLY ARISES IF THE CUBIC DOES NOT TEND
                        //        TO INFINITY IN THE DIRECTION OF THE STEP.
                        //
                        gamma = s*Math.Sqrt(Math.Max(0, math.sqr(theta/s)-dx/s*(dp/s)));
                        if( (double)(stp)>(double)(stx) )
                        {
                            gamma = -gamma;
                        }
                        p = gamma-dp+theta;
                        q = gamma+(dx-dp)+gamma;
                        r = p/q;
                        if( (double)(r)<(double)(0) & (double)(gamma)!=(double)(0) )
                        {
                            stpc = stp+r*(stx-stp);
                        }
                        else
                        {
                            if( (double)(stp)>(double)(stx) )
                            {
                                stpc = stmax;
                            }
                            else
                            {
                                stpc = stmin;
                            }
                        }
                        stpq = stp+dp/(dp-dx)*(stx-stp);
                        if( brackt )
                        {
                            if( (double)(Math.Abs(stp-stpc))<(double)(Math.Abs(stp-stpq)) )
                            {
                                stpf = stpc;
                            }
                            else
                            {
                                stpf = stpq;
                            }
                        }
                        else
                        {
                            if( (double)(Math.Abs(stp-stpc))>(double)(Math.Abs(stp-stpq)) )
                            {
                                stpf = stpc;
                            }
                            else
                            {
                                stpf = stpq;
                            }
                        }
                    }
                    else
                    {
                        
                        //
                        //     FOURTH CASE. A LOWER FUNCTION VALUE, DERIVATIVES OF THE
                        //     SAME SIGN, AND THE MAGNITUDE OF THE DERIVATIVE DOES
                        //     NOT DECREASE. IF THE MINIMUM IS NOT BRACKETED, THE STEP
                        //     IS EITHER STPMIN OR STPMAX, ELSE THE CUBIC STEP IS TAKEN.
                        //
                        info = 4;
                        bound = false;
                        if( brackt )
                        {
                            theta = 3*(fp-fy)/(sty-stp)+dy+dp;
                            s = Math.Max(Math.Abs(theta), Math.Max(Math.Abs(dy), Math.Abs(dp)));
                            gamma = s*Math.Sqrt(math.sqr(theta/s)-dy/s*(dp/s));
                            if( (double)(stp)>(double)(sty) )
                            {
                                gamma = -gamma;
                            }
                            p = gamma-dp+theta;
                            q = gamma-dp+gamma+dy;
                            r = p/q;
                            stpc = stp+r*(sty-stp);
                            stpf = stpc;
                        }
                        else
                        {
                            if( (double)(stp)>(double)(stx) )
                            {
                                stpf = stmax;
                            }
                            else
                            {
                                stpf = stmin;
                            }
                        }
                    }
                }
            }
            
            //
            //     UPDATE THE INTERVAL OF UNCERTAINTY. THIS UPDATE DOES NOT
            //     DEPEND ON THE NEW STEP OR THE CASE ANALYSIS ABOVE.
            //
            if( (double)(fp)>(double)(fx) )
            {
                sty = stp;
                fy = fp;
                dy = dp;
            }
            else
            {
                if( (double)(sgnd)<(double)(0.0) )
                {
                    sty = stx;
                    fy = fx;
                    dy = dx;
                }
                stx = stp;
                fx = fp;
                dx = dp;
            }
            
            //
            //     COMPUTE THE NEW STEP AND SAFEGUARD IT.
            //
            stpf = Math.Min(stmax, stpf);
            stpf = Math.Max(stmin, stpf);
            stp = stpf;
            if( brackt & bound )
            {
                if( (double)(sty)>(double)(stx) )
                {
                    stp = Math.Min(stx+0.66*(sty-stx), stp);
                }
                else
                {
                    stp = Math.Max(stx+0.66*(sty-stx), stp);
                }
            }
        }


    }
    public class optserv
    {
        /*************************************************************************
        This subroutine is used to prepare threshold value which will be used for
        trimming of the target function (see comments on TrimFunction() for more
        information).

        This function accepts only one parameter: function value at the starting
        point. It returns threshold which will be used for trimming.

          -- ALGLIB --
             Copyright 10.05.2011 by Bochkanov Sergey
        *************************************************************************/
        public static void trimprepare(double f,
            ref double threshold)
        {
            threshold = 0;

            threshold = 10*(Math.Abs(f)+1);
        }


        /*************************************************************************
        This subroutine is used to "trim" target function, i.e. to do following
        transformation:

                           { {F,G}          if F<Threshold
            {F_tr, G_tr} = {
                           { {Threshold, 0} if F>=Threshold
                           
        Such transformation allows us to  solve  problems  with  singularities  by
        redefining function in such way that it becomes bounded from above.

          -- ALGLIB --
             Copyright 10.05.2011 by Bochkanov Sergey
        *************************************************************************/
        public static void trimfunction(ref double f,
            ref double[] g,
            int n,
            double threshold)
        {
            int i = 0;

            if( (double)(f)>=(double)(threshold) )
            {
                f = threshold;
                for(i=0; i<=n-1; i++)
                {
                    g[i] = 0.0;
                }
            }
        }


    }
    public class ftbase
    {
        public class ftplan
        {
            public int[] plan;
            public double[] precomputed;
            public double[] tmpbuf;
            public double[] stackbuf;
            public ftplan()
            {
                plan = new int[0];
                precomputed = new double[0];
                tmpbuf = new double[0];
                stackbuf = new double[0];
            }
        };




        public const int ftbaseplanentrysize = 8;
        public const int ftbasecffttask = 0;
        public const int ftbaserfhttask = 1;
        public const int ftbaserffttask = 2;
        public const int fftcooleytukeyplan = 0;
        public const int fftbluesteinplan = 1;
        public const int fftcodeletplan = 2;
        public const int fhtcooleytukeyplan = 3;
        public const int fhtcodeletplan = 4;
        public const int fftrealcooleytukeyplan = 5;
        public const int fftemptyplan = 6;
        public const int fhtn2plan = 999;
        public const int ftbaseupdatetw = 4;
        public const int ftbasecodeletrecommended = 5;
        public const double ftbaseinefficiencyfactor = 1.3;
        public const int ftbasemaxsmoothfactor = 5;


        /*************************************************************************
        This subroutine generates FFT plan - a decomposition of a N-length FFT to
        the more simpler operations. Plan consists of the root entry and the child
        entries.

        Subroutine parameters:
            N               task size
            
        Output parameters:
            Plan            plan

          -- ALGLIB --
             Copyright 01.05.2009 by Bochkanov Sergey
        *************************************************************************/
        public static void ftbasegeneratecomplexfftplan(int n,
            ftplan plan)
        {
            int planarraysize = 0;
            int plansize = 0;
            int precomputedsize = 0;
            int tmpmemsize = 0;
            int stackmemsize = 0;
            int stackptr = 0;

            planarraysize = 1;
            plansize = 0;
            precomputedsize = 0;
            stackmemsize = 0;
            stackptr = 0;
            tmpmemsize = 2*n;
            plan.plan = new int[planarraysize];
            ftbasegenerateplanrec(n, ftbasecffttask, plan, ref plansize, ref precomputedsize, ref planarraysize, ref tmpmemsize, ref stackmemsize, stackptr);
            ap.assert(stackptr==0, "Internal error in FTBaseGenerateComplexFFTPlan: stack ptr!");
            plan.stackbuf = new double[Math.Max(stackmemsize, 1)];
            plan.tmpbuf = new double[Math.Max(tmpmemsize, 1)];
            plan.precomputed = new double[Math.Max(precomputedsize, 1)];
            stackptr = 0;
            ftbaseprecomputeplanrec(plan, 0, stackptr);
            ap.assert(stackptr==0, "Internal error in FTBaseGenerateComplexFFTPlan: stack ptr!");
        }


        /*************************************************************************
        Generates real FFT plan
        *************************************************************************/
        public static void ftbasegeneraterealfftplan(int n,
            ftplan plan)
        {
            int planarraysize = 0;
            int plansize = 0;
            int precomputedsize = 0;
            int tmpmemsize = 0;
            int stackmemsize = 0;
            int stackptr = 0;

            planarraysize = 1;
            plansize = 0;
            precomputedsize = 0;
            stackmemsize = 0;
            stackptr = 0;
            tmpmemsize = 2*n;
            plan.plan = new int[planarraysize];
            ftbasegenerateplanrec(n, ftbaserffttask, plan, ref plansize, ref precomputedsize, ref planarraysize, ref tmpmemsize, ref stackmemsize, stackptr);
            ap.assert(stackptr==0, "Internal error in FTBaseGenerateRealFFTPlan: stack ptr!");
            plan.stackbuf = new double[Math.Max(stackmemsize, 1)];
            plan.tmpbuf = new double[Math.Max(tmpmemsize, 1)];
            plan.precomputed = new double[Math.Max(precomputedsize, 1)];
            stackptr = 0;
            ftbaseprecomputeplanrec(plan, 0, stackptr);
            ap.assert(stackptr==0, "Internal error in FTBaseGenerateRealFFTPlan: stack ptr!");
        }


        /*************************************************************************
        Generates real FHT plan
        *************************************************************************/
        public static void ftbasegeneraterealfhtplan(int n,
            ftplan plan)
        {
            int planarraysize = 0;
            int plansize = 0;
            int precomputedsize = 0;
            int tmpmemsize = 0;
            int stackmemsize = 0;
            int stackptr = 0;

            planarraysize = 1;
            plansize = 0;
            precomputedsize = 0;
            stackmemsize = 0;
            stackptr = 0;
            tmpmemsize = n;
            plan.plan = new int[planarraysize];
            ftbasegenerateplanrec(n, ftbaserfhttask, plan, ref plansize, ref precomputedsize, ref planarraysize, ref tmpmemsize, ref stackmemsize, stackptr);
            ap.assert(stackptr==0, "Internal error in FTBaseGenerateRealFHTPlan: stack ptr!");
            plan.stackbuf = new double[Math.Max(stackmemsize, 1)];
            plan.tmpbuf = new double[Math.Max(tmpmemsize, 1)];
            plan.precomputed = new double[Math.Max(precomputedsize, 1)];
            stackptr = 0;
            ftbaseprecomputeplanrec(plan, 0, stackptr);
            ap.assert(stackptr==0, "Internal error in FTBaseGenerateRealFHTPlan: stack ptr!");
        }


        /*************************************************************************
        This subroutine executes FFT/FHT plan.

        If Plan is a:
        * complex FFT plan  -   sizeof(A)=2*N,
                                A contains interleaved real/imaginary values
        * real FFT plan     -   sizeof(A)=2*N,
                                A contains real values interleaved with zeros
        * real FHT plan     -   sizeof(A)=2*N,
                                A contains real values interleaved with zeros

          -- ALGLIB --
             Copyright 01.05.2009 by Bochkanov Sergey
        *************************************************************************/
        public static void ftbaseexecuteplan(ref double[] a,
            int aoffset,
            int n,
            ftplan plan)
        {
            int stackptr = 0;

            stackptr = 0;
            ftbaseexecuteplanrec(ref a, aoffset, plan, 0, stackptr);
        }


        /*************************************************************************
        Recurrent subroutine for the FTBaseExecutePlan

        Parameters:
            A           FFT'ed array
            AOffset     offset of the FFT'ed part (distance is measured in doubles)

          -- ALGLIB --
             Copyright 01.05.2009 by Bochkanov Sergey
        *************************************************************************/
        public static void ftbaseexecuteplanrec(ref double[] a,
            int aoffset,
            ftplan plan,
            int entryoffset,
            int stackptr)
        {
            int i = 0;
            int j = 0;
            int k = 0;
            int n1 = 0;
            int n2 = 0;
            int n = 0;
            int m = 0;
            int offs = 0;
            int offs1 = 0;
            int offs2 = 0;
            int offsa = 0;
            int offsb = 0;
            int offsp = 0;
            double hk = 0;
            double hnk = 0;
            double x = 0;
            double y = 0;
            double bx = 0;
            double by = 0;
            double[] emptyarray = new double[0];
            double a0x = 0;
            double a0y = 0;
            double a1x = 0;
            double a1y = 0;
            double a2x = 0;
            double a2y = 0;
            double a3x = 0;
            double a3y = 0;
            double v0 = 0;
            double v1 = 0;
            double v2 = 0;
            double v3 = 0;
            double t1x = 0;
            double t1y = 0;
            double t2x = 0;
            double t2y = 0;
            double t3x = 0;
            double t3y = 0;
            double t4x = 0;
            double t4y = 0;
            double t5x = 0;
            double t5y = 0;
            double m1x = 0;
            double m1y = 0;
            double m2x = 0;
            double m2y = 0;
            double m3x = 0;
            double m3y = 0;
            double m4x = 0;
            double m4y = 0;
            double m5x = 0;
            double m5y = 0;
            double s1x = 0;
            double s1y = 0;
            double s2x = 0;
            double s2y = 0;
            double s3x = 0;
            double s3y = 0;
            double s4x = 0;
            double s4y = 0;
            double s5x = 0;
            double s5y = 0;
            double c1 = 0;
            double c2 = 0;
            double c3 = 0;
            double c4 = 0;
            double c5 = 0;
            double[] tmp = new double[0];
            int i_ = 0;
            int i1_ = 0;

            if( plan.plan[entryoffset+3]==fftemptyplan )
            {
                return;
            }
            if( plan.plan[entryoffset+3]==fftcooleytukeyplan )
            {
                
                //
                // Cooley-Tukey plan
                // * transposition
                // * row-wise FFT
                // * twiddle factors:
                //   - TwBase is a basis twiddle factor for I=1, J=1
                //   - TwRow is a twiddle factor for a second element in a row (J=1)
                //   - Tw is a twiddle factor for a current element
                // * transposition again
                // * row-wise FFT again
                //
                n1 = plan.plan[entryoffset+1];
                n2 = plan.plan[entryoffset+2];
                internalcomplexlintranspose(ref a, n1, n2, aoffset, ref plan.tmpbuf);
                for(i=0; i<=n2-1; i++)
                {
                    ftbaseexecuteplanrec(ref a, aoffset+i*n1*2, plan, plan.plan[entryoffset+5], stackptr);
                }
                ffttwcalc(ref a, aoffset, n1, n2);
                internalcomplexlintranspose(ref a, n2, n1, aoffset, ref plan.tmpbuf);
                for(i=0; i<=n1-1; i++)
                {
                    ftbaseexecuteplanrec(ref a, aoffset+i*n2*2, plan, plan.plan[entryoffset+6], stackptr);
                }
                internalcomplexlintranspose(ref a, n1, n2, aoffset, ref plan.tmpbuf);
                return;
            }
            if( plan.plan[entryoffset+3]==fftrealcooleytukeyplan )
            {
                
                //
                // Cooley-Tukey plan
                // * transposition
                // * row-wise FFT
                // * twiddle factors:
                //   - TwBase is a basis twiddle factor for I=1, J=1
                //   - TwRow is a twiddle factor for a second element in a row (J=1)
                //   - Tw is a twiddle factor for a current element
                // * transposition again
                // * row-wise FFT again
                //
                n1 = plan.plan[entryoffset+1];
                n2 = plan.plan[entryoffset+2];
                internalcomplexlintranspose(ref a, n2, n1, aoffset, ref plan.tmpbuf);
                for(i=0; i<=n1/2-1; i++)
                {
                    
                    //
                    // pack two adjacent smaller real FFT's together,
                    // make one complex FFT,
                    // unpack result
                    //
                    offs = aoffset+2*i*n2*2;
                    for(k=0; k<=n2-1; k++)
                    {
                        a[offs+2*k+1] = a[offs+2*n2+2*k+0];
                    }
                    ftbaseexecuteplanrec(ref a, offs, plan, plan.plan[entryoffset+6], stackptr);
                    plan.tmpbuf[0] = a[offs+0];
                    plan.tmpbuf[1] = 0;
                    plan.tmpbuf[2*n2+0] = a[offs+1];
                    plan.tmpbuf[2*n2+1] = 0;
                    for(k=1; k<=n2-1; k++)
                    {
                        offs1 = 2*k;
                        offs2 = 2*n2+2*k;
                        hk = a[offs+2*k+0];
                        hnk = a[offs+2*(n2-k)+0];
                        plan.tmpbuf[offs1+0] = 0.5*(hk+hnk);
                        plan.tmpbuf[offs2+1] = -(0.5*(hk-hnk));
                        hk = a[offs+2*k+1];
                        hnk = a[offs+2*(n2-k)+1];
                        plan.tmpbuf[offs2+0] = 0.5*(hk+hnk);
                        plan.tmpbuf[offs1+1] = 0.5*(hk-hnk);
                    }
                    i1_ = (0) - (offs);
                    for(i_=offs; i_<=offs+2*n2*2-1;i_++)
                    {
                        a[i_] = plan.tmpbuf[i_+i1_];
                    }
                }
                if( n1%2!=0 )
                {
                    ftbaseexecuteplanrec(ref a, aoffset+(n1-1)*n2*2, plan, plan.plan[entryoffset+6], stackptr);
                }
                ffttwcalc(ref a, aoffset, n2, n1);
                internalcomplexlintranspose(ref a, n1, n2, aoffset, ref plan.tmpbuf);
                for(i=0; i<=n2-1; i++)
                {
                    ftbaseexecuteplanrec(ref a, aoffset+i*n1*2, plan, plan.plan[entryoffset+5], stackptr);
                }
                internalcomplexlintranspose(ref a, n2, n1, aoffset, ref plan.tmpbuf);
                return;
            }
            if( plan.plan[entryoffset+3]==fhtcooleytukeyplan )
            {
                
                //
                // Cooley-Tukey FHT plan:
                // * transpose                    \
                // * smaller FHT's                |
                // * pre-process                  |
                // * multiply by twiddle factors  | corresponds to multiplication by H1
                // * post-process                 |
                // * transpose again              /
                // * multiply by H2 (smaller FHT's)
                // * final transposition
                //
                // For more details see Vitezslav Vesely, "Fast algorithms
                // of Fourier and Hartley transform and their implementation in MATLAB",
                // page 31.
                //
                n1 = plan.plan[entryoffset+1];
                n2 = plan.plan[entryoffset+2];
                n = n1*n2;
                internalreallintranspose(ref a, n1, n2, aoffset, ref plan.tmpbuf);
                for(i=0; i<=n2-1; i++)
                {
                    ftbaseexecuteplanrec(ref a, aoffset+i*n1, plan, plan.plan[entryoffset+5], stackptr);
                }
                for(i=0; i<=n2-1; i++)
                {
                    for(j=0; j<=n1-1; j++)
                    {
                        offsa = aoffset+i*n1;
                        hk = a[offsa+j];
                        hnk = a[offsa+(n1-j)%n1];
                        offs = 2*(i*n1+j);
                        plan.tmpbuf[offs+0] = -(0.5*(hnk-hk));
                        plan.tmpbuf[offs+1] = 0.5*(hk+hnk);
                    }
                }
                ffttwcalc(ref plan.tmpbuf, 0, n1, n2);
                for(j=0; j<=n1-1; j++)
                {
                    a[aoffset+j] = plan.tmpbuf[2*j+0]+plan.tmpbuf[2*j+1];
                }
                if( n2%2==0 )
                {
                    offs = 2*(n2/2)*n1;
                    offsa = aoffset+n2/2*n1;
                    for(j=0; j<=n1-1; j++)
                    {
                        a[offsa+j] = plan.tmpbuf[offs+2*j+0]+plan.tmpbuf[offs+2*j+1];
                    }
                }
                for(i=1; i<=(n2+1)/2-1; i++)
                {
                    offs = 2*i*n1;
                    offs2 = 2*(n2-i)*n1;
                    offsa = aoffset+i*n1;
                    for(j=0; j<=n1-1; j++)
                    {
                        a[offsa+j] = plan.tmpbuf[offs+2*j+1]+plan.tmpbuf[offs2+2*j+0];
                    }
                    offsa = aoffset+(n2-i)*n1;
                    for(j=0; j<=n1-1; j++)
                    {
                        a[offsa+j] = plan.tmpbuf[offs+2*j+0]+plan.tmpbuf[offs2+2*j+1];
                    }
                }
                internalreallintranspose(ref a, n2, n1, aoffset, ref plan.tmpbuf);
                for(i=0; i<=n1-1; i++)
                {
                    ftbaseexecuteplanrec(ref a, aoffset+i*n2, plan, plan.plan[entryoffset+6], stackptr);
                }
                internalreallintranspose(ref a, n1, n2, aoffset, ref plan.tmpbuf);
                return;
            }
            if( plan.plan[entryoffset+3]==fhtn2plan )
            {
                
                //
                // Cooley-Tukey FHT plan
                //
                n1 = plan.plan[entryoffset+1];
                n2 = plan.plan[entryoffset+2];
                n = n1*n2;
                reffht(ref a, n, aoffset);
                return;
            }
            if( plan.plan[entryoffset+3]==fftcodeletplan )
            {
                n1 = plan.plan[entryoffset+1];
                n2 = plan.plan[entryoffset+2];
                n = n1*n2;
                if( n==2 )
                {
                    a0x = a[aoffset+0];
                    a0y = a[aoffset+1];
                    a1x = a[aoffset+2];
                    a1y = a[aoffset+3];
                    v0 = a0x+a1x;
                    v1 = a0y+a1y;
                    v2 = a0x-a1x;
                    v3 = a0y-a1y;
                    a[aoffset+0] = v0;
                    a[aoffset+1] = v1;
                    a[aoffset+2] = v2;
                    a[aoffset+3] = v3;
                    return;
                }
                if( n==3 )
                {
                    offs = plan.plan[entryoffset+7];
                    c1 = plan.precomputed[offs+0];
                    c2 = plan.precomputed[offs+1];
                    a0x = a[aoffset+0];
                    a0y = a[aoffset+1];
                    a1x = a[aoffset+2];
                    a1y = a[aoffset+3];
                    a2x = a[aoffset+4];
                    a2y = a[aoffset+5];
                    t1x = a1x+a2x;
                    t1y = a1y+a2y;
                    a0x = a0x+t1x;
                    a0y = a0y+t1y;
                    m1x = c1*t1x;
                    m1y = c1*t1y;
                    m2x = c2*(a1y-a2y);
                    m2y = c2*(a2x-a1x);
                    s1x = a0x+m1x;
                    s1y = a0y+m1y;
                    a1x = s1x+m2x;
                    a1y = s1y+m2y;
                    a2x = s1x-m2x;
                    a2y = s1y-m2y;
                    a[aoffset+0] = a0x;
                    a[aoffset+1] = a0y;
                    a[aoffset+2] = a1x;
                    a[aoffset+3] = a1y;
                    a[aoffset+4] = a2x;
                    a[aoffset+5] = a2y;
                    return;
                }
                if( n==4 )
                {
                    a0x = a[aoffset+0];
                    a0y = a[aoffset+1];
                    a1x = a[aoffset+2];
                    a1y = a[aoffset+3];
                    a2x = a[aoffset+4];
                    a2y = a[aoffset+5];
                    a3x = a[aoffset+6];
                    a3y = a[aoffset+7];
                    t1x = a0x+a2x;
                    t1y = a0y+a2y;
                    t2x = a1x+a3x;
                    t2y = a1y+a3y;
                    m2x = a0x-a2x;
                    m2y = a0y-a2y;
                    m3x = a1y-a3y;
                    m3y = a3x-a1x;
                    a[aoffset+0] = t1x+t2x;
                    a[aoffset+1] = t1y+t2y;
                    a[aoffset+4] = t1x-t2x;
                    a[aoffset+5] = t1y-t2y;
                    a[aoffset+2] = m2x+m3x;
                    a[aoffset+3] = m2y+m3y;
                    a[aoffset+6] = m2x-m3x;
                    a[aoffset+7] = m2y-m3y;
                    return;
                }
                if( n==5 )
                {
                    offs = plan.plan[entryoffset+7];
                    c1 = plan.precomputed[offs+0];
                    c2 = plan.precomputed[offs+1];
                    c3 = plan.precomputed[offs+2];
                    c4 = plan.precomputed[offs+3];
                    c5 = plan.precomputed[offs+4];
                    t1x = a[aoffset+2]+a[aoffset+8];
                    t1y = a[aoffset+3]+a[aoffset+9];
                    t2x = a[aoffset+4]+a[aoffset+6];
                    t2y = a[aoffset+5]+a[aoffset+7];
                    t3x = a[aoffset+2]-a[aoffset+8];
                    t3y = a[aoffset+3]-a[aoffset+9];
                    t4x = a[aoffset+6]-a[aoffset+4];
                    t4y = a[aoffset+7]-a[aoffset+5];
                    t5x = t1x+t2x;
                    t5y = t1y+t2y;
                    a[aoffset+0] = a[aoffset+0]+t5x;
                    a[aoffset+1] = a[aoffset+1]+t5y;
                    m1x = c1*t5x;
                    m1y = c1*t5y;
                    m2x = c2*(t1x-t2x);
                    m2y = c2*(t1y-t2y);
                    m3x = -(c3*(t3y+t4y));
                    m3y = c3*(t3x+t4x);
                    m4x = -(c4*t4y);
                    m4y = c4*t4x;
                    m5x = -(c5*t3y);
                    m5y = c5*t3x;
                    s3x = m3x-m4x;
                    s3y = m3y-m4y;
                    s5x = m3x+m5x;
                    s5y = m3y+m5y;
                    s1x = a[aoffset+0]+m1x;
                    s1y = a[aoffset+1]+m1y;
                    s2x = s1x+m2x;
                    s2y = s1y+m2y;
                    s4x = s1x-m2x;
                    s4y = s1y-m2y;
                    a[aoffset+2] = s2x+s3x;
                    a[aoffset+3] = s2y+s3y;
                    a[aoffset+4] = s4x+s5x;
                    a[aoffset+5] = s4y+s5y;
                    a[aoffset+6] = s4x-s5x;
                    a[aoffset+7] = s4y-s5y;
                    a[aoffset+8] = s2x-s3x;
                    a[aoffset+9] = s2y-s3y;
                    return;
                }
            }
            if( plan.plan[entryoffset+3]==fhtcodeletplan )
            {
                n1 = plan.plan[entryoffset+1];
                n2 = plan.plan[entryoffset+2];
                n = n1*n2;
                if( n==2 )
                {
                    a0x = a[aoffset+0];
                    a1x = a[aoffset+1];
                    a[aoffset+0] = a0x+a1x;
                    a[aoffset+1] = a0x-a1x;
                    return;
                }
                if( n==3 )
                {
                    offs = plan.plan[entryoffset+7];
                    c1 = plan.precomputed[offs+0];
                    c2 = plan.precomputed[offs+1];
                    a0x = a[aoffset+0];
                    a1x = a[aoffset+1];
                    a2x = a[aoffset+2];
                    t1x = a1x+a2x;
                    a0x = a0x+t1x;
                    m1x = c1*t1x;
                    m2y = c2*(a2x-a1x);
                    s1x = a0x+m1x;
                    a[aoffset+0] = a0x;
                    a[aoffset+1] = s1x-m2y;
                    a[aoffset+2] = s1x+m2y;
                    return;
                }
                if( n==4 )
                {
                    a0x = a[aoffset+0];
                    a1x = a[aoffset+1];
                    a2x = a[aoffset+2];
                    a3x = a[aoffset+3];
                    t1x = a0x+a2x;
                    t2x = a1x+a3x;
                    m2x = a0x-a2x;
                    m3y = a3x-a1x;
                    a[aoffset+0] = t1x+t2x;
                    a[aoffset+1] = m2x-m3y;
                    a[aoffset+2] = t1x-t2x;
                    a[aoffset+3] = m2x+m3y;
                    return;
                }
                if( n==5 )
                {
                    offs = plan.plan[entryoffset+7];
                    c1 = plan.precomputed[offs+0];
                    c2 = plan.precomputed[offs+1];
                    c3 = plan.precomputed[offs+2];
                    c4 = plan.precomputed[offs+3];
                    c5 = plan.precomputed[offs+4];
                    t1x = a[aoffset+1]+a[aoffset+4];
                    t2x = a[aoffset+2]+a[aoffset+3];
                    t3x = a[aoffset+1]-a[aoffset+4];
                    t4x = a[aoffset+3]-a[aoffset+2];
                    t5x = t1x+t2x;
                    v0 = a[aoffset+0]+t5x;
                    a[aoffset+0] = v0;
                    m2x = c2*(t1x-t2x);
                    m3y = c3*(t3x+t4x);
                    s3y = m3y-c4*t4x;
                    s5y = m3y+c5*t3x;
                    s1x = v0+c1*t5x;
                    s2x = s1x+m2x;
                    s4x = s1x-m2x;
                    a[aoffset+1] = s2x-s3y;
                    a[aoffset+2] = s4x-s5y;
                    a[aoffset+3] = s4x+s5y;
                    a[aoffset+4] = s2x+s3y;
                    return;
                }
            }
            if( plan.plan[entryoffset+3]==fftbluesteinplan )
            {
                
                //
                // Bluestein plan:
                // 1. multiply by precomputed coefficients
                // 2. make convolution: forward FFT, multiplication by precomputed FFT
                //    and backward FFT. backward FFT is represented as
                //
                //        invfft(x) = fft(x')'/M
                //
                //    for performance reasons reduction of inverse FFT to
                //    forward FFT is merged with multiplication of FFT components
                //    and last stage of Bluestein's transformation.
                // 3. post-multiplication by Bluestein factors
                //
                n = plan.plan[entryoffset+1];
                m = plan.plan[entryoffset+4];
                offs = plan.plan[entryoffset+7];
                for(i=stackptr+2*n; i<=stackptr+2*m-1; i++)
                {
                    plan.stackbuf[i] = 0;
                }
                offsp = offs+2*m;
                offsa = aoffset;
                offsb = stackptr;
                for(i=0; i<=n-1; i++)
                {
                    bx = plan.precomputed[offsp+0];
                    by = plan.precomputed[offsp+1];
                    x = a[offsa+0];
                    y = a[offsa+1];
                    plan.stackbuf[offsb+0] = x*bx-y*-by;
                    plan.stackbuf[offsb+1] = x*-by+y*bx;
                    offsp = offsp+2;
                    offsa = offsa+2;
                    offsb = offsb+2;
                }
                ftbaseexecuteplanrec(ref plan.stackbuf, stackptr, plan, plan.plan[entryoffset+5], stackptr+2*2*m);
                offsb = stackptr;
                offsp = offs;
                for(i=0; i<=m-1; i++)
                {
                    x = plan.stackbuf[offsb+0];
                    y = plan.stackbuf[offsb+1];
                    bx = plan.precomputed[offsp+0];
                    by = plan.precomputed[offsp+1];
                    plan.stackbuf[offsb+0] = x*bx-y*by;
                    plan.stackbuf[offsb+1] = -(x*by+y*bx);
                    offsb = offsb+2;
                    offsp = offsp+2;
                }
                ftbaseexecuteplanrec(ref plan.stackbuf, stackptr, plan, plan.plan[entryoffset+5], stackptr+2*2*m);
                offsb = stackptr;
                offsp = offs+2*m;
                offsa = aoffset;
                for(i=0; i<=n-1; i++)
                {
                    x = plan.stackbuf[offsb+0]/m;
                    y = -(plan.stackbuf[offsb+1]/m);
                    bx = plan.precomputed[offsp+0];
                    by = plan.precomputed[offsp+1];
                    a[offsa+0] = x*bx-y*-by;
                    a[offsa+1] = x*-by+y*bx;
                    offsp = offsp+2;
                    offsa = offsa+2;
                    offsb = offsb+2;
                }
                return;
            }
        }


        /*************************************************************************
        Returns good factorization N=N1*N2.

        Usually N1<=N2 (but not always - small N's may be exception).
        if N1<>1 then N2<>1.

        Factorization is chosen depending on task type and codelets we have.

          -- ALGLIB --
             Copyright 01.05.2009 by Bochkanov Sergey
        *************************************************************************/
        public static void ftbasefactorize(int n,
            int tasktype,
            ref int n1,
            ref int n2)
        {
            int j = 0;

            n1 = 0;
            n2 = 0;

            n1 = 0;
            n2 = 0;
            
            //
            // try to find good codelet
            //
            if( n1*n2!=n )
            {
                for(j=ftbasecodeletrecommended; j>=2; j--)
                {
                    if( n%j==0 )
                    {
                        n1 = j;
                        n2 = n/j;
                        break;
                    }
                }
            }
            
            //
            // try to factorize N
            //
            if( n1*n2!=n )
            {
                for(j=ftbasecodeletrecommended+1; j<=n-1; j++)
                {
                    if( n%j==0 )
                    {
                        n1 = j;
                        n2 = n/j;
                        break;
                    }
                }
            }
            
            //
            // looks like N is prime :(
            //
            if( n1*n2!=n )
            {
                n1 = 1;
                n2 = n;
            }
            
            //
            // normalize
            //
            if( n2==1 & n1!=1 )
            {
                n2 = n1;
                n1 = 1;
            }
        }


        /*************************************************************************
        Is number smooth?

          -- ALGLIB --
             Copyright 01.05.2009 by Bochkanov Sergey
        *************************************************************************/
        public static bool ftbaseissmooth(int n)
        {
            bool result = new bool();
            int i = 0;

            for(i=2; i<=ftbasemaxsmoothfactor; i++)
            {
                while( n%i==0 )
                {
                    n = n/i;
                }
            }
            result = n==1;
            return result;
        }


        /*************************************************************************
        Returns smallest smooth (divisible only by 2, 3, 5) number that is greater
        than or equal to max(N,2)

          -- ALGLIB --
             Copyright 01.05.2009 by Bochkanov Sergey
        *************************************************************************/
        public static int ftbasefindsmooth(int n)
        {
            int result = 0;
            int best = 0;

            best = 2;
            while( best<n )
            {
                best = 2*best;
            }
            ftbasefindsmoothrec(n, 1, 2, ref best);
            result = best;
            return result;
        }


        /*************************************************************************
        Returns  smallest  smooth  (divisible only by 2, 3, 5) even number that is
        greater than or equal to max(N,2)

          -- ALGLIB --
             Copyright 01.05.2009 by Bochkanov Sergey
        *************************************************************************/
        public static int ftbasefindsmootheven(int n)
        {
            int result = 0;
            int best = 0;

            best = 2;
            while( best<n )
            {
                best = 2*best;
            }
            ftbasefindsmoothrec(n, 2, 2, ref best);
            result = best;
            return result;
        }


        /*************************************************************************
        Returns estimate of FLOP count for the FFT.

        It is only an estimate based on operations count for the PERFECT FFT
        and relative inefficiency of the algorithm actually used.

        N should be power of 2, estimates are badly wrong for non-power-of-2 N's.

          -- ALGLIB --
             Copyright 01.05.2009 by Bochkanov Sergey
        *************************************************************************/
        public static double ftbasegetflopestimate(int n)
        {
            double result = 0;

            result = ftbaseinefficiencyfactor*(4*n*Math.Log(n)/Math.Log(2)-6*n+8);
            return result;
        }


        /*************************************************************************
        Recurrent subroutine for the FFTGeneratePlan:

        PARAMETERS:
            N                   plan size
            IsReal              whether input is real or not.
                                subroutine MUST NOT ignore this flag because real
                                inputs comes with non-initialized imaginary parts,
                                so ignoring this flag will result in corrupted output
            HalfOut             whether full output or only half of it from 0 to
                                floor(N/2) is needed. This flag may be ignored if
                                doing so will simplify calculations
            Plan                plan array
            PlanSize            size of used part (in integers)
            PrecomputedSize     size of precomputed array allocated yet
            PlanArraySize       plan array size (actual)
            TmpMemSize          temporary memory required size
            BluesteinMemSize    temporary memory required size

          -- ALGLIB --
             Copyright 01.05.2009 by Bochkanov Sergey
        *************************************************************************/
        private static void ftbasegenerateplanrec(int n,
            int tasktype,
            ftplan plan,
            ref int plansize,
            ref int precomputedsize,
            ref int planarraysize,
            ref int tmpmemsize,
            ref int stackmemsize,
            int stackptr)
        {
            int k = 0;
            int m = 0;
            int n1 = 0;
            int n2 = 0;
            int esize = 0;
            int entryoffset = 0;

            
            //
            // prepare
            //
            if( plansize+ftbaseplanentrysize>planarraysize )
            {
                fftarrayresize(ref plan.plan, ref planarraysize, 8*planarraysize);
            }
            entryoffset = plansize;
            esize = ftbaseplanentrysize;
            plansize = plansize+esize;
            
            //
            // if N=1, generate empty plan and exit
            //
            if( n==1 )
            {
                plan.plan[entryoffset+0] = esize;
                plan.plan[entryoffset+1] = -1;
                plan.plan[entryoffset+2] = -1;
                plan.plan[entryoffset+3] = fftemptyplan;
                plan.plan[entryoffset+4] = -1;
                plan.plan[entryoffset+5] = -1;
                plan.plan[entryoffset+6] = -1;
                plan.plan[entryoffset+7] = -1;
                return;
            }
            
            //
            // generate plans
            //
            ftbasefactorize(n, tasktype, ref n1, ref n2);
            if( tasktype==ftbasecffttask | tasktype==ftbaserffttask )
            {
                
                //
                // complex FFT plans
                //
                if( n1!=1 )
                {
                    
                    //
                    // Cooley-Tukey plan (real or complex)
                    //
                    // Note that child plans are COMPLEX
                    // (whether plan itself is complex or not).
                    //
                    tmpmemsize = Math.Max(tmpmemsize, 2*n1*n2);
                    plan.plan[entryoffset+0] = esize;
                    plan.plan[entryoffset+1] = n1;
                    plan.plan[entryoffset+2] = n2;
                    if( tasktype==ftbasecffttask )
                    {
                        plan.plan[entryoffset+3] = fftcooleytukeyplan;
                    }
                    else
                    {
                        plan.plan[entryoffset+3] = fftrealcooleytukeyplan;
                    }
                    plan.plan[entryoffset+4] = 0;
                    plan.plan[entryoffset+5] = plansize;
                    ftbasegenerateplanrec(n1, ftbasecffttask, plan, ref plansize, ref precomputedsize, ref planarraysize, ref tmpmemsize, ref stackmemsize, stackptr);
                    plan.plan[entryoffset+6] = plansize;
                    ftbasegenerateplanrec(n2, ftbasecffttask, plan, ref plansize, ref precomputedsize, ref planarraysize, ref tmpmemsize, ref stackmemsize, stackptr);
                    plan.plan[entryoffset+7] = -1;
                    return;
                }
                else
                {
                    if( ((n==2 | n==3) | n==4) | n==5 )
                    {
                        
                        //
                        // hard-coded plan
                        //
                        plan.plan[entryoffset+0] = esize;
                        plan.plan[entryoffset+1] = n1;
                        plan.plan[entryoffset+2] = n2;
                        plan.plan[entryoffset+3] = fftcodeletplan;
                        plan.plan[entryoffset+4] = 0;
                        plan.plan[entryoffset+5] = -1;
                        plan.plan[entryoffset+6] = -1;
                        plan.plan[entryoffset+7] = precomputedsize;
                        if( n==3 )
                        {
                            precomputedsize = precomputedsize+2;
                        }
                        if( n==5 )
                        {
                            precomputedsize = precomputedsize+5;
                        }
                        return;
                    }
                    else
                    {
                        
                        //
                        // Bluestein's plan
                        //
                        // Select such M that M>=2*N-1, M is composite, and M's
                        // factors are 2, 3, 5
                        //
                        k = 2*n2-1;
                        m = ftbasefindsmooth(k);
                        tmpmemsize = Math.Max(tmpmemsize, 2*m);
                        plan.plan[entryoffset+0] = esize;
                        plan.plan[entryoffset+1] = n2;
                        plan.plan[entryoffset+2] = -1;
                        plan.plan[entryoffset+3] = fftbluesteinplan;
                        plan.plan[entryoffset+4] = m;
                        plan.plan[entryoffset+5] = plansize;
                        stackptr = stackptr+2*2*m;
                        stackmemsize = Math.Max(stackmemsize, stackptr);
                        ftbasegenerateplanrec(m, ftbasecffttask, plan, ref plansize, ref precomputedsize, ref planarraysize, ref tmpmemsize, ref stackmemsize, stackptr);
                        stackptr = stackptr-2*2*m;
                        plan.plan[entryoffset+6] = -1;
                        plan.plan[entryoffset+7] = precomputedsize;
                        precomputedsize = precomputedsize+2*m+2*n;
                        return;
                    }
                }
            }
            if( tasktype==ftbaserfhttask )
            {
                
                //
                // real FHT plans
                //
                if( n1!=1 )
                {
                    
                    //
                    // Cooley-Tukey plan
                    //
                    //
                    tmpmemsize = Math.Max(tmpmemsize, 2*n1*n2);
                    plan.plan[entryoffset+0] = esize;
                    plan.plan[entryoffset+1] = n1;
                    plan.plan[entryoffset+2] = n2;
                    plan.plan[entryoffset+3] = fhtcooleytukeyplan;
                    plan.plan[entryoffset+4] = 0;
                    plan.plan[entryoffset+5] = plansize;
                    ftbasegenerateplanrec(n1, tasktype, plan, ref plansize, ref precomputedsize, ref planarraysize, ref tmpmemsize, ref stackmemsize, stackptr);
                    plan.plan[entryoffset+6] = plansize;
                    ftbasegenerateplanrec(n2, tasktype, plan, ref plansize, ref precomputedsize, ref planarraysize, ref tmpmemsize, ref stackmemsize, stackptr);
                    plan.plan[entryoffset+7] = -1;
                    return;
                }
                else
                {
                    
                    //
                    // N2 plan
                    //
                    plan.plan[entryoffset+0] = esize;
                    plan.plan[entryoffset+1] = n1;
                    plan.plan[entryoffset+2] = n2;
                    plan.plan[entryoffset+3] = fhtn2plan;
                    plan.plan[entryoffset+4] = 0;
                    plan.plan[entryoffset+5] = -1;
                    plan.plan[entryoffset+6] = -1;
                    plan.plan[entryoffset+7] = -1;
                    if( ((n==2 | n==3) | n==4) | n==5 )
                    {
                        
                        //
                        // hard-coded plan
                        //
                        plan.plan[entryoffset+0] = esize;
                        plan.plan[entryoffset+1] = n1;
                        plan.plan[entryoffset+2] = n2;
                        plan.plan[entryoffset+3] = fhtcodeletplan;
                        plan.plan[entryoffset+4] = 0;
                        plan.plan[entryoffset+5] = -1;
                        plan.plan[entryoffset+6] = -1;
                        plan.plan[entryoffset+7] = precomputedsize;
                        if( n==3 )
                        {
                            precomputedsize = precomputedsize+2;
                        }
                        if( n==5 )
                        {
                            precomputedsize = precomputedsize+5;
                        }
                        return;
                    }
                    return;
                }
            }
        }


        /*************************************************************************
        Recurrent subroutine for precomputing FFT plans

          -- ALGLIB --
             Copyright 01.05.2009 by Bochkanov Sergey
        *************************************************************************/
        private static void ftbaseprecomputeplanrec(ftplan plan,
            int entryoffset,
            int stackptr)
        {
            int i = 0;
            int n1 = 0;
            int n2 = 0;
            int n = 0;
            int m = 0;
            int offs = 0;
            double v = 0;
            double[] emptyarray = new double[0];
            double bx = 0;
            double by = 0;

            if( (plan.plan[entryoffset+3]==fftcooleytukeyplan | plan.plan[entryoffset+3]==fftrealcooleytukeyplan) | plan.plan[entryoffset+3]==fhtcooleytukeyplan )
            {
                ftbaseprecomputeplanrec(plan, plan.plan[entryoffset+5], stackptr);
                ftbaseprecomputeplanrec(plan, plan.plan[entryoffset+6], stackptr);
                return;
            }
            if( plan.plan[entryoffset+3]==fftcodeletplan | plan.plan[entryoffset+3]==fhtcodeletplan )
            {
                n1 = plan.plan[entryoffset+1];
                n2 = plan.plan[entryoffset+2];
                n = n1*n2;
                if( n==3 )
                {
                    offs = plan.plan[entryoffset+7];
                    plan.precomputed[offs+0] = Math.Cos(2*Math.PI/3)-1;
                    plan.precomputed[offs+1] = Math.Sin(2*Math.PI/3);
                    return;
                }
                if( n==5 )
                {
                    offs = plan.plan[entryoffset+7];
                    v = 2*Math.PI/5;
                    plan.precomputed[offs+0] = (Math.Cos(v)+Math.Cos(2*v))/2-1;
                    plan.precomputed[offs+1] = (Math.Cos(v)-Math.Cos(2*v))/2;
                    plan.precomputed[offs+2] = -Math.Sin(v);
                    plan.precomputed[offs+3] = -(Math.Sin(v)+Math.Sin(2*v));
                    plan.precomputed[offs+4] = Math.Sin(v)-Math.Sin(2*v);
                    return;
                }
            }
            if( plan.plan[entryoffset+3]==fftbluesteinplan )
            {
                ftbaseprecomputeplanrec(plan, plan.plan[entryoffset+5], stackptr);
                n = plan.plan[entryoffset+1];
                m = plan.plan[entryoffset+4];
                offs = plan.plan[entryoffset+7];
                for(i=0; i<=2*m-1; i++)
                {
                    plan.precomputed[offs+i] = 0;
                }
                for(i=0; i<=n-1; i++)
                {
                    bx = Math.Cos(Math.PI*math.sqr(i)/n);
                    by = Math.Sin(Math.PI*math.sqr(i)/n);
                    plan.precomputed[offs+2*i+0] = bx;
                    plan.precomputed[offs+2*i+1] = by;
                    plan.precomputed[offs+2*m+2*i+0] = bx;
                    plan.precomputed[offs+2*m+2*i+1] = by;
                    if( i>0 )
                    {
                        plan.precomputed[offs+2*(m-i)+0] = bx;
                        plan.precomputed[offs+2*(m-i)+1] = by;
                    }
                }
                ftbaseexecuteplanrec(ref plan.precomputed, offs, plan, plan.plan[entryoffset+5], stackptr);
                return;
            }
        }


        /*************************************************************************
        Twiddle factors calculation

          -- ALGLIB --
             Copyright 01.05.2009 by Bochkanov Sergey
        *************************************************************************/
        private static void ffttwcalc(ref double[] a,
            int aoffset,
            int n1,
            int n2)
        {
            int i = 0;
            int j = 0;
            int n = 0;
            int idx = 0;
            int offs = 0;
            double x = 0;
            double y = 0;
            double twxm1 = 0;
            double twy = 0;
            double twbasexm1 = 0;
            double twbasey = 0;
            double twrowxm1 = 0;
            double twrowy = 0;
            double tmpx = 0;
            double tmpy = 0;
            double v = 0;

            n = n1*n2;
            v = -(2*Math.PI/n);
            twbasexm1 = -(2*math.sqr(Math.Sin(0.5*v)));
            twbasey = Math.Sin(v);
            twrowxm1 = 0;
            twrowy = 0;
            for(i=0; i<=n2-1; i++)
            {
                twxm1 = 0;
                twy = 0;
                for(j=0; j<=n1-1; j++)
                {
                    idx = i*n1+j;
                    offs = aoffset+2*idx;
                    x = a[offs+0];
                    y = a[offs+1];
                    tmpx = x*twxm1-y*twy;
                    tmpy = x*twy+y*twxm1;
                    a[offs+0] = x+tmpx;
                    a[offs+1] = y+tmpy;
                    
                    //
                    // update Tw: Tw(new) = Tw(old)*TwRow
                    //
                    if( j<n1-1 )
                    {
                        if( j%ftbaseupdatetw==0 )
                        {
                            v = -(2*Math.PI*i*(j+1)/n);
                            twxm1 = -(2*math.sqr(Math.Sin(0.5*v)));
                            twy = Math.Sin(v);
                        }
                        else
                        {
                            tmpx = twrowxm1+twxm1*twrowxm1-twy*twrowy;
                            tmpy = twrowy+twxm1*twrowy+twy*twrowxm1;
                            twxm1 = twxm1+tmpx;
                            twy = twy+tmpy;
                        }
                    }
                }
                
                //
                // update TwRow: TwRow(new) = TwRow(old)*TwBase
                //
                if( i<n2-1 )
                {
                    if( j%ftbaseupdatetw==0 )
                    {
                        v = -(2*Math.PI*(i+1)/n);
                        twrowxm1 = -(2*math.sqr(Math.Sin(0.5*v)));
                        twrowy = Math.Sin(v);
                    }
                    else
                    {
                        tmpx = twbasexm1+twrowxm1*twbasexm1-twrowy*twbasey;
                        tmpy = twbasey+twrowxm1*twbasey+twrowy*twbasexm1;
                        twrowxm1 = twrowxm1+tmpx;
                        twrowy = twrowy+tmpy;
                    }
                }
            }
        }


        /*************************************************************************
        Linear transpose: transpose complex matrix stored in 1-dimensional array

          -- ALGLIB --
             Copyright 01.05.2009 by Bochkanov Sergey
        *************************************************************************/
        private static void internalcomplexlintranspose(ref double[] a,
            int m,
            int n,
            int astart,
            ref double[] buf)
        {
            int i_ = 0;
            int i1_ = 0;

            ffticltrec(ref a, astart, n, ref buf, 0, m, m, n);
            i1_ = (0) - (astart);
            for(i_=astart; i_<=astart+2*m*n-1;i_++)
            {
                a[i_] = buf[i_+i1_];
            }
        }


        /*************************************************************************
        Linear transpose: transpose real matrix stored in 1-dimensional array

          -- ALGLIB --
             Copyright 01.05.2009 by Bochkanov Sergey
        *************************************************************************/
        private static void internalreallintranspose(ref double[] a,
            int m,
            int n,
            int astart,
            ref double[] buf)
        {
            int i_ = 0;
            int i1_ = 0;

            fftirltrec(ref a, astart, n, ref buf, 0, m, m, n);
            i1_ = (0) - (astart);
            for(i_=astart; i_<=astart+m*n-1;i_++)
            {
                a[i_] = buf[i_+i1_];
            }
        }


        /*************************************************************************
        Recurrent subroutine for a InternalComplexLinTranspose

        Write A^T to B, where:
        * A is m*n complex matrix stored in array A as pairs of real/image values,
          beginning from AStart position, with AStride stride
        * B is n*m complex matrix stored in array B as pairs of real/image values,
          beginning from BStart position, with BStride stride
        stride is measured in complex numbers, i.e. in real/image pairs.

          -- ALGLIB --
             Copyright 01.05.2009 by Bochkanov Sergey
        *************************************************************************/
        private static void ffticltrec(ref double[] a,
            int astart,
            int astride,
            ref double[] b,
            int bstart,
            int bstride,
            int m,
            int n)
        {
            int i = 0;
            int j = 0;
            int idx1 = 0;
            int idx2 = 0;
            int m2 = 0;
            int m1 = 0;
            int n1 = 0;

            if( m==0 | n==0 )
            {
                return;
            }
            if( Math.Max(m, n)<=8 )
            {
                m2 = 2*bstride;
                for(i=0; i<=m-1; i++)
                {
                    idx1 = bstart+2*i;
                    idx2 = astart+2*i*astride;
                    for(j=0; j<=n-1; j++)
                    {
                        b[idx1+0] = a[idx2+0];
                        b[idx1+1] = a[idx2+1];
                        idx1 = idx1+m2;
                        idx2 = idx2+2;
                    }
                }
                return;
            }
            if( n>m )
            {
                
                //
                // New partition:
                //
                // "A^T -> B" becomes "(A1 A2)^T -> ( B1 )
                //                                  ( B2 )
                //
                n1 = n/2;
                if( n-n1>=8 & n1%8!=0 )
                {
                    n1 = n1+(8-n1%8);
                }
                ap.assert(n-n1>0);
                ffticltrec(ref a, astart, astride, ref b, bstart, bstride, m, n1);
                ffticltrec(ref a, astart+2*n1, astride, ref b, bstart+2*n1*bstride, bstride, m, n-n1);
            }
            else
            {
                
                //
                // New partition:
                //
                // "A^T -> B" becomes "( A1 )^T -> ( B1 B2 )
                //                     ( A2 )
                //
                m1 = m/2;
                if( m-m1>=8 & m1%8!=0 )
                {
                    m1 = m1+(8-m1%8);
                }
                ap.assert(m-m1>0);
                ffticltrec(ref a, astart, astride, ref b, bstart, bstride, m1, n);
                ffticltrec(ref a, astart+2*m1*astride, astride, ref b, bstart+2*m1, bstride, m-m1, n);
            }
        }


        /*************************************************************************
        Recurrent subroutine for a InternalRealLinTranspose


          -- ALGLIB --
             Copyright 01.05.2009 by Bochkanov Sergey
        *************************************************************************/
        private static void fftirltrec(ref double[] a,
            int astart,
            int astride,
            ref double[] b,
            int bstart,
            int bstride,
            int m,
            int n)
        {
            int i = 0;
            int j = 0;
            int idx1 = 0;
            int idx2 = 0;
            int m1 = 0;
            int n1 = 0;

            if( m==0 | n==0 )
            {
                return;
            }
            if( Math.Max(m, n)<=8 )
            {
                for(i=0; i<=m-1; i++)
                {
                    idx1 = bstart+i;
                    idx2 = astart+i*astride;
                    for(j=0; j<=n-1; j++)
                    {
                        b[idx1] = a[idx2];
                        idx1 = idx1+bstride;
                        idx2 = idx2+1;
                    }
                }
                return;
            }
            if( n>m )
            {
                
                //
                // New partition:
                //
                // "A^T -> B" becomes "(A1 A2)^T -> ( B1 )
                //                                  ( B2 )
                //
                n1 = n/2;
                if( n-n1>=8 & n1%8!=0 )
                {
                    n1 = n1+(8-n1%8);
                }
                ap.assert(n-n1>0);
                fftirltrec(ref a, astart, astride, ref b, bstart, bstride, m, n1);
                fftirltrec(ref a, astart+n1, astride, ref b, bstart+n1*bstride, bstride, m, n-n1);
            }
            else
            {
                
                //
                // New partition:
                //
                // "A^T -> B" becomes "( A1 )^T -> ( B1 B2 )
                //                     ( A2 )
                //
                m1 = m/2;
                if( m-m1>=8 & m1%8!=0 )
                {
                    m1 = m1+(8-m1%8);
                }
                ap.assert(m-m1>0);
                fftirltrec(ref a, astart, astride, ref b, bstart, bstride, m1, n);
                fftirltrec(ref a, astart+m1*astride, astride, ref b, bstart+m1, bstride, m-m1, n);
            }
        }


        /*************************************************************************
        recurrent subroutine for FFTFindSmoothRec

          -- ALGLIB --
             Copyright 01.05.2009 by Bochkanov Sergey
        *************************************************************************/
        private static void ftbasefindsmoothrec(int n,
            int seed,
            int leastfactor,
            ref int best)
        {
            ap.assert(ftbasemaxsmoothfactor<=5, "FTBaseFindSmoothRec: internal error!");
            if( seed>=n )
            {
                best = Math.Min(best, seed);
                return;
            }
            if( leastfactor<=2 )
            {
                ftbasefindsmoothrec(n, seed*2, 2, ref best);
            }
            if( leastfactor<=3 )
            {
                ftbasefindsmoothrec(n, seed*3, 3, ref best);
            }
            if( leastfactor<=5 )
            {
                ftbasefindsmoothrec(n, seed*5, 5, ref best);
            }
        }


        /*************************************************************************
        Internal subroutine: array resize

          -- ALGLIB --
             Copyright 01.05.2009 by Bochkanov Sergey
        *************************************************************************/
        private static void fftarrayresize(ref int[] a,
            ref int asize,
            int newasize)
        {
            int[] tmp = new int[0];
            int i = 0;

            tmp = new int[asize];
            for(i=0; i<=asize-1; i++)
            {
                tmp[i] = a[i];
            }
            a = new int[newasize];
            for(i=0; i<=asize-1; i++)
            {
                a[i] = tmp[i];
            }
            asize = newasize;
        }


        /*************************************************************************
        Reference FHT stub
        *************************************************************************/
        private static void reffht(ref double[] a,
            int n,
            int offs)
        {
            double[] buf = new double[0];
            int i = 0;
            int j = 0;
            double v = 0;

            ap.assert(n>0, "RefFHTR1D: incorrect N!");
            buf = new double[n];
            for(i=0; i<=n-1; i++)
            {
                v = 0;
                for(j=0; j<=n-1; j++)
                {
                    v = v+a[offs+j]*(Math.Cos(2*Math.PI*i*j/n)+Math.Sin(2*Math.PI*i*j/n));
                }
                buf[i] = v;
            }
            for(i=0; i<=n-1; i++)
            {
                a[offs+i] = buf[i];
            }
        }


    }
    public class nearunityunit
    {
        public static double nulog1p(double x)
        {
            double result = 0;
            double z = 0;
            double lp = 0;
            double lq = 0;

            z = 1.0+x;
            if( (double)(z)<(double)(0.70710678118654752440) | (double)(z)>(double)(1.41421356237309504880) )
            {
                result = Math.Log(z);
                return result;
            }
            z = x*x;
            lp = 4.5270000862445199635215E-5;
            lp = lp*x+4.9854102823193375972212E-1;
            lp = lp*x+6.5787325942061044846969E0;
            lp = lp*x+2.9911919328553073277375E1;
            lp = lp*x+6.0949667980987787057556E1;
            lp = lp*x+5.7112963590585538103336E1;
            lp = lp*x+2.0039553499201281259648E1;
            lq = 1.0000000000000000000000E0;
            lq = lq*x+1.5062909083469192043167E1;
            lq = lq*x+8.3047565967967209469434E1;
            lq = lq*x+2.2176239823732856465394E2;
            lq = lq*x+3.0909872225312059774938E2;
            lq = lq*x+2.1642788614495947685003E2;
            lq = lq*x+6.0118660497603843919306E1;
            z = -(0.5*z)+x*(z*lp/lq);
            result = x+z;
            return result;
        }


        public static double nuexpm1(double x)
        {
            double result = 0;
            double r = 0;
            double xx = 0;
            double ep = 0;
            double eq = 0;

            if( (double)(x)<(double)(-0.5) | (double)(x)>(double)(0.5) )
            {
                result = Math.Exp(x)-1.0;
                return result;
            }
            xx = x*x;
            ep = 1.2617719307481059087798E-4;
            ep = ep*xx+3.0299440770744196129956E-2;
            ep = ep*xx+9.9999999999999999991025E-1;
            eq = 3.0019850513866445504159E-6;
            eq = eq*xx+2.5244834034968410419224E-3;
            eq = eq*xx+2.2726554820815502876593E-1;
            eq = eq*xx+2.0000000000000000000897E0;
            r = x*ep;
            r = r/(eq-r);
            result = r+r;
            return result;
        }


        public static double nucosm1(double x)
        {
            double result = 0;
            double xx = 0;
            double c = 0;

            if( (double)(x)<(double)(-(0.25*Math.PI)) | (double)(x)>(double)(0.25*Math.PI) )
            {
                result = Math.Cos(x)-1;
                return result;
            }
            xx = x*x;
            c = 4.7377507964246204691685E-14;
            c = c*xx-1.1470284843425359765671E-11;
            c = c*xx+2.0876754287081521758361E-9;
            c = c*xx-2.7557319214999787979814E-7;
            c = c*xx+2.4801587301570552304991E-5;
            c = c*xx-1.3888888888888872993737E-3;
            c = c*xx+4.1666666666666666609054E-2;
            result = -(0.5*xx)+xx*xx*c;
            return result;
        }


    }
    public class alglibbasics
    {


    }
}

