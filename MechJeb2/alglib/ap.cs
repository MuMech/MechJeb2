/**************************************************************************
ALGLIB 3.19.0 (source code generated 2022-06-07)
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
**************************************************************************/
#if ALGLIB_USE_SIMD
#define _ALGLIB_ALREADY_DEFINED_SIMD_ALIASES
using Sse2 = System.Runtime.Intrinsics.X86.Sse2;
using Avx2 = System.Runtime.Intrinsics.X86.Avx2;
using Fma  = System.Runtime.Intrinsics.X86.Fma;
using Intrinsics = System.Runtime.Intrinsics;
#endif
using System;
public partial class alglib
{
    /********************************************************************
    Callback definitions for optimizers/fitters/solvers.
    
    Callbacks for unparameterized (general) functions:
    * ndimensional_func         calculates f(arg), stores result to func
    * ndimensional_grad         calculates func = f(arg), 
                                grad[i] = df(arg)/d(arg[i])
    * ndimensional_hess         calculates func = f(arg),
                                grad[i] = df(arg)/d(arg[i]),
                                hess[i,j] = d2f(arg)/(d(arg[i])*d(arg[j]))
    
    Callbacks for systems of functions:
    * ndimensional_fvec         calculates vector function f(arg),
                                stores result to fi
    * ndimensional_jac          calculates f[i] = fi(arg)
                                jac[i,j] = df[i](arg)/d(arg[j])
                                
    Callbacks for  parameterized  functions,  i.e.  for  functions  which 
    depend on two vectors: P and Q.  Gradient  and Hessian are calculated 
    with respect to P only.
    * ndimensional_pfunc        calculates f(p,q),
                                stores result to func
    * ndimensional_pgrad        calculates func = f(p,q),
                                grad[i] = df(p,q)/d(p[i])
    * ndimensional_phess        calculates func = f(p,q),
                                grad[i] = df(p,q)/d(p[i]),
                                hess[i,j] = d2f(p,q)/(d(p[i])*d(p[j]))

    Callbacks for progress reports:
    * ndimensional_rep          reports current position of optimization algo    
    
    Callbacks for ODE solvers:
    * ndimensional_ode_rp       calculates dy/dx for given y[] and x
    
    Callbacks for integrators:
    * integrator1_func          calculates f(x) for given x
                                (additional parameters xminusa and bminusx
                                contain x-a and b-x)
    ********************************************************************/
    public delegate void ndimensional_func (double[] arg, ref double func, object obj);
    public delegate void ndimensional_grad (double[] arg, ref double func, double[] grad, object obj);
    public delegate void ndimensional_hess (double[] arg, ref double func, double[] grad, double[,] hess, object obj);
    
    public delegate void ndimensional_fvec (double[] arg, double[] fi, object obj);
    public delegate void ndimensional_jac  (double[] arg, double[] fi, double[,] jac, object obj);
    
    public delegate void ndimensional_pfunc(double[] p, double[] q, ref double func, object obj);
    public delegate void ndimensional_pgrad(double[] p, double[] q, ref double func, double[] grad, object obj);
    public delegate void ndimensional_phess(double[] p, double[] q, ref double func, double[] grad, double[,] hess, object obj);
    
    public delegate void ndimensional_rep(double[] arg, double func, object obj);

    public delegate void ndimensional_ode_rp (double[] y, double x, double[] dy, object obj);

    public delegate void integrator1_func (double x, double xminusa, double bminusx, ref double f, object obj);

    /********************************************************************
    Class defining a complex number with double precision.
    ********************************************************************/
    public struct complex
    {
        public double x;
        public double y;

        public complex(double _x)
        {
            x = _x;
            y = 0;
        }
        public complex(double _x, double _y)
        {
            x = _x;
            y = _y;
        }
        public static implicit operator complex(double _x)
        {
            return new complex(_x);
        }
        public static bool operator==(complex lhs, complex rhs)
        {
            return ((double)lhs.x==(double)rhs.x) & ((double)lhs.y==(double)rhs.y);
        }
        public static bool operator!=(complex lhs, complex rhs)
        {
            return ((double)lhs.x!=(double)rhs.x) | ((double)lhs.y!=(double)rhs.y);
        }
        public static complex operator+(complex lhs)
        {
            return lhs;
        }
        public static complex operator-(complex lhs)
        {
            return new complex(-lhs.x,-lhs.y);
        }
        public static complex operator+(complex lhs, complex rhs)
        {
            return new complex(lhs.x+rhs.x,lhs.y+rhs.y);
        }
        public static complex operator-(complex lhs, complex rhs)
        {
            return new complex(lhs.x-rhs.x,lhs.y-rhs.y);
        }
        public static complex operator*(complex lhs, complex rhs)
        { 
            return new complex(lhs.x*rhs.x-lhs.y*rhs.y, lhs.x*rhs.y+lhs.y*rhs.x);
        }
        public static complex operator/(complex lhs, complex rhs)
        {
            complex result;
            double e;
            double f;
            if( System.Math.Abs(rhs.y)<System.Math.Abs(rhs.x) )
            {
                e = rhs.y/rhs.x;
                f = rhs.x+rhs.y*e;
                result.x = (lhs.x+lhs.y*e)/f;
                result.y = (lhs.y-lhs.x*e)/f;
            }
            else
            {
                e = rhs.x/rhs.y;
                f = rhs.y+rhs.x*e;
                result.x = (lhs.y+lhs.x*e)/f;
                result.y = (-lhs.x+lhs.y*e)/f;
            }
            return result;
        }
        public override int GetHashCode() 
        { 
            return x.GetHashCode() ^ y.GetHashCode(); 
        }
        public override bool Equals(object obj) 
        { 
            if( obj is byte)
                return Equals(new complex((byte)obj));
            if( obj is sbyte)
                return Equals(new complex((sbyte)obj));
            if( obj is short)
                return Equals(new complex((short)obj));
            if( obj is ushort)
                return Equals(new complex((ushort)obj));
            if( obj is int)
                return Equals(new complex((int)obj));
            if( obj is uint)
                return Equals(new complex((uint)obj));
            if( obj is long)
                return Equals(new complex((long)obj));
            if( obj is ulong)
                return Equals(new complex((ulong)obj));
            if( obj is float)
                return Equals(new complex((float)obj));
            if( obj is double)
                return Equals(new complex((double)obj));
            if( obj is decimal)
                return Equals(new complex((double)(decimal)obj));
            return base.Equals(obj); 
        }    
    }    
    
    /********************************************************************
    Class defining an ALGLIB exception
    ********************************************************************/
    public class alglibexception : System.Exception
    {
        public string msg;
        public alglibexception(string s)
        {
            msg = s;
        }
    }
    
    /********************************************************************
    Critical failure, resilts in immediate termination of entire program.
    ********************************************************************/
    public static void AE_CRITICAL_ASSERT(bool x)
    {
        if( !x )
            System.Environment.FailFast("ALGLIB: critical error");
    }
    
    /********************************************************************
    ALGLIB object, parent  class  for  all  internal  AlgoPascal  objects
    managed by ALGLIB.
    
    Any internal AlgoPascal object inherits from this class.
    
    User-visible objects inherit from alglibobject (see below).
    ********************************************************************/
    public abstract class apobject
    {
        public abstract void init();
        public abstract apobject make_copy();
    }
    
    /********************************************************************
    ALGLIB object, parent class for all user-visible objects  managed  by
    ALGLIB.
    
    Methods:
        _deallocate()       deallocation:
                            * in managed ALGLIB it does nothing
                            * in native ALGLIB it clears  dynamic  memory
                              being  hold  by  object  and  sets internal
                              reference to null.
        make_copy()         creates deep copy of the object.
                            Works in both managed and native versions  of
                            ALGLIB.
    ********************************************************************/
    public abstract class alglibobject : IDisposable
    {
        public virtual void _deallocate() {}
        public abstract alglibobject make_copy();
        public void Dispose()
        {
            _deallocate();
        }
    }
    
    /********************************************************************
    xparams object, used to pass additional parameters like multithreading
    settings, and several predefined values
    ********************************************************************/
    public class xparams
    {
        public ulong flags;
        public xparams(ulong v)
        {
            flags = v;
        }
    }
    private static ulong FLG_THREADING_MASK          = 0x7;
    private static   int FLG_THREADING_SHIFT         = 0;
    private static ulong FLG_THREADING_USE_GLOBAL    = 0x0;
    private static ulong FLG_THREADING_SERIAL        = 0x1;
    private static ulong FLG_THREADING_PARALLEL      = 0x2;
    public static xparams serial   = new xparams(FLG_THREADING_SERIAL);
    public static xparams parallel = new xparams(FLG_THREADING_PARALLEL);

    /********************************************************************
    Global flags, split into several char-sized variables in order
    to avoid problem with non-atomic reads/writes (single-byte ops
    are atomic on all modern architectures);
    
    Following variables are included:
    * threading-related settings
    ********************************************************************/
    public static byte global_threading_flags = (byte)(FLG_THREADING_SERIAL>>FLG_THREADING_SHIFT);
    
    public static void setglobalthreading(xparams p)
    {
        AE_CRITICAL_ASSERT(p!=null);
        ae_set_global_threading(p.flags);
    }
    
    public static void ae_set_global_threading(ulong flg_value)
    {
        flg_value = flg_value&FLG_THREADING_MASK;
        AE_CRITICAL_ASSERT(flg_value==FLG_THREADING_SERIAL || flg_value==FLG_THREADING_PARALLEL);
        global_threading_flags = (byte)(flg_value>>FLG_THREADING_SHIFT);
    }
    
    public static ulong ae_get_global_threading()
    {
        return ((ulong)global_threading_flags)<<FLG_THREADING_SHIFT;
    }
    
    static ulong ae_get_effective_threading(xparams p)
    {
        if( p==null || (p.flags&FLG_THREADING_MASK)==FLG_THREADING_USE_GLOBAL )
            return ((ulong)global_threading_flags)<<FLG_THREADING_SHIFT;
        return p.flags&FLG_THREADING_MASK;
    }
    
    /********************************************************************
    Deallocation of ALGLIB object:
    * in managed ALGLIB this method just sets refence to null
    * in native ALGLIB call of this method:
      1) clears dynamic memory being hold by  object  and  sets  internal
         reference to null.
      2) sets to null variable being passed to this method
    
    IMPORTANT (1): in  native  edition  of  ALGLIB,  obj becomes unusable
                   after this call!!!  It  is  possible  to  save  a copy
                   of reference in another variable (original variable is
                   set to null), but any attempt to work with this object
                   will crash your program.
    
    IMPORTANT (2): memory owned by object will be recycled by GC  in  any
                   case. This method just enforces IMMEDIATE deallocation.
    ********************************************************************/
    public static void deallocateimmediately<T>(ref T obj) where T : alglib.alglibobject
    {
        obj._deallocate();
        obj = null;
    }

    /********************************************************************
    Allocation counter:
    * in managed ALGLIB it always returns 0 (dummy code)
    * in native ALGLIB it returns current value of the allocation counter
      (if it was activated)
    ********************************************************************/
    public static long alloc_counter()
    {
        return 0;
    }
    
    /********************************************************************
    Activization of the allocation counter:
    * in managed ALGLIB it does nothing (dummy code)
    * in native ALGLIB it turns on allocation counting.
    ********************************************************************/
    public static void alloc_counter_activate()
    {
    }
    
    /********************************************************************
    This function allows to set one of the debug flags.
    In managed ALGLIB does nothing (dummy).
    ********************************************************************/
    public static void set_dbg_flag(long flag_id, long flag_value)
    {
    }
    
    /********************************************************************
    This function allows to get one of the debug counters.
    In managed ALGLIB does nothing (dummy).
    ********************************************************************/
    public static long get_dbg_value(long id)
    {
        return 0;
    }
    
    /********************************************************************
    Activization of the allocation counter:
    * in managed ALGLIB it does nothing (dummy code)
    * in native ALGLIB it turns on allocation counting.
    ********************************************************************/
    public static void free_disposed_items()
    {
    }
    
    /************************************************************************
    This function maps nworkers  number  (which  can  be  positive,  zero  or
    negative with 0 meaning "all cores", -1 meaning "all cores -1" and so on)
    to "effective", strictly positive workers count.

    This  function  is  intended  to  be used by debugging/testing code which
    tests different number of worker threads. It is NOT aligned  in  any  way
    with ALGLIB multithreading framework (i.e. it can return  non-zero worker
    count even for single-threaded GPLed ALGLIB).
    ************************************************************************/
    public static int get_effective_workers(int nworkers)
    {
        int ncores = System.Environment.ProcessorCount;
        if( nworkers>=1 )
            return nworkers>ncores ? ncores : nworkers;
        return ncores+nworkers>=1 ? ncores+nworkers : 1;
    }
    
    /********************************************************************
    This function activates trace output, with trace log being  saved  to
    file (appended to the end).

    Tracing allows us to study behavior of ALGLIB solvers  and  to  debug
    their failures:
    * tracing is  limited  by one/several ALGLIB parts specified by means
      of trace tags, like "SLP" (for SLP solver) or "OPTGUARD"  (OptGuard
      integrity checker).
    * some ALGLIB solvers support hierarchies of trace tags which activate
      different kinds of tracing. Say, "SLP" defines some basic  tracing,
      but "SLP.PROBING" defines more detailed and costly tracing.
    * generally, "TRACETAG.SUBTAG"   also  implicitly  activates  logging
      which is activated by "TRACETAG"
    * you may define multiple trace tags by separating them with  commas,
      like "SLP,OPTGUARD,SLP.PROBING"
    * trace tags are case-insensitive
    * spaces/tabs are NOT allowed in the tags string

    Trace log is saved to file "filename", which is opened in the  append
    mode. If no file with such name  can  be  opened,  tracing  won't  be
    performed (but no exception will be generated).
    ********************************************************************/
    public static void trace_file(string tags, string filename)
    {
        ap.trace_file(tags, filename);
    }
    
    /********************************************************************
    This function disables tracing.
    ********************************************************************/
    public static void trace_disable()
    {
        ap.trace_disable();
    }
    
    /********************************************************************
    reverse communication structure
    ********************************************************************/
    public class rcommstate : apobject
    {
        public rcommstate()
        {
            init();
        }
        public override void init()
        {
            stage = -1;
            ia = new int[0];
            ba = new bool[0];
            ra = new double[0];
            ca = new alglib.complex[0];
        }
        public override apobject make_copy()
        {
            rcommstate result = new rcommstate();
            result.stage = stage;
            result.ia = (int[])ia.Clone();
            result.ba = (bool[])ba.Clone();
            result.ra = (double[])ra.Clone();
            result.ca = (alglib.complex[])ca.Clone();
            return result;
        }
        public int stage;
        public int[] ia;
        public bool[] ba;
        public double[] ra;
        public alglib.complex[] ca;
    };

    /********************************************************************
    internal functions
    ********************************************************************/
    public class ap
    {
        public static int len<T>(T[] a)
        { return a.Length; }
        public static int rows<T>(T[,] a)
        { return a.GetLength(0); }
        public static int cols<T>(T[,] a)
        { return a.GetLength(1); }
        public static void swap<T>(ref T a, ref T b)
        {
            T t = a;
            a = b;
            b = t;
        }
        
        public static void assert(bool cond, string s)
        {
            if( !cond )
            {
                if( trace_mode!=TRACE_MODE.NONE )
                    trace("---!!! CRITICAL ERROR !!!--- exception with message '"+s+"' was generated\n");
                throw new alglibexception(s);
            }
        }
        
        public static void assert(bool cond)
        {
            assert(cond, "ALGLIB: assertion failed");
        }
        
        /****************************************************************
        Error tracking for unit testing purposes; utility functions.
        ****************************************************************/
        public static string sef_xdesc = "";
        
        public static void seterrorflag(ref bool flag, bool cond, string xdesc)
        {
            if( cond )
            {
                flag = true;
                sef_xdesc = xdesc;
            }
        }
        
        /****************************************************************
        returns dps (digits-of-precision) value corresponding to threshold.
        dps(0.9)  = dps(0.5)  = dps(0.1) = 0
        dps(0.09) = dps(0.05) = dps(0.01) = 1
        and so on
        ****************************************************************/
        public static int threshold2dps(double threshold)
        {
            int result = 0;
            double t;
            for (result = 0, t = 1; t / 10 > threshold*(1+1E-10); result++, t /= 10) ;
            return result;
        }

        /****************************************************************
        prints formatted complex
        ****************************************************************/
        public static string format(complex a, int _dps)
        {
            int dps = Math.Abs(_dps);
            string fmt = _dps>=0 ? "F" : "E";
            string fmtx = String.Format("{{0:"+fmt+"{0}}}", dps);
            string fmty = String.Format("{{0:"+fmt+"{0}}}", dps);
            string result = String.Format(fmtx, a.x) + (a.y >= 0 ? "+" : "-") + String.Format(fmty, Math.Abs(a.y)) + "i";
            result = result.Replace(',', '.');
            return result;
        }

        /****************************************************************
        prints formatted array
        ****************************************************************/
        public static string format(bool[] a)
        {
            string[] result = new string[len(a)];
            int i;
            for(i=0; i<len(a); i++)
                if( a[i] )
                    result[i] = "true";
                else
                    result[i] = "false";
            return "{"+String.Join(",",result)+"}";
        }
        
        /****************************************************************
        prints formatted array
        ****************************************************************/
        public static string format(int[] a)
        {
            string[] result = new string[len(a)];
            int i;
            for (i = 0; i < len(a); i++)
                result[i] = a[i].ToString();
            return "{" + String.Join(",", result) + "}";
        }

        /****************************************************************
        prints formatted array
        ****************************************************************/
        public static string format(double[] a, int _dps)
        {
            int dps = Math.Abs(_dps);
            string sfmt = _dps >= 0 ? "F" : "E";
            string fmt = String.Format("{{0:" + sfmt + "{0}}}", dps);
            string[] result = new string[len(a)];
            int i;
            for (i = 0; i < len(a); i++)
            {
                result[i] = String.Format(fmt, a[i]);
                result[i] = result[i].Replace(',', '.');
            }
            return "{" + String.Join(",", result) + "}";
        }

        /****************************************************************
        prints formatted array
        ****************************************************************/
        public static string format(complex[] a, int _dps)
        {
            int dps = Math.Abs(_dps);
            string fmt = _dps >= 0 ? "F" : "E";
            string fmtx = String.Format("{{0:"+fmt+"{0}}}", dps);
            string fmty = String.Format("{{0:"+fmt+"{0}}}", dps);
            string[] result = new string[len(a)];
            int i;
            for (i = 0; i < len(a); i++)
            {
                result[i] = String.Format(fmtx, a[i].x) + (a[i].y >= 0 ? "+" : "-") + String.Format(fmty, Math.Abs(a[i].y)) + "i";
                result[i] = result[i].Replace(',', '.');
            }
            return "{" + String.Join(",", result) + "}";
        }

        /****************************************************************
        prints formatted matrix
        ****************************************************************/
        public static string format(bool[,] a)
        {
            int i, j, m, n;
            n = cols(a);
            m = rows(a);
            bool[] line = new bool[n];
            string[] result = new string[m];
            for (i = 0; i < m; i++)
            {
                for (j = 0; j < n; j++)
                    line[j] = a[i, j];
                result[i] = format(line);
            }
            return "{" + String.Join(",", result) + "}";
        }

        /****************************************************************
        prints formatted matrix
        ****************************************************************/
        public static string format(int[,] a)
        {
            int i, j, m, n;
            n = cols(a);
            m = rows(a);
            int[] line = new int[n];
            string[] result = new string[m];
            for (i = 0; i < m; i++)
            {
                for (j = 0; j < n; j++)
                    line[j] = a[i, j];
                result[i] = format(line);
            }
            return "{" + String.Join(",", result) + "}";
        }

        /****************************************************************
        prints formatted matrix
        ****************************************************************/
        public static string format(double[,] a, int dps)
        {
            int i, j, m, n;
            n = cols(a);
            m = rows(a);
            double[] line = new double[n];
            string[] result = new string[m];
            for (i = 0; i < m; i++)
            {
                for (j = 0; j < n; j++)
                    line[j] = a[i, j];
                result[i] = format(line, dps);
            }
            return "{" + String.Join(",", result) + "}";
        }

        /****************************************************************
        prints formatted matrix
        ****************************************************************/
        public static string format(complex[,] a, int dps)
        {
            int i, j, m, n;
            n = cols(a);
            m = rows(a);
            complex[] line = new complex[n];
            string[] result = new string[m];
            for (i = 0; i < m; i++)
            {
                for (j = 0; j < n; j++)
                    line[j] = a[i, j];
                result[i] = format(line, dps);
            }
            return "{" + String.Join(",", result) + "}";
        }

        /****************************************************************
        checks that matrix is symmetric.
        max|A-A^T| is calculated; if it is within 1.0E-14 of max|A|,
        matrix is considered symmetric
        ****************************************************************/
        public static bool issymmetric(double[,] a)
        {
            int i, j, n;
            double err, mx, v1, v2;
            if( rows(a)!=cols(a) )
                return false;
            n = rows(a);
            if( n==0 )
                return true;
            mx = 0;
            err = 0;
            for( i=0; i<n; i++)
            {
                for(j=i+1; j<n; j++)
                {
                    v1 = a[i,j];
                    v2 = a[j,i];
                    if( !math.isfinite(v1) )
                        return false;
                    if( !math.isfinite(v2) )
                        return false;
                    err = Math.Max(err, Math.Abs(v1-v2));
                    mx  = Math.Max(mx,  Math.Abs(v1));
                    mx  = Math.Max(mx,  Math.Abs(v2));
                }
                v1 = a[i,i];
                if( !math.isfinite(v1) )
                    return false;
                mx = Math.Max(mx, Math.Abs(v1));
            }
            if( mx==0 )
                return true;
            return err/mx<=1.0E-14;
        }
        
        /****************************************************************
        checks that matrix is Hermitian.
        max|A-A^H| is calculated; if it is within 1.0E-14 of max|A|,
        matrix is considered Hermitian
        ****************************************************************/
        public static bool ishermitian(complex[,] a)
        {
            int i, j, n;
            double err, mx;
            complex v1, v2, vt;
            if( rows(a)!=cols(a) )
                return false;
            n = rows(a);
            if( n==0 )
                return true;
            mx = 0;
            err = 0;
            for( i=0; i<n; i++)
            {
                for(j=i+1; j<n; j++)
                {
                    v1 = a[i,j];
                    v2 = a[j,i];
                    if( !math.isfinite(v1.x) )
                        return false;
                    if( !math.isfinite(v1.y) )
                        return false;
                    if( !math.isfinite(v2.x) )
                        return false;
                    if( !math.isfinite(v2.y) )
                        return false;
                    vt.x = v1.x-v2.x;
                    vt.y = v1.y+v2.y;
                    err = Math.Max(err, math.abscomplex(vt));
                    mx  = Math.Max(mx,  math.abscomplex(v1));
                    mx  = Math.Max(mx,  math.abscomplex(v2));
                }
                v1 = a[i,i];
                if( !math.isfinite(v1.x) )
                    return false;
                if( !math.isfinite(v1.y) )
                    return false;
                err = Math.Max(err, Math.Abs(v1.y));
                mx = Math.Max(mx, math.abscomplex(v1));
            }
            if( mx==0 )
                return true;
            return err/mx<=1.0E-14;
        }
        
        
        /****************************************************************
        Forces symmetricity by copying upper half of A to the lower one
        ****************************************************************/
        public static bool forcesymmetric(double[,] a)
        {
            int i, j, n;
            if( rows(a)!=cols(a) )
                return false;
            n = rows(a);
            if( n==0 )
                return true;
            for( i=0; i<n; i++)
                for(j=i+1; j<n; j++)
                    a[i,j] = a[j,i];
            return true;
        }
        
        /****************************************************************
        Forces Hermiticity by copying upper half of A to the lower one
        ****************************************************************/
        public static bool forcehermitian(complex[,] a)
        {
            int i, j, n;
            complex v;
            if( rows(a)!=cols(a) )
                return false;
            n = rows(a);
            if( n==0 )
                return true;
            for( i=0; i<n; i++)
                for(j=i+1; j<n; j++)
                {
                    v = a[j,i];
                    a[i,j].x = v.x;
                    a[i,j].y = -v.y;
                }
            return true;
        }
        
        /********************************************************************
        Tracing and logging
        ********************************************************************/
        enum TRACE_MODE { NONE, FILE };
        private static TRACE_MODE trace_mode = TRACE_MODE.NONE;
        private static string trace_tags = "";
        private static string trace_filename = "";
        
        public static void trace_file(string tags, string filename)
        {
            trace_mode     = TRACE_MODE.FILE;
            trace_tags     = ","+tags.ToLower()+",";
            trace_filename = filename;
            trace("####################################################################################################\n");
            trace("# TRACING ENABLED: "+System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")+"\n");
            trace("# TRACE TAGS:      '"+tags+"'\n");
            trace("####################################################################################################\n");
        }
        
        public static void trace_disable()
        {
            trace_mode     = TRACE_MODE.NONE;
            trace_tags     = "";
        }
        
        public static bool istraceenabled(string tag, xparams _params)
        {
            // trace disabled
            if( trace_mode==TRACE_MODE.NONE )
                return false;
            
            // contains tag (followed by comma, which means exact match)
            if( trace_tags.Contains(","+tag.ToLower()+",") )
                return true;
            
            // contains tag (followed by dot, which means match with child)
            if( trace_tags.Contains(","+tag.ToLower()+".") )
                return true;
            
            // nothing
            return false;
        }
        
        public static void trace(string s)
        {
            if( trace_mode==TRACE_MODE.NONE )
                return;
            if( trace_mode==TRACE_MODE.FILE )
            {
                System.IO.File.AppendAllText(trace_filename,s);
                return;
            }
        }
    };
    
    /********************************************************************
    math functions
    ********************************************************************/
    public class math
    {
        //public static System.Random RndObject = new System.Random(System.DateTime.Now.Millisecond);
        public static System.Random rndobject = new System.Random(System.DateTime.Now.Millisecond + 1000*System.DateTime.Now.Second + 60*1000*System.DateTime.Now.Minute);

        public const double machineepsilon = 5E-16;
        public const double maxrealnumber = 1E300;
        public const double minrealnumber = 1E-300;
        
        public static bool isfinite(double d)
        {
            return !System.Double.IsNaN(d) && !System.Double.IsInfinity(d);
        }
        
        public static double randomreal()
        {
            double r = 0;
            lock(rndobject){ r = rndobject.NextDouble(); }
            return r;
        }
        public static int randominteger(int N)
        {
            int r = 0;
            lock(rndobject){ r = rndobject.Next(N); }
            return r;
        }
        public static double sqr(double X)
        {
            return X*X;
        }        
        public static double abscomplex(complex z)
        {
            double w;
            double xabs;
            double yabs;
            double v;
    
            xabs = System.Math.Abs(z.x);
            yabs = System.Math.Abs(z.y);
            w = xabs>yabs ? xabs : yabs;
            v = xabs<yabs ? xabs : yabs; 
            if( v==0 )
                return w;
            else
            {
                double t = v/w;
                return w*System.Math.Sqrt(1+t*t);
            }
        }
        public static complex conj(complex z)
        {
            return new complex(z.x, -z.y); 
        }    
        public static complex csqr(complex z)
        {
            return new complex(z.x*z.x-z.y*z.y, 2*z.x*z.y); 
        }

    }

