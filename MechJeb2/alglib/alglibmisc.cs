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


    /*************************************************************************
    Portable high quality random number generator state.
    Initialized with HQRNDRandomize() or HQRNDSeed().

    Fields:
        S1, S2      -   seed values
        V           -   precomputed value
        MagicV      -   'magic' value used to determine whether State structure
                        was correctly initialized.
    *************************************************************************/
    public class hqrndstate
    {
        //
        // Public declarations
        //

        public hqrndstate()
        {
            _innerobj = new hqrnd.hqrndstate();
        }

        //
        // Although some of declarations below are public, you should not use them
        // They are intended for internal use only
        //
        private hqrnd.hqrndstate _innerobj;
        public hqrnd.hqrndstate innerobj { get { return _innerobj; } }
        public hqrndstate(hqrnd.hqrndstate obj)
        {
            _innerobj = obj;
        }
    }

    /*************************************************************************
    HQRNDState  initialization  with  random  values  which come from standard
    RNG.

      -- ALGLIB --
         Copyright 02.12.2009 by Bochkanov Sergey
    *************************************************************************/
    public static void hqrndrandomize(out hqrndstate state)
    {
        state = new hqrndstate();
        hqrnd.hqrndrandomize(state.innerobj);
        return;
    }

    /*************************************************************************
    HQRNDState initialization with seed values

      -- ALGLIB --
         Copyright 02.12.2009 by Bochkanov Sergey
    *************************************************************************/
    public static void hqrndseed(int s1, int s2, out hqrndstate state)
    {
        state = new hqrndstate();
        hqrnd.hqrndseed(s1, s2, state.innerobj);
        return;
    }

    /*************************************************************************
    This function generates random real number in (0,1),
    not including interval boundaries

    State structure must be initialized with HQRNDRandomize() or HQRNDSeed().

      -- ALGLIB --
         Copyright 02.12.2009 by Bochkanov Sergey
    *************************************************************************/
    public static double hqrnduniformr(hqrndstate state)
    {

        double result = hqrnd.hqrnduniformr(state.innerobj);
        return result;
    }

    /*************************************************************************
    This function generates random integer number in [0, N)

    1. N must be less than HQRNDMax-1.
    2. State structure must be initialized with HQRNDRandomize() or HQRNDSeed()

      -- ALGLIB --
         Copyright 02.12.2009 by Bochkanov Sergey
    *************************************************************************/
    public static int hqrnduniformi(hqrndstate state, int n)
    {

        int result = hqrnd.hqrnduniformi(state.innerobj, n);
        return result;
    }

    /*************************************************************************
    Random number generator: normal numbers

    This function generates one random number from normal distribution.
    Its performance is equal to that of HQRNDNormal2()

    State structure must be initialized with HQRNDRandomize() or HQRNDSeed().

      -- ALGLIB --
         Copyright 02.12.2009 by Bochkanov Sergey
    *************************************************************************/
    public static double hqrndnormal(hqrndstate state)
    {

        double result = hqrnd.hqrndnormal(state.innerobj);
        return result;
    }

    /*************************************************************************
    Random number generator: random X and Y such that X^2+Y^2=1

    State structure must be initialized with HQRNDRandomize() or HQRNDSeed().

      -- ALGLIB --
         Copyright 02.12.2009 by Bochkanov Sergey
    *************************************************************************/
    public static void hqrndunit2(hqrndstate state, out double x, out double y)
    {
        x = 0;
        y = 0;
        hqrnd.hqrndunit2(state.innerobj, ref x, ref y);
        return;
    }

    /*************************************************************************
    Random number generator: normal numbers

    This function generates two independent random numbers from normal
    distribution. Its performance is equal to that of HQRNDNormal()

    State structure must be initialized with HQRNDRandomize() or HQRNDSeed().

      -- ALGLIB --
         Copyright 02.12.2009 by Bochkanov Sergey
    *************************************************************************/
    public static void hqrndnormal2(hqrndstate state, out double x1, out double x2)
    {
        x1 = 0;
        x2 = 0;
        hqrnd.hqrndnormal2(state.innerobj, ref x1, ref x2);
        return;
    }

    /*************************************************************************
    Random number generator: exponential distribution

    State structure must be initialized with HQRNDRandomize() or HQRNDSeed().

      -- ALGLIB --
         Copyright 11.08.2007 by Bochkanov Sergey
    *************************************************************************/
    public static double hqrndexponential(hqrndstate state, double lambdav)
    {

        double result = hqrnd.hqrndexponential(state.innerobj, lambdav);
        return result;
    }

}
public partial class alglib
{


    /*************************************************************************

    *************************************************************************/
    public class kdtree
    {
        //
        // Public declarations
        //

        public kdtree()
        {
            _innerobj = new nearestneighbor.kdtree();
        }

        //
        // Although some of declarations below are public, you should not use them
        // They are intended for internal use only
        //
        private nearestneighbor.kdtree _innerobj;
        public nearestneighbor.kdtree innerobj { get { return _innerobj; } }
        public kdtree(nearestneighbor.kdtree obj)
        {
            _innerobj = obj;
        }
    }


    /*************************************************************************
    This function serializes data structure to string.

    Important properties of s_out:
    * it contains alphanumeric characters, dots, underscores, minus signs
    * these symbols are grouped into words, which are separated by spaces
      and Windows-style (CR+LF) newlines
    * although  serializer  uses  spaces and CR+LF as separators, you can 
      replace any separator character by arbitrary combination of spaces,
      tabs, Windows or Unix newlines. It allows flexible reformatting  of
      the  string  in  case you want to include it into text or XML file. 
      But you should not insert separators into the middle of the "words"
      nor you should change case of letters.
    * s_out can be freely moved between 32-bit and 64-bit systems, little
      and big endian machines, and so on. You can serialize structure  on
      32-bit machine and unserialize it on 64-bit one (or vice versa), or
      serialize  it  on  SPARC  and  unserialize  on  x86.  You  can also 
      serialize  it  in  C# version of ALGLIB and unserialize in C++ one, 
      and vice versa.
    *************************************************************************/
    public static void kdtreeserialize(kdtree obj, out string s_out)
    {
        alglib.serializer s = new alglib.serializer();
        s.alloc_start();
        nearestneighbor.kdtreealloc(s, obj.innerobj);
        s.sstart_str();
        nearestneighbor.kdtreeserialize(s, obj.innerobj);
        s.stop();
        s_out = s.get_string();
    }


    /*************************************************************************
    This function unserializes data structure from string.
    *************************************************************************/
    public static void kdtreeunserialize(string s_in, out kdtree obj)
    {
        alglib.serializer s = new alglib.serializer();
        obj = new kdtree();
        s.ustart_str(s_in);
        nearestneighbor.kdtreeunserialize(s, obj.innerobj);
        s.stop();
    }

    /*************************************************************************
    KD-tree creation

    This subroutine creates KD-tree from set of X-values and optional Y-values

    INPUT PARAMETERS
        XY      -   dataset, array[0..N-1,0..NX+NY-1].
                    one row corresponds to one point.
                    first NX columns contain X-values, next NY (NY may be zero)
                    columns may contain associated Y-values
        N       -   number of points, N>=1
        NX      -   space dimension, NX>=1.
        NY      -   number of optional Y-values, NY>=0.
        NormType-   norm type:
                    * 0 denotes infinity-norm
                    * 1 denotes 1-norm
                    * 2 denotes 2-norm (Euclidean norm)

    OUTPUT PARAMETERS
        KDT     -   KD-tree


    NOTES

    1. KD-tree  creation  have O(N*logN) complexity and O(N*(2*NX+NY))  memory
       requirements.
    2. Although KD-trees may be used with any combination of N  and  NX,  they
       are more efficient than brute-force search only when N >> 4^NX. So they
       are most useful in low-dimensional tasks (NX=2, NX=3). NX=1  is another
       inefficient case, because  simple  binary  search  (without  additional
       structures) is much more efficient in such tasks than KD-trees.

      -- ALGLIB --
         Copyright 28.02.2010 by Bochkanov Sergey
    *************************************************************************/
    public static void kdtreebuild(double[,] xy, int n, int nx, int ny, int normtype, out kdtree kdt)
    {
        kdt = new kdtree();
        nearestneighbor.kdtreebuild(xy, n, nx, ny, normtype, kdt.innerobj);
        return;
    }
    public static void kdtreebuild(double[,] xy, int nx, int ny, int normtype, out kdtree kdt)
    {
        int n;

        kdt = new kdtree();
        n = ap.rows(xy);
        nearestneighbor.kdtreebuild(xy, n, nx, ny, normtype, kdt.innerobj);

        return;
    }

    /*************************************************************************
    KD-tree creation

    This  subroutine  creates  KD-tree  from set of X-values, integer tags and
    optional Y-values

    INPUT PARAMETERS
        XY      -   dataset, array[0..N-1,0..NX+NY-1].
                    one row corresponds to one point.
                    first NX columns contain X-values, next NY (NY may be zero)
                    columns may contain associated Y-values
        Tags    -   tags, array[0..N-1], contains integer tags associated
                    with points.
        N       -   number of points, N>=1
        NX      -   space dimension, NX>=1.
        NY      -   number of optional Y-values, NY>=0.
        NormType-   norm type:
                    * 0 denotes infinity-norm
                    * 1 denotes 1-norm
                    * 2 denotes 2-norm (Euclidean norm)

    OUTPUT PARAMETERS
        KDT     -   KD-tree

    NOTES

    1. KD-tree  creation  have O(N*logN) complexity and O(N*(2*NX+NY))  memory
       requirements.
    2. Although KD-trees may be used with any combination of N  and  NX,  they
       are more efficient than brute-force search only when N >> 4^NX. So they
       are most useful in low-dimensional tasks (NX=2, NX=3). NX=1  is another
       inefficient case, because  simple  binary  search  (without  additional
       structures) is much more efficient in such tasks than KD-trees.

      -- ALGLIB --
         Copyright 28.02.2010 by Bochkanov Sergey
    *************************************************************************/
    public static void kdtreebuildtagged(double[,] xy, int[] tags, int n, int nx, int ny, int normtype, out kdtree kdt)
    {
        kdt = new kdtree();
        nearestneighbor.kdtreebuildtagged(xy, tags, n, nx, ny, normtype, kdt.innerobj);
        return;
    }
    public static void kdtreebuildtagged(double[,] xy, int[] tags, int nx, int ny, int normtype, out kdtree kdt)
    {
        int n;
        if( (ap.rows(xy)!=ap.len(tags)))
            throw new alglibexception("Error while calling 'kdtreebuildtagged': looks like one of arguments has wrong size");
        kdt = new kdtree();
        n = ap.rows(xy);
        nearestneighbor.kdtreebuildtagged(xy, tags, n, nx, ny, normtype, kdt.innerobj);

        return;
    }

