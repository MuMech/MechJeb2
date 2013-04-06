/*************************************************************************
AP library
Copyright (c) 2003-2009 Sergey Bochkanov (ALGLIB project).

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
    reverse communication structure
    ********************************************************************/
    public class rcommstate
    {
        public rcommstate()
        {
            stage = -1;
            ia = new int[0];
            ba = new bool[0];
            ra = new double[0];
            ca = new alglib.complex[0];
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
                throw new alglibexception(s);
        }
        
        public static void assert(bool cond)
        {
            assert(cond, "ALGLIB: assertion failed");
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
    
    /********************************************************************
    serializer object (should not be used directly)
    ********************************************************************/
    public class serializer
    {
        enum SMODE { DEFAULT, ALLOC, TO_STRING, FROM_STRING };
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
        
        public serializer()
        {
            mode = SMODE.DEFAULT;
            entries_needed = 0;
            bytes_asked = 0;
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

        private int get_alloc_size()
        {
            int rows, lastrowsize, result;
            
            // check and change mode
            if( mode!=SMODE.ALLOC )
                throw new alglib.alglibexception("ALGLIB: internal error during (un)serialization");
            
            // if no entries needes (degenerate case)
            if( entries_needed==0 )
            {
                bytes_asked = 1;
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
            result  = ((rows-1)*SER_ENTRIES_PER_ROW+lastrowsize)*SER_ENTRY_LENGTH;
            result +=  (rows-1)*(SER_ENTRIES_PER_ROW-1)+(lastrowsize-1);
            result += rows*2;
            bytes_asked = result;
            return result;
        }

        public void sstart_str()
        {
            int allocsize = get_alloc_size();
            
            // check and change mode
            if( mode!=SMODE.ALLOC )
                throw new alglib.alglibexception("ALGLIB: internal error during (un)serialization");
            mode = SMODE.TO_STRING;
            
            // other preparations
            out_str = new char[allocsize];
            entries_saved = 0;
            bytes_written = 0;
        }

        public void ustart_str(string s)
        {
            // check and change mode
            if( mode!=SMODE.DEFAULT )
                throw new alglib.alglibexception("ALGLIB: internal error during (un)serialization");
            mode = SMODE.FROM_STRING;
            
            in_str = s.ToCharArray();
            bytes_read = 0;
        }

        public void serialize_bool(bool v)
        {
            if( mode!=SMODE.TO_STRING )
                throw new alglib.alglibexception("ALGLIB: internal error during (un)serialization");
            bool2str(v, out_str, ref bytes_written);
            entries_saved++;
            if( entries_saved%SER_ENTRIES_PER_ROW!=0 )
            {
                out_str[bytes_written] = ' ';
                bytes_written++;
            }
            else
            {
                out_str[bytes_written+0] = '\r';
                out_str[bytes_written+1] = '\n';
                bytes_written+=2;
            }            
        }

        public void serialize_int(int v)
        {
            if( mode!=SMODE.TO_STRING )
                throw new alglib.alglibexception("ALGLIB: internal error during (un)serialization");
            int2str(v, out_str, ref bytes_written);
            entries_saved++;
            if( entries_saved%SER_ENTRIES_PER_ROW!=0 )
            {
                out_str[bytes_written] = ' ';
                bytes_written++;
            }
            else
            {
                out_str[bytes_written+0] = '\r';
                out_str[bytes_written+1] = '\n';
                bytes_written+=2;
            }
        }

        public void serialize_double(double v)
        {
            if( mode!=SMODE.TO_STRING )
                throw new alglib.alglibexception("ALGLIB: internal error during (un)serialization");
            double2str(v, out_str, ref bytes_written);
            entries_saved++;
            if( entries_saved%SER_ENTRIES_PER_ROW!=0 )
            {
                out_str[bytes_written] = ' ';
                bytes_written++;
            }
            else
            {
                out_str[bytes_written+0] = '\r';
                out_str[bytes_written+1] = '\n';
                bytes_written+=2;
            }
        }

        public bool unserialize_bool()
        {
            if( mode!=SMODE.FROM_STRING )
                throw new alglib.alglibexception("ALGLIB: internal error during (un)serialization");
            return str2bool(in_str, ref bytes_read);
        }

        public int unserialize_int()
        {
            if( mode!=SMODE.FROM_STRING )
                throw new alglib.alglibexception("ALGLIB: internal error during (un)serialization");
            return str2int(in_str, ref bytes_read);
        }

        public double unserialize_double()
        {
            if( mode!=SMODE.FROM_STRING )
                throw new alglib.alglibexception("ALGLIB: internal error during (un)serialization");
            return str2double(in_str, ref bytes_read);
        }

        public void stop()
        {
        }

        public string get_string()
        {
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
    }}