    /*
     * CSV functionality
     */
     
    public static int CSV_DEFAULT      = 0x0;
    public static int CSV_SKIP_HEADERS = 0x1;
    
    /*
     * CSV operations: reading CSV file to real matrix.
     * 
     * This function reads CSV  file  and  stores  its  contents  to  double
     * precision 2D array. Format of the data file must conform to RFC  4180
     * specification, with additional notes:
     * - file size should be less than 2GB
     * - ASCI encoding, UTF-8 without BOM (in header names) are supported
     * - any character (comma/tab/space) may be used as field separator,  as
     *   long as it is distinct from one used for decimal point
     * - multiple subsequent field separators (say, two  spaces) are treated
     *   as MULTIPLE separators, not one big separator
     * - both comma and full stop may be used as decimal point. Parser  will
     *   automatically determine specific character being used.  Both  fixed
     *   and exponential number formats are  allowed.   Thousand  separators
     *   are NOT allowed.
     * - line may end with \n (Unix style) or \r\n (Windows  style),  parser
     *   will automatically adapt to chosen convention
     * - escaped fields (ones in double quotes) are not supported
     * 
     * INPUT PARAMETERS:
     *     filename        relative/absolute path
     *     separator       character used to separate fields.  May  be  ' ',
     *                     ',', '\t'. Other separators are possible too.
     *     flags           several values combined with bitwise OR:
     *                     * alglib::CSV_SKIP_HEADERS -  if present, first row
     *                       contains headers  and  will  be  skipped.   Its
     *                       contents is used to determine fields count, and
     *                       that's all.
     *                     If no flags are specified, default value 0x0  (or
     *                     alglib::CSV_DEFAULT, which is same) should be used.
     *                     
     * OUTPUT PARAMETERS:
     *     out             2D matrix, CSV file parsed with atof()
     *     
     * HANDLING OF SPECIAL CASES:
     * - file does not exist - alglib::ap_error exception is thrown
     * - empty file - empty array is returned (no exception)
     * - skip_first_row=true, only one row in file - empty array is returned
     * - field contents is not recognized by atof() - field value is replaced
     *   by 0.0
     */
    public static void read_csv(string filename, char separator, int flags, out double[,] matrix)
    {
        //
        // Parameters
        //
        bool skip_first_row = (flags&CSV_SKIP_HEADERS)!=0;
        
        //
        // Prepare empty output array
        //
        matrix = new double[0,0];
        
        //
        // Read file, normalize file contents:
        // * replace 0x0 by spaces
        // * remove trailing spaces and newlines
        // * append trailing '\n' and '\0' characters
        // Return if file contains only spaces/newlines.
        //
        byte b_space = System.Convert.ToByte(' ');
        byte b_tab   = System.Convert.ToByte('\t');
        byte b_lf    = System.Convert.ToByte('\n');
        byte b_cr    = System.Convert.ToByte('\r');
        byte b_comma = System.Convert.ToByte(',');
        byte b_fullstop= System.Convert.ToByte('.');
        byte[] v0 = System.IO.File.ReadAllBytes(filename);
        if( v0.Length==0 )
            return;
        byte[] v1 = new byte[v0.Length+2];
        int filesize = v0.Length;
        for(int i=0; i<filesize; i++)
            v1[i] = v0[i]==0 ? b_space : v0[i];
        for(; filesize>0; )
        {
            byte c = v1[filesize-1];
            if( c==b_space || c==b_tab || c==b_cr || c==b_lf )
            {
                filesize--;
                continue;
            }
            break;
        }
        if( filesize==0 )
            return;
        v1[filesize+0] = b_lf;
        v1[filesize+1] = 0x0;
        filesize+=2;
        
        
        //
        // Scan dataset.
        //
        int rows_count, cols_count, max_length = 0;
        cols_count = 1;
        for(int idx=0; idx<filesize; idx++)
        {
            if( v1[idx]==separator )
                cols_count++;
            if( v1[idx]==b_lf )
                break;
        }
        rows_count = 0;
        for(int idx=0; idx<filesize; idx++)
            if( v1[idx]==b_lf )
                rows_count++;
        if( rows_count==1 && skip_first_row ) // empty output, return
            return;
        int[] offsets = new int[rows_count*cols_count];
        int[] lengths = new int[rows_count*cols_count];
        int cur_row_idx = 0;
        for(int row_start=0; v1[row_start]!=0x0; )
        {
            // determine row length
            int row_length;
            for(row_length=0; v1[row_start+row_length]!=b_lf; row_length++);
            
            // determine cols count, perform integrity check
            int cur_cols_cnt=1;
            for(int idx=0; idx<row_length; idx++)
                if( v1[row_start+idx]==separator )
                    cur_cols_cnt++;
            if( cols_count!=cur_cols_cnt )
                throw new alglib.alglibexception("read_csv: non-rectangular contents, rows have different sizes");
            
            // store offsets and lengths of the fields
            int cur_offs = 0;
            int cur_col_idx = 0;
            for(int idx=0; idx<row_length+1; idx++)
                if( v1[row_start+idx]==separator || v1[row_start+idx]==b_lf )
                {
                    offsets[cur_row_idx*cols_count+cur_col_idx] = row_start+cur_offs;
                    lengths[cur_row_idx*cols_count+cur_col_idx] = idx-cur_offs;
                    max_length = idx-cur_offs>max_length ? idx-cur_offs : max_length;
                    cur_offs = idx+1;
                    cur_col_idx++;
                }
            
            // advance row start
            cur_row_idx++;
            row_start = row_start+row_length+1;
        }
        
        //
        // Convert
        //
        int row0 = skip_first_row ? 1 : 0;
        int row1 = rows_count;
        System.Globalization.CultureInfo culture = System.Globalization.CultureInfo.CreateSpecificCulture(""); // invariant culture
        matrix = new double[row1-row0, cols_count];
        alglib.AE_CRITICAL_ASSERT(culture.NumberFormat.NumberDecimalSeparator==".");
        for(int ridx=row0; ridx<row1; ridx++)
            for(int cidx=0; cidx<cols_count; cidx++)
            {
                int field_len  = lengths[ridx*cols_count+cidx];
                int field_offs = offsets[ridx*cols_count+cidx];
                
                // replace , by full stop
                for(int idx=0; idx<field_len; idx++)
                    if( v1[field_offs+idx]==b_comma )
                        v1[field_offs+idx] = b_fullstop;
                
                // convert
                string s_val = System.Text.Encoding.ASCII.GetString(v1, field_offs, field_len);
                double d_val;
                Double.TryParse(s_val, System.Globalization.NumberStyles.Float, culture, out d_val);
                matrix[ridx-row0,cidx] = d_val;
            }
    }
    
    
    /********************************************************************
    serializer object (should not be used directly)
    ********************************************************************/
    public class serializer
    {
        enum SMODE { DEFAULT, ALLOC, TO_STRING, FROM_STRING, TO_STREAM, FROM_STREAM };
        private const int SER_ENTRIES_PER_ROW = 5;
        private const int SER_ENTRY_LENGTH    = 11;
        
        private SMODE mode;
        private int entries_needed;
        private int entries_saved;
        private int bytes_asked;
        private int bytes_written;
        private int bytes_read;
        private char[] out_str;
        private char[] in_str;
        private System.IO.Stream io_stream;
        
        // local temporaries
        private char[] entry_buf_char;
        private byte[] entry_buf_byte; 
        
        public serializer()
        {
            mode = SMODE.DEFAULT;
            entries_needed = 0;
            bytes_asked = 0;
            entry_buf_byte = new byte[SER_ENTRY_LENGTH+2];
            entry_buf_char = new char[SER_ENTRY_LENGTH+2];
        }

        public void clear_buffers()
        {
            out_str = null;
            in_str = null;
            io_stream = null;
        }

        public void alloc_start()
        {
            entries_needed = 0;
            bytes_asked = 0;
            mode = SMODE.ALLOC;
        }

        public void alloc_entry()
        {
            if( mode!=SMODE.ALLOC )
                throw new alglib.alglibexception("ALGLIB: internal error during (un)serialization");
            entries_needed++;
        }

        public void alloc_byte_array(byte[] a)
        {
            if( mode!=SMODE.ALLOC )
                throw new alglib.alglibexception("ALGLIB: internal error during (un)serialization");
            int n = ap.len(a);
            n = n/8 + (n%8>0 ? 1 : 0);
            entries_needed += 1+n;
        }

        private int get_alloc_size()
        {
            int rows, lastrowsize, result;
            
            // check and change mode
            if( mode!=SMODE.ALLOC )
                throw new alglib.alglibexception("ALGLIB: internal error during (un)serialization");
            
            // if no entries needes (degenerate case)
            if( entries_needed==0 )
            {
                bytes_asked = 4; /* a pair of chars for \r\n, one for space, one for dot */ 
                return bytes_asked;
            }
            
            // non-degenerate case
            rows = entries_needed/SER_ENTRIES_PER_ROW;
            lastrowsize = SER_ENTRIES_PER_ROW;
            if( entries_needed%SER_ENTRIES_PER_ROW!=0 )
            {
                lastrowsize = entries_needed%SER_ENTRIES_PER_ROW;
                rows++;
            }
            
            // calculate result size
            result  = ((rows-1)*SER_ENTRIES_PER_ROW+lastrowsize)*SER_ENTRY_LENGTH;  /* data size */
            result +=  (rows-1)*(SER_ENTRIES_PER_ROW-1)+(lastrowsize-1);            /* space symbols */
            result += rows*2;                                                       /* newline symbols */
            result += 1;                                                            /* trailing dot */
            bytes_asked = result;
            return result;
        }

        public void sstart_str()
        {
            int allocsize = get_alloc_size();
            
            // clear input/output buffers which may hold pointers to unneeded memory
            // NOTE: it also helps us to avoid errors when data are written to incorrect location
            clear_buffers();
            
            // check and change mode
            if( mode!=SMODE.ALLOC )
                throw new alglib.alglibexception("ALGLIB: internal error during (un)serialization");
            mode = SMODE.TO_STRING;
            
            // other preparations
            out_str = new char[allocsize];
            entries_saved = 0;
            bytes_written = 0;
        }

        public void sstart_stream(System.IO.Stream o_stream)
        {   
            // clear input/output buffers which may hold pointers to unneeded memory
            // NOTE: it also helps us to avoid errors when data are written to incorrect location
            clear_buffers();
            
            // check and change mode
            if( mode!=SMODE.ALLOC )
                throw new alglib.alglibexception("ALGLIB: internal error during (un)serialization");
            mode = SMODE.TO_STREAM;
            io_stream = o_stream;
        }

        public void ustart_str(string s)
        {
            // clear input/output buffers which may hold pointers to unneeded memory
            // NOTE: it also helps us to avoid errors when data are written to incorrect location
            clear_buffers();
            
            // check and change mode
            if( mode!=SMODE.DEFAULT )
                throw new alglib.alglibexception("ALGLIB: internal error during (un)serialization");
            mode = SMODE.FROM_STRING;
            
            in_str = s.ToCharArray();
            bytes_read = 0;
        }

        public void ustart_stream(System.IO.Stream i_stream)
        {
            // clear input/output buffers which may hold pointers to unneeded memory
            // NOTE: it also helps us to avoid errors when data are written to incorrect location
            clear_buffers();
            
            // check and change mode
            if( mode!=SMODE.DEFAULT )
                throw new alglib.alglibexception("ALGLIB: internal error during (un)serialization");
            mode = SMODE.FROM_STREAM;
            io_stream = i_stream;
        }

        private void serialize_value(bool v0, int v1, double v2, ulong v3, int val_idx)
        {
            // prepare serialization
            char[] arr_out = null;
            int cnt_out = 0;
            if( mode==SMODE.TO_STRING )
            {
                arr_out = out_str;
                cnt_out = bytes_written;
            }
            else if( mode==SMODE.TO_STREAM )
            {
                arr_out = entry_buf_char;
                cnt_out = 0;
            }
            else
                throw new alglib.alglibexception("ALGLIB: internal error during serialization");
            
            // serialize
            if( val_idx==0 )
                bool2str(  v0, arr_out, ref cnt_out);
            else if( val_idx==1 )
                int2str(   v1, arr_out, ref cnt_out);
            else if( val_idx==2 )
                double2str(v2, arr_out, ref cnt_out);
            else if( val_idx==3 )
                ulong2str( v3, arr_out, ref cnt_out);
            else
                throw new alglib.alglibexception("ALGLIB: internal error during serialization");
            entries_saved++;
            if( entries_saved%SER_ENTRIES_PER_ROW!=0 )
            {
                arr_out[cnt_out] = ' ';
                cnt_out++;
            }
            else
            {
                arr_out[cnt_out+0] = '\r';
                arr_out[cnt_out+1] = '\n';
                cnt_out+=2;
            }
            
            // post-process
            if( mode==SMODE.TO_STRING )
            {
                bytes_written = cnt_out;
                return;
            }
            else if( mode==SMODE.TO_STREAM )
            {
                for(int k=0; k<cnt_out; k++)
                    entry_buf_byte[k] = (byte)entry_buf_char[k];
                io_stream.Write(entry_buf_byte, 0, cnt_out);
                return;
            }
            else
                throw new alglib.alglibexception("ALGLIB: internal error during serialization");
        }

        private void unstream_entry_char()
        {
            if( mode!=SMODE.FROM_STREAM )
                throw new alglib.alglibexception("ALGLIB: internal error during unserialization");
            int c;
            for(;;)
            {
                c = io_stream.ReadByte();
                if( c<0 )
                    throw new alglib.alglibexception("ALGLIB: internal error during unserialization");
                if( c!=' ' && c!='\t' && c!='\n' && c!='\r' )
                    break;
            }
            entry_buf_char[0] = (char)c;
            for(int k=1; k<SER_ENTRY_LENGTH; k++)
            {
                c = io_stream.ReadByte();
                entry_buf_char[k] = (char)c;
                if( c<0 || c==' ' || c=='\t' || c=='\n' || c=='\r' )
                    throw new alglib.alglibexception("ALGLIB: internal error during unserialization");
            }
            entry_buf_char[SER_ENTRY_LENGTH] = (char)0;
        }

        public void serialize_bool(bool v)
        {
            serialize_value(v, 0, 0, 0, 0);
        }

        public void serialize_int(int v)
        {
            serialize_value(false, v, 0, 0, 1);
        }

        public void serialize_double(double v)
        {
            serialize_value(false, 0, v, 0, 2);
        }

        public void serialize_ulong(ulong v)
        {
            serialize_value(false, 0, 0, v, 3);
        }

        public void serialize_byte_array(byte[] v)
        {
            int chunk_size = 8;
            
            // save array length
            int n = ap.len(v);
            serialize_int(n);
            
            // determine entries count
            int entries_count = n/chunk_size + (n%chunk_size>0 ? 1 : 0);
            for(int eidx=0; eidx<entries_count; eidx++)
            {
                int elen = n-eidx*chunk_size;
                elen = elen>chunk_size ? chunk_size : elen;
                ulong tmp = 0x0;
                for(int i=0; i<elen; i++)
                    tmp = tmp | (((ulong)v[eidx*chunk_size+i])<<(8*i));
                serialize_ulong(tmp);
            }
        }

        public bool unserialize_bool()
        {
            if( mode==SMODE.FROM_STRING )
                return str2bool(in_str, ref bytes_read);
            if( mode==SMODE.FROM_STREAM )
            {
                unstream_entry_char();
                int dummy = 0;
                return str2bool(entry_buf_char, ref dummy);
            }
            throw new alglib.alglibexception("ALGLIB: internal error during (un)serialization");
        }

        public int unserialize_int()
        {
            if( mode==SMODE.FROM_STRING )
                return str2int(in_str, ref bytes_read);
            if( mode==SMODE.FROM_STREAM )
            {
                unstream_entry_char();
                int dummy = 0;
                return str2int(entry_buf_char, ref dummy);
            }
            throw new alglib.alglibexception("ALGLIB: internal error during (un)serialization");
        }

        public double unserialize_double()
        {
            if( mode==SMODE.FROM_STRING )
                return str2double(in_str, ref bytes_read);
            if( mode==SMODE.FROM_STREAM )
            {
                unstream_entry_char();
                int dummy = 0;
                return str2double(entry_buf_char, ref dummy);
            }
            throw new alglib.alglibexception("ALGLIB: internal error during (un)serialization");
        }

        public ulong unserialize_ulong()
        {
            if( mode==SMODE.FROM_STRING )
                return str2ulong(in_str, ref bytes_read);
            if( mode==SMODE.FROM_STREAM )
            {
                unstream_entry_char();
                int dummy = 0;
                return str2ulong(entry_buf_char, ref dummy);
            }
            throw new alglib.alglibexception("ALGLIB: internal error during (un)serialization");
        }

        public byte[] unserialize_byte_array()
        {
            int chunk_size = 8;
            
            // read array length, allocate output
            int n = unserialize_int();
            byte[] result = new byte[n];
            
            // determine entries count
            int entries_count = n/chunk_size + (n%chunk_size>0 ? 1 : 0);
            for(int eidx=0; eidx<entries_count; eidx++)
            {
                int elen = n-eidx*chunk_size;
                elen = elen>chunk_size ? chunk_size : elen;
                ulong tmp = unserialize_ulong();
                for(int i=0; i<elen; i++)
                    result[eidx*chunk_size+i] = unchecked((byte)(tmp>>(8*i)));
            }
            
            // done
            return result;
        }

        public void stop()
        {
            if( mode==SMODE.TO_STRING )
            {
                out_str[bytes_written] = '.';
                bytes_written++;
                return;
            }
            if( mode==SMODE.FROM_STRING )
            {
                //
                // because input string may be from pre-3.11 serializer,
                // which does not include trailing dot, we do not test
                // string for presence of "." symbol. Anyway, because string
                // is not stream, we do not have to read ALL trailing symbols.
                //
                return;
            }
            if( mode==SMODE.TO_STREAM )
            {
                io_stream.WriteByte((byte)'.');
                return;
            }
            if( mode==SMODE.FROM_STREAM )
            {
                for(;;)
                {
                    int c = io_stream.ReadByte();
                    if( c==' ' || c=='\t' || c=='\n' || c=='\r' )
                        continue;
                    if( c=='.' )
                        break;
                    throw new alglib.alglibexception("ALGLIB: internal error during unserialization");
                }
                return;
            }
            throw new alglib.alglibexception("ALGLIB: internal error during unserialization");
        }

        public string get_string()
        {
            if( mode!=SMODE.TO_STRING )
                throw new alglib.alglibexception("ALGLIB: internal error during (un)serialization");
             return new string(out_str, 0, bytes_written);
        }


        /************************************************************************
        This function converts six-bit value (from 0 to 63)  to  character  (only
        digits, lowercase and uppercase letters, minus and underscore are used).

        If v is negative or greater than 63, this function returns '?'.
        ************************************************************************/
        private static char[] _sixbits2char_tbl = new char[64]{ 
                '0', '1', '2', '3', '4', '5', '6', '7',
                '8', '9', 'A', 'B', 'C', 'D', 'E', 'F',
                'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N',
                'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V',
                'W', 'X', 'Y', 'Z', 'a', 'b', 'c', 'd', 
                'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 
                'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 
                'u', 'v', 'w', 'x', 'y', 'z', '-', '_' };
        private static char sixbits2char(int v)
        {
            if( v<0 || v>63 )
                return '?';
            return _sixbits2char_tbl[v];
        }
        
        /************************************************************************
        This function converts character to six-bit value (from 0 to 63).

        This function is inverse of ae_sixbits2char()
        If c is not correct character, this function returns -1.
        ************************************************************************/
        private static int[] _char2sixbits_tbl = new int[128] {
            -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, 62, -1, -1,
             0,  1,  2,  3,  4,  5,  6,  7,
             8,  9, -1, -1, -1, -1, -1, -1,
            -1, 10, 11, 12, 13, 14, 15, 16,
            17, 18, 19, 20, 21, 22, 23, 24,
            25, 26, 27, 28, 29, 30, 31, 32,
            33, 34, 35, -1, -1, -1, -1, 63,
            -1, 36, 37, 38, 39, 40, 41, 42,
            43, 44, 45, 46, 47, 48, 49, 50,
            51, 52, 53, 54, 55, 56, 57, 58,
            59, 60, 61, -1, -1, -1, -1, -1 };
        private static int char2sixbits(char c)
        {
            return (c>=0 && c<127) ? _char2sixbits_tbl[c] : -1;
        }
        
        /************************************************************************
        This function converts three bytes (24 bits) to four six-bit values 
        (24 bits again).

        src         array
        src_offs    offset of three-bytes chunk
        dst         array for ints
        dst_offs    offset of four-ints chunk
        ************************************************************************/
        private static void threebytes2foursixbits(byte[] src, int src_offs, int[] dst, int dst_offs)
        {
            dst[dst_offs+0] =  src[src_offs+0] & 0x3F;
            dst[dst_offs+1] = (src[src_offs+0]>>6) | ((src[src_offs+1]&0x0F)<<2);
            dst[dst_offs+2] = (src[src_offs+1]>>4) | ((src[src_offs+2]&0x03)<<4);
            dst[dst_offs+3] =  src[src_offs+2]>>2;
        }

        /************************************************************************
        This function converts four six-bit values (24 bits) to three bytes
        (24 bits again).

        src         pointer to four ints
        src_offs    offset of the chunk
        dst         pointer to three bytes
        dst_offs    offset of the chunk
        ************************************************************************/
        private static void foursixbits2threebytes(int[] src, int src_offs, byte[] dst, int dst_offs)
        {
            dst[dst_offs+0] =      (byte)(src[src_offs+0] | ((src[src_offs+1]&0x03)<<6));
            dst[dst_offs+1] = (byte)((src[src_offs+1]>>2) | ((src[src_offs+2]&0x0F)<<4));
            dst[dst_offs+2] = (byte)((src[src_offs+2]>>4) |  (src[src_offs+3]<<2));
        }

        /************************************************************************
        This function serializes boolean value into buffer

        v           boolean value to be serialized
        buf         buffer, at least 11 characters wide
        offs        offset in the buffer
        
        after return from this function, offs points to the char's past the value
        being read.
        ************************************************************************/
        private static void bool2str(bool v, char[] buf, ref int offs)
        {
            char c = v ? '1' : '0';
            int i;
            for(i=0; i<SER_ENTRY_LENGTH; i++)
                buf[offs+i] = c;
            offs += SER_ENTRY_LENGTH;
        }

        /************************************************************************
        This function unserializes boolean value from buffer

        buf         buffer which contains value; leading spaces/tabs/newlines are 
                    ignored, traling spaces/tabs/newlines are treated as  end  of
                    the boolean value.
        offs        offset in the buffer
        
        after return from this function, offs points to the char's past the value
        being read.

        This function raises an error in case unexpected symbol is found
        ************************************************************************/
        private static bool str2bool(char[] buf, ref int offs)
        {
            bool was0, was1;
            string emsg = "ALGLIB: unable to read boolean value from stream";
            
            was0 = false;
            was1 = false;
            while( buf[offs]==' ' || buf[offs]=='\t' || buf[offs]=='\n' || buf[offs]=='\r' )
                offs++;
            while( buf[offs]!=' ' && buf[offs]!='\t' && buf[offs]!='\n' && buf[offs]!='\r' && buf[offs]!=0 )
            {
                if( buf[offs]=='0' )
                {
                    was0 = true;
                    offs++;
                    continue;
                }
                if( buf[offs]=='1' )
                {
                    was1 = true;
                    offs++;
                    continue;
                }
                throw new alglib.alglibexception(emsg);
            }
            if( (!was0) && (!was1) )
                throw new alglib.alglibexception(emsg);
            if( was0 && was1 )
                throw new alglib.alglibexception(emsg);
            return was1 ? true : false;
        }

        /************************************************************************
        This function serializes integer value into buffer

        v           integer value to be serialized
        buf         buffer, at least 11 characters wide 
        offs        offset in the buffer
        
        after return from this function, offs points to the char's past the value
        being read.

        This function raises an error in case unexpected symbol is found
        ************************************************************************/
        private static void int2str(int v, char[] buf, ref int offs)
        {
            int i;
            byte[] _bytes = System.BitConverter.GetBytes((int)v);
            byte[]  bytes = new byte[9];
            int[] sixbits = new int[12];
            byte c;
            
            //
            // copy v to array of bytes, sign extending it and 
            // converting to little endian order. Additionally, 
            // we set 9th byte to zero in order to simplify 
            // conversion to six-bit representation
            //
            if( !System.BitConverter.IsLittleEndian )
                System.Array.Reverse(_bytes);
            c = v<0 ? (byte)0xFF : (byte)0x00;
            for(i=0; i<sizeof(int); i++)
                bytes[i] = _bytes[i];
            for(i=sizeof(int); i<8; i++)
                bytes[i] = c;
            bytes[8] = 0;
            
            //
            // convert to six-bit representation, output
            //
            // NOTE: last 12th element of sixbits is always zero, we do not output it
            //
            threebytes2foursixbits(bytes, 0, sixbits, 0);
            threebytes2foursixbits(bytes, 3, sixbits, 4);
            threebytes2foursixbits(bytes, 6, sixbits, 8);        
            for(i=0; i<SER_ENTRY_LENGTH; i++)
                buf[offs+i] = sixbits2char(sixbits[i]);
            offs += SER_ENTRY_LENGTH;
        }