    /*************************************************************************
    K-NN query: K nearest neighbors

    INPUT PARAMETERS
        KDT         -   KD-tree
        X           -   point, array[0..NX-1].
        K           -   number of neighbors to return, K>=1
        SelfMatch   -   whether self-matches are allowed:
                        * if True, nearest neighbor may be the point itself
                          (if it exists in original dataset)
                        * if False, then only points with non-zero distance
                          are returned
                        * if not given, considered True

    RESULT
        number of actual neighbors found (either K or N, if K>N).

    This  subroutine  performs  query  and  stores  its result in the internal
    structures of the KD-tree. You can use  following  subroutines  to  obtain
    these results:
    * KDTreeQueryResultsX() to get X-values
    * KDTreeQueryResultsXY() to get X- and Y-values
    * KDTreeQueryResultsTags() to get tag values
    * KDTreeQueryResultsDistances() to get distances

      -- ALGLIB --
         Copyright 28.02.2010 by Bochkanov Sergey
    *************************************************************************/
    public static int kdtreequeryknn(kdtree kdt, double[] x, int k, bool selfmatch)
    {

        int result = nearestneighbor.kdtreequeryknn(kdt.innerobj, x, k, selfmatch);
        return result;
    }
    public static int kdtreequeryknn(kdtree kdt, double[] x, int k)
    {
        bool selfmatch;


        selfmatch = true;
        int result = nearestneighbor.kdtreequeryknn(kdt.innerobj, x, k, selfmatch);

        return result;
    }

    /*************************************************************************
    R-NN query: all points within R-sphere centered at X

    INPUT PARAMETERS
        KDT         -   KD-tree
        X           -   point, array[0..NX-1].
        R           -   radius of sphere (in corresponding norm), R>0
        SelfMatch   -   whether self-matches are allowed:
                        * if True, nearest neighbor may be the point itself
                          (if it exists in original dataset)
                        * if False, then only points with non-zero distance
                          are returned
                        * if not given, considered True

    RESULT
        number of neighbors found, >=0

    This  subroutine  performs  query  and  stores  its result in the internal
    structures of the KD-tree. You can use  following  subroutines  to  obtain
    actual results:
    * KDTreeQueryResultsX() to get X-values
    * KDTreeQueryResultsXY() to get X- and Y-values
    * KDTreeQueryResultsTags() to get tag values
    * KDTreeQueryResultsDistances() to get distances

      -- ALGLIB --
         Copyright 28.02.2010 by Bochkanov Sergey
    *************************************************************************/
    public static int kdtreequeryrnn(kdtree kdt, double[] x, double r, bool selfmatch)
    {

        int result = nearestneighbor.kdtreequeryrnn(kdt.innerobj, x, r, selfmatch);
        return result;
    }
    public static int kdtreequeryrnn(kdtree kdt, double[] x, double r)
    {
        bool selfmatch;


        selfmatch = true;
        int result = nearestneighbor.kdtreequeryrnn(kdt.innerobj, x, r, selfmatch);

        return result;
    }

    /*************************************************************************
    K-NN query: approximate K nearest neighbors

    INPUT PARAMETERS
        KDT         -   KD-tree
        X           -   point, array[0..NX-1].
        K           -   number of neighbors to return, K>=1
        SelfMatch   -   whether self-matches are allowed:
                        * if True, nearest neighbor may be the point itself
                          (if it exists in original dataset)
                        * if False, then only points with non-zero distance
                          are returned
                        * if not given, considered True
        Eps         -   approximation factor, Eps>=0. eps-approximate  nearest
                        neighbor  is  a  neighbor  whose distance from X is at
                        most (1+eps) times distance of true nearest neighbor.

    RESULT
        number of actual neighbors found (either K or N, if K>N).

    NOTES
        significant performance gain may be achieved only when Eps  is  is  on
        the order of magnitude of 1 or larger.

    This  subroutine  performs  query  and  stores  its result in the internal
    structures of the KD-tree. You can use  following  subroutines  to  obtain
    these results:
    * KDTreeQueryResultsX() to get X-values
    * KDTreeQueryResultsXY() to get X- and Y-values
    * KDTreeQueryResultsTags() to get tag values
    * KDTreeQueryResultsDistances() to get distances

      -- ALGLIB --
         Copyright 28.02.2010 by Bochkanov Sergey
    *************************************************************************/
    public static int kdtreequeryaknn(kdtree kdt, double[] x, int k, bool selfmatch, double eps)
    {

        int result = nearestneighbor.kdtreequeryaknn(kdt.innerobj, x, k, selfmatch, eps);
        return result;
    }
    public static int kdtreequeryaknn(kdtree kdt, double[] x, int k, double eps)
    {
        bool selfmatch;


        selfmatch = true;
        int result = nearestneighbor.kdtreequeryaknn(kdt.innerobj, x, k, selfmatch, eps);

        return result;
    }

    /*************************************************************************
    X-values from last query

    INPUT PARAMETERS
        KDT     -   KD-tree
        X       -   possibly pre-allocated buffer. If X is too small to store
                    result, it is resized. If size(X) is enough to store
                    result, it is left unchanged.

    OUTPUT PARAMETERS
        X       -   rows are filled with X-values

    NOTES
    1. points are ordered by distance from the query point (first = closest)
    2. if  XY is larger than required to store result, only leading part  will
       be overwritten; trailing part will be left unchanged. So  if  on  input
       XY = [[A,B],[C,D]], and result is [1,2],  then  on  exit  we  will  get
       XY = [[1,2],[C,D]]. This is done purposely to increase performance;  if
       you want function  to  resize  array  according  to  result  size,  use
       function with same name and suffix 'I'.

    SEE ALSO
    * KDTreeQueryResultsXY()            X- and Y-values
    * KDTreeQueryResultsTags()          tag values
    * KDTreeQueryResultsDistances()     distances

      -- ALGLIB --
         Copyright 28.02.2010 by Bochkanov Sergey
    *************************************************************************/
    public static void kdtreequeryresultsx(kdtree kdt, ref double[,] x)
    {

        nearestneighbor.kdtreequeryresultsx(kdt.innerobj, ref x);
        return;
    }

    /*************************************************************************
    X- and Y-values from last query

    INPUT PARAMETERS
        KDT     -   KD-tree
        XY      -   possibly pre-allocated buffer. If XY is too small to store
                    result, it is resized. If size(XY) is enough to store
                    result, it is left unchanged.

    OUTPUT PARAMETERS
        XY      -   rows are filled with points: first NX columns with
                    X-values, next NY columns - with Y-values.

    NOTES
    1. points are ordered by distance from the query point (first = closest)
    2. if  XY is larger than required to store result, only leading part  will
       be overwritten; trailing part will be left unchanged. So  if  on  input
       XY = [[A,B],[C,D]], and result is [1,2],  then  on  exit  we  will  get
       XY = [[1,2],[C,D]]. This is done purposely to increase performance;  if
       you want function  to  resize  array  according  to  result  size,  use
       function with same name and suffix 'I'.

    SEE ALSO
    * KDTreeQueryResultsX()             X-values
    * KDTreeQueryResultsTags()          tag values
    * KDTreeQueryResultsDistances()     distances

      -- ALGLIB --
         Copyright 28.02.2010 by Bochkanov Sergey
    *************************************************************************/
    public static void kdtreequeryresultsxy(kdtree kdt, ref double[,] xy)
    {

        nearestneighbor.kdtreequeryresultsxy(kdt.innerobj, ref xy);
        return;
    }

    /*************************************************************************
    Tags from last query

    INPUT PARAMETERS
        KDT     -   KD-tree
        Tags    -   possibly pre-allocated buffer. If X is too small to store
                    result, it is resized. If size(X) is enough to store
                    result, it is left unchanged.

    OUTPUT PARAMETERS
        Tags    -   filled with tags associated with points,
                    or, when no tags were supplied, with zeros

    NOTES
    1. points are ordered by distance from the query point (first = closest)
    2. if  XY is larger than required to store result, only leading part  will
       be overwritten; trailing part will be left unchanged. So  if  on  input
       XY = [[A,B],[C,D]], and result is [1,2],  then  on  exit  we  will  get
       XY = [[1,2],[C,D]]. This is done purposely to increase performance;  if
       you want function  to  resize  array  according  to  result  size,  use
       function with same name and suffix 'I'.

    SEE ALSO
    * KDTreeQueryResultsX()             X-values
    * KDTreeQueryResultsXY()            X- and Y-values
    * KDTreeQueryResultsDistances()     distances

      -- ALGLIB --
         Copyright 28.02.2010 by Bochkanov Sergey
    *************************************************************************/
    public static void kdtreequeryresultstags(kdtree kdt, ref int[] tags)
    {

        nearestneighbor.kdtreequeryresultstags(kdt.innerobj, ref tags);
        return;
    }

