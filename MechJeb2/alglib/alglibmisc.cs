/*************************************************************************
ALGLIB 3.10.0 (source code generated 2015-08-19)
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
    public class hqrndstate : alglibobject
    {
        //
        // Public declarations
        //

        public hqrndstate()
        {
            _innerobj = new hqrnd.hqrndstate();
        }
        
        public override alglib.alglibobject make_copy()
        {
            return new hqrndstate((hqrnd.hqrndstate)_innerobj.make_copy());
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

    1. State structure must be initialized with HQRNDRandomize() or HQRNDSeed()
    2. N can be any positive number except for very large numbers:
       * close to 2^31 on 32-bit systems
       * close to 2^62 on 64-bit systems
       An exception will be generated if N is too large.

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

    /*************************************************************************
    This function generates  random number from discrete distribution given by
    finite sample X.

    INPUT PARAMETERS
        State   -   high quality random number generator, must be
                    initialized with HQRNDRandomize() or HQRNDSeed().
            X   -   finite sample
            N   -   number of elements to use, N>=1

    RESULT
        this function returns one of the X[i] for random i=0..N-1

      -- ALGLIB --
         Copyright 08.11.2011 by Bochkanov Sergey
    *************************************************************************/
    public static double hqrnddiscrete(hqrndstate state, double[] x, int n)
    {

        double result = hqrnd.hqrnddiscrete(state.innerobj, x, n);
        return result;
    }

    /*************************************************************************
    This function generates random number from continuous  distribution  given
    by finite sample X.

    INPUT PARAMETERS
        State   -   high quality random number generator, must be
                    initialized with HQRNDRandomize() or HQRNDSeed().
            X   -   finite sample, array[N] (can be larger, in this  case only
                    leading N elements are used). THIS ARRAY MUST BE SORTED BY
                    ASCENDING.
            N   -   number of elements to use, N>=1

    RESULT
        this function returns random number from continuous distribution which
        tries to approximate X as mush as possible. min(X)<=Result<=max(X).

      -- ALGLIB --
         Copyright 08.11.2011 by Bochkanov Sergey
    *************************************************************************/
    public static double hqrndcontinuous(hqrndstate state, double[] x, int n)
    {

        double result = hqrnd.hqrndcontinuous(state.innerobj, x, n);
        return result;
    }

}
public partial class alglib
{


    /*************************************************************************

    *************************************************************************/
    public class kdtree : alglibobject
    {
        //
        // Public declarations
        //

        public kdtree()
        {
            _innerobj = new nearestneighbor.kdtree();
        }
        