        /************************************************************************
        This function unserializes integer value from string

        buf         buffer which contains value; leading spaces/tabs/newlines are 
                    ignored, traling spaces/tabs/newlines are treated as  end  of
                    the integer value.
        offs        offset in the buffer
        
        after return from this function, offs points to the char's past the value
        being read.

        This function raises an error in case unexpected symbol is found
        ************************************************************************/
        private static int str2int(char[] buf, ref int offs)
        {
            string emsg =       "ALGLIB: unable to read integer value from stream";
            string emsg3264 =   "ALGLIB: unable to read integer value from stream (value does not fit into 32 bits)";
            int[] sixbits = new int[12];
            byte[] bytes = new byte[9];
            byte[] _bytes = new byte[sizeof(int)];
            int sixbitsread, i;
            byte c;
            
            // 
            // 1. skip leading spaces
            // 2. read and decode six-bit digits
            // 3. set trailing digits to zeros
            // 4. convert to little endian 64-bit integer representation
            // 5. check that we fit into int
            // 6. convert to big endian representation, if needed
            //
            sixbitsread = 0;
            while( buf[offs]==' ' || buf[offs]=='\t' || buf[offs]=='\n' || buf[offs]=='\r' )
                offs++;
            while( buf[offs]!=' ' && buf[offs]!='\t' && buf[offs]!='\n' && buf[offs]!='\r' && buf[offs]!=0 )
            {
                int d;
                d = char2sixbits(buf[offs]);
                if( d<0 || sixbitsread>=SER_ENTRY_LENGTH )
                    throw new alglib.alglibexception(emsg);
                sixbits[sixbitsread] = d;
                sixbitsread++;
                offs++;
            }
            if( sixbitsread==0 )
                throw new alglib.alglibexception(emsg);
            for(i=sixbitsread; i<12; i++)
                sixbits[i] = 0;
            foursixbits2threebytes(sixbits, 0, bytes, 0);
            foursixbits2threebytes(sixbits, 4, bytes, 3);
            foursixbits2threebytes(sixbits, 8, bytes, 6);
            c = (bytes[sizeof(int)-1] & 0x80)!=0 ? (byte)0xFF : (byte)0x00;
            for(i=sizeof(int); i<8; i++)
                if( bytes[i]!=c )
                    throw new alglib.alglibexception(emsg3264);
            for(i=0; i<sizeof(int); i++)
                _bytes[i] = bytes[i];        
            if( !System.BitConverter.IsLittleEndian )
                System.Array.Reverse(_bytes);
            return System.BitConverter.ToInt32(_bytes,0);
        }
        
        
        /************************************************************************
        This function serializes double value into buffer

        v           double value to be serialized
        buf         buffer, at least 11 characters wide 
        offs        offset in the buffer
        
        after return from this function, offs points to the char's past the value
        being read.
        ************************************************************************/
        private static void double2str(double v, char[] buf, ref int offs)
        {
            int i;
            int[] sixbits = new int[12];
            byte[] bytes = new byte[9];

            //
            // handle special quantities
            //
            if( System.Double.IsNaN(v) )
            {
                buf[offs+0] = '.';
                buf[offs+1] = 'n';
                buf[offs+2] = 'a';
                buf[offs+3] = 'n';
                buf[offs+4] = '_';
                buf[offs+5] = '_';
                buf[offs+6] = '_';
                buf[offs+7] = '_';
                buf[offs+8] = '_';
                buf[offs+9] = '_';
                buf[offs+10] = '_';
                offs += SER_ENTRY_LENGTH;
                return;
            }
            if( System.Double.IsPositiveInfinity(v) )
            {
                buf[offs+0] = '.';
                buf[offs+1] = 'p';
                buf[offs+2] = 'o';
                buf[offs+3] = 's';
                buf[offs+4] = 'i';
                buf[offs+5] = 'n';
                buf[offs+6] = 'f';
                buf[offs+7] = '_';
                buf[offs+8] = '_';
                buf[offs+9] = '_';
                buf[offs+10] = '_';
                offs += SER_ENTRY_LENGTH;
                return;
            }
            if( System.Double.IsNegativeInfinity(v) )
            {
                buf[offs+0] = '.';
                buf[offs+1] = 'n';
                buf[offs+2] = 'e';
                buf[offs+3] = 'g';
                buf[offs+4] = 'i';
                buf[offs+5] = 'n';
                buf[offs+6] = 'f';
                buf[offs+7] = '_';
                buf[offs+8] = '_';
                buf[offs+9] = '_';
                buf[offs+10] = '_';
                offs += SER_ENTRY_LENGTH;
                return;
            }
            
            //
            // process general case:
            // 1. copy v to array of chars
            // 2. set 9th byte to zero in order to simplify conversion to six-bit representation
            // 3. convert to little endian (if needed)
            // 4. convert to six-bit representation
            //    (last 12th element of sixbits is always zero, we do not output it)
            //
            byte[] _bytes = System.BitConverter.GetBytes((double)v);
            if( !System.BitConverter.IsLittleEndian )
                System.Array.Reverse(_bytes);
            for(i=0; i<sizeof(double); i++)
                bytes[i] = _bytes[i];
            for(i=sizeof(double); i<9; i++)
                bytes[i] = 0;
            threebytes2foursixbits(bytes, 0, sixbits, 0);
            threebytes2foursixbits(bytes, 3, sixbits, 4);
            threebytes2foursixbits(bytes, 6, sixbits, 8);
            for(i=0; i<SER_ENTRY_LENGTH; i++)
                buf[offs+i] = sixbits2char(sixbits[i]);
            offs += SER_ENTRY_LENGTH;
        }
        
        
        /************************************************************************
        This function serializes ulong value into buffer

        v           ulong value to be serialized
        buf         buffer, at least 11 characters wide 
        offs        offset in the buffer
        
        after return from this function, offs points to the char's past the value
        being read.
        ************************************************************************/
        private static void ulong2str(ulong v, char[] buf, ref int offs)
        {
            int i;
            int[] sixbits = new int[12];
            byte[] bytes = new byte[9];
            
            //
            // process general case:
            // 1. copy v to array of chars
            // 2. set 9th byte to zero in order to simplify conversion to six-bit representation
            // 3. convert to little endian (if needed)
            // 4. convert to six-bit representation
            //    (last 12th element of sixbits is always zero, we do not output it)
            //
            byte[] _bytes = System.BitConverter.GetBytes((ulong)v);
            if( !System.BitConverter.IsLittleEndian )
                System.Array.Reverse(_bytes);
            for(i=0; i<sizeof(ulong); i++)
                bytes[i] = _bytes[i];
            for(i=sizeof(ulong); i<9; i++)
                bytes[i] = 0;
            threebytes2foursixbits(bytes, 0, sixbits, 0);
            threebytes2foursixbits(bytes, 3, sixbits, 4);
            threebytes2foursixbits(bytes, 6, sixbits, 8);
            for(i=0; i<SER_ENTRY_LENGTH; i++)
                buf[offs+i] = sixbits2char(sixbits[i]);
            offs += SER_ENTRY_LENGTH;
        }

        /************************************************************************
        This function unserializes double value from string

        buf         buffer which contains value; leading spaces/tabs/newlines are 
                    ignored, traling spaces/tabs/newlines are treated as  end  of
                    the double value.
        offs        offset in the buffer
        
        after return from this function, offs points to the char's past the value
        being read.

        This function raises an error in case unexpected symbol is found
        ************************************************************************/
        private static double str2double(char[] buf, ref int offs)
        {
            string emsg = "ALGLIB: unable to read double value from stream";
            int[] sixbits = new int[12];
            byte[]  bytes = new byte[9];
            byte[] _bytes = new byte[sizeof(double)];
            int sixbitsread, i;
            
            
            // 
            // skip leading spaces
            //
            while( buf[offs]==' ' || buf[offs]=='\t' || buf[offs]=='\n' || buf[offs]=='\r' )
                offs++;
            
              
            //
            // Handle special cases
            //
            if( buf[offs]=='.' )
            {
                string s = new string(buf, offs, SER_ENTRY_LENGTH);
                if( s==".nan_______" )
                {
                    offs += SER_ENTRY_LENGTH;
                    return System.Double.NaN;
                }
                if( s==".posinf____" )
                {
                    offs += SER_ENTRY_LENGTH;
                    return System.Double.PositiveInfinity;
                }
                if( s==".neginf____" )
                {
                    offs += SER_ENTRY_LENGTH;
                    return System.Double.NegativeInfinity;
                }
                throw new alglib.alglibexception(emsg);
            }
            
            // 
            // General case:
            // 1. read and decode six-bit digits
            // 2. check that all 11 digits were read
            // 3. set last 12th digit to zero (needed for simplicity of conversion)
            // 4. convert to 8 bytes
            // 5. convert to big endian representation, if needed
            //
            sixbitsread = 0;
            while( buf[offs]!=' ' && buf[offs]!='\t' && buf[offs]!='\n' && buf[offs]!='\r' && buf[offs]!=0 )
            {
                int d;
                d = char2sixbits(buf[offs]);
                if( d<0 || sixbitsread>=SER_ENTRY_LENGTH )
                    throw new alglib.alglibexception(emsg);
                sixbits[sixbitsread] = d;
                sixbitsread++;
                offs++;
            }
            if( sixbitsread!=SER_ENTRY_LENGTH )
                throw new alglib.alglibexception(emsg);
            sixbits[SER_ENTRY_LENGTH] = 0;
            foursixbits2threebytes(sixbits, 0, bytes, 0);
            foursixbits2threebytes(sixbits, 4, bytes, 3);
            foursixbits2threebytes(sixbits, 8, bytes, 6);
            for(i=0; i<sizeof(double); i++)
                _bytes[i] = bytes[i];        
            if( !System.BitConverter.IsLittleEndian )
                System.Array.Reverse(_bytes);        
            return System.BitConverter.ToDouble(_bytes,0);
        }

        /************************************************************************
        This function unserializes ulong value from string

        buf         buffer which contains value; leading spaces/tabs/newlines are 
                    ignored, traling spaces/tabs/newlines are treated as  end  of
                    the ulong value.
        offs        offset in the buffer
        
        after return from this function, offs points to the char's past the value
        being read.

        This function raises an error in case unexpected symbol is found
        ************************************************************************/
        private static ulong str2ulong(char[] buf, ref int offs)
        {
            string emsg = "ALGLIB: unable to read ulong value from stream";
            int[] sixbits = new int[12];
            byte[]  bytes = new byte[9];
            byte[] _bytes = new byte[sizeof(ulong)];
            int sixbitsread, i;
            
            
            // 
            // skip leading spaces
            //
            while( buf[offs]==' ' || buf[offs]=='\t' || buf[offs]=='\n' || buf[offs]=='\r' )
                offs++;
            
            // 
            // 1. read and decode six-bit digits
            // 2. check that all 11 digits were read
            // 3. set last 12th digit to zero (needed for simplicity of conversion)
            // 4. convert to 8 bytes
            // 5. convert to big endian representation, if needed
            //
            sixbitsread = 0;
            while( buf[offs]!=' ' && buf[offs]!='\t' && buf[offs]!='\n' && buf[offs]!='\r' && buf[offs]!=0 )
            {
                int d;
                d = char2sixbits(buf[offs]);
                if( d<0 || sixbitsread>=SER_ENTRY_LENGTH )
                    throw new alglib.alglibexception(emsg);
                sixbits[sixbitsread] = d;
                sixbitsread++;
                offs++;
            }
            if( sixbitsread!=SER_ENTRY_LENGTH )
                throw new alglib.alglibexception(emsg);
            sixbits[SER_ENTRY_LENGTH] = 0;
            foursixbits2threebytes(sixbits, 0, bytes, 0);
            foursixbits2threebytes(sixbits, 4, bytes, 3);
            foursixbits2threebytes(sixbits, 8, bytes, 6);
            for(i=0; i<sizeof(ulong); i++)
                _bytes[i] = bytes[i];        
            if( !System.BitConverter.IsLittleEndian )
                System.Array.Reverse(_bytes);        
            return System.BitConverter.ToUInt64(_bytes,0);
        }
    }
    
    /*
     * Parts of alglib.smp class which are shared with GPL version of ALGLIB
     */
    public partial class smp
    {
        #pragma warning disable 420
        public const int AE_LOCK_CYCLES = 512;
        public const int AE_LOCK_TESTS_BEFORE_YIELD = 16;
        
        /*
         * This variable is used to perform spin-wait loops in a platform-independent manner
         * (loops which should work same way on Mono and Microsoft NET). You SHOULD NEVER
         * change this field - it must be zero during all program life.
         */
        public static volatile int never_change_it = 0;
        
        /*************************************************************************
        Lock.

        This class provides lightweight spin lock
        *************************************************************************/
        public class ae_lock
        {
            public volatile int is_locked;
        }

        /********************************************************************
        Shared pool: data structure used to provide thread-safe access to pool
        of temporary variables.
        ********************************************************************/
        public class sharedpoolentry
        {
            public apobject obj;
            public sharedpoolentry next_entry;
        }
        public class shared_pool : apobject
        {
            /* lock object which protects pool */
            public ae_lock pool_lock;
    
            /* seed object (used to create new instances of temporaries) */
            public volatile apobject seed_object;
            
            /*
             * list of recycled OBJECTS:
             * 1. entries in this list store pointers to recycled objects
             * 2. every time we retrieve object, we retrieve first entry from this list,
             *    move it to recycled_entries and return its obj field to caller/
             */
            public volatile sharedpoolentry recycled_objects;
            
            /* 
             * list of recycled ENTRIES:
             * 1. this list holds entries which are not used to store recycled objects;
             *    every time recycled object is retrieved, its entry is moved to this list.
             * 2. every time object is recycled, we try to fetch entry for him from this list
             *    before allocating it with malloc()
             */
            public volatile sharedpoolentry recycled_entries;
            
            /* enumeration pointer, points to current recycled object*/
            public volatile sharedpoolentry enumeration_counter;
            
            /* constructor */
            public shared_pool()
            {
                ae_init_lock(ref pool_lock);
            }
            
            /* initializer - creation of empty pool */
            public override void init()
            {
                seed_object = null;
                recycled_objects = null;
                recycled_entries = null;
                enumeration_counter = null;
            }
            
            /* copy constructor (it is NOT thread-safe) */
            public override apobject make_copy()
            {
                sharedpoolentry ptr, buf;
                shared_pool result = new shared_pool();
                
                /* create lock */
                ae_init_lock(ref result.pool_lock);
    
                /* copy seed object */
                if( seed_object!=null )
                    result.seed_object = seed_object.make_copy();
                
                /*
                 * copy recycled objects:
                 * 1. copy to temporary list (objects are inserted to beginning, order is reversed)
                 * 2. copy temporary list to output list (order is restored back to normal)
                 */
                buf = null;
                for(ptr=recycled_objects; ptr!=null; ptr=ptr.next_entry)
                {
                    sharedpoolentry tmp = new sharedpoolentry();
                    tmp.obj =  ptr.obj.make_copy();
                    tmp.next_entry = buf;
                    buf = tmp;
                }
                result.recycled_objects = null;
                for(ptr=buf; ptr!=null;)
                {
                    sharedpoolentry next_ptr = ptr.next_entry;
                    ptr.next_entry = result.recycled_objects;
                    result.recycled_objects = ptr;
                    ptr = next_ptr;
                }
    
                /* recycled entries are not copied because they do not store any information */
                result.recycled_entries = null;
    
                /* enumeration counter is reset on copying */
                result.enumeration_counter = null;
    
                return result;
            }
        }
        

        /************************************************************************
        This function performs given number of spin-wait iterations
        ************************************************************************/
        public static void ae_spin_wait(int cnt)
        {
            /*
             * these strange operations with ae_never_change_it are necessary to
             * prevent compiler optimization of the loop.
             */
            int i;
            
            /* very unlikely because no one will wait for such amount of cycles */
            if( cnt>0x12345678 )
                never_change_it = cnt%10;
            
            /* spin wait, test condition which will never be true */
            for(i=0; i<cnt; i++)
                if( never_change_it>0 )
                    never_change_it--;
        }


        /************************************************************************
        This function causes the calling thread to relinquish the CPU. The thread
        is moved to the end of the queue and some other thread gets to run.
        ************************************************************************/
        public static void ae_yield()
        {
            System.Threading.Thread.Sleep(0);
        }

        /************************************************************************
        This function initializes ae_lock structure and sets lock in a free mode.
        ************************************************************************/
        public static void ae_init_lock(ref ae_lock obj)
        {
            obj = new ae_lock();
            obj.is_locked = 0;
        }


        /************************************************************************
        This function acquires lock. In case lock is busy, we perform several
        iterations inside tight loop before trying again.
        ************************************************************************/
        public static void ae_acquire_lock(ae_lock obj)
        {
            int cnt = 0;
            for(;;)
            {
                if( System.Threading.Interlocked.CompareExchange(ref obj.is_locked, 1, 0)==0 )
                    return;
                ae_spin_wait(AE_LOCK_CYCLES);
                cnt++;
                if( cnt%AE_LOCK_TESTS_BEFORE_YIELD==0 )
                    ae_yield();
            }
        }


        /************************************************************************
        This function releases lock.
        ************************************************************************/
        public static void ae_release_lock(ae_lock obj)
        {
            System.Threading.Interlocked.Exchange(ref obj.is_locked, 0);
        }


        /************************************************************************
        This function frees ae_lock structure.
        ************************************************************************/
        public static void ae_free_lock(ref ae_lock obj)
        {
            obj = null;
        }
        
        
        /************************************************************************
        This function returns True, if internal seed object was set.  It  returns
        False for un-seeded pool.

        dst                 destination pool (initialized by constructor function)

        NOTE: this function is NOT thread-safe. It does not acquire pool lock, so
              you should NOT call it when lock can be used by another thread.
        ************************************************************************/
        public static bool ae_shared_pool_is_initialized(shared_pool dst)
        {
            return dst.seed_object!=null;
        }


        /************************************************************************
        This function sets internal seed object. All objects owned by the pool
        (current seed object, recycled objects) are automatically freed.

        dst                 destination pool (initialized by constructor function)
        seed_object         new seed object

        NOTE: this function is NOT thread-safe. It does not acquire pool lock, so
              you should NOT call it when lock can be used by another thread.
        ************************************************************************/
        public static void ae_shared_pool_set_seed(shared_pool dst, alglib.apobject seed_object)
        {
            dst.seed_object = seed_object.make_copy();
            dst.recycled_objects = null;
            dst.enumeration_counter = null;
        }


        /************************************************************************
        This  function  retrieves  a  copy  of  the seed object from the pool and
        stores it to target variable.

        pool                pool
        obj                 target variable
        
        NOTE: this function IS thread-safe.  It  acquires  pool  lock  during its
              operation and can be used simultaneously from several threads.
        ************************************************************************/
        public static void ae_shared_pool_retrieve<T>(shared_pool pool, ref T obj) where T : alglib.apobject
        {
            alglib.apobject new_obj;
            
            /* assert that pool was seeded */
            alglib.ap.assert(pool.seed_object!=null, "ALGLIB: shared pool is not seeded, PoolRetrieve() failed");
            
            /* acquire lock */
            ae_acquire_lock(pool.pool_lock);
            
            /* try to reuse recycled objects */
            if( pool.recycled_objects!=null )
            {
                /* retrieve entry/object from list of recycled objects */
                sharedpoolentry result = pool.recycled_objects;
                pool.recycled_objects = pool.recycled_objects.next_entry;
                new_obj = result.obj;
                result.obj = null;
                
                /* move entry to list of recycled entries */
                result.next_entry = pool.recycled_entries;
                pool.recycled_entries = result;
                
                /* release lock */
                ae_release_lock(pool.pool_lock);
                
                /* assign object to smart pointer */
                obj = (T)new_obj;
                
                return;
            }
                
            /*
             * release lock; we do not need it anymore because
             * copy constructor does not modify source variable.
             */
            ae_release_lock(pool.pool_lock);
            
            /* create new object from seed */
            new_obj = pool.seed_object.make_copy();
                
            /* assign object to pointer and return */
            obj = (T)new_obj;
        }


        /************************************************************************
        This  function  recycles object owned by the source variable by moving it
        to internal storage of the shared pool.

        Source  variable  must  own  the  object,  i.e.  be  the only place where
        reference  to  object  is  stored.  After  call  to  this function source
        variable becomes NULL.

        pool                pool
        obj                 source variable

        NOTE: this function IS thread-safe.  It  acquires  pool  lock  during its
              operation and can be used simultaneously from several threads.
        ************************************************************************/
        public static void ae_shared_pool_recycle<T>(shared_pool pool, ref T obj) where T : alglib.apobject
        {
            sharedpoolentry new_entry;
            
            /* assert that pool was seeded */
            alglib.ap.assert(pool.seed_object!=null, "ALGLIB: shared pool is not seeded, PoolRecycle() failed");
            
            /* assert that pointer non-null */
            alglib.ap.assert(obj!=null, "ALGLIB: obj in ae_shared_pool_recycle() is NULL");
            
            /* acquire lock */
            ae_acquire_lock(pool.pool_lock);
            
            /* acquire shared pool entry (reuse one from recycled_entries or malloc new one) */
            if( pool.recycled_entries!=null )
            {
                /* reuse previously allocated entry */
                new_entry = pool.recycled_entries;
                pool.recycled_entries = new_entry.next_entry;
            }
            else
            {
                /*
                 * Allocate memory for new entry.
                 *
                 * NOTE: we release pool lock during allocation because new() may raise
                 *       exception and we do not want our pool to be left in the locked state.
                 */
                ae_release_lock(pool.pool_lock);
                new_entry = new sharedpoolentry();
                ae_acquire_lock(pool.pool_lock);
            }
            
            /* add object to the list of recycled objects */
            new_entry.obj = obj;
            new_entry.next_entry = pool.recycled_objects;
            pool.recycled_objects = new_entry;
            
            /* release lock object */
            ae_release_lock(pool.pool_lock);
            
            /* release source pointer */
            obj = null;
        }


        /************************************************************************
        This function clears internal list of  recycled  objects,  but  does  not
        change seed object managed by the pool.

        pool                pool

        NOTE: this function is NOT thread-safe. It does not acquire pool lock, so
              you should NOT call it when lock can be used by another thread.
        ************************************************************************/
        public static void ae_shared_pool_clear_recycled(shared_pool pool)
        {
            pool.recycled_objects = null;
        }


        /************************************************************************
        This function allows to enumerate recycled elements of the  shared  pool.
        It stores reference to the first recycled object in the smart pointer.

        IMPORTANT:
        * in case target variable owns non-NULL value, it is rewritten
        * recycled object IS NOT removed from pool
        * target variable DOES NOT become owner of the new value; you can use
          reference to recycled object, but you do not own it.
        * this function IS NOT thread-safe
        * you SHOULD NOT modify shared pool during enumeration (although you  can
          modify state of the objects retrieved from pool)
        * in case there is no recycled objects in the pool, NULL is stored to obj
        * in case pool is not seeded, NULL is stored to obj

        pool                pool
        obj                 reference
        ************************************************************************/
        public static void ae_shared_pool_first_recycled<T>(shared_pool pool, ref T obj) where T : alglib.apobject
        {   
            /* modify internal enumeration counter */
            pool.enumeration_counter = pool.recycled_objects;
            
            /* exit on empty list */
            if( pool.enumeration_counter==null )
            {
                obj = null;
                return;
            }
            
            /* assign object to smart pointer */
            obj = (T)pool.enumeration_counter.obj;
        }


        /************************************************************************
        This function allows to enumerate recycled elements of the  shared  pool.
        It stores pointer to the next recycled object in the smart pointer.

        IMPORTANT:
        * in case target variable owns non-NULL value, it is rewritten
        * recycled object IS NOT removed from pool
        * target pointer DOES NOT become owner of the new value
        * this function IS NOT thread-safe
        * you SHOULD NOT modify shared pool during enumeration (although you  can
          modify state of the objects retrieved from pool)
        * in case there is no recycled objects left in the pool, NULL is stored.
        * in case pool is not seeded, NULL is stored.

        pool                pool
        obj                 target variable
        ************************************************************************/
        public static void ae_shared_pool_next_recycled<T>(shared_pool pool, ref T obj) where T : alglib.apobject
        {   
            /* exit on end of list */
            if( pool.enumeration_counter==null )
            {
                obj = null;
                return;
            }
            
            /* modify internal enumeration counter */
            pool.enumeration_counter = pool.enumeration_counter.next_entry;
            
            /* exit on empty list */
            if( pool.enumeration_counter==null )
            {
                obj = null;
                return;
            }
            
            /* assign object to smart pointer */
            obj = (T)pool.enumeration_counter.obj;
        }


        /************************************************************************
        This function clears internal list of recycled objects and  seed  object.
        However, pool still can be used (after initialization with another seed).

        pool                pool
        state               ALGLIB environment state

        NOTE: this function is NOT thread-safe. It does not acquire pool lock, so
              you should NOT call it when lock can be used by another thread.
        ************************************************************************/
        public static void ae_shared_pool_reset(shared_pool pool)
        {   
            pool.seed_object = null;
            pool.recycled_objects = null;
            pool.enumeration_counter = null;
        }
    }
}
#if ALGLIB_NO_FAST_KERNELS==false
#if ALGLIB_USE_SIMD && !_ALGLIB_ALREADY_DEFINED_SIMD_ALIASES
#define _ALGLIB_ALREADY_DEFINED_SIMD_ALIASES
using Sse2 = System.Runtime.Intrinsics.X86.Sse2;
using Avx2 = System.Runtime.Intrinsics.X86.Avx2;
using Fma  = System.Runtime.Intrinsics.X86.Fma;
using Intrinsics = System.Runtime.Intrinsics;
#endif
#pragma warning disable 164
#pragma warning disable 219
public partial class alglib
{
    #if ALGLIB_USE_SIMD
    private static int _ABLASF_KERNEL_SIZE1 =  8;
    private static int _ABLASF_KERNEL_SIZE2 =  8;
    private static int _ABLASF_KERNEL_SIZE3 =  8;
    #endif
    
    /*************************************************************************
    ABLASF kernels
    *************************************************************************/
    public partial class ablasf
    {
        #if ALGLIB_USE_SIMD
        /*************************************************************************
        SIMD kernel for rdot() and similar funcs

          -- ALGLIB --
             Copyright 20.07.2021 by Bochkanov Sergey
        *************************************************************************/
        private static unsafe bool try_rdot(
            int n,
            double *A,
            double *B,
            out double R)
        {
            R = 0;
            #if !ALGLIB_NO_SSE2
            #if !ALGLIB_NO_AVX2
            #if !ALGLIB_NO_FMA
            if( Fma.IsSupported )
            {
                int i;
                int n4 = n>>2;
                int head = n4<<2;
                Intrinsics.Vector256<double> avx_dot = Intrinsics.Vector256<double>.Zero;
                for(i=0; i<head; i+=4)
                {
                    avx_dot = Fma.MultiplyAdd(
                                Avx2.LoadVector256(A+i),
                                Avx2.LoadVector256(B+i),
                                avx_dot
                                );
                }
                double *vdot = stackalloc double[4];
                Avx2.Store(vdot, avx_dot);
                for(i=head; i<n; i++)
                    vdot[0] += A[i]*B[i];
                R = vdot[0]+vdot[1]+vdot[2]+vdot[3];
                return true;
            }
            #endif // no-fma
            if( Avx2.IsSupported )
            {
                int i;
                int n4 = n>>2;
                int head = n4<<2;
                Intrinsics.Vector256<double> avx_dot = Intrinsics.Vector256<double>.Zero;
                for(i=0; i<head; i+=4)
                {
                    avx_dot = Avx2.Add(
                                Avx2.Multiply(
                                    Avx2.LoadVector256(A+i),
                                    Avx2.LoadVector256(B+i)
                                    ),
                                avx_dot
                                );
                }
                double *vdot = stackalloc double[4];
                Avx2.Store(vdot, avx_dot);
                for(i=head; i<n; i++)
                    vdot[0] += A[i]*B[i];
                R = vdot[0]+vdot[1]+vdot[2]+vdot[3];
                return true;
            }
            #endif // no-avx2
            #endif // no-sse2
            return false;
        }
        #endif
        #if ALGLIB_USE_SIMD
        /*************************************************************************
        SIMD kernel for rdotv2()

          -- ALGLIB --
             Copyright 20.07.2021 by Bochkanov Sergey
        *************************************************************************/
        private static unsafe bool try_rdotv2(
            int n,
            double *A,
            out double R)
        {
            R = 0;
            #if !ALGLIB_NO_SSE2
            #if !ALGLIB_NO_AVX2
            #if !ALGLIB_NO_FMA
            if( Fma.IsSupported )
            {
                int i;
                int n4 = n>>2;
                int head = n4<<2;
                Intrinsics.Vector256<double> avx_dot = Intrinsics.Vector256<double>.Zero;
                for(i=0; i<head; i+=4)
                {
                    Intrinsics.Vector256<double> Ai = Avx2.LoadVector256(A+i);
                    avx_dot = Fma.MultiplyAdd(Ai, Ai, avx_dot);
                }
                double *vdot = stackalloc double[4];
                Avx2.Store(vdot, avx_dot);
                for(i=head; i<n; i++)
                    vdot[0] += A[i]*A[i];
                R = vdot[0]+vdot[1]+vdot[2]+vdot[3];
                return true;
            }
            #endif // no-fma
            if( Avx2.IsSupported )
            {
                int i;
                int n4 = n>>2;
                int head = n4<<2;
                Intrinsics.Vector256<double> avx_dot = Intrinsics.Vector256<double>.Zero;
                for(i=0; i<head; i+=4)
                {
                    Intrinsics.Vector256<double> Ai = Avx2.LoadVector256(A+i);
                    avx_dot = Avx2.Add(Avx2.Multiply(Ai, Ai), avx_dot);
                }
                double *vdot = stackalloc double[4];
                Avx2.Store(vdot, avx_dot);
                for(i=head; i<n; i++)
                    vdot[0] += A[i]*A[i];
                R = vdot[0]+vdot[1]+vdot[2]+vdot[3];
                return true;
            }
            #endif // no-avx2
            #endif // no-sse2
            return false;
        }
        #endif
        