    /*************************************************************************
    Distances from last query

    INPUT PARAMETERS
        KDT     -   KD-tree
        R       -   possibly pre-allocated buffer. If X is too small to store
                    result, it is resized. If size(X) is enough to store
                    result, it is left unchanged.

    OUTPUT PARAMETERS
        R       -   filled with distances (in corresponding norm)

    NOTES
    1. points are ordered by distance from the query point (first = closest)
    2. if  XY is larger than required to store result, only leading part  will
       be overwritten; trailing part will be left unchanged. So  if  on  input
       XY = [[A,B],[C,D]], and result is [1,2],  then  on  exit  we  will  get
       XY = [[1,2],[C,D]]. This is done purposely to increase performance;  if
       you want function  to  resize  array  according  to  result  size,  use
       function with same name and suffix 'I'.

    SEE ALSO
    * KDTreeQueryResultsX()             X-values
    * KDTreeQueryResultsXY()            X- and Y-values
    * KDTreeQueryResultsTags()          tag values

      -- ALGLIB --
         Copyright 28.02.2010 by Bochkanov Sergey
    *************************************************************************/
    public static void kdtreequeryresultsdistances(kdtree kdt, ref double[] r)
    {

        nearestneighbor.kdtreequeryresultsdistances(kdt.innerobj, ref r);
        return;
    }

    /*************************************************************************
    X-values from last query; 'interactive' variant for languages like  Python
    which   support    constructs   like  "X = KDTreeQueryResultsXI(KDT)"  and
    interactive mode of interpreter.

    This function allocates new array on each call,  so  it  is  significantly
    slower than its 'non-interactive' counterpart, but it is  more  convenient
    when you call it from command line.

      -- ALGLIB --
         Copyright 28.02.2010 by Bochkanov Sergey
    *************************************************************************/
    public static void kdtreequeryresultsxi(kdtree kdt, out double[,] x)
    {
        x = new double[0,0];
        nearestneighbor.kdtreequeryresultsxi(kdt.innerobj, ref x);
        return;
    }

    /*************************************************************************
    XY-values from last query; 'interactive' variant for languages like Python
    which   support    constructs   like "XY = KDTreeQueryResultsXYI(KDT)" and
    interactive mode of interpreter.

    This function allocates new array on each call,  so  it  is  significantly
    slower than its 'non-interactive' counterpart, but it is  more  convenient
    when you call it from command line.

      -- ALGLIB --
         Copyright 28.02.2010 by Bochkanov Sergey
    *************************************************************************/
    public static void kdtreequeryresultsxyi(kdtree kdt, out double[,] xy)
    {
        xy = new double[0,0];
        nearestneighbor.kdtreequeryresultsxyi(kdt.innerobj, ref xy);
        return;
    }

    /*************************************************************************
    Tags  from  last  query;  'interactive' variant for languages like  Python
    which  support  constructs  like "Tags = KDTreeQueryResultsTagsI(KDT)" and
    interactive mode of interpreter.

    This function allocates new array on each call,  so  it  is  significantly
    slower than its 'non-interactive' counterpart, but it is  more  convenient
    when you call it from command line.

      -- ALGLIB --
         Copyright 28.02.2010 by Bochkanov Sergey
    *************************************************************************/
    public static void kdtreequeryresultstagsi(kdtree kdt, out int[] tags)
    {
        tags = new int[0];
        nearestneighbor.kdtreequeryresultstagsi(kdt.innerobj, ref tags);
        return;
    }

    /*************************************************************************
    Distances from last query; 'interactive' variant for languages like Python
    which  support  constructs   like  "R = KDTreeQueryResultsDistancesI(KDT)"
    and interactive mode of interpreter.

    This function allocates new array on each call,  so  it  is  significantly
    slower than its 'non-interactive' counterpart, but it is  more  convenient
    when you call it from command line.

      -- ALGLIB --
         Copyright 28.02.2010 by Bochkanov Sergey
    *************************************************************************/
    public static void kdtreequeryresultsdistancesi(kdtree kdt, out double[] r)
    {
        r = new double[0];
        nearestneighbor.kdtreequeryresultsdistancesi(kdt.innerobj, ref r);
        return;
    }

}
public partial class alglib
{
    public class hqrnd
    {
        /*************************************************************************
        Portable high quality random number generator state.
        Initialized with HQRNDRandomize() or HQRNDSeed().

        Fields:
            S1, S2      -   seed values
            V           -   precomputed value
            MagicV      -   'magic' value used to determine whether State structure
                            was correctly initialized.
        *************************************************************************/
        public class hqrndstate
        {
            public int s1;
            public int s2;
            public double v;
            public int magicv;
        };




        public const int hqrndmax = 2147483563;
        public const int hqrndm1 = 2147483563;
        public const int hqrndm2 = 2147483399;
        public const int hqrndmagic = 1634357784;


        /*************************************************************************
        HQRNDState  initialization  with  random  values  which come from standard
        RNG.

          -- ALGLIB --
             Copyright 02.12.2009 by Bochkanov Sergey
        *************************************************************************/
        public static void hqrndrandomize(hqrndstate state)
        {
            hqrndseed(math.randominteger(hqrndm1), math.randominteger(hqrndm2), state);
        }


        /*************************************************************************
        HQRNDState initialization with seed values

          -- ALGLIB --
             Copyright 02.12.2009 by Bochkanov Sergey
        *************************************************************************/
        public static void hqrndseed(int s1,
            int s2,
            hqrndstate state)
        {
            state.s1 = s1%(hqrndm1-1)+1;
            state.s2 = s2%(hqrndm2-1)+1;
            state.v = (double)1/(double)hqrndmax;
            state.magicv = hqrndmagic;
        }


        /*************************************************************************
        This function generates random real number in (0,1),
        not including interval boundaries

        State structure must be initialized with HQRNDRandomize() or HQRNDSeed().

          -- ALGLIB --
             Copyright 02.12.2009 by Bochkanov Sergey
        *************************************************************************/
        public static double hqrnduniformr(hqrndstate state)
        {
            double result = 0;

            result = state.v*hqrndintegerbase(state);
            return result;
        }


        /*************************************************************************
        This function generates random integer number in [0, N)

        1. N must be less than HQRNDMax-1.
        2. State structure must be initialized with HQRNDRandomize() or HQRNDSeed()

          -- ALGLIB --
             Copyright 02.12.2009 by Bochkanov Sergey
        *************************************************************************/
        public static int hqrnduniformi(hqrndstate state,
            int n)
        {
            int result = 0;
            int mx = 0;

            
            //
            // Correct handling of N's close to RNDBaseMax
            // (avoiding skewed distributions for RNDBaseMax<>K*N)
            //
            ap.assert(n>0, "HQRNDUniformI: N<=0!");
            ap.assert(n<hqrndmax-1, "HQRNDUniformI: N>=RNDBaseMax-1!");
            mx = hqrndmax-1-(hqrndmax-1)%n;
            do
            {
                result = hqrndintegerbase(state)-1;
            }
            while( result>=mx );
            result = result%n;
            return result;
        }


        /*************************************************************************
        Random number generator: normal numbers

        This function generates one random number from normal distribution.
        Its performance is equal to that of HQRNDNormal2()

        State structure must be initialized with HQRNDRandomize() or HQRNDSeed().

          -- ALGLIB --
             Copyright 02.12.2009 by Bochkanov Sergey
        *************************************************************************/
        public static double hqrndnormal(hqrndstate state)
        {
            double result = 0;
            double v1 = 0;
            double v2 = 0;

            hqrndnormal2(state, ref v1, ref v2);
            result = v1;
            return result;
        }


        /*************************************************************************
        Random number generator: random X and Y such that X^2+Y^2=1

        State structure must be initialized with HQRNDRandomize() or HQRNDSeed().

          -- ALGLIB --
             Copyright 02.12.2009 by Bochkanov Sergey
        *************************************************************************/
        public static void hqrndunit2(hqrndstate state,
            ref double x,
            ref double y)
        {
            double v = 0;
            double mx = 0;
            double mn = 0;

            x = 0;
            y = 0;

            do
            {
                hqrndnormal2(state, ref x, ref y);
            }
            while( !((double)(x)!=(double)(0) | (double)(y)!=(double)(0)) );
            mx = Math.Max(Math.Abs(x), Math.Abs(y));
            mn = Math.Min(Math.Abs(x), Math.Abs(y));
            v = mx*Math.Sqrt(1+math.sqr(mn/mx));
            x = x/v;
            y = y/v;
        }


        /*************************************************************************
        Random number generator: normal numbers

        This function generates two independent random numbers from normal
        distribution. Its performance is equal to that of HQRNDNormal()

        State structure must be initialized with HQRNDRandomize() or HQRNDSeed().

          -- ALGLIB --
             Copyright 02.12.2009 by Bochkanov Sergey
        *************************************************************************/
        public static void hqrndnormal2(hqrndstate state,
            ref double x1,
            ref double x2)
        {
            double u = 0;
            double v = 0;
            double s = 0;

            x1 = 0;
            x2 = 0;

            while( true )
            {
                u = 2*hqrnduniformr(state)-1;
                v = 2*hqrnduniformr(state)-1;
                s = math.sqr(u)+math.sqr(v);
                if( (double)(s)>(double)(0) & (double)(s)<(double)(1) )
                {
                    
                    //
                    // two Sqrt's instead of one to
                    // avoid overflow when S is too small
                    //
                    s = Math.Sqrt(-(2*Math.Log(s)))/Math.Sqrt(s);
                    x1 = u*s;
                    x2 = v*s;
                    return;
                }
            }
        }