        public override alglib.alglibobject make_copy()
        {
            return new kdtree((nearestneighbor.kdtree)_innerobj.make_copy());
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
        N       -   number of points, N>=0.
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
        N       -   number of points, N>=0
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


    /*************************************************************************

    *************************************************************************/
    public class xdebugrecord1 : alglibobject
    {
        //
        // Public declarations
        //
        public int i { get { return _innerobj.i; } set { _innerobj.i = value; } }
        public complex c { get { return _innerobj.c; } set { _innerobj.c = value; } }
        public double[] a { get { return _innerobj.a; } set { _innerobj.a = value; } }

        public xdebugrecord1()
        {
            _innerobj = new xdebug.xdebugrecord1();
        }
        
        public override alglib.alglibobject make_copy()
        {
            return new xdebugrecord1((xdebug.xdebugrecord1)_innerobj.make_copy());
        }

        //
        // Although some of declarations below are public, you should not use them
        // They are intended for internal use only
        //
        private xdebug.xdebugrecord1 _innerobj;
        public xdebug.xdebugrecord1 innerobj { get { return _innerobj; } }
        public xdebugrecord1(xdebug.xdebugrecord1 obj)
        {
            _innerobj = obj;
        }
    }

    /*************************************************************************
    This is debug function intended for testing ALGLIB interface generator.
    Never use it in any real life project.

    Creates and returns XDebugRecord1 structure:
    * integer and complex fields of Rec1 are set to 1 and 1+i correspondingly
    * array field of Rec1 is set to [2,3]

      -- ALGLIB --
         Copyright 27.05.2014 by Bochkanov Sergey
    *************************************************************************/
    public static void xdebuginitrecord1(out xdebugrecord1 rec1)
    {
        rec1 = new xdebugrecord1();
        xdebug.xdebuginitrecord1(rec1.innerobj);
        return;
    }

    /*************************************************************************
    This is debug function intended for testing ALGLIB interface generator.
    Never use it in any real life project.

    Counts number of True values in the boolean 1D array.

      -- ALGLIB --
         Copyright 11.10.2013 by Bochkanov Sergey
    *************************************************************************/
    public static int xdebugb1count(bool[] a)
    {

        int result = xdebug.xdebugb1count(a);
        return result;
    }

    /*************************************************************************
    This is debug function intended for testing ALGLIB interface generator.
    Never use it in any real life project.

    Replace all values in array by NOT(a[i]).
    Array is passed using "shared" convention.

      -- ALGLIB --
         Copyright 11.10.2013 by Bochkanov Sergey
    *************************************************************************/
    public static void xdebugb1not(ref bool[] a)
    {

        xdebug.xdebugb1not(a);
        return;
    }

    /*************************************************************************
    This is debug function intended for testing ALGLIB interface generator.
    Never use it in any real life project.

    Appends copy of array to itself.
    Array is passed using "var" convention.

      -- ALGLIB --
         Copyright 11.10.2013 by Bochkanov Sergey
    *************************************************************************/
    public static void xdebugb1appendcopy(ref bool[] a)
    {

        xdebug.xdebugb1appendcopy(ref a);
        return;
    }

    /*************************************************************************
    This is debug function intended for testing ALGLIB interface generator.
    Never use it in any real life project.

    Generate N-element array with even-numbered elements set to True.
    Array is passed using "out" convention.

      -- ALGLIB --
         Copyright 11.10.2013 by Bochkanov Sergey
    *************************************************************************/
    public static void xdebugb1outeven(int n, out bool[] a)
    {
        a = new bool[0];
        xdebug.xdebugb1outeven(n, ref a);
        return;
    }

    /*************************************************************************
    This is debug function intended for testing ALGLIB interface generator.
    Never use it in any real life project.

    Returns sum of elements in the array.

      -- ALGLIB --
         Copyright 11.10.2013 by Bochkanov Sergey
    *************************************************************************/
    public static int xdebugi1sum(int[] a)
    {

        int result = xdebug.xdebugi1sum(a);
        return result;
    }

    /*************************************************************************
    This is debug function intended for testing ALGLIB interface generator.
    Never use it in any real life project.

    Replace all values in array by -A[I]
    Array is passed using "shared" convention.

      -- ALGLIB --
         Copyright 11.10.2013 by Bochkanov Sergey
    *************************************************************************/
    public static void xdebugi1neg(ref int[] a)
    {

        xdebug.xdebugi1neg(a);
        return;
    }

    /*************************************************************************
    This is debug function intended for testing ALGLIB interface generator.
    Never use it in any real life project.

    Appends copy of array to itself.
    Array is passed using "var" convention.

      -- ALGLIB --
         Copyright 11.10.2013 by Bochkanov Sergey
    *************************************************************************/
    public static void xdebugi1appendcopy(ref int[] a)
    {

        xdebug.xdebugi1appendcopy(ref a);
        return;
    }

    /*************************************************************************
    This is debug function intended for testing ALGLIB interface generator.
    Never use it in any real life project.

    Generate N-element array with even-numbered A[I] set to I, and odd-numbered
    ones set to 0.

    Array is passed using "out" convention.

      -- ALGLIB --
         Copyright 11.10.2013 by Bochkanov Sergey
    *************************************************************************/
    public static void xdebugi1outeven(int n, out int[] a)
    {
        a = new int[0];
        xdebug.xdebugi1outeven(n, ref a);
        return;
    }

    /*************************************************************************
    This is debug function intended for testing ALGLIB interface generator.
    Never use it in any real life project.

    Returns sum of elements in the array.

      -- ALGLIB --
         Copyright 11.10.2013 by Bochkanov Sergey
    *************************************************************************/
    public static double xdebugr1sum(double[] a)
    {

        double result = xdebug.xdebugr1sum(a);
        return result;
    }

    /*************************************************************************
    This is debug function intended for testing ALGLIB interface generator.
    Never use it in any real life project.

    Replace all values in array by -A[I]
    Array is passed using "shared" convention.

      -- ALGLIB --
         Copyright 11.10.2013 by Bochkanov Sergey
    *************************************************************************/
    public static void xdebugr1neg(ref double[] a)
    {

        xdebug.xdebugr1neg(a);
        return;
    }

    /*************************************************************************
    This is debug function intended for testing ALGLIB interface generator.
    Never use it in any real life project.

    Appends copy of array to itself.
    Array is passed using "var" convention.

      -- ALGLIB --
         Copyright 11.10.2013 by Bochkanov Sergey
    *************************************************************************/
    public static void xdebugr1appendcopy(ref double[] a)
    {

        xdebug.xdebugr1appendcopy(ref a);
        return;
    }

    /*************************************************************************
    This is debug function intended for testing ALGLIB interface generator.
    Never use it in any real life project.

    Generate N-element array with even-numbered A[I] set to I*0.25,
    and odd-numbered ones are set to 0.

    Array is passed using "out" convention.

      -- ALGLIB --
         Copyright 11.10.2013 by Bochkanov Sergey
    *************************************************************************/
    public static void xdebugr1outeven(int n, out double[] a)
    {
        a = new double[0];
        xdebug.xdebugr1outeven(n, ref a);
        return;
    }

    /*************************************************************************
    This is debug function intended for testing ALGLIB interface generator.
    Never use it in any real life project.

    Returns sum of elements in the array.

      -- ALGLIB --
         Copyright 11.10.2013 by Bochkanov Sergey
    *************************************************************************/
    public static complex xdebugc1sum(complex[] a)
    {

        complex result = xdebug.xdebugc1sum(a);
        return result;
    }

    /*************************************************************************
    This is debug function intended for testing ALGLIB interface generator.
    Never use it in any real life project.

    Replace all values in array by -A[I]
    Array is passed using "shared" convention.

      -- ALGLIB --
         Copyright 11.10.2013 by Bochkanov Sergey
    *************************************************************************/
    public static void xdebugc1neg(ref complex[] a)
    {

        xdebug.xdebugc1neg(a);
        return;
    }

    /*************************************************************************
    This is debug function intended for testing ALGLIB interface generator.
    Never use it in any real life project.

    Appends copy of array to itself.
    Array is passed using "var" convention.

      -- ALGLIB --
         Copyright 11.10.2013 by Bochkanov Sergey
    *************************************************************************/
    public static void xdebugc1appendcopy(ref complex[] a)
    {

        xdebug.xdebugc1appendcopy(ref a);
        return;
    }

    /*************************************************************************
    This is debug function intended for testing ALGLIB interface generator.
    Never use it in any real life project.

    Generate N-element array with even-numbered A[K] set to (x,y) = (K*0.25, K*0.125)
    and odd-numbered ones are set to 0.

    Array is passed using "out" convention.

      -- ALGLIB --
         Copyright 11.10.2013 by Bochkanov Sergey
    *************************************************************************/
    public static void xdebugc1outeven(int n, out complex[] a)
    {
        a = new complex[0];
        xdebug.xdebugc1outeven(n, ref a);
        return;
    }

    /*************************************************************************
    This is debug function intended for testing ALGLIB interface generator.
    Never use it in any real life project.

    Counts number of True values in the boolean 2D array.

      -- ALGLIB --
         Copyright 11.10.2013 by Bochkanov Sergey
    *************************************************************************/
    public static int xdebugb2count(bool[,] a)
    {

        int result = xdebug.xdebugb2count(a);
        return result;
    }

    /*************************************************************************
    This is debug function intended for testing ALGLIB interface generator.
    Never use it in any real life project.

    Replace all values in array by NOT(a[i]).
    Array is passed using "shared" convention.

      -- ALGLIB --
         Copyright 11.10.2013 by Bochkanov Sergey
    *************************************************************************/
    public static void xdebugb2not(ref bool[,] a)
    {

        xdebug.xdebugb2not(a);
        return;
    }

    /*************************************************************************
    This is debug function intended for testing ALGLIB interface generator.
    Never use it in any real life project.

    Transposes array.
    Array is passed using "var" convention.

      -- ALGLIB --
         Copyright 11.10.2013 by Bochkanov Sergey
    *************************************************************************/
    public static void xdebugb2transpose(ref bool[,] a)
    {

        xdebug.xdebugb2transpose(ref a);
        return;
    }

    /*************************************************************************
    This is debug function intended for testing ALGLIB interface generator.
    Never use it in any real life project.

    Generate MxN matrix with elements set to "Sin(3*I+5*J)>0"
    Array is passed using "out" convention.

      -- ALGLIB --
         Copyright 11.10.2013 by Bochkanov Sergey
    *************************************************************************/
    public static void xdebugb2outsin(int m, int n, out bool[,] a)
    {
        a = new bool[0,0];
        xdebug.xdebugb2outsin(m, n, ref a);
        return;
    }

    /*************************************************************************
    This is debug function intended for testing ALGLIB interface generator.
    Never use it in any real life project.

    Returns sum of elements in the array.

      -- ALGLIB --
         Copyright 11.10.2013 by Bochkanov Sergey
    *************************************************************************/
    public static int xdebugi2sum(int[,] a)
    {

        int result = xdebug.xdebugi2sum(a);
        return result;
    }

    /*************************************************************************
    This is debug function intended for testing ALGLIB interface generator.
    Never use it in any real life project.

    Replace all values in array by -a[i,j]
    Array is passed using "shared" convention.

      -- ALGLIB --
         Copyright 11.10.2013 by Bochkanov Sergey
    *************************************************************************/
    public static void xdebugi2neg(ref int[,] a)
    {

        xdebug.xdebugi2neg(a);
        return;
    }

    /*************************************************************************
    This is debug function intended for testing ALGLIB interface generator.
    Never use it in any real life project.

    Transposes array.
    Array is passed using "var" convention.

      -- ALGLIB --
         Copyright 11.10.2013 by Bochkanov Sergey
    *************************************************************************/
    public static void xdebugi2transpose(ref int[,] a)
    {

        xdebug.xdebugi2transpose(ref a);
        return;
    }

    /*************************************************************************
    This is debug function intended for testing ALGLIB interface generator.
    Never use it in any real life project.

    Generate MxN matrix with elements set to "Sign(Sin(3*I+5*J))"
    Array is passed using "out" convention.

      -- ALGLIB --
         Copyright 11.10.2013 by Bochkanov Sergey
    *************************************************************************/
    public static void xdebugi2outsin(int m, int n, out int[,] a)
    {
        a = new int[0,0];
        xdebug.xdebugi2outsin(m, n, ref a);
        return;
    }

    /*************************************************************************
    This is debug function intended for testing ALGLIB interface generator.
    Never use it in any real life project.

    Returns sum of elements in the array.

      -- ALGLIB --
         Copyright 11.10.2013 by Bochkanov Sergey
    *************************************************************************/
    public static double xdebugr2sum(double[,] a)
    {

        double result = xdebug.xdebugr2sum(a);
        return result;
    }

    /*************************************************************************
    This is debug function intended for testing ALGLIB interface generator.
    Never use it in any real life project.

    Replace all values in array by -a[i,j]
    Array is passed using "shared" convention.

      -- ALGLIB --
         Copyright 11.10.2013 by Bochkanov Sergey
    *************************************************************************/
    public static void xdebugr2neg(ref double[,] a)
    {

        xdebug.xdebugr2neg(a);
        return;
    }

    /*************************************************************************
    This is debug function intended for testing ALGLIB interface generator.
    Never use it in any real life project.

    Transposes array.
    Array is passed using "var" convention.

      -- ALGLIB --
         Copyright 11.10.2013 by Bochkanov Sergey
    *************************************************************************/
    public static void xdebugr2transpose(ref double[,] a)
    {

        xdebug.xdebugr2transpose(ref a);
        return;
    }

    /*************************************************************************
    This is debug function intended for testing ALGLIB interface generator.
    Never use it in any real life project.

    Generate MxN matrix with elements set to "Sin(3*I+5*J)"
    Array is passed using "out" convention.

      -- ALGLIB --
         Copyright 11.10.2013 by Bochkanov Sergey
    *************************************************************************/
    public static void xdebugr2outsin(int m, int n, out double[,] a)
    {
        a = new double[0,0];
        xdebug.xdebugr2outsin(m, n, ref a);
        return;
    }

    /*************************************************************************
    This is debug function intended for testing ALGLIB interface generator.
    Never use it in any real life project.

    Returns sum of elements in the array.

      -- ALGLIB --
         Copyright 11.10.2013 by Bochkanov Sergey
    *************************************************************************/
    public static complex xdebugc2sum(complex[,] a)
    {

        complex result = xdebug.xdebugc2sum(a);
        return result;
    }

    /*************************************************************************
    This is debug function intended for testing ALGLIB interface generator.
    Never use it in any real life project.

    Replace all values in array by -a[i,j]
    Array is passed using "shared" convention.

      -- ALGLIB --
         Copyright 11.10.2013 by Bochkanov Sergey
    *************************************************************************/
    public static void xdebugc2neg(ref complex[,] a)
    {

        xdebug.xdebugc2neg(a);
        return;
    }

    /*************************************************************************
    This is debug function intended for testing ALGLIB interface generator.
    Never use it in any real life project.

    Transposes array.
    Array is passed using "var" convention.

      -- ALGLIB --
         Copyright 11.10.2013 by Bochkanov Sergey
    *************************************************************************/
    public static void xdebugc2transpose(ref complex[,] a)
    {

        xdebug.xdebugc2transpose(ref a);
        return;
    }

    /*************************************************************************
    This is debug function intended for testing ALGLIB interface generator.
    Never use it in any real life project.

    Generate MxN matrix with elements set to "Sin(3*I+5*J),Cos(3*I+5*J)"
    Array is passed using "out" convention.

      -- ALGLIB --
         Copyright 11.10.2013 by Bochkanov Sergey
    *************************************************************************/
    public static void xdebugc2outsincos(int m, int n, out complex[,] a)
    {
        a = new complex[0,0];
        xdebug.xdebugc2outsincos(m, n, ref a);
        return;
    }

    /*************************************************************************
    This is debug function intended for testing ALGLIB interface generator.
    Never use it in any real life project.

    Returns sum of a[i,j]*(1+b[i,j]) such that c[i,j] is True

      -- ALGLIB --
         Copyright 11.10.2013 by Bochkanov Sergey
    *************************************************************************/
    public static double xdebugmaskedbiasedproductsum(int m, int n, double[,] a, double[,] b, bool[,] c)
    {

        double result = xdebug.xdebugmaskedbiasedproductsum(m, n, a, b, c);
        return result;
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
        public class hqrndstate : apobject
        {
            public int s1;
            public int s2;
            public int magicv;
            public hqrndstate()
            {
                init();
            }
            public override void init()
            {
            }
            public override alglib.apobject make_copy()
            {
                hqrndstate _result = new hqrndstate();
                _result.s1 = s1;
                _result.s2 = s2;
                _result.magicv = magicv;
                return _result;
            }
        };




        public const int hqrndmax = 2147483561;
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
            int s0 = 0;
            int s1 = 0;

            s0 = math.randominteger(hqrndm1);
            s1 = math.randominteger(hqrndm2);
            hqrndseed(s0, s1, state);
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
            
            //
            // Protection against negative seeds:
            //
            //     SEED := -(SEED+1)
            //
            // We can use just "-SEED" because there exists such integer number  N
            // that N<0, -N=N<0 too. (This number is equal to 0x800...000).   Need
            // to handle such seed correctly forces us to use  a  bit  complicated
            // formula.
            //
            if( s1<0 )
            {
                s1 = -(s1+1);
            }
            if( s2<0 )
            {
                s2 = -(s2+1);
            }
            state.s1 = s1%(hqrndm1-1)+1;
            state.s2 = s2%(hqrndm2-1)+1;
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

            result = (double)(hqrndintegerbase(state)+1)/(double)(hqrndmax+2);
            return result;
        }


        /*************************************************************************
        This function generates random integer number in [0, N)

        1. State structure must be initialized with HQRNDRandomize() or HQRNDSeed()
        2. N can be any positive number except for very large numbers:
           * close to 2^31 on 32-bit systems
           * close to 2^62 on 64-bit systems
           An exception will be generated if N is too large.

          -- ALGLIB --
             Copyright 02.12.2009 by Bochkanov Sergey
        *************************************************************************/
        public static int hqrnduniformi(hqrndstate state,
            int n)
        {
            int result = 0;
            int maxcnt = 0;
            int mx = 0;
            int a = 0;
            int b = 0;

            alglib.ap.assert(n>0, "HQRNDUniformI: N<=0!");
            maxcnt = hqrndmax+1;
            
            //
            // Two branches: one for N<=MaxCnt, another for N>MaxCnt.
            //
            if( n>maxcnt )
            {
                
                //
                // N>=MaxCnt.
                //
                // We have two options here:
                // a) N is exactly divisible by MaxCnt
                // b) N is not divisible by MaxCnt
                //
                // In both cases we reduce problem on interval spanning [0,N)
                // to several subproblems on intervals spanning [0,MaxCnt).
                //
                if( n%maxcnt==0 )
                {
                    
                    //
                    // N is exactly divisible by MaxCnt.
                    //
                    // [0,N) range is dividided into N/MaxCnt bins,
                    // each of them having length equal to MaxCnt.
                    //
                    // We generate:
                    // * random bin number B
                    // * random offset within bin A
                    // Both random numbers are generated by recursively
                    // calling HQRNDUniformI().
                    //
                    // Result is equal to A+MaxCnt*B.
                    //
                    alglib.ap.assert(n/maxcnt<=maxcnt, "HQRNDUniformI: N is too large");
                    a = hqrnduniformi(state, maxcnt);
                    b = hqrnduniformi(state, n/maxcnt);
                    result = a+maxcnt*b;
                }
                else
                {
                    
                    //
                    // N is NOT exactly divisible by MaxCnt.
                    //
                    // [0,N) range is dividided into Ceil(N/MaxCnt) bins,
                    // each of them having length equal to MaxCnt.
                    //
                    // We generate:
                    // * random bin number B in [0, Ceil(N/MaxCnt)-1]
                    // * random offset within bin A
                    // * if both of what is below is true
                    //   1) bin number B is that of the last bin
                    //   2) A >= N mod MaxCnt
                    //   then we repeat generation of A/B.
                    //   This stage is essential in order to avoid bias in the result.
                    // * otherwise, we return A*MaxCnt+N
                    //
                    alglib.ap.assert(n/maxcnt+1<=maxcnt, "HQRNDUniformI: N is too large");
                    result = -1;
                    do
                    {
                        a = hqrnduniformi(state, maxcnt);
                        b = hqrnduniformi(state, n/maxcnt+1);
                        if( b==n/maxcnt && a>=n%maxcnt )
                        {
                            continue;
                        }
                        result = a+maxcnt*b;
                    }
                    while( result<0 );
                }
            }
            else
            {
                
                //
                // N<=MaxCnt
                //
                // Code below is a bit complicated because we can not simply
                // return "HQRNDIntegerBase() mod N" - it will be skewed for
                // large N's in [0.1*HQRNDMax...HQRNDMax].
                //
                mx = maxcnt-maxcnt%n;
                do
                {
                    result = hqrndintegerbase(state);
                }
                while( result>=mx );
                result = result%n;
            }
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
            while( !((double)(x)!=(double)(0) || (double)(y)!=(double)(0)) );
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
                if( (double)(s)>(double)(0) && (double)(s)<(double)(1) )
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

            alglib.ap.assert((double)(lambdav)>(double)(0), "HQRNDExponential: LambdaV<=0!");
            result = -(Math.Log(hqrnduniformr(state))/lambdav);
            return result;
        }


        /*************************************************************************
        This function generates  random number from discrete distribution given by
        finite sample X.

        INPUT PARAMETERS
            State   -   high quality random number generator, must be
                        initialized with HQRNDRandomize() or HQRNDSeed().
                X   -   finite sample
                N   -   number of elements to use, N>=1

        RESULT
            this function returns one of the X[i] for random i=0..N-1

          -- ALGLIB --
             Copyright 08.11.2011 by Bochkanov Sergey
        *************************************************************************/
        public static double hqrnddiscrete(hqrndstate state,
            double[] x,
            int n)
        {
            double result = 0;

            alglib.ap.assert(n>0, "HQRNDDiscrete: N<=0");
            alglib.ap.assert(n<=alglib.ap.len(x), "HQRNDDiscrete: Length(X)<N");
            result = x[hqrnduniformi(state, n)];
            return result;
        }


        /*************************************************************************
        This function generates random number from continuous  distribution  given
        by finite sample X.

        INPUT PARAMETERS
            State   -   high quality random number generator, must be
                        initialized with HQRNDRandomize() or HQRNDSeed().
                X   -   finite sample, array[N] (can be larger, in this  case only
                        leading N elements are used). THIS ARRAY MUST BE SORTED BY
                        ASCENDING.
                N   -   number of elements to use, N>=1

        RESULT
            this function returns random number from continuous distribution which  
            tries to approximate X as mush as possible. min(X)<=Result<=max(X).

          -- ALGLIB --
             Copyright 08.11.2011 by Bochkanov Sergey
        *************************************************************************/
        public static double hqrndcontinuous(hqrndstate state,
            double[] x,
            int n)
        {
            double result = 0;
            double mx = 0;
            double mn = 0;
            int i = 0;

            alglib.ap.assert(n>0, "HQRNDContinuous: N<=0");
            alglib.ap.assert(n<=alglib.ap.len(x), "HQRNDContinuous: Length(X)<N");
            if( n==1 )
            {
                result = x[0];
                return result;
            }
            i = hqrnduniformi(state, n-1);
            mn = x[i];
            mx = x[i+1];
            alglib.ap.assert((double)(mx)>=(double)(mn), "HQRNDDiscrete: X is not sorted by ascending");
            if( (double)(mx)!=(double)(mn) )
            {
                result = (mx-mn)*hqrnduniformr(state)+mn;
            }
            else
            {
                result = mn;
            }
            return result;
        }


        /*************************************************************************
        This function returns random integer in [0,HQRNDMax]

        L'Ecuyer, Efficient and portable combined random number generators
        *************************************************************************/
        private static int hqrndintegerbase(hqrndstate state)
        {
            int result = 0;
            int k = 0;

            alglib.ap.assert(state.magicv==hqrndmagic, "HQRNDIntegerBase: State is not correctly initialized!");
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
            result = result-1;
            return result;
        }


    }
    public class nearestneighbor
    {
        public class kdtree : apobject
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
                init();
            }
            public override void init()
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
            public override alglib.apobject make_copy()
            {
                kdtree _result = new kdtree();
                _result.n = n;
                _result.nx = nx;
                _result.ny = ny;
                _result.normtype = normtype;
                _result.xy = (double[,])xy.Clone();
                _result.tags = (int[])tags.Clone();
                _result.boxmin = (double[])boxmin.Clone();
                _result.boxmax = (double[])boxmax.Clone();
                _result.nodes = (int[])nodes.Clone();
                _result.splits = (double[])splits.Clone();
                _result.x = (double[])x.Clone();
                _result.kneeded = kneeded;
                _result.rneeded = rneeded;
                _result.selfmatch = selfmatch;
                _result.approxf = approxf;
                _result.kcur = kcur;
                _result.idx = (int[])idx.Clone();
                _result.r = (double[])r.Clone();
                _result.buf = (double[])buf.Clone();
                _result.curboxmin = (double[])curboxmin.Clone();
                _result.curboxmax = (double[])curboxmax.Clone();
                _result.curdist = curdist;
                _result.debugcounter = debugcounter;
                return _result;
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
            N       -   number of points, N>=0.
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

            alglib.ap.assert(n>=0, "KDTreeBuild: N<0");
            alglib.ap.assert(nx>=1, "KDTreeBuild: NX<1");
            alglib.ap.assert(ny>=0, "KDTreeBuild: NY<0");
            alglib.ap.assert(normtype>=0 && normtype<=2, "KDTreeBuild: incorrect NormType");
            alglib.ap.assert(alglib.ap.rows(xy)>=n, "KDTreeBuild: rows(X)<N");
            alglib.ap.assert(alglib.ap.cols(xy)>=nx+ny || n==0, "KDTreeBuild: cols(X)<NX+NY");
            alglib.ap.assert(apserv.apservisfinitematrix(xy, n, nx+ny), "KDTreeBuild: XY contains infinite or NaN values");
            if( n>0 )
            {
                tags = new int[n];
                for(i=0; i<=n-1; i++)
                {
                    tags[i] = 0;
                }
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
            N       -   number of points, N>=0
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

            alglib.ap.assert(n>=0, "KDTreeBuildTagged: N<0");
            alglib.ap.assert(nx>=1, "KDTreeBuildTagged: NX<1");
            alglib.ap.assert(ny>=0, "KDTreeBuildTagged: NY<0");
            alglib.ap.assert(normtype>=0 && normtype<=2, "KDTreeBuildTagged: incorrect NormType");
            alglib.ap.assert(alglib.ap.rows(xy)>=n, "KDTreeBuildTagged: rows(X)<N");
            alglib.ap.assert(alglib.ap.cols(xy)>=nx+ny || n==0, "KDTreeBuildTagged: cols(X)<NX+NY");
            alglib.ap.assert(apserv.apservisfinitematrix(xy, n, nx+ny), "KDTreeBuildTagged: XY contains infinite or NaN values");
            
            //
            // initialize
            //
            kdt.n = n;
            kdt.nx = nx;
            kdt.ny = ny;
            kdt.normtype = normtype;
            kdt.kcur = 0;
            
            //
            // N=0 => quick exit
            //
            if( n==0 )
            {
                return;
            }
            
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

            alglib.ap.assert(k>=1, "KDTreeQueryKNN: K<1!");
            alglib.ap.assert(alglib.ap.len(x)>=kdt.nx, "KDTreeQueryKNN: Length(X)<NX!");
            alglib.ap.assert(apserv.isfinitevector(x, kdt.nx), "KDTreeQueryKNN: X contains infinite or NaN values!");
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

            alglib.ap.assert((double)(r)>(double)(0), "KDTreeQueryRNN: incorrect R!");
            alglib.ap.assert(alglib.ap.len(x)>=kdt.nx, "KDTreeQueryRNN: Length(X)<NX!");
            alglib.ap.assert(apserv.isfinitevector(x, kdt.nx), "KDTreeQueryRNN: X contains infinite or NaN values!");
            
            //
            // Handle special case: KDT.N=0
            //
            if( kdt.n==0 )
            {
                kdt.kcur = 0;
                result = 0;
                return result;
            }
            
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

            alglib.ap.assert(k>0, "KDTreeQueryAKNN: incorrect K!");
            alglib.ap.assert((double)(eps)>=(double)(0), "KDTreeQueryAKNN: incorrect Eps!");
            alglib.ap.assert(alglib.ap.len(x)>=kdt.nx, "KDTreeQueryAKNN: Length(X)<NX!");
            alglib.ap.assert(apserv.isfinitevector(x, kdt.nx), "KDTreeQueryAKNN: X contains infinite or NaN values!");
            
            //
            // Handle special case: KDT.N=0
            //
            if( kdt.n==0 )
            {
                kdt.kcur = 0;
                result = 0;
                return result;
            }
            
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
            if( alglib.ap.rows(x)<kdt.kcur || alglib.ap.cols(x)<kdt.nx )
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
            if( alglib.ap.rows(xy)<kdt.kcur || alglib.ap.cols(xy)<kdt.nx+kdt.ny )
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
            if( alglib.ap.len(tags)<kdt.kcur )
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
            if( alglib.ap.len(r)<kdt.kcur )
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
            alglib.ap.assert(i0==scodes.getkdtreeserializationcode(), "KDTreeUnserialize: stream header corrupted");
            i1 = s.unserialize_int();
            alglib.ap.assert(i1==kdtreefirstversion, "KDTreeUnserialize: stream header corrupted");
            
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

            alglib.ap.assert(kdt.n>0, "KDTreeSplit: internal error");
            
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
            double v0 = 0;
            double v1 = 0;
            int i_ = 0;
            int i1_ = 0;

            alglib.ap.assert(kdt.n>0, "KDTreeGenerateTreeRec: internal error");
            alglib.ap.assert(i2>i1, "KDTreeGenerateTreeRec: internal error");
            
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
            // Select dimension to split:
            // * D is a dimension number
            // In case bounding box has zero size, we enforce creation of the leaf node.
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
            if( (double)(ds)==(double)(0) )
            {
                kdt.nodes[nodesoffs+0] = i2-i1;
                kdt.nodes[nodesoffs+1] = i1;
                nodesoffs = nodesoffs+2;
                return;
            }
            
            //
            // Select split position S using sliding midpoint rule,
            // rearrange points into [I1,I3) and [I3,I2).
            //
            // In case all points has same value of D-th component
            // (MinV=MaxV) we enforce D-th dimension of bounding
            // box to become exactly zero and repeat tree construction.
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
            if( (double)(minv)==(double)(maxv) )
            {
                
                //
                // In case all points has same value of D-th component
                // (MinV=MaxV) we enforce D-th dimension of bounding
                // box to become exactly zero and repeat tree construction.
                //
                v0 = kdt.curboxmin[d];
                v1 = kdt.curboxmax[d];
                kdt.curboxmin[d] = minv;
                kdt.curboxmax[d] = maxv;
                kdtreegeneratetreerec(kdt, ref nodesoffs, ref splitsoffs, i1, i2, maxleafsize);
                kdt.curboxmin[d] = v0;
                kdt.curboxmax[d] = v1;
                return;
            }
            if( cntless>0 && cntgreater>0 )
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
                        for(i=0; i<=2*nx+ny-1; i++)
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
                        for(i=0; i<=2*nx+ny-1; i++)
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

            alglib.ap.assert(kdt.n>0, "KDTreeQueryNNRec: internal error");
            
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
                    if( (double)(ptdist)==(double)(0) && !kdt.selfmatch )
                    {
                        continue;
                    }
                    
                    //
                    // We CAN'T process point if R-criterion isn't satisfied,
                    // i.e. (RNeeded<>0) AND (PtDist>R).
                    //
                    if( (double)(kdt.rneeded)==(double)(0) || (double)(ptdist)<=(double)(kdt.rneeded) )
                    {
                        
                        //
                        // R-criterion is satisfied, we must either:
                        // * replace worst point, if (KNeeded<>0) AND (KCur=KNeeded)
                        //   (or skip, if worst point is better)
                        // * add point without replacement otherwise
                        //
                        if( kdt.kcur<kdt.kneeded || kdt.kneeded==0 )
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
                    if( (double)(kdt.rneeded)!=(double)(0) && (double)(kdt.curdist)>(double)(kdt.rneeded) )
                    {
                        todive = false;
                    }
                    else
                    {
                        if( kdt.kcur<kdt.kneeded || kdt.kneeded==0 )
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

            alglib.ap.assert(kdt.n>0, "KDTreeInitBox: internal error");
            
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
            alglib.ap.assert(kdt.n>0, "KDTreeAllocDatasetIndependent: internal error");
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
            alglib.ap.assert(n>0, "KDTreeAllocDatasetDependent: internal error");
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
            alglib.ap.assert(n>0, "KDTreeAllocTemporaries: internal error");
            kdt.x = new double[nx];
            kdt.idx = new int[n];
            kdt.r = new double[n];
            kdt.buf = new double[Math.Max(n, nx)];
            kdt.curboxmin = new double[nx];
            kdt.curboxmax = new double[nx];
        }


    }
    public class xdebug
    {
        public class xdebugrecord1 : apobject
        {
            public int i;
            public complex c;
            public double[] a;
            public xdebugrecord1()
            {
                init();
            }
            public override void init()
            {
                a = new double[0];
            }
            public override alglib.apobject make_copy()
            {
                xdebugrecord1 _result = new xdebugrecord1();
                _result.i = i;
                _result.c = c;
                _result.a = (double[])a.Clone();
                return _result;
            }
        };




        /*************************************************************************
        This is debug function intended for testing ALGLIB interface generator.
        Never use it in any real life project.

        Creates and returns XDebugRecord1 structure:
        * integer and complex fields of Rec1 are set to 1 and 1+i correspondingly
        * array field of Rec1 is set to [2,3]

          -- ALGLIB --
             Copyright 27.05.2014 by Bochkanov Sergey
        *************************************************************************/
        public static void xdebuginitrecord1(xdebugrecord1 rec1)
        {
            rec1.i = 1;
            rec1.c.x = 1;
            rec1.c.y = 1;
            rec1.a = new double[2];
            rec1.a[0] = 2;
            rec1.a[1] = 3;
        }


        /*************************************************************************
        This is debug function intended for testing ALGLIB interface generator.
        Never use it in any real life project.

        Counts number of True values in the boolean 1D array.

          -- ALGLIB --
             Copyright 11.10.2013 by Bochkanov Sergey
        *************************************************************************/
        public static int xdebugb1count(bool[] a)
        {
            int result = 0;
            int i = 0;

            result = 0;
            for(i=0; i<=alglib.ap.len(a)-1; i++)
            {
                if( a[i] )
                {
                    result = result+1;
                }
            }
            return result;
        }


        /*************************************************************************
        This is debug function intended for testing ALGLIB interface generator.
        Never use it in any real life project.

        Replace all values in array by NOT(a[i]).
        Array is passed using "shared" convention.

          -- ALGLIB --
             Copyright 11.10.2013 by Bochkanov Sergey
        *************************************************************************/
        public static void xdebugb1not(bool[] a)
        {
            int i = 0;

            for(i=0; i<=alglib.ap.len(a)-1; i++)
            {
                a[i] = !a[i];
            }
        }


        /*************************************************************************
        This is debug function intended for testing ALGLIB interface generator.
        Never use it in any real life project.

        Appends copy of array to itself.
        Array is passed using "var" convention.

          -- ALGLIB --
             Copyright 11.10.2013 by Bochkanov Sergey
        *************************************************************************/
        public static void xdebugb1appendcopy(ref bool[] a)
        {
            int i = 0;
            bool[] b = new bool[0];

            b = new bool[alglib.ap.len(a)];
            for(i=0; i<=alglib.ap.len(b)-1; i++)
            {
                b[i] = a[i];
            }
            a = new bool[2*alglib.ap.len(b)];
            for(i=0; i<=alglib.ap.len(a)-1; i++)
            {
                a[i] = b[i%alglib.ap.len(b)];
            }
        }


        /*************************************************************************
        This is debug function intended for testing ALGLIB interface generator.
        Never use it in any real life project.

        Generate N-element array with even-numbered elements set to True.
        Array is passed using "out" convention.

          -- ALGLIB --
             Copyright 11.10.2013 by Bochkanov Sergey
        *************************************************************************/
        public static void xdebugb1outeven(int n,
            ref bool[] a)
        {
            int i = 0;

            a = new bool[0];

            a = new bool[n];
            for(i=0; i<=alglib.ap.len(a)-1; i++)
            {
                a[i] = i%2==0;
            }
        }


        /*************************************************************************
        This is debug function intended for testing ALGLIB interface generator.
        Never use it in any real life project.

        Returns sum of elements in the array.

          -- ALGLIB --
             Copyright 11.10.2013 by Bochkanov Sergey
        *************************************************************************/
        public static int xdebugi1sum(int[] a)
        {
            int result = 0;
            int i = 0;

            result = 0;
            for(i=0; i<=alglib.ap.len(a)-1; i++)
            {
                result = result+a[i];
            }
            return result;
        }


        /*************************************************************************
        This is debug function intended for testing ALGLIB interface generator.
        Never use it in any real life project.

        Replace all values in array by -A[I]
        Array is passed using "shared" convention.

          -- ALGLIB --
             Copyright 11.10.2013 by Bochkanov Sergey
        *************************************************************************/
        public static void xdebugi1neg(int[] a)
        {
            int i = 0;

            for(i=0; i<=alglib.ap.len(a)-1; i++)
            {
                a[i] = -a[i];
            }
        }


        /*************************************************************************
        This is debug function intended for testing ALGLIB interface generator.
        Never use it in any real life project.

        Appends copy of array to itself.
        Array is passed using "var" convention.

          -- ALGLIB --
             Copyright 11.10.2013 by Bochkanov Sergey
        *************************************************************************/
        public static void xdebugi1appendcopy(ref int[] a)
        {
            int i = 0;
            int[] b = new int[0];

            b = new int[alglib.ap.len(a)];
            for(i=0; i<=alglib.ap.len(b)-1; i++)
            {
                b[i] = a[i];
            }
            a = new int[2*alglib.ap.len(b)];
            for(i=0; i<=alglib.ap.len(a)-1; i++)
            {
                a[i] = b[i%alglib.ap.len(b)];
            }
        }


        /*************************************************************************
        This is debug function intended for testing ALGLIB interface generator.
        Never use it in any real life project.

        Generate N-element array with even-numbered A[I] set to I, and odd-numbered
        ones set to 0.

        Array is passed using "out" convention.

          -- ALGLIB --
             Copyright 11.10.2013 by Bochkanov Sergey
        *************************************************************************/
        public static void xdebugi1outeven(int n,
            ref int[] a)
        {
            int i = 0;

            a = new int[0];

            a = new int[n];
            for(i=0; i<=alglib.ap.len(a)-1; i++)
            {
                if( i%2==0 )
                {
                    a[i] = i;
                }
                else
                {
                    a[i] = 0;
                }
            }
        }


        /*************************************************************************
        This is debug function intended for testing ALGLIB interface generator.
        Never use it in any real life project.

        Returns sum of elements in the array.

          -- ALGLIB --
             Copyright 11.10.2013 by Bochkanov Sergey
        *************************************************************************/
        public static double xdebugr1sum(double[] a)
        {
            double result = 0;
            int i = 0;

            result = 0;
            for(i=0; i<=alglib.ap.len(a)-1; i++)
            {
                result = result+a[i];
            }
            return result;
        }


        /*************************************************************************
        This is debug function intended for testing ALGLIB interface generator.
        Never use it in any real life project.

        Replace all values in array by -A[I]
        Array is passed using "shared" convention.

          -- ALGLIB --
             Copyright 11.10.2013 by Bochkanov Sergey
        *************************************************************************/
        public static void xdebugr1neg(double[] a)
        {
            int i = 0;

            for(i=0; i<=alglib.ap.len(a)-1; i++)
            {
                a[i] = -a[i];
            }
        }


        /*************************************************************************
        This is debug function intended for testing ALGLIB interface generator.
        Never use it in any real life project.

        Appends copy of array to itself.
        Array is passed using "var" convention.

          -- ALGLIB --
             Copyright 11.10.2013 by Bochkanov Sergey
        *************************************************************************/
        public static void xdebugr1appendcopy(ref double[] a)
        {
            int i = 0;
            double[] b = new double[0];

            b = new double[alglib.ap.len(a)];
            for(i=0; i<=alglib.ap.len(b)-1; i++)
            {
                b[i] = a[i];
            }
            a = new double[2*alglib.ap.len(b)];
            for(i=0; i<=alglib.ap.len(a)-1; i++)
            {
                a[i] = b[i%alglib.ap.len(b)];
            }
        }


        /*************************************************************************
        This is debug function intended for testing ALGLIB interface generator.
        Never use it in any real life project.

        Generate N-element array with even-numbered A[I] set to I*0.25,
        and odd-numbered ones are set to 0.

        Array is passed using "out" convention.

          -- ALGLIB --
             Copyright 11.10.2013 by Bochkanov Sergey
        *************************************************************************/
        public static void xdebugr1outeven(int n,
            ref double[] a)
        {
            int i = 0;

            a = new double[0];

            a = new double[n];
            for(i=0; i<=alglib.ap.len(a)-1; i++)
            {
                if( i%2==0 )
                {
                    a[i] = i*0.25;
                }
                else
                {
                    a[i] = 0;
                }
            }
        }


        /*************************************************************************
        This is debug function intended for testing ALGLIB interface generator.
        Never use it in any real life project.

        Returns sum of elements in the array.

          -- ALGLIB --
             Copyright 11.10.2013 by Bochkanov Sergey
        *************************************************************************/
        public static complex xdebugc1sum(complex[] a)
        {
            complex result = 0;
            int i = 0;

            result = 0;
            for(i=0; i<=alglib.ap.len(a)-1; i++)
            {
                result = result+a[i];
            }
            return result;
        }


        /*************************************************************************
        This is debug function intended for testing ALGLIB interface generator.
        Never use it in any real life project.

        Replace all values in array by -A[I]
        Array is passed using "shared" convention.

          -- ALGLIB --
             Copyright 11.10.2013 by Bochkanov Sergey
        *************************************************************************/
        public static void xdebugc1neg(complex[] a)
        {
            int i = 0;

            for(i=0; i<=alglib.ap.len(a)-1; i++)
            {
                a[i] = -a[i];
            }
        }


        /*************************************************************************
        This is debug function intended for testing ALGLIB interface generator.
        Never use it in any real life project.

        Appends copy of array to itself.
        Array is passed using "var" convention.

          -- ALGLIB --
             Copyright 11.10.2013 by Bochkanov Sergey
        *************************************************************************/
        public static void xdebugc1appendcopy(ref complex[] a)
        {
            int i = 0;
            complex[] b = new complex[0];

            b = new complex[alglib.ap.len(a)];
            for(i=0; i<=alglib.ap.len(b)-1; i++)
            {
                b[i] = a[i];
            }
            a = new complex[2*alglib.ap.len(b)];
            for(i=0; i<=alglib.ap.len(a)-1; i++)
            {
                a[i] = b[i%alglib.ap.len(b)];
            }
        }


        /*************************************************************************
        This is debug function intended for testing ALGLIB interface generator.
        Never use it in any real life project.

        Generate N-element array with even-numbered A[K] set to (x,y) = (K*0.25, K*0.125)
        and odd-numbered ones are set to 0.

        Array is passed using "out" convention.

          -- ALGLIB --
             Copyright 11.10.2013 by Bochkanov Sergey
        *************************************************************************/
        public static void xdebugc1outeven(int n,
            ref complex[] a)
        {
            int i = 0;

            a = new complex[0];

            a = new complex[n];
            for(i=0; i<=alglib.ap.len(a)-1; i++)
            {
                if( i%2==0 )
                {
                    a[i].x = i*0.250;
                    a[i].y = i*0.125;
                }
                else
                {
                    a[i] = 0;
                }
            }
        }


        /*************************************************************************
        This is debug function intended for testing ALGLIB interface generator.
        Never use it in any real life project.

        Counts number of True values in the boolean 2D array.

          -- ALGLIB --
             Copyright 11.10.2013 by Bochkanov Sergey
        *************************************************************************/
        public static int xdebugb2count(bool[,] a)
        {
            int result = 0;
            int i = 0;
            int j = 0;

            result = 0;
            for(i=0; i<=alglib.ap.rows(a)-1; i++)
            {
                for(j=0; j<=alglib.ap.cols(a)-1; j++)
                {
                    if( a[i,j] )
                    {
                        result = result+1;
                    }
                }
            }
            return result;
        }


        /*************************************************************************
        This is debug function intended for testing ALGLIB interface generator.
        Never use it in any real life project.

        Replace all values in array by NOT(a[i]).
        Array is passed using "shared" convention.

          -- ALGLIB --
             Copyright 11.10.2013 by Bochkanov Sergey
        *************************************************************************/
        public static void xdebugb2not(bool[,] a)
        {
            int i = 0;
            int j = 0;

            for(i=0; i<=alglib.ap.rows(a)-1; i++)
            {
                for(j=0; j<=alglib.ap.cols(a)-1; j++)
                {
                    a[i,j] = !a[i,j];
                }
            }
        }


        /*************************************************************************
        This is debug function intended for testing ALGLIB interface generator.
        Never use it in any real life project.

        Transposes array.
        Array is passed using "var" convention.

          -- ALGLIB --
             Copyright 11.10.2013 by Bochkanov Sergey
        *************************************************************************/
        public static void xdebugb2transpose(ref bool[,] a)
        {
            int i = 0;
            int j = 0;
            bool[,] b = new bool[0,0];

            b = new bool[alglib.ap.rows(a), alglib.ap.cols(a)];
            for(i=0; i<=alglib.ap.rows(b)-1; i++)
            {
                for(j=0; j<=alglib.ap.cols(b)-1; j++)
                {
                    b[i,j] = a[i,j];
                }
            }
            a = new bool[alglib.ap.cols(b), alglib.ap.rows(b)];
            for(i=0; i<=alglib.ap.rows(b)-1; i++)
            {
                for(j=0; j<=alglib.ap.cols(b)-1; j++)
                {
                    a[j,i] = b[i,j];
                }
            }
        }


        /*************************************************************************
        This is debug function intended for testing ALGLIB interface generator.
        Never use it in any real life project.

        Generate MxN matrix with elements set to "Sin(3*I+5*J)>0"
        Array is passed using "out" convention.

          -- ALGLIB --
             Copyright 11.10.2013 by Bochkanov Sergey
        *************************************************************************/
        public static void xdebugb2outsin(int m,
            int n,
            ref bool[,] a)
        {
            int i = 0;
            int j = 0;

            a = new bool[0,0];

            a = new bool[m, n];
            for(i=0; i<=alglib.ap.rows(a)-1; i++)
            {
                for(j=0; j<=alglib.ap.cols(a)-1; j++)
                {
                    a[i,j] = (double)(Math.Sin(3*i+5*j))>(double)(0);
                }
            }
        }


        /*************************************************************************
        This is debug function intended for testing ALGLIB interface generator.
        Never use it in any real life project.

        Returns sum of elements in the array.

          -- ALGLIB --
             Copyright 11.10.2013 by Bochkanov Sergey
        *************************************************************************/
        public static int xdebugi2sum(int[,] a)
        {
            int result = 0;
            int i = 0;
            int j = 0;

            result = 0;
            for(i=0; i<=alglib.ap.rows(a)-1; i++)
            {
                for(j=0; j<=alglib.ap.cols(a)-1; j++)
                {
                    result = result+a[i,j];
                }
            }
            return result;
        }


        /*************************************************************************
        This is debug function intended for testing ALGLIB interface generator.
        Never use it in any real life project.

        Replace all values in array by -a[i,j]
        Array is passed using "shared" convention.

          -- ALGLIB --
             Copyright 11.10.2013 by Bochkanov Sergey
        *************************************************************************/
        public static void xdebugi2neg(int[,] a)
        {
            int i = 0;
            int j = 0;

            for(i=0; i<=alglib.ap.rows(a)-1; i++)
            {
                for(j=0; j<=alglib.ap.cols(a)-1; j++)
                {
                    a[i,j] = -a[i,j];
                }
            }
        }


        /*************************************************************************
        This is debug function intended for testing ALGLIB interface generator.
        Never use it in any real life project.

        Transposes array.
        Array is passed using "var" convention.

          -- ALGLIB --
             Copyright 11.10.2013 by Bochkanov Sergey
        *************************************************************************/
        public static void xdebugi2transpose(ref int[,] a)
        {
            int i = 0;
            int j = 0;
            int[,] b = new int[0,0];

            b = new int[alglib.ap.rows(a), alglib.ap.cols(a)];
            for(i=0; i<=alglib.ap.rows(b)-1; i++)
            {
                for(j=0; j<=alglib.ap.cols(b)-1; j++)
                {
                    b[i,j] = a[i,j];
                }
            }
            a = new int[alglib.ap.cols(b), alglib.ap.rows(b)];
            for(i=0; i<=alglib.ap.rows(b)-1; i++)
            {
                for(j=0; j<=alglib.ap.cols(b)-1; j++)
                {
                    a[j,i] = b[i,j];
                }
            }
        }


        /*************************************************************************
        This is debug function intended for testing ALGLIB interface generator.
        Never use it in any real life project.

        Generate MxN matrix with elements set to "Sign(Sin(3*I+5*J))"
        Array is passed using "out" convention.

          -- ALGLIB --
             Copyright 11.10.2013 by Bochkanov Sergey
        *************************************************************************/
        public static void xdebugi2outsin(int m,
            int n,
            ref int[,] a)
        {
            int i = 0;
            int j = 0;

            a = new int[0,0];

            a = new int[m, n];
            for(i=0; i<=alglib.ap.rows(a)-1; i++)
            {
                for(j=0; j<=alglib.ap.cols(a)-1; j++)
                {
                    a[i,j] = Math.Sign(Math.Sin(3*i+5*j));
                }
            }
        }


        /*************************************************************************
        This is debug function intended for testing ALGLIB interface generator.
        Never use it in any real life project.

        Returns sum of elements in the array.

          -- ALGLIB --
             Copyright 11.10.2013 by Bochkanov Sergey
        *************************************************************************/
        public static double xdebugr2sum(double[,] a)
        {
            double result = 0;
            int i = 0;
            int j = 0;

            result = 0;
            for(i=0; i<=alglib.ap.rows(a)-1; i++)
            {
                for(j=0; j<=alglib.ap.cols(a)-1; j++)
                {
                    result = result+a[i,j];
                }
            }
            return result;
        }


        /*************************************************************************
        This is debug function intended for testing ALGLIB interface generator.
        Never use it in any real life project.

        Replace all values in array by -a[i,j]
        Array is passed using "shared" convention.

          -- ALGLIB --
             Copyright 11.10.2013 by Bochkanov Sergey
        *************************************************************************/
        public static void xdebugr2neg(double[,] a)
        {
            int i = 0;
            int j = 0;

            for(i=0; i<=alglib.ap.rows(a)-1; i++)
            {
                for(j=0; j<=alglib.ap.cols(a)-1; j++)
                {
                    a[i,j] = -a[i,j];
                }
            }
        }


        /*************************************************************************
        This is debug function intended for testing ALGLIB interface generator.
        Never use it in any real life project.

        Transposes array.
        Array is passed using "var" convention.

          -- ALGLIB --
             Copyright 11.10.2013 by Bochkanov Sergey
        *************************************************************************/
        public static void xdebugr2transpose(ref double[,] a)
        {
            int i = 0;
            int j = 0;
            double[,] b = new double[0,0];

            b = new double[alglib.ap.rows(a), alglib.ap.cols(a)];
            for(i=0; i<=alglib.ap.rows(b)-1; i++)
            {
                for(j=0; j<=alglib.ap.cols(b)-1; j++)
                {
                    b[i,j] = a[i,j];
                }
            }
            a = new double[alglib.ap.cols(b), alglib.ap.rows(b)];
            for(i=0; i<=alglib.ap.rows(b)-1; i++)
            {
                for(j=0; j<=alglib.ap.cols(b)-1; j++)
                {
                    a[j,i] = b[i,j];
                }
            }
        }


        /*************************************************************************
        This is debug function intended for testing ALGLIB interface generator.
        Never use it in any real life project.

        Generate MxN matrix with elements set to "Sin(3*I+5*J)"
        Array is passed using "out" convention.

          -- ALGLIB --
             Copyright 11.10.2013 by Bochkanov Sergey
        *************************************************************************/
        public static void xdebugr2outsin(int m,
            int n,
            ref double[,] a)
        {
            int i = 0;
            int j = 0;

            a = new double[0,0];

            a = new double[m, n];
            for(i=0; i<=alglib.ap.rows(a)-1; i++)
            {
                for(j=0; j<=alglib.ap.cols(a)-1; j++)
                {
                    a[i,j] = Math.Sin(3*i+5*j);
                }
            }
        }


        /*************************************************************************
        This is debug function intended for testing ALGLIB interface generator.
        Never use it in any real life project.

        Returns sum of elements in the array.

          -- ALGLIB --
             Copyright 11.10.2013 by Bochkanov Sergey
        *************************************************************************/
        public static complex xdebugc2sum(complex[,] a)
        {
            complex result = 0;
            int i = 0;
            int j = 0;

            result = 0;
            for(i=0; i<=alglib.ap.rows(a)-1; i++)
            {
                for(j=0; j<=alglib.ap.cols(a)-1; j++)
                {
                    result = result+a[i,j];
                }
            }
            return result;
        }


        /*************************************************************************
        This is debug function intended for testing ALGLIB interface generator.
        Never use it in any real life project.

        Replace all values in array by -a[i,j]
        Array is passed using "shared" convention.

          -- ALGLIB --
             Copyright 11.10.2013 by Bochkanov Sergey
        *************************************************************************/
        public static void xdebugc2neg(complex[,] a)
        {
            int i = 0;
            int j = 0;

            for(i=0; i<=alglib.ap.rows(a)-1; i++)
            {
                for(j=0; j<=alglib.ap.cols(a)-1; j++)
                {
                    a[i,j] = -a[i,j];
                }
            }
        }


        /*************************************************************************
        This is debug function intended for testing ALGLIB interface generator.
        Never use it in any real life project.

        Transposes array.
        Array is passed using "var" convention.

          -- ALGLIB --
             Copyright 11.10.2013 by Bochkanov Sergey
        *************************************************************************/
        public static void xdebugc2transpose(ref complex[,] a)
        {
            int i = 0;
            int j = 0;
            complex[,] b = new complex[0,0];

            b = new complex[alglib.ap.rows(a), alglib.ap.cols(a)];
            for(i=0; i<=alglib.ap.rows(b)-1; i++)
            {
                for(j=0; j<=alglib.ap.cols(b)-1; j++)
                {
                    b[i,j] = a[i,j];
                }
            }
            a = new complex[alglib.ap.cols(b), alglib.ap.rows(b)];
            for(i=0; i<=alglib.ap.rows(b)-1; i++)
            {
                for(j=0; j<=alglib.ap.cols(b)-1; j++)
                {
                    a[j,i] = b[i,j];
                }
            }
        }


        /*************************************************************************
        This is debug function intended for testing ALGLIB interface generator.
        Never use it in any real life project.

        Generate MxN matrix with elements set to "Sin(3*I+5*J),Cos(3*I+5*J)"
        Array is passed using "out" convention.

          -- ALGLIB --
             Copyright 11.10.2013 by Bochkanov Sergey
        *************************************************************************/
        public static void xdebugc2outsincos(int m,
            int n,
            ref complex[,] a)
        {
            int i = 0;
            int j = 0;

            a = new complex[0,0];

            a = new complex[m, n];
            for(i=0; i<=alglib.ap.rows(a)-1; i++)
            {
                for(j=0; j<=alglib.ap.cols(a)-1; j++)
                {
                    a[i,j].x = Math.Sin(3*i+5*j);
                    a[i,j].y = Math.Cos(3*i+5*j);
                }
            }
        }


        /*************************************************************************
        This is debug function intended for testing ALGLIB interface generator.
        Never use it in any real life project.

        Returns sum of a[i,j]*(1+b[i,j]) such that c[i,j] is True

          -- ALGLIB --
             Copyright 11.10.2013 by Bochkanov Sergey
        *************************************************************************/
        public static double xdebugmaskedbiasedproductsum(int m,
            int n,
            double[,] a,
            double[,] b,
            bool[,] c)
        {
            double result = 0;
            int i = 0;
            int j = 0;

            alglib.ap.assert(m>=alglib.ap.rows(a));
            alglib.ap.assert(m>=alglib.ap.rows(b));
            alglib.ap.assert(m>=alglib.ap.rows(c));
            alglib.ap.assert(n>=alglib.ap.cols(a));
            alglib.ap.assert(n>=alglib.ap.cols(b));
            alglib.ap.assert(n>=alglib.ap.cols(c));
            result = 0.0;
            for(i=0; i<=m-1; i++)
            {
                for(j=0; j<=n-1; j++)
                {
                    if( c[i,j] )
                    {
                        result = result+a[i,j]*(1+b[i,j]);
                    }
                }
            }
            return result;
        }


    }
}