        /*************************************************************************
        Computes dot product (X,Y) for elements [0,N) of X[] and Y[]

        INPUT PARAMETERS:
            N       -   vector length
            X       -   array[N], vector to process
            Y       -   array[N], vector to process

        RESULT:
            (X,Y)

          -- ALGLIB --
             Copyright 20.01.2020 by Bochkanov Sergey
        *************************************************************************/
        public static double rdotv(int n,
            double[] x,
            double[] y,
            alglib.xparams _params)
        {
            double result = 0;
            int i = 0;
            
            #if ALGLIB_USE_SIMD
            if( n>=_ABLASF_KERNEL_SIZE1 )
                unsafe
                {
                    fixed(double* px=x, py=y)
                    {
                        double r;
                        if( try_rdot(n, py, px, out r) )
                            return r;
                    }
                }
            #endif

            result = 0;
            for(i=0; i<=n-1; i++)
            {
                result = result+x[i]*y[i];
            }
            return result;
        }

        /*************************************************************************
        Computes dot product (X,A[i]) for elements [0,N) of vector X[] and row A[i,*]

        INPUT PARAMETERS:
            N       -   vector length
            X       -   array[N], vector to process
            A       -   array[?,N], matrix to process
            I       -   row index

        RESULT:
            (X,Ai)

          -- ALGLIB --
             Copyright 20.01.2020 by Bochkanov Sergey
        *************************************************************************/
        public static double rdotvr(int n,
            double[] x,
            double[,] a,
            int i,
            alglib.xparams _params)
        {
            double result = 0;
            int j = 0;
            
            #if ALGLIB_USE_SIMD
            if( n>=_ABLASF_KERNEL_SIZE1 )
                unsafe
                {
                    fixed(double* px=x, pa=a)
                    {
                        double r;
                        if( try_rdot(n, px, pa+i*a.GetLength(1), out r) )
                            return r;
                    }
                }
            #endif

            result = 0;
            for(j=0; j<=n-1; j++)
            {
                result = result+x[j]*a[i,j];
            }
            return result;
        }

        /*************************************************************************
        Computes dot product (X,A[i]) for rows A[ia,*] and B[ib,*]

        INPUT PARAMETERS:
            N       -   vector length
            X       -   array[N], vector to process
            A       -   array[?,N], matrix to process
            I       -   row index

        RESULT:
            (X,Ai)

          -- ALGLIB --
             Copyright 20.01.2020 by Bochkanov Sergey
        *************************************************************************/
        public static double rdotrr(int n,
            double[,] a,
            int ia,
            double[,] b,
            int ib,
            alglib.xparams _params)
        {
            double result = 0;
            int j = 0;
            
            #if ALGLIB_USE_SIMD
            if( n>=_ABLASF_KERNEL_SIZE1 )
                unsafe
                {
                    fixed(double* pa=a, pb=b)
                    {
                        double r;
                        if( try_rdot(n, pa+ia*a.GetLength(1), pb+ib*b.GetLength(1), out r) )
                            return r;
                    }
                }
            #endif

            result = 0;
            for(j=0; j<=n-1; j++)
            {
                result = result+a[ia,j]*b[ib,j];
            }
            return result;
        }

        /*************************************************************************
        Computes dot product (X,X) for elements [0,N) of X[]

        INPUT PARAMETERS:
            N       -   vector length
            X       -   array[N], vector to process

        RESULT:
            (X,X)

          -- ALGLIB --
             Copyright 20.01.2020 by Bochkanov Sergey
        *************************************************************************/
        public static double rdotv2(int n,
            double[] x,
            alglib.xparams _params)
        {
            double result = 0;
            int i = 0;
            double v = 0;
            
            #if ALGLIB_USE_SIMD
            if( n>=_ABLASF_KERNEL_SIZE1 )
                unsafe
                {
                    fixed(double* px=x)
                    {
                        double r;
                        if( try_rdotv2(n, px, out r) )
                            return r;
                    }
                }
            #endif

            result = 0;
            for(i=0; i<=n-1; i++)
            {
                v = x[i];
                result = result+v*v;
            }
            return result;
        }

        #if ALGLIB_USE_SIMD
        /*************************************************************************
        SIMD kernel for raddv() and similar funcs

          -- ALGLIB --
             Copyright 20.07.2021 by Bochkanov Sergey
        *************************************************************************/
        private static unsafe bool try_raddv(
            int n,
            double vSrc,
            double *Src,
            double *Dst)
        {
            #if !ALGLIB_NO_SSE2
            #if !ALGLIB_NO_AVX2
            #if !ALGLIB_NO_FMA
            if( Fma.IsSupported )
            {
                int i;
                int n4 = n>>2;
                int head = n4<<2;
                Intrinsics.Vector256<double> avx_vsrc = Avx2.BroadcastScalarToVector256(&vSrc);
                for(i=0; i<head; i+=4)
                {
                    Avx2.Store(
                        Dst+i,
                        Fma.MultiplyAdd(
                            Avx2.LoadVector256(Src+i),
                            avx_vsrc,
                            Avx2.LoadVector256(Dst+i)
                            )
                        );
                }
                for(i=head; i<n; i++)
                    Dst[i] += vSrc*Src[i];
                return true;
            }
            #endif // no-fma
            if( Avx2.IsSupported )
            {
                int i;
                int n4 = n>>2;
                int head = n4<<2;
                Intrinsics.Vector256<double> avx_vsrc = Avx2.BroadcastScalarToVector256(&vSrc);
                for(i=0; i<head; i+=4)
                {
                    Avx2.Store(
                        Dst+i,
                        Avx2.Add(
                            Avx2.Multiply(
                                Avx2.LoadVector256(Src+i),
                                avx_vsrc
                                ),
                            Avx2.LoadVector256(Dst+i)
                            )
                        );
                }
                for(i=head; i<n; i++)
                    Dst[i] += vSrc*Src[i];
                return true;
            }
            #endif // no-avx2
            #endif // no-sse2
            return false;
        }
        #endif

        #if ALGLIB_USE_SIMD
        /*************************************************************************
        SIMD kernel for rmuladdv() and similar funcs

          -- ALGLIB --
             Copyright 20.07.2021 by Bochkanov Sergey
        *************************************************************************/
        private static unsafe bool try_rmuladdv(
            int n,
            double *Src0,
            double *Src1,
            double *Dst)
        {
            #if !ALGLIB_NO_SSE2
            #if !ALGLIB_NO_AVX2
            #if !ALGLIB_NO_FMA
            if( Fma.IsSupported )
            {
                int i;
                int n4 = n>>2;
                int head = n4<<2;
                for(i=0; i<head; i+=4)
                {
                    Avx2.Store(
                        Dst+i,
                        Fma.MultiplyAdd(
                            Avx2.LoadVector256(Src0+i),
                            Avx2.LoadVector256(Src1+i),
                            Avx2.LoadVector256(Dst+i)
                            )
                        );
                }
                for(i=head; i<n; i++)
                    Dst[i] += Src0[i]*Src1[i];
                return true;
            }
            #endif // no-fma
            #endif // no-avx2
            #endif // no-sse2
            return false;
        }
        #endif

        #if ALGLIB_USE_SIMD
        /*************************************************************************
        SIMD kernel for rmuladdv() and similar funcs

          -- ALGLIB --
             Copyright 20.07.2021 by Bochkanov Sergey
        *************************************************************************/
        private static unsafe bool try_rnegmuladdv(
            int n,
            double *Src0,
            double *Src1,
            double *Dst)
        {
            #if !ALGLIB_NO_SSE2
            #if !ALGLIB_NO_AVX2
            #if !ALGLIB_NO_FMA
            if( Fma.IsSupported )
            {
                int i;
                int n4 = n>>2;
                int head = n4<<2;
                for(i=0; i<head; i+=4)
                {
                    Avx2.Store(
                        Dst+i,
                        Fma.MultiplyAddNegated(
                            Avx2.LoadVector256(Src0+i),
                            Avx2.LoadVector256(Src1+i),
                            Avx2.LoadVector256(Dst+i)
                            )
                        );
                }
                for(i=head; i<n; i++)
                    Dst[i] -= Src0[i]*Src1[i];
                return true;
            }
            #endif // no-fma
            #endif // no-avx2
            #endif // no-sse2
            return false;
        }
        #endif

        #if ALGLIB_USE_SIMD
        /*************************************************************************
        SIMD kernel for rcopymuladdv() and similar funcs

          -- ALGLIB --
             Copyright 20.07.2021 by Bochkanov Sergey
        *************************************************************************/
        private static unsafe bool try_rcopymuladdv(
            int n,
            double *Src0,
            double *Src1,
            double *Src2,
            double *Dst)
        {
            #if !ALGLIB_NO_SSE2
            #if !ALGLIB_NO_AVX2
            #if !ALGLIB_NO_FMA
            if( Fma.IsSupported )
            {
                int i;
                int n4 = n>>2;
                int head = n4<<2;
                for(i=0; i<head; i+=4)
                {
                    Avx2.Store(
                        Dst+i,
                        Fma.MultiplyAdd(
                            Avx2.LoadVector256(Src0+i),
                            Avx2.LoadVector256(Src1+i),
                            Avx2.LoadVector256(Src2+i)
                            )
                        );
                }
                for(i=head; i<n; i++)
                    Dst[i] = Src2[i]+Src0[i]*Src1[i];
                return true;
            }
            #endif // no-fma
            #endif // no-avx2
            #endif // no-sse2
            return false;
        }
        #endif

        #if ALGLIB_USE_SIMD
        /*************************************************************************
        SIMD kernel for rcopymuladdv() and similar funcs

          -- ALGLIB --
             Copyright 20.07.2021 by Bochkanov Sergey
        *************************************************************************/
        private static unsafe bool try_rcopynegmuladdv(
            int n,
            double *Src0,
            double *Src1,
            double *Src2,
            double *Dst)
        {
            #if !ALGLIB_NO_SSE2
            #if !ALGLIB_NO_AVX2
            #if !ALGLIB_NO_FMA
            if( Fma.IsSupported )
            {
                int i;
                int n4 = n>>2;
                int head = n4<<2;
                for(i=0; i<head; i+=4)
                {
                    Avx2.Store(
                        Dst+i,
                        Fma.MultiplyAddNegated(
                            Avx2.LoadVector256(Src0+i),
                            Avx2.LoadVector256(Src1+i),
                            Avx2.LoadVector256(Src2+i)
                            )
                        );
                }
                for(i=head; i<n; i++)
                    Dst[i] = Src2[i]-Src0[i]*Src1[i];
                return true;
            }
            #endif // no-fma
            #endif // no-avx2
            #endif // no-sse2
            return false;
        }
        #endif
        
        #if ALGLIB_USE_SIMD
        /*************************************************************************
        SIMD kernel for rmul()
        
          -- ALGLIB --
             Copyright 20.07.2021 by Bochkanov Sergey
        *************************************************************************/
        private static unsafe bool try_rmulv(
            int n,
            double vDst,
            double *Dst)
        {   
            #if !ALGLIB_NO_SSE2
            #if !ALGLIB_NO_AVX2
            if( Avx2.IsSupported )
            {
                int i;
                int n4 = n>>2;
                int head = n4<<2;
                Intrinsics.Vector256<double> avx_vdst = Avx2.BroadcastScalarToVector256(&vDst);
                for(i=0; i<head; i+=4)
                {
                    Avx2.Store(
                        Dst+i,
                        Avx2.Multiply(
                            Avx2.LoadVector256(Dst+i),
                            avx_vdst
                            )
                        );
                }
                for(i=head; i<n; i++)
                    Dst[i] *= vDst;
                return true;
            }
            #endif // no-avx2
            #endif // no-sse2
            return false;
        }
        #endif
        
        #if ALGLIB_USE_SIMD
        /*************************************************************************
        SIMD kernel for rsqrt()
        
          -- ALGLIB --
             Copyright 20.07.2021 by Bochkanov Sergey
        *************************************************************************/
        private static unsafe bool try_rsqrtv(
            int n,
            double *Dst)
        {   
            #if !ALGLIB_NO_SSE2
            #if !ALGLIB_NO_AVX2
            if( Avx2.IsSupported )
            {
                int i;
                int n4 = n>>2;
                int head = n4<<2;
                for(i=0; i<head; i+=4)
                    Avx2.Store(Dst+i, Avx2.Sqrt(Avx2.LoadVector256(Dst+i)));
                for(i=head; i<n; i++)
                    Dst[i] = System.Math.Sqrt(Dst[i]);
                return true;
            }
            #endif // no-avx2
            #endif // no-sse2
            return false;
        }
        #endif

        #if ALGLIB_USE_SIMD
        /*************************************************************************
        SIMD kernel for rcopy()

          -- ALGLIB --
             Copyright 20.07.2021 by Bochkanov Sergey
        *************************************************************************/
        private static unsafe bool try_rcopy(
            int n,
            double *Src,
            double *Dst)
        {   
            #if !ALGLIB_NO_SSE2
            #if !ALGLIB_NO_AVX2
            if( Avx2.IsSupported )
            {
                int i;
                int n4 = n>>2;
                int head = n4<<2;
                for(i=0; i<head; i+=4)
                {
                    Avx2.Store(
                        Dst+i,
                        Avx2.LoadVector256(Src+i)
                        );
                }
                for(i=head; i<n; i++)
                    Dst[i] = Src[i];
                return true;
            }
            #endif // no-avx2
            #endif // no-sse2
            return false;
        }
        #endif

        #if ALGLIB_USE_SIMD
        /*************************************************************************
        SIMD kernel for icopy()

          -- ALGLIB --
             Copyright 20.07.2021 by Bochkanov Sergey
        *************************************************************************/
        private static unsafe bool try_icopy(
            int n,
            int *Src,
            int *Dst)
        {   
            #if !ALGLIB_NO_SSE2
            #if !ALGLIB_NO_AVX2
            if( Avx2.IsSupported )
            {
                int i;
                int n8 = n>>3;
                int head = n8<<3;
                for(i=0; i<head; i+=8)
                {
                    Avx2.Store(
                        Dst+i,
                        Avx2.LoadVector256(Src+i)
                        );
                }
                for(i=head; i<n; i++)
                    Dst[i] = Src[i];
                return true;
            }
            #endif // no-avx2
            #endif // no-sse2
            return false;
        }
        #endif

        #if ALGLIB_USE_SIMD
        /*************************************************************************
        SIMD kernel for rcopymul()

          -- ALGLIB --
             Copyright 20.07.2021 by Bochkanov Sergey
        *************************************************************************/
        private static unsafe bool try_rcopymul(
            int n,
            double vSrc,
            double *Src,
            double *Dst)
        {
            #if !ALGLIB_NO_SSE2
            #if !ALGLIB_NO_AVX2
            if( Avx2.IsSupported )
            {
                int i;
                int n4 = n>>2;
                int head = n4<<2;
                Intrinsics.Vector256<double> avx_vsrc = Avx2.BroadcastScalarToVector256(&vSrc);
                for(i=0; i<head; i+=4)
                {
                    Avx2.Store(
                        Dst+i,
                        Avx2.Multiply(
                            Avx2.LoadVector256(Src+i),
                            avx_vsrc)
                        );
                }
                for(i=head; i<n; i++)
                    Dst[i] = vSrc*Src[i];
                return true;
            }
            #endif // no-avx2
            #endif // no-sse2
            return false;
        }
        #endif

        #if ALGLIB_USE_SIMD
        /*************************************************************************
        SIMD kernel for rset()
        
          -- ALGLIB --
             Copyright 20.07.2021 by Bochkanov Sergey
        *************************************************************************/
        private static unsafe bool try_rset(
            int n,
            double vDst,
            double *Dst)
        {   
            #if !ALGLIB_NO_SSE2
            #if !ALGLIB_NO_AVX2
            if( Avx2.IsSupported )
            {
                int i;
                int n4 = n>>2;
                int head = n4<<2;
                Intrinsics.Vector256<double> avx_vdst = Avx2.BroadcastScalarToVector256(&vDst);
                for(i=0; i<head; i+=4)
                    Avx2.Store(Dst+i, avx_vdst);
                for(i=head; i<n; i++)
                    Dst[i] = vDst;
                return true;
            }
            #endif // no-avx2
            #endif // no-sse2
            return false;
        }
        #endif

        #if ALGLIB_USE_SIMD
        /*************************************************************************
        SIMD kernel for mergemul()
        
          -- ALGLIB --
             Copyright 20.07.2021 by Bochkanov Sergey
        *************************************************************************/
        private static unsafe bool try_rmergemul(
            int n,
            double *Src,
            double *Dst)
        {   
            #if !ALGLIB_NO_SSE2
            #if !ALGLIB_NO_AVX2
            if( Avx2.IsSupported )
            {
                int i;
                int n4 = n>>2;
                int head = n4<<2;
                for(i=0; i<head; i+=4)
                    Avx2.Store(
                        Dst+i,
                        Avx2.Multiply(
                            Avx2.LoadVector256(Dst+i),
                            Avx2.LoadVector256(Src+i)
                            )
                        );
                for(i=head; i<n; i++)
                    Dst[i] *= Src[i];
                return true;
            }
            #endif // no-avx2
            #endif // no-sse2
            return false;
        }
        #endif

        #if ALGLIB_USE_SIMD
        /*************************************************************************
        SIMD kernel for mergediv()
        
          -- ALGLIB --
             Copyright 20.07.2021 by Bochkanov Sergey
        *************************************************************************/
        private static unsafe bool try_rmergediv(
            int n,
            double *Src,
            double *Dst)
        {   
            #if !ALGLIB_NO_SSE2
            #if !ALGLIB_NO_AVX2
            if( Avx2.IsSupported )
            {
                int i;
                int n4 = n>>2;
                int head = n4<<2;
                for(i=0; i<head; i+=4)
                    Avx2.Store(
                        Dst+i,
                        Avx2.Divide(
                            Avx2.LoadVector256(Dst+i),
                            Avx2.LoadVector256(Src+i)
                            )
                        );
                for(i=head; i<n; i++)
                    Dst[i] /= Src[i];
                return true;
            }
            #endif // no-avx2
            #endif // no-sse2
            return false;
        }
        #endif

        #if ALGLIB_USE_SIMD
        /*************************************************************************
        SIMD kernel for mergemax()
        
          -- ALGLIB --
             Copyright 20.07.2021 by Bochkanov Sergey
        *************************************************************************/
        private static unsafe bool try_rmergemax(
            int n,
            double *Src,
            double *Dst)
        {   
            #if !ALGLIB_NO_SSE2
            #if !ALGLIB_NO_AVX2
            if( Avx2.IsSupported )
            {
                int i;
                int n4 = n>>2;
                int head = n4<<2;
                for(i=0; i<head; i+=4)
                    Avx2.Store(
                        Dst+i,
                        Avx2.Max(
                            Avx2.LoadVector256(Dst+i),
                            Avx2.LoadVector256(Src+i)
                            )
                        );
                for(i=head; i<n; i++)
                    Dst[i] = System.Math.Max(Dst[i],Src[i]);
                return true;
            }
            #endif // no-avx2
            #endif // no-sse2
            return false;
        }
        #endif

        #if ALGLIB_USE_SIMD
        /*************************************************************************
        SIMD kernel for mergemin()
        
          -- ALGLIB --
             Copyright 20.07.2021 by Bochkanov Sergey
        *************************************************************************/
        private static unsafe bool try_rmergemin(
            int n,
            double *Src,
            double *Dst)
        {   
            #if !ALGLIB_NO_SSE2
            #if !ALGLIB_NO_AVX2
            if( Avx2.IsSupported )
            {
                int i;
                int n4 = n>>2;
                int head = n4<<2;
                for(i=0; i<head; i+=4)
                    Avx2.Store(
                        Dst+i,
                        Avx2.Min(
                            Avx2.LoadVector256(Dst+i),
                            Avx2.LoadVector256(Src+i)
                            )
                        );
                for(i=head; i<n; i++)
                    Dst[i] = System.Math.Min(Dst[i],Src[i]);
                return true;
            }
            #endif // no-avx2
            #endif // no-sse2
            return false;
        }
        #endif

        /*************************************************************************
        Performs inplace addition of Y[] to X[]

        INPUT PARAMETERS:
            N       -   vector length
            Alpha   -   multiplier
            Y       -   array[N], vector to process
            X       -   array[N], vector to process

        RESULT:
            X := X + alpha*Y

          -- ALGLIB --
             Copyright 20.01.2020 by Bochkanov Sergey
        *************************************************************************/
        public static void raddv(int n,
            double alpha,
            double[] y,
            double[] x,
            alglib.xparams _params)
        {
            int i;
            
            #if ALGLIB_USE_SIMD
            if( n>=_ABLASF_KERNEL_SIZE1 )
                unsafe
                {
                    fixed(double* px=x, py=y)
                    {
                        if( try_raddv(n, alpha, py, px) )
                            return;
                    }
                }
            #endif

            for(i=0; i<=n-1; i++)
            {
                x[i] = x[i]+alpha*y[i];
            }
        }

        /*************************************************************************
        Performs inplace addition of Y[] to X[]

        INPUT PARAMETERS:
            N       -   vector length
            Alpha   -   multiplier
            Y       -   source vector
            OffsY   -   source offset
            X       -   destination vector
            OffsX   -   destination offset

        RESULT:
            X := X + alpha*Y

          -- ALGLIB --
             Copyright 20.01.2020 by Bochkanov Sergey
        *************************************************************************/
        public static void raddvx(int n,
            double alpha,
            double[] y,
            int offsy,
            double[] x,
            int offsx,
            alglib.xparams _params)
        {
            int i = 0;
            
            #if ALGLIB_USE_SIMD
            if( n>=_ABLASF_KERNEL_SIZE1 )
                unsafe
                {
                    fixed(double* px=x, py=y)
                    {
                        if( try_raddv(n, alpha, py+offsy, px+offsx) )
                            return;
                    }
                }
            #endif

            for(i=0; i<=n-1; i++)
            {
                x[offsx+i] = x[offsx+i]+alpha*y[offsy+i];
            }
        }


        /*************************************************************************
        Performs inplace addition of vector Y[] to row X[]

        INPUT PARAMETERS:
            N       -   vector length
            Alpha   -   multiplier
            Y       -   vector to add
            X       -   target row RowIdx

        RESULT:
            X := X + alpha*Y

          -- ALGLIB --
             Copyright 20.01.2020 by Bochkanov Sergey
        *************************************************************************/
        public static void raddvr(int n,
            double alpha,
            double[] y,
            double[,] x,
            int rowidx,
            alglib.xparams _params)
        {
            int i = 0;
            
            #if ALGLIB_USE_SIMD
            if( n>=_ABLASF_KERNEL_SIZE1 )
                unsafe
                {
                    fixed(double* px=x, py=y)
                    {
                        if( try_raddv(n, alpha, py, px+rowidx*x.GetLength(1)) )
                            return;
                    }
                }
            #endif

            for(i=0; i<=n-1; i++)
            {
                x[rowidx,i] = x[rowidx,i]+alpha*y[i];
            }
        }

        /*************************************************************************
        Performs inplace addition of Y[]*Z[] to X[]

        INPUT PARAMETERS:
            N       -   vector length
            Y       -   array[N], vector to process
            Z       -   array[N], vector to process
            X       -   array[N], vector to process

        RESULT:
            X := X + Y*Z

          -- ALGLIB --
             Copyright 29.10.2021 by Bochkanov Sergey
        *************************************************************************/
        public static void rmuladdv(int n,
            double[] y,
            double[] z,
            double[] x,
            alglib.xparams _params)
        {
            int i = 0;
            #if ALGLIB_USE_SIMD
            if( n>=_ABLASF_KERNEL_SIZE1 )
                unsafe
                {
                    fixed(double* px=x, py=y, pz=z)
                    {
                        if( try_rmuladdv(n, py, pz, px) )
                            return;
                    }
                }
            #endif
            for(i=0; i<=n-1; i++)
                x[i] += y[i]*z[i];
        }
        
        /*************************************************************************
        Performs inplace subtraction of Y[]*Z[] from X[]

        INPUT PARAMETERS:
            N       -   vector length
            Y       -   array[N], vector to process
            Z       -   array[N], vector to process
            X       -   array[N], vector to process

        RESULT:
            X := X - Y*Z

          -- ALGLIB --
             Copyright 29.10.2021 by Bochkanov Sergey
        *************************************************************************/
        public static void rnegmuladdv(int n,
            double[] y,
            double[] z,
            double[] x,
            alglib.xparams _params)
        {
            int i = 0;
            #if ALGLIB_USE_SIMD
            if( n>=_ABLASF_KERNEL_SIZE1 )
                unsafe
                {
                    fixed(double* px=x, py=y, pz=z)
                    {
                        if( try_rnegmuladdv(n, py, pz, px) )
                            return;
                    }
                }
            #endif
            for(i=0; i<=n-1; i++)
                x[i] -= y[i]*z[i];
        }
        
        /*************************************************************************
        Performs addition of Y[]*Z[] to X[]

        INPUT PARAMETERS:
            N       -   vector length
            Y       -   array[N], vector to process
            Z       -   array[N], vector to process
            X       -   array[N], vector to process
            R       -   array[N], vector to process

        RESULT:
            R := X + Y*Z

          -- ALGLIB --
             Copyright 29.10.2021 by Bochkanov Sergey
        *************************************************************************/
        public static void rcopymuladdv(int n,
            double[] y,
            double[] z,
            double[] x,
            double[] r,
            alglib.xparams _params)
        {
            int i = 0;
            #if ALGLIB_USE_SIMD
            if( n>=_ABLASF_KERNEL_SIZE1 )
                unsafe
                {
                    fixed(double* px=x, py=y, pz=z, pr=r)
                    {
                        if( try_rcopymuladdv(n, py, pz, px, pr) )
                            return;
                    }
                }
            #endif
            for(i=0; i<=n-1; i++)
                r[i] = x[i]+y[i]*z[i];
        }
        