        /*************************************************************************
        Random number generator: exponential distribution

        State structure must be initialized with HQRNDRandomize() or HQRNDSeed().

          -- ALGLIB --
             Copyright 11.08.2007 by Bochkanov Sergey
        *************************************************************************/
        public static double hqrndexponential(hqrndstate state,
            double lambdav)
        {
            double result = 0;

            ap.assert((double)(lambdav)>(double)(0), "HQRNDExponential: LambdaV<=0!");
            result = -(Math.Log(hqrnduniformr(state))/lambdav);
            return result;
        }


        /*************************************************************************

        L'Ecuyer, Efficient and portable combined random number generators
        *************************************************************************/
        private static int hqrndintegerbase(hqrndstate state)
        {
            int result = 0;
            int k = 0;

            ap.assert(state.magicv==hqrndmagic, "HQRNDIntegerBase: State is not correctly initialized!");
            k = state.s1/53668;
            state.s1 = 40014*(state.s1-k*53668)-k*12211;
            if( state.s1<0 )
            {
                state.s1 = state.s1+2147483563;
            }
            k = state.s2/52774;
            state.s2 = 40692*(state.s2-k*52774)-k*3791;
            if( state.s2<0 )
            {
                state.s2 = state.s2+2147483399;
            }
            
            //
            // Result
            //
            result = state.s1-state.s2;
            if( result<1 )
            {
                result = result+2147483562;
            }
            return result;
        }


    }
    public class nearestneighbor
    {
        public class kdtree
        {
            public int n;
            public int nx;
            public int ny;
            public int normtype;
            public double[,] xy;
            public int[] tags;
            public double[] boxmin;
            public double[] boxmax;
            public int[] nodes;
            public double[] splits;
            public double[] x;
            public int kneeded;
            public double rneeded;
            public bool selfmatch;
            public double approxf;
            public int kcur;
            public int[] idx;
            public double[] r;
            public double[] buf;
            public double[] curboxmin;
            public double[] curboxmax;
            public double curdist;
            public int debugcounter;
            public kdtree()
            {
                xy = new double[0,0];
                tags = new int[0];
                boxmin = new double[0];
                boxmax = new double[0];
                nodes = new int[0];
                splits = new double[0];
                x = new double[0];
                idx = new int[0];
                r = new double[0];
                buf = new double[0];
                curboxmin = new double[0];
                curboxmax = new double[0];
            }
        };




        public const int splitnodesize = 6;
        public const int kdtreefirstversion = 0;


        /*************************************************************************
        KD-tree creation

        This subroutine creates KD-tree from set of X-values and optional Y-values

        INPUT PARAMETERS
            XY      -   dataset, array[0..N-1,0..NX+NY-1].
                        one row corresponds to one point.
                        first NX columns contain X-values, next NY (NY may be zero)
                        columns may contain associated Y-values
            N       -   number of points, N>=1
            NX      -   space dimension, NX>=1.
            NY      -   number of optional Y-values, NY>=0.
            NormType-   norm type:
                        * 0 denotes infinity-norm
                        * 1 denotes 1-norm
                        * 2 denotes 2-norm (Euclidean norm)
                        
        OUTPUT PARAMETERS
            KDT     -   KD-tree
            
            
        NOTES

        1. KD-tree  creation  have O(N*logN) complexity and O(N*(2*NX+NY))  memory
           requirements.
        2. Although KD-trees may be used with any combination of N  and  NX,  they
           are more efficient than brute-force search only when N >> 4^NX. So they
           are most useful in low-dimensional tasks (NX=2, NX=3). NX=1  is another
           inefficient case, because  simple  binary  search  (without  additional
           structures) is much more efficient in such tasks than KD-trees.

          -- ALGLIB --
             Copyright 28.02.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void kdtreebuild(double[,] xy,
            int n,
            int nx,
            int ny,
            int normtype,
            kdtree kdt)
        {
            int[] tags = new int[0];
            int i = 0;

            ap.assert(n>=1, "KDTreeBuild: N<1!");
            ap.assert(nx>=1, "KDTreeBuild: NX<1!");
            ap.assert(ny>=0, "KDTreeBuild: NY<0!");
            ap.assert(normtype>=0 & normtype<=2, "KDTreeBuild: incorrect NormType!");
            ap.assert(ap.rows(xy)>=n, "KDTreeBuild: rows(X)<N!");
            ap.assert(ap.cols(xy)>=nx+ny, "KDTreeBuild: cols(X)<NX+NY!");
            ap.assert(apserv.apservisfinitematrix(xy, n, nx+ny), "KDTreeBuild: X contains infinite or NaN values!");
            tags = new int[n];
            for(i=0; i<=n-1; i++)
            {
                tags[i] = 0;
            }
            kdtreebuildtagged(xy, tags, n, nx, ny, normtype, kdt);
        }


        /*************************************************************************
        KD-tree creation

        This  subroutine  creates  KD-tree  from set of X-values, integer tags and
        optional Y-values

        INPUT PARAMETERS
            XY      -   dataset, array[0..N-1,0..NX+NY-1].
                        one row corresponds to one point.
                        first NX columns contain X-values, next NY (NY may be zero)
                        columns may contain associated Y-values
            Tags    -   tags, array[0..N-1], contains integer tags associated
                        with points.
            N       -   number of points, N>=1
            NX      -   space dimension, NX>=1.
            NY      -   number of optional Y-values, NY>=0.
            NormType-   norm type:
                        * 0 denotes infinity-norm
                        * 1 denotes 1-norm
                        * 2 denotes 2-norm (Euclidean norm)

        OUTPUT PARAMETERS
            KDT     -   KD-tree

        NOTES

        1. KD-tree  creation  have O(N*logN) complexity and O(N*(2*NX+NY))  memory
           requirements.
        2. Although KD-trees may be used with any combination of N  and  NX,  they
           are more efficient than brute-force search only when N >> 4^NX. So they
           are most useful in low-dimensional tasks (NX=2, NX=3). NX=1  is another
           inefficient case, because  simple  binary  search  (without  additional
           structures) is much more efficient in such tasks than KD-trees.

          -- ALGLIB --
             Copyright 28.02.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void kdtreebuildtagged(double[,] xy,
            int[] tags,
            int n,
            int nx,
            int ny,
            int normtype,
            kdtree kdt)
        {
            int i = 0;
            int j = 0;
            int maxnodes = 0;
            int nodesoffs = 0;
            int splitsoffs = 0;
            int i_ = 0;
            int i1_ = 0;

            ap.assert(n>=1, "KDTreeBuildTagged: N<1!");
            ap.assert(nx>=1, "KDTreeBuildTagged: NX<1!");
            ap.assert(ny>=0, "KDTreeBuildTagged: NY<0!");
            ap.assert(normtype>=0 & normtype<=2, "KDTreeBuildTagged: incorrect NormType!");
            ap.assert(ap.rows(xy)>=n, "KDTreeBuildTagged: rows(X)<N!");
            ap.assert(ap.cols(xy)>=nx+ny, "KDTreeBuildTagged: cols(X)<NX+NY!");
            ap.assert(apserv.apservisfinitematrix(xy, n, nx+ny), "KDTreeBuildTagged: X contains infinite or NaN values!");
            
            //
            // initialize
            //
            kdt.n = n;
            kdt.nx = nx;
            kdt.ny = ny;
            kdt.normtype = normtype;
            
            //
            // Allocate
            //
            kdtreeallocdatasetindependent(kdt, nx, ny);
            kdtreeallocdatasetdependent(kdt, n, nx, ny);
            
            //
            // Initial fill
            //
            for(i=0; i<=n-1; i++)
            {
                for(i_=0; i_<=nx-1;i_++)
                {
                    kdt.xy[i,i_] = xy[i,i_];
                }
                i1_ = (0) - (nx);
                for(i_=nx; i_<=2*nx+ny-1;i_++)
                {
                    kdt.xy[i,i_] = xy[i,i_+i1_];
                }
                kdt.tags[i] = tags[i];
            }
            
            //
            // Determine bounding box
            //
            for(i_=0; i_<=nx-1;i_++)
            {
                kdt.boxmin[i_] = kdt.xy[0,i_];
            }
            for(i_=0; i_<=nx-1;i_++)
            {
                kdt.boxmax[i_] = kdt.xy[0,i_];
            }
            for(i=1; i<=n-1; i++)
            {
                for(j=0; j<=nx-1; j++)
                {
                    kdt.boxmin[j] = Math.Min(kdt.boxmin[j], kdt.xy[i,j]);
                    kdt.boxmax[j] = Math.Max(kdt.boxmax[j], kdt.xy[i,j]);
                }
            }
            
            //
            // prepare tree structure
            // * MaxNodes=N because we guarantee no trivial splits, i.e.
            //   every split will generate two non-empty boxes
            //
            maxnodes = n;
            kdt.nodes = new int[splitnodesize*2*maxnodes];
            kdt.splits = new double[2*maxnodes];
            nodesoffs = 0;
            splitsoffs = 0;
            for(i_=0; i_<=nx-1;i_++)
            {
                kdt.curboxmin[i_] = kdt.boxmin[i_];
            }
            for(i_=0; i_<=nx-1;i_++)
            {
                kdt.curboxmax[i_] = kdt.boxmax[i_];
            }
            kdtreegeneratetreerec(kdt, ref nodesoffs, ref splitsoffs, 0, n, 8);
            
            //
            // Set current query size to 0
            //
            kdt.kcur = 0;
        }


        /*************************************************************************
        K-NN query: K nearest neighbors

        INPUT PARAMETERS
            KDT         -   KD-tree
            X           -   point, array[0..NX-1].
            K           -   number of neighbors to return, K>=1
            SelfMatch   -   whether self-matches are allowed:
                            * if True, nearest neighbor may be the point itself
                              (if it exists in original dataset)
                            * if False, then only points with non-zero distance
                              are returned
                            * if not given, considered True

        RESULT
            number of actual neighbors found (either K or N, if K>N).

        This  subroutine  performs  query  and  stores  its result in the internal
        structures of the KD-tree. You can use  following  subroutines  to  obtain
        these results:
        * KDTreeQueryResultsX() to get X-values
        * KDTreeQueryResultsXY() to get X- and Y-values
        * KDTreeQueryResultsTags() to get tag values
        * KDTreeQueryResultsDistances() to get distances

          -- ALGLIB --
             Copyright 28.02.2010 by Bochkanov Sergey
        *************************************************************************/
        public static int kdtreequeryknn(kdtree kdt,
            double[] x,
            int k,
            bool selfmatch)
        {
            int result = 0;

            ap.assert(k>=1, "KDTreeQueryKNN: K<1!");
            ap.assert(ap.len(x)>=kdt.nx, "KDTreeQueryKNN: Length(X)<NX!");
            ap.assert(apserv.isfinitevector(x, kdt.nx), "KDTreeQueryKNN: X contains infinite or NaN values!");
            result = kdtreequeryaknn(kdt, x, k, selfmatch, 0.0);
            return result;
        }


