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



}
public partial class alglib
{



}
public partial class alglib
{



}
public partial class alglib
{


    /*************************************************************************
    This object stores state of the nonlinear CG optimizer.

    You should use ALGLIB functions to work with this object.
    *************************************************************************/
    public class mincgstate
    {
        //
        // Public declarations
        //
        public bool needf { get { return _innerobj.needf; } set { _innerobj.needf = value; } }
        public bool needfg { get { return _innerobj.needfg; } set { _innerobj.needfg = value; } }
        public bool xupdated { get { return _innerobj.xupdated; } set { _innerobj.xupdated = value; } }
        public double f { get { return _innerobj.f; } set { _innerobj.f = value; } }
        public double[] g { get { return _innerobj.g; } }
        public double[] x { get { return _innerobj.x; } }

        public mincgstate()
        {
            _innerobj = new mincg.mincgstate();
        }

        //
        // Although some of declarations below are public, you should not use them
        // They are intended for internal use only
        //
        private mincg.mincgstate _innerobj;
        public mincg.mincgstate innerobj { get { return _innerobj; } }
        public mincgstate(mincg.mincgstate obj)
        {
            _innerobj = obj;
        }
    }


    /*************************************************************************

    *************************************************************************/
    public class mincgreport
    {
        //
        // Public declarations
        //
        public int iterationscount { get { return _innerobj.iterationscount; } set { _innerobj.iterationscount = value; } }
        public int nfev { get { return _innerobj.nfev; } set { _innerobj.nfev = value; } }
        public int varidx { get { return _innerobj.varidx; } set { _innerobj.varidx = value; } }
        public int terminationtype { get { return _innerobj.terminationtype; } set { _innerobj.terminationtype = value; } }

        public mincgreport()
        {
            _innerobj = new mincg.mincgreport();
        }

        //
        // Although some of declarations below are public, you should not use them
        // They are intended for internal use only
        //
        private mincg.mincgreport _innerobj;
        public mincg.mincgreport innerobj { get { return _innerobj; } }
        public mincgreport(mincg.mincgreport obj)
        {
            _innerobj = obj;
        }
    }

    /*************************************************************************
            NONLINEAR CONJUGATE GRADIENT METHOD

    DESCRIPTION:
    The subroutine minimizes function F(x) of N arguments by using one of  the
    nonlinear conjugate gradient methods.

    These CG methods are globally convergent (even on non-convex functions) as
    long as grad(f) is Lipschitz continuous in  a  some  neighborhood  of  the
    L = { x : f(x)<=f(x0) }.


    REQUIREMENTS:
    Algorithm will request following information during its operation:
    * function value F and its gradient G (simultaneously) at given point X


    USAGE:
    1. User initializes algorithm state with MinCGCreate() call
    2. User tunes solver parameters with MinCGSetCond(), MinCGSetStpMax() and
       other functions
    3. User calls MinCGOptimize() function which takes algorithm  state   and
       pointer (delegate, etc.) to callback function which calculates F/G.
    4. User calls MinCGResults() to get solution
    5. Optionally, user may call MinCGRestartFrom() to solve another  problem
       with same N but another starting point and/or another function.
       MinCGRestartFrom() allows to reuse already initialized structure.


    INPUT PARAMETERS:
        N       -   problem dimension, N>0:
                    * if given, only leading N elements of X are used
                    * if not given, automatically determined from size of X
        X       -   starting point, array[0..N-1].

    OUTPUT PARAMETERS:
        State   -   structure which stores algorithm state

      -- ALGLIB --
         Copyright 25.03.2010 by Bochkanov Sergey
    *************************************************************************/
    public static void mincgcreate(int n, double[] x, out mincgstate state)
    {
        state = new mincgstate();
        mincg.mincgcreate(n, x, state.innerobj);
        return;
    }
    public static void mincgcreate(double[] x, out mincgstate state)
    {
        int n;

        state = new mincgstate();
        n = ap.len(x);
        mincg.mincgcreate(n, x, state.innerobj);

        return;
    }

    /*************************************************************************
    The subroutine is finite difference variant of MinCGCreate(). It uses
    finite differences in order to differentiate target function.

    Description below contains information which is specific to this function
    only. We recommend to read comments on MinCGCreate() in order to get more
    information about creation of CG optimizer.

    INPUT PARAMETERS:
        N       -   problem dimension, N>0:
                    * if given, only leading N elements of X are used
                    * if not given, automatically determined from size of X
        X       -   starting point, array[0..N-1].
        DiffStep-   differentiation step, >0

    OUTPUT PARAMETERS:
        State   -   structure which stores algorithm state

    NOTES:
    1. algorithm uses 4-point central formula for differentiation.
    2. differentiation step along I-th axis is equal to DiffStep*S[I] where
       S[] is scaling vector which can be set by MinCGSetScale() call.
    3. we recommend you to use moderate values of  differentiation  step.  Too
       large step will result in too large truncation  errors, while too small
       step will result in too large numerical  errors.  1.0E-6  can  be  good
       value to start with.
    4. Numerical  differentiation  is   very   inefficient  -   one   gradient
       calculation needs 4*N function evaluations. This function will work for
       any N - either small (1...10), moderate (10...100) or  large  (100...).
       However, performance penalty will be too severe for any N's except  for
       small ones.
       We should also say that code which relies on numerical  differentiation
       is  less  robust  and  precise.  L-BFGS  needs  exact  gradient values.
       Imprecise  gradient may slow down  convergence,  especially  on  highly
       nonlinear problems.
       Thus  we  recommend to use this function for fast prototyping on small-
       dimensional problems only, and to implement analytical gradient as soon
       as possible.

      -- ALGLIB --
         Copyright 16.05.2011 by Bochkanov Sergey
    *************************************************************************/
    public static void mincgcreatef(int n, double[] x, double diffstep, out mincgstate state)
    {
        state = new mincgstate();
        mincg.mincgcreatef(n, x, diffstep, state.innerobj);
        return;
    }
    public static void mincgcreatef(double[] x, double diffstep, out mincgstate state)
    {
        int n;

        state = new mincgstate();
        n = ap.len(x);
        mincg.mincgcreatef(n, x, diffstep, state.innerobj);

        return;
    }

    /*************************************************************************
    This function sets stopping conditions for CG optimization algorithm.

    INPUT PARAMETERS:
        State   -   structure which stores algorithm state
        EpsG    -   >=0
                    The  subroutine  finishes  its  work   if   the  condition
                    |v|<EpsG is satisfied, where:
                    * |.| means Euclidian norm
                    * v - scaled gradient vector, v[i]=g[i]*s[i]
                    * g - gradient
                    * s - scaling coefficients set by MinCGSetScale()
        EpsF    -   >=0
                    The  subroutine  finishes  its work if on k+1-th iteration
                    the  condition  |F(k+1)-F(k)|<=EpsF*max{|F(k)|,|F(k+1)|,1}
                    is satisfied.
        EpsX    -   >=0
                    The subroutine finishes its work if  on  k+1-th  iteration
                    the condition |v|<=EpsX is fulfilled, where:
                    * |.| means Euclidian norm
                    * v - scaled step vector, v[i]=dx[i]/s[i]
                    * dx - ste pvector, dx=X(k+1)-X(k)
                    * s - scaling coefficients set by MinCGSetScale()
        MaxIts  -   maximum number of iterations. If MaxIts=0, the  number  of
                    iterations is unlimited.

    Passing EpsG=0, EpsF=0, EpsX=0 and MaxIts=0 (simultaneously) will lead to
    automatic stopping criterion selection (small EpsX).

      -- ALGLIB --
         Copyright 02.04.2010 by Bochkanov Sergey
    *************************************************************************/
    public static void mincgsetcond(mincgstate state, double epsg, double epsf, double epsx, int maxits)
    {

        mincg.mincgsetcond(state.innerobj, epsg, epsf, epsx, maxits);
        return;
    }

    /*************************************************************************
    This function sets scaling coefficients for CG optimizer.

    ALGLIB optimizers use scaling matrices to test stopping  conditions  (step
    size and gradient are scaled before comparison with tolerances).  Scale of
    the I-th variable is a translation invariant measure of:
    a) "how large" the variable is
    b) how large the step should be to make significant changes in the function

    Scaling is also used by finite difference variant of CG optimizer  -  step
    along I-th axis is equal to DiffStep*S[I].

    In   most   optimizers  (and  in  the  CG  too)  scaling is NOT a form  of
    preconditioning. It just  affects  stopping  conditions.  You  should  set
    preconditioner by separate call to one of the MinCGSetPrec...() functions.

    There  is  special  preconditioning  mode, however,  which  uses   scaling
    coefficients to form diagonal preconditioning matrix. You  can  turn  this
    mode on, if you want.   But  you should understand that scaling is not the
    same thing as preconditioning - these are two different, although  related
    forms of tuning solver.

    INPUT PARAMETERS:
        State   -   structure stores algorithm state
        S       -   array[N], non-zero scaling coefficients
                    S[i] may be negative, sign doesn't matter.

      -- ALGLIB --
         Copyright 14.01.2011 by Bochkanov Sergey
    *************************************************************************/
    public static void mincgsetscale(mincgstate state, double[] s)
    {

        mincg.mincgsetscale(state.innerobj, s);
        return;
    }

    /*************************************************************************
    This function turns on/off reporting.

    INPUT PARAMETERS:
        State   -   structure which stores algorithm state
        NeedXRep-   whether iteration reports are needed or not

    If NeedXRep is True, algorithm will call rep() callback function if  it is
    provided to MinCGOptimize().

      -- ALGLIB --
         Copyright 02.04.2010 by Bochkanov Sergey
    *************************************************************************/
    public static void mincgsetxrep(mincgstate state, bool needxrep)
    {

        mincg.mincgsetxrep(state.innerobj, needxrep);
        return;
    }

    /*************************************************************************
    This function sets CG algorithm.

    INPUT PARAMETERS:
        State   -   structure which stores algorithm state
        CGType  -   algorithm type:
                    * -1    automatic selection of the best algorithm
                    * 0     DY (Dai and Yuan) algorithm
                    * 1     Hybrid DY-HS algorithm

      -- ALGLIB --
         Copyright 02.04.2010 by Bochkanov Sergey
    *************************************************************************/
    public static void mincgsetcgtype(mincgstate state, int cgtype)
    {

        mincg.mincgsetcgtype(state.innerobj, cgtype);
        return;
    }

    /*************************************************************************
    This function sets maximum step length

    INPUT PARAMETERS:
        State   -   structure which stores algorithm state
        StpMax  -   maximum step length, >=0. Set StpMax to 0.0,  if you don't
                    want to limit step length.

    Use this subroutine when you optimize target function which contains exp()
    or  other  fast  growing  functions,  and optimization algorithm makes too
    large  steps  which  leads  to overflow. This function allows us to reject
    steps  that  are  too  large  (and  therefore  expose  us  to the possible
    overflow) without actually calculating function value at the x+stp*d.

      -- ALGLIB --
         Copyright 02.04.2010 by Bochkanov Sergey
    *************************************************************************/
    public static void mincgsetstpmax(mincgstate state, double stpmax)
    {

        mincg.mincgsetstpmax(state.innerobj, stpmax);
        return;
    }

    /*************************************************************************
    This function allows to suggest initial step length to the CG algorithm.

    Suggested  step  length  is used as starting point for the line search. It
    can be useful when you have  badly  scaled  problem,  i.e.  when  ||grad||
    (which is used as initial estimate for the first step) is many  orders  of
    magnitude different from the desired step.

    Line search  may  fail  on  such problems without good estimate of initial
    step length. Imagine, for example, problem with ||grad||=10^50 and desired
    step equal to 0.1 Line  search function will use 10^50  as  initial  step,
    then  it  will  decrease step length by 2 (up to 20 attempts) and will get
    10^44, which is still too large.

    This function allows us to tell than line search should  be  started  from
    some moderate step length, like 1.0, so algorithm will be able  to  detect
    desired step length in a several searches.

    Default behavior (when no step is suggested) is to use preconditioner,  if
    it is available, to generate initial estimate of step length.

    This function influences only first iteration of algorithm. It  should  be
    called between MinCGCreate/MinCGRestartFrom() call and MinCGOptimize call.
    Suggested step is ignored if you have preconditioner.

    INPUT PARAMETERS:
        State   -   structure used to store algorithm state.
        Stp     -   initial estimate of the step length.
                    Can be zero (no estimate).

      -- ALGLIB --
         Copyright 30.07.2010 by Bochkanov Sergey
    *************************************************************************/
    public static void mincgsuggeststep(mincgstate state, double stp)
    {

        mincg.mincgsuggeststep(state.innerobj, stp);
        return;
    }

    /*************************************************************************
    Modification of the preconditioner: preconditioning is turned off.

    INPUT PARAMETERS:
        State   -   structure which stores algorithm state

    NOTE:  you  can  change  preconditioner  "on  the  fly",  during algorithm
    iterations.

      -- ALGLIB --
         Copyright 13.10.2010 by Bochkanov Sergey
    *************************************************************************/
    public static void mincgsetprecdefault(mincgstate state)
    {

        mincg.mincgsetprecdefault(state.innerobj);
        return;
    }

    /*************************************************************************
    Modification  of  the  preconditioner:  diagonal of approximate Hessian is
    used.

    INPUT PARAMETERS:
        State   -   structure which stores algorithm state
        D       -   diagonal of the approximate Hessian, array[0..N-1],
                    (if larger, only leading N elements are used).

    NOTE:  you  can  change  preconditioner  "on  the  fly",  during algorithm
    iterations.

    NOTE 2: D[i] should be positive. Exception will be thrown otherwise.

    NOTE 3: you should pass diagonal of approximate Hessian - NOT ITS INVERSE.

      -- ALGLIB --
         Copyright 13.10.2010 by Bochkanov Sergey
    *************************************************************************/
    public static void mincgsetprecdiag(mincgstate state, double[] d)
    {

        mincg.mincgsetprecdiag(state.innerobj, d);
        return;
    }

    /*************************************************************************
    Modification of the preconditioner: scale-based diagonal preconditioning.

    This preconditioning mode can be useful when you  don't  have  approximate
    diagonal of Hessian, but you know that your  variables  are  badly  scaled
    (for  example,  one  variable is in [1,10], and another in [1000,100000]),
    and most part of the ill-conditioning comes from different scales of vars.

    In this case simple  scale-based  preconditioner,  with H[i] = 1/(s[i]^2),
    can greatly improve convergence.

    IMPRTANT: you should set scale of your variables with MinCGSetScale() call
    (before or after MinCGSetPrecScale() call). Without knowledge of the scale
    of your variables scale-based preconditioner will be just unit matrix.

    INPUT PARAMETERS:
        State   -   structure which stores algorithm state

    NOTE:  you  can  change  preconditioner  "on  the  fly",  during algorithm
    iterations.

      -- ALGLIB --
         Copyright 13.10.2010 by Bochkanov Sergey
    *************************************************************************/
    public static void mincgsetprecscale(mincgstate state)
    {

        mincg.mincgsetprecscale(state.innerobj);
        return;
    }

    /*************************************************************************
    This function provides reverse communication interface
    Reverse communication interface is not documented or recommended to use.
    See below for functions which provide better documented API
    *************************************************************************/
    public static bool mincgiteration(mincgstate state)
    {

        bool result = mincg.mincgiteration(state.innerobj);
        return result;
    }
    /*************************************************************************
    This family of functions is used to launcn iterations of nonlinear optimizer

    These functions accept following parameters:
        func    -   callback which calculates function (or merit function)
                    value func at given point x
        grad    -   callback which calculates function (or merit function)
                    value func and gradient grad at given point x
        rep     -   optional callback which is called after each iteration
                    can be null
        obj     -   optional object which is passed to func/grad/hess/jac/rep
                    can be null

    NOTES:

    1. This function has two different implementations: one which  uses  exact
       (analytical) user-supplied  gradient, and one which uses function value
       only  and  numerically  differentiates  function  in  order  to  obtain
       gradient.

       Depending  on  the  specific  function  used to create optimizer object
       (either MinCGCreate()  for analytical gradient  or  MinCGCreateF()  for
       numerical differentiation) you should  choose  appropriate  variant  of
       MinCGOptimize() - one which accepts function AND gradient or one  which
       accepts function ONLY.

       Be careful to choose variant of MinCGOptimize()  which  corresponds  to
       your optimization scheme! Table below lists different  combinations  of
       callback (function/gradient) passed  to  MinCGOptimize()  and  specific
       function used to create optimizer.


                      |         USER PASSED TO MinCGOptimize()
       CREATED WITH   |  function only   |  function and gradient
       ------------------------------------------------------------
       MinCGCreateF() |     work                FAIL
       MinCGCreate()  |     FAIL                work

       Here "FAIL" denotes inappropriate combinations  of  optimizer  creation
       function and MinCGOptimize() version. Attemps to use  such  combination
       (for  example,  to create optimizer with  MinCGCreateF()  and  to  pass
       gradient information to MinCGOptimize()) will lead to  exception  being
       thrown. Either  you  did  not  pass  gradient when it WAS needed or you
       passed gradient when it was NOT needed.

      -- ALGLIB --
         Copyright 20.04.2009 by Bochkanov Sergey

    *************************************************************************/
    public static void mincgoptimize(mincgstate state, ndimensional_func func, ndimensional_rep rep, object obj)
    {
        if( func==null )
            throw new alglibexception("ALGLIB: error in 'mincgoptimize()' (func is null)");
        while( alglib.mincgiteration(state) )
        {
            if( state.needf )
            {
                func(state.x, ref state.innerobj.f, obj);
                continue;
            }
            if( state.innerobj.xupdated )
            {
                if( rep!=null )
                    rep(state.innerobj.x, state.innerobj.f, obj);
                continue;
            }
            throw new alglibexception("ALGLIB: error in 'mincgoptimize' (some derivatives were not provided?)");
        }
    }


    public static void mincgoptimize(mincgstate state, ndimensional_grad grad, ndimensional_rep rep, object obj)
    {
        if( grad==null )
            throw new alglibexception("ALGLIB: error in 'mincgoptimize()' (grad is null)");
        while( alglib.mincgiteration(state) )
        {
            if( state.needfg )
            {
                grad(state.x, ref state.innerobj.f, state.innerobj.g, obj);
                continue;
            }
            if( state.innerobj.xupdated )
            {
                if( rep!=null )
                    rep(state.innerobj.x, state.innerobj.f, obj);
                continue;
            }
            throw new alglibexception("ALGLIB: error in 'mincgoptimize' (some derivatives were not provided?)");
        }
    }



    /*************************************************************************
    Conjugate gradient results

    INPUT PARAMETERS:
        State   -   algorithm state

    OUTPUT PARAMETERS:
        X       -   array[0..N-1], solution
        Rep     -   optimization report:
                    * Rep.TerminationType completetion code:
                        * -7    gradient verification failed.
                                See MinCGSetGradientCheck() for more information.
                        *  1    relative function improvement is no more than
                                EpsF.
                        *  2    relative step is no more than EpsX.
                        *  4    gradient norm is no more than EpsG
                        *  5    MaxIts steps was taken
                        *  7    stopping conditions are too stringent,
                                further improvement is impossible,
                                we return best X found so far
                        *  8    terminated by user
                    * Rep.IterationsCount contains iterations count
                    * NFEV countains number of function calculations

      -- ALGLIB --
         Copyright 20.04.2009 by Bochkanov Sergey
    *************************************************************************/
    public static void mincgresults(mincgstate state, out double[] x, out mincgreport rep)
    {
        x = new double[0];
        rep = new mincgreport();
        mincg.mincgresults(state.innerobj, ref x, rep.innerobj);
        return;
    }

    /*************************************************************************
    Conjugate gradient results

    Buffered implementation of MinCGResults(), which uses pre-allocated buffer
    to store X[]. If buffer size is  too  small,  it  resizes  buffer.  It  is
    intended to be used in the inner cycles of performance critical algorithms
    where array reallocation penalty is too large to be ignored.

      -- ALGLIB --
         Copyright 20.04.2009 by Bochkanov Sergey
    *************************************************************************/
    public static void mincgresultsbuf(mincgstate state, ref double[] x, mincgreport rep)
    {

        mincg.mincgresultsbuf(state.innerobj, ref x, rep.innerobj);
        return;
    }

    /*************************************************************************
    This  subroutine  restarts  CG  algorithm from new point. All optimization
    parameters are left unchanged.

    This  function  allows  to  solve multiple  optimization  problems  (which
    must have same number of dimensions) without object reallocation penalty.

    INPUT PARAMETERS:
        State   -   structure used to store algorithm state.
        X       -   new starting point.

      -- ALGLIB --
         Copyright 30.07.2010 by Bochkanov Sergey
    *************************************************************************/
    public static void mincgrestartfrom(mincgstate state, double[] x)
    {

        mincg.mincgrestartfrom(state.innerobj, x);
        return;
    }

    /*************************************************************************

    This  subroutine  turns  on  verification  of  the  user-supplied analytic
    gradient:
    * user calls this subroutine before optimization begins
    * MinCGOptimize() is called
    * prior to  actual  optimization, for each component  of  parameters being
      optimized X[i] algorithm performs following steps:
      * two trial steps are made to X[i]-TestStep*S[i] and X[i]+TestStep*S[i],
        where X[i] is i-th component of the initial point and S[i] is a  scale
        of i-th parameter
      * F(X) is evaluated at these trial points
      * we perform one more evaluation in the middle point of the interval
      * we  build  cubic  model using function values and derivatives at trial
        points and we compare its prediction with actual value in  the  middle
        point
      * in case difference between prediction and actual value is higher  than
        some predetermined threshold, algorithm stops with completion code -7;
        Rep.VarIdx is set to index of the parameter with incorrect derivative.
    * after verification is over, algorithm proceeds to the actual optimization.

    NOTE 1: verification  needs  N (parameters count) gradient evaluations. It
            is very costly and you should use  it  only  for  low  dimensional
            problems,  when  you  want  to  be  sure  that  you've   correctly
            calculated  analytic  derivatives.  You  should  not use it in the
            production code (unless you want to check derivatives provided  by
            some third party).

    NOTE 2: you  should  carefully  choose  TestStep. Value which is too large
            (so large that function behaviour is significantly non-cubic) will
            lead to false alarms. You may use  different  step  for  different
            parameters by means of setting scale with MinCGSetScale().

    NOTE 3: this function may lead to false positives. In case it reports that
            I-th  derivative was calculated incorrectly, you may decrease test
            step  and  try  one  more  time  - maybe your function changes too
            sharply  and  your  step  is  too  large for such rapidly chanding
            function.

    INPUT PARAMETERS:
        State       -   structure used to store algorithm state
        TestStep    -   verification step:
                        * TestStep=0 turns verification off
                        * TestStep>0 activates verification

      -- ALGLIB --
         Copyright 31.05.2012 by Bochkanov Sergey
    *************************************************************************/
    public static void mincgsetgradientcheck(mincgstate state, double teststep)
    {

        mincg.mincgsetgradientcheck(state.innerobj, teststep);
        return;
    }

}
public partial class alglib
{


    /*************************************************************************
    This object stores nonlinear optimizer state.
    You should use functions provided by MinBLEIC subpackage to work with this
    object
    *************************************************************************/
    public class minbleicstate
    {
        //
        // Public declarations
        //
        public bool needf { get { return _innerobj.needf; } set { _innerobj.needf = value; } }
        public bool needfg { get { return _innerobj.needfg; } set { _innerobj.needfg = value; } }
        public bool xupdated { get { return _innerobj.xupdated; } set { _innerobj.xupdated = value; } }
        public double f { get { return _innerobj.f; } set { _innerobj.f = value; } }
        public double[] g { get { return _innerobj.g; } }
        public double[] x { get { return _innerobj.x; } }

        public minbleicstate()
        {
            _innerobj = new minbleic.minbleicstate();
        }

        //
        // Although some of declarations below are public, you should not use them
        // They are intended for internal use only
        //
        private minbleic.minbleicstate _innerobj;
        public minbleic.minbleicstate innerobj { get { return _innerobj; } }
        public minbleicstate(minbleic.minbleicstate obj)
        {
            _innerobj = obj;
        }
    }


    /*************************************************************************
    This structure stores optimization report:
    * IterationsCount           number of iterations
    * NFEV                      number of gradient evaluations
    * TerminationType           termination type (see below)

    TERMINATION CODES

    TerminationType field contains completion code, which can be:
      -7    gradient verification failed.
            See MinBLEICSetGradientCheck() for more information.
      -3    inconsistent constraints. Feasible point is
            either nonexistent or too hard to find. Try to
            restart optimizer with better initial approximation
       1    relative function improvement is no more than EpsF.
       2    relative step is no more than EpsX.
       4    gradient norm is no more than EpsG
       5    MaxIts steps was taken
       7    stopping conditions are too stringent,
            further improvement is impossible,
            X contains best point found so far.

    ADDITIONAL FIELDS

    There are additional fields which can be used for debugging:
    * DebugEqErr                error in the equality constraints (2-norm)
    * DebugFS                   f, calculated at projection of initial point
                                to the feasible set
    * DebugFF                   f, calculated at the final point
    * DebugDX                   |X_start-X_final|
    *************************************************************************/
    public class minbleicreport
    {
        //
        // Public declarations
        //
        public int iterationscount { get { return _innerobj.iterationscount; } set { _innerobj.iterationscount = value; } }
        public int nfev { get { return _innerobj.nfev; } set { _innerobj.nfev = value; } }
        public int varidx { get { return _innerobj.varidx; } set { _innerobj.varidx = value; } }
        public int terminationtype { get { return _innerobj.terminationtype; } set { _innerobj.terminationtype = value; } }
        public double debugeqerr { get { return _innerobj.debugeqerr; } set { _innerobj.debugeqerr = value; } }
        public double debugfs { get { return _innerobj.debugfs; } set { _innerobj.debugfs = value; } }
        public double debugff { get { return _innerobj.debugff; } set { _innerobj.debugff = value; } }
        public double debugdx { get { return _innerobj.debugdx; } set { _innerobj.debugdx = value; } }
        public int debugfeasqpits { get { return _innerobj.debugfeasqpits; } set { _innerobj.debugfeasqpits = value; } }
        public int debugfeasgpaits { get { return _innerobj.debugfeasgpaits; } set { _innerobj.debugfeasgpaits = value; } }
        public int inneriterationscount { get { return _innerobj.inneriterationscount; } set { _innerobj.inneriterationscount = value; } }
        public int outeriterationscount { get { return _innerobj.outeriterationscount; } set { _innerobj.outeriterationscount = value; } }

        public minbleicreport()
        {
            _innerobj = new minbleic.minbleicreport();
        }

        //
        // Although some of declarations below are public, you should not use them
        // They are intended for internal use only
        //
        private minbleic.minbleicreport _innerobj;
        public minbleic.minbleicreport innerobj { get { return _innerobj; } }
        public minbleicreport(minbleic.minbleicreport obj)
        {
            _innerobj = obj;
        }
    }

    /*************************************************************************
                         BOUND CONSTRAINED OPTIMIZATION
           WITH ADDITIONAL LINEAR EQUALITY AND INEQUALITY CONSTRAINTS

    DESCRIPTION:
    The  subroutine  minimizes  function   F(x)  of N arguments subject to any
    combination of:
    * bound constraints
    * linear inequality constraints
    * linear equality constraints

    REQUIREMENTS:
    * user must provide function value and gradient
    * starting point X0 must be feasible or
      not too far away from the feasible set
    * grad(f) must be Lipschitz continuous on a level set:
      L = { x : f(x)<=f(x0) }
    * function must be defined everywhere on the feasible set F

    USAGE:

    Constrained optimization if far more complex than the unconstrained one.
    Here we give very brief outline of the BLEIC optimizer. We strongly recommend
    you to read examples in the ALGLIB Reference Manual and to read ALGLIB User Guide
    on optimization, which is available at http://www.alglib.net/optimization/

    1. User initializes algorithm state with MinBLEICCreate() call

    2. USer adds boundary and/or linear constraints by calling
       MinBLEICSetBC() and MinBLEICSetLC() functions.

    3. User sets stopping conditions with MinBLEICSetCond().

    4. User calls MinBLEICOptimize() function which takes algorithm  state and
       pointer (delegate, etc.) to callback function which calculates F/G.

    5. User calls MinBLEICResults() to get solution

    6. Optionally user may call MinBLEICRestartFrom() to solve another problem
       with same N but another starting point.
       MinBLEICRestartFrom() allows to reuse already initialized structure.


    INPUT PARAMETERS:
        N       -   problem dimension, N>0:
                    * if given, only leading N elements of X are used
                    * if not given, automatically determined from size ofX
        X       -   starting point, array[N]:
                    * it is better to set X to a feasible point
                    * but X can be infeasible, in which case algorithm will try
                      to find feasible point first, using X as initial
                      approximation.

    OUTPUT PARAMETERS:
        State   -   structure stores algorithm state

      -- ALGLIB --
         Copyright 28.11.2010 by Bochkanov Sergey
    *************************************************************************/
    public static void minbleiccreate(int n, double[] x, out minbleicstate state)
    {
        state = new minbleicstate();
        minbleic.minbleiccreate(n, x, state.innerobj);
        return;
    }
    public static void minbleiccreate(double[] x, out minbleicstate state)
    {
        int n;

        state = new minbleicstate();
        n = ap.len(x);
        minbleic.minbleiccreate(n, x, state.innerobj);

        return;
    }

    /*************************************************************************
    The subroutine is finite difference variant of MinBLEICCreate().  It  uses
    finite differences in order to differentiate target function.

    Description below contains information which is specific to  this function
    only. We recommend to read comments on MinBLEICCreate() in  order  to  get
    more information about creation of BLEIC optimizer.

    INPUT PARAMETERS:
        N       -   problem dimension, N>0:
                    * if given, only leading N elements of X are used
                    * if not given, automatically determined from size of X
        X       -   starting point, array[0..N-1].
        DiffStep-   differentiation step, >0

    OUTPUT PARAMETERS:
        State   -   structure which stores algorithm state

    NOTES:
    1. algorithm uses 4-point central formula for differentiation.
    2. differentiation step along I-th axis is equal to DiffStep*S[I] where
       S[] is scaling vector which can be set by MinBLEICSetScale() call.
    3. we recommend you to use moderate values of  differentiation  step.  Too
       large step will result in too large truncation  errors, while too small
       step will result in too large numerical  errors.  1.0E-6  can  be  good
       value to start with.
    4. Numerical  differentiation  is   very   inefficient  -   one   gradient
       calculation needs 4*N function evaluations. This function will work for
       any N - either small (1...10), moderate (10...100) or  large  (100...).
       However, performance penalty will be too severe for any N's except  for
       small ones.
       We should also say that code which relies on numerical  differentiation
       is  less  robust and precise. CG needs exact gradient values. Imprecise
       gradient may slow  down  convergence, especially  on  highly  nonlinear
       problems.
       Thus  we  recommend to use this function for fast prototyping on small-
       dimensional problems only, and to implement analytical gradient as soon
       as possible.

      -- ALGLIB --
         Copyright 16.05.2011 by Bochkanov Sergey
    *************************************************************************/
    public static void minbleiccreatef(int n, double[] x, double diffstep, out minbleicstate state)
    {
        state = new minbleicstate();
        minbleic.minbleiccreatef(n, x, diffstep, state.innerobj);
        return;
    }
    public static void minbleiccreatef(double[] x, double diffstep, out minbleicstate state)
    {
        int n;

        state = new minbleicstate();
        n = ap.len(x);
        minbleic.minbleiccreatef(n, x, diffstep, state.innerobj);

        return;
    }

    /*************************************************************************
    This function sets boundary constraints for BLEIC optimizer.

    Boundary constraints are inactive by default (after initial creation).
    They are preserved after algorithm restart with MinBLEICRestartFrom().

    INPUT PARAMETERS:
        State   -   structure stores algorithm state
        BndL    -   lower bounds, array[N].
                    If some (all) variables are unbounded, you may specify
                    very small number or -INF.
        BndU    -   upper bounds, array[N].
                    If some (all) variables are unbounded, you may specify
                    very large number or +INF.

    NOTE 1: it is possible to specify BndL[i]=BndU[i]. In this case I-th
    variable will be "frozen" at X[i]=BndL[i]=BndU[i].

    NOTE 2: this solver has following useful properties:
    * bound constraints are always satisfied exactly
    * function is evaluated only INSIDE area specified by  bound  constraints,
      even  when  numerical  differentiation is used (algorithm adjusts  nodes
      according to boundary constraints)

      -- ALGLIB --
         Copyright 28.11.2010 by Bochkanov Sergey
    *************************************************************************/
    public static void minbleicsetbc(minbleicstate state, double[] bndl, double[] bndu)
    {

        minbleic.minbleicsetbc(state.innerobj, bndl, bndu);
        return;
    }

    /*************************************************************************
    This function sets linear constraints for BLEIC optimizer.

    Linear constraints are inactive by default (after initial creation).
    They are preserved after algorithm restart with MinBLEICRestartFrom().

    INPUT PARAMETERS:
        State   -   structure previously allocated with MinBLEICCreate call.
        C       -   linear constraints, array[K,N+1].
                    Each row of C represents one constraint, either equality
                    or inequality (see below):
                    * first N elements correspond to coefficients,
                    * last element corresponds to the right part.
                    All elements of C (including right part) must be finite.
        CT      -   type of constraints, array[K]:
                    * if CT[i]>0, then I-th constraint is C[i,*]*x >= C[i,n+1]
                    * if CT[i]=0, then I-th constraint is C[i,*]*x  = C[i,n+1]
                    * if CT[i]<0, then I-th constraint is C[i,*]*x <= C[i,n+1]
        K       -   number of equality/inequality constraints, K>=0:
                    * if given, only leading K elements of C/CT are used
                    * if not given, automatically determined from sizes of C/CT

    NOTE 1: linear (non-bound) constraints are satisfied only approximately:
    * there always exists some minor violation (about Epsilon in magnitude)
      due to rounding errors
    * numerical differentiation, if used, may  lead  to  function  evaluations
      outside  of the feasible  area,   because   algorithm  does  NOT  change
      numerical differentiation formula according to linear constraints.
    If you want constraints to be  satisfied  exactly, try to reformulate your
    problem  in  such  manner  that  all constraints will become boundary ones
    (this kind of constraints is always satisfied exactly, both in  the  final
    solution and in all intermediate points).

      -- ALGLIB --
         Copyright 28.11.2010 by Bochkanov Sergey
    *************************************************************************/
    public static void minbleicsetlc(minbleicstate state, double[,] c, int[] ct, int k)
    {

        minbleic.minbleicsetlc(state.innerobj, c, ct, k);
        return;
    }
    public static void minbleicsetlc(minbleicstate state, double[,] c, int[] ct)
    {
        int k;
        if( (ap.rows(c)!=ap.len(ct)))
            throw new alglibexception("Error while calling 'minbleicsetlc': looks like one of arguments has wrong size");

        k = ap.rows(c);
        minbleic.minbleicsetlc(state.innerobj, c, ct, k);

        return;
    }

    /*************************************************************************
    This function sets stopping conditions for the optimizer.

    INPUT PARAMETERS:
        State   -   structure which stores algorithm state
        EpsG    -   >=0
                    The  subroutine  finishes  its  work   if   the  condition
                    |v|<EpsG is satisfied, where:
                    * |.| means Euclidian norm
                    * v - scaled gradient vector, v[i]=g[i]*s[i]
                    * g - gradient
                    * s - scaling coefficients set by MinBLEICSetScale()
        EpsF    -   >=0
                    The  subroutine  finishes  its work if on k+1-th iteration
                    the  condition  |F(k+1)-F(k)|<=EpsF*max{|F(k)|,|F(k+1)|,1}
                    is satisfied.
        EpsX    -   >=0
                    The subroutine finishes its work if  on  k+1-th  iteration
                    the condition |v|<=EpsX is fulfilled, where:
                    * |.| means Euclidian norm
                    * v - scaled step vector, v[i]=dx[i]/s[i]
                    * dx - ste pvector, dx=X(k+1)-X(k)
                    * s - scaling coefficients set by MinBLEICSetScale()
        MaxIts  -   maximum number of iterations. If MaxIts=0, the  number  of
                    iterations is unlimited.

    Passing EpsG=0, EpsF=0 and EpsX=0 and MaxIts=0 (simultaneously) will lead
    to automatic stopping criterion selection.

      -- ALGLIB --
         Copyright 28.11.2010 by Bochkanov Sergey
    *************************************************************************/
    public static void minbleicsetcond(minbleicstate state, double epsg, double epsf, double epsx, int maxits)
    {

        minbleic.minbleicsetcond(state.innerobj, epsg, epsf, epsx, maxits);
        return;
    }

    /*************************************************************************
    This function sets scaling coefficients for BLEIC optimizer.

    ALGLIB optimizers use scaling matrices to test stopping  conditions  (step
    size and gradient are scaled before comparison with tolerances).  Scale of
    the I-th variable is a translation invariant measure of:
    a) "how large" the variable is
    b) how large the step should be to make significant changes in the function

    Scaling is also used by finite difference variant of the optimizer  - step
    along I-th axis is equal to DiffStep*S[I].

    In  most  optimizers  (and  in  the  BLEIC  too)  scaling is NOT a form of
    preconditioning. It just  affects  stopping  conditions.  You  should  set
    preconditioner  by  separate  call  to  one  of  the  MinBLEICSetPrec...()
    functions.

    There is a special  preconditioning  mode, however,  which  uses   scaling
    coefficients to form diagonal preconditioning matrix. You  can  turn  this
    mode on, if you want.   But  you should understand that scaling is not the
    same thing as preconditioning - these are two different, although  related
    forms of tuning solver.

    INPUT PARAMETERS:
        State   -   structure stores algorithm state
        S       -   array[N], non-zero scaling coefficients
                    S[i] may be negative, sign doesn't matter.

      -- ALGLIB --
         Copyright 14.01.2011 by Bochkanov Sergey
    *************************************************************************/
    public static void minbleicsetscale(minbleicstate state, double[] s)
    {

        minbleic.minbleicsetscale(state.innerobj, s);
        return;
    }

    /*************************************************************************
    Modification of the preconditioner: preconditioning is turned off.

    INPUT PARAMETERS:
        State   -   structure which stores algorithm state

      -- ALGLIB --
         Copyright 13.10.2010 by Bochkanov Sergey
    *************************************************************************/
    public static void minbleicsetprecdefault(minbleicstate state)
    {

        minbleic.minbleicsetprecdefault(state.innerobj);
        return;
    }

    /*************************************************************************
    Modification  of  the  preconditioner:  diagonal of approximate Hessian is
    used.

    INPUT PARAMETERS:
        State   -   structure which stores algorithm state
        D       -   diagonal of the approximate Hessian, array[0..N-1],
                    (if larger, only leading N elements are used).

    NOTE 1: D[i] should be positive. Exception will be thrown otherwise.

    NOTE 2: you should pass diagonal of approximate Hessian - NOT ITS INVERSE.

      -- ALGLIB --
         Copyright 13.10.2010 by Bochkanov Sergey
    *************************************************************************/
    public static void minbleicsetprecdiag(minbleicstate state, double[] d)
    {

        minbleic.minbleicsetprecdiag(state.innerobj, d);
        return;
    }

    /*************************************************************************
    Modification of the preconditioner: scale-based diagonal preconditioning.

    This preconditioning mode can be useful when you  don't  have  approximate
    diagonal of Hessian, but you know that your  variables  are  badly  scaled
    (for  example,  one  variable is in [1,10], and another in [1000,100000]),
    and most part of the ill-conditioning comes from different scales of vars.

    In this case simple  scale-based  preconditioner,  with H[i] = 1/(s[i]^2),
    can greatly improve convergence.

    IMPRTANT: you should set scale of your variables  with  MinBLEICSetScale()
    call  (before  or after MinBLEICSetPrecScale() call). Without knowledge of
    the scale of your variables scale-based preconditioner will be  just  unit
    matrix.

    INPUT PARAMETERS:
        State   -   structure which stores algorithm state

      -- ALGLIB --
         Copyright 13.10.2010 by Bochkanov Sergey
    *************************************************************************/
    public static void minbleicsetprecscale(minbleicstate state)
    {

        minbleic.minbleicsetprecscale(state.innerobj);
        return;
    }

    /*************************************************************************
    This function turns on/off reporting.

    INPUT PARAMETERS:
        State   -   structure which stores algorithm state
        NeedXRep-   whether iteration reports are needed or not

    If NeedXRep is True, algorithm will call rep() callback function if  it is
    provided to MinBLEICOptimize().

      -- ALGLIB --
         Copyright 28.11.2010 by Bochkanov Sergey
    *************************************************************************/
    public static void minbleicsetxrep(minbleicstate state, bool needxrep)
    {

        minbleic.minbleicsetxrep(state.innerobj, needxrep);
        return;
    }

    /*************************************************************************
    This function sets maximum step length

    IMPORTANT: this feature is hard to combine with preconditioning. You can't
    set upper limit on step length, when you solve optimization  problem  with
    linear (non-boundary) constraints AND preconditioner turned on.

    When  non-boundary  constraints  are  present,  you  have to either a) use
    preconditioner, or b) use upper limit on step length.  YOU CAN'T USE BOTH!
    In this case algorithm will terminate with appropriate error code.

    INPUT PARAMETERS:
        State   -   structure which stores algorithm state
        StpMax  -   maximum step length, >=0. Set StpMax to 0.0,  if you don't
                    want to limit step length.

    Use this subroutine when you optimize target function which contains exp()
    or  other  fast  growing  functions,  and optimization algorithm makes too
    large  steps  which  lead   to overflow. This function allows us to reject
    steps  that  are  too  large  (and  therefore  expose  us  to the possible
    overflow) without actually calculating function value at the x+stp*d.

      -- ALGLIB --
         Copyright 02.04.2010 by Bochkanov Sergey
    *************************************************************************/
    public static void minbleicsetstpmax(minbleicstate state, double stpmax)
    {

        minbleic.minbleicsetstpmax(state.innerobj, stpmax);
        return;
    }

    /*************************************************************************
    This function provides reverse communication interface
    Reverse communication interface is not documented or recommended to use.
    See below for functions which provide better documented API
    *************************************************************************/
    public static bool minbleiciteration(minbleicstate state)
    {

        bool result = minbleic.minbleiciteration(state.innerobj);
        return result;
    }
    /*************************************************************************
    This family of functions is used to launcn iterations of nonlinear optimizer

    These functions accept following parameters:
        func    -   callback which calculates function (or merit function)
                    value func at given point x
        grad    -   callback which calculates function (or merit function)
                    value func and gradient grad at given point x
        rep     -   optional callback which is called after each iteration
                    can be null
        obj     -   optional object which is passed to func/grad/hess/jac/rep
                    can be null

    NOTES:

    1. This function has two different implementations: one which  uses  exact
       (analytical) user-supplied gradient,  and one which uses function value
       only  and  numerically  differentiates  function  in  order  to  obtain
       gradient.

       Depending  on  the  specific  function  used to create optimizer object
       (either  MinBLEICCreate() for analytical gradient or  MinBLEICCreateF()
       for numerical differentiation) you should choose appropriate variant of
       MinBLEICOptimize() - one  which  accepts  function  AND gradient or one
       which accepts function ONLY.

       Be careful to choose variant of MinBLEICOptimize() which corresponds to
       your optimization scheme! Table below lists different  combinations  of
       callback (function/gradient) passed to MinBLEICOptimize()  and specific
       function used to create optimizer.


                         |         USER PASSED TO MinBLEICOptimize()
       CREATED WITH      |  function only   |  function and gradient
       ------------------------------------------------------------
       MinBLEICCreateF() |     work                FAIL
       MinBLEICCreate()  |     FAIL                work

       Here "FAIL" denotes inappropriate combinations  of  optimizer  creation
       function  and  MinBLEICOptimize()  version.   Attemps   to   use   such
       combination (for  example,  to  create optimizer with MinBLEICCreateF()
       and  to  pass  gradient  information  to  MinCGOptimize()) will lead to
       exception being thrown. Either  you  did  not pass gradient when it WAS
       needed or you passed gradient when it was NOT needed.

      -- ALGLIB --
         Copyright 28.11.2010 by Bochkanov Sergey

    *************************************************************************/
    public static void minbleicoptimize(minbleicstate state, ndimensional_func func, ndimensional_rep rep, object obj)
    {
        if( func==null )
            throw new alglibexception("ALGLIB: error in 'minbleicoptimize()' (func is null)");
        while( alglib.minbleiciteration(state) )
        {
            if( state.needf )
            {
                func(state.x, ref state.innerobj.f, obj);
                continue;
            }
            if( state.innerobj.xupdated )
            {
                if( rep!=null )
                    rep(state.innerobj.x, state.innerobj.f, obj);
                continue;
            }
            throw new alglibexception("ALGLIB: error in 'minbleicoptimize' (some derivatives were not provided?)");
        }
    }


    public static void minbleicoptimize(minbleicstate state, ndimensional_grad grad, ndimensional_rep rep, object obj)
    {
        if( grad==null )
            throw new alglibexception("ALGLIB: error in 'minbleicoptimize()' (grad is null)");
        while( alglib.minbleiciteration(state) )
        {
            if( state.needfg )
            {
                grad(state.x, ref state.innerobj.f, state.innerobj.g, obj);
                continue;
            }
            if( state.innerobj.xupdated )
            {
                if( rep!=null )
                    rep(state.innerobj.x, state.innerobj.f, obj);
                continue;
            }
            throw new alglibexception("ALGLIB: error in 'minbleicoptimize' (some derivatives were not provided?)");
        }
    }



    /*************************************************************************
    BLEIC results

    INPUT PARAMETERS:
        State   -   algorithm state

    OUTPUT PARAMETERS:
        X       -   array[0..N-1], solution
        Rep     -   optimization report. You should check Rep.TerminationType
                    in  order  to  distinguish  successful  termination  from
                    unsuccessful one:
                    * -7   gradient verification failed.
                           See MinBLEICSetGradientCheck() for more information.
                    * -3   inconsistent constraints. Feasible point is
                           either nonexistent or too hard to find. Try to
                           restart optimizer with better initial approximation
                    *  1   relative function improvement is no more than EpsF.
                    *  2   relative step is no more than EpsX.
                    *  4   gradient norm is no more than EpsG
                    *  5   MaxIts steps was taken
                    More information about fields of this  structure  can  be
                    found in the comments on MinBLEICReport datatype.

      -- ALGLIB --
         Copyright 28.11.2010 by Bochkanov Sergey
    *************************************************************************/
    public static void minbleicresults(minbleicstate state, out double[] x, out minbleicreport rep)
    {
        x = new double[0];
        rep = new minbleicreport();
        minbleic.minbleicresults(state.innerobj, ref x, rep.innerobj);
        return;
    }

    /*************************************************************************
    BLEIC results

    Buffered implementation of MinBLEICResults() which uses pre-allocated buffer
    to store X[]. If buffer size is  too  small,  it  resizes  buffer.  It  is
    intended to be used in the inner cycles of performance critical algorithms
    where array reallocation penalty is too large to be ignored.

      -- ALGLIB --
         Copyright 28.11.2010 by Bochkanov Sergey
    *************************************************************************/
    public static void minbleicresultsbuf(minbleicstate state, ref double[] x, minbleicreport rep)
    {

        minbleic.minbleicresultsbuf(state.innerobj, ref x, rep.innerobj);
        return;
    }

    /*************************************************************************
    This subroutine restarts algorithm from new point.
    All optimization parameters (including constraints) are left unchanged.

    This  function  allows  to  solve multiple  optimization  problems  (which
    must have  same number of dimensions) without object reallocation penalty.

    INPUT PARAMETERS:
        State   -   structure previously allocated with MinBLEICCreate call.
        X       -   new starting point.

      -- ALGLIB --
         Copyright 28.11.2010 by Bochkanov Sergey
    *************************************************************************/
    public static void minbleicrestartfrom(minbleicstate state, double[] x)
    {

        minbleic.minbleicrestartfrom(state.innerobj, x);
        return;
    }

    /*************************************************************************
    This  subroutine  turns  on  verification  of  the  user-supplied analytic
    gradient:
    * user calls this subroutine before optimization begins
    * MinBLEICOptimize() is called
    * prior to  actual  optimization, for each component  of  parameters being
      optimized X[i] algorithm performs following steps:
      * two trial steps are made to X[i]-TestStep*S[i] and X[i]+TestStep*S[i],
        where X[i] is i-th component of the initial point and S[i] is a  scale
        of i-th parameter
      * if needed, steps are bounded with respect to constraints on X[]
      * F(X) is evaluated at these trial points
      * we perform one more evaluation in the middle point of the interval
      * we  build  cubic  model using function values and derivatives at trial
        points and we compare its prediction with actual value in  the  middle
        point
      * in case difference between prediction and actual value is higher  than
        some predetermined threshold, algorithm stops with completion code -7;
        Rep.VarIdx is set to index of the parameter with incorrect derivative.
    * after verification is over, algorithm proceeds to the actual optimization.

    NOTE 1: verification  needs  N (parameters count) gradient evaluations. It
            is very costly and you should use  it  only  for  low  dimensional
            problems,  when  you  want  to  be  sure  that  you've   correctly
            calculated  analytic  derivatives.  You  should  not use it in the
            production code (unless you want to check derivatives provided  by
            some third party).

    NOTE 2: you  should  carefully  choose  TestStep. Value which is too large
            (so large that function behaviour is significantly non-cubic) will
            lead to false alarms. You may use  different  step  for  different
            parameters by means of setting scale with MinBLEICSetScale().

    NOTE 3: this function may lead to false positives. In case it reports that
            I-th  derivative was calculated incorrectly, you may decrease test
            step  and  try  one  more  time  - maybe your function changes too
            sharply  and  your  step  is  too  large for such rapidly chanding
            function.

    INPUT PARAMETERS:
        State       -   structure used to store algorithm state
        TestStep    -   verification step:
                        * TestStep=0 turns verification off
                        * TestStep>0 activates verification

      -- ALGLIB --
         Copyright 15.06.2012 by Bochkanov Sergey
    *************************************************************************/
    public static void minbleicsetgradientcheck(minbleicstate state, double teststep)
    {

        minbleic.minbleicsetgradientcheck(state.innerobj, teststep);
        return;
    }

}
public partial class alglib
{


    /*************************************************************************

    *************************************************************************/
    public class minlbfgsstate
    {
        //
        // Public declarations
        //
        public bool needf { get { return _innerobj.needf; } set { _innerobj.needf = value; } }
        public bool needfg { get { return _innerobj.needfg; } set { _innerobj.needfg = value; } }
        public bool xupdated { get { return _innerobj.xupdated; } set { _innerobj.xupdated = value; } }
        public double f { get { return _innerobj.f; } set { _innerobj.f = value; } }
        public double[] g { get { return _innerobj.g; } }
        public double[] x { get { return _innerobj.x; } }

        public minlbfgsstate()
        {
            _innerobj = new minlbfgs.minlbfgsstate();
        }

        //
        // Although some of declarations below are public, you should not use them
        // They are intended for internal use only
        //
        private minlbfgs.minlbfgsstate _innerobj;
        public minlbfgs.minlbfgsstate innerobj { get { return _innerobj; } }
        public minlbfgsstate(minlbfgs.minlbfgsstate obj)
        {
            _innerobj = obj;
        }
    }


    /*************************************************************************

    *************************************************************************/
    public class minlbfgsreport
    {
        //
        // Public declarations
        //
        public int iterationscount { get { return _innerobj.iterationscount; } set { _innerobj.iterationscount = value; } }
        public int nfev { get { return _innerobj.nfev; } set { _innerobj.nfev = value; } }
        public int varidx { get { return _innerobj.varidx; } set { _innerobj.varidx = value; } }
        public int terminationtype { get { return _innerobj.terminationtype; } set { _innerobj.terminationtype = value; } }

        public minlbfgsreport()
        {
            _innerobj = new minlbfgs.minlbfgsreport();
        }

        //
        // Although some of declarations below are public, you should not use them
        // They are intended for internal use only
        //
        private minlbfgs.minlbfgsreport _innerobj;
        public minlbfgs.minlbfgsreport innerobj { get { return _innerobj; } }
        public minlbfgsreport(minlbfgs.minlbfgsreport obj)
        {
            _innerobj = obj;
        }
    }

    /*************************************************************************
            LIMITED MEMORY BFGS METHOD FOR LARGE SCALE OPTIMIZATION

    DESCRIPTION:
    The subroutine minimizes function F(x) of N arguments by  using  a  quasi-
    Newton method (LBFGS scheme) which is optimized to use  a  minimum  amount
    of memory.
    The subroutine generates the approximation of an inverse Hessian matrix by
    using information about the last M steps of the algorithm  (instead of N).
    It lessens a required amount of memory from a value  of  order  N^2  to  a
    value of order 2*N*M.


    REQUIREMENTS:
    Algorithm will request following information during its operation:
    * function value F and its gradient G (simultaneously) at given point X


    USAGE:
    1. User initializes algorithm state with MinLBFGSCreate() call
    2. User tunes solver parameters with MinLBFGSSetCond() MinLBFGSSetStpMax()
       and other functions
    3. User calls MinLBFGSOptimize() function which takes algorithm  state and
       pointer (delegate, etc.) to callback function which calculates F/G.
    4. User calls MinLBFGSResults() to get solution
    5. Optionally user may call MinLBFGSRestartFrom() to solve another problem
       with same N/M but another starting point and/or another function.
       MinLBFGSRestartFrom() allows to reuse already initialized structure.


    INPUT PARAMETERS:
        N       -   problem dimension. N>0
        M       -   number of corrections in the BFGS scheme of Hessian
                    approximation update. Recommended value:  3<=M<=7. The smaller
                    value causes worse convergence, the bigger will  not  cause  a
                    considerably better convergence, but will cause a fall in  the
                    performance. M<=N.
        X       -   initial solution approximation, array[0..N-1].


    OUTPUT PARAMETERS:
        State   -   structure which stores algorithm state


    NOTES:
    1. you may tune stopping conditions with MinLBFGSSetCond() function
    2. if target function contains exp() or other fast growing functions,  and
       optimization algorithm makes too large steps which leads  to  overflow,
       use MinLBFGSSetStpMax() function to bound algorithm's  steps.  However,
       L-BFGS rarely needs such a tuning.


      -- ALGLIB --
         Copyright 02.04.2010 by Bochkanov Sergey
    *************************************************************************/
    public static void minlbfgscreate(int n, int m, double[] x, out minlbfgsstate state)
    {
        state = new minlbfgsstate();
        minlbfgs.minlbfgscreate(n, m, x, state.innerobj);
        return;
    }
    public static void minlbfgscreate(int m, double[] x, out minlbfgsstate state)
    {
        int n;

        state = new minlbfgsstate();
        n = ap.len(x);
        minlbfgs.minlbfgscreate(n, m, x, state.innerobj);

        return;
    }

    /*************************************************************************
    The subroutine is finite difference variant of MinLBFGSCreate().  It  uses
    finite differences in order to differentiate target function.

    Description below contains information which is specific to  this function
    only. We recommend to read comments on MinLBFGSCreate() in  order  to  get
    more information about creation of LBFGS optimizer.

    INPUT PARAMETERS:
        N       -   problem dimension, N>0:
                    * if given, only leading N elements of X are used
                    * if not given, automatically determined from size of X
        M       -   number of corrections in the BFGS scheme of Hessian
                    approximation update. Recommended value:  3<=M<=7. The smaller
                    value causes worse convergence, the bigger will  not  cause  a
                    considerably better convergence, but will cause a fall in  the
                    performance. M<=N.
        X       -   starting point, array[0..N-1].
        DiffStep-   differentiation step, >0

    OUTPUT PARAMETERS:
        State   -   structure which stores algorithm state

    NOTES:
    1. algorithm uses 4-point central formula for differentiation.
    2. differentiation step along I-th axis is equal to DiffStep*S[I] where
       S[] is scaling vector which can be set by MinLBFGSSetScale() call.
    3. we recommend you to use moderate values of  differentiation  step.  Too
       large step will result in too large truncation  errors, while too small
       step will result in too large numerical  errors.  1.0E-6  can  be  good
       value to start with.
    4. Numerical  differentiation  is   very   inefficient  -   one   gradient
       calculation needs 4*N function evaluations. This function will work for
       any N - either small (1...10), moderate (10...100) or  large  (100...).
       However, performance penalty will be too severe for any N's except  for
       small ones.
       We should also say that code which relies on numerical  differentiation
       is   less  robust  and  precise.  LBFGS  needs  exact  gradient values.
       Imprecise gradient may slow  down  convergence,  especially  on  highly
       nonlinear problems.
       Thus  we  recommend to use this function for fast prototyping on small-
       dimensional problems only, and to implement analytical gradient as soon
       as possible.

      -- ALGLIB --
         Copyright 16.05.2011 by Bochkanov Sergey
    *************************************************************************/
    public static void minlbfgscreatef(int n, int m, double[] x, double diffstep, out minlbfgsstate state)
    {
        state = new minlbfgsstate();
        minlbfgs.minlbfgscreatef(n, m, x, diffstep, state.innerobj);
        return;
    }
    public static void minlbfgscreatef(int m, double[] x, double diffstep, out minlbfgsstate state)
    {
        int n;

        state = new minlbfgsstate();
        n = ap.len(x);
        minlbfgs.minlbfgscreatef(n, m, x, diffstep, state.innerobj);

        return;
    }

    /*************************************************************************
    This function sets stopping conditions for L-BFGS optimization algorithm.

    INPUT PARAMETERS:
        State   -   structure which stores algorithm state
        EpsG    -   >=0
                    The  subroutine  finishes  its  work   if   the  condition
                    |v|<EpsG is satisfied, where:
                    * |.| means Euclidian norm
                    * v - scaled gradient vector, v[i]=g[i]*s[i]
                    * g - gradient
                    * s - scaling coefficients set by MinLBFGSSetScale()
        EpsF    -   >=0
                    The  subroutine  finishes  its work if on k+1-th iteration
                    the  condition  |F(k+1)-F(k)|<=EpsF*max{|F(k)|,|F(k+1)|,1}
                    is satisfied.
        EpsX    -   >=0
                    The subroutine finishes its work if  on  k+1-th  iteration
                    the condition |v|<=EpsX is fulfilled, where:
                    * |.| means Euclidian norm
                    * v - scaled step vector, v[i]=dx[i]/s[i]
                    * dx - ste pvector, dx=X(k+1)-X(k)
                    * s - scaling coefficients set by MinLBFGSSetScale()
        MaxIts  -   maximum number of iterations. If MaxIts=0, the  number  of
                    iterations is unlimited.

    Passing EpsG=0, EpsF=0, EpsX=0 and MaxIts=0 (simultaneously) will lead to
    automatic stopping criterion selection (small EpsX).

      -- ALGLIB --
         Copyright 02.04.2010 by Bochkanov Sergey
    *************************************************************************/
    public static void minlbfgssetcond(minlbfgsstate state, double epsg, double epsf, double epsx, int maxits)
    {

        minlbfgs.minlbfgssetcond(state.innerobj, epsg, epsf, epsx, maxits);
        return;
    }

    /*************************************************************************
    This function turns on/off reporting.

    INPUT PARAMETERS:
        State   -   structure which stores algorithm state
        NeedXRep-   whether iteration reports are needed or not

    If NeedXRep is True, algorithm will call rep() callback function if  it is
    provided to MinLBFGSOptimize().


      -- ALGLIB --
         Copyright 02.04.2010 by Bochkanov Sergey
    *************************************************************************/
    public static void minlbfgssetxrep(minlbfgsstate state, bool needxrep)
    {

        minlbfgs.minlbfgssetxrep(state.innerobj, needxrep);
        return;
    }

    /*************************************************************************
    This function sets maximum step length

    INPUT PARAMETERS:
        State   -   structure which stores algorithm state
        StpMax  -   maximum step length, >=0. Set StpMax to 0.0 (default),  if
                    you don't want to limit step length.

    Use this subroutine when you optimize target function which contains exp()
    or  other  fast  growing  functions,  and optimization algorithm makes too
    large  steps  which  leads  to overflow. This function allows us to reject
    steps  that  are  too  large  (and  therefore  expose  us  to the possible
    overflow) without actually calculating function value at the x+stp*d.

      -- ALGLIB --
         Copyright 02.04.2010 by Bochkanov Sergey
    *************************************************************************/
    public static void minlbfgssetstpmax(minlbfgsstate state, double stpmax)
    {

        minlbfgs.minlbfgssetstpmax(state.innerobj, stpmax);
        return;
    }

    /*************************************************************************
    This function sets scaling coefficients for LBFGS optimizer.

    ALGLIB optimizers use scaling matrices to test stopping  conditions  (step
    size and gradient are scaled before comparison with tolerances).  Scale of
    the I-th variable is a translation invariant measure of:
    a) "how large" the variable is
    b) how large the step should be to make significant changes in the function

    Scaling is also used by finite difference variant of the optimizer  - step
    along I-th axis is equal to DiffStep*S[I].

    In  most  optimizers  (and  in  the  LBFGS  too)  scaling is NOT a form of
    preconditioning. It just  affects  stopping  conditions.  You  should  set
    preconditioner  by  separate  call  to  one  of  the  MinLBFGSSetPrec...()
    functions.

    There  is  special  preconditioning  mode, however,  which  uses   scaling
    coefficients to form diagonal preconditioning matrix. You  can  turn  this
    mode on, if you want.   But  you should understand that scaling is not the
    same thing as preconditioning - these are two different, although  related
    forms of tuning solver.

    INPUT PARAMETERS:
        State   -   structure stores algorithm state
        S       -   array[N], non-zero scaling coefficients
                    S[i] may be negative, sign doesn't matter.

      -- ALGLIB --
         Copyright 14.01.2011 by Bochkanov Sergey
    *************************************************************************/
    public static void minlbfgssetscale(minlbfgsstate state, double[] s)
    {

        minlbfgs.minlbfgssetscale(state.innerobj, s);
        return;
    }

    /*************************************************************************
    Modification  of  the  preconditioner:  default  preconditioner    (simple
    scaling, same for all elements of X) is used.

    INPUT PARAMETERS:
        State   -   structure which stores algorithm state

    NOTE:  you  can  change  preconditioner  "on  the  fly",  during algorithm
    iterations.

      -- ALGLIB --
         Copyright 13.10.2010 by Bochkanov Sergey
    *************************************************************************/
    public static void minlbfgssetprecdefault(minlbfgsstate state)
    {

        minlbfgs.minlbfgssetprecdefault(state.innerobj);
        return;
    }

    /*************************************************************************
    Modification of the preconditioner: Cholesky factorization of  approximate
    Hessian is used.

    INPUT PARAMETERS:
        State   -   structure which stores algorithm state
        P       -   triangular preconditioner, Cholesky factorization of
                    the approximate Hessian. array[0..N-1,0..N-1],
                    (if larger, only leading N elements are used).
        IsUpper -   whether upper or lower triangle of P is given
                    (other triangle is not referenced)

    After call to this function preconditioner is changed to P  (P  is  copied
    into the internal buffer).

    NOTE:  you  can  change  preconditioner  "on  the  fly",  during algorithm
    iterations.

    NOTE 2:  P  should  be nonsingular. Exception will be thrown otherwise.

      -- ALGLIB --
         Copyright 13.10.2010 by Bochkanov Sergey
    *************************************************************************/
    public static void minlbfgssetpreccholesky(minlbfgsstate state, double[,] p, bool isupper)
    {

        minlbfgs.minlbfgssetpreccholesky(state.innerobj, p, isupper);
        return;
    }

    /*************************************************************************
    Modification  of  the  preconditioner:  diagonal of approximate Hessian is
    used.

    INPUT PARAMETERS:
        State   -   structure which stores algorithm state
        D       -   diagonal of the approximate Hessian, array[0..N-1],
                    (if larger, only leading N elements are used).

    NOTE:  you  can  change  preconditioner  "on  the  fly",  during algorithm
    iterations.

    NOTE 2: D[i] should be positive. Exception will be thrown otherwise.

    NOTE 3: you should pass diagonal of approximate Hessian - NOT ITS INVERSE.

      -- ALGLIB --
         Copyright 13.10.2010 by Bochkanov Sergey
    *************************************************************************/
    public static void minlbfgssetprecdiag(minlbfgsstate state, double[] d)
    {

        minlbfgs.minlbfgssetprecdiag(state.innerobj, d);
        return;
    }

    /*************************************************************************
    Modification of the preconditioner: scale-based diagonal preconditioning.

    This preconditioning mode can be useful when you  don't  have  approximate
    diagonal of Hessian, but you know that your  variables  are  badly  scaled
    (for  example,  one  variable is in [1,10], and another in [1000,100000]),
    and most part of the ill-conditioning comes from different scales of vars.

    In this case simple  scale-based  preconditioner,  with H[i] = 1/(s[i]^2),
    can greatly improve convergence.

    IMPRTANT: you should set scale of your variables  with  MinLBFGSSetScale()
    call  (before  or after MinLBFGSSetPrecScale() call). Without knowledge of
    the scale of your variables scale-based preconditioner will be  just  unit
    matrix.

    INPUT PARAMETERS:
        State   -   structure which stores algorithm state

      -- ALGLIB --
         Copyright 13.10.2010 by Bochkanov Sergey
    *************************************************************************/
    public static void minlbfgssetprecscale(minlbfgsstate state)
    {

        minlbfgs.minlbfgssetprecscale(state.innerobj);
        return;
    }

    /*************************************************************************
    This function provides reverse communication interface
    Reverse communication interface is not documented or recommended to use.
    See below for functions which provide better documented API
    *************************************************************************/
    public static bool minlbfgsiteration(minlbfgsstate state)
    {

        bool result = minlbfgs.minlbfgsiteration(state.innerobj);
        return result;
    }
    /*************************************************************************
    This family of functions is used to launcn iterations of nonlinear optimizer

    These functions accept following parameters:
        func    -   callback which calculates function (or merit function)
                    value func at given point x
        grad    -   callback which calculates function (or merit function)
                    value func and gradient grad at given point x
        rep     -   optional callback which is called after each iteration
                    can be null
        obj     -   optional object which is passed to func/grad/hess/jac/rep
                    can be null

    NOTES:

    1. This function has two different implementations: one which  uses  exact
       (analytical) user-supplied gradient,  and one which uses function value
       only  and  numerically  differentiates  function  in  order  to  obtain
       gradient.

       Depending  on  the  specific  function  used to create optimizer object
       (either MinLBFGSCreate() for analytical gradient  or  MinLBFGSCreateF()
       for numerical differentiation) you should choose appropriate variant of
       MinLBFGSOptimize() - one  which  accepts  function  AND gradient or one
       which accepts function ONLY.

       Be careful to choose variant of MinLBFGSOptimize() which corresponds to
       your optimization scheme! Table below lists different  combinations  of
       callback (function/gradient) passed to MinLBFGSOptimize()  and specific
       function used to create optimizer.


                         |         USER PASSED TO MinLBFGSOptimize()
       CREATED WITH      |  function only   |  function and gradient
       ------------------------------------------------------------
       MinLBFGSCreateF() |     work                FAIL
       MinLBFGSCreate()  |     FAIL                work

       Here "FAIL" denotes inappropriate combinations  of  optimizer  creation
       function  and  MinLBFGSOptimize()  version.   Attemps   to   use   such
       combination (for example, to create optimizer with MinLBFGSCreateF() and
       to pass gradient information to MinCGOptimize()) will lead to exception
       being thrown. Either  you  did  not pass gradient when it WAS needed or
       you passed gradient when it was NOT needed.

      -- ALGLIB --
         Copyright 20.03.2009 by Bochkanov Sergey

    *************************************************************************/
    public static void minlbfgsoptimize(minlbfgsstate state, ndimensional_func func, ndimensional_rep rep, object obj)
    {
        if( func==null )
            throw new alglibexception("ALGLIB: error in 'minlbfgsoptimize()' (func is null)");
        while( alglib.minlbfgsiteration(state) )
        {
            if( state.needf )
            {
                func(state.x, ref state.innerobj.f, obj);
                continue;
            }
            if( state.innerobj.xupdated )
            {
                if( rep!=null )
                    rep(state.innerobj.x, state.innerobj.f, obj);
                continue;
            }
            throw new alglibexception("ALGLIB: error in 'minlbfgsoptimize' (some derivatives were not provided?)");
        }
    }


    public static void minlbfgsoptimize(minlbfgsstate state, ndimensional_grad grad, ndimensional_rep rep, object obj)
    {
        if( grad==null )
            throw new alglibexception("ALGLIB: error in 'minlbfgsoptimize()' (grad is null)");
        while( alglib.minlbfgsiteration(state) )
        {
            if( state.needfg )
            {
                grad(state.x, ref state.innerobj.f, state.innerobj.g, obj);
                continue;
            }
            if( state.innerobj.xupdated )
            {
                if( rep!=null )
                    rep(state.innerobj.x, state.innerobj.f, obj);
                continue;
            }
            throw new alglibexception("ALGLIB: error in 'minlbfgsoptimize' (some derivatives were not provided?)");
        }
    }



    /*************************************************************************
    L-BFGS algorithm results

    INPUT PARAMETERS:
        State   -   algorithm state

    OUTPUT PARAMETERS:
        X       -   array[0..N-1], solution
        Rep     -   optimization report:
                    * Rep.TerminationType completetion code:
                        * -7    gradient verification failed.
                                See MinLBFGSSetGradientCheck() for more information.
                        * -2    rounding errors prevent further improvement.
                                X contains best point found.
                        * -1    incorrect parameters were specified
                        *  1    relative function improvement is no more than
                                EpsF.
                        *  2    relative step is no more than EpsX.
                        *  4    gradient norm is no more than EpsG
                        *  5    MaxIts steps was taken
                        *  7    stopping conditions are too stringent,
                                further improvement is impossible
                    * Rep.IterationsCount contains iterations count
                    * NFEV countains number of function calculations

      -- ALGLIB --
         Copyright 02.04.2010 by Bochkanov Sergey
    *************************************************************************/
    public static void minlbfgsresults(minlbfgsstate state, out double[] x, out minlbfgsreport rep)
    {
        x = new double[0];
        rep = new minlbfgsreport();
        minlbfgs.minlbfgsresults(state.innerobj, ref x, rep.innerobj);
        return;
    }

    /*************************************************************************
    L-BFGS algorithm results

    Buffered implementation of MinLBFGSResults which uses pre-allocated buffer
    to store X[]. If buffer size is  too  small,  it  resizes  buffer.  It  is
    intended to be used in the inner cycles of performance critical algorithms
    where array reallocation penalty is too large to be ignored.

      -- ALGLIB --
         Copyright 20.08.2010 by Bochkanov Sergey
    *************************************************************************/
    public static void minlbfgsresultsbuf(minlbfgsstate state, ref double[] x, minlbfgsreport rep)
    {

        minlbfgs.minlbfgsresultsbuf(state.innerobj, ref x, rep.innerobj);
        return;
    }

    /*************************************************************************
    This  subroutine restarts LBFGS algorithm from new point. All optimization
    parameters are left unchanged.

    This  function  allows  to  solve multiple  optimization  problems  (which
    must have same number of dimensions) without object reallocation penalty.

    INPUT PARAMETERS:
        State   -   structure used to store algorithm state
        X       -   new starting point.

      -- ALGLIB --
         Copyright 30.07.2010 by Bochkanov Sergey
    *************************************************************************/
    public static void minlbfgsrestartfrom(minlbfgsstate state, double[] x)
    {

        minlbfgs.minlbfgsrestartfrom(state.innerobj, x);
        return;
    }

    /*************************************************************************
    This  subroutine  turns  on  verification  of  the  user-supplied analytic
    gradient:
    * user calls this subroutine before optimization begins
    * MinLBFGSOptimize() is called
    * prior to  actual  optimization, for each component  of  parameters being
      optimized X[i] algorithm performs following steps:
      * two trial steps are made to X[i]-TestStep*S[i] and X[i]+TestStep*S[i],
        where X[i] is i-th component of the initial point and S[i] is a  scale
        of i-th parameter
      * if needed, steps are bounded with respect to constraints on X[]
      * F(X) is evaluated at these trial points
      * we perform one more evaluation in the middle point of the interval
      * we  build  cubic  model using function values and derivatives at trial
        points and we compare its prediction with actual value in  the  middle
        point
      * in case difference between prediction and actual value is higher  than
        some predetermined threshold, algorithm stops with completion code -7;
        Rep.VarIdx is set to index of the parameter with incorrect derivative.
    * after verification is over, algorithm proceeds to the actual optimization.

    NOTE 1: verification  needs  N (parameters count) gradient evaluations. It
            is very costly and you should use  it  only  for  low  dimensional
            problems,  when  you  want  to  be  sure  that  you've   correctly
            calculated  analytic  derivatives.  You  should  not use it in the
            production code (unless you want to check derivatives provided  by
            some third party).

    NOTE 2: you  should  carefully  choose  TestStep. Value which is too large
            (so large that function behaviour is significantly non-cubic) will
            lead to false alarms. You may use  different  step  for  different
            parameters by means of setting scale with MinLBFGSSetScale().

    NOTE 3: this function may lead to false positives. In case it reports that
            I-th  derivative was calculated incorrectly, you may decrease test
            step  and  try  one  more  time  - maybe your function changes too
            sharply  and  your  step  is  too  large for such rapidly chanding
            function.

    INPUT PARAMETERS:
        State       -   structure used to store algorithm state
        TestStep    -   verification step:
                        * TestStep=0 turns verification off
                        * TestStep>0 activates verification

      -- ALGLIB --
         Copyright 24.05.2012 by Bochkanov Sergey
    *************************************************************************/
    public static void minlbfgssetgradientcheck(minlbfgsstate state, double teststep)
    {

        minlbfgs.minlbfgssetgradientcheck(state.innerobj, teststep);
        return;
    }

}
public partial class alglib
{


    /*************************************************************************
    This object stores nonlinear optimizer state.
    You should use functions provided by MinQP subpackage to work with this
    object
    *************************************************************************/
    public class minqpstate
    {
        //
        // Public declarations
        //

        public minqpstate()
        {
            _innerobj = new minqp.minqpstate();
        }

        //
        // Although some of declarations below are public, you should not use them
        // They are intended for internal use only
        //
        private minqp.minqpstate _innerobj;
        public minqp.minqpstate innerobj { get { return _innerobj; } }
        public minqpstate(minqp.minqpstate obj)
        {
            _innerobj = obj;
        }
    }


    /*************************************************************************
    This structure stores optimization report:
    * InnerIterationsCount      number of inner iterations
    * OuterIterationsCount      number of outer iterations
    * NCholesky                 number of Cholesky decomposition
    * NMV                       number of matrix-vector products
                                (only products calculated as part of iterative
                                process are counted)
    * TerminationType           completion code (see below)

    Completion codes:
    * -5    inappropriate solver was used:
            * Cholesky solver for semidefinite or indefinite problems
            * Cholesky solver for problems with non-boundary constraints
    * -3    inconsistent constraints (or, maybe, feasible point is
            too hard to find). If you are sure that constraints are feasible,
            try to restart optimizer with better initial approximation.
    * -1    solver error
    *  4    successful completion
    *  5    MaxIts steps was taken
    *  7    stopping conditions are too stringent,
            further improvement is impossible,
            X contains best point found so far.
    *************************************************************************/
    public class minqpreport
    {
        //
        // Public declarations
        //
        public int inneriterationscount { get { return _innerobj.inneriterationscount; } set { _innerobj.inneriterationscount = value; } }
        public int outeriterationscount { get { return _innerobj.outeriterationscount; } set { _innerobj.outeriterationscount = value; } }
        public int nmv { get { return _innerobj.nmv; } set { _innerobj.nmv = value; } }
        public int ncholesky { get { return _innerobj.ncholesky; } set { _innerobj.ncholesky = value; } }
        public int terminationtype { get { return _innerobj.terminationtype; } set { _innerobj.terminationtype = value; } }

        public minqpreport()
        {
            _innerobj = new minqp.minqpreport();
        }

        //
        // Although some of declarations below are public, you should not use them
        // They are intended for internal use only
        //
        private minqp.minqpreport _innerobj;
        public minqp.minqpreport innerobj { get { return _innerobj; } }
        public minqpreport(minqp.minqpreport obj)
        {
            _innerobj = obj;
        }
    }

    /*************************************************************************
                        CONSTRAINED QUADRATIC PROGRAMMING

    The subroutine creates QP optimizer. After initial creation,  it  contains
    default optimization problem with zero quadratic and linear terms  and  no
    constraints. You should set quadratic/linear terms with calls to functions
    provided by MinQP subpackage.

    INPUT PARAMETERS:
        N       -   problem size

    OUTPUT PARAMETERS:
        State   -   optimizer with zero quadratic/linear terms
                    and no constraints

      -- ALGLIB --
         Copyright 11.01.2011 by Bochkanov Sergey
    *************************************************************************/
    public static void minqpcreate(int n, out minqpstate state)
    {
        state = new minqpstate();
        minqp.minqpcreate(n, state.innerobj);
        return;
    }

    /*************************************************************************
    This function sets linear term for QP solver.

    By default, linear term is zero.

    INPUT PARAMETERS:
        State   -   structure which stores algorithm state
        B       -   linear term, array[N].

      -- ALGLIB --
         Copyright 11.01.2011 by Bochkanov Sergey
    *************************************************************************/
    public static void minqpsetlinearterm(minqpstate state, double[] b)
    {

        minqp.minqpsetlinearterm(state.innerobj, b);
        return;
    }

    /*************************************************************************
    This function sets quadratic term for QP solver.

    By default quadratic term is zero.

    IMPORTANT: this solver minimizes following  function:
        f(x) = 0.5*x'*A*x + b'*x.
    Note that quadratic term has 0.5 before it. So if  you  want  to  minimize
        f(x) = x^2 + x
    you should rewrite your problem as follows:
        f(x) = 0.5*(2*x^2) + x
    and your matrix A will be equal to [[2.0]], not to [[1.0]]

    INPUT PARAMETERS:
        State   -   structure which stores algorithm state
        A       -   matrix, array[N,N]
        IsUpper -   (optional) storage type:
                    * if True, symmetric matrix  A  is  given  by  its  upper
                      triangle, and the lower triangle isn’t used
                    * if False, symmetric matrix  A  is  given  by  its lower
                      triangle, and the upper triangle isn’t used
                    * if not given, both lower and upper  triangles  must  be
                      filled.

      -- ALGLIB --
         Copyright 11.01.2011 by Bochkanov Sergey
    *************************************************************************/
    public static void minqpsetquadraticterm(minqpstate state, double[,] a, bool isupper)
    {

        minqp.minqpsetquadraticterm(state.innerobj, a, isupper);
        return;
    }
    public static void minqpsetquadraticterm(minqpstate state, double[,] a)
    {
        bool isupper;
        if( !alglib.ap.issymmetric(a) )
            throw new alglibexception("'a' parameter is not symmetric matrix");

        isupper = false;
        minqp.minqpsetquadraticterm(state.innerobj, a, isupper);

        return;
    }

    /*************************************************************************
    This function sets starting point for QP solver. It is useful to have
    good initial approximation to the solution, because it will increase
    speed of convergence and identification of active constraints.

    INPUT PARAMETERS:
        State   -   structure which stores algorithm state
        X       -   starting point, array[N].

      -- ALGLIB --
         Copyright 11.01.2011 by Bochkanov Sergey
    *************************************************************************/
    public static void minqpsetstartingpoint(minqpstate state, double[] x)
    {

        minqp.minqpsetstartingpoint(state.innerobj, x);
        return;
    }

    /*************************************************************************
    This  function sets origin for QP solver. By default, following QP program
    is solved:

        min(0.5*x'*A*x+b'*x)

    This function allows to solve different problem:

        min(0.5*(x-x_origin)'*A*(x-x_origin)+b'*(x-x_origin))

    INPUT PARAMETERS:
        State   -   structure which stores algorithm state
        XOrigin -   origin, array[N].

      -- ALGLIB --
         Copyright 11.01.2011 by Bochkanov Sergey
    *************************************************************************/
    public static void minqpsetorigin(minqpstate state, double[] xorigin)
    {

        minqp.minqpsetorigin(state.innerobj, xorigin);
        return;
    }

    /*************************************************************************
    This function tells solver to use Cholesky-based algorithm.

    Cholesky-based algorithm can be used when:
    * problem is convex
    * there is no constraints or only boundary constraints are present

    This algorithm has O(N^3) complexity for unconstrained problem and  is  up
    to several times slower on bound constrained  problems  (these  additional
    iterations are needed to identify active constraints).

    INPUT PARAMETERS:
        State   -   structure which stores algorithm state

      -- ALGLIB --
         Copyright 11.01.2011 by Bochkanov Sergey
    *************************************************************************/
    public static void minqpsetalgocholesky(minqpstate state)
    {

        minqp.minqpsetalgocholesky(state.innerobj);
        return;
    }

    /*************************************************************************
    This function sets boundary constraints for QP solver

    Boundary constraints are inactive by default (after initial creation).
    After  being  set,  they  are  preserved  until explicitly turned off with
    another SetBC() call.

    INPUT PARAMETERS:
        State   -   structure stores algorithm state
        BndL    -   lower bounds, array[N].
                    If some (all) variables are unbounded, you may specify
                    very small number or -INF (latter is recommended because
                    it will allow solver to use better algorithm).
        BndU    -   upper bounds, array[N].
                    If some (all) variables are unbounded, you may specify
                    very large number or +INF (latter is recommended because
                    it will allow solver to use better algorithm).

    NOTE: it is possible to specify BndL[i]=BndU[i]. In this case I-th
    variable will be "frozen" at X[i]=BndL[i]=BndU[i].

      -- ALGLIB --
         Copyright 11.01.2011 by Bochkanov Sergey
    *************************************************************************/
    public static void minqpsetbc(minqpstate state, double[] bndl, double[] bndu)
    {

        minqp.minqpsetbc(state.innerobj, bndl, bndu);
        return;
    }

    /*************************************************************************
    This function sets linear constraints for QP optimizer.

    Linear constraints are inactive by default (after initial creation).

    INPUT PARAMETERS:
        State   -   structure previously allocated with MinQPCreate call.
        C       -   linear constraints, array[K,N+1].
                    Each row of C represents one constraint, either equality
                    or inequality (see below):
                    * first N elements correspond to coefficients,
                    * last element corresponds to the right part.
                    All elements of C (including right part) must be finite.
        CT      -   type of constraints, array[K]:
                    * if CT[i]>0, then I-th constraint is C[i,*]*x >= C[i,n+1]
                    * if CT[i]=0, then I-th constraint is C[i,*]*x  = C[i,n+1]
                    * if CT[i]<0, then I-th constraint is C[i,*]*x <= C[i,n+1]
        K       -   number of equality/inequality constraints, K>=0:
                    * if given, only leading K elements of C/CT are used
                    * if not given, automatically determined from sizes of C/CT

    NOTE 1: linear (non-bound) constraints are satisfied only approximately  -
            there always exists some minor violation (about 10^-10...10^-13)
            due to numerical errors.

      -- ALGLIB --
         Copyright 19.06.2012 by Bochkanov Sergey
    *************************************************************************/
    public static void minqpsetlc(minqpstate state, double[,] c, int[] ct, int k)
    {

        minqp.minqpsetlc(state.innerobj, c, ct, k);
        return;
    }
    public static void minqpsetlc(minqpstate state, double[,] c, int[] ct)
    {
        int k;
        if( (ap.rows(c)!=ap.len(ct)))
            throw new alglibexception("Error while calling 'minqpsetlc': looks like one of arguments has wrong size");

        k = ap.rows(c);
        minqp.minqpsetlc(state.innerobj, c, ct, k);

        return;
    }

    /*************************************************************************
    This function solves quadratic programming problem.
    You should call it after setting solver options with MinQPSet...() calls.

    INPUT PARAMETERS:
        State   -   algorithm state

    You should use MinQPResults() function to access results after calls
    to this function.

      -- ALGLIB --
         Copyright 11.01.2011 by Bochkanov Sergey.
         Special thanks to Elvira Illarionova  for  important  suggestions  on
         the linearly constrained QP algorithm.
    *************************************************************************/
    public static void minqpoptimize(minqpstate state)
    {

        minqp.minqpoptimize(state.innerobj);
        return;
    }

    /*************************************************************************
    QP solver results

    INPUT PARAMETERS:
        State   -   algorithm state

    OUTPUT PARAMETERS:
        X       -   array[0..N-1], solution
        Rep     -   optimization report. You should check Rep.TerminationType,
                    which contains completion code, and you may check  another
                    fields which contain another information  about  algorithm
                    functioning.

      -- ALGLIB --
         Copyright 11.01.2011 by Bochkanov Sergey
    *************************************************************************/
    public static void minqpresults(minqpstate state, out double[] x, out minqpreport rep)
    {
        x = new double[0];
        rep = new minqpreport();
        minqp.minqpresults(state.innerobj, ref x, rep.innerobj);
        return;
    }

    /*************************************************************************
    QP results

    Buffered implementation of MinQPResults() which uses pre-allocated  buffer
    to store X[]. If buffer size is  too  small,  it  resizes  buffer.  It  is
    intended to be used in the inner cycles of performance critical algorithms
    where array reallocation penalty is too large to be ignored.

      -- ALGLIB --
         Copyright 11.01.2011 by Bochkanov Sergey
    *************************************************************************/
    public static void minqpresultsbuf(minqpstate state, ref double[] x, minqpreport rep)
    {

        minqp.minqpresultsbuf(state.innerobj, ref x, rep.innerobj);
        return;
    }

}
public partial class alglib
{


    /*************************************************************************
    Levenberg-Marquardt optimizer.

    This structure should be created using one of the MinLMCreate???()
    functions. You should not access its fields directly; use ALGLIB functions
    to work with it.
    *************************************************************************/
    public class minlmstate
    {
        //
        // Public declarations
        //
        public bool needf { get { return _innerobj.needf; } set { _innerobj.needf = value; } }
        public bool needfg { get { return _innerobj.needfg; } set { _innerobj.needfg = value; } }
        public bool needfgh { get { return _innerobj.needfgh; } set { _innerobj.needfgh = value; } }
        public bool needfi { get { return _innerobj.needfi; } set { _innerobj.needfi = value; } }
        public bool needfij { get { return _innerobj.needfij; } set { _innerobj.needfij = value; } }
        public bool xupdated { get { return _innerobj.xupdated; } set { _innerobj.xupdated = value; } }
        public double f { get { return _innerobj.f; } set { _innerobj.f = value; } }
        public double[] fi { get { return _innerobj.fi; } }
        public double[] g { get { return _innerobj.g; } }
        public double[,] h { get { return _innerobj.h; } }
        public double[,] j { get { return _innerobj.j; } }
        public double[] x { get { return _innerobj.x; } }

        public minlmstate()
        {
            _innerobj = new minlm.minlmstate();
        }

        //
        // Although some of declarations below are public, you should not use them
        // They are intended for internal use only
        //
        private minlm.minlmstate _innerobj;
        public minlm.minlmstate innerobj { get { return _innerobj; } }
        public minlmstate(minlm.minlmstate obj)
        {
            _innerobj = obj;
        }
    }


    /*************************************************************************
    Optimization report, filled by MinLMResults() function

    FIELDS:
    * TerminationType, completetion code:
        * -7    derivative correctness check failed;
                see Rep.WrongNum, Rep.WrongI, Rep.WrongJ for
                more information.
        *  1    relative function improvement is no more than
                EpsF.
        *  2    relative step is no more than EpsX.
        *  4    gradient is no more than EpsG.
        *  5    MaxIts steps was taken
        *  7    stopping conditions are too stringent,
                further improvement is impossible
    * IterationsCount, contains iterations count
    * NFunc, number of function calculations
    * NJac, number of Jacobi matrix calculations
    * NGrad, number of gradient calculations
    * NHess, number of Hessian calculations
    * NCholesky, number of Cholesky decomposition calculations
    *************************************************************************/
    public class minlmreport
    {
        //
        // Public declarations
        //
        public int iterationscount { get { return _innerobj.iterationscount; } set { _innerobj.iterationscount = value; } }
        public int terminationtype { get { return _innerobj.terminationtype; } set { _innerobj.terminationtype = value; } }
        public int funcidx { get { return _innerobj.funcidx; } set { _innerobj.funcidx = value; } }
        public int varidx { get { return _innerobj.varidx; } set { _innerobj.varidx = value; } }
        public int nfunc { get { return _innerobj.nfunc; } set { _innerobj.nfunc = value; } }
        public int njac { get { return _innerobj.njac; } set { _innerobj.njac = value; } }
        public int ngrad { get { return _innerobj.ngrad; } set { _innerobj.ngrad = value; } }
        public int nhess { get { return _innerobj.nhess; } set { _innerobj.nhess = value; } }
        public int ncholesky { get { return _innerobj.ncholesky; } set { _innerobj.ncholesky = value; } }

        public minlmreport()
        {
            _innerobj = new minlm.minlmreport();
        }

        //
        // Although some of declarations below are public, you should not use them
        // They are intended for internal use only
        //
        private minlm.minlmreport _innerobj;
        public minlm.minlmreport innerobj { get { return _innerobj; } }
        public minlmreport(minlm.minlmreport obj)
        {
            _innerobj = obj;
        }
    }

    /*************************************************************************
                    IMPROVED LEVENBERG-MARQUARDT METHOD FOR
                     NON-LINEAR LEAST SQUARES OPTIMIZATION

    DESCRIPTION:
    This function is used to find minimum of function which is represented  as
    sum of squares:
        F(x) = f[0]^2(x[0],...,x[n-1]) + ... + f[m-1]^2(x[0],...,x[n-1])
    using value of function vector f[] and Jacobian of f[].


    REQUIREMENTS:
    This algorithm will request following information during its operation:

    * function vector f[] at given point X
    * function vector f[] and Jacobian of f[] (simultaneously) at given point

    There are several overloaded versions of  MinLMOptimize()  function  which
    correspond  to  different LM-like optimization algorithms provided by this
    unit. You should choose version which accepts fvec()  and jac() callbacks.
    First  one  is used to calculate f[] at given point, second one calculates
    f[] and Jacobian df[i]/dx[j].

    You can try to initialize MinLMState structure with VJ  function and  then
    use incorrect version  of  MinLMOptimize()  (for  example,  version  which
    works  with  general  form function and does not provide Jacobian), but it
    will  lead  to  exception  being  thrown  after first attempt to calculate
    Jacobian.


    USAGE:
    1. User initializes algorithm state with MinLMCreateVJ() call
    2. User tunes solver parameters with MinLMSetCond(),  MinLMSetStpMax() and
       other functions
    3. User calls MinLMOptimize() function which  takes algorithm  state   and
       callback functions.
    4. User calls MinLMResults() to get solution
    5. Optionally, user may call MinLMRestartFrom() to solve  another  problem
       with same N/M but another starting point and/or another function.
       MinLMRestartFrom() allows to reuse already initialized structure.


    INPUT PARAMETERS:
        N       -   dimension, N>1
                    * if given, only leading N elements of X are used
                    * if not given, automatically determined from size of X
        M       -   number of functions f[i]
        X       -   initial solution, array[0..N-1]

    OUTPUT PARAMETERS:
        State   -   structure which stores algorithm state

    NOTES:
    1. you may tune stopping conditions with MinLMSetCond() function
    2. if target function contains exp() or other fast growing functions,  and
       optimization algorithm makes too large steps which leads  to  overflow,
       use MinLMSetStpMax() function to bound algorithm's steps.

      -- ALGLIB --
         Copyright 30.03.2009 by Bochkanov Sergey
    *************************************************************************/
    public static void minlmcreatevj(int n, int m, double[] x, out minlmstate state)
    {
        state = new minlmstate();
        minlm.minlmcreatevj(n, m, x, state.innerobj);
        return;
    }
    public static void minlmcreatevj(int m, double[] x, out minlmstate state)
    {
        int n;

        state = new minlmstate();
        n = ap.len(x);
        minlm.minlmcreatevj(n, m, x, state.innerobj);

        return;
    }

    /*************************************************************************
                    IMPROVED LEVENBERG-MARQUARDT METHOD FOR
                     NON-LINEAR LEAST SQUARES OPTIMIZATION

    DESCRIPTION:
    This function is used to find minimum of function which is represented  as
    sum of squares:
        F(x) = f[0]^2(x[0],...,x[n-1]) + ... + f[m-1]^2(x[0],...,x[n-1])
    using value of function vector f[] only. Finite differences  are  used  to
    calculate Jacobian.


    REQUIREMENTS:
    This algorithm will request following information during its operation:
    * function vector f[] at given point X

    There are several overloaded versions of  MinLMOptimize()  function  which
    correspond  to  different LM-like optimization algorithms provided by this
    unit. You should choose version which accepts fvec() callback.

    You can try to initialize MinLMState structure with VJ  function and  then
    use incorrect version  of  MinLMOptimize()  (for  example,  version  which
    works with general form function and does not accept function vector), but
    it will  lead  to  exception being thrown after first attempt to calculate
    Jacobian.


    USAGE:
    1. User initializes algorithm state with MinLMCreateV() call
    2. User tunes solver parameters with MinLMSetCond(),  MinLMSetStpMax() and
       other functions
    3. User calls MinLMOptimize() function which  takes algorithm  state   and
       callback functions.
    4. User calls MinLMResults() to get solution
    5. Optionally, user may call MinLMRestartFrom() to solve  another  problem
       with same N/M but another starting point and/or another function.
       MinLMRestartFrom() allows to reuse already initialized structure.


    INPUT PARAMETERS:
        N       -   dimension, N>1
                    * if given, only leading N elements of X are used
                    * if not given, automatically determined from size of X
        M       -   number of functions f[i]
        X       -   initial solution, array[0..N-1]
        DiffStep-   differentiation step, >0

    OUTPUT PARAMETERS:
        State   -   structure which stores algorithm state

    See also MinLMIteration, MinLMResults.

    NOTES:
    1. you may tune stopping conditions with MinLMSetCond() function
    2. if target function contains exp() or other fast growing functions,  and
       optimization algorithm makes too large steps which leads  to  overflow,
       use MinLMSetStpMax() function to bound algorithm's steps.

      -- ALGLIB --
         Copyright 30.03.2009 by Bochkanov Sergey
    *************************************************************************/
    public static void minlmcreatev(int n, int m, double[] x, double diffstep, out minlmstate state)
    {
        state = new minlmstate();
        minlm.minlmcreatev(n, m, x, diffstep, state.innerobj);
        return;
    }
    public static void minlmcreatev(int m, double[] x, double diffstep, out minlmstate state)
    {
        int n;

        state = new minlmstate();
        n = ap.len(x);
        minlm.minlmcreatev(n, m, x, diffstep, state.innerobj);

        return;
    }

    /*************************************************************************
        LEVENBERG-MARQUARDT-LIKE METHOD FOR NON-LINEAR OPTIMIZATION

    DESCRIPTION:
    This  function  is  used  to  find  minimum  of general form (not "sum-of-
    -squares") function
        F = F(x[0], ..., x[n-1])
    using  its  gradient  and  Hessian.  Levenberg-Marquardt modification with
    L-BFGS pre-optimization and internal pre-conditioned  L-BFGS  optimization
    after each Levenberg-Marquardt step is used.


    REQUIREMENTS:
    This algorithm will request following information during its operation:

    * function value F at given point X
    * F and gradient G (simultaneously) at given point X
    * F, G and Hessian H (simultaneously) at given point X

    There are several overloaded versions of  MinLMOptimize()  function  which
    correspond  to  different LM-like optimization algorithms provided by this
    unit. You should choose version which accepts func(),  grad()  and  hess()
    function pointers. First pointer is used to calculate F  at  given  point,
    second  one  calculates  F(x)  and  grad F(x),  third one calculates F(x),
    grad F(x), hess F(x).

    You can try to initialize MinLMState structure with FGH-function and  then
    use incorrect version of MinLMOptimize() (for example, version which  does
    not provide Hessian matrix), but it will lead to  exception  being  thrown
    after first attempt to calculate Hessian.


    USAGE:
    1. User initializes algorithm state with MinLMCreateFGH() call
    2. User tunes solver parameters with MinLMSetCond(),  MinLMSetStpMax() and
       other functions
    3. User calls MinLMOptimize() function which  takes algorithm  state   and
       pointers (delegates, etc.) to callback functions.
    4. User calls MinLMResults() to get solution
    5. Optionally, user may call MinLMRestartFrom() to solve  another  problem
       with same N but another starting point and/or another function.
       MinLMRestartFrom() allows to reuse already initialized structure.


    INPUT PARAMETERS:
        N       -   dimension, N>1
                    * if given, only leading N elements of X are used
                    * if not given, automatically determined from size of X
        X       -   initial solution, array[0..N-1]

    OUTPUT PARAMETERS:
        State   -   structure which stores algorithm state

    NOTES:
    1. you may tune stopping conditions with MinLMSetCond() function
    2. if target function contains exp() or other fast growing functions,  and
       optimization algorithm makes too large steps which leads  to  overflow,
       use MinLMSetStpMax() function to bound algorithm's steps.

      -- ALGLIB --
         Copyright 30.03.2009 by Bochkanov Sergey
    *************************************************************************/
    public static void minlmcreatefgh(int n, double[] x, out minlmstate state)
    {
        state = new minlmstate();
        minlm.minlmcreatefgh(n, x, state.innerobj);
        return;
    }
    public static void minlmcreatefgh(double[] x, out minlmstate state)
    {
        int n;

        state = new minlmstate();
        n = ap.len(x);
        minlm.minlmcreatefgh(n, x, state.innerobj);

        return;
    }

    /*************************************************************************
    This function sets stopping conditions for Levenberg-Marquardt optimization
    algorithm.

    INPUT PARAMETERS:
        State   -   structure which stores algorithm state
        EpsG    -   >=0
                    The  subroutine  finishes  its  work   if   the  condition
                    |v|<EpsG is satisfied, where:
                    * |.| means Euclidian norm
                    * v - scaled gradient vector, v[i]=g[i]*s[i]
                    * g - gradient
                    * s - scaling coefficients set by MinLMSetScale()
        EpsF    -   >=0
                    The  subroutine  finishes  its work if on k+1-th iteration
                    the  condition  |F(k+1)-F(k)|<=EpsF*max{|F(k)|,|F(k+1)|,1}
                    is satisfied.
        EpsX    -   >=0
                    The subroutine finishes its work if  on  k+1-th  iteration
                    the condition |v|<=EpsX is fulfilled, where:
                    * |.| means Euclidian norm
                    * v - scaled step vector, v[i]=dx[i]/s[i]
                    * dx - ste pvector, dx=X(k+1)-X(k)
                    * s - scaling coefficients set by MinLMSetScale()
        MaxIts  -   maximum number of iterations. If MaxIts=0, the  number  of
                    iterations   is    unlimited.   Only   Levenberg-Marquardt
                    iterations  are  counted  (L-BFGS/CG  iterations  are  NOT
                    counted because their cost is very low compared to that of
                    LM).

    Passing EpsG=0, EpsF=0, EpsX=0 and MaxIts=0 (simultaneously) will lead to
    automatic stopping criterion selection (small EpsX).

      -- ALGLIB --
         Copyright 02.04.2010 by Bochkanov Sergey
    *************************************************************************/
    public static void minlmsetcond(minlmstate state, double epsg, double epsf, double epsx, int maxits)
    {

        minlm.minlmsetcond(state.innerobj, epsg, epsf, epsx, maxits);
        return;
    }

    /*************************************************************************
    This function turns on/off reporting.

    INPUT PARAMETERS:
        State   -   structure which stores algorithm state
        NeedXRep-   whether iteration reports are needed or not

    If NeedXRep is True, algorithm will call rep() callback function if  it is
    provided to MinLMOptimize(). Both Levenberg-Marquardt and internal  L-BFGS
    iterations are reported.

      -- ALGLIB --
         Copyright 02.04.2010 by Bochkanov Sergey
    *************************************************************************/
    public static void minlmsetxrep(minlmstate state, bool needxrep)
    {

        minlm.minlmsetxrep(state.innerobj, needxrep);
        return;
    }

    /*************************************************************************
    This function sets maximum step length

    INPUT PARAMETERS:
        State   -   structure which stores algorithm state
        StpMax  -   maximum step length, >=0. Set StpMax to 0.0,  if you don't
                    want to limit step length.

    Use this subroutine when you optimize target function which contains exp()
    or  other  fast  growing  functions,  and optimization algorithm makes too
    large  steps  which  leads  to overflow. This function allows us to reject
    steps  that  are  too  large  (and  therefore  expose  us  to the possible
    overflow) without actually calculating function value at the x+stp*d.

    NOTE: non-zero StpMax leads to moderate  performance  degradation  because
    intermediate  step  of  preconditioned L-BFGS optimization is incompatible
    with limits on step size.

      -- ALGLIB --
         Copyright 02.04.2010 by Bochkanov Sergey
    *************************************************************************/
    public static void minlmsetstpmax(minlmstate state, double stpmax)
    {

        minlm.minlmsetstpmax(state.innerobj, stpmax);
        return;
    }

    /*************************************************************************
    This function sets scaling coefficients for LM optimizer.

    ALGLIB optimizers use scaling matrices to test stopping  conditions  (step
    size and gradient are scaled before comparison with tolerances).  Scale of
    the I-th variable is a translation invariant measure of:
    a) "how large" the variable is
    b) how large the step should be to make significant changes in the function

    Generally, scale is NOT considered to be a form of preconditioner.  But LM
    optimizer is unique in that it uses scaling matrix both  in  the  stopping
    condition tests and as Marquardt damping factor.

    Proper scaling is very important for the algorithm performance. It is less
    important for the quality of results, but still has some influence (it  is
    easier  to  converge  when  variables  are  properly  scaled, so premature
    stopping is possible when very badly scalled variables are  combined  with
    relaxed stopping conditions).

    INPUT PARAMETERS:
        State   -   structure stores algorithm state
        S       -   array[N], non-zero scaling coefficients
                    S[i] may be negative, sign doesn't matter.

      -- ALGLIB --
         Copyright 14.01.2011 by Bochkanov Sergey
    *************************************************************************/
    public static void minlmsetscale(minlmstate state, double[] s)
    {

        minlm.minlmsetscale(state.innerobj, s);
        return;
    }

    /*************************************************************************
    This function sets boundary constraints for LM optimizer

    Boundary constraints are inactive by default (after initial creation).
    They are preserved until explicitly turned off with another SetBC() call.

    INPUT PARAMETERS:
        State   -   structure stores algorithm state
        BndL    -   lower bounds, array[N].
                    If some (all) variables are unbounded, you may specify
                    very small number or -INF (latter is recommended because
                    it will allow solver to use better algorithm).
        BndU    -   upper bounds, array[N].
                    If some (all) variables are unbounded, you may specify
                    very large number or +INF (latter is recommended because
                    it will allow solver to use better algorithm).

    NOTE 1: it is possible to specify BndL[i]=BndU[i]. In this case I-th
    variable will be "frozen" at X[i]=BndL[i]=BndU[i].

    NOTE 2: this solver has following useful properties:
    * bound constraints are always satisfied exactly
    * function is evaluated only INSIDE area specified by bound constraints
      or at its boundary

      -- ALGLIB --
         Copyright 14.01.2011 by Bochkanov Sergey
    *************************************************************************/
    public static void minlmsetbc(minlmstate state, double[] bndl, double[] bndu)
    {

        minlm.minlmsetbc(state.innerobj, bndl, bndu);
        return;
    }

    /*************************************************************************
    This function is used to change acceleration settings

    You can choose between three acceleration strategies:
    * AccType=0, no acceleration.
    * AccType=1, secant updates are used to update quadratic model after  each
      iteration. After fixed number of iterations (or after  model  breakdown)
      we  recalculate  quadratic  model  using  analytic  Jacobian  or  finite
      differences. Number of secant-based iterations depends  on  optimization
      settings: about 3 iterations - when we have analytic Jacobian, up to 2*N
      iterations - when we use finite differences to calculate Jacobian.

    AccType=1 is recommended when Jacobian  calculation  cost  is  prohibitive
    high (several Mx1 function vector calculations  followed  by  several  NxN
    Cholesky factorizations are faster than calculation of one M*N  Jacobian).
    It should also be used when we have no Jacobian, because finite difference
    approximation takes too much time to compute.

    Table below list  optimization  protocols  (XYZ  protocol  corresponds  to
    MinLMCreateXYZ) and acceleration types they support (and use by  default).

    ACCELERATION TYPES SUPPORTED BY OPTIMIZATION PROTOCOLS:

    protocol    0   1   comment
    V           +   +
    VJ          +   +
    FGH         +

    DAFAULT VALUES:

    protocol    0   1   comment
    V               x   without acceleration it is so slooooooooow
    VJ          x
    FGH         x

    NOTE: this  function should be called before optimization. Attempt to call
    it during algorithm iterations may result in unexpected behavior.

    NOTE: attempt to call this function with unsupported protocol/acceleration
    combination will result in exception being thrown.

      -- ALGLIB --
         Copyright 14.10.2010 by Bochkanov Sergey
    *************************************************************************/
    public static void minlmsetacctype(minlmstate state, int acctype)
    {

        minlm.minlmsetacctype(state.innerobj, acctype);
        return;
    }

    /*************************************************************************
    This function provides reverse communication interface
    Reverse communication interface is not documented or recommended to use.
    See below for functions which provide better documented API
    *************************************************************************/
    public static bool minlmiteration(minlmstate state)
    {

        bool result = minlm.minlmiteration(state.innerobj);
        return result;
    }
    /*************************************************************************
    This family of functions is used to launcn iterations of nonlinear optimizer

    These functions accept following parameters:
        func    -   callback which calculates function (or merit function)
                    value func at given point x
        grad    -   callback which calculates function (or merit function)
                    value func and gradient grad at given point x
        hess    -   callback which calculates function (or merit function)
                    value func, gradient grad and Hessian hess at given point x
        fvec    -   callback which calculates function vector fi[]
                    at given point x
        jac     -   callback which calculates function vector fi[]
                    and Jacobian jac at given point x
        rep     -   optional callback which is called after each iteration
                    can be null
        obj     -   optional object which is passed to func/grad/hess/jac/rep
                    can be null

    NOTES:

    1. Depending on function used to create state  structure,  this  algorithm
       may accept Jacobian and/or Hessian and/or gradient.  According  to  the
       said above, there ase several versions of this function,  which  accept
       different sets of callbacks.

       This flexibility opens way to subtle errors - you may create state with
       MinLMCreateFGH() (optimization using Hessian), but call function  which
       does not accept Hessian. So when algorithm will request Hessian,  there
       will be no callback to call. In this case exception will be thrown.

       Be careful to avoid such errors because there is no way to find them at
       compile time - you can see them at runtime only.

      -- ALGLIB --
         Copyright 10.03.2009 by Bochkanov Sergey

    *************************************************************************/
    public static void minlmoptimize(minlmstate state, ndimensional_fvec  fvec, ndimensional_rep rep, object obj)
    {
        if( fvec==null )
            throw new alglibexception("ALGLIB: error in 'minlmoptimize()' (fvec is null)");
        while( alglib.minlmiteration(state) )
        {
            if( state.needfi )
            {
                fvec(state.x, state.innerobj.fi, obj);
                continue;
            }
            if( state.innerobj.xupdated )
            {
                if( rep!=null )
                    rep(state.innerobj.x, state.innerobj.f, obj);
                continue;
            }
            throw new alglibexception("ALGLIB: error in 'minlmoptimize' (some derivatives were not provided?)");
        }
    }


    public static void minlmoptimize(minlmstate state, ndimensional_fvec  fvec, ndimensional_jac  jac, ndimensional_rep rep, object obj)
    {
        if( fvec==null )
            throw new alglibexception("ALGLIB: error in 'minlmoptimize()' (fvec is null)");
        if( jac==null )
            throw new alglibexception("ALGLIB: error in 'minlmoptimize()' (jac is null)");
        while( alglib.minlmiteration(state) )
        {
            if( state.needfi )
            {
                fvec(state.x, state.innerobj.fi, obj);
                continue;
            }
            if( state.needfij )
            {
                jac(state.x, state.innerobj.fi, state.innerobj.j, obj);
                continue;
            }
            if( state.innerobj.xupdated )
            {
                if( rep!=null )
                    rep(state.innerobj.x, state.innerobj.f, obj);
                continue;
            }
            throw new alglibexception("ALGLIB: error in 'minlmoptimize' (some derivatives were not provided?)");
        }
    }


    public static void minlmoptimize(minlmstate state, ndimensional_func func, ndimensional_grad grad, ndimensional_hess hess, ndimensional_rep rep, object obj)
    {
        if( func==null )
            throw new alglibexception("ALGLIB: error in 'minlmoptimize()' (func is null)");
        if( grad==null )
            throw new alglibexception("ALGLIB: error in 'minlmoptimize()' (grad is null)");
        if( hess==null )
            throw new alglibexception("ALGLIB: error in 'minlmoptimize()' (hess is null)");
        while( alglib.minlmiteration(state) )
        {
            if( state.needf )
            {
                func(state.x, ref state.innerobj.f, obj);
                continue;
            }
            if( state.needfg )
            {
                grad(state.x, ref state.innerobj.f, state.innerobj.g, obj);
                continue;
            }
            if( state.needfgh )
            {
                hess(state.x, ref state.innerobj.f, state.innerobj.g, state.innerobj.h, obj);
                continue;
            }
            if( state.innerobj.xupdated )
            {
                if( rep!=null )
                    rep(state.innerobj.x, state.innerobj.f, obj);
                continue;
            }
            throw new alglibexception("ALGLIB: error in 'minlmoptimize' (some derivatives were not provided?)");
        }
    }


    public static void minlmoptimize(minlmstate state, ndimensional_func func, ndimensional_jac  jac, ndimensional_rep rep, object obj)
    {
        if( func==null )
            throw new alglibexception("ALGLIB: error in 'minlmoptimize()' (func is null)");
        if( jac==null )
            throw new alglibexception("ALGLIB: error in 'minlmoptimize()' (jac is null)");
        while( alglib.minlmiteration(state) )
        {
            if( state.needf )
            {
                func(state.x, ref state.innerobj.f, obj);
                continue;
            }
            if( state.needfij )
            {
                jac(state.x, state.innerobj.fi, state.innerobj.j, obj);
                continue;
            }
            if( state.innerobj.xupdated )
            {
                if( rep!=null )
                    rep(state.innerobj.x, state.innerobj.f, obj);
                continue;
            }
            throw new alglibexception("ALGLIB: error in 'minlmoptimize' (some derivatives were not provided?)");
        }
    }


    public static void minlmoptimize(minlmstate state, ndimensional_func func, ndimensional_grad grad, ndimensional_jac  jac, ndimensional_rep rep, object obj)
    {
        if( func==null )
            throw new alglibexception("ALGLIB: error in 'minlmoptimize()' (func is null)");
        if( grad==null )
            throw new alglibexception("ALGLIB: error in 'minlmoptimize()' (grad is null)");
        if( jac==null )
            throw new alglibexception("ALGLIB: error in 'minlmoptimize()' (jac is null)");
        while( alglib.minlmiteration(state) )
        {
            if( state.needf )
            {
                func(state.x, ref state.innerobj.f, obj);
                continue;
            }
            if( state.needfg )
            {
                grad(state.x, ref state.innerobj.f, state.innerobj.g, obj);
                continue;
            }
            if( state.needfij )
            {
                jac(state.x, state.innerobj.fi, state.innerobj.j, obj);
                continue;
            }
            if( state.innerobj.xupdated )
            {
                if( rep!=null )
                    rep(state.innerobj.x, state.innerobj.f, obj);
                continue;
            }
            throw new alglibexception("ALGLIB: error in 'minlmoptimize' (some derivatives were not provided?)");
        }
    }



    /*************************************************************************
    Levenberg-Marquardt algorithm results

    INPUT PARAMETERS:
        State   -   algorithm state

    OUTPUT PARAMETERS:
        X       -   array[0..N-1], solution
        Rep     -   optimization report;
                    see comments for this structure for more info.

      -- ALGLIB --
         Copyright 10.03.2009 by Bochkanov Sergey
    *************************************************************************/
    public static void minlmresults(minlmstate state, out double[] x, out minlmreport rep)
    {
        x = new double[0];
        rep = new minlmreport();
        minlm.minlmresults(state.innerobj, ref x, rep.innerobj);
        return;
    }

    /*************************************************************************
    Levenberg-Marquardt algorithm results

    Buffered implementation of MinLMResults(), which uses pre-allocated buffer
    to store X[]. If buffer size is  too  small,  it  resizes  buffer.  It  is
    intended to be used in the inner cycles of performance critical algorithms
    where array reallocation penalty is too large to be ignored.

      -- ALGLIB --
         Copyright 10.03.2009 by Bochkanov Sergey
    *************************************************************************/
    public static void minlmresultsbuf(minlmstate state, ref double[] x, minlmreport rep)
    {

        minlm.minlmresultsbuf(state.innerobj, ref x, rep.innerobj);
        return;
    }

    /*************************************************************************
    This  subroutine  restarts  LM  algorithm from new point. All optimization
    parameters are left unchanged.

    This  function  allows  to  solve multiple  optimization  problems  (which
    must have same number of dimensions) without object reallocation penalty.

    INPUT PARAMETERS:
        State   -   structure used for reverse communication previously
                    allocated with MinLMCreateXXX call.
        X       -   new starting point.

      -- ALGLIB --
         Copyright 30.07.2010 by Bochkanov Sergey
    *************************************************************************/
    public static void minlmrestartfrom(minlmstate state, double[] x)
    {

        minlm.minlmrestartfrom(state.innerobj, x);
        return;
    }

    /*************************************************************************
    This is obsolete function.

    Since ALGLIB 3.3 it is equivalent to MinLMCreateVJ().

      -- ALGLIB --
         Copyright 30.03.2009 by Bochkanov Sergey
    *************************************************************************/
    public static void minlmcreatevgj(int n, int m, double[] x, out minlmstate state)
    {
        state = new minlmstate();
        minlm.minlmcreatevgj(n, m, x, state.innerobj);
        return;
    }
    public static void minlmcreatevgj(int m, double[] x, out minlmstate state)
    {
        int n;

        state = new minlmstate();
        n = ap.len(x);
        minlm.minlmcreatevgj(n, m, x, state.innerobj);

        return;
    }

    /*************************************************************************
    This is obsolete function.

    Since ALGLIB 3.3 it is equivalent to MinLMCreateFJ().

      -- ALGLIB --
         Copyright 30.03.2009 by Bochkanov Sergey
    *************************************************************************/
    public static void minlmcreatefgj(int n, int m, double[] x, out minlmstate state)
    {
        state = new minlmstate();
        minlm.minlmcreatefgj(n, m, x, state.innerobj);
        return;
    }
    public static void minlmcreatefgj(int m, double[] x, out minlmstate state)
    {
        int n;

        state = new minlmstate();
        n = ap.len(x);
        minlm.minlmcreatefgj(n, m, x, state.innerobj);

        return;
    }

    /*************************************************************************
    This function is considered obsolete since ALGLIB 3.1.0 and is present for
    backward  compatibility  only.  We  recommend  to use MinLMCreateVJ, which
    provides similar, but more consistent and feature-rich interface.

      -- ALGLIB --
         Copyright 30.03.2009 by Bochkanov Sergey
    *************************************************************************/
    public static void minlmcreatefj(int n, int m, double[] x, out minlmstate state)
    {
        state = new minlmstate();
        minlm.minlmcreatefj(n, m, x, state.innerobj);
        return;
    }
    public static void minlmcreatefj(int m, double[] x, out minlmstate state)
    {
        int n;

        state = new minlmstate();
        n = ap.len(x);
        minlm.minlmcreatefj(n, m, x, state.innerobj);

        return;
    }

    /*************************************************************************
    This  subroutine  turns  on  verification  of  the  user-supplied analytic
    gradient:
    * user calls this subroutine before optimization begins
    * MinLMOptimize() is called
    * prior to actual optimization, for  each  function Fi and each  component
      of parameters  being  optimized X[j] algorithm performs following steps:
      * two trial steps are made to X[j]-TestStep*S[j] and X[j]+TestStep*S[j],
        where X[j] is j-th parameter and S[j] is a scale of j-th parameter
      * if needed, steps are bounded with respect to constraints on X[]
      * Fi(X) is evaluated at these trial points
      * we perform one more evaluation in the middle point of the interval
      * we  build  cubic  model using function values and derivatives at trial
        points and we compare its prediction with actual value in  the  middle
        point
      * in case difference between prediction and actual value is higher  than
        some predetermined threshold, algorithm stops with completion code -7;
        Rep.VarIdx is set to index of the parameter with incorrect derivative,
        Rep.FuncIdx is set to index of the function.
    * after verification is over, algorithm proceeds to the actual optimization.

    NOTE 1: verification  needs  N (parameters count) Jacobian evaluations. It
            is  very  costly  and  you  should use it only for low dimensional
            problems,  when  you  want  to  be  sure  that  you've   correctly
            calculated  analytic  derivatives.  You should not  use  it in the
            production code  (unless  you  want  to check derivatives provided
            by some third party).

    NOTE 2: you  should  carefully  choose  TestStep. Value which is too large
            (so large that function behaviour is significantly non-cubic) will
            lead to false alarms. You may use  different  step  for  different
            parameters by means of setting scale with MinLMSetScale().

    NOTE 3: this function may lead to false positives. In case it reports that
            I-th  derivative was calculated incorrectly, you may decrease test
            step  and  try  one  more  time  - maybe your function changes too
            sharply  and  your  step  is  too  large for such rapidly chanding
            function.

    INPUT PARAMETERS:
        State       -   structure used to store algorithm state
        TestStep    -   verification step:
                        * TestStep=0 turns verification off
                        * TestStep>0 activates verification

      -- ALGLIB --
         Copyright 15.06.2012 by Bochkanov Sergey
    *************************************************************************/
    public static void minlmsetgradientcheck(minlmstate state, double teststep)
    {

        minlm.minlmsetgradientcheck(state.innerobj, teststep);
        return;
    }

}
public partial class alglib
{


    /*************************************************************************

    *************************************************************************/
    public class minasastate
    {
        //
        // Public declarations
        //
        public bool needfg { get { return _innerobj.needfg; } set { _innerobj.needfg = value; } }
        public bool xupdated { get { return _innerobj.xupdated; } set { _innerobj.xupdated = value; } }
        public double f { get { return _innerobj.f; } set { _innerobj.f = value; } }
        public double[] g { get { return _innerobj.g; } }
        public double[] x { get { return _innerobj.x; } }

        public minasastate()
        {
            _innerobj = new mincomp.minasastate();
        }

        //
        // Although some of declarations below are public, you should not use them
        // They are intended for internal use only
        //
        private mincomp.minasastate _innerobj;
        public mincomp.minasastate innerobj { get { return _innerobj; } }
        public minasastate(mincomp.minasastate obj)
        {
            _innerobj = obj;
        }
    }


    /*************************************************************************

    *************************************************************************/
    public class minasareport
    {
        //
        // Public declarations
        //
        public int iterationscount { get { return _innerobj.iterationscount; } set { _innerobj.iterationscount = value; } }
        public int nfev { get { return _innerobj.nfev; } set { _innerobj.nfev = value; } }
        public int terminationtype { get { return _innerobj.terminationtype; } set { _innerobj.terminationtype = value; } }
        public int activeconstraints { get { return _innerobj.activeconstraints; } set { _innerobj.activeconstraints = value; } }

        public minasareport()
        {
            _innerobj = new mincomp.minasareport();
        }

        //
        // Although some of declarations below are public, you should not use them
        // They are intended for internal use only
        //
        private mincomp.minasareport _innerobj;
        public mincomp.minasareport innerobj { get { return _innerobj; } }
        public minasareport(mincomp.minasareport obj)
        {
            _innerobj = obj;
        }
    }

    /*************************************************************************
    Obsolete function, use MinLBFGSSetPrecDefault() instead.

      -- ALGLIB --
         Copyright 13.10.2010 by Bochkanov Sergey
    *************************************************************************/
    public static void minlbfgssetdefaultpreconditioner(minlbfgsstate state)
    {

        mincomp.minlbfgssetdefaultpreconditioner(state.innerobj);
        return;
    }

    /*************************************************************************
    Obsolete function, use MinLBFGSSetCholeskyPreconditioner() instead.

      -- ALGLIB --
         Copyright 13.10.2010 by Bochkanov Sergey
    *************************************************************************/
    public static void minlbfgssetcholeskypreconditioner(minlbfgsstate state, double[,] p, bool isupper)
    {

        mincomp.minlbfgssetcholeskypreconditioner(state.innerobj, p, isupper);
        return;
    }

    /*************************************************************************
    This is obsolete function which was used by previous version of the  BLEIC
    optimizer. It does nothing in the current version of BLEIC.

      -- ALGLIB --
         Copyright 28.11.2010 by Bochkanov Sergey
    *************************************************************************/
    public static void minbleicsetbarrierwidth(minbleicstate state, double mu)
    {

        mincomp.minbleicsetbarrierwidth(state.innerobj, mu);
        return;
    }

    /*************************************************************************
    This is obsolete function which was used by previous version of the  BLEIC
    optimizer. It does nothing in the current version of BLEIC.

      -- ALGLIB --
         Copyright 28.11.2010 by Bochkanov Sergey
    *************************************************************************/
    public static void minbleicsetbarrierdecay(minbleicstate state, double mudecay)
    {

        mincomp.minbleicsetbarrierdecay(state.innerobj, mudecay);
        return;
    }

    /*************************************************************************
    Obsolete optimization algorithm.
    Was replaced by MinBLEIC subpackage.

      -- ALGLIB --
         Copyright 25.03.2010 by Bochkanov Sergey
    *************************************************************************/
    public static void minasacreate(int n, double[] x, double[] bndl, double[] bndu, out minasastate state)
    {
        state = new minasastate();
        mincomp.minasacreate(n, x, bndl, bndu, state.innerobj);
        return;
    }
    public static void minasacreate(double[] x, double[] bndl, double[] bndu, out minasastate state)
    {
        int n;
        if( (ap.len(x)!=ap.len(bndl)) || (ap.len(x)!=ap.len(bndu)))
            throw new alglibexception("Error while calling 'minasacreate': looks like one of arguments has wrong size");
        state = new minasastate();
        n = ap.len(x);
        mincomp.minasacreate(n, x, bndl, bndu, state.innerobj);

        return;
    }

    /*************************************************************************
    Obsolete optimization algorithm.
    Was replaced by MinBLEIC subpackage.

      -- ALGLIB --
         Copyright 02.04.2010 by Bochkanov Sergey
    *************************************************************************/
    public static void minasasetcond(minasastate state, double epsg, double epsf, double epsx, int maxits)
    {

        mincomp.minasasetcond(state.innerobj, epsg, epsf, epsx, maxits);
        return;
    }

    /*************************************************************************
    Obsolete optimization algorithm.
    Was replaced by MinBLEIC subpackage.

      -- ALGLIB --
         Copyright 02.04.2010 by Bochkanov Sergey
    *************************************************************************/
    public static void minasasetxrep(minasastate state, bool needxrep)
    {

        mincomp.minasasetxrep(state.innerobj, needxrep);
        return;
    }

    /*************************************************************************
    Obsolete optimization algorithm.
    Was replaced by MinBLEIC subpackage.

      -- ALGLIB --
         Copyright 02.04.2010 by Bochkanov Sergey
    *************************************************************************/
    public static void minasasetalgorithm(minasastate state, int algotype)
    {

        mincomp.minasasetalgorithm(state.innerobj, algotype);
        return;
    }

    /*************************************************************************
    Obsolete optimization algorithm.
    Was replaced by MinBLEIC subpackage.

      -- ALGLIB --
         Copyright 02.04.2010 by Bochkanov Sergey
    *************************************************************************/
    public static void minasasetstpmax(minasastate state, double stpmax)
    {

        mincomp.minasasetstpmax(state.innerobj, stpmax);
        return;
    }

    /*************************************************************************
    This function provides reverse communication interface
    Reverse communication interface is not documented or recommended to use.
    See below for functions which provide better documented API
    *************************************************************************/
    public static bool minasaiteration(minasastate state)
    {

        bool result = mincomp.minasaiteration(state.innerobj);
        return result;
    }
    /*************************************************************************
    This family of functions is used to launcn iterations of nonlinear optimizer

    These functions accept following parameters:
        grad    -   callback which calculates function (or merit function)
                    value func and gradient grad at given point x
        rep     -   optional callback which is called after each iteration
                    can be null
        obj     -   optional object which is passed to func/grad/hess/jac/rep
                    can be null


      -- ALGLIB --
         Copyright 20.03.2009 by Bochkanov Sergey

    *************************************************************************/
    public static void minasaoptimize(minasastate state, ndimensional_grad grad, ndimensional_rep rep, object obj)
    {
        if( grad==null )
            throw new alglibexception("ALGLIB: error in 'minasaoptimize()' (grad is null)");
        while( alglib.minasaiteration(state) )
        {
            if( state.needfg )
            {
                grad(state.x, ref state.innerobj.f, state.innerobj.g, obj);
                continue;
            }
            if( state.innerobj.xupdated )
            {
                if( rep!=null )
                    rep(state.innerobj.x, state.innerobj.f, obj);
                continue;
            }
            throw new alglibexception("ALGLIB: error in 'minasaoptimize' (some derivatives were not provided?)");
        }
    }



    /*************************************************************************
    Obsolete optimization algorithm.
    Was replaced by MinBLEIC subpackage.

      -- ALGLIB --
         Copyright 20.03.2009 by Bochkanov Sergey
    *************************************************************************/
    public static void minasaresults(minasastate state, out double[] x, out minasareport rep)
    {
        x = new double[0];
        rep = new minasareport();
        mincomp.minasaresults(state.innerobj, ref x, rep.innerobj);
        return;
    }

    /*************************************************************************
    Obsolete optimization algorithm.
    Was replaced by MinBLEIC subpackage.

      -- ALGLIB --
         Copyright 20.03.2009 by Bochkanov Sergey
    *************************************************************************/
    public static void minasaresultsbuf(minasastate state, ref double[] x, minasareport rep)
    {

        mincomp.minasaresultsbuf(state.innerobj, ref x, rep.innerobj);
        return;
    }

    /*************************************************************************
    Obsolete optimization algorithm.
    Was replaced by MinBLEIC subpackage.

      -- ALGLIB --
         Copyright 30.07.2010 by Bochkanov Sergey
    *************************************************************************/
    public static void minasarestartfrom(minasastate state, double[] x, double[] bndl, double[] bndu)
    {

        mincomp.minasarestartfrom(state.innerobj, x, bndl, bndu);
        return;
    }

}
public partial class alglib
{



}
public partial class alglib
{
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


        /*************************************************************************
        This function enforces boundary constraints in the X.

        This function correctly (although a bit inefficient) handles BL[i] which
        are -INF and BU[i] which are +INF.

        We have NMain+NSlack  dimensional  X,  with first NMain components bounded
        by BL/BU, and next NSlack ones bounded by non-negativity constraints.

        INPUT PARAMETERS
            X       -   array[NMain+NSlack], point
            BL      -   array[NMain], lower bounds
                        (may contain -INF, when bound is not present)
            HaveBL  -   array[NMain], if HaveBL[i] is False,
                        then i-th bound is not present
            BU      -   array[NMain], upper bounds
                        (may contain +INF, when bound is not present)
            HaveBU  -   array[NMain], if HaveBU[i] is False,
                        then i-th bound is not present

        OUTPUT PARAMETERS
            X       -   X with all constraints being enforced

        It returns True when constraints are consistent,
        False - when constraints are inconsistent.

          -- ALGLIB --
             Copyright 10.01.2012 by Bochkanov Sergey
        *************************************************************************/
        public static bool enforceboundaryconstraints(ref double[] x,
            double[] bl,
            bool[] havebl,
            double[] bu,
            bool[] havebu,
            int nmain,
            int nslack)
        {
            bool result = new bool();
            int i = 0;

            result = false;
            for(i=0; i<=nmain-1; i++)
            {
                if( (havebl[i] && havebu[i]) && (double)(bl[i])>(double)(bu[i]) )
                {
                    return result;
                }
                if( havebl[i] && (double)(x[i])<(double)(bl[i]) )
                {
                    x[i] = bl[i];
                }
                if( havebu[i] && (double)(x[i])>(double)(bu[i]) )
                {
                    x[i] = bu[i];
                }
            }
            for(i=0; i<=nslack-1; i++)
            {
                if( (double)(x[nmain+i])<(double)(0) )
                {
                    x[nmain+i] = 0;
                }
            }
            result = true;
            return result;
        }


        /*************************************************************************
        This function projects gradient into feasible area of boundary constrained
        optimization  problem.  X  can  be  infeasible  with  respect  to boundary
        constraints.  We  have  NMain+NSlack  dimensional  X,   with  first  NMain 
        components bounded by BL/BU, and next NSlack ones bounded by non-negativity
        constraints.

        INPUT PARAMETERS
            X       -   array[NMain+NSlack], point
            G       -   array[NMain+NSlack], gradient
            BL      -   lower bounds (may contain -INF, when bound is not present)
            HaveBL  -   if HaveBL[i] is False, then i-th bound is not present
            BU      -   upper bounds (may contain +INF, when bound is not present)
            HaveBU  -   if HaveBU[i] is False, then i-th bound is not present

        OUTPUT PARAMETERS
            G       -   projection of G. Components of G which satisfy one of the
                        following
                            (1) (X[I]<=BndL[I]) and (G[I]>0), OR
                            (2) (X[I]>=BndU[I]) and (G[I]<0)
                        are replaced by zeros.

        NOTE 1: this function assumes that constraints are feasible. It throws
        exception otherwise.

        NOTE 2: in fact, projection of ANTI-gradient is calculated,  because  this
        function trims components of -G which points outside of the feasible area.
        However, working with -G is considered confusing, because all optimization
        source work with G.

          -- ALGLIB --
             Copyright 10.01.2012 by Bochkanov Sergey
        *************************************************************************/
        public static void projectgradientintobc(double[] x,
            ref double[] g,
            double[] bl,
            bool[] havebl,
            double[] bu,
            bool[] havebu,
            int nmain,
            int nslack)
        {
            int i = 0;

            for(i=0; i<=nmain-1; i++)
            {
                alglib.ap.assert((!havebl[i] || !havebu[i]) || (double)(bl[i])<=(double)(bu[i]), "ProjectGradientIntoBC: internal error (infeasible constraints)");
                if( (havebl[i] && (double)(x[i])<=(double)(bl[i])) && (double)(g[i])>(double)(0) )
                {
                    g[i] = 0;
                }
                if( (havebu[i] && (double)(x[i])>=(double)(bu[i])) && (double)(g[i])<(double)(0) )
                {
                    g[i] = 0;
                }
            }
            for(i=0; i<=nslack-1; i++)
            {
                if( (double)(x[nmain+i])<=(double)(0) && (double)(g[nmain+i])>(double)(0) )
                {
                    g[nmain+i] = 0;
                }
            }
        }


        /*************************************************************************
        Given
            a) initial point X0[NMain+NSlack]
               (feasible with respect to bound constraints)
            b) step vector alpha*D[NMain+NSlack]
            c) boundary constraints BndL[NMain], BndU[NMain]
            d) implicit non-negativity constraints for slack variables
        this  function  calculates  bound  on  the step length subject to boundary
        constraints.

        It returns:
            *  MaxStepLen - such step length that X0+MaxStepLen*alpha*D is exactly
               at the boundary given by constraints
            *  VariableToFreeze - index of the constraint to be activated,
               0 <= VariableToFreeze < NMain+NSlack
            *  ValueToFreeze - value of the corresponding constraint.

        Notes:
            * it is possible that several constraints can be activated by the step
              at once. In such cases only one constraint is returned. It is caller
              responsibility to check other constraints. This function makes  sure
              that we activate at least one constraint, and everything else is the
              responsibility of the caller.
            * steps smaller than MaxStepLen still can activate constraints due  to
              numerical errors. Thus purpose of this  function  is  not  to  guard 
              against accidental activation of the constraints - quite the reverse, 
              its purpose is to activate at least constraint upon performing  step
              which is too long.
            * in case there is no constraints to activate, we return negative
              VariableToFreeze and zero MaxStepLen and ValueToFreeze.
            * this function assumes that constraints are consistent; it throws
              exception otherwise.

        INPUT PARAMETERS
            X           -   array[NMain+NSlack], point. Must be feasible with respect 
                            to bound constraints (exception will be thrown otherwise)
            D           -   array[NMain+NSlack], step direction
            alpha       -   scalar multiplier before D, alpha<>0
            BndL        -   lower bounds, array[NMain]
                            (may contain -INF, when bound is not present)
            HaveBndL    -   array[NMain], if HaveBndL[i] is False,
                            then i-th bound is not present
            BndU        -   array[NMain], upper bounds
                            (may contain +INF, when bound is not present)
            HaveBndU    -   array[NMain], if HaveBndU[i] is False,
                            then i-th bound is not present
            NMain       -   number of main variables
            NSlack      -   number of slack variables
            
        OUTPUT PARAMETERS
            VariableToFreeze:
                            * negative value     = step is unbounded, ValueToFreeze=0,
                                                   MaxStepLen=0.
                            * non-negative value = at least one constraint, given by
                                                   this parameter, will  be  activated
                                                   upon performing maximum step.
            ValueToFreeze-  value of the variable which will be constrained
            MaxStepLen  -   maximum length of the step. Can be zero when step vector
                            looks outside of the feasible area.

          -- ALGLIB --
             Copyright 10.01.2012 by Bochkanov Sergey
        *************************************************************************/
        public static void calculatestepbound(double[] x,
            double[] d,
            double alpha,
            double[] bndl,
            bool[] havebndl,
            double[] bndu,
            bool[] havebndu,
            int nmain,
            int nslack,
            ref int variabletofreeze,
            ref double valuetofreeze,
            ref double maxsteplen)
        {
            int i = 0;
            double prevmax = 0;
            double initval = 0;

            variabletofreeze = 0;
            valuetofreeze = 0;
            maxsteplen = 0;

            alglib.ap.assert((double)(alpha)!=(double)(0), "CalculateStepBound: zero alpha");
            variabletofreeze = -1;
            initval = math.maxrealnumber;
            maxsteplen = initval;
            for(i=0; i<=nmain-1; i++)
            {
                if( havebndl[i] && (double)(alpha*d[i])<(double)(0) )
                {
                    alglib.ap.assert((double)(x[i])>=(double)(bndl[i]), "CalculateStepBound: infeasible X");
                    prevmax = maxsteplen;
                    maxsteplen = apserv.safeminposrv(x[i]-bndl[i], -(alpha*d[i]), maxsteplen);
                    if( (double)(maxsteplen)<(double)(prevmax) )
                    {
                        variabletofreeze = i;
                        valuetofreeze = bndl[i];
                    }
                }
                if( havebndu[i] && (double)(alpha*d[i])>(double)(0) )
                {
                    alglib.ap.assert((double)(x[i])<=(double)(bndu[i]), "CalculateStepBound: infeasible X");
                    prevmax = maxsteplen;
                    maxsteplen = apserv.safeminposrv(bndu[i]-x[i], alpha*d[i], maxsteplen);
                    if( (double)(maxsteplen)<(double)(prevmax) )
                    {
                        variabletofreeze = i;
                        valuetofreeze = bndu[i];
                    }
                }
            }
            for(i=0; i<=nslack-1; i++)
            {
                if( (double)(alpha*d[nmain+i])<(double)(0) )
                {
                    alglib.ap.assert((double)(x[nmain+i])>=(double)(0), "CalculateStepBound: infeasible X");
                    prevmax = maxsteplen;
                    maxsteplen = apserv.safeminposrv(x[nmain+i], -(alpha*d[nmain+i]), maxsteplen);
                    if( (double)(maxsteplen)<(double)(prevmax) )
                    {
                        variabletofreeze = nmain+i;
                        valuetofreeze = 0;
                    }
                }
            }
            if( (double)(maxsteplen)==(double)(initval) )
            {
                valuetofreeze = 0;
                maxsteplen = 0;
            }
        }


        /*************************************************************************
        This function postprocesses bounded step by:
        * analysing step length (whether it is equal to MaxStepLen) and activating 
          constraint given by VariableToFreeze if needed
        * checking for additional bound constraints to activate

        This function uses final point of the step, quantities calculated  by  the
        CalculateStepBound()  function.  As  result,  it  returns  point  which is 
        exactly feasible with respect to boundary constraints.

        NOTE 1: this function does NOT handle and check linear equality constraints
        NOTE 2: when StepTaken=MaxStepLen we always activate at least one constraint

        INPUT PARAMETERS
            X           -   array[NMain+NSlack], final point to postprocess
            XPrev       -   array[NMain+NSlack], initial point
            BndL        -   lower bounds, array[NMain]
                            (may contain -INF, when bound is not present)
            HaveBndL    -   array[NMain], if HaveBndL[i] is False,
                            then i-th bound is not present
            BndU        -   array[NMain], upper bounds
                            (may contain +INF, when bound is not present)
            HaveBndU    -   array[NMain], if HaveBndU[i] is False,
                            then i-th bound is not present
            NMain       -   number of main variables
            NSlack      -   number of slack variables
            VariableToFreeze-result of CalculateStepBound()
            ValueToFreeze-  result of CalculateStepBound()
            StepTaken   -   actual step length (actual step is equal to the possibly 
                            non-unit step direction vector times this parameter).
                            StepTaken<=MaxStepLen.
            MaxStepLen  -   result of CalculateStepBound()
            
        OUTPUT PARAMETERS
            X           -   point bounded with respect to constraints.
                            components corresponding to active constraints are exactly
                            equal to the boundary values.
                            
        RESULT:
            number of constraints activated in addition to previously active ones.
            Constraints which were DEACTIVATED are ignored (do not influence
            function value).

          -- ALGLIB --
             Copyright 10.01.2012 by Bochkanov Sergey
        *************************************************************************/
        public static int postprocessboundedstep(ref double[] x,
            double[] xprev,
            double[] bndl,
            bool[] havebndl,
            double[] bndu,
            bool[] havebndu,
            int nmain,
            int nslack,
            int variabletofreeze,
            double valuetofreeze,
            double steptaken,
            double maxsteplen)
        {
            int result = 0;
            int i = 0;
            bool wasactivated = new bool();

            alglib.ap.assert(variabletofreeze<0 || (double)(steptaken)<=(double)(maxsteplen));
            
            //
            // Activate constraints
            //
            if( variabletofreeze>=0 && (double)(steptaken)==(double)(maxsteplen) )
            {
                x[variabletofreeze] = valuetofreeze;
            }
            for(i=0; i<=nmain-1; i++)
            {
                if( havebndl[i] && (double)(x[i])<(double)(bndl[i]) )
                {
                    x[i] = bndl[i];
                }
                if( havebndu[i] && (double)(x[i])>(double)(bndu[i]) )
                {
                    x[i] = bndu[i];
                }
            }
            for(i=0; i<=nslack-1; i++)
            {
                if( (double)(x[nmain+i])<=(double)(0) )
                {
                    x[nmain+i] = 0;
                }
            }
            
            //
            // Calculate number of constraints being activated
            //
            result = 0;
            for(i=0; i<=nmain-1; i++)
            {
                wasactivated = (double)(x[i])!=(double)(xprev[i]) && ((havebndl[i] && (double)(x[i])==(double)(bndl[i])) || (havebndu[i] && (double)(x[i])==(double)(bndu[i])));
                wasactivated = wasactivated || variabletofreeze==i;
                if( wasactivated )
                {
                    result = result+1;
                }
            }
            for(i=0; i<=nslack-1; i++)
            {
                wasactivated = (double)(x[nmain+i])!=(double)(xprev[nmain+i]) && (double)(x[nmain+i])==(double)(0.0);
                wasactivated = wasactivated || variabletofreeze==nmain+i;
                if( wasactivated )
                {
                    result = result+1;
                }
            }
            return result;
        }


        /*************************************************************************
        The  purpose  of  this  function is to prevent algorithm from "unsticking" 
        from  the  active  bound  constraints  because  of  numerical noise in the
        gradient or Hessian.

        It is done by zeroing some components of the search direction D.  D[i]  is
        zeroed when both (a) and (b) are true:
        a) corresponding X[i] is exactly at the boundary
        b) |D[i]*S[i]| <= DropTol*Sqrt(SUM(D[i]^2*S[I]^2))

        D  can  be  step  direction , antigradient, gradient, or anything similar. 
        Sign of D does not matter, nor matters step length.

        NOTE 1: boundary constraints are expected to be consistent, as well as X
                is expected to be feasible. Exception will be thrown otherwise.

        INPUT PARAMETERS
            D           -   array[NMain+NSlack], direction
            X           -   array[NMain+NSlack], current point
            BndL        -   lower bounds, array[NMain]
                            (may contain -INF, when bound is not present)
            HaveBndL    -   array[NMain], if HaveBndL[i] is False,
                            then i-th bound is not present
            BndU        -   array[NMain], upper bounds
                            (may contain +INF, when bound is not present)
            HaveBndU    -   array[NMain], if HaveBndU[i] is False,
                            then i-th bound is not present
            S           -   array[NMain+NSlack], scaling of the variables
            NMain       -   number of main variables
            NSlack      -   number of slack variables
            DropTol     -   drop tolerance, >=0
            
        OUTPUT PARAMETERS
            X           -   point bounded with respect to constraints.
                            components corresponding to active constraints are exactly
                            equal to the boundary values.

          -- ALGLIB --
             Copyright 10.01.2012 by Bochkanov Sergey
        *************************************************************************/
        public static void filterdirection(ref double[] d,
            double[] x,
            double[] bndl,
            bool[] havebndl,
            double[] bndu,
            bool[] havebndu,
            double[] s,
            int nmain,
            int nslack,
            double droptol)
        {
            int i = 0;
            double scalednorm = 0;
            bool isactive = new bool();

            scalednorm = 0.0;
            for(i=0; i<=nmain+nslack-1; i++)
            {
                scalednorm = scalednorm+math.sqr(d[i]*s[i]);
            }
            scalednorm = Math.Sqrt(scalednorm);
            for(i=0; i<=nmain-1; i++)
            {
                alglib.ap.assert(!havebndl[i] || (double)(x[i])>=(double)(bndl[i]), "FilterDirection: infeasible point");
                alglib.ap.assert(!havebndu[i] || (double)(x[i])<=(double)(bndu[i]), "FilterDirection: infeasible point");
                isactive = (havebndl[i] && (double)(x[i])==(double)(bndl[i])) || (havebndu[i] && (double)(x[i])==(double)(bndu[i]));
                if( isactive && (double)(Math.Abs(d[i]*s[i]))<=(double)(droptol*scalednorm) )
                {
                    d[i] = 0.0;
                }
            }
            for(i=0; i<=nslack-1; i++)
            {
                alglib.ap.assert((double)(x[nmain+i])>=(double)(0), "FilterDirection: infeasible point");
                if( (double)(x[nmain+i])==(double)(0) && (double)(Math.Abs(d[nmain+i]*s[nmain+i]))<=(double)(droptol*scalednorm) )
                {
                    d[nmain+i] = 0.0;
                }
            }
        }


        /*************************************************************************
        This function returns number of bound constraints whose state was  changed
        (either activated or deactivated) when making step from XPrev to X.

        Constraints are considered:
        * active - when we are exactly at the boundary
        * inactive - when we are not at the boundary

        You should note that antigradient direction is NOT taken into account when
        we make decions on the constraint status.

        INPUT PARAMETERS
            X           -   array[NMain+NSlack], final point.
                            Must be feasible with respect to bound constraints.
            XPrev       -   array[NMain+NSlack], initial point.
                            Must be feasible with respect to bound constraints.
            BndL        -   lower bounds, array[NMain]
                            (may contain -INF, when bound is not present)
            HaveBndL    -   array[NMain], if HaveBndL[i] is False,
                            then i-th bound is not present
            BndU        -   array[NMain], upper bounds
                            (may contain +INF, when bound is not present)
            HaveBndU    -   array[NMain], if HaveBndU[i] is False,
                            then i-th bound is not present
            NMain       -   number of main variables
            NSlack      -   number of slack variables
            
        RESULT:
            number of constraints whose state was changed.

          -- ALGLIB --
             Copyright 10.01.2012 by Bochkanov Sergey
        *************************************************************************/
        public static int numberofchangedconstraints(double[] x,
            double[] xprev,
            double[] bndl,
            bool[] havebndl,
            double[] bndu,
            bool[] havebndu,
            int nmain,
            int nslack)
        {
            int result = 0;
            int i = 0;
            bool statuschanged = new bool();

            result = 0;
            for(i=0; i<=nmain-1; i++)
            {
                if( (double)(x[i])!=(double)(xprev[i]) )
                {
                    statuschanged = false;
                    if( havebndl[i] && ((double)(x[i])==(double)(bndl[i]) || (double)(xprev[i])==(double)(bndl[i])) )
                    {
                        statuschanged = true;
                    }
                    if( havebndu[i] && ((double)(x[i])==(double)(bndu[i]) || (double)(xprev[i])==(double)(bndu[i])) )
                    {
                        statuschanged = true;
                    }
                    if( statuschanged )
                    {
                        result = result+1;
                    }
                }
            }
            for(i=0; i<=nslack-1; i++)
            {
                if( (double)(x[nmain+i])!=(double)(xprev[nmain+i]) && ((double)(x[nmain+i])==(double)(0) || (double)(xprev[nmain+i])==(double)(0)) )
                {
                    result = result+1;
                }
            }
            return result;
        }


        /*************************************************************************
        This function finds feasible point of  (NMain+NSlack)-dimensional  problem
        subject to NMain explicit boundary constraints (some  constraints  can  be
        omitted), NSlack implicit non-negativity constraints,  K  linear  equality
        constraints.

        INPUT PARAMETERS
            X           -   array[NMain+NSlack], initial point.
            BndL        -   lower bounds, array[NMain]
                            (may contain -INF, when bound is not present)
            HaveBndL    -   array[NMain], if HaveBndL[i] is False,
                            then i-th bound is not present
            BndU        -   array[NMain], upper bounds
                            (may contain +INF, when bound is not present)
            HaveBndU    -   array[NMain], if HaveBndU[i] is False,
                            then i-th bound is not present
            NMain       -   number of main variables
            NSlack      -   number of slack variables
            CE          -   array[K,NMain+NSlack+1], equality  constraints CE*x=b.
                            Rows contain constraints, first  NMain+NSlack  columns
                            contain coefficients before X[], last  column  contain
                            right part.
            K           -   number of linear constraints
            EpsI        -   infeasibility (error in the right part) allowed in the
                            solution

        OUTPUT PARAMETERS:
            X           -   feasible point or best infeasible point found before
                            algorithm termination
            QPIts       -   number of QP iterations (for debug purposes)
            GPAIts      -   number of GPA iterations (for debug purposes)
            
        RESULT:
            True in case X is feasible, False - if it is infeasible.

          -- ALGLIB --
             Copyright 20.01.2012 by Bochkanov Sergey
        *************************************************************************/
        public static bool findfeasiblepoint(ref double[] x,
            double[] bndl,
            bool[] havebndl,
            double[] bndu,
            bool[] havebndu,
            int nmain,
            int nslack,
            double[,] ce,
            int k,
            double epsi,
            ref int qpits,
            ref int gpaits)
        {
            bool result = new bool();
            int i = 0;
            int j = 0;
            int idx0 = 0;
            int idx1 = 0;
            double[] permx = new double[0];
            double[] xn = new double[0];
            double[] xa = new double[0];
            double[] newtonstep = new double[0];
            double[] g = new double[0];
            double[] pg = new double[0];
            double[,] a = new double[0,0];
            double armijostep = 0;
            double armijobeststep = 0;
            double armijobestfeas = 0;
            double v = 0;
            double mx = 0;
            double feaserr = 0;
            double feasold = 0;
            double feasnew = 0;
            double pgnorm = 0;
            double vn = 0;
            double vd = 0;
            double stp = 0;
            int vartofreeze = 0;
            double valtofreeze = 0;
            double maxsteplen = 0;
            bool werechangesinconstraints = new bool();
            bool stage1isover = new bool();
            bool converged = new bool();
            double[] activeconstraints = new double[0];
            double[] tmpk = new double[0];
            double[] colnorms = new double[0];
            int nactive = 0;
            int nfree = 0;
            int nsvd = 0;
            int[] p1 = new int[0];
            int[] p2 = new int[0];
            apserv.apbuffers buf = new apserv.apbuffers();
            double[] w = new double[0];
            double[] s = new double[0];
            double[,] u = new double[0,0];
            double[,] vt = new double[0,0];
            int itscount = 0;
            int itswithintolerance = 0;
            int maxitswithintolerance = 0;
            int gparuns = 0;
            int maxgparuns = 0;
            int maxarmijoruns = 0;
            int i_ = 0;

            ce = (double[,])ce.Clone();
            qpits = 0;
            gpaits = 0;

            maxitswithintolerance = 3;
            maxgparuns = 3;
            maxarmijoruns = 5;
            qpits = 0;
            gpaits = 0;
            
            //
            // Initial enforcement of the feasibility with respect to boundary constraints
            // NOTE: after this block we assume that boundary constraints are consistent.
            //
            if( !enforceboundaryconstraints(ref x, bndl, havebndl, bndu, havebndu, nmain, nslack) )
            {
                result = false;
                return result;
            }
            if( k==0 )
            {
                
                //
                // No linear constraints, we can exit right now
                //
                result = true;
                return result;
            }
            
            //
            // Scale rows of CE in such way that max(CE[i,0..nmain+nslack-1])=1 for any i=0..k-1
            //
            for(i=0; i<=k-1; i++)
            {
                v = 0.0;
                for(j=0; j<=nmain+nslack-1; j++)
                {
                    v = Math.Max(v, Math.Abs(ce[i,j]));
                }
                if( (double)(v)!=(double)(0) )
                {
                    v = 1/v;
                    for(i_=0; i_<=nmain+nslack;i_++)
                    {
                        ce[i,i_] = v*ce[i,i_];
                    }
                }
            }
            
            //
            // Allocate temporaries
            //
            xn = new double[nmain+nslack];
            xa = new double[nmain+nslack];
            permx = new double[nmain+nslack];
            g = new double[nmain+nslack];
            pg = new double[nmain+nslack];
            tmpk = new double[k];
            a = new double[k, nmain+nslack];
            activeconstraints = new double[nmain+nslack];
            newtonstep = new double[nmain+nslack];
            s = new double[nmain+nslack];
            colnorms = new double[nmain+nslack];
            for(i=0; i<=nmain+nslack-1; i++)
            {
                s[i] = 1.0;
                colnorms[i] = 0.0;
                for(j=0; j<=k-1; j++)
                {
                    colnorms[i] = colnorms[i]+math.sqr(ce[j,i]);
                }
            }
            
            //
            // K>0, we have linear equality constraints combined with bound constraints.
            //
            // Try to find feasible point as minimizer of the quadratic function
            //     F(x) = 0.5*||CE*x-b||^2 = 0.5*x'*(CE'*CE)*x - (b'*CE)*x + 0.5*b'*b
            // subject to boundary constraints given by BL, BU and non-negativity of
            // the slack variables. BTW, we drop constant term because it does not
            // actually influences on the solution.
            //
            // Below we will assume that K>0.
            //
            itswithintolerance = 0;
            itscount = 0;
            while( true )
            {
                
                //
                // Stage 0: check for exact convergence
                //
                converged = true;
                feaserr = 0;
                for(i=0; i<=k-1; i++)
                {
                    
                    //
                    // Calculate:
                    // * V - error in the right part
                    // * MX - maximum term in the left part
                    //
                    // Terminate if error in the right part is not greater than 100*Eps*MX.
                    //
                    // IMPORTANT: we must perform check for non-strict inequality, i.e. to use <= instead of <.
                    //            it will allow us to easily handle situations with zero rows of CE.
                    //
                    mx = 0;
                    v = -ce[i,nmain+nslack];
                    for(j=0; j<=nmain+nslack-1; j++)
                    {
                        mx = Math.Max(mx, Math.Abs(ce[i,j]*x[j]));
                        v = v+ce[i,j]*x[j];
                    }
                    feaserr = feaserr+math.sqr(v);
                    converged = converged && (double)(Math.Abs(v))<=(double)(100*math.machineepsilon*mx);
                }
                feaserr = Math.Sqrt(feaserr);
                if( converged )
                {
                    result = (double)(feaserr)<=(double)(epsi);
                    return result;
                }
                
                //
                // Stage 1: equality constrained quadratic programming
                //
                // * treat active bound constraints as equality ones (constraint is considered 
                //   active when we are at the boundary, independently of the antigradient direction)
                // * calculate unrestricted Newton step to point XM (which may be infeasible)
                //   calculate MaxStepLen = largest step in direction of XM which retains feasibility.
                // * perform bounded step from X to XN:
                //   a) XN=XM                  (if XM is feasible)
                //   b) XN=X-MaxStepLen*(XM-X) (otherwise)
                // * X := XN
                // * if XM (Newton step subject to currently active constraints) was feasible, goto Stage 2
                // * repeat Stage 1
                //
                // NOTE 1: in order to solve constrained qudratic subproblem we will have to reorder
                //         variables in such way that ones corresponding to inactive constraints will
                //         be first, and active ones will be last in the list. CE and X are now
                //                                                       [ xi ]
                //         separated into two parts: CE = [CEi CEa], x = [    ], where CEi/Xi correspond
                //                                                       [ xa ]
                //         to INACTIVE constraints, and CEa/Xa correspond to the ACTIVE ones.
                //
                //         Now, instead of F=0.5*x'*(CE'*CE)*x - (b'*CE)*x + 0.5*b'*b, we have
                //         F(xi) = 0.5*(CEi*xi,CEi*xi) + (CEa*xa-b,CEi*xi) + (0.5*CEa*xa-b,CEa*xa).
                //         Here xa is considered constant, i.e. we optimize with respect to xi, leaving xa fixed.
                //
                //         We can solve it by performing SVD of CEi and calculating pseudoinverse of the
                //         Hessian matrix. Of course, we do NOT calculate pseudoinverse explicitly - we
                //         just use singular vectors to perform implicit multiplication by it.
                //
                //
                while( true )
                {
                    
                    //
                    // Calculate G - gradient subject to equality constraints,
                    // multiply it by inverse of the Hessian diagonal to obtain initial
                    // step vector.
                    //
                    // Bound step subject to constraints which can be activated,
                    // run Armijo search with increasing step size.
                    // Search is terminated when feasibility error stops to decrease.
                    //
                    // NOTE: it is important to test for "stops to decrease" instead
                    // of "starts to increase" in order to correctly handle cases with
                    // zero CE.
                    //
                    armijobeststep = 0.0;
                    armijobestfeas = 0.0;
                    for(i=0; i<=nmain+nslack-1; i++)
                    {
                        g[i] = 0;
                    }
                    for(i=0; i<=k-1; i++)
                    {
                        v = 0.0;
                        for(i_=0; i_<=nmain+nslack-1;i_++)
                        {
                            v += ce[i,i_]*x[i_];
                        }
                        v = v-ce[i,nmain+nslack];
                        armijobestfeas = armijobestfeas+math.sqr(v);
                        for(i_=0; i_<=nmain+nslack-1;i_++)
                        {
                            g[i_] = g[i_] + v*ce[i,i_];
                        }
                    }
                    armijobestfeas = Math.Sqrt(armijobestfeas);
                    for(i=0; i<=nmain-1; i++)
                    {
                        if( havebndl[i] && (double)(x[i])==(double)(bndl[i]) )
                        {
                            g[i] = 0.0;
                        }
                        if( havebndu[i] && (double)(x[i])==(double)(bndu[i]) )
                        {
                            g[i] = 0.0;
                        }
                    }
                    for(i=0; i<=nslack-1; i++)
                    {
                        if( (double)(x[nmain+i])==(double)(0.0) )
                        {
                            g[nmain+i] = 0.0;
                        }
                    }
                    v = 0.0;
                    for(i=0; i<=nmain+nslack-1; i++)
                    {
                        if( (double)(math.sqr(colnorms[i]))!=(double)(0) )
                        {
                            newtonstep[i] = -(g[i]/math.sqr(colnorms[i]));
                        }
                        else
                        {
                            newtonstep[i] = 0.0;
                        }
                        v = v+math.sqr(newtonstep[i]);
                    }
                    if( (double)(v)==(double)(0) )
                    {
                        
                        //
                        // Constrained gradient is zero, QP iterations are over
                        //
                        break;
                    }
                    calculatestepbound(x, newtonstep, 1.0, bndl, havebndl, bndu, havebndu, nmain, nslack, ref vartofreeze, ref valtofreeze, ref maxsteplen);
                    if( vartofreeze>=0 && (double)(maxsteplen)==(double)(0) )
                    {
                        
                        //
                        // Can not perform step, QP iterations are over
                        //
                        break;
                    }
                    if( vartofreeze>=0 )
                    {
                        armijostep = Math.Min(1.0, maxsteplen);
                    }
                    else
                    {
                        armijostep = 1;
                    }
                    while( true )
                    {
                        for(i_=0; i_<=nmain+nslack-1;i_++)
                        {
                            xa[i_] = x[i_];
                        }
                        for(i_=0; i_<=nmain+nslack-1;i_++)
                        {
                            xa[i_] = xa[i_] + armijostep*newtonstep[i_];
                        }
                        enforceboundaryconstraints(ref xa, bndl, havebndl, bndu, havebndu, nmain, nslack);
                        feaserr = 0.0;
                        for(i=0; i<=k-1; i++)
                        {
                            v = 0.0;
                            for(i_=0; i_<=nmain+nslack-1;i_++)
                            {
                                v += ce[i,i_]*xa[i_];
                            }
                            v = v-ce[i,nmain+nslack];
                            feaserr = feaserr+math.sqr(v);
                        }
                        feaserr = Math.Sqrt(feaserr);
                        if( (double)(feaserr)>=(double)(armijobestfeas) )
                        {
                            break;
                        }
                        armijobestfeas = feaserr;
                        armijobeststep = armijostep;
                        armijostep = 2.0*armijostep;
                    }
                    for(i_=0; i_<=nmain+nslack-1;i_++)
                    {
                        x[i_] = x[i_] + armijobeststep*newtonstep[i_];
                    }
                    enforceboundaryconstraints(ref x, bndl, havebndl, bndu, havebndu, nmain, nslack);
                    
                    //
                    // Determine number of active and free constraints
                    //
                    nactive = 0;
                    for(i=0; i<=nmain-1; i++)
                    {
                        activeconstraints[i] = 0;
                        if( havebndl[i] && (double)(x[i])==(double)(bndl[i]) )
                        {
                            activeconstraints[i] = 1;
                        }
                        if( havebndu[i] && (double)(x[i])==(double)(bndu[i]) )
                        {
                            activeconstraints[i] = 1;
                        }
                        if( (double)(activeconstraints[i])>(double)(0) )
                        {
                            nactive = nactive+1;
                        }
                    }
                    for(i=0; i<=nslack-1; i++)
                    {
                        activeconstraints[nmain+i] = 0;
                        if( (double)(x[nmain+i])==(double)(0.0) )
                        {
                            activeconstraints[nmain+i] = 1;
                        }
                        if( (double)(activeconstraints[nmain+i])>(double)(0) )
                        {
                            nactive = nactive+1;
                        }
                    }
                    nfree = nmain+nslack-nactive;
                    if( nfree==0 )
                    {
                        break;
                    }
                    qpits = qpits+1;
                    
                    //
                    // Reorder variables
                    //
                    tsort.tagsortbuf(ref activeconstraints, nmain+nslack, ref p1, ref p2, buf);
                    for(i=0; i<=k-1; i++)
                    {
                        for(j=0; j<=nmain+nslack-1; j++)
                        {
                            a[i,j] = ce[i,j];
                        }
                    }
                    for(j=0; j<=nmain+nslack-1; j++)
                    {
                        permx[j] = x[j];
                    }
                    for(j=0; j<=nmain+nslack-1; j++)
                    {
                        if( p2[j]!=j )
                        {
                            idx0 = p2[j];
                            idx1 = j;
                            for(i=0; i<=k-1; i++)
                            {
                                v = a[i,idx0];
                                a[i,idx0] = a[i,idx1];
                                a[i,idx1] = v;
                            }
                            v = permx[idx0];
                            permx[idx0] = permx[idx1];
                            permx[idx1] = v;
                        }
                    }
                    
                    //
                    // Calculate (unprojected) gradient:
                    // G(xi) = CEi'*(CEi*xi + CEa*xa - b)
                    //
                    for(i=0; i<=nfree-1; i++)
                    {
                        g[i] = 0;
                    }
                    for(i=0; i<=k-1; i++)
                    {
                        v = 0.0;
                        for(i_=0; i_<=nmain+nslack-1;i_++)
                        {
                            v += a[i,i_]*permx[i_];
                        }
                        tmpk[i] = v-ce[i,nmain+nslack];
                    }
                    for(i=0; i<=k-1; i++)
                    {
                        v = tmpk[i];
                        for(i_=0; i_<=nfree-1;i_++)
                        {
                            g[i_] = g[i_] + v*a[i,i_];
                        }
                    }
                    
                    //
                    // Calculate Newton step using SVD of CEi:
                    //     F(xi)  = 0.5*xi'*H*xi + g'*xi    (Taylor decomposition)
                    //     XN     = -H^(-1)*g               (new point, solution of the QP subproblem)
                    //     H      = CEi'*CEi                
                    //     CEi    = U*W*V'                  (SVD of CEi)
                    //     H      = V*W^2*V'                 
                    //     H^(-1) = V*W^(-2)*V'
                    //     step     = -V*W^(-2)*V'*g          (it is better to perform multiplication from right to left)
                    //
                    // NOTE 1: we do NOT need left singular vectors to perform Newton step.
                    //
                    nsvd = Math.Min(k, nfree);
                    if( !svd.rmatrixsvd(a, k, nfree, 0, 1, 2, ref w, ref u, ref vt) )
                    {
                        result = false;
                        return result;
                    }
                    for(i=0; i<=nsvd-1; i++)
                    {
                        v = 0.0;
                        for(i_=0; i_<=nfree-1;i_++)
                        {
                            v += vt[i,i_]*g[i_];
                        }
                        tmpk[i] = v;
                    }
                    for(i=0; i<=nsvd-1; i++)
                    {
                        
                        //
                        // It is important to have strict ">" in order to correctly 
                        // handle zero singular values.
                        //
                        if( (double)(math.sqr(w[i]))>(double)(math.sqr(w[0])*(nmain+nslack)*math.machineepsilon) )
                        {
                            tmpk[i] = tmpk[i]/math.sqr(w[i]);
                        }
                        else
                        {
                            tmpk[i] = 0;
                        }
                    }
                    for(i=0; i<=nmain+nslack-1; i++)
                    {
                        newtonstep[i] = 0;
                    }
                    for(i=0; i<=nsvd-1; i++)
                    {
                        v = tmpk[i];
                        for(i_=0; i_<=nfree-1;i_++)
                        {
                            newtonstep[i_] = newtonstep[i_] - v*vt[i,i_];
                        }
                    }
                    for(j=nmain+nslack-1; j>=0; j--)
                    {
                        if( p2[j]!=j )
                        {
                            idx0 = p2[j];
                            idx1 = j;
                            v = newtonstep[idx0];
                            newtonstep[idx0] = newtonstep[idx1];
                            newtonstep[idx1] = v;
                        }
                    }
                    
                    //
                    // NewtonStep contains Newton step subject to active bound constraints.
                    //
                    // Such step leads us to the minimizer of the equality constrained F,
                    // but such minimizer may be infeasible because some constraints which
                    // are inactive at the initial point can be violated at the solution.
                    //
                    // Thus, we perform optimization in two stages:
                    // a) perform bounded Newton step, i.e. step in the Newton direction
                    //    until activation of the first constraint
                    // b) in case (MaxStepLen>0)and(MaxStepLen<1), perform additional iteration
                    //    of the Armijo line search in the rest of the Newton direction.
                    //
                    calculatestepbound(x, newtonstep, 1.0, bndl, havebndl, bndu, havebndu, nmain, nslack, ref vartofreeze, ref valtofreeze, ref maxsteplen);
                    if( vartofreeze>=0 && (double)(maxsteplen)==(double)(0) )
                    {
                        
                        //
                        // Activation of the constraints prevent us from performing step,
                        // QP iterations are over
                        //
                        break;
                    }
                    if( vartofreeze>=0 )
                    {
                        v = Math.Min(1.0, maxsteplen);
                    }
                    else
                    {
                        v = 1.0;
                    }
                    for(i_=0; i_<=nmain+nslack-1;i_++)
                    {
                        xn[i_] = v*newtonstep[i_];
                    }
                    for(i_=0; i_<=nmain+nslack-1;i_++)
                    {
                        xn[i_] = xn[i_] + x[i_];
                    }
                    postprocessboundedstep(ref xn, x, bndl, havebndl, bndu, havebndu, nmain, nslack, vartofreeze, valtofreeze, v, maxsteplen);
                    if( (double)(maxsteplen)>(double)(0) && (double)(maxsteplen)<(double)(1) )
                    {
                        
                        //
                        // Newton step was restricted by activation of the constraints,
                        // perform Armijo iteration.
                        //
                        // Initial estimate for best step is zero step. We try different
                        // step sizes, from the 1-MaxStepLen (residual of the full Newton
                        // step) to progressively smaller and smaller steps.
                        //
                        armijobeststep = 0.0;
                        armijobestfeas = 0.0;
                        for(i=0; i<=k-1; i++)
                        {
                            v = 0.0;
                            for(i_=0; i_<=nmain+nslack-1;i_++)
                            {
                                v += ce[i,i_]*xn[i_];
                            }
                            v = v-ce[i,nmain+nslack];
                            armijobestfeas = armijobestfeas+math.sqr(v);
                        }
                        armijobestfeas = Math.Sqrt(armijobestfeas);
                        armijostep = 1-maxsteplen;
                        for(j=0; j<=maxarmijoruns-1; j++)
                        {
                            for(i_=0; i_<=nmain+nslack-1;i_++)
                            {
                                xa[i_] = xn[i_];
                            }
                            for(i_=0; i_<=nmain+nslack-1;i_++)
                            {
                                xa[i_] = xa[i_] + armijostep*newtonstep[i_];
                            }
                            enforceboundaryconstraints(ref xa, bndl, havebndl, bndu, havebndu, nmain, nslack);
                            feaserr = 0.0;
                            for(i=0; i<=k-1; i++)
                            {
                                v = 0.0;
                                for(i_=0; i_<=nmain+nslack-1;i_++)
                                {
                                    v += ce[i,i_]*xa[i_];
                                }
                                v = v-ce[i,nmain+nslack];
                                feaserr = feaserr+math.sqr(v);
                            }
                            feaserr = Math.Sqrt(feaserr);
                            if( (double)(feaserr)<(double)(armijobestfeas) )
                            {
                                armijobestfeas = feaserr;
                                armijobeststep = armijostep;
                            }
                            armijostep = 0.5*armijostep;
                        }
                        for(i_=0; i_<=nmain+nslack-1;i_++)
                        {
                            xa[i_] = xn[i_];
                        }
                        for(i_=0; i_<=nmain+nslack-1;i_++)
                        {
                            xa[i_] = xa[i_] + armijobeststep*newtonstep[i_];
                        }
                        enforceboundaryconstraints(ref xa, bndl, havebndl, bndu, havebndu, nmain, nslack);
                    }
                    else
                    {
                        
                        //
                        // Armijo iteration is not performed
                        //
                        for(i_=0; i_<=nmain+nslack-1;i_++)
                        {
                            xa[i_] = xn[i_];
                        }
                    }
                    stage1isover = (double)(maxsteplen)>=(double)(1) || (double)(maxsteplen)==(double)(0);
                    
                    //
                    // Calculate feasibility errors for old and new X.
                    // These quantinies are used for debugging purposes only.
                    // However, we can leave them in release code because performance impact is insignificant.
                    //
                    // Update X. Exit if needed.
                    //
                    feasold = 0;
                    feasnew = 0;
                    for(i=0; i<=k-1; i++)
                    {
                        v = 0.0;
                        for(i_=0; i_<=nmain+nslack-1;i_++)
                        {
                            v += ce[i,i_]*x[i_];
                        }
                        feasold = feasold+math.sqr(v-ce[i,nmain+nslack]);
                        v = 0.0;
                        for(i_=0; i_<=nmain+nslack-1;i_++)
                        {
                            v += ce[i,i_]*xa[i_];
                        }
                        feasnew = feasnew+math.sqr(v-ce[i,nmain+nslack]);
                    }
                    feasold = Math.Sqrt(feasold);
                    feasnew = Math.Sqrt(feasnew);
                    if( (double)(feasnew)>=(double)(feasold) )
                    {
                        break;
                    }
                    for(i_=0; i_<=nmain+nslack-1;i_++)
                    {
                        x[i_] = xa[i_];
                    }
                    if( stage1isover )
                    {
                        break;
                    }
                }
                
                //
                // Stage 2: gradient projection algorithm (GPA)
                //
                // * calculate feasibility error (with respect to linear equality constraints)
                // * calculate gradient G of F, project it into feasible area (G => PG)
                // * exit if norm(PG) is exactly zero or feasibility error is smaller than EpsC
                // * let XM be exact minimum of F along -PG (XM may be infeasible).
                //   calculate MaxStepLen = largest step in direction of -PG which retains feasibility.
                // * perform bounded step from X to XN:
                //   a) XN=XM              (if XM is feasible)
                //   b) XN=X-MaxStepLen*PG (otherwise)
                // * X := XN
                // * stop after specified number of iterations or when no new constraints was activated
                //
                // NOTES:
                // * grad(F) = (CE'*CE)*x - (b'*CE)^T
                // * CE[i] denotes I-th row of CE
                // * XM = X+stp*(-PG) where stp=(grad(F(X)),PG)/(CE*PG,CE*PG).
                //   Here PG is a projected gradient, but in fact it can be arbitrary non-zero 
                //   direction vector - formula for minimum of F along PG still will be correct.
                //
                werechangesinconstraints = false;
                for(gparuns=1; gparuns<=k; gparuns++)
                {
                    
                    //
                    // calculate feasibility error and G
                    //
                    feaserr = 0;
                    for(i=0; i<=nmain+nslack-1; i++)
                    {
                        g[i] = 0;
                    }
                    for(i=0; i<=k-1; i++)
                    {
                        
                        //
                        // G += CE[i]^T * (CE[i]*x-b[i])
                        //
                        v = 0.0;
                        for(i_=0; i_<=nmain+nslack-1;i_++)
                        {
                            v += ce[i,i_]*x[i_];
                        }
                        v = v-ce[i,nmain+nslack];
                        feaserr = feaserr+math.sqr(v);
                        for(i_=0; i_<=nmain+nslack-1;i_++)
                        {
                            g[i_] = g[i_] + v*ce[i,i_];
                        }
                    }
                    
                    //
                    // project G, filter it (strip numerical noise)
                    //
                    for(i_=0; i_<=nmain+nslack-1;i_++)
                    {
                        pg[i_] = g[i_];
                    }
                    projectgradientintobc(x, ref pg, bndl, havebndl, bndu, havebndu, nmain, nslack);
                    filterdirection(ref pg, x, bndl, havebndl, bndu, havebndu, s, nmain, nslack, 1.0E-9);
                    for(i=0; i<=nmain+nslack-1; i++)
                    {
                        if( (double)(math.sqr(colnorms[i]))!=(double)(0) )
                        {
                            pg[i] = pg[i]/math.sqr(colnorms[i]);
                        }
                        else
                        {
                            pg[i] = 0.0;
                        }
                    }
                    
                    //
                    // Check GNorm and feasibility.
                    // Exit when GNorm is exactly zero.
                    //
                    pgnorm = 0.0;
                    for(i_=0; i_<=nmain+nslack-1;i_++)
                    {
                        pgnorm += pg[i_]*pg[i_];
                    }
                    feaserr = Math.Sqrt(feaserr);
                    pgnorm = Math.Sqrt(pgnorm);
                    if( (double)(pgnorm)==(double)(0) )
                    {
                        result = (double)(feaserr)<=(double)(epsi);
                        return result;
                    }
                    
                    //
                    // calculate planned step length
                    //
                    vn = 0.0;
                    for(i_=0; i_<=nmain+nslack-1;i_++)
                    {
                        vn += g[i_]*pg[i_];
                    }
                    vd = 0;
                    for(i=0; i<=k-1; i++)
                    {
                        v = 0.0;
                        for(i_=0; i_<=nmain+nslack-1;i_++)
                        {
                            v += ce[i,i_]*pg[i_];
                        }
                        vd = vd+math.sqr(v);
                    }
                    stp = vn/vd;
                    
                    //
                    // Calculate step bound.
                    // Perform bounded step and post-process it
                    //
                    calculatestepbound(x, pg, -1.0, bndl, havebndl, bndu, havebndu, nmain, nslack, ref vartofreeze, ref valtofreeze, ref maxsteplen);
                    if( vartofreeze>=0 && (double)(maxsteplen)==(double)(0) )
                    {
                        result = false;
                        return result;
                    }
                    if( vartofreeze>=0 )
                    {
                        v = Math.Min(stp, maxsteplen);
                    }
                    else
                    {
                        v = stp;
                    }
                    for(i_=0; i_<=nmain+nslack-1;i_++)
                    {
                        xn[i_] = x[i_];
                    }
                    for(i_=0; i_<=nmain+nslack-1;i_++)
                    {
                        xn[i_] = xn[i_] - v*pg[i_];
                    }
                    postprocessboundedstep(ref xn, x, bndl, havebndl, bndu, havebndu, nmain, nslack, vartofreeze, valtofreeze, v, maxsteplen);
                    
                    //
                    // update X
                    // check stopping criteria
                    //
                    werechangesinconstraints = werechangesinconstraints || numberofchangedconstraints(xn, x, bndl, havebndl, bndu, havebndu, nmain, nslack)>0;
                    for(i_=0; i_<=nmain+nslack-1;i_++)
                    {
                        x[i_] = xn[i_];
                    }
                    gpaits = gpaits+1;
                    if( !werechangesinconstraints )
                    {
                        break;
                    }
                }
                
                //
                // Stage 3: decide to stop algorithm or not to stop
                //
                // 1. we can stop when last GPA run did NOT changed constraints status.
                //    It means that we've found final set of the active constraints even
                //    before GPA made its run. And it means that Newton step moved us to
                //    the minimum subject to the present constraints.
                //    Depending on feasibility error, True or False is returned.
                //
                feaserr = 0;
                for(i=0; i<=k-1; i++)
                {
                    v = 0.0;
                    for(i_=0; i_<=nmain+nslack-1;i_++)
                    {
                        v += ce[i,i_]*x[i_];
                    }
                    v = v-ce[i,nmain+nslack];
                    feaserr = feaserr+math.sqr(v);
                }
                feaserr = Math.Sqrt(feaserr);
                if( (double)(feaserr)<=(double)(epsi) )
                {
                    itswithintolerance = itswithintolerance+1;
                }
                else
                {
                    itswithintolerance = 0;
                }
                if( !werechangesinconstraints || itswithintolerance>=maxitswithintolerance )
                {
                    result = (double)(feaserr)<=(double)(epsi);
                    return result;
                }
                itscount = itscount+1;
            }
            return result;
        }


        /*************************************************************************
            This function check, that input derivatives are right. First it scale
        parameters DF0 and DF1 from segment [A;B] to [0;1]. Than it build Hermite
        spline and derivative of it in 0,5. Search scale as Max(DF0,DF1, |F0-F1|).
        Right derivative has to satisfy condition:
            |H-F|/S<=0,01, |H'-F'|/S<=0,01.
            
        INPUT PARAMETERS:
            F0  -   function's value in X-TestStep point;
            DF0 -   derivative's value in X-TestStep point;
            F1  -   function's value in X+TestStep point;
            DF1 -   derivative's value in X+TestStep point;
            F   -   testing function's value;
            DF  -   testing derivative's value;
           Width-   width of verification segment.

        RESULT:
            If input derivatives is right then function returns true, else 
            function returns false.
            
          -- ALGLIB --
             Copyright 29.05.2012 by Bochkanov Sergey
        *************************************************************************/
        public static bool derivativecheck(double f0,
            double df0,
            double f1,
            double df1,
            double f,
            double df,
            double width)
        {
            bool result = new bool();
            double s = 0;
            double h = 0;
            double dh = 0;

            df = width*df;
            df0 = width*df0;
            df1 = width*df1;
            s = Math.Max(Math.Max(Math.Abs(df0), Math.Abs(df1)), Math.Abs(f1-f0));
            h = 0.5*f0+0.125*df0+0.5*f1-0.125*df1;
            dh = -(1.5*f0)-0.25*df0+1.5*f1-0.25*df1;
            if( (double)(s)!=(double)(0) )
            {
                if( (double)(Math.Abs(h-f)/s)>(double)(0.001) || (double)(Math.Abs(dh-df)/s)>(double)(0.001) )
                {
                    result = false;
                    return result;
                }
            }
            else
            {
                if( (double)(h-f)!=(double)(0.0) || (double)(dh-df)!=(double)(0.0) )
                {
                    result = false;
                    return result;
                }
            }
            result = true;
            return result;
        }


    }
    public class cqmodels
    {
        /*************************************************************************
        This structure describes convex quadratic model of the form:
            f(x) = 0.5*(Alpha*x'*A*x + Tau*x'*D*x) + 0.5*Theta*(Q*x-r)'*(Q*x-r) + b'*x
        where:
            * Alpha>=0, Tau>=0, Theta>=0, Alpha+Tau>0.
            * A is NxN matrix, Q is NxK matrix (N>=1, K>=0), b is Nx1 vector,
              D is NxN diagonal matrix.
            * "main" quadratic term Alpha*A+Lambda*D is symmetric
              positive definite
        Structure may contain optional equality constraints of the form x[i]=x0[i],
        in this case functions provided by this unit calculate Newton step subject
        to these equality constraints.
        *************************************************************************/
        public class convexquadraticmodel : apobject
        {
            public int n;
            public int k;
            public double alpha;
            public double tau;
            public double theta;
            public double[,] a;
            public double[,] q;
            public double[] b;
            public double[] r;
            public double[] xc;
            public double[] d;
            public bool[] activeset;
            public double[,] tq2dense;
            public double[,] tk2;
            public double[] tq2diag;
            public double[] tq1;
            public double[] tk1;
            public double tq0;
            public double tk0;
            public double[] txc;
            public double[] tb;
            public int nfree;
            public int ecakind;
            public double[,] ecadense;
            public double[,] eq;
            public double[,] eccm;
            public double[] ecadiag;
            public double[] eb;
            public double ec;
            public double[] tmp0;
            public double[] tmp1;
            public double[] tmpg;
            public double[,] tmp2;
            public bool ismaintermchanged;
            public bool issecondarytermchanged;
            public bool islineartermchanged;
            public bool isactivesetchanged;
            public convexquadraticmodel()
            {
                init();
            }
            public override void init()
            {
                a = new double[0,0];
                q = new double[0,0];
                b = new double[0];
                r = new double[0];
                xc = new double[0];
                d = new double[0];
                activeset = new bool[0];
                tq2dense = new double[0,0];
                tk2 = new double[0,0];
                tq2diag = new double[0];
                tq1 = new double[0];
                tk1 = new double[0];
                txc = new double[0];
                tb = new double[0];
                ecadense = new double[0,0];
                eq = new double[0,0];
                eccm = new double[0,0];
                ecadiag = new double[0];
                eb = new double[0];
                tmp0 = new double[0];
                tmp1 = new double[0];
                tmpg = new double[0];
                tmp2 = new double[0,0];
            }
            public override alglib.apobject make_copy()
            {
                convexquadraticmodel _result = new convexquadraticmodel();
                _result.n = n;
                _result.k = k;
                _result.alpha = alpha;
                _result.tau = tau;
                _result.theta = theta;
                _result.a = (double[,])a.Clone();
                _result.q = (double[,])q.Clone();
                _result.b = (double[])b.Clone();
                _result.r = (double[])r.Clone();
                _result.xc = (double[])xc.Clone();
                _result.d = (double[])d.Clone();
                _result.activeset = (bool[])activeset.Clone();
                _result.tq2dense = (double[,])tq2dense.Clone();
                _result.tk2 = (double[,])tk2.Clone();
                _result.tq2diag = (double[])tq2diag.Clone();
                _result.tq1 = (double[])tq1.Clone();
                _result.tk1 = (double[])tk1.Clone();
                _result.tq0 = tq0;
                _result.tk0 = tk0;
                _result.txc = (double[])txc.Clone();
                _result.tb = (double[])tb.Clone();
                _result.nfree = nfree;
                _result.ecakind = ecakind;
                _result.ecadense = (double[,])ecadense.Clone();
                _result.eq = (double[,])eq.Clone();
                _result.eccm = (double[,])eccm.Clone();
                _result.ecadiag = (double[])ecadiag.Clone();
                _result.eb = (double[])eb.Clone();
                _result.ec = ec;
                _result.tmp0 = (double[])tmp0.Clone();
                _result.tmp1 = (double[])tmp1.Clone();
                _result.tmpg = (double[])tmpg.Clone();
                _result.tmp2 = (double[,])tmp2.Clone();
                _result.ismaintermchanged = ismaintermchanged;
                _result.issecondarytermchanged = issecondarytermchanged;
                _result.islineartermchanged = islineartermchanged;
                _result.isactivesetchanged = isactivesetchanged;
                return _result;
            }
        };




        public const int newtonrefinementits = 3;


        /*************************************************************************
        This subroutine is used to initialize CQM. By default, empty NxN model  is
        generated, with Alpha=Lambda=Theta=0.0 and zero b.

        Previously allocated buffer variables are reused as much as possible.

          -- ALGLIB --
             Copyright 12.06.2012 by Bochkanov Sergey
        *************************************************************************/
        public static void cqminit(int n,
            convexquadraticmodel s)
        {
            int i = 0;

            s.n = n;
            s.k = 0;
            s.nfree = n;
            s.ecakind = -1;
            s.alpha = 0.0;
            s.tau = 0.0;
            s.theta = 0.0;
            s.ismaintermchanged = true;
            s.issecondarytermchanged = true;
            s.islineartermchanged = true;
            s.isactivesetchanged = true;
            apserv.bvectorsetlengthatleast(ref s.activeset, n);
            apserv.rvectorsetlengthatleast(ref s.xc, n);
            apserv.rvectorsetlengthatleast(ref s.eb, n);
            apserv.rvectorsetlengthatleast(ref s.tq1, n);
            apserv.rvectorsetlengthatleast(ref s.txc, n);
            apserv.rvectorsetlengthatleast(ref s.tb, n);
            apserv.rvectorsetlengthatleast(ref s.b, s.n);
            apserv.rvectorsetlengthatleast(ref s.tk1, s.n);
            for(i=0; i<=n-1; i++)
            {
                s.activeset[i] = false;
                s.xc[i] = 0.0;
                s.b[i] = 0.0;
            }
        }


        /*************************************************************************
        This subroutine changes main quadratic term of the model.

        INPUT PARAMETERS:
            S       -   model
            A       -   NxN matrix, only upper or lower triangle is referenced
            IsUpper -   True, when matrix is stored in upper triangle
            Alpha   -   multiplier; when Alpha=0, A is not referenced at all

          -- ALGLIB --
             Copyright 12.06.2012 by Bochkanov Sergey
        *************************************************************************/
        public static void cqmseta(convexquadraticmodel s,
            double[,] a,
            bool isupper,
            double alpha)
        {
            int i = 0;
            int j = 0;
            double v = 0;

            alglib.ap.assert(math.isfinite(alpha) && (double)(alpha)>=(double)(0), "CQMSetA: Alpha<0 or is not finite number");
            alglib.ap.assert((double)(alpha)==(double)(0) || apserv.isfinitertrmatrix(a, s.n, isupper), "CQMSetA: A is not finite NxN matrix");
            s.alpha = alpha;
            if( (double)(alpha)>(double)(0) )
            {
                apserv.rmatrixsetlengthatleast(ref s.a, s.n, s.n);
                apserv.rmatrixsetlengthatleast(ref s.ecadense, s.n, s.n);
                apserv.rmatrixsetlengthatleast(ref s.tq2dense, s.n, s.n);
                for(i=0; i<=s.n-1; i++)
                {
                    for(j=i; j<=s.n-1; j++)
                    {
                        if( isupper )
                        {
                            v = a[i,j];
                        }
                        else
                        {
                            v = a[j,i];
                        }
                        s.a[i,j] = v;
                        s.a[j,i] = v;
                    }
                }
            }
            s.ismaintermchanged = true;
        }


        /*************************************************************************
        This subroutine rewrites diagonal of the main quadratic term of the  model
        (dense  A)  by  vector  Z/Alpha (current value of the Alpha coefficient is
        used).

        IMPORTANT: in  case  model  has  no  dense  quadratic  term, this function
                   allocates N*N dense matrix of zeros, and fills its diagonal  by
                   non-zero values.

        INPUT PARAMETERS:
            S       -   model
            Z       -   new diagonal, array[N]

          -- ALGLIB --
             Copyright 12.06.2012 by Bochkanov Sergey
        *************************************************************************/
        public static void cqmrewritedensediagonal(convexquadraticmodel s,
            double[] z)
        {
            int n = 0;
            int i = 0;
            int j = 0;

            n = s.n;
            if( (double)(s.alpha)==(double)(0) )
            {
                apserv.rmatrixsetlengthatleast(ref s.a, s.n, s.n);
                apserv.rmatrixsetlengthatleast(ref s.ecadense, s.n, s.n);
                apserv.rmatrixsetlengthatleast(ref s.tq2dense, s.n, s.n);
                for(i=0; i<=n-1; i++)
                {
                    for(j=0; j<=n-1; j++)
                    {
                        s.a[i,j] = 0.0;
                    }
                }
                s.alpha = 1.0;
            }
            for(i=0; i<=s.n-1; i++)
            {
                s.a[i,i] = z[i]/s.alpha;
            }
            s.ismaintermchanged = true;
        }


        /*************************************************************************
        This subroutine changes diagonal quadratic term of the model.

        INPUT PARAMETERS:
            S       -   model
            D       -   array[N], semidefinite diagonal matrix
            Tau     -   multiplier; when Tau=0, D is not referenced at all

          -- ALGLIB --
             Copyright 12.06.2012 by Bochkanov Sergey
        *************************************************************************/
        public static void cqmsetd(convexquadraticmodel s,
            double[] d,
            double tau)
        {
            int i = 0;

            alglib.ap.assert(math.isfinite(tau) && (double)(tau)>=(double)(0), "CQMSetD: Tau<0 or is not finite number");
            alglib.ap.assert((double)(tau)==(double)(0) || apserv.isfinitevector(d, s.n), "CQMSetD: D is not finite Nx1 vector");
            s.tau = tau;
            if( (double)(tau)>(double)(0) )
            {
                apserv.rvectorsetlengthatleast(ref s.d, s.n);
                apserv.rvectorsetlengthatleast(ref s.ecadiag, s.n);
                apserv.rvectorsetlengthatleast(ref s.tq2diag, s.n);
                for(i=0; i<=s.n-1; i++)
                {
                    alglib.ap.assert((double)(d[i])>=(double)(0), "CQMSetD: D[i]<0");
                    s.d[i] = d[i];
                }
            }
            s.ismaintermchanged = true;
        }


        /*************************************************************************
        This subroutine drops main quadratic term A from the model. It is same  as
        call  to  CQMSetA()  with  zero  A,   but gives better performance because
        algorithm  knows  that  matrix  is  zero  and  can  optimize    subsequent
        calculations.

        INPUT PARAMETERS:
            S       -   model

          -- ALGLIB --
             Copyright 12.06.2012 by Bochkanov Sergey
        *************************************************************************/
        public static void cqmdropa(convexquadraticmodel s)
        {
            s.alpha = 0.0;
            s.ismaintermchanged = true;
        }


        /*************************************************************************
        This subroutine changes linear term of the model

          -- ALGLIB --
             Copyright 12.06.2012 by Bochkanov Sergey
        *************************************************************************/
        public static void cqmsetb(convexquadraticmodel s,
            double[] b)
        {
            int i = 0;

            alglib.ap.assert(apserv.isfinitevector(b, s.n), "CQMSetB: B is not finite vector");
            apserv.rvectorsetlengthatleast(ref s.b, s.n);
            for(i=0; i<=s.n-1; i++)
            {
                s.b[i] = b[i];
            }
            s.islineartermchanged = true;
        }


        /*************************************************************************
        This subroutine changes linear term of the model

          -- ALGLIB --
             Copyright 12.06.2012 by Bochkanov Sergey
        *************************************************************************/
        public static void cqmsetq(convexquadraticmodel s,
            double[,] q,
            double[] r,
            int k,
            double theta)
        {
            int i = 0;
            int j = 0;

            alglib.ap.assert(k>=0, "CQMSetQ: K<0");
            alglib.ap.assert((k==0 || (double)(theta)==(double)(0)) || apserv.apservisfinitematrix(q, k, s.n), "CQMSetQ: Q is not finite matrix");
            alglib.ap.assert((k==0 || (double)(theta)==(double)(0)) || apserv.isfinitevector(r, k), "CQMSetQ: R is not finite vector");
            alglib.ap.assert(math.isfinite(theta) && (double)(theta)>=(double)(0), "CQMSetQ: Theta<0 or is not finite number");
            
            //
            // degenerate case: K=0 or Theta=0
            //
            if( k==0 || (double)(theta)==(double)(0) )
            {
                s.k = 0;
                s.theta = 0;
                s.issecondarytermchanged = true;
                return;
            }
            
            //
            // General case: both Theta>0 and K>0
            //
            s.k = k;
            s.theta = theta;
            apserv.rmatrixsetlengthatleast(ref s.q, s.k, s.n);
            apserv.rvectorsetlengthatleast(ref s.r, s.k);
            apserv.rmatrixsetlengthatleast(ref s.eq, s.k, s.n);
            apserv.rmatrixsetlengthatleast(ref s.eccm, s.k, s.k);
            apserv.rmatrixsetlengthatleast(ref s.tk2, s.k, s.n);
            for(i=0; i<=s.k-1; i++)
            {
                for(j=0; j<=s.n-1; j++)
                {
                    s.q[i,j] = q[i,j];
                }
                s.r[i] = r[i];
            }
            s.issecondarytermchanged = true;
        }


        /*************************************************************************
        This subroutine changes active set

        INPUT PARAMETERS
            S       -   model
            X       -   array[N], constraint values
            ActiveSet-  array[N], active set. If ActiveSet[I]=True, then I-th
                        variables is constrained to X[I].

          -- ALGLIB --
             Copyright 12.06.2012 by Bochkanov Sergey
        *************************************************************************/
        public static void cqmsetactiveset(convexquadraticmodel s,
            double[] x,
            bool[] activeset)
        {
            int i = 0;

            alglib.ap.assert(alglib.ap.len(x)>=s.n, "CQMSetActiveSet: Length(X)<N");
            alglib.ap.assert(alglib.ap.len(activeset)>=s.n, "CQMSetActiveSet: Length(ActiveSet)<N");
            for(i=0; i<=s.n-1; i++)
            {
                s.isactivesetchanged = s.isactivesetchanged || (s.activeset[i] && !activeset[i]);
                s.isactivesetchanged = s.isactivesetchanged || (activeset[i] && !s.activeset[i]);
                s.activeset[i] = activeset[i];
                if( activeset[i] )
                {
                    alglib.ap.assert(math.isfinite(x[i]), "CQMSetActiveSet: X[] contains infinite constraints");
                    s.isactivesetchanged = s.isactivesetchanged || (double)(s.xc[i])!=(double)(x[i]);
                    s.xc[i] = x[i];
                }
            }
        }


        /*************************************************************************
        This subroutine evaluates model at X. Active constraints are ignored.

          -- ALGLIB --
             Copyright 12.06.2012 by Bochkanov Sergey
        *************************************************************************/
        public static double cqmeval(convexquadraticmodel s,
            double[] x)
        {
            double result = 0;
            int n = 0;
            int i = 0;
            int j = 0;
            double v = 0;
            int i_ = 0;

            n = s.n;
            alglib.ap.assert(apserv.isfinitevector(x, n), "CQMEval: X is not finite vector");
            result = 0.0;
            
            //
            // main quadratic term
            //
            if( (double)(s.alpha)>(double)(0) )
            {
                for(i=0; i<=n-1; i++)
                {
                    for(j=0; j<=n-1; j++)
                    {
                        result = result+s.alpha*0.5*x[i]*s.a[i,j]*x[j];
                    }
                }
            }
            if( (double)(s.tau)>(double)(0) )
            {
                for(i=0; i<=n-1; i++)
                {
                    result = result+0.5*math.sqr(x[i])*s.tau*s.d[i];
                }
            }
            
            //
            // secondary quadratic term
            //
            if( (double)(s.theta)>(double)(0) )
            {
                for(i=0; i<=s.k-1; i++)
                {
                    v = 0.0;
                    for(i_=0; i_<=n-1;i_++)
                    {
                        v += s.q[i,i_]*x[i_];
                    }
                    result = result+0.5*s.theta*math.sqr(v-s.r[i]);
                }
            }
            
            //
            // linear term
            //
            for(i=0; i<=s.n-1; i++)
            {
                result = result+x[i]*s.b[i];
            }
            return result;
        }


        /*************************************************************************
        This subroutine evaluates model at X. Active constraints are ignored.
        It returns:
            R   -   model value
            Noise-  estimate of the numerical noise in data

          -- ALGLIB --
             Copyright 12.06.2012 by Bochkanov Sergey
        *************************************************************************/
        public static void cqmevalx(convexquadraticmodel s,
            double[] x,
            ref double r,
            ref double noise)
        {
            int n = 0;
            int i = 0;
            int j = 0;
            double v = 0;
            double v2 = 0;
            double mxq = 0;
            double eps = 0;

            r = 0;
            noise = 0;

            n = s.n;
            alglib.ap.assert(apserv.isfinitevector(x, n), "CQMEval: X is not finite vector");
            r = 0.0;
            noise = 0.0;
            eps = 2*math.machineepsilon;
            mxq = 0.0;
            
            //
            // Main quadratic term.
            //
            // Noise from the main quadratic term is equal to the
            // maximum summand in the term.
            //
            if( (double)(s.alpha)>(double)(0) )
            {
                for(i=0; i<=n-1; i++)
                {
                    for(j=0; j<=n-1; j++)
                    {
                        v = s.alpha*0.5*x[i]*s.a[i,j]*x[j];
                        r = r+v;
                        noise = Math.Max(noise, eps*Math.Abs(v));
                    }
                }
            }
            if( (double)(s.tau)>(double)(0) )
            {
                for(i=0; i<=n-1; i++)
                {
                    v = 0.5*math.sqr(x[i])*s.tau*s.d[i];
                    r = r+v;
                    noise = Math.Max(noise, eps*Math.Abs(v));
                }
            }
            
            //
            // secondary quadratic term
            //
            // Noise from the secondary quadratic term is estimated as follows:
            // * noise in qi*x-r[i] is estimated as
            //   Eps*MXQ = Eps*max(|r[i]|, |q[i,j]*x[j]|)
            // * noise in (qi*x-r[i])^2 is estimated as
            //   NOISE = (|qi*x-r[i]|+Eps*MXQ)^2-(|qi*x-r[i]|)^2
            //         = Eps*MXQ*(2*|qi*x-r[i]|+Eps*MXQ)
            //
            if( (double)(s.theta)>(double)(0) )
            {
                for(i=0; i<=s.k-1; i++)
                {
                    v = 0.0;
                    mxq = Math.Abs(s.r[i]);
                    for(j=0; j<=n-1; j++)
                    {
                        v2 = s.q[i,j]*x[j];
                        v = v+v2;
                        mxq = Math.Max(mxq, Math.Abs(v2));
                    }
                    r = r+0.5*s.theta*math.sqr(v-s.r[i]);
                    noise = Math.Max(noise, eps*mxq*(2*Math.Abs(v-s.r[i])+eps*mxq));
                }
            }
            
            //
            // linear term
            //
            for(i=0; i<=s.n-1; i++)
            {
                r = r+x[i]*s.b[i];
                noise = Math.Max(noise, eps*Math.Abs(x[i]*s.b[i]));
            }
            
            //
            // Final update of the noise
            //
            noise = n*noise;
        }


        /*************************************************************************
        This  subroutine  evaluates  gradient of the model; active constraints are
        ignored.

        INPUT PARAMETERS:
            S       -   convex model
            X       -   point, array[N]
            G       -   possibly preallocated buffer; resized, if too small

          -- ALGLIB --
             Copyright 12.06.2012 by Bochkanov Sergey
        *************************************************************************/
        public static void cqmgradunconstrained(convexquadraticmodel s,
            double[] x,
            ref double[] g)
        {
            int n = 0;
            int i = 0;
            int j = 0;
            double v = 0;
            int i_ = 0;

            n = s.n;
            alglib.ap.assert(apserv.isfinitevector(x, n), "CQMEvalGradUnconstrained: X is not finite vector");
            apserv.rvectorsetlengthatleast(ref g, n);
            for(i=0; i<=n-1; i++)
            {
                g[i] = 0;
            }
            
            //
            // main quadratic term
            //
            if( (double)(s.alpha)>(double)(0) )
            {
                for(i=0; i<=n-1; i++)
                {
                    v = 0.0;
                    for(j=0; j<=n-1; j++)
                    {
                        v = v+s.alpha*s.a[i,j]*x[j];
                    }
                    g[i] = g[i]+v;
                }
            }
            if( (double)(s.tau)>(double)(0) )
            {
                for(i=0; i<=n-1; i++)
                {
                    g[i] = g[i]+x[i]*s.tau*s.d[i];
                }
            }
            
            //
            // secondary quadratic term
            //
            if( (double)(s.theta)>(double)(0) )
            {
                for(i=0; i<=s.k-1; i++)
                {
                    v = 0.0;
                    for(i_=0; i_<=n-1;i_++)
                    {
                        v += s.q[i,i_]*x[i_];
                    }
                    v = s.theta*(v-s.r[i]);
                    for(i_=0; i_<=n-1;i_++)
                    {
                        g[i_] = g[i_] + v*s.q[i,i_];
                    }
                }
            }
            
            //
            // linear term
            //
            for(i=0; i<=n-1; i++)
            {
                g[i] = g[i]+s.b[i];
            }
        }


        /*************************************************************************
        This subroutine evaluates x'*(0.5*alpha*A+tau*D)*x

          -- ALGLIB --
             Copyright 12.06.2012 by Bochkanov Sergey
        *************************************************************************/
        public static double cqmxtadx2(convexquadraticmodel s,
            double[] x)
        {
            double result = 0;
            int n = 0;
            int i = 0;
            int j = 0;

            n = s.n;
            alglib.ap.assert(apserv.isfinitevector(x, n), "CQMEval: X is not finite vector");
            result = 0.0;
            
            //
            // main quadratic term
            //
            if( (double)(s.alpha)>(double)(0) )
            {
                for(i=0; i<=n-1; i++)
                {
                    for(j=0; j<=n-1; j++)
                    {
                        result = result+s.alpha*0.5*x[i]*s.a[i,j]*x[j];
                    }
                }
            }
            if( (double)(s.tau)>(double)(0) )
            {
                for(i=0; i<=n-1; i++)
                {
                    result = result+0.5*math.sqr(x[i])*s.tau*s.d[i];
                }
            }
            return result;
        }


        /*************************************************************************
        This subroutine evaluates (0.5*alpha*A+tau*D)*x

        Y is automatically resized if needed

          -- ALGLIB --
             Copyright 12.06.2012 by Bochkanov Sergey
        *************************************************************************/
        public static void cqmadx(convexquadraticmodel s,
            double[] x,
            ref double[] y)
        {
            int n = 0;
            int i = 0;
            double v = 0;
            int i_ = 0;

            n = s.n;
            alglib.ap.assert(apserv.isfinitevector(x, n), "CQMEval: X is not finite vector");
            apserv.rvectorsetlengthatleast(ref y, n);
            
            //
            // main quadratic term
            //
            for(i=0; i<=n-1; i++)
            {
                y[i] = 0;
            }
            if( (double)(s.alpha)>(double)(0) )
            {
                for(i=0; i<=n-1; i++)
                {
                    v = 0.0;
                    for(i_=0; i_<=n-1;i_++)
                    {
                        v += s.a[i,i_]*x[i_];
                    }
                    y[i] = y[i]+s.alpha*v;
                }
            }
            if( (double)(s.tau)>(double)(0) )
            {
                for(i=0; i<=n-1; i++)
                {
                    y[i] = y[i]+x[i]*s.tau*s.d[i];
                }
            }
        }


        /*************************************************************************
        This subroutine finds optimum of the model. It returns  False  on  failure
        (indefinite/semidefinite matrix).  Optimum  is  found  subject  to  active
        constraints.

        INPUT PARAMETERS
            S       -   model
            X       -   possibly preallocated buffer; automatically resized, if
                        too small enough.

          -- ALGLIB --
             Copyright 12.06.2012 by Bochkanov Sergey
        *************************************************************************/
        public static bool cqmconstrainedoptimum(convexquadraticmodel s,
            ref double[] x)
        {
            bool result = new bool();
            int n = 0;
            int nfree = 0;
            int k = 0;
            int i = 0;
            double v = 0;
            int cidx0 = 0;
            int itidx = 0;
            int i_ = 0;

            
            //
            // Rebuild internal structures
            //
            if( !cqmrebuild(s) )
            {
                result = false;
                return result;
            }
            n = s.n;
            k = s.k;
            nfree = s.nfree;
            result = true;
            
            //
            // Calculate initial point for the iterative refinement:
            // * free components are set to zero
            // * constrained components are set to their constrained values
            //
            apserv.rvectorsetlengthatleast(ref x, n);
            for(i=0; i<=n-1; i++)
            {
                if( s.activeset[i] )
                {
                    x[i] = s.xc[i];
                }
                else
                {
                    x[i] = 0;
                }
            }
            
            //
            // Iterative refinement.
            //
            // In an ideal world without numerical errors it would be enough
            // to make just one Newton step from initial point:
            //   x_new = -H^(-1)*grad(x=0)
            // However, roundoff errors can significantly deteriorate quality
            // of the solution. So we have to recalculate gradient and to
            // perform Newton steps several times.
            //
            // Below we perform fixed number of Newton iterations.
            //
            for(itidx=0; itidx<=newtonrefinementits-1; itidx++)
            {
                
                //
                // Calculate gradient at the current point.
                // Move free components of the gradient in the beginning.
                //
                cqmgradunconstrained(s, x, ref s.tmpg);
                cidx0 = 0;
                for(i=0; i<=n-1; i++)
                {
                    if( !s.activeset[i] )
                    {
                        s.tmpg[cidx0] = s.tmpg[i];
                        cidx0 = cidx0+1;
                    }
                }
                
                //
                // Free components of the extrema are calculated in the first NFree elements of TXC.
                //
                // First, we have to calculate original Newton step, without rank-K perturbations
                //
                for(i_=0; i_<=nfree-1;i_++)
                {
                    s.txc[i_] = -s.tmpg[i_];
                }
                cqmsolveea(s, ref s.txc, ref s.tmp0);
                
                //
                // Then, we account for rank-K correction.
                // Woodbury matrix identity is used.
                //
                if( s.k>0 && (double)(s.theta)>(double)(0) )
                {
                    apserv.rvectorsetlengthatleast(ref s.tmp0, Math.Max(nfree, k));
                    apserv.rvectorsetlengthatleast(ref s.tmp1, Math.Max(nfree, k));
                    for(i_=0; i_<=nfree-1;i_++)
                    {
                        s.tmp1[i_] = -s.tmpg[i_];
                    }
                    cqmsolveea(s, ref s.tmp1, ref s.tmp0);
                    for(i=0; i<=k-1; i++)
                    {
                        v = 0.0;
                        for(i_=0; i_<=nfree-1;i_++)
                        {
                            v += s.eq[i,i_]*s.tmp1[i_];
                        }
                        s.tmp0[i] = v;
                    }
                    fbls.fblscholeskysolve(s.eccm, 1.0, k, true, ref s.tmp0, ref s.tmp1);
                    for(i=0; i<=nfree-1; i++)
                    {
                        s.tmp1[i] = 0.0;
                    }
                    for(i=0; i<=k-1; i++)
                    {
                        v = s.tmp0[i];
                        for(i_=0; i_<=nfree-1;i_++)
                        {
                            s.tmp1[i_] = s.tmp1[i_] + v*s.eq[i,i_];
                        }
                    }
                    cqmsolveea(s, ref s.tmp1, ref s.tmp0);
                    for(i_=0; i_<=nfree-1;i_++)
                    {
                        s.txc[i_] = s.txc[i_] - s.tmp1[i_];
                    }
                }
                
                //
                // Unpack components from TXC into X. We pass through all
                // free components of X and add our step.
                //
                cidx0 = 0;
                for(i=0; i<=n-1; i++)
                {
                    if( !s.activeset[i] )
                    {
                        x[i] = x[i]+s.txc[cidx0];
                        cidx0 = cidx0+1;
                    }
                }
            }
            return result;
        }


        /*************************************************************************
        This function scales vector  by  multiplying it by inverse of the diagonal
        of the Hessian matrix. It should be used to  accelerate  steepest  descent
        phase of the QP solver.

        Although  it  is  called  "scale-grad",  it  can be called for any vector,
        whether it is gradient, anti-gradient, or just some vector.

        This function does NOT takes into account current set of  constraints,  it
        just performs matrix-vector multiplication  without  taking  into  account
        constraints.

        INPUT PARAMETERS:
            S       -   model
            X       -   vector to scale

        OUTPUT PARAMETERS:
            X       -   scaled vector
            
        NOTE:
            when called for non-SPD matrices, it silently skips components of X
            which correspond to zero or negative diagonal elements.
            
        NOTE:
            this function uses diagonals of A and D; it ignores Q - rank-K term of
            the quadratic model.

          -- ALGLIB --
             Copyright 12.06.2012 by Bochkanov Sergey
        *************************************************************************/
        public static void cqmscalevector(convexquadraticmodel s,
            ref double[] x)
        {
            int n = 0;
            int i = 0;
            double v = 0;

            n = s.n;
            for(i=0; i<=n-1; i++)
            {
                v = 0.0;
                if( (double)(s.alpha)>(double)(0) )
                {
                    v = v+s.a[i,i];
                }
                if( (double)(s.tau)>(double)(0) )
                {
                    v = v+s.d[i];
                }
                if( (double)(v)>(double)(0) )
                {
                    x[i] = x[i]/v;
                }
            }
        }


        /*************************************************************************
        This subroutine calls CQMRebuild() and evaluates model at X subject to
        active constraints.

        It  is  intended  for  debug  purposes only, because it evaluates model by
        means of temporaries, which were calculated  by  CQMRebuild().  The   only
        purpose of this function  is  to  check  correctness  of  CQMRebuild()  by
        comparing results of this function with ones obtained by CQMEval(),  which
        is  used  as  reference  point. The  idea is that significant deviation in
        results  of  these  two  functions  is  evidence  of  some  error  in  the
        CQMRebuild().

        NOTE: suffix T denotes that temporaries marked by T-prefix are used. There
              is one more variant of this function, which uses  "effective"  model
              built by CQMRebuild().

        NOTE2: in case CQMRebuild() fails (due to model non-convexity), this
              function returns NAN.

          -- ALGLIB --
             Copyright 12.06.2012 by Bochkanov Sergey
        *************************************************************************/
        public static double cqmdebugconstrainedevalt(convexquadraticmodel s,
            double[] x)
        {
            double result = 0;
            int n = 0;
            int nfree = 0;
            int i = 0;
            int j = 0;
            double v = 0;

            n = s.n;
            alglib.ap.assert(apserv.isfinitevector(x, n), "CQMDebugConstrainedEvalT: X is not finite vector");
            if( !cqmrebuild(s) )
            {
                result = Double.NaN;
                return result;
            }
            result = 0.0;
            nfree = s.nfree;
            
            //
            // Reorder variables
            //
            j = 0;
            for(i=0; i<=n-1; i++)
            {
                if( !s.activeset[i] )
                {
                    alglib.ap.assert(j<nfree, "CQMDebugConstrainedEvalT: internal error");
                    s.txc[j] = x[i];
                    j = j+1;
                }
            }
            
            //
            // TQ2, TQ1, TQ0
            //
            //
            if( (double)(s.alpha)>(double)(0) )
            {
                
                //
                // Dense TQ2
                //
                for(i=0; i<=nfree-1; i++)
                {
                    for(j=0; j<=nfree-1; j++)
                    {
                        result = result+0.5*s.txc[i]*s.tq2dense[i,j]*s.txc[j];
                    }
                }
            }
            else
            {
                
                //
                // Diagonal TQ2
                //
                for(i=0; i<=nfree-1; i++)
                {
                    result = result+0.5*s.tq2diag[i]*math.sqr(s.txc[i]);
                }
            }
            for(i=0; i<=nfree-1; i++)
            {
                result = result+s.tq1[i]*s.txc[i];
            }
            result = result+s.tq0;
            
            //
            // TK2, TK1, TK0
            //
            if( s.k>0 && (double)(s.theta)>(double)(0) )
            {
                for(i=0; i<=s.k-1; i++)
                {
                    v = 0;
                    for(j=0; j<=nfree-1; j++)
                    {
                        v = v+s.tk2[i,j]*s.txc[j];
                    }
                    result = result+0.5*math.sqr(v);
                }
                for(i=0; i<=nfree-1; i++)
                {
                    result = result+s.tk1[i]*s.txc[i];
                }
                result = result+s.tk0;
            }
            
            //
            // TB (Bf and Bc parts)
            //
            for(i=0; i<=n-1; i++)
            {
                result = result+s.tb[i]*s.txc[i];
            }
            return result;
        }


        /*************************************************************************
        This subroutine calls CQMRebuild() and evaluates model at X subject to
        active constraints.

        It  is  intended  for  debug  purposes only, because it evaluates model by
        means of "effective" matrices built by CQMRebuild(). The only  purpose  of
        this function is to check correctness of CQMRebuild() by comparing results
        of this function with  ones  obtained  by  CQMEval(),  which  is  used  as
        reference  point.  The  idea  is  that significant deviation in results of
        these two functions is evidence of some error in the CQMRebuild().

        NOTE: suffix E denotes that effective matrices. There is one more  variant
              of this function, which uses temporary matrices built by
              CQMRebuild().

        NOTE2: in case CQMRebuild() fails (due to model non-convexity), this
              function returns NAN.

          -- ALGLIB --
             Copyright 12.06.2012 by Bochkanov Sergey
        *************************************************************************/
        public static double cqmdebugconstrainedevale(convexquadraticmodel s,
            double[] x)
        {
            double result = 0;
            int n = 0;
            int nfree = 0;
            int i = 0;
            int j = 0;
            double v = 0;

            n = s.n;
            alglib.ap.assert(apserv.isfinitevector(x, n), "CQMDebugConstrainedEvalE: X is not finite vector");
            if( !cqmrebuild(s) )
            {
                result = Double.NaN;
                return result;
            }
            result = 0.0;
            nfree = s.nfree;
            
            //
            // Reorder variables
            //
            j = 0;
            for(i=0; i<=n-1; i++)
            {
                if( !s.activeset[i] )
                {
                    alglib.ap.assert(j<nfree, "CQMDebugConstrainedEvalE: internal error");
                    s.txc[j] = x[i];
                    j = j+1;
                }
            }
            
            //
            // ECA
            //
            alglib.ap.assert((s.ecakind==0 || s.ecakind==1) || (s.ecakind==-1 && nfree==0), "CQMDebugConstrainedEvalE: unexpected ECAKind");
            if( s.ecakind==0 )
            {
                
                //
                // Dense ECA
                //
                for(i=0; i<=nfree-1; i++)
                {
                    v = 0.0;
                    for(j=i; j<=nfree-1; j++)
                    {
                        v = v+s.ecadense[i,j]*s.txc[j];
                    }
                    result = result+0.5*math.sqr(v);
                }
            }
            if( s.ecakind==1 )
            {
                
                //
                // Diagonal ECA
                //
                for(i=0; i<=nfree-1; i++)
                {
                    result = result+0.5*math.sqr(s.ecadiag[i]*s.txc[i]);
                }
            }
            
            //
            // EQ
            //
            for(i=0; i<=s.k-1; i++)
            {
                v = 0.0;
                for(j=0; j<=nfree-1; j++)
                {
                    v = v+s.eq[i,j]*s.txc[j];
                }
                result = result+0.5*math.sqr(v);
            }
            
            //
            // EB
            //
            for(i=0; i<=nfree-1; i++)
            {
                result = result+s.eb[i]*s.txc[i];
            }
            
            //
            // EC
            //
            result = result+s.ec;
            return result;
        }


        /*************************************************************************
        Internal function, rebuilds "effective" model subject to constraints.
        Returns False on failure (non-SPD main quadratic term)

          -- ALGLIB --
             Copyright 10.05.2011 by Bochkanov Sergey
        *************************************************************************/
        private static bool cqmrebuild(convexquadraticmodel s)
        {
            bool result = new bool();
            int n = 0;
            int nfree = 0;
            int k = 0;
            int i = 0;
            int j = 0;
            int ridx0 = 0;
            int ridx1 = 0;
            int cidx0 = 0;
            int cidx1 = 0;
            double v = 0;
            int i_ = 0;

            if( (double)(s.alpha)==(double)(0) && (double)(s.tau)==(double)(0) )
            {
                
                //
                // Non-SPD model, quick exit
                //
                result = false;
                return result;
            }
            result = true;
            n = s.n;
            k = s.k;
            
            //
            // Determine number of free variables.
            // Fill TXC - array whose last N-NFree elements store constraints.
            //
            if( s.isactivesetchanged )
            {
                s.nfree = 0;
                for(i=0; i<=n-1; i++)
                {
                    if( !s.activeset[i] )
                    {
                        s.nfree = s.nfree+1;
                    }
                }
                j = s.nfree;
                for(i=0; i<=n-1; i++)
                {
                    if( s.activeset[i] )
                    {
                        s.txc[j] = s.xc[i];
                        j = j+1;
                    }
                }
            }
            nfree = s.nfree;
            
            //
            // Re-evaluate TQ2/TQ1/TQ0, if needed
            //
            if( s.isactivesetchanged || s.ismaintermchanged )
            {
                
                //
                // Handle cases Alpha>0 and Alpha=0 separately:
                // * in the first case we have dense matrix
                // * in the second one we have diagonal matrix, which can be
                //   handled more efficiently
                //
                if( (double)(s.alpha)>(double)(0) )
                {
                    
                    //
                    // Alpha>0, dense QP
                    //
                    // Split variables into two groups - free (F) and constrained (C). Reorder
                    // variables in such way that free vars come first, constrained are last:
                    // x = [xf, xc].
                    // 
                    // Main quadratic term x'*(alpha*A+tau*D)*x now splits into quadratic part,
                    // linear part and constant part:
                    //                   ( alpha*Aff+tau*Df  alpha*Afc        ) ( xf )              
                    //   0.5*( xf' xc' )*(                                    )*(    ) =
                    //                   ( alpha*Acf         alpha*Acc+tau*Dc ) ( xc )
                    //
                    //   = 0.5*xf'*(alpha*Aff+tau*Df)*xf + (alpha*Afc*xc)'*xf + 0.5*xc'(alpha*Acc+tau*Dc)*xc
                    //                    
                    // We store these parts into temporary variables:
                    // * alpha*Aff+tau*Df, alpha*Afc, alpha*Acc+tau*Dc are stored into upper
                    //   triangle of TQ2
                    // * alpha*Afc*xc is stored into TQ1
                    // * 0.5*xc'(alpha*Acc+tau*Dc)*xc is stored into TQ0
                    //
                    // Below comes first part of the work - generation of TQ2:
                    // * we pass through rows of A and copy I-th row into upper block (Aff/Afc) or
                    //   lower one (Acf/Acc) of TQ2, depending on presence of X[i] in the active set.
                    //   RIdx0 variable contains current position for insertion into upper block,
                    //   RIdx1 contains current position for insertion into lower one.
                    // * within each row, we copy J-th element into left half (Aff/Acf) or right
                    //   one (Afc/Acc), depending on presence of X[j] in the active set. CIdx0
                    //   contains current position for insertion into left block, CIdx1 contains
                    //   position for insertion into right one.
                    // * during copying, we multiply elements by alpha and add diagonal matrix D.
                    //
                    ridx0 = 0;
                    ridx1 = s.nfree;
                    for(i=0; i<=n-1; i++)
                    {
                        cidx0 = 0;
                        cidx1 = s.nfree;
                        for(j=0; j<=n-1; j++)
                        {
                            if( !s.activeset[i] && !s.activeset[j] )
                            {
                                
                                //
                                // Element belongs to Aff
                                //
                                v = s.alpha*s.a[i,j];
                                if( i==j && (double)(s.tau)>(double)(0) )
                                {
                                    v = v+s.tau*s.d[i];
                                }
                                s.tq2dense[ridx0,cidx0] = v;
                            }
                            if( !s.activeset[i] && s.activeset[j] )
                            {
                                
                                //
                                // Element belongs to Afc
                                //
                                s.tq2dense[ridx0,cidx1] = s.alpha*s.a[i,j];
                            }
                            if( s.activeset[i] && !s.activeset[j] )
                            {
                                
                                //
                                // Element belongs to Acf
                                //
                                s.tq2dense[ridx1,cidx0] = s.alpha*s.a[i,j];
                            }
                            if( s.activeset[i] && s.activeset[j] )
                            {
                                
                                //
                                // Element belongs to Acc
                                //
                                v = s.alpha*s.a[i,j];
                                if( i==j && (double)(s.tau)>(double)(0) )
                                {
                                    v = v+s.tau*s.d[i];
                                }
                                s.tq2dense[ridx1,cidx1] = v;
                            }
                            if( s.activeset[j] )
                            {
                                cidx1 = cidx1+1;
                            }
                            else
                            {
                                cidx0 = cidx0+1;
                            }
                        }
                        if( s.activeset[i] )
                        {
                            ridx1 = ridx1+1;
                        }
                        else
                        {
                            ridx0 = ridx0+1;
                        }
                    }
                    
                    //
                    // Now we have TQ2, and we can evaluate TQ1.
                    // In the special case when we have Alpha=0, NFree=0 or NFree=N,
                    // TQ1 is filled by zeros.
                    //
                    for(i=0; i<=n-1; i++)
                    {
                        s.tq1[i] = 0.0;
                    }
                    if( s.nfree>0 && s.nfree<n )
                    {
                        ablas.rmatrixmv(s.nfree, n-s.nfree, s.tq2dense, 0, s.nfree, 0, s.txc, s.nfree, ref s.tq1, 0);
                    }
                    
                    //
                    // And finally, we evaluate TQ0.
                    //
                    v = 0.0;
                    for(i=s.nfree; i<=n-1; i++)
                    {
                        for(j=s.nfree; j<=n-1; j++)
                        {
                            v = v+0.5*s.txc[i]*s.tq2dense[i,j]*s.txc[j];
                        }
                    }
                    s.tq0 = v;
                }
                else
                {
                    
                    //
                    // Alpha=0, diagonal QP
                    //
                    // Split variables into two groups - free (F) and constrained (C). Reorder
                    // variables in such way that free vars come first, constrained are last:
                    // x = [xf, xc].
                    // 
                    // Main quadratic term x'*(tau*D)*x now splits into quadratic and constant
                    // parts:
                    //                   ( tau*Df        ) ( xf )              
                    //   0.5*( xf' xc' )*(               )*(    ) =
                    //                   (        tau*Dc ) ( xc )
                    //
                    //   = 0.5*xf'*(tau*Df)*xf + 0.5*xc'(tau*Dc)*xc
                    //                    
                    // We store these parts into temporary variables:
                    // * tau*Df is stored in TQ2Diag
                    // * 0.5*xc'(tau*Dc)*xc is stored into TQ0
                    //
                    s.tq0 = 0.0;
                    ridx0 = 0;
                    for(i=0; i<=n-1; i++)
                    {
                        if( !s.activeset[i] )
                        {
                            s.tq2diag[ridx0] = s.tau*s.d[i];
                            ridx0 = ridx0+1;
                        }
                        else
                        {
                            s.tq0 = s.tq0+0.5*s.tau*s.d[i]*math.sqr(s.xc[i]);
                        }
                    }
                    for(i=0; i<=n-1; i++)
                    {
                        s.tq1[i] = 0.0;
                    }
                }
            }
            
            //
            // Re-evaluate TK2/TK1/TK0, if needed
            //
            if( s.isactivesetchanged || s.issecondarytermchanged )
            {
                
                //
                // Split variables into two groups - free (F) and constrained (C). Reorder
                // variables in such way that free vars come first, constrained are last:
                // x = [xf, xc].
                // 
                // Secondary term theta*(Q*x-r)'*(Q*x-r) now splits into quadratic part,
                // linear part and constant part:
                //             (          ( xf )     )'  (          ( xf )     )
                //   0.5*theta*( (Qf Qc)'*(    ) - r ) * ( (Qf Qc)'*(    ) - r ) =
                //             (          ( xc )     )   (          ( xc )     )
                //
                //   = 0.5*theta*xf'*(Qf'*Qf)*xf + theta*((Qc*xc-r)'*Qf)*xf + 
                //     + theta*(-r'*(Qc*xc-r)-0.5*r'*r+0.5*xc'*Qc'*Qc*xc)
                //                    
                // We store these parts into temporary variables:
                // * sqrt(theta)*Qf is stored into TK2
                // * theta*((Qc*xc-r)'*Qf) is stored into TK1
                // * theta*(-r'*(Qc*xc-r)-0.5*r'*r+0.5*xc'*Qc'*Qc*xc) is stored into TK0
                //
                // We use several other temporaries to store intermediate results:
                // * Tmp0 - to store Qc*xc-r
                // * Tmp1 - to store Qc*xc
                //
                // Generation of TK2/TK1/TK0 is performed as follows:
                // * we fill TK2/TK1/TK0 (to handle K=0 or Theta=0)
                // * other steps are performed only for K>0 and Theta>0
                // * we pass through columns of Q and copy I-th column into left block (Qf) or
                //   right one (Qc) of TK2, depending on presence of X[i] in the active set.
                //   CIdx0 variable contains current position for insertion into upper block,
                //   CIdx1 contains current position for insertion into lower one.
                // * we calculate Qc*xc-r and store it into Tmp0
                // * we calculate TK0 and TK1
                // * we multiply leading part of TK2 which stores Qf by sqrt(theta)
                //   it is important to perform this step AFTER calculation of TK0 and TK1,
                //   because we need original (non-modified) Qf to calculate TK0 and TK1.
                //
                for(j=0; j<=n-1; j++)
                {
                    for(i=0; i<=k-1; i++)
                    {
                        s.tk2[i,j] = 0.0;
                    }
                    s.tk1[j] = 0.0;
                }
                s.tk0 = 0.0;
                if( s.k>0 && (double)(s.theta)>(double)(0) )
                {
                    
                    //
                    // Split Q into Qf and Qc
                    // Calculate Qc*xc-r, store in Tmp0
                    //
                    apserv.rvectorsetlengthatleast(ref s.tmp0, k);
                    apserv.rvectorsetlengthatleast(ref s.tmp1, k);
                    cidx0 = 0;
                    cidx1 = nfree;
                    for(i=0; i<=k-1; i++)
                    {
                        s.tmp1[i] = 0.0;
                    }
                    for(j=0; j<=n-1; j++)
                    {
                        if( s.activeset[j] )
                        {
                            for(i=0; i<=k-1; i++)
                            {
                                s.tk2[i,cidx1] = s.q[i,j];
                                s.tmp1[i] = s.tmp1[i]+s.q[i,j]*s.txc[cidx1];
                            }
                            cidx1 = cidx1+1;
                        }
                        else
                        {
                            for(i=0; i<=k-1; i++)
                            {
                                s.tk2[i,cidx0] = s.q[i,j];
                            }
                            cidx0 = cidx0+1;
                        }
                    }
                    for(i=0; i<=k-1; i++)
                    {
                        s.tmp0[i] = s.tmp1[i]-s.r[i];
                    }
                    
                    //
                    // Calculate TK0
                    //
                    v = 0.0;
                    for(i=0; i<=k-1; i++)
                    {
                        v = v+s.theta*(0.5*math.sqr(s.tmp1[i])-s.r[i]*s.tmp0[i]-0.5*math.sqr(s.r[i]));
                    }
                    s.tk0 = v;
                    
                    //
                    // Calculate TK1
                    //
                    if( nfree>0 )
                    {
                        for(i=0; i<=k-1; i++)
                        {
                            v = s.theta*s.tmp0[i];
                            for(i_=0; i_<=nfree-1;i_++)
                            {
                                s.tk1[i_] = s.tk1[i_] + v*s.tk2[i,i_];
                            }
                        }
                    }
                    
                    //
                    // Calculate TK2
                    //
                    if( nfree>0 )
                    {
                        v = Math.Sqrt(s.theta);
                        for(i=0; i<=k-1; i++)
                        {
                            for(i_=0; i_<=nfree-1;i_++)
                            {
                                s.tk2[i,i_] = v*s.tk2[i,i_];
                            }
                        }
                    }
                }
            }
            
            //
            // Re-evaluate TB
            //
            if( s.isactivesetchanged || s.islineartermchanged )
            {
                ridx0 = 0;
                ridx1 = nfree;
                for(i=0; i<=n-1; i++)
                {
                    if( s.activeset[i] )
                    {
                        s.tb[ridx1] = s.b[i];
                        ridx1 = ridx1+1;
                    }
                    else
                    {
                        s.tb[ridx0] = s.b[i];
                        ridx0 = ridx0+1;
                    }
                }
            }
            
            //
            // Compose ECA: either dense ECA or diagonal ECA
            //
            if( (s.isactivesetchanged || s.ismaintermchanged) && nfree>0 )
            {
                if( (double)(s.alpha)>(double)(0) )
                {
                    
                    //
                    // Dense ECA
                    //
                    s.ecakind = 0;
                    for(i=0; i<=nfree-1; i++)
                    {
                        for(j=i; j<=nfree-1; j++)
                        {
                            s.ecadense[i,j] = s.tq2dense[i,j];
                        }
                    }
                    if( !trfac.spdmatrixcholeskyrec(ref s.ecadense, 0, nfree, true, ref s.tmp0) )
                    {
                        result = false;
                        return result;
                    }
                }
                else
                {
                    
                    //
                    // Diagonal ECA
                    //
                    s.ecakind = 1;
                    for(i=0; i<=nfree-1; i++)
                    {
                        if( (double)(s.tq2diag[i])<(double)(0) )
                        {
                            result = false;
                            return result;
                        }
                        s.ecadiag[i] = Math.Sqrt(s.tq2diag[i]);
                    }
                }
            }
            
            //
            // Compose EQ
            //
            if( s.isactivesetchanged || s.issecondarytermchanged )
            {
                for(i=0; i<=k-1; i++)
                {
                    for(j=0; j<=nfree-1; j++)
                    {
                        s.eq[i,j] = s.tk2[i,j];
                    }
                }
            }
            
            //
            // Calculate ECCM
            //
            if( ((((s.isactivesetchanged || s.ismaintermchanged) || s.issecondarytermchanged) && s.k>0) && (double)(s.theta)>(double)(0)) && nfree>0 )
            {
                
                //
                // Calculate ECCM - Cholesky factor of the "effective" capacitance
                // matrix CM = I + EQ*inv(EffectiveA)*EQ'.
                //
                // We calculate CM as follows:
                //   CM = I + EQ*inv(EffectiveA)*EQ'
                //      = I + EQ*ECA^(-1)*ECA^(-T)*EQ'
                //      = I + (EQ*ECA^(-1))*(EQ*ECA^(-1))'
                //
                // Then we perform Cholesky decomposition of CM.
                //
                apserv.rmatrixsetlengthatleast(ref s.tmp2, k, n);
                ablas.rmatrixcopy(k, nfree, s.eq, 0, 0, ref s.tmp2, 0, 0);
                alglib.ap.assert(s.ecakind==0 || s.ecakind==1, "CQMRebuild: unexpected ECAKind");
                if( s.ecakind==0 )
                {
                    ablas.rmatrixrighttrsm(k, nfree, s.ecadense, 0, 0, true, false, 0, ref s.tmp2, 0, 0);
                }
                if( s.ecakind==1 )
                {
                    for(i=0; i<=k-1; i++)
                    {
                        for(j=0; j<=nfree-1; j++)
                        {
                            s.tmp2[i,j] = s.tmp2[i,j]/s.ecadiag[j];
                        }
                    }
                }
                for(i=0; i<=k-1; i++)
                {
                    for(j=0; j<=k-1; j++)
                    {
                        s.eccm[i,j] = 0.0;
                    }
                    s.eccm[i,i] = 1.0;
                }
                ablas.rmatrixsyrk(k, nfree, 1.0, s.tmp2, 0, 0, 0, 1.0, ref s.eccm, 0, 0, true);
                if( !trfac.spdmatrixcholeskyrec(ref s.eccm, 0, k, true, ref s.tmp0) )
                {
                    result = false;
                    return result;
                }
            }
            
            //
            // Compose EB and EC
            //
            // NOTE: because these quantities are cheap to compute, we do not
            // use caching here.
            //
            for(i=0; i<=nfree-1; i++)
            {
                s.eb[i] = s.tq1[i]+s.tk1[i]+s.tb[i];
            }
            s.ec = s.tq0+s.tk0;
            for(i=nfree; i<=n-1; i++)
            {
                s.ec = s.ec+s.tb[i]*s.txc[i];
            }
            
            //
            // Change cache status - everything is cached 
            //
            s.ismaintermchanged = false;
            s.issecondarytermchanged = false;
            s.islineartermchanged = false;
            s.isactivesetchanged = false;
            return result;
        }


        /*************************************************************************
        Internal function, solves system Effective_A*x = b.
        It should be called after successful completion of CQMRebuild().

        INPUT PARAMETERS:
            S       -   quadratic model, after call to CQMRebuild()
            X       -   right part B, array[S.NFree]
            Tmp     -   temporary array, automatically reallocated if needed

        OUTPUT PARAMETERS:
            X       -   solution, array[S.NFree]
            
        NOTE: when called with zero S.NFree, returns silently
        NOTE: this function assumes that EA is non-degenerate

          -- ALGLIB --
             Copyright 10.05.2011 by Bochkanov Sergey
        *************************************************************************/
        private static void cqmsolveea(convexquadraticmodel s,
            ref double[] x,
            ref double[] tmp)
        {
            int i = 0;

            alglib.ap.assert((s.ecakind==0 || s.ecakind==1) || (s.ecakind==-1 && s.nfree==0), "CQMSolveEA: unexpected ECAKind");
            if( s.ecakind==0 )
            {
                
                //
                // Dense ECA, use FBLSCholeskySolve() dense solver.
                //
                fbls.fblscholeskysolve(s.ecadense, 1.0, s.nfree, true, ref x, ref tmp);
            }
            if( s.ecakind==1 )
            {
                
                //
                // Diagonal ECA
                //
                for(i=0; i<=s.nfree-1; i++)
                {
                    x[i] = x[i]/math.sqr(s.ecadiag[i]);
                }
            }
        }


    }
    public class snnls
    {
        /*************************************************************************
        This structure is a SNNLS (Specialized Non-Negative Least Squares) solver.

        It solves problems of the form |A*x-b|^2 => min subject to  non-negativity
        constraints on SOME components of x, with structured A (first  NS  columns
        are just unit matrix, next ND columns store dense part).

        This solver is suited for solution of many sequential NNLS  subproblems  -
        it keeps track of previously allocated memory and reuses  it  as  much  as
        possible.
        *************************************************************************/
        public class snnlssolver : apobject
        {
            public int ns;
            public int nd;
            public int nr;
            public double[,] densea;
            public double[] b;
            public bool[] nnc;
            public int refinementits;
            public double debugflops;
            public int debugmaxnewton;
            public double[] xn;
            public double[,] tmpz;
            public double[,] tmpca;
            public double[] g;
            public double[] d;
            public double[] dx;
            public double[] diagaa;
            public double[] cb;
            public double[] cx;
            public double[] cborg;
            public int[] columnmap;
            public int[] rowmap;
            public double[] tmpcholesky;
            public double[] r;
            public snnlssolver()
            {
                init();
            }
            public override void init()
            {
                densea = new double[0,0];
                b = new double[0];
                nnc = new bool[0];
                xn = new double[0];
                tmpz = new double[0,0];
                tmpca = new double[0,0];
                g = new double[0];
                d = new double[0];
                dx = new double[0];
                diagaa = new double[0];
                cb = new double[0];
                cx = new double[0];
                cborg = new double[0];
                columnmap = new int[0];
                rowmap = new int[0];
                tmpcholesky = new double[0];
                r = new double[0];
            }
            public override alglib.apobject make_copy()
            {
                snnlssolver _result = new snnlssolver();
                _result.ns = ns;
                _result.nd = nd;
                _result.nr = nr;
                _result.densea = (double[,])densea.Clone();
                _result.b = (double[])b.Clone();
                _result.nnc = (bool[])nnc.Clone();
                _result.refinementits = refinementits;
                _result.debugflops = debugflops;
                _result.debugmaxnewton = debugmaxnewton;
                _result.xn = (double[])xn.Clone();
                _result.tmpz = (double[,])tmpz.Clone();
                _result.tmpca = (double[,])tmpca.Clone();
                _result.g = (double[])g.Clone();
                _result.d = (double[])d.Clone();
                _result.dx = (double[])dx.Clone();
                _result.diagaa = (double[])diagaa.Clone();
                _result.cb = (double[])cb.Clone();
                _result.cx = (double[])cx.Clone();
                _result.cborg = (double[])cborg.Clone();
                _result.columnmap = (int[])columnmap.Clone();
                _result.rowmap = (int[])rowmap.Clone();
                _result.tmpcholesky = (double[])tmpcholesky.Clone();
                _result.r = (double[])r.Clone();
                return _result;
            }
        };




        public const int iterativerefinementits = 3;


        /*************************************************************************
        This subroutine is used to initialize SNNLS solver.

        By default, empty NNLS problem is produced, but we allocated enough  space
        to store problems with NSMax+NDMax columns and  NRMax  rows.  It  is  good
        place to provide algorithm with initial estimate of the space requirements,
        although you may underestimate problem size or even pass zero estimates  -
        in this case buffer variables will be resized automatically  when  you set
        NNLS problem.

        Previously allocated buffer variables are reused as much as possible. This
        function does not clear structure completely, it tries to preserve as much
        dynamically allocated memory as possible.

          -- ALGLIB --
             Copyright 10.10.2012 by Bochkanov Sergey
        *************************************************************************/
        public static void snnlsinit(int nsmax,
            int ndmax,
            int nrmax,
            snnlssolver s)
        {
            s.ns = 0;
            s.nd = 0;
            s.nr = 0;
            apserv.rmatrixsetlengthatleast(ref s.densea, nrmax, ndmax);
            apserv.rmatrixsetlengthatleast(ref s.tmpca, nrmax, ndmax);
            apserv.rmatrixsetlengthatleast(ref s.tmpz, ndmax, ndmax);
            apserv.rvectorsetlengthatleast(ref s.b, nrmax);
            apserv.bvectorsetlengthatleast(ref s.nnc, nsmax+ndmax);
            s.debugflops = 0.0;
            s.debugmaxnewton = 0;
            s.refinementits = iterativerefinementits;
        }


        /*************************************************************************
        This subroutine is used to set NNLS problem:

                ( [ 1     |      ]   [   ]   [   ] )^2
                ( [   1   |      ]   [   ]   [   ] )
            min ( [     1 |  Ad  ] * [ x ] - [ b ] )    s.t. x>=0
                ( [       |      ]   [   ]   [   ] )
                ( [       |      ]   [   ]   [   ] )

        where:
        * identity matrix has NS*NS size (NS<=NR, NS can be zero)
        * dense matrix Ad has NR*ND size
        * b is NR*1 vector
        * x is (NS+ND)*1 vector
        * all elements of x are non-negative (this constraint can be removed later
          by calling SNNLSDropNNC() function)

        Previously allocated buffer variables are reused as much as possible.
        After you set problem, you can solve it with SNNLSSolve().

        INPUT PARAMETERS:
            S   -   SNNLS solver, must be initialized with SNNLSInit() call
            A   -   array[NR,ND], dense part of the system
            B   -   array[NR], right part
            NS  -   size of the sparse part of the system, 0<=NS<=NR
            ND  -   size of the dense part of the system, ND>=0
            NR  -   rows count, NR>0

        NOTE:
            1. You can have NS+ND=0, solver will correctly accept such combination
               and return empty array as problem solution.
            
          -- ALGLIB --
             Copyright 10.10.2012 by Bochkanov Sergey
        *************************************************************************/
        public static void snnlssetproblem(snnlssolver s,
            double[,] a,
            double[] b,
            int ns,
            int nd,
            int nr)
        {
            int i = 0;
            int i_ = 0;

            alglib.ap.assert(nd>=0, "SNNLSSetProblem: ND<0");
            alglib.ap.assert(ns>=0, "SNNLSSetProblem: NS<0");
            alglib.ap.assert(nr>0, "SNNLSSetProblem: NR<=0");
            alglib.ap.assert(ns<=nr, "SNNLSSetProblem: NS>NR");
            alglib.ap.assert(alglib.ap.rows(a)>=nr || nd==0, "SNNLSSetProblem: rows(A)<NR");
            alglib.ap.assert(alglib.ap.cols(a)>=nd, "SNNLSSetProblem: cols(A)<ND");
            alglib.ap.assert(alglib.ap.len(b)>=nr, "SNNLSSetProblem: length(B)<NR");
            alglib.ap.assert(apserv.apservisfinitematrix(a, nr, nd), "SNNLSSetProblem: A contains INF/NAN");
            alglib.ap.assert(apserv.isfinitevector(b, nr), "SNNLSSetProblem: B contains INF/NAN");
            
            //
            // Copy problem
            //
            s.ns = ns;
            s.nd = nd;
            s.nr = nr;
            if( nd>0 )
            {
                apserv.rmatrixsetlengthatleast(ref s.densea, nr, nd);
                for(i=0; i<=nr-1; i++)
                {
                    for(i_=0; i_<=nd-1;i_++)
                    {
                        s.densea[i,i_] = a[i,i_];
                    }
                }
            }
            apserv.rvectorsetlengthatleast(ref s.b, nr);
            for(i_=0; i_<=nr-1;i_++)
            {
                s.b[i_] = b[i_];
            }
            apserv.bvectorsetlengthatleast(ref s.nnc, ns+nd);
            for(i=0; i<=ns+nd-1; i++)
            {
                s.nnc[i] = true;
            }
        }


        /*************************************************************************
        This subroutine drops non-negativity constraint from the  problem  set  by
        SNNLSSetProblem() call. This function must be called AFTER problem is set,
        because each SetProblem() call resets constraints to their  default  state
        (all constraints are present).

        INPUT PARAMETERS:
            S   -   SNNLS solver, must be initialized with SNNLSInit() call,
                    problem must be set with SNNLSSetProblem() call.
            Idx -   constraint index, 0<=IDX<NS+ND
            
          -- ALGLIB --
             Copyright 10.10.2012 by Bochkanov Sergey
        *************************************************************************/
        public static void snnlsdropnnc(snnlssolver s,
            int idx)
        {
            alglib.ap.assert(idx>=0, "SNNLSDropNNC: Idx<0");
            alglib.ap.assert(idx<s.ns+s.nd, "SNNLSDropNNC: Idx>=NS+ND");
            s.nnc[idx] = false;
        }


        /*************************************************************************
        This subroutine is used to solve NNLS problem.

        INPUT PARAMETERS:
            S   -   SNNLS solver, must be initialized with SNNLSInit() call and
                    problem must be set up with SNNLSSetProblem() call.
            X   -   possibly preallocated buffer, automatically resized if needed

        OUTPUT PARAMETERS:
            X   -   array[NS+ND], solution
            
        NOTE:
            1. You can have NS+ND=0, solver will correctly accept such combination
               and return empty array as problem solution.
            
            2. Internal field S.DebugFLOPS contains rough estimate of  FLOPs  used
               to solve problem. It can be used for debugging purposes. This field
               is real-valued.
            
          -- ALGLIB --
             Copyright 10.10.2012 by Bochkanov Sergey
        *************************************************************************/
        public static void snnlssolve(snnlssolver s,
            ref double[] x)
        {
            int i = 0;
            int j = 0;
            int ns = 0;
            int nd = 0;
            int nr = 0;
            int nsc = 0;
            int ndc = 0;
            int newtoncnt = 0;
            bool terminationneeded = new bool();
            double eps = 0;
            double fcur = 0;
            double fprev = 0;
            double fcand = 0;
            double noiselevel = 0;
            double noisetolerance = 0;
            double stplen = 0;
            double d2 = 0;
            double d1 = 0;
            double d0 = 0;
            bool wasactivation = new bool();
            int rfsits = 0;
            double lambdav = 0;
            double v0 = 0;
            double v1 = 0;
            double v = 0;
            int i_ = 0;
            int i1_ = 0;

            
            //
            // Prepare
            //
            ns = s.ns;
            nd = s.nd;
            nr = s.nr;
            s.debugflops = 0.0;
            
            //
            // Handle special cases:
            // * NS+ND=0
            // * ND=0
            //
            if( ns+nd==0 )
            {
                return;
            }
            if( nd==0 )
            {
                apserv.rvectorsetlengthatleast(ref x, ns);
                for(i=0; i<=ns-1; i++)
                {
                    x[i] = s.b[i];
                    if( s.nnc[i] )
                    {
                        x[i] = Math.Max(x[i], 0.0);
                    }
                }
                return;
            }
            
            //
            // Main cycle of BLEIC-SNNLS algorithm.
            // Below we assume that ND>0.
            //
            apserv.rvectorsetlengthatleast(ref x, ns+nd);
            apserv.rvectorsetlengthatleast(ref s.xn, ns+nd);
            apserv.rvectorsetlengthatleast(ref s.g, ns+nd);
            apserv.rvectorsetlengthatleast(ref s.d, ns+nd);
            apserv.rvectorsetlengthatleast(ref s.r, nr);
            apserv.rvectorsetlengthatleast(ref s.diagaa, nd);
            apserv.rvectorsetlengthatleast(ref s.dx, ns+nd);
            for(i=0; i<=ns+nd-1; i++)
            {
                x[i] = 0.0;
            }
            eps = 2*math.machineepsilon;
            noisetolerance = 10.0;
            lambdav = 1.0E6*math.machineepsilon;
            newtoncnt = 0;
            while( true )
            {
                
                //
                // Phase 1: perform steepest descent step.
                //
                // TerminationNeeded control variable is set on exit from this loop:
                // * TerminationNeeded=False in case we have to proceed to Phase 2 (Newton step)
                // * TerminationNeeded=True in case we found solution (step along projected gradient is small enough)
                //
                // Temporaries used:
                // * R      (I|A)*x-b
                //
                // NOTE 1. It is assumed that initial point X is feasible. This feasibility
                //         is retained during all iterations.
                //
                terminationneeded = false;
                while( true )
                {
                    
                    //
                    // Calculate gradient G and constrained descent direction D
                    //
                    for(i=0; i<=nr-1; i++)
                    {
                        i1_ = (ns)-(0);
                        v = 0.0;
                        for(i_=0; i_<=nd-1;i_++)
                        {
                            v += s.densea[i,i_]*x[i_+i1_];
                        }
                        if( i<ns )
                        {
                            v = v+x[i];
                        }
                        s.r[i] = v-s.b[i];
                    }
                    for(i=0; i<=ns-1; i++)
                    {
                        s.g[i] = s.r[i];
                    }
                    for(i=ns; i<=ns+nd-1; i++)
                    {
                        s.g[i] = 0.0;
                    }
                    for(i=0; i<=nr-1; i++)
                    {
                        v = s.r[i];
                        i1_ = (0) - (ns);
                        for(i_=ns; i_<=ns+nd-1;i_++)
                        {
                            s.g[i_] = s.g[i_] + v*s.densea[i,i_+i1_];
                        }
                    }
                    for(i=0; i<=ns+nd-1; i++)
                    {
                        if( (s.nnc[i] && (double)(x[i])<=(double)(0)) && (double)(s.g[i])>(double)(0) )
                        {
                            s.d[i] = 0.0;
                        }
                        else
                        {
                            s.d[i] = -s.g[i];
                        }
                    }
                    s.debugflops = s.debugflops+2*2*nr*nd;
                    
                    //
                    // Build quadratic model of F along descent direction:
                    //     F(x+alpha*d) = D2*alpha^2 + D1*alpha + D0
                    //
                    // Estimate numerical noise in the X (noise level is used
                    // to classify step as singificant or insignificant). Noise
                    // comes from two sources:
                    // * noise when calculating rows of (I|A)*x
                    // * noise when calculating norm of residual
                    //
                    // In case function curvature is negative or product of descent
                    // direction and gradient is non-negative, iterations are terminated.
                    //
                    // NOTE: D0 is not actually used, but we prefer to maintain it.
                    //
                    fprev = 0.0;
                    for(i_=0; i_<=nr-1;i_++)
                    {
                        fprev += s.r[i_]*s.r[i_];
                    }
                    fprev = fprev/2;
                    noiselevel = 0.0;
                    for(i=0; i<=nr-1; i++)
                    {
                        
                        //
                        // Estimate noise introduced by I-th row of (I|A)*x
                        //
                        v = 0.0;
                        if( i<ns )
                        {
                            v = eps*x[i];
                        }
                        for(j=0; j<=nd-1; j++)
                        {
                            v = Math.Max(v, eps*Math.Abs(s.densea[i,j]*x[ns+j]));
                        }
                        v = 2*Math.Abs(s.r[i]*v)+v*v;
                        
                        //
                        // Add to summary noise in the model
                        //
                        noiselevel = noiselevel+v;
                    }
                    noiselevel = Math.Max(noiselevel, eps*fprev);
                    d2 = 0.0;
                    for(i=0; i<=nr-1; i++)
                    {
                        i1_ = (ns)-(0);
                        v = 0.0;
                        for(i_=0; i_<=nd-1;i_++)
                        {
                            v += s.densea[i,i_]*s.d[i_+i1_];
                        }
                        if( i<ns )
                        {
                            v = v+s.d[i];
                        }
                        d2 = d2+0.5*math.sqr(v);
                    }
                    v = 0.0;
                    for(i_=0; i_<=ns+nd-1;i_++)
                    {
                        v += s.d[i_]*s.g[i_];
                    }
                    d1 = v;
                    d0 = fprev;
                    if( (double)(d2)<=(double)(0) || (double)(d1)>=(double)(0) )
                    {
                        terminationneeded = true;
                        break;
                    }
                    s.debugflops = s.debugflops+2*nr*nd;
                    
                    //
                    // Perform full (unconstrained) step with length StpLen in direction D.
                    //
                    // We can terminate iterations in case one of two criteria is met:
                    // 1. function change is dominated by noise (or function actually increased
                    //    instead of decreasing)
                    // 2. relative change in X is small enough
                    //
                    // First condition is not enough to guarantee algorithm termination because
                    // sometimes our noise estimate is too optimistic (say, in situations when
                    // function value at solition is zero).
                    //
                    stplen = -(d1/(2*d2));
                    for(i_=0; i_<=ns+nd-1;i_++)
                    {
                        s.xn[i_] = x[i_];
                    }
                    for(i_=0; i_<=ns+nd-1;i_++)
                    {
                        s.xn[i_] = s.xn[i_] + stplen*s.d[i_];
                    }
                    fcand = 0.0;
                    for(i=0; i<=nr-1; i++)
                    {
                        i1_ = (ns)-(0);
                        v = 0.0;
                        for(i_=0; i_<=nd-1;i_++)
                        {
                            v += s.densea[i,i_]*s.xn[i_+i1_];
                        }
                        if( i<ns )
                        {
                            v = v+s.xn[i];
                        }
                        fcand = fcand+0.5*math.sqr(v-s.b[i]);
                    }
                    s.debugflops = s.debugflops+2*nr*nd;
                    if( (double)(fcand)>=(double)(fprev-noiselevel*noisetolerance) )
                    {
                        terminationneeded = true;
                        break;
                    }
                    v = 0;
                    for(i=0; i<=ns+nd-1; i++)
                    {
                        v0 = Math.Abs(x[i]);
                        v1 = Math.Abs(s.xn[i]);
                        if( (double)(v0)!=(double)(0) || (double)(v1)!=(double)(0) )
                        {
                            v = Math.Max(v, Math.Abs(x[i]-s.xn[i])/Math.Max(v0, v1));
                        }
                    }
                    if( (double)(v)<=(double)(eps*noisetolerance) )
                    {
                        terminationneeded = true;
                        break;
                    }
                    
                    //
                    // Perform step one more time, now with non-negativity constraints.
                    //
                    // NOTE: complicated code below which deals with VarIdx temporary makes
                    //       sure that in case unconstrained step leads us outside of feasible
                    //       area, we activate at least one constraint.
                    //
                    wasactivation = boundedstepandactivation(x, s.xn, s.nnc, ns+nd);
                    fcur = 0.0;
                    for(i=0; i<=nr-1; i++)
                    {
                        i1_ = (ns)-(0);
                        v = 0.0;
                        for(i_=0; i_<=nd-1;i_++)
                        {
                            v += s.densea[i,i_]*x[i_+i1_];
                        }
                        if( i<ns )
                        {
                            v = v+x[i];
                        }
                        fcur = fcur+0.5*math.sqr(v-s.b[i]);
                    }
                    s.debugflops = s.debugflops+2*nr*nd;
                    
                    //
                    // Depending on results, decide what to do:
                    // 1. In case step was performed without activation of constraints,
                    //    we proceed to Newton method
                    // 2. In case there was activated at least one constraint, we repeat
                    //    steepest descent step.
                    //
                    if( !wasactivation )
                    {
                        
                        //
                        // Step without activation, proceed to Newton
                        //
                        break;
                    }
                }
                if( terminationneeded )
                {
                    break;
                }
                
                //
                // Phase 2: Newton method.
                //
                apserv.rvectorsetlengthatleast(ref s.cx, ns+nd);
                apserv.ivectorsetlengthatleast(ref s.columnmap, ns+nd);
                apserv.ivectorsetlengthatleast(ref s.rowmap, nr);
                apserv.rmatrixsetlengthatleast(ref s.tmpca, nr, nd);
                apserv.rmatrixsetlengthatleast(ref s.tmpz, nd, nd);
                apserv.rvectorsetlengthatleast(ref s.cborg, nr);
                apserv.rvectorsetlengthatleast(ref s.cb, nr);
                terminationneeded = false;
                while( true )
                {
                    
                    //
                    // Prepare equality constrained subproblem with NSC<=NS "sparse"
                    // variables and NDC<=ND "dense" variables.
                    //
                    // First, we reorder variables (columns) and move all unconstrained
                    // variables "to the left", ColumnMap stores this permutation.
                    //
                    // Then, we reorder first NS rows of A and first NS elements of B in
                    // such way that we still have identity matrix in first NSC columns
                    // of problem. This permutation is stored in RowMap.
                    //
                    nsc = 0;
                    ndc = 0;
                    for(i=0; i<=ns-1; i++)
                    {
                        if( !(s.nnc[i] && (double)(x[i])==(double)(0)) )
                        {
                            s.columnmap[nsc] = i;
                            nsc = nsc+1;
                        }
                    }
                    for(i=ns; i<=ns+nd-1; i++)
                    {
                        if( !(s.nnc[i] && (double)(x[i])==(double)(0)) )
                        {
                            s.columnmap[nsc+ndc] = i;
                            ndc = ndc+1;
                        }
                    }
                    for(i=0; i<=nsc-1; i++)
                    {
                        s.rowmap[i] = s.columnmap[i];
                    }
                    j = nsc;
                    for(i=0; i<=ns-1; i++)
                    {
                        if( s.nnc[i] && (double)(x[i])==(double)(0) )
                        {
                            s.rowmap[j] = i;
                            j = j+1;
                        }
                    }
                    for(i=ns; i<=nr-1; i++)
                    {
                        s.rowmap[i] = i;
                    }
                    
                    //
                    // Now, permutations are ready, and we can copy/reorder
                    // A, B and X to CA, CB and CX.
                    //
                    for(i=0; i<=nsc+ndc-1; i++)
                    {
                        s.cx[i] = x[s.columnmap[i]];
                    }
                    for(i=0; i<=nr-1; i++)
                    {
                        for(j=0; j<=ndc-1; j++)
                        {
                            s.tmpca[i,j] = s.densea[s.rowmap[i],s.columnmap[nsc+j]-ns];
                        }
                        s.cb[i] = s.b[s.rowmap[i]];
                    }
                    
                    //
                    // Solve equality constrained subproblem.
                    //
                    if( ndc>0 )
                    {
                        
                        //
                        // NDC>0.
                        //
                        // Solve subproblem using Newton-type algorithm. We have a
                        // NR*(NSC+NDC) linear least squares subproblem
                        //
                        //         | ( I  AU )   ( XU )   ( BU ) |^2
                        //     min | (       ) * (    ) - (    ) |
                        //         | ( 0  AL )   ( XL )   ( BL ) |
                        //
                        // where:
                        // * I is a NSC*NSC identity matrix
                        // * AU is NSC*NDC dense matrix (first NSC rows of CA)
                        // * AL is (NR-NSC)*NDC dense matrix (next NR-NSC rows of CA)
                        // * BU and BL are correspondingly sized parts of CB
                        //
                        // After conversion to normal equations and small regularization,
                        // we get:
                        //
                        //     ( I   AU ) (  XU )   ( BU            )
                        //     (        )*(     ) = (               )
                        //     ( AU' Y  ) (  XL )   ( AU'*BU+AL'*BL )
                        //
                        // where Y = AU'*AU + AL'*AL + lambda*diag(AU'*AU+AL'*AL).
                        //
                        // With Schur Complement Method this system can be solved in
                        // O(NR*NDC^2+NDC^3) operations. In order to solve it we multiply
                        // first row by AU' and subtract it from the second one. As result,
                        // we get system
                        //
                        //     Z*XL = AL'*BL, where Z=AL'*AL+lambda*diag(AU'*AU+AL'*AL)
                        //
                        // We can easily solve it for XL, and we can get XU as XU = BU-AU*XL.
                        //
                        // We will start solution from calculating Cholesky decomposition of Z.
                        //
                        for(i=0; i<=nr-1; i++)
                        {
                            s.cborg[i] = s.cb[i];
                        }
                        for(i=0; i<=ndc-1; i++)
                        {
                            s.diagaa[i] = 0;
                        }
                        for(i=0; i<=nr-1; i++)
                        {
                            for(j=0; j<=ndc-1; j++)
                            {
                                s.diagaa[j] = s.diagaa[j]+math.sqr(s.tmpca[i,j]);
                            }
                        }
                        for(j=0; j<=ndc-1; j++)
                        {
                            if( (double)(s.diagaa[j])==(double)(0) )
                            {
                                s.diagaa[j] = 1;
                            }
                        }
                        while( true )
                        {
                            
                            //
                            // NOTE: we try to factorize Z. In case of failure we increase
                            //       regularization parameter and try again.
                            //
                            s.debugflops = s.debugflops+2*(nr-nsc)*math.sqr(ndc)+Math.Pow(ndc, 3)/3;
                            for(i=0; i<=ndc-1; i++)
                            {
                                for(j=0; j<=ndc-1; j++)
                                {
                                    s.tmpz[i,j] = 0.0;
                                }
                            }
                            ablas.rmatrixsyrk(ndc, nr-nsc, 1.0, s.tmpca, nsc, 0, 2, 0.0, ref s.tmpz, 0, 0, true);
                            for(i=0; i<=ndc-1; i++)
                            {
                                s.tmpz[i,i] = s.tmpz[i,i]+lambdav*s.diagaa[i];
                            }
                            if( trfac.spdmatrixcholeskyrec(ref s.tmpz, 0, ndc, true, ref s.tmpcholesky) )
                            {
                                break;
                            }
                            lambdav = lambdav*10;
                        }
                        
                        //
                        // We have Cholesky decomposition of Z, now we can solve system:
                        // * we start from initial point CX
                        // * we perform several iterations of refinement:
                        //   * BU_new := BU_orig - XU_cur - AU*XL_cur
                        //   * BL_new := BL_orig - AL*XL_cur
                        //   * solve for BU_new/BL_new, obtain solution dx
                        //   * XU_cur := XU_cur + dx_u
                        //   * XL_cur := XL_cur + dx_l
                        // * BU_new/BL_new are stored in CB, original right part is
                        //   stored in CBOrg, correction to X is stored in DX, current
                        //   X is stored in CX
                        //
                        for(rfsits=1; rfsits<=s.refinementits; rfsits++)
                        {
                            for(i=0; i<=nr-1; i++)
                            {
                                i1_ = (nsc)-(0);
                                v = 0.0;
                                for(i_=0; i_<=ndc-1;i_++)
                                {
                                    v += s.tmpca[i,i_]*s.cx[i_+i1_];
                                }
                                s.cb[i] = s.cborg[i]-v;
                                if( i<nsc )
                                {
                                    s.cb[i] = s.cb[i]-s.cx[i];
                                }
                            }
                            s.debugflops = s.debugflops+2*nr*ndc;
                            for(i=0; i<=ndc-1; i++)
                            {
                                s.dx[i] = 0.0;
                            }
                            for(i=nsc; i<=nr-1; i++)
                            {
                                v = s.cb[i];
                                for(i_=0; i_<=ndc-1;i_++)
                                {
                                    s.dx[i_] = s.dx[i_] + v*s.tmpca[i,i_];
                                }
                            }
                            fbls.fblscholeskysolve(s.tmpz, 1.0, ndc, true, ref s.dx, ref s.tmpcholesky);
                            s.debugflops = s.debugflops+2*ndc*ndc;
                            i1_ = (0) - (nsc);
                            for(i_=nsc; i_<=nsc+ndc-1;i_++)
                            {
                                s.cx[i_] = s.cx[i_] + s.dx[i_+i1_];
                            }
                            for(i=0; i<=nsc-1; i++)
                            {
                                v = 0.0;
                                for(i_=0; i_<=ndc-1;i_++)
                                {
                                    v += s.tmpca[i,i_]*s.dx[i_];
                                }
                                s.cx[i] = s.cx[i]+s.cb[i]-v;
                            }
                            s.debugflops = s.debugflops+2*nsc*ndc;
                        }
                    }
                    else
                    {
                        
                        //
                        // NDC=0.
                        //
                        // We have a NR*NSC linear least squares subproblem
                        //
                        //     min |XU-BU|^2
                        //
                        // solution is easy to find - it is XU=BU!
                        //
                        for(i=0; i<=nsc-1; i++)
                        {
                            s.cx[i] = s.cb[i];
                        }
                    }
                    for(i=0; i<=ns+nd-1; i++)
                    {
                        s.xn[i] = x[i];
                    }
                    for(i=0; i<=nsc+ndc-1; i++)
                    {
                        s.xn[s.columnmap[i]] = s.cx[i];
                    }
                    newtoncnt = newtoncnt+1;
                    
                    //
                    // Step to candidate point.
                    // If no constraints was added, accept candidate point XN and move to next phase.
                    //
                    terminationneeded = s.debugmaxnewton>0 && newtoncnt>=s.debugmaxnewton;
                    if( !boundedstepandactivation(x, s.xn, s.nnc, ns+nd) )
                    {
                        break;
                    }
                    if( terminationneeded )
                    {
                        break;
                    }
                }
                if( terminationneeded )
                {
                    break;
                }
            }
        }


        /*************************************************************************
        Having feasible current point XC and possibly infeasible candidate   point
        XN,  this  function  performs  longest  step  from  XC to XN which retains
        feasibility. In case XN is found to be infeasible, at least one constraint
        is activated.

        For example, if we have:
          XC=0.5
          XN=-1.2
          x>=0
        then this function will move us to X=0 and activate constraint "x>=0".

        INPUT PARAMETERS:
            XC      -   current point, must be feasible with respect to
                        all constraints
            XN      -   candidate point, can be infeasible with respect to some
                        constraints
            NNC     -   NNC[i] is True when I-th variable is non-negatively
                        constrained
            N       -   variable count

        OUTPUT PARAMETERS:
            XC      -   new position

        RESULT:
            True in case at least one constraint was activated by step

          -- ALGLIB --
             Copyright 19.10.2012 by Bochkanov Sergey
        *************************************************************************/
        private static bool boundedstepandactivation(double[] xc,
            double[] xn,
            bool[] nnc,
            int n)
        {
            bool result = new bool();
            int i = 0;
            int varidx = 0;
            double vmax = 0;
            double v = 0;
            double stplen = 0;

            
            //
            // Check constraints.
            //
            // NOTE: it is important to test for XN[i]<XC[i] (strict inequality,
            //       allows to handle correctly situations with XC[i]=0 without
            //       activating already active constraints), but to check for
            //       XN[i]<=0 (non-strict inequality, correct handling of some
            //       special cases when unconstrained step ends at the boundary).
            //
            result = false;
            varidx = -1;
            vmax = math.maxrealnumber;
            for(i=0; i<=n-1; i++)
            {
                if( (nnc[i] && (double)(xn[i])<(double)(xc[i])) && (double)(xn[i])<=(double)(0.0) )
                {
                    v = vmax;
                    vmax = apserv.safeminposrv(xc[i], xc[i]-xn[i], vmax);
                    if( (double)(vmax)<(double)(v) )
                    {
                        varidx = i;
                    }
                }
            }
            stplen = Math.Min(vmax, 1.0);
            
            //
            // Perform step with activation.
            //
            // NOTE: it is important to use (1-StpLen)*XC + StpLen*XN because
            //       it allows us to step exactly to XN when StpLen=1, even in
            //       the presence of numerical errors.
            //
            for(i=0; i<=n-1; i++)
            {
                xc[i] = (1-stplen)*xc[i]+stplen*xn[i];
            }
            if( varidx>=0 )
            {
                xc[varidx] = 0.0;
                result = true;
            }
            for(i=0; i<=n-1; i++)
            {
                if( nnc[i] && (double)(xc[i])<(double)(0.0) )
                {
                    xc[i] = 0.0;
                    result = true;
                }
            }
            return result;
        }


    }
    public class sactivesets
    {
        /*************************************************************************
        This structure describes set of linear constraints (boundary  and  general
        ones) which can be active and inactive. It also has functionality to  work
        with current point and current  gradient  (determine  active  constraints,
        move current point, project gradient into  constrained  subspace,  perform
        constrained preconditioning and so on.

        This  structure  is  intended  to  be  used  by constrained optimizers for
        management of all constraint-related functionality.

        External code may access following internal fields of the structure:
            XC          -   stores current point, array[N].
                            can be accessed only in optimization mode
            ActiveSet   -   active set, array[N+NEC+NIC]:
                            * ActiveSet[I]>0    I-th constraint is in the active set
                            * ActiveSet[I]=0    I-th constraint is at the boundary, but inactive
                            * ActiveSet[I]<0    I-th constraint is far from the boundary (and inactive)
                            * elements from 0 to N-1 correspond to boundary constraints
                            * elements from N to N+NEC+NIC-1 correspond to linear constraints
                            * elements from N to N+NEC-1 are always +1
            PBasis,
            IBasis,
            SBasis      -   after call to SASRebuildBasis() these  matrices  store
                            active constraints, reorthogonalized with  respect  to
                            some inner product:
                            a) for PBasis - one  given  by  preconditioner  matrix
                               (inverse Hessian)
                            b) for SBasis - one given by square of the scale matrix
                            c) for IBasis - traditional dot product
                            
                            array[BasisSize,N+1], where BasisSize is a  number  of
                            positive elements in  ActiveSet[N:N+NEC+NIC-1].  First
                            N columns store linear term, last column stores  right
                            part. All  three  matrices  are linearly equivalent to
                            each other, span(PBasis)=span(IBasis)=span(SBasis).
                            
                            IMPORTANT: you have to call  SASRebuildBasis()  before
                                       accessing these arrays  in  order  to  make
                                       sure that they are up to date.
            BasisSize   -   basis size (PBasis/SBasis/IBasis)
        *************************************************************************/
        public class sactiveset : apobject
        {
            public int n;
            public int algostate;
            public double[] xc;
            public bool hasxc;
            public double[] s;
            public double[] h;
            public int[] activeset;
            public bool basisisready;
            public double[,] sbasis;
            public double[,] pbasis;
            public double[,] ibasis;
            public int basissize;
            public bool constraintschanged;
            public bool[] hasbndl;
            public bool[] hasbndu;
            public double[] bndl;
            public double[] bndu;
            public double[,] cleic;
            public int nec;
            public int nic;
            public double[] mtx;
            public int[] mtas;
            public double[] cdtmp;
            public double[] corrtmp;
            public double[] unitdiagonal;
            public snnls.snnlssolver solver;
            public double[] scntmp;
            public double[] tmp0;
            public double[] tmpfeas;
            public double[,] tmpm0;
            public double[] rctmps;
            public double[] rctmpg;
            public double[] rctmprightpart;
            public double[,] rctmpdense0;
            public double[,] rctmpdense1;
            public bool[] rctmpisequality;
            public int[] rctmpconstraintidx;
            public double[] rctmplambdas;
            public double[,] tmpbasis;
            public sactiveset()
            {
                init();
            }
            public override void init()
            {
                xc = new double[0];
                s = new double[0];
                h = new double[0];
                activeset = new int[0];
                sbasis = new double[0,0];
                pbasis = new double[0,0];
                ibasis = new double[0,0];
                hasbndl = new bool[0];
                hasbndu = new bool[0];
                bndl = new double[0];
                bndu = new double[0];
                cleic = new double[0,0];
                mtx = new double[0];
                mtas = new int[0];
                cdtmp = new double[0];
                corrtmp = new double[0];
                unitdiagonal = new double[0];
                solver = new snnls.snnlssolver();
                scntmp = new double[0];
                tmp0 = new double[0];
                tmpfeas = new double[0];
                tmpm0 = new double[0,0];
                rctmps = new double[0];
                rctmpg = new double[0];
                rctmprightpart = new double[0];
                rctmpdense0 = new double[0,0];
                rctmpdense1 = new double[0,0];
                rctmpisequality = new bool[0];
                rctmpconstraintidx = new int[0];
                rctmplambdas = new double[0];
                tmpbasis = new double[0,0];
            }
            public override alglib.apobject make_copy()
            {
                sactiveset _result = new sactiveset();
                _result.n = n;
                _result.algostate = algostate;
                _result.xc = (double[])xc.Clone();
                _result.hasxc = hasxc;
                _result.s = (double[])s.Clone();
                _result.h = (double[])h.Clone();
                _result.activeset = (int[])activeset.Clone();
                _result.basisisready = basisisready;
                _result.sbasis = (double[,])sbasis.Clone();
                _result.pbasis = (double[,])pbasis.Clone();
                _result.ibasis = (double[,])ibasis.Clone();
                _result.basissize = basissize;
                _result.constraintschanged = constraintschanged;
                _result.hasbndl = (bool[])hasbndl.Clone();
                _result.hasbndu = (bool[])hasbndu.Clone();
                _result.bndl = (double[])bndl.Clone();
                _result.bndu = (double[])bndu.Clone();
                _result.cleic = (double[,])cleic.Clone();
                _result.nec = nec;
                _result.nic = nic;
                _result.mtx = (double[])mtx.Clone();
                _result.mtas = (int[])mtas.Clone();
                _result.cdtmp = (double[])cdtmp.Clone();
                _result.corrtmp = (double[])corrtmp.Clone();
                _result.unitdiagonal = (double[])unitdiagonal.Clone();
                _result.solver = (snnls.snnlssolver)solver.make_copy();
                _result.scntmp = (double[])scntmp.Clone();
                _result.tmp0 = (double[])tmp0.Clone();
                _result.tmpfeas = (double[])tmpfeas.Clone();
                _result.tmpm0 = (double[,])tmpm0.Clone();
                _result.rctmps = (double[])rctmps.Clone();
                _result.rctmpg = (double[])rctmpg.Clone();
                _result.rctmprightpart = (double[])rctmprightpart.Clone();
                _result.rctmpdense0 = (double[,])rctmpdense0.Clone();
                _result.rctmpdense1 = (double[,])rctmpdense1.Clone();
                _result.rctmpisequality = (bool[])rctmpisequality.Clone();
                _result.rctmpconstraintidx = (int[])rctmpconstraintidx.Clone();
                _result.rctmplambdas = (double[])rctmplambdas.Clone();
                _result.tmpbasis = (double[,])tmpbasis.Clone();
                return _result;
            }
        };




        /*************************************************************************
        This   subroutine   is   used  to initialize active set. By default, empty
        N-variable model with no constraints is  generated.  Previously  allocated
        buffer variables are reused as much as possible.

        Two use cases for this object are described below.

        CASE 1 - STEEPEST DESCENT:

            SASInit()
            repeat:
                SASReactivateConstraints()
                SASDescentDirection()
                SASExploreDirection()
                SASMoveTo()
            until convergence

        CASE 1 - PRECONDITIONED STEEPEST DESCENT:

            SASInit()
            repeat:
                SASReactivateConstraintsPrec()
                SASDescentDirectionPrec()
                SASExploreDirection()
                SASMoveTo()
            until convergence

          -- ALGLIB --
             Copyright 21.12.2012 by Bochkanov Sergey
        *************************************************************************/
        public static void sasinit(int n,
            sactiveset s)
        {
            int i = 0;

            s.n = n;
            s.algostate = 0;
            
            //
            // Constraints
            //
            s.constraintschanged = true;
            s.nec = 0;
            s.nic = 0;
            apserv.rvectorsetlengthatleast(ref s.bndl, n);
            apserv.bvectorsetlengthatleast(ref s.hasbndl, n);
            apserv.rvectorsetlengthatleast(ref s.bndu, n);
            apserv.bvectorsetlengthatleast(ref s.hasbndu, n);
            for(i=0; i<=n-1; i++)
            {
                s.bndl[i] = Double.NegativeInfinity;
                s.bndu[i] = Double.PositiveInfinity;
                s.hasbndl[i] = false;
                s.hasbndu[i] = false;
            }
            
            //
            // current point, scale
            //
            s.hasxc = false;
            apserv.rvectorsetlengthatleast(ref s.xc, n);
            apserv.rvectorsetlengthatleast(ref s.s, n);
            apserv.rvectorsetlengthatleast(ref s.h, n);
            for(i=0; i<=n-1; i++)
            {
                s.xc[i] = 0.0;
                s.s[i] = 1.0;
                s.h[i] = 1.0;
            }
            
            //
            // Other
            //
            apserv.rvectorsetlengthatleast(ref s.unitdiagonal, n);
            for(i=0; i<=n-1; i++)
            {
                s.unitdiagonal[i] = 1.0;
            }
        }


        /*************************************************************************
        This function sets scaling coefficients for SAS object.

        ALGLIB optimizers use scaling matrices to test stopping  conditions  (step
        size and gradient are scaled before comparison with tolerances).  Scale of
        the I-th variable is a translation invariant measure of:
        a) "how large" the variable is
        b) how large the step should be to make significant changes in the function

        During orthogonalization phase, scale is used to calculate drop tolerances
        (whether vector is significantly non-zero or not).

        INPUT PARAMETERS:
            State   -   structure stores algorithm state
            S       -   array[N], non-zero scaling coefficients
                        S[i] may be negative, sign doesn't matter.

          -- ALGLIB --
             Copyright 21.12.2012 by Bochkanov Sergey
        *************************************************************************/
        public static void sassetscale(sactiveset state,
            double[] s)
        {
            int i = 0;

            alglib.ap.assert(state.algostate==0, "SASSetScale: you may change scale only in modification mode");
            alglib.ap.assert(alglib.ap.len(s)>=state.n, "SASSetScale: Length(S)<N");
            for(i=0; i<=state.n-1; i++)
            {
                alglib.ap.assert(math.isfinite(s[i]), "SASSetScale: S contains infinite or NAN elements");
                alglib.ap.assert((double)(s[i])!=(double)(0), "SASSetScale: S contains zero elements");
            }
            for(i=0; i<=state.n-1; i++)
            {
                state.s[i] = Math.Abs(s[i]);
            }
        }


        /*************************************************************************
        Modification  of  the  preconditioner:  diagonal of approximate Hessian is
        used.

        INPUT PARAMETERS:
            State   -   structure which stores algorithm state
            D       -   diagonal of the approximate Hessian, array[0..N-1],
                        (if larger, only leading N elements are used).

        NOTE 1: D[i] should be positive. Exception will be thrown otherwise.

        NOTE 2: you should pass diagonal of approximate Hessian - NOT ITS INVERSE.

          -- ALGLIB --
             Copyright 21.12.2012 by Bochkanov Sergey
        *************************************************************************/
        public static void sassetprecdiag(sactiveset state,
            double[] d)
        {
            int i = 0;

            alglib.ap.assert(state.algostate==0, "SASSetPrecDiag: you may change preconditioner only in modification mode");
            alglib.ap.assert(alglib.ap.len(d)>=state.n, "SASSetPrecDiag: D is too short");
            for(i=0; i<=state.n-1; i++)
            {
                alglib.ap.assert(math.isfinite(d[i]), "SASSetPrecDiag: D contains infinite or NAN elements");
                alglib.ap.assert((double)(d[i])>(double)(0), "SASSetPrecDiag: D contains non-positive elements");
            }
            for(i=0; i<=state.n-1; i++)
            {
                state.h[i] = d[i];
            }
        }


        /*************************************************************************
        This function sets/changes boundary constraints.

        INPUT PARAMETERS:
            State   -   structure stores algorithm state
            BndL    -   lower bounds, array[N].
                        If some (all) variables are unbounded, you may specify
                        very small number or -INF.
            BndU    -   upper bounds, array[N].
                        If some (all) variables are unbounded, you may specify
                        very large number or +INF.

        NOTE 1: it is possible to specify BndL[i]=BndU[i]. In this case I-th
        variable will be "frozen" at X[i]=BndL[i]=BndU[i].

          -- ALGLIB --
             Copyright 21.12.2012 by Bochkanov Sergey
        *************************************************************************/
        public static void sassetbc(sactiveset state,
            double[] bndl,
            double[] bndu)
        {
            int i = 0;
            int n = 0;

            alglib.ap.assert(state.algostate==0, "SASSetBC: you may change constraints only in modification mode");
            n = state.n;
            alglib.ap.assert(alglib.ap.len(bndl)>=n, "SASSetBC: Length(BndL)<N");
            alglib.ap.assert(alglib.ap.len(bndu)>=n, "SASSetBC: Length(BndU)<N");
            for(i=0; i<=n-1; i++)
            {
                alglib.ap.assert(math.isfinite(bndl[i]) || Double.IsNegativeInfinity(bndl[i]), "SASSetBC: BndL contains NAN or +INF");
                alglib.ap.assert(math.isfinite(bndu[i]) || Double.IsPositiveInfinity(bndu[i]), "SASSetBC: BndL contains NAN or -INF");
                state.bndl[i] = bndl[i];
                state.hasbndl[i] = math.isfinite(bndl[i]);
                state.bndu[i] = bndu[i];
                state.hasbndu[i] = math.isfinite(bndu[i]);
            }
            state.constraintschanged = true;
        }


        /*************************************************************************
        This function sets linear constraints for SAS object.

        Linear constraints are inactive by default (after initial creation).

        INPUT PARAMETERS:
            State   -   SAS structure
            C       -   linear constraints, array[K,N+1].
                        Each row of C represents one constraint, either equality
                        or inequality (see below):
                        * first N elements correspond to coefficients,
                        * last element corresponds to the right part.
                        All elements of C (including right part) must be finite.
            CT      -   type of constraints, array[K]:
                        * if CT[i]>0, then I-th constraint is C[i,*]*x >= C[i,n+1]
                        * if CT[i]=0, then I-th constraint is C[i,*]*x  = C[i,n+1]
                        * if CT[i]<0, then I-th constraint is C[i,*]*x <= C[i,n+1]
            K       -   number of equality/inequality constraints, K>=0

        NOTE 1: linear (non-bound) constraints are satisfied only approximately:
        * there always exists some minor violation (about Epsilon in magnitude)
          due to rounding errors
        * numerical differentiation, if used, may  lead  to  function  evaluations
          outside  of the feasible  area,   because   algorithm  does  NOT  change
          numerical differentiation formula according to linear constraints.
        If you want constraints to be  satisfied  exactly, try to reformulate your
        problem  in  such  manner  that  all constraints will become boundary ones
        (this kind of constraints is always satisfied exactly, both in  the  final
        solution and in all intermediate points).

          -- ALGLIB --
             Copyright 28.11.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void sassetlc(sactiveset state,
            double[,] c,
            int[] ct,
            int k)
        {
            int n = 0;
            int i = 0;
            int i_ = 0;

            alglib.ap.assert(state.algostate==0, "SASSetLC: you may change constraints only in modification mode");
            n = state.n;
            
            //
            // First, check for errors in the inputs
            //
            alglib.ap.assert(k>=0, "SASSetLC: K<0");
            alglib.ap.assert(alglib.ap.cols(c)>=n+1 || k==0, "SASSetLC: Cols(C)<N+1");
            alglib.ap.assert(alglib.ap.rows(c)>=k, "SASSetLC: Rows(C)<K");
            alglib.ap.assert(alglib.ap.len(ct)>=k, "SASSetLC: Length(CT)<K");
            alglib.ap.assert(apserv.apservisfinitematrix(c, k, n+1), "SASSetLC: C contains infinite or NaN values!");
            
            //
            // Handle zero K
            //
            if( k==0 )
            {
                state.nec = 0;
                state.nic = 0;
                state.constraintschanged = true;
                return;
            }
            
            //
            // Equality constraints are stored first, in the upper
            // NEC rows of State.CLEIC matrix. Inequality constraints
            // are stored in the next NIC rows.
            //
            // NOTE: we convert inequality constraints to the form
            // A*x<=b before copying them.
            //
            apserv.rmatrixsetlengthatleast(ref state.cleic, k, n+1);
            state.nec = 0;
            state.nic = 0;
            for(i=0; i<=k-1; i++)
            {
                if( ct[i]==0 )
                {
                    for(i_=0; i_<=n;i_++)
                    {
                        state.cleic[state.nec,i_] = c[i,i_];
                    }
                    state.nec = state.nec+1;
                }
            }
            for(i=0; i<=k-1; i++)
            {
                if( ct[i]!=0 )
                {
                    if( ct[i]>0 )
                    {
                        for(i_=0; i_<=n;i_++)
                        {
                            state.cleic[state.nec+state.nic,i_] = -c[i,i_];
                        }
                    }
                    else
                    {
                        for(i_=0; i_<=n;i_++)
                        {
                            state.cleic[state.nec+state.nic,i_] = c[i,i_];
                        }
                    }
                    state.nic = state.nic+1;
                }
            }
            
            //
            // Mark state as changed
            //
            state.constraintschanged = true;
        }


        /*************************************************************************
        Another variation of SASSetLC(), which accepts  linear  constraints  using
        another representation.

        Linear constraints are inactive by default (after initial creation).

        INPUT PARAMETERS:
            State   -   SAS structure
            CLEIC   -   linear constraints, array[NEC+NIC,N+1].
                        Each row of C represents one constraint:
                        * first N elements correspond to coefficients,
                        * last element corresponds to the right part.
                        First NEC rows store equality constraints, next NIC -  are
                        inequality ones.
                        All elements of C (including right part) must be finite.
            NEC     -   number of equality constraints, NEC>=0
            NIC     -   number of inequality constraints, NIC>=0

        NOTE 1: linear (non-bound) constraints are satisfied only approximately:
        * there always exists some minor violation (about Epsilon in magnitude)
          due to rounding errors
        * numerical differentiation, if used, may  lead  to  function  evaluations
          outside  of the feasible  area,   because   algorithm  does  NOT  change
          numerical differentiation formula according to linear constraints.
        If you want constraints to be  satisfied  exactly, try to reformulate your
        problem  in  such  manner  that  all constraints will become boundary ones
        (this kind of constraints is always satisfied exactly, both in  the  final
        solution and in all intermediate points).

          -- ALGLIB --
             Copyright 28.11.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void sassetlcx(sactiveset state,
            double[,] cleic,
            int nec,
            int nic)
        {
            int n = 0;
            int i = 0;
            int j = 0;

            alglib.ap.assert(state.algostate==0, "SASSetLCX: you may change constraints only in modification mode");
            n = state.n;
            
            //
            // First, check for errors in the inputs
            //
            alglib.ap.assert(nec>=0, "SASSetLCX: NEC<0");
            alglib.ap.assert(nic>=0, "SASSetLCX: NIC<0");
            alglib.ap.assert(alglib.ap.cols(cleic)>=n+1 || nec+nic==0, "SASSetLCX: Cols(CLEIC)<N+1");
            alglib.ap.assert(alglib.ap.rows(cleic)>=nec+nic, "SASSetLCX: Rows(CLEIC)<NEC+NIC");
            alglib.ap.assert(apserv.apservisfinitematrix(cleic, nec+nic, n+1), "SASSetLCX: CLEIC contains infinite or NaN values!");
            
            //
            // Store constraints
            //
            apserv.rmatrixsetlengthatleast(ref state.cleic, nec+nic, n+1);
            state.nec = nec;
            state.nic = nic;
            for(i=0; i<=nec+nic-1; i++)
            {
                for(j=0; j<=n; j++)
                {
                    state.cleic[i,j] = cleic[i,j];
                }
            }
            
            //
            // Mark state as changed
            //
            state.constraintschanged = true;
        }


        /*************************************************************************
        This subroutine turns on optimization mode:
        1. feasibility in X is enforced  (in case X=S.XC and constraints  have not
           changed, algorithm just uses X without any modifications at all)
        2. constraints are marked as "candidate" or "inactive"

        INPUT PARAMETERS:
            S   -   active set object
            X   -   initial point (candidate), array[N]. It is expected that X
                    contains only finite values (we do not check it).
            
        OUTPUT PARAMETERS:
            S   -   state is changed
            X   -   initial point can be changed to enforce feasibility
            
        RESULT:
            True in case feasible point was found (mode was changed to "optimization")
            False in case no feasible point was found (mode was not changed)

          -- ALGLIB --
             Copyright 21.12.2012 by Bochkanov Sergey
        *************************************************************************/
        public static bool sasstartoptimization(sactiveset state,
            double[] x)
        {
            bool result = new bool();
            int n = 0;
            int nec = 0;
            int nic = 0;
            int i = 0;
            int j = 0;
            double v = 0;
            int i_ = 0;

            alglib.ap.assert(state.algostate==0, "SASStartOptimization: already in optimization mode");
            result = false;
            n = state.n;
            nec = state.nec;
            nic = state.nic;
            
            //
            // Enforce feasibility and calculate set of "candidate"/"active" constraints.
            // Always active equality constraints are marked as "active", all other constraints
            // are marked as "candidate".
            //
            apserv.ivectorsetlengthatleast(ref state.activeset, n+nec+nic);
            for(i=0; i<=n-1; i++)
            {
                if( state.hasbndl[i] && state.hasbndu[i] )
                {
                    if( (double)(state.bndl[i])>(double)(state.bndu[i]) )
                    {
                        return result;
                    }
                }
            }
            for(i_=0; i_<=n-1;i_++)
            {
                state.xc[i_] = x[i_];
            }
            if( state.nec+state.nic>0 )
            {
                
                //
                // General linear constraints are present; general code is used.
                //
                apserv.rvectorsetlengthatleast(ref state.tmp0, n);
                apserv.rvectorsetlengthatleast(ref state.tmpfeas, n+state.nic);
                apserv.rmatrixsetlengthatleast(ref state.tmpm0, state.nec+state.nic, n+state.nic+1);
                for(i=0; i<=state.nec+state.nic-1; i++)
                {
                    for(i_=0; i_<=n-1;i_++)
                    {
                        state.tmpm0[i,i_] = state.cleic[i,i_];
                    }
                    for(j=n; j<=n+state.nic-1; j++)
                    {
                        state.tmpm0[i,j] = 0;
                    }
                    if( i>=state.nec )
                    {
                        state.tmpm0[i,n+i-state.nec] = 1.0;
                    }
                    state.tmpm0[i,n+state.nic] = state.cleic[i,n];
                }
                for(i_=0; i_<=n-1;i_++)
                {
                    state.tmpfeas[i_] = state.xc[i_];
                }
                for(i=0; i<=state.nic-1; i++)
                {
                    v = 0.0;
                    for(i_=0; i_<=n-1;i_++)
                    {
                        v += state.cleic[i+state.nec,i_]*state.xc[i_];
                    }
                    state.tmpfeas[i+n] = Math.Max(state.cleic[i+state.nec,n]-v, 0.0);
                }
                if( !optserv.findfeasiblepoint(ref state.tmpfeas, state.bndl, state.hasbndl, state.bndu, state.hasbndu, n, state.nic, state.tmpm0, state.nec+state.nic, 1.0E-6, ref i, ref j) )
                {
                    return result;
                }
                for(i_=0; i_<=n-1;i_++)
                {
                    state.xc[i_] = state.tmpfeas[i_];
                }
                for(i=0; i<=n-1; i++)
                {
                    if( (state.hasbndl[i] && state.hasbndu[i]) && (double)(state.bndl[i])==(double)(state.bndu[i]) )
                    {
                        state.activeset[i] = 1;
                        continue;
                    }
                    if( (state.hasbndl[i] && (double)(state.xc[i])==(double)(state.bndl[i])) || (state.hasbndu[i] && (double)(state.xc[i])==(double)(state.bndu[i])) )
                    {
                        state.activeset[i] = 0;
                        continue;
                    }
                    state.activeset[i] = -1;
                }
                for(i=0; i<=state.nec-1; i++)
                {
                    state.activeset[n+i] = 1;
                }
                for(i=0; i<=state.nic-1; i++)
                {
                    if( (double)(state.tmpfeas[n+i])==(double)(0) )
                    {
                        state.activeset[n+state.nec+i] = 0;
                    }
                    else
                    {
                        state.activeset[n+state.nec+i] = -1;
                    }
                }
            }
            else
            {
                
                //
                // Only bound constraints are present, quick code can be used
                //
                for(i=0; i<=n-1; i++)
                {
                    state.activeset[i] = -1;
                    if( (state.hasbndl[i] && state.hasbndu[i]) && (double)(state.bndl[i])==(double)(state.bndu[i]) )
                    {
                        state.activeset[i] = 1;
                        state.xc[i] = state.bndl[i];
                        continue;
                    }
                    if( state.hasbndl[i] && (double)(state.xc[i])<=(double)(state.bndl[i]) )
                    {
                        state.xc[i] = state.bndl[i];
                        state.activeset[i] = 0;
                        continue;
                    }
                    if( state.hasbndu[i] && (double)(state.xc[i])>=(double)(state.bndu[i]) )
                    {
                        state.xc[i] = state.bndu[i];
                        state.activeset[i] = 0;
                        continue;
                    }
                }
            }
            
            //
            // Change state, allocate temporaries
            //
            result = true;
            state.algostate = 1;
            state.basisisready = false;
            state.hasxc = true;
            apserv.rmatrixsetlengthatleast(ref state.pbasis, Math.Min(nec+nic, n), n+1);
            apserv.rmatrixsetlengthatleast(ref state.ibasis, Math.Min(nec+nic, n), n+1);
            apserv.rmatrixsetlengthatleast(ref state.sbasis, Math.Min(nec+nic, n), n+1);
            return result;
        }


        /*************************************************************************
        This function explores search direction and calculates bound for  step  as
        well as information for activation of constraints.

        INPUT PARAMETERS:
            State       -   SAS structure which stores current point and all other
                            active set related information
            D           -   descent direction to explore

        OUTPUT PARAMETERS:
            StpMax      -   upper  limit  on  step  length imposed by yet inactive
                            constraints. Can be  zero  in  case  some  constraints
                            can be activated by zero step.  Equal  to  some  large
                            value in case step is unlimited.
            CIdx        -   -1 for unlimited step, in [0,N+NEC+NIC) in case of
                            limited step.
            VVal        -   value which is assigned to X[CIdx] during activation.
                            For CIdx<0 or CIdx>=N some dummy value is assigned to
                            this parameter.
        *************************************************************************/
        public static void sasexploredirection(sactiveset state,
            double[] d,
            ref double stpmax,
            ref int cidx,
            ref double vval)
        {
            int n = 0;
            int nec = 0;
            int nic = 0;
            int i = 0;
            double prevmax = 0;
            double vc = 0;
            double vd = 0;
            int i_ = 0;

            stpmax = 0;
            cidx = 0;
            vval = 0;

            alglib.ap.assert(state.algostate==1, "SASExploreDirection: is not in optimization mode");
            n = state.n;
            nec = state.nec;
            nic = state.nic;
            cidx = -1;
            vval = 0;
            stpmax = 1.0E50;
            for(i=0; i<=n-1; i++)
            {
                if( state.activeset[i]<=0 )
                {
                    alglib.ap.assert(!state.hasbndl[i] || (double)(state.xc[i])>=(double)(state.bndl[i]), "SASExploreDirection: internal error - infeasible X");
                    alglib.ap.assert(!state.hasbndu[i] || (double)(state.xc[i])<=(double)(state.bndu[i]), "SASExploreDirection: internal error - infeasible X");
                    if( state.hasbndl[i] && (double)(d[i])<(double)(0) )
                    {
                        prevmax = stpmax;
                        stpmax = apserv.safeminposrv(state.xc[i]-state.bndl[i], -d[i], stpmax);
                        if( (double)(stpmax)<(double)(prevmax) )
                        {
                            cidx = i;
                            vval = state.bndl[i];
                        }
                    }
                    if( state.hasbndu[i] && (double)(d[i])>(double)(0) )
                    {
                        prevmax = stpmax;
                        stpmax = apserv.safeminposrv(state.bndu[i]-state.xc[i], d[i], stpmax);
                        if( (double)(stpmax)<(double)(prevmax) )
                        {
                            cidx = i;
                            vval = state.bndu[i];
                        }
                    }
                }
            }
            for(i=nec; i<=nec+nic-1; i++)
            {
                if( state.activeset[n+i]<=0 )
                {
                    vc = 0.0;
                    for(i_=0; i_<=n-1;i_++)
                    {
                        vc += state.cleic[i,i_]*state.xc[i_];
                    }
                    vc = vc-state.cleic[i,n];
                    vd = 0.0;
                    for(i_=0; i_<=n-1;i_++)
                    {
                        vd += state.cleic[i,i_]*d[i_];
                    }
                    if( (double)(vd)<=(double)(0) )
                    {
                        continue;
                    }
                    if( (double)(vc)<(double)(0) )
                    {
                        
                        //
                        // XC is strictly feasible with respect to I-th constraint,
                        // we can perform non-zero step because there is non-zero distance
                        // between XC and bound.
                        //
                        prevmax = stpmax;
                        stpmax = apserv.safeminposrv(-vc, vd, stpmax);
                        if( (double)(stpmax)<(double)(prevmax) )
                        {
                            cidx = n+i;
                        }
                    }
                    else
                    {
                        
                        //
                        // XC is at the boundary (or slightly beyond it), and step vector
                        // points beyond the boundary.
                        //
                        // The only thing we can do is to perform zero step and activate
                        // I-th constraint.
                        //
                        stpmax = 0;
                        cidx = n+i;
                    }
                }
            }
        }


        /*************************************************************************
        This subroutine moves current point to XN,  in  the  direction  previously
        explored with SASExploreDirection() function.

        Step may activate one constraint. It is assumed than XN  is  approximately
        feasible (small error as  large  as several  ulps  is  possible).   Strict
        feasibility  with  respect  to  bound  constraints  is  enforced    during
        activation, feasibility with respect to general linear constraints is  not
        enforced.

        INPUT PARAMETERS:
            S       -   active set object
            XN      -   new point.
            NeedAct -   True in case one constraint needs activation
            CIdx    -   index of constraint, in [0,N+NEC+NIC).
                        Ignored if NeedAct is false.
                        This value is calculated by SASExploreDirection().
            CVal    -   for CIdx in [0,N) this field stores value which is
                        assigned to XC[CIdx] during activation. CVal is ignored in
                        other cases.
                        This value is calculated by SASExploreDirection().
            
        OUTPUT PARAMETERS:
            S       -   current point and list of active constraints are changed.

        RESULT:
            >0, in case at least one inactive non-candidate constraint was activated
            =0, in case only "candidate" constraints were activated
            <0, in case no constraints were activated by the step

        NOTE: in general case State.XC<>XN because activation of  constraints  may
              slightly change current point (to enforce feasibility).

          -- ALGLIB --
             Copyright 21.12.2012 by Bochkanov Sergey
        *************************************************************************/
        public static int sasmoveto(sactiveset state,
            double[] xn,
            bool needact,
            int cidx,
            double cval)
        {
            int result = 0;
            int n = 0;
            int nec = 0;
            int nic = 0;
            int i = 0;
            bool wasactivation = new bool();

            alglib.ap.assert(state.algostate==1, "SASMoveTo: is not in optimization mode");
            n = state.n;
            nec = state.nec;
            nic = state.nic;
            
            //
            // Save previous state, update current point
            //
            apserv.rvectorsetlengthatleast(ref state.mtx, n);
            apserv.ivectorsetlengthatleast(ref state.mtas, n+nec+nic);
            for(i=0; i<=n-1; i++)
            {
                state.mtx[i] = state.xc[i];
                state.xc[i] = xn[i];
            }
            for(i=0; i<=n+nec+nic-1; i++)
            {
                state.mtas[i] = state.activeset[i];
            }
            
            //
            // Activate constraints
            //
            wasactivation = false;
            if( needact )
            {
                
                //
                // Activation
                //
                alglib.ap.assert(cidx>=0 && cidx<n+nec+nic, "SASMoveTo: incorrect CIdx");
                if( cidx<n )
                {
                    
                    //
                    // CIdx in [0,N-1] means that bound constraint was activated.
                    // We activate it explicitly to avoid situation when roundoff-error
                    // prevents us from moving EXACTLY to x=CVal.
                    //
                    state.xc[cidx] = cval;
                }
                state.activeset[cidx] = 1;
                wasactivation = true;
            }
            for(i=0; i<=n-1; i++)
            {
                
                //
                // Post-check (some constraints may be activated because of numerical errors)
                //
                if( state.hasbndl[i] && (double)(state.xc[i])<(double)(state.bndl[i]) )
                {
                    state.xc[i] = state.bndl[i];
                    state.activeset[i] = 1;
                    wasactivation = true;
                }
                if( state.hasbndu[i] && (double)(state.xc[i])>(double)(state.bndu[i]) )
                {
                    state.xc[i] = state.bndu[i];
                    state.activeset[i] = 1;
                    wasactivation = true;
                }
            }
            
            //
            // Determine return status:
            // * -1 in case no constraints were activated
            // *  0 in case only "candidate" constraints were activated
            // * +1 in case at least one "non-candidate" constraint was activated
            //
            if( wasactivation )
            {
                
                //
                // Step activated one/several constraints, but sometimes it is spurious
                // activation - RecalculateConstraints() tells us that constraint is
                // inactive (negative Largrange multiplier), but step activates it
                // because of numerical noise.
                //
                // This block of code checks whether step activated truly new constraints
                // (ones which were not in the active set at the solution):
                //
                // * for non-boundary constraint it is enough to check that previous value
                //   of ActiveSet[i] is negative (=far from boundary), and new one is
                //   positive (=we are at the boundary, constraint is activated).
                //
                // * for boundary constraints previous criterion won't work. Each variable
                //   has two constraints, and simply checking their status is not enough -
                //   we have to correctly identify cases when we leave one boundary
                //   (PrevActiveSet[i]=0) and move to another boundary (ActiveSet[i]>0).
                //   Such cases can be identified if we compare previous X with new X.
                //
                // In case only "candidate" constraints were activated, result variable
                // is set to 0. In case at least one new constraint was activated, result
                // is set to 1.
                //
                result = 0;
                for(i=0; i<=n-1; i++)
                {
                    if( state.activeset[i]>0 && (double)(state.xc[i])!=(double)(state.mtx[i]) )
                    {
                        result = 1;
                    }
                }
                for(i=n; i<=n+state.nec+state.nic-1; i++)
                {
                    if( state.mtas[i]<0 && state.activeset[i]>0 )
                    {
                        result = 1;
                    }
                }
            }
            else
            {
                
                //
                // No activation, return -1
                //
                result = -1;
            }
            
            //
            // Invalidate basis
            //
            state.basisisready = false;
            return result;
        }


        /*************************************************************************
        This subroutine performs immediate activation of one constraint:
        * "immediate" means that we do not have to move to activate it
        * in case boundary constraint is activated, we enforce current point to be
          exactly at the boundary

        INPUT PARAMETERS:
            S       -   active set object
            CIdx    -   index of constraint, in [0,N+NEC+NIC).
                        This value is calculated by SASExploreDirection().
            CVal    -   for CIdx in [0,N) this field stores value which is
                        assigned to XC[CIdx] during activation. CVal is ignored in
                        other cases.
                        This value is calculated by SASExploreDirection().
            
        OUTPUT PARAMETERS:
            S       -   current point and list of active constraints are changed.

          -- ALGLIB --
             Copyright 21.12.2012 by Bochkanov Sergey
        *************************************************************************/
        public static void sasimmediateactivation(sactiveset state,
            int cidx,
            double cval)
        {
            alglib.ap.assert(state.algostate==1, "SASMoveTo: is not in optimization mode");
            if( cidx<state.n )
            {
                state.xc[cidx] = cval;
            }
            state.activeset[cidx] = 1;
            state.basisisready = false;
        }


        /*************************************************************************
        This subroutine calculates descent direction subject to current active set.

        INPUT PARAMETERS:
            S       -   active set object
            G       -   array[N], gradient
            D       -   possibly prealocated buffer;
                        automatically resized if needed.
            
        OUTPUT PARAMETERS:
            D       -   descent direction projected onto current active set.
                        Components of D which correspond to active boundary
                        constraints are forced to be exactly zero.
                        In case D is non-zero, it is normalized to have unit norm.
                        
        NOTE: in  case active set has N  active  constraints  (or  more),  descent
              direction is forced to be exactly zero.

          -- ALGLIB --
             Copyright 21.12.2012 by Bochkanov Sergey
        *************************************************************************/
        public static void sasconstraineddescent(sactiveset state,
            double[] g,
            ref double[] d)
        {
            alglib.ap.assert(state.algostate==1, "SASConstrainedDescent: is not in optimization mode");
            sasrebuildbasis(state);
            constraineddescent(state, g, state.unitdiagonal, state.ibasis, true, ref d);
        }


        /*************************************************************************
        This  subroutine  calculates  preconditioned  descent direction subject to
        current active set.

        INPUT PARAMETERS:
            S       -   active set object
            G       -   array[N], gradient
            D       -   possibly prealocated buffer;
                        automatically resized if needed.
            
        OUTPUT PARAMETERS:
            D       -   descent direction projected onto current active set.
                        Components of D which correspond to active boundary
                        constraints are forced to be exactly zero.
                        In case D is non-zero, it is normalized to have unit norm.
                        
        NOTE: in  case active set has N  active  constraints  (or  more),  descent
              direction is forced to be exactly zero.

          -- ALGLIB --
             Copyright 21.12.2012 by Bochkanov Sergey
        *************************************************************************/
        public static void sasconstraineddescentprec(sactiveset state,
            double[] g,
            ref double[] d)
        {
            alglib.ap.assert(state.algostate==1, "SASConstrainedDescentPrec: is not in optimization mode");
            sasrebuildbasis(state);
            constraineddescent(state, g, state.h, state.pbasis, true, ref d);
        }


        /*************************************************************************
        This subroutine calculates product of direction vector and  preconditioner
        multiplied subject to current active set.

        INPUT PARAMETERS:
            S       -   active set object
            D       -   array[N], direction
            
        OUTPUT PARAMETERS:
            D       -   preconditioned direction projected onto current active set.
                        Components of D which correspond to active boundary
                        constraints are forced to be exactly zero.
                        
        NOTE: in  case active set has N  active  constraints  (or  more),  descent
              direction is forced to be exactly zero.

          -- ALGLIB --
             Copyright 21.12.2012 by Bochkanov Sergey
        *************************************************************************/
        public static void sasconstraineddirection(sactiveset state,
            ref double[] d)
        {
            int i = 0;

            alglib.ap.assert(state.algostate==1, "SASConstrainedAntigradientPrec: is not in optimization mode");
            sasrebuildbasis(state);
            constraineddescent(state, d, state.unitdiagonal, state.ibasis, false, ref state.cdtmp);
            for(i=0; i<=state.n-1; i++)
            {
                d[i] = -state.cdtmp[i];
            }
        }


        /*************************************************************************
        This subroutine calculates product of direction vector and  preconditioner
        multiplied subject to current active set.

        INPUT PARAMETERS:
            S       -   active set object
            D       -   array[N], direction
            
        OUTPUT PARAMETERS:
            D       -   preconditioned direction projected onto current active set.
                        Components of D which correspond to active boundary
                        constraints are forced to be exactly zero.
                        
        NOTE: in  case active set has N  active  constraints  (or  more),  descent
              direction is forced to be exactly zero.

          -- ALGLIB --
             Copyright 21.12.2012 by Bochkanov Sergey
        *************************************************************************/
        public static void sasconstraineddirectionprec(sactiveset state,
            ref double[] d)
        {
            int i = 0;

            alglib.ap.assert(state.algostate==1, "SASConstrainedAntigradientPrec: is not in optimization mode");
            sasrebuildbasis(state);
            constraineddescent(state, d, state.h, state.pbasis, false, ref state.cdtmp);
            for(i=0; i<=state.n-1; i++)
            {
                d[i] = -state.cdtmp[i];
            }
        }


        /*************************************************************************
        This  subroutine  performs  correction of some (possibly infeasible) point
        with respect to a) current active set, b) all boundary  constraints,  both
        active and inactive:

        1) first, it performs projection (orthogonal with respect to scale  matrix
           S) of X into current active set: X -> X1.
           P1 is set to scaled norm of X-X1.
        2) next, we perform projection with respect to  ALL  boundary  constraints
           which are violated at X1: X1 -> X2.
           P2 is set to scaled norm of X2-X1.
        3) X is replaced by X2, P1+P2 are returned in "Penalty" parameter.

        The idea is that this function can preserve and enforce feasibility during
        optimization, and additional penalty parameter can be used to prevent algo
        from leaving feasible set because of rounding errors.

        INPUT PARAMETERS:
            S       -   active set object
            X       -   array[N], candidate point
            
        OUTPUT PARAMETERS:
            X       -   "improved" candidate point:
                        a) feasible with respect to all boundary constraints
                        b) feasibility with respect to active set is retained at
                           good level.
            Penalty -   penalty term, which can be added to function value if user
                        wants to penalize violation of constraints (recommended).
                        
        NOTE: this function is not intended to find exact  projection  (i.e.  best
              approximation) of X into feasible set. It just improves situation  a
              bit.
              Regular  use  of   this function will help you to retain feasibility
              - if you already have something to start  with  and  constrain  your
              steps is such way that the only source of infeasibility are roundoff
              errors.

          -- ALGLIB --
             Copyright 21.12.2012 by Bochkanov Sergey
        *************************************************************************/
        public static void sascorrection(sactiveset state,
            ref double[] x,
            ref double penalty)
        {
            int i = 0;
            int j = 0;
            int n = 0;
            double v = 0;
            double p1 = 0;
            double p2 = 0;
            int i_ = 0;

            penalty = 0;

            alglib.ap.assert(state.algostate==1, "SASCorrection: is not in optimization mode");
            sasrebuildbasis(state);
            n = state.n;
            apserv.rvectorsetlengthatleast(ref state.corrtmp, n);
            
            //
            // Perform projection 1.
            //
            // This projecton is given by:
            //
            //     x_proj = x - S*S*As'*(As*x-b)
            //
            // where x is original x before projection, S is a scale matrix,
            // As is a matrix of equality constraints (active set) which were
            // orthogonalized with respect to inner product given by S (i.e. we
            // have As*S*S'*As'=I), b is a right part of the orthogonalized
            // constraints.
            //
            // NOTE: you can verify that x_proj is strictly feasible w.r.t.
            //       active set by multiplying it by As - you will get
            //       As*x_proj = As*x - As*x + b = b.
            //
            //       This formula for projection can be obtained by solving
            //       following minimization problem.
            //
            //           min ||inv(S)*(x_proj-x)||^2 s.t. As*x_proj=b
            //       
            //
            for(i_=0; i_<=n-1;i_++)
            {
                state.corrtmp[i_] = x[i_];
            }
            for(i=0; i<=state.basissize-1; i++)
            {
                v = -state.sbasis[i,n];
                for(j=0; j<=n-1; j++)
                {
                    v = v+state.sbasis[i,j]*state.corrtmp[j];
                }
                for(j=0; j<=n-1; j++)
                {
                    state.corrtmp[j] = state.corrtmp[j]-v*state.sbasis[i,j]*math.sqr(state.s[j]);
                }
            }
            for(i=0; i<=n-1; i++)
            {
                if( state.activeset[i]>0 )
                {
                    state.corrtmp[i] = state.xc[i];
                }
            }
            p1 = 0;
            for(i=0; i<=n-1; i++)
            {
                p1 = p1+math.sqr((state.corrtmp[i]-x[i])/state.s[i]);
            }
            
            //
            // Perform projection 2
            //
            p2 = 0;
            for(i=0; i<=n-1; i++)
            {
                x[i] = state.corrtmp[i];
                if( state.hasbndl[i] && (double)(x[i])<(double)(state.bndl[i]) )
                {
                    x[i] = state.bndl[i];
                }
                if( state.hasbndu[i] && (double)(x[i])>(double)(state.bndu[i]) )
                {
                    x[i] = state.bndu[i];
                }
                p2 = p2+math.sqr((state.corrtmp[i]-x[i])/state.s[i]);
            }
            penalty = p1+p2;
        }


        /*************************************************************************
        This subroutine calculates scaled norm of  vector  after  projection  onto
        subspace of active constraints. Most often this function is used  to  test
        stopping conditions.

        INPUT PARAMETERS:
            S       -   active set object
            D       -   vector whose norm is calculated
            
        RESULT:
            Vector norm (after projection and scaling)
            
        NOTE: projection is performed first, scaling is performed after projection

          -- ALGLIB --
             Copyright 21.12.2012 by Bochkanov Sergey
        *************************************************************************/
        public static double sasscaledconstrainednorm(sactiveset state,
            double[] d)
        {
            double result = 0;
            int i = 0;
            int n = 0;
            double v = 0;
            int i_ = 0;

            alglib.ap.assert(state.algostate==1, "SASMoveTo: is not in optimization mode");
            n = state.n;
            apserv.rvectorsetlengthatleast(ref state.scntmp, n);
            
            //
            // Prepare basis (if needed)
            //
            sasrebuildbasis(state);
            
            //
            // Calculate descent direction
            //
            for(i=0; i<=n-1; i++)
            {
                if( state.activeset[i]>0 )
                {
                    state.scntmp[i] = 0;
                }
                else
                {
                    state.scntmp[i] = d[i];
                }
            }
            for(i=0; i<=state.basissize-1; i++)
            {
                v = 0.0;
                for(i_=0; i_<=n-1;i_++)
                {
                    v += state.ibasis[i,i_]*state.scntmp[i_];
                }
                for(i_=0; i_<=n-1;i_++)
                {
                    state.scntmp[i_] = state.scntmp[i_] - v*state.ibasis[i,i_];
                }
            }
            v = 0.0;
            for(i=0; i<=n-1; i++)
            {
                v = v+math.sqr(state.s[i]*state.scntmp[i]);
            }
            result = Math.Sqrt(v);
            return result;
        }


        /*************************************************************************
        This subroutine turns off optimization mode.

        INPUT PARAMETERS:
            S   -   active set object
            
        OUTPUT PARAMETERS:
            S   -   state is changed

          -- ALGLIB --
             Copyright 21.12.2012 by Bochkanov Sergey
        *************************************************************************/
        public static void sasstopoptimization(sactiveset state)
        {
            alglib.ap.assert(state.algostate==1, "SASStopOptimization: already stopped");
            state.algostate = 0;
        }


        /*************************************************************************
        This function recalculates constraints - activates  and  deactivates  them
        according to gradient value at current point. Algorithm  assumes  that  we
        want to make steepest descent step from  current  point;  constraints  are
        activated and deactivated in such way that we won't violate any constraint
        by steepest descent step.

        After call to this function active set is ready to  try  steepest  descent
        step (SASDescentDirection-SASExploreDirection-SASMoveTo).

        Only already "active" and "candidate" elements of ActiveSet are  examined;
        constraints which are not active are not examined.

        INPUT PARAMETERS:
            State       -   active set object
            GC          -   array[N], gradient at XC
            
        OUTPUT PARAMETERS:
            State       -   active set object, with new set of constraint

          -- ALGLIB --
             Copyright 26.09.2012 by Bochkanov Sergey
        *************************************************************************/
        public static void sasreactivateconstraints(sactiveset state,
            double[] gc)
        {
            alglib.ap.assert(state.algostate==1, "SASReactivateConstraints: must be in optimization mode");
            reactivateconstraints(state, gc, state.unitdiagonal);
        }


        /*************************************************************************
        This function recalculates constraints - activates  and  deactivates  them
        according to gradient value at current point.

        Algorithm  assumes  that  we  want  to make Quasi-Newton step from current
        point with diagonal Quasi-Newton matrix H. Constraints are  activated  and
        deactivated in such way that we won't violate any constraint by step.

        After call to  this  function  active set is ready to  try  preconditioned
        steepest descent step (SASDescentDirection-SASExploreDirection-SASMoveTo).

        Only already "active" and "candidate" elements of ActiveSet are  examined;
        constraints which are not active are not examined.

        INPUT PARAMETERS:
            State       -   active set object
            GC          -   array[N], gradient at XC
            
        OUTPUT PARAMETERS:
            State       -   active set object, with new set of constraint

          -- ALGLIB --
             Copyright 26.09.2012 by Bochkanov Sergey
        *************************************************************************/
        public static void sasreactivateconstraintsprec(sactiveset state,
            double[] gc)
        {
            alglib.ap.assert(state.algostate==1, "SASReactivateConstraintsPrec: must be in optimization mode");
            reactivateconstraints(state, gc, state.h);
        }


        /*************************************************************************
        This function builds three orthonormal basises for current active set:
        * P-orthogonal one, which is orthogonalized with inner product
          (x,y) = x'*P*y, where P=inv(H) is current preconditioner
        * S-orthogonal one, which is orthogonalized with inner product
          (x,y) = x'*S'*S*y, where S is diagonal scaling matrix
        * I-orthogonal one, which is orthogonalized with standard dot product

        NOTE: all sets of orthogonal vectors are guaranteed  to  have  same  size.
              P-orthogonal basis is built first, I/S-orthogonal basises are forced
              to have same number of vectors as P-orthogonal one (padded  by  zero
              vectors if needed).
              
        NOTE: this function tracks changes in active set; first call  will  result
              in reorthogonalization

        INPUT PARAMETERS:
            State   -   active set object
            H       -   diagonal preconditioner, H[i]>0

        OUTPUT PARAMETERS:
            State   -   active set object with new basis
            
          -- ALGLIB --
             Copyright 20.06.2012 by Bochkanov Sergey
        *************************************************************************/
        public static void sasrebuildbasis(sactiveset state)
        {
            int n = 0;
            int nec = 0;
            int nic = 0;
            int i = 0;
            int j = 0;
            int t = 0;
            int nactivelin = 0;
            int nactivebnd = 0;
            double v = 0;
            double vmax = 0;
            int kmax = 0;
            int i_ = 0;

            if( state.basisisready )
            {
                return;
            }
            n = state.n;
            nec = state.nec;
            nic = state.nic;
            apserv.rmatrixsetlengthatleast(ref state.tmpbasis, nec+nic, n+1);
            state.basissize = 0;
            state.basisisready = true;
            
            //
            // Determine number of active boundary and non-boundary
            // constraints, move them to TmpBasis. Quick exit if no
            // non-boundary constraints were detected.
            //
            nactivelin = 0;
            nactivebnd = 0;
            for(i=0; i<=nec+nic-1; i++)
            {
                if( state.activeset[n+i]>0 )
                {
                    nactivelin = nactivelin+1;
                }
            }
            for(j=0; j<=n-1; j++)
            {
                if( state.activeset[j]>0 )
                {
                    nactivebnd = nactivebnd+1;
                }
            }
            if( nactivelin==0 )
            {
                return;
            }
            
            //
            // Orthogonalize linear constraints (inner product is given by preconditioner)
            // with respect to each other and boundary ones:
            // * normalize all constraints
            // * orthogonalize with respect to boundary ones
            // * repeat:
            //   * if basisSize+nactivebnd=n - TERMINATE
            //   * choose largest row from TmpBasis
            //   * if row norm is too small  - TERMINATE
            //   * add row to basis, normalize
            //   * remove from TmpBasis, orthogonalize other constraints with respect to this one
            //
            nactivelin = 0;
            for(i=0; i<=nec+nic-1; i++)
            {
                if( state.activeset[n+i]>0 )
                {
                    for(i_=0; i_<=n;i_++)
                    {
                        state.tmpbasis[nactivelin,i_] = state.cleic[i,i_];
                    }
                    nactivelin = nactivelin+1;
                }
            }
            for(i=0; i<=nactivelin-1; i++)
            {
                v = 0.0;
                for(j=0; j<=n-1; j++)
                {
                    v = v+math.sqr(state.tmpbasis[i,j])/state.h[j];
                }
                if( (double)(v)>(double)(0) )
                {
                    v = 1/Math.Sqrt(v);
                    for(j=0; j<=n; j++)
                    {
                        state.tmpbasis[i,j] = state.tmpbasis[i,j]*v;
                    }
                }
            }
            for(j=0; j<=n-1; j++)
            {
                if( state.activeset[j]>0 )
                {
                    for(i=0; i<=nactivelin-1; i++)
                    {
                        state.tmpbasis[i,n] = state.tmpbasis[i,n]-state.tmpbasis[i,j]*state.xc[j];
                        state.tmpbasis[i,j] = 0.0;
                    }
                }
            }
            while( state.basissize+nactivebnd<n )
            {
                
                //
                // Find largest vector, add to basis
                //
                vmax = -1;
                kmax = -1;
                for(i=0; i<=nactivelin-1; i++)
                {
                    v = 0.0;
                    for(j=0; j<=n-1; j++)
                    {
                        v = v+math.sqr(state.tmpbasis[i,j])/state.h[j];
                    }
                    v = Math.Sqrt(v);
                    if( (double)(v)>(double)(vmax) )
                    {
                        vmax = v;
                        kmax = i;
                    }
                }
                if( (double)(vmax)<(double)(1.0E4*math.machineepsilon) )
                {
                    break;
                }
                v = 1/vmax;
                for(i_=0; i_<=n;i_++)
                {
                    state.pbasis[state.basissize,i_] = v*state.tmpbasis[kmax,i_];
                }
                state.basissize = state.basissize+1;
                
                //
                // Reorthogonalize other vectors with respect to chosen one.
                // Remove it from the array.
                //
                for(i=0; i<=nactivelin-1; i++)
                {
                    if( i!=kmax )
                    {
                        v = 0;
                        for(j=0; j<=n-1; j++)
                        {
                            v = v+state.pbasis[state.basissize-1,j]*state.tmpbasis[i,j]/state.h[j];
                        }
                        for(i_=0; i_<=n;i_++)
                        {
                            state.tmpbasis[i,i_] = state.tmpbasis[i,i_] - v*state.pbasis[state.basissize-1,i_];
                        }
                    }
                }
                for(j=0; j<=n; j++)
                {
                    state.tmpbasis[kmax,j] = 0;
                }
            }
            
            //
            // Orthogonalize linear constraints using traditional dot product
            // with respect to each other and boundary ones.
            //
            // NOTE: we force basis size to be equal to one which was computed
            //       at the previous step, with preconditioner-based inner product.
            //
            nactivelin = 0;
            for(i=0; i<=nec+nic-1; i++)
            {
                if( state.activeset[n+i]>0 )
                {
                    for(i_=0; i_<=n;i_++)
                    {
                        state.tmpbasis[nactivelin,i_] = state.cleic[i,i_];
                    }
                    nactivelin = nactivelin+1;
                }
            }
            for(i=0; i<=nactivelin-1; i++)
            {
                v = 0.0;
                for(j=0; j<=n-1; j++)
                {
                    v = v+math.sqr(state.tmpbasis[i,j]);
                }
                if( (double)(v)>(double)(0) )
                {
                    v = 1/Math.Sqrt(v);
                    for(j=0; j<=n; j++)
                    {
                        state.tmpbasis[i,j] = state.tmpbasis[i,j]*v;
                    }
                }
            }
            for(j=0; j<=n-1; j++)
            {
                if( state.activeset[j]>0 )
                {
                    for(i=0; i<=nactivelin-1; i++)
                    {
                        state.tmpbasis[i,n] = state.tmpbasis[i,n]-state.tmpbasis[i,j]*state.xc[j];
                        state.tmpbasis[i,j] = 0.0;
                    }
                }
            }
            for(t=0; t<=state.basissize-1; t++)
            {
                
                //
                // Find largest vector, add to basis.
                //
                vmax = -1;
                kmax = -1;
                for(i=0; i<=nactivelin-1; i++)
                {
                    v = 0.0;
                    for(j=0; j<=n-1; j++)
                    {
                        v = v+math.sqr(state.tmpbasis[i,j]);
                    }
                    v = Math.Sqrt(v);
                    if( (double)(v)>(double)(vmax) )
                    {
                        vmax = v;
                        kmax = i;
                    }
                }
                if( (double)(vmax)==(double)(0) )
                {
                    for(j=0; j<=n; j++)
                    {
                        state.ibasis[t,j] = 0.0;
                    }
                    continue;
                }
                v = 1/vmax;
                for(i_=0; i_<=n;i_++)
                {
                    state.ibasis[t,i_] = v*state.tmpbasis[kmax,i_];
                }
                
                //
                // Reorthogonalize other vectors with respect to chosen one.
                // Remove it from the array.
                //
                for(i=0; i<=nactivelin-1; i++)
                {
                    if( i!=kmax )
                    {
                        v = 0;
                        for(j=0; j<=n-1; j++)
                        {
                            v = v+state.ibasis[t,j]*state.tmpbasis[i,j];
                        }
                        for(i_=0; i_<=n;i_++)
                        {
                            state.tmpbasis[i,i_] = state.tmpbasis[i,i_] - v*state.ibasis[t,i_];
                        }
                    }
                }
                for(j=0; j<=n; j++)
                {
                    state.tmpbasis[kmax,j] = 0;
                }
            }
            
            //
            // Orthogonalize linear constraints using inner product given by
            // scale matrix.
            //
            // NOTE: we force basis size to be equal to one which was computed
            //       with preconditioner-based inner product.
            //
            nactivelin = 0;
            for(i=0; i<=nec+nic-1; i++)
            {
                if( state.activeset[n+i]>0 )
                {
                    for(i_=0; i_<=n;i_++)
                    {
                        state.tmpbasis[nactivelin,i_] = state.cleic[i,i_];
                    }
                    nactivelin = nactivelin+1;
                }
            }
            for(i=0; i<=nactivelin-1; i++)
            {
                v = 0.0;
                for(j=0; j<=n-1; j++)
                {
                    v = v+math.sqr(state.tmpbasis[i,j]*state.s[j]);
                }
                if( (double)(v)>(double)(0) )
                {
                    v = 1/Math.Sqrt(v);
                    for(j=0; j<=n; j++)
                    {
                        state.tmpbasis[i,j] = state.tmpbasis[i,j]*v;
                    }
                }
            }
            for(j=0; j<=n-1; j++)
            {
                if( state.activeset[j]>0 )
                {
                    for(i=0; i<=nactivelin-1; i++)
                    {
                        state.tmpbasis[i,n] = state.tmpbasis[i,n]-state.tmpbasis[i,j]*state.xc[j];
                        state.tmpbasis[i,j] = 0.0;
                    }
                }
            }
            for(t=0; t<=state.basissize-1; t++)
            {
                
                //
                // Find largest vector, add to basis.
                //
                vmax = -1;
                kmax = -1;
                for(i=0; i<=nactivelin-1; i++)
                {
                    v = 0.0;
                    for(j=0; j<=n-1; j++)
                    {
                        v = v+math.sqr(state.tmpbasis[i,j]*state.s[j]);
                    }
                    v = Math.Sqrt(v);
                    if( (double)(v)>(double)(vmax) )
                    {
                        vmax = v;
                        kmax = i;
                    }
                }
                if( (double)(vmax)==(double)(0) )
                {
                    for(j=0; j<=n; j++)
                    {
                        state.sbasis[t,j] = 0.0;
                    }
                    continue;
                }
                v = 1/vmax;
                for(i_=0; i_<=n;i_++)
                {
                    state.sbasis[t,i_] = v*state.tmpbasis[kmax,i_];
                }
                
                //
                // Reorthogonalize other vectors with respect to chosen one.
                // Remove it from the array.
                //
                for(i=0; i<=nactivelin-1; i++)
                {
                    if( i!=kmax )
                    {
                        v = 0;
                        for(j=0; j<=n-1; j++)
                        {
                            v = v+state.sbasis[t,j]*state.tmpbasis[i,j]*math.sqr(state.s[j]);
                        }
                        for(i_=0; i_<=n;i_++)
                        {
                            state.tmpbasis[i,i_] = state.tmpbasis[i,i_] - v*state.sbasis[t,i_];
                        }
                    }
                }
                for(j=0; j<=n; j++)
                {
                    state.tmpbasis[kmax,j] = 0;
                }
            }
        }


        /*************************************************************************
        This  subroutine  calculates  preconditioned  descent direction subject to
        current active set.

        INPUT PARAMETERS:
            State   -   active set object
            G       -   array[N], gradient
            H       -   array[N], Hessian matrix
            HA      -   active constraints orthogonalized in such way
                        that HA*inv(H)*HA'= I.
            Normalize-  whether we need normalized descent or not
            D       -   possibly preallocated buffer; automatically resized.
            
        OUTPUT PARAMETERS:
            D       -   descent direction projected onto current active set.
                        Components of D which correspond to active boundary
                        constraints are forced to be exactly zero.
                        In case D is non-zero and Normalize is True, it is
                        normalized to have unit norm.

          -- ALGLIB --
             Copyright 21.12.2012 by Bochkanov Sergey
        *************************************************************************/
        private static void constraineddescent(sactiveset state,
            double[] g,
            double[] h,
            double[,] ha,
            bool normalize,
            ref double[] d)
        {
            int i = 0;
            int j = 0;
            int n = 0;
            double v = 0;
            int nactive = 0;
            int i_ = 0;

            alglib.ap.assert(state.algostate==1, "SAS: internal error in ConstrainedDescent() - not in optimization mode");
            alglib.ap.assert(state.basisisready, "SAS: internal error in ConstrainedDescent() - no basis");
            n = state.n;
            apserv.rvectorsetlengthatleast(ref d, n);
            
            //
            // Calculate preconditioned constrained descent direction:
            //
            //     d := -inv(H)*( g - HA'*(HA*inv(H)*g) )
            //
            // Formula above always gives direction which is orthogonal to rows of HA.
            // You can verify it by multiplication of both sides by HA[i] (I-th row),
            // taking into account that HA*inv(H)*HA'= I (by definition of HA - it is
            // orthogonal basis with inner product given by inv(H)).
            //
            nactive = 0;
            for(i=0; i<=n-1; i++)
            {
                if( state.activeset[i]>0 )
                {
                    d[i] = 0;
                    nactive = nactive+1;
                }
                else
                {
                    d[i] = g[i];
                }
            }
            for(i=0; i<=state.basissize-1; i++)
            {
                v = 0.0;
                for(j=0; j<=n-1; j++)
                {
                    v = v+ha[i,j]*d[j]/h[j];
                }
                for(i_=0; i_<=n-1;i_++)
                {
                    d[i_] = d[i_] - v*ha[i,i_];
                }
                nactive = nactive+1;
            }
            v = 0.0;
            for(i=0; i<=n-1; i++)
            {
                if( state.activeset[i]>0 )
                {
                    d[i] = 0;
                }
                else
                {
                    d[i] = -(d[i]/h[i]);
                    v = v+math.sqr(d[i]);
                }
            }
            v = Math.Sqrt(v);
            if( nactive>=n )
            {
                v = 0;
                for(i=0; i<=n-1; i++)
                {
                    d[i] = 0;
                }
            }
            if( normalize && (double)(v)>(double)(0) )
            {
                for(i=0; i<=n-1; i++)
                {
                    d[i] = d[i]/v;
                }
            }
        }


        /*************************************************************************
        This function recalculates constraints - activates  and  deactivates  them
        according to gradient value at current point.

        Algorithm  assumes  that  we  want  to make Quasi-Newton step from current
        point with diagonal Quasi-Newton matrix H. Constraints are  activated  and
        deactivated in such way that we won't violate any constraint by step.

        Only already "active" and "candidate" elements of ActiveSet are  examined;
        constraints which are not active are not examined.

        INPUT PARAMETERS:
            State       -   active set object
            GC          -   array[N], gradient at XC
            H           -   array[N], Hessian matrix
            
        OUTPUT PARAMETERS:
            State       -   active set object, with new set of constraint

          -- ALGLIB --
             Copyright 26.09.2012 by Bochkanov Sergey
        *************************************************************************/
        private static void reactivateconstraints(sactiveset state,
            double[] gc,
            double[] h)
        {
            int n = 0;
            int nec = 0;
            int nic = 0;
            int i = 0;
            int j = 0;
            int idx0 = 0;
            int idx1 = 0;
            double v = 0;
            int nactivebnd = 0;
            int nactivelin = 0;
            int nactiveconstraints = 0;
            double rowscale = 0;
            int i_ = 0;

            alglib.ap.assert(state.algostate==1, "SASReactivateConstraintsPrec: must be in optimization mode");
            
            //
            // Prepare
            //
            n = state.n;
            nec = state.nec;
            nic = state.nic;
            state.basisisready = false;
            
            //
            // Handle important special case - no linear constraints,
            // only boundary constraints are present
            //
            if( nec+nic==0 )
            {
                for(i=0; i<=n-1; i++)
                {
                    if( (state.hasbndl[i] && state.hasbndu[i]) && (double)(state.bndl[i])==(double)(state.bndu[i]) )
                    {
                        state.activeset[i] = 1;
                        continue;
                    }
                    if( (state.hasbndl[i] && (double)(state.xc[i])==(double)(state.bndl[i])) && (double)(gc[i])>=(double)(0) )
                    {
                        state.activeset[i] = 1;
                        continue;
                    }
                    if( (state.hasbndu[i] && (double)(state.xc[i])==(double)(state.bndu[i])) && (double)(gc[i])<=(double)(0) )
                    {
                        state.activeset[i] = 1;
                        continue;
                    }
                    state.activeset[i] = -1;
                }
                return;
            }
            
            //
            // General case.
            // Allocate temporaries.
            //
            apserv.rvectorsetlengthatleast(ref state.rctmpg, n);
            apserv.rvectorsetlengthatleast(ref state.rctmprightpart, n);
            apserv.rvectorsetlengthatleast(ref state.rctmps, n);
            apserv.rmatrixsetlengthatleast(ref state.rctmpdense0, n, nec+nic);
            apserv.rmatrixsetlengthatleast(ref state.rctmpdense1, n, nec+nic);
            apserv.bvectorsetlengthatleast(ref state.rctmpisequality, n+nec+nic);
            apserv.ivectorsetlengthatleast(ref state.rctmpconstraintidx, n+nec+nic);
            
            //
            // Calculate descent direction
            //
            for(i_=0; i_<=n-1;i_++)
            {
                state.rctmpg[i_] = -gc[i_];
            }
            
            //
            // Determine candidates to the active set.
            //
            // After this block constraints become either "inactive" (ActiveSet[i]<0)
            // or "candidates" (ActiveSet[i]=0). Previously active constraints always
            // become "candidates".
            //
            for(i=0; i<=n+nec+nic-1; i++)
            {
                if( state.activeset[i]>0 )
                {
                    state.activeset[i] = 0;
                }
                else
                {
                    state.activeset[i] = -1;
                }
            }
            nactiveconstraints = 0;
            nactivebnd = 0;
            nactivelin = 0;
            for(i=0; i<=n-1; i++)
            {
                
                //
                // Activate boundary constraints:
                // * copy constraint index to RCTmpConstraintIdx
                // * set corresponding element of ActiveSet[] to "candidate"
                // * fill RCTmpS by either +1 (lower bound) or -1 (upper bound)
                // * set RCTmpIsEquality to False (BndL<BndU) or True (BndL=BndU)
                // * increase counters
                //
                if( (state.hasbndl[i] && state.hasbndu[i]) && (double)(state.bndl[i])==(double)(state.bndu[i]) )
                {
                    
                    //
                    // Equality constraint is activated
                    //
                    state.rctmpconstraintidx[nactiveconstraints] = i;
                    state.activeset[i] = 0;
                    state.rctmps[i] = 1.0;
                    state.rctmpisequality[nactiveconstraints] = true;
                    nactiveconstraints = nactiveconstraints+1;
                    nactivebnd = nactivebnd+1;
                    continue;
                }
                if( state.hasbndl[i] && (double)(state.xc[i])==(double)(state.bndl[i]) )
                {
                    
                    //
                    // Lower bound is activated
                    //
                    state.rctmpconstraintidx[nactiveconstraints] = i;
                    state.activeset[i] = 0;
                    state.rctmps[i] = -1.0;
                    state.rctmpisequality[nactiveconstraints] = false;
                    nactiveconstraints = nactiveconstraints+1;
                    nactivebnd = nactivebnd+1;
                    continue;
                }
                if( state.hasbndu[i] && (double)(state.xc[i])==(double)(state.bndu[i]) )
                {
                    
                    //
                    // Upper bound is activated
                    //
                    state.rctmpconstraintidx[nactiveconstraints] = i;
                    state.activeset[i] = 0;
                    state.rctmps[i] = 1.0;
                    state.rctmpisequality[nactiveconstraints] = false;
                    nactiveconstraints = nactiveconstraints+1;
                    nactivebnd = nactivebnd+1;
                    continue;
                }
            }
            for(i=0; i<=nec+nic-1; i++)
            {
                if( i>=nec )
                {
                    
                    //
                    // Inequality constraints are skipped if we too far away from
                    // the boundary.
                    //
                    rowscale = 0.0;
                    v = -state.cleic[i,n];
                    for(j=0; j<=n-1; j++)
                    {
                        v = v+state.cleic[i,j]*state.xc[j];
                        rowscale = Math.Max(rowscale, Math.Abs(state.cleic[i,j]*state.s[j]));
                    }
                    if( (double)(v)<=(double)(-(1.0E5*math.machineepsilon*rowscale)) )
                    {
                        
                        //
                        // NOTE: it is important to check for non-strict inequality
                        //       because we have to correctly handle zero constraint
                        //       0*x<=0
                        //
                        continue;
                    }
                }
                for(i_=0; i_<=n-1;i_++)
                {
                    state.rctmpdense0[i_,nactivelin] = state.cleic[i,i_];
                }
                state.rctmpconstraintidx[nactiveconstraints] = n+i;
                state.activeset[n+i] = 0;
                state.rctmpisequality[nactiveconstraints] = i<nec;
                nactiveconstraints = nactiveconstraints+1;
                nactivelin = nactivelin+1;
            }
            
            //
            // Skip if no "candidate" constraints was found
            //
            if( nactiveconstraints==0 )
            {
                for(i=0; i<=n-1; i++)
                {
                    if( (state.hasbndl[i] && state.hasbndu[i]) && (double)(state.bndl[i])==(double)(state.bndu[i]) )
                    {
                        state.activeset[i] = 1;
                        continue;
                    }
                    if( (state.hasbndl[i] && (double)(state.xc[i])==(double)(state.bndl[i])) && (double)(gc[i])>=(double)(0) )
                    {
                        state.activeset[i] = 1;
                        continue;
                    }
                    if( (state.hasbndu[i] && (double)(state.xc[i])==(double)(state.bndu[i])) && (double)(gc[i])<=(double)(0) )
                    {
                        state.activeset[i] = 1;
                        continue;
                    }
                }
                return;
            }
            
            //
            // General case.
            //
            // APPROACH TO CONSTRAINTS ACTIVATION/DEACTIVATION
            //
            // We have NActiveConstraints "candidates": NActiveBnd boundary candidates,
            // NActiveLin linear candidates. Indexes of boundary constraints are stored
            // in RCTmpConstraintIdx[0:NActiveBnd-1], indexes of linear ones are stored
            // in RCTmpConstraintIdx[NActiveBnd:NActiveBnd+NActiveLin-1]. Some of the
            // constraints are equality ones, some are inequality - as specified by 
            // RCTmpIsEquality[i].
            //
            // Now we have to determine active subset of "candidates" set. In order to
            // do so we solve following constrained minimization problem:
            //         (                         )^2
            //     min ( SUM(lambda[i]*A[i]) + G )
            //         (                         )
            // Here:
            // * G is a gradient (column vector)
            // * A[i] is a column vector, linear (left) part of I-th constraint.
            //   I=0..NActiveConstraints-1, first NActiveBnd elements of A are just
            //   subset of identity matrix (boundary constraints), next NActiveLin
            //   elements are subset of rows of the matrix of general linear constraints.
            // * lambda[i] is a Lagrange multiplier corresponding to I-th constraint
            //
            // NOTE: for preconditioned setting A is replaced by A*H^(-0.5), G is
            //       replaced by G*H^(-0.5). We apply this scaling at the last stage,
            //       before passing data to NNLS solver.
            //
            // Minimization is performed subject to non-negativity constraints on
            // lambda[i] corresponding to inequality constraints. Inequality constraints
            // which correspond to non-zero lambda are activated, equality constraints
            // are always considered active.
            //
            // Informally speaking, we "decompose" descent direction -G and represent
            // it as sum of constraint vectors and "residual" part (which is equal to
            // the actual descent direction subject to constraints).
            //
            // SOLUTION OF THE NNLS PROBLEM
            //
            // We solve this optimization problem with Non-Negative Least Squares solver,
            // which can efficiently solve least squares problems of the form
            //
            //         ( [ I | AU ]     )^2
            //     min ( [   |    ]*x-b )   s.t. non-negativity constraints on some x[i]
            //         ( [ 0 | AL ]     )
            //
            // In order to use this solver we have to rearrange rows of A[] and G in
            // such way that first NActiveBnd columns of A store identity matrix (before
            // sorting non-zero elements are randomly distributed in the first NActiveBnd
            // columns of A, during sorting we move them to first NActiveBnd rows).
            //
            // Then we create instance of NNLS solver (we reuse instance left from the
            // previous run of the optimization problem) and solve NNLS problem.
            //
            idx0 = 0;
            idx1 = nactivebnd;
            for(i=0; i<=n-1; i++)
            {
                if( state.activeset[i]>=0 )
                {
                    v = 1/Math.Sqrt(h[i]);
                    for(j=0; j<=nactivelin-1; j++)
                    {
                        state.rctmpdense1[idx0,j] = state.rctmpdense0[i,j]/state.rctmps[i]*v;
                    }
                    state.rctmprightpart[idx0] = state.rctmpg[i]/state.rctmps[i]*v;
                    idx0 = idx0+1;
                }
                else
                {
                    v = 1/Math.Sqrt(h[i]);
                    for(j=0; j<=nactivelin-1; j++)
                    {
                        state.rctmpdense1[idx1,j] = state.rctmpdense0[i,j]*v;
                    }
                    state.rctmprightpart[idx1] = state.rctmpg[i]*v;
                    idx1 = idx1+1;
                }
            }
            snnls.snnlsinit(n, nec+nic, n, state.solver);
            snnls.snnlssetproblem(state.solver, state.rctmpdense1, state.rctmprightpart, nactivebnd, nactiveconstraints-nactivebnd, n);
            for(i=0; i<=nactiveconstraints-1; i++)
            {
                if( state.rctmpisequality[i] )
                {
                    snnls.snnlsdropnnc(state.solver, i);
                }
            }
            snnls.snnlssolve(state.solver, ref state.rctmplambdas);
            
            //
            // After solution of the problem we activate equality constraints (always active)
            // and inequality constraints with non-zero Lagrange multipliers. Then we reorthogonalize
            // active constraints.
            //
            for(i=0; i<=nactiveconstraints-1; i++)
            {
                if( state.rctmpisequality[i] || (double)(state.rctmplambdas[i])>(double)(0) )
                {
                    state.activeset[state.rctmpconstraintidx[i]] = 1;
                }
                else
                {
                    state.activeset[state.rctmpconstraintidx[i]] = 0;
                }
            }
            sasrebuildbasis(state);
        }


    }
    public class mincg
    {
        /*************************************************************************
        This object stores state of the nonlinear CG optimizer.

        You should use ALGLIB functions to work with this object.
        *************************************************************************/
        public class mincgstate : apobject
        {
            public int n;
            public double epsg;
            public double epsf;
            public double epsx;
            public int maxits;
            public double stpmax;
            public double suggestedstep;
            public bool xrep;
            public bool drep;
            public int cgtype;
            public int prectype;
            public double[] diagh;
            public double[] diaghl2;
            public double[,] vcorr;
            public int vcnt;
            public double[] s;
            public double diffstep;
            public int nfev;
            public int mcstage;
            public int k;
            public double[] xk;
            public double[] dk;
            public double[] xn;
            public double[] dn;
            public double[] d;
            public double fold;
            public double stp;
            public double curstpmax;
            public double[] yk;
            public double lastgoodstep;
            public double lastscaledstep;
            public int mcinfo;
            public bool innerresetneeded;
            public bool terminationneeded;
            public double trimthreshold;
            public int rstimer;
            public double[] x;
            public double f;
            public double[] g;
            public bool needf;
            public bool needfg;
            public bool xupdated;
            public bool algpowerup;
            public bool lsstart;
            public bool lsend;
            public double teststep;
            public rcommstate rstate;
            public int repiterationscount;
            public int repnfev;
            public int repvaridx;
            public int repterminationtype;
            public int debugrestartscount;
            public linmin.linminstate lstate;
            public double fbase;
            public double fm2;
            public double fm1;
            public double fp1;
            public double fp2;
            public double betahs;
            public double betady;
            public double[] work0;
            public double[] work1;
            public mincgstate()
            {
                init();
            }
            public override void init()
            {
                diagh = new double[0];
                diaghl2 = new double[0];
                vcorr = new double[0,0];
                s = new double[0];
                xk = new double[0];
                dk = new double[0];
                xn = new double[0];
                dn = new double[0];
                d = new double[0];
                yk = new double[0];
                x = new double[0];
                g = new double[0];
                rstate = new rcommstate();
                lstate = new linmin.linminstate();
                work0 = new double[0];
                work1 = new double[0];
            }
            public override alglib.apobject make_copy()
            {
                mincgstate _result = new mincgstate();
                _result.n = n;
                _result.epsg = epsg;
                _result.epsf = epsf;
                _result.epsx = epsx;
                _result.maxits = maxits;
                _result.stpmax = stpmax;
                _result.suggestedstep = suggestedstep;
                _result.xrep = xrep;
                _result.drep = drep;
                _result.cgtype = cgtype;
                _result.prectype = prectype;
                _result.diagh = (double[])diagh.Clone();
                _result.diaghl2 = (double[])diaghl2.Clone();
                _result.vcorr = (double[,])vcorr.Clone();
                _result.vcnt = vcnt;
                _result.s = (double[])s.Clone();
                _result.diffstep = diffstep;
                _result.nfev = nfev;
                _result.mcstage = mcstage;
                _result.k = k;
                _result.xk = (double[])xk.Clone();
                _result.dk = (double[])dk.Clone();
                _result.xn = (double[])xn.Clone();
                _result.dn = (double[])dn.Clone();
                _result.d = (double[])d.Clone();
                _result.fold = fold;
                _result.stp = stp;
                _result.curstpmax = curstpmax;
                _result.yk = (double[])yk.Clone();
                _result.lastgoodstep = lastgoodstep;
                _result.lastscaledstep = lastscaledstep;
                _result.mcinfo = mcinfo;
                _result.innerresetneeded = innerresetneeded;
                _result.terminationneeded = terminationneeded;
                _result.trimthreshold = trimthreshold;
                _result.rstimer = rstimer;
                _result.x = (double[])x.Clone();
                _result.f = f;
                _result.g = (double[])g.Clone();
                _result.needf = needf;
                _result.needfg = needfg;
                _result.xupdated = xupdated;
                _result.algpowerup = algpowerup;
                _result.lsstart = lsstart;
                _result.lsend = lsend;
                _result.teststep = teststep;
                _result.rstate = (rcommstate)rstate.make_copy();
                _result.repiterationscount = repiterationscount;
                _result.repnfev = repnfev;
                _result.repvaridx = repvaridx;
                _result.repterminationtype = repterminationtype;
                _result.debugrestartscount = debugrestartscount;
                _result.lstate = (linmin.linminstate)lstate.make_copy();
                _result.fbase = fbase;
                _result.fm2 = fm2;
                _result.fm1 = fm1;
                _result.fp1 = fp1;
                _result.fp2 = fp2;
                _result.betahs = betahs;
                _result.betady = betady;
                _result.work0 = (double[])work0.Clone();
                _result.work1 = (double[])work1.Clone();
                return _result;
            }
        };


        public class mincgreport : apobject
        {
            public int iterationscount;
            public int nfev;
            public int varidx;
            public int terminationtype;
            public mincgreport()
            {
                init();
            }
            public override void init()
            {
            }
            public override alglib.apobject make_copy()
            {
                mincgreport _result = new mincgreport();
                _result.iterationscount = iterationscount;
                _result.nfev = nfev;
                _result.varidx = varidx;
                _result.terminationtype = terminationtype;
                return _result;
            }
        };




        public const int rscountdownlen = 10;
        public const double gtol = 0.3;


        /*************************************************************************
                NONLINEAR CONJUGATE GRADIENT METHOD

        DESCRIPTION:
        The subroutine minimizes function F(x) of N arguments by using one of  the
        nonlinear conjugate gradient methods.

        These CG methods are globally convergent (even on non-convex functions) as
        long as grad(f) is Lipschitz continuous in  a  some  neighborhood  of  the
        L = { x : f(x)<=f(x0) }.


        REQUIREMENTS:
        Algorithm will request following information during its operation:
        * function value F and its gradient G (simultaneously) at given point X


        USAGE:
        1. User initializes algorithm state with MinCGCreate() call
        2. User tunes solver parameters with MinCGSetCond(), MinCGSetStpMax() and
           other functions
        3. User calls MinCGOptimize() function which takes algorithm  state   and
           pointer (delegate, etc.) to callback function which calculates F/G.
        4. User calls MinCGResults() to get solution
        5. Optionally, user may call MinCGRestartFrom() to solve another  problem
           with same N but another starting point and/or another function.
           MinCGRestartFrom() allows to reuse already initialized structure.


        INPUT PARAMETERS:
            N       -   problem dimension, N>0:
                        * if given, only leading N elements of X are used
                        * if not given, automatically determined from size of X
            X       -   starting point, array[0..N-1].

        OUTPUT PARAMETERS:
            State   -   structure which stores algorithm state

          -- ALGLIB --
             Copyright 25.03.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void mincgcreate(int n,
            double[] x,
            mincgstate state)
        {
            alglib.ap.assert(n>=1, "MinCGCreate: N too small!");
            alglib.ap.assert(alglib.ap.len(x)>=n, "MinCGCreate: Length(X)<N!");
            alglib.ap.assert(apserv.isfinitevector(x, n), "MinCGCreate: X contains infinite or NaN values!");
            mincginitinternal(n, 0.0, state);
            mincgrestartfrom(state, x);
        }


        /*************************************************************************
        The subroutine is finite difference variant of MinCGCreate(). It uses
        finite differences in order to differentiate target function.

        Description below contains information which is specific to this function
        only. We recommend to read comments on MinCGCreate() in order to get more
        information about creation of CG optimizer.

        INPUT PARAMETERS:
            N       -   problem dimension, N>0:
                        * if given, only leading N elements of X are used
                        * if not given, automatically determined from size of X
            X       -   starting point, array[0..N-1].
            DiffStep-   differentiation step, >0

        OUTPUT PARAMETERS:
            State   -   structure which stores algorithm state

        NOTES:
        1. algorithm uses 4-point central formula for differentiation.
        2. differentiation step along I-th axis is equal to DiffStep*S[I] where
           S[] is scaling vector which can be set by MinCGSetScale() call.
        3. we recommend you to use moderate values of  differentiation  step.  Too
           large step will result in too large truncation  errors, while too small
           step will result in too large numerical  errors.  1.0E-6  can  be  good
           value to start with.
        4. Numerical  differentiation  is   very   inefficient  -   one   gradient
           calculation needs 4*N function evaluations. This function will work for
           any N - either small (1...10), moderate (10...100) or  large  (100...).
           However, performance penalty will be too severe for any N's except  for
           small ones.
           We should also say that code which relies on numerical  differentiation
           is  less  robust  and  precise.  L-BFGS  needs  exact  gradient values.
           Imprecise  gradient may slow down  convergence,  especially  on  highly
           nonlinear problems.
           Thus  we  recommend to use this function for fast prototyping on small-
           dimensional problems only, and to implement analytical gradient as soon
           as possible.

          -- ALGLIB --
             Copyright 16.05.2011 by Bochkanov Sergey
        *************************************************************************/
        public static void mincgcreatef(int n,
            double[] x,
            double diffstep,
            mincgstate state)
        {
            alglib.ap.assert(n>=1, "MinCGCreateF: N too small!");
            alglib.ap.assert(alglib.ap.len(x)>=n, "MinCGCreateF: Length(X)<N!");
            alglib.ap.assert(apserv.isfinitevector(x, n), "MinCGCreateF: X contains infinite or NaN values!");
            alglib.ap.assert(math.isfinite(diffstep), "MinCGCreateF: DiffStep is infinite or NaN!");
            alglib.ap.assert((double)(diffstep)>(double)(0), "MinCGCreateF: DiffStep is non-positive!");
            mincginitinternal(n, diffstep, state);
            mincgrestartfrom(state, x);
        }


        /*************************************************************************
        This function sets stopping conditions for CG optimization algorithm.

        INPUT PARAMETERS:
            State   -   structure which stores algorithm state
            EpsG    -   >=0
                        The  subroutine  finishes  its  work   if   the  condition
                        |v|<EpsG is satisfied, where:
                        * |.| means Euclidian norm
                        * v - scaled gradient vector, v[i]=g[i]*s[i]
                        * g - gradient
                        * s - scaling coefficients set by MinCGSetScale()
            EpsF    -   >=0
                        The  subroutine  finishes  its work if on k+1-th iteration
                        the  condition  |F(k+1)-F(k)|<=EpsF*max{|F(k)|,|F(k+1)|,1}
                        is satisfied.
            EpsX    -   >=0
                        The subroutine finishes its work if  on  k+1-th  iteration
                        the condition |v|<=EpsX is fulfilled, where:
                        * |.| means Euclidian norm
                        * v - scaled step vector, v[i]=dx[i]/s[i]
                        * dx - ste pvector, dx=X(k+1)-X(k)
                        * s - scaling coefficients set by MinCGSetScale()
            MaxIts  -   maximum number of iterations. If MaxIts=0, the  number  of
                        iterations is unlimited.

        Passing EpsG=0, EpsF=0, EpsX=0 and MaxIts=0 (simultaneously) will lead to
        automatic stopping criterion selection (small EpsX).

          -- ALGLIB --
             Copyright 02.04.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void mincgsetcond(mincgstate state,
            double epsg,
            double epsf,
            double epsx,
            int maxits)
        {
            alglib.ap.assert(math.isfinite(epsg), "MinCGSetCond: EpsG is not finite number!");
            alglib.ap.assert((double)(epsg)>=(double)(0), "MinCGSetCond: negative EpsG!");
            alglib.ap.assert(math.isfinite(epsf), "MinCGSetCond: EpsF is not finite number!");
            alglib.ap.assert((double)(epsf)>=(double)(0), "MinCGSetCond: negative EpsF!");
            alglib.ap.assert(math.isfinite(epsx), "MinCGSetCond: EpsX is not finite number!");
            alglib.ap.assert((double)(epsx)>=(double)(0), "MinCGSetCond: negative EpsX!");
            alglib.ap.assert(maxits>=0, "MinCGSetCond: negative MaxIts!");
            if( (((double)(epsg)==(double)(0) && (double)(epsf)==(double)(0)) && (double)(epsx)==(double)(0)) && maxits==0 )
            {
                epsx = 1.0E-6;
            }
            state.epsg = epsg;
            state.epsf = epsf;
            state.epsx = epsx;
            state.maxits = maxits;
        }


        /*************************************************************************
        This function sets scaling coefficients for CG optimizer.

        ALGLIB optimizers use scaling matrices to test stopping  conditions  (step
        size and gradient are scaled before comparison with tolerances).  Scale of
        the I-th variable is a translation invariant measure of:
        a) "how large" the variable is
        b) how large the step should be to make significant changes in the function

        Scaling is also used by finite difference variant of CG optimizer  -  step
        along I-th axis is equal to DiffStep*S[I].

        In   most   optimizers  (and  in  the  CG  too)  scaling is NOT a form  of
        preconditioning. It just  affects  stopping  conditions.  You  should  set
        preconditioner by separate call to one of the MinCGSetPrec...() functions.

        There  is  special  preconditioning  mode, however,  which  uses   scaling
        coefficients to form diagonal preconditioning matrix. You  can  turn  this
        mode on, if you want.   But  you should understand that scaling is not the
        same thing as preconditioning - these are two different, although  related
        forms of tuning solver.

        INPUT PARAMETERS:
            State   -   structure stores algorithm state
            S       -   array[N], non-zero scaling coefficients
                        S[i] may be negative, sign doesn't matter.

          -- ALGLIB --
             Copyright 14.01.2011 by Bochkanov Sergey
        *************************************************************************/
        public static void mincgsetscale(mincgstate state,
            double[] s)
        {
            int i = 0;

            alglib.ap.assert(alglib.ap.len(s)>=state.n, "MinCGSetScale: Length(S)<N");
            for(i=0; i<=state.n-1; i++)
            {
                alglib.ap.assert(math.isfinite(s[i]), "MinCGSetScale: S contains infinite or NAN elements");
                alglib.ap.assert((double)(s[i])!=(double)(0), "MinCGSetScale: S contains zero elements");
                state.s[i] = Math.Abs(s[i]);
            }
        }


        /*************************************************************************
        This function turns on/off reporting.

        INPUT PARAMETERS:
            State   -   structure which stores algorithm state
            NeedXRep-   whether iteration reports are needed or not

        If NeedXRep is True, algorithm will call rep() callback function if  it is
        provided to MinCGOptimize().

          -- ALGLIB --
             Copyright 02.04.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void mincgsetxrep(mincgstate state,
            bool needxrep)
        {
            state.xrep = needxrep;
        }


        /*************************************************************************
        This function turns on/off line search reports.
        These reports are described in more details in developer-only  comments on
        MinCGState object.

        INPUT PARAMETERS:
            State   -   structure which stores algorithm state
            NeedDRep-   whether line search reports are needed or not

        This function is intended for private use only. Turning it on artificially
        may cause program failure.

          -- ALGLIB --
             Copyright 02.04.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void mincgsetdrep(mincgstate state,
            bool needdrep)
        {
            state.drep = needdrep;
        }


        /*************************************************************************
        This function sets CG algorithm.

        INPUT PARAMETERS:
            State   -   structure which stores algorithm state
            CGType  -   algorithm type:
                        * -1    automatic selection of the best algorithm
                        * 0     DY (Dai and Yuan) algorithm
                        * 1     Hybrid DY-HS algorithm

          -- ALGLIB --
             Copyright 02.04.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void mincgsetcgtype(mincgstate state,
            int cgtype)
        {
            alglib.ap.assert(cgtype>=-1 && cgtype<=1, "MinCGSetCGType: incorrect CGType!");
            if( cgtype==-1 )
            {
                cgtype = 1;
            }
            state.cgtype = cgtype;
        }


        /*************************************************************************
        This function sets maximum step length

        INPUT PARAMETERS:
            State   -   structure which stores algorithm state
            StpMax  -   maximum step length, >=0. Set StpMax to 0.0,  if you don't
                        want to limit step length.

        Use this subroutine when you optimize target function which contains exp()
        or  other  fast  growing  functions,  and optimization algorithm makes too
        large  steps  which  leads  to overflow. This function allows us to reject
        steps  that  are  too  large  (and  therefore  expose  us  to the possible
        overflow) without actually calculating function value at the x+stp*d.

          -- ALGLIB --
             Copyright 02.04.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void mincgsetstpmax(mincgstate state,
            double stpmax)
        {
            alglib.ap.assert(math.isfinite(stpmax), "MinCGSetStpMax: StpMax is not finite!");
            alglib.ap.assert((double)(stpmax)>=(double)(0), "MinCGSetStpMax: StpMax<0!");
            state.stpmax = stpmax;
        }


        /*************************************************************************
        This function allows to suggest initial step length to the CG algorithm.

        Suggested  step  length  is used as starting point for the line search. It
        can be useful when you have  badly  scaled  problem,  i.e.  when  ||grad||
        (which is used as initial estimate for the first step) is many  orders  of
        magnitude different from the desired step.

        Line search  may  fail  on  such problems without good estimate of initial
        step length. Imagine, for example, problem with ||grad||=10^50 and desired
        step equal to 0.1 Line  search function will use 10^50  as  initial  step,
        then  it  will  decrease step length by 2 (up to 20 attempts) and will get
        10^44, which is still too large.

        This function allows us to tell than line search should  be  started  from
        some moderate step length, like 1.0, so algorithm will be able  to  detect
        desired step length in a several searches.

        Default behavior (when no step is suggested) is to use preconditioner,  if
        it is available, to generate initial estimate of step length.

        This function influences only first iteration of algorithm. It  should  be
        called between MinCGCreate/MinCGRestartFrom() call and MinCGOptimize call.
        Suggested step is ignored if you have preconditioner.

        INPUT PARAMETERS:
            State   -   structure used to store algorithm state.
            Stp     -   initial estimate of the step length.
                        Can be zero (no estimate).

          -- ALGLIB --
             Copyright 30.07.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void mincgsuggeststep(mincgstate state,
            double stp)
        {
            alglib.ap.assert(math.isfinite(stp), "MinCGSuggestStep: Stp is infinite or NAN");
            alglib.ap.assert((double)(stp)>=(double)(0), "MinCGSuggestStep: Stp<0");
            state.suggestedstep = stp;
        }


        /*************************************************************************
        Modification of the preconditioner: preconditioning is turned off.

        INPUT PARAMETERS:
            State   -   structure which stores algorithm state

        NOTE:  you  can  change  preconditioner  "on  the  fly",  during algorithm
        iterations.

          -- ALGLIB --
             Copyright 13.10.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void mincgsetprecdefault(mincgstate state)
        {
            state.prectype = 0;
            state.innerresetneeded = true;
        }


        /*************************************************************************
        Modification  of  the  preconditioner:  diagonal of approximate Hessian is
        used.

        INPUT PARAMETERS:
            State   -   structure which stores algorithm state
            D       -   diagonal of the approximate Hessian, array[0..N-1],
                        (if larger, only leading N elements are used).

        NOTE:  you  can  change  preconditioner  "on  the  fly",  during algorithm
        iterations.

        NOTE 2: D[i] should be positive. Exception will be thrown otherwise.

        NOTE 3: you should pass diagonal of approximate Hessian - NOT ITS INVERSE.

          -- ALGLIB --
             Copyright 13.10.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void mincgsetprecdiag(mincgstate state,
            double[] d)
        {
            int i = 0;

            alglib.ap.assert(alglib.ap.len(d)>=state.n, "MinCGSetPrecDiag: D is too short");
            for(i=0; i<=state.n-1; i++)
            {
                alglib.ap.assert(math.isfinite(d[i]), "MinCGSetPrecDiag: D contains infinite or NAN elements");
                alglib.ap.assert((double)(d[i])>(double)(0), "MinCGSetPrecDiag: D contains non-positive elements");
            }
            mincgsetprecdiagfast(state, d);
        }


        /*************************************************************************
        Modification of the preconditioner: scale-based diagonal preconditioning.

        This preconditioning mode can be useful when you  don't  have  approximate
        diagonal of Hessian, but you know that your  variables  are  badly  scaled
        (for  example,  one  variable is in [1,10], and another in [1000,100000]),
        and most part of the ill-conditioning comes from different scales of vars.

        In this case simple  scale-based  preconditioner,  with H[i] = 1/(s[i]^2),
        can greatly improve convergence.

        IMPRTANT: you should set scale of your variables with MinCGSetScale() call
        (before or after MinCGSetPrecScale() call). Without knowledge of the scale
        of your variables scale-based preconditioner will be just unit matrix.

        INPUT PARAMETERS:
            State   -   structure which stores algorithm state

        NOTE:  you  can  change  preconditioner  "on  the  fly",  during algorithm
        iterations.

          -- ALGLIB --
             Copyright 13.10.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void mincgsetprecscale(mincgstate state)
        {
            state.prectype = 3;
            state.innerresetneeded = true;
        }


        /*************************************************************************
        NOTES:

        1. This function has two different implementations: one which  uses  exact
           (analytical) user-supplied  gradient, and one which uses function value
           only  and  numerically  differentiates  function  in  order  to  obtain
           gradient.
           
           Depending  on  the  specific  function  used to create optimizer object
           (either MinCGCreate()  for analytical gradient  or  MinCGCreateF()  for
           numerical differentiation) you should  choose  appropriate  variant  of
           MinCGOptimize() - one which accepts function AND gradient or one  which
           accepts function ONLY.

           Be careful to choose variant of MinCGOptimize()  which  corresponds  to
           your optimization scheme! Table below lists different  combinations  of
           callback (function/gradient) passed  to  MinCGOptimize()  and  specific
           function used to create optimizer.
           

                          |         USER PASSED TO MinCGOptimize()
           CREATED WITH   |  function only   |  function and gradient
           ------------------------------------------------------------
           MinCGCreateF() |     work                FAIL
           MinCGCreate()  |     FAIL                work

           Here "FAIL" denotes inappropriate combinations  of  optimizer  creation
           function and MinCGOptimize() version. Attemps to use  such  combination
           (for  example,  to create optimizer with  MinCGCreateF()  and  to  pass
           gradient information to MinCGOptimize()) will lead to  exception  being
           thrown. Either  you  did  not  pass  gradient when it WAS needed or you
           passed gradient when it was NOT needed.

          -- ALGLIB --
             Copyright 20.04.2009 by Bochkanov Sergey
        *************************************************************************/
        public static bool mincgiteration(mincgstate state)
        {
            bool result = new bool();
            int n = 0;
            int i = 0;
            double betak = 0;
            double v = 0;
            double vv = 0;
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
                i = state.rstate.ia[1];
                betak = state.rstate.ra[0];
                v = state.rstate.ra[1];
                vv = state.rstate.ra[2];
            }
            else
            {
                n = -983;
                i = -989;
                betak = -834;
                v = 900;
                vv = -287;
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
            if( state.rstate.stage==4 )
            {
                goto lbl_4;
            }
            if( state.rstate.stage==5 )
            {
                goto lbl_5;
            }
            if( state.rstate.stage==6 )
            {
                goto lbl_6;
            }
            if( state.rstate.stage==7 )
            {
                goto lbl_7;
            }
            if( state.rstate.stage==8 )
            {
                goto lbl_8;
            }
            if( state.rstate.stage==9 )
            {
                goto lbl_9;
            }
            if( state.rstate.stage==10 )
            {
                goto lbl_10;
            }
            if( state.rstate.stage==11 )
            {
                goto lbl_11;
            }
            if( state.rstate.stage==12 )
            {
                goto lbl_12;
            }
            if( state.rstate.stage==13 )
            {
                goto lbl_13;
            }
            if( state.rstate.stage==14 )
            {
                goto lbl_14;
            }
            if( state.rstate.stage==15 )
            {
                goto lbl_15;
            }
            if( state.rstate.stage==16 )
            {
                goto lbl_16;
            }
            if( state.rstate.stage==17 )
            {
                goto lbl_17;
            }
            if( state.rstate.stage==18 )
            {
                goto lbl_18;
            }
            if( state.rstate.stage==19 )
            {
                goto lbl_19;
            }
            
            //
            // Routine body
            //
            
            //
            // Prepare
            //
            n = state.n;
            state.repterminationtype = 0;
            state.repiterationscount = 0;
            state.repvaridx = -1;
            state.repnfev = 0;
            state.debugrestartscount = 0;
            
            //
            //  Check, that transferred derivative value is right
            //
            clearrequestfields(state);
            if( !((double)(state.diffstep)==(double)(0) && (double)(state.teststep)>(double)(0)) )
            {
                goto lbl_20;
            }
            state.needfg = true;
            i = 0;
        lbl_22:
            if( i>n-1 )
            {
                goto lbl_24;
            }
            v = state.x[i];
            state.x[i] = v-state.teststep*state.s[i];
            state.rstate.stage = 0;
            goto lbl_rcomm;
        lbl_0:
            state.fm1 = state.f;
            state.fp1 = state.g[i];
            state.x[i] = v+state.teststep*state.s[i];
            state.rstate.stage = 1;
            goto lbl_rcomm;
        lbl_1:
            state.fm2 = state.f;
            state.fp2 = state.g[i];
            state.x[i] = v;
            state.rstate.stage = 2;
            goto lbl_rcomm;
        lbl_2:
            
            //
            // 2*State.TestStep   -   scale parameter
            // width of segment [Xi-TestStep;Xi+TestStep]
            //
            if( !optserv.derivativecheck(state.fm1, state.fp1, state.fm2, state.fp2, state.f, state.g[i], 2*state.teststep) )
            {
                state.repvaridx = i;
                state.repterminationtype = -7;
                result = false;
                return result;
            }
            i = i+1;
            goto lbl_22;
        lbl_24:
            state.needfg = false;
        lbl_20:
            
            //
            // Preparations continue:
            // * set XK
            // * calculate F/G
            // * set DK to -G
            // * powerup algo (it may change preconditioner)
            // * apply preconditioner to DK
            // * report update of X
            // * check stopping conditions for G
            //
            for(i_=0; i_<=n-1;i_++)
            {
                state.xk[i_] = state.x[i_];
            }
            state.terminationneeded = false;
            clearrequestfields(state);
            if( (double)(state.diffstep)!=(double)(0) )
            {
                goto lbl_25;
            }
            state.needfg = true;
            state.rstate.stage = 3;
            goto lbl_rcomm;
        lbl_3:
            state.needfg = false;
            goto lbl_26;
        lbl_25:
            state.needf = true;
            state.rstate.stage = 4;
            goto lbl_rcomm;
        lbl_4:
            state.fbase = state.f;
            i = 0;
        lbl_27:
            if( i>n-1 )
            {
                goto lbl_29;
            }
            v = state.x[i];
            state.x[i] = v-state.diffstep*state.s[i];
            state.rstate.stage = 5;
            goto lbl_rcomm;
        lbl_5:
            state.fm2 = state.f;
            state.x[i] = v-0.5*state.diffstep*state.s[i];
            state.rstate.stage = 6;
            goto lbl_rcomm;
        lbl_6:
            state.fm1 = state.f;
            state.x[i] = v+0.5*state.diffstep*state.s[i];
            state.rstate.stage = 7;
            goto lbl_rcomm;
        lbl_7:
            state.fp1 = state.f;
            state.x[i] = v+state.diffstep*state.s[i];
            state.rstate.stage = 8;
            goto lbl_rcomm;
        lbl_8:
            state.fp2 = state.f;
            state.x[i] = v;
            state.g[i] = (8*(state.fp1-state.fm1)-(state.fp2-state.fm2))/(6*state.diffstep*state.s[i]);
            i = i+1;
            goto lbl_27;
        lbl_29:
            state.f = state.fbase;
            state.needf = false;
        lbl_26:
            if( !state.drep )
            {
                goto lbl_30;
            }
            
            //
            // Report algorithm powerup (if needed)
            //
            clearrequestfields(state);
            state.algpowerup = true;
            state.rstate.stage = 9;
            goto lbl_rcomm;
        lbl_9:
            state.algpowerup = false;
        lbl_30:
            optserv.trimprepare(state.f, ref state.trimthreshold);
            for(i_=0; i_<=n-1;i_++)
            {
                state.dk[i_] = -state.g[i_];
            }
            preconditionedmultiply(state, ref state.dk, ref state.work0, ref state.work1);
            if( !state.xrep )
            {
                goto lbl_32;
            }
            clearrequestfields(state);
            state.xupdated = true;
            state.rstate.stage = 10;
            goto lbl_rcomm;
        lbl_10:
            state.xupdated = false;
        lbl_32:
            if( state.terminationneeded )
            {
                for(i_=0; i_<=n-1;i_++)
                {
                    state.xn[i_] = state.xk[i_];
                }
                state.repterminationtype = 8;
                result = false;
                return result;
            }
            v = 0;
            for(i=0; i<=n-1; i++)
            {
                v = v+math.sqr(state.g[i]*state.s[i]);
            }
            if( (double)(Math.Sqrt(v))<=(double)(state.epsg) )
            {
                for(i_=0; i_<=n-1;i_++)
                {
                    state.xn[i_] = state.xk[i_];
                }
                state.repterminationtype = 4;
                result = false;
                return result;
            }
            state.repnfev = 1;
            state.k = 0;
            state.fold = state.f;
            
            //
            // Choose initial step.
            // Apply preconditioner, if we have something other than default.
            //
            if( state.prectype==2 || state.prectype==3 )
            {
                
                //
                // because we use preconditioner, step length must be equal
                // to the norm of DK
                //
                v = 0.0;
                for(i_=0; i_<=n-1;i_++)
                {
                    v += state.dk[i_]*state.dk[i_];
                }
                state.lastgoodstep = Math.Sqrt(v);
            }
            else
            {
                
                //
                // No preconditioner is used, we try to use suggested step
                //
                if( (double)(state.suggestedstep)>(double)(0) )
                {
                    state.lastgoodstep = state.suggestedstep;
                }
                else
                {
                    state.lastgoodstep = 1.0;
                }
            }
            
            //
            // Main cycle
            //
            state.rstimer = rscountdownlen;
        lbl_34:
            if( false )
            {
                goto lbl_35;
            }
            
            //
            // * clear reset flag
            // * clear termination flag
            // * store G[k] for later calculation of Y[k]
            // * prepare starting point and direction and step length for line search
            //
            state.innerresetneeded = false;
            state.terminationneeded = false;
            for(i_=0; i_<=n-1;i_++)
            {
                state.yk[i_] = -state.g[i_];
            }
            for(i_=0; i_<=n-1;i_++)
            {
                state.d[i_] = state.dk[i_];
            }
            for(i_=0; i_<=n-1;i_++)
            {
                state.x[i_] = state.xk[i_];
            }
            state.mcstage = 0;
            state.stp = 1.0;
            linmin.linminnormalized(ref state.d, ref state.stp, n);
            if( (double)(state.lastgoodstep)!=(double)(0) )
            {
                state.stp = state.lastgoodstep;
            }
            state.curstpmax = state.stpmax;
            
            //
            // Report beginning of line search (if needed)
            // Terminate algorithm, if user request was detected
            //
            if( !state.drep )
            {
                goto lbl_36;
            }
            clearrequestfields(state);
            state.lsstart = true;
            state.rstate.stage = 11;
            goto lbl_rcomm;
        lbl_11:
            state.lsstart = false;
        lbl_36:
            if( state.terminationneeded )
            {
                for(i_=0; i_<=n-1;i_++)
                {
                    state.xn[i_] = state.x[i_];
                }
                state.repterminationtype = 8;
                result = false;
                return result;
            }
            
            //
            // Minimization along D
            //
            linmin.mcsrch(n, ref state.x, ref state.f, ref state.g, state.d, ref state.stp, state.curstpmax, gtol, ref state.mcinfo, ref state.nfev, ref state.work0, state.lstate, ref state.mcstage);
        lbl_38:
            if( state.mcstage==0 )
            {
                goto lbl_39;
            }
            
            //
            // Calculate function/gradient using either
            // analytical gradient supplied by user
            // or finite difference approximation.
            //
            // "Trim" function in order to handle near-singularity points.
            //
            clearrequestfields(state);
            if( (double)(state.diffstep)!=(double)(0) )
            {
                goto lbl_40;
            }
            state.needfg = true;
            state.rstate.stage = 12;
            goto lbl_rcomm;
        lbl_12:
            state.needfg = false;
            goto lbl_41;
        lbl_40:
            state.needf = true;
            state.rstate.stage = 13;
            goto lbl_rcomm;
        lbl_13:
            state.fbase = state.f;
            i = 0;
        lbl_42:
            if( i>n-1 )
            {
                goto lbl_44;
            }
            v = state.x[i];
            state.x[i] = v-state.diffstep*state.s[i];
            state.rstate.stage = 14;
            goto lbl_rcomm;
        lbl_14:
            state.fm2 = state.f;
            state.x[i] = v-0.5*state.diffstep*state.s[i];
            state.rstate.stage = 15;
            goto lbl_rcomm;
        lbl_15:
            state.fm1 = state.f;
            state.x[i] = v+0.5*state.diffstep*state.s[i];
            state.rstate.stage = 16;
            goto lbl_rcomm;
        lbl_16:
            state.fp1 = state.f;
            state.x[i] = v+state.diffstep*state.s[i];
            state.rstate.stage = 17;
            goto lbl_rcomm;
        lbl_17:
            state.fp2 = state.f;
            state.x[i] = v;
            state.g[i] = (8*(state.fp1-state.fm1)-(state.fp2-state.fm2))/(6*state.diffstep*state.s[i]);
            i = i+1;
            goto lbl_42;
        lbl_44:
            state.f = state.fbase;
            state.needf = false;
        lbl_41:
            optserv.trimfunction(ref state.f, ref state.g, n, state.trimthreshold);
            
            //
            // Call MCSRCH again
            //
            linmin.mcsrch(n, ref state.x, ref state.f, ref state.g, state.d, ref state.stp, state.curstpmax, gtol, ref state.mcinfo, ref state.nfev, ref state.work0, state.lstate, ref state.mcstage);
            goto lbl_38;
        lbl_39:
            
            //
            // * report end of line search
            // * store current point to XN
            // * report iteration
            // * terminate algorithm if user request was detected
            //
            if( !state.drep )
            {
                goto lbl_45;
            }
            
            //
            // Report end of line search (if needed)
            //
            clearrequestfields(state);
            state.lsend = true;
            state.rstate.stage = 18;
            goto lbl_rcomm;
        lbl_18:
            state.lsend = false;
        lbl_45:
            for(i_=0; i_<=n-1;i_++)
            {
                state.xn[i_] = state.x[i_];
            }
            if( !state.xrep )
            {
                goto lbl_47;
            }
            clearrequestfields(state);
            state.xupdated = true;
            state.rstate.stage = 19;
            goto lbl_rcomm;
        lbl_19:
            state.xupdated = false;
        lbl_47:
            if( state.terminationneeded )
            {
                for(i_=0; i_<=n-1;i_++)
                {
                    state.xn[i_] = state.x[i_];
                }
                state.repterminationtype = 8;
                result = false;
                return result;
            }
            
            //
            // Line search is finished.
            // * calculate BetaK
            // * calculate DN
            // * update timers
            // * calculate step length:
            //   * LastScaledStep is ALWAYS calculated because it is used in the stopping criteria
            //   * LastGoodStep is updated only when MCINFO is equal to 1 (Wolfe conditions hold).
            //     See below for more explanation.
            //
            if( state.mcinfo==1 && !state.innerresetneeded )
            {
                
                //
                // Standard Wolfe conditions hold
                // Calculate Y[K] and D[K]'*Y[K]
                //
                for(i_=0; i_<=n-1;i_++)
                {
                    state.yk[i_] = state.yk[i_] + state.g[i_];
                }
                vv = 0.0;
                for(i_=0; i_<=n-1;i_++)
                {
                    vv += state.yk[i_]*state.dk[i_];
                }
                
                //
                // Calculate BetaK according to DY formula
                //
                v = preconditionedmultiply2(state, ref state.g, ref state.g, ref state.work0, ref state.work1);
                state.betady = v/vv;
                
                //
                // Calculate BetaK according to HS formula
                //
                v = preconditionedmultiply2(state, ref state.g, ref state.yk, ref state.work0, ref state.work1);
                state.betahs = v/vv;
                
                //
                // Choose BetaK
                //
                if( state.cgtype==0 )
                {
                    betak = state.betady;
                }
                if( state.cgtype==1 )
                {
                    betak = Math.Max(0, Math.Min(state.betady, state.betahs));
                }
            }
            else
            {
                
                //
                // Something is wrong (may be function is too wild or too flat)
                // or we just have to restart algo.
                //
                // We'll set BetaK=0, which will restart CG algorithm.
                // We can stop later (during normal checks) if stopping conditions are met.
                //
                betak = 0;
                state.debugrestartscount = state.debugrestartscount+1;
            }
            if( state.repiterationscount>0 && state.repiterationscount%(3+n)==0 )
            {
                
                //
                // clear Beta every N iterations
                //
                betak = 0;
            }
            if( state.mcinfo==1 || state.mcinfo==5 )
            {
                state.rstimer = rscountdownlen;
            }
            else
            {
                state.rstimer = state.rstimer-1;
            }
            for(i_=0; i_<=n-1;i_++)
            {
                state.dn[i_] = -state.g[i_];
            }
            preconditionedmultiply(state, ref state.dn, ref state.work0, ref state.work1);
            for(i_=0; i_<=n-1;i_++)
            {
                state.dn[i_] = state.dn[i_] + betak*state.dk[i_];
            }
            state.lastscaledstep = 0.0;
            for(i=0; i<=n-1; i++)
            {
                state.lastscaledstep = state.lastscaledstep+math.sqr(state.d[i]/state.s[i]);
            }
            state.lastscaledstep = state.stp*Math.Sqrt(state.lastscaledstep);
            if( state.mcinfo==1 )
            {
                
                //
                // Step is good (Wolfe conditions hold), update LastGoodStep.
                //
                // This check for MCINFO=1 is essential because sometimes in the
                // constrained optimization setting we may take very short steps
                // (like 1E-15) because we were very close to boundary of the
                // feasible area. Such short step does not mean that we've converged
                // to the solution - it was so short because we were close to the
                // boundary and there was a limit on step length.
                //
                // So having such short step is quite normal situation. However, we
                // should NOT start next iteration from step whose initial length is
                // estimated as 1E-15 because it may lead to the failure of the
                // linear minimizer (step is too short, function does not changes,
                // line search stagnates).
                //
                state.lastgoodstep = 0;
                for(i=0; i<=n-1; i++)
                {
                    state.lastgoodstep = state.lastgoodstep+math.sqr(state.d[i]);
                }
                state.lastgoodstep = state.stp*Math.Sqrt(state.lastgoodstep);
            }
            
            //
            // Update information.
            // Check stopping conditions.
            //
            state.repnfev = state.repnfev+state.nfev;
            state.repiterationscount = state.repiterationscount+1;
            if( state.repiterationscount>=state.maxits && state.maxits>0 )
            {
                
                //
                // Too many iterations
                //
                state.repterminationtype = 5;
                result = false;
                return result;
            }
            v = 0;
            for(i=0; i<=n-1; i++)
            {
                v = v+math.sqr(state.g[i]*state.s[i]);
            }
            if( (double)(Math.Sqrt(v))<=(double)(state.epsg) )
            {
                
                //
                // Gradient is small enough
                //
                state.repterminationtype = 4;
                result = false;
                return result;
            }
            if( !state.innerresetneeded )
            {
                
                //
                // These conditions are checked only when no inner reset was requested by user
                //
                if( (double)(state.fold-state.f)<=(double)(state.epsf*Math.Max(Math.Abs(state.fold), Math.Max(Math.Abs(state.f), 1.0))) )
                {
                    
                    //
                    // F(k+1)-F(k) is small enough
                    //
                    state.repterminationtype = 1;
                    result = false;
                    return result;
                }
                if( (double)(state.lastscaledstep)<=(double)(state.epsx) )
                {
                    
                    //
                    // X(k+1)-X(k) is small enough
                    //
                    state.repterminationtype = 2;
                    result = false;
                    return result;
                }
            }
            if( state.rstimer<=0 )
            {
                
                //
                // Too many subsequent restarts
                //
                state.repterminationtype = 7;
                result = false;
                return result;
            }
            
            //
            // Shift Xk/Dk, update other information
            //
            for(i_=0; i_<=n-1;i_++)
            {
                state.xk[i_] = state.xn[i_];
            }
            for(i_=0; i_<=n-1;i_++)
            {
                state.dk[i_] = state.dn[i_];
            }
            state.fold = state.f;
            state.k = state.k+1;
            goto lbl_34;
        lbl_35:
            result = false;
            return result;
            
            //
            // Saving state
            //
        lbl_rcomm:
            result = true;
            state.rstate.ia[0] = n;
            state.rstate.ia[1] = i;
            state.rstate.ra[0] = betak;
            state.rstate.ra[1] = v;
            state.rstate.ra[2] = vv;
            return result;
        }


        /*************************************************************************
        Conjugate gradient results

        INPUT PARAMETERS:
            State   -   algorithm state

        OUTPUT PARAMETERS:
            X       -   array[0..N-1], solution
            Rep     -   optimization report:
                        * Rep.TerminationType completetion code:
                            * -7    gradient verification failed.
                                    See MinCGSetGradientCheck() for more information.
                            *  1    relative function improvement is no more than
                                    EpsF.
                            *  2    relative step is no more than EpsX.
                            *  4    gradient norm is no more than EpsG
                            *  5    MaxIts steps was taken
                            *  7    stopping conditions are too stringent,
                                    further improvement is impossible,
                                    we return best X found so far
                            *  8    terminated by user
                        * Rep.IterationsCount contains iterations count
                        * NFEV countains number of function calculations

          -- ALGLIB --
             Copyright 20.04.2009 by Bochkanov Sergey
        *************************************************************************/
        public static void mincgresults(mincgstate state,
            ref double[] x,
            mincgreport rep)
        {
            x = new double[0];

            mincgresultsbuf(state, ref x, rep);
        }


        /*************************************************************************
        Conjugate gradient results

        Buffered implementation of MinCGResults(), which uses pre-allocated buffer
        to store X[]. If buffer size is  too  small,  it  resizes  buffer.  It  is
        intended to be used in the inner cycles of performance critical algorithms
        where array reallocation penalty is too large to be ignored.

          -- ALGLIB --
             Copyright 20.04.2009 by Bochkanov Sergey
        *************************************************************************/
        public static void mincgresultsbuf(mincgstate state,
            ref double[] x,
            mincgreport rep)
        {
            int i_ = 0;

            if( alglib.ap.len(x)<state.n )
            {
                x = new double[state.n];
            }
            for(i_=0; i_<=state.n-1;i_++)
            {
                x[i_] = state.xn[i_];
            }
            rep.iterationscount = state.repiterationscount;
            rep.nfev = state.repnfev;
            rep.varidx = state.repvaridx;
            rep.terminationtype = state.repterminationtype;
        }


        /*************************************************************************
        This  subroutine  restarts  CG  algorithm from new point. All optimization
        parameters are left unchanged.

        This  function  allows  to  solve multiple  optimization  problems  (which
        must have same number of dimensions) without object reallocation penalty.

        INPUT PARAMETERS:
            State   -   structure used to store algorithm state.
            X       -   new starting point.

          -- ALGLIB --
             Copyright 30.07.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void mincgrestartfrom(mincgstate state,
            double[] x)
        {
            int i_ = 0;

            alglib.ap.assert(alglib.ap.len(x)>=state.n, "MinCGRestartFrom: Length(X)<N!");
            alglib.ap.assert(apserv.isfinitevector(x, state.n), "MinCGCreate: X contains infinite or NaN values!");
            for(i_=0; i_<=state.n-1;i_++)
            {
                state.x[i_] = x[i_];
            }
            mincgsuggeststep(state, 0.0);
            state.rstate.ia = new int[1+1];
            state.rstate.ra = new double[2+1];
            state.rstate.stage = -1;
            clearrequestfields(state);
        }


        /*************************************************************************
        Faster version of MinCGSetPrecDiag(), for time-critical parts of code,
        without safety checks.

          -- ALGLIB --
             Copyright 13.10.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void mincgsetprecdiagfast(mincgstate state,
            double[] d)
        {
            int i = 0;

            apserv.rvectorsetlengthatleast(ref state.diagh, state.n);
            apserv.rvectorsetlengthatleast(ref state.diaghl2, state.n);
            state.prectype = 2;
            state.vcnt = 0;
            state.innerresetneeded = true;
            for(i=0; i<=state.n-1; i++)
            {
                state.diagh[i] = d[i];
                state.diaghl2[i] = 0.0;
            }
        }


        /*************************************************************************
        This function sets low-rank preconditioner for Hessian matrix  H=D+V'*C*V,
        where:
        * H is a Hessian matrix, which is approximated by D/V/C
        * D=D1+D2 is a diagonal matrix, which includes two positive definite terms:
          * constant term D1 (is not updated or infrequently updated)
          * variable term D2 (can be cheaply updated from iteration to iteration)
        * V is a low-rank correction
        * C is a diagonal factor of low-rank correction

        Preconditioner P is calculated using approximate Woodburry formula:
            P  = D^(-1) - D^(-1)*V'*(C^(-1)+V*D1^(-1)*V')^(-1)*V*D^(-1)
               = D^(-1) - D^(-1)*VC'*VC*D^(-1),
        where
            VC = sqrt(B)*V
            B  = (C^(-1)+V*D1^(-1)*V')^(-1)
            
        Note that B is calculated using constant term (D1) only,  which  allows us
        to update D2 without recalculation of B or   VC.  Such  preconditioner  is
        exact when D2 is zero. When D2 is non-zero, it is only approximation,  but
        very good and cheap one.

        This function accepts D1, V, C.
        D2 is set to zero by default.

        Cost of this update is O(N*VCnt*VCnt), but D2 can be updated in just O(N)
        by MinCGSetPrecVarPart.

          -- ALGLIB --
             Copyright 13.10.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void mincgsetpreclowrankfast(mincgstate state,
            double[] d1,
            double[] c,
            double[,] v,
            int vcnt)
        {
            int i = 0;
            int j = 0;
            int k = 0;
            int n = 0;
            double t = 0;
            double[,] b = new double[0,0];
            int i_ = 0;

            if( vcnt==0 )
            {
                mincgsetprecdiagfast(state, d1);
                return;
            }
            n = state.n;
            b = new double[vcnt, vcnt];
            apserv.rvectorsetlengthatleast(ref state.diagh, n);
            apserv.rvectorsetlengthatleast(ref state.diaghl2, n);
            apserv.rmatrixsetlengthatleast(ref state.vcorr, vcnt, n);
            state.prectype = 2;
            state.vcnt = vcnt;
            state.innerresetneeded = true;
            for(i=0; i<=n-1; i++)
            {
                state.diagh[i] = d1[i];
                state.diaghl2[i] = 0.0;
            }
            for(i=0; i<=vcnt-1; i++)
            {
                for(j=i; j<=vcnt-1; j++)
                {
                    t = 0;
                    for(k=0; k<=n-1; k++)
                    {
                        t = t+v[i,k]*v[j,k]/d1[k];
                    }
                    b[i,j] = t;
                }
                b[i,i] = b[i,i]+1.0/c[i];
            }
            if( !trfac.spdmatrixcholeskyrec(ref b, 0, vcnt, true, ref state.work0) )
            {
                state.vcnt = 0;
                return;
            }
            for(i=0; i<=vcnt-1; i++)
            {
                for(i_=0; i_<=n-1;i_++)
                {
                    state.vcorr[i,i_] = v[i,i_];
                }
                for(j=0; j<=i-1; j++)
                {
                    t = b[j,i];
                    for(i_=0; i_<=n-1;i_++)
                    {
                        state.vcorr[i,i_] = state.vcorr[i,i_] - t*state.vcorr[j,i_];
                    }
                }
                t = 1/b[i,i];
                for(i_=0; i_<=n-1;i_++)
                {
                    state.vcorr[i,i_] = t*state.vcorr[i,i_];
                }
            }
        }


        /*************************************************************************
        This function updates variable part (diagonal matrix D2)
        of low-rank preconditioner.

        This update is very cheap and takes just O(N) time.

        It has no effect with default preconditioner.

          -- ALGLIB --
             Copyright 13.10.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void mincgsetprecvarpart(mincgstate state,
            double[] d2)
        {
            int i = 0;
            int n = 0;

            n = state.n;
            for(i=0; i<=n-1; i++)
            {
                state.diaghl2[i] = d2[i];
            }
        }


        /*************************************************************************

        This  subroutine  turns  on  verification  of  the  user-supplied analytic
        gradient:
        * user calls this subroutine before optimization begins
        * MinCGOptimize() is called
        * prior to  actual  optimization, for each component  of  parameters being
          optimized X[i] algorithm performs following steps:
          * two trial steps are made to X[i]-TestStep*S[i] and X[i]+TestStep*S[i],
            where X[i] is i-th component of the initial point and S[i] is a  scale
            of i-th parameter
          * F(X) is evaluated at these trial points
          * we perform one more evaluation in the middle point of the interval
          * we  build  cubic  model using function values and derivatives at trial
            points and we compare its prediction with actual value in  the  middle
            point
          * in case difference between prediction and actual value is higher  than
            some predetermined threshold, algorithm stops with completion code -7;
            Rep.VarIdx is set to index of the parameter with incorrect derivative.
        * after verification is over, algorithm proceeds to the actual optimization.

        NOTE 1: verification  needs  N (parameters count) gradient evaluations. It
                is very costly and you should use  it  only  for  low  dimensional
                problems,  when  you  want  to  be  sure  that  you've   correctly
                calculated  analytic  derivatives.  You  should  not use it in the
                production code (unless you want to check derivatives provided  by
                some third party).

        NOTE 2: you  should  carefully  choose  TestStep. Value which is too large
                (so large that function behaviour is significantly non-cubic) will
                lead to false alarms. You may use  different  step  for  different
                parameters by means of setting scale with MinCGSetScale().

        NOTE 3: this function may lead to false positives. In case it reports that
                I-th  derivative was calculated incorrectly, you may decrease test
                step  and  try  one  more  time  - maybe your function changes too
                sharply  and  your  step  is  too  large for such rapidly chanding
                function.

        INPUT PARAMETERS:
            State       -   structure used to store algorithm state
            TestStep    -   verification step:
                            * TestStep=0 turns verification off
                            * TestStep>0 activates verification

          -- ALGLIB --
             Copyright 31.05.2012 by Bochkanov Sergey
        *************************************************************************/
        public static void mincgsetgradientcheck(mincgstate state,
            double teststep)
        {
            alglib.ap.assert(math.isfinite(teststep), "MinCGSetGradientCheck: TestStep contains NaN or Infinite");
            alglib.ap.assert((double)(teststep)>=(double)(0), "MinCGSetGradientCheck: invalid argument TestStep(TestStep<0)");
            state.teststep = teststep;
        }


        /*************************************************************************
        Clears request fileds (to be sure that we don't forgot to clear something)
        *************************************************************************/
        private static void clearrequestfields(mincgstate state)
        {
            state.needf = false;
            state.needfg = false;
            state.xupdated = false;
            state.lsstart = false;
            state.lsend = false;
            state.algpowerup = false;
        }


        /*************************************************************************
        This function calculates preconditioned product H^(-1)*x and stores result
        back into X. Work0[] and Work1[] are used as temporaries (size must be at
        least N; this function doesn't allocate arrays).

          -- ALGLIB --
             Copyright 13.10.2010 by Bochkanov Sergey
        *************************************************************************/
        private static void preconditionedmultiply(mincgstate state,
            ref double[] x,
            ref double[] work0,
            ref double[] work1)
        {
            int i = 0;
            int n = 0;
            int vcnt = 0;
            double v = 0;
            int i_ = 0;

            n = state.n;
            vcnt = state.vcnt;
            if( state.prectype==0 )
            {
                return;
            }
            if( state.prectype==3 )
            {
                for(i=0; i<=n-1; i++)
                {
                    x[i] = x[i]*state.s[i]*state.s[i];
                }
                return;
            }
            alglib.ap.assert(state.prectype==2, "MinCG: internal error (unexpected PrecType)");
            
            //
            // handle part common for VCnt=0 and VCnt<>0
            //
            for(i=0; i<=n-1; i++)
            {
                x[i] = x[i]/(state.diagh[i]+state.diaghl2[i]);
            }
            
            //
            // if VCnt>0
            //
            if( vcnt>0 )
            {
                for(i=0; i<=vcnt-1; i++)
                {
                    v = 0.0;
                    for(i_=0; i_<=n-1;i_++)
                    {
                        v += state.vcorr[i,i_]*x[i_];
                    }
                    work0[i] = v;
                }
                for(i=0; i<=n-1; i++)
                {
                    work1[i] = 0;
                }
                for(i=0; i<=vcnt-1; i++)
                {
                    v = work0[i];
                    for(i_=0; i_<=n-1;i_++)
                    {
                        state.work1[i_] = state.work1[i_] + v*state.vcorr[i,i_];
                    }
                }
                for(i=0; i<=n-1; i++)
                {
                    x[i] = x[i]-state.work1[i]/(state.diagh[i]+state.diaghl2[i]);
                }
            }
        }


        /*************************************************************************
        This function calculates preconditioned product x'*H^(-1)*y. Work0[] and
        Work1[] are used as temporaries (size must be at least N; this function
        doesn't allocate arrays).

          -- ALGLIB --
             Copyright 13.10.2010 by Bochkanov Sergey
        *************************************************************************/
        private static double preconditionedmultiply2(mincgstate state,
            ref double[] x,
            ref double[] y,
            ref double[] work0,
            ref double[] work1)
        {
            double result = 0;
            int i = 0;
            int n = 0;
            int vcnt = 0;
            double v0 = 0;
            double v1 = 0;
            int i_ = 0;

            n = state.n;
            vcnt = state.vcnt;
            
            //
            // no preconditioning
            //
            if( state.prectype==0 )
            {
                v0 = 0.0;
                for(i_=0; i_<=n-1;i_++)
                {
                    v0 += x[i_]*y[i_];
                }
                result = v0;
                return result;
            }
            if( state.prectype==3 )
            {
                result = 0;
                for(i=0; i<=n-1; i++)
                {
                    result = result+x[i]*state.s[i]*state.s[i]*y[i];
                }
                return result;
            }
            alglib.ap.assert(state.prectype==2, "MinCG: internal error (unexpected PrecType)");
            
            //
            // low rank preconditioning
            //
            result = 0.0;
            for(i=0; i<=n-1; i++)
            {
                result = result+x[i]*y[i]/(state.diagh[i]+state.diaghl2[i]);
            }
            if( vcnt>0 )
            {
                for(i=0; i<=n-1; i++)
                {
                    work0[i] = x[i]/(state.diagh[i]+state.diaghl2[i]);
                    work1[i] = y[i]/(state.diagh[i]+state.diaghl2[i]);
                }
                for(i=0; i<=vcnt-1; i++)
                {
                    v0 = 0.0;
                    for(i_=0; i_<=n-1;i_++)
                    {
                        v0 += work0[i_]*state.vcorr[i,i_];
                    }
                    v1 = 0.0;
                    for(i_=0; i_<=n-1;i_++)
                    {
                        v1 += work1[i_]*state.vcorr[i,i_];
                    }
                    result = result-v0*v1;
                }
            }
            return result;
        }


        /*************************************************************************
        Internal initialization subroutine

          -- ALGLIB --
             Copyright 16.05.2011 by Bochkanov Sergey
        *************************************************************************/
        private static void mincginitinternal(int n,
            double diffstep,
            mincgstate state)
        {
            int i = 0;

            
            //
            // Initialize
            //
            state.teststep = 0;
            state.n = n;
            state.diffstep = diffstep;
            mincgsetcond(state, 0, 0, 0, 0);
            mincgsetxrep(state, false);
            mincgsetdrep(state, false);
            mincgsetstpmax(state, 0);
            mincgsetcgtype(state, -1);
            mincgsetprecdefault(state);
            state.xk = new double[n];
            state.dk = new double[n];
            state.xn = new double[n];
            state.dn = new double[n];
            state.x = new double[n];
            state.d = new double[n];
            state.g = new double[n];
            state.work0 = new double[n];
            state.work1 = new double[n];
            state.yk = new double[n];
            state.s = new double[n];
            for(i=0; i<=n-1; i++)
            {
                state.s[i] = 1.0;
            }
        }


    }
    public class minbleic
    {
        /*************************************************************************
        This object stores nonlinear optimizer state.
        You should use functions provided by MinBLEIC subpackage to work with this
        object
        *************************************************************************/
        public class minbleicstate : apobject
        {
            public int nmain;
            public int nslack;
            public double epsg;
            public double epsf;
            public double epsx;
            public int maxits;
            public bool xrep;
            public double stpmax;
            public double diffstep;
            public sactivesets.sactiveset sas;
            public double[] s;
            public int prectype;
            public double[] diagh;
            public double[] x;
            public double f;
            public double[] g;
            public bool needf;
            public bool needfg;
            public bool xupdated;
            public double teststep;
            public rcommstate rstate;
            public double[] gc;
            public double[] xn;
            public double[] gn;
            public double[] xp;
            public double[] gp;
            public double fc;
            public double fn;
            public double fp;
            public double[] d;
            public double[,] cleic;
            public int nec;
            public int nic;
            public double lastgoodstep;
            public double lastscaledgoodstep;
            public bool[] hasbndl;
            public bool[] hasbndu;
            public double[] bndl;
            public double[] bndu;
            public int repinneriterationscount;
            public int repouteriterationscount;
            public int repnfev;
            public int repvaridx;
            public int repterminationtype;
            public double repdebugeqerr;
            public double repdebugfs;
            public double repdebugff;
            public double repdebugdx;
            public int repdebugfeasqpits;
            public int repdebugfeasgpaits;
            public double[] xstart;
            public snnls.snnlssolver solver;
            public double fbase;
            public double fm2;
            public double fm1;
            public double fp1;
            public double fp2;
            public double xm1;
            public double xp1;
            public double gm1;
            public double gp1;
            public int cidx;
            public double cval;
            public double[] tmpprec;
            public int nfev;
            public int mcstage;
            public double stp;
            public double curstpmax;
            public double activationstep;
            public double[] work;
            public linmin.linminstate lstate;
            public double trimthreshold;
            public int nonmonotoniccnt;
            public int k;
            public int q;
            public int p;
            public double[] rho;
            public double[,] yk;
            public double[,] sk;
            public double[] theta;
            public minbleicstate()
            {
                init();
            }
            public override void init()
            {
                sas = new sactivesets.sactiveset();
                s = new double[0];
                diagh = new double[0];
                x = new double[0];
                g = new double[0];
                rstate = new rcommstate();
                gc = new double[0];
                xn = new double[0];
                gn = new double[0];
                xp = new double[0];
                gp = new double[0];
                d = new double[0];
                cleic = new double[0,0];
                hasbndl = new bool[0];
                hasbndu = new bool[0];
                bndl = new double[0];
                bndu = new double[0];
                xstart = new double[0];
                solver = new snnls.snnlssolver();
                tmpprec = new double[0];
                work = new double[0];
                lstate = new linmin.linminstate();
                rho = new double[0];
                yk = new double[0,0];
                sk = new double[0,0];
                theta = new double[0];
            }
            public override alglib.apobject make_copy()
            {
                minbleicstate _result = new minbleicstate();
                _result.nmain = nmain;
                _result.nslack = nslack;
                _result.epsg = epsg;
                _result.epsf = epsf;
                _result.epsx = epsx;
                _result.maxits = maxits;
                _result.xrep = xrep;
                _result.stpmax = stpmax;
                _result.diffstep = diffstep;
                _result.sas = (sactivesets.sactiveset)sas.make_copy();
                _result.s = (double[])s.Clone();
                _result.prectype = prectype;
                _result.diagh = (double[])diagh.Clone();
                _result.x = (double[])x.Clone();
                _result.f = f;
                _result.g = (double[])g.Clone();
                _result.needf = needf;
                _result.needfg = needfg;
                _result.xupdated = xupdated;
                _result.teststep = teststep;
                _result.rstate = (rcommstate)rstate.make_copy();
                _result.gc = (double[])gc.Clone();
                _result.xn = (double[])xn.Clone();
                _result.gn = (double[])gn.Clone();
                _result.xp = (double[])xp.Clone();
                _result.gp = (double[])gp.Clone();
                _result.fc = fc;
                _result.fn = fn;
                _result.fp = fp;
                _result.d = (double[])d.Clone();
                _result.cleic = (double[,])cleic.Clone();
                _result.nec = nec;
                _result.nic = nic;
                _result.lastgoodstep = lastgoodstep;
                _result.lastscaledgoodstep = lastscaledgoodstep;
                _result.hasbndl = (bool[])hasbndl.Clone();
                _result.hasbndu = (bool[])hasbndu.Clone();
                _result.bndl = (double[])bndl.Clone();
                _result.bndu = (double[])bndu.Clone();
                _result.repinneriterationscount = repinneriterationscount;
                _result.repouteriterationscount = repouteriterationscount;
                _result.repnfev = repnfev;
                _result.repvaridx = repvaridx;
                _result.repterminationtype = repterminationtype;
                _result.repdebugeqerr = repdebugeqerr;
                _result.repdebugfs = repdebugfs;
                _result.repdebugff = repdebugff;
                _result.repdebugdx = repdebugdx;
                _result.repdebugfeasqpits = repdebugfeasqpits;
                _result.repdebugfeasgpaits = repdebugfeasgpaits;
                _result.xstart = (double[])xstart.Clone();
                _result.solver = (snnls.snnlssolver)solver.make_copy();
                _result.fbase = fbase;
                _result.fm2 = fm2;
                _result.fm1 = fm1;
                _result.fp1 = fp1;
                _result.fp2 = fp2;
                _result.xm1 = xm1;
                _result.xp1 = xp1;
                _result.gm1 = gm1;
                _result.gp1 = gp1;
                _result.cidx = cidx;
                _result.cval = cval;
                _result.tmpprec = (double[])tmpprec.Clone();
                _result.nfev = nfev;
                _result.mcstage = mcstage;
                _result.stp = stp;
                _result.curstpmax = curstpmax;
                _result.activationstep = activationstep;
                _result.work = (double[])work.Clone();
                _result.lstate = (linmin.linminstate)lstate.make_copy();
                _result.trimthreshold = trimthreshold;
                _result.nonmonotoniccnt = nonmonotoniccnt;
                _result.k = k;
                _result.q = q;
                _result.p = p;
                _result.rho = (double[])rho.Clone();
                _result.yk = (double[,])yk.Clone();
                _result.sk = (double[,])sk.Clone();
                _result.theta = (double[])theta.Clone();
                return _result;
            }
        };


        /*************************************************************************
        This structure stores optimization report:
        * IterationsCount           number of iterations
        * NFEV                      number of gradient evaluations
        * TerminationType           termination type (see below)

        TERMINATION CODES

        TerminationType field contains completion code, which can be:
          -7    gradient verification failed.
                See MinBLEICSetGradientCheck() for more information.
          -3    inconsistent constraints. Feasible point is
                either nonexistent or too hard to find. Try to
                restart optimizer with better initial approximation
           1    relative function improvement is no more than EpsF.
           2    relative step is no more than EpsX.
           4    gradient norm is no more than EpsG
           5    MaxIts steps was taken
           7    stopping conditions are too stringent,
                further improvement is impossible,
                X contains best point found so far.

        ADDITIONAL FIELDS

        There are additional fields which can be used for debugging:
        * DebugEqErr                error in the equality constraints (2-norm)
        * DebugFS                   f, calculated at projection of initial point
                                    to the feasible set
        * DebugFF                   f, calculated at the final point
        * DebugDX                   |X_start-X_final|
        *************************************************************************/
        public class minbleicreport : apobject
        {
            public int iterationscount;
            public int nfev;
            public int varidx;
            public int terminationtype;
            public double debugeqerr;
            public double debugfs;
            public double debugff;
            public double debugdx;
            public int debugfeasqpits;
            public int debugfeasgpaits;
            public int inneriterationscount;
            public int outeriterationscount;
            public minbleicreport()
            {
                init();
            }
            public override void init()
            {
            }
            public override alglib.apobject make_copy()
            {
                minbleicreport _result = new minbleicreport();
                _result.iterationscount = iterationscount;
                _result.nfev = nfev;
                _result.varidx = varidx;
                _result.terminationtype = terminationtype;
                _result.debugeqerr = debugeqerr;
                _result.debugfs = debugfs;
                _result.debugff = debugff;
                _result.debugdx = debugdx;
                _result.debugfeasqpits = debugfeasqpits;
                _result.debugfeasgpaits = debugfeasgpaits;
                _result.inneriterationscount = inneriterationscount;
                _result.outeriterationscount = outeriterationscount;
                return _result;
            }
        };




        public const double gtol = 0.4;
        public const double maxnonmonotoniclen = 1.0E6;
        public const double initialdecay = 0.5;
        public const double mindecay = 0.01;
        public const double decaycorrection = 0.8;


        /*************************************************************************
                             BOUND CONSTRAINED OPTIMIZATION
               WITH ADDITIONAL LINEAR EQUALITY AND INEQUALITY CONSTRAINTS

        DESCRIPTION:
        The  subroutine  minimizes  function   F(x)  of N arguments subject to any
        combination of:
        * bound constraints
        * linear inequality constraints
        * linear equality constraints

        REQUIREMENTS:
        * user must provide function value and gradient
        * starting point X0 must be feasible or
          not too far away from the feasible set
        * grad(f) must be Lipschitz continuous on a level set:
          L = { x : f(x)<=f(x0) }
        * function must be defined everywhere on the feasible set F

        USAGE:

        Constrained optimization if far more complex than the unconstrained one.
        Here we give very brief outline of the BLEIC optimizer. We strongly recommend
        you to read examples in the ALGLIB Reference Manual and to read ALGLIB User Guide
        on optimization, which is available at http://www.alglib.net/optimization/

        1. User initializes algorithm state with MinBLEICCreate() call

        2. USer adds boundary and/or linear constraints by calling
           MinBLEICSetBC() and MinBLEICSetLC() functions.

        3. User sets stopping conditions with MinBLEICSetCond().

        4. User calls MinBLEICOptimize() function which takes algorithm  state and
           pointer (delegate, etc.) to callback function which calculates F/G.

        5. User calls MinBLEICResults() to get solution

        6. Optionally user may call MinBLEICRestartFrom() to solve another problem
           with same N but another starting point.
           MinBLEICRestartFrom() allows to reuse already initialized structure.


        INPUT PARAMETERS:
            N       -   problem dimension, N>0:
                        * if given, only leading N elements of X are used
                        * if not given, automatically determined from size ofX
            X       -   starting point, array[N]:
                        * it is better to set X to a feasible point
                        * but X can be infeasible, in which case algorithm will try
                          to find feasible point first, using X as initial
                          approximation.

        OUTPUT PARAMETERS:
            State   -   structure stores algorithm state

          -- ALGLIB --
             Copyright 28.11.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void minbleiccreate(int n,
            double[] x,
            minbleicstate state)
        {
            double[,] c = new double[0,0];
            int[] ct = new int[0];

            alglib.ap.assert(n>=1, "MinBLEICCreate: N<1");
            alglib.ap.assert(alglib.ap.len(x)>=n, "MinBLEICCreate: Length(X)<N");
            alglib.ap.assert(apserv.isfinitevector(x, n), "MinBLEICCreate: X contains infinite or NaN values!");
            minbleicinitinternal(n, x, 0.0, state);
        }


        /*************************************************************************
        The subroutine is finite difference variant of MinBLEICCreate().  It  uses
        finite differences in order to differentiate target function.

        Description below contains information which is specific to  this function
        only. We recommend to read comments on MinBLEICCreate() in  order  to  get
        more information about creation of BLEIC optimizer.

        INPUT PARAMETERS:
            N       -   problem dimension, N>0:
                        * if given, only leading N elements of X are used
                        * if not given, automatically determined from size of X
            X       -   starting point, array[0..N-1].
            DiffStep-   differentiation step, >0

        OUTPUT PARAMETERS:
            State   -   structure which stores algorithm state

        NOTES:
        1. algorithm uses 4-point central formula for differentiation.
        2. differentiation step along I-th axis is equal to DiffStep*S[I] where
           S[] is scaling vector which can be set by MinBLEICSetScale() call.
        3. we recommend you to use moderate values of  differentiation  step.  Too
           large step will result in too large truncation  errors, while too small
           step will result in too large numerical  errors.  1.0E-6  can  be  good
           value to start with.
        4. Numerical  differentiation  is   very   inefficient  -   one   gradient
           calculation needs 4*N function evaluations. This function will work for
           any N - either small (1...10), moderate (10...100) or  large  (100...).
           However, performance penalty will be too severe for any N's except  for
           small ones.
           We should also say that code which relies on numerical  differentiation
           is  less  robust and precise. CG needs exact gradient values. Imprecise
           gradient may slow  down  convergence, especially  on  highly  nonlinear
           problems.
           Thus  we  recommend to use this function for fast prototyping on small-
           dimensional problems only, and to implement analytical gradient as soon
           as possible.

          -- ALGLIB --
             Copyright 16.05.2011 by Bochkanov Sergey
        *************************************************************************/
        public static void minbleiccreatef(int n,
            double[] x,
            double diffstep,
            minbleicstate state)
        {
            double[,] c = new double[0,0];
            int[] ct = new int[0];

            alglib.ap.assert(n>=1, "MinBLEICCreateF: N<1");
            alglib.ap.assert(alglib.ap.len(x)>=n, "MinBLEICCreateF: Length(X)<N");
            alglib.ap.assert(apserv.isfinitevector(x, n), "MinBLEICCreateF: X contains infinite or NaN values!");
            alglib.ap.assert(math.isfinite(diffstep), "MinBLEICCreateF: DiffStep is infinite or NaN!");
            alglib.ap.assert((double)(diffstep)>(double)(0), "MinBLEICCreateF: DiffStep is non-positive!");
            minbleicinitinternal(n, x, diffstep, state);
        }


        /*************************************************************************
        This function sets boundary constraints for BLEIC optimizer.

        Boundary constraints are inactive by default (after initial creation).
        They are preserved after algorithm restart with MinBLEICRestartFrom().

        INPUT PARAMETERS:
            State   -   structure stores algorithm state
            BndL    -   lower bounds, array[N].
                        If some (all) variables are unbounded, you may specify
                        very small number or -INF.
            BndU    -   upper bounds, array[N].
                        If some (all) variables are unbounded, you may specify
                        very large number or +INF.

        NOTE 1: it is possible to specify BndL[i]=BndU[i]. In this case I-th
        variable will be "frozen" at X[i]=BndL[i]=BndU[i].

        NOTE 2: this solver has following useful properties:
        * bound constraints are always satisfied exactly
        * function is evaluated only INSIDE area specified by  bound  constraints,
          even  when  numerical  differentiation is used (algorithm adjusts  nodes
          according to boundary constraints)

          -- ALGLIB --
             Copyright 28.11.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void minbleicsetbc(minbleicstate state,
            double[] bndl,
            double[] bndu)
        {
            int i = 0;
            int n = 0;

            n = state.nmain;
            alglib.ap.assert(alglib.ap.len(bndl)>=n, "MinBLEICSetBC: Length(BndL)<N");
            alglib.ap.assert(alglib.ap.len(bndu)>=n, "MinBLEICSetBC: Length(BndU)<N");
            for(i=0; i<=n-1; i++)
            {
                alglib.ap.assert(math.isfinite(bndl[i]) || Double.IsNegativeInfinity(bndl[i]), "MinBLEICSetBC: BndL contains NAN or +INF");
                alglib.ap.assert(math.isfinite(bndu[i]) || Double.IsPositiveInfinity(bndu[i]), "MinBLEICSetBC: BndL contains NAN or -INF");
                state.bndl[i] = bndl[i];
                state.hasbndl[i] = math.isfinite(bndl[i]);
                state.bndu[i] = bndu[i];
                state.hasbndu[i] = math.isfinite(bndu[i]);
            }
            sactivesets.sassetbc(state.sas, bndl, bndu);
        }


        /*************************************************************************
        This function sets linear constraints for BLEIC optimizer.

        Linear constraints are inactive by default (after initial creation).
        They are preserved after algorithm restart with MinBLEICRestartFrom().

        INPUT PARAMETERS:
            State   -   structure previously allocated with MinBLEICCreate call.
            C       -   linear constraints, array[K,N+1].
                        Each row of C represents one constraint, either equality
                        or inequality (see below):
                        * first N elements correspond to coefficients,
                        * last element corresponds to the right part.
                        All elements of C (including right part) must be finite.
            CT      -   type of constraints, array[K]:
                        * if CT[i]>0, then I-th constraint is C[i,*]*x >= C[i,n+1]
                        * if CT[i]=0, then I-th constraint is C[i,*]*x  = C[i,n+1]
                        * if CT[i]<0, then I-th constraint is C[i,*]*x <= C[i,n+1]
            K       -   number of equality/inequality constraints, K>=0:
                        * if given, only leading K elements of C/CT are used
                        * if not given, automatically determined from sizes of C/CT

        NOTE 1: linear (non-bound) constraints are satisfied only approximately:
        * there always exists some minor violation (about Epsilon in magnitude)
          due to rounding errors
        * numerical differentiation, if used, may  lead  to  function  evaluations
          outside  of the feasible  area,   because   algorithm  does  NOT  change
          numerical differentiation formula according to linear constraints.
        If you want constraints to be  satisfied  exactly, try to reformulate your
        problem  in  such  manner  that  all constraints will become boundary ones
        (this kind of constraints is always satisfied exactly, both in  the  final
        solution and in all intermediate points).

          -- ALGLIB --
             Copyright 28.11.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void minbleicsetlc(minbleicstate state,
            double[,] c,
            int[] ct,
            int k)
        {
            int n = 0;
            int i = 0;
            int j = 0;
            double v = 0;
            int i_ = 0;

            n = state.nmain;
            
            //
            // First, check for errors in the inputs
            //
            alglib.ap.assert(k>=0, "MinBLEICSetLC: K<0");
            alglib.ap.assert(alglib.ap.cols(c)>=n+1 || k==0, "MinBLEICSetLC: Cols(C)<N+1");
            alglib.ap.assert(alglib.ap.rows(c)>=k, "MinBLEICSetLC: Rows(C)<K");
            alglib.ap.assert(alglib.ap.len(ct)>=k, "MinBLEICSetLC: Length(CT)<K");
            alglib.ap.assert(apserv.apservisfinitematrix(c, k, n+1), "MinBLEICSetLC: C contains infinite or NaN values!");
            
            //
            // Handle zero K
            //
            if( k==0 )
            {
                state.nec = 0;
                state.nic = 0;
                return;
            }
            
            //
            // Equality constraints are stored first, in the upper
            // NEC rows of State.CLEIC matrix. Inequality constraints
            // are stored in the next NIC rows.
            //
            // NOTE: we convert inequality constraints to the form
            // A*x<=b before copying them.
            //
            apserv.rmatrixsetlengthatleast(ref state.cleic, k, n+1);
            state.nec = 0;
            state.nic = 0;
            for(i=0; i<=k-1; i++)
            {
                if( ct[i]==0 )
                {
                    for(i_=0; i_<=n;i_++)
                    {
                        state.cleic[state.nec,i_] = c[i,i_];
                    }
                    state.nec = state.nec+1;
                }
            }
            for(i=0; i<=k-1; i++)
            {
                if( ct[i]!=0 )
                {
                    if( ct[i]>0 )
                    {
                        for(i_=0; i_<=n;i_++)
                        {
                            state.cleic[state.nec+state.nic,i_] = -c[i,i_];
                        }
                    }
                    else
                    {
                        for(i_=0; i_<=n;i_++)
                        {
                            state.cleic[state.nec+state.nic,i_] = c[i,i_];
                        }
                    }
                    state.nic = state.nic+1;
                }
            }
            
            //
            // Normalize rows of State.CLEIC: each row must have unit norm.
            // Norm is calculated using first N elements (i.e. right part is
            // not counted when we calculate norm).
            //
            for(i=0; i<=k-1; i++)
            {
                v = 0;
                for(j=0; j<=n-1; j++)
                {
                    v = v+math.sqr(state.cleic[i,j]);
                }
                if( (double)(v)==(double)(0) )
                {
                    continue;
                }
                v = 1/Math.Sqrt(v);
                for(i_=0; i_<=n;i_++)
                {
                    state.cleic[i,i_] = v*state.cleic[i,i_];
                }
            }
            sactivesets.sassetlc(state.sas, c, ct, k);
        }


        /*************************************************************************
        This function sets stopping conditions for the optimizer.

        INPUT PARAMETERS:
            State   -   structure which stores algorithm state
            EpsG    -   >=0
                        The  subroutine  finishes  its  work   if   the  condition
                        |v|<EpsG is satisfied, where:
                        * |.| means Euclidian norm
                        * v - scaled gradient vector, v[i]=g[i]*s[i]
                        * g - gradient
                        * s - scaling coefficients set by MinBLEICSetScale()
            EpsF    -   >=0
                        The  subroutine  finishes  its work if on k+1-th iteration
                        the  condition  |F(k+1)-F(k)|<=EpsF*max{|F(k)|,|F(k+1)|,1}
                        is satisfied.
            EpsX    -   >=0
                        The subroutine finishes its work if  on  k+1-th  iteration
                        the condition |v|<=EpsX is fulfilled, where:
                        * |.| means Euclidian norm
                        * v - scaled step vector, v[i]=dx[i]/s[i]
                        * dx - ste pvector, dx=X(k+1)-X(k)
                        * s - scaling coefficients set by MinBLEICSetScale()
            MaxIts  -   maximum number of iterations. If MaxIts=0, the  number  of
                        iterations is unlimited.

        Passing EpsG=0, EpsF=0 and EpsX=0 and MaxIts=0 (simultaneously) will lead
        to automatic stopping criterion selection.

          -- ALGLIB --
             Copyright 28.11.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void minbleicsetcond(minbleicstate state,
            double epsg,
            double epsf,
            double epsx,
            int maxits)
        {
            alglib.ap.assert(math.isfinite(epsg), "MinBLEICSetCond: EpsG is not finite number");
            alglib.ap.assert((double)(epsg)>=(double)(0), "MinBLEICSetCond: negative EpsG");
            alglib.ap.assert(math.isfinite(epsf), "MinBLEICSetCond: EpsF is not finite number");
            alglib.ap.assert((double)(epsf)>=(double)(0), "MinBLEICSetCond: negative EpsF");
            alglib.ap.assert(math.isfinite(epsx), "MinBLEICSetCond: EpsX is not finite number");
            alglib.ap.assert((double)(epsx)>=(double)(0), "MinBLEICSetCond: negative EpsX");
            alglib.ap.assert(maxits>=0, "MinBLEICSetCond: negative MaxIts!");
            if( (((double)(epsg)==(double)(0) && (double)(epsf)==(double)(0)) && (double)(epsx)==(double)(0)) && maxits==0 )
            {
                epsx = 1.0E-6;
            }
            state.epsg = epsg;
            state.epsf = epsf;
            state.epsx = epsx;
            state.maxits = maxits;
        }


        /*************************************************************************
        This function sets scaling coefficients for BLEIC optimizer.

        ALGLIB optimizers use scaling matrices to test stopping  conditions  (step
        size and gradient are scaled before comparison with tolerances).  Scale of
        the I-th variable is a translation invariant measure of:
        a) "how large" the variable is
        b) how large the step should be to make significant changes in the function

        Scaling is also used by finite difference variant of the optimizer  - step
        along I-th axis is equal to DiffStep*S[I].

        In  most  optimizers  (and  in  the  BLEIC  too)  scaling is NOT a form of
        preconditioning. It just  affects  stopping  conditions.  You  should  set
        preconditioner  by  separate  call  to  one  of  the  MinBLEICSetPrec...()
        functions.

        There is a special  preconditioning  mode, however,  which  uses   scaling
        coefficients to form diagonal preconditioning matrix. You  can  turn  this
        mode on, if you want.   But  you should understand that scaling is not the
        same thing as preconditioning - these are two different, although  related
        forms of tuning solver.

        INPUT PARAMETERS:
            State   -   structure stores algorithm state
            S       -   array[N], non-zero scaling coefficients
                        S[i] may be negative, sign doesn't matter.

          -- ALGLIB --
             Copyright 14.01.2011 by Bochkanov Sergey
        *************************************************************************/
        public static void minbleicsetscale(minbleicstate state,
            double[] s)
        {
            int i = 0;

            alglib.ap.assert(alglib.ap.len(s)>=state.nmain, "MinBLEICSetScale: Length(S)<N");
            for(i=0; i<=state.nmain-1; i++)
            {
                alglib.ap.assert(math.isfinite(s[i]), "MinBLEICSetScale: S contains infinite or NAN elements");
                alglib.ap.assert((double)(s[i])!=(double)(0), "MinBLEICSetScale: S contains zero elements");
                state.s[i] = Math.Abs(s[i]);
            }
            sactivesets.sassetscale(state.sas, s);
        }


        /*************************************************************************
        Modification of the preconditioner: preconditioning is turned off.

        INPUT PARAMETERS:
            State   -   structure which stores algorithm state

          -- ALGLIB --
             Copyright 13.10.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void minbleicsetprecdefault(minbleicstate state)
        {
            state.prectype = 0;
        }


        /*************************************************************************
        Modification  of  the  preconditioner:  diagonal of approximate Hessian is
        used.

        INPUT PARAMETERS:
            State   -   structure which stores algorithm state
            D       -   diagonal of the approximate Hessian, array[0..N-1],
                        (if larger, only leading N elements are used).

        NOTE 1: D[i] should be positive. Exception will be thrown otherwise.

        NOTE 2: you should pass diagonal of approximate Hessian - NOT ITS INVERSE.

          -- ALGLIB --
             Copyright 13.10.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void minbleicsetprecdiag(minbleicstate state,
            double[] d)
        {
            int i = 0;

            alglib.ap.assert(alglib.ap.len(d)>=state.nmain, "MinBLEICSetPrecDiag: D is too short");
            for(i=0; i<=state.nmain-1; i++)
            {
                alglib.ap.assert(math.isfinite(d[i]), "MinBLEICSetPrecDiag: D contains infinite or NAN elements");
                alglib.ap.assert((double)(d[i])>(double)(0), "MinBLEICSetPrecDiag: D contains non-positive elements");
            }
            apserv.rvectorsetlengthatleast(ref state.diagh, state.nmain);
            state.prectype = 2;
            for(i=0; i<=state.nmain-1; i++)
            {
                state.diagh[i] = d[i];
            }
        }


        /*************************************************************************
        Modification of the preconditioner: scale-based diagonal preconditioning.

        This preconditioning mode can be useful when you  don't  have  approximate
        diagonal of Hessian, but you know that your  variables  are  badly  scaled
        (for  example,  one  variable is in [1,10], and another in [1000,100000]),
        and most part of the ill-conditioning comes from different scales of vars.

        In this case simple  scale-based  preconditioner,  with H[i] = 1/(s[i]^2),
        can greatly improve convergence.

        IMPRTANT: you should set scale of your variables  with  MinBLEICSetScale()
        call  (before  or after MinBLEICSetPrecScale() call). Without knowledge of
        the scale of your variables scale-based preconditioner will be  just  unit
        matrix.

        INPUT PARAMETERS:
            State   -   structure which stores algorithm state

          -- ALGLIB --
             Copyright 13.10.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void minbleicsetprecscale(minbleicstate state)
        {
            state.prectype = 3;
        }


        /*************************************************************************
        This function turns on/off reporting.

        INPUT PARAMETERS:
            State   -   structure which stores algorithm state
            NeedXRep-   whether iteration reports are needed or not

        If NeedXRep is True, algorithm will call rep() callback function if  it is
        provided to MinBLEICOptimize().

          -- ALGLIB --
             Copyright 28.11.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void minbleicsetxrep(minbleicstate state,
            bool needxrep)
        {
            state.xrep = needxrep;
        }


        /*************************************************************************
        This function sets maximum step length

        IMPORTANT: this feature is hard to combine with preconditioning. You can't
        set upper limit on step length, when you solve optimization  problem  with
        linear (non-boundary) constraints AND preconditioner turned on.

        When  non-boundary  constraints  are  present,  you  have to either a) use
        preconditioner, or b) use upper limit on step length.  YOU CAN'T USE BOTH!
        In this case algorithm will terminate with appropriate error code.

        INPUT PARAMETERS:
            State   -   structure which stores algorithm state
            StpMax  -   maximum step length, >=0. Set StpMax to 0.0,  if you don't
                        want to limit step length.

        Use this subroutine when you optimize target function which contains exp()
        or  other  fast  growing  functions,  and optimization algorithm makes too
        large  steps  which  lead   to overflow. This function allows us to reject
        steps  that  are  too  large  (and  therefore  expose  us  to the possible
        overflow) without actually calculating function value at the x+stp*d.

          -- ALGLIB --
             Copyright 02.04.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void minbleicsetstpmax(minbleicstate state,
            double stpmax)
        {
            alglib.ap.assert(math.isfinite(stpmax), "MinBLEICSetStpMax: StpMax is not finite!");
            alglib.ap.assert((double)(stpmax)>=(double)(0), "MinBLEICSetStpMax: StpMax<0!");
            state.stpmax = stpmax;
        }


        /*************************************************************************
        NOTES:

        1. This function has two different implementations: one which  uses  exact
           (analytical) user-supplied gradient,  and one which uses function value
           only  and  numerically  differentiates  function  in  order  to  obtain
           gradient.

           Depending  on  the  specific  function  used to create optimizer object
           (either  MinBLEICCreate() for analytical gradient or  MinBLEICCreateF()
           for numerical differentiation) you should choose appropriate variant of
           MinBLEICOptimize() - one  which  accepts  function  AND gradient or one
           which accepts function ONLY.

           Be careful to choose variant of MinBLEICOptimize() which corresponds to
           your optimization scheme! Table below lists different  combinations  of
           callback (function/gradient) passed to MinBLEICOptimize()  and specific
           function used to create optimizer.


                             |         USER PASSED TO MinBLEICOptimize()
           CREATED WITH      |  function only   |  function and gradient
           ------------------------------------------------------------
           MinBLEICCreateF() |     work                FAIL
           MinBLEICCreate()  |     FAIL                work

           Here "FAIL" denotes inappropriate combinations  of  optimizer  creation
           function  and  MinBLEICOptimize()  version.   Attemps   to   use   such
           combination (for  example,  to  create optimizer with MinBLEICCreateF()
           and  to  pass  gradient  information  to  MinCGOptimize()) will lead to
           exception being thrown. Either  you  did  not pass gradient when it WAS
           needed or you passed gradient when it was NOT needed.

          -- ALGLIB --
             Copyright 28.11.2010 by Bochkanov Sergey
        *************************************************************************/
        public static bool minbleiciteration(minbleicstate state)
        {
            bool result = new bool();
            int n = 0;
            int m = 0;
            int i = 0;
            int j = 0;
            double v = 0;
            double vv = 0;
            int badbfgsits = 0;
            bool b = new bool();
            int nextaction = 0;
            int actstatus = 0;
            int mcinfo = 0;
            int ic = 0;
            double penalty = 0;
            double ginit = 0;
            double gdecay = 0;
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
                m = state.rstate.ia[1];
                i = state.rstate.ia[2];
                j = state.rstate.ia[3];
                badbfgsits = state.rstate.ia[4];
                nextaction = state.rstate.ia[5];
                actstatus = state.rstate.ia[6];
                mcinfo = state.rstate.ia[7];
                ic = state.rstate.ia[8];
                b = state.rstate.ba[0];
                v = state.rstate.ra[0];
                vv = state.rstate.ra[1];
                penalty = state.rstate.ra[2];
                ginit = state.rstate.ra[3];
                gdecay = state.rstate.ra[4];
            }
            else
            {
                n = -983;
                m = -989;
                i = -834;
                j = 900;
                badbfgsits = -287;
                nextaction = 364;
                actstatus = 214;
                mcinfo = -338;
                ic = -686;
                b = false;
                v = 585;
                vv = 497;
                penalty = -271;
                ginit = -581;
                gdecay = 745;
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
            if( state.rstate.stage==4 )
            {
                goto lbl_4;
            }
            if( state.rstate.stage==5 )
            {
                goto lbl_5;
            }
            if( state.rstate.stage==6 )
            {
                goto lbl_6;
            }
            if( state.rstate.stage==7 )
            {
                goto lbl_7;
            }
            if( state.rstate.stage==8 )
            {
                goto lbl_8;
            }
            if( state.rstate.stage==9 )
            {
                goto lbl_9;
            }
            if( state.rstate.stage==10 )
            {
                goto lbl_10;
            }
            if( state.rstate.stage==11 )
            {
                goto lbl_11;
            }
            if( state.rstate.stage==12 )
            {
                goto lbl_12;
            }
            if( state.rstate.stage==13 )
            {
                goto lbl_13;
            }
            if( state.rstate.stage==14 )
            {
                goto lbl_14;
            }
            if( state.rstate.stage==15 )
            {
                goto lbl_15;
            }
            if( state.rstate.stage==16 )
            {
                goto lbl_16;
            }
            if( state.rstate.stage==17 )
            {
                goto lbl_17;
            }
            if( state.rstate.stage==18 )
            {
                goto lbl_18;
            }
            if( state.rstate.stage==19 )
            {
                goto lbl_19;
            }
            if( state.rstate.stage==20 )
            {
                goto lbl_20;
            }
            if( state.rstate.stage==21 )
            {
                goto lbl_21;
            }
            if( state.rstate.stage==22 )
            {
                goto lbl_22;
            }
            if( state.rstate.stage==23 )
            {
                goto lbl_23;
            }
            if( state.rstate.stage==24 )
            {
                goto lbl_24;
            }
            if( state.rstate.stage==25 )
            {
                goto lbl_25;
            }
            if( state.rstate.stage==26 )
            {
                goto lbl_26;
            }
            if( state.rstate.stage==27 )
            {
                goto lbl_27;
            }
            if( state.rstate.stage==28 )
            {
                goto lbl_28;
            }
            if( state.rstate.stage==29 )
            {
                goto lbl_29;
            }
            if( state.rstate.stage==30 )
            {
                goto lbl_30;
            }
            if( state.rstate.stage==31 )
            {
                goto lbl_31;
            }
            if( state.rstate.stage==32 )
            {
                goto lbl_32;
            }
            if( state.rstate.stage==33 )
            {
                goto lbl_33;
            }
            if( state.rstate.stage==34 )
            {
                goto lbl_34;
            }
            if( state.rstate.stage==35 )
            {
                goto lbl_35;
            }
            if( state.rstate.stage==36 )
            {
                goto lbl_36;
            }
            if( state.rstate.stage==37 )
            {
                goto lbl_37;
            }
            if( state.rstate.stage==38 )
            {
                goto lbl_38;
            }
            if( state.rstate.stage==39 )
            {
                goto lbl_39;
            }
            
            //
            // Routine body
            //
            
            //
            // Algorithm parameters:
            // * M          number of L-BFGS corrections.
            //              This coefficient remains fixed during iterations.
            // * GDecay     desired decrease of constrained gradient during L-BFGS iterations.
            //              This coefficient is decreased after each L-BFGS round until
            //              it reaches minimum decay.
            //
            m = 5;
            gdecay = initialdecay;
            
            //
            // Init
            //
            n = state.nmain;
            state.repterminationtype = 0;
            state.repinneriterationscount = 0;
            state.repouteriterationscount = 0;
            state.repnfev = 0;
            state.repvaridx = -1;
            state.repdebugeqerr = 0.0;
            state.repdebugfs = Double.NaN;
            state.repdebugff = Double.NaN;
            state.repdebugdx = Double.NaN;
            if( (double)(state.stpmax)!=(double)(0) && state.prectype!=0 )
            {
                state.repterminationtype = -10;
                result = false;
                return result;
            }
            apserv.rvectorsetlengthatleast(ref state.rho, m);
            apserv.rvectorsetlengthatleast(ref state.theta, m);
            apserv.rmatrixsetlengthatleast(ref state.yk, m, n);
            apserv.rmatrixsetlengthatleast(ref state.sk, m, n);
            
            //
            // Fill TmpPrec with current preconditioner
            //
            apserv.rvectorsetlengthatleast(ref state.tmpprec, n);
            for(i=0; i<=n-1; i++)
            {
                if( state.prectype==2 )
                {
                    state.tmpprec[i] = state.diagh[i];
                    continue;
                }
                if( state.prectype==3 )
                {
                    state.tmpprec[i] = 1/math.sqr(state.s[i]);
                    continue;
                }
                state.tmpprec[i] = 1;
            }
            sactivesets.sassetprecdiag(state.sas, state.tmpprec);
            
            //
            // Start optimization
            //
            if( !sactivesets.sasstartoptimization(state.sas, state.xstart) )
            {
                state.repterminationtype = -3;
                result = false;
                return result;
            }
            
            //
            //  Check correctness of user-supplied gradient
            //
            if( !((double)(state.diffstep)==(double)(0) && (double)(state.teststep)>(double)(0)) )
            {
                goto lbl_40;
            }
            clearrequestfields(state);
            for(i_=0; i_<=n-1;i_++)
            {
                state.x[i_] = state.sas.xc[i_];
            }
            state.needfg = true;
            i = 0;
        lbl_42:
            if( i>n-1 )
            {
                goto lbl_44;
            }
            alglib.ap.assert(!state.hasbndl[i] || (double)(state.sas.xc[i])>=(double)(state.bndl[i]), "MinBLEICIteration: internal error(State.X is out of bounds)");
            alglib.ap.assert(!state.hasbndu[i] || (double)(state.sas.xc[i])<=(double)(state.bndu[i]), "MinBLEICIteration: internal error(State.X is out of bounds)");
            v = state.x[i];
            state.x[i] = v-state.teststep*state.s[i];
            if( state.hasbndl[i] )
            {
                state.x[i] = Math.Max(state.x[i], state.bndl[i]);
            }
            state.xm1 = state.x[i];
            state.rstate.stage = 0;
            goto lbl_rcomm;
        lbl_0:
            state.fm1 = state.f;
            state.gm1 = state.g[i];
            state.x[i] = v+state.teststep*state.s[i];
            if( state.hasbndu[i] )
            {
                state.x[i] = Math.Min(state.x[i], state.bndu[i]);
            }
            state.xp1 = state.x[i];
            state.rstate.stage = 1;
            goto lbl_rcomm;
        lbl_1:
            state.fp1 = state.f;
            state.gp1 = state.g[i];
            state.x[i] = (state.xm1+state.xp1)/2;
            if( state.hasbndl[i] )
            {
                state.x[i] = Math.Max(state.x[i], state.bndl[i]);
            }
            if( state.hasbndu[i] )
            {
                state.x[i] = Math.Min(state.x[i], state.bndu[i]);
            }
            state.rstate.stage = 2;
            goto lbl_rcomm;
        lbl_2:
            state.x[i] = v;
            if( !optserv.derivativecheck(state.fm1, state.gm1, state.fp1, state.gp1, state.f, state.g[i], state.xp1-state.xm1) )
            {
                state.repvaridx = i;
                state.repterminationtype = -7;
                sactivesets.sasstopoptimization(state.sas);
                result = false;
                return result;
            }
            i = i+1;
            goto lbl_42;
        lbl_44:
            state.needfg = false;
        lbl_40:
            
            //
            // Main cycle of BLEIC-PG algorithm
            //
            state.repterminationtype = 4;
            badbfgsits = 0;
            state.lastgoodstep = 0;
            state.lastscaledgoodstep = 0;
            state.nonmonotoniccnt = n+state.nic;
            for(i_=0; i_<=n-1;i_++)
            {
                state.x[i_] = state.sas.xc[i_];
            }
            clearrequestfields(state);
            if( (double)(state.diffstep)!=(double)(0) )
            {
                goto lbl_45;
            }
            state.needfg = true;
            state.rstate.stage = 3;
            goto lbl_rcomm;
        lbl_3:
            state.needfg = false;
            goto lbl_46;
        lbl_45:
            state.needf = true;
            state.rstate.stage = 4;
            goto lbl_rcomm;
        lbl_4:
            state.needf = false;
        lbl_46:
            state.fc = state.f;
            optserv.trimprepare(state.f, ref state.trimthreshold);
            state.repnfev = state.repnfev+1;
            if( !state.xrep )
            {
                goto lbl_47;
            }
            
            //
            // Report current point
            //
            for(i_=0; i_<=n-1;i_++)
            {
                state.x[i_] = state.sas.xc[i_];
            }
            state.f = state.fc;
            state.xupdated = true;
            state.rstate.stage = 5;
            goto lbl_rcomm;
        lbl_5:
            state.xupdated = false;
        lbl_47:
        lbl_49:
            if( false )
            {
                goto lbl_50;
            }
            
            //
            // Phase 1
            //
            // (a) calculate unconstrained gradient
            // (b) determine active set
            //
            for(i_=0; i_<=n-1;i_++)
            {
                state.x[i_] = state.sas.xc[i_];
            }
            clearrequestfields(state);
            if( (double)(state.diffstep)!=(double)(0) )
            {
                goto lbl_51;
            }
            
            //
            // Analytic gradient
            //
            state.needfg = true;
            state.rstate.stage = 6;
            goto lbl_rcomm;
        lbl_6:
            state.needfg = false;
            goto lbl_52;
        lbl_51:
            
            //
            // Numerical differentiation
            //
            state.needf = true;
            state.rstate.stage = 7;
            goto lbl_rcomm;
        lbl_7:
            state.fbase = state.f;
            i = 0;
        lbl_53:
            if( i>n-1 )
            {
                goto lbl_55;
            }
            v = state.x[i];
            b = false;
            if( state.hasbndl[i] )
            {
                b = b || (double)(v-state.diffstep*state.s[i])<(double)(state.bndl[i]);
            }
            if( state.hasbndu[i] )
            {
                b = b || (double)(v+state.diffstep*state.s[i])>(double)(state.bndu[i]);
            }
            if( b )
            {
                goto lbl_56;
            }
            state.x[i] = v-state.diffstep*state.s[i];
            state.rstate.stage = 8;
            goto lbl_rcomm;
        lbl_8:
            state.fm2 = state.f;
            state.x[i] = v-0.5*state.diffstep*state.s[i];
            state.rstate.stage = 9;
            goto lbl_rcomm;
        lbl_9:
            state.fm1 = state.f;
            state.x[i] = v+0.5*state.diffstep*state.s[i];
            state.rstate.stage = 10;
            goto lbl_rcomm;
        lbl_10:
            state.fp1 = state.f;
            state.x[i] = v+state.diffstep*state.s[i];
            state.rstate.stage = 11;
            goto lbl_rcomm;
        lbl_11:
            state.fp2 = state.f;
            state.g[i] = (8*(state.fp1-state.fm1)-(state.fp2-state.fm2))/(6*state.diffstep*state.s[i]);
            goto lbl_57;
        lbl_56:
            state.xm1 = v-state.diffstep*state.s[i];
            state.xp1 = v+state.diffstep*state.s[i];
            if( state.hasbndl[i] && (double)(state.xm1)<(double)(state.bndl[i]) )
            {
                state.xm1 = state.bndl[i];
            }
            if( state.hasbndu[i] && (double)(state.xp1)>(double)(state.bndu[i]) )
            {
                state.xp1 = state.bndu[i];
            }
            state.x[i] = state.xm1;
            state.rstate.stage = 12;
            goto lbl_rcomm;
        lbl_12:
            state.fm1 = state.f;
            state.x[i] = state.xp1;
            state.rstate.stage = 13;
            goto lbl_rcomm;
        lbl_13:
            state.fp1 = state.f;
            if( (double)(state.xm1)!=(double)(state.xp1) )
            {
                state.g[i] = (state.fp1-state.fm1)/(state.xp1-state.xm1);
            }
            else
            {
                state.g[i] = 0;
            }
        lbl_57:
            state.x[i] = v;
            i = i+1;
            goto lbl_53;
        lbl_55:
            state.f = state.fbase;
            state.needf = false;
        lbl_52:
            state.fc = state.f;
            for(i_=0; i_<=n-1;i_++)
            {
                state.gc[i_] = state.g[i_];
            }
            sactivesets.sasreactivateconstraintsprec(state.sas, state.gc);
            
            //
            // Phase 2: perform steepest descent step.
            //
            // NextAction control variable is set on exit from this loop:
            // * NextAction>0 in case we have to proceed to Phase 3 (L-BFGS step)
            // * NextAction<0 in case we have to proceed to Phase 1 (recalculate active set)
            // * NextAction=0 in case we found solution (step size or function change are small enough)
            //
            nextaction = 0;
        lbl_58:
            if( false )
            {
                goto lbl_59;
            }
            
            //
            // Check gradient-based stopping criteria
            //
            if( (double)(sactivesets.sasscaledconstrainednorm(state.sas, state.gc))<=(double)(state.epsg) )
            {
                
                //
                // Gradient is small enough, stop iterations
                //
                state.repterminationtype = 4;
                nextaction = 0;
                goto lbl_59;
            }
            
            //
            // Calculate normalized constrained descent direction, store to D.
            // Try to use previous scaled step length as initial estimate for new step.
            //
            // NOTE: D can be exactly zero, in this case Stp is set to 1.0
            //
            sactivesets.sasconstraineddescentprec(state.sas, state.gc, ref state.d);
            v = 0;
            for(i=0; i<=n-1; i++)
            {
                v = v+math.sqr(state.d[i]/state.s[i]);
            }
            v = Math.Sqrt(v);
            if( (double)(state.lastscaledgoodstep)>(double)(0) && (double)(v)>(double)(0) )
            {
                state.stp = state.lastscaledgoodstep/v;
            }
            else
            {
                state.stp = 1.0;
            }
            
            //
            // Calculate bound on step length.
            // Enforce user-supplied limit on step length.
            //
            sactivesets.sasexploredirection(state.sas, state.d, ref state.curstpmax, ref state.cidx, ref state.cval);
            state.activationstep = state.curstpmax;
            if( state.cidx>=0 && (double)(state.activationstep)==(double)(0) )
            {
                sactivesets.sasimmediateactivation(state.sas, state.cidx, state.cval);
                goto lbl_58;
            }
            if( (double)(state.stpmax)>(double)(0) )
            {
                state.curstpmax = Math.Min(state.curstpmax, state.stpmax);
            }
            
            //
            // Perform optimization of F along XC+alpha*D.
            //
            state.mcstage = 0;
            for(i_=0; i_<=n-1;i_++)
            {
                state.xn[i_] = state.sas.xc[i_];
            }
            for(i_=0; i_<=n-1;i_++)
            {
                state.gn[i_] = state.gc[i_];
            }
            state.fn = state.fc;
            linmin.mcsrch(n, ref state.xn, ref state.fn, ref state.gn, state.d, ref state.stp, state.curstpmax, gtol, ref mcinfo, ref state.nfev, ref state.work, state.lstate, ref state.mcstage);
        lbl_60:
            if( state.mcstage==0 )
            {
                goto lbl_61;
            }
            
            //
            // Enforce constraints (correction) in XN.
            // Copy current point from XN to X.
            //
            sactivesets.sascorrection(state.sas, ref state.xn, ref penalty);
            for(i=0; i<=n-1; i++)
            {
                state.x[i] = state.xn[i];
            }
            
            //
            // Gradient, either user-provided or numerical differentiation
            //
            clearrequestfields(state);
            if( (double)(state.diffstep)!=(double)(0) )
            {
                goto lbl_62;
            }
            
            //
            // Analytic gradient
            //
            state.needfg = true;
            state.rstate.stage = 14;
            goto lbl_rcomm;
        lbl_14:
            state.needfg = false;
            state.repnfev = state.repnfev+1;
            goto lbl_63;
        lbl_62:
            
            //
            // Numerical differentiation
            //
            state.needf = true;
            state.rstate.stage = 15;
            goto lbl_rcomm;
        lbl_15:
            state.fbase = state.f;
            i = 0;
        lbl_64:
            if( i>n-1 )
            {
                goto lbl_66;
            }
            v = state.x[i];
            b = false;
            if( state.hasbndl[i] )
            {
                b = b || (double)(v-state.diffstep*state.s[i])<(double)(state.bndl[i]);
            }
            if( state.hasbndu[i] )
            {
                b = b || (double)(v+state.diffstep*state.s[i])>(double)(state.bndu[i]);
            }
            if( b )
            {
                goto lbl_67;
            }
            state.x[i] = v-state.diffstep*state.s[i];
            state.rstate.stage = 16;
            goto lbl_rcomm;
        lbl_16:
            state.fm2 = state.f;
            state.x[i] = v-0.5*state.diffstep*state.s[i];
            state.rstate.stage = 17;
            goto lbl_rcomm;
        lbl_17:
            state.fm1 = state.f;
            state.x[i] = v+0.5*state.diffstep*state.s[i];
            state.rstate.stage = 18;
            goto lbl_rcomm;
        lbl_18:
            state.fp1 = state.f;
            state.x[i] = v+state.diffstep*state.s[i];
            state.rstate.stage = 19;
            goto lbl_rcomm;
        lbl_19:
            state.fp2 = state.f;
            state.g[i] = (8*(state.fp1-state.fm1)-(state.fp2-state.fm2))/(6*state.diffstep*state.s[i]);
            state.repnfev = state.repnfev+4;
            goto lbl_68;
        lbl_67:
            state.xm1 = v-state.diffstep*state.s[i];
            state.xp1 = v+state.diffstep*state.s[i];
            if( state.hasbndl[i] && (double)(state.xm1)<(double)(state.bndl[i]) )
            {
                state.xm1 = state.bndl[i];
            }
            if( state.hasbndu[i] && (double)(state.xp1)>(double)(state.bndu[i]) )
            {
                state.xp1 = state.bndu[i];
            }
            state.x[i] = state.xm1;
            state.rstate.stage = 20;
            goto lbl_rcomm;
        lbl_20:
            state.fm1 = state.f;
            state.x[i] = state.xp1;
            state.rstate.stage = 21;
            goto lbl_rcomm;
        lbl_21:
            state.fp1 = state.f;
            if( (double)(state.xm1)!=(double)(state.xp1) )
            {
                state.g[i] = (state.fp1-state.fm1)/(state.xp1-state.xm1);
            }
            else
            {
                state.g[i] = 0;
            }
            state.repnfev = state.repnfev+2;
        lbl_68:
            state.x[i] = v;
            i = i+1;
            goto lbl_64;
        lbl_66:
            state.f = state.fbase;
            state.needf = false;
        lbl_63:
            
            //
            // Back to MCSRCH
            //
            // NOTE: penalty term from correction is added to FN in order
            //       to penalize increase in infeasibility.
            //
            state.fn = state.f+penalty;
            for(i_=0; i_<=n-1;i_++)
            {
                state.gn[i_] = state.g[i_];
            }
            optserv.trimfunction(ref state.fn, ref state.gn, n, state.trimthreshold);
            linmin.mcsrch(n, ref state.xn, ref state.fn, ref state.gn, state.d, ref state.stp, state.curstpmax, gtol, ref mcinfo, ref state.nfev, ref state.work, state.lstate, ref state.mcstage);
            goto lbl_60;
        lbl_61:
            
            //
            // Handle possible failure of the line search
            //
            if( mcinfo!=1 && mcinfo!=5 )
            {
                
                //
                // We can not find step which decreases function value. We have
                // two possibilities:
                // (a) numerical properties of the function do not allow us to
                //     find good solution.
                // (b) we are close to activation of some constraint, and it is
                //     so close that step which activates it leads to change in
                //     target function which is smaller than numerical noise.
                //
                // Optimization algorithm must be able to handle case (b), because
                // inability to handle it will cause failure when algorithm
                // started very close to boundary of the feasible area.
                //
                // In order to correctly handle such cases we allow limited amount
                // of small steps which increase function value.
                //
                v = 0.0;
                for(i=0; i<=n-1; i++)
                {
                    v = v+math.sqr(state.d[i]*state.curstpmax/state.s[i]);
                }
                v = Math.Sqrt(v);
                if( (state.cidx>=0 && (double)(v)<=(double)(maxnonmonotoniclen*math.machineepsilon)) && state.nonmonotoniccnt>0 )
                {
                    
                    //
                    // We enforce non-monotonic step:
                    // * Stp    := CurStpMax
                    // * MCINFO := 5
                    // * XN     := XC+CurStpMax*D
                    // * non-monotonic counter is decreased
                    //
                    state.stp = state.curstpmax;
                    mcinfo = 5;
                    v = state.curstpmax;
                    for(i_=0; i_<=n-1;i_++)
                    {
                        state.xn[i_] = state.sas.xc[i_];
                    }
                    for(i_=0; i_<=n-1;i_++)
                    {
                        state.xn[i_] = state.xn[i_] + v*state.d[i_];
                    }
                    state.nonmonotoniccnt = state.nonmonotoniccnt-1;
                }
                else
                {
                    
                    //
                    // Numerical properties of the function does not allow us to solve problem
                    //
                    state.repterminationtype = 7;
                    nextaction = 0;
                    goto lbl_59;
                }
            }
            
            //
            // Current point is updated.
            //
            for(i_=0; i_<=n-1;i_++)
            {
                state.xp[i_] = state.sas.xc[i_];
            }
            for(i_=0; i_<=n-1;i_++)
            {
                state.gp[i_] = state.gc[i_];
            }
            state.fp = state.fc;
            actstatus = sactivesets.sasmoveto(state.sas, state.xn, state.cidx>=0 && (double)(state.stp)>=(double)(state.activationstep), state.cidx, state.cval);
            for(i_=0; i_<=n-1;i_++)
            {
                state.gc[i_] = state.gn[i_];
            }
            state.fc = state.fn;
            state.repinneriterationscount = state.repinneriterationscount+1;
            if( !state.xrep )
            {
                goto lbl_69;
            }
            for(i_=0; i_<=n-1;i_++)
            {
                state.x[i_] = state.sas.xc[i_];
            }
            clearrequestfields(state);
            state.xupdated = true;
            state.rstate.stage = 22;
            goto lbl_rcomm;
        lbl_22:
            state.xupdated = false;
        lbl_69:
            
            //
            // Check for stopping.
            //
            // Step, gradient and function-based stopping criteria are tested only
            // for steps which satisfy Wolfe conditions.
            //
            // MaxIts-based stopping condition is checked for all steps
            //
            if( mcinfo==1 )
            {
                
                //
                // Step is small enough
                //
                v = 0;
                vv = 0;
                for(i=0; i<=n-1; i++)
                {
                    v = v+math.sqr((state.sas.xc[i]-state.xp[i])/state.s[i]);
                    vv = vv+math.sqr(state.sas.xc[i]-state.xp[i]);
                }
                v = Math.Sqrt(v);
                vv = Math.Sqrt(vv);
                if( (double)(v)<=(double)(state.epsx) )
                {
                    state.repterminationtype = 2;
                    nextaction = 0;
                    goto lbl_59;
                }
                state.lastgoodstep = vv;
                updateestimateofgoodstep(ref state.lastscaledgoodstep, v);
                
                //
                // Function change is small enough
                //
                if( (double)(Math.Abs(state.fp-state.fc))<=(double)(state.epsf*Math.Max(Math.Abs(state.fc), Math.Max(Math.Abs(state.fp), 1.0))) )
                {
                    
                    //
                    // Function change is small enough
                    //
                    state.repterminationtype = 1;
                    nextaction = 0;
                    goto lbl_59;
                }
            }
            if( state.maxits>0 && state.repinneriterationscount>=state.maxits )
            {
                
                //
                // Required number of iterations was performed
                //
                state.repterminationtype = 5;
                nextaction = 0;
                goto lbl_59;
            }
            
            //
            // Decide where to move:
            // * in case only "candidate" constraints were activated, repeat stage 2
            // * in case no constraints was activated, move to stage 3
            // * otherwise, move to stage 1 (re-evaluation of the active set)
            //
            if( actstatus==0 )
            {
                goto lbl_58;
            }
            if( actstatus<0 )
            {
                nextaction = 1;
            }
            else
            {
                nextaction = -1;
            }
            goto lbl_59;
            goto lbl_58;
        lbl_59:
            if( nextaction<0 )
            {
                goto lbl_49;
            }
            if( nextaction==0 )
            {
                goto lbl_50;
            }
            
            //
            // Phase 3: L-BFGS step
            //
            for(i_=0; i_<=n-1;i_++)
            {
                state.x[i_] = state.sas.xc[i_];
            }
            clearrequestfields(state);
            if( (double)(state.diffstep)!=(double)(0) )
            {
                goto lbl_71;
            }
            
            //
            // Analytic gradient
            //
            state.needfg = true;
            state.rstate.stage = 23;
            goto lbl_rcomm;
        lbl_23:
            state.needfg = false;
            state.repnfev = state.repnfev+1;
            goto lbl_72;
        lbl_71:
            
            //
            // Numerical differentiation
            //
            state.needf = true;
            state.rstate.stage = 24;
            goto lbl_rcomm;
        lbl_24:
            state.fbase = state.f;
            i = 0;
        lbl_73:
            if( i>n-1 )
            {
                goto lbl_75;
            }
            v = state.x[i];
            b = false;
            if( state.hasbndl[i] )
            {
                b = b || (double)(v-state.diffstep*state.s[i])<(double)(state.bndl[i]);
            }
            if( state.hasbndu[i] )
            {
                b = b || (double)(v+state.diffstep*state.s[i])>(double)(state.bndu[i]);
            }
            if( b )
            {
                goto lbl_76;
            }
            state.x[i] = v-state.diffstep*state.s[i];
            state.rstate.stage = 25;
            goto lbl_rcomm;
        lbl_25:
            state.fm2 = state.f;
            state.x[i] = v-0.5*state.diffstep*state.s[i];
            state.rstate.stage = 26;
            goto lbl_rcomm;
        lbl_26:
            state.fm1 = state.f;
            state.x[i] = v+0.5*state.diffstep*state.s[i];
            state.rstate.stage = 27;
            goto lbl_rcomm;
        lbl_27:
            state.fp1 = state.f;
            state.x[i] = v+state.diffstep*state.s[i];
            state.rstate.stage = 28;
            goto lbl_rcomm;
        lbl_28:
            state.fp2 = state.f;
            state.g[i] = (8*(state.fp1-state.fm1)-(state.fp2-state.fm2))/(6*state.diffstep*state.s[i]);
            state.repnfev = state.repnfev+4;
            goto lbl_77;
        lbl_76:
            state.xm1 = v-state.diffstep*state.s[i];
            state.xp1 = v+state.diffstep*state.s[i];
            if( state.hasbndl[i] && (double)(state.xm1)<(double)(state.bndl[i]) )
            {
                state.xm1 = state.bndl[i];
            }
            if( state.hasbndu[i] && (double)(state.xp1)>(double)(state.bndu[i]) )
            {
                state.xp1 = state.bndu[i];
            }
            state.x[i] = state.xm1;
            state.rstate.stage = 29;
            goto lbl_rcomm;
        lbl_29:
            state.fm1 = state.f;
            state.x[i] = state.xp1;
            state.rstate.stage = 30;
            goto lbl_rcomm;
        lbl_30:
            state.fp1 = state.f;
            if( (double)(state.xm1)!=(double)(state.xp1) )
            {
                state.g[i] = (state.fp1-state.fm1)/(state.xp1-state.xm1);
            }
            else
            {
                state.g[i] = 0;
            }
            state.repnfev = state.repnfev+2;
        lbl_77:
            state.x[i] = v;
            i = i+1;
            goto lbl_73;
        lbl_75:
            state.f = state.fbase;
            state.needf = false;
        lbl_72:
            state.fc = state.f;
            optserv.trimprepare(state.fc, ref state.trimthreshold);
            for(i_=0; i_<=n-1;i_++)
            {
                state.gc[i_] = state.g[i_];
            }
            for(i_=0; i_<=n-1;i_++)
            {
                state.d[i_] = -state.g[i_];
            }
            sactivesets.sasconstraineddirection(state.sas, ref state.gc);
            sactivesets.sasconstraineddirectionprec(state.sas, ref state.d);
            ginit = 0.0;
            for(i_=0; i_<=n-1;i_++)
            {
                ginit += state.gc[i_]*state.gc[i_];
            }
            ginit = Math.Sqrt(ginit);
            state.k = 0;
        lbl_78:
            if( state.k>n )
            {
                goto lbl_79;
            }
            
            //
            // Main cycle: prepare to 1-D line search
            //
            state.p = state.k%m;
            state.q = Math.Min(state.k, m-1);
            
            //
            // Store X[k], G[k]
            //
            for(i_=0; i_<=n-1;i_++)
            {
                state.sk[state.p,i_] = -state.sas.xc[i_];
            }
            for(i_=0; i_<=n-1;i_++)
            {
                state.yk[state.p,i_] = -state.gc[i_];
            }
            
            //
            // Try to use previous scaled step length as initial estimate for new step.
            //
            v = 0;
            for(i=0; i<=n-1; i++)
            {
                v = v+math.sqr(state.d[i]/state.s[i]);
            }
            v = Math.Sqrt(v);
            if( (double)(state.lastscaledgoodstep)>(double)(0) && (double)(v)>(double)(0) )
            {
                state.stp = state.lastscaledgoodstep/v;
            }
            else
            {
                state.stp = 1.0;
            }
            
            //
            // Calculate bound on step length
            //
            sactivesets.sasexploredirection(state.sas, state.d, ref state.curstpmax, ref state.cidx, ref state.cval);
            state.activationstep = state.curstpmax;
            if( state.cidx>=0 && (double)(state.activationstep)==(double)(0) )
            {
                goto lbl_79;
            }
            if( (double)(state.stpmax)>(double)(0) )
            {
                v = 0.0;
                for(i_=0; i_<=n-1;i_++)
                {
                    v += state.d[i_]*state.d[i_];
                }
                v = Math.Sqrt(v);
                if( (double)(v)>(double)(0) )
                {
                    state.curstpmax = Math.Min(state.curstpmax, state.stpmax/v);
                }
            }
            
            //
            // Minimize F(x+alpha*d)
            //
            for(i_=0; i_<=n-1;i_++)
            {
                state.xn[i_] = state.sas.xc[i_];
            }
            for(i_=0; i_<=n-1;i_++)
            {
                state.gn[i_] = state.gc[i_];
            }
            state.fn = state.fc;
            state.mcstage = 0;
            linmin.mcsrch(n, ref state.xn, ref state.fn, ref state.gn, state.d, ref state.stp, state.curstpmax, gtol, ref mcinfo, ref state.nfev, ref state.work, state.lstate, ref state.mcstage);
        lbl_80:
            if( state.mcstage==0 )
            {
                goto lbl_81;
            }
            
            //
            // Perform correction (constraints are enforced)
            // Copy XN to X
            //
            sactivesets.sascorrection(state.sas, ref state.xn, ref penalty);
            for(i=0; i<=n-1; i++)
            {
                state.x[i] = state.xn[i];
            }
            
            //
            // Gradient, either user-provided or numerical differentiation
            //
            clearrequestfields(state);
            if( (double)(state.diffstep)!=(double)(0) )
            {
                goto lbl_82;
            }
            
            //
            // Analytic gradient
            //
            state.needfg = true;
            state.rstate.stage = 31;
            goto lbl_rcomm;
        lbl_31:
            state.needfg = false;
            state.repnfev = state.repnfev+1;
            goto lbl_83;
        lbl_82:
            
            //
            // Numerical differentiation
            //
            state.needf = true;
            state.rstate.stage = 32;
            goto lbl_rcomm;
        lbl_32:
            state.fbase = state.f;
            i = 0;
        lbl_84:
            if( i>n-1 )
            {
                goto lbl_86;
            }
            v = state.x[i];
            b = false;
            if( state.hasbndl[i] )
            {
                b = b || (double)(v-state.diffstep*state.s[i])<(double)(state.bndl[i]);
            }
            if( state.hasbndu[i] )
            {
                b = b || (double)(v+state.diffstep*state.s[i])>(double)(state.bndu[i]);
            }
            if( b )
            {
                goto lbl_87;
            }
            state.x[i] = v-state.diffstep*state.s[i];
            state.rstate.stage = 33;
            goto lbl_rcomm;
        lbl_33:
            state.fm2 = state.f;
            state.x[i] = v-0.5*state.diffstep*state.s[i];
            state.rstate.stage = 34;
            goto lbl_rcomm;
        lbl_34:
            state.fm1 = state.f;
            state.x[i] = v+0.5*state.diffstep*state.s[i];
            state.rstate.stage = 35;
            goto lbl_rcomm;
        lbl_35:
            state.fp1 = state.f;
            state.x[i] = v+state.diffstep*state.s[i];
            state.rstate.stage = 36;
            goto lbl_rcomm;
        lbl_36:
            state.fp2 = state.f;
            state.g[i] = (8*(state.fp1-state.fm1)-(state.fp2-state.fm2))/(6*state.diffstep*state.s[i]);
            state.repnfev = state.repnfev+4;
            goto lbl_88;
        lbl_87:
            state.xm1 = v-state.diffstep*state.s[i];
            state.xp1 = v+state.diffstep*state.s[i];
            if( state.hasbndl[i] && (double)(state.xm1)<(double)(state.bndl[i]) )
            {
                state.xm1 = state.bndl[i];
            }
            if( state.hasbndu[i] && (double)(state.xp1)>(double)(state.bndu[i]) )
            {
                state.xp1 = state.bndu[i];
            }
            state.x[i] = state.xm1;
            state.rstate.stage = 37;
            goto lbl_rcomm;
        lbl_37:
            state.fm1 = state.f;
            state.x[i] = state.xp1;
            state.rstate.stage = 38;
            goto lbl_rcomm;
        lbl_38:
            state.fp1 = state.f;
            if( (double)(state.xm1)!=(double)(state.xp1) )
            {
                state.g[i] = (state.fp1-state.fm1)/(state.xp1-state.xm1);
            }
            else
            {
                state.g[i] = 0;
            }
            state.repnfev = state.repnfev+2;
        lbl_88:
            state.x[i] = v;
            i = i+1;
            goto lbl_84;
        lbl_86:
            state.f = state.fbase;
            state.needf = false;
        lbl_83:
            
            //
            // Back to MCSRCH
            //
            // NOTE: penalty term from correction is added to FN in order
            //       to penalize increase in infeasibility.
            //
            state.fn = state.f+penalty;
            for(i_=0; i_<=n-1;i_++)
            {
                state.gn[i_] = state.g[i_];
            }
            sactivesets.sasconstraineddirection(state.sas, ref state.gn);
            optserv.trimfunction(ref state.fn, ref state.gn, n, state.trimthreshold);
            linmin.mcsrch(n, ref state.xn, ref state.fn, ref state.gn, state.d, ref state.stp, state.curstpmax, gtol, ref mcinfo, ref state.nfev, ref state.work, state.lstate, ref state.mcstage);
            goto lbl_80;
        lbl_81:
            for(i_=0; i_<=n-1;i_++)
            {
                state.sk[state.p,i_] = state.sk[state.p,i_] + state.xn[i_];
            }
            for(i_=0; i_<=n-1;i_++)
            {
                state.yk[state.p,i_] = state.yk[state.p,i_] + state.gn[i_];
            }
            
            //
            // Handle possible failure of the line search
            //
            if( mcinfo!=1 && mcinfo!=5 )
            {
                goto lbl_79;
            }
            
            //
            // Current point is updated.
            //
            for(i_=0; i_<=n-1;i_++)
            {
                state.xp[i_] = state.sas.xc[i_];
            }
            for(i_=0; i_<=n-1;i_++)
            {
                state.gp[i_] = state.gc[i_];
            }
            state.fp = state.fc;
            actstatus = sactivesets.sasmoveto(state.sas, state.xn, state.cidx>=0 && (double)(state.stp)>=(double)(state.activationstep), state.cidx, state.cval);
            for(i_=0; i_<=n-1;i_++)
            {
                state.gc[i_] = state.gn[i_];
            }
            state.fc = state.fn;
            if( !state.xrep )
            {
                goto lbl_89;
            }
            for(i_=0; i_<=n-1;i_++)
            {
                state.x[i_] = state.sas.xc[i_];
            }
            clearrequestfields(state);
            state.xupdated = true;
            state.rstate.stage = 39;
            goto lbl_rcomm;
        lbl_39:
            state.xupdated = false;
        lbl_89:
            state.repinneriterationscount = state.repinneriterationscount+1;
            
            //
            // Update length of the good step
            //
            if( mcinfo==1 )
            {
                v = 0;
                vv = 0;
                for(i=0; i<=n-1; i++)
                {
                    v = v+math.sqr((state.sas.xc[i]-state.xp[i])/state.s[i]);
                    vv = vv+math.sqr(state.sas.xc[i]-state.xp[i]);
                }
                state.lastgoodstep = Math.Sqrt(vv);
                updateestimateofgoodstep(ref state.lastscaledgoodstep, Math.Sqrt(v));
            }
            
            //
            // Termination of the L-BFGS algorithm:
            // a) line search was performed with activation of constraint
            // b) gradient decreased below GDecay
            //
            if( actstatus>=0 )
            {
                goto lbl_79;
            }
            v = 0.0;
            for(i_=0; i_<=n-1;i_++)
            {
                v += state.gc[i_]*state.gc[i_];
            }
            if( (double)(Math.Sqrt(v))<(double)(gdecay*ginit) )
            {
                goto lbl_79;
            }
            
            //
            // Update L-BFGS model:
            // * calculate Rho[k]
            // * calculate d(k+1) = -H(k+1)*g(k+1)
            //   (use constrained preconditioner to perform multiplication)
            //
            v = 0.0;
            for(i_=0; i_<=n-1;i_++)
            {
                v += state.yk[state.p,i_]*state.sk[state.p,i_];
            }
            vv = 0.0;
            for(i_=0; i_<=n-1;i_++)
            {
                vv += state.yk[state.p,i_]*state.yk[state.p,i_];
            }
            if( (double)(v)==(double)(0) || (double)(vv)==(double)(0) )
            {
                goto lbl_79;
            }
            state.rho[state.p] = 1/v;
            for(i_=0; i_<=n-1;i_++)
            {
                state.work[i_] = state.gn[i_];
            }
            for(i=state.k; i>=state.k-state.q; i--)
            {
                ic = i%m;
                v = 0.0;
                for(i_=0; i_<=n-1;i_++)
                {
                    v += state.sk[ic,i_]*state.work[i_];
                }
                state.theta[ic] = v;
                vv = v*state.rho[ic];
                for(i_=0; i_<=n-1;i_++)
                {
                    state.work[i_] = state.work[i_] - vv*state.yk[ic,i_];
                }
            }
            sactivesets.sasconstraineddirectionprec(state.sas, ref state.work);
            for(i=state.k-state.q; i<=state.k; i++)
            {
                ic = i%m;
                v = 0.0;
                for(i_=0; i_<=n-1;i_++)
                {
                    v += state.yk[ic,i_]*state.work[i_];
                }
                vv = state.rho[ic]*(-v+state.theta[ic]);
                for(i_=0; i_<=n-1;i_++)
                {
                    state.work[i_] = state.work[i_] + vv*state.sk[ic,i_];
                }
            }
            for(i_=0; i_<=n-1;i_++)
            {
                state.d[i_] = -state.work[i_];
            }
            state.k = state.k+1;
            goto lbl_78;
        lbl_79:
            
            //
            // Decrease decay coefficient. Subsequent L-BFGS stages will
            // have more stringent stopping criteria.
            //
            gdecay = Math.Max(gdecay*decaycorrection, mindecay);
            goto lbl_49;
        lbl_50:
            sactivesets.sasstopoptimization(state.sas);
            state.repouteriterationscount = 1;
            result = false;
            return result;
            
            //
            // Saving state
            //
        lbl_rcomm:
            result = true;
            state.rstate.ia[0] = n;
            state.rstate.ia[1] = m;
            state.rstate.ia[2] = i;
            state.rstate.ia[3] = j;
            state.rstate.ia[4] = badbfgsits;
            state.rstate.ia[5] = nextaction;
            state.rstate.ia[6] = actstatus;
            state.rstate.ia[7] = mcinfo;
            state.rstate.ia[8] = ic;
            state.rstate.ba[0] = b;
            state.rstate.ra[0] = v;
            state.rstate.ra[1] = vv;
            state.rstate.ra[2] = penalty;
            state.rstate.ra[3] = ginit;
            state.rstate.ra[4] = gdecay;
            return result;
        }


        /*************************************************************************
        BLEIC results

        INPUT PARAMETERS:
            State   -   algorithm state

        OUTPUT PARAMETERS:
            X       -   array[0..N-1], solution
            Rep     -   optimization report. You should check Rep.TerminationType
                        in  order  to  distinguish  successful  termination  from
                        unsuccessful one:
                        * -7   gradient verification failed.
                               See MinBLEICSetGradientCheck() for more information.
                        * -3   inconsistent constraints. Feasible point is
                               either nonexistent or too hard to find. Try to
                               restart optimizer with better initial approximation
                        *  1   relative function improvement is no more than EpsF.
                        *  2   relative step is no more than EpsX.
                        *  4   gradient norm is no more than EpsG
                        *  5   MaxIts steps was taken
                        More information about fields of this  structure  can  be
                        found in the comments on MinBLEICReport datatype.
           
          -- ALGLIB --
             Copyright 28.11.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void minbleicresults(minbleicstate state,
            ref double[] x,
            minbleicreport rep)
        {
            x = new double[0];

            minbleicresultsbuf(state, ref x, rep);
        }


        /*************************************************************************
        BLEIC results

        Buffered implementation of MinBLEICResults() which uses pre-allocated buffer
        to store X[]. If buffer size is  too  small,  it  resizes  buffer.  It  is
        intended to be used in the inner cycles of performance critical algorithms
        where array reallocation penalty is too large to be ignored.

          -- ALGLIB --
             Copyright 28.11.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void minbleicresultsbuf(minbleicstate state,
            ref double[] x,
            minbleicreport rep)
        {
            int i = 0;
            int i_ = 0;

            if( alglib.ap.len(x)<state.nmain )
            {
                x = new double[state.nmain];
            }
            rep.iterationscount = state.repinneriterationscount;
            rep.inneriterationscount = state.repinneriterationscount;
            rep.outeriterationscount = state.repouteriterationscount;
            rep.nfev = state.repnfev;
            rep.varidx = state.repvaridx;
            rep.terminationtype = state.repterminationtype;
            if( state.repterminationtype>0 )
            {
                for(i_=0; i_<=state.nmain-1;i_++)
                {
                    x[i_] = state.sas.xc[i_];
                }
            }
            else
            {
                for(i=0; i<=state.nmain-1; i++)
                {
                    x[i] = Double.NaN;
                }
            }
            rep.debugeqerr = state.repdebugeqerr;
            rep.debugfs = state.repdebugfs;
            rep.debugff = state.repdebugff;
            rep.debugdx = state.repdebugdx;
            rep.debugfeasqpits = state.repdebugfeasqpits;
            rep.debugfeasgpaits = state.repdebugfeasgpaits;
        }


        /*************************************************************************
        This subroutine restarts algorithm from new point.
        All optimization parameters (including constraints) are left unchanged.

        This  function  allows  to  solve multiple  optimization  problems  (which
        must have  same number of dimensions) without object reallocation penalty.

        INPUT PARAMETERS:
            State   -   structure previously allocated with MinBLEICCreate call.
            X       -   new starting point.

          -- ALGLIB --
             Copyright 28.11.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void minbleicrestartfrom(minbleicstate state,
            double[] x)
        {
            int n = 0;
            int i_ = 0;

            n = state.nmain;
            
            //
            // First, check for errors in the inputs
            //
            alglib.ap.assert(alglib.ap.len(x)>=n, "MinBLEICRestartFrom: Length(X)<N");
            alglib.ap.assert(apserv.isfinitevector(x, n), "MinBLEICRestartFrom: X contains infinite or NaN values!");
            
            //
            // Set XC
            //
            for(i_=0; i_<=n-1;i_++)
            {
                state.xstart[i_] = x[i_];
            }
            
            //
            // prepare RComm facilities
            //
            state.rstate.ia = new int[8+1];
            state.rstate.ba = new bool[0+1];
            state.rstate.ra = new double[4+1];
            state.rstate.stage = -1;
            clearrequestfields(state);
        }


        /*************************************************************************
        This  subroutine  turns  on  verification  of  the  user-supplied analytic
        gradient:
        * user calls this subroutine before optimization begins
        * MinBLEICOptimize() is called
        * prior to  actual  optimization, for each component  of  parameters being
          optimized X[i] algorithm performs following steps:
          * two trial steps are made to X[i]-TestStep*S[i] and X[i]+TestStep*S[i],
            where X[i] is i-th component of the initial point and S[i] is a  scale
            of i-th parameter
          * if needed, steps are bounded with respect to constraints on X[]
          * F(X) is evaluated at these trial points
          * we perform one more evaluation in the middle point of the interval
          * we  build  cubic  model using function values and derivatives at trial
            points and we compare its prediction with actual value in  the  middle
            point
          * in case difference between prediction and actual value is higher  than
            some predetermined threshold, algorithm stops with completion code -7;
            Rep.VarIdx is set to index of the parameter with incorrect derivative.
        * after verification is over, algorithm proceeds to the actual optimization.

        NOTE 1: verification  needs  N (parameters count) gradient evaluations. It
                is very costly and you should use  it  only  for  low  dimensional
                problems,  when  you  want  to  be  sure  that  you've   correctly
                calculated  analytic  derivatives.  You  should  not use it in the
                production code (unless you want to check derivatives provided  by
                some third party).

        NOTE 2: you  should  carefully  choose  TestStep. Value which is too large
                (so large that function behaviour is significantly non-cubic) will
                lead to false alarms. You may use  different  step  for  different
                parameters by means of setting scale with MinBLEICSetScale().

        NOTE 3: this function may lead to false positives. In case it reports that
                I-th  derivative was calculated incorrectly, you may decrease test
                step  and  try  one  more  time  - maybe your function changes too
                sharply  and  your  step  is  too  large for such rapidly chanding
                function.

        INPUT PARAMETERS:
            State       -   structure used to store algorithm state
            TestStep    -   verification step:
                            * TestStep=0 turns verification off
                            * TestStep>0 activates verification

          -- ALGLIB --
             Copyright 15.06.2012 by Bochkanov Sergey
        *************************************************************************/
        public static void minbleicsetgradientcheck(minbleicstate state,
            double teststep)
        {
            alglib.ap.assert(math.isfinite(teststep), "MinBLEICSetGradientCheck: TestStep contains NaN or Infinite");
            alglib.ap.assert((double)(teststep)>=(double)(0), "MinBLEICSetGradientCheck: invalid argument TestStep(TestStep<0)");
            state.teststep = teststep;
        }


        /*************************************************************************
        Clears request fileds (to be sure that we don't forget to clear something)
        *************************************************************************/
        private static void clearrequestfields(minbleicstate state)
        {
            state.needf = false;
            state.needfg = false;
            state.xupdated = false;
        }


        /*************************************************************************
        Internal initialization subroutine
        *************************************************************************/
        private static void minbleicinitinternal(int n,
            double[] x,
            double diffstep,
            minbleicstate state)
        {
            int i = 0;
            double[,] c = new double[0,0];
            int[] ct = new int[0];

            
            //
            // Initialize
            //
            state.teststep = 0;
            state.nmain = n;
            state.diffstep = diffstep;
            sactivesets.sasinit(n, state.sas);
            state.bndl = new double[n];
            state.hasbndl = new bool[n];
            state.bndu = new double[n];
            state.hasbndu = new bool[n];
            state.xstart = new double[n];
            state.gc = new double[n];
            state.xn = new double[n];
            state.gn = new double[n];
            state.xp = new double[n];
            state.gp = new double[n];
            state.d = new double[n];
            state.s = new double[n];
            state.x = new double[n];
            state.g = new double[n];
            state.work = new double[n];
            for(i=0; i<=n-1; i++)
            {
                state.bndl[i] = Double.NegativeInfinity;
                state.hasbndl[i] = false;
                state.bndu[i] = Double.PositiveInfinity;
                state.hasbndu[i] = false;
                state.s[i] = 1.0;
            }
            minbleicsetlc(state, c, ct, 0);
            minbleicsetcond(state, 0.0, 0.0, 0.0, 0);
            minbleicsetxrep(state, false);
            minbleicsetstpmax(state, 0.0);
            minbleicsetprecdefault(state);
            minbleicrestartfrom(state, x);
        }


        /*************************************************************************
        This subroutine updates estimate of the good step length given:
        1) previous estimate
        2) new length of the good step

        It makes sure that estimate does not change too rapidly - ratio of new and
        old estimates will be at least 0.01, at most 100.0

        In case previous estimate of good step is zero (no estimate), new estimate
        is used unconditionally.

          -- ALGLIB --
             Copyright 16.01.2013 by Bochkanov Sergey
        *************************************************************************/
        private static void updateestimateofgoodstep(ref double estimate,
            double newstep)
        {
            if( (double)(estimate)==(double)(0) )
            {
                estimate = newstep;
                return;
            }
            if( (double)(newstep)<(double)(estimate*0.01) )
            {
                estimate = estimate*0.01;
                return;
            }
            if( (double)(newstep)>(double)(estimate*100) )
            {
                estimate = estimate*100;
                return;
            }
            estimate = newstep;
        }


    }
    public class minlbfgs
    {
        public class minlbfgsstate : apobject
        {
            public int n;
            public int m;
            public double epsg;
            public double epsf;
            public double epsx;
            public int maxits;
            public bool xrep;
            public double stpmax;
            public double[] s;
            public double diffstep;
            public int nfev;
            public int mcstage;
            public int k;
            public int q;
            public int p;
            public double[] rho;
            public double[,] yk;
            public double[,] sk;
            public double[] theta;
            public double[] d;
            public double stp;
            public double[] work;
            public double fold;
            public double trimthreshold;
            public int prectype;
            public double gammak;
            public double[,] denseh;
            public double[] diagh;
            public double fbase;
            public double fm2;
            public double fm1;
            public double fp1;
            public double fp2;
            public double[] autobuf;
            public double[] x;
            public double f;
            public double[] g;
            public bool needf;
            public bool needfg;
            public bool xupdated;
            public double teststep;
            public rcommstate rstate;
            public int repiterationscount;
            public int repnfev;
            public int repvaridx;
            public int repterminationtype;
            public linmin.linminstate lstate;
            public minlbfgsstate()
            {
                init();
            }
            public override void init()
            {
                s = new double[0];
                rho = new double[0];
                yk = new double[0,0];
                sk = new double[0,0];
                theta = new double[0];
                d = new double[0];
                work = new double[0];
                denseh = new double[0,0];
                diagh = new double[0];
                autobuf = new double[0];
                x = new double[0];
                g = new double[0];
                rstate = new rcommstate();
                lstate = new linmin.linminstate();
            }
            public override alglib.apobject make_copy()
            {
                minlbfgsstate _result = new minlbfgsstate();
                _result.n = n;
                _result.m = m;
                _result.epsg = epsg;
                _result.epsf = epsf;
                _result.epsx = epsx;
                _result.maxits = maxits;
                _result.xrep = xrep;
                _result.stpmax = stpmax;
                _result.s = (double[])s.Clone();
                _result.diffstep = diffstep;
                _result.nfev = nfev;
                _result.mcstage = mcstage;
                _result.k = k;
                _result.q = q;
                _result.p = p;
                _result.rho = (double[])rho.Clone();
                _result.yk = (double[,])yk.Clone();
                _result.sk = (double[,])sk.Clone();
                _result.theta = (double[])theta.Clone();
                _result.d = (double[])d.Clone();
                _result.stp = stp;
                _result.work = (double[])work.Clone();
                _result.fold = fold;
                _result.trimthreshold = trimthreshold;
                _result.prectype = prectype;
                _result.gammak = gammak;
                _result.denseh = (double[,])denseh.Clone();
                _result.diagh = (double[])diagh.Clone();
                _result.fbase = fbase;
                _result.fm2 = fm2;
                _result.fm1 = fm1;
                _result.fp1 = fp1;
                _result.fp2 = fp2;
                _result.autobuf = (double[])autobuf.Clone();
                _result.x = (double[])x.Clone();
                _result.f = f;
                _result.g = (double[])g.Clone();
                _result.needf = needf;
                _result.needfg = needfg;
                _result.xupdated = xupdated;
                _result.teststep = teststep;
                _result.rstate = (rcommstate)rstate.make_copy();
                _result.repiterationscount = repiterationscount;
                _result.repnfev = repnfev;
                _result.repvaridx = repvaridx;
                _result.repterminationtype = repterminationtype;
                _result.lstate = (linmin.linminstate)lstate.make_copy();
                return _result;
            }
        };


        public class minlbfgsreport : apobject
        {
            public int iterationscount;
            public int nfev;
            public int varidx;
            public int terminationtype;
            public minlbfgsreport()
            {
                init();
            }
            public override void init()
            {
            }
            public override alglib.apobject make_copy()
            {
                minlbfgsreport _result = new minlbfgsreport();
                _result.iterationscount = iterationscount;
                _result.nfev = nfev;
                _result.varidx = varidx;
                _result.terminationtype = terminationtype;
                return _result;
            }
        };




        public const double gtol = 0.4;


        /*************************************************************************
                LIMITED MEMORY BFGS METHOD FOR LARGE SCALE OPTIMIZATION

        DESCRIPTION:
        The subroutine minimizes function F(x) of N arguments by  using  a  quasi-
        Newton method (LBFGS scheme) which is optimized to use  a  minimum  amount
        of memory.
        The subroutine generates the approximation of an inverse Hessian matrix by
        using information about the last M steps of the algorithm  (instead of N).
        It lessens a required amount of memory from a value  of  order  N^2  to  a
        value of order 2*N*M.


        REQUIREMENTS:
        Algorithm will request following information during its operation:
        * function value F and its gradient G (simultaneously) at given point X


        USAGE:
        1. User initializes algorithm state with MinLBFGSCreate() call
        2. User tunes solver parameters with MinLBFGSSetCond() MinLBFGSSetStpMax()
           and other functions
        3. User calls MinLBFGSOptimize() function which takes algorithm  state and
           pointer (delegate, etc.) to callback function which calculates F/G.
        4. User calls MinLBFGSResults() to get solution
        5. Optionally user may call MinLBFGSRestartFrom() to solve another problem
           with same N/M but another starting point and/or another function.
           MinLBFGSRestartFrom() allows to reuse already initialized structure.


        INPUT PARAMETERS:
            N       -   problem dimension. N>0
            M       -   number of corrections in the BFGS scheme of Hessian
                        approximation update. Recommended value:  3<=M<=7. The smaller
                        value causes worse convergence, the bigger will  not  cause  a
                        considerably better convergence, but will cause a fall in  the
                        performance. M<=N.
            X       -   initial solution approximation, array[0..N-1].


        OUTPUT PARAMETERS:
            State   -   structure which stores algorithm state
            

        NOTES:
        1. you may tune stopping conditions with MinLBFGSSetCond() function
        2. if target function contains exp() or other fast growing functions,  and
           optimization algorithm makes too large steps which leads  to  overflow,
           use MinLBFGSSetStpMax() function to bound algorithm's  steps.  However,
           L-BFGS rarely needs such a tuning.


          -- ALGLIB --
             Copyright 02.04.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void minlbfgscreate(int n,
            int m,
            double[] x,
            minlbfgsstate state)
        {
            alglib.ap.assert(n>=1, "MinLBFGSCreate: N<1!");
            alglib.ap.assert(m>=1, "MinLBFGSCreate: M<1");
            alglib.ap.assert(m<=n, "MinLBFGSCreate: M>N");
            alglib.ap.assert(alglib.ap.len(x)>=n, "MinLBFGSCreate: Length(X)<N!");
            alglib.ap.assert(apserv.isfinitevector(x, n), "MinLBFGSCreate: X contains infinite or NaN values!");
            minlbfgscreatex(n, m, x, 0, 0.0, state);
        }


        /*************************************************************************
        The subroutine is finite difference variant of MinLBFGSCreate().  It  uses
        finite differences in order to differentiate target function.

        Description below contains information which is specific to  this function
        only. We recommend to read comments on MinLBFGSCreate() in  order  to  get
        more information about creation of LBFGS optimizer.

        INPUT PARAMETERS:
            N       -   problem dimension, N>0:
                        * if given, only leading N elements of X are used
                        * if not given, automatically determined from size of X
            M       -   number of corrections in the BFGS scheme of Hessian
                        approximation update. Recommended value:  3<=M<=7. The smaller
                        value causes worse convergence, the bigger will  not  cause  a
                        considerably better convergence, but will cause a fall in  the
                        performance. M<=N.
            X       -   starting point, array[0..N-1].
            DiffStep-   differentiation step, >0

        OUTPUT PARAMETERS:
            State   -   structure which stores algorithm state

        NOTES:
        1. algorithm uses 4-point central formula for differentiation.
        2. differentiation step along I-th axis is equal to DiffStep*S[I] where
           S[] is scaling vector which can be set by MinLBFGSSetScale() call.
        3. we recommend you to use moderate values of  differentiation  step.  Too
           large step will result in too large truncation  errors, while too small
           step will result in too large numerical  errors.  1.0E-6  can  be  good
           value to start with.
        4. Numerical  differentiation  is   very   inefficient  -   one   gradient
           calculation needs 4*N function evaluations. This function will work for
           any N - either small (1...10), moderate (10...100) or  large  (100...).
           However, performance penalty will be too severe for any N's except  for
           small ones.
           We should also say that code which relies on numerical  differentiation
           is   less  robust  and  precise.  LBFGS  needs  exact  gradient values.
           Imprecise gradient may slow  down  convergence,  especially  on  highly
           nonlinear problems.
           Thus  we  recommend to use this function for fast prototyping on small-
           dimensional problems only, and to implement analytical gradient as soon
           as possible.

          -- ALGLIB --
             Copyright 16.05.2011 by Bochkanov Sergey
        *************************************************************************/
        public static void minlbfgscreatef(int n,
            int m,
            double[] x,
            double diffstep,
            minlbfgsstate state)
        {
            alglib.ap.assert(n>=1, "MinLBFGSCreateF: N too small!");
            alglib.ap.assert(m>=1, "MinLBFGSCreateF: M<1");
            alglib.ap.assert(m<=n, "MinLBFGSCreateF: M>N");
            alglib.ap.assert(alglib.ap.len(x)>=n, "MinLBFGSCreateF: Length(X)<N!");
            alglib.ap.assert(apserv.isfinitevector(x, n), "MinLBFGSCreateF: X contains infinite or NaN values!");
            alglib.ap.assert(math.isfinite(diffstep), "MinLBFGSCreateF: DiffStep is infinite or NaN!");
            alglib.ap.assert((double)(diffstep)>(double)(0), "MinLBFGSCreateF: DiffStep is non-positive!");
            minlbfgscreatex(n, m, x, 0, diffstep, state);
        }


        /*************************************************************************
        This function sets stopping conditions for L-BFGS optimization algorithm.

        INPUT PARAMETERS:
            State   -   structure which stores algorithm state
            EpsG    -   >=0
                        The  subroutine  finishes  its  work   if   the  condition
                        |v|<EpsG is satisfied, where:
                        * |.| means Euclidian norm
                        * v - scaled gradient vector, v[i]=g[i]*s[i]
                        * g - gradient
                        * s - scaling coefficients set by MinLBFGSSetScale()
            EpsF    -   >=0
                        The  subroutine  finishes  its work if on k+1-th iteration
                        the  condition  |F(k+1)-F(k)|<=EpsF*max{|F(k)|,|F(k+1)|,1}
                        is satisfied.
            EpsX    -   >=0
                        The subroutine finishes its work if  on  k+1-th  iteration
                        the condition |v|<=EpsX is fulfilled, where:
                        * |.| means Euclidian norm
                        * v - scaled step vector, v[i]=dx[i]/s[i]
                        * dx - ste pvector, dx=X(k+1)-X(k)
                        * s - scaling coefficients set by MinLBFGSSetScale()
            MaxIts  -   maximum number of iterations. If MaxIts=0, the  number  of
                        iterations is unlimited.

        Passing EpsG=0, EpsF=0, EpsX=0 and MaxIts=0 (simultaneously) will lead to
        automatic stopping criterion selection (small EpsX).

          -- ALGLIB --
             Copyright 02.04.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void minlbfgssetcond(minlbfgsstate state,
            double epsg,
            double epsf,
            double epsx,
            int maxits)
        {
            alglib.ap.assert(math.isfinite(epsg), "MinLBFGSSetCond: EpsG is not finite number!");
            alglib.ap.assert((double)(epsg)>=(double)(0), "MinLBFGSSetCond: negative EpsG!");
            alglib.ap.assert(math.isfinite(epsf), "MinLBFGSSetCond: EpsF is not finite number!");
            alglib.ap.assert((double)(epsf)>=(double)(0), "MinLBFGSSetCond: negative EpsF!");
            alglib.ap.assert(math.isfinite(epsx), "MinLBFGSSetCond: EpsX is not finite number!");
            alglib.ap.assert((double)(epsx)>=(double)(0), "MinLBFGSSetCond: negative EpsX!");
            alglib.ap.assert(maxits>=0, "MinLBFGSSetCond: negative MaxIts!");
            if( (((double)(epsg)==(double)(0) && (double)(epsf)==(double)(0)) && (double)(epsx)==(double)(0)) && maxits==0 )
            {
                epsx = 1.0E-6;
            }
            state.epsg = epsg;
            state.epsf = epsf;
            state.epsx = epsx;
            state.maxits = maxits;
        }


        /*************************************************************************
        This function turns on/off reporting.

        INPUT PARAMETERS:
            State   -   structure which stores algorithm state
            NeedXRep-   whether iteration reports are needed or not

        If NeedXRep is True, algorithm will call rep() callback function if  it is
        provided to MinLBFGSOptimize().


          -- ALGLIB --
             Copyright 02.04.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void minlbfgssetxrep(minlbfgsstate state,
            bool needxrep)
        {
            state.xrep = needxrep;
        }


        /*************************************************************************
        This function sets maximum step length

        INPUT PARAMETERS:
            State   -   structure which stores algorithm state
            StpMax  -   maximum step length, >=0. Set StpMax to 0.0 (default),  if
                        you don't want to limit step length.

        Use this subroutine when you optimize target function which contains exp()
        or  other  fast  growing  functions,  and optimization algorithm makes too
        large  steps  which  leads  to overflow. This function allows us to reject
        steps  that  are  too  large  (and  therefore  expose  us  to the possible
        overflow) without actually calculating function value at the x+stp*d.

          -- ALGLIB --
             Copyright 02.04.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void minlbfgssetstpmax(minlbfgsstate state,
            double stpmax)
        {
            alglib.ap.assert(math.isfinite(stpmax), "MinLBFGSSetStpMax: StpMax is not finite!");
            alglib.ap.assert((double)(stpmax)>=(double)(0), "MinLBFGSSetStpMax: StpMax<0!");
            state.stpmax = stpmax;
        }


        /*************************************************************************
        This function sets scaling coefficients for LBFGS optimizer.

        ALGLIB optimizers use scaling matrices to test stopping  conditions  (step
        size and gradient are scaled before comparison with tolerances).  Scale of
        the I-th variable is a translation invariant measure of:
        a) "how large" the variable is
        b) how large the step should be to make significant changes in the function

        Scaling is also used by finite difference variant of the optimizer  - step
        along I-th axis is equal to DiffStep*S[I].

        In  most  optimizers  (and  in  the  LBFGS  too)  scaling is NOT a form of
        preconditioning. It just  affects  stopping  conditions.  You  should  set
        preconditioner  by  separate  call  to  one  of  the  MinLBFGSSetPrec...()
        functions.

        There  is  special  preconditioning  mode, however,  which  uses   scaling
        coefficients to form diagonal preconditioning matrix. You  can  turn  this
        mode on, if you want.   But  you should understand that scaling is not the
        same thing as preconditioning - these are two different, although  related
        forms of tuning solver.

        INPUT PARAMETERS:
            State   -   structure stores algorithm state
            S       -   array[N], non-zero scaling coefficients
                        S[i] may be negative, sign doesn't matter.

          -- ALGLIB --
             Copyright 14.01.2011 by Bochkanov Sergey
        *************************************************************************/
        public static void minlbfgssetscale(minlbfgsstate state,
            double[] s)
        {
            int i = 0;

            alglib.ap.assert(alglib.ap.len(s)>=state.n, "MinLBFGSSetScale: Length(S)<N");
            for(i=0; i<=state.n-1; i++)
            {
                alglib.ap.assert(math.isfinite(s[i]), "MinLBFGSSetScale: S contains infinite or NAN elements");
                alglib.ap.assert((double)(s[i])!=(double)(0), "MinLBFGSSetScale: S contains zero elements");
                state.s[i] = Math.Abs(s[i]);
            }
        }


        /*************************************************************************
        Extended subroutine for internal use only.

        Accepts additional parameters:

            Flags - additional settings:
                    * Flags = 0     means no additional settings
                    * Flags = 1     "do not allocate memory". used when solving
                                    a many subsequent tasks with  same N/M  values.
                                    First  call MUST  be without this flag bit set,
                                    subsequent  calls   of   MinLBFGS   with   same
                                    MinLBFGSState structure can set Flags to 1.
            DiffStep - numerical differentiation step

          -- ALGLIB --
             Copyright 02.04.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void minlbfgscreatex(int n,
            int m,
            double[] x,
            int flags,
            double diffstep,
            minlbfgsstate state)
        {
            bool allocatemem = new bool();
            int i = 0;

            alglib.ap.assert(n>=1, "MinLBFGS: N too small!");
            alglib.ap.assert(m>=1, "MinLBFGS: M too small!");
            alglib.ap.assert(m<=n, "MinLBFGS: M too large!");
            
            //
            // Initialize
            //
            state.teststep = 0;
            state.diffstep = diffstep;
            state.n = n;
            state.m = m;
            allocatemem = flags%2==0;
            flags = flags/2;
            if( allocatemem )
            {
                state.rho = new double[m];
                state.theta = new double[m];
                state.yk = new double[m, n];
                state.sk = new double[m, n];
                state.d = new double[n];
                state.x = new double[n];
                state.s = new double[n];
                state.g = new double[n];
                state.work = new double[n];
            }
            minlbfgssetcond(state, 0, 0, 0, 0);
            minlbfgssetxrep(state, false);
            minlbfgssetstpmax(state, 0);
            minlbfgsrestartfrom(state, x);
            for(i=0; i<=n-1; i++)
            {
                state.s[i] = 1.0;
            }
            state.prectype = 0;
        }


        /*************************************************************************
        Modification  of  the  preconditioner:  default  preconditioner    (simple
        scaling, same for all elements of X) is used.

        INPUT PARAMETERS:
            State   -   structure which stores algorithm state

        NOTE:  you  can  change  preconditioner  "on  the  fly",  during algorithm
        iterations.

          -- ALGLIB --
             Copyright 13.10.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void minlbfgssetprecdefault(minlbfgsstate state)
        {
            state.prectype = 0;
        }


        /*************************************************************************
        Modification of the preconditioner: Cholesky factorization of  approximate
        Hessian is used.

        INPUT PARAMETERS:
            State   -   structure which stores algorithm state
            P       -   triangular preconditioner, Cholesky factorization of
                        the approximate Hessian. array[0..N-1,0..N-1],
                        (if larger, only leading N elements are used).
            IsUpper -   whether upper or lower triangle of P is given
                        (other triangle is not referenced)

        After call to this function preconditioner is changed to P  (P  is  copied
        into the internal buffer).

        NOTE:  you  can  change  preconditioner  "on  the  fly",  during algorithm
        iterations.

        NOTE 2:  P  should  be nonsingular. Exception will be thrown otherwise.

          -- ALGLIB --
             Copyright 13.10.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void minlbfgssetpreccholesky(minlbfgsstate state,
            double[,] p,
            bool isupper)
        {
            int i = 0;
            double mx = 0;

            alglib.ap.assert(apserv.isfinitertrmatrix(p, state.n, isupper), "MinLBFGSSetPrecCholesky: P contains infinite or NAN values!");
            mx = 0;
            for(i=0; i<=state.n-1; i++)
            {
                mx = Math.Max(mx, Math.Abs(p[i,i]));
            }
            alglib.ap.assert((double)(mx)>(double)(0), "MinLBFGSSetPrecCholesky: P is strictly singular!");
            if( alglib.ap.rows(state.denseh)<state.n || alglib.ap.cols(state.denseh)<state.n )
            {
                state.denseh = new double[state.n, state.n];
            }
            state.prectype = 1;
            if( isupper )
            {
                ablas.rmatrixcopy(state.n, state.n, p, 0, 0, ref state.denseh, 0, 0);
            }
            else
            {
                ablas.rmatrixtranspose(state.n, state.n, p, 0, 0, ref state.denseh, 0, 0);
            }
        }


        /*************************************************************************
        Modification  of  the  preconditioner:  diagonal of approximate Hessian is
        used.

        INPUT PARAMETERS:
            State   -   structure which stores algorithm state
            D       -   diagonal of the approximate Hessian, array[0..N-1],
                        (if larger, only leading N elements are used).

        NOTE:  you  can  change  preconditioner  "on  the  fly",  during algorithm
        iterations.

        NOTE 2: D[i] should be positive. Exception will be thrown otherwise.

        NOTE 3: you should pass diagonal of approximate Hessian - NOT ITS INVERSE.

          -- ALGLIB --
             Copyright 13.10.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void minlbfgssetprecdiag(minlbfgsstate state,
            double[] d)
        {
            int i = 0;

            alglib.ap.assert(alglib.ap.len(d)>=state.n, "MinLBFGSSetPrecDiag: D is too short");
            for(i=0; i<=state.n-1; i++)
            {
                alglib.ap.assert(math.isfinite(d[i]), "MinLBFGSSetPrecDiag: D contains infinite or NAN elements");
                alglib.ap.assert((double)(d[i])>(double)(0), "MinLBFGSSetPrecDiag: D contains non-positive elements");
            }
            apserv.rvectorsetlengthatleast(ref state.diagh, state.n);
            state.prectype = 2;
            for(i=0; i<=state.n-1; i++)
            {
                state.diagh[i] = d[i];
            }
        }


        /*************************************************************************
        Modification of the preconditioner: scale-based diagonal preconditioning.

        This preconditioning mode can be useful when you  don't  have  approximate
        diagonal of Hessian, but you know that your  variables  are  badly  scaled
        (for  example,  one  variable is in [1,10], and another in [1000,100000]),
        and most part of the ill-conditioning comes from different scales of vars.

        In this case simple  scale-based  preconditioner,  with H[i] = 1/(s[i]^2),
        can greatly improve convergence.

        IMPRTANT: you should set scale of your variables  with  MinLBFGSSetScale()
        call  (before  or after MinLBFGSSetPrecScale() call). Without knowledge of
        the scale of your variables scale-based preconditioner will be  just  unit
        matrix.

        INPUT PARAMETERS:
            State   -   structure which stores algorithm state

          -- ALGLIB --
             Copyright 13.10.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void minlbfgssetprecscale(minlbfgsstate state)
        {
            state.prectype = 3;
        }


        /*************************************************************************
        NOTES:

        1. This function has two different implementations: one which  uses  exact
           (analytical) user-supplied gradient,  and one which uses function value
           only  and  numerically  differentiates  function  in  order  to  obtain
           gradient.

           Depending  on  the  specific  function  used to create optimizer object
           (either MinLBFGSCreate() for analytical gradient  or  MinLBFGSCreateF()
           for numerical differentiation) you should choose appropriate variant of
           MinLBFGSOptimize() - one  which  accepts  function  AND gradient or one
           which accepts function ONLY.

           Be careful to choose variant of MinLBFGSOptimize() which corresponds to
           your optimization scheme! Table below lists different  combinations  of
           callback (function/gradient) passed to MinLBFGSOptimize()  and specific
           function used to create optimizer.


                             |         USER PASSED TO MinLBFGSOptimize()
           CREATED WITH      |  function only   |  function and gradient
           ------------------------------------------------------------
           MinLBFGSCreateF() |     work                FAIL
           MinLBFGSCreate()  |     FAIL                work

           Here "FAIL" denotes inappropriate combinations  of  optimizer  creation
           function  and  MinLBFGSOptimize()  version.   Attemps   to   use   such
           combination (for example, to create optimizer with MinLBFGSCreateF() and
           to pass gradient information to MinCGOptimize()) will lead to exception
           being thrown. Either  you  did  not pass gradient when it WAS needed or
           you passed gradient when it was NOT needed.

          -- ALGLIB --
             Copyright 20.03.2009 by Bochkanov Sergey
        *************************************************************************/
        public static bool minlbfgsiteration(minlbfgsstate state)
        {
            bool result = new bool();
            int n = 0;
            int m = 0;
            int i = 0;
            int j = 0;
            int ic = 0;
            int mcinfo = 0;
            double v = 0;
            double vv = 0;
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
                m = state.rstate.ia[1];
                i = state.rstate.ia[2];
                j = state.rstate.ia[3];
                ic = state.rstate.ia[4];
                mcinfo = state.rstate.ia[5];
                v = state.rstate.ra[0];
                vv = state.rstate.ra[1];
            }
            else
            {
                n = -983;
                m = -989;
                i = -834;
                j = 900;
                ic = -287;
                mcinfo = 364;
                v = 214;
                vv = -338;
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
            if( state.rstate.stage==4 )
            {
                goto lbl_4;
            }
            if( state.rstate.stage==5 )
            {
                goto lbl_5;
            }
            if( state.rstate.stage==6 )
            {
                goto lbl_6;
            }
            if( state.rstate.stage==7 )
            {
                goto lbl_7;
            }
            if( state.rstate.stage==8 )
            {
                goto lbl_8;
            }
            if( state.rstate.stage==9 )
            {
                goto lbl_9;
            }
            if( state.rstate.stage==10 )
            {
                goto lbl_10;
            }
            if( state.rstate.stage==11 )
            {
                goto lbl_11;
            }
            if( state.rstate.stage==12 )
            {
                goto lbl_12;
            }
            if( state.rstate.stage==13 )
            {
                goto lbl_13;
            }
            if( state.rstate.stage==14 )
            {
                goto lbl_14;
            }
            if( state.rstate.stage==15 )
            {
                goto lbl_15;
            }
            if( state.rstate.stage==16 )
            {
                goto lbl_16;
            }
            
            //
            // Routine body
            //
            
            //
            // Unload frequently used variables from State structure
            // (just for typing convinience)
            //
            n = state.n;
            m = state.m;
            state.repterminationtype = 0;
            state.repiterationscount = 0;
            state.repvaridx = -1;
            state.repnfev = 0;
            
            //
            //  Check, that transferred derivative value is right
            //
            clearrequestfields(state);
            if( !((double)(state.diffstep)==(double)(0) && (double)(state.teststep)>(double)(0)) )
            {
                goto lbl_17;
            }
            state.needfg = true;
            i = 0;
        lbl_19:
            if( i>n-1 )
            {
                goto lbl_21;
            }
            v = state.x[i];
            state.x[i] = v-state.teststep*state.s[i];
            state.rstate.stage = 0;
            goto lbl_rcomm;
        lbl_0:
            state.fm1 = state.f;
            state.fp1 = state.g[i];
            state.x[i] = v+state.teststep*state.s[i];
            state.rstate.stage = 1;
            goto lbl_rcomm;
        lbl_1:
            state.fm2 = state.f;
            state.fp2 = state.g[i];
            state.x[i] = v;
            state.rstate.stage = 2;
            goto lbl_rcomm;
        lbl_2:
            
            //
            // 2*State.TestStep   -   scale parameter
            // width of segment [Xi-TestStep;Xi+TestStep]
            //
            if( !optserv.derivativecheck(state.fm1, state.fp1, state.fm2, state.fp2, state.f, state.g[i], 2*state.teststep) )
            {
                state.repvaridx = i;
                state.repterminationtype = -7;
                result = false;
                return result;
            }
            i = i+1;
            goto lbl_19;
        lbl_21:
            state.needfg = false;
        lbl_17:
            
            //
            // Calculate F/G at the initial point
            //
            clearrequestfields(state);
            if( (double)(state.diffstep)!=(double)(0) )
            {
                goto lbl_22;
            }
            state.needfg = true;
            state.rstate.stage = 3;
            goto lbl_rcomm;
        lbl_3:
            state.needfg = false;
            goto lbl_23;
        lbl_22:
            state.needf = true;
            state.rstate.stage = 4;
            goto lbl_rcomm;
        lbl_4:
            state.fbase = state.f;
            i = 0;
        lbl_24:
            if( i>n-1 )
            {
                goto lbl_26;
            }
            v = state.x[i];
            state.x[i] = v-state.diffstep*state.s[i];
            state.rstate.stage = 5;
            goto lbl_rcomm;
        lbl_5:
            state.fm2 = state.f;
            state.x[i] = v-0.5*state.diffstep*state.s[i];
            state.rstate.stage = 6;
            goto lbl_rcomm;
        lbl_6:
            state.fm1 = state.f;
            state.x[i] = v+0.5*state.diffstep*state.s[i];
            state.rstate.stage = 7;
            goto lbl_rcomm;
        lbl_7:
            state.fp1 = state.f;
            state.x[i] = v+state.diffstep*state.s[i];
            state.rstate.stage = 8;
            goto lbl_rcomm;
        lbl_8:
            state.fp2 = state.f;
            state.x[i] = v;
            state.g[i] = (8*(state.fp1-state.fm1)-(state.fp2-state.fm2))/(6*state.diffstep*state.s[i]);
            i = i+1;
            goto lbl_24;
        lbl_26:
            state.f = state.fbase;
            state.needf = false;
        lbl_23:
            optserv.trimprepare(state.f, ref state.trimthreshold);
            if( !state.xrep )
            {
                goto lbl_27;
            }
            clearrequestfields(state);
            state.xupdated = true;
            state.rstate.stage = 9;
            goto lbl_rcomm;
        lbl_9:
            state.xupdated = false;
        lbl_27:
            state.repnfev = 1;
            state.fold = state.f;
            v = 0;
            for(i=0; i<=n-1; i++)
            {
                v = v+math.sqr(state.g[i]*state.s[i]);
            }
            if( (double)(Math.Sqrt(v))<=(double)(state.epsg) )
            {
                state.repterminationtype = 4;
                result = false;
                return result;
            }
            
            //
            // Choose initial step and direction.
            // Apply preconditioner, if we have something other than default.
            //
            for(i_=0; i_<=n-1;i_++)
            {
                state.d[i_] = -state.g[i_];
            }
            if( state.prectype==0 )
            {
                
                //
                // Default preconditioner is used, but we can't use it before iterations will start
                //
                v = 0.0;
                for(i_=0; i_<=n-1;i_++)
                {
                    v += state.g[i_]*state.g[i_];
                }
                v = Math.Sqrt(v);
                if( (double)(state.stpmax)==(double)(0) )
                {
                    state.stp = Math.Min(1.0/v, 1);
                }
                else
                {
                    state.stp = Math.Min(1.0/v, state.stpmax);
                }
            }
            if( state.prectype==1 )
            {
                
                //
                // Cholesky preconditioner is used
                //
                fbls.fblscholeskysolve(state.denseh, 1.0, n, true, ref state.d, ref state.autobuf);
                state.stp = 1;
            }
            if( state.prectype==2 )
            {
                
                //
                // diagonal approximation is used
                //
                for(i=0; i<=n-1; i++)
                {
                    state.d[i] = state.d[i]/state.diagh[i];
                }
                state.stp = 1;
            }
            if( state.prectype==3 )
            {
                
                //
                // scale-based preconditioner is used
                //
                for(i=0; i<=n-1; i++)
                {
                    state.d[i] = state.d[i]*state.s[i]*state.s[i];
                }
                state.stp = 1;
            }
            
            //
            // Main cycle
            //
            state.k = 0;
        lbl_29:
            if( false )
            {
                goto lbl_30;
            }
            
            //
            // Main cycle: prepare to 1-D line search
            //
            state.p = state.k%m;
            state.q = Math.Min(state.k, m-1);
            
            //
            // Store X[k], G[k]
            //
            for(i_=0; i_<=n-1;i_++)
            {
                state.sk[state.p,i_] = -state.x[i_];
            }
            for(i_=0; i_<=n-1;i_++)
            {
                state.yk[state.p,i_] = -state.g[i_];
            }
            
            //
            // Minimize F(x+alpha*d)
            // Calculate S[k], Y[k]
            //
            state.mcstage = 0;
            if( state.k!=0 )
            {
                state.stp = 1.0;
            }
            linmin.linminnormalized(ref state.d, ref state.stp, n);
            linmin.mcsrch(n, ref state.x, ref state.f, ref state.g, state.d, ref state.stp, state.stpmax, gtol, ref mcinfo, ref state.nfev, ref state.work, state.lstate, ref state.mcstage);
        lbl_31:
            if( state.mcstage==0 )
            {
                goto lbl_32;
            }
            clearrequestfields(state);
            if( (double)(state.diffstep)!=(double)(0) )
            {
                goto lbl_33;
            }
            state.needfg = true;
            state.rstate.stage = 10;
            goto lbl_rcomm;
        lbl_10:
            state.needfg = false;
            goto lbl_34;
        lbl_33:
            state.needf = true;
            state.rstate.stage = 11;
            goto lbl_rcomm;
        lbl_11:
            state.fbase = state.f;
            i = 0;
        lbl_35:
            if( i>n-1 )
            {
                goto lbl_37;
            }
            v = state.x[i];
            state.x[i] = v-state.diffstep*state.s[i];
            state.rstate.stage = 12;
            goto lbl_rcomm;
        lbl_12:
            state.fm2 = state.f;
            state.x[i] = v-0.5*state.diffstep*state.s[i];
            state.rstate.stage = 13;
            goto lbl_rcomm;
        lbl_13:
            state.fm1 = state.f;
            state.x[i] = v+0.5*state.diffstep*state.s[i];
            state.rstate.stage = 14;
            goto lbl_rcomm;
        lbl_14:
            state.fp1 = state.f;
            state.x[i] = v+state.diffstep*state.s[i];
            state.rstate.stage = 15;
            goto lbl_rcomm;
        lbl_15:
            state.fp2 = state.f;
            state.x[i] = v;
            state.g[i] = (8*(state.fp1-state.fm1)-(state.fp2-state.fm2))/(6*state.diffstep*state.s[i]);
            i = i+1;
            goto lbl_35;
        lbl_37:
            state.f = state.fbase;
            state.needf = false;
        lbl_34:
            optserv.trimfunction(ref state.f, ref state.g, n, state.trimthreshold);
            linmin.mcsrch(n, ref state.x, ref state.f, ref state.g, state.d, ref state.stp, state.stpmax, gtol, ref mcinfo, ref state.nfev, ref state.work, state.lstate, ref state.mcstage);
            goto lbl_31;
        lbl_32:
            if( !state.xrep )
            {
                goto lbl_38;
            }
            
            //
            // report
            //
            clearrequestfields(state);
            state.xupdated = true;
            state.rstate.stage = 16;
            goto lbl_rcomm;
        lbl_16:
            state.xupdated = false;
        lbl_38:
            state.repnfev = state.repnfev+state.nfev;
            state.repiterationscount = state.repiterationscount+1;
            for(i_=0; i_<=n-1;i_++)
            {
                state.sk[state.p,i_] = state.sk[state.p,i_] + state.x[i_];
            }
            for(i_=0; i_<=n-1;i_++)
            {
                state.yk[state.p,i_] = state.yk[state.p,i_] + state.g[i_];
            }
            
            //
            // Stopping conditions
            //
            if( state.repiterationscount>=state.maxits && state.maxits>0 )
            {
                
                //
                // Too many iterations
                //
                state.repterminationtype = 5;
                result = false;
                return result;
            }
            v = 0;
            for(i=0; i<=n-1; i++)
            {
                v = v+math.sqr(state.g[i]*state.s[i]);
            }
            if( (double)(Math.Sqrt(v))<=(double)(state.epsg) )
            {
                
                //
                // Gradient is small enough
                //
                state.repterminationtype = 4;
                result = false;
                return result;
            }
            if( (double)(state.fold-state.f)<=(double)(state.epsf*Math.Max(Math.Abs(state.fold), Math.Max(Math.Abs(state.f), 1.0))) )
            {
                
                //
                // F(k+1)-F(k) is small enough
                //
                state.repterminationtype = 1;
                result = false;
                return result;
            }
            v = 0;
            for(i=0; i<=n-1; i++)
            {
                v = v+math.sqr(state.sk[state.p,i]/state.s[i]);
            }
            if( (double)(Math.Sqrt(v))<=(double)(state.epsx) )
            {
                
                //
                // X(k+1)-X(k) is small enough
                //
                state.repterminationtype = 2;
                result = false;
                return result;
            }
            
            //
            // If Wolfe conditions are satisfied, we can update
            // limited memory model.
            //
            // However, if conditions are not satisfied (NFEV limit is met,
            // function is too wild, ...), we'll skip L-BFGS update
            //
            if( mcinfo!=1 )
            {
                
                //
                // Skip update.
                //
                // In such cases we'll initialize search direction by
                // antigradient vector, because it  leads to more
                // transparent code with less number of special cases
                //
                state.fold = state.f;
                for(i_=0; i_<=n-1;i_++)
                {
                    state.d[i_] = -state.g[i_];
                }
            }
            else
            {
                
                //
                // Calculate Rho[k], GammaK
                //
                v = 0.0;
                for(i_=0; i_<=n-1;i_++)
                {
                    v += state.yk[state.p,i_]*state.sk[state.p,i_];
                }
                vv = 0.0;
                for(i_=0; i_<=n-1;i_++)
                {
                    vv += state.yk[state.p,i_]*state.yk[state.p,i_];
                }
                if( (double)(v)==(double)(0) || (double)(vv)==(double)(0) )
                {
                    
                    //
                    // Rounding errors make further iterations impossible.
                    //
                    state.repterminationtype = -2;
                    result = false;
                    return result;
                }
                state.rho[state.p] = 1/v;
                state.gammak = v/vv;
                
                //
                //  Calculate d(k+1) = -H(k+1)*g(k+1)
                //
                //  for I:=K downto K-Q do
                //      V = s(i)^T * work(iteration:I)
                //      theta(i) = V
                //      work(iteration:I+1) = work(iteration:I) - V*Rho(i)*y(i)
                //  work(last iteration) = H0*work(last iteration) - preconditioner
                //  for I:=K-Q to K do
                //      V = y(i)^T*work(iteration:I)
                //      work(iteration:I+1) = work(iteration:I) +(-V+theta(i))*Rho(i)*s(i)
                //
                //  NOW WORK CONTAINS d(k+1)
                //
                for(i_=0; i_<=n-1;i_++)
                {
                    state.work[i_] = state.g[i_];
                }
                for(i=state.k; i>=state.k-state.q; i--)
                {
                    ic = i%m;
                    v = 0.0;
                    for(i_=0; i_<=n-1;i_++)
                    {
                        v += state.sk[ic,i_]*state.work[i_];
                    }
                    state.theta[ic] = v;
                    vv = v*state.rho[ic];
                    for(i_=0; i_<=n-1;i_++)
                    {
                        state.work[i_] = state.work[i_] - vv*state.yk[ic,i_];
                    }
                }
                if( state.prectype==0 )
                {
                    
                    //
                    // Simple preconditioner is used
                    //
                    v = state.gammak;
                    for(i_=0; i_<=n-1;i_++)
                    {
                        state.work[i_] = v*state.work[i_];
                    }
                }
                if( state.prectype==1 )
                {
                    
                    //
                    // Cholesky preconditioner is used
                    //
                    fbls.fblscholeskysolve(state.denseh, 1, n, true, ref state.work, ref state.autobuf);
                }
                if( state.prectype==2 )
                {
                    
                    //
                    // diagonal approximation is used
                    //
                    for(i=0; i<=n-1; i++)
                    {
                        state.work[i] = state.work[i]/state.diagh[i];
                    }
                }
                if( state.prectype==3 )
                {
                    
                    //
                    // scale-based preconditioner is used
                    //
                    for(i=0; i<=n-1; i++)
                    {
                        state.work[i] = state.work[i]*state.s[i]*state.s[i];
                    }
                }
                for(i=state.k-state.q; i<=state.k; i++)
                {
                    ic = i%m;
                    v = 0.0;
                    for(i_=0; i_<=n-1;i_++)
                    {
                        v += state.yk[ic,i_]*state.work[i_];
                    }
                    vv = state.rho[ic]*(-v+state.theta[ic]);
                    for(i_=0; i_<=n-1;i_++)
                    {
                        state.work[i_] = state.work[i_] + vv*state.sk[ic,i_];
                    }
                }
                for(i_=0; i_<=n-1;i_++)
                {
                    state.d[i_] = -state.work[i_];
                }
                
                //
                // Next step
                //
                state.fold = state.f;
                state.k = state.k+1;
            }
            goto lbl_29;
        lbl_30:
            result = false;
            return result;
            
            //
            // Saving state
            //
        lbl_rcomm:
            result = true;
            state.rstate.ia[0] = n;
            state.rstate.ia[1] = m;
            state.rstate.ia[2] = i;
            state.rstate.ia[3] = j;
            state.rstate.ia[4] = ic;
            state.rstate.ia[5] = mcinfo;
            state.rstate.ra[0] = v;
            state.rstate.ra[1] = vv;
            return result;
        }


        /*************************************************************************
        L-BFGS algorithm results

        INPUT PARAMETERS:
            State   -   algorithm state

        OUTPUT PARAMETERS:
            X       -   array[0..N-1], solution
            Rep     -   optimization report:
                        * Rep.TerminationType completetion code:
                            * -7    gradient verification failed.
                                    See MinLBFGSSetGradientCheck() for more information.
                            * -2    rounding errors prevent further improvement.
                                    X contains best point found.
                            * -1    incorrect parameters were specified
                            *  1    relative function improvement is no more than
                                    EpsF.
                            *  2    relative step is no more than EpsX.
                            *  4    gradient norm is no more than EpsG
                            *  5    MaxIts steps was taken
                            *  7    stopping conditions are too stringent,
                                    further improvement is impossible
                        * Rep.IterationsCount contains iterations count
                        * NFEV countains number of function calculations

          -- ALGLIB --
             Copyright 02.04.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void minlbfgsresults(minlbfgsstate state,
            ref double[] x,
            minlbfgsreport rep)
        {
            x = new double[0];

            minlbfgsresultsbuf(state, ref x, rep);
        }


        /*************************************************************************
        L-BFGS algorithm results

        Buffered implementation of MinLBFGSResults which uses pre-allocated buffer
        to store X[]. If buffer size is  too  small,  it  resizes  buffer.  It  is
        intended to be used in the inner cycles of performance critical algorithms
        where array reallocation penalty is too large to be ignored.

          -- ALGLIB --
             Copyright 20.08.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void minlbfgsresultsbuf(minlbfgsstate state,
            ref double[] x,
            minlbfgsreport rep)
        {
            int i_ = 0;

            if( alglib.ap.len(x)<state.n )
            {
                x = new double[state.n];
            }
            for(i_=0; i_<=state.n-1;i_++)
            {
                x[i_] = state.x[i_];
            }
            rep.iterationscount = state.repiterationscount;
            rep.nfev = state.repnfev;
            rep.varidx = state.repvaridx;
            rep.terminationtype = state.repterminationtype;
        }


        /*************************************************************************
        This  subroutine restarts LBFGS algorithm from new point. All optimization
        parameters are left unchanged.

        This  function  allows  to  solve multiple  optimization  problems  (which
        must have same number of dimensions) without object reallocation penalty.

        INPUT PARAMETERS:
            State   -   structure used to store algorithm state
            X       -   new starting point.

          -- ALGLIB --
             Copyright 30.07.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void minlbfgsrestartfrom(minlbfgsstate state,
            double[] x)
        {
            int i_ = 0;

            alglib.ap.assert(alglib.ap.len(x)>=state.n, "MinLBFGSRestartFrom: Length(X)<N!");
            alglib.ap.assert(apserv.isfinitevector(x, state.n), "MinLBFGSRestartFrom: X contains infinite or NaN values!");
            for(i_=0; i_<=state.n-1;i_++)
            {
                state.x[i_] = x[i_];
            }
            state.rstate.ia = new int[5+1];
            state.rstate.ra = new double[1+1];
            state.rstate.stage = -1;
            clearrequestfields(state);
        }


        /*************************************************************************
        This  subroutine  turns  on  verification  of  the  user-supplied analytic
        gradient:
        * user calls this subroutine before optimization begins
        * MinLBFGSOptimize() is called
        * prior to  actual  optimization, for each component  of  parameters being
          optimized X[i] algorithm performs following steps:
          * two trial steps are made to X[i]-TestStep*S[i] and X[i]+TestStep*S[i],
            where X[i] is i-th component of the initial point and S[i] is a  scale
            of i-th parameter
          * if needed, steps are bounded with respect to constraints on X[]
          * F(X) is evaluated at these trial points
          * we perform one more evaluation in the middle point of the interval
          * we  build  cubic  model using function values and derivatives at trial
            points and we compare its prediction with actual value in  the  middle
            point
          * in case difference between prediction and actual value is higher  than
            some predetermined threshold, algorithm stops with completion code -7;
            Rep.VarIdx is set to index of the parameter with incorrect derivative.
        * after verification is over, algorithm proceeds to the actual optimization.

        NOTE 1: verification  needs  N (parameters count) gradient evaluations. It
                is very costly and you should use  it  only  for  low  dimensional
                problems,  when  you  want  to  be  sure  that  you've   correctly
                calculated  analytic  derivatives.  You  should  not use it in the
                production code (unless you want to check derivatives provided  by
                some third party).

        NOTE 2: you  should  carefully  choose  TestStep. Value which is too large
                (so large that function behaviour is significantly non-cubic) will
                lead to false alarms. You may use  different  step  for  different
                parameters by means of setting scale with MinLBFGSSetScale().

        NOTE 3: this function may lead to false positives. In case it reports that
                I-th  derivative was calculated incorrectly, you may decrease test
                step  and  try  one  more  time  - maybe your function changes too
                sharply  and  your  step  is  too  large for such rapidly chanding
                function.

        INPUT PARAMETERS:
            State       -   structure used to store algorithm state
            TestStep    -   verification step:
                            * TestStep=0 turns verification off
                            * TestStep>0 activates verification

          -- ALGLIB --
             Copyright 24.05.2012 by Bochkanov Sergey
        *************************************************************************/
        public static void minlbfgssetgradientcheck(minlbfgsstate state,
            double teststep)
        {
            alglib.ap.assert(math.isfinite(teststep), "MinLBFGSSetGradientCheck: TestStep contains NaN or Infinite");
            alglib.ap.assert((double)(teststep)>=(double)(0), "MinLBFGSSetGradientCheck: invalid argument TestStep(TestStep<0)");
            state.teststep = teststep;
        }


        /*************************************************************************
        Clears request fileds (to be sure that we don't forgot to clear something)
        *************************************************************************/
        private static void clearrequestfields(minlbfgsstate state)
        {
            state.needf = false;
            state.needfg = false;
            state.xupdated = false;
        }


    }
    public class minqp
    {
        /*************************************************************************
        This object stores nonlinear optimizer state.
        You should use functions provided by MinQP subpackage to work with this
        object
        *************************************************************************/
        public class minqpstate : apobject
        {
            public int n;
            public int algokind;
            public cqmodels.convexquadraticmodel a;
            public double anorm;
            public double[] b;
            public double[] bndl;
            public double[] bndu;
            public double[] s;
            public bool[] havebndl;
            public bool[] havebndu;
            public double[] xorigin;
            public double[] startx;
            public bool havex;
            public double[,] cleic;
            public int nec;
            public int nic;
            public sactivesets.sactiveset sas;
            public double[] gc;
            public double[] xn;
            public double[] pg;
            public double[] workbndl;
            public double[] workbndu;
            public double[,] workcleic;
            public double[] xs;
            public int repinneriterationscount;
            public int repouteriterationscount;
            public int repncholesky;
            public int repnmv;
            public int repterminationtype;
            public double debugphase1flops;
            public double debugphase2flops;
            public double debugphase3flops;
            public double[] tmp0;
            public double[] tmp1;
            public bool[] tmpb;
            public double[] rctmpg;
            public normestimator.normestimatorstate estimator;
            public minqpstate()
            {
                init();
            }
            public override void init()
            {
                a = new cqmodels.convexquadraticmodel();
                b = new double[0];
                bndl = new double[0];
                bndu = new double[0];
                s = new double[0];
                havebndl = new bool[0];
                havebndu = new bool[0];
                xorigin = new double[0];
                startx = new double[0];
                cleic = new double[0,0];
                sas = new sactivesets.sactiveset();
                gc = new double[0];
                xn = new double[0];
                pg = new double[0];
                workbndl = new double[0];
                workbndu = new double[0];
                workcleic = new double[0,0];
                xs = new double[0];
                tmp0 = new double[0];
                tmp1 = new double[0];
                tmpb = new bool[0];
                rctmpg = new double[0];
                estimator = new normestimator.normestimatorstate();
            }
            public override alglib.apobject make_copy()
            {
                minqpstate _result = new minqpstate();
                _result.n = n;
                _result.algokind = algokind;
                _result.a = (cqmodels.convexquadraticmodel)a.make_copy();
                _result.anorm = anorm;
                _result.b = (double[])b.Clone();
                _result.bndl = (double[])bndl.Clone();
                _result.bndu = (double[])bndu.Clone();
                _result.s = (double[])s.Clone();
                _result.havebndl = (bool[])havebndl.Clone();
                _result.havebndu = (bool[])havebndu.Clone();
                _result.xorigin = (double[])xorigin.Clone();
                _result.startx = (double[])startx.Clone();
                _result.havex = havex;
                _result.cleic = (double[,])cleic.Clone();
                _result.nec = nec;
                _result.nic = nic;
                _result.sas = (sactivesets.sactiveset)sas.make_copy();
                _result.gc = (double[])gc.Clone();
                _result.xn = (double[])xn.Clone();
                _result.pg = (double[])pg.Clone();
                _result.workbndl = (double[])workbndl.Clone();
                _result.workbndu = (double[])workbndu.Clone();
                _result.workcleic = (double[,])workcleic.Clone();
                _result.xs = (double[])xs.Clone();
                _result.repinneriterationscount = repinneriterationscount;
                _result.repouteriterationscount = repouteriterationscount;
                _result.repncholesky = repncholesky;
                _result.repnmv = repnmv;
                _result.repterminationtype = repterminationtype;
                _result.debugphase1flops = debugphase1flops;
                _result.debugphase2flops = debugphase2flops;
                _result.debugphase3flops = debugphase3flops;
                _result.tmp0 = (double[])tmp0.Clone();
                _result.tmp1 = (double[])tmp1.Clone();
                _result.tmpb = (bool[])tmpb.Clone();
                _result.rctmpg = (double[])rctmpg.Clone();
                _result.estimator = (normestimator.normestimatorstate)estimator.make_copy();
                return _result;
            }
        };


        /*************************************************************************
        This structure stores optimization report:
        * InnerIterationsCount      number of inner iterations
        * OuterIterationsCount      number of outer iterations
        * NCholesky                 number of Cholesky decomposition
        * NMV                       number of matrix-vector products
                                    (only products calculated as part of iterative
                                    process are counted)
        * TerminationType           completion code (see below)

        Completion codes:
        * -5    inappropriate solver was used:
                * Cholesky solver for semidefinite or indefinite problems
                * Cholesky solver for problems with non-boundary constraints
        * -3    inconsistent constraints (or, maybe, feasible point is
                too hard to find). If you are sure that constraints are feasible,
                try to restart optimizer with better initial approximation.
        * -1    solver error
        *  4    successful completion
        *  5    MaxIts steps was taken
        *  7    stopping conditions are too stringent,
                further improvement is impossible,
                X contains best point found so far.
        *************************************************************************/
        public class minqpreport : apobject
        {
            public int inneriterationscount;
            public int outeriterationscount;
            public int nmv;
            public int ncholesky;
            public int terminationtype;
            public minqpreport()
            {
                init();
            }
            public override void init()
            {
            }
            public override alglib.apobject make_copy()
            {
                minqpreport _result = new minqpreport();
                _result.inneriterationscount = inneriterationscount;
                _result.outeriterationscount = outeriterationscount;
                _result.nmv = nmv;
                _result.ncholesky = ncholesky;
                _result.terminationtype = terminationtype;
                return _result;
            }
        };




        public const int maxlagrangeits = 10;
        public const int maxbadnewtonits = 7;


        /*************************************************************************
                            CONSTRAINED QUADRATIC PROGRAMMING

        The subroutine creates QP optimizer. After initial creation,  it  contains
        default optimization problem with zero quadratic and linear terms  and  no
        constraints. You should set quadratic/linear terms with calls to functions
        provided by MinQP subpackage.

        INPUT PARAMETERS:
            N       -   problem size
            
        OUTPUT PARAMETERS:
            State   -   optimizer with zero quadratic/linear terms
                        and no constraints

          -- ALGLIB --
             Copyright 11.01.2011 by Bochkanov Sergey
        *************************************************************************/
        public static void minqpcreate(int n,
            minqpstate state)
        {
            int i = 0;

            alglib.ap.assert(n>=1, "MinQPCreate: N<1");
            
            //
            // initialize QP solver
            //
            state.n = n;
            state.nec = 0;
            state.nic = 0;
            state.repterminationtype = 0;
            state.anorm = 1;
            cqmodels.cqminit(n, state.a);
            sactivesets.sasinit(n, state.sas);
            state.b = new double[n];
            state.bndl = new double[n];
            state.bndu = new double[n];
            state.workbndl = new double[n];
            state.workbndu = new double[n];
            state.havebndl = new bool[n];
            state.havebndu = new bool[n];
            state.s = new double[n];
            state.startx = new double[n];
            state.xorigin = new double[n];
            state.xs = new double[n];
            state.xn = new double[n];
            state.gc = new double[n];
            state.pg = new double[n];
            for(i=0; i<=n-1; i++)
            {
                state.bndl[i] = Double.NegativeInfinity;
                state.bndu[i] = Double.PositiveInfinity;
                state.havebndl[i] = false;
                state.havebndu[i] = false;
                state.b[i] = 0.0;
                state.startx[i] = 0.0;
                state.xorigin[i] = 0.0;
                state.s[i] = 1.0;
            }
            state.havex = false;
            minqpsetalgocholesky(state);
            normestimator.normestimatorcreate(n, n, 5, 5, state.estimator);
        }


        /*************************************************************************
        This function sets linear term for QP solver.

        By default, linear term is zero.

        INPUT PARAMETERS:
            State   -   structure which stores algorithm state
            B       -   linear term, array[N].

          -- ALGLIB --
             Copyright 11.01.2011 by Bochkanov Sergey
        *************************************************************************/
        public static void minqpsetlinearterm(minqpstate state,
            double[] b)
        {
            int n = 0;

            n = state.n;
            alglib.ap.assert(alglib.ap.len(b)>=n, "MinQPSetLinearTerm: Length(B)<N");
            alglib.ap.assert(apserv.isfinitevector(b, n), "MinQPSetLinearTerm: B contains infinite or NaN elements");
            minqpsetlineartermfast(state, b);
        }


        /*************************************************************************
        This function sets quadratic term for QP solver.

        By default quadratic term is zero.

        IMPORTANT: this solver minimizes following  function:
            f(x) = 0.5*x'*A*x + b'*x.
        Note that quadratic term has 0.5 before it. So if  you  want  to  minimize
            f(x) = x^2 + x
        you should rewrite your problem as follows:
            f(x) = 0.5*(2*x^2) + x
        and your matrix A will be equal to [[2.0]], not to [[1.0]]

        INPUT PARAMETERS:
            State   -   structure which stores algorithm state
            A       -   matrix, array[N,N]
            IsUpper -   (optional) storage type:
                        * if True, symmetric matrix  A  is  given  by  its  upper
                          triangle, and the lower triangle isn’t used
                        * if False, symmetric matrix  A  is  given  by  its lower
                          triangle, and the upper triangle isn’t used
                        * if not given, both lower and upper  triangles  must  be
                          filled.

          -- ALGLIB --
             Copyright 11.01.2011 by Bochkanov Sergey
        *************************************************************************/
        public static void minqpsetquadraticterm(minqpstate state,
            double[,] a,
            bool isupper)
        {
            int n = 0;

            n = state.n;
            alglib.ap.assert(alglib.ap.rows(a)>=n, "MinQPSetQuadraticTerm: Rows(A)<N");
            alglib.ap.assert(alglib.ap.cols(a)>=n, "MinQPSetQuadraticTerm: Cols(A)<N");
            alglib.ap.assert(apserv.isfinitertrmatrix(a, n, isupper), "MinQPSetQuadraticTerm: A contains infinite or NaN elements");
            minqpsetquadratictermfast(state, a, isupper, 0.0);
        }


        /*************************************************************************
        This function sets starting point for QP solver. It is useful to have
        good initial approximation to the solution, because it will increase
        speed of convergence and identification of active constraints.

        INPUT PARAMETERS:
            State   -   structure which stores algorithm state
            X       -   starting point, array[N].

          -- ALGLIB --
             Copyright 11.01.2011 by Bochkanov Sergey
        *************************************************************************/
        public static void minqpsetstartingpoint(minqpstate state,
            double[] x)
        {
            int n = 0;

            n = state.n;
            alglib.ap.assert(alglib.ap.len(x)>=n, "MinQPSetStartingPoint: Length(B)<N");
            alglib.ap.assert(apserv.isfinitevector(x, n), "MinQPSetStartingPoint: X contains infinite or NaN elements");
            minqpsetstartingpointfast(state, x);
        }


        /*************************************************************************
        This  function sets origin for QP solver. By default, following QP program
        is solved:

            min(0.5*x'*A*x+b'*x)
            
        This function allows to solve different problem:

            min(0.5*(x-x_origin)'*A*(x-x_origin)+b'*(x-x_origin))
            
        INPUT PARAMETERS:
            State   -   structure which stores algorithm state
            XOrigin -   origin, array[N].

          -- ALGLIB --
             Copyright 11.01.2011 by Bochkanov Sergey
        *************************************************************************/
        public static void minqpsetorigin(minqpstate state,
            double[] xorigin)
        {
            int n = 0;

            n = state.n;
            alglib.ap.assert(alglib.ap.len(xorigin)>=n, "MinQPSetOrigin: Length(B)<N");
            alglib.ap.assert(apserv.isfinitevector(xorigin, n), "MinQPSetOrigin: B contains infinite or NaN elements");
            minqpsetoriginfast(state, xorigin);
        }


        /*************************************************************************
        This function tells solver to use Cholesky-based algorithm.

        Cholesky-based algorithm can be used when:
        * problem is convex
        * there is no constraints or only boundary constraints are present

        This algorithm has O(N^3) complexity for unconstrained problem and  is  up
        to several times slower on bound constrained  problems  (these  additional
        iterations are needed to identify active constraints).

        INPUT PARAMETERS:
            State   -   structure which stores algorithm state

          -- ALGLIB --
             Copyright 11.01.2011 by Bochkanov Sergey
        *************************************************************************/
        public static void minqpsetalgocholesky(minqpstate state)
        {
            state.algokind = 1;
        }


        /*************************************************************************
        This function sets boundary constraints for QP solver

        Boundary constraints are inactive by default (after initial creation).
        After  being  set,  they  are  preserved  until explicitly turned off with
        another SetBC() call.

        INPUT PARAMETERS:
            State   -   structure stores algorithm state
            BndL    -   lower bounds, array[N].
                        If some (all) variables are unbounded, you may specify
                        very small number or -INF (latter is recommended because
                        it will allow solver to use better algorithm).
            BndU    -   upper bounds, array[N].
                        If some (all) variables are unbounded, you may specify
                        very large number or +INF (latter is recommended because
                        it will allow solver to use better algorithm).
                        
        NOTE: it is possible to specify BndL[i]=BndU[i]. In this case I-th
        variable will be "frozen" at X[i]=BndL[i]=BndU[i].

          -- ALGLIB --
             Copyright 11.01.2011 by Bochkanov Sergey
        *************************************************************************/
        public static void minqpsetbc(minqpstate state,
            double[] bndl,
            double[] bndu)
        {
            int i = 0;
            int n = 0;

            n = state.n;
            alglib.ap.assert(alglib.ap.len(bndl)>=n, "MinQPSetBC: Length(BndL)<N");
            alglib.ap.assert(alglib.ap.len(bndu)>=n, "MinQPSetBC: Length(BndU)<N");
            for(i=0; i<=n-1; i++)
            {
                alglib.ap.assert(math.isfinite(bndl[i]) || Double.IsNegativeInfinity(bndl[i]), "MinQPSetBC: BndL contains NAN or +INF");
                alglib.ap.assert(math.isfinite(bndu[i]) || Double.IsPositiveInfinity(bndu[i]), "MinQPSetBC: BndU contains NAN or -INF");
                state.bndl[i] = bndl[i];
                state.havebndl[i] = math.isfinite(bndl[i]);
                state.bndu[i] = bndu[i];
                state.havebndu[i] = math.isfinite(bndu[i]);
            }
        }


        /*************************************************************************
        This function sets linear constraints for QP optimizer.

        Linear constraints are inactive by default (after initial creation).

        INPUT PARAMETERS:
            State   -   structure previously allocated with MinQPCreate call.
            C       -   linear constraints, array[K,N+1].
                        Each row of C represents one constraint, either equality
                        or inequality (see below):
                        * first N elements correspond to coefficients,
                        * last element corresponds to the right part.
                        All elements of C (including right part) must be finite.
            CT      -   type of constraints, array[K]:
                        * if CT[i]>0, then I-th constraint is C[i,*]*x >= C[i,n+1]
                        * if CT[i]=0, then I-th constraint is C[i,*]*x  = C[i,n+1]
                        * if CT[i]<0, then I-th constraint is C[i,*]*x <= C[i,n+1]
            K       -   number of equality/inequality constraints, K>=0:
                        * if given, only leading K elements of C/CT are used
                        * if not given, automatically determined from sizes of C/CT

        NOTE 1: linear (non-bound) constraints are satisfied only approximately  -
                there always exists some minor violation (about 10^-10...10^-13)
                due to numerical errors.

          -- ALGLIB --
             Copyright 19.06.2012 by Bochkanov Sergey
        *************************************************************************/
        public static void minqpsetlc(minqpstate state,
            double[,] c,
            int[] ct,
            int k)
        {
            int n = 0;
            int i = 0;
            int j = 0;
            double v = 0;
            int i_ = 0;

            n = state.n;
            
            //
            // First, check for errors in the inputs
            //
            alglib.ap.assert(k>=0, "MinQPSetLC: K<0");
            alglib.ap.assert(alglib.ap.cols(c)>=n+1 || k==0, "MinQPSetLC: Cols(C)<N+1");
            alglib.ap.assert(alglib.ap.rows(c)>=k, "MinQPSetLC: Rows(C)<K");
            alglib.ap.assert(alglib.ap.len(ct)>=k, "MinQPSetLC: Length(CT)<K");
            alglib.ap.assert(apserv.apservisfinitematrix(c, k, n+1), "MinQPSetLC: C contains infinite or NaN values!");
            
            //
            // Handle zero K
            //
            if( k==0 )
            {
                state.nec = 0;
                state.nic = 0;
                return;
            }
            
            //
            // Equality constraints are stored first, in the upper
            // NEC rows of State.CLEIC matrix. Inequality constraints
            // are stored in the next NIC rows.
            //
            // NOTE: we convert inequality constraints to the form
            // A*x<=b before copying them.
            //
            apserv.rmatrixsetlengthatleast(ref state.cleic, k, n+1);
            state.nec = 0;
            state.nic = 0;
            for(i=0; i<=k-1; i++)
            {
                if( ct[i]==0 )
                {
                    for(i_=0; i_<=n;i_++)
                    {
                        state.cleic[state.nec,i_] = c[i,i_];
                    }
                    state.nec = state.nec+1;
                }
            }
            for(i=0; i<=k-1; i++)
            {
                if( ct[i]!=0 )
                {
                    if( ct[i]>0 )
                    {
                        for(i_=0; i_<=n;i_++)
                        {
                            state.cleic[state.nec+state.nic,i_] = -c[i,i_];
                        }
                    }
                    else
                    {
                        for(i_=0; i_<=n;i_++)
                        {
                            state.cleic[state.nec+state.nic,i_] = c[i,i_];
                        }
                    }
                    state.nic = state.nic+1;
                }
            }
            
            //
            // Normalize rows of State.CLEIC: each row must have unit norm.
            // Norm is calculated using first N elements (i.e. right part is
            // not counted when we calculate norm).
            //
            for(i=0; i<=k-1; i++)
            {
                v = 0;
                for(j=0; j<=n-1; j++)
                {
                    v = v+math.sqr(state.cleic[i,j]);
                }
                if( (double)(v)==(double)(0) )
                {
                    continue;
                }
                v = 1/Math.Sqrt(v);
                for(i_=0; i_<=n;i_++)
                {
                    state.cleic[i,i_] = v*state.cleic[i,i_];
                }
            }
        }


        /*************************************************************************
        This function solves quadratic programming problem.
        You should call it after setting solver options with MinQPSet...() calls.

        INPUT PARAMETERS:
            State   -   algorithm state

        You should use MinQPResults() function to access results after calls
        to this function.

          -- ALGLIB --
             Copyright 11.01.2011 by Bochkanov Sergey.
             Special thanks to Elvira Illarionova  for  important  suggestions  on
             the linearly constrained QP algorithm.
        *************************************************************************/
        public static void minqpoptimize(minqpstate state)
        {
            int n = 0;
            int i = 0;
            int nbc = 0;
            double v0 = 0;
            double v1 = 0;
            double v = 0;
            double d2 = 0;
            double d1 = 0;
            double d0 = 0;
            double noisetolerance = 0;
            double fprev = 0;
            double fcand = 0;
            double fcur = 0;
            int nextaction = 0;
            int actstatus = 0;
            double noiselevel = 0;
            int badnewtonits = 0;
            int i_ = 0;

            noisetolerance = 10;
            n = state.n;
            state.repterminationtype = -5;
            state.repinneriterationscount = 0;
            state.repouteriterationscount = 0;
            state.repncholesky = 0;
            state.repnmv = 0;
            state.debugphase1flops = 0;
            state.debugphase2flops = 0;
            state.debugphase3flops = 0;
            apserv.rvectorsetlengthatleast(ref state.rctmpg, n);
            
            //
            // check correctness of constraints
            //
            for(i=0; i<=n-1; i++)
            {
                if( state.havebndl[i] && state.havebndu[i] )
                {
                    if( (double)(state.bndl[i])>(double)(state.bndu[i]) )
                    {
                        state.repterminationtype = -3;
                        return;
                    }
                }
            }
            
            //
            // count number of bound and linear constraints
            //
            nbc = 0;
            for(i=0; i<=n-1; i++)
            {
                if( state.havebndl[i] )
                {
                    nbc = nbc+1;
                }
                if( state.havebndu[i] )
                {
                    nbc = nbc+1;
                }
            }
            
            //
            // Our formulation of quadratic problem includes origin point,
            // i.e. we have F(x-x_origin) which is minimized subject to
            // constraints on x, instead of having simply F(x).
            //
            // Here we make transition from non-zero origin to zero one.
            // In order to make such transition we have to:
            // 1. subtract x_origin from x_start
            // 2. modify constraints
            // 3. solve problem
            // 4. add x_origin to solution
            //
            // There is alternate solution - to modify quadratic function
            // by expansion of multipliers containing (x-x_origin), but
            // we prefer to modify constraints, because it is a) more precise
            // and b) easier to to.
            //
            // Parts (1)-(2) are done here. After this block is over,
            // we have:
            // * XS, which stores shifted XStart (if we don't have XStart,
            //   value of XS will be ignored later)
            // * WorkBndL, WorkBndU, which store modified boundary constraints.
            //
            for(i=0; i<=n-1; i++)
            {
                if( state.havebndl[i] )
                {
                    state.workbndl[i] = state.bndl[i]-state.xorigin[i];
                }
                else
                {
                    state.workbndl[i] = Double.NegativeInfinity;
                }
                if( state.havebndu[i] )
                {
                    state.workbndu[i] = state.bndu[i]-state.xorigin[i];
                }
                else
                {
                    state.workbndu[i] = Double.PositiveInfinity;
                }
            }
            apserv.rmatrixsetlengthatleast(ref state.workcleic, state.nec+state.nic, n+1);
            for(i=0; i<=state.nec+state.nic-1; i++)
            {
                v = 0.0;
                for(i_=0; i_<=n-1;i_++)
                {
                    v += state.cleic[i,i_]*state.xorigin[i_];
                }
                for(i_=0; i_<=n-1;i_++)
                {
                    state.workcleic[i,i_] = state.cleic[i,i_];
                }
                state.workcleic[i,n] = state.cleic[i,n]-v;
            }
            
            //
            // modify starting point XS according to boundary constraints
            //
            if( state.havex )
            {
                
                //
                // We have starting point in StartX, so we just have to shift and bound it
                //
                for(i=0; i<=n-1; i++)
                {
                    state.xs[i] = state.startx[i]-state.xorigin[i];
                    if( state.havebndl[i] )
                    {
                        if( (double)(state.xs[i])<(double)(state.workbndl[i]) )
                        {
                            state.xs[i] = state.workbndl[i];
                        }
                    }
                    if( state.havebndu[i] )
                    {
                        if( (double)(state.xs[i])>(double)(state.workbndu[i]) )
                        {
                            state.xs[i] = state.workbndu[i];
                        }
                    }
                }
            }
            else
            {
                
                //
                // We don't have starting point, so we deduce it from
                // constraints (if they are present).
                //
                // NOTE: XS contains some meaningless values from previous block
                // which are ignored by code below.
                //
                for(i=0; i<=n-1; i++)
                {
                    if( state.havebndl[i] && state.havebndu[i] )
                    {
                        state.xs[i] = 0.5*(state.workbndl[i]+state.workbndu[i]);
                        if( (double)(state.xs[i])<(double)(state.workbndl[i]) )
                        {
                            state.xs[i] = state.workbndl[i];
                        }
                        if( (double)(state.xs[i])>(double)(state.workbndu[i]) )
                        {
                            state.xs[i] = state.workbndu[i];
                        }
                        continue;
                    }
                    if( state.havebndl[i] )
                    {
                        state.xs[i] = state.workbndl[i];
                        continue;
                    }
                    if( state.havebndu[i] )
                    {
                        state.xs[i] = state.workbndu[i];
                        continue;
                    }
                    state.xs[i] = 0;
                }
            }
            
            //
            // Select algo
            //
            if( state.algokind==1 )
            {
                
                //
                // Cholesky-based algorithm for dense bound constrained problems.
                //
                // This algorithm exists in two variants:
                // * unconstrained one, which can solve problem using only one NxN
                //   double matrix
                // * bound constrained one, which needs two NxN matrices
                //
                // We will try to solve problem using unconstrained algorithm,
                // and will use bound constrained version only when constraints
                // are actually present
                //
                if( nbc==0 && state.nec+state.nic==0 )
                {
                    
                    //
                    // "Simple" unconstrained version
                    //
                    apserv.bvectorsetlengthatleast(ref state.tmpb, n);
                    for(i=0; i<=n-1; i++)
                    {
                        state.tmpb[i] = false;
                    }
                    state.repncholesky = state.repncholesky+1;
                    cqmodels.cqmsetb(state.a, state.b);
                    cqmodels.cqmsetactiveset(state.a, state.xs, state.tmpb);
                    if( !cqmodels.cqmconstrainedoptimum(state.a, ref state.xn) )
                    {
                        state.repterminationtype = -5;
                        return;
                    }
                    for(i_=0; i_<=n-1;i_++)
                    {
                        state.xs[i_] = state.xn[i_];
                    }
                    for(i_=0; i_<=n-1;i_++)
                    {
                        state.xs[i_] = state.xs[i_] + state.xorigin[i_];
                    }
                    state.repinneriterationscount = 1;
                    state.repouteriterationscount = 1;
                    state.repterminationtype = 4;
                    return;
                }
                sactivesets.sassetbc(state.sas, state.workbndl, state.workbndu);
                sactivesets.sassetlcx(state.sas, state.workcleic, state.nec, state.nic);
                sactivesets.sassetscale(state.sas, state.s);
                if( !sactivesets.sasstartoptimization(state.sas, state.xs) )
                {
                    state.repterminationtype = -3;
                    return;
                }
                
                //
                // Main cycle of BLEIC-QP algorithm
                //
                state.repterminationtype = 4;
                badnewtonits = 0;
                while( true )
                {
                    
                    //
                    // Update iterations count
                    //
                    state.repinneriterationscount = state.repinneriterationscount+1;
                    
                    //
                    // Phase 1: determine active set
                    //
                    cqmodels.cqmadx(state.a, state.sas.xc, ref state.rctmpg);
                    for(i_=0; i_<=n-1;i_++)
                    {
                        state.rctmpg[i_] = state.rctmpg[i_] + state.b[i_];
                    }
                    sactivesets.sasreactivateconstraints(state.sas, state.rctmpg);
                    
                    //
                    // Phase 2: perform penalized steepest descent step.
                    //
                    // NextAction control variable is set on exit from this loop:
                    // * NextAction>0 in case we have to proceed to Phase 3 (Newton step)
                    // * NextAction<0 in case we have to proceed to Phase 1 (recalculate active set)
                    // * NextAction=0 in case we found solution (step along projected gradient is small enough)
                    //
                    while( true )
                    {
                        
                        //
                        // Calculate constrained descent direction, store to PG
                        //
                        cqmodels.cqmadx(state.a, state.sas.xc, ref state.gc);
                        for(i_=0; i_<=n-1;i_++)
                        {
                            state.gc[i_] = state.gc[i_] + state.b[i_];
                        }
                        sactivesets.sasconstraineddescent(state.sas, state.gc, ref state.pg);
                        state.debugphase2flops = state.debugphase2flops+4*(state.nec+state.nic)*n;
                        
                        //
                        // Build quadratic model of F along descent direction:
                        //     F(xc+alpha*pg) = D2*alpha^2 + D1*alpha + D0
                        // Store noise level in the XC (noise level is used to classify
                        // step as singificant or insignificant).
                        //
                        // In case function curvature is negative or product of descent
                        // direction and gradient is non-negative, iterations are terminated.
                        //
                        // NOTE: D0 is not actually used, but we prefer to maintain it.
                        //
                        fprev = minqpmodelvalue(state.a, state.b, state.sas.xc, n, ref state.tmp0);
                        cqmodels.cqmevalx(state.a, state.sas.xc, ref v, ref noiselevel);
                        v0 = cqmodels.cqmxtadx2(state.a, state.pg);
                        state.debugphase2flops = state.debugphase2flops+3*2*n*n;
                        d2 = v0;
                        v1 = 0.0;
                        for(i_=0; i_<=n-1;i_++)
                        {
                            v1 += state.pg[i_]*state.gc[i_];
                        }
                        d1 = v1;
                        d0 = fprev;
                        if( (double)(d2)<=(double)(0) || (double)(d1)>=(double)(0) )
                        {
                            nextaction = 0;
                            break;
                        }
                        
                        //
                        // Modify quadratic model - add penalty for violation of the active
                        // constraints.
                        //
                        // Boundary constraints are always satisfied exactly, so we do not
                        // add penalty term for them. General equality constraint of the
                        // form a'*(xc+alpha*d)=b adds penalty term:
                        //     P(alpha) = (a'*(xc+alpha*d)-b)^2
                        //              = (alpha*(a'*d) + (a'*xc-b))^2
                        //              = alpha^2*(a'*d)^2 + alpha*2*(a'*d)*(a'*xc-b) + (a'*xc-b)^2
                        // Each penalty term is multiplied by 100*Anorm before adding it to
                        // the 1-dimensional quadratic model.
                        //
                        // Penalization of the quadratic model improves behavior of the
                        // algorithm in the presense of the multiple degenerate constraints.
                        // In particular, it prevents algorithm from making large steps in
                        // directions which violate equality constraints.
                        //
                        for(i=0; i<=state.nec+state.nic-1; i++)
                        {
                            if( state.sas.activeset[n+i]>0 )
                            {
                                v0 = 0.0;
                                for(i_=0; i_<=n-1;i_++)
                                {
                                    v0 += state.workcleic[i,i_]*state.pg[i_];
                                }
                                v1 = 0.0;
                                for(i_=0; i_<=n-1;i_++)
                                {
                                    v1 += state.workcleic[i,i_]*state.sas.xc[i_];
                                }
                                v1 = v1-state.workcleic[i,n];
                                v = 100*state.anorm;
                                d2 = d2+v*math.sqr(v0);
                                d1 = d1+v*2*v0*v1;
                                d0 = d0+v*math.sqr(v1);
                            }
                        }
                        state.debugphase2flops = state.debugphase2flops+2*2*(state.nec+state.nic)*n;
                        
                        //
                        // Try unbounded step.
                        // In case function change is dominated by noise or function actually increased
                        // instead of decreasing, we terminate iterations.
                        //
                        v = -(d1/(2*d2));
                        for(i_=0; i_<=n-1;i_++)
                        {
                            state.xn[i_] = state.sas.xc[i_];
                        }
                        for(i_=0; i_<=n-1;i_++)
                        {
                            state.xn[i_] = state.xn[i_] + v*state.pg[i_];
                        }
                        fcand = minqpmodelvalue(state.a, state.b, state.xn, n, ref state.tmp0);
                        state.debugphase2flops = state.debugphase2flops+2*n*n;
                        if( (double)(fcand)>=(double)(fprev-noiselevel*noisetolerance) )
                        {
                            nextaction = 0;
                            break;
                        }
                        
                        //
                        // Save active set
                        // Perform bounded step with (possible) activation
                        //
                        actstatus = minqpboundedstepandactivation(state, state.xn, ref state.tmp0);
                        fcur = minqpmodelvalue(state.a, state.b, state.sas.xc, n, ref state.tmp0);
                        state.debugphase2flops = state.debugphase2flops+2*n*n;
                        
                        //
                        // Depending on results, decide what to do:
                        // 1. In case step was performed without activation of constraints,
                        //    we proceed to Newton method
                        // 2. In case there was activated at least one constraint with ActiveSet[I]<0,
                        //    we proceed to Phase 1 and re-evaluate active set.
                        // 3. Otherwise (activation of the constraints with ActiveSet[I]=0)
                        //    we try Phase 2 one more time.
                        //
                        if( actstatus<0 )
                        {
                            
                            //
                            // Step without activation, proceed to Newton
                            //
                            nextaction = 1;
                            break;
                        }
                        if( actstatus==0 )
                        {
                            
                            //
                            // No new constraints added during last activation - only
                            // ones which were at the boundary (ActiveSet[I]=0), but
                            // inactive due to numerical noise.
                            //
                            // Now, these constraints are added to the active set, and
                            // we try to perform steepest descent (Phase 2) one more time.
                            //
                            continue;
                        }
                        else
                        {
                            
                            //
                            // Last step activated at least one significantly new
                            // constraint (ActiveSet[I]<0), we have to re-evaluate
                            // active set (Phase 1).
                            //
                            nextaction = -1;
                            break;
                        }
                    }
                    if( nextaction<0 )
                    {
                        continue;
                    }
                    if( nextaction==0 )
                    {
                        break;
                    }
                    
                    //
                    // Phase 3: Newton method.
                    //
                    // NOTE: this phase uses Augmented Lagrangian algorithm to solve
                    //       equality-constrained subproblems. This algorithm may
                    //       perform steps which increase function values instead of
                    //       decreasing it (in hard cases, like overconstrained problems).
                    //
                    //       Such non-monononic steps may create a loop, when Augmented
                    //       Lagrangian algorithm performs uphill step, and steepest
                    //       descent algorithm (Phase 2) performs downhill step in the
                    //       opposite direction.
                    //
                    //       In order to prevent iterations to continue forever we
                    //       count iterations when AL algorithm increased function
                    //       value instead of decreasing it. When number of such "bad"
                    //       iterations will increase beyong MaxBadNewtonIts, we will
                    //       terminate algorithm.
                    //
                    fprev = minqpmodelvalue(state.a, state.b, state.sas.xc, n, ref state.tmp0);
                    while( true )
                    {
                        
                        //
                        // Calculate optimum subject to presently active constraints
                        //
                        state.repncholesky = state.repncholesky+1;
                        state.debugphase3flops = state.debugphase3flops+Math.Pow(n, 3)/3;
                        if( !minqpconstrainedoptimum(state, state.a, state.anorm, state.b, ref state.xn, ref state.tmp0, ref state.tmpb, ref state.tmp1) )
                        {
                            state.repterminationtype = -5;
                            sactivesets.sasstopoptimization(state.sas);
                            return;
                        }
                        
                        //
                        // Add constraints.
                        // If no constraints was added, accept candidate point XN and move to next phase.
                        //
                        if( minqpboundedstepandactivation(state, state.xn, ref state.tmp0)<0 )
                        {
                            break;
                        }
                    }
                    fcur = minqpmodelvalue(state.a, state.b, state.sas.xc, n, ref state.tmp0);
                    if( (double)(fcur)>=(double)(fprev) )
                    {
                        badnewtonits = badnewtonits+1;
                    }
                    if( badnewtonits>=maxbadnewtonits )
                    {
                        
                        //
                        // Algorithm found solution, but keeps iterating because Newton
                        // algorithm performs uphill steps (noise in the Augmented Lagrangian
                        // algorithm). We terminate algorithm; it is considered normal
                        // termination.
                        //
                        break;
                    }
                }
                state.repouteriterationscount = 1;
                sactivesets.sasstopoptimization(state.sas);
                
                //
                // Post-process: add XOrigin to XC
                //
                for(i=0; i<=n-1; i++)
                {
                    if( state.havebndl[i] && (double)(state.sas.xc[i])==(double)(state.workbndl[i]) )
                    {
                        state.xs[i] = state.bndl[i];
                        continue;
                    }
                    if( state.havebndu[i] && (double)(state.sas.xc[i])==(double)(state.workbndu[i]) )
                    {
                        state.xs[i] = state.bndu[i];
                        continue;
                    }
                    state.xs[i] = apserv.boundval(state.sas.xc[i]+state.xorigin[i], state.bndl[i], state.bndu[i]);
                }
                return;
            }
        }


        /*************************************************************************
        QP solver results

        INPUT PARAMETERS:
            State   -   algorithm state

        OUTPUT PARAMETERS:
            X       -   array[0..N-1], solution
            Rep     -   optimization report. You should check Rep.TerminationType,
                        which contains completion code, and you may check  another
                        fields which contain another information  about  algorithm
                        functioning.

          -- ALGLIB --
             Copyright 11.01.2011 by Bochkanov Sergey
        *************************************************************************/
        public static void minqpresults(minqpstate state,
            ref double[] x,
            minqpreport rep)
        {
            x = new double[0];

            minqpresultsbuf(state, ref x, rep);
        }


        /*************************************************************************
        QP results

        Buffered implementation of MinQPResults() which uses pre-allocated  buffer
        to store X[]. If buffer size is  too  small,  it  resizes  buffer.  It  is
        intended to be used in the inner cycles of performance critical algorithms
        where array reallocation penalty is too large to be ignored.

          -- ALGLIB --
             Copyright 11.01.2011 by Bochkanov Sergey
        *************************************************************************/
        public static void minqpresultsbuf(minqpstate state,
            ref double[] x,
            minqpreport rep)
        {
            int i_ = 0;

            if( alglib.ap.len(x)<state.n )
            {
                x = new double[state.n];
            }
            for(i_=0; i_<=state.n-1;i_++)
            {
                x[i_] = state.xs[i_];
            }
            rep.inneriterationscount = state.repinneriterationscount;
            rep.outeriterationscount = state.repouteriterationscount;
            rep.nmv = state.repnmv;
            rep.ncholesky = state.repncholesky;
            rep.terminationtype = state.repterminationtype;
        }


        /*************************************************************************
        Fast version of MinQPSetLinearTerm(), which doesn't check its arguments.
        For internal use only.

          -- ALGLIB --
             Copyright 11.01.2011 by Bochkanov Sergey
        *************************************************************************/
        public static void minqpsetlineartermfast(minqpstate state,
            double[] b)
        {
            int i_ = 0;

            for(i_=0; i_<=state.n-1;i_++)
            {
                state.b[i_] = b[i_];
            }
        }


        /*************************************************************************
        Fast version of MinQPSetQuadraticTerm(), which doesn't check its arguments.

        It accepts additional parameter - shift S, which allows to "shift"  matrix
        A by adding s*I to A. S must be positive (although it is not checked).

        For internal use only.

          -- ALGLIB --
             Copyright 11.01.2011 by Bochkanov Sergey
        *************************************************************************/
        public static void minqpsetquadratictermfast(minqpstate state,
            double[,] a,
            bool isupper,
            double s)
        {
            int i = 0;
            int j = 0;
            int n = 0;

            n = state.n;
            cqmodels.cqmseta(state.a, a, isupper, 1.0);
            if( (double)(s)>(double)(0) )
            {
                apserv.rvectorsetlengthatleast(ref state.tmp0, n);
                for(i=0; i<=n-1; i++)
                {
                    state.tmp0[i] = a[i,i]+s;
                }
                cqmodels.cqmrewritedensediagonal(state.a, state.tmp0);
            }
            
            //
            // Estimate norm of A
            // (it will be used later in the quadratic penalty function)
            //
            state.anorm = 0;
            for(i=0; i<=n-1; i++)
            {
                if( isupper )
                {
                    for(j=i; j<=n-1; j++)
                    {
                        state.anorm = Math.Max(state.anorm, Math.Abs(a[i,j]));
                    }
                }
                else
                {
                    for(j=0; j<=i; j++)
                    {
                        state.anorm = Math.Max(state.anorm, Math.Abs(a[i,j]));
                    }
                }
            }
            state.anorm = state.anorm*n;
        }


        /*************************************************************************
        Internal function which allows to rewrite diagonal of quadratic term.
        For internal use only.

        This function can be used only when you have dense A and already made
        MinQPSetQuadraticTerm(Fast) call.

          -- ALGLIB --
             Copyright 16.01.2011 by Bochkanov Sergey
        *************************************************************************/
        public static void minqprewritediagonal(minqpstate state,
            double[] s)
        {
            cqmodels.cqmrewritedensediagonal(state.a, s);
        }


        /*************************************************************************
        Fast version of MinQPSetStartingPoint(), which doesn't check its arguments.
        For internal use only.

          -- ALGLIB --
             Copyright 11.01.2011 by Bochkanov Sergey
        *************************************************************************/
        public static void minqpsetstartingpointfast(minqpstate state,
            double[] x)
        {
            int n = 0;
            int i_ = 0;

            n = state.n;
            for(i_=0; i_<=n-1;i_++)
            {
                state.startx[i_] = x[i_];
            }
            state.havex = true;
        }


        /*************************************************************************
        Fast version of MinQPSetOrigin(), which doesn't check its arguments.
        For internal use only.

          -- ALGLIB --
             Copyright 11.01.2011 by Bochkanov Sergey
        *************************************************************************/
        public static void minqpsetoriginfast(minqpstate state,
            double[] xorigin)
        {
            int n = 0;
            int i_ = 0;

            n = state.n;
            for(i_=0; i_<=n-1;i_++)
            {
                state.xorigin[i_] = xorigin[i_];
            }
        }


        /*************************************************************************
        Having feasible current point XC and possibly infeasible candidate   point
        XN,  this  function  performs  longest  step  from  XC to XN which retains
        feasibility. In case XN is found to be infeasible, at least one constraint
        is activated.

        For example, if we have:
          XC=0.5
          XN=1.2
          x>=0, x<=1
        then this function will move us to X=1.0 and activate constraint "x<=1".

        INPUT PARAMETERS:
            State   -   MinQP state.
            XC      -   current point, must be feasible with respect to
                        all constraints
            XN      -   candidate point, can be infeasible with respect to some
                        constraints. Must be located in the subspace of current
                        active set, i.e. it is feasible with respect to already
                        active constraints.
            Buf     -   temporary buffer, automatically resized if needed

        OUTPUT PARAMETERS:
            State   -   this function changes following fields of State:
                        * State.ActiveSet
                        * State.ActiveC     -   active linear constraints
            XC      -   new position

        RESULT:
            >0, in case at least one inactive non-candidate constraint was activated
            =0, in case only "candidate" constraints were activated
            <0, in case no constraints were activated by the step


          -- ALGLIB --
             Copyright 29.02.2012 by Bochkanov Sergey
        *************************************************************************/
        private static int minqpboundedstepandactivation(minqpstate state,
            double[] xn,
            ref double[] buf)
        {
            int result = 0;
            int n = 0;
            double stpmax = 0;
            int cidx = 0;
            double cval = 0;
            bool needact = new bool();
            double v = 0;
            int i_ = 0;

            n = state.n;
            apserv.rvectorsetlengthatleast(ref buf, n);
            for(i_=0; i_<=n-1;i_++)
            {
                buf[i_] = xn[i_];
            }
            for(i_=0; i_<=n-1;i_++)
            {
                buf[i_] = buf[i_] - state.sas.xc[i_];
            }
            sactivesets.sasexploredirection(state.sas, buf, ref stpmax, ref cidx, ref cval);
            needact = (double)(stpmax)<=(double)(1);
            v = Math.Min(stpmax, 1.0);
            for(i_=0; i_<=n-1;i_++)
            {
                buf[i_] = v*buf[i_];
            }
            for(i_=0; i_<=n-1;i_++)
            {
                buf[i_] = buf[i_] + state.sas.xc[i_];
            }
            result = sactivesets.sasmoveto(state.sas, buf, needact, cidx, cval);
            return result;
        }


        /*************************************************************************
        Model value: f = 0.5*x'*A*x + b'*x

        INPUT PARAMETERS:
            A       -   convex quadratic model; only main quadratic term is used,
                        other parts of the model (D/Q/linear term) are ignored.
                        This function does not modify model state.
            B       -   right part
            XC      -   evaluation point
            Tmp     -   temporary buffer, automatically resized if needed

          -- ALGLIB --
             Copyright 20.06.2012 by Bochkanov Sergey
        *************************************************************************/
        private static double minqpmodelvalue(cqmodels.convexquadraticmodel a,
            double[] b,
            double[] xc,
            int n,
            ref double[] tmp)
        {
            double result = 0;
            double v0 = 0;
            double v1 = 0;
            int i_ = 0;

            apserv.rvectorsetlengthatleast(ref tmp, n);
            cqmodels.cqmadx(a, xc, ref tmp);
            v0 = 0.0;
            for(i_=0; i_<=n-1;i_++)
            {
                v0 += xc[i_]*tmp[i_];
            }
            v1 = 0.0;
            for(i_=0; i_<=n-1;i_++)
            {
                v1 += xc[i_]*b[i_];
            }
            result = 0.5*v0+v1;
            return result;
        }


        /*************************************************************************
        Optimum of A subject to:
        a) active boundary constraints (given by ActiveSet[] and corresponding
           elements of XC)
        b) active linear constraints (given by C, R, LagrangeC)

        INPUT PARAMETERS:
            A       -   main quadratic term of the model;
                        although structure may  store  linear  and  rank-K  terms,
                        these terms are ignored and rewritten  by  this  function.
            ANorm   -   estimate of ||A|| (2-norm is used)
            B       -   array[N], linear term of the model
            XN      -   possibly preallocated buffer
            Tmp     -   temporary buffer (automatically resized)
            Tmp1    -   temporary buffer (automatically resized)

        OUTPUT PARAMETERS:
            A       -   modified quadratic model (this function changes rank-K
                        term and linear term of the model)
            LagrangeC-  current estimate of the Lagrange coefficients
            XN      -   solution

        RESULT:
            True on success, False on failure (non-SPD model)

          -- ALGLIB --
             Copyright 20.06.2012 by Bochkanov Sergey
        *************************************************************************/
        private static bool minqpconstrainedoptimum(minqpstate state,
            cqmodels.convexquadraticmodel a,
            double anorm,
            double[] b,
            ref double[] xn,
            ref double[] tmp,
            ref bool[] tmpb,
            ref double[] lagrangec)
        {
            bool result = new bool();
            int itidx = 0;
            int i = 0;
            double v = 0;
            double feaserrold = 0;
            double feaserrnew = 0;
            double theta = 0;
            int n = 0;
            int i_ = 0;

            n = state.n;
            
            //
            // Rebuild basis accroding to current active set.
            // We call SASRebuildBasis() to make sure that fields of SAS
            // store up to date values.
            //
            sactivesets.sasrebuildbasis(state.sas);
            
            //
            // Allocate temporaries.
            //
            apserv.rvectorsetlengthatleast(ref tmp, Math.Max(n, state.sas.basissize));
            apserv.bvectorsetlengthatleast(ref tmpb, n);
            apserv.rvectorsetlengthatleast(ref lagrangec, state.sas.basissize);
            
            //
            // Prepare model
            //
            for(i=0; i<=state.sas.basissize-1; i++)
            {
                tmp[i] = state.sas.pbasis[i,n];
            }
            theta = 100.0*anorm;
            for(i=0; i<=n-1; i++)
            {
                if( state.sas.activeset[i]>0 )
                {
                    tmpb[i] = true;
                }
                else
                {
                    tmpb[i] = false;
                }
            }
            cqmodels.cqmsetactiveset(a, state.sas.xc, tmpb);
            cqmodels.cqmsetq(a, state.sas.pbasis, tmp, state.sas.basissize, theta);
            
            //
            // Iterate until optimal values of Lagrange multipliers are found
            //
            for(i=0; i<=state.sas.basissize-1; i++)
            {
                lagrangec[i] = 0;
            }
            feaserrnew = math.maxrealnumber;
            result = true;
            for(itidx=1; itidx<=maxlagrangeits; itidx++)
            {
                
                //
                // Generate right part B using linear term and current
                // estimate of the Lagrange multipliers.
                //
                for(i_=0; i_<=n-1;i_++)
                {
                    tmp[i_] = b[i_];
                }
                for(i=0; i<=state.sas.basissize-1; i++)
                {
                    v = lagrangec[i];
                    for(i_=0; i_<=n-1;i_++)
                    {
                        tmp[i_] = tmp[i_] - v*state.sas.pbasis[i,i_];
                    }
                }
                cqmodels.cqmsetb(a, tmp);
                
                //
                // Solve
                //
                result = cqmodels.cqmconstrainedoptimum(a, ref xn);
                if( !result )
                {
                    return result;
                }
                
                //
                // Compare feasibility errors.
                // Terminate if error decreased too slowly.
                //
                feaserrold = feaserrnew;
                feaserrnew = 0;
                for(i=0; i<=state.sas.basissize-1; i++)
                {
                    v = 0.0;
                    for(i_=0; i_<=n-1;i_++)
                    {
                        v += state.sas.pbasis[i,i_]*xn[i_];
                    }
                    feaserrnew = feaserrnew+math.sqr(v-state.sas.pbasis[i,n]);
                }
                feaserrnew = Math.Sqrt(feaserrnew);
                if( (double)(feaserrnew)>=(double)(0.2*feaserrold) )
                {
                    break;
                }
                
                //
                // Update Lagrange multipliers
                //
                for(i=0; i<=state.sas.basissize-1; i++)
                {
                    v = 0.0;
                    for(i_=0; i_<=n-1;i_++)
                    {
                        v += state.sas.pbasis[i,i_]*xn[i_];
                    }
                    lagrangec[i] = lagrangec[i]-theta*(v-state.sas.pbasis[i,n]);
                }
            }
            return result;
        }


    }
    public class minlm
    {
        /*************************************************************************
        Levenberg-Marquardt optimizer.

        This structure should be created using one of the MinLMCreate???()
        functions. You should not access its fields directly; use ALGLIB functions
        to work with it.
        *************************************************************************/
        public class minlmstate : apobject
        {
            public int n;
            public int m;
            public double diffstep;
            public double epsg;
            public double epsf;
            public double epsx;
            public int maxits;
            public bool xrep;
            public double stpmax;
            public int maxmodelage;
            public bool makeadditers;
            public double[] x;
            public double f;
            public double[] fi;
            public double[,] j;
            public double[,] h;
            public double[] g;
            public bool needf;
            public bool needfg;
            public bool needfgh;
            public bool needfij;
            public bool needfi;
            public bool xupdated;
            public int algomode;
            public bool hasf;
            public bool hasfi;
            public bool hasg;
            public double[] xbase;
            public double fbase;
            public double[] fibase;
            public double[] gbase;
            public double[,] quadraticmodel;
            public double[] bndl;
            public double[] bndu;
            public bool[] havebndl;
            public bool[] havebndu;
            public double[] s;
            public double lambdav;
            public double nu;
            public int modelage;
            public double[] xdir;
            public double[] deltax;
            public double[] deltaf;
            public bool deltaxready;
            public bool deltafready;
            public double teststep;
            public int repiterationscount;
            public int repterminationtype;
            public int repfuncidx;
            public int repvaridx;
            public int repnfunc;
            public int repnjac;
            public int repngrad;
            public int repnhess;
            public int repncholesky;
            public rcommstate rstate;
            public double[] choleskybuf;
            public double[] tmp0;
            public double actualdecrease;
            public double predicteddecrease;
            public double xm1;
            public double xp1;
            public double[] fm1;
            public double[] fp1;
            public double[] fc1;
            public double[] gm1;
            public double[] gp1;
            public double[] gc1;
            public minlbfgs.minlbfgsstate internalstate;
            public minlbfgs.minlbfgsreport internalrep;
            public minqp.minqpstate qpstate;
            public minqp.minqpreport qprep;
            public minlmstate()
            {
                init();
            }
            public override void init()
            {
                x = new double[0];
                fi = new double[0];
                j = new double[0,0];
                h = new double[0,0];
                g = new double[0];
                xbase = new double[0];
                fibase = new double[0];
                gbase = new double[0];
                quadraticmodel = new double[0,0];
                bndl = new double[0];
                bndu = new double[0];
                havebndl = new bool[0];
                havebndu = new bool[0];
                s = new double[0];
                xdir = new double[0];
                deltax = new double[0];
                deltaf = new double[0];
                rstate = new rcommstate();
                choleskybuf = new double[0];
                tmp0 = new double[0];
                fm1 = new double[0];
                fp1 = new double[0];
                fc1 = new double[0];
                gm1 = new double[0];
                gp1 = new double[0];
                gc1 = new double[0];
                internalstate = new minlbfgs.minlbfgsstate();
                internalrep = new minlbfgs.minlbfgsreport();
                qpstate = new minqp.minqpstate();
                qprep = new minqp.minqpreport();
            }
            public override alglib.apobject make_copy()
            {
                minlmstate _result = new minlmstate();
                _result.n = n;
                _result.m = m;
                _result.diffstep = diffstep;
                _result.epsg = epsg;
                _result.epsf = epsf;
                _result.epsx = epsx;
                _result.maxits = maxits;
                _result.xrep = xrep;
                _result.stpmax = stpmax;
                _result.maxmodelage = maxmodelage;
                _result.makeadditers = makeadditers;
                _result.x = (double[])x.Clone();
                _result.f = f;
                _result.fi = (double[])fi.Clone();
                _result.j = (double[,])j.Clone();
                _result.h = (double[,])h.Clone();
                _result.g = (double[])g.Clone();
                _result.needf = needf;
                _result.needfg = needfg;
                _result.needfgh = needfgh;
                _result.needfij = needfij;
                _result.needfi = needfi;
                _result.xupdated = xupdated;
                _result.algomode = algomode;
                _result.hasf = hasf;
                _result.hasfi = hasfi;
                _result.hasg = hasg;
                _result.xbase = (double[])xbase.Clone();
                _result.fbase = fbase;
                _result.fibase = (double[])fibase.Clone();
                _result.gbase = (double[])gbase.Clone();
                _result.quadraticmodel = (double[,])quadraticmodel.Clone();
                _result.bndl = (double[])bndl.Clone();
                _result.bndu = (double[])bndu.Clone();
                _result.havebndl = (bool[])havebndl.Clone();
                _result.havebndu = (bool[])havebndu.Clone();
                _result.s = (double[])s.Clone();
                _result.lambdav = lambdav;
                _result.nu = nu;
                _result.modelage = modelage;
                _result.xdir = (double[])xdir.Clone();
                _result.deltax = (double[])deltax.Clone();
                _result.deltaf = (double[])deltaf.Clone();
                _result.deltaxready = deltaxready;
                _result.deltafready = deltafready;
                _result.teststep = teststep;
                _result.repiterationscount = repiterationscount;
                _result.repterminationtype = repterminationtype;
                _result.repfuncidx = repfuncidx;
                _result.repvaridx = repvaridx;
                _result.repnfunc = repnfunc;
                _result.repnjac = repnjac;
                _result.repngrad = repngrad;
                _result.repnhess = repnhess;
                _result.repncholesky = repncholesky;
                _result.rstate = (rcommstate)rstate.make_copy();
                _result.choleskybuf = (double[])choleskybuf.Clone();
                _result.tmp0 = (double[])tmp0.Clone();
                _result.actualdecrease = actualdecrease;
                _result.predicteddecrease = predicteddecrease;
                _result.xm1 = xm1;
                _result.xp1 = xp1;
                _result.fm1 = (double[])fm1.Clone();
                _result.fp1 = (double[])fp1.Clone();
                _result.fc1 = (double[])fc1.Clone();
                _result.gm1 = (double[])gm1.Clone();
                _result.gp1 = (double[])gp1.Clone();
                _result.gc1 = (double[])gc1.Clone();
                _result.internalstate = (minlbfgs.minlbfgsstate)internalstate.make_copy();
                _result.internalrep = (minlbfgs.minlbfgsreport)internalrep.make_copy();
                _result.qpstate = (minqp.minqpstate)qpstate.make_copy();
                _result.qprep = (minqp.minqpreport)qprep.make_copy();
                return _result;
            }
        };


        /*************************************************************************
        Optimization report, filled by MinLMResults() function

        FIELDS:
        * TerminationType, completetion code:
            * -7    derivative correctness check failed;
                    see Rep.WrongNum, Rep.WrongI, Rep.WrongJ for
                    more information.
            *  1    relative function improvement is no more than
                    EpsF.
            *  2    relative step is no more than EpsX.
            *  4    gradient is no more than EpsG.
            *  5    MaxIts steps was taken
            *  7    stopping conditions are too stringent,
                    further improvement is impossible
        * IterationsCount, contains iterations count
        * NFunc, number of function calculations
        * NJac, number of Jacobi matrix calculations
        * NGrad, number of gradient calculations
        * NHess, number of Hessian calculations
        * NCholesky, number of Cholesky decomposition calculations
        *************************************************************************/
        public class minlmreport : apobject
        {
            public int iterationscount;
            public int terminationtype;
            public int funcidx;
            public int varidx;
            public int nfunc;
            public int njac;
            public int ngrad;
            public int nhess;
            public int ncholesky;
            public minlmreport()
            {
                init();
            }
            public override void init()
            {
            }
            public override alglib.apobject make_copy()
            {
                minlmreport _result = new minlmreport();
                _result.iterationscount = iterationscount;
                _result.terminationtype = terminationtype;
                _result.funcidx = funcidx;
                _result.varidx = varidx;
                _result.nfunc = nfunc;
                _result.njac = njac;
                _result.ngrad = ngrad;
                _result.nhess = nhess;
                _result.ncholesky = ncholesky;
                return _result;
            }
        };




        public const double lambdaup = 2.0;
        public const double lambdadown = 0.33;
        public const double suspiciousnu = 16;
        public const int smallmodelage = 3;
        public const int additers = 5;


        /*************************************************************************
                        IMPROVED LEVENBERG-MARQUARDT METHOD FOR
                         NON-LINEAR LEAST SQUARES OPTIMIZATION

        DESCRIPTION:
        This function is used to find minimum of function which is represented  as
        sum of squares:
            F(x) = f[0]^2(x[0],...,x[n-1]) + ... + f[m-1]^2(x[0],...,x[n-1])
        using value of function vector f[] and Jacobian of f[].


        REQUIREMENTS:
        This algorithm will request following information during its operation:

        * function vector f[] at given point X
        * function vector f[] and Jacobian of f[] (simultaneously) at given point

        There are several overloaded versions of  MinLMOptimize()  function  which
        correspond  to  different LM-like optimization algorithms provided by this
        unit. You should choose version which accepts fvec()  and jac() callbacks.
        First  one  is used to calculate f[] at given point, second one calculates
        f[] and Jacobian df[i]/dx[j].

        You can try to initialize MinLMState structure with VJ  function and  then
        use incorrect version  of  MinLMOptimize()  (for  example,  version  which
        works  with  general  form function and does not provide Jacobian), but it
        will  lead  to  exception  being  thrown  after first attempt to calculate
        Jacobian.


        USAGE:
        1. User initializes algorithm state with MinLMCreateVJ() call
        2. User tunes solver parameters with MinLMSetCond(),  MinLMSetStpMax() and
           other functions
        3. User calls MinLMOptimize() function which  takes algorithm  state   and
           callback functions.
        4. User calls MinLMResults() to get solution
        5. Optionally, user may call MinLMRestartFrom() to solve  another  problem
           with same N/M but another starting point and/or another function.
           MinLMRestartFrom() allows to reuse already initialized structure.


        INPUT PARAMETERS:
            N       -   dimension, N>1
                        * if given, only leading N elements of X are used
                        * if not given, automatically determined from size of X
            M       -   number of functions f[i]
            X       -   initial solution, array[0..N-1]

        OUTPUT PARAMETERS:
            State   -   structure which stores algorithm state

        NOTES:
        1. you may tune stopping conditions with MinLMSetCond() function
        2. if target function contains exp() or other fast growing functions,  and
           optimization algorithm makes too large steps which leads  to  overflow,
           use MinLMSetStpMax() function to bound algorithm's steps.

          -- ALGLIB --
             Copyright 30.03.2009 by Bochkanov Sergey
        *************************************************************************/
        public static void minlmcreatevj(int n,
            int m,
            double[] x,
            minlmstate state)
        {
            alglib.ap.assert(n>=1, "MinLMCreateVJ: N<1!");
            alglib.ap.assert(m>=1, "MinLMCreateVJ: M<1!");
            alglib.ap.assert(alglib.ap.len(x)>=n, "MinLMCreateVJ: Length(X)<N!");
            alglib.ap.assert(apserv.isfinitevector(x, n), "MinLMCreateVJ: X contains infinite or NaN values!");
            
            //
            // initialize, check parameters
            //
            state.teststep = 0;
            state.n = n;
            state.m = m;
            state.algomode = 1;
            state.hasf = false;
            state.hasfi = true;
            state.hasg = false;
            
            //
            // second stage of initialization
            //
            lmprepare(n, m, false, state);
            minlmsetacctype(state, 0);
            minlmsetcond(state, 0, 0, 0, 0);
            minlmsetxrep(state, false);
            minlmsetstpmax(state, 0);
            minlmrestartfrom(state, x);
        }


        /*************************************************************************
                        IMPROVED LEVENBERG-MARQUARDT METHOD FOR
                         NON-LINEAR LEAST SQUARES OPTIMIZATION

        DESCRIPTION:
        This function is used to find minimum of function which is represented  as
        sum of squares:
            F(x) = f[0]^2(x[0],...,x[n-1]) + ... + f[m-1]^2(x[0],...,x[n-1])
        using value of function vector f[] only. Finite differences  are  used  to
        calculate Jacobian.


        REQUIREMENTS:
        This algorithm will request following information during its operation:
        * function vector f[] at given point X

        There are several overloaded versions of  MinLMOptimize()  function  which
        correspond  to  different LM-like optimization algorithms provided by this
        unit. You should choose version which accepts fvec() callback.

        You can try to initialize MinLMState structure with VJ  function and  then
        use incorrect version  of  MinLMOptimize()  (for  example,  version  which
        works with general form function and does not accept function vector), but
        it will  lead  to  exception being thrown after first attempt to calculate
        Jacobian.


        USAGE:
        1. User initializes algorithm state with MinLMCreateV() call
        2. User tunes solver parameters with MinLMSetCond(),  MinLMSetStpMax() and
           other functions
        3. User calls MinLMOptimize() function which  takes algorithm  state   and
           callback functions.
        4. User calls MinLMResults() to get solution
        5. Optionally, user may call MinLMRestartFrom() to solve  another  problem
           with same N/M but another starting point and/or another function.
           MinLMRestartFrom() allows to reuse already initialized structure.


        INPUT PARAMETERS:
            N       -   dimension, N>1
                        * if given, only leading N elements of X are used
                        * if not given, automatically determined from size of X
            M       -   number of functions f[i]
            X       -   initial solution, array[0..N-1]
            DiffStep-   differentiation step, >0

        OUTPUT PARAMETERS:
            State   -   structure which stores algorithm state

        See also MinLMIteration, MinLMResults.

        NOTES:
        1. you may tune stopping conditions with MinLMSetCond() function
        2. if target function contains exp() or other fast growing functions,  and
           optimization algorithm makes too large steps which leads  to  overflow,
           use MinLMSetStpMax() function to bound algorithm's steps.

          -- ALGLIB --
             Copyright 30.03.2009 by Bochkanov Sergey
        *************************************************************************/
        public static void minlmcreatev(int n,
            int m,
            double[] x,
            double diffstep,
            minlmstate state)
        {
            alglib.ap.assert(math.isfinite(diffstep), "MinLMCreateV: DiffStep is not finite!");
            alglib.ap.assert((double)(diffstep)>(double)(0), "MinLMCreateV: DiffStep<=0!");
            alglib.ap.assert(n>=1, "MinLMCreateV: N<1!");
            alglib.ap.assert(m>=1, "MinLMCreateV: M<1!");
            alglib.ap.assert(alglib.ap.len(x)>=n, "MinLMCreateV: Length(X)<N!");
            alglib.ap.assert(apserv.isfinitevector(x, n), "MinLMCreateV: X contains infinite or NaN values!");
            
            //
            // Initialize
            //
            state.teststep = 0;
            state.n = n;
            state.m = m;
            state.algomode = 0;
            state.hasf = false;
            state.hasfi = true;
            state.hasg = false;
            state.diffstep = diffstep;
            
            //
            // Second stage of initialization
            //
            lmprepare(n, m, false, state);
            minlmsetacctype(state, 1);
            minlmsetcond(state, 0, 0, 0, 0);
            minlmsetxrep(state, false);
            minlmsetstpmax(state, 0);
            minlmrestartfrom(state, x);
        }


        /*************************************************************************
            LEVENBERG-MARQUARDT-LIKE METHOD FOR NON-LINEAR OPTIMIZATION

        DESCRIPTION:
        This  function  is  used  to  find  minimum  of general form (not "sum-of-
        -squares") function
            F = F(x[0], ..., x[n-1])
        using  its  gradient  and  Hessian.  Levenberg-Marquardt modification with
        L-BFGS pre-optimization and internal pre-conditioned  L-BFGS  optimization
        after each Levenberg-Marquardt step is used.


        REQUIREMENTS:
        This algorithm will request following information during its operation:

        * function value F at given point X
        * F and gradient G (simultaneously) at given point X
        * F, G and Hessian H (simultaneously) at given point X

        There are several overloaded versions of  MinLMOptimize()  function  which
        correspond  to  different LM-like optimization algorithms provided by this
        unit. You should choose version which accepts func(),  grad()  and  hess()
        function pointers. First pointer is used to calculate F  at  given  point,
        second  one  calculates  F(x)  and  grad F(x),  third one calculates F(x),
        grad F(x), hess F(x).

        You can try to initialize MinLMState structure with FGH-function and  then
        use incorrect version of MinLMOptimize() (for example, version which  does
        not provide Hessian matrix), but it will lead to  exception  being  thrown
        after first attempt to calculate Hessian.


        USAGE:
        1. User initializes algorithm state with MinLMCreateFGH() call
        2. User tunes solver parameters with MinLMSetCond(),  MinLMSetStpMax() and
           other functions
        3. User calls MinLMOptimize() function which  takes algorithm  state   and
           pointers (delegates, etc.) to callback functions.
        4. User calls MinLMResults() to get solution
        5. Optionally, user may call MinLMRestartFrom() to solve  another  problem
           with same N but another starting point and/or another function.
           MinLMRestartFrom() allows to reuse already initialized structure.


        INPUT PARAMETERS:
            N       -   dimension, N>1
                        * if given, only leading N elements of X are used
                        * if not given, automatically determined from size of X
            X       -   initial solution, array[0..N-1]

        OUTPUT PARAMETERS:
            State   -   structure which stores algorithm state

        NOTES:
        1. you may tune stopping conditions with MinLMSetCond() function
        2. if target function contains exp() or other fast growing functions,  and
           optimization algorithm makes too large steps which leads  to  overflow,
           use MinLMSetStpMax() function to bound algorithm's steps.

          -- ALGLIB --
             Copyright 30.03.2009 by Bochkanov Sergey
        *************************************************************************/
        public static void minlmcreatefgh(int n,
            double[] x,
            minlmstate state)
        {
            alglib.ap.assert(n>=1, "MinLMCreateFGH: N<1!");
            alglib.ap.assert(alglib.ap.len(x)>=n, "MinLMCreateFGH: Length(X)<N!");
            alglib.ap.assert(apserv.isfinitevector(x, n), "MinLMCreateFGH: X contains infinite or NaN values!");
            
            //
            // initialize
            //
            state.teststep = 0;
            state.n = n;
            state.m = 0;
            state.algomode = 2;
            state.hasf = true;
            state.hasfi = false;
            state.hasg = true;
            
            //
            // init2
            //
            lmprepare(n, 0, true, state);
            minlmsetacctype(state, 2);
            minlmsetcond(state, 0, 0, 0, 0);
            minlmsetxrep(state, false);
            minlmsetstpmax(state, 0);
            minlmrestartfrom(state, x);
        }


        /*************************************************************************
        This function sets stopping conditions for Levenberg-Marquardt optimization
        algorithm.

        INPUT PARAMETERS:
            State   -   structure which stores algorithm state
            EpsG    -   >=0
                        The  subroutine  finishes  its  work   if   the  condition
                        |v|<EpsG is satisfied, where:
                        * |.| means Euclidian norm
                        * v - scaled gradient vector, v[i]=g[i]*s[i]
                        * g - gradient
                        * s - scaling coefficients set by MinLMSetScale()
            EpsF    -   >=0
                        The  subroutine  finishes  its work if on k+1-th iteration
                        the  condition  |F(k+1)-F(k)|<=EpsF*max{|F(k)|,|F(k+1)|,1}
                        is satisfied.
            EpsX    -   >=0
                        The subroutine finishes its work if  on  k+1-th  iteration
                        the condition |v|<=EpsX is fulfilled, where:
                        * |.| means Euclidian norm
                        * v - scaled step vector, v[i]=dx[i]/s[i]
                        * dx - ste pvector, dx=X(k+1)-X(k)
                        * s - scaling coefficients set by MinLMSetScale()
            MaxIts  -   maximum number of iterations. If MaxIts=0, the  number  of
                        iterations   is    unlimited.   Only   Levenberg-Marquardt
                        iterations  are  counted  (L-BFGS/CG  iterations  are  NOT
                        counted because their cost is very low compared to that of
                        LM).

        Passing EpsG=0, EpsF=0, EpsX=0 and MaxIts=0 (simultaneously) will lead to
        automatic stopping criterion selection (small EpsX).

          -- ALGLIB --
             Copyright 02.04.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void minlmsetcond(minlmstate state,
            double epsg,
            double epsf,
            double epsx,
            int maxits)
        {
            alglib.ap.assert(math.isfinite(epsg), "MinLMSetCond: EpsG is not finite number!");
            alglib.ap.assert((double)(epsg)>=(double)(0), "MinLMSetCond: negative EpsG!");
            alglib.ap.assert(math.isfinite(epsf), "MinLMSetCond: EpsF is not finite number!");
            alglib.ap.assert((double)(epsf)>=(double)(0), "MinLMSetCond: negative EpsF!");
            alglib.ap.assert(math.isfinite(epsx), "MinLMSetCond: EpsX is not finite number!");
            alglib.ap.assert((double)(epsx)>=(double)(0), "MinLMSetCond: negative EpsX!");
            alglib.ap.assert(maxits>=0, "MinLMSetCond: negative MaxIts!");
            if( (((double)(epsg)==(double)(0) && (double)(epsf)==(double)(0)) && (double)(epsx)==(double)(0)) && maxits==0 )
            {
                epsx = 1.0E-6;
            }
            state.epsg = epsg;
            state.epsf = epsf;
            state.epsx = epsx;
            state.maxits = maxits;
        }


        /*************************************************************************
        This function turns on/off reporting.

        INPUT PARAMETERS:
            State   -   structure which stores algorithm state
            NeedXRep-   whether iteration reports are needed or not

        If NeedXRep is True, algorithm will call rep() callback function if  it is
        provided to MinLMOptimize(). Both Levenberg-Marquardt and internal  L-BFGS
        iterations are reported.

          -- ALGLIB --
             Copyright 02.04.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void minlmsetxrep(minlmstate state,
            bool needxrep)
        {
            state.xrep = needxrep;
        }


        /*************************************************************************
        This function sets maximum step length

        INPUT PARAMETERS:
            State   -   structure which stores algorithm state
            StpMax  -   maximum step length, >=0. Set StpMax to 0.0,  if you don't
                        want to limit step length.

        Use this subroutine when you optimize target function which contains exp()
        or  other  fast  growing  functions,  and optimization algorithm makes too
        large  steps  which  leads  to overflow. This function allows us to reject
        steps  that  are  too  large  (and  therefore  expose  us  to the possible
        overflow) without actually calculating function value at the x+stp*d.

        NOTE: non-zero StpMax leads to moderate  performance  degradation  because
        intermediate  step  of  preconditioned L-BFGS optimization is incompatible
        with limits on step size.

          -- ALGLIB --
             Copyright 02.04.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void minlmsetstpmax(minlmstate state,
            double stpmax)
        {
            alglib.ap.assert(math.isfinite(stpmax), "MinLMSetStpMax: StpMax is not finite!");
            alglib.ap.assert((double)(stpmax)>=(double)(0), "MinLMSetStpMax: StpMax<0!");
            state.stpmax = stpmax;
        }


        /*************************************************************************
        This function sets scaling coefficients for LM optimizer.

        ALGLIB optimizers use scaling matrices to test stopping  conditions  (step
        size and gradient are scaled before comparison with tolerances).  Scale of
        the I-th variable is a translation invariant measure of:
        a) "how large" the variable is
        b) how large the step should be to make significant changes in the function

        Generally, scale is NOT considered to be a form of preconditioner.  But LM
        optimizer is unique in that it uses scaling matrix both  in  the  stopping
        condition tests and as Marquardt damping factor.

        Proper scaling is very important for the algorithm performance. It is less
        important for the quality of results, but still has some influence (it  is
        easier  to  converge  when  variables  are  properly  scaled, so premature
        stopping is possible when very badly scalled variables are  combined  with
        relaxed stopping conditions).

        INPUT PARAMETERS:
            State   -   structure stores algorithm state
            S       -   array[N], non-zero scaling coefficients
                        S[i] may be negative, sign doesn't matter.

          -- ALGLIB --
             Copyright 14.01.2011 by Bochkanov Sergey
        *************************************************************************/
        public static void minlmsetscale(minlmstate state,
            double[] s)
        {
            int i = 0;

            alglib.ap.assert(alglib.ap.len(s)>=state.n, "MinLMSetScale: Length(S)<N");
            for(i=0; i<=state.n-1; i++)
            {
                alglib.ap.assert(math.isfinite(s[i]), "MinLMSetScale: S contains infinite or NAN elements");
                alglib.ap.assert((double)(s[i])!=(double)(0), "MinLMSetScale: S contains zero elements");
                state.s[i] = Math.Abs(s[i]);
            }
        }


        /*************************************************************************
        This function sets boundary constraints for LM optimizer

        Boundary constraints are inactive by default (after initial creation).
        They are preserved until explicitly turned off with another SetBC() call.

        INPUT PARAMETERS:
            State   -   structure stores algorithm state
            BndL    -   lower bounds, array[N].
                        If some (all) variables are unbounded, you may specify
                        very small number or -INF (latter is recommended because
                        it will allow solver to use better algorithm).
            BndU    -   upper bounds, array[N].
                        If some (all) variables are unbounded, you may specify
                        very large number or +INF (latter is recommended because
                        it will allow solver to use better algorithm).

        NOTE 1: it is possible to specify BndL[i]=BndU[i]. In this case I-th
        variable will be "frozen" at X[i]=BndL[i]=BndU[i].

        NOTE 2: this solver has following useful properties:
        * bound constraints are always satisfied exactly
        * function is evaluated only INSIDE area specified by bound constraints
          or at its boundary

          -- ALGLIB --
             Copyright 14.01.2011 by Bochkanov Sergey
        *************************************************************************/
        public static void minlmsetbc(minlmstate state,
            double[] bndl,
            double[] bndu)
        {
            int i = 0;
            int n = 0;

            n = state.n;
            alglib.ap.assert(alglib.ap.len(bndl)>=n, "MinLMSetBC: Length(BndL)<N");
            alglib.ap.assert(alglib.ap.len(bndu)>=n, "MinLMSetBC: Length(BndU)<N");
            for(i=0; i<=n-1; i++)
            {
                alglib.ap.assert(math.isfinite(bndl[i]) || Double.IsNegativeInfinity(bndl[i]), "MinLMSetBC: BndL contains NAN or +INF");
                alglib.ap.assert(math.isfinite(bndu[i]) || Double.IsPositiveInfinity(bndu[i]), "MinLMSetBC: BndU contains NAN or -INF");
                state.bndl[i] = bndl[i];
                state.havebndl[i] = math.isfinite(bndl[i]);
                state.bndu[i] = bndu[i];
                state.havebndu[i] = math.isfinite(bndu[i]);
            }
        }


        /*************************************************************************
        This function is used to change acceleration settings

        You can choose between three acceleration strategies:
        * AccType=0, no acceleration.
        * AccType=1, secant updates are used to update quadratic model after  each
          iteration. After fixed number of iterations (or after  model  breakdown)
          we  recalculate  quadratic  model  using  analytic  Jacobian  or  finite
          differences. Number of secant-based iterations depends  on  optimization
          settings: about 3 iterations - when we have analytic Jacobian, up to 2*N
          iterations - when we use finite differences to calculate Jacobian.

        AccType=1 is recommended when Jacobian  calculation  cost  is  prohibitive
        high (several Mx1 function vector calculations  followed  by  several  NxN
        Cholesky factorizations are faster than calculation of one M*N  Jacobian).
        It should also be used when we have no Jacobian, because finite difference
        approximation takes too much time to compute.

        Table below list  optimization  protocols  (XYZ  protocol  corresponds  to
        MinLMCreateXYZ) and acceleration types they support (and use by  default).

        ACCELERATION TYPES SUPPORTED BY OPTIMIZATION PROTOCOLS:

        protocol    0   1   comment
        V           +   +
        VJ          +   +
        FGH         +

        DAFAULT VALUES:

        protocol    0   1   comment
        V               x   without acceleration it is so slooooooooow
        VJ          x
        FGH         x

        NOTE: this  function should be called before optimization. Attempt to call
        it during algorithm iterations may result in unexpected behavior.

        NOTE: attempt to call this function with unsupported protocol/acceleration
        combination will result in exception being thrown.

          -- ALGLIB --
             Copyright 14.10.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void minlmsetacctype(minlmstate state,
            int acctype)
        {
            alglib.ap.assert((acctype==0 || acctype==1) || acctype==2, "MinLMSetAccType: incorrect AccType!");
            if( acctype==2 )
            {
                acctype = 0;
            }
            if( acctype==0 )
            {
                state.maxmodelage = 0;
                state.makeadditers = false;
                return;
            }
            if( acctype==1 )
            {
                alglib.ap.assert(state.hasfi, "MinLMSetAccType: AccType=1 is incompatible with current protocol!");
                if( state.algomode==0 )
                {
                    state.maxmodelage = 2*state.n;
                }
                else
                {
                    state.maxmodelage = smallmodelage;
                }
                state.makeadditers = false;
                return;
            }
        }


        /*************************************************************************
        NOTES:

        1. Depending on function used to create state  structure,  this  algorithm
           may accept Jacobian and/or Hessian and/or gradient.  According  to  the
           said above, there ase several versions of this function,  which  accept
           different sets of callbacks.

           This flexibility opens way to subtle errors - you may create state with
           MinLMCreateFGH() (optimization using Hessian), but call function  which
           does not accept Hessian. So when algorithm will request Hessian,  there
           will be no callback to call. In this case exception will be thrown.

           Be careful to avoid such errors because there is no way to find them at
           compile time - you can see them at runtime only.

          -- ALGLIB --
             Copyright 10.03.2009 by Bochkanov Sergey
        *************************************************************************/
        public static bool minlmiteration(minlmstate state)
        {
            bool result = new bool();
            int n = 0;
            int m = 0;
            bool bflag = new bool();
            int iflag = 0;
            double v = 0;
            double s = 0;
            double t = 0;
            int i = 0;
            int k = 0;
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
                m = state.rstate.ia[1];
                iflag = state.rstate.ia[2];
                i = state.rstate.ia[3];
                k = state.rstate.ia[4];
                bflag = state.rstate.ba[0];
                v = state.rstate.ra[0];
                s = state.rstate.ra[1];
                t = state.rstate.ra[2];
            }
            else
            {
                n = -983;
                m = -989;
                iflag = -834;
                i = 900;
                k = -287;
                bflag = false;
                v = 214;
                s = -338;
                t = -686;
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
            if( state.rstate.stage==4 )
            {
                goto lbl_4;
            }
            if( state.rstate.stage==5 )
            {
                goto lbl_5;
            }
            if( state.rstate.stage==6 )
            {
                goto lbl_6;
            }
            if( state.rstate.stage==7 )
            {
                goto lbl_7;
            }
            if( state.rstate.stage==8 )
            {
                goto lbl_8;
            }
            if( state.rstate.stage==9 )
            {
                goto lbl_9;
            }
            if( state.rstate.stage==10 )
            {
                goto lbl_10;
            }
            if( state.rstate.stage==11 )
            {
                goto lbl_11;
            }
            if( state.rstate.stage==12 )
            {
                goto lbl_12;
            }
            if( state.rstate.stage==13 )
            {
                goto lbl_13;
            }
            if( state.rstate.stage==14 )
            {
                goto lbl_14;
            }
            if( state.rstate.stage==15 )
            {
                goto lbl_15;
            }
            if( state.rstate.stage==16 )
            {
                goto lbl_16;
            }
            if( state.rstate.stage==17 )
            {
                goto lbl_17;
            }
            if( state.rstate.stage==18 )
            {
                goto lbl_18;
            }
            
            //
            // Routine body
            //
            
            //
            // prepare
            //
            n = state.n;
            m = state.m;
            state.repiterationscount = 0;
            state.repterminationtype = 0;
            state.repfuncidx = -1;
            state.repvaridx = -1;
            state.repnfunc = 0;
            state.repnjac = 0;
            state.repngrad = 0;
            state.repnhess = 0;
            state.repncholesky = 0;
            
            //
            // check consistency of constraints,
            // enforce feasibility of the solution
            // set constraints
            //
            if( !optserv.enforceboundaryconstraints(ref state.xbase, state.bndl, state.havebndl, state.bndu, state.havebndu, n, 0) )
            {
                state.repterminationtype = -3;
                result = false;
                return result;
            }
            minqp.minqpsetbc(state.qpstate, state.bndl, state.bndu);
            
            //
            //  Check, that transferred derivative value is right
            //
            clearrequestfields(state);
            if( !(state.algomode==1 && (double)(state.teststep)>(double)(0)) )
            {
                goto lbl_19;
            }
            for(i_=0; i_<=n-1;i_++)
            {
                state.x[i_] = state.xbase[i_];
            }
            state.needfij = true;
            i = 0;
        lbl_21:
            if( i>n-1 )
            {
                goto lbl_23;
            }
            alglib.ap.assert((state.havebndl[i] && (double)(state.bndl[i])<=(double)(state.x[i])) || !state.havebndl[i], "MinLM: internal error(State.X is out of bounds)");
            alglib.ap.assert((state.havebndu[i] && (double)(state.x[i])<=(double)(state.bndu[i])) || !state.havebndu[i], "MinLMIteration: internal error(State.X is out of bounds)");
            v = state.x[i];
            state.x[i] = v-state.teststep*state.s[i];
            if( state.havebndl[i] )
            {
                state.x[i] = Math.Max(state.x[i], state.bndl[i]);
            }
            state.xm1 = state.x[i];
            state.rstate.stage = 0;
            goto lbl_rcomm;
        lbl_0:
            for(i_=0; i_<=m-1;i_++)
            {
                state.fm1[i_] = state.fi[i_];
            }
            for(i_=0; i_<=m-1;i_++)
            {
                state.gm1[i_] = state.j[i_,i];
            }
            state.x[i] = v+state.teststep*state.s[i];
            if( state.havebndu[i] )
            {
                state.x[i] = Math.Min(state.x[i], state.bndu[i]);
            }
            state.xp1 = state.x[i];
            state.rstate.stage = 1;
            goto lbl_rcomm;
        lbl_1:
            for(i_=0; i_<=m-1;i_++)
            {
                state.fp1[i_] = state.fi[i_];
            }
            for(i_=0; i_<=m-1;i_++)
            {
                state.gp1[i_] = state.j[i_,i];
            }
            state.x[i] = (state.xm1+state.xp1)/2;
            if( state.havebndl[i] )
            {
                state.x[i] = Math.Max(state.x[i], state.bndl[i]);
            }
            if( state.havebndu[i] )
            {
                state.x[i] = Math.Min(state.x[i], state.bndu[i]);
            }
            state.rstate.stage = 2;
            goto lbl_rcomm;
        lbl_2:
            for(i_=0; i_<=m-1;i_++)
            {
                state.fc1[i_] = state.fi[i_];
            }
            for(i_=0; i_<=m-1;i_++)
            {
                state.gc1[i_] = state.j[i_,i];
            }
            state.x[i] = v;
            for(k=0; k<=m-1; k++)
            {
                if( !optserv.derivativecheck(state.fm1[k], state.gm1[k], state.fp1[k], state.gp1[k], state.fc1[k], state.gc1[k], state.xp1-state.xm1) )
                {
                    state.repfuncidx = k;
                    state.repvaridx = i;
                    state.repterminationtype = -7;
                    result = false;
                    return result;
                }
            }
            i = i+1;
            goto lbl_21;
        lbl_23:
            state.needfij = false;
        lbl_19:
            
            //
            // Initial report of current point
            //
            // Note 1: we rewrite State.X twice because
            // user may accidentally change it after first call.
            //
            // Note 2: we set NeedF or NeedFI depending on what
            // information about function we have.
            //
            if( !state.xrep )
            {
                goto lbl_24;
            }
            for(i_=0; i_<=n-1;i_++)
            {
                state.x[i_] = state.xbase[i_];
            }
            clearrequestfields(state);
            if( !state.hasf )
            {
                goto lbl_26;
            }
            state.needf = true;
            state.rstate.stage = 3;
            goto lbl_rcomm;
        lbl_3:
            state.needf = false;
            goto lbl_27;
        lbl_26:
            alglib.ap.assert(state.hasfi, "MinLM: internal error 2!");
            state.needfi = true;
            state.rstate.stage = 4;
            goto lbl_rcomm;
        lbl_4:
            state.needfi = false;
            v = 0.0;
            for(i_=0; i_<=m-1;i_++)
            {
                v += state.fi[i_]*state.fi[i_];
            }
            state.f = v;
        lbl_27:
            state.repnfunc = state.repnfunc+1;
            for(i_=0; i_<=n-1;i_++)
            {
                state.x[i_] = state.xbase[i_];
            }
            clearrequestfields(state);
            state.xupdated = true;
            state.rstate.stage = 5;
            goto lbl_rcomm;
        lbl_5:
            state.xupdated = false;
        lbl_24:
            
            //
            // Prepare control variables
            //
            state.nu = 1;
            state.lambdav = -math.maxrealnumber;
            state.modelage = state.maxmodelage+1;
            state.deltaxready = false;
            state.deltafready = false;
            
            //
            // Main cycle.
            //
            // We move through it until either:
            // * one of the stopping conditions is met
            // * we decide that stopping conditions are too stringent
            //   and break from cycle
            //
            //
        lbl_28:
            if( false )
            {
                goto lbl_29;
            }
            
            //
            // First, we have to prepare quadratic model for our function.
            // We use BFlag to ensure that model is prepared;
            // if it is false at the end of this block, something went wrong.
            //
            // We may either calculate brand new model or update old one.
            //
            // Before this block we have:
            // * State.XBase            - current position.
            // * State.DeltaX           - if DeltaXReady is True
            // * State.DeltaF           - if DeltaFReady is True
            //
            // After this block is over, we will have:
            // * State.XBase            - base point (unchanged)
            // * State.FBase            - F(XBase)
            // * State.GBase            - linear term
            // * State.QuadraticModel   - quadratic term
            // * State.LambdaV          - current estimate for lambda
            //
            // We also clear DeltaXReady/DeltaFReady flags
            // after initialization is done.
            //
            bflag = false;
            if( !(state.algomode==0 || state.algomode==1) )
            {
                goto lbl_30;
            }
            
            //
            // Calculate f[] and Jacobian
            //
            if( !(state.modelage>state.maxmodelage || !(state.deltaxready && state.deltafready)) )
            {
                goto lbl_32;
            }
            
            //
            // Refresh model (using either finite differences or analytic Jacobian)
            //
            if( state.algomode!=0 )
            {
                goto lbl_34;
            }
            
            //
            // Optimization using F values only.
            // Use finite differences to estimate Jacobian.
            //
            alglib.ap.assert(state.hasfi, "MinLMIteration: internal error when estimating Jacobian (no f[])");
            k = 0;
        lbl_36:
            if( k>n-1 )
            {
                goto lbl_38;
            }
            
            //
            // We guard X[k] from leaving [BndL,BndU].
            // In case BndL=BndU, we assume that derivative in this direction is zero.
            //
            for(i_=0; i_<=n-1;i_++)
            {
                state.x[i_] = state.xbase[i_];
            }
            state.x[k] = state.x[k]-state.s[k]*state.diffstep;
            if( state.havebndl[k] )
            {
                state.x[k] = Math.Max(state.x[k], state.bndl[k]);
            }
            if( state.havebndu[k] )
            {
                state.x[k] = Math.Min(state.x[k], state.bndu[k]);
            }
            state.xm1 = state.x[k];
            clearrequestfields(state);
            state.needfi = true;
            state.rstate.stage = 6;
            goto lbl_rcomm;
        lbl_6:
            state.repnfunc = state.repnfunc+1;
            for(i_=0; i_<=m-1;i_++)
            {
                state.fm1[i_] = state.fi[i_];
            }
            for(i_=0; i_<=n-1;i_++)
            {
                state.x[i_] = state.xbase[i_];
            }
            state.x[k] = state.x[k]+state.s[k]*state.diffstep;
            if( state.havebndl[k] )
            {
                state.x[k] = Math.Max(state.x[k], state.bndl[k]);
            }
            if( state.havebndu[k] )
            {
                state.x[k] = Math.Min(state.x[k], state.bndu[k]);
            }
            state.xp1 = state.x[k];
            clearrequestfields(state);
            state.needfi = true;
            state.rstate.stage = 7;
            goto lbl_rcomm;
        lbl_7:
            state.repnfunc = state.repnfunc+1;
            for(i_=0; i_<=m-1;i_++)
            {
                state.fp1[i_] = state.fi[i_];
            }
            v = state.xp1-state.xm1;
            if( (double)(v)!=(double)(0) )
            {
                v = 1/v;
                for(i_=0; i_<=m-1;i_++)
                {
                    state.j[i_,k] = v*state.fp1[i_];
                }
                for(i_=0; i_<=m-1;i_++)
                {
                    state.j[i_,k] = state.j[i_,k] - v*state.fm1[i_];
                }
            }
            else
            {
                for(i=0; i<=m-1; i++)
                {
                    state.j[i,k] = 0;
                }
            }
            k = k+1;
            goto lbl_36;
        lbl_38:
            
            //
            // Calculate F(XBase)
            //
            for(i_=0; i_<=n-1;i_++)
            {
                state.x[i_] = state.xbase[i_];
            }
            clearrequestfields(state);
            state.needfi = true;
            state.rstate.stage = 8;
            goto lbl_rcomm;
        lbl_8:
            state.needfi = false;
            state.repnfunc = state.repnfunc+1;
            state.repnjac = state.repnjac+1;
            
            //
            // New model
            //
            state.modelage = 0;
            goto lbl_35;
        lbl_34:
            
            //
            // Obtain f[] and Jacobian
            //
            for(i_=0; i_<=n-1;i_++)
            {
                state.x[i_] = state.xbase[i_];
            }
            clearrequestfields(state);
            state.needfij = true;
            state.rstate.stage = 9;
            goto lbl_rcomm;
        lbl_9:
            state.needfij = false;
            state.repnfunc = state.repnfunc+1;
            state.repnjac = state.repnjac+1;
            
            //
            // New model
            //
            state.modelage = 0;
        lbl_35:
            goto lbl_33;
        lbl_32:
            
            //
            // State.J contains Jacobian or its current approximation;
            // refresh it using secant updates:
            //
            // f(x0+dx) = f(x0) + J*dx,
            // J_new = J_old + u*h'
            // h = x_new-x_old
            // u = (f_new - f_old - J_old*h)/(h'h)
            //
            // We can explicitly generate h and u, but it is
            // preferential to do in-place calculations. Only
            // I-th row of J_old is needed to calculate u[I],
            // so we can update J row by row in one pass.
            //
            // NOTE: we expect that State.XBase contains new point,
            // State.FBase contains old point, State.DeltaX and
            // State.DeltaY contain updates from last step.
            //
            alglib.ap.assert(state.deltaxready && state.deltafready, "MinLMIteration: uninitialized DeltaX/DeltaF");
            t = 0.0;
            for(i_=0; i_<=n-1;i_++)
            {
                t += state.deltax[i_]*state.deltax[i_];
            }
            alglib.ap.assert((double)(t)!=(double)(0), "MinLM: internal error (T=0)");
            for(i=0; i<=m-1; i++)
            {
                v = 0.0;
                for(i_=0; i_<=n-1;i_++)
                {
                    v += state.j[i,i_]*state.deltax[i_];
                }
                v = (state.deltaf[i]-v)/t;
                for(i_=0; i_<=n-1;i_++)
                {
                    state.j[i,i_] = state.j[i,i_] + v*state.deltax[i_];
                }
            }
            for(i_=0; i_<=m-1;i_++)
            {
                state.fi[i_] = state.fibase[i_];
            }
            for(i_=0; i_<=m-1;i_++)
            {
                state.fi[i_] = state.fi[i_] + state.deltaf[i_];
            }
            
            //
            // Increase model age
            //
            state.modelage = state.modelage+1;
        lbl_33:
            
            //
            // Generate quadratic model:
            //     f(xbase+dx) =
            //       = (f0 + J*dx)'(f0 + J*dx)
            //       = f0^2 + dx'J'f0 + f0*J*dx + dx'J'J*dx
            //       = f0^2 + 2*f0*J*dx + dx'J'J*dx
            //
            // Note that we calculate 2*(J'J) instead of J'J because
            // our quadratic model is based on Tailor decomposition,
            // i.e. it has 0.5 before quadratic term.
            //
            ablas.rmatrixgemm(n, n, m, 2.0, state.j, 0, 0, 1, state.j, 0, 0, 0, 0.0, state.quadraticmodel, 0, 0);
            ablas.rmatrixmv(n, m, state.j, 0, 0, 1, state.fi, 0, ref state.gbase, 0);
            for(i_=0; i_<=n-1;i_++)
            {
                state.gbase[i_] = 2*state.gbase[i_];
            }
            v = 0.0;
            for(i_=0; i_<=m-1;i_++)
            {
                v += state.fi[i_]*state.fi[i_];
            }
            state.fbase = v;
            for(i_=0; i_<=m-1;i_++)
            {
                state.fibase[i_] = state.fi[i_];
            }
            
            //
            // set control variables
            //
            bflag = true;
        lbl_30:
            if( state.algomode!=2 )
            {
                goto lbl_39;
            }
            alglib.ap.assert(!state.hasfi, "MinLMIteration: internal error (HasFI is True in Hessian-based mode)");
            
            //
            // Obtain F, G, H
            //
            for(i_=0; i_<=n-1;i_++)
            {
                state.x[i_] = state.xbase[i_];
            }
            clearrequestfields(state);
            state.needfgh = true;
            state.rstate.stage = 10;
            goto lbl_rcomm;
        lbl_10:
            state.needfgh = false;
            state.repnfunc = state.repnfunc+1;
            state.repngrad = state.repngrad+1;
            state.repnhess = state.repnhess+1;
            ablas.rmatrixcopy(n, n, state.h, 0, 0, ref state.quadraticmodel, 0, 0);
            for(i_=0; i_<=n-1;i_++)
            {
                state.gbase[i_] = state.g[i_];
            }
            state.fbase = state.f;
            
            //
            // set control variables
            //
            bflag = true;
            state.modelage = 0;
        lbl_39:
            alglib.ap.assert(bflag, "MinLM: internal integrity check failed!");
            state.deltaxready = false;
            state.deltafready = false;
            
            //
            // If Lambda is not initialized, initialize it using quadratic model
            //
            if( (double)(state.lambdav)<(double)(0) )
            {
                state.lambdav = 0;
                for(i=0; i<=n-1; i++)
                {
                    state.lambdav = Math.Max(state.lambdav, Math.Abs(state.quadraticmodel[i,i])*math.sqr(state.s[i]));
                }
                state.lambdav = 0.001*state.lambdav;
                if( (double)(state.lambdav)==(double)(0) )
                {
                    state.lambdav = 1;
                }
            }
            
            //
            // Test stopping conditions for function gradient
            //
            if( (double)(boundedscaledantigradnorm(state, state.xbase, state.gbase))>(double)(state.epsg) )
            {
                goto lbl_41;
            }
            if( state.modelage!=0 )
            {
                goto lbl_43;
            }
            
            //
            // Model is fresh, we can rely on it and terminate algorithm
            //
            state.repterminationtype = 4;
            if( !state.xrep )
            {
                goto lbl_45;
            }
            for(i_=0; i_<=n-1;i_++)
            {
                state.x[i_] = state.xbase[i_];
            }
            state.f = state.fbase;
            clearrequestfields(state);
            state.xupdated = true;
            state.rstate.stage = 11;
            goto lbl_rcomm;
        lbl_11:
            state.xupdated = false;
        lbl_45:
            result = false;
            return result;
            goto lbl_44;
        lbl_43:
            
            //
            // Model is not fresh, we should refresh it and test
            // conditions once more
            //
            state.modelage = state.maxmodelage+1;
            goto lbl_28;
        lbl_44:
        lbl_41:
            
            //
            // Find value of Levenberg-Marquardt damping parameter which:
            // * leads to positive definite damped model
            // * within bounds specified by StpMax
            // * generates step which decreases function value
            //
            // After this block IFlag is set to:
            // * -3, if constraints are infeasible
            // * -2, if model update is needed (either Lambda growth is too large
            //       or step is too short, but we can't rely on model and stop iterations)
            // * -1, if model is fresh, Lambda have grown too large, termination is needed
            // *  0, if everything is OK, continue iterations
            //
            // State.Nu can have any value on enter, but after exit it is set to 1.0
            //
            iflag = -99;
        lbl_47:
            if( false )
            {
                goto lbl_48;
            }
            
            //
            // Do we need model update?
            //
            if( state.modelage>0 && (double)(state.nu)>=(double)(suspiciousnu) )
            {
                iflag = -2;
                goto lbl_48;
            }
            
            //
            // Setup quadratic solver and solve quadratic programming problem.
            // After problem is solved we'll try to bound step by StpMax
            // (Lambda will be increased if step size is too large).
            //
            // We use BFlag variable to indicate that we have to increase Lambda.
            // If it is False, we will try to increase Lambda and move to new iteration.
            //
            bflag = true;
            minqp.minqpsetstartingpointfast(state.qpstate, state.xbase);
            minqp.minqpsetoriginfast(state.qpstate, state.xbase);
            minqp.minqpsetlineartermfast(state.qpstate, state.gbase);
            minqp.minqpsetquadratictermfast(state.qpstate, state.quadraticmodel, true, 0.0);
            for(i=0; i<=n-1; i++)
            {
                state.tmp0[i] = state.quadraticmodel[i,i]+state.lambdav/math.sqr(state.s[i]);
            }
            minqp.minqprewritediagonal(state.qpstate, state.tmp0);
            minqp.minqpoptimize(state.qpstate);
            minqp.minqpresultsbuf(state.qpstate, ref state.xdir, state.qprep);
            if( state.qprep.terminationtype>0 )
            {
                
                //
                // successful solution of QP problem
                //
                for(i_=0; i_<=n-1;i_++)
                {
                    state.xdir[i_] = state.xdir[i_] - state.xbase[i_];
                }
                v = 0.0;
                for(i_=0; i_<=n-1;i_++)
                {
                    v += state.xdir[i_]*state.xdir[i_];
                }
                if( math.isfinite(v) )
                {
                    v = Math.Sqrt(v);
                    if( (double)(state.stpmax)>(double)(0) && (double)(v)>(double)(state.stpmax) )
                    {
                        bflag = false;
                    }
                }
                else
                {
                    bflag = false;
                }
            }
            else
            {
                
                //
                // Either problem is non-convex (increase LambdaV) or constraints are inconsistent
                //
                alglib.ap.assert(state.qprep.terminationtype==-3 || state.qprep.terminationtype==-5, "MinLM: unexpected completion code from QP solver");
                if( state.qprep.terminationtype==-3 )
                {
                    iflag = -3;
                    goto lbl_48;
                }
                bflag = false;
            }
            if( !bflag )
            {
                
                //
                // Solution failed:
                // try to increase lambda to make matrix positive definite and continue.
                //
                if( !increaselambda(ref state.lambdav, ref state.nu) )
                {
                    iflag = -1;
                    goto lbl_48;
                }
                goto lbl_47;
            }
            
            //
            // Step in State.XDir and it is bounded by StpMax.
            //
            // We should check stopping conditions on step size here.
            // DeltaX, which is used for secant updates, is initialized here.
            //
            // This code is a bit tricky because sometimes XDir<>0, but
            // it is so small that XDir+XBase==XBase (in finite precision
            // arithmetics). So we set DeltaX to XBase, then
            // add XDir, and then subtract XBase to get exact value of
            // DeltaX.
            //
            // Step length is estimated using DeltaX.
            //
            // NOTE: stopping conditions are tested
            // for fresh models only (ModelAge=0)
            //
            for(i_=0; i_<=n-1;i_++)
            {
                state.deltax[i_] = state.xbase[i_];
            }
            for(i_=0; i_<=n-1;i_++)
            {
                state.deltax[i_] = state.deltax[i_] + state.xdir[i_];
            }
            for(i_=0; i_<=n-1;i_++)
            {
                state.deltax[i_] = state.deltax[i_] - state.xbase[i_];
            }
            state.deltaxready = true;
            v = 0.0;
            for(i=0; i<=n-1; i++)
            {
                v = v+math.sqr(state.deltax[i]/state.s[i]);
            }
            v = Math.Sqrt(v);
            if( (double)(v)>(double)(state.epsx) )
            {
                goto lbl_49;
            }
            if( state.modelage!=0 )
            {
                goto lbl_51;
            }
            
            //
            // Step is too short, model is fresh and we can rely on it.
            // Terminating.
            //
            state.repterminationtype = 2;
            if( !state.xrep )
            {
                goto lbl_53;
            }
            for(i_=0; i_<=n-1;i_++)
            {
                state.x[i_] = state.xbase[i_];
            }
            state.f = state.fbase;
            clearrequestfields(state);
            state.xupdated = true;
            state.rstate.stage = 12;
            goto lbl_rcomm;
        lbl_12:
            state.xupdated = false;
        lbl_53:
            result = false;
            return result;
            goto lbl_52;
        lbl_51:
            
            //
            // Step is suspiciously short, but model is not fresh
            // and we can't rely on it.
            //
            iflag = -2;
            goto lbl_48;
        lbl_52:
        lbl_49:
            
            //
            // Let's evaluate new step:
            // a) if we have Fi vector, we evaluate it using rcomm, and
            //    then we manually calculate State.F as sum of squares of Fi[]
            // b) if we have F value, we just evaluate it through rcomm interface
            //
            // We prefer (a) because we may need Fi vector for additional
            // iterations
            //
            alglib.ap.assert(state.hasfi || state.hasf, "MinLM: internal error 2!");
            for(i_=0; i_<=n-1;i_++)
            {
                state.x[i_] = state.xbase[i_];
            }
            for(i_=0; i_<=n-1;i_++)
            {
                state.x[i_] = state.x[i_] + state.xdir[i_];
            }
            clearrequestfields(state);
            if( !state.hasfi )
            {
                goto lbl_55;
            }
            state.needfi = true;
            state.rstate.stage = 13;
            goto lbl_rcomm;
        lbl_13:
            state.needfi = false;
            v = 0.0;
            for(i_=0; i_<=m-1;i_++)
            {
                v += state.fi[i_]*state.fi[i_];
            }
            state.f = v;
            for(i_=0; i_<=m-1;i_++)
            {
                state.deltaf[i_] = state.fi[i_];
            }
            for(i_=0; i_<=m-1;i_++)
            {
                state.deltaf[i_] = state.deltaf[i_] - state.fibase[i_];
            }
            state.deltafready = true;
            goto lbl_56;
        lbl_55:
            state.needf = true;
            state.rstate.stage = 14;
            goto lbl_rcomm;
        lbl_14:
            state.needf = false;
        lbl_56:
            state.repnfunc = state.repnfunc+1;
            if( (double)(state.f)>=(double)(state.fbase) )
            {
                
                //
                // Increase lambda and continue
                //
                if( !increaselambda(ref state.lambdav, ref state.nu) )
                {
                    iflag = -1;
                    goto lbl_48;
                }
                goto lbl_47;
            }
            
            //
            // We've found our step!
            //
            iflag = 0;
            goto lbl_48;
            goto lbl_47;
        lbl_48:
            state.nu = 1;
            alglib.ap.assert(iflag>=-3 && iflag<=0, "MinLM: internal integrity check failed!");
            if( iflag==-3 )
            {
                state.repterminationtype = -3;
                result = false;
                return result;
            }
            if( iflag==-2 )
            {
                state.modelage = state.maxmodelage+1;
                goto lbl_28;
            }
            if( iflag==-1 )
            {
                goto lbl_29;
            }
            
            //
            // Levenberg-Marquardt step is ready.
            // Compare predicted vs. actual decrease and decide what to do with lambda.
            //
            // NOTE: we expect that State.DeltaX contains direction of step,
            // State.F contains function value at new point.
            //
            alglib.ap.assert(state.deltaxready, "MinLM: deltaX is not ready");
            t = 0;
            for(i=0; i<=n-1; i++)
            {
                v = 0.0;
                for(i_=0; i_<=n-1;i_++)
                {
                    v += state.quadraticmodel[i,i_]*state.deltax[i_];
                }
                t = t+state.deltax[i]*state.gbase[i]+0.5*state.deltax[i]*v;
            }
            state.predicteddecrease = -t;
            state.actualdecrease = -(state.f-state.fbase);
            if( (double)(state.predicteddecrease)<=(double)(0) )
            {
                goto lbl_29;
            }
            v = state.actualdecrease/state.predicteddecrease;
            if( (double)(v)>=(double)(0.1) )
            {
                goto lbl_57;
            }
            if( increaselambda(ref state.lambdav, ref state.nu) )
            {
                goto lbl_59;
            }
            
            //
            // Lambda is too large, we have to break iterations.
            //
            state.repterminationtype = 7;
            if( !state.xrep )
            {
                goto lbl_61;
            }
            for(i_=0; i_<=n-1;i_++)
            {
                state.x[i_] = state.xbase[i_];
            }
            state.f = state.fbase;
            clearrequestfields(state);
            state.xupdated = true;
            state.rstate.stage = 15;
            goto lbl_rcomm;
        lbl_15:
            state.xupdated = false;
        lbl_61:
            result = false;
            return result;
        lbl_59:
        lbl_57:
            if( (double)(v)>(double)(0.5) )
            {
                decreaselambda(ref state.lambdav, ref state.nu);
            }
            
            //
            // Accept step, report it and
            // test stopping conditions on iterations count and function decrease.
            //
            // NOTE: we expect that State.DeltaX contains direction of step,
            // State.F contains function value at new point.
            //
            // NOTE2: we should update XBase ONLY. In the beginning of the next
            // iteration we expect that State.FIBase is NOT updated and
            // contains old value of a function vector.
            //
            for(i_=0; i_<=n-1;i_++)
            {
                state.xbase[i_] = state.xbase[i_] + state.deltax[i_];
            }
            if( !state.xrep )
            {
                goto lbl_63;
            }
            for(i_=0; i_<=n-1;i_++)
            {
                state.x[i_] = state.xbase[i_];
            }
            clearrequestfields(state);
            state.xupdated = true;
            state.rstate.stage = 16;
            goto lbl_rcomm;
        lbl_16:
            state.xupdated = false;
        lbl_63:
            state.repiterationscount = state.repiterationscount+1;
            if( state.repiterationscount>=state.maxits && state.maxits>0 )
            {
                state.repterminationtype = 5;
            }
            if( state.modelage==0 )
            {
                if( (double)(Math.Abs(state.f-state.fbase))<=(double)(state.epsf*Math.Max(1, Math.Max(Math.Abs(state.f), Math.Abs(state.fbase)))) )
                {
                    state.repterminationtype = 1;
                }
            }
            if( state.repterminationtype<=0 )
            {
                goto lbl_65;
            }
            if( !state.xrep )
            {
                goto lbl_67;
            }
            
            //
            // Report: XBase contains new point, F contains function value at new point
            //
            for(i_=0; i_<=n-1;i_++)
            {
                state.x[i_] = state.xbase[i_];
            }
            clearrequestfields(state);
            state.xupdated = true;
            state.rstate.stage = 17;
            goto lbl_rcomm;
        lbl_17:
            state.xupdated = false;
        lbl_67:
            result = false;
            return result;
        lbl_65:
            state.modelage = state.modelage+1;
            goto lbl_28;
        lbl_29:
            
            //
            // Lambda is too large, we have to break iterations.
            //
            state.repterminationtype = 7;
            if( !state.xrep )
            {
                goto lbl_69;
            }
            for(i_=0; i_<=n-1;i_++)
            {
                state.x[i_] = state.xbase[i_];
            }
            state.f = state.fbase;
            clearrequestfields(state);
            state.xupdated = true;
            state.rstate.stage = 18;
            goto lbl_rcomm;
        lbl_18:
            state.xupdated = false;
        lbl_69:
            result = false;
            return result;
            
            //
            // Saving state
            //
        lbl_rcomm:
            result = true;
            state.rstate.ia[0] = n;
            state.rstate.ia[1] = m;
            state.rstate.ia[2] = iflag;
            state.rstate.ia[3] = i;
            state.rstate.ia[4] = k;
            state.rstate.ba[0] = bflag;
            state.rstate.ra[0] = v;
            state.rstate.ra[1] = s;
            state.rstate.ra[2] = t;
            return result;
        }


        /*************************************************************************
        Levenberg-Marquardt algorithm results

        INPUT PARAMETERS:
            State   -   algorithm state

        OUTPUT PARAMETERS:
            X       -   array[0..N-1], solution
            Rep     -   optimization report;
                        see comments for this structure for more info.

          -- ALGLIB --
             Copyright 10.03.2009 by Bochkanov Sergey
        *************************************************************************/
        public static void minlmresults(minlmstate state,
            ref double[] x,
            minlmreport rep)
        {
            x = new double[0];

            minlmresultsbuf(state, ref x, rep);
        }


        /*************************************************************************
        Levenberg-Marquardt algorithm results

        Buffered implementation of MinLMResults(), which uses pre-allocated buffer
        to store X[]. If buffer size is  too  small,  it  resizes  buffer.  It  is
        intended to be used in the inner cycles of performance critical algorithms
        where array reallocation penalty is too large to be ignored.

          -- ALGLIB --
             Copyright 10.03.2009 by Bochkanov Sergey
        *************************************************************************/
        public static void minlmresultsbuf(minlmstate state,
            ref double[] x,
            minlmreport rep)
        {
            int i_ = 0;

            if( alglib.ap.len(x)<state.n )
            {
                x = new double[state.n];
            }
            for(i_=0; i_<=state.n-1;i_++)
            {
                x[i_] = state.x[i_];
            }
            rep.iterationscount = state.repiterationscount;
            rep.terminationtype = state.repterminationtype;
            rep.funcidx = state.repfuncidx;
            rep.varidx = state.repvaridx;
            rep.nfunc = state.repnfunc;
            rep.njac = state.repnjac;
            rep.ngrad = state.repngrad;
            rep.nhess = state.repnhess;
            rep.ncholesky = state.repncholesky;
        }


        /*************************************************************************
        This  subroutine  restarts  LM  algorithm from new point. All optimization
        parameters are left unchanged.

        This  function  allows  to  solve multiple  optimization  problems  (which
        must have same number of dimensions) without object reallocation penalty.

        INPUT PARAMETERS:
            State   -   structure used for reverse communication previously
                        allocated with MinLMCreateXXX call.
            X       -   new starting point.

          -- ALGLIB --
             Copyright 30.07.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void minlmrestartfrom(minlmstate state,
            double[] x)
        {
            int i_ = 0;

            alglib.ap.assert(alglib.ap.len(x)>=state.n, "MinLMRestartFrom: Length(X)<N!");
            alglib.ap.assert(apserv.isfinitevector(x, state.n), "MinLMRestartFrom: X contains infinite or NaN values!");
            for(i_=0; i_<=state.n-1;i_++)
            {
                state.xbase[i_] = x[i_];
            }
            state.rstate.ia = new int[4+1];
            state.rstate.ba = new bool[0+1];
            state.rstate.ra = new double[2+1];
            state.rstate.stage = -1;
            clearrequestfields(state);
        }


        /*************************************************************************
        This is obsolete function.

        Since ALGLIB 3.3 it is equivalent to MinLMCreateVJ().

          -- ALGLIB --
             Copyright 30.03.2009 by Bochkanov Sergey
        *************************************************************************/
        public static void minlmcreatevgj(int n,
            int m,
            double[] x,
            minlmstate state)
        {
            minlmcreatevj(n, m, x, state);
        }


        /*************************************************************************
        This is obsolete function.

        Since ALGLIB 3.3 it is equivalent to MinLMCreateFJ().

          -- ALGLIB --
             Copyright 30.03.2009 by Bochkanov Sergey
        *************************************************************************/
        public static void minlmcreatefgj(int n,
            int m,
            double[] x,
            minlmstate state)
        {
            minlmcreatefj(n, m, x, state);
        }


        /*************************************************************************
        This function is considered obsolete since ALGLIB 3.1.0 and is present for
        backward  compatibility  only.  We  recommend  to use MinLMCreateVJ, which
        provides similar, but more consistent and feature-rich interface.

          -- ALGLIB --
             Copyright 30.03.2009 by Bochkanov Sergey
        *************************************************************************/
        public static void minlmcreatefj(int n,
            int m,
            double[] x,
            minlmstate state)
        {
            alglib.ap.assert(n>=1, "MinLMCreateFJ: N<1!");
            alglib.ap.assert(m>=1, "MinLMCreateFJ: M<1!");
            alglib.ap.assert(alglib.ap.len(x)>=n, "MinLMCreateFJ: Length(X)<N!");
            alglib.ap.assert(apserv.isfinitevector(x, n), "MinLMCreateFJ: X contains infinite or NaN values!");
            
            //
            // initialize
            //
            state.teststep = 0;
            state.n = n;
            state.m = m;
            state.algomode = 1;
            state.hasf = true;
            state.hasfi = false;
            state.hasg = false;
            
            //
            // init 2
            //
            lmprepare(n, m, true, state);
            minlmsetacctype(state, 0);
            minlmsetcond(state, 0, 0, 0, 0);
            minlmsetxrep(state, false);
            minlmsetstpmax(state, 0);
            minlmrestartfrom(state, x);
        }


        /*************************************************************************
        This  subroutine  turns  on  verification  of  the  user-supplied analytic
        gradient:
        * user calls this subroutine before optimization begins
        * MinLMOptimize() is called
        * prior to actual optimization, for  each  function Fi and each  component
          of parameters  being  optimized X[j] algorithm performs following steps:
          * two trial steps are made to X[j]-TestStep*S[j] and X[j]+TestStep*S[j],
            where X[j] is j-th parameter and S[j] is a scale of j-th parameter
          * if needed, steps are bounded with respect to constraints on X[]
          * Fi(X) is evaluated at these trial points
          * we perform one more evaluation in the middle point of the interval
          * we  build  cubic  model using function values and derivatives at trial
            points and we compare its prediction with actual value in  the  middle
            point
          * in case difference between prediction and actual value is higher  than
            some predetermined threshold, algorithm stops with completion code -7;
            Rep.VarIdx is set to index of the parameter with incorrect derivative,
            Rep.FuncIdx is set to index of the function.
        * after verification is over, algorithm proceeds to the actual optimization.

        NOTE 1: verification  needs  N (parameters count) Jacobian evaluations. It
                is  very  costly  and  you  should use it only for low dimensional
                problems,  when  you  want  to  be  sure  that  you've   correctly
                calculated  analytic  derivatives.  You should not  use  it in the
                production code  (unless  you  want  to check derivatives provided
                by some third party).

        NOTE 2: you  should  carefully  choose  TestStep. Value which is too large
                (so large that function behaviour is significantly non-cubic) will
                lead to false alarms. You may use  different  step  for  different
                parameters by means of setting scale with MinLMSetScale().

        NOTE 3: this function may lead to false positives. In case it reports that
                I-th  derivative was calculated incorrectly, you may decrease test
                step  and  try  one  more  time  - maybe your function changes too
                sharply  and  your  step  is  too  large for such rapidly chanding
                function.

        INPUT PARAMETERS:
            State       -   structure used to store algorithm state
            TestStep    -   verification step:
                            * TestStep=0 turns verification off
                            * TestStep>0 activates verification

          -- ALGLIB --
             Copyright 15.06.2012 by Bochkanov Sergey
        *************************************************************************/
        public static void minlmsetgradientcheck(minlmstate state,
            double teststep)
        {
            alglib.ap.assert(math.isfinite(teststep), "MinLMSetGradientCheck: TestStep contains NaN or Infinite");
            alglib.ap.assert((double)(teststep)>=(double)(0), "MinLMSetGradientCheck: invalid argument TestStep(TestStep<0)");
            state.teststep = teststep;
        }


        /*************************************************************************
        Prepare internal structures (except for RComm).

        Note: M must be zero for FGH mode, non-zero for V/VJ/FJ/FGJ mode.
        *************************************************************************/
        private static void lmprepare(int n,
            int m,
            bool havegrad,
            minlmstate state)
        {
            int i = 0;

            if( n<=0 || m<0 )
            {
                return;
            }
            if( havegrad )
            {
                state.g = new double[n];
            }
            if( m!=0 )
            {
                state.j = new double[m, n];
                state.fi = new double[m];
                state.fibase = new double[m];
                state.deltaf = new double[m];
                state.fm1 = new double[m];
                state.fp1 = new double[m];
                state.fc1 = new double[m];
                state.gm1 = new double[m];
                state.gp1 = new double[m];
                state.gc1 = new double[m];
            }
            else
            {
                state.h = new double[n, n];
            }
            state.x = new double[n];
            state.deltax = new double[n];
            state.quadraticmodel = new double[n, n];
            state.xbase = new double[n];
            state.gbase = new double[n];
            state.xdir = new double[n];
            state.tmp0 = new double[n];
            
            //
            // prepare internal L-BFGS
            //
            for(i=0; i<=n-1; i++)
            {
                state.x[i] = 0;
            }
            minlbfgs.minlbfgscreate(n, Math.Min(additers, n), state.x, state.internalstate);
            minlbfgs.minlbfgssetcond(state.internalstate, 0.0, 0.0, 0.0, Math.Min(additers, n));
            
            //
            // Prepare internal QP solver
            //
            minqp.minqpcreate(n, state.qpstate);
            minqp.minqpsetalgocholesky(state.qpstate);
            
            //
            // Prepare boundary constraints
            //
            state.bndl = new double[n];
            state.bndu = new double[n];
            state.havebndl = new bool[n];
            state.havebndu = new bool[n];
            for(i=0; i<=n-1; i++)
            {
                state.bndl[i] = Double.NegativeInfinity;
                state.havebndl[i] = false;
                state.bndu[i] = Double.PositiveInfinity;
                state.havebndu[i] = false;
            }
            
            //
            // Prepare scaling matrix
            //
            state.s = new double[n];
            for(i=0; i<=n-1; i++)
            {
                state.s[i] = 1.0;
            }
        }


        /*************************************************************************
        Clears request fileds (to be sure that we don't forgot to clear something)
        *************************************************************************/
        private static void clearrequestfields(minlmstate state)
        {
            state.needf = false;
            state.needfg = false;
            state.needfgh = false;
            state.needfij = false;
            state.needfi = false;
            state.xupdated = false;
        }


        /*************************************************************************
        Increases lambda, returns False when there is a danger of overflow
        *************************************************************************/
        private static bool increaselambda(ref double lambdav,
            ref double nu)
        {
            bool result = new bool();
            double lnlambda = 0;
            double lnnu = 0;
            double lnlambdaup = 0;
            double lnmax = 0;

            result = false;
            lnlambda = Math.Log(lambdav);
            lnlambdaup = Math.Log(lambdaup);
            lnnu = Math.Log(nu);
            lnmax = Math.Log(math.maxrealnumber);
            if( (double)(lnlambda+lnlambdaup+lnnu)>(double)(0.25*lnmax) )
            {
                return result;
            }
            if( (double)(lnnu+Math.Log(2))>(double)(lnmax) )
            {
                return result;
            }
            lambdav = lambdav*lambdaup*nu;
            nu = nu*2;
            result = true;
            return result;
        }


        /*************************************************************************
        Decreases lambda, but leaves it unchanged when there is danger of underflow.
        *************************************************************************/
        private static void decreaselambda(ref double lambdav,
            ref double nu)
        {
            nu = 1;
            if( (double)(Math.Log(lambdav)+Math.Log(lambdadown))<(double)(Math.Log(math.minrealnumber)) )
            {
                lambdav = math.minrealnumber;
            }
            else
            {
                lambdav = lambdav*lambdadown;
            }
        }


        /*************************************************************************
        Returns norm of bounded scaled anti-gradient.

        Bounded antigradient is a vector obtained from  anti-gradient  by  zeroing
        components which point outwards:
            result = norm(v)
            v[i]=0     if ((-g[i]<0)and(x[i]=bndl[i])) or
                          ((-g[i]>0)and(x[i]=bndu[i]))
            v[i]=-g[i]*s[i] otherwise, where s[i] is a scale for I-th variable

        This function may be used to check a stopping criterion.

          -- ALGLIB --
             Copyright 14.01.2011 by Bochkanov Sergey
        *************************************************************************/
        private static double boundedscaledantigradnorm(minlmstate state,
            double[] x,
            double[] g)
        {
            double result = 0;
            int n = 0;
            int i = 0;
            double v = 0;

            result = 0;
            n = state.n;
            for(i=0; i<=n-1; i++)
            {
                v = -(g[i]*state.s[i]);
                if( state.havebndl[i] )
                {
                    if( (double)(x[i])<=(double)(state.bndl[i]) && (double)(-g[i])<(double)(0) )
                    {
                        v = 0;
                    }
                }
                if( state.havebndu[i] )
                {
                    if( (double)(x[i])>=(double)(state.bndu[i]) && (double)(-g[i])>(double)(0) )
                    {
                        v = 0;
                    }
                }
                result = result+math.sqr(v);
            }
            result = Math.Sqrt(result);
            return result;
        }


    }
    public class mincomp
    {
        public class minasastate : apobject
        {
            public int n;
            public double epsg;
            public double epsf;
            public double epsx;
            public int maxits;
            public bool xrep;
            public double stpmax;
            public int cgtype;
            public int k;
            public int nfev;
            public int mcstage;
            public double[] bndl;
            public double[] bndu;
            public int curalgo;
            public int acount;
            public double mu;
            public double finit;
            public double dginit;
            public double[] ak;
            public double[] xk;
            public double[] dk;
            public double[] an;
            public double[] xn;
            public double[] dn;
            public double[] d;
            public double fold;
            public double stp;
            public double[] work;
            public double[] yk;
            public double[] gc;
            public double laststep;
            public double[] x;
            public double f;
            public double[] g;
            public bool needfg;
            public bool xupdated;
            public rcommstate rstate;
            public int repiterationscount;
            public int repnfev;
            public int repterminationtype;
            public int debugrestartscount;
            public linmin.linminstate lstate;
            public double betahs;
            public double betady;
            public minasastate()
            {
                init();
            }
            public override void init()
            {
                bndl = new double[0];
                bndu = new double[0];
                ak = new double[0];
                xk = new double[0];
                dk = new double[0];
                an = new double[0];
                xn = new double[0];
                dn = new double[0];
                d = new double[0];
                work = new double[0];
                yk = new double[0];
                gc = new double[0];
                x = new double[0];
                g = new double[0];
                rstate = new rcommstate();
                lstate = new linmin.linminstate();
            }
            public override alglib.apobject make_copy()
            {
                minasastate _result = new minasastate();
                _result.n = n;
                _result.epsg = epsg;
                _result.epsf = epsf;
                _result.epsx = epsx;
                _result.maxits = maxits;
                _result.xrep = xrep;
                _result.stpmax = stpmax;
                _result.cgtype = cgtype;
                _result.k = k;
                _result.nfev = nfev;
                _result.mcstage = mcstage;
                _result.bndl = (double[])bndl.Clone();
                _result.bndu = (double[])bndu.Clone();
                _result.curalgo = curalgo;
                _result.acount = acount;
                _result.mu = mu;
                _result.finit = finit;
                _result.dginit = dginit;
                _result.ak = (double[])ak.Clone();
                _result.xk = (double[])xk.Clone();
                _result.dk = (double[])dk.Clone();
                _result.an = (double[])an.Clone();
                _result.xn = (double[])xn.Clone();
                _result.dn = (double[])dn.Clone();
                _result.d = (double[])d.Clone();
                _result.fold = fold;
                _result.stp = stp;
                _result.work = (double[])work.Clone();
                _result.yk = (double[])yk.Clone();
                _result.gc = (double[])gc.Clone();
                _result.laststep = laststep;
                _result.x = (double[])x.Clone();
                _result.f = f;
                _result.g = (double[])g.Clone();
                _result.needfg = needfg;
                _result.xupdated = xupdated;
                _result.rstate = (rcommstate)rstate.make_copy();
                _result.repiterationscount = repiterationscount;
                _result.repnfev = repnfev;
                _result.repterminationtype = repterminationtype;
                _result.debugrestartscount = debugrestartscount;
                _result.lstate = (linmin.linminstate)lstate.make_copy();
                _result.betahs = betahs;
                _result.betady = betady;
                return _result;
            }
        };


        public class minasareport : apobject
        {
            public int iterationscount;
            public int nfev;
            public int terminationtype;
            public int activeconstraints;
            public minasareport()
            {
                init();
            }
            public override void init()
            {
            }
            public override alglib.apobject make_copy()
            {
                minasareport _result = new minasareport();
                _result.iterationscount = iterationscount;
                _result.nfev = nfev;
                _result.terminationtype = terminationtype;
                _result.activeconstraints = activeconstraints;
                return _result;
            }
        };




        public const int n1 = 2;
        public const int n2 = 2;
        public const double stpmin = 1.0E-300;
        public const double gtol = 0.3;
        public const double gpaftol = 0.0001;
        public const double gpadecay = 0.5;
        public const double asarho = 0.5;


        /*************************************************************************
        Obsolete function, use MinLBFGSSetPrecDefault() instead.

          -- ALGLIB --
             Copyright 13.10.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void minlbfgssetdefaultpreconditioner(minlbfgs.minlbfgsstate state)
        {
            minlbfgs.minlbfgssetprecdefault(state);
        }


        /*************************************************************************
        Obsolete function, use MinLBFGSSetCholeskyPreconditioner() instead.

          -- ALGLIB --
             Copyright 13.10.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void minlbfgssetcholeskypreconditioner(minlbfgs.minlbfgsstate state,
            double[,] p,
            bool isupper)
        {
            minlbfgs.minlbfgssetpreccholesky(state, p, isupper);
        }


        /*************************************************************************
        This is obsolete function which was used by previous version of the  BLEIC
        optimizer. It does nothing in the current version of BLEIC.

          -- ALGLIB --
             Copyright 28.11.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void minbleicsetbarrierwidth(minbleic.minbleicstate state,
            double mu)
        {
        }


        /*************************************************************************
        This is obsolete function which was used by previous version of the  BLEIC
        optimizer. It does nothing in the current version of BLEIC.

          -- ALGLIB --
             Copyright 28.11.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void minbleicsetbarrierdecay(minbleic.minbleicstate state,
            double mudecay)
        {
        }


        /*************************************************************************
        Obsolete optimization algorithm.
        Was replaced by MinBLEIC subpackage.

          -- ALGLIB --
             Copyright 25.03.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void minasacreate(int n,
            double[] x,
            double[] bndl,
            double[] bndu,
            minasastate state)
        {
            int i = 0;

            alglib.ap.assert(n>=1, "MinASA: N too small!");
            alglib.ap.assert(alglib.ap.len(x)>=n, "MinCGCreate: Length(X)<N!");
            alglib.ap.assert(apserv.isfinitevector(x, n), "MinCGCreate: X contains infinite or NaN values!");
            alglib.ap.assert(alglib.ap.len(bndl)>=n, "MinCGCreate: Length(BndL)<N!");
            alglib.ap.assert(apserv.isfinitevector(bndl, n), "MinCGCreate: BndL contains infinite or NaN values!");
            alglib.ap.assert(alglib.ap.len(bndu)>=n, "MinCGCreate: Length(BndU)<N!");
            alglib.ap.assert(apserv.isfinitevector(bndu, n), "MinCGCreate: BndU contains infinite or NaN values!");
            for(i=0; i<=n-1; i++)
            {
                alglib.ap.assert((double)(bndl[i])<=(double)(bndu[i]), "MinASA: inconsistent bounds!");
                alglib.ap.assert((double)(bndl[i])<=(double)(x[i]), "MinASA: infeasible X!");
                alglib.ap.assert((double)(x[i])<=(double)(bndu[i]), "MinASA: infeasible X!");
            }
            
            //
            // Initialize
            //
            state.n = n;
            minasasetcond(state, 0, 0, 0, 0);
            minasasetxrep(state, false);
            minasasetstpmax(state, 0);
            minasasetalgorithm(state, -1);
            state.bndl = new double[n];
            state.bndu = new double[n];
            state.ak = new double[n];
            state.xk = new double[n];
            state.dk = new double[n];
            state.an = new double[n];
            state.xn = new double[n];
            state.dn = new double[n];
            state.x = new double[n];
            state.d = new double[n];
            state.g = new double[n];
            state.gc = new double[n];
            state.work = new double[n];
            state.yk = new double[n];
            minasarestartfrom(state, x, bndl, bndu);
        }


        /*************************************************************************
        Obsolete optimization algorithm.
        Was replaced by MinBLEIC subpackage.

          -- ALGLIB --
             Copyright 02.04.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void minasasetcond(minasastate state,
            double epsg,
            double epsf,
            double epsx,
            int maxits)
        {
            alglib.ap.assert(math.isfinite(epsg), "MinASASetCond: EpsG is not finite number!");
            alglib.ap.assert((double)(epsg)>=(double)(0), "MinASASetCond: negative EpsG!");
            alglib.ap.assert(math.isfinite(epsf), "MinASASetCond: EpsF is not finite number!");
            alglib.ap.assert((double)(epsf)>=(double)(0), "MinASASetCond: negative EpsF!");
            alglib.ap.assert(math.isfinite(epsx), "MinASASetCond: EpsX is not finite number!");
            alglib.ap.assert((double)(epsx)>=(double)(0), "MinASASetCond: negative EpsX!");
            alglib.ap.assert(maxits>=0, "MinASASetCond: negative MaxIts!");
            if( (((double)(epsg)==(double)(0) && (double)(epsf)==(double)(0)) && (double)(epsx)==(double)(0)) && maxits==0 )
            {
                epsx = 1.0E-6;
            }
            state.epsg = epsg;
            state.epsf = epsf;
            state.epsx = epsx;
            state.maxits = maxits;
        }


        /*************************************************************************
        Obsolete optimization algorithm.
        Was replaced by MinBLEIC subpackage.

          -- ALGLIB --
             Copyright 02.04.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void minasasetxrep(minasastate state,
            bool needxrep)
        {
            state.xrep = needxrep;
        }


        /*************************************************************************
        Obsolete optimization algorithm.
        Was replaced by MinBLEIC subpackage.

          -- ALGLIB --
             Copyright 02.04.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void minasasetalgorithm(minasastate state,
            int algotype)
        {
            alglib.ap.assert(algotype>=-1 && algotype<=1, "MinASASetAlgorithm: incorrect AlgoType!");
            if( algotype==-1 )
            {
                algotype = 1;
            }
            state.cgtype = algotype;
        }


        /*************************************************************************
        Obsolete optimization algorithm.
        Was replaced by MinBLEIC subpackage.

          -- ALGLIB --
             Copyright 02.04.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void minasasetstpmax(minasastate state,
            double stpmax)
        {
            alglib.ap.assert(math.isfinite(stpmax), "MinASASetStpMax: StpMax is not finite!");
            alglib.ap.assert((double)(stpmax)>=(double)(0), "MinASASetStpMax: StpMax<0!");
            state.stpmax = stpmax;
        }


        /*************************************************************************

          -- ALGLIB --
             Copyright 20.03.2009 by Bochkanov Sergey
        *************************************************************************/
        public static bool minasaiteration(minasastate state)
        {
            bool result = new bool();
            int n = 0;
            int i = 0;
            double betak = 0;
            double v = 0;
            double vv = 0;
            int mcinfo = 0;
            bool b = new bool();
            bool stepfound = new bool();
            int diffcnt = 0;
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
                i = state.rstate.ia[1];
                mcinfo = state.rstate.ia[2];
                diffcnt = state.rstate.ia[3];
                b = state.rstate.ba[0];
                stepfound = state.rstate.ba[1];
                betak = state.rstate.ra[0];
                v = state.rstate.ra[1];
                vv = state.rstate.ra[2];
            }
            else
            {
                n = -983;
                i = -989;
                mcinfo = -834;
                diffcnt = 900;
                b = true;
                stepfound = false;
                betak = 214;
                v = -338;
                vv = -686;
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
            if( state.rstate.stage==4 )
            {
                goto lbl_4;
            }
            if( state.rstate.stage==5 )
            {
                goto lbl_5;
            }
            if( state.rstate.stage==6 )
            {
                goto lbl_6;
            }
            if( state.rstate.stage==7 )
            {
                goto lbl_7;
            }
            if( state.rstate.stage==8 )
            {
                goto lbl_8;
            }
            if( state.rstate.stage==9 )
            {
                goto lbl_9;
            }
            if( state.rstate.stage==10 )
            {
                goto lbl_10;
            }
            if( state.rstate.stage==11 )
            {
                goto lbl_11;
            }
            if( state.rstate.stage==12 )
            {
                goto lbl_12;
            }
            if( state.rstate.stage==13 )
            {
                goto lbl_13;
            }
            if( state.rstate.stage==14 )
            {
                goto lbl_14;
            }
            
            //
            // Routine body
            //
            
            //
            // Prepare
            //
            n = state.n;
            state.repterminationtype = 0;
            state.repiterationscount = 0;
            state.repnfev = 0;
            state.debugrestartscount = 0;
            state.cgtype = 1;
            for(i_=0; i_<=n-1;i_++)
            {
                state.xk[i_] = state.x[i_];
            }
            for(i=0; i<=n-1; i++)
            {
                if( (double)(state.xk[i])==(double)(state.bndl[i]) || (double)(state.xk[i])==(double)(state.bndu[i]) )
                {
                    state.ak[i] = 0;
                }
                else
                {
                    state.ak[i] = 1;
                }
            }
            state.mu = 0.1;
            state.curalgo = 0;
            
            //
            // Calculate F/G, initialize algorithm
            //
            clearrequestfields(state);
            state.needfg = true;
            state.rstate.stage = 0;
            goto lbl_rcomm;
        lbl_0:
            state.needfg = false;
            if( !state.xrep )
            {
                goto lbl_15;
            }
            
            //
            // progress report
            //
            clearrequestfields(state);
            state.xupdated = true;
            state.rstate.stage = 1;
            goto lbl_rcomm;
        lbl_1:
            state.xupdated = false;
        lbl_15:
            if( (double)(asaboundedantigradnorm(state))<=(double)(state.epsg) )
            {
                state.repterminationtype = 4;
                result = false;
                return result;
            }
            state.repnfev = state.repnfev+1;
            
            //
            // Main cycle
            //
            // At the beginning of new iteration:
            // * CurAlgo stores current algorithm selector
            // * State.XK, State.F and State.G store current X/F/G
            // * State.AK stores current set of active constraints
            //
        lbl_17:
            if( false )
            {
                goto lbl_18;
            }
            
            //
            // GPA algorithm
            //
            if( state.curalgo!=0 )
            {
                goto lbl_19;
            }
            state.k = 0;
            state.acount = 0;
        lbl_21:
            if( false )
            {
                goto lbl_22;
            }
            
            //
            // Determine Dk = proj(xk - gk)-xk
            //
            for(i=0; i<=n-1; i++)
            {
                state.d[i] = apserv.boundval(state.xk[i]-state.g[i], state.bndl[i], state.bndu[i])-state.xk[i];
            }
            
            //
            // Armijo line search.
            // * exact search with alpha=1 is tried first,
            //   'exact' means that we evaluate f() EXACTLY at
            //   bound(x-g,bndl,bndu), without intermediate floating
            //   point operations.
            // * alpha<1 are tried if explicit search wasn't successful
            // Result is placed into XN.
            //
            // Two types of search are needed because we can't
            // just use second type with alpha=1 because in finite
            // precision arithmetics (x1-x0)+x0 may differ from x1.
            // So while x1 is correctly bounded (it lie EXACTLY on
            // boundary, if it is active), (x1-x0)+x0 may be
            // not bounded.
            //
            v = 0.0;
            for(i_=0; i_<=n-1;i_++)
            {
                v += state.d[i_]*state.g[i_];
            }
            state.dginit = v;
            state.finit = state.f;
            if( !((double)(asad1norm(state))<=(double)(state.stpmax) || (double)(state.stpmax)==(double)(0)) )
            {
                goto lbl_23;
            }
            
            //
            // Try alpha=1 step first
            //
            for(i=0; i<=n-1; i++)
            {
                state.x[i] = apserv.boundval(state.xk[i]-state.g[i], state.bndl[i], state.bndu[i]);
            }
            clearrequestfields(state);
            state.needfg = true;
            state.rstate.stage = 2;
            goto lbl_rcomm;
        lbl_2:
            state.needfg = false;
            state.repnfev = state.repnfev+1;
            stepfound = (double)(state.f)<=(double)(state.finit+gpaftol*state.dginit);
            goto lbl_24;
        lbl_23:
            stepfound = false;
        lbl_24:
            if( !stepfound )
            {
                goto lbl_25;
            }
            
            //
            // we are at the boundary(ies)
            //
            for(i_=0; i_<=n-1;i_++)
            {
                state.xn[i_] = state.x[i_];
            }
            state.stp = 1;
            goto lbl_26;
        lbl_25:
            
            //
            // alpha=1 is too large, try smaller values
            //
            state.stp = 1;
            linmin.linminnormalized(ref state.d, ref state.stp, n);
            state.dginit = state.dginit/state.stp;
            state.stp = gpadecay*state.stp;
            if( (double)(state.stpmax)>(double)(0) )
            {
                state.stp = Math.Min(state.stp, state.stpmax);
            }
        lbl_27:
            if( false )
            {
                goto lbl_28;
            }
            v = state.stp;
            for(i_=0; i_<=n-1;i_++)
            {
                state.x[i_] = state.xk[i_];
            }
            for(i_=0; i_<=n-1;i_++)
            {
                state.x[i_] = state.x[i_] + v*state.d[i_];
            }
            clearrequestfields(state);
            state.needfg = true;
            state.rstate.stage = 3;
            goto lbl_rcomm;
        lbl_3:
            state.needfg = false;
            state.repnfev = state.repnfev+1;
            if( (double)(state.stp)<=(double)(stpmin) )
            {
                goto lbl_28;
            }
            if( (double)(state.f)<=(double)(state.finit+state.stp*gpaftol*state.dginit) )
            {
                goto lbl_28;
            }
            state.stp = state.stp*gpadecay;
            goto lbl_27;
        lbl_28:
            for(i_=0; i_<=n-1;i_++)
            {
                state.xn[i_] = state.x[i_];
            }
        lbl_26:
            state.repiterationscount = state.repiterationscount+1;
            if( !state.xrep )
            {
                goto lbl_29;
            }
            
            //
            // progress report
            //
            clearrequestfields(state);
            state.xupdated = true;
            state.rstate.stage = 4;
            goto lbl_rcomm;
        lbl_4:
            state.xupdated = false;
        lbl_29:
            
            //
            // Calculate new set of active constraints.
            // Reset counter if active set was changed.
            // Prepare for the new iteration
            //
            for(i=0; i<=n-1; i++)
            {
                if( (double)(state.xn[i])==(double)(state.bndl[i]) || (double)(state.xn[i])==(double)(state.bndu[i]) )
                {
                    state.an[i] = 0;
                }
                else
                {
                    state.an[i] = 1;
                }
            }
            for(i=0; i<=n-1; i++)
            {
                if( (double)(state.ak[i])!=(double)(state.an[i]) )
                {
                    state.acount = -1;
                    break;
                }
            }
            state.acount = state.acount+1;
            for(i_=0; i_<=n-1;i_++)
            {
                state.xk[i_] = state.xn[i_];
            }
            for(i_=0; i_<=n-1;i_++)
            {
                state.ak[i_] = state.an[i_];
            }
            
            //
            // Stopping conditions
            //
            if( !(state.repiterationscount>=state.maxits && state.maxits>0) )
            {
                goto lbl_31;
            }
            
            //
            // Too many iterations
            //
            state.repterminationtype = 5;
            if( !state.xrep )
            {
                goto lbl_33;
            }
            clearrequestfields(state);
            state.xupdated = true;
            state.rstate.stage = 5;
            goto lbl_rcomm;
        lbl_5:
            state.xupdated = false;
        lbl_33:
            result = false;
            return result;
        lbl_31:
            if( (double)(asaboundedantigradnorm(state))>(double)(state.epsg) )
            {
                goto lbl_35;
            }
            
            //
            // Gradient is small enough
            //
            state.repterminationtype = 4;
            if( !state.xrep )
            {
                goto lbl_37;
            }
            clearrequestfields(state);
            state.xupdated = true;
            state.rstate.stage = 6;
            goto lbl_rcomm;
        lbl_6:
            state.xupdated = false;
        lbl_37:
            result = false;
            return result;
        lbl_35:
            v = 0.0;
            for(i_=0; i_<=n-1;i_++)
            {
                v += state.d[i_]*state.d[i_];
            }
            if( (double)(Math.Sqrt(v)*state.stp)>(double)(state.epsx) )
            {
                goto lbl_39;
            }
            
            //
            // Step size is too small, no further improvement is
            // possible
            //
            state.repterminationtype = 2;
            if( !state.xrep )
            {
                goto lbl_41;
            }
            clearrequestfields(state);
            state.xupdated = true;
            state.rstate.stage = 7;
            goto lbl_rcomm;
        lbl_7:
            state.xupdated = false;
        lbl_41:
            result = false;
            return result;
        lbl_39:
            if( (double)(state.finit-state.f)>(double)(state.epsf*Math.Max(Math.Abs(state.finit), Math.Max(Math.Abs(state.f), 1.0))) )
            {
                goto lbl_43;
            }
            
            //
            // F(k+1)-F(k) is small enough
            //
            state.repterminationtype = 1;
            if( !state.xrep )
            {
                goto lbl_45;
            }
            clearrequestfields(state);
            state.xupdated = true;
            state.rstate.stage = 8;
            goto lbl_rcomm;
        lbl_8:
            state.xupdated = false;
        lbl_45:
            result = false;
            return result;
        lbl_43:
            
            //
            // Decide - should we switch algorithm or not
            //
            if( asauisempty(state) )
            {
                if( (double)(asaginorm(state))>=(double)(state.mu*asad1norm(state)) )
                {
                    state.curalgo = 1;
                    goto lbl_22;
                }
                else
                {
                    state.mu = state.mu*asarho;
                }
            }
            else
            {
                if( state.acount==n1 )
                {
                    if( (double)(asaginorm(state))>=(double)(state.mu*asad1norm(state)) )
                    {
                        state.curalgo = 1;
                        goto lbl_22;
                    }
                }
            }
            
            //
            // Next iteration
            //
            state.k = state.k+1;
            goto lbl_21;
        lbl_22:
        lbl_19:
            
            //
            // CG algorithm
            //
            if( state.curalgo!=1 )
            {
                goto lbl_47;
            }
            
            //
            // first, check that there are non-active constraints.
            // move to GPA algorithm, if all constraints are active
            //
            b = true;
            for(i=0; i<=n-1; i++)
            {
                if( (double)(state.ak[i])!=(double)(0) )
                {
                    b = false;
                    break;
                }
            }
            if( b )
            {
                state.curalgo = 0;
                goto lbl_17;
            }
            
            //
            // CG iterations
            //
            state.fold = state.f;
            for(i_=0; i_<=n-1;i_++)
            {
                state.xk[i_] = state.x[i_];
            }
            for(i=0; i<=n-1; i++)
            {
                state.dk[i] = -(state.g[i]*state.ak[i]);
                state.gc[i] = state.g[i]*state.ak[i];
            }
        lbl_49:
            if( false )
            {
                goto lbl_50;
            }
            
            //
            // Store G[k] for later calculation of Y[k]
            //
            for(i=0; i<=n-1; i++)
            {
                state.yk[i] = -state.gc[i];
            }
            
            //
            // Make a CG step in direction given by DK[]:
            // * calculate step. Step projection into feasible set
            //   is used. It has several benefits: a) step may be
            //   found with usual line search, b) multiple constraints
            //   may be activated with one step, c) activated constraints
            //   are detected in a natural way - just compare x[i] with
            //   bounds
            // * update active set, set B to True, if there
            //   were changes in the set.
            //
            for(i_=0; i_<=n-1;i_++)
            {
                state.d[i_] = state.dk[i_];
            }
            for(i_=0; i_<=n-1;i_++)
            {
                state.xn[i_] = state.xk[i_];
            }
            state.mcstage = 0;
            state.stp = 1;
            linmin.linminnormalized(ref state.d, ref state.stp, n);
            if( (double)(state.laststep)!=(double)(0) )
            {
                state.stp = state.laststep;
            }
            linmin.mcsrch(n, ref state.xn, ref state.f, ref state.gc, state.d, ref state.stp, state.stpmax, gtol, ref mcinfo, ref state.nfev, ref state.work, state.lstate, ref state.mcstage);
        lbl_51:
            if( state.mcstage==0 )
            {
                goto lbl_52;
            }
            
            //
            // preprocess data: bound State.XN so it belongs to the
            // feasible set and store it in the State.X
            //
            for(i=0; i<=n-1; i++)
            {
                state.x[i] = apserv.boundval(state.xn[i], state.bndl[i], state.bndu[i]);
            }
            
            //
            // RComm
            //
            clearrequestfields(state);
            state.needfg = true;
            state.rstate.stage = 9;
            goto lbl_rcomm;
        lbl_9:
            state.needfg = false;
            
            //
            // postprocess data: zero components of G corresponding to
            // the active constraints
            //
            for(i=0; i<=n-1; i++)
            {
                if( (double)(state.x[i])==(double)(state.bndl[i]) || (double)(state.x[i])==(double)(state.bndu[i]) )
                {
                    state.gc[i] = 0;
                }
                else
                {
                    state.gc[i] = state.g[i];
                }
            }
            linmin.mcsrch(n, ref state.xn, ref state.f, ref state.gc, state.d, ref state.stp, state.stpmax, gtol, ref mcinfo, ref state.nfev, ref state.work, state.lstate, ref state.mcstage);
            goto lbl_51;
        lbl_52:
            diffcnt = 0;
            for(i=0; i<=n-1; i++)
            {
                
                //
                // XN contains unprojected result, project it,
                // save copy to X (will be used for progress reporting)
                //
                state.xn[i] = apserv.boundval(state.xn[i], state.bndl[i], state.bndu[i]);
                
                //
                // update active set
                //
                if( (double)(state.xn[i])==(double)(state.bndl[i]) || (double)(state.xn[i])==(double)(state.bndu[i]) )
                {
                    state.an[i] = 0;
                }
                else
                {
                    state.an[i] = 1;
                }
                if( (double)(state.an[i])!=(double)(state.ak[i]) )
                {
                    diffcnt = diffcnt+1;
                }
                state.ak[i] = state.an[i];
            }
            for(i_=0; i_<=n-1;i_++)
            {
                state.xk[i_] = state.xn[i_];
            }
            state.repnfev = state.repnfev+state.nfev;
            state.repiterationscount = state.repiterationscount+1;
            if( !state.xrep )
            {
                goto lbl_53;
            }
            
            //
            // progress report
            //
            clearrequestfields(state);
            state.xupdated = true;
            state.rstate.stage = 10;
            goto lbl_rcomm;
        lbl_10:
            state.xupdated = false;
        lbl_53:
            
            //
            // Update info about step length
            //
            v = 0.0;
            for(i_=0; i_<=n-1;i_++)
            {
                v += state.d[i_]*state.d[i_];
            }
            state.laststep = Math.Sqrt(v)*state.stp;
            
            //
            // Check stopping conditions.
            //
            if( (double)(asaboundedantigradnorm(state))>(double)(state.epsg) )
            {
                goto lbl_55;
            }
            
            //
            // Gradient is small enough
            //
            state.repterminationtype = 4;
            if( !state.xrep )
            {
                goto lbl_57;
            }
            clearrequestfields(state);
            state.xupdated = true;
            state.rstate.stage = 11;
            goto lbl_rcomm;
        lbl_11:
            state.xupdated = false;
        lbl_57:
            result = false;
            return result;
        lbl_55:
            if( !(state.repiterationscount>=state.maxits && state.maxits>0) )
            {
                goto lbl_59;
            }
            
            //
            // Too many iterations
            //
            state.repterminationtype = 5;
            if( !state.xrep )
            {
                goto lbl_61;
            }
            clearrequestfields(state);
            state.xupdated = true;
            state.rstate.stage = 12;
            goto lbl_rcomm;
        lbl_12:
            state.xupdated = false;
        lbl_61:
            result = false;
            return result;
        lbl_59:
            if( !((double)(asaginorm(state))>=(double)(state.mu*asad1norm(state)) && diffcnt==0) )
            {
                goto lbl_63;
            }
            
            //
            // These conditions (EpsF/EpsX) are explicitly or implicitly
            // related to the current step size and influenced
            // by changes in the active constraints.
            //
            // For these reasons they are checked only when we don't
            // want to 'unstick' at the end of the iteration and there
            // were no changes in the active set.
            //
            // NOTE: consition |G|>=Mu*|D1| must be exactly opposite
            // to the condition used to switch back to GPA. At least
            // one inequality must be strict, otherwise infinite cycle
            // may occur when |G|=Mu*|D1| (we DON'T test stopping
            // conditions and we DON'T switch to GPA, so we cycle
            // indefinitely).
            //
            if( (double)(state.fold-state.f)>(double)(state.epsf*Math.Max(Math.Abs(state.fold), Math.Max(Math.Abs(state.f), 1.0))) )
            {
                goto lbl_65;
            }
            
            //
            // F(k+1)-F(k) is small enough
            //
            state.repterminationtype = 1;
            if( !state.xrep )
            {
                goto lbl_67;
            }
            clearrequestfields(state);
            state.xupdated = true;
            state.rstate.stage = 13;
            goto lbl_rcomm;
        lbl_13:
            state.xupdated = false;
        lbl_67:
            result = false;
            return result;
        lbl_65:
            if( (double)(state.laststep)>(double)(state.epsx) )
            {
                goto lbl_69;
            }
            
            //
            // X(k+1)-X(k) is small enough
            //
            state.repterminationtype = 2;
            if( !state.xrep )
            {
                goto lbl_71;
            }
            clearrequestfields(state);
            state.xupdated = true;
            state.rstate.stage = 14;
            goto lbl_rcomm;
        lbl_14:
            state.xupdated = false;
        lbl_71:
            result = false;
            return result;
        lbl_69:
        lbl_63:
            
            //
            // Check conditions for switching
            //
            if( (double)(asaginorm(state))<(double)(state.mu*asad1norm(state)) )
            {
                state.curalgo = 0;
                goto lbl_50;
            }
            if( diffcnt>0 )
            {
                if( asauisempty(state) || diffcnt>=n2 )
                {
                    state.curalgo = 1;
                }
                else
                {
                    state.curalgo = 0;
                }
                goto lbl_50;
            }
            
            //
            // Calculate D(k+1)
            //
            // Line search may result in:
            // * maximum feasible step being taken (already processed)
            // * point satisfying Wolfe conditions
            // * some kind of error (CG is restarted by assigning 0.0 to Beta)
            //
            if( mcinfo==1 )
            {
                
                //
                // Standard Wolfe conditions are satisfied:
                // * calculate Y[K] and BetaK
                //
                for(i_=0; i_<=n-1;i_++)
                {
                    state.yk[i_] = state.yk[i_] + state.gc[i_];
                }
                vv = 0.0;
                for(i_=0; i_<=n-1;i_++)
                {
                    vv += state.yk[i_]*state.dk[i_];
                }
                v = 0.0;
                for(i_=0; i_<=n-1;i_++)
                {
                    v += state.gc[i_]*state.gc[i_];
                }
                state.betady = v/vv;
                v = 0.0;
                for(i_=0; i_<=n-1;i_++)
                {
                    v += state.gc[i_]*state.yk[i_];
                }
                state.betahs = v/vv;
                if( state.cgtype==0 )
                {
                    betak = state.betady;
                }
                if( state.cgtype==1 )
                {
                    betak = Math.Max(0, Math.Min(state.betady, state.betahs));
                }
            }
            else
            {
                
                //
                // Something is wrong (may be function is too wild or too flat).
                //
                // We'll set BetaK=0, which will restart CG algorithm.
                // We can stop later (during normal checks) if stopping conditions are met.
                //
                betak = 0;
                state.debugrestartscount = state.debugrestartscount+1;
            }
            for(i_=0; i_<=n-1;i_++)
            {
                state.dn[i_] = -state.gc[i_];
            }
            for(i_=0; i_<=n-1;i_++)
            {
                state.dn[i_] = state.dn[i_] + betak*state.dk[i_];
            }
            for(i_=0; i_<=n-1;i_++)
            {
                state.dk[i_] = state.dn[i_];
            }
            
            //
            // update other information
            //
            state.fold = state.f;
            state.k = state.k+1;
            goto lbl_49;
        lbl_50:
        lbl_47:
            goto lbl_17;
        lbl_18:
            result = false;
            return result;
            
            //
            // Saving state
            //
        lbl_rcomm:
            result = true;
            state.rstate.ia[0] = n;
            state.rstate.ia[1] = i;
            state.rstate.ia[2] = mcinfo;
            state.rstate.ia[3] = diffcnt;
            state.rstate.ba[0] = b;
            state.rstate.ba[1] = stepfound;
            state.rstate.ra[0] = betak;
            state.rstate.ra[1] = v;
            state.rstate.ra[2] = vv;
            return result;
        }


        /*************************************************************************
        Obsolete optimization algorithm.
        Was replaced by MinBLEIC subpackage.

          -- ALGLIB --
             Copyright 20.03.2009 by Bochkanov Sergey
        *************************************************************************/
        public static void minasaresults(minasastate state,
            ref double[] x,
            minasareport rep)
        {
            x = new double[0];

            minasaresultsbuf(state, ref x, rep);
        }


        /*************************************************************************
        Obsolete optimization algorithm.
        Was replaced by MinBLEIC subpackage.

          -- ALGLIB --
             Copyright 20.03.2009 by Bochkanov Sergey
        *************************************************************************/
        public static void minasaresultsbuf(minasastate state,
            ref double[] x,
            minasareport rep)
        {
            int i = 0;
            int i_ = 0;

            if( alglib.ap.len(x)<state.n )
            {
                x = new double[state.n];
            }
            for(i_=0; i_<=state.n-1;i_++)
            {
                x[i_] = state.x[i_];
            }
            rep.iterationscount = state.repiterationscount;
            rep.nfev = state.repnfev;
            rep.terminationtype = state.repterminationtype;
            rep.activeconstraints = 0;
            for(i=0; i<=state.n-1; i++)
            {
                if( (double)(state.ak[i])==(double)(0) )
                {
                    rep.activeconstraints = rep.activeconstraints+1;
                }
            }
        }


        /*************************************************************************
        Obsolete optimization algorithm.
        Was replaced by MinBLEIC subpackage.

          -- ALGLIB --
             Copyright 30.07.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void minasarestartfrom(minasastate state,
            double[] x,
            double[] bndl,
            double[] bndu)
        {
            int i_ = 0;

            alglib.ap.assert(alglib.ap.len(x)>=state.n, "MinASARestartFrom: Length(X)<N!");
            alglib.ap.assert(apserv.isfinitevector(x, state.n), "MinASARestartFrom: X contains infinite or NaN values!");
            alglib.ap.assert(alglib.ap.len(bndl)>=state.n, "MinASARestartFrom: Length(BndL)<N!");
            alglib.ap.assert(apserv.isfinitevector(bndl, state.n), "MinASARestartFrom: BndL contains infinite or NaN values!");
            alglib.ap.assert(alglib.ap.len(bndu)>=state.n, "MinASARestartFrom: Length(BndU)<N!");
            alglib.ap.assert(apserv.isfinitevector(bndu, state.n), "MinASARestartFrom: BndU contains infinite or NaN values!");
            for(i_=0; i_<=state.n-1;i_++)
            {
                state.x[i_] = x[i_];
            }
            for(i_=0; i_<=state.n-1;i_++)
            {
                state.bndl[i_] = bndl[i_];
            }
            for(i_=0; i_<=state.n-1;i_++)
            {
                state.bndu[i_] = bndu[i_];
            }
            state.laststep = 0;
            state.rstate.ia = new int[3+1];
            state.rstate.ba = new bool[1+1];
            state.rstate.ra = new double[2+1];
            state.rstate.stage = -1;
            clearrequestfields(state);
        }


        /*************************************************************************
        Returns norm of bounded anti-gradient.

        Bounded antigradient is a vector obtained from  anti-gradient  by  zeroing
        components which point outwards:
            result = norm(v)
            v[i]=0     if ((-g[i]<0)and(x[i]=bndl[i])) or
                          ((-g[i]>0)and(x[i]=bndu[i]))
            v[i]=-g[i] otherwise

        This function may be used to check a stopping criterion.

          -- ALGLIB --
             Copyright 20.03.2009 by Bochkanov Sergey
        *************************************************************************/
        private static double asaboundedantigradnorm(minasastate state)
        {
            double result = 0;
            int i = 0;
            double v = 0;

            result = 0;
            for(i=0; i<=state.n-1; i++)
            {
                v = -state.g[i];
                if( (double)(state.x[i])==(double)(state.bndl[i]) && (double)(-state.g[i])<(double)(0) )
                {
                    v = 0;
                }
                if( (double)(state.x[i])==(double)(state.bndu[i]) && (double)(-state.g[i])>(double)(0) )
                {
                    v = 0;
                }
                result = result+math.sqr(v);
            }
            result = Math.Sqrt(result);
            return result;
        }


        /*************************************************************************
        Returns norm of GI(x).

        GI(x) is  a  gradient  vector  whose  components  associated  with  active
        constraints are zeroed. It  differs  from  bounded  anti-gradient  because
        components  of   GI(x)   are   zeroed  independently  of  sign(g[i]),  and
        anti-gradient's components are zeroed with respect to both constraint  and
        sign.

          -- ALGLIB --
             Copyright 20.03.2009 by Bochkanov Sergey
        *************************************************************************/
        private static double asaginorm(minasastate state)
        {
            double result = 0;
            int i = 0;

            result = 0;
            for(i=0; i<=state.n-1; i++)
            {
                if( (double)(state.x[i])!=(double)(state.bndl[i]) && (double)(state.x[i])!=(double)(state.bndu[i]) )
                {
                    result = result+math.sqr(state.g[i]);
                }
            }
            result = Math.Sqrt(result);
            return result;
        }


        /*************************************************************************
        Returns norm(D1(State.X))

        For a meaning of D1 see 'NEW ACTIVE SET ALGORITHM FOR BOX CONSTRAINED
        OPTIMIZATION' by WILLIAM W. HAGER AND HONGCHAO ZHANG.

          -- ALGLIB --
             Copyright 20.03.2009 by Bochkanov Sergey
        *************************************************************************/
        private static double asad1norm(minasastate state)
        {
            double result = 0;
            int i = 0;

            result = 0;
            for(i=0; i<=state.n-1; i++)
            {
                result = result+math.sqr(apserv.boundval(state.x[i]-state.g[i], state.bndl[i], state.bndu[i])-state.x[i]);
            }
            result = Math.Sqrt(result);
            return result;
        }


        /*************************************************************************
        Returns True, if U set is empty.

        * State.X is used as point,
        * State.G - as gradient,
        * D is calculated within function (because State.D may have different
          meaning depending on current optimization algorithm)

        For a meaning of U see 'NEW ACTIVE SET ALGORITHM FOR BOX CONSTRAINED
        OPTIMIZATION' by WILLIAM W. HAGER AND HONGCHAO ZHANG.

          -- ALGLIB --
             Copyright 20.03.2009 by Bochkanov Sergey
        *************************************************************************/
        private static bool asauisempty(minasastate state)
        {
            bool result = new bool();
            int i = 0;
            double d = 0;
            double d2 = 0;
            double d32 = 0;

            d = asad1norm(state);
            d2 = Math.Sqrt(d);
            d32 = d*d2;
            result = true;
            for(i=0; i<=state.n-1; i++)
            {
                if( (double)(Math.Abs(state.g[i]))>=(double)(d2) && (double)(Math.Min(state.x[i]-state.bndl[i], state.bndu[i]-state.x[i]))>=(double)(d32) )
                {
                    result = false;
                    return result;
                }
            }
            return result;
        }


        /*************************************************************************
        Clears request fileds (to be sure that we don't forgot to clear something)
        *************************************************************************/
        private static void clearrequestfields(minasastate state)
        {
            state.needfg = false;
            state.xupdated = false;
        }


    }
    public class linfeas
    {
        /*************************************************************************
        This structure is a linear feasibility solver.

        It finds feasible point subject to boundary and linear equality/inequality
        constraints.

        This  solver  is  suited  for  solution of multiple sequential feasibility
        subproblems - it keeps track of previously allocated memory and reuses  it
        as much as possible.
        *************************************************************************/
        public class linfeassolver : apobject
        {
            public double debugflops;
            public linfeassolver()
            {
                init();
            }
            public override void init()
            {
            }
            public override alglib.apobject make_copy()
            {
                linfeassolver _result = new linfeassolver();
                _result.debugflops = debugflops;
                return _result;
            }
        };






    }
}