        /*************************************************************************
        Performs subtraction of Y[]*Z[] from X[]

        INPUT PARAMETERS:
            N       -   vector length
            Y       -   array[N], vector to process
            Z       -   array[N], vector to process
            X       -   array[N], vector to process
            R       -   array[N], vector to process

        RESULT:
            R := X - Y*Z

          -- ALGLIB --
             Copyright 29.10.2021 by Bochkanov Sergey
        *************************************************************************/
        public static void rcopynegmuladdv(int n,
            double[] y,
            double[] z,
            double[] x,
            double[] r,
            alglib.xparams _params)
        {
            int i = 0;
            #if ALGLIB_USE_SIMD
            if( n>=_ABLASF_KERNEL_SIZE1 )
                unsafe
                {
                    fixed(double* px=x, py=y, pz=z, pr=r)
                    {
                        if( try_rcopynegmuladdv(n, py, pz, px, pr) )
                            return;
                    }
                }
            #endif
            for(i=0; i<=n-1; i++)
                r[i] = x[i]-y[i]*z[i];
        }
        
        /*************************************************************************
        Performs componentwise multiplication of vector X[] by vector Y[]

        INPUT PARAMETERS:
            N       -   vector length
            Y       -   vector to multiply by
            X       -   target vector

        RESULT:
            X := componentwise(X*Y)

          -- ALGLIB --
             Copyright 20.01.2020 by Bochkanov Sergey
        *************************************************************************/
        public static void rmergemulv(int n,
            double[] y,
            double[] x,
            alglib.xparams _params)
        {
            int i = 0;
            
            #if ALGLIB_USE_SIMD
            if( n>=_ABLASF_KERNEL_SIZE1 )
                unsafe
                {
                    fixed(double* px=x, py=y)
                    {
                        if( try_rmergemul(n, py, px) )
                            return;
                    }
                }
            #endif

            for(i=0; i<=n-1; i++)
            {
                x[i] = x[i]*y[i];
            }
        }

        /*************************************************************************
        Performs componentwise multiplication of row X[] by vector Y[]

        INPUT PARAMETERS:
            N       -   vector length
            Y       -   vector to multiply by
            X       -   target row RowIdx

        RESULT:
            X := componentwise(X*Y)

          -- ALGLIB --
             Copyright 20.01.2020 by Bochkanov Sergey
        *************************************************************************/
        public static void rmergemulvr(int n,
            double[] y,
            double[,] x,
            int rowidx,
            alglib.xparams _params)
        {
            int i = 0;
            
            #if ALGLIB_USE_SIMD
            if( n>=_ABLASF_KERNEL_SIZE1 )
                unsafe
                {
                    fixed(double* px=x, py=y)
                    {
                        if( try_rmergemul(n, py, px+rowidx*x.GetLength(1)) )
                            return;
                    }
                }
            #endif

            for(i=0; i<=n-1; i++)
            {
                x[rowidx,i] = x[rowidx,i]*y[i];
            }
        }

        /*************************************************************************
        Performs componentwise multiplication of row X[] by vector Y[]

        INPUT PARAMETERS:
            N       -   vector length
            Y       -   vector to multiply by
            X       -   target row RowIdx

        RESULT:
            X := componentwise(X*Y)

          -- ALGLIB --
             Copyright 20.01.2020 by Bochkanov Sergey
        *************************************************************************/
        public static void rmergemulrv(int n,
            double[,] y,
            int rowidx,
            double[] x,
            alglib.xparams _params)
        {
            int i = 0;
            
            #if ALGLIB_USE_SIMD
            if( n>=_ABLASF_KERNEL_SIZE1 )
                unsafe
                {
                    fixed(double* px=x, py=y)
                    {
                        if( try_rmergemul(n, py+rowidx*y.GetLength(1), px) )
                            return;
                    }
                }
            #endif

            for(i=0; i<=n-1; i++)
            {
                x[i] = x[i]*y[rowidx,i];
            }
        }
        
        
        
        /*************************************************************************
        Performs componentwise division of vector X[] by vector Y[]

        INPUT PARAMETERS:
            N       -   vector length
            Y       -   vector to divide by
            X       -   target vector

        RESULT:
            X := componentwise(X/Y)

          -- ALGLIB --
             Copyright 20.01.2020 by Bochkanov Sergey
        *************************************************************************/
        public static void rmergedivv(int n,
            double[] y,
            double[] x,
            alglib.xparams _params)
        {
            int i = 0;
            
            #if ALGLIB_USE_SIMD
            if( n>=_ABLASF_KERNEL_SIZE1 )
                unsafe
                {
                    fixed(double* px=x, py=y)
                    {
                        if( try_rmergediv(n, py, px) )
                            return;
                    }
                }
            #endif

            for(i=0; i<=n-1; i++)
            {
                x[i] = x[i]/y[i];
            }
        }

        /*************************************************************************
        Performs componentwise division of row X[] by vector Y[]

        INPUT PARAMETERS:
            N       -   vector length
            Y       -   vector to divide by
            X       -   target row RowIdx

        RESULT:
            X := componentwise(X/Y)

          -- ALGLIB --
             Copyright 20.01.2020 by Bochkanov Sergey
        *************************************************************************/
        public static void rmergedivvr(int n,
            double[] y,
            double[,] x,
            int rowidx,
            alglib.xparams _params)
        {
            int i = 0;
            
            #if ALGLIB_USE_SIMD
            if( n>=_ABLASF_KERNEL_SIZE1 )
                unsafe
                {
                    fixed(double* px=x, py=y)
                    {
                        if( try_rmergediv(n, py, px+rowidx*x.GetLength(1)) )
                            return;
                    }
                }
            #endif

            for(i=0; i<=n-1; i++)
            {
                x[rowidx,i] = x[rowidx,i]/y[i];
            }
        }

        /*************************************************************************
        Performs componentwise division of row X[] by vector Y[]

        INPUT PARAMETERS:
            N       -   vector length
            Y       -   vector to divide by
            X       -   target row RowIdx

        RESULT:
            X := componentwise(X/Y)

          -- ALGLIB --
             Copyright 20.01.2020 by Bochkanov Sergey
        *************************************************************************/
        public static void rmergedivrv(int n,
            double[,] y,
            int rowidx,
            double[] x,
            alglib.xparams _params)
        {
            int i = 0;
            
            #if ALGLIB_USE_SIMD
            if( n>=_ABLASF_KERNEL_SIZE1 )
                unsafe
                {
                    fixed(double* px=x, py=y)
                    {
                        if( try_rmergediv(n, py+rowidx*y.GetLength(1), px) )
                            return;
                    }
                }
            #endif

            for(i=0; i<=n-1; i++)
            {
                x[i] = x[i]/y[rowidx,i];
            }
        }

        /*************************************************************************
        Performs componentwise max of vector X[] and vector Y[]

        INPUT PARAMETERS:
            N       -   vector length
            Y       -   vector to multiply by
            X       -   target vector

        RESULT:
            X := componentwise_max(X,Y)

          -- ALGLIB --
             Copyright 20.01.2020 by Bochkanov Sergey
        *************************************************************************/
        public static void rmergemaxv(int n,
            double[] y,
            double[] x,
            alglib.xparams _params)
        {
            int i = 0;
            
            #if ALGLIB_USE_SIMD
            if( n>=_ABLASF_KERNEL_SIZE1 )
                unsafe
                {
                    fixed(double* px=x, py=y)
                    {
                        if( try_rmergemax(n, py, px) )
                            return;
                    }
                }
            #endif

            for(i=0; i<=n-1; i++)
            {
                x[i] = System.Math.Max(x[i], y[i]);
            }
        }

        /*************************************************************************
        Performs componentwise max of row X[] and vector Y[]

        INPUT PARAMETERS:
            N       -   vector length
            Y       -   vector to multiply by
            X       -   target row RowIdx

        RESULT:
            X := componentwise_max(X,Y)

          -- ALGLIB --
             Copyright 20.01.2020 by Bochkanov Sergey
        *************************************************************************/
        public static void rmergemaxvr(int n,
            double[] y,
            double[,] x,
            int rowidx,
            alglib.xparams _params)
        {
            int i = 0;
            
            #if ALGLIB_USE_SIMD
            if( n>=_ABLASF_KERNEL_SIZE1 )
                unsafe
                {
                    fixed(double* px=x, py=y)
                    {
                        if( try_rmergemax(n, py, px+rowidx*x.GetLength(1)) )
                            return;
                    }
                }
            #endif

            for(i=0; i<=n-1; i++)
            {
                x[rowidx,i] = System.Math.Max(x[rowidx,i], y[i]);
            }
        }

        /*************************************************************************
        Performs componentwise max of row X[I] and vector Y[] 

        INPUT PARAMETERS:
            N       -   vector length
            X       -   matrix, I-th row is source
            X       -   target row RowIdx

        RESULT:
            Y := componentwise_max(Y,X)

          -- ALGLIB --
             Copyright 20.01.2020 by Bochkanov Sergey
        *************************************************************************/
        public static void rmergemaxrv(int n,
            double[,] x,
            int rowidx,
            double[] y,
            alglib.xparams _params)
        {
            int i = 0;
            
            #if ALGLIB_USE_SIMD
            if( n>=_ABLASF_KERNEL_SIZE1 )
                unsafe
                {
                    fixed(double* px=x, py=y)
                    {
                        if( try_rmergemax(n, px+rowidx*x.GetLength(1), py) )
                            return;
                    }
                }
            #endif

            for(i=0; i<=n-1; i++)
            {
                y[i] = System.Math.Max(y[i], x[rowidx,i]);
            }
        }

        /*************************************************************************
        Performs componentwise max of vector X[] and vector Y[]

        INPUT PARAMETERS:
            N       -   vector length
            Y       -   vector to multiply by
            X       -   target vector

        RESULT:
            X := componentwise_max(X,Y)

          -- ALGLIB --
             Copyright 20.01.2020 by Bochkanov Sergey
        *************************************************************************/
        public static void rmergeminv(int n,
            double[] y,
            double[] x,
            alglib.xparams _params)
        {
            int i = 0;
            
            #if ALGLIB_USE_SIMD
            if( n>=_ABLASF_KERNEL_SIZE1 )
                unsafe
                {
                    fixed(double* px=x, py=y)
                    {
                        if( try_rmergemin(n, py, px) )
                            return;
                    }
                }
            #endif

            for(i=0; i<=n-1; i++)
            {
                x[i] = System.Math.Min(x[i], y[i]);
            }
        }

        /*************************************************************************
        Performs componentwise max of row X[] and vector Y[]

        INPUT PARAMETERS:
            N       -   vector length
            Y       -   vector to multiply by
            X       -   target row RowIdx

        RESULT:
            X := componentwise_max(X,Y)

          -- ALGLIB --
             Copyright 20.01.2020 by Bochkanov Sergey
        *************************************************************************/
        public static void rmergeminvr(int n,
            double[] y,
            double[,] x,
            int rowidx,
            alglib.xparams _params)
        {
            int i = 0;
            
            #if ALGLIB_USE_SIMD
            if( n>=_ABLASF_KERNEL_SIZE1 )
                unsafe
                {
                    fixed(double* px=x, py=y)
                    {
                        if( try_rmergemin(n, py, px+rowidx*x.GetLength(1)) )
                            return;
                    }
                }
            #endif

            for(i=0; i<=n-1; i++)
            {
                x[rowidx,i] = System.Math.Min(x[rowidx,i], y[i]);
            }
        }

        /*************************************************************************
        Performs componentwise max of row X[I] and vector Y[] 

        INPUT PARAMETERS:
            N       -   vector length
            X       -   matrix, I-th row is source
            X       -   target row RowIdx

        RESULT:
            X := componentwise_max(X,Y)

          -- ALGLIB --
             Copyright 20.01.2020 by Bochkanov Sergey
        *************************************************************************/
        public static void rmergeminrv(int n,
            double[,] x,
            int rowidx,
            double[] y,
            alglib.xparams _params)
        {
            int i = 0;
            
            #if ALGLIB_USE_SIMD
            if( n>=_ABLASF_KERNEL_SIZE1 )
                unsafe
                {
                    fixed(double* px=x, py=y)
                    {
                        if( try_rmergemin(n, px+rowidx*x.GetLength(1), py) )
                            return;
                    }
                }
            #endif

            for(i=0; i<=n-1; i++)
            {
                y[i] = System.Math.Min(y[i], x[rowidx,i]);
            }
        }

        /*************************************************************************
        Performs inplace addition of Y[RIdx,...] to X[]

        INPUT PARAMETERS:
            N       -   vector length
            Alpha   -   multiplier
            Y       -   array[?,N], matrix whose RIdx-th row is added
            RIdx    -   row index
            X       -   array[N], vector to process

        RESULT:
            X := X + alpha*Y

          -- ALGLIB --
             Copyright 20.01.2020 by Bochkanov Sergey
        *************************************************************************/
        public static void raddrv(int n,
            double alpha,
            double[,] y,
            int ridx,
            double[] x,
            alglib.xparams _params)
        {
            int i = 0;
            
            #if ALGLIB_USE_SIMD
            if( n>=_ABLASF_KERNEL_SIZE1 )
                unsafe
                {
                    fixed(double* px=x, py=y)
                    {
                        if( try_raddv(n, alpha, py+ridx*y.GetLength(1), px) )
                            return;
                    }
                }
            #endif

            for(i=0; i<=n-1; i++)
            {
                x[i] = x[i]+alpha*y[ridx,i];
            }
        }

        /*************************************************************************
        Performs inplace addition of Y[RIdx,...] to X[RIdxDst]

        INPUT PARAMETERS:
            N       -   vector length
            Alpha   -   multiplier
            Y       -   array[?,N], matrix whose RIdxSrc-th row is added
            RIdxSrc -   source row index
            X       -   array[?,N], matrix whose RIdxDst-th row is target
            RIdxDst -   destination row index

        RESULT:
            X := X + alpha*Y

          -- ALGLIB --
             Copyright 20.01.2020 by Bochkanov Sergey
        *************************************************************************/
        public static void raddrr(int n,
            double alpha,
            double[,] y,
            int ridxsrc,
            double[,] x,
            int ridxdst,
            alglib.xparams _params)
        {
            int i = 0;
            
            #if ALGLIB_USE_SIMD
            if( n>=_ABLASF_KERNEL_SIZE1 )
                unsafe
                {
                    fixed(double* px=x, py=y)
                    {
                        if( try_raddv(n, alpha, py+ridxsrc*y.GetLength(1), px+ridxdst*x.GetLength(1)) )
                            return;
                    }
                }
            #endif

            for(i=0; i<=n-1; i++)
            {
                x[ridxdst,i] = x[ridxdst,i]+alpha*y[ridxsrc,i];
            }
        }

        /*************************************************************************
        Performs inplace multiplication of X[] by V

        INPUT PARAMETERS:
            N       -   vector length
            X       -   array[N], vector to process
            V       -   multiplier

        OUTPUT PARAMETERS:
            X       -   elements 0...N-1 multiplied by V

          -- ALGLIB --
             Copyright 20.01.2020 by Bochkanov Sergey
        *************************************************************************/
        public static void rmulv(int n,
            double v,
            double[] x,
            alglib.xparams _params)
        {
            int i = 0;
            
            #if ALGLIB_USE_SIMD
            if( n>=_ABLASF_KERNEL_SIZE1 )
                unsafe
                {
                    fixed(double* px=x)
                    {
                        if( try_rmulv(n, v, px) )
                            return;
                    }
                }
            #endif

            for(i=0; i<=n-1; i++)
            {
                x[i] = x[i]*v;
            }
        }

        /*************************************************************************
        Performs inplace multiplication of X[] by V

        INPUT PARAMETERS:
            N       -   row length
            X       -   array[?,N], row to process
            V       -   multiplier

        OUTPUT PARAMETERS:
            X       -   elements 0...N-1 of row RowIdx are multiplied by V

          -- ALGLIB --
             Copyright 20.01.2020 by Bochkanov Sergey
        *************************************************************************/
        public static void rmulr(int n,
            double v,
            double[,] x,
            int rowidx,
            alglib.xparams _params)
        {
            int i = 0;
            
            #if ALGLIB_USE_SIMD
            if( n>=_ABLASF_KERNEL_SIZE1 )
                unsafe
                {
                    fixed(double* px=x)
                    {
                        if( try_rmulv(n, v, px+rowidx*x.GetLength(1)) )
                            return;
                    }
                }
            #endif


            for(i=0; i<=n-1; i++)
            {
                x[rowidx,i] = x[rowidx,i]*v;
            }
        }

        /*************************************************************************
        Performs inplace computation of Sqrt(X)

        INPUT PARAMETERS:
            N       -   vector length
            X       -   array[N], vector to process

        OUTPUT PARAMETERS:
            X       -   elements 0...N-1 replaced by Sqrt(X)

          -- ALGLIB --
             Copyright 20.01.2020 by Bochkanov Sergey
        *************************************************************************/
        public static void rsqrtv(int n,
            double[] x,
            alglib.xparams _params)
        {
            int i = 0;
            
            #if ALGLIB_USE_SIMD
            if( n>=_ABLASF_KERNEL_SIZE1 )
                unsafe
                {
                    fixed(double* px=x)
                    {
                        if( try_rsqrtv(n, px) )
                            return;
                    }
                }
            #endif

            for(i=0; i<=n-1; i++)
            {
                x[i] = System.Math.Sqrt(x[i]);
            }
        }

        /*************************************************************************
        Performs inplace computation of Sqrt(X[RowIdx,*])

        INPUT PARAMETERS:
            N       -   vector length
            X       -   array[?,N], matrix to process

        OUTPUT PARAMETERS:
            X       -   elements 0...N-1 replaced by Sqrt(X)

          -- ALGLIB --
             Copyright 20.01.2020 by Bochkanov Sergey
        *************************************************************************/
        public static void rsqrtr(int n,
            double[,] x,
            int rowidx,
            alglib.xparams _params)
        {
            int i = 0;
            
            #if ALGLIB_USE_SIMD
            if( n>=_ABLASF_KERNEL_SIZE1 )
                unsafe
                {
                    fixed(double* px=x)
                    {
                        if( try_rsqrtv(n, px+rowidx*x.GetLength(1)) )
                            return;
                    }
                }
            #endif


            for(i=0; i<=n-1; i++)
            {
                x[rowidx,i] = System.Math.Sqrt(x[rowidx,i]);
            }
        }

        /*************************************************************************
        Performs inplace multiplication of X[OffsX:OffsX+N-1] by V

        INPUT PARAMETERS:
            N       -   subvector length
            X       -   vector to process
            V       -   multiplier

        OUTPUT PARAMETERS:
            X       -   elements OffsX:OffsX+N-1 multiplied by V

          -- ALGLIB --
             Copyright 20.01.2020 by Bochkanov Sergey
        *************************************************************************/
        public static void rmulvx(int n,
            double v,
            double[] x,
            int offsx,
            alglib.xparams _params)
        {
            int i = 0;
            
            #if ALGLIB_USE_SIMD
            if( n>=_ABLASF_KERNEL_SIZE1 )
                unsafe
                {
                    fixed(double* px=x)
                    {
                        if( try_rmulv(n, v, px+offsx) )
                            return;
                    }
                }
            #endif

            for(i=0; i<=n-1; i++)
            {
                x[offsx+i] = x[offsx+i]*v;
            }
        }

        /*************************************************************************
        Returns maximum X

        INPUT PARAMETERS:
            N       -   vector length
            X       -   array[N], vector to process

        OUTPUT PARAMETERS:
            max(X[i])
            zero for N=0

          -- ALGLIB --
             Copyright 20.01.2020 by Bochkanov Sergey
        *************************************************************************/
        public static double rmaxv(int n,
            double[] x,
            alglib.xparams _params)
        {
            double result = 0;
            int i = 0;
            double v = 0;

            if( n<=0 )
            {
                result = 0;
                return result;
            }
            result = x[0];
            for(i=1; i<=n-1; i++)
            {
                v = x[i];
                if( v>result )
                {
                    result = v;
                }
            }
            return result;
        }

        /*************************************************************************
        Returns maximum |X|

        INPUT PARAMETERS:
            N       -   vector length
            X       -   array[N], vector to process

        OUTPUT PARAMETERS:
            max(|X[i]|)
            zero for N=0

          -- ALGLIB --
             Copyright 20.01.2020 by Bochkanov Sergey
        *************************************************************************/
        public static double rmaxabsv(int n,
            double[] x,
            alglib.xparams _params)
        {
            double result = 0;
            int i = 0;
            double v = 0;

            result = 0;
            for(i=0; i<=n-1; i++)
            {
                v = System.Math.Abs(x[i]);
                if( v>result )
                {
                    result = v;
                }
            }
            return result;
        }

        /*************************************************************************
        Returns maximum X

        INPUT PARAMETERS:
            N       -   vector length
            X       -   matrix to process, RowIdx-th row is processed

        OUTPUT PARAMETERS:
            max(X[RowIdx,i])
            zero for N=0

          -- ALGLIB --
             Copyright 20.01.2020 by Bochkanov Sergey
        *************************************************************************/
        public static double rmaxr(int n,
            double[,] x,
            int rowidx,
            alglib.xparams _params)
        {
            double result = 0;
            int i = 0;
            double v = 0;

            if( n<=0 )
            {
                result = 0;
                return result;
            }
            result = x[rowidx,0];
            for(i=1; i<=n-1; i++)
            {
                v = x[rowidx,i];
                if( v>result )
                {
                    result = v;
                }
            }
            return result;
        }

        /*************************************************************************
        Returns maximum |X|

        INPUT PARAMETERS:
            N       -   vector length
            X       -   matrix to process, RowIdx-th row is processed

        OUTPUT PARAMETERS:
            max(|X[RowIdx,i]|)
            zero for N=0

          -- ALGLIB --
             Copyright 20.01.2020 by Bochkanov Sergey
        *************************************************************************/
        public static double rmaxabsr(int n,
            double[,] x,
            int rowidx,
            alglib.xparams _params)
        {
            double result = 0;
            int i = 0;
            double v = 0;

            result = 0;
            for(i=0; i<=n-1; i++)
            {
                v = System.Math.Abs(x[rowidx,i]);
                if( v>result )
                {
                    result = v;
                }
            }
            return result;
        }

        /*************************************************************************
        Sets vector X[] to V

        INPUT PARAMETERS:
            N       -   vector length
            V       -   value to set
            X       -   array[N]

        OUTPUT PARAMETERS:
            X       -   leading N elements are replaced by V

          -- ALGLIB --
             Copyright 20.01.2020 by Bochkanov Sergey
        *************************************************************************/
        public static void rsetv(int n,
            double v,
            double[] x,
            alglib.xparams _params)
        {
            int j = 0;
            
            #if ALGLIB_USE_SIMD
            if( n>=_ABLASF_KERNEL_SIZE1 )
                unsafe
                {
                    fixed(double* px=x)
                    {
                        if( try_rset(n, v, px) )
                            return;
                    }
                }
            #endif

            for(j=0; j<=n-1; j++)
            {
                x[j] = v;
            }
        }

        /*************************************************************************
        Sets X[OffsX:OffsX+N-1] to V

        INPUT PARAMETERS:
            N       -   subvector length
            V       -   value to set
            X       -   array[N]

        OUTPUT PARAMETERS:
            X       -   X[OffsX:OffsX+N-1] is replaced by V

          -- ALGLIB --
             Copyright 20.01.2020 by Bochkanov Sergey
        *************************************************************************/
        public static void rsetvx(int n,
            double v,
            double[] x,
            int offsx,
            alglib.xparams _params)
        {
            int j = 0;
            
            #if ALGLIB_USE_SIMD
            if( n>=_ABLASF_KERNEL_SIZE1 )
                unsafe
                {
                    fixed(double* px=x)
                    {
                        if( try_rset(n, v, px+offsx) )
                            return;
                    }
                }
            #endif

            for(j=0; j<=n-1; j++)
            {
                x[offsx+j] = v;
            }
        }

        /*************************************************************************
        Sets vector X[] to V

        INPUT PARAMETERS:
            N       -   vector length
            V       -   value to set
            X       -   array[N]

        OUTPUT PARAMETERS:
            X       -   leading N elements are replaced by V

          -- ALGLIB --
             Copyright 20.01.2020 by Bochkanov Sergey
        *************************************************************************/
        public static void isetv(int n,
            int v,
            int[] x,
            alglib.xparams _params)
        {
            int j = 0;
            
            for(j=0; j<=n-1; j++)
            {
                x[j] = v;
            }
        }

        /*************************************************************************
        Sets vector X[] to V

        INPUT PARAMETERS:
            N       -   vector length
            V       -   value to set
            X       -   array[N]

        OUTPUT PARAMETERS:
            X       -   leading N elements are replaced by V

          -- ALGLIB --
             Copyright 20.01.2020 by Bochkanov Sergey
        *************************************************************************/
        public static void bsetv(int n,
            bool v,
            bool[] x,
            alglib.xparams _params)
        {
            int j = 0;

            for(j=0; j<=n-1; j++)
            {
                x[j] = v;
            }
        }

        /*************************************************************************
        Sets matrix A[] to V

        INPUT PARAMETERS:
            M, N    -   rows/cols count
            V       -   value to set
            A       -   array[M,N]

        OUTPUT PARAMETERS:
            A       -   leading M rows, N cols are replaced by V

          -- ALGLIB --
             Copyright 20.01.2020 by Bochkanov Sergey
        *************************************************************************/
        public static void rsetm(int m,
            int n,
            double v,
            double[,] a,
            alglib.xparams _params)
        {
            int i = 0;
            int j = 0;
            
            #if ALGLIB_USE_SIMD
            if( n>=_ABLASF_KERNEL_SIZE1 )
                unsafe
                {
                    fixed(double* pa=a)
                    {
                        for(i=0; i<m; i++)
                        {
                            double *prow = pa+i*a.GetLength(1);
                            if( !try_rset(n, v, prow) )
                            {
                                for(j=0; j<n; j++)
                                    prow[j] = v;
                            }
                        }
                    }
                    return;
                }
            #endif

            for(i=0; i<=m-1; i++)
            {
                for(j=0; j<=n-1; j++)
                {
                    a[i,j] = v;
                }
            }
        }

        /*************************************************************************
        Sets row I of A[,] to V

        INPUT PARAMETERS:
            N       -   vector length
            V       -   value to set
            A       -   array[N,N] or larger
            I       -   row index

        OUTPUT PARAMETERS:
            A       -   leading N elements of I-th row are replaced by V

          -- ALGLIB --
             Copyright 20.01.2020 by Bochkanov Sergey
        *************************************************************************/
        public static void rsetr(int n,
            double v,
            double[,] a,
            int i,
            alglib.xparams _params)
        {
            int j = 0;
            
            #if ALGLIB_USE_SIMD
            if( n>=_ABLASF_KERNEL_SIZE1 )
                unsafe
                {
                    fixed(double* pa=a)
                    {
                        if( try_rset(n, v, pa+i*a.GetLength(1)) )
                            return;
                    }
                }
            #endif


            for(j=0; j<=n-1; j++)
            {
                a[i,j] = v;
            }
        }

        /*************************************************************************
        Copies vector X[] to Y[]

        INPUT PARAMETERS:
            N       -   vector length
            X       -   array[N], source
            Y       -   preallocated array[N]

        OUTPUT PARAMETERS:
            Y       -   leading N elements are replaced by X

            
        NOTE: destination and source should NOT overlap

          -- ALGLIB --
             Copyright 20.01.2020 by Bochkanov Sergey
        *************************************************************************/
        public static void rcopyv(int n,
            double[] x,
            double[] y,
            alglib.xparams _params)
        {
            int j = 0;
            
            #if ALGLIB_USE_SIMD
            if( n>=_ABLASF_KERNEL_SIZE1 )
                unsafe
                {
                    fixed(double* px=x, py=y)
                    {
                        if( try_rcopy(n, px, py) )
                            return;
                    }
                }
            #endif

            for(j=0; j<=n-1; j++)
            {
                y[j] = x[j];
            }
        }
        