        /*************************************************************************
        R-NN query: all points within R-sphere centered at X

        INPUT PARAMETERS
            KDT         -   KD-tree
            X           -   point, array[0..NX-1].
            R           -   radius of sphere (in corresponding norm), R>0
            SelfMatch   -   whether self-matches are allowed:
                            * if True, nearest neighbor may be the point itself
                              (if it exists in original dataset)
                            * if False, then only points with non-zero distance
                              are returned
                            * if not given, considered True

        RESULT
            number of neighbors found, >=0

        This  subroutine  performs  query  and  stores  its result in the internal
        structures of the KD-tree. You can use  following  subroutines  to  obtain
        actual results:
        * KDTreeQueryResultsX() to get X-values
        * KDTreeQueryResultsXY() to get X- and Y-values
        * KDTreeQueryResultsTags() to get tag values
        * KDTreeQueryResultsDistances() to get distances

          -- ALGLIB --
             Copyright 28.02.2010 by Bochkanov Sergey
        *************************************************************************/
        public static int kdtreequeryrnn(kdtree kdt,
            double[] x,
            double r,
            bool selfmatch)
        {
            int result = 0;
            int i = 0;
            int j = 0;

            ap.assert((double)(r)>(double)(0), "KDTreeQueryRNN: incorrect R!");
            ap.assert(ap.len(x)>=kdt.nx, "KDTreeQueryRNN: Length(X)<NX!");
            ap.assert(apserv.isfinitevector(x, kdt.nx), "KDTreeQueryRNN: X contains infinite or NaN values!");
            
            //
            // Prepare parameters
            //
            kdt.kneeded = 0;
            if( kdt.normtype!=2 )
            {
                kdt.rneeded = r;
            }
            else
            {
                kdt.rneeded = math.sqr(r);
            }
            kdt.selfmatch = selfmatch;
            kdt.approxf = 1;
            kdt.kcur = 0;
            
            //
            // calculate distance from point to current bounding box
            //
            kdtreeinitbox(kdt, x);
            
            //
            // call recursive search
            // results are returned as heap
            //
            kdtreequerynnrec(kdt, 0);
            
            //
            // pop from heap to generate ordered representation
            //
            // last element is not pop'ed because it is already in
            // its place
            //
            result = kdt.kcur;
            j = kdt.kcur;
            for(i=kdt.kcur; i>=2; i--)
            {
                tsort.tagheappopi(ref kdt.r, ref kdt.idx, ref j);
            }
            return result;
        }


        /*************************************************************************
        K-NN query: approximate K nearest neighbors

        INPUT PARAMETERS
            KDT         -   KD-tree
            X           -   point, array[0..NX-1].
            K           -   number of neighbors to return, K>=1
            SelfMatch   -   whether self-matches are allowed:
                            * if True, nearest neighbor may be the point itself
                              (if it exists in original dataset)
                            * if False, then only points with non-zero distance
                              are returned
                            * if not given, considered True
            Eps         -   approximation factor, Eps>=0. eps-approximate  nearest
                            neighbor  is  a  neighbor  whose distance from X is at
                            most (1+eps) times distance of true nearest neighbor.

        RESULT
            number of actual neighbors found (either K or N, if K>N).
            
        NOTES
            significant performance gain may be achieved only when Eps  is  is  on
            the order of magnitude of 1 or larger.

        This  subroutine  performs  query  and  stores  its result in the internal
        structures of the KD-tree. You can use  following  subroutines  to  obtain
        these results:
        * KDTreeQueryResultsX() to get X-values
        * KDTreeQueryResultsXY() to get X- and Y-values
        * KDTreeQueryResultsTags() to get tag values
        * KDTreeQueryResultsDistances() to get distances

          -- ALGLIB --
             Copyright 28.02.2010 by Bochkanov Sergey
        *************************************************************************/
        public static int kdtreequeryaknn(kdtree kdt,
            double[] x,
            int k,
            bool selfmatch,
            double eps)
        {
            int result = 0;
            int i = 0;
            int j = 0;

            ap.assert(k>0, "KDTreeQueryAKNN: incorrect K!");
            ap.assert((double)(eps)>=(double)(0), "KDTreeQueryAKNN: incorrect Eps!");
            ap.assert(ap.len(x)>=kdt.nx, "KDTreeQueryAKNN: Length(X)<NX!");
            ap.assert(apserv.isfinitevector(x, kdt.nx), "KDTreeQueryAKNN: X contains infinite or NaN values!");
            
            //
            // Prepare parameters
            //
            k = Math.Min(k, kdt.n);
            kdt.kneeded = k;
            kdt.rneeded = 0;
            kdt.selfmatch = selfmatch;
            if( kdt.normtype==2 )
            {
                kdt.approxf = 1/math.sqr(1+eps);
            }
            else
            {
                kdt.approxf = 1/(1+eps);
            }
            kdt.kcur = 0;
            
            //
            // calculate distance from point to current bounding box
            //
            kdtreeinitbox(kdt, x);
            
            //
            // call recursive search
            // results are returned as heap
            //
            kdtreequerynnrec(kdt, 0);
            
            //
            // pop from heap to generate ordered representation
            //
            // last element is non pop'ed because it is already in
            // its place
            //
            result = kdt.kcur;
            j = kdt.kcur;
            for(i=kdt.kcur; i>=2; i--)
            {
                tsort.tagheappopi(ref kdt.r, ref kdt.idx, ref j);
            }
            return result;
        }


        /*************************************************************************
        X-values from last query

        INPUT PARAMETERS
            KDT     -   KD-tree
            X       -   possibly pre-allocated buffer. If X is too small to store
                        result, it is resized. If size(X) is enough to store
                        result, it is left unchanged.

        OUTPUT PARAMETERS
            X       -   rows are filled with X-values

        NOTES
        1. points are ordered by distance from the query point (first = closest)
        2. if  XY is larger than required to store result, only leading part  will
           be overwritten; trailing part will be left unchanged. So  if  on  input
           XY = [[A,B],[C,D]], and result is [1,2],  then  on  exit  we  will  get
           XY = [[1,2],[C,D]]. This is done purposely to increase performance;  if
           you want function  to  resize  array  according  to  result  size,  use
           function with same name and suffix 'I'.

        SEE ALSO
        * KDTreeQueryResultsXY()            X- and Y-values
        * KDTreeQueryResultsTags()          tag values
        * KDTreeQueryResultsDistances()     distances

          -- ALGLIB --
             Copyright 28.02.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void kdtreequeryresultsx(kdtree kdt,
            ref double[,] x)
        {
            int i = 0;
            int k = 0;
            int i_ = 0;
            int i1_ = 0;

            if( kdt.kcur==0 )
            {
                return;
            }
            if( ap.rows(x)<kdt.kcur | ap.cols(x)<kdt.nx )
            {
                x = new double[kdt.kcur, kdt.nx];
            }
            k = kdt.kcur;
            for(i=0; i<=k-1; i++)
            {
                i1_ = (kdt.nx) - (0);
                for(i_=0; i_<=kdt.nx-1;i_++)
                {
                    x[i,i_] = kdt.xy[kdt.idx[i],i_+i1_];
                }
            }
        }


        /*************************************************************************
        X- and Y-values from last query

        INPUT PARAMETERS
            KDT     -   KD-tree
            XY      -   possibly pre-allocated buffer. If XY is too small to store
                        result, it is resized. If size(XY) is enough to store
                        result, it is left unchanged.

        OUTPUT PARAMETERS
            XY      -   rows are filled with points: first NX columns with
                        X-values, next NY columns - with Y-values.

        NOTES
        1. points are ordered by distance from the query point (first = closest)
        2. if  XY is larger than required to store result, only leading part  will
           be overwritten; trailing part will be left unchanged. So  if  on  input
           XY = [[A,B],[C,D]], and result is [1,2],  then  on  exit  we  will  get
           XY = [[1,2],[C,D]]. This is done purposely to increase performance;  if
           you want function  to  resize  array  according  to  result  size,  use
           function with same name and suffix 'I'.

        SEE ALSO
        * KDTreeQueryResultsX()             X-values
        * KDTreeQueryResultsTags()          tag values
        * KDTreeQueryResultsDistances()     distances

          -- ALGLIB --
             Copyright 28.02.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void kdtreequeryresultsxy(kdtree kdt,
            ref double[,] xy)
        {
            int i = 0;
            int k = 0;
            int i_ = 0;
            int i1_ = 0;

            if( kdt.kcur==0 )
            {
                return;
            }
            if( ap.rows(xy)<kdt.kcur | ap.cols(xy)<kdt.nx+kdt.ny )
            {
                xy = new double[kdt.kcur, kdt.nx+kdt.ny];
            }
            k = kdt.kcur;
            for(i=0; i<=k-1; i++)
            {
                i1_ = (kdt.nx) - (0);
                for(i_=0; i_<=kdt.nx+kdt.ny-1;i_++)
                {
                    xy[i,i_] = kdt.xy[kdt.idx[i],i_+i1_];
                }
            }
        }


        /*************************************************************************
        Tags from last query

        INPUT PARAMETERS
            KDT     -   KD-tree
            Tags    -   possibly pre-allocated buffer. If X is too small to store
                        result, it is resized. If size(X) is enough to store
                        result, it is left unchanged.

        OUTPUT PARAMETERS
            Tags    -   filled with tags associated with points,
                        or, when no tags were supplied, with zeros

        NOTES
        1. points are ordered by distance from the query point (first = closest)
        2. if  XY is larger than required to store result, only leading part  will
           be overwritten; trailing part will be left unchanged. So  if  on  input
           XY = [[A,B],[C,D]], and result is [1,2],  then  on  exit  we  will  get
           XY = [[1,2],[C,D]]. This is done purposely to increase performance;  if
           you want function  to  resize  array  according  to  result  size,  use
           function with same name and suffix 'I'.

        SEE ALSO
        * KDTreeQueryResultsX()             X-values
        * KDTreeQueryResultsXY()            X- and Y-values
        * KDTreeQueryResultsDistances()     distances

          -- ALGLIB --
             Copyright 28.02.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void kdtreequeryresultstags(kdtree kdt,
            ref int[] tags)
        {
            int i = 0;
            int k = 0;

            if( kdt.kcur==0 )
            {
                return;
            }
            if( ap.len(tags)<kdt.kcur )
            {
                tags = new int[kdt.kcur];
            }
            k = kdt.kcur;
            for(i=0; i<=k-1; i++)
            {
                tags[i] = kdt.tags[kdt.idx[i]];
            }
        }


        /*************************************************************************
        Distances from last query

        INPUT PARAMETERS
            KDT     -   KD-tree
            R       -   possibly pre-allocated buffer. If X is too small to store
                        result, it is resized. If size(X) is enough to store
                        result, it is left unchanged.

        OUTPUT PARAMETERS
            R       -   filled with distances (in corresponding norm)

        NOTES
        1. points are ordered by distance from the query point (first = closest)
        2. if  XY is larger than required to store result, only leading part  will
           be overwritten; trailing part will be left unchanged. So  if  on  input
           XY = [[A,B],[C,D]], and result is [1,2],  then  on  exit  we  will  get
           XY = [[1,2],[C,D]]. This is done purposely to increase performance;  if
           you want function  to  resize  array  according  to  result  size,  use
           function with same name and suffix 'I'.

        SEE ALSO
        * KDTreeQueryResultsX()             X-values
        * KDTreeQueryResultsXY()            X- and Y-values
        * KDTreeQueryResultsTags()          tag values

          -- ALGLIB --
             Copyright 28.02.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void kdtreequeryresultsdistances(kdtree kdt,
            ref double[] r)
        {
            int i = 0;
            int k = 0;

            if( kdt.kcur==0 )
            {
                return;
            }
            if( ap.len(r)<kdt.kcur )
            {
                r = new double[kdt.kcur];
            }
            k = kdt.kcur;
            
            //
            // unload norms
            //
            // Abs() call is used to handle cases with negative norms
            // (generated during KFN requests)
            //
            if( kdt.normtype==0 )
            {
                for(i=0; i<=k-1; i++)
                {
                    r[i] = Math.Abs(kdt.r[i]);
                }
            }
            if( kdt.normtype==1 )
            {
                for(i=0; i<=k-1; i++)
                {
                    r[i] = Math.Abs(kdt.r[i]);
                }
            }
            if( kdt.normtype==2 )
            {
                for(i=0; i<=k-1; i++)
                {
                    r[i] = Math.Sqrt(Math.Abs(kdt.r[i]));
                }
            }
        }


        /*************************************************************************
        X-values from last query; 'interactive' variant for languages like  Python
        which   support    constructs   like  "X = KDTreeQueryResultsXI(KDT)"  and
        interactive mode of interpreter.

        This function allocates new array on each call,  so  it  is  significantly
        slower than its 'non-interactive' counterpart, but it is  more  convenient
        when you call it from command line.

          -- ALGLIB --
             Copyright 28.02.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void kdtreequeryresultsxi(kdtree kdt,
            ref double[,] x)
        {
            x = new double[0,0];

            kdtreequeryresultsx(kdt, ref x);
        }


        /*************************************************************************
        XY-values from last query; 'interactive' variant for languages like Python
        which   support    constructs   like "XY = KDTreeQueryResultsXYI(KDT)" and
        interactive mode of interpreter.

        This function allocates new array on each call,  so  it  is  significantly
        slower than its 'non-interactive' counterpart, but it is  more  convenient
        when you call it from command line.

          -- ALGLIB --
             Copyright 28.02.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void kdtreequeryresultsxyi(kdtree kdt,
            ref double[,] xy)
        {
            xy = new double[0,0];

            kdtreequeryresultsxy(kdt, ref xy);
        }


        /*************************************************************************
        Tags  from  last  query;  'interactive' variant for languages like  Python
        which  support  constructs  like "Tags = KDTreeQueryResultsTagsI(KDT)" and
        interactive mode of interpreter.

        This function allocates new array on each call,  so  it  is  significantly
        slower than its 'non-interactive' counterpart, but it is  more  convenient
        when you call it from command line.

          -- ALGLIB --
             Copyright 28.02.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void kdtreequeryresultstagsi(kdtree kdt,
            ref int[] tags)
        {
            tags = new int[0];

            kdtreequeryresultstags(kdt, ref tags);
        }


        /*************************************************************************
        Distances from last query; 'interactive' variant for languages like Python
        which  support  constructs   like  "R = KDTreeQueryResultsDistancesI(KDT)"
        and interactive mode of interpreter.

        This function allocates new array on each call,  so  it  is  significantly
        slower than its 'non-interactive' counterpart, but it is  more  convenient
        when you call it from command line.

          -- ALGLIB --
             Copyright 28.02.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void kdtreequeryresultsdistancesi(kdtree kdt,
            ref double[] r)
        {
            r = new double[0];

            kdtreequeryresultsdistances(kdt, ref r);
        }


        /*************************************************************************
        Serializer: allocation

          -- ALGLIB --
             Copyright 14.03.2011 by Bochkanov Sergey
        *************************************************************************/
        public static void kdtreealloc(alglib.serializer s,
            kdtree tree)
        {
            
            //
            // Header
            //
            s.alloc_entry();
            s.alloc_entry();
            
            //
            // Data
            //
            s.alloc_entry();
            s.alloc_entry();
            s.alloc_entry();
            s.alloc_entry();
            apserv.allocrealmatrix(s, tree.xy, -1, -1);
            apserv.allocintegerarray(s, tree.tags, -1);
            apserv.allocrealarray(s, tree.boxmin, -1);
            apserv.allocrealarray(s, tree.boxmax, -1);
            apserv.allocintegerarray(s, tree.nodes, -1);
            apserv.allocrealarray(s, tree.splits, -1);
        }


        /*************************************************************************
        Serializer: serialization

          -- ALGLIB --
             Copyright 14.03.2011 by Bochkanov Sergey
        *************************************************************************/
        public static void kdtreeserialize(alglib.serializer s,
            kdtree tree)
        {
            
            //
            // Header
            //
            s.serialize_int(scodes.getkdtreeserializationcode());
            s.serialize_int(kdtreefirstversion);
            
            //
            // Data
            //
            s.serialize_int(tree.n);
            s.serialize_int(tree.nx);
            s.serialize_int(tree.ny);
            s.serialize_int(tree.normtype);
            apserv.serializerealmatrix(s, tree.xy, -1, -1);
            apserv.serializeintegerarray(s, tree.tags, -1);
            apserv.serializerealarray(s, tree.boxmin, -1);
            apserv.serializerealarray(s, tree.boxmax, -1);
            apserv.serializeintegerarray(s, tree.nodes, -1);
            apserv.serializerealarray(s, tree.splits, -1);
        }


        /*************************************************************************
        Serializer: unserialization

          -- ALGLIB --
             Copyright 14.03.2011 by Bochkanov Sergey
        *************************************************************************/
        public static void kdtreeunserialize(alglib.serializer s,
            kdtree tree)
        {
            int i0 = 0;
            int i1 = 0;

            
            //
            // check correctness of header
            //
            i0 = s.unserialize_int();
            ap.assert(i0==scodes.getkdtreeserializationcode(), "KDTreeUnserialize: stream header corrupted");
            i1 = s.unserialize_int();
            ap.assert(i1==kdtreefirstversion, "KDTreeUnserialize: stream header corrupted");
            
            //
            // Unserialize data
            //
            tree.n = s.unserialize_int();
            tree.nx = s.unserialize_int();
            tree.ny = s.unserialize_int();
            tree.normtype = s.unserialize_int();
            apserv.unserializerealmatrix(s, ref tree.xy);
            apserv.unserializeintegerarray(s, ref tree.tags);
            apserv.unserializerealarray(s, ref tree.boxmin);
            apserv.unserializerealarray(s, ref tree.boxmax);
            apserv.unserializeintegerarray(s, ref tree.nodes);
            apserv.unserializerealarray(s, ref tree.splits);
            kdtreealloctemporaries(tree, tree.n, tree.nx, tree.ny);
        }