        /*************************************************************************
        Copies vector X[] to Y[]

        INPUT PARAMETERS:
            N       -   vector length
            X       -   array[N], source
            Y       -   preallocated array[N]

        OUTPUT PARAMETERS:
            Y       -   leading N elements are replaced by X

            
        NOTE: destination and source should NOT overlap

          -- ALGLIB --
             Copyright 20.01.2020 by Bochkanov Sergey
        *************************************************************************/
        public static void bcopyv(int n,
            bool[] x,
            bool[] y,
            alglib.xparams _params)
        {
            int j = 0;

            for(j=0; j<=n-1; j++)
            {
                y[j] = x[j];
            }
        }
        
        /*************************************************************************
        Copies vector X[] to Y[]

        INPUT PARAMETERS:
            N       -   vector length
            X       -   source array
            Y       -   preallocated array[N]

        OUTPUT PARAMETERS:
            Y       -   X copied to Y

          -- ALGLIB --
             Copyright 20.01.2020 by Bochkanov Sergey
        *************************************************************************/
        public static void icopyv(int n,
            int[] x,
            int[] y,
            alglib.xparams _params)
        {
            int j = 0;
            
            #if ALGLIB_USE_SIMD
            if( n>=_ABLASF_KERNEL_SIZE1 )
                unsafe
                {
                    fixed(int* px=x, py=y)
                    {
                        if( try_icopy(n, px, py) )
                            return;
                    }
                }
            #endif

            for(j=0; j<=n-1; j++)
            {
                y[j] = x[j];
            }
        }
 
        /*************************************************************************
        Performs copying with multiplication of V*X[] to Y[]

        INPUT PARAMETERS:
            N       -   vector length
            V       -   multiplier
            X       -   array[N], source
            Y       -   preallocated array[N]

        OUTPUT PARAMETERS:
            Y       -   array[N], Y = V*X

          -- ALGLIB --
             Copyright 20.01.2020 by Bochkanov Sergey
        *************************************************************************/
        public static void rcopymulv(int n,
            double v,
            double[] x,
            double[] y,
            alglib.xparams _params)
        {
            int i = 0;
            
            #if ALGLIB_USE_SIMD
            if( n>=_ABLASF_KERNEL_SIZE1 )
                unsafe
                {
                    fixed(double* px=x, py=y)
                    {
                        if( try_rcopymul(n, v, px, py) )
                            return;
                    }
                }
            #endif

            for(i=0; i<=n-1; i++)
            {
                y[i] = v*x[i];
            }
        }
        
        /*************************************************************************
        Performs copying with multiplication of V*X[] to Y[I,*]

        INPUT PARAMETERS:
            N       -   vector length
            V       -   multiplier
            X       -   array[N], source
            Y       -   preallocated array[?,N]
            RIdx    -   destination row index

        OUTPUT PARAMETERS:
            Y       -   Y[RIdx,...] = V*X

          -- ALGLIB --
             Copyright 20.01.2020 by Bochkanov Sergey
        *************************************************************************/
        public static void rcopymulvr(int n,
            double v,
            double[] x,
            double[,] y,
            int ridx,
            alglib.xparams _params)
        {
            int i = 0;
            
            #if ALGLIB_USE_SIMD
            if( n>=_ABLASF_KERNEL_SIZE1 )
                unsafe
                {
                    fixed(double* px=x, py=y)
                    {
                        if( try_rcopymul(n, v, px, py+ridx*y.GetLength(1)) )
                            return;
                    }
                }
            #endif

            for(i=0; i<=n-1; i++)
            {
                y[ridx,i] = v*x[i];
            }
        }
        
        /*************************************************************************
        Copies vector X[] to row I of A[,]

        INPUT PARAMETERS:
            N       -   vector length
            X       -   array[N], source
            A       -   preallocated 2D array large enough to store result
            I       -   destination row index

        OUTPUT PARAMETERS:
            A       -   leading N elements of I-th row are replaced by X

          -- ALGLIB --
             Copyright 20.01.2020 by Bochkanov Sergey
        *************************************************************************/
        public static void rcopyvr(int n,
            double[] x,
            double[,] a,
            int i,
            alglib.xparams _params)
        {
            int j = 0;
            
            #if ALGLIB_USE_SIMD
            if( n>=_ABLASF_KERNEL_SIZE1 )
                unsafe
                {
                    fixed(double* px=x, pa=a)
                    {
                        if( try_rcopy(n, px, pa+i*a.GetLength(1)) )
                            return;
                    }
                }
            #endif

            for(j=0; j<=n-1; j++)
            {
                a[i,j] = x[j];
            }
        }
        
        /*************************************************************************
        Copies row I of A[,] to vector X[]

        INPUT PARAMETERS:
            N       -   vector length
            A       -   2D array, source
            I       -   source row index
            X       -   preallocated destination

        OUTPUT PARAMETERS:
            X       -   array[N], destination

          -- ALGLIB --
             Copyright 20.01.2020 by Bochkanov Sergey
        *************************************************************************/
        public static void rcopyrv(int n,
            double[,] a,
            int i,
            double[] x,
            alglib.xparams _params)
        {
            int j = 0;
            
            #if ALGLIB_USE_SIMD
            if( n>=_ABLASF_KERNEL_SIZE1 )
                unsafe
                {
                    fixed(double* px=x, pa=a)
                    {
                        if( try_rcopy(n, pa+i*a.GetLength(1), px) )
                            return;
                    }
                }
            #endif

            for(j=0; j<=n-1; j++)
            {
                x[j] = a[i,j];
            }
        }
        
        /*************************************************************************
        Copies row I of A[,] to row K of B[,].

        A[i,...] and B[k,...] may overlap.

        INPUT PARAMETERS:
            N       -   vector length
            A       -   2D array, source
            I       -   source row index
            B       -   preallocated destination
            K       -   destination row index

        OUTPUT PARAMETERS:
            B       -   row K overwritten

          -- ALGLIB --
             Copyright 20.01.2020 by Bochkanov Sergey
        *************************************************************************/
        public static void rcopyrr(int n,
            double[,] a,
            int i,
            double[,] b,
            int k,
            alglib.xparams _params)
        {
            int j = 0;
            
            #if ALGLIB_USE_SIMD
            if( n>=_ABLASF_KERNEL_SIZE1 )
                unsafe
                {
                    fixed(double* pa=a, pb=b)
                    {
                        if( try_rcopy(n, pa+i*a.GetLength(1), pb+k*b.GetLength(1)) )
                            return;
                    }
                }
            #endif

            for(j=0; j<=n-1; j++)
            {
                b[k,j] = a[i,j];
            }
        }

        /*************************************************************************
        Copies vector X[] to Y[], extended version

        INPUT PARAMETERS:
            N       -   vector length
            X       -   source array
            OffsX   -   source offset
            Y       -   preallocated array[N]
            OffsY   -   destination offset

        OUTPUT PARAMETERS:
            Y       -   N elements starting from OffsY are replaced by X[OffsX:OffsX+N-1]
            
        NOTE: destination and source should NOT overlap

          -- ALGLIB --
             Copyright 20.01.2020 by Bochkanov Sergey
        *************************************************************************/
        public static void rcopyvx(int n,
            double[] x,
            int offsx,
            double[] y,
            int offsy,
            alglib.xparams _params)
        {
            int j = 0;
            
            #if ALGLIB_USE_SIMD
            if( n>=_ABLASF_KERNEL_SIZE1 )
                unsafe
                {
                    fixed(double* px=x, py=y)
                    {
                        if( try_rcopy(n, px+offsx, py+offsy) )
                            return;
                    }
                }
            #endif

            for(j=0; j<=n-1; j++)
            {
                y[offsy+j] = x[offsx+j];
            }
        }

        /*************************************************************************
        Copies vector X[] to Y[], extended version

        INPUT PARAMETERS:
            N       -   vector length
            X       -   source array
            OffsX   -   source offset
            Y       -   preallocated array[N]
            OffsY   -   destination offset

        OUTPUT PARAMETERS:
            Y       -   N elements starting from OffsY are replaced by X[OffsX:OffsX+N-1]
            
        NOTE: destination and source should NOT overlap

          -- ALGLIB --
             Copyright 20.01.2020 by Bochkanov Sergey
        *************************************************************************/
        public static void icopyvx(int n,
            int[] x,
            int offsx,
            int[] y,
            int offsy,
            alglib.xparams _params)
        {
            int j = 0;
            
            #if ALGLIB_USE_SIMD
            if( n>=_ABLASF_KERNEL_SIZE1 )
                unsafe
                {
                    fixed(int* px=x, py=y)
                    {
                        if( try_icopy(n, px+offsx, py+offsy) )
                            return;
                    }
                }
            #endif

            for(j=0; j<=n-1; j++)
            {
                y[offsy+j] = x[offsx+j];
            }
        }
        
        
        #if ALGLIB_USE_SIMD
        /*************************************************************************
        SIMD kernel for rgemv() and rgemvx()

          -- ALGLIB --
             Copyright 20.07.2021 by Bochkanov Sergey
        *************************************************************************/
        private static unsafe bool try_rgemv(
            int m,
            int n,
            double  alpha,
            double* a,
            int stride_a,
            int opa,
            double* x,
            double* y)
        {
            #if !ALGLIB_NO_SSE2
            #if !ALGLIB_NO_AVX2
            #if !ALGLIB_NO_FMA
            if( Fma.IsSupported )
            {
                if( opa==0 )
                {
                    
                    //
                    // y += A*x
                    //
                    int n4 = n>>2;
                    int head = n4<<2;
                    double *a_row = a;
                    int i, j;
                    double *vdot = stackalloc double[4];
                    for(i=0; i<m; i++)
                    {
                        Intrinsics.Vector256<double> simd_dot = Intrinsics.Vector256<double>.Zero;
                        for(j=0; j<head; j+=4)
                            simd_dot = Fma.MultiplyAdd(Fma.LoadVector256(a_row+j), Fma.LoadVector256(x+j), simd_dot);
                        Fma.Store(vdot, simd_dot);
                        for(j=head; j<n; j++)
                            vdot[0] += a_row[j]*x[j];
                        double v = vdot[0]+vdot[1]+vdot[2]+vdot[3];
                        y[i] = alpha*v+y[i];
                        a_row += stride_a;
                    }
                    return true;
                }
                if( opa==1 )
                {
                    
                    //
                    // y += A^T*x
                    //
                    double *a_row = a;
                    int i, j;
                    int m4 = m>>2;
                    int head = m4<<2;
                    for(i=0; i<n; i++)
                    {
                        double v = alpha*x[i];
                        Intrinsics.Vector256<double> simd_v = Fma.BroadcastScalarToVector256(&v);
                        for(j=0; j<head; j+=4)
                            Fma.Store(y+j, Fma.MultiplyAdd(Fma.LoadVector256(a_row+j), simd_v, Fma.LoadVector256(y+j)));
                        for(j=head; j<m; j++)
                            y[j] += v*a_row[j];
                        a_row += stride_a;
                    }
                    return true;
                }
                return false;
            }
            #endif // no-fma
            if( Avx2.IsSupported )
            {
                if( opa==0 )
                {
                    
                    //
                    // y += A*x
                    //
                    int n4 = n>>2;
                    int head = n4<<2;
                    double *a_row = a;
                    int i, j;
                    double *vdot = stackalloc double[4];
                    for(i=0; i<m; i++)
                    {
                        Intrinsics.Vector256<double> simd_dot = Intrinsics.Vector256<double>.Zero;
                        for(j=0; j<head; j+=4)
                            simd_dot = Avx2.Add(Avx2.Multiply(Avx2.LoadVector256(a_row+j), Avx2.LoadVector256(x+j)), simd_dot);
                        Avx2.Store(vdot, simd_dot);
                        for(j=head; j<n; j++)
                            vdot[0] += a_row[j]*x[j];
                        double v = vdot[0]+vdot[1]+vdot[2]+vdot[3];
                        y[i] = alpha*v+y[i];
                        a_row += stride_a;
                    }
                    return true;
                }
                if( opa==1 )
                {
                    
                    //
                    // y += A^T*x
                    //
                    double *a_row = a;
                    int i, j;
                    int m4 = m>>2;
                    int head = m4<<2;
                    for(i=0; i<n; i++)
                    {
                        double v = alpha*x[i];
                        Intrinsics.Vector256<double> simd_v = Avx2.BroadcastScalarToVector256(&v);
                        for(j=0; j<head; j+=4)
                            Avx2.Store(y+j, Avx2.Add(Avx2.Multiply(Avx2.LoadVector256(a_row+j), simd_v), Avx2.LoadVector256(y+j)));
                        for(j=head; j<m; j++)
                            y[j] += v*a_row[j];
                        a_row += stride_a;
                    }
                    return true;
                }
                return false;
            }
            #endif // no-avx2
            #endif // no-sse2
            return false;
        }
        #endif
        
        /*************************************************************************
        Matrix-vector product: y := alpha*op(A)*x + beta*y

        NOTE: this  function  expects  Y  to  be  large enough to store result. No
              automatic preallocation happens for  smaller  arrays.  No  integrity
              checks is performed for sizes of A, x, y.

        INPUT PARAMETERS:
            M   -   number of rows of op(A)
            N   -   number of columns of op(A)
            Alpha-  coefficient
            A   -   source matrix
            OpA -   operation type:
                    * OpA=0     =>  op(A) = A
                    * OpA=1     =>  op(A) = A^T
            X   -   input vector, has at least N elements
            Beta-   coefficient
            Y   -   preallocated output array, has at least M elements

        OUTPUT PARAMETERS:
            Y   -   vector which stores result

        HANDLING OF SPECIAL CASES:
            * if M=0, then subroutine does nothing. It does not even touch arrays.
            * if N=0 or Alpha=0.0, then:
              * if Beta=0, then Y is filled by zeros. A and X are  not  referenced
                at all. Initial values of Y are ignored (we do not  multiply  Y by
                zero, we just rewrite it by zeros)
              * if Beta<>0, then Y is replaced by Beta*Y
            * if M>0, N>0, Alpha<>0, but  Beta=0,  then  Y  is  replaced  by  A*x;
               initial state of Y is ignored (rewritten by  A*x,  without  initial
               multiplication by zeros).


          -- ALGLIB routine --

             01.09.2021
             Bochkanov Sergey
        *************************************************************************/
        public static void rgemv(int m,
            int n,
            double alpha,
            double[,] a,
            int opa,
            double[] x,
            double beta,
            double[] y,
            alglib.xparams _params)
        {
            int i = 0;
            int j = 0;
            double v = 0;

            
            //
            // Properly premultiply Y by Beta.
            //
            // Quick exit for M=0, N=0 or Alpha=0.
            // After this block we have M>0, N>0, Alpha<>0.
            //
            if( m<=0 )
            {
                return;
            }
            if( (double)(beta)!=(double)(0) )
            {
                rmulv(m, beta, y, _params);
            }
            else
            {
                rsetv(m, 0.0, y, _params);
            }
            if( n<=0 || (double)(alpha)==(double)(0.0) )
            {
                return;
            }
            
            //
            // Try fast kernel
            //
            #if ALGLIB_USE_SIMD
            if( (opa==0 && n>=_ABLASF_KERNEL_SIZE2) || (opa==1 && m>=_ABLASF_KERNEL_SIZE2) )
                unsafe
                {
                    fixed(double* pa=a, px=x, py=y)
                    {
                        if( try_rgemv(m, n, alpha, pa, a.GetLength(1), opa, px, py) )
                            return;
                    }
                }
            #endif
            
            //
            // Generic code
            //
            if( opa==0 )
            {
                
                //
                // y += A*x
                //
                for(i=0; i<=m-1; i++)
                {
                    v = 0;
                    for(j=0; j<=n-1; j++)
                    {
                        v = v+a[i,j]*x[j];
                    }
                    y[i] = alpha*v+y[i];
                }
                return;
            }
            if( opa==1 )
            {
                
                //
                // y += A^T*x
                //
                for(i=0; i<=n-1; i++)
                {
                    v = alpha*x[i];
                    for(j=0; j<=m-1; j++)
                    {
                        y[j] = y[j]+v*a[i,j];
                    }
                }
                return;
            }
        }
        
        /*************************************************************************
        Matrix-vector product: y := alpha*op(A)*x + beta*y

        Here x, y, A are subvectors/submatrices of larger vectors/matrices.

        NOTE: this  function  expects  Y  to  be  large enough to store result. No
              automatic preallocation happens for  smaller  arrays.  No  integrity
              checks is performed for sizes of A, x, y.

        INPUT PARAMETERS:
            M   -   number of rows of op(A)
            N   -   number of columns of op(A)
            Alpha-  coefficient
            A   -   source matrix
            IA  -   submatrix offset (row index)
            JA  -   submatrix offset (column index)
            OpA -   operation type:
                    * OpA=0     =>  op(A) = A
                    * OpA=1     =>  op(A) = A^T
            X   -   input vector, has at least N+IX elements
            IX  -   subvector offset
            Beta-   coefficient
            Y   -   preallocated output array, has at least M+IY elements
            IY  -   subvector offset

        OUTPUT PARAMETERS:
            Y   -   vector which stores result

        HANDLING OF SPECIAL CASES:
            * if M=0, then subroutine does nothing. It does not even touch arrays.
            * if N=0 or Alpha=0.0, then:
              * if Beta=0, then Y is filled by zeros. A and X are  not  referenced
                at all. Initial values of Y are ignored (we do not  multiply  Y by
                zero, we just rewrite it by zeros)
              * if Beta<>0, then Y is replaced by Beta*Y
            * if M>0, N>0, Alpha<>0, but  Beta=0,  then  Y  is  replaced  by  A*x;
               initial state of Y is ignored (rewritten by  A*x,  without  initial
               multiplication by zeros).


          -- ALGLIB routine --

             01.09.2021
             Bochkanov Sergey
        *************************************************************************/
        public static void rgemvx(int m,
            int n,
            double alpha,
            double[,] a,
            int ia,
            int ja,
            int opa,
            double[] x,
            int ix,
            double beta,
            double[] y,
            int iy,
            alglib.xparams _params)
        {
            int i = 0;
            int j = 0;
            double v = 0;

            
            //
            // Properly premultiply Y by Beta.
            //
            // Quick exit for M=0, N=0 or Alpha=0.
            // After this block we have M>0, N>0, Alpha<>0.
            //
            if( m<=0 )
            {
                return;
            }
            if( (double)(beta)!=(double)(0) )
            {
                rmulvx(m, beta, y, iy, _params);
            }
            else
            {
                rsetvx(m, 0.0, y, iy, _params);
            }
            if( n<=0 || (double)(alpha)==(double)(0.0) )
            {
                return;
            }
            
            //
            // Try fast kernel
            //
            #if ALGLIB_USE_SIMD
            if( (opa==0 && n>=_ABLASF_KERNEL_SIZE2) || (opa==1 && m>=_ABLASF_KERNEL_SIZE2) )
                unsafe
                {
                    fixed(double* pa=a, px=x, py=y)
                    {
                        if( try_rgemv(m, n, alpha, pa+ia*a.GetLength(1)+ja, a.GetLength(1), opa, px+ix, py+iy) )
                            return;
                    }
                }
            #endif
            
            //
            // Generic code
            //
            if( opa==0 )
            {
                
                //
                // y += A*x
                //
                for(i=0; i<=m-1; i++)
                {
                    v = 0;
                    for(j=0; j<=n-1; j++)
                    {
                        v = v+a[ia+i,ja+j]*x[ix+j];
                    }
                    y[iy+i] = alpha*v+y[iy+i];
                }
                return;
            }
            if( opa==1 )
            {
                
                //
                // y += A^T*x
                //
                for(i=0; i<=n-1; i++)
                {
                    v = alpha*x[ix+i];
                    for(j=0; j<=m-1; j++)
                    {
                        y[iy+j] = y[iy+j]+v*a[ia+i,ja+j];
                    }
                }
                return;
            }
        }
        
        
        /*************************************************************************
        Rank-1 correction: A := A + alpha*u*v'

        NOTE: this  function  expects  A  to  be  large enough to store result. No
              automatic preallocation happens for  smaller  arrays.  No  integrity
              checks is performed for sizes of A, u, v.

        INPUT PARAMETERS:
            M   -   number of rows
            N   -   number of columns
            A   -   target MxN matrix
            Alpha-  coefficient
            U   -   vector #1
            V   -   vector #2


          -- ALGLIB routine --
             07.09.2021
             Bochkanov Sergey
        *************************************************************************/
        public static void rger(int m,
            int n,
            double alpha,
            double[] u,
            double[] v,
            double[,] a,
            alglib.xparams _params)
        {
            int i = 0;
            int j = 0;
            double s = 0;

            if( (m<=0 || n<=0) || (double)(alpha)==(double)(0) )
            {
                return;
            }
            for(i=0; i<=m-1; i++)
            {
                s = alpha*u[i];
                for(j=0; j<=n-1; j++)
                {
                    a[i,j] = a[i,j]+s*v[j];
                }
            }
        }
        
        
        /*************************************************************************
        This subroutine solves linear system op(A)*x=b where:
        * A is NxN upper/lower triangular/unitriangular matrix
        * X and B are Nx1 vectors
        * "op" may be identity transformation or transposition

        Solution replaces X.

        IMPORTANT: * no overflow/underflow/denegeracy tests is performed.
                   * no integrity checks for operand sizes, out-of-bounds accesses
                     and so on is performed

        INPUT PARAMETERS
            N   -   matrix size, N>=0
            A       -   matrix, actial matrix is stored in A[IA:IA+N-1,JA:JA+N-1]
            IA      -   submatrix offset
            JA      -   submatrix offset
            IsUpper -   whether matrix is upper triangular
            IsUnit  -   whether matrix is unitriangular
            OpType  -   transformation type:
                        * 0 - no transformation
                        * 1 - transposition
            X       -   right part, actual vector is stored in X[IX:IX+N-1]
            IX      -   offset
            
        OUTPUT PARAMETERS
            X       -   solution replaces elements X[IX:IX+N-1]

          -- ALGLIB routine --
             (c) 07.09.2021 Bochkanov Sergey
        *************************************************************************/
        public static void rtrsvx(int n,
            double[,] a,
            int ia,
            int ja,
            bool isupper,
            bool isunit,
            int optype,
            double[] x,
            int ix,
            alglib.xparams _params)
        {
            int i = 0;
            int j = 0;
            double v = 0;

            if( n<=0 )
            {
                return;
            }
            if( optype==0 && isupper )
            {
                for(i=n-1; i>=0; i--)
                {
                    v = x[ix+i];
                    for(j=i+1; j<=n-1; j++)
                    {
                        v = v-a[ia+i,ja+j]*x[ix+j];
                    }
                    if( !isunit )
                    {
                        v = v/a[ia+i,ja+i];
                    }
                    x[ix+i] = v;
                }
                return;
            }
            if( optype==0 && !isupper )
            {
                for(i=0; i<=n-1; i++)
                {
                    v = x[ix+i];
                    for(j=0; j<=i-1; j++)
                    {
                        v = v-a[ia+i,ja+j]*x[ix+j];
                    }
                    if( !isunit )
                    {
                        v = v/a[ia+i,ja+i];
                    }
                    x[ix+i] = v;
                }
                return;
            }
            if( optype==1 && isupper )
            {
                for(i=0; i<=n-1; i++)
                {
                    v = x[ix+i];
                    if( !isunit )
                    {
                        v = v/a[ia+i,ja+i];
                    }
                    x[ix+i] = v;
                    if( v==0 )
                    {
                        continue;
                    }
                    for(j=i+1; j<=n-1; j++)
                    {
                        x[ix+j] = x[ix+j]-v*a[ia+i,ja+j];
                    }
                }
                return;
            }
            if( optype==1 && !isupper )
            {
                for(i=n-1; i>=0; i--)
                {
                    v = x[ix+i];
                    if( !isunit )
                    {
                        v = v/a[ia+i,ja+i];
                    }
                    x[ix+i] = v;
                    if( v==0 )
                    {
                        continue;
                    }
                    for(j=0; j<=i-1; j++)
                    {
                        x[ix+j] = x[ix+j]-v*a[ia+i,ja+j];
                    }
                }
                return;
            }
            alglib.ap.assert(false, "rTRSVX: unexpected operation type");
        }
        
        
        /*************************************************************************
        Fast kernel (new version with AVX2/FMA)

          -- ALGLIB routine --
             19.09.2021
             Bochkanov Sergey
        *************************************************************************/
        #if ALGLIB_USE_SIMD
        /*************************************************************************
        Block packing function for fast rGEMM. Loads long  WIDTH*LENGTH  submatrix
        with LENGTH<=BLOCK_SIZE and WIDTH<=MICRO_SIZE into contiguous  MICRO_SIZE*
        BLOCK_SIZE row-wise 'horizontal' storage (hence H in the function name).

        The matrix occupies first ROUND_LENGTH cols of the  storage  (with  LENGTH
        being rounded up to nearest SIMD granularity).  ROUND_LENGTH  is  returned
        as result. It is guaranteed that ROUND_LENGTH depends only on LENGTH,  and
        that it will be same for all function calls.

        Unused rows and columns in [LENGTH,ROUND_LENGTH) range are filled by zeros;
        unused cols in [ROUND_LENGTH,BLOCK_SIZE) range are ignored.

        * op=0 means that source is an WIDTH*LENGTH matrix stored with  src_stride
          stride. The matrix is NOT transposed on load.
        * op=1 means that source is an LENGTH*WIDTH matrix  stored with src_stride
          that is loaded with transposition
        * present version of the function supports only MICRO_SIZE=2, the behavior
          is undefined for other micro sizes.
        * the target is properly aligned; the source can be unaligned.

        Requires AVX2, does NOT check its presense.

        The function is present in two versions, one  with  variable  opsrc_length
        and another one with opsrc_length==block_size==32.

          -- ALGLIB routine --
             19.07.2021
             Bochkanov Sergey
        *************************************************************************/
        private static unsafe int ablasf_packblkh_avx2(
            double *src,
            int src_stride,
            int op,
            int opsrc_length,
            int opsrc_width,
            double   *dst,
            int block_size,
            int micro_size)
        {
            int i;
            
            /*
             * Write to the storage
             */
            if( op==0 )
            {
                /*
                 * Copy without transposition
                 */
                int len8=(opsrc_length>>3)<<3;
                double *src1 = src+src_stride;
                double *dst1 = dst+block_size;
                if( opsrc_width==2 )
                {
                    /*
                     * Width=2
                     */
                    for(i=0; i<len8; i+=8)
                    {
                        Avx2.StoreAligned(dst+i,    Avx2.LoadVector256(src+i));
                        Avx2.StoreAligned(dst+i+4,  Avx2.LoadVector256(src+i+4));
                        Avx2.StoreAligned(dst1+i,   Avx2.LoadVector256(src1+i));
                        Avx2.StoreAligned(dst1+i+4, Avx2.LoadVector256(src1+i+4));
                    }
                    for(i=len8; i<opsrc_length; i++)
                    {
                        dst[i]  = src[i];
                        dst1[i] = src1[i];
                    }
                }
                else
                {
                    /*
                     * Width=1, pad by zeros
                     */
                    Intrinsics.Vector256<double> vz = Intrinsics.Vector256<double>.Zero;
                    for(i=0; i<len8; i+=8)
                    {
                        Avx2.StoreAligned(dst+i,    Avx2.LoadVector256(src+i));
                        Avx2.StoreAligned(dst+i+4,  Avx2.LoadVector256(src+i+4));
                        Avx2.StoreAligned(dst1+i,   vz);
                        Avx2.StoreAligned(dst1+i+4, vz);
                    }
                    for(i=len8; i<opsrc_length; i++)
                    {
                        dst[i]  = src[i];
                        dst1[i] = 0.0;
                    }
                }
            }
            else
            {
                /*
                 * Copy with transposition
                 */
                int stride2 = src_stride<<1;
                int stride3 = src_stride+stride2;
                int stride4 = src_stride<<2;
                int len4=(opsrc_length>>2)<<2;
                double *srci = src;
                double *dst1 = dst+block_size;
                if( opsrc_width==2 )
                {
                    /*
                     * Width=2
                     */
                    for(i=0; i<len4; i+=4)
                    {
                        Intrinsics.Vector128<double> s0 = Sse2.LoadVector128(srci),         s1 = Sse2.LoadVector128(srci+src_stride);
                        Intrinsics.Vector128<double> s2 = Sse2.LoadVector128(srci+stride2), s3 = Sse2.LoadVector128(srci+stride3);
                        Sse2.Store(dst+i,    Sse2.UnpackLow( s0,s1));
                        Sse2.Store(dst1+i,   Sse2.UnpackHigh(s0,s1));
                        Sse2.Store(dst+i+2,  Sse2.UnpackLow( s2,s3));
                        Sse2.Store(dst1+i+2, Sse2.UnpackHigh(s2,s3));
                        srci += stride4;
                    }
                    for(i=len4; i<opsrc_length; i++)
                    {
                        dst[i]  = srci[0];
                        dst1[i] = srci[1];
                        srci += src_stride;
                    }
                }
                else
                {
                    /*
                     * Width=1, pad by zeros
                     */
                    Intrinsics.Vector128<double> vz = Intrinsics.Vector128<double>.Zero;
                    for(i=0; i<len4; i+=4)
                    {
                        Intrinsics.Vector128<double> s0 = Sse2.LoadVector128(srci), s1 = Sse2.LoadVector128(srci+src_stride);
                        Intrinsics.Vector128<double> s2 = Sse2.LoadVector128(srci+stride2), s3 = Sse2.LoadVector128(srci+stride3);
                        Sse2.Store(dst+i,    Sse2.UnpackLow(s0,s1));
                        Sse2.Store(dst+i+2,  Sse2.UnpackLow(s2,s3));
                        Sse2.Store(dst1+i,   vz);
                        Sse2.Store(dst1+i+2, vz);
                        srci += stride4;
                    }
                    for(i=len4; i<opsrc_length; i++)
                    {
                        dst[i]  = srci[0];
                        dst1[i] = 0.0;
                        srci += src_stride;
                    }
                }
            }
            
            /*
             * Pad by zeros, if needed
             */
            int round_length = ((opsrc_length+3)>>2)<<2;
            for(i=opsrc_length; i<round_length; i++)
            {
                dst[i] = 0;
                dst[i+block_size] = 0;
            }
            return round_length;
        }
        
        /*************************************************************************
        Computes  product   A*transpose(B)  of two MICRO_SIZE*ROUND_LENGTH rowwise 
        'horizontal' matrices, stored with stride=block_size, and writes it to the
        row-wise matrix C.

        ROUND_LENGTH is expected to be properly SIMD-rounded length,  as  returned
        by ablasf_packblkh_avx2().

        Present version of the function supports only MICRO_SIZE=2,  the  behavior
        is undefined for other micro sizes.

        Assumes that at least AVX2 is present; additionally checks for FMA and tries
        to use it.

          -- ALGLIB routine --
             19.07.2021
             Bochkanov Sergey
        *************************************************************************/
        private static unsafe void ablasf_dotblkh_avx2_fma(
            double *src_a,
            double *src_b,
            int round_length,
            int block_size,
            int micro_size,
            double *dst,
            int dst_stride)
        {
            int z;
            #if !ALGLIB_NO_SSE2
            #if !ALGLIB_NO_AVX2
            #if !ALGLIB_NO_FMA
            if( Fma.IsSupported )
            {
                /*
                 * Try FMA version
                 */
                Intrinsics.Vector256<double> r00 = Intrinsics.Vector256<double>.Zero,
                                             r01 = Intrinsics.Vector256<double>.Zero,
                                             r10 = Intrinsics.Vector256<double>.Zero,
                                             r11 = Intrinsics.Vector256<double>.Zero;
                if( (round_length&0x7)!=0 )
                {
                    /*
                     * round_length is multiple of 4, but not multiple of 8
                     */
                    for(z=0; z<round_length; z+=4, src_a+=4, src_b+=4)
                    {
                        Intrinsics.Vector256<double> a0 = Fma.LoadAlignedVector256(src_a);
                        Intrinsics.Vector256<double> a1 = Fma.LoadAlignedVector256(src_a+block_size);
                        Intrinsics.Vector256<double> b0 = Fma.LoadAlignedVector256(src_b);
                        Intrinsics.Vector256<double> b1 = Fma.LoadAlignedVector256(src_b+block_size);
                        r00 = Fma.MultiplyAdd(a0, b0, r00);
                        r01 = Fma.MultiplyAdd(a0, b1, r01);
                        r10 = Fma.MultiplyAdd(a1, b0, r10);
                        r11 = Fma.MultiplyAdd(a1, b1, r11);
                    }
                }
                else
                {
                    /*
                     * round_length is multiple of 8
                     */
                    for(z=0; z<round_length; z+=8, src_a+=8, src_b+=8)
                    {
                        Intrinsics.Vector256<double> a0 = Fma.LoadAlignedVector256(src_a);
                        Intrinsics.Vector256<double> a1 = Fma.LoadAlignedVector256(src_a+block_size);
                        Intrinsics.Vector256<double> b0 = Fma.LoadAlignedVector256(src_b);
                        Intrinsics.Vector256<double> b1 = Fma.LoadAlignedVector256(src_b+block_size);
                        Intrinsics.Vector256<double> c0 = Fma.LoadAlignedVector256(src_a+4);
                        Intrinsics.Vector256<double> c1 = Fma.LoadAlignedVector256(src_a+block_size+4);
                        Intrinsics.Vector256<double> d0 = Fma.LoadAlignedVector256(src_b+4);
                        Intrinsics.Vector256<double> d1 = Fma.LoadAlignedVector256(src_b+block_size+4);
                        r00 = Fma.MultiplyAdd(c0, d0, Fma.MultiplyAdd(a0, b0, r00));
                        r01 = Fma.MultiplyAdd(c0, d1, Fma.MultiplyAdd(a0, b1, r01));
                        r10 = Fma.MultiplyAdd(c1, d0, Fma.MultiplyAdd(a1, b0, r10));
                        r11 = Fma.MultiplyAdd(c1, d1, Fma.MultiplyAdd(a1, b1, r11));
                    }
                }
                Intrinsics.Vector256<double> sum0 = Fma.HorizontalAdd(r00,r01);
                Intrinsics.Vector256<double> sum1 = Fma.HorizontalAdd(r10,r11);
                Sse2.Store(dst,            Sse2.Add(Fma.ExtractVector128(sum0,0), Fma.ExtractVector128(sum0,1)));
                Sse2.Store(dst+dst_stride, Sse2.Add(Fma.ExtractVector128(sum1,0), Fma.ExtractVector128(sum1,1)));
            }
            else
            #endif // no-fma
            {
                /*
                 * Only AVX2 is present
                 */
                Intrinsics.Vector256<double> r00 = Intrinsics.Vector256<double>.Zero,
                                             r01 = Intrinsics.Vector256<double>.Zero,
                                             r10 = Intrinsics.Vector256<double>.Zero,
                                             r11 = Intrinsics.Vector256<double>.Zero;
                if( (round_length&0x7)!=0 )
                {
                    /*
                     * round_length is multiple of 4, but not multiple of 8
                     */
                    for(z=0; z<round_length; z+=4, src_a+=4, src_b+=4)
                    {
                        Intrinsics.Vector256<double> a0 = Avx2.LoadAlignedVector256(src_a);
                        Intrinsics.Vector256<double> a1 = Avx2.LoadAlignedVector256(src_a+block_size);
                        Intrinsics.Vector256<double> b0 = Avx2.LoadAlignedVector256(src_b);
                        Intrinsics.Vector256<double> b1 = Avx2.LoadAlignedVector256(src_b+block_size);
                        r00 = Avx2.Add(Avx2.Multiply(a0, b0), r00);
                        r01 = Avx2.Add(Avx2.Multiply(a0, b1), r01);
                        r10 = Avx2.Add(Avx2.Multiply(a1, b0), r10);
                        r11 = Avx2.Add(Avx2.Multiply(a1, b1), r11);
                    }
                }
                else
                {
                    /*
                     * round_length is multiple of 8
                     */
                    for(z=0; z<round_length; z+=8, src_a+=8, src_b+=8)
                    {
                        Intrinsics.Vector256<double> a0 = Avx2.LoadAlignedVector256(src_a);
                        Intrinsics.Vector256<double> a1 = Avx2.LoadAlignedVector256(src_a+block_size);
                        Intrinsics.Vector256<double> b0 = Avx2.LoadAlignedVector256(src_b);
                        Intrinsics.Vector256<double> b1 = Avx2.LoadAlignedVector256(src_b+block_size);
                        Intrinsics.Vector256<double> c0 = Avx2.LoadAlignedVector256(src_a+4);
                        Intrinsics.Vector256<double> c1 = Avx2.LoadAlignedVector256(src_a+block_size+4);
                        Intrinsics.Vector256<double> d0 = Avx2.LoadAlignedVector256(src_b+4);
                        Intrinsics.Vector256<double> d1 = Avx2.LoadAlignedVector256(src_b+block_size+4);
                        r00 = Avx2.Add(Avx2.Multiply(c0, d0), Avx2.Add(Avx2.Multiply(a0, b0), r00));
                        r01 = Avx2.Add(Avx2.Multiply(c0, d1), Avx2.Add(Avx2.Multiply(a0, b1), r01));
                        r10 = Avx2.Add(Avx2.Multiply(c1, d0), Avx2.Add(Avx2.Multiply(a1, b0), r10));
                        r11 = Avx2.Add(Avx2.Multiply(c1, d1), Avx2.Add(Avx2.Multiply(a1, b1), r11));
                    }
                }
                Intrinsics.Vector256<double> sum0 = Avx2.HorizontalAdd(r00,r01);
                Intrinsics.Vector256<double> sum1 = Avx2.HorizontalAdd(r10,r11);
                Sse2.Store(dst,            Sse2.Add(Avx2.ExtractVector128(sum0,0), Avx2.ExtractVector128(sum0,1)));
                Sse2.Store(dst+dst_stride, Sse2.Add(Avx2.ExtractVector128(sum1,0), Avx2.ExtractVector128(sum1,1)));
            }
            #endif // no-avx2
            #endif // no-sse2
        }
        
        /*************************************************************************
        Y := alpha*X + beta*Y

        Requires AVX2, does NOT check its presense.

          -- ALGLIB routine --
             19.07.2021
             Bochkanov Sergey
        *************************************************************************/
        private static unsafe void ablasf_daxpby_avx2(
            int    n,
            double alpha,
            double *src,
            double beta,
            double *dst)
        {
            if( beta==1.0 )
            {
                /*
                 * The most optimized case: DST := alpha*SRC + DST
                 *
                 * First, we process leading elements with generic C code until DST is aligned.
                 * Then, we process central part, assuming that DST is properly aligned.
                 * Finally, we process tail.
                 */
                int i, n4;
                Intrinsics.Vector256<double> avx_alpha = Intrinsics.Vector256.Create(alpha);
                while( n>0 && ((((ulong)dst)&31)!=0) )
                {
                    *dst += alpha*(*src);
                    n--;
                    dst++;
                    src++;
                }
                n4=(n>>2)<<2;
                for(i=0; i<n4; i+=4)
                    Avx2.StoreAligned(dst+i, Avx2.Add(Avx2.Multiply(avx_alpha, Avx2.LoadVector256(src+i)), Avx2.LoadAlignedVector256(dst+i)));
                for(i=n4; i<n; i++)
                    dst[i] = alpha*src[i]+dst[i];
            }
            else if( beta!=0.0 )
            {
                /*
                 * Well optimized: DST := alpha*SRC + beta*DST
                 */
                int i, n4;
                Intrinsics.Vector256<double> avx_alpha = Intrinsics.Vector256.Create(alpha);
                Intrinsics.Vector256<double> avx_beta  = Intrinsics.Vector256.Create(beta);
                while( n>0 && ((((ulong)dst)&31)!=0) )
                {
                    *dst = alpha*(*src) + beta*(*dst);
                    n--;
                    dst++;
                    src++;
                }
                n4=(n>>2)<<2;
                for(i=0; i<n4; i+=4)
                    Avx2.StoreAligned(dst+i, Avx2.Add(Avx2.Multiply(avx_alpha, Avx2.LoadVector256(src+i)), Avx2.Multiply(avx_beta,Avx2.LoadAlignedVector256(dst+i))));
                for(i=n4; i<n; i++)
                    dst[i] = alpha*src[i]+beta*dst[i];
            }
            else
            {
                /*
                 * Easy case: DST := alpha*SRC
                 */
                int i;
                for(i=0; i<n; i++)
                    dst[i] = alpha*src[i];
            }
        }
        #endif

        private static bool rgemm32basecase(int m,
            int n,
            int k,
            double alpha,
            double[,] _a,
            int ia,
            int ja,
            int optypea,
            double[,] _b,
            int ib,
            int jb,
            int optypeb,
            double beta,
            double[,] _c,
            int ic,
            int jc,
            alglib.xparams _params)
        {
            #if !ALGLIB_USE_SIMD
            return false;
            #else
            //
            // Quick exit
            //
            int block_size = 32;
            if( m<=_ABLASF_KERNEL_SIZE3 || n<=_ABLASF_KERNEL_SIZE3 || k<=_ABLASF_KERNEL_SIZE3 )
                return false;
            if( m>block_size || n>block_size || k>block_size || m==0 || n==0 || !Avx2.IsSupported )
                return false;
            
            //
            // Pin arrays and multiply using SIMD
            //
            int micro_size = 2;
            int alignment_doubles = 4;
            ulong alignment_bytes = (ulong)(alignment_doubles*sizeof(double));
            unsafe
            {
                fixed(double *c = &_c[ic,jc])
                {
                    int out0, out1;
                    int stride_c = _c.GetLength(1);
                    
                    /*
                     * Do we have alpha*A*B ?
                     */
                    if( alpha!=0 && k>0 )
                    {
                        fixed(double* a=&_a[ia,ja], b=&_b[ib,jb])
                        {
                            /*
                             * Prepare structures
                             */
                            int base0, base1, offs0;
                            int stride_a = _a.GetLength(1);
                            int stride_b = _b.GetLength(1);
                            double*      _blka = stackalloc double[block_size*micro_size+alignment_doubles];
                            double* _blkb_long = stackalloc double[block_size*block_size+alignment_doubles];
                            double*      _blkc = stackalloc double[micro_size*block_size+alignment_doubles];
                            double* blka          = (double*)(((((ulong)_blka)+alignment_bytes-1)/alignment_bytes)*alignment_bytes);
                            double* storageb_long = (double*)(((((ulong)_blkb_long)+alignment_bytes-1)/alignment_bytes)*alignment_bytes);
                            double* blkc          = (double*)(((((ulong)_blkc)+alignment_bytes-1)/alignment_bytes)*alignment_bytes);
                            
                            /*
                             * Pack transform(B) into precomputed block form
                             */
                            for(base1=0; base1<n; base1+=micro_size)
                            {
                                int lim1 = n-base1<micro_size ? n-base1 : micro_size;
                                double *curb = storageb_long+base1*block_size;
                                ablasf_packblkh_avx2(
                                    b + (optypeb==0 ? base1 : base1*stride_b), stride_b, optypeb==0 ? 1 : 0, k, lim1,
                                    curb, block_size, micro_size);
                            }
                            
                            /*
                             * Output
                             */
                            for(base0=0; base0<m; base0+=micro_size)
                            {
                                /*
                                 * Load block row of transform(A)
                                 */
                                int lim0    = m-base0<micro_size ? m-base0 : micro_size;
                                int round_k = ablasf_packblkh_avx2(
                                    a + (optypea==0 ? base0*stride_a : base0), stride_a, optypea, k, lim0,
                                    blka, block_size, micro_size);
                                    
                                /*
                                 * Compute block(A)'*entire(B)
                                 */
                                for(base1=0; base1<n; base1+=micro_size)
                                    ablasf_dotblkh_avx2_fma(blka, storageb_long+base1*block_size, round_k, block_size, micro_size, blkc+base1, block_size);

                                /*
                                 * Output block row of block(A)'*entire(B)
                                 */
                                for(offs0=0; offs0<lim0; offs0++)
                                    ablasf_daxpby_avx2(n, alpha, blkc+offs0*block_size, beta, c+(base0+offs0)*stride_c);
                            }
                        }
                    }
                    else
                    {
                        /*
                         * No A*B, just beta*C (degenerate case, not optimized)
                         */
                        if( beta==0 )
                        {
                            for(out0=0; out0<m; out0++)
                                for(out1=0; out1<n; out1++)
                                    c[out0*stride_c+out1] = 0.0;
                        }
                        else if( beta!=1 )
                        {
                            for(out0=0; out0<m; out0++)
                                for(out1=0; out1<n; out1++)
                                    c[out0*stride_c+out1] *= beta;
                        }
                    }
                }
            }
            return true;
            #endif
        }
    }
        
    /*************************************************************************
    Sparse Cholesky/LDLT kernels
    *************************************************************************/
    public partial class spchol
    {
        private static int spsymmgetmaxsimd(alglib.xparams _params)
        {
        #if ALGLIB_USE_SIMD
            return 4;
        #else
            return 1;
        #endif
        }

        /*************************************************************************
        Solving linear system: propagating computed supernode.

        Propagates computed supernode to the rest of the RHS  using  SIMD-friendly
        RHS storage format.

        INPUT PARAMETERS:

        OUTPUT PARAMETERS:

          -- ALGLIB routine --
             08.09.2021
             Bochkanov Sergey
        *************************************************************************/
        private static void propagatefwd(double[] x,
            int cols0,
            int blocksize,
            int[] superrowidx,
            int rbase,
            int offdiagsize,
            double[] rowstorage,
            int offss,
            int sstride,
            double[] simdbuf,
            int simdwidth,
            alglib.xparams _params)
        {
            int i = 0;
            int j = 0;
            int k = 0;
            int baseoffs = 0;
            double v = 0;

            for(k=0; k<=offdiagsize-1; k++)
            {
                i = superrowidx[rbase+k];
                baseoffs = offss+(k+blocksize)*sstride;
                v = simdbuf[i*simdwidth];
                for(j=0; j<=blocksize-1; j++)
                {
                    v = v-rowstorage[baseoffs+j]*x[cols0+j];
                }
                simdbuf[i*simdwidth] = v;
            }
        }