        /*************************************************************************
        Rearranges nodes [I1,I2) using partition in D-th dimension with S as threshold.
        Returns split position I3: [I1,I3) and [I3,I2) are created as result.

        This subroutine doesn't create tree structures, just rearranges nodes.
        *************************************************************************/
        private static void kdtreesplit(kdtree kdt,
            int i1,
            int i2,
            int d,
            double s,
            ref int i3)
        {
            int i = 0;
            int j = 0;
            int ileft = 0;
            int iright = 0;
            double v = 0;

            i3 = 0;

            
            //
            // split XY/Tags in two parts:
            // * [ILeft,IRight] is non-processed part of XY/Tags
            //
            // After cycle is done, we have Ileft=IRight. We deal with
            // this element separately.
            //
            // After this, [I1,ILeft) contains left part, and [ILeft,I2)
            // contains right part.
            //
            ileft = i1;
            iright = i2-1;
            while( ileft<iright )
            {
                if( (double)(kdt.xy[ileft,d])<=(double)(s) )
                {
                    
                    //
                    // XY[ILeft] is on its place.
                    // Advance ILeft.
                    //
                    ileft = ileft+1;
                }
                else
                {
                    
                    //
                    // XY[ILeft,..] must be at IRight.
                    // Swap and advance IRight.
                    //
                    for(i=0; i<=2*kdt.nx+kdt.ny-1; i++)
                    {
                        v = kdt.xy[ileft,i];
                        kdt.xy[ileft,i] = kdt.xy[iright,i];
                        kdt.xy[iright,i] = v;
                    }
                    j = kdt.tags[ileft];
                    kdt.tags[ileft] = kdt.tags[iright];
                    kdt.tags[iright] = j;
                    iright = iright-1;
                }
            }
            if( (double)(kdt.xy[ileft,d])<=(double)(s) )
            {
                ileft = ileft+1;
            }
            else
            {
                iright = iright-1;
            }
            i3 = ileft;
        }


        /*************************************************************************
        Recursive kd-tree generation subroutine.

        PARAMETERS
            KDT         tree
            NodesOffs   unused part of Nodes[] which must be filled by tree
            SplitsOffs  unused part of Splits[]
            I1, I2      points from [I1,I2) are processed
            
        NodesOffs[] and SplitsOffs[] must be large enough.

          -- ALGLIB --
             Copyright 28.02.2010 by Bochkanov Sergey
        *************************************************************************/
        private static void kdtreegeneratetreerec(kdtree kdt,
            ref int nodesoffs,
            ref int splitsoffs,
            int i1,
            int i2,
            int maxleafsize)
        {
            int n = 0;
            int nx = 0;
            int ny = 0;
            int i = 0;
            int j = 0;
            int oldoffs = 0;
            int i3 = 0;
            int cntless = 0;
            int cntgreater = 0;
            double minv = 0;
            double maxv = 0;
            int minidx = 0;
            int maxidx = 0;
            int d = 0;
            double ds = 0;
            double s = 0;
            double v = 0;
            int i_ = 0;
            int i1_ = 0;

            ap.assert(i2>i1, "KDTreeGenerateTreeRec: internal error");
            
            //
            // Generate leaf if needed
            //
            if( i2-i1<=maxleafsize )
            {
                kdt.nodes[nodesoffs+0] = i2-i1;
                kdt.nodes[nodesoffs+1] = i1;
                nodesoffs = nodesoffs+2;
                return;
            }
            
            //
            // Load values for easier access
            //
            nx = kdt.nx;
            ny = kdt.ny;
            
            //
            // select dimension to split:
            // * D is a dimension number
            //
            d = 0;
            ds = kdt.curboxmax[0]-kdt.curboxmin[0];
            for(i=1; i<=nx-1; i++)
            {
                v = kdt.curboxmax[i]-kdt.curboxmin[i];
                if( (double)(v)>(double)(ds) )
                {
                    ds = v;
                    d = i;
                }
            }
            
            //
            // Select split position S using sliding midpoint rule,
            // rearrange points into [I1,I3) and [I3,I2)
            //
            s = kdt.curboxmin[d]+0.5*ds;
            i1_ = (i1) - (0);
            for(i_=0; i_<=i2-i1-1;i_++)
            {
                kdt.buf[i_] = kdt.xy[i_+i1_,d];
            }
            n = i2-i1;
            cntless = 0;
            cntgreater = 0;
            minv = kdt.buf[0];
            maxv = kdt.buf[0];
            minidx = i1;
            maxidx = i1;
            for(i=0; i<=n-1; i++)
            {
                v = kdt.buf[i];
                if( (double)(v)<(double)(minv) )
                {
                    minv = v;
                    minidx = i1+i;
                }
                if( (double)(v)>(double)(maxv) )
                {
                    maxv = v;
                    maxidx = i1+i;
                }
                if( (double)(v)<(double)(s) )
                {
                    cntless = cntless+1;
                }
                if( (double)(v)>(double)(s) )
                {
                    cntgreater = cntgreater+1;
                }
            }
            if( cntless>0 & cntgreater>0 )
            {
                
                //
                // normal midpoint split
                //
                kdtreesplit(kdt, i1, i2, d, s, ref i3);
            }
            else
            {
                
                //
                // sliding midpoint
                //
                if( cntless==0 )
                {
                    
                    //
                    // 1. move split to MinV,
                    // 2. place one point to the left bin (move to I1),
                    //    others - to the right bin
                    //
                    s = minv;
                    if( minidx!=i1 )
                    {
                        for(i=0; i<=2*kdt.nx+kdt.ny-1; i++)
                        {
                            v = kdt.xy[minidx,i];
                            kdt.xy[minidx,i] = kdt.xy[i1,i];
                            kdt.xy[i1,i] = v;
                        }
                        j = kdt.tags[minidx];
                        kdt.tags[minidx] = kdt.tags[i1];
                        kdt.tags[i1] = j;
                    }
                    i3 = i1+1;
                }
                else
                {
                    
                    //
                    // 1. move split to MaxV,
                    // 2. place one point to the right bin (move to I2-1),
                    //    others - to the left bin
                    //
                    s = maxv;
                    if( maxidx!=i2-1 )
                    {
                        for(i=0; i<=2*kdt.nx+kdt.ny-1; i++)
                        {
                            v = kdt.xy[maxidx,i];
                            kdt.xy[maxidx,i] = kdt.xy[i2-1,i];
                            kdt.xy[i2-1,i] = v;
                        }
                        j = kdt.tags[maxidx];
                        kdt.tags[maxidx] = kdt.tags[i2-1];
                        kdt.tags[i2-1] = j;
                    }
                    i3 = i2-1;
                }
            }
            
            //
            // Generate 'split' node
            //
            kdt.nodes[nodesoffs+0] = 0;
            kdt.nodes[nodesoffs+1] = d;
            kdt.nodes[nodesoffs+2] = splitsoffs;
            kdt.splits[splitsoffs+0] = s;
            oldoffs = nodesoffs;
            nodesoffs = nodesoffs+splitnodesize;
            splitsoffs = splitsoffs+1;
            
            //
            // Recirsive generation:
            // * update CurBox
            // * call subroutine
            // * restore CurBox
            //
            kdt.nodes[oldoffs+3] = nodesoffs;
            v = kdt.curboxmax[d];
            kdt.curboxmax[d] = s;
            kdtreegeneratetreerec(kdt, ref nodesoffs, ref splitsoffs, i1, i3, maxleafsize);
            kdt.curboxmax[d] = v;
            kdt.nodes[oldoffs+4] = nodesoffs;
            v = kdt.curboxmin[d];
            kdt.curboxmin[d] = s;
            kdtreegeneratetreerec(kdt, ref nodesoffs, ref splitsoffs, i3, i2, maxleafsize);
            kdt.curboxmin[d] = v;
        }