        /*************************************************************************
        Fast kernels for small supernodal updates: special 4x4x4x4 function.

        ! See comments on UpdateSupernode() for information  on generic supernodal
        ! updates, including notation used below.

        The generic update has following form:

            S := S - scatter(U*D*Uc')

        This specialized function performs AxBxCx4 update, i.e.:
        * S is a tHeight*A matrix with row stride equal to 4 (usually it means that
          it has 3 or 4 columns)
        * U is a uHeight*B matrix
        * Uc' is a B*C matrix, with C<=A
        * scatter() scatters rows and columns of U*Uc'
          
        Return value:
        * True if update was applied
        * False if kernel refused to perform an update (quick exit for unsupported
          combinations of input sizes)

          -- ALGLIB routine --
             20.09.2020
             Bochkanov Sergey
        *************************************************************************/
        #if ALGLIB_USE_SIMD
        private static unsafe bool try_updatekernelabc4(double* rowstorage,
            int offss,
            int twidth,
            int offsu,
            int uheight,
            int urank,
            int urowstride,
            int uwidth,
            double* diagd,
            int offsd,
            int* raw2smap,
            int* superrowidx,
            int urbase)
        {
            #if !ALGLIB_NO_SSE2
            #if !ALGLIB_NO_AVX2
            #if !ALGLIB_NO_FMA
            if( Fma.IsSupported )
            {
                int k;
                int targetrow;
                int targetcol;
                
                /*
                 * Filter out unsupported combinations (ones that are too sparse for the non-SIMD code)
                 */
                if( twidth<3||twidth>4 )
                    return false;
                if( uwidth<1||uwidth>4 )
                    return false;
                if( urank>4 )
                    return false;
                
                /*
                 * Shift input arrays to the beginning of the working area.
                 * Prepare SIMD masks
                 */
                Intrinsics.Vector256<double> v_rankmask = Fma.CompareGreaterThan(
                    Intrinsics.Vector256.Create((double)urank, (double)urank, (double)urank, (double)urank),
                    Intrinsics.Vector256.Create(0.0, 1.0, 2.0, 3.0));
                double *update_storage = rowstorage+offsu;
                double *target_storage = rowstorage+offss;
                superrowidx += urbase;
                
                /*
                 * Load head of the update matrix
                 */
                Intrinsics.Vector256<double> v_d0123 = Fma.MaskLoad(diagd+offsd, v_rankmask);
                Intrinsics.Vector256<double> u_0_0123 = Intrinsics.Vector256<double>.Zero;
                Intrinsics.Vector256<double> u_1_0123 = Intrinsics.Vector256<double>.Zero;
                Intrinsics.Vector256<double> u_2_0123 = Intrinsics.Vector256<double>.Zero;
                Intrinsics.Vector256<double> u_3_0123 = Intrinsics.Vector256<double>.Zero;
                for(k=0; k<=uwidth-1; k++)
                {
                    targetcol = raw2smap[superrowidx[k]];
                    if( targetcol==0 )
                        u_0_0123 = Fma.Multiply(v_d0123, Fma.MaskLoad(update_storage+k*urowstride, v_rankmask));
                    if( targetcol==1 )
                        u_1_0123 = Fma.Multiply(v_d0123, Fma.MaskLoad(update_storage+k*urowstride, v_rankmask));
                    if( targetcol==2 )
                        u_2_0123 = Fma.Multiply(v_d0123, Fma.MaskLoad(update_storage+k*urowstride, v_rankmask));
                    if( targetcol==3 )
                        u_3_0123 = Fma.Multiply(v_d0123, Fma.MaskLoad(update_storage+k*urowstride, v_rankmask));
                }
                
                /*
                 * Transpose head
                 */
                Intrinsics.Vector256<double> u01_lo = Fma.UnpackLow( u_0_0123,u_1_0123);
                Intrinsics.Vector256<double> u01_hi = Fma.UnpackHigh(u_0_0123,u_1_0123);
                Intrinsics.Vector256<double> u23_lo = Fma.UnpackLow( u_2_0123,u_3_0123);
                Intrinsics.Vector256<double> u23_hi = Fma.UnpackHigh(u_2_0123,u_3_0123);
                Intrinsics.Vector256<double> u_0123_0 = Fma.Permute2x128(u01_lo, u23_lo, 0x20);
                Intrinsics.Vector256<double> u_0123_1 = Fma.Permute2x128(u01_hi, u23_hi, 0x20);
                Intrinsics.Vector256<double> u_0123_2 = Fma.Permute2x128(u23_lo, u01_lo, 0x13);
                Intrinsics.Vector256<double> u_0123_3 = Fma.Permute2x128(u23_hi, u01_hi, 0x13);
                
                /*
                 * Run update
                 */
                if( urank==1 )
                {
                    for(k=0; k<=uheight-1; k++)
                    {
                        targetrow = raw2smap[superrowidx[k]]*4;
                        double *update_row = rowstorage+offsu+k*urowstride;
                        Fma.Store(target_storage+targetrow,
                            Fma.MultiplyAddNegated(Fma.BroadcastScalarToVector256(update_row+0), u_0123_0,
                                Fma.LoadVector256(target_storage+targetrow)));
                    }
                }
                if( urank==2 )
                {
                    for(k=0; k<=uheight-1; k++)
                    {
                        targetrow = raw2smap[superrowidx[k]]*4;
                        double *update_row = rowstorage+offsu+k*urowstride;
                        Fma.Store(target_storage+targetrow,
                            Fma.MultiplyAddNegated(Fma.BroadcastScalarToVector256(update_row+1), u_0123_1,
                            Fma.MultiplyAddNegated(Fma.BroadcastScalarToVector256(update_row+0), u_0123_0,
                                Fma.LoadVector256(target_storage+targetrow))));
                    }
                }
                if( urank==3 )
                {
                    for(k=0; k<=uheight-1; k++)
                    {
                        targetrow = raw2smap[superrowidx[k]]*4;
                        double *update_row = rowstorage+offsu+k*urowstride;
                        Fma.Store(target_storage+targetrow,
                            Fma.MultiplyAddNegated(Fma.BroadcastScalarToVector256(update_row+2), u_0123_2,
                            Fma.MultiplyAddNegated(Fma.BroadcastScalarToVector256(update_row+1), u_0123_1,
                            Fma.MultiplyAddNegated(Fma.BroadcastScalarToVector256(update_row+0), u_0123_0,
                                Fma.LoadVector256(target_storage+targetrow)))));
                    }
                }
                if( urank==4 )
                {
                    for(k=0; k<=uheight-1; k++)
                    {
                        targetrow = raw2smap[superrowidx[k]]*4;
                        double *update_row = rowstorage+offsu+k*urowstride;
                        Fma.Store(target_storage+targetrow,
                            Fma.MultiplyAddNegated(Fma.BroadcastScalarToVector256(update_row+3), u_0123_3,
                            Fma.MultiplyAddNegated(Fma.BroadcastScalarToVector256(update_row+2), u_0123_2,
                            Fma.MultiplyAddNegated(Fma.BroadcastScalarToVector256(update_row+1), u_0123_1,
                            Fma.MultiplyAddNegated(Fma.BroadcastScalarToVector256(update_row+0), u_0123_0,
                                Fma.LoadVector256(target_storage+targetrow))))));
                    }
                }
                return true;
            }
            #endif // no-fma
            if( Avx2.IsSupported )
            {
                int k;
                int targetrow;
                int targetcol;
                
                /*
                 * Filter out unsupported combinations (ones that are too sparse for the non-SIMD code)
                 */
                if( twidth<3||twidth>4 )
                    return false;
                if( uwidth<1||uwidth>4 )
                    return false;
                if( urank>4 )
                    return false;
                
                /*
                 * Shift input arrays to the beginning of the working area.
                 * Prepare SIMD masks
                 */
                Intrinsics.Vector256<double> v_rankmask = Avx2.CompareGreaterThan(
                    Intrinsics.Vector256.Create((double)urank, (double)urank, (double)urank, (double)urank),
                    Intrinsics.Vector256.Create(0.0, 1.0, 2.0, 3.0));
                double *update_storage = rowstorage+offsu;
                double *target_storage = rowstorage+offss;
                superrowidx += urbase;
                
                /*
                 * Load head of the update matrix
                 */
                Intrinsics.Vector256<double> v_d0123 = Avx2.MaskLoad(diagd+offsd, v_rankmask);
                Intrinsics.Vector256<double> u_0_0123 = Intrinsics.Vector256<double>.Zero;
                Intrinsics.Vector256<double> u_1_0123 = Intrinsics.Vector256<double>.Zero;
                Intrinsics.Vector256<double> u_2_0123 = Intrinsics.Vector256<double>.Zero;
                Intrinsics.Vector256<double> u_3_0123 = Intrinsics.Vector256<double>.Zero;
                for(k=0; k<=uwidth-1; k++)
                {
                    targetcol = raw2smap[superrowidx[k]];
                    if( targetcol==0 )
                        u_0_0123 = Avx2.Multiply(v_d0123, Avx2.MaskLoad(update_storage+k*urowstride, v_rankmask));
                    if( targetcol==1 )
                        u_1_0123 = Avx2.Multiply(v_d0123, Avx2.MaskLoad(update_storage+k*urowstride, v_rankmask));
                    if( targetcol==2 )
                        u_2_0123 = Avx2.Multiply(v_d0123, Avx2.MaskLoad(update_storage+k*urowstride, v_rankmask));
                    if( targetcol==3 )
                        u_3_0123 = Avx2.Multiply(v_d0123, Avx2.MaskLoad(update_storage+k*urowstride, v_rankmask));
                }
                
                /*
                 * Transpose head
                 */
                Intrinsics.Vector256<double> u01_lo = Avx2.UnpackLow( u_0_0123,u_1_0123);
                Intrinsics.Vector256<double> u01_hi = Avx2.UnpackHigh(u_0_0123,u_1_0123);
                Intrinsics.Vector256<double> u23_lo = Avx2.UnpackLow( u_2_0123,u_3_0123);
                Intrinsics.Vector256<double> u23_hi = Avx2.UnpackHigh(u_2_0123,u_3_0123);
                Intrinsics.Vector256<double> u_0123_0 = Avx2.Permute2x128(u01_lo, u23_lo, 0x20);
                Intrinsics.Vector256<double> u_0123_1 = Avx2.Permute2x128(u01_hi, u23_hi, 0x20);
                Intrinsics.Vector256<double> u_0123_2 = Avx2.Permute2x128(u23_lo, u01_lo, 0x13);
                Intrinsics.Vector256<double> u_0123_3 = Avx2.Permute2x128(u23_hi, u01_hi, 0x13);
                
                /*
                 * Run update
                 */
                if( urank==1 )
                {
                    for(k=0; k<=uheight-1; k++)
                    {
                        targetrow = raw2smap[superrowidx[k]]*4;
                        double *update_row = rowstorage+offsu+k*urowstride;
                        Avx2.Store(target_storage+targetrow,
                            Avx2.Subtract(Avx2.LoadVector256(target_storage+targetrow),
                                Avx2.Multiply(Avx2.BroadcastScalarToVector256(update_row+0), u_0123_0)));
                    }
                }
                if( urank==2 )
                {
                    for(k=0; k<=uheight-1; k++)
                    {
                        targetrow = raw2smap[superrowidx[k]]*4;
                        double *update_row = rowstorage+offsu+k*urowstride;
                        Avx2.Store(target_storage+targetrow,
                            Avx2.Subtract(Avx2.Subtract(Avx2.LoadVector256(target_storage+targetrow),
                                Avx2.Multiply(Avx2.BroadcastScalarToVector256(update_row+1), u_0123_1)),
                                Avx2.Multiply(Avx2.BroadcastScalarToVector256(update_row+0), u_0123_0)));
                    }
                }
                if( urank==3 )
                {
                    for(k=0; k<=uheight-1; k++)
                    {
                        targetrow = raw2smap[superrowidx[k]]*4;
                        double *update_row = rowstorage+offsu+k*urowstride;
                        Avx2.Store(target_storage+targetrow,
                            Avx2.Subtract(Avx2.Subtract(Avx2.Subtract(Avx2.LoadVector256(target_storage+targetrow),
                                Avx2.Multiply(Avx2.BroadcastScalarToVector256(update_row+2), u_0123_2)),
                                Avx2.Multiply(Avx2.BroadcastScalarToVector256(update_row+1), u_0123_1)),
                                Avx2.Multiply(Avx2.BroadcastScalarToVector256(update_row+0), u_0123_0)));
                    }
                }
                if( urank==4 )
                {
                    for(k=0; k<=uheight-1; k++)
                    {
                        targetrow = raw2smap[superrowidx[k]]*4;
                        double *update_row = rowstorage+offsu+k*urowstride;
                        Avx2.Store(target_storage+targetrow,
                            Avx2.Subtract(Avx2.Subtract(Avx2.Subtract(Avx2.Subtract(Avx2.LoadVector256(target_storage+targetrow),
                                Avx2.Multiply(Avx2.BroadcastScalarToVector256(update_row+3), u_0123_3)),
                                Avx2.Multiply(Avx2.BroadcastScalarToVector256(update_row+2), u_0123_2)),
                                Avx2.Multiply(Avx2.BroadcastScalarToVector256(update_row+1), u_0123_1)),
                                Avx2.Multiply(Avx2.BroadcastScalarToVector256(update_row+0), u_0123_0)));
                    }
                }
                return true;
            }
            #endif // no-avx2
            #endif // no-sse2
            return false;
        }
        #endif
        private static bool updatekernelabc4(double[] rowstorage,
            int offss,
            int twidth,
            int offsu,
            int uheight,
            int urank,
            int urowstride,
            int uwidth,
            double[] diagd,
            int offsd,
            int[] raw2smap,
            int[] superrowidx,
            int urbase,
            alglib.xparams _params)
        {
            #if ALGLIB_USE_SIMD
            unsafe
            {
                fixed(double* p_rowstorage=rowstorage, p_diagd = diagd)
                fixed(int* p_raw2smap=raw2smap, p_superrowidx=superrowidx)
                {
                    if( try_updatekernelabc4(p_rowstorage, offss, twidth, offsu, uheight, urank, urowstride, uwidth, p_diagd, offsd, p_raw2smap, p_superrowidx, urbase) )
                        return true;
                }
            }
            #endif
            
            //
            // Fallback pure C# code
            //
            bool result = new bool();
            int k = 0;
            int targetrow = 0;
            int targetcol = 0;
            int offsk = 0;
            double d0 = 0;
            double d1 = 0;
            double d2 = 0;
            double d3 = 0;
            double u00 = 0;
            double u01 = 0;
            double u02 = 0;
            double u03 = 0;
            double u10 = 0;
            double u11 = 0;
            double u12 = 0;
            double u13 = 0;
            double u20 = 0;
            double u21 = 0;
            double u22 = 0;
            double u23 = 0;
            double u30 = 0;
            double u31 = 0;
            double u32 = 0;
            double u33 = 0;
            double uk0 = 0;
            double uk1 = 0;
            double uk2 = 0;
            double uk3 = 0;
            int srccol0 = 0;
            int srccol1 = 0;
            int srccol2 = 0;
            int srccol3 = 0;

            
            //
            // Filter out unsupported combinations (ones that are too sparse for the non-SIMD code)
            //
            result = false;
            if( twidth<3 || twidth>4 )
            {
                return result;
            }
            if( uwidth<3 || uwidth>4 )
            {
                return result;
            }
            if( urank>4 )
            {
                return result;
            }
            
            //
            // Determine source columns for target columns, -1 if target column
            // is not updated.
            //
            srccol0 = -1;
            srccol1 = -1;
            srccol2 = -1;
            srccol3 = -1;
            for(k=0; k<=uwidth-1; k++)
            {
                targetcol = raw2smap[superrowidx[urbase+k]];
                if( targetcol==0 )
                {
                    srccol0 = k;
                }
                if( targetcol==1 )
                {
                    srccol1 = k;
                }
                if( targetcol==2 )
                {
                    srccol2 = k;
                }
                if( targetcol==3 )
                {
                    srccol3 = k;
                }
            }
            
            //
            // Load update matrix into aligned/rearranged 4x4 storage
            //
            d0 = 0;
            d1 = 0;
            d2 = 0;
            d3 = 0;
            u00 = 0;
            u01 = 0;
            u02 = 0;
            u03 = 0;
            u10 = 0;
            u11 = 0;
            u12 = 0;
            u13 = 0;
            u20 = 0;
            u21 = 0;
            u22 = 0;
            u23 = 0;
            u30 = 0;
            u31 = 0;
            u32 = 0;
            u33 = 0;
            if( urank>=1 )
            {
                d0 = diagd[offsd+0];
            }
            if( urank>=2 )
            {
                d1 = diagd[offsd+1];
            }
            if( urank>=3 )
            {
                d2 = diagd[offsd+2];
            }
            if( urank>=4 )
            {
                d3 = diagd[offsd+3];
            }
            if( srccol0>=0 )
            {
                if( urank>=1 )
                {
                    u00 = d0*rowstorage[offsu+srccol0*urowstride+0];
                }
                if( urank>=2 )
                {
                    u01 = d1*rowstorage[offsu+srccol0*urowstride+1];
                }
                if( urank>=3 )
                {
                    u02 = d2*rowstorage[offsu+srccol0*urowstride+2];
                }
                if( urank>=4 )
                {
                    u03 = d3*rowstorage[offsu+srccol0*urowstride+3];
                }
            }
            if( srccol1>=0 )
            {
                if( urank>=1 )
                {
                    u10 = d0*rowstorage[offsu+srccol1*urowstride+0];
                }
                if( urank>=2 )
                {
                    u11 = d1*rowstorage[offsu+srccol1*urowstride+1];
                }
                if( urank>=3 )
                {
                    u12 = d2*rowstorage[offsu+srccol1*urowstride+2];
                }
                if( urank>=4 )
                {
                    u13 = d3*rowstorage[offsu+srccol1*urowstride+3];
                }
            }
            if( srccol2>=0 )
            {
                if( urank>=1 )
                {
                    u20 = d0*rowstorage[offsu+srccol2*urowstride+0];
                }
                if( urank>=2 )
                {
                    u21 = d1*rowstorage[offsu+srccol2*urowstride+1];
                }
                if( urank>=3 )
                {
                    u22 = d2*rowstorage[offsu+srccol2*urowstride+2];
                }
                if( urank>=4 )
                {
                    u23 = d3*rowstorage[offsu+srccol2*urowstride+3];
                }
            }
            if( srccol3>=0 )
            {
                if( urank>=1 )
                {
                    u30 = d0*rowstorage[offsu+srccol3*urowstride+0];
                }
                if( urank>=2 )
                {
                    u31 = d1*rowstorage[offsu+srccol3*urowstride+1];
                }
                if( urank>=3 )
                {
                    u32 = d2*rowstorage[offsu+srccol3*urowstride+2];
                }
                if( urank>=4 )
                {
                    u33 = d3*rowstorage[offsu+srccol3*urowstride+3];
                }
            }
            
            //
            // Run update
            //
            if( urank==1 )
            {
                for(k=0; k<=uheight-1; k++)
                {
                    targetrow = offss+raw2smap[superrowidx[urbase+k]]*4;
                    offsk = offsu+k*urowstride;
                    uk0 = rowstorage[offsk+0];
                    rowstorage[targetrow+0] = rowstorage[targetrow+0]-u00*uk0;
                    rowstorage[targetrow+1] = rowstorage[targetrow+1]-u10*uk0;
                    rowstorage[targetrow+2] = rowstorage[targetrow+2]-u20*uk0;
                    rowstorage[targetrow+3] = rowstorage[targetrow+3]-u30*uk0;
                }
            }
            if( urank==2 )
            {
                for(k=0; k<=uheight-1; k++)
                {
                    targetrow = offss+raw2smap[superrowidx[urbase+k]]*4;
                    offsk = offsu+k*urowstride;
                    uk0 = rowstorage[offsk+0];
                    uk1 = rowstorage[offsk+1];
                    rowstorage[targetrow+0] = rowstorage[targetrow+0]-u00*uk0-u01*uk1;
                    rowstorage[targetrow+1] = rowstorage[targetrow+1]-u10*uk0-u11*uk1;
                    rowstorage[targetrow+2] = rowstorage[targetrow+2]-u20*uk0-u21*uk1;
                    rowstorage[targetrow+3] = rowstorage[targetrow+3]-u30*uk0-u31*uk1;
                }
            }
            if( urank==3 )
            {
                for(k=0; k<=uheight-1; k++)
                {
                    targetrow = offss+raw2smap[superrowidx[urbase+k]]*4;
                    offsk = offsu+k*urowstride;
                    uk0 = rowstorage[offsk+0];
                    uk1 = rowstorage[offsk+1];
                    uk2 = rowstorage[offsk+2];
                    rowstorage[targetrow+0] = rowstorage[targetrow+0]-u00*uk0-u01*uk1-u02*uk2;
                    rowstorage[targetrow+1] = rowstorage[targetrow+1]-u10*uk0-u11*uk1-u12*uk2;
                    rowstorage[targetrow+2] = rowstorage[targetrow+2]-u20*uk0-u21*uk1-u22*uk2;
                    rowstorage[targetrow+3] = rowstorage[targetrow+3]-u30*uk0-u31*uk1-u32*uk2;
                }
            }
            if( urank==4 )
            {
                for(k=0; k<=uheight-1; k++)
                {
                    targetrow = offss+raw2smap[superrowidx[urbase+k]]*4;
                    offsk = offsu+k*urowstride;
                    uk0 = rowstorage[offsk+0];
                    uk1 = rowstorage[offsk+1];
                    uk2 = rowstorage[offsk+2];
                    uk3 = rowstorage[offsk+3];
                    rowstorage[targetrow+0] = rowstorage[targetrow+0]-u00*uk0-u01*uk1-u02*uk2-u03*uk3;
                    rowstorage[targetrow+1] = rowstorage[targetrow+1]-u10*uk0-u11*uk1-u12*uk2-u13*uk3;
                    rowstorage[targetrow+2] = rowstorage[targetrow+2]-u20*uk0-u21*uk1-u22*uk2-u23*uk3;
                    rowstorage[targetrow+3] = rowstorage[targetrow+3]-u30*uk0-u31*uk1-u32*uk2-u33*uk3;
                }
            }
            result = true;
            return result;
        }

        /*************************************************************************
        Fast kernels for small supernodal updates: special 4x4x4x4 function.

        ! See comments on UpdateSupernode() for information  on generic supernodal
        ! updates, including notation used below.

        The generic update has following form:

            S := S - scatter(U*D*Uc')

        This specialized function performs 4x4x4x4 update, i.e.:
        * S is a tHeight*4 matrix
        * U is a uHeight*4 matrix
        * Uc' is a 4*4 matrix
        * scatter() scatters rows of U*Uc', but does not scatter columns (they are
          densely packed).
          
        Return value:
        * True if update was applied
        * False if kernel refused to perform an update.

          -- ALGLIB routine --
             20.09.2020
             Bochkanov Sergey
        *************************************************************************/
        #if ALGLIB_USE_SIMD
        private static unsafe bool try_updatekernel4444(double* rowstorage,
            int offss,
            int sheight,
            int offsu,
            int uheight,
            double *diagd,
            int offsd,
            int[] raw2smap,
            int[] superrowidx,
            int urbase)
        {
            #if !ALGLIB_NO_SSE2
            #if !ALGLIB_NO_AVX2
            #if !ALGLIB_NO_FMA
            if( Fma.IsSupported )
            {
                int k, targetrow, offsk;
                Intrinsics.Vector256<double> v_negd_u0, v_negd_u1, v_negd_u2, v_negd_u3, v_negd;
                Intrinsics.Vector256<double> v_w0, v_w1, v_w2, v_w3, u01_lo, u01_hi, u23_lo, u23_hi;
                
                /*
                 * Compute W = -D*transpose(U[0:3])
                 */
                v_negd = Avx2.Multiply(Avx2.LoadVector256(diagd+offsd),Intrinsics.Vector256.Create(-1.0));
                v_negd_u0   = Avx2.Multiply(Avx2.LoadVector256(rowstorage+offsu+0*4),v_negd);
                v_negd_u1   = Avx2.Multiply(Avx2.LoadVector256(rowstorage+offsu+1*4),v_negd);
                v_negd_u2   = Avx2.Multiply(Avx2.LoadVector256(rowstorage+offsu+2*4),v_negd);
                v_negd_u3   = Avx2.Multiply(Avx2.LoadVector256(rowstorage+offsu+3*4),v_negd);
                u01_lo = Avx2.UnpackLow( v_negd_u0,v_negd_u1);
                u01_hi = Avx2.UnpackHigh(v_negd_u0,v_negd_u1);
                u23_lo = Avx2.UnpackLow( v_negd_u2,v_negd_u3);
                u23_hi = Avx2.UnpackHigh(v_negd_u2,v_negd_u3);
                v_w0 = Avx2.Permute2x128(u01_lo, u23_lo, 0x20);
                v_w1 = Avx2.Permute2x128(u01_hi, u23_hi, 0x20);
                v_w2 = Avx2.Permute2x128(u23_lo, u01_lo, 0x13);
                v_w3 = Avx2.Permute2x128(u23_hi, u01_hi, 0x13);
                
                //
                // Compute update S:= S + row_scatter(U*W)
                //
                if( sheight==uheight )
                {
                    /*
                     * No row scatter, the most efficient code
                     */
                    for(k=0; k<=uheight-1; k++)
                    {
                        Intrinsics.Vector256<double> target;
                        
                        targetrow = offss+k*4;
                        offsk = offsu+k*4;
                        
                        target = Avx2.LoadVector256(rowstorage+targetrow);
                        target = Fma.MultiplyAdd(Avx2.BroadcastScalarToVector256(rowstorage+offsk+0),v_w0,target);
                        target = Fma.MultiplyAdd(Avx2.BroadcastScalarToVector256(rowstorage+offsk+1),v_w1,target);
                        target = Fma.MultiplyAdd(Avx2.BroadcastScalarToVector256(rowstorage+offsk+2),v_w2,target);
                        target = Fma.MultiplyAdd(Avx2.BroadcastScalarToVector256(rowstorage+offsk+3),v_w3,target);
                        Avx2.Store(rowstorage+targetrow, target);
                    }
                }
                else
                {
                    /*
                     * Row scatter is performed, less efficient code using double mapping to determine target row index
                     */
                    for(k=0; k<=uheight-1; k++)
                    {
                        Intrinsics.Vector256<double> target;
                        
                        targetrow = offss+raw2smap[superrowidx[urbase+k]]*4;
                        offsk = offsu+k*4;
                        
                        target = Avx2.LoadVector256(rowstorage+targetrow);
                        target = Fma.MultiplyAdd(Avx2.BroadcastScalarToVector256(rowstorage+offsk+0),v_w0,target);
                        target = Fma.MultiplyAdd(Avx2.BroadcastScalarToVector256(rowstorage+offsk+1),v_w1,target);
                        target = Fma.MultiplyAdd(Avx2.BroadcastScalarToVector256(rowstorage+offsk+2),v_w2,target);
                        target = Fma.MultiplyAdd(Avx2.BroadcastScalarToVector256(rowstorage+offsk+3),v_w3,target);
                        Avx2.Store(rowstorage+targetrow, target);
                    }
                }
                return true;
            }
            #endif // no-fma
            if( Avx2.IsSupported )
            {
                int k, targetrow, offsk;
                Intrinsics.Vector256<double> v_negd_u0, v_negd_u1, v_negd_u2, v_negd_u3, v_negd;
                Intrinsics.Vector256<double> v_w0, v_w1, v_w2, v_w3, u01_lo, u01_hi, u23_lo, u23_hi;
                
                /*
                 * Compute W = -D*transpose(U[0:3])
                 */
                v_negd = Avx2.Multiply(Avx2.LoadVector256(diagd+offsd),Intrinsics.Vector256.Create(-1.0));
                v_negd_u0   = Avx2.Multiply(Avx2.LoadVector256(rowstorage+offsu+0*4),v_negd);
                v_negd_u1   = Avx2.Multiply(Avx2.LoadVector256(rowstorage+offsu+1*4),v_negd);
                v_negd_u2   = Avx2.Multiply(Avx2.LoadVector256(rowstorage+offsu+2*4),v_negd);
                v_negd_u3   = Avx2.Multiply(Avx2.LoadVector256(rowstorage+offsu+3*4),v_negd);
                u01_lo = Avx2.UnpackLow( v_negd_u0,v_negd_u1);
                u01_hi = Avx2.UnpackHigh(v_negd_u0,v_negd_u1);
                u23_lo = Avx2.UnpackLow( v_negd_u2,v_negd_u3);
                u23_hi = Avx2.UnpackHigh(v_negd_u2,v_negd_u3);
                v_w0 = Avx2.Permute2x128(u01_lo, u23_lo, 0x20);
                v_w1 = Avx2.Permute2x128(u01_hi, u23_hi, 0x20);
                v_w2 = Avx2.Permute2x128(u23_lo, u01_lo, 0x13);
                v_w3 = Avx2.Permute2x128(u23_hi, u01_hi, 0x13);
                
                //
                // Compute update S:= S + row_scatter(U*W)
                //
                if( sheight==uheight )
                {
                    /*
                     * No row scatter, the most efficient code
                     */
                    for(k=0; k<=uheight-1; k++)
                    {
                        Intrinsics.Vector256<double> target;
                        
                        targetrow = offss+k*4;
                        offsk = offsu+k*4;
                        
                        target = Avx2.LoadVector256(rowstorage+targetrow);
                        target = Avx2.Add(Avx2.Multiply(Avx2.BroadcastScalarToVector256(rowstorage+offsk+0),v_w0),target);
                        target = Avx2.Add(Avx2.Multiply(Avx2.BroadcastScalarToVector256(rowstorage+offsk+1),v_w1),target);
                        target = Avx2.Add(Avx2.Multiply(Avx2.BroadcastScalarToVector256(rowstorage+offsk+2),v_w2),target);
                        target = Avx2.Add(Avx2.Multiply(Avx2.BroadcastScalarToVector256(rowstorage+offsk+3),v_w3),target);
                        Avx2.Store(rowstorage+targetrow, target);
                    }
                }
                else
                {
                    /*
                     * Row scatter is performed, less efficient code using double mapping to determine target row index
                     */
                    for(k=0; k<=uheight-1; k++)
                    {
                        Intrinsics.Vector256<double> target;
                        
                        targetrow = offss+raw2smap[superrowidx[urbase+k]]*4;
                        offsk = offsu+k*4;
                        
                        target = Avx2.LoadVector256(rowstorage+targetrow);
                        target = Avx2.Add(Avx2.Multiply(Avx2.BroadcastScalarToVector256(rowstorage+offsk+0),v_w0),target);
                        target = Avx2.Add(Avx2.Multiply(Avx2.BroadcastScalarToVector256(rowstorage+offsk+1),v_w1),target);
                        target = Avx2.Add(Avx2.Multiply(Avx2.BroadcastScalarToVector256(rowstorage+offsk+2),v_w2),target);
                        target = Avx2.Add(Avx2.Multiply(Avx2.BroadcastScalarToVector256(rowstorage+offsk+3),v_w3),target);
                        Avx2.Store(rowstorage+targetrow, target);
                    }
                }
                return true;
            }
            #endif // no-avx2
            #endif // no-sse2
            return false;
        }
        #endif
        private static bool updatekernel4444(double[] rowstorage,
            int offss,
            int sheight,
            int offsu,
            int uheight,
            double[] diagd,
            int offsd,
            int[] raw2smap,
            int[] superrowidx,
            int urbase,
            alglib.xparams _params)
        {
            #if ALGLIB_USE_SIMD
            unsafe
            {
                fixed(double* p_rowstorage=rowstorage, p_diagd = diagd)
                {
                    if( try_updatekernel4444(p_rowstorage, offss, sheight, offsu, uheight, p_diagd, offsd, raw2smap, superrowidx, urbase) )
                        return true;
                }
            }
            #endif
            
            //
            // Fallback pure C# code
            //
            bool result = new bool();
            int k = 0;
            int targetrow = 0;
            int offsk = 0;
            double d0 = 0;
            double d1 = 0;
            double d2 = 0;
            double d3 = 0;
            double u00 = 0;
            double u01 = 0;
            double u02 = 0;
            double u03 = 0;
            double u10 = 0;
            double u11 = 0;
            double u12 = 0;
            double u13 = 0;
            double u20 = 0;
            double u21 = 0;
            double u22 = 0;
            double u23 = 0;
            double u30 = 0;
            double u31 = 0;
            double u32 = 0;
            double u33 = 0;
            double uk0 = 0;
            double uk1 = 0;
            double uk2 = 0;
            double uk3 = 0;

            d0 = diagd[offsd+0];
            d1 = diagd[offsd+1];
            d2 = diagd[offsd+2];
            d3 = diagd[offsd+3];
            u00 = d0*rowstorage[offsu+0*4+0];
            u01 = d1*rowstorage[offsu+0*4+1];
            u02 = d2*rowstorage[offsu+0*4+2];
            u03 = d3*rowstorage[offsu+0*4+3];
            u10 = d0*rowstorage[offsu+1*4+0];
            u11 = d1*rowstorage[offsu+1*4+1];
            u12 = d2*rowstorage[offsu+1*4+2];
            u13 = d3*rowstorage[offsu+1*4+3];
            u20 = d0*rowstorage[offsu+2*4+0];
            u21 = d1*rowstorage[offsu+2*4+1];
            u22 = d2*rowstorage[offsu+2*4+2];
            u23 = d3*rowstorage[offsu+2*4+3];
            u30 = d0*rowstorage[offsu+3*4+0];
            u31 = d1*rowstorage[offsu+3*4+1];
            u32 = d2*rowstorage[offsu+3*4+2];
            u33 = d3*rowstorage[offsu+3*4+3];
            for(k=0; k<=uheight-1; k++)
            {
                targetrow = offss+raw2smap[superrowidx[urbase+k]]*4;
                offsk = offsu+k*4;
                uk0 = rowstorage[offsk+0];
                uk1 = rowstorage[offsk+1];
                uk2 = rowstorage[offsk+2];
                uk3 = rowstorage[offsk+3];
                rowstorage[targetrow+0] = rowstorage[targetrow+0]-u00*uk0-u01*uk1-u02*uk2-u03*uk3;
                rowstorage[targetrow+1] = rowstorage[targetrow+1]-u10*uk0-u11*uk1-u12*uk2-u13*uk3;
                rowstorage[targetrow+2] = rowstorage[targetrow+2]-u20*uk0-u21*uk1-u22*uk2-u23*uk3;
                rowstorage[targetrow+3] = rowstorage[targetrow+3]-u30*uk0-u31*uk1-u32*uk2-u33*uk3;
            }
            result = true;
            return result;
        }
    }
}
#endif
public partial class alglib
{
    public partial class smp
    {
        public static int cores_count = 1;
        public static volatile int cores_to_use = 1;
        public static bool isparallelcontext()
        {
            return false;
        }
    }
    public class smpselftests
    {
        public static bool runtests()
        {
            return true;
        }
    }
    public static void setnworkers(int nworkers)
    {
        alglib.smp.cores_to_use = nworkers;
    }
    public static int getnworkers()
    {
        return alglib.smp.cores_to_use;
    }
}