        /*************************************************************************
        Recursive subroutine for NN queries.

          -- ALGLIB --
             Copyright 28.02.2010 by Bochkanov Sergey
        *************************************************************************/
        private static void kdtreequerynnrec(kdtree kdt,
            int offs)
        {
            double ptdist = 0;
            int i = 0;
            int j = 0;
            int nx = 0;
            int i1 = 0;
            int i2 = 0;
            int d = 0;
            double s = 0;
            double v = 0;
            double t1 = 0;
            int childbestoffs = 0;
            int childworstoffs = 0;
            int childoffs = 0;
            double prevdist = 0;
            bool todive = new bool();
            bool bestisleft = new bool();
            bool updatemin = new bool();

            
            //
            // Leaf node.
            // Process points.
            //
            if( kdt.nodes[offs]>0 )
            {
                i1 = kdt.nodes[offs+1];
                i2 = i1+kdt.nodes[offs];
                for(i=i1; i<=i2-1; i++)
                {
                    
                    //
                    // Calculate distance
                    //
                    ptdist = 0;
                    nx = kdt.nx;
                    if( kdt.normtype==0 )
                    {
                        for(j=0; j<=nx-1; j++)
                        {
                            ptdist = Math.Max(ptdist, Math.Abs(kdt.xy[i,j]-kdt.x[j]));
                        }
                    }
                    if( kdt.normtype==1 )
                    {
                        for(j=0; j<=nx-1; j++)
                        {
                            ptdist = ptdist+Math.Abs(kdt.xy[i,j]-kdt.x[j]);
                        }
                    }
                    if( kdt.normtype==2 )
                    {
                        for(j=0; j<=nx-1; j++)
                        {
                            ptdist = ptdist+math.sqr(kdt.xy[i,j]-kdt.x[j]);
                        }
                    }
                    
                    //
                    // Skip points with zero distance if self-matches are turned off
                    //
                    if( (double)(ptdist)==(double)(0) & !kdt.selfmatch )
                    {
                        continue;
                    }
                    
                    //
                    // We CAN'T process point if R-criterion isn't satisfied,
                    // i.e. (RNeeded<>0) AND (PtDist>R).
                    //
                    if( (double)(kdt.rneeded)==(double)(0) | (double)(ptdist)<=(double)(kdt.rneeded) )
                    {
                        
                        //
                        // R-criterion is satisfied, we must either:
                        // * replace worst point, if (KNeeded<>0) AND (KCur=KNeeded)
                        //   (or skip, if worst point is better)
                        // * add point without replacement otherwise
                        //
                        if( kdt.kcur<kdt.kneeded | kdt.kneeded==0 )
                        {
                            
                            //
                            // add current point to heap without replacement
                            //
                            tsort.tagheappushi(ref kdt.r, ref kdt.idx, ref kdt.kcur, ptdist, i);
                        }
                        else
                        {
                            
                            //
                            // New points are added or not, depending on their distance.
                            // If added, they replace element at the top of the heap
                            //
                            if( (double)(ptdist)<(double)(kdt.r[0]) )
                            {
                                if( kdt.kneeded==1 )
                                {
                                    kdt.idx[0] = i;
                                    kdt.r[0] = ptdist;
                                }
                                else
                                {
                                    tsort.tagheapreplacetopi(ref kdt.r, ref kdt.idx, kdt.kneeded, ptdist, i);
                                }
                            }
                        }
                    }
                }
                return;
            }
            
            //
            // Simple split
            //
            if( kdt.nodes[offs]==0 )
            {
                
                //
                // Load:
                // * D  dimension to split
                // * S  split position
                //
                d = kdt.nodes[offs+1];
                s = kdt.splits[kdt.nodes[offs+2]];
                
                //
                // Calculate:
                // * ChildBestOffs      child box with best chances
                // * ChildWorstOffs     child box with worst chances
                //
                if( (double)(kdt.x[d])<=(double)(s) )
                {
                    childbestoffs = kdt.nodes[offs+3];
                    childworstoffs = kdt.nodes[offs+4];
                    bestisleft = true;
                }
                else
                {
                    childbestoffs = kdt.nodes[offs+4];
                    childworstoffs = kdt.nodes[offs+3];
                    bestisleft = false;
                }
                
                //
                // Navigate through childs
                //
                for(i=0; i<=1; i++)
                {
                    
                    //
                    // Select child to process:
                    // * ChildOffs      current child offset in Nodes[]
                    // * UpdateMin      whether minimum or maximum value
                    //                  of bounding box is changed on update
                    //
                    if( i==0 )
                    {
                        childoffs = childbestoffs;
                        updatemin = !bestisleft;
                    }
                    else
                    {
                        updatemin = bestisleft;
                        childoffs = childworstoffs;
                    }
                    
                    //
                    // Update bounding box and current distance
                    //
                    if( updatemin )
                    {
                        prevdist = kdt.curdist;
                        t1 = kdt.x[d];
                        v = kdt.curboxmin[d];
                        if( (double)(t1)<=(double)(s) )
                        {
                            if( kdt.normtype==0 )
                            {
                                kdt.curdist = Math.Max(kdt.curdist, s-t1);
                            }
                            if( kdt.normtype==1 )
                            {
                                kdt.curdist = kdt.curdist-Math.Max(v-t1, 0)+s-t1;
                            }
                            if( kdt.normtype==2 )
                            {
                                kdt.curdist = kdt.curdist-math.sqr(Math.Max(v-t1, 0))+math.sqr(s-t1);
                            }
                        }
                        kdt.curboxmin[d] = s;
                    }
                    else
                    {
                        prevdist = kdt.curdist;
                        t1 = kdt.x[d];
                        v = kdt.curboxmax[d];
                        if( (double)(t1)>=(double)(s) )
                        {
                            if( kdt.normtype==0 )
                            {
                                kdt.curdist = Math.Max(kdt.curdist, t1-s);
                            }
                            if( kdt.normtype==1 )
                            {
                                kdt.curdist = kdt.curdist-Math.Max(t1-v, 0)+t1-s;
                            }
                            if( kdt.normtype==2 )
                            {
                                kdt.curdist = kdt.curdist-math.sqr(Math.Max(t1-v, 0))+math.sqr(t1-s);
                            }
                        }
                        kdt.curboxmax[d] = s;
                    }
                    
                    //
                    // Decide: to dive into cell or not to dive
                    //
                    if( (double)(kdt.rneeded)!=(double)(0) & (double)(kdt.curdist)>(double)(kdt.rneeded) )
                    {
                        todive = false;
                    }
                    else
                    {
                        if( kdt.kcur<kdt.kneeded | kdt.kneeded==0 )
                        {
                            
                            //
                            // KCur<KNeeded (i.e. not all points are found)
                            //
                            todive = true;
                        }
                        else
                        {
                            
                            //
                            // KCur=KNeeded, decide to dive or not to dive
                            // using point position relative to bounding box.
                            //
                            todive = (double)(kdt.curdist)<=(double)(kdt.r[0]*kdt.approxf);
                        }
                    }
                    if( todive )
                    {
                        kdtreequerynnrec(kdt, childoffs);
                    }
                    
                    //
                    // Restore bounding box and distance
                    //
                    if( updatemin )
                    {
                        kdt.curboxmin[d] = v;
                    }
                    else
                    {
                        kdt.curboxmax[d] = v;
                    }
                    kdt.curdist = prevdist;
                }
                return;
            }
        }


        /*************************************************************************
        Copies X[] to KDT.X[]
        Loads distance from X[] to bounding box.
        Initializes CurBox[].

          -- ALGLIB --
             Copyright 28.02.2010 by Bochkanov Sergey
        *************************************************************************/
        private static void kdtreeinitbox(kdtree kdt,
            double[] x)
        {
            int i = 0;
            double vx = 0;
            double vmin = 0;
            double vmax = 0;

            
            //
            // calculate distance from point to current bounding box
            //
            kdt.curdist = 0;
            if( kdt.normtype==0 )
            {
                for(i=0; i<=kdt.nx-1; i++)
                {
                    vx = x[i];
                    vmin = kdt.boxmin[i];
                    vmax = kdt.boxmax[i];
                    kdt.x[i] = vx;
                    kdt.curboxmin[i] = vmin;
                    kdt.curboxmax[i] = vmax;
                    if( (double)(vx)<(double)(vmin) )
                    {
                        kdt.curdist = Math.Max(kdt.curdist, vmin-vx);
                    }
                    else
                    {
                        if( (double)(vx)>(double)(vmax) )
                        {
                            kdt.curdist = Math.Max(kdt.curdist, vx-vmax);
                        }
                    }
                }
            }
            if( kdt.normtype==1 )
            {
                for(i=0; i<=kdt.nx-1; i++)
                {
                    vx = x[i];
                    vmin = kdt.boxmin[i];
                    vmax = kdt.boxmax[i];
                    kdt.x[i] = vx;
                    kdt.curboxmin[i] = vmin;
                    kdt.curboxmax[i] = vmax;
                    if( (double)(vx)<(double)(vmin) )
                    {
                        kdt.curdist = kdt.curdist+vmin-vx;
                    }
                    else
                    {
                        if( (double)(vx)>(double)(vmax) )
                        {
                            kdt.curdist = kdt.curdist+vx-vmax;
                        }
                    }
                }
            }
            if( kdt.normtype==2 )
            {
                for(i=0; i<=kdt.nx-1; i++)
                {
                    vx = x[i];
                    vmin = kdt.boxmin[i];
                    vmax = kdt.boxmax[i];
                    kdt.x[i] = vx;
                    kdt.curboxmin[i] = vmin;
                    kdt.curboxmax[i] = vmax;
                    if( (double)(vx)<(double)(vmin) )
                    {
                        kdt.curdist = kdt.curdist+math.sqr(vmin-vx);
                    }
                    else
                    {
                        if( (double)(vx)>(double)(vmax) )
                        {
                            kdt.curdist = kdt.curdist+math.sqr(vx-vmax);
                        }
                    }
                }
            }
        }


        /*************************************************************************
        This function allocates all dataset-independent array  fields  of  KDTree,
        i.e.  such  array  fields  that  their dimensions do not depend on dataset
        size.

        This function do not sets KDT.NX or KDT.NY - it just allocates arrays

          -- ALGLIB --
             Copyright 14.03.2011 by Bochkanov Sergey
        *************************************************************************/
        private static void kdtreeallocdatasetindependent(kdtree kdt,
            int nx,
            int ny)
        {
            kdt.x = new double[nx];
            kdt.boxmin = new double[nx];
            kdt.boxmax = new double[nx];
            kdt.curboxmin = new double[nx];
            kdt.curboxmax = new double[nx];
        }


        /*************************************************************************
        This function allocates all dataset-dependent array fields of KDTree, i.e.
        such array fields that their dimensions depend on dataset size.

        This function do not sets KDT.N, KDT.NX or KDT.NY -
        it just allocates arrays.

          -- ALGLIB --
             Copyright 14.03.2011 by Bochkanov Sergey
        *************************************************************************/
        private static void kdtreeallocdatasetdependent(kdtree kdt,
            int n,
            int nx,
            int ny)
        {
            kdt.xy = new double[n, 2*nx+ny];
            kdt.tags = new int[n];
            kdt.idx = new int[n];
            kdt.r = new double[n];
            kdt.x = new double[nx];
            kdt.buf = new double[Math.Max(n, nx)];
            kdt.nodes = new int[splitnodesize*2*n];
            kdt.splits = new double[2*n];
        }


        /*************************************************************************
        This function allocates temporaries.

        This function do not sets KDT.N, KDT.NX or KDT.NY -
        it just allocates arrays.

          -- ALGLIB --
             Copyright 14.03.2011 by Bochkanov Sergey
        *************************************************************************/
        private static void kdtreealloctemporaries(kdtree kdt,
            int n,
            int nx,
            int ny)
        {
            kdt.x = new double[nx];
            kdt.idx = new int[n];
            kdt.r = new double[n];
            kdt.buf = new double[Math.Max(n, nx)];
            kdt.curboxmin = new double[nx];
            kdt.curboxmax = new double[nx];
        }


    }
}

