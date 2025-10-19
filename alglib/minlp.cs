/*************************************************************************
ALGLIB 4.06.0 (source code generated 2025-10-08)
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
#pragma warning disable 1691
#pragma warning disable 162
#pragma warning disable 164
#pragma warning disable 219
#pragma warning disable 8981
using System;

public partial class alglib
{



}
public partial class alglib
{



}
public partial class alglib
{


    /*************************************************************************
    This object stores nonlinear optimizer state.
    You should use functions provided by MinNLC subpackage to work  with  this
    object
    *************************************************************************/
    public class minlpsolverstate : alglibobject
    {
        //
        // Public declarations
        //
    
        public minlpsolverstate()
        {
            _innerobj = new minlpsolvers.minlpsolverstate();
        }
        
        public override alglib.alglibobject make_copy()
        {
            return new minlpsolverstate((minlpsolvers.minlpsolverstate)_innerobj.make_copy());
        }
    
        //
        // Although some of declarations below are public, you should not use them
        // They are intended for internal use only
        //
        private minlpsolvers.minlpsolverstate _innerobj;
        public minlpsolvers.minlpsolverstate innerobj { get { return _innerobj; } }
        public minlpsolverstate(minlpsolvers.minlpsolverstate obj)
        {
            _innerobj = obj;
        }
    }


    /*************************************************************************
    This structure stores the optimization report.

    The following fields are set by all MINLP solvers:
    * f                         objective value at the solution
    * nfev                      number of value/gradient evaluations
    * terminationtype           termination type (see below)

    The BBGD solver additionally sets the following fields:
    * pdgap                     final primal-dual gap
    * ntreenodes                number of B&B tree nodes traversed
    * nsubproblems              total number of NLP relaxations solved; can be
                                larger than ntreenodes because of restarts
    * nnodesbeforefeasibility   number of nodes evaluated before finding first
                                integer feasible solution

    TERMINATION CODES

    TerminationType field contains completion code, which can be either FAILURE
    code or SUCCESS code.

    === FAILURE CODE ===
      -33   timed out, failed to find a feasible point within  time  limit  or
            iteration budget
      -8    internal integrity control detected  infinite  or  NAN  values  in
            function/gradient, recovery was impossible.  Abnormal  termination
            signaled.
      -3    integer infeasibility is signaled:
            * for convex problems: proved to be infeasible
            * for nonconvex problems: a primal feasible point  is  nonexistent
              or too difficult to find


    === SUCCESS CODE ===
       2    successful solution:
            * for BBGD - entire tree was scanned
            * for MIVNS - either entire  integer  grid  was  scanned,  or  the
              neighborhood size  based  condition  was  triggered  (in  future
              versions other criteria may be introduced)
       5    a primal feasible point was found, but time or iteration limit was
            exhausted but we  failed  to  find  a  better  one  or  prove  its
            optimality; the best point so far is returned.
    *************************************************************************/
    public class minlpsolverreport : alglibobject
    {
        //
        // Public declarations
        //
        public double f { get { return _innerobj.f; } set { _innerobj.f = value; } }
        public int nfev { get { return _innerobj.nfev; } set { _innerobj.nfev = value; } }
        public int nsubproblems { get { return _innerobj.nsubproblems; } set { _innerobj.nsubproblems = value; } }
        public int ntreenodes { get { return _innerobj.ntreenodes; } set { _innerobj.ntreenodes = value; } }
        public int nnodesbeforefeasibility { get { return _innerobj.nnodesbeforefeasibility; } set { _innerobj.nnodesbeforefeasibility = value; } }
        public int terminationtype { get { return _innerobj.terminationtype; } set { _innerobj.terminationtype = value; } }
        public double pdgap { get { return _innerobj.pdgap; } set { _innerobj.pdgap = value; } }
    
        public minlpsolverreport()
        {
            _innerobj = new minlpsolvers.minlpsolverreport();
        }
        
        public override alglib.alglibobject make_copy()
        {
            return new minlpsolverreport((minlpsolvers.minlpsolverreport)_innerobj.make_copy());
        }
    
        //
        // Although some of declarations below are public, you should not use them
        // They are intended for internal use only
        //
        private minlpsolvers.minlpsolverreport _innerobj;
        public minlpsolvers.minlpsolverreport innerobj { get { return _innerobj; } }
        public minlpsolverreport(minlpsolvers.minlpsolverreport obj)
        {
            _innerobj = obj;
        }
    }
    
    /*************************************************************************
                    MIXED INTEGER NONLINEAR PROGRAMMING SOLVER

    DESCRIPTION:
    The  subroutine  minimizes a function  F(x)  of N arguments subject to any
    combination of:
    * box constraints
    * linear equality/inequality/range constraints CL<=Ax<=CU
    * nonlinear equality/inequality/range constraints HL<=Hi(x)<=HU
    * integrality constraints on some variables

    REQUIREMENTS:
    * F(), H() are continuously differentiable on the  feasible  set  and  its
      neighborhood
    * starting point X0, which can be infeasible

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
         Copyright 01.01.2025 by Bochkanov Sergey
    *************************************************************************/
    public static void minlpsolvercreate(int n, double[] x, out minlpsolverstate state)
    {
        state = new minlpsolverstate();
        minlpsolvers.minlpsolvercreate(n, x, state.innerobj, null);
    }
    
    public static void minlpsolvercreate(int n, double[] x, out minlpsolverstate state, alglib.xparams _params)
    {
        state = new minlpsolverstate();
        minlpsolvers.minlpsolvercreate(n, x, state.innerobj, _params);
    }
            
    public static void minlpsolvercreate(double[] x, out minlpsolverstate state)
    {
        int n;
    
        state = new minlpsolverstate();
        n = ap.len(x);
        minlpsolvers.minlpsolvercreate(n, x, state.innerobj, null);
    
        return;
    }
            
    public static void minlpsolvercreate(double[] x, out minlpsolverstate state, alglib.xparams _params)
    {
        int n;
    
        state = new minlpsolverstate();
        n = ap.len(x);
        minlpsolvers.minlpsolvercreate(n, x, state.innerobj, _params);
    
        return;
    }
    
    /*************************************************************************
    This function sets box constraints for the mixed integer optimizer.

    Box constraints are inactive by default.

    IMPORTANT: box constraints work in parallel with the integrality ones:
               * a variable marked as integral is considered  having no bounds
                 until minlpsolversetbc() is called
               * a  variable  with  lower  and  upper bounds set is considered
                 continuous   until    marked    as    integral    with    the
                 minlpsolversetintkth() function.

    INPUT PARAMETERS:
        State   -   structure stores algorithm state
        BndL    -   lower bounds, array[N].
                    If some (all) variables are unbounded, you may  specify  a
                    very small number or -INF, with the  latter  option  being
                    recommended.
        BndU    -   upper bounds, array[N].
                    If some (all) variables are unbounded, you may  specify  a
                    very large number or +INF, with the  latter  option  being
                    recommended.

    NOTE 1:  it is possible to specify  BndL[i]=BndU[i].  In  this  case  I-th
             variable will be "frozen" at X[i]=BndL[i]=BndU[i].

      -- ALGLIB --
         Copyright 01.01.2025 by Bochkanov Sergey
    *************************************************************************/
    public static void minlpsolversetbc(minlpsolverstate state, double[] bndl, double[] bndu)
    {
    
        minlpsolvers.minlpsolversetbc(state.innerobj, bndl, bndu, null);
    }
    
    public static void minlpsolversetbc(minlpsolverstate state, double[] bndl, double[] bndu, alglib.xparams _params)
    {
    
        minlpsolvers.minlpsolversetbc(state.innerobj, bndl, bndu, _params);
    }
    
    /*************************************************************************
    This function sets two-sided linear constraints AL <= A*x <= AU with dense
    constraint matrix A.

    INPUT PARAMETERS:
        State   -   structure previously allocated with minlpsolvercreate() call.
        A       -   linear constraints, array[K,N]. Each row of  A  represents
                    one  constraint. One-sided  inequality   constraints, two-
                    sided inequality  constraints,  equality  constraints  are
                    supported (see below)
        AL, AU  -   lower and upper bounds, array[K];
                    * AL[i]=AU[i] => equality constraint Ai*x
                    * AL[i]<AU[i] => two-sided constraint AL[i]<=Ai*x<=AU[i]
                    * AL[i]=-INF  => one-sided constraint Ai*x<=AU[i]
                    * AU[i]=+INF  => one-sided constraint AL[i]<=Ai*x
                    * AL[i]=-INF, AU[i]=+INF => constraint is ignored
        K       -   number of equality/inequality constraints,  K>=0;  if  not
                    given, inferred from sizes of A, AL, AU.

      -- ALGLIB --
         Copyright 15.04.2024 by Bochkanov Sergey
    *************************************************************************/
    public static void minlpsolversetlc2dense(minlpsolverstate state, double[,] a, double[] al, double[] au, int k)
    {
    
        minlpsolvers.minlpsolversetlc2dense(state.innerobj, a, al, au, k, null);
    }
    
    public static void minlpsolversetlc2dense(minlpsolverstate state, double[,] a, double[] al, double[] au, int k, alglib.xparams _params)
    {
    
        minlpsolvers.minlpsolversetlc2dense(state.innerobj, a, al, au, k, _params);
    }
            
    public static void minlpsolversetlc2dense(minlpsolverstate state, double[,] a, double[] al, double[] au)
    {
        int k;
        if( (ap.rows(a)!=ap.len(al)) || (ap.rows(a)!=ap.len(au)))
            throw new alglibexception("Error while calling 'minlpsolversetlc2dense': looks like one of arguments has wrong size");
    
        k = ap.rows(a);
        minlpsolvers.minlpsolversetlc2dense(state.innerobj, a, al, au, k, null);
    
        return;
    }
            
    public static void minlpsolversetlc2dense(minlpsolverstate state, double[,] a, double[] al, double[] au, alglib.xparams _params)
    {
        int k;
        if( (ap.rows(a)!=ap.len(al)) || (ap.rows(a)!=ap.len(au)))
            throw new alglibexception("Error while calling 'minlpsolversetlc2dense': looks like one of arguments has wrong size");
    
        k = ap.rows(a);
        minlpsolvers.minlpsolversetlc2dense(state.innerobj, a, al, au, k, _params);
    
        return;
    }
    
    /*************************************************************************
    This  function  sets  two-sided linear  constraints  AL <= A*x <= AU  with
    a sparse constraining matrix A. Recommended for large-scale problems.

    This  function  overwrites  linear  (non-box)  constraints set by previous
    calls (if such calls were made).

    INPUT PARAMETERS:
        State   -   structure previously allocated with minlpsolvercreate() call.
        A       -   sparse matrix with size [K,N] (exactly!).
                    Each row of A represents one general linear constraint.
                    A can be stored in any sparse storage format.
        AL, AU  -   lower and upper bounds, array[K];
                    * AL[i]=AU[i] => equality constraint Ai*x
                    * AL[i]<AU[i] => two-sided constraint AL[i]<=Ai*x<=AU[i]
                    * AL[i]=-INF  => one-sided constraint Ai*x<=AU[i]
                    * AU[i]=+INF  => one-sided constraint AL[i]<=Ai*x
                    * AL[i]=-INF, AU[i]=+INF => constraint is ignored
        K       -   number  of equality/inequality constraints, K>=0.  If  K=0
                    is specified, A, AL, AU are ignored.

      -- ALGLIB --
         Copyright 15.04.2024 by Bochkanov Sergey
    *************************************************************************/
    public static void minlpsolversetlc2(minlpsolverstate state, sparsematrix a, double[] al, double[] au, int k)
    {
    
        minlpsolvers.minlpsolversetlc2(state.innerobj, a.innerobj, al, au, k, null);
    }
    
    public static void minlpsolversetlc2(minlpsolverstate state, sparsematrix a, double[] al, double[] au, int k, alglib.xparams _params)
    {
    
        minlpsolvers.minlpsolversetlc2(state.innerobj, a.innerobj, al, au, k, _params);
    }
    
    /*************************************************************************
    This  function  sets  two-sided linear  constraints  AL <= A*x <= AU  with
    a mixed constraining matrix A including a sparse part (first SparseK rows)
    and a dense part (last DenseK rows). Recommended for large-scale problems.

    This  function  overwrites  linear  (non-box)  constraints set by previous
    calls (if such calls were made).

    This function may be useful if constraint matrix includes large number  of
    both types of rows - dense and sparse. If you have just a few sparse rows,
    you  may  represent  them  in  dense  format  without losing  performance.
    Similarly, if you have just a few dense rows, you may store them in sparse
    format with almost same performance.

    INPUT PARAMETERS:
        State   -   structure previously allocated with minlpsolvercreate() call.
        SparseA -   sparse matrix with size [K,N] (exactly!).
                    Each row of A represents one general linear constraint.
                    A can be stored in any sparse storage format.
        SparseK -   number of sparse constraints, SparseK>=0
        DenseA  -   linear constraints, array[K,N], set of dense constraints.
                    Each row of A represents one general linear constraint.
        DenseK  -   number of dense constraints, DenseK>=0
        AL, AU  -   lower and upper bounds, array[SparseK+DenseK], with former
                    SparseK elements corresponding to sparse constraints,  and
                    latter DenseK elements corresponding to dense constraints;
                    * AL[i]=AU[i] => equality constraint Ai*x
                    * AL[i]<AU[i] => two-sided constraint AL[i]<=Ai*x<=AU[i]
                    * AL[i]=-INF  => one-sided constraint Ai*x<=AU[i]
                    * AU[i]=+INF  => one-sided constraint AL[i]<=Ai*x
                    * AL[i]=-INF, AU[i]=+INF => constraint is ignored
        K       -   number  of equality/inequality constraints, K>=0.  If  K=0
                    is specified, A, AL, AU are ignored.

      -- ALGLIB --
         Copyright 15.04.2024 by Bochkanov Sergey
    *************************************************************************/
    public static void minlpsolversetlc2mixed(minlpsolverstate state, sparsematrix sparsea, int ksparse, double[,] densea, int kdense, double[] al, double[] au)
    {
    
        minlpsolvers.minlpsolversetlc2mixed(state.innerobj, sparsea.innerobj, ksparse, densea, kdense, al, au, null);
    }
    
    public static void minlpsolversetlc2mixed(minlpsolverstate state, sparsematrix sparsea, int ksparse, double[,] densea, int kdense, double[] al, double[] au, alglib.xparams _params)
    {
    
        minlpsolvers.minlpsolversetlc2mixed(state.innerobj, sparsea.innerobj, ksparse, densea, kdense, al, au, _params);
    }
    
    /*************************************************************************
    This function appends a two-sided linear constraint AL <= A*x <= AU to the
    matrix of dense constraints.

    INPUT PARAMETERS:
        State   -   structure previously allocated with minlpsolvercreate() call.
        A       -   linear constraint coefficient, array[N], right side is NOT
                    included.
        AL, AU  -   lower and upper bounds;
                    * AL=AU    => equality constraint Ai*x
                    * AL<AU    => two-sided constraint AL<=A*x<=AU
                    * AL=-INF  => one-sided constraint Ai*x<=AU
                    * AU=+INF  => one-sided constraint AL<=Ai*x
                    * AL=-INF, AU=+INF => constraint is ignored

      -- ALGLIB --
         Copyright 15.04.2024 by Bochkanov Sergey
    *************************************************************************/
    public static void minlpsolveraddlc2dense(minlpsolverstate state, double[] a, double al, double au)
    {
    
        minlpsolvers.minlpsolveraddlc2dense(state.innerobj, a, al, au, null);
    }
    
    public static void minlpsolveraddlc2dense(minlpsolverstate state, double[] a, double al, double au, alglib.xparams _params)
    {
    
        minlpsolvers.minlpsolveraddlc2dense(state.innerobj, a, al, au, _params);
    }
    
    /*************************************************************************
    This function appends two-sided linear constraint  AL <= A*x <= AU  to the
    list of currently present sparse constraints.

    Constraint is passed in compressed format: as list of non-zero entries  of
    coefficient vector A. Such approach is more efficient than  dense  storage
    for highly sparse constraint vectors.

    INPUT PARAMETERS:
        State   -   structure previously allocated with minlpsolvercreate() call.
        IdxA    -   array[NNZ], indexes of non-zero elements of A:
                    * can be unsorted
                    * can include duplicate indexes (corresponding entries  of
                      ValA[] will be summed)
        ValA    -   array[NNZ], values of non-zero elements of A
        NNZ     -   number of non-zero coefficients in A
        AL, AU  -   lower and upper bounds;
                    * AL=AU    => equality constraint A*x
                    * AL<AU    => two-sided constraint AL<=A*x<=AU
                    * AL=-INF  => one-sided constraint A*x<=AU
                    * AU=+INF  => one-sided constraint AL<=A*x
                    * AL=-INF, AU=+INF => constraint is ignored

      -- ALGLIB --
         Copyright 19.07.2018 by Bochkanov Sergey
    *************************************************************************/
    public static void minlpsolveraddlc2(minlpsolverstate state, int[] idxa, double[] vala, int nnz, double al, double au)
    {
    
        minlpsolvers.minlpsolveraddlc2(state.innerobj, idxa, vala, nnz, al, au, null);
    }
    
    public static void minlpsolveraddlc2(minlpsolverstate state, int[] idxa, double[] vala, int nnz, double al, double au, alglib.xparams _params)
    {
    
        minlpsolvers.minlpsolveraddlc2(state.innerobj, idxa, vala, nnz, al, au, _params);
    }
    
    /*************************************************************************
    This function appends two-sided linear constraint  AL <= A*x <= AU  to the
    list of currently present sparse constraints.

    Constraint vector A is  passed  as  a  dense  array  which  is  internally
    sparsified by this function.

    INPUT PARAMETERS:
        State   -   structure previously allocated with minlpsolvercreate() call.
        DA      -   array[N], constraint vector
        AL, AU  -   lower and upper bounds;
                    * AL=AU    => equality constraint A*x
                    * AL<AU    => two-sided constraint AL<=A*x<=AU
                    * AL=-INF  => one-sided constraint A*x<=AU
                    * AU=+INF  => one-sided constraint AL<=A*x
                    * AL=-INF, AU=+INF => constraint is ignored

      -- ALGLIB --
         Copyright 19.07.2018 by Bochkanov Sergey
    *************************************************************************/
    public static void minlpsolveraddlc2sparsefromdense(minlpsolverstate state, double[] da, double al, double au)
    {
    
        minlpsolvers.minlpsolveraddlc2sparsefromdense(state.innerobj, da, al, au, null);
    }
    
    public static void minlpsolveraddlc2sparsefromdense(minlpsolverstate state, double[] da, double al, double au, alglib.xparams _params)
    {
    
        minlpsolvers.minlpsolveraddlc2sparsefromdense(state.innerobj, da, al, au, _params);
    }
    
    /*************************************************************************
    This function sets two-sided nonlinear constraints for MINLP optimizer.

    In fact, this function sets  only  constraints  COUNT  and  their  BOUNDS.
    Constraints  themselves  (constraint  functions)   are   passed   to   the
    MINLPSolverOptimize() method as callbacks.

    MINLPSolverOptimize() method accepts a user-defined vector function F[] and its
    Jacobian J[], where:
    * first element of F[] and first row of J[] correspond to the target
    * subsequent NNLC components of F[] (and rows of J[]) correspond  to  two-
      sided nonlinear constraints NL<=C(x)<=NU, where
      * NL[i]=NU[i] => I-th row is an equality constraint Ci(x)=NL
      * NL[i]<NU[i] => I-th tow is a  two-sided constraint NL[i]<=Ci(x)<=NU[i]
      * NL[i]=-INF  => I-th row is an one-sided constraint Ci(x)<=NU[i]
      * NU[i]=+INF  => I-th row is an one-sided constraint NL[i]<=Ci(x)
      * NL[i]=-INF, NU[i]=+INF => constraint is ignored

    NOTE: you may combine nonlinear constraints with linear/boundary ones.  If
          your problem has mixed constraints, you  may explicitly specify some
          of them as linear or box ones.
          It helps optimizer to handle them more efficiently.

    INPUT PARAMETERS:
        State   -   structure previously allocated with MINLPSolverCreate call.
        NL      -   array[NNLC], lower bounds, can contain -INF
        NU      -   array[NNLC], lower bounds, can contain +INF
        NNLC    -   constraints count, NNLC>=0

    NOTE 1: nonlinear constraints are satisfied only  approximately!   It   is
            possible that the algorithm will evaluate the function  outside of
            the feasible area!

    NOTE 2: algorithm scales variables  according  to the scale  specified by
            MINLPSolverSetScale()  function,  so it can handle problems with badly
            scaled variables (as long as we KNOW their scales).

            However,  there  is  no  way  to  automatically  scale   nonlinear
            constraints. Inappropriate scaling  of nonlinear  constraints  may
            ruin convergence. Solving problem with  constraint  "1000*G0(x)=0"
            is NOT the same as solving it with constraint "0.001*G0(x)=0".

            It means that YOU are  the  one who is responsible for the correct
            scaling of the nonlinear constraints Gi(x) and Hi(x). We recommend
            you to scale nonlinear constraints in such a way that the Jacobian
            rows have approximately unit magnitude  (for  problems  with  unit
            scale) or have magnitude approximately equal to 1/S[i] (where S is
            a scale set by MINLPSolverSetScale() function).

      -- ALGLIB --
         Copyright 05.01.2025 by Bochkanov Sergey
    *************************************************************************/
    public static void minlpsolversetnlc2(minlpsolverstate state, double[] nl, double[] nu, int nnlc)
    {
    
        minlpsolvers.minlpsolversetnlc2(state.innerobj, nl, nu, nnlc, null);
    }
    
    public static void minlpsolversetnlc2(minlpsolverstate state, double[] nl, double[] nu, int nnlc, alglib.xparams _params)
    {
    
        minlpsolvers.minlpsolversetnlc2(state.innerobj, nl, nu, nnlc, _params);
    }
            
    public static void minlpsolversetnlc2(minlpsolverstate state, double[] nl, double[] nu)
    {
        int nnlc;
        if( (ap.len(nl)!=ap.len(nu)))
            throw new alglibexception("Error while calling 'minlpsolversetnlc2': looks like one of arguments has wrong size");
    
        nnlc = ap.len(nl);
        minlpsolvers.minlpsolversetnlc2(state.innerobj, nl, nu, nnlc, null);
    
        return;
    }
            
    public static void minlpsolversetnlc2(minlpsolverstate state, double[] nl, double[] nu, alglib.xparams _params)
    {
        int nnlc;
        if( (ap.len(nl)!=ap.len(nu)))
            throw new alglibexception("Error while calling 'minlpsolversetnlc2': looks like one of arguments has wrong size");
    
        nnlc = ap.len(nl);
        minlpsolvers.minlpsolversetnlc2(state.innerobj, nl, nu, nnlc, _params);
    
        return;
    }
    
    /*************************************************************************
    This function APPENDS a two-sided nonlinear constraint to the list.

    In fact, this function adds constraint bounds.  A  constraints  itself  (a
    function) is passed to the MINLPSolverOptimize() method as a callback. See
    comments on  MINLPSolverSetNLC2()  for  more  information  about  callback
    structure.

    The function adds a two-sided nonlinear constraint NL<=C(x)<=NU, where
    * NL=NU => I-th row is an equality constraint Ci(x)=NL
    * NL<NU => I-th tow is a  two-sided constraint NL<=Ci(x)<=NU
    * NL=-INF  => I-th row is an one-sided constraint Ci(x)<=NU
    * NU=+INF  => I-th row is an one-sided constraint NL<=Ci(x)
    * NL=-INF, NU=+INF => constraint is ignored

    NOTE: you may combine nonlinear constraints with linear/boundary ones.  If
          your problem has mixed constraints, you  may explicitly specify some
          of them as linear or box ones. It helps the optimizer to handle them
          more efficiently.

    INPUT PARAMETERS:
        State   -   structure previously allocated with MINLPSolverCreate call.
        NL      -   lower bound, can be -INF
        NU      -   upper bound, can be +INF

    NOTE 1: nonlinear constraints are satisfied only  approximately!   It   is
            possible that the algorithm will evaluate the function  outside of
            the feasible area!

    NOTE 2: algorithm scales variables  according  to the scale  specified by
            MINLPSolverSetScale()  function,  so it can handle problems with badly
            scaled variables (as long as we KNOW their scales).

            However,  there  is  no  way  to  automatically  scale   nonlinear
            constraints. Inappropriate scaling  of nonlinear  constraints  may
            ruin convergence. Solving problem with  constraint  "1000*G0(x)=0"
            is NOT the same as solving it with constraint "0.001*G0(x)=0".

            It means that YOU are  the  one who is responsible for the correct
            scaling of the nonlinear constraints Gi(x) and Hi(x). We recommend
            you to scale nonlinear constraints in such a way that the Jacobian
            rows have approximately unit magnitude  (for  problems  with  unit
            scale) or have magnitude approximately equal to 1/S[i] (where S is
            a scale set by MINLPSolverSetScale() function).

    NOTE 3: use addnlc2masked() in order to specify variable  mask.  Masks are
            essential  for  derivative-free  optimization because they provide
            important information about relevant and irrelevant variables.

      -- ALGLIB --
         Copyright 05.01.2025 by Bochkanov Sergey
    *************************************************************************/
    public static void minlpsolveraddnlc2(minlpsolverstate state, double nl, double nu)
    {
    
        minlpsolvers.minlpsolveraddnlc2(state.innerobj, nl, nu, null);
    }
    
    public static void minlpsolveraddnlc2(minlpsolverstate state, double nl, double nu, alglib.xparams _params)
    {
    
        minlpsolvers.minlpsolveraddnlc2(state.innerobj, nl, nu, _params);
    }
    
    /*************************************************************************
    This function APPENDS a two-sided nonlinear constraint to the  list,  with
    the  variable   mask  being  specified  as  a  compressed  index  array. A
    variable mask is a set of variables actually appearing in the constraint.

    ----- ABOUT VARIABLE MASKS -----------------------------------------------

    Variable masks provide crucial information  for  derivative-free  solvers,
    greatly accelerating surrogate model construction. This  applies  to  both
    continuous and integral variables, with results for binary variables being
    more pronounced.

    Up to 2x improvement in convergence speed has been observed for sufficiently
    sparse MINLP problems.

    NOTE: In order to unleash the full potential of variable  masking,  it  is
          important to provide masks for objective as well  as  all  nonlinear
          constraints.

          Even partial  information  matters,  i.e.  if you are 100% sure that
          your black-box  function  does  not  depend  on  some variables, but
          unsure about other ones, mark surely irrelevant variables, and  tell
          the solver that other ones may be relevant.

    NOTE: the solver is may behave unpredictably  if  some  relevant  variable
          is not included into the mask. Most likely it will fail to converge,
          although it sometimes possible to converge  to  solution  even  with
          incorrectly specified mask.

    NOTE: minlpsolversetobjectivemask() can be used to set  variable  mask for
          the objective.

    NOTE: Masks  are  ignored  by  branch-and-bound-type  solvers  relying  on
          analytic gradients.

    ----- ABOUT NONLINEAR CONSTRAINTS ----------------------------------------

    In fact, this function adds constraint bounds.  A  constraint   itself  (a
    function) is passed to the MINLPSolverOptimize() method as a callback. See
    comments on  MINLPSolverSetNLC2()  for  more  information  about  callback
    structure.

    The function adds a two-sided nonlinear constraint NL<=C(x)<=NU, where
    * NL=NU => I-th row is an equality constraint Ci(x)=NL
    * NL<NU => I-th tow is a  two-sided constraint NL<=Ci(x)<=NU
    * NL=-INF  => I-th row is an one-sided constraint Ci(x)<=NU
    * NU=+INF  => I-th row is an one-sided constraint NL<=Ci(x)
    * NL=-INF, NU=+INF => constraint is ignored

    NOTE: you may combine nonlinear constraints with linear/boundary ones.  If
          your problem has mixed constraints, you  may explicitly specify some
          of them as linear or box ones. It helps the optimizer to handle them
          more efficiently.

    INPUT PARAMETERS:
        State   -   structure previously allocated with MINLPSolverCreate call.
        NL      -   lower bound, can be -INF
        NU      -   upper bound, can be +INF
        VarIdx  -   array[NMSK], with potentially  unsorted  and  non-distinct
                    indexes (the function will sort and merge duplicates).  If
                    a variable index K appears in the list, it  means that the
                    constraint potentially depends  on  K-th  variable.  If  a
                    variable index K does NOT appear in  the  list,  it  means
                    that the constraint does NOT depend on K-th variable.
                    The array can have more than NMSK elements, in which  case
                    only leading NMSK will be used.
        NMSK    -   NMSK>=0, VarIdx[] size:
                    * NMSK>0 means that the constraint depends on up  to  NMSK
                      variables whose indexes are stored in VarIdx[]
                    * NMSK=0 means that the constraint is a constant function;
                      the solver may fail if it is not actually the case.

    NOTE 1: nonlinear constraints are satisfied only  approximately!   It   is
            possible that the algorithm will evaluate the function  outside of
            the feasible area!

    NOTE 2: algorithm scales variables  according  to the scale  specified by
            MINLPSolverSetScale()  function,  so it can handle problems with badly
            scaled variables (as long as we KNOW their scales).

            However,  there  is  no  way  to  automatically  scale   nonlinear
            constraints. Inappropriate scaling  of nonlinear  constraints  may
            ruin convergence. Solving problem with  constraint  "1000*G0(x)=0"
            is NOT the same as solving it with constraint "0.001*G0(x)=0".

            It means that YOU are  the  one who is responsible for the correct
            scaling of the nonlinear constraints Gi(x) and Hi(x). We recommend
            you to scale nonlinear constraints in such a way that the Jacobian
            rows have approximately unit magnitude  (for  problems  with  unit
            scale) or have magnitude approximately equal to 1/S[i] (where S is
            a scale set by MINLPSolverSetScale() function).

      -- ALGLIB --
         Copyright 05.01.2025 by Bochkanov Sergey
    *************************************************************************/
    public static void minlpsolveraddnlc2masked(minlpsolverstate state, double nl, double nu, int[] varidx, int nmsk)
    {
    
        minlpsolvers.minlpsolveraddnlc2masked(state.innerobj, nl, nu, varidx, nmsk, null);
    }
    
    public static void minlpsolveraddnlc2masked(minlpsolverstate state, double nl, double nu, int[] varidx, int nmsk, alglib.xparams _params)
    {
    
        minlpsolvers.minlpsolveraddnlc2masked(state.innerobj, nl, nu, varidx, nmsk, _params);
    }
    
    /*************************************************************************
    This function sets stopping condition for the branch-and-bound  family  of
    solvers: a solver must when when the gap between primal and dual bounds is
    less than PDGap.

    The solver computes relative gap, equal to |Fprim-Fdual|/max(|Fprim|,1).

    This parameter is ignored by other types of solvers, e.g. MIVNS.

    INPUT PARAMETERS:
        State   -   structure stores algorithm state
        PDGap   -   >=0, tolerance. Zero value means that some default value
                    is automatically selected.

      -- ALGLIB --
         Copyright 01.01.2025 by Bochkanov Sergey
    *************************************************************************/
    public static void minlpsolversetpdgap(minlpsolverstate state, double pdgap)
    {
    
        minlpsolvers.minlpsolversetpdgap(state.innerobj, pdgap, null);
    }
    
    public static void minlpsolversetpdgap(minlpsolverstate state, double pdgap, alglib.xparams _params)
    {
    
        minlpsolvers.minlpsolversetpdgap(state.innerobj, pdgap, _params);
    }
    
    /*************************************************************************
    This function sets tolerance for nonlinear constraints;  points  violating
    constraints by no more than CTol are considered feasible.

    Depending on the specific algorithm  used,  constraint  violation  may  be
    checked against  internally  scaled/normalized  constraints  (some  smooth
    solvers renormalize constraints in such a way that they have roughly  unit
    gradient magnitudes) or against raw constraint values:
    * BBSYNC renormalizes constraints prior to comparing them with CTol
    * MIRBF-VNS checks violation against raw constraint values

    IMPORTANT: one  should  be  careful  when choosing tolerances and stopping
               criteria.

               A solver stops  as  soon  as  stopping  criteria are triggered;
               a feasibility check is  performed  after  that.  If  too  loose
               stopping criteria are  used, the solver  may  fail  to  enforce
               constraints  with  sufficient  accuracy  and  fail to recognize
               solution as a feasible one.

               For example, stopping with EpsX=0.01 and checking CTol=0.000001
               will almost surely result in problems. Ideally, CTol should  be
               1-2 orders of magnitude more relaxed than stopping criteria.

    INPUT PARAMETERS:
        State   -   structure stores algorithm state
        CTol    -   >0, tolerance.

      -- ALGLIB --
         Copyright 01.01.2025 by Bochkanov Sergey
    *************************************************************************/
    public static void minlpsolversetctol(minlpsolverstate state, double ctol)
    {
    
        minlpsolvers.minlpsolversetctol(state.innerobj, ctol, null);
    }
    
    public static void minlpsolversetctol(minlpsolverstate state, double ctol, alglib.xparams _params)
    {
    
        minlpsolvers.minlpsolversetctol(state.innerobj, ctol, _params);
    }
    
    /*************************************************************************
    This  function  tells  MINLP solver  to  use  an  objective-based stopping
    condition for an underlying subsolver, i.e. to stop subsolver if  relative
    change in objective between iterations is less than EpsF.

    Too tight EspF, as always, result in spending too much time in the solver.
    Zero value means that some default non-zero value will be used.

    Exact action of this condition as well as reaction  to  too  relaxed  EpsF
    depend on specific MINLP solver being used

    * BBSYNC. This condition controls SQP subsolver used to solve NLP (relaxed)
      subproblems arising during B&B  tree  search. Good  values are typically
      between 1E-6 and 1E-7.

      Too relaxed values may result in subproblems being  mistakenly  fathomed
      (feasible solutions not identified), too  large  constraint  violations,
      etc.

    * MIVNS. This condition controls RBF-based surrogate model subsolver  used
      to handle continuous variables. It is ignored for integer-only problems.

      The subsolver stops if total objective change in last  several  (between
      5 and 10) steps is less than EpsF. More than one step is used  to  check
      convergence because surrogate  model-based  solvers  usually  need  more
      stringent stopping criteria than SQP.

      Good values are relatively high, between 0.01 and 0.0001,  depending  on
      a  problem.  The  MIVNS  solver  is  designed to gracefully handle large
      values of EpsF - it will stop early, but it won't compromise feasibility
      (it will try to reduce constraint violations below CTol)  and  will  not
      drop promising integral nodes.

    INPUT PARAMETERS:
        State   -   solver structure
        EpsF    -   >0, stopping condition

      -- ALGLIB --
         Copyright 01.01.2025 by Bochkanov Sergey
    *************************************************************************/
    public static void minlpsolversetsubsolverepsf(minlpsolverstate state, double epsf)
    {
    
        minlpsolvers.minlpsolversetsubsolverepsf(state.innerobj, epsf, null);
    }
    
    public static void minlpsolversetsubsolverepsf(minlpsolverstate state, double epsf, alglib.xparams _params)
    {
    
        minlpsolvers.minlpsolversetsubsolverepsf(state.innerobj, epsf, _params);
    }
    
    /*************************************************************************
    This  function  tells  MINLP solver to use a step-based stopping condition
    for an underlying subsolver, i.e. to stop subsolver  if  typical step size
    becomes less than EpsX.

    Too tight EspX, as always, result in spending too much time in the solver.
    Zero value means that some default non-zero value will be used.

    Exact action of this condition as well as reaction  to  too  relaxed  EpsX
    depend on specific MINLP solver being used

    * BBSYNC. This condition controls SQP subsolver used to solve NLP (relaxed)
      subproblems arising during B&B  tree  search. Good  values are typically
      between 1E-6 and 1E-7.

      Too relaxed values may result in subproblems being  mistakenly  fathomed
      (feasible solutions not identified), too  large  constraint  violations,
      etc.

    * MIVNS. This condition controls RBF-based surrogate model subsolver  used
      to handle continuous variables. It is ignored for integer-only problems.

      The subsolver stops if trust radius  for  a  surrogate  model  optimizer
      becomes less than EpsX.

      Good values are relatively high, between 0.01 and 0.0001,  depending  on
      a  problem.  The  MIVNS  solver  is  designed to gracefully handle large
      values of EpsX - it will stop early, but it won't compromise feasibility
      (it will try to reduce constraint violations below CTol)  and  will  not
      drop promising integral nodes.

    INPUT PARAMETERS:
        State   -   solver structure
        EpsX    -   >=0, stopping condition. Zero value means that some default
                    value will be used.

      -- ALGLIB --
         Copyright 01.01.2025 by Bochkanov Sergey
    *************************************************************************/
    public static void minlpsolversetsubsolverepsx(minlpsolverstate state, double epsx)
    {
    
        minlpsolvers.minlpsolversetsubsolverepsx(state.innerobj, epsx, null);
    }
    
    public static void minlpsolversetsubsolverepsx(minlpsolverstate state, double epsx, alglib.xparams _params)
    {
    
        minlpsolvers.minlpsolversetsubsolverepsx(state.innerobj, epsx, _params);
    }
    
    /*************************************************************************
    This function controls adaptive internal parallelism, i.e. algorithm  used
    by  the  solver  to  adaptively  decide  whether parallel acceleration  of
    solver's internal calculations (B&B  code,  SQP,  parallel linear algebra)
    should be actually used or not.

    This  function  tells  the  solver  to  favor  parallelism,  i.e.  utilize
    multithreading (when allowed by the  user)  until  statistics  prove  that
    overhead from starting/stopping worker threads is too large.

    This way solver gets the best performance  on  problems  with  significant
    amount  of  internal  calculations  (large  QP/MIQP  subproblems,  lengthy
    surrogate model optimization sessions) from the very beginning. The  price
    is that problems with small solver overhead that does not justify internal
    parallelism (<1ms per iteration) will suffer slowdown for several  initial
    10-20 milliseconds until the solver proves that parallelism makes no sense

    Use  MINLPSolver.CautiousInternalParallelism()  to  avoid slowing down the
    solver on easy problems.

    NOTE: the internal parallelism is distinct from the callback  parallelism.
          The former is the ability to utilize parallelism to speed-up solvers
          own internal calculations,  while  the  latter  is  the  ability  to
          perform several callback evaluations at once. Aside from performance
          considerations, the internal parallelism is entirely transparent  to
          the user. The callback parallelism requries  the  user  to  write  a
          thread-safe, reentrant callback.

    NOTE: in order to use internal parallelism, adaptive or not, the user must
          activate it by   specifying  alglib::parallel  in  flags  or  global
          threading settings. ALGLIB for C++ must be compiled in the  OS-aware
          mode.

      -- ALGLIB --
         Copyright 01.01.2025 by Bochkanov Sergey
    *************************************************************************/
    public static void minlpsolverfavorinternalparallelism(minlpsolverstate state)
    {
    
        minlpsolvers.minlpsolverfavorinternalparallelism(state.innerobj, null);
    }
    
    public static void minlpsolverfavorinternalparallelism(minlpsolverstate state, alglib.xparams _params)
    {
    
        minlpsolvers.minlpsolverfavorinternalparallelism(state.innerobj, _params);
    }
    
    /*************************************************************************
    This function controls adaptive internal parallelism, i.e. algorithm  used
    by  the  solver  to  adaptively  decide  whether parallel acceleration  of
    solver's internal calculations (B&B  code,  SQP,  parallel linear algebra)
    should be actually used or not.

    This function tells the solver  to  do calculations in the single-threaded
    mode until statistics  prove  that  iteration  cost  justified  activating
    multithreading.

    This way solver does not suffer slow-down on problems with small iteration
    overhead (<1ms per iteration), at the cost of spending  initial  10-20  ms
    in the single-threaded  mode  even  on  difficult  problems  that  justify
    parallelism usage.

    Use  MINLPSolver.FavorInternalParallelism() to use parallelism until it is
    proven to be useless.

    NOTE: the internal parallelism is distinct from the callback  parallelism.
          The former is the ability to utilize parallelism to speed-up solvers
          own internal calculations,  while  the  latter  is  the  ability  to
          perform several callback evaluations at once. Aside from performance
          considerations, the internal parallelism is entirely transparent  to
          the user. The callback parallelism requries  the  user  to  write  a
          thread-safe, reentrant callback.

    NOTE: in order to use internal parallelism, adaptive or not, the user must
          activate it by   specifying  alglib::parallel  in  flags  or  global
          threading settings. ALGLIB for C++ must be compiled in the  OS-aware
          mode.

      -- ALGLIB --
         Copyright 01.01.2025 by Bochkanov Sergey
    *************************************************************************/
    public static void minlpsolvercautiousinternalparallelism(minlpsolverstate state)
    {
    
        minlpsolvers.minlpsolvercautiousinternalparallelism(state.innerobj, null);
    }
    
    public static void minlpsolvercautiousinternalparallelism(minlpsolverstate state, alglib.xparams _params)
    {
    
        minlpsolvers.minlpsolvercautiousinternalparallelism(state.innerobj, _params);
    }
    
    /*************************************************************************
    This function controls adaptive internal parallelism, i.e. algorithm  used
    by  the  solver  to  adaptively  decide  whether parallel acceleration  of
    solver's internal calculations (B&B  code,  SQP,  parallel linear algebra)
    should be actually used or not.

    This function tells the solver to do calculations exactly as prescribed by
    the user: in the parallel mode when alglib::parallel flag  is  passed,  in
    the single-threaded mode otherwise. The solver  does  not  analyze  actual
    running times to decide whether parallelism is justified or not.

    NOTE: the internal parallelism is distinct from the callback  parallelism.
          The former is the ability to utilize parallelism to speed-up solvers
          own internal calculations,  while  the  latter  is  the  ability  to
          perform several callback evaluations at once. Aside from performance
          considerations, the internal parallelism is entirely transparent  to
          the user. The callback parallelism requries  the  user  to  write  a
          thread-safe, reentrant callback.

    NOTE: in order to use internal parallelism, adaptive or not, the user must
          activate it by   specifying  alglib::parallel  in  flags  or  global
          threading settings. ALGLIB for C++ must be compiled in the  OS-aware
          mode.

      -- ALGLIB --
         Copyright 01.01.2025 by Bochkanov Sergey
    *************************************************************************/
    public static void minlpsolvernoadaptiveinternalparallelism(minlpsolverstate state)
    {
    
        minlpsolvers.minlpsolvernoadaptiveinternalparallelism(state.innerobj, null);
    }
    
    public static void minlpsolvernoadaptiveinternalparallelism(minlpsolverstate state, alglib.xparams _params)
    {
    
        minlpsolvers.minlpsolvernoadaptiveinternalparallelism(state.innerobj, _params);
    }
    
    /*************************************************************************
    This function marks K-th variable as an integral one.

    Unless box constraints are set for the variable, it is unconstrained (i.e.
    can take positive or  negative  values).  By  default  all  variables  are
    continuous.

    IMPORTANT: box constraints work in parallel with the integrality ones:
               * a variable marked as integral is considered  having no bounds
                 until minlpsolversetbc() is called
               * a  variable  with  lower  and  upper bounds set is considered
                 continuous   until    marked    as    integral    with    the
                 minlpsolversetintkth() function.

    INPUT PARAMETERS:
        State   -   structure stores algorithm state
        K       -   0<=K<N, variable index

      -- ALGLIB --
         Copyright 01.01.2025 by Bochkanov Sergey
    *************************************************************************/
    public static void minlpsolversetintkth(minlpsolverstate state, int k)
    {
    
        minlpsolvers.minlpsolversetintkth(state.innerobj, k, null);
    }
    
    public static void minlpsolversetintkth(minlpsolverstate state, int k, alglib.xparams _params)
    {
    
        minlpsolvers.minlpsolversetintkth(state.innerobj, k, _params);
    }
    
    /*************************************************************************
    This function sets variable  mask for the objective.  A variable  mask  is
    a set of variables actually appearing in the objective.

    If you want  to  set  variable  mask  for  a  nonlinear  constraint,   use
    addnlc2masked() or addnlc2maskeddense() to add  a constraint together with
    a constraint-specific mask.

    Variable masks provide crucial information  for  derivative-free  solvers,
    greatly accelerating surrogate model construction. This  applies  to  both
    continuous and integral variables, with results for binary variables being
    more pronounced.

    Up to 2x improvement in convergence speed has been observed for sufficiently
    sparse MINLP problems.

    NOTE: In order to unleash the full potential of variable  masking,  it  is
          important to provide masks for objective as well  as  all  nonlinear
          constraints.

          Even partial  information  matters,  i.e.  if you are 100% sure that
          your black-box  function  does  not  depend  on  some variables, but
          unsure about other ones, mark surely irrelevant variables, and  tell
          the solver that other ones may be relevant.

    NOTE: the solver is may behave unpredictably  if  some  relevant  variable
          is not included into the mask. Most likely it will fail to converge,
          although it sometimes possible to converge  to  solution  even  with
          incorrectly specified mask.

    NOTE: Masks  are  ignored  by  branch-and-bound-type  solvers  relying  on
          analytic gradients.

    INPUT PARAMETERS:
        State   -   structure stores algorithm state
        ObjMask -   array[N],  I-th  element  is  False  if  I-th variable  is
                    irrelevant.

      -- ALGLIB --
         Copyright 01.01.2025 by Bochkanov Sergey
    *************************************************************************/
    public static void minlpsolversetobjectivemaskdense(minlpsolverstate state, bool[] objmask)
    {
    
        minlpsolvers.minlpsolversetobjectivemaskdense(state.innerobj, objmask, null);
    }
    
    public static void minlpsolversetobjectivemaskdense(minlpsolverstate state, bool[] objmask, alglib.xparams _params)
    {
    
        minlpsolvers.minlpsolversetobjectivemaskdense(state.innerobj, objmask, _params);
    }
    
    /*************************************************************************
    This function sets scaling coefficients for the mixed integer optimizer.

    ALGLIB optimizers use scaling matrices to test stopping  conditions  (step
    size and gradient are scaled before comparison  with  tolerances).  Scales
    are also used by the finite difference variant of the optimizer - the step
    along I-th axis is equal to DiffStep*S[I]. Finally,  variable  scales  are
    used for preconditioning (i.e. to speed up the solver).

    The scale of the I-th variable is a translation invariant measure of:
    a) "how large" the variable is
    b) how large the step should be to make significant changes in the function

    INPUT PARAMETERS:
        State   -   structure stores algorithm state
        S       -   array[N], non-zero scaling coefficients
                    S[i] may be negative, sign doesn't matter.

      -- ALGLIB --
         Copyright 06.01.2025 by Bochkanov Sergey
    *************************************************************************/
    public static void minlpsolversetscale(minlpsolverstate state, double[] s)
    {
    
        minlpsolvers.minlpsolversetscale(state.innerobj, s, null);
    }
    
    public static void minlpsolversetscale(minlpsolverstate state, double[] s, alglib.xparams _params)
    {
    
        minlpsolvers.minlpsolversetscale(state.innerobj, s, _params);
    }
    
    /*************************************************************************
    This function tell the solver to use BBSYNC (Branch&Bound with Synchronous
    processing) mixed-integer nonlinear programming algorithm.

    The BBSYNC algorithm is an NLP-based branch-and-bound method with integral
    and spatial splits, supporting both convex  and  nonconvex  problems.  The
    algorithm combines parallelism support with deterministic  behavior  (i.e.
    the same branching decisions are performed with every paralell run).

    Non-convex (multiextremal) problems can be solved with  multiple  restarts
    from random points, which are activated by minlpsolversetmultistarts()

    IMPORTANT: contrary to the popular  misconception,  MINLP  is  not  easily
               parallelizable. B&B trees often have  profiles  unsuitable  for
               parallel processing (too short and/or too linear).  Spatial  or
               integral splits adds some limited degree of parallelism (up  to
               2x in the very best case), but in practice it often results  in
               just a 1.5x speed-up at best  due  imbalanced  leaf  processing
               times.  Furthermore ,  determinism  is  always  at   odds  with
               efficiency.

               Achieving good parallel speed-up requires some amount of tuning
               and having a 2x-3x speed-up is already a good result.

               On the other hand, setups using multiple  random  restarts  are
               obviously highly parallelizable.

    INPUT PARAMETERS:
        State           -   structure that stores algorithm state

        GroupSize       -   >=1, group size. Up to GroupSize tree nodes can be
                            processed in the parallel manner.

                            Increasing  this   parameter   makes   the  solver
                            less efficient serially (it always tries  to  fill
                            the batch with nodes, even if there  is  a  chance
                            that most of them will be  discarded  later),  but
                            increases its parallel potential.

                            Parallel speed-up comes from two sources:
                            * callback parallelism (several  objective  values
                              are computed concurrently), which is significant
                              for problems with callbacks that take  more than
                              1ms per evaluation
                            * internal parallelism, i.e. ability to do parallel
                              sparse matrix factorization  and  other  solver-
                              related tasks
                            By  default,  the  solver  runs  serially even for
                            GroupSize>1. Both kinds of parallelism have to  be
                            activated by the user, see ALGLIB Reference Manual
                            for more information.

                            Recommended value, depending on callback cost  and
                            matrix factorization overhead, can be:
                            * 1 for 'easy' problems with cheap  callbacks  and
                              small dimensions; also for problems with  nearly
                              linear B&B trees.
                            * 2-3  for   problems   with  sufficiently  costly
                              callbacks (or sufficiently high  linear  algebra
                              overhead) that it makes sense to utilize limited
                              parallelism.
                            * cores count - for difficult problems  with  deep
                              and  wide   B&B trees  and  sufficiently  costly
                              callbacks (or sufficiently high  linear  algebra
                              overhead).

    NOTES: DETERMINISM

    Running with fixed GroupSize generally produces same results independently
    of whether parallelism is used or not. Changing  GroupSize  parameter  may
    change results in the following ways:

    * for problems that are solved to optimality  but have multiple solutions,
      different values of this parameter may  result  in  different  solutions
      being returned (but still with the same objective value)

    * while operating close to exhausting budget (either timeout or iterations
      limit), different GroupSize values may result in different  outcomes:  a
      solution being found, or budget being exhausted

    * finally, on difficult problems that are too hard to solve to  optimality
      but still allow finding primal feasible solutions changing GroupSize may
      result in different primal feasible solutions being returned.

      -- ALGLIB --
         Copyright 01.01.2025 by Bochkanov Sergey
    *************************************************************************/
    public static void minlpsolversetalgobbsync(minlpsolverstate state, int groupsize)
    {
    
        minlpsolvers.minlpsolversetalgobbsync(state.innerobj, groupsize, null);
    }
    
    public static void minlpsolversetalgobbsync(minlpsolverstate state, int groupsize, alglib.xparams _params)
    {
    
        minlpsolvers.minlpsolversetalgobbsync(state.innerobj, groupsize, _params);
    }
    
    /*************************************************************************
    This function  tell  the  solver  to  use  MIVNS  (Mixed-Integer  Variable
    Neighborhood Search) solver for  derivative-free  mixed-integer  nonlinear
    programming with expensive objective/constraints and non-relaxable integer
    variables.

    The solver is intended for moderately-sized problems, typically with  tens
    of variables.

    The algorithm has the following features:
    * it supports all-integer and mixed-integer problems with box, linear  and
      nonlinear equality and inequality  constraints
    * it makes no assumptions about problem convexity
    * it does not require derivative information. Although  it  still  assumes
      that objective/constraints are smooth wrt continuous variables, no  such
      assumptions are made regarding dependence on integer variables.
    * it efficiently uses limited computational budget and  scales  well  with
      larger budgets
    * it does not evaluate objective/constraints at points violating integrality
    * it also respects linear constraints in all intermediate points

    NOTE: In  particular,  if  your  task  uses integrality+sum-to-one set  of
          constraints to encode multiple choice options (e.g. [1,0,0], [0,1,0]
          or [0,0,1]), you can be sure that the algorithm will not ask for  an
          objective value at a point with fractional values like [0.1,0.5,0.4]
          or at one that is not a correct one-hot encoded value (e.g.  [1,1,0]
          which has two variables set to 1).

    The algorithm is intended for low-to-medium accuracy solution of otherwise
    intractable problems with expensive objective/constraints.

    It can solve any MINLP problem; however, it is optimized for the following
    problem classes:
    * limited variable count
    * expensive objective/constraints
    * nonrelaxable integer variables
    * no derivative information
    * problems where changes in integer variables lead to  structural  changes
      in the entire system. Speaking in other words, on  problems  where  each
      integer variable acts as an on/off or "choice"  switch  that  completely
      rewires the model - turning constraints, variables, or whole sub-systems
      on or off

    INPUT PARAMETERS:
        State           -   structure that stores algorithm state

        Budget          -   optimization  budget (function  evaluations).  The
                            solver will not stop  immediately  after  reaching
                            Budget evaluations, but  will  stop  shortly after
                            that (usually within 2N+1 evaluations). Zero value
                            means no limit.

        MaxNeighborhood -   stopping condition for the solver.  The  algorithm
                            will stop as soon as there are  no  points  better
                            than the current candidate in a neighborhood whose
                            size is equal to or exceeds MaxNeighborhood.  Zero
                            means no stopping condition.

                            Recommended neighborhood size is  proportional  to
                            the difference between integral variables count NI
                            and the number of linear equality  constraints  on
                            integral variables L (such constraints effectively
                            reduce problem dimensionality).

                            The very minimal value for binary problems is NI-L,
                            which means that the solution can not be  improved
                            by flipping one of variables between 0 and 1.  The
                            very minimal value for non-binary integral vars is
                            twice as much (because  now  each  point  has  two
                            neighbors per  variable).  However,  such  minimal
                            values often result in an early termination.

                            It is recommended to set this parameter to 5*N  or
                            10*N (ignoring LI) and to test how it  behaves  on
                            your problem.

        BatchSize           >=1,   recommended  batch  size  for  neighborhood
                            exploration.   Up   to  BatchSize  nodes  will  be
                            evaluated at any  moment,  thus  up  to  BatchSize
                            objective evaluations can be performed in parallel.

                            Increasing  this   parameter   makes   the  solver
                            slightly less efficient serially (it always  tries
                            to fill the batch with nodes, even if there  is  a
                            chance that most of them will be discarded later),
                            but greatly increases its parallel potential.

                            Recommended values depend on the cores  count  and
                            on the limitations  of  the  objective/constraints
                            callback:
                            * 1 for serial execution, callback that can not be
                              called  from   multiple   threads,   or   highly
                              parallelized  expensive  callback that keeps all
                              cores occupied
                            * small fixed value like 5  or  10,  if  you  need
                              reproducible behavior independent from the cores
                              count
                            * CORESCOUNT, 2*CORESCOUNT or some other  multiple
                              of CORESCOUNT, if you want to utilize parallelism
                              to the maximum extent

                            Parallel speed-up comes from two sources:
                            * callback parallelism (several  objective  values
                              are computed concurrently), which is significant
                              for problems with callbacks that take  more than
                              1ms per evaluation
                            * internal parallelism, i.e. ability to do parallel
                              sparse matrix factorization  and  other  solver-
                              related tasks
                            By  default,  the  solver  runs  serially even for
                            GroupSize>1. Both kinds of parallelism have to  be
                            activated by the user, see ALGLIB Reference Manual
                            for more information.

    NOTES: if no stopping criteria is specified (unlimited budget, no timeout,
           no  neighborhood  size  limit),  then  the  solver  will  run until
           enumerating all integer solutions.

    ===== ALGORITHM DESCRIPTION ==============================================

    A simplified description for an  all-integer  algorithm, omitting stopping
    criteria and various checks:

        MIVNS (ALL-INTEGER):
            1. Input: initial integral point, may be infeasible wrt  nonlinear
               constraints, but is feasible wrt linear  ones.  Enforce  linear
               feasibility, if needed.
            2. Generate initial neighborhood around the current point that  is
               equal to the point itself. The point is marked as explored.
            3. Scan  neighborhood  for  a  better  point  (one  that  is  less
               infeasible or has lower objective);  if  one  is found, make it
               current and goto #2
            4. Scan neighborhood for an unexplored point (one with no objective
               computed). If one if found, compute objective, mark the point as
               explored, goto #3
            5. If there are no unexplored or better points in the neighborhood,
               expand it: find a  point  that  was  not  used  for  expansion,
               compute up to 2N its nearest integral neighbors,  add  them  to
               the neighborhood and mark as unexplored. Goto #3.

        NOTE: A nearest integral neighbor is a nearest point that  differs  at
              least by +1 or -1 in one  of  integral  variables  and  that  is
              feasible with respect to box and  linear  constraints  (ignoring
              nonlinear ones). For problems  with  difficult  constraint  sets
              integral neighbors are found by solving MIQP subproblems.

    The algorithm above systematically scans neighborhood  of  a  point  until
    either better point is found, an entire integer grid is enumerated, or one
    of stopping conditions is met.

    A mixed-integer version of the algorithm is more complex:
    * it still sees optimization space as a set of integer  nodes,  each  node
      having a subspace of continuous variables associated with it
    * after starting to explore a node, the algorithm runs an  RBF  surrogate-
      based subsolver for the node. It manages a dedicated subsolver for  each
      node in a neighborhood and adaptively divides its  computational  budget
      between subsolvers, switching to a node as soon as its  subsolver  shows
      better results than its competitors.
    * the algorithm remembers all previously evaluated points and reuses  them
      as much as possible

    ===== ALGORITHM SCALING WITH VARIABLES COUNT N ===========================

    A 'neighborhood scan' is a minimum number of function evaluations   needed
    to perform at least minimal evaluation of the immediate  neighborhood. For
    an N-dimensional problem with NI  integer variables and NF continuous ones
    we have ~NI nodes in an immediate neighborhood, and each  node  needs  ~NF
    evalutations to build at least linear model of the objective.

    Thus, a MIVNS neighborhood scan will need  about NI*NF=NI*(N-NI)=NF*(N-NF)
    objective evaluations.

    It is important to note that MIVNS  does  not  share  information  between
    nodes because it assumes that objective landscape can  drastically  change
    when jumping from node to node. That's why we need  NI*NF instead of NI+NF
    objective values.

    In practice, when started not too far away from the minimum, we can expect
    to get some improvement in 5-10 scans, and to get significant progress  in
    50-100 scans.

    For problems with NF being small or NI  being  small  we  have  scan  cost
    being proportional to variables count N, which allows us to  achieve  good
    progress using between 5N and 100N objective values.  However,  when  both
    NI and NF are close to N/2,  a  scan  needs  ~N^2  objective  evaluations,
    which results in a much worse scaling behavior.

      -- ALGLIB --
         Copyright 01.01.2025 by Bochkanov Sergey
    *************************************************************************/
    public static void minlpsolversetalgomivns(minlpsolverstate state, int budget, int maxneighborhood, int batchsize)
    {
    
        minlpsolvers.minlpsolversetalgomivns(state.innerobj, budget, maxneighborhood, batchsize, null);
    }
    
    public static void minlpsolversetalgomivns(minlpsolverstate state, int budget, int maxneighborhood, int batchsize, alglib.xparams _params)
    {
    
        minlpsolvers.minlpsolversetalgomivns(state.innerobj, budget, maxneighborhood, batchsize, _params);
    }
    
    /*************************************************************************
    This function activates multiple random restarts (performed for each node,
    including root and child ones) that help to find global solutions to  non-
    convex problems.

    This parameter is used  by  branch-and-bound  solvers  and  is  presently
    ignored by derivative-free solvers.

    INPUT PARAMETERS:
        State           -   structure that stores algorithm state
        NMultistarts    -   >=1, number of random restarts:
                            * 1 means that no restarts performed, the solver
                              assumes convexity
                            * >=1 means that solver assumes non-convexity and
                              performs fixed amount of random restarts

      -- ALGLIB --
         Copyright 01.01.2025 by Bochkanov Sergey
    *************************************************************************/
    public static void minlpsolversetmultistarts(minlpsolverstate state, int nmultistarts)
    {
    
        minlpsolvers.minlpsolversetmultistarts(state.innerobj, nmultistarts, null);
    }
    
    public static void minlpsolversetmultistarts(minlpsolverstate state, int nmultistarts, alglib.xparams _params)
    {
    
        minlpsolvers.minlpsolversetmultistarts(state.innerobj, nmultistarts, _params);
    }
    
    /*************************************************************************
    This function activates timeout feature. The solver finishes after running
    for a specified amount of time (in seconds, fractions can  be  used)  with
    the best point so far.

    Depending on the situation, the following completion codes can be reported
    in rep.terminationtype:
    * -33 (failure), if timed out without finding a feasible point
    * 5 (partial success), if timed out after finding at least one feasible point

    The solver does not stop immediately after timeout was  triggered  because
    it needs some time for underlying subsolvers to react to  timeout  signal.
    Generally, about one additional subsolver iteration (which is usually  far
    less than one B&B split) will be performed prior to stopping.

    INPUT PARAMETERS:
        State           -   structure that stores algorithm state
        Timeout         -   >=0, timeout in seconds (floating point number):
                            * 0 means no timeout
                            * >=0 means stopping after specified number of
                              seconds.

      -- ALGLIB --
         Copyright 01.01.2025 by Bochkanov Sergey
    *************************************************************************/
    public static void minlpsolversettimeout(minlpsolverstate state, double timeout)
    {
    
        minlpsolvers.minlpsolversettimeout(state.innerobj, timeout, null);
    }
    
    public static void minlpsolversettimeout(minlpsolverstate state, double timeout, alglib.xparams _params)
    {
    
        minlpsolvers.minlpsolversettimeout(state.innerobj, timeout, _params);
    }
    
    /*************************************************************************
    This function provides reverse communication interface
    Reverse communication interface is not documented or recommended to use.
    See below for functions which provide better documented API
    *************************************************************************/
    public static bool minlpsolveriteration(minlpsolverstate state)
    {
    
        return minlpsolvers.minlpsolveriteration(state.innerobj, null);
    }
    
    public static bool minlpsolveriteration(minlpsolverstate state, alglib.xparams _params)
    {
    
        return minlpsolvers.minlpsolveriteration(state.innerobj, _params);
    }
    /*************************************************************************
    This family of functions is used to start iterations of nonlinear optimizer

    These functions accept following parameters:
        fvec    -   callback which calculates function vector fi[]
                    at given point x
        jac     -   callback which calculates function vector fi[]
                    and Jacobian jac at given point x
        sjac    -   callback which calculates function vector fi[]
                    and sparse Jacobian sjac at given point x
        rep     -   optional callback which is called after each iteration
                    can be null
        obj     -   optional object which is passed to func/grad/hess/jac/rep
                    can be null



      -- ALGLIB --
         Copyright 01.01.2025 by Bochkanov Sergey

    *************************************************************************/
    public static void minlpsolveroptimize(minlpsolverstate state, ndimensional_fvec  fvec, ndimensional_rep rep, object obj)
    {
        minlpsolveroptimize(state, fvec, rep, obj, null);
    }
    public static void minlpsolveroptimize(minlpsolverstate state, ndimensional_fvec  fvec, ndimensional_rep rep, object obj, alglib.xparams _params)
    {
        if( fvec==null )
            throw new alglibexception("ALGLIB: error in 'minlpsolveroptimize()' (fvec is null)");
        alglib.ap.rcommv2_callbacks callbacks = new alglib.ap.rcommv2_callbacks();
        callbacks.fvec = fvec;
    
        alglib.minlpsolvers.minlpsolversetprotocolv2(state.innerobj, _params);
        while( alglib.minlpsolveriteration(state, _params) )
        {
            alglib.ap.rcommv2_request request = new alglib.ap.rcommv2_request(
                state.innerobj.requesttype,
                state.innerobj.querysize, state.innerobj.queryfuncs, state.innerobj.queryvars, state.innerobj.querydim, state.innerobj.queryformulasize,
                state.innerobj.querydata, state.innerobj.replyfi, state.innerobj.replydj, state.innerobj.replysj, obj, "minlpsolver");
            alglib.ap.rcommv2_buffers buffers = new alglib.ap.rcommv2_buffers(
                state.innerobj.tmpx1,
                state.innerobj.tmpc1,
                state.innerobj.tmpf1,
                state.innerobj.tmpg1,
                state.innerobj.tmpj1,
                state.innerobj.tmps1);
            if( state.innerobj.requesttype==3 )
            { 
                int njobs = request.size*request.vars+request.size;
                for(int job_idx=0; job_idx<njobs; job_idx++)
                    alglib.ap.process_v2request_3phase0(request, job_idx, callbacks, buffers);
                alglib.ap.process_v2request_3phase1(request);
                request.request = 0;
                continue;
            }
            if( state.innerobj.requesttype==5 )
            { 
                int njobs = request.size*request.vars+request.size;
                for(int job_idx=0; job_idx<njobs; job_idx++)
                    alglib.ap.process_v2request_5phase0(request, job_idx, callbacks, buffers);
                alglib.ap.process_v2request_5phase1(request);
                request.request = 0;
                continue;
            }if( state.innerobj.requesttype==4 )
            { 
                for(int qidx=0; qidx<state.innerobj.querysize; qidx++)
                    alglib.ap.process_v2request_4(request, qidx, callbacks, buffers);
                state.innerobj.requesttype = 0;
                continue;
            }
            if( state.innerobj.requesttype==-1 )
            {
                if( rep!=null )
                    rep(state.innerobj.reportx, state.innerobj.reportf, obj);
                continue;
            }
            throw new alglibexception("ALGLIB: error in 'minlpsolveroptimize' (some derivatives were not provided?)");
        }
    }


    public static void minlpsolveroptimize(minlpsolverstate state, ndimensional_jac  jac, ndimensional_rep rep, object obj)
    {
        minlpsolveroptimize(state, jac, rep, obj, null);
    }
    public static void minlpsolveroptimize(minlpsolverstate state, ndimensional_jac  jac, ndimensional_rep rep, object obj, alglib.xparams _params)
    {
        if( jac==null )
            throw new alglibexception("ALGLIB: error in 'minlpsolveroptimize()' (jac is null)");
        alglib.ap.rcommv2_callbacks callbacks = new alglib.ap.rcommv2_callbacks();
        callbacks.jac = jac;
    
        alglib.minlpsolvers.minlpsolversetprotocolv2(state.innerobj, _params);
        while( alglib.minlpsolveriteration(state, _params) )
        {
            alglib.ap.rcommv2_request request = new alglib.ap.rcommv2_request(
                state.innerobj.requesttype,
                state.innerobj.querysize, state.innerobj.queryfuncs, state.innerobj.queryvars, state.innerobj.querydim, state.innerobj.queryformulasize,
                state.innerobj.querydata, state.innerobj.replyfi, state.innerobj.replydj, state.innerobj.replysj, obj, "minlpsolver");
            alglib.ap.rcommv2_buffers buffers = new alglib.ap.rcommv2_buffers(
                state.innerobj.tmpx1,
                state.innerobj.tmpc1,
                state.innerobj.tmpf1,
                state.innerobj.tmpg1,
                state.innerobj.tmpj1,
                state.innerobj.tmps1);
            if( state.innerobj.requesttype==2 )
            { 
                for(int qidx=0; qidx<state.innerobj.querysize; qidx++)
                    alglib.ap.process_v2request_2(request, qidx, callbacks, buffers);
                state.innerobj.requesttype = 0;
                continue;
            }
            if( state.innerobj.requesttype==-1 )
            {
                if( rep!=null )
                    rep(state.innerobj.reportx, state.innerobj.reportf, obj);
                continue;
            }
            throw new alglibexception("ALGLIB: error in 'minlpsolveroptimize' (some derivatives were not provided?)");
        }
    }


    public static void minlpsolveroptimize(minlpsolverstate state, ndimensional_sjac  sjac, ndimensional_rep rep, object obj)
    {
        minlpsolveroptimize(state, sjac, rep, obj, null);
    }
    public static void minlpsolveroptimize(minlpsolverstate state, ndimensional_sjac  sjac, ndimensional_rep rep, object obj, alglib.xparams _params)
    {
        if( sjac==null )
            throw new alglibexception("ALGLIB: error in 'minlpsolveroptimize()' (sjac is null)");
        alglib.ap.rcommv2_callbacks callbacks = new alglib.ap.rcommv2_callbacks();
        callbacks.sjac = sjac;
    
        alglib.minlpsolvers.minlpsolversetprotocolv2s(state.innerobj, _params);
        while( alglib.minlpsolveriteration(state, _params) )
        {
            alglib.ap.rcommv2_request request = new alglib.ap.rcommv2_request(
                state.innerobj.requesttype,
                state.innerobj.querysize, state.innerobj.queryfuncs, state.innerobj.queryvars, state.innerobj.querydim, state.innerobj.queryformulasize,
                state.innerobj.querydata, state.innerobj.replyfi, state.innerobj.replydj, state.innerobj.replysj, obj, "minlpsolver");
            alglib.ap.rcommv2_buffers buffers = new alglib.ap.rcommv2_buffers(
                state.innerobj.tmpx1,
                state.innerobj.tmpc1,
                state.innerobj.tmpf1,
                state.innerobj.tmpg1,
                state.innerobj.tmpj1,
                state.innerobj.tmps1);
            if( state.innerobj.requesttype==1 )
            { 
                
                alglib.sparsecreatecrsemptybuf(request.vars, request.reply_sj, alglib.xdefault);
                for(int qidx=0; qidx<state.innerobj.querysize; qidx++)
                    alglib.ap.process_v2request_1(request, qidx, callbacks, buffers, request.reply_sj);
                state.innerobj.requesttype = 0;
                continue;
            }
            if( state.innerobj.requesttype==-1 )
            {
                if( rep!=null )
                    rep(state.innerobj.reportx, state.innerobj.reportf, obj);
                continue;
            }
            throw new alglibexception("ALGLIB: error in 'minlpsolveroptimize' (some derivatives were not provided?)");
        }
    }


    
    /*************************************************************************
    This subroutine  restarts  algorithm  from  new  point.  All  optimization
    parameters (including constraints) are left unchanged.

    This  function  allows  to  solve multiple  optimization  problems  (which
    must have  same number of dimensions) without object reallocation penalty.

    INPUT PARAMETERS:
        State   -   structure previously allocated with MINLPSolverCreate call.
        X       -   new starting point.

      -- ALGLIB --
         Copyright 28.11.2010 by Bochkanov Sergey
    *************************************************************************/
    public static void minlpsolverrestartfrom(minlpsolverstate state, double[] x)
    {
    
        minlpsolvers.minlpsolverrestartfrom(state.innerobj, x, null);
    }
    
    public static void minlpsolverrestartfrom(minlpsolverstate state, double[] x, alglib.xparams _params)
    {
    
        minlpsolvers.minlpsolverrestartfrom(state.innerobj, x, _params);
    }
    
    /*************************************************************************
    MINLPSolver results:  the  solution  found,  completion  codes  and  additional
    information.

    INPUT PARAMETERS:
        Solver  -   solver

    OUTPUT PARAMETERS:
        X       -   array[N], solution
        Rep     -   optimization report, contains information about completion
                    code, constraint violation at the solution and so on.

                    rep.f contains objective value at the solution.

                    You   should   check   rep.terminationtype  in  order   to
                    distinguish successful termination from unsuccessful one.

                    More information about fields of this  structure  can  be
                    found in the comments on the minlpsolverreport datatype.

      -- ALGLIB --
         Copyright 18.01.2024 by Bochkanov Sergey
    *************************************************************************/
    public static void minlpsolverresults(minlpsolverstate state, out double[] x, out minlpsolverreport rep)
    {
        x = new double[0];
        rep = new minlpsolverreport();
        minlpsolvers.minlpsolverresults(state.innerobj, ref x, rep.innerobj, null);
    }
    
    public static void minlpsolverresults(minlpsolverstate state, out double[] x, out minlpsolverreport rep, alglib.xparams _params)
    {
        x = new double[0];
        rep = new minlpsolverreport();
        minlpsolvers.minlpsolverresults(state.innerobj, ref x, rep.innerobj, _params);
    }
    
    /*************************************************************************
    NLC results

    Buffered implementation of MINLPSolverResults() which uses pre-allocated buffer
    to store X[]. If buffer size is  too  small,  it  resizes  buffer.  It  is
    intended to be used in the inner cycles of performance critical algorithms
    where array reallocation penalty is too large to be ignored.

      -- ALGLIB --
         Copyright 28.11.2010 by Bochkanov Sergey
    *************************************************************************/
    public static void minlpsolverresultsbuf(minlpsolverstate state, ref double[] x, minlpsolverreport rep)
    {
    
        minlpsolvers.minlpsolverresultsbuf(state.innerobj, ref x, rep.innerobj, null);
    }
    
    public static void minlpsolverresultsbuf(minlpsolverstate state, ref double[] x, minlpsolverreport rep, alglib.xparams _params)
    {
    
        minlpsolvers.minlpsolverresultsbuf(state.innerobj, ref x, rep.innerobj, _params);
    }

}
public partial class alglib
{
    public class bbgd
    {
        /*************************************************************************
        Subproblem formulation for the solver
        *************************************************************************/
        public class bbgdsubproblem : apobject
        {
            public int leafid;
            public int n;
            public double[] x0;
            public double[] bndl;
            public double[] bndu;
            public int leafidx;
            public double parentfdual;
            public int ncuttingplanes;
            public bool hasprimalsolution;
            public double[] xprim;
            public double fprim;
            public double hprim;
            public bool hasdualsolution;
            public double[] bestxdual;
            public double bestfdual;
            public double besthdual;
            public double[] worstxdual;
            public double worstfdual;
            public double worsthdual;
            public bool bestdualisintfeas;
            public double dualbound;
            public bbgdsubproblem()
            {
                init();
            }
            public override void init()
            {
                x0 = new double[0];
                bndl = new double[0];
                bndu = new double[0];
                xprim = new double[0];
                bestxdual = new double[0];
                worstxdual = new double[0];
            }
            public override alglib.apobject make_copy()
            {
                bbgdsubproblem _result = new bbgdsubproblem();
                _result.leafid = leafid;
                _result.n = n;
                _result.x0 = (double[])x0.Clone();
                _result.bndl = (double[])bndl.Clone();
                _result.bndu = (double[])bndu.Clone();
                _result.leafidx = leafidx;
                _result.parentfdual = parentfdual;
                _result.ncuttingplanes = ncuttingplanes;
                _result.hasprimalsolution = hasprimalsolution;
                _result.xprim = (double[])xprim.Clone();
                _result.fprim = fprim;
                _result.hprim = hprim;
                _result.hasdualsolution = hasdualsolution;
                _result.bestxdual = (double[])bestxdual.Clone();
                _result.bestfdual = bestfdual;
                _result.besthdual = besthdual;
                _result.worstxdual = (double[])worstxdual.Clone();
                _result.worstfdual = worstfdual;
                _result.worsthdual = worsthdual;
                _result.bestdualisintfeas = bestdualisintfeas;
                _result.dualbound = dualbound;
                return _result;
            }
        };


        /*************************************************************************
        Subsolver, one of potentially many started by a front entry
        *************************************************************************/
        public class bbgdfrontsubsolver : apobject
        {
            public int subsolverstatus;
            public bbgdsubproblem subproblem;
            public minnlc.minnlcstate nlpsubsolver;
            public ipm2solver.ipm2state qpsubsolver;
            public minnlc.minnlcreport nlprep;
            public rcommstate rstate;
            public double[] xsol;
            public double[] tmp0;
            public double[] tmp1;
            public double[] tmp2;
            public double[] tmp3;
            public int[] tmpi;
            public double[] wrks;
            public double[] wrkbndl;
            public double[] wrkbndu;
            public double[] wrkb;
            public int[] psvpackxyperm;
            public int[] psvunpackxyperm;
            public double[] psvs;
            public double[] psvxorigin;
            public double[] psvbndl;
            public double[] psvbndu;
            public double[] psvb;
            public double[] psvfixvals;
            public double[] psvrawbndl;
            public double[] psvrawbndu;
            public int npsv;
            public sparse.sparsematrix psva;
            public sparse.sparsematrix psvsparsec;
            public double[] psvcl;
            public double[] psvcu;
            public int psvlccnt;
            public int[] psvqpordering;
            public bbgdfrontsubsolver()
            {
                init();
            }
            public override void init()
            {
                subproblem = new bbgdsubproblem();
                nlpsubsolver = new minnlc.minnlcstate();
                qpsubsolver = new ipm2solver.ipm2state();
                nlprep = new minnlc.minnlcreport();
                rstate = new rcommstate();
                xsol = new double[0];
                tmp0 = new double[0];
                tmp1 = new double[0];
                tmp2 = new double[0];
                tmp3 = new double[0];
                tmpi = new int[0];
                wrks = new double[0];
                wrkbndl = new double[0];
                wrkbndu = new double[0];
                wrkb = new double[0];
                psvpackxyperm = new int[0];
                psvunpackxyperm = new int[0];
                psvs = new double[0];
                psvxorigin = new double[0];
                psvbndl = new double[0];
                psvbndu = new double[0];
                psvb = new double[0];
                psvfixvals = new double[0];
                psvrawbndl = new double[0];
                psvrawbndu = new double[0];
                psva = new sparse.sparsematrix();
                psvsparsec = new sparse.sparsematrix();
                psvcl = new double[0];
                psvcu = new double[0];
                psvqpordering = new int[0];
            }
            public override alglib.apobject make_copy()
            {
                bbgdfrontsubsolver _result = new bbgdfrontsubsolver();
                _result.subsolverstatus = subsolverstatus;
                _result.subproblem = subproblem!=null ? (bbgdsubproblem)subproblem.make_copy() : null;
                _result.nlpsubsolver = nlpsubsolver!=null ? (minnlc.minnlcstate)nlpsubsolver.make_copy() : null;
                _result.qpsubsolver = qpsubsolver!=null ? (ipm2solver.ipm2state)qpsubsolver.make_copy() : null;
                _result.nlprep = nlprep!=null ? (minnlc.minnlcreport)nlprep.make_copy() : null;
                _result.rstate = rstate!=null ? (rcommstate)rstate.make_copy() : null;
                _result.xsol = (double[])xsol.Clone();
                _result.tmp0 = (double[])tmp0.Clone();
                _result.tmp1 = (double[])tmp1.Clone();
                _result.tmp2 = (double[])tmp2.Clone();
                _result.tmp3 = (double[])tmp3.Clone();
                _result.tmpi = (int[])tmpi.Clone();
                _result.wrks = (double[])wrks.Clone();
                _result.wrkbndl = (double[])wrkbndl.Clone();
                _result.wrkbndu = (double[])wrkbndu.Clone();
                _result.wrkb = (double[])wrkb.Clone();
                _result.psvpackxyperm = (int[])psvpackxyperm.Clone();
                _result.psvunpackxyperm = (int[])psvunpackxyperm.Clone();
                _result.psvs = (double[])psvs.Clone();
                _result.psvxorigin = (double[])psvxorigin.Clone();
                _result.psvbndl = (double[])psvbndl.Clone();
                _result.psvbndu = (double[])psvbndu.Clone();
                _result.psvb = (double[])psvb.Clone();
                _result.psvfixvals = (double[])psvfixvals.Clone();
                _result.psvrawbndl = (double[])psvrawbndl.Clone();
                _result.psvrawbndu = (double[])psvrawbndu.Clone();
                _result.npsv = npsv;
                _result.psva = psva!=null ? (sparse.sparsematrix)psva.make_copy() : null;
                _result.psvsparsec = psvsparsec!=null ? (sparse.sparsematrix)psvsparsec.make_copy() : null;
                _result.psvcl = (double[])psvcl.Clone();
                _result.psvcu = (double[])psvcu.Clone();
                _result.psvlccnt = psvlccnt;
                _result.psvqpordering = (int[])psvqpordering.Clone();
                return _result;
            }
        };


        /*************************************************************************
        Front entry: subproblems (two leafs) + subsolvers

        EntryStatus can be one of the following:
        * stReadyToRun  there are subproblems to process (either in the spQueue array
          or already instantiated as a part of subsolver), but no subsolver is waiting
          for RComm reply
        * stWaitingForRComm if there is at least one subsolver waiting for RComm
        * stSolved if all subproblems were solved
        * stTimeout if timeout was signalled, or if similar stopping condition was
          fired (soft or hard max nodes)
        *************************************************************************/
        public class bbgdfrontentry : apobject
        {
            public int entrystatus;
            public int entrylock;
            public bool isroot;
            public int maxsubsolvers;
            public bool hastimeout;
            public int timeout;
            public apserv.stimer timerlocal;
            public bbgdsubproblem parentsubproblem;
            public bbgdsubproblem bestsubproblem0;
            public bbgdsubproblem bestsubproblem1;
            public rcommstate rstate;
            public ap.objarray subsolvers;
            public bbgdfrontsubsolver commonsubsolver;
            public ap.objarray spqueue;
            public int branchvar;
            public double branchval;
            public bbgdfrontentry()
            {
                init();
            }
            public override void init()
            {
                timerlocal = new apserv.stimer();
                parentsubproblem = new bbgdsubproblem();
                bestsubproblem0 = new bbgdsubproblem();
                bestsubproblem1 = new bbgdsubproblem();
                rstate = new rcommstate();
                subsolvers = new ap.objarray();
                commonsubsolver = new bbgdfrontsubsolver();
                spqueue = new ap.objarray();
            }
            public override alglib.apobject make_copy()
            {
                bbgdfrontentry _result = new bbgdfrontentry();
                _result.entrystatus = entrystatus;
                _result.entrylock = entrylock;
                _result.isroot = isroot;
                _result.maxsubsolvers = maxsubsolvers;
                _result.hastimeout = hastimeout;
                _result.timeout = timeout;
                _result.timerlocal = timerlocal!=null ? (apserv.stimer)timerlocal.make_copy() : null;
                _result.parentsubproblem = parentsubproblem!=null ? (bbgdsubproblem)parentsubproblem.make_copy() : null;
                _result.bestsubproblem0 = bestsubproblem0!=null ? (bbgdsubproblem)bestsubproblem0.make_copy() : null;
                _result.bestsubproblem1 = bestsubproblem1!=null ? (bbgdsubproblem)bestsubproblem1.make_copy() : null;
                _result.rstate = rstate!=null ? (rcommstate)rstate.make_copy() : null;
                _result.subsolvers = subsolvers!=null ? (ap.objarray)subsolvers.make_copy() : null;
                _result.commonsubsolver = commonsubsolver!=null ? (bbgdfrontsubsolver)commonsubsolver.make_copy() : null;
                _result.spqueue = spqueue!=null ? (ap.objarray)spqueue.make_copy() : null;
                _result.branchvar = branchvar;
                _result.branchval = branchval;
                return _result;
            }
        };


        /*************************************************************************
        Front - a set of simultaneously solved subproblems
        *************************************************************************/
        public class bbgdfront : apobject
        {
            public int frontmode;
            public int frontstatus;
            public bool popmostrecent;
            public int backtrackbudget;
            public int frontsize;
            public ap.objarray entries;
            public alglib.smp.shared_pool entrypool;
            public int flag;
            public rcommstate rstate;
            public int[] jobs;
            public bbgdfront()
            {
                init();
            }
            public override void init()
            {
                entries = new ap.objarray();
                entrypool = new alglib.smp.shared_pool();
                rstate = new rcommstate();
                jobs = new int[0];
            }
            public override alglib.apobject make_copy()
            {
                bbgdfront _result = new bbgdfront();
                _result.frontmode = frontmode;
                _result.frontstatus = frontstatus;
                _result.popmostrecent = popmostrecent;
                _result.backtrackbudget = backtrackbudget;
                _result.frontsize = frontsize;
                _result.entries = entries!=null ? (ap.objarray)entries.make_copy() : null;
                _result.entrypool = entrypool!=null ? (alglib.smp.shared_pool)entrypool.make_copy() : null;
                _result.flag = flag;
                _result.rstate = rstate!=null ? (rcommstate)rstate.make_copy() : null;
                _result.jobs = (int[])jobs.Clone();
                return _result;
            }
        };


        /*************************************************************************
        This object stores nonlinear optimizer state.
        You should use functions provided by MinNLC subpackage to work  with  this
        object
        *************************************************************************/
        public class bbgdstate : apobject
        {
            public int n;
            public optserv.nlpstoppingcriteria criteria;
            public double diffstep;
            public int convexityflag;
            public double pdgap;
            public double ctol;
            public double epsx;
            public double epsf;
            public int nonrootmaxitslin;
            public int nonrootmaxitsconst;
            public int nonrootadditsforfeasibility;
            public double pseudocostmu;
            public int minbranchreliability;
            public int nmultistarts;
            public bool usepseudocosts;
            public int dodiving;
            public int timeout;
            public int bbgdgroupsize;
            public int bbalgo;
            public int maxsubsolvers;
            public bool forceserial;
            public int softmaxnodes;
            public int hardmaxnodes;
            public int maxprimalcandidates;
            public double[] s;
            public double[] bndl;
            public double[] bndu;
            public bool[] hasbndl;
            public bool[] hasbndu;
            public bool[] isintegral;
            public bool[] isbinary;
            public int objtype;
            public sparse.sparsematrix obja;
            public double[] objb;
            public double objc0;
            public int[] qpordering;
            public sparse.sparsematrix rawa;
            public double[] rawal;
            public double[] rawau;
            public int[] lcsrcidx;
            public int lccnt;
            public int nnlc;
            public double[] nl;
            public double[] nu;
            public bool hasx0;
            public double[] x0;
            public bool userterminationneeded;
            public double[] xc;
            public int repnfev;
            public int repnsubproblems;
            public int repntreenodes;
            public int repnnodesbeforefeasibility;
            public int repnprimalcandidates;
            public int repterminationtype;
            public double repf;
            public double reppdgap;
            public apserv.stimer timerglobal;
            public bool dotrace;
            public bool dolaconictrace;
            public int nextleafid;
            public bool hasprimalsolution;
            public double[] xprim;
            public double fprim;
            public double hprim;
            public double ffdual;
            public bool timedout;
            public bbgdsubproblem rootsubproblem;
            public ap.objarray bbsubproblems;
            public int bbsubproblemsheapsize;
            public int bbsubproblemsrecentlyadded;
            public bbgdfront front;
            public alglib.smp.shared_pool sppool;
            public alglib.smp.shared_pool subsolverspool;
            public double[] pseudocostsup;
            public double[] pseudocostsdown;
            public int[] pseudocostscntup;
            public int[] pseudocostscntdown;
            public double globalpseudocostup;
            public double globalpseudocostdown;
            public int globalpseudocostcntup;
            public int globalpseudocostcntdown;
            public hqrnd.hqrndstate unsafeglobalrng;
            public int requestsource;
            public int lastrequesttype;
            public bbgdsubproblem dummysubproblem;
            public bbgdfrontsubsolver dummysubsolver;
            public ipm2solver.ipm2state dummyqpsubsolver;
            public bbgdfrontentry dummyentry;
            public double[,] densedummy2;
            public rcommstate rstate;
            public bbgdstate()
            {
                init();
            }
            public override void init()
            {
                criteria = new optserv.nlpstoppingcriteria();
                s = new double[0];
                bndl = new double[0];
                bndu = new double[0];
                hasbndl = new bool[0];
                hasbndu = new bool[0];
                isintegral = new bool[0];
                isbinary = new bool[0];
                obja = new sparse.sparsematrix();
                objb = new double[0];
                qpordering = new int[0];
                rawa = new sparse.sparsematrix();
                rawal = new double[0];
                rawau = new double[0];
                lcsrcidx = new int[0];
                nl = new double[0];
                nu = new double[0];
                x0 = new double[0];
                xc = new double[0];
                timerglobal = new apserv.stimer();
                xprim = new double[0];
                rootsubproblem = new bbgdsubproblem();
                bbsubproblems = new ap.objarray();
                front = new bbgdfront();
                sppool = new alglib.smp.shared_pool();
                subsolverspool = new alglib.smp.shared_pool();
                pseudocostsup = new double[0];
                pseudocostsdown = new double[0];
                pseudocostscntup = new int[0];
                pseudocostscntdown = new int[0];
                unsafeglobalrng = new hqrnd.hqrndstate();
                dummysubproblem = new bbgdsubproblem();
                dummysubsolver = new bbgdfrontsubsolver();
                dummyqpsubsolver = new ipm2solver.ipm2state();
                dummyentry = new bbgdfrontentry();
                densedummy2 = new double[0,0];
                rstate = new rcommstate();
            }
            public override alglib.apobject make_copy()
            {
                bbgdstate _result = new bbgdstate();
                _result.n = n;
                _result.criteria = criteria!=null ? (optserv.nlpstoppingcriteria)criteria.make_copy() : null;
                _result.diffstep = diffstep;
                _result.convexityflag = convexityflag;
                _result.pdgap = pdgap;
                _result.ctol = ctol;
                _result.epsx = epsx;
                _result.epsf = epsf;
                _result.nonrootmaxitslin = nonrootmaxitslin;
                _result.nonrootmaxitsconst = nonrootmaxitsconst;
                _result.nonrootadditsforfeasibility = nonrootadditsforfeasibility;
                _result.pseudocostmu = pseudocostmu;
                _result.minbranchreliability = minbranchreliability;
                _result.nmultistarts = nmultistarts;
                _result.usepseudocosts = usepseudocosts;
                _result.dodiving = dodiving;
                _result.timeout = timeout;
                _result.bbgdgroupsize = bbgdgroupsize;
                _result.bbalgo = bbalgo;
                _result.maxsubsolvers = maxsubsolvers;
                _result.forceserial = forceserial;
                _result.softmaxnodes = softmaxnodes;
                _result.hardmaxnodes = hardmaxnodes;
                _result.maxprimalcandidates = maxprimalcandidates;
                _result.s = (double[])s.Clone();
                _result.bndl = (double[])bndl.Clone();
                _result.bndu = (double[])bndu.Clone();
                _result.hasbndl = (bool[])hasbndl.Clone();
                _result.hasbndu = (bool[])hasbndu.Clone();
                _result.isintegral = (bool[])isintegral.Clone();
                _result.isbinary = (bool[])isbinary.Clone();
                _result.objtype = objtype;
                _result.obja = obja!=null ? (sparse.sparsematrix)obja.make_copy() : null;
                _result.objb = (double[])objb.Clone();
                _result.objc0 = objc0;
                _result.qpordering = (int[])qpordering.Clone();
                _result.rawa = rawa!=null ? (sparse.sparsematrix)rawa.make_copy() : null;
                _result.rawal = (double[])rawal.Clone();
                _result.rawau = (double[])rawau.Clone();
                _result.lcsrcidx = (int[])lcsrcidx.Clone();
                _result.lccnt = lccnt;
                _result.nnlc = nnlc;
                _result.nl = (double[])nl.Clone();
                _result.nu = (double[])nu.Clone();
                _result.hasx0 = hasx0;
                _result.x0 = (double[])x0.Clone();
                _result.userterminationneeded = userterminationneeded;
                _result.xc = (double[])xc.Clone();
                _result.repnfev = repnfev;
                _result.repnsubproblems = repnsubproblems;
                _result.repntreenodes = repntreenodes;
                _result.repnnodesbeforefeasibility = repnnodesbeforefeasibility;
                _result.repnprimalcandidates = repnprimalcandidates;
                _result.repterminationtype = repterminationtype;
                _result.repf = repf;
                _result.reppdgap = reppdgap;
                _result.timerglobal = timerglobal!=null ? (apserv.stimer)timerglobal.make_copy() : null;
                _result.dotrace = dotrace;
                _result.dolaconictrace = dolaconictrace;
                _result.nextleafid = nextleafid;
                _result.hasprimalsolution = hasprimalsolution;
                _result.xprim = (double[])xprim.Clone();
                _result.fprim = fprim;
                _result.hprim = hprim;
                _result.ffdual = ffdual;
                _result.timedout = timedout;
                _result.rootsubproblem = rootsubproblem!=null ? (bbgdsubproblem)rootsubproblem.make_copy() : null;
                _result.bbsubproblems = bbsubproblems!=null ? (ap.objarray)bbsubproblems.make_copy() : null;
                _result.bbsubproblemsheapsize = bbsubproblemsheapsize;
                _result.bbsubproblemsrecentlyadded = bbsubproblemsrecentlyadded;
                _result.front = front!=null ? (bbgdfront)front.make_copy() : null;
                _result.sppool = sppool!=null ? (alglib.smp.shared_pool)sppool.make_copy() : null;
                _result.subsolverspool = subsolverspool!=null ? (alglib.smp.shared_pool)subsolverspool.make_copy() : null;
                _result.pseudocostsup = (double[])pseudocostsup.Clone();
                _result.pseudocostsdown = (double[])pseudocostsdown.Clone();
                _result.pseudocostscntup = (int[])pseudocostscntup.Clone();
                _result.pseudocostscntdown = (int[])pseudocostscntdown.Clone();
                _result.globalpseudocostup = globalpseudocostup;
                _result.globalpseudocostdown = globalpseudocostdown;
                _result.globalpseudocostcntup = globalpseudocostcntup;
                _result.globalpseudocostcntdown = globalpseudocostcntdown;
                _result.unsafeglobalrng = unsafeglobalrng!=null ? (hqrnd.hqrndstate)unsafeglobalrng.make_copy() : null;
                _result.requestsource = requestsource;
                _result.lastrequesttype = lastrequesttype;
                _result.dummysubproblem = dummysubproblem!=null ? (bbgdsubproblem)dummysubproblem.make_copy() : null;
                _result.dummysubsolver = dummysubsolver!=null ? (bbgdfrontsubsolver)dummysubsolver.make_copy() : null;
                _result.dummyqpsubsolver = dummyqpsubsolver!=null ? (ipm2solver.ipm2state)dummyqpsubsolver.make_copy() : null;
                _result.dummyentry = dummyentry!=null ? (bbgdfrontentry)dummyentry.make_copy() : null;
                _result.densedummy2 = (double[,])densedummy2.Clone();
                _result.rstate = rstate!=null ? (rcommstate)rstate.make_copy() : null;
                return _result;
            }
        };




        public const double safetyfactor = 0.001;
        public const int backtracklimit = 0;
        public const double alphaint = 0.01;
        public const int ftundefined = -1;
        public const int ftroot = 0;
        public const int ftbasic = 1;
        public const int ftdynamic = 2;
        public const int stundefined = -1;
        public const int stfrontrunning = 698;
        public const int stfrontreadytorun = 699;
        public const int streadytorun = 700;
        public const int stwaitingforrcomm = 701;
        public const int stsolved = 702;
        public const int sttimeout = 703;
        public const int rqsrcfront = 1;
        public const int rqsrcxc = 2;
        public const int divenever = 0;
        public const int diveuntilprimal = 1;
        public const int divealways = 2;
        public const int maxipmits = 200;
        public const int maxqprfsits = 5;


        /*************************************************************************
        BBGD solver initialization.
        --------------------------------------------------------------------------

          -- ALGLIB --
             Copyright 01.01.2025 by Bochkanov Sergey
        *************************************************************************/
        public static void bbgdcreatebuf(int n,
            double[] bndl,
            double[] bndu,
            double[] s,
            double[] x0,
            bool[] isintegral,
            bool[] isbinary,
            sparse.sparsematrix sparsea,
            double[] al,
            double[] au,
            int[] lcsrcidx,
            int lccnt,
            double[] nl,
            double[] nu,
            int nnlc,
            int groupsize,
            int nmultistarts,
            int timeout,
            int tracelevel,
            bbgdstate state,
            alglib.xparams _params)
        {
            int i = 0;

            alglib.ap.assert(n>=1, "BBGDCreateBuf: N<1");
            alglib.ap.assert(alglib.ap.len(x0)>=n, "BBGDCreateBuf: Length(X0)<N");
            alglib.ap.assert(apserv.isfinitevector(x0, n, _params), "BBGDCreateBuf: X contains infinite or NaN values");
            alglib.ap.assert(alglib.ap.len(bndl)>=n, "BBGDCreateBuf: Length(BndL)<N");
            alglib.ap.assert(alglib.ap.len(bndu)>=n, "BBGDCreateBuf: Length(BndU)<N");
            alglib.ap.assert(alglib.ap.len(s)>=n, "BBGDCreateBuf: Length(S)<N");
            alglib.ap.assert(alglib.ap.len(isintegral)>=n, "BBGDCreateBuf: Length(IsIntegral)<N");
            alglib.ap.assert(alglib.ap.len(isbinary)>=n, "BBGDCreateBuf: Length(IsBinary)<N");
            alglib.ap.assert(nnlc>=0, "BBGDCreateBuf: NNLC<0");
            alglib.ap.assert(alglib.ap.len(nl)>=nnlc, "BBGDCreateBuf: Length(NL)<NNLC");
            alglib.ap.assert(alglib.ap.len(nu)>=nnlc, "BBGDCreateBuf: Length(NU)<NNLC");
            alglib.ap.assert(groupsize>=1, "BBGDCreateBuf: GroupSize<1");
            alglib.ap.assert(nmultistarts>=1, "BBGDCreateBuf: NMultistarts<1");
            alglib.ap.assert(timeout>=0, "BBGDCreateBuf: Timeout<0");
            alglib.ap.assert((tracelevel==0 || tracelevel==1) || tracelevel==2, "BBGDCreateBuf: unexpected trace level");
            initinternal(n, x0, 0, 0.0, state, _params);
            state.forceserial = false;
            state.bbgdgroupsize = groupsize;
            state.nmultistarts = nmultistarts;
            state.timeout = timeout;
            state.dotrace = tracelevel==2;
            state.dolaconictrace = tracelevel==1;
            for(i=0; i<=n-1; i++)
            {
                alglib.ap.assert(math.isfinite(bndl[i]) || Double.IsNegativeInfinity(bndl[i]), "BBGDCreateBuf: BndL contains NAN or +INF");
                alglib.ap.assert(math.isfinite(bndu[i]) || Double.IsPositiveInfinity(bndu[i]), "BBGDCreateBuf: BndL contains NAN or -INF");
                alglib.ap.assert(isintegral[i] || !isbinary[i], "BBGDCreateBuf: variable marked as binary but not integral");
                alglib.ap.assert(math.isfinite(s[i]), "BBGDCreateBuf: S contains infinite or NAN elements");
                alglib.ap.assert((double)(s[i])!=(double)(0), "BBGDCreateBuf: S contains zero elements");
                state.bndl[i] = bndl[i];
                state.hasbndl[i] = math.isfinite(bndl[i]);
                state.bndu[i] = bndu[i];
                state.hasbndu[i] = math.isfinite(bndu[i]);
                state.isintegral[i] = isintegral[i];
                state.isbinary[i] = isbinary[i];
                state.s[i] = apserv.rcase2(isintegral[i], 1.0, Math.Abs(s[i]), _params);
            }
            state.lccnt = lccnt;
            if( lccnt>0 )
            {
                sparse.sparsecopybuf(sparsea, state.rawa, _params);
                ablasf.rcopyallocv(lccnt, al, ref state.rawal, _params);
                ablasf.rcopyallocv(lccnt, au, ref state.rawau, _params);
                ablasf.icopyallocv(lccnt, lcsrcidx, ref state.lcsrcidx, _params);
            }
            state.nnlc = nnlc;
            ablasf.rallocv(nnlc, ref state.nl, _params);
            ablasf.rallocv(nnlc, ref state.nu, _params);
            for(i=0; i<=nnlc-1; i++)
            {
                alglib.ap.assert(math.isfinite(nl[i]) || Double.IsNegativeInfinity(nl[i]), "BBGDCreateBuf: NL[i] is +INF or NAN");
                alglib.ap.assert(math.isfinite(nu[i]) || Double.IsPositiveInfinity(nu[i]), "BBGDCreateBuf: NU[i] is -INF or NAN");
                state.nl[i] = nl[i];
                state.nu[i] = nu[i];
            }
        }


        /*************************************************************************
        Enforces serial processing (useful when BBGD is called as a part of larger
        multithreaded algorithm)

          -- ALGLIB --
             Copyright 01.01.2025 by Bochkanov Sergey
        *************************************************************************/
        public static void bbgdforceserial(bbgdstate state,
            alglib.xparams _params)
        {
            state.forceserial = true;
        }


        /*************************************************************************
        Set required primal-dual gap

          -- ALGLIB --
             Copyright 01.01.2025 by Bochkanov Sergey
        *************************************************************************/
        public static void bbgdsetpdgap(bbgdstate state,
            double pdgap,
            alglib.xparams _params)
        {
            state.pdgap = pdgap;
        }


        /*************************************************************************
        Set tolerance for violation of nonlinear constraints

          -- ALGLIB --
             Copyright 01.01.2025 by Bochkanov Sergey
        *************************************************************************/
        public static void bbgdsetctol(bbgdstate state,
            double ctol,
            alglib.xparams _params)
        {
            state.ctol = ctol;
        }


        /*************************************************************************
        Set subsolver stopping condition

          -- ALGLIB --
             Copyright 01.01.2025 by Bochkanov Sergey
        *************************************************************************/
        public static void bbgdsetepsf(bbgdstate state,
            double epsf,
            alglib.xparams _params)
        {
            state.epsf = epsf;
        }


        /*************************************************************************
        Sets diving strategy:
        * 0 for no diving
        * 1 for diving until finding first primal solution, then switching to best
          first
        * 2 for always diving

          -- ALGLIB --
             Copyright 01.01.2025 by Bochkanov Sergey
        *************************************************************************/
        public static void bbgdsetdiving(bbgdstate state,
            int divingmode,
            alglib.xparams _params)
        {
            if( divingmode==0 )
            {
                state.dodiving = divenever;
                return;
            }
            if( divingmode==1 )
            {
                state.dodiving = diveuntilprimal;
                return;
            }
            if( divingmode==2 )
            {
                state.dodiving = divealways;
                return;
            }
            alglib.ap.assert(false, "BBGDSetDiving: unexpected diving mode");
        }


        /*************************************************************************
        Tells the solver to stop after finding MaxCand primal candidates (integral
        solutions that were accepted or fathomed)

          -- ALGLIB --
             Copyright 01.01.2025 by Bochkanov Sergey
        *************************************************************************/
        public static void bbgdsetmaxprimalcandidates(bbgdstate state,
            int maxcand,
            alglib.xparams _params)
        {
            state.maxprimalcandidates = maxcand;
        }


        /*************************************************************************
        Set soft max nodes (stop after this amount of nodes, if we have a primal
        solution)

          -- ALGLIB --
             Copyright 01.01.2025 by Bochkanov Sergey
        *************************************************************************/
        public static void bbgdsetsoftmaxnodes(bbgdstate state,
            int maxnodes,
            alglib.xparams _params)
        {
            state.softmaxnodes = maxnodes;
        }


        /*************************************************************************
        Set hard max nodes (stop after this amount of nodes, no matter primal
        solution status)

          -- ALGLIB --
             Copyright 01.01.2025 by Bochkanov Sergey
        *************************************************************************/
        public static void bbgdsethardmaxnodes(bbgdstate state,
            int maxnodes,
            alglib.xparams _params)
        {
            state.hardmaxnodes = maxnodes;
        }


        /*************************************************************************
        Set subsolver stopping condition

          -- ALGLIB --
             Copyright 01.01.2025 by Bochkanov Sergey
        *************************************************************************/
        public static void bbgdsetepsx(bbgdstate state,
            double epsx,
            alglib.xparams _params)
        {
            state.epsx = epsx;
        }


        /*************************************************************************
        Sets quadratic objective. If no nonlinear constraints  were given,  it  is
        guaranteed that no RCOMM requests will be issued during the optimization.

        The objective has the form 0.5*x'*A*x + b'*x + c0

        Sparse A can be stored in any format, but presently this function supports
        only matrices given by their lower triangle.

          -- ALGLIB --
             Copyright 01.01.2025 by Bochkanov Sergey
        *************************************************************************/
        public static void bbgdsetquadraticobjective(bbgdstate state,
            sparse.sparsematrix a,
            bool isupper,
            double[] b,
            double c0,
            alglib.xparams _params)
        {
            alglib.ap.assert(!isupper, "BBGDSetQuadraticObjective: IsUpper=False is not implemented yet");
            state.objtype = 1;
            sparse.sparsecopytocrs(a, state.obja, _params);
            ablasf.rcopyallocv(state.n, b, ref state.objb, _params);
            state.objc0 = c0;
        }


        /*************************************************************************


          -- ALGLIB --
             Copyright 01.01.2025 by Bochkanov Sergey
        *************************************************************************/
        public static bool bbgditeration(bbgdstate state,
            alglib.xparams _params)
        {
            bool result = new bool();
            int n = 0;
            int i = 0;
            int k = 0;

            
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
                k = state.rstate.ia[2];
            }
            else
            {
                n = 359;
                i = -58;
                k = -919;
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
            
            //
            // Init
            //
            n = state.n;
            clearoutputs(state, _params);
            state.bbsubproblems.clear();
            state.bbsubproblemsheapsize = 0;
            state.bbsubproblemsrecentlyadded = 0;
            ablasf.rsetallocv(state.n, 1.0, ref state.pseudocostsup, _params);
            ablasf.rsetallocv(state.n, 1.0, ref state.pseudocostsdown, _params);
            ablasf.isetallocv(state.n, 0, ref state.pseudocostscntup, _params);
            ablasf.isetallocv(state.n, 0, ref state.pseudocostscntdown, _params);
            state.globalpseudocostup = 1.0;
            state.globalpseudocostdown = 1.0;
            state.globalpseudocostcntup = 0;
            state.globalpseudocostcntdown = 0;
            alglib.ap.assert(state.objtype==0 || state.objtype==1, "BBGD: 661544 failed");
            if( state.objtype==1 )
            {
                for(i=0; i<=n-1; i++)
                {
                    if( state.obja.ridx[i]<state.obja.didx[i] )
                    {
                        alglib.ap.assert(false, "BBGD: preordering for non-diagonal A is not implemented yet");
                    }
                }
                ipm2solver.ipm2proposeordering(state.dummyqpsubsolver, n, true, state.hasbndl, state.hasbndu, state.rawa, state.rawal, state.rawau, state.lccnt, ref state.qpordering, _params);
            }
            
            //
            // Initialize globally shared information
            //
            state.nextleafid = 0;
            state.hasprimalsolution = false;
            state.fprim = Double.PositiveInfinity;
            state.timedout = false;
            state.ffdual = Double.NegativeInfinity;
            frontinitundefined(state.front, state, _params);
            alglib.smp.ae_shared_pool_set_seed_if_different(state.sppool, state.dummysubproblem);
            alglib.smp.ae_shared_pool_set_seed_if_different(state.subsolverspool, state.dummysubsolver);
            
            //
            // Prepare root subproblem, perform initial feasibility checks, solve it. This part is the
            // same for all BB algorithms.
            //
            apserv.stimerinit(state.timerglobal, _params);
            apserv.stimerstart(state.timerglobal, _params);
            state.rootsubproblem.leafid = apserv.weakatomicfetchadd(ref state.nextleafid, 1, _params);
            state.rootsubproblem.leafidx = -1;
            state.rootsubproblem.parentfdual = math.maxrealnumber;
            state.rootsubproblem.n = n;
            alglib.ap.assert(state.hasx0, "BBGD: integrity check 500655 failed");
            ablasf.rcopyallocv(n, state.x0, ref state.rootsubproblem.x0, _params);
            ablasf.rcopyallocv(n, state.bndl, ref state.rootsubproblem.bndl, _params);
            ablasf.rcopyallocv(n, state.bndu, ref state.rootsubproblem.bndu, _params);
            for(i=0; i<=n-1; i++)
            {
                if( state.isintegral[i] )
                {
                    if( math.isfinite(state.rootsubproblem.bndl[i]) )
                    {
                        state.rootsubproblem.bndl[i] = (int)Math.Ceiling(state.rootsubproblem.bndl[i]-state.ctol);
                    }
                    if( math.isfinite(state.rootsubproblem.bndu[i]) )
                    {
                        state.rootsubproblem.bndu[i] = (int)Math.Floor(state.rootsubproblem.bndu[i]+state.ctol);
                    }
                }
                if( state.isbinary[i] )
                {
                    if( Double.IsNegativeInfinity(state.rootsubproblem.bndl[i]) || (double)(state.rootsubproblem.bndl[i])<(double)(0) )
                    {
                        state.rootsubproblem.bndl[i] = 0;
                    }
                    if( Double.IsPositiveInfinity(state.rootsubproblem.bndu[i]) || (double)(state.rootsubproblem.bndu[i])>(double)(1) )
                    {
                        state.rootsubproblem.bndu[i] = 1;
                    }
                }
            }
            state.rootsubproblem.hasprimalsolution = false;
            state.rootsubproblem.hasdualsolution = false;
            state.rootsubproblem.ncuttingplanes = 0;
            for(i=0; i<=n-1; i++)
            {
                if( (math.isfinite(state.rootsubproblem.bndl[i]) && math.isfinite(state.rootsubproblem.bndu[i])) && (double)(state.rootsubproblem.bndl[i])>(double)(state.rootsubproblem.bndu[i]+state.ctol) )
                {
                    if( state.dotrace )
                    {
                        alglib.ap.trace("> a combination of box and integrality constraints is infeasible, stopping\n");
                    }
                    state.repterminationtype = -3;
                    result = false;
                    return result;
                }
            }
            if( state.dotrace )
            {
                alglib.ap.trace("> generated root node, starting to solve it\n");
            }
            frontstartroot(state.front, state.rootsubproblem, state, _params);
        lbl_4:
            if( !frontrun(state.front, state, _params) )
            {
                goto lbl_5;
            }
            state.requestsource = rqsrcfront;
            state.rstate.stage = 0;
            goto lbl_rcomm;
        lbl_0:
            goto lbl_4;
        lbl_5:
            alglib.ap.assert(state.front.frontstatus==stsolved || state.front.frontstatus==sttimeout, "BBGD: integrity check 184017 failed");
            if( state.front.frontstatus==stsolved )
            {
                frontpushsolution(state.front, state, _params);
                if( state.dotrace )
                {
                    alglib.ap.trace(System.String.Format("> root subproblem solved in {0,0:F0} ms\n", apserv.stimergetmsrunning(state.timerglobal, _params)));
                    alglib.ap.trace(System.String.Format(">> primal (upper) bound is {0,0:E12}\n", state.fprim));
                    alglib.ap.trace(System.String.Format(">> dual   (lower) bound is {0,0:E12}\n", state.ffdual));
                }
            }
            else
            {
                if( state.dotrace )
                {
                    alglib.ap.trace(System.String.Format("> timeout was signaled during solution of the root subproblem, {0,0:F0} ms passed\n", apserv.stimergetmsrunning(state.timerglobal, _params)));
                }
                state.timedout = true;
            }
            
            //
            // The part below is different for different values of BBAlgo
            //
            alglib.ap.assert(state.bbalgo==0 || state.bbalgo==1, "BBGD: 767318 failed");
            if( state.bbalgo!=0 )
            {
                goto lbl_6;
            }
            
            //
            // Initial BBGD version: iterate until small gap is achieved
            //
            state.repterminationtype = 1;
        lbl_8:
            if( state.bbsubproblems.getlength()<=0 )
            {
                goto lbl_9;
            }
            
            //
            // Check for small primal-dual gap
            //
            if( state.hasprimalsolution && (double)(state.ffdual)>=(double)(state.fprim-state.pdgap*apserv.rmaxabs2(state.fprim, 1, _params)) )
            {
                if( state.dotrace )
                {
                    alglib.ap.trace(System.String.Format("> relative duality gap decreased below {0,0:E2}, stopping\n", state.pdgap));
                }
                goto lbl_9;
            }
            
            //
            // Explore B&B tree:
            // * if no primal solution was found yet, try diving from most recently added subproblems
            // * otherwise, start from from the best problem(s) in the tree
            //
            if( state.hasprimalsolution || !frontstartfromrecentlyadded(state.front, state, _params) )
            {
                if( !frontstart(state.front, state, _params) )
                {
                    goto lbl_9;
                }
            }
        lbl_10:
            if( !frontrun(state.front, state, _params) )
            {
                goto lbl_11;
            }
            state.requestsource = rqsrcfront;
            state.rstate.stage = 1;
            goto lbl_rcomm;
        lbl_1:
            goto lbl_10;
        lbl_11:
            alglib.ap.assert(state.front.frontstatus==stsolved || state.front.frontstatus==sttimeout, "BBGD: integrity check 231020 failed");
            if( state.front.frontstatus==sttimeout )
            {
                if( state.dotrace )
                {
                    alglib.ap.trace(System.String.Format("> timeout was signaled, {0,0:F0} ms passed\n", apserv.stimergetmsrunning(state.timerglobal, _params)));
                }
                state.timedout = true;
                goto lbl_9;
            }
            frontpushsolution(state.front, state, _params);
            goto lbl_8;
        lbl_9:
        lbl_6:
            if( state.bbalgo!=1 )
            {
                goto lbl_12;
            }
            
            //
            // Dynamic front with better parallelism options
            //
            state.repterminationtype = 1;
            frontstartdynamic(state.front, state, _params);
        lbl_14:
            if( !frontrun(state.front, state, _params) )
            {
                goto lbl_15;
            }
            state.requestsource = rqsrcfront;
            state.rstate.stage = 2;
            goto lbl_rcomm;
        lbl_2:
            goto lbl_14;
        lbl_15:
            alglib.ap.assert(state.front.frontstatus==stsolved || state.front.frontstatus==sttimeout, "BBGD: integrity check 826253 failed");
            if( state.front.frontstatus==sttimeout )
            {
                if( state.dotrace )
                {
                    alglib.ap.trace(System.String.Format("> timeout was signaled, {0,0:F0} ms passed\n", apserv.stimergetmsrunning(state.timerglobal, _params)));
                }
                state.timedout = true;
            }
        lbl_12:
            
            //
            // Write out solution
            //
            if( !state.hasprimalsolution )
            {
                goto lbl_16;
            }
            
            //
            // A primal solution was found
            //
            alglib.ap.assert(state.repterminationtype>0, "BBGD: integrity check 080232 failed");
            alglib.ap.assert(state.objtype==0 || state.objtype==1, "BBGD: 778547 failed");
            ablasf.rcopyallocv(n, state.xprim, ref state.xc, _params);
            if( state.objtype!=0 )
            {
                goto lbl_18;
            }
            state.requestsource = rqsrcxc;
            state.rstate.stage = 3;
            goto lbl_rcomm;
        lbl_3:
            goto lbl_19;
        lbl_18:
            state.repf = 0.5*sparse.sparsevsmv(state.obja, false, state.xc, _params)+ablasf.rdotv(n, state.xc, state.objb, _params)+state.objc0;
        lbl_19:
            state.reppdgap = Math.Max(state.fprim-state.ffdual, 0)/apserv.rmaxabs2(state.fprim, 1, _params);
            state.repterminationtype = 1;
            if( state.timedout )
            {
                state.repterminationtype = 5;
            }
            if( state.dotrace )
            {
                alglib.ap.trace(System.String.Format("> the solution is found: f={0,0:E9}, relative duality gap is {1,0:E3}\n", state.repf, state.reppdgap));
            }
            goto lbl_17;
        lbl_16:
            
            //
            // The problem is infeasible
            //
            alglib.ap.assert(state.front.frontstatus==stsolved || state.front.frontstatus==sttimeout, "BBGD: integrity check 280023 failed");
            state.repterminationtype = -3;
            if( state.timedout )
            {
                state.repterminationtype = -33;
            }
            if( state.dotrace )
            {
                alglib.ap.trace("> the problem is infeasible (or feasible point is too difficult to find)\n");
            }
        lbl_17:
            result = false;
            return result;
            
            //
            // Saving state
            //
        lbl_rcomm:
            result = true;
            state.rstate.ia[0] = n;
            state.rstate.ia[1] = i;
            state.rstate.ia[2] = k;
            return result;
        }


        /*************************************************************************
        Produce RComm request in RCOMM-V2 format, according to the current request
        source
        *************************************************************************/
        public static void bbgdoffloadrcommrequest(bbgdstate state,
            ref int requesttype,
            ref int querysize,
            ref int queryfuncs,
            ref int queryvars,
            ref int querydim,
            ref int queryformulasize,
            ref double[] querydata,
            alglib.xparams _params)
        {
            if( state.requestsource==rqsrcfront )
            {
                requesttype = 0;
                frontpackqueries(state.front, state, ref requesttype, ref querysize, ref queryfuncs, ref queryvars, ref querydim, ref queryformulasize, ref querydata, _params);
                alglib.ap.assert(querysize>0, "BBGD: 074812 failed");
                state.repnfev = state.repnfev+querysize;
                return;
            }
            if( state.requestsource==rqsrcxc )
            {
                requesttype = 1;
                querysize = 1;
                queryfuncs = 1+state.nnlc;
                queryvars = state.n;
                querydim = 0;
                ablasf.rcopyallocv(state.n, state.xc, ref querydata, _params);
                return;
            }
            alglib.ap.assert(false, "BBGD: integrity check 094519 failed");
        }


        /*************************************************************************
        Process RComm reply in RCOMM-V2 format, according to the current request
        source
        *************************************************************************/
        public static void bbgdloadrcommreply(bbgdstate state,
            int requesttype,
            int querysize,
            int queryfuncs,
            int queryvars,
            int querydim,
            int queryformulasize,
            double[] replyfi,
            double[] replydj,
            sparse.sparsematrix replysj,
            alglib.xparams _params)
        {
            if( state.requestsource==rqsrcfront )
            {
                frontunpackreplies(state, requesttype, querysize, queryfuncs, queryvars, querydim, queryformulasize, replyfi, replydj, replysj, state.front, _params);
                return;
            }
            if( state.requestsource==rqsrcxc )
            {
                state.repf = replyfi[0];
                return;
            }
            alglib.ap.assert(false, "BBGD: integrity check 118522 failed");
        }


        /*************************************************************************
        Clears output fields during initialization
        *************************************************************************/
        private static void clearoutputs(bbgdstate state,
            alglib.xparams _params)
        {
            state.userterminationneeded = false;
            state.repnfev = 0;
            state.repterminationtype = 0;
            state.repf = 0;
            state.reppdgap = math.maxrealnumber;
            state.repnsubproblems = 0;
            state.repntreenodes = 0;
            state.repnnodesbeforefeasibility = -1;
            state.repnprimalcandidates = 0;
        }


        /*************************************************************************
        Internal initialization subroutine.
        Sets default NLC solver with default criteria.
        *************************************************************************/
        private static void initinternal(int n,
            double[] x,
            int solvermode,
            double diffstep,
            bbgdstate state,
            alglib.xparams _params)
        {
            int i = 0;
            double[,] c = new double[0,0];
            int[] ct = new int[0];

            state.convexityflag = 0;
            
            //
            // Initialize other params
            //
            optserv.critinitdefault(state.criteria, _params);
            state.timeout = 0;
            state.pdgap = 1.0E-6;
            state.ctol = 1.0E-5;
            state.n = n;
            state.epsx = 1.0E-7;
            state.epsf = 1.0E-7;
            state.nonrootmaxitslin = 2;
            state.nonrootmaxitsconst = 50;
            state.nonrootadditsforfeasibility = 5;
            state.minbranchreliability = 1;
            state.nmultistarts = 1;
            state.usepseudocosts = true;
            state.dodiving = diveuntilprimal;
            state.pseudocostmu = 0.001;
            state.diffstep = diffstep;
            state.userterminationneeded = false;
            state.bbalgo = 1;
            state.maxsubsolvers = 4*apserv.maxconcurrency(_params);
            state.softmaxnodes = 0;
            state.hardmaxnodes = 0;
            state.maxprimalcandidates = 0;
            ablasf.bsetallocv(n, false, ref state.isintegral, _params);
            ablasf.bsetallocv(n, false, ref state.isbinary, _params);
            state.bndl = new double[n];
            state.hasbndl = new bool[n];
            state.bndu = new double[n];
            state.hasbndu = new bool[n];
            state.s = new double[n];
            state.x0 = new double[n];
            state.xc = new double[n];
            for(i=0; i<=n-1; i++)
            {
                state.bndl[i] = Double.NegativeInfinity;
                state.hasbndl[i] = false;
                state.bndu[i] = Double.PositiveInfinity;
                state.hasbndu[i] = false;
                state.s[i] = 1.0;
                state.x0[i] = x[i];
                state.xc[i] = x[i];
            }
            state.hasx0 = true;
            
            //
            // Objective
            //
            state.objtype = 0;
            
            //
            // Constraints
            //
            state.lccnt = 0;
            state.nnlc = 0;
            
            //
            // Report fields
            //
            clearoutputs(state, _params);
            
            //
            // Other structures
            //
            hqrnd.hqrndseed(8543, 7455, state.unsafeglobalrng, _params);
            
            //
            // RComm
            //
            state.rstate.ia = new int[2+1];
            state.rstate.stage = -1;
        }


        /*************************************************************************
        Appends RComm-V2 request coming from the subsolver to a queue. Performs
        dimensional reduction, truncating slack variable.

        On the first call to this function State.RequestType must be zero.
        *************************************************************************/
        private static void reduceandappendrequestto(minnlc.minnlcstate subsolver,
            bbgdstate state,
            ref int requesttype,
            ref int querysize,
            ref int queryfuncs,
            ref int queryvars,
            ref int querydim,
            ref int queryformulasize,
            ref double[] querydata,
            alglib.xparams _params)
        {
            int localrequesttype = 0;
            int n = 0;

            
            //
            // If our request is the first one in a queue, initialize aggregated request.
            // Otherwise, perform compatibility checks.
            //
            localrequesttype = subsolver.requesttype;
            alglib.ap.assert((((localrequesttype==1 || localrequesttype==2) || localrequesttype==3) || localrequesttype==4) || localrequesttype==5, "BBGD: subsolver sends unsupported request");
            if( requesttype==0 )
            {
                
                //
                // Load query metric and perform integrity checks
                //
                requesttype = localrequesttype;
                state.lastrequesttype = localrequesttype;
                querysize = 0;
                alglib.ap.assert(subsolver.queryfuncs>=1, "BBGD: integrity check 946245 failed");
                alglib.ap.assert(subsolver.queryvars>=1, "BBGD: integrity check 947246 failed");
                alglib.ap.assert(subsolver.querydim==0, "BBGD: integrity check 947247 failed");
                queryfuncs = subsolver.queryfuncs;
                queryvars = subsolver.queryvars;
                querydim = 0;
                queryformulasize = subsolver.queryformulasize;
            }
            alglib.ap.assert(requesttype==localrequesttype, "BBGD: subsolvers send incompatible request types that can not be aggregated");
            alglib.ap.assert(queryfuncs==subsolver.queryfuncs, "BBGD: subsolvers send requests that have incompatible sizes and can not be aggregated");
            alglib.ap.assert(queryvars==subsolver.queryvars, "BBGD: subsolvers send requests that have incompatible sizes and can not be aggregated");
            alglib.ap.assert(subsolver.querydim==0, "BBGD: subsolver send request with QueryDim<>0, unexpected");
            alglib.ap.assert((localrequesttype!=3 && localrequesttype!=5) || queryformulasize==subsolver.queryformulasize, "BBGD: subsolvers send requests that are incompatible due to different query formula sizes");
            n = queryvars;
            
            //
            // Handle various request types
            //
            if( localrequesttype==1 )
            {
                
                //
                // Query sparse Jacobian
                //
                ablasf.rgrowv(querysize*queryvars+subsolver.querysize*queryvars, ref querydata, _params);
                ablasf.rcopyvx(subsolver.querysize*n, subsolver.querydata, 0, querydata, querysize*queryvars, _params);
                querysize = querysize+subsolver.querysize;
                return;
            }
            if( localrequesttype==2 )
            {
                
                //
                // Query dense Jacobian
                //
                ablasf.rgrowv(querysize*queryvars+subsolver.querysize*queryvars, ref querydata, _params);
                ablasf.rcopyvx(subsolver.querysize*n, subsolver.querydata, 0, querydata, querysize*queryvars, _params);
                querysize = querysize+subsolver.querysize;
                return;
            }
            alglib.ap.assert(false, "ReduceAndAppendRequestTo: unsupported protocol");
        }


        /*************************************************************************
        Extracts Subproblem.QuerySize replies, starting from RequestIdx-th one,
        from the aggregated reply, reformulates nonlinear objective as an additional
        constraint and extends the problem with a slack variable.
        *************************************************************************/
        private static void extractextendandforwardreplyto(bbgdstate state,
            int requesttype,
            int querysize,
            int queryfuncs,
            int queryvars,
            int querydim,
            int queryformulasize,
            double[] replyfi,
            double[] replydj,
            sparse.sparsematrix replysj,
            ref int requestidx,
            minnlc.minnlcstate subsolver,
            alglib.xparams _params)
        {
            int n = 0;
            int localrequesttype = 0;
            int fidstoffs = 0;
            int fisrcoffs = 0;
            int jacdstoffs = 0;
            int jacsrcoffs = 0;

            
            //
            // Compatibility checks.
            //
            localrequesttype = subsolver.requesttype;
            alglib.ap.assert(localrequesttype==state.lastrequesttype, "BBGD: integrity check 040003 failed");
            alglib.ap.assert(subsolver.queryfuncs==queryfuncs, "BBGD: integrity check 041003 failed");
            alglib.ap.assert(subsolver.queryvars==queryvars, "BBGD: integrity check 042003 failed");
            alglib.ap.assert(querydim==0, "BBGD: integrity check 043003 failed");
            alglib.ap.assert(requestidx+subsolver.querysize<=querysize, "BBGD: integrity check 044003 failed");
            n = queryvars;
            
            //
            // Handle various request types
            //
            if( state.lastrequesttype==1 )
            {
                
                //
                // A sparse Jacobian is retrieved
                //
                fidstoffs = 0;
                fisrcoffs = requestidx*queryfuncs;
                jacdstoffs = 0;
                jacsrcoffs = requestidx*queryfuncs;
                ablasf.rcopyvx(subsolver.querysize*queryfuncs, replyfi, fisrcoffs, subsolver.replyfi, fidstoffs, _params);
                sparse.sparsecreatecrsfromcrsrangebuf(replysj, jacsrcoffs, jacsrcoffs+subsolver.querysize*queryfuncs, subsolver.replysj, _params);
                requestidx = requestidx+subsolver.querysize;
                return;
            }
            if( state.lastrequesttype==2 )
            {
                
                //
                // A dense Jacobian is retrieved
                //
                fidstoffs = 0;
                fisrcoffs = requestidx*queryfuncs;
                jacdstoffs = 0;
                jacsrcoffs = requestidx*queryvars*queryfuncs;
                ablasf.rcopyvx(subsolver.querysize*queryfuncs, replyfi, fisrcoffs, subsolver.replyfi, fidstoffs, _params);
                ablasf.rcopyvx(n*subsolver.querysize*queryfuncs, replydj, jacsrcoffs, subsolver.replydj, jacdstoffs, _params);
                requestidx = requestidx+subsolver.querysize;
                return;
            }
            alglib.ap.assert(false, "ExtractExtendAndForwardReplyTo: unsupported protocol");
        }


        /*************************************************************************
        Create a copy of subproblem with new ID (which can be equal to the original
        one, though).
        *************************************************************************/
        private static void subproblemcopy(bbgdsubproblem src,
            int newid,
            bbgdsubproblem dst,
            alglib.xparams _params)
        {
            dst.leafid = newid;
            dst.leafidx = src.leafidx;
            dst.parentfdual = src.parentfdual;
            dst.n = src.n;
            ablasf.rcopyallocv(src.n, src.x0, ref dst.x0, _params);
            ablasf.rcopyallocv(src.n, src.bndl, ref dst.bndl, _params);
            ablasf.rcopyallocv(src.n, src.bndu, ref dst.bndu, _params);
            alglib.ap.assert(src.ncuttingplanes==0, "BBGD: integrity check 346147 failed");
            dst.ncuttingplanes = src.ncuttingplanes;
            dst.hasprimalsolution = src.hasprimalsolution;
            dst.hasdualsolution = src.hasdualsolution;
            if( src.hasprimalsolution )
            {
                ablasf.rcopyallocv(src.n, src.xprim, ref dst.xprim, _params);
            }
            dst.fprim = src.fprim;
            dst.hprim = src.hprim;
            if( src.hasdualsolution )
            {
                ablasf.rcopyallocv(src.n, src.bestxdual, ref dst.bestxdual, _params);
                ablasf.rcopyallocv(src.n, src.worstxdual, ref dst.worstxdual, _params);
            }
            dst.bestfdual = src.bestfdual;
            dst.besthdual = src.besthdual;
            dst.worstfdual = src.worstfdual;
            dst.worsthdual = src.worsthdual;
            dst.bestdualisintfeas = src.bestdualisintfeas;
            dst.dualbound = src.dualbound;
        }


        /*************************************************************************
        Create a copy of subproblem with new ID, in an unsolved state
        *************************************************************************/
        private static void subproblemcopyasunsolved(bbgdsubproblem src,
            int newid,
            bbgdsubproblem dst,
            alglib.xparams _params)
        {
            subproblemcopy(src, newid, dst, _params);
            dst.hasprimalsolution = false;
            dst.hasdualsolution = false;
            dst.bestdualisintfeas = false;
            dst.fprim = Double.PositiveInfinity;
            dst.hprim = Double.PositiveInfinity;
            dst.bestfdual = Double.PositiveInfinity;
            dst.besthdual = Double.PositiveInfinity;
            dst.worstfdual = Double.PositiveInfinity;
            dst.worsthdual = Double.PositiveInfinity;
            dst.dualbound = Double.PositiveInfinity;
        }


        /*************************************************************************
        Computes dual bound having best and worst versions of a dual solution.
        Sets it to +INF if no dual solution is present.
        *************************************************************************/
        private static void subproblemrecomputedualbound(bbgdsubproblem s,
            alglib.xparams _params)
        {
            double bestworstspread = 0;

            s.dualbound = Double.PositiveInfinity;
            if( s.hasdualsolution )
            {
                bestworstspread = Math.Abs(s.bestfdual-s.worstfdual);
                s.dualbound = s.bestfdual-safetyfactor*bestworstspread;
            }
        }


        /*************************************************************************
        Randomize initial point of a subproblem
        *************************************************************************/
        private static void subproblemrandomizex0(bbgdsubproblem p,
            bbgdstate state,
            alglib.xparams _params)
        {
            int i = 0;
            double vs = 0;

            alglib.ap.assert(p.n==state.n, "BBGD: integrity check 797204 failed");
            vs = Math.Pow(2, 0.5*hqrnd.hqrndnormal(state.unsafeglobalrng, _params));
            for(i=0; i<=state.n-1; i++)
            {
                if( math.isfinite(p.bndl[i]) && math.isfinite(p.bndu[i]) )
                {
                    p.x0[i] = p.bndl[i]+(p.bndu[i]-p.bndl[i])*hqrnd.hqrnduniformr(state.unsafeglobalrng, _params);
                    p.x0[i] = Math.Max(p.x0[i], p.bndl[i]);
                    p.x0[i] = Math.Min(p.x0[i], p.bndu[i]);
                    continue;
                }
                if( math.isfinite(p.bndl[i]) )
                {
                    p.x0[i] = Math.Max(p.x0[i], p.bndl[i])+vs*hqrnd.hqrndnormal(state.unsafeglobalrng, _params);
                    p.x0[i] = Math.Max(p.x0[i], p.bndl[i]);
                    continue;
                }
                if( math.isfinite(p.bndu[i]) )
                {
                    p.x0[i] = Math.Min(p.x0[i], p.bndu[i])+vs*hqrnd.hqrndnormal(state.unsafeglobalrng, _params);
                    p.x0[i] = Math.Min(p.x0[i], p.bndu[i]);
                    continue;
                }
                p.x0[i] = p.x0[i]+vs*hqrnd.hqrndnormal(state.unsafeglobalrng, _params);
            }
        }


        /*************************************************************************
        Initialize front in an undefined state. Ideally, it should be called  once
        per entire optimization session.
        *************************************************************************/
        private static void frontinitundefined(bbgdfront front,
            bbgdstate state,
            alglib.xparams _params)
        {
            front.entries.clear();
            front.frontmode = ftundefined;
            front.frontstatus = stundefined;
            front.popmostrecent = false;
            front.backtrackbudget = backtracklimit;
            alglib.smp.ae_shared_pool_set_seed_if_different(front.entrypool, state.dummyentry);
        }


        /*************************************************************************
        Having an initialized front, configures it to solve a root subproblem.
        *************************************************************************/
        private static void frontstartroot(bbgdfront front,
            bbgdsubproblem r,
            bbgdstate state,
            alglib.xparams _params)
        {
            bbgdfrontentry e = null;

            alglib.ap.assert(state.nmultistarts>=1, "BBGD: integrity check 832130 failed");
            front.frontmode = ftroot;
            front.frontstatus = stfrontreadytorun;
            front.frontsize = 1;
            while( front.entries.getlength()<front.frontsize )
            {
                alglib.smp.ae_shared_pool_retrieve(front.entrypool, ref e);
                front.entries.append(e);
            }
            while( front.entries.getlength()>front.frontsize )
            {
                front.entries.pop_transfer(ref e);
                alglib.smp.ae_shared_pool_recycle(front.entrypool, ref e);
            }
            front.entries.get(0, ref e);
            entryprepareroot(e, front, r, state, _params);
            front.rstate.stage = -1;
        }


        /*************************************************************************
        Starts ftBasic front.

        Retrieves subproblems that are located on top of the heap.

        After that it creates two copies in an unsolved state, performs branch on
        the most infeasible variable and returns True.

        The original subproblem is returned to the spPool.

        Handling of special cases:
        * Problems that have no variables to branch on are skipped
        * Silently returns False on an empty B&B tree or on a tree that have no
          subproblems that can be branched
        *************************************************************************/
        private static bool frontstart(bbgdfront front,
            bbgdstate sstate,
            alglib.xparams _params)
        {
            bool result = new bool();
            bbgdsubproblem p = null;
            bbgdfrontentry e = null;

            result = true;
            
            //
            // Clear the front
            //
            front.frontmode = ftbasic;
            front.frontstatus = stfrontreadytorun;
            front.frontsize = 0;
            while( front.entries.getlength()>front.frontsize )
            {
                front.entries.pop_transfer(ref e);
                alglib.smp.ae_shared_pool_recycle(front.entrypool, ref e);
            }
            
            //
            // Iterate until the B&B tree is empty or until we find a subproblem
            // that can be split.
            //
            while( sstate.bbsubproblems.getlength()>0 && front.frontsize<sstate.bbgdgroupsize )
            {
                
                //
                // Retrieve either the most recently added subproblem (one that is not moved
                // to the heap yet) or one from the top of the heap, depending on the PopMostRecent
                // flag.
                //
                growheapandpoptop(sstate, _params);
                sstate.bbsubproblems.pop_transfer(ref p);
                alglib.ap.assert(p.hasdualsolution, "BBGD: integrity check 456217 failed");
                if( sstate.hasprimalsolution && (double)(p.dualbound)>=(double)(sstate.fprim-sstate.pdgap*apserv.rmaxabs2(sstate.fprim, 1, _params)) )
                {
                    if( sstate.dotrace )
                    {
                        alglib.ap.trace(System.String.Format("> fathomed {0,8:d}P during tree search (p.bestfdual={1,0:E2}, p.dual_bound={2,0:E2}, global.fprim={3,0:E2})\n", p.leafid, p.bestfdual, p.dualbound, sstate.fprim));
                    }
                    alglib.smp.ae_shared_pool_recycle(sstate.sppool, ref p);
                    continue;
                }
                alglib.smp.ae_shared_pool_retrieve(front.entrypool, ref e);
                front.entries.append(e);
                if( !entryprepareleafs(e, front, p, sstate, _params) )
                {
                    
                    //
                    // Looks like the subproblem we extracted does not need splitting.
                    // Next one, please.
                    //
                    if( sstate.dotrace )
                    {
                        alglib.ap.trace(System.String.Format("> subproblem {0,8:d}P does not need integral or spatial branching, skipping\n", p.leafid));
                    }
                    front.entries.pop_transfer(ref e);
                    alglib.smp.ae_shared_pool_recycle(front.entrypool, ref e);
                    alglib.smp.ae_shared_pool_recycle(sstate.sppool, ref p);
                    continue;
                }
                front.frontsize = front.frontsize+1;
                alglib.smp.ae_shared_pool_recycle(sstate.sppool, ref p);
            }
            if( front.frontsize==0 )
            {
                if( sstate.dotrace )
                {
                    alglib.ap.trace("> B&B tree has no subproblems that can be split, stopping\n");
                }
                result = false;
                return result;
            }
            
            //
            // Done
            //
            front.rstate.stage = -1;
            return result;
        }


        /*************************************************************************
        Starts synchronous dynamic front.

        Basically, it creates an empty front that will be dynamically populated by
        FrontRun().

        This function always succeedes.
        *************************************************************************/
        private static void frontstartdynamic(bbgdfront front,
            bbgdstate sstate,
            alglib.xparams _params)
        {
            bbgdfrontentry e = null;

            front.frontmode = ftdynamic;
            front.frontstatus = stfrontreadytorun;
            front.frontsize = 0;
            while( front.entries.getlength()>front.frontsize )
            {
                front.entries.pop_transfer(ref e);
                alglib.smp.ae_shared_pool_recycle(front.entrypool, ref e);
            }
            front.rstate.stage = -1;
        }


        /*************************************************************************
        Retrieves subproblems that were most recently added, decreasing
        State.bbSubproblemsRecentlyAdded variable until zero.

        After that it creates two copies in an unsolved state, performs branch on
        the most infeasible variable and returns True.

        The original subproblem is returned to the spPool.

        Handling of special cases:
        * Problems that have no variables to branch on are skipped
        * Silently returns False on an empty B&B tree or on a tree that have no
          subproblems that can be branched
        *************************************************************************/
        private static bool frontstartfromrecentlyadded(bbgdfront front,
            bbgdstate sstate,
            alglib.xparams _params)
        {
            bool result = new bool();
            bbgdsubproblem p = null;
            bbgdfrontentry e = null;

            result = front.popmostrecent && sstate.bbsubproblemsrecentlyadded>0;
            if( !result )
            {
                return result;
            }
            
            //
            // Clear the front
            //
            front.frontmode = ftbasic;
            front.frontstatus = stfrontreadytorun;
            front.frontsize = 0;
            while( front.entries.getlength()>front.frontsize )
            {
                front.entries.pop_transfer(ref e);
                alglib.smp.ae_shared_pool_recycle(front.entrypool, ref e);
            }
            
            //
            // Iterate until the B&B tree is empty or until we find a subproblem
            // that can be split.
            //
            while( (sstate.bbsubproblemsrecentlyadded>0 && sstate.bbsubproblemsheapsize<sstate.bbsubproblems.getlength()) && front.frontsize<sstate.bbgdgroupsize )
            {
                
                //
                // Retrieve either the most recently added subproblem (one that is not moved
                // to the heap yet) or one from the top of the heap, depending on the PopMostRecent
                // flag.
                //
                sstate.bbsubproblems.pop_transfer(ref p);
                sstate.bbsubproblemsrecentlyadded = sstate.bbsubproblemsrecentlyadded-1;
                alglib.ap.assert(p.hasdualsolution, "BBGD: integrity check 282550 failed");
                if( sstate.hasprimalsolution && (double)(p.dualbound)>=(double)(sstate.fprim-sstate.pdgap*apserv.rmaxabs2(sstate.fprim, 1, _params)) )
                {
                    if( sstate.dotrace )
                    {
                        alglib.ap.trace(System.String.Format("> fathomed {0,8:d}P during tree search (p.bestfdual={1,0:E2}, p.dual_bound={2,0:E2}, global.fprim={3,0:E2})\n", p.leafid, p.bestfdual, p.dualbound, sstate.fprim));
                    }
                    alglib.smp.ae_shared_pool_recycle(sstate.sppool, ref p);
                    continue;
                }
                alglib.smp.ae_shared_pool_retrieve(front.entrypool, ref e);
                front.entries.append(e);
                if( !entryprepareleafs(e, front, p, sstate, _params) )
                {
                    
                    //
                    // Looks like the subproblem we extracted does not need splitting.
                    // Next one, please.
                    //
                    if( sstate.dotrace )
                    {
                        alglib.ap.trace(System.String.Format("> subproblem {0,8:d}P does not need integral or spatial branching, skipping\n", p.leafid));
                    }
                    front.entries.pop_transfer(ref e);
                    alglib.smp.ae_shared_pool_recycle(front.entrypool, ref e);
                    alglib.smp.ae_shared_pool_recycle(sstate.sppool, ref p);
                    continue;
                }
                front.frontsize = front.frontsize+1;
                alglib.smp.ae_shared_pool_recycle(sstate.sppool, ref p);
            }
            if( front.frontsize==0 )
            {
                if( sstate.dotrace )
                {
                    alglib.ap.trace("> B&B tree has no subproblems that can be split, stopping\n");
                }
                result = false;
                return result;
            }
            
            //
            // Done
            //
            front.rstate.stage = -1;
            return result;
        }


        /*************************************************************************
        Recomputes State.FFDual using current heap and front entries being processed.
        Works only with dynamic fronts.
        *************************************************************************/
        private static void frontrecomputedualbound(bbgdfront front,
            bbgdstate state,
            alglib.xparams _params)
        {
            bbgdfrontentry e = null;
            bbgdsubproblem p = null;
            int i = 0;

            alglib.ap.assert(front.frontmode==ftdynamic, "BBGD: 647012 failed");
            state.ffdual = math.maxrealnumber;
            if( state.hasprimalsolution )
            {
                state.ffdual = Math.Min(state.ffdual, state.fprim);
            }
            for(i=0; i<=front.frontsize-1; i++)
            {
                front.entries.get(i, ref e);
                alglib.ap.assert(e.parentsubproblem.hasdualsolution, "BBGD: 775356 failed");
                state.ffdual = Math.Min(state.ffdual, e.parentsubproblem.dualbound);
            }
            if( state.bbsubproblems.getlength()>0 )
            {
                growheap(state, _params);
                state.bbsubproblems.get(0, ref p);
                alglib.ap.assert(p.hasdualsolution, "BBGD: integrity check 810337 failed");
                state.ffdual = Math.Min(state.ffdual, p.dualbound);
            }
            if( (double)(state.ffdual)==(double)(math.maxrealnumber) )
            {
                state.ffdual = -math.maxrealnumber;
            }
        }


        /*************************************************************************
        Run front
        *************************************************************************/
        private static bool frontrun(bbgdfront front,
            bbgdstate state,
            alglib.xparams _params)
        {
            bool result = new bool();

            
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
            if( front.rstate.stage>=0 )
            {
            }
            else
            {
            }
            if( front.rstate.stage==0 )
            {
                goto lbl_0;
            }
            
            //
            // Routine body
            //
            alglib.ap.assert(front.frontstatus==stfrontreadytorun, "BBGD: integrity check 249321 failed");
            front.frontstatus = stfrontrunning;
        lbl_1:
            if( false )
            {
                goto lbl_2;
            }
            if( !frontruninternal(front, state, _params) )
            {
                goto lbl_2;
            }
            front.rstate.stage = 0;
            goto lbl_rcomm;
        lbl_0:
            goto lbl_1;
        lbl_2:
            result = false;
            return result;
            
            //
            // Saving state
            //
        lbl_rcomm:
            result = true;
            return result;
        }


        /*************************************************************************
        Run front (internal function), returns False when the front finished its
        job.
        *************************************************************************/
        private static bool frontruninternal(bbgdfront front,
            bbgdstate state,
            alglib.xparams _params)
        {
            bool result = new bool();
            bbgdfrontentry e = null;
            bbgdsubproblem p = null;
            bbgdfrontsubsolver subsolver = null;
            int i = 0;
            int j = 0;
            int jobscnt = 0;
            int waitingcnt = 0;
            bool bdummy = new bool();
            bool continuediving = new bool();
            bool handled = new bool();

            result = true;
            
            //
            // Front types for initial BBGD
            //
            if( front.frontmode==ftroot || front.frontmode==ftbasic )
            {
                alglib.ap.assert(front.frontsize>=1, "BBGD: 551121 failed");
                jobscnt = 0;
                for(i=0; i<=front.frontsize-1; i++)
                {
                    ablasf.igrowappendv(jobscnt+1, ref front.jobs, i, _params);
                    jobscnt = jobscnt+1;
                }
                System.Threading.Thread.VolatileWrite(ref front.flag, 0);
                frontparallelrunentries(front, 0, jobscnt, true, state, _params);
                result = front.flag!=0;
                
                //
                // If finished, set front status to stSolved or stTimeout
                //
                if( !result )
                {
                    front.frontstatus = stsolved;
                    for(i=0; i<=front.frontsize-1; i++)
                    {
                        front.entries.get(i, ref e);
                        alglib.ap.assert(e.entrystatus==stsolved || e.entrystatus==sttimeout, "BBGD: integrity check 282324 failed");
                        if( e.entrystatus==sttimeout )
                        {
                            front.frontstatus = sttimeout;
                        }
                    }
                }
                if( (state.softmaxnodes>0 && state.hasprimalsolution) && state.repntreenodes>=state.softmaxnodes )
                {
                    if( state.dotrace )
                    {
                        alglib.ap.trace("> soft max nodes triggered (stop if have primal solution), stopping\n");
                    }
                    front.frontstatus = sttimeout;
                }
                if( state.hardmaxnodes>0 && state.repntreenodes>=state.hardmaxnodes )
                {
                    if( state.dotrace )
                    {
                        alglib.ap.trace("> hard max nodes triggered (stop independently of primal solution status), stopping\n");
                    }
                    front.frontstatus = sttimeout;
                }
                if( (state.maxprimalcandidates>0 && state.hasprimalsolution) && state.repnprimalcandidates>=state.maxprimalcandidates )
                {
                    if( state.dotrace )
                    {
                        alglib.ap.trace(System.String.Format("> maximum number of primal candidates tried (more than {0,0:d}), stopping\n", state.maxprimalcandidates));
                    }
                    front.frontstatus = sttimeout;
                }
                return result;
            }
            
            //
            // Dynamic front
            //
            if( front.frontmode==ftdynamic )
            {
                
                //
                // Phase 0: integrity check. At the entry the front must have only stReadyToRun or stWaitingForRComm
                //          entries (or be empty). All subsolvers, if present, must also be stWaitingForRComm
                //
                for(i=0; i<=front.frontsize-1; i++)
                {
                    front.entries.get(i, ref e);
                    waitingcnt = 0;
                    for(j=0; j<=e.subsolvers.getlength()-1; j++)
                    {
                        e.subsolvers.get(j, ref subsolver);
                        alglib.ap.assert(subsolver.subsolverstatus==stwaitingforrcomm || subsolver.subsolverstatus==streadytorun, "BBGD: 713006 failed");
                        if( subsolver.subsolverstatus==stwaitingforrcomm )
                        {
                            waitingcnt = waitingcnt+1;
                        }
                    }
                    alglib.ap.assert((e.entrystatus==streadytorun && waitingcnt==0) || (e.entrystatus==stwaitingforrcomm && waitingcnt>0), "BBGD: 665242 failed");
                }
                
                //
                // Internal loop: repeat until front size at the end of the loop is non-zero
                //
                do
                {
                    
                    //
                    // Append entries from the BB heap until the front is full (or the heap is empty).
                    // If the front is empty after this phase it means that we are done.
                    //
                    while( state.bbsubproblems.getlength()>0 && front.frontsize<state.bbgdgroupsize )
                    {
                        growheapandpoptop(state, _params);
                        state.bbsubproblems.pop_transfer(ref p);
                        alglib.ap.assert(p.hasdualsolution, "BBGD: integrity check 687259 failed");
                        if( state.hasprimalsolution && (double)(p.dualbound)>=(double)(state.fprim-state.pdgap*apserv.rmaxabs2(state.fprim, 1, _params)) )
                        {
                            if( state.dotrace )
                            {
                                alglib.ap.trace(System.String.Format("> fathomed {0,8:d}P during tree search (p.bestfdual={1,0:E2}, p.dual_bound={2,0:E2}, global.fprim={3,0:E2})\n", p.leafid, p.bestfdual, p.dualbound, state.fprim));
                            }
                            alglib.smp.ae_shared_pool_recycle(state.sppool, ref p);
                            continue;
                        }
                        alglib.smp.ae_shared_pool_retrieve(front.entrypool, ref e);
                        if( !entryprepareleafs(e, front, p, state, _params) )
                        {
                            
                            //
                            // Looks like the subproblem we extracted does not need splitting.
                            // Next one, please.
                            //
                            if( state.dotrace )
                            {
                                alglib.ap.trace(System.String.Format("> subproblem {0,8:d}P does not need integral or spatial branching, skipping\n", p.leafid));
                            }
                            alglib.smp.ae_shared_pool_recycle(front.entrypool, ref e);
                            alglib.smp.ae_shared_pool_recycle(state.sppool, ref p);
                            continue;
                        }
                        front.entries.append(e);
                        front.frontsize = front.frontsize+1;
                        alglib.smp.ae_shared_pool_recycle(state.sppool, ref p);
                    }
                    frontrecomputedualbound(front, state, _params);
                    if( front.frontsize==0 )
                    {
                        if( state.dotrace )
                        {
                            alglib.ap.trace("> B&B tree has no subproblems that can be split, stopping\n");
                        }
                        result = false;
                        front.frontstatus = stsolved;
                        return result;
                    }
                    
                    //
                    // Activate subsolvers in each entry until we hit MaxSubsolvers limit
                    //
                    for(i=0; i<=front.frontsize-1; i++)
                    {
                        front.entries.get(i, ref e);
                        while( e.spqueue.getlength()>0 && e.subsolvers.getlength()<e.maxsubsolvers )
                        {
                            e.spqueue.pop_transfer(ref p);
                            alglib.smp.ae_shared_pool_retrieve(state.subsolverspool, ref subsolver);
                            entrypreparesubsolver(state, front, e, p, false, subsolver, _params);
                            e.subsolvers.append(subsolver);
                            alglib.smp.ae_shared_pool_recycle(state.sppool, ref p);
                        }
                    }
                    
                    //
                    // Parallel call to FrontRunKthEntry()
                    //
                    jobscnt = 0;
                    for(i=0; i<=front.frontsize-1; i++)
                    {
                        front.entries.get(i, ref e);
                        for(j=0; j<=e.subsolvers.getlength()-1; j++)
                        {
                            ablasf.igrowappendv(jobscnt+1, ref front.jobs, i+j*front.frontsize, _params);
                            jobscnt = jobscnt+1;
                        }
                    }
                    frontparallelrunentries(front, 0, jobscnt, true, state, _params);
                    
                    //
                    // Analyze solution: signal timeout, check that all entries are stSolved or stWaitingForRComm,
                    // first-phase process solved entries (update global stats).
                    //
                    for(i=0; i<=front.frontsize-1; i++)
                    {
                        front.entries.get(i, ref e);
                        e.entrystatus = apserv.icase2(e.spqueue.getlength()>0, streadytorun, stsolved, _params);
                        j = 0;
                        while( j<e.subsolvers.getlength() )
                        {
                            e.subsolvers.get(j, ref subsolver);
                            if( subsolver.subsolverstatus==sttimeout )
                            {
                                e.entrystatus = sttimeout;
                                front.frontstatus = sttimeout;
                                result = false;
                                return result;
                            }
                            if( subsolver.subsolverstatus==stwaitingforrcomm )
                            {
                                e.entrystatus = stwaitingforrcomm;
                                j = j+1;
                                continue;
                            }
                            alglib.ap.assert(subsolver.subsolverstatus==stsolved, "BBGD: integrity check 840056 failed");
                            if( j!=e.subsolvers.getlength()-1 )
                            {
                                e.subsolvers.swap(j, e.subsolvers.getlength()-1);
                            }
                            e.subsolvers.pop_transfer(ref subsolver);
                            alglib.smp.ae_shared_pool_recycle(state.subsolverspool, ref subsolver);
                        }
                        if( e.entrystatus==streadytorun || e.entrystatus==stwaitingforrcomm )
                        {
                            continue;
                        }
                        alglib.ap.assert(e.entrystatus==stsolved, "BBGD: integrity check 670157 failed");
                        entryupdateglobalstats(e, state, _params);
                    }
                    if( state.hasprimalsolution && state.repnnodesbeforefeasibility<0 )
                    {
                        state.repnnodesbeforefeasibility = state.repntreenodes;
                    }
                    
                    //
                    // Push solutions to the heap, check stopping criteria for PDGap, recompute dual bound.
                    //
                    // After this phase is done either:
                    // a) the front is empty (in which case we repeat the loop), or
                    // b) there are entries, with all of them being stWaitingForRComm or stReadyToRun,
                    //    in which case we exit in order for RComm request to be processed by the caller
                    //
                    i = 0;
                    while( i<front.frontsize )
                    {
                        
                        //
                        // Analyze I-th entry, skip if ready to run or waiting for RComm
                        //
                        front.entries.get(i, ref e);
                        if( e.entrystatus==streadytorun || e.entrystatus==stwaitingforrcomm )
                        {
                            i = i+1;
                            continue;
                        }
                        alglib.ap.assert(e.entrystatus==stsolved, "BBGD: integrity check 670158 failed");
                        
                        //
                        // Process entry by either pushing both leaves to the heap or by perfoming
                        // a diving (the better leaf is continued, the worse one is pushed to the heap)
                        //
                        continuediving = false;
                        handled = false;
                        if( state.dodiving==divealways )
                        {
                            continuediving = entrytrypushanddive(e, front, state, _params);
                            handled = true;
                        }
                        if( state.dodiving==diveuntilprimal && !state.hasprimalsolution )
                        {
                            continuediving = entrytrypushanddive(e, front, state, _params);
                            handled = true;
                        }
                        if( !handled )
                        {
                            entrypushsolution(e, state, ref bdummy, _params);
                        }
                        if( !continuediving )
                        {
                            
                            //
                            // No diving, push the solution and remove the entry
                            //
                            if( i!=front.frontsize-1 )
                            {
                                front.entries.swap(i, front.frontsize-1);
                            }
                            front.entries.pop_transfer(ref e);
                            front.frontsize = front.frontsize-1;
                            alglib.smp.ae_shared_pool_recycle(front.entrypool, ref e);
                        }
                    }
                    frontrecomputedualbound(front, state, _params);
                    if( front.frontsize==0 && state.bbsubproblems.getlength()==0 )
                    {
                        if( state.dotrace )
                        {
                            alglib.ap.trace("> B&B tree has no subproblems that can be split, stopping\n");
                        }
                        result = false;
                        front.frontstatus = stsolved;
                        return result;
                    }
                    if( state.dotrace )
                    {
                        alglib.ap.trace(System.String.Format(">> global dual bound was recomputed as {0,0:E12}, global primal bound is {1,0:E12}\n", state.ffdual, state.fprim));
                    }
                    if( state.hasprimalsolution && (double)(state.ffdual)>=(double)(state.fprim-state.pdgap*apserv.rmaxabs2(state.fprim, 1, _params)) )
                    {
                        if( state.dotrace )
                        {
                            alglib.ap.trace(System.String.Format("> relative duality gap decreased below {0,0:E2}, stopping\n", state.pdgap));
                        }
                        result = false;
                        front.frontstatus = stsolved;
                        return result;
                    }
                    if( (state.softmaxnodes>0 && state.hasprimalsolution) && state.repntreenodes>=state.softmaxnodes )
                    {
                        if( state.dotrace )
                        {
                            alglib.ap.trace("> soft max nodes triggered (stop if have primal solution), stopping\n");
                        }
                        result = false;
                        front.frontstatus = sttimeout;
                        return result;
                    }
                    if( state.hardmaxnodes>0 && state.repntreenodes>=state.hardmaxnodes )
                    {
                        if( state.dotrace )
                        {
                            alglib.ap.trace("> hard max nodes triggered (stop independently of primal solution status), stopping\n");
                        }
                        result = false;
                        front.frontstatus = sttimeout;
                        return result;
                    }
                    if( (state.maxprimalcandidates>0 && state.hasprimalsolution) && state.repnprimalcandidates>=state.maxprimalcandidates )
                    {
                        if( state.dotrace )
                        {
                            alglib.ap.trace(System.String.Format("> maximum number of primal candidates tried (more than {0,0:d}), stopping\n", state.maxprimalcandidates));
                        }
                        result = false;
                        front.frontstatus = sttimeout;
                        return result;
                    }
                    
                    //
                    // Count entries that wait for RComm; exit if RComm is needed. Continue iteration if all entries are stReadyToRun,
                    // we will generate RComm requests at the next round.
                    //
                    waitingcnt = 0;
                    for(i=0; i<=front.frontsize-1; i++)
                    {
                        front.entries.get(i, ref e);
                        if( e.entrystatus==stwaitingforrcomm )
                        {
                            waitingcnt = waitingcnt+1;
                        }
                    }
                }
                while( waitingcnt<=0 );
                return result;
            }
            
            //
            // Unexpected front type
            //
            alglib.ap.assert(false, "BBGD: 592133 failed (unexpected front type)");
            return result;
        }


        /*************************************************************************
        Call EntryRun in parallel.

        Job0 and Job1 denote a range [Job0,Job1) in Front.Jobs to process,
        IsRootCall must be True when called (used to distinguish recursive calls
        from root ones).
        *************************************************************************/
        private static void frontparallelrunentries(bbgdfront front,
            int job0,
            int job1,
            bool isrootcall,
            bbgdstate state,
            alglib.xparams _params)
        {
            int jobmid = 0;

            alglib.ap.assert(job1>job0, "BBGD: 551121 failed");
            if( job1-job0==1 )
            {
                frontrunkthentryjthsubsolver(front, front.jobs[job0]%front.frontsize, front.jobs[job0]/front.frontsize, state, _params);
                return;
            }
            if( isrootcall && !state.forceserial )
            {
                if( _trypexec_frontparallelrunentries(front,job0,job1,isrootcall,state, _params) )
                {
                    return;
                }
            }
            jobmid = job0+(job1-job0)/2;
            frontparallelrunentries(front, job0, jobmid, false, state, _params);
            frontparallelrunentries(front, jobmid, job1, false, state, _params);
        }


        /*************************************************************************
        Serial stub for GPL edition.
        *************************************************************************/
        public static bool _trypexec_frontparallelrunentries(bbgdfront front,
            int job0,
            int job1,
            bool isrootcall,
            bbgdstate state, alglib.xparams _params)
        {
            return false;
        }


        /*************************************************************************
        Run k-th entry of the front.

        For Front.FrontMode=ftBasic or ftRoot:

            If the entry is in stReadyToRun or stWaitingForRComm state and
            EntryRun() returned True, sets Front.Flag to 1. Does not change
            it otherwise.
        *************************************************************************/
        private static void frontrunkthentryjthsubsolver(bbgdfront front,
            int k,
            int j,
            bbgdstate state,
            alglib.xparams _params)
        {
            bbgdfrontentry e = null;
            bbgdfrontsubsolver s = null;

            front.entries.get(k, ref e);
            if( front.frontmode==ftroot || front.frontmode==ftbasic )
            {
                if( (e.entrystatus==streadytorun || e.entrystatus==stwaitingforrcomm) && entryrunnondynamicfront(e, state, _params) )
                {
                    System.Threading.Thread.VolatileWrite(ref front.flag, 1);
                }
                return;
            }
            if( front.frontmode==ftdynamic )
            {
                e.subsolvers.get(j, ref s);
                alglib.ap.assert(s.subsolverstatus==streadytorun || s.subsolverstatus==stwaitingforrcomm, "BBGD: 979201 failed");
                subsolverrun(state, front, e, s, _params);
                return;
            }
        }


        /*************************************************************************
        Pack requests coming from the front elements into the grand RComm request
        *************************************************************************/
        private static void frontpackqueries(bbgdfront front,
            bbgdstate state,
            ref int requesttype,
            ref int querysize,
            ref int queryfuncs,
            ref int queryvars,
            ref int querydim,
            ref int queryformulasize,
            ref double[] querydata,
            alglib.xparams _params)
        {
            bbgdfrontentry e = null;
            bbgdfrontsubsolver subsolver = null;
            int i = 0;
            int j = 0;

            for(i=0; i<=front.frontsize-1; i++)
            {
                front.entries.get(i, ref e);
                alglib.ap.assert(((e.entrystatus==streadytorun || e.entrystatus==stwaitingforrcomm) || e.entrystatus==stsolved) || e.entrystatus==sttimeout, "BBGD: integrity check 304325 failed");
                if( e.entrystatus==stwaitingforrcomm )
                {
                    if( front.frontmode==ftroot || front.frontmode==ftbasic )
                    {
                        reduceandappendrequestto(e.commonsubsolver.nlpsubsolver, state, ref requesttype, ref querysize, ref queryfuncs, ref queryvars, ref querydim, ref queryformulasize, ref querydata, _params);
                        continue;
                    }
                    if( front.frontmode==ftdynamic )
                    {
                        for(j=0; j<=e.subsolvers.getlength()-1; j++)
                        {
                            e.subsolvers.get(j, ref subsolver);
                            alglib.ap.assert(subsolver.subsolverstatus==stwaitingforrcomm, "BBGD: 021235 failed");
                            reduceandappendrequestto(subsolver.nlpsubsolver, state, ref requesttype, ref querysize, ref queryfuncs, ref queryvars, ref querydim, ref queryformulasize, ref querydata, _params);
                        }
                        continue;
                    }
                    alglib.ap.assert(false, "BBGD: 026236 failed");
                }
            }
        }


        /*************************************************************************
        Unpack RComm replies and distribute them to front entries
        *************************************************************************/
        private static void frontunpackreplies(bbgdstate state,
            int requesttype,
            int querysize,
            int queryfuncs,
            int queryvars,
            int querydim,
            int queryformulasize,
            double[] replyfi,
            double[] replydj,
            sparse.sparsematrix replysj,
            bbgdfront front,
            alglib.xparams _params)
        {
            int i = 0;
            int j = 0;
            int offs = 0;
            bbgdfrontentry e = null;
            bbgdfrontsubsolver subsolver = null;

            offs = 0;
            for(i=0; i<=front.frontsize-1; i++)
            {
                front.entries.get(i, ref e);
                alglib.ap.assert(((e.entrystatus==streadytorun || e.entrystatus==stwaitingforrcomm) || e.entrystatus==stsolved) || e.entrystatus==sttimeout, "BBGD: integrity check 304325 failed");
                if( e.entrystatus==stwaitingforrcomm )
                {
                    if( front.frontmode==ftroot || front.frontmode==ftbasic )
                    {
                        extractextendandforwardreplyto(state, requesttype, querysize, queryfuncs, queryvars, querydim, queryformulasize, replyfi, replydj, replysj, ref offs, e.commonsubsolver.nlpsubsolver, _params);
                        continue;
                    }
                    if( front.frontmode==ftdynamic )
                    {
                        for(j=0; j<=e.subsolvers.getlength()-1; j++)
                        {
                            e.subsolvers.get(j, ref subsolver);
                            alglib.ap.assert(subsolver.subsolverstatus==stwaitingforrcomm, "BBGD: 070237 failed");
                            extractextendandforwardreplyto(state, requesttype, querysize, queryfuncs, queryvars, querydim, queryformulasize, replyfi, replydj, replysj, ref offs, subsolver.nlpsubsolver, _params);
                        }
                        continue;
                    }
                    alglib.ap.assert(false, "BBGD: 071236 failed");
                }
            }
            alglib.ap.assert(offs==querysize, "BBGD: integrity check 606236 failed");
        }


        /*************************************************************************
        Having fully processed front, pushes its solution to the B&B tree,
        initializing (when working with a root front) or updating (when working
        with subsequent fronts) global primal/dual bounds as well as best solution so far.
        *************************************************************************/
        private static void frontpushsolution(bbgdfront front,
            bbgdstate state,
            alglib.xparams _params)
        {
            bbgdfrontentry e = null;
            bbgdsubproblem p = null;
            int i = 0;
            int cnt = 0;
            int offs = 0;
            int appendlen = 0;

            alglib.ap.assert(front.frontstatus==stsolved && (front.frontmode==ftroot || front.frontmode==ftbasic), "BBGD: integrity check 332315 failed");
            
            //
            // First, update global information (primal bound, pseudocosts, subproblem counts)
            //
            for(i=0; i<=front.frontsize-1; i++)
            {
                front.entries.get(i, ref e);
                alglib.ap.assert(e.entrystatus==stsolved, "BBGD: integrity check 259949 failed");
                entryupdateglobalstats(e, state, _params);
            }
            if( state.hasprimalsolution && state.repnnodesbeforefeasibility<0 )
            {
                state.repnnodesbeforefeasibility = state.repntreenodes;
            }
            
            //
            // Then push solutions to the end of the bbSubproblems array and resort last AppendLen
            // elements using heapsort.
            //
            front.popmostrecent = false;
            offs = state.bbsubproblems.getlength();
            for(i=0; i<=front.frontsize-1; i++)
            {
                front.entries.get(i, ref e);
                entrypushsolution(e, state, ref front.popmostrecent, _params);
            }
            appendlen = state.bbsubproblems.getlength()-offs;
            state.bbsubproblemsrecentlyadded = appendlen;
            subproblemheapgrow(state.bbsubproblems, offs, 0, appendlen, _params);
            while( appendlen>0 )
            {
                appendlen = subproblemheappoptop(state.bbsubproblems, offs, appendlen, _params);
            }
            
            //
            // Update dual bound, print report
            //
            state.ffdual = math.maxrealnumber;
            cnt = state.bbsubproblems.getlength();
            for(i=0; i<=cnt-1; i++)
            {
                state.bbsubproblems.get(i, ref p);
                alglib.ap.assert(p.hasdualsolution, "BBGD: integrity check 613337 failed");
                state.ffdual = Math.Min(state.ffdual, p.dualbound);
            }
            if( state.dotrace )
            {
                alglib.ap.trace(System.String.Format(">> global dual bound was recomputed as {0,0:E12}, global primal bound is {1,0:E12}\n", state.ffdual, state.fprim));
            }
        }


        /*************************************************************************
        Prepare subsolver for the front entry. Sets timers if timeout was specified.
        *************************************************************************/
        private static void entryprepareroot(bbgdfrontentry entry,
            bbgdfront front,
            bbgdsubproblem rootsubproblem,
            bbgdstate state,
            alglib.xparams _params)
        {
            subproblemcopyasunsolved(rootsubproblem, rootsubproblem.leafid, entry.parentsubproblem, _params);
            subproblemcopyasunsolved(rootsubproblem, rootsubproblem.leafid, entry.bestsubproblem0, _params);
            subproblemcopyasunsolved(rootsubproblem, rootsubproblem.leafid, entry.bestsubproblem1, _params);
            entrypreparex(entry, front, state, true, _params);
        }


        /*************************************************************************
        Prepare subsolver for the front entry. Sets timers if timeout was specified.
        *************************************************************************/
        private static bool entryprepareleafs(bbgdfrontentry entry,
            bbgdfront front,
            bbgdsubproblem s,
            bbgdstate state,
            alglib.xparams _params)
        {
            bool result = new bool();
            bool done = new bool();
            int n = 0;
            int i = 0;
            int branchidx = 0;
            double v = 0;
            double vcostup = 0;
            double vcostdown = 0;
            double vscore = 0;
            double vmid = 0;
            double branchscore = 0;
            double maxinterr = 0;
            int leaf0 = 0;
            int leaf1 = 0;

            done = false;
            n = s.n;
            alglib.ap.assert(s.hasdualsolution, "BBGD: integrity check 391031 failed");
            
            //
            // Our first attempt to split: split subproblems with significant integrality errors.
            //
            if( !done && !s.bestdualisintfeas )
            {
                
                //
                // Select the most infeasible variable to branch on.
                //
                maxinterr = 0;
                for(i=0; i<=n-1; i++)
                {
                    if( state.isintegral[i] )
                    {
                        v = s.bestxdual[i]-(int)Math.Floor(s.bestxdual[i]);
                        maxinterr = Math.Max(maxinterr, Math.Min(v, 1-v));
                    }
                }
                alglib.ap.assert((double)(alphaint)<=(double)(0.95), "BBGD: integrity check 943151 failed");
                branchidx = -1;
                branchscore = 0;
                for(i=0; i<=n-1; i++)
                {
                    
                    //
                    // Skip non-integral variables and variables with integrality error below fraction of MaxIntErr
                    //
                    if( !state.isintegral[i] )
                    {
                        continue;
                    }
                    v = s.bestxdual[i]-(int)Math.Floor(s.bestxdual[i]);
                    if( (double)(Math.Min(v, 1-v))<(double)(alphaint*maxinterr) )
                    {
                        continue;
                    }
                    
                    //
                    // Choose variable to branch
                    //
                    vscore = Math.Min(v, 1-v);
                    if( state.usepseudocosts )
                    {
                        vcostup = state.globalpseudocostup;
                        vcostdown = state.globalpseudocostdown;
                        if( state.pseudocostscntup[i]>=state.minbranchreliability )
                        {
                            vcostup = state.pseudocostsup[i];
                        }
                        if( state.pseudocostscntdown[i]>=state.minbranchreliability )
                        {
                            vcostdown = state.pseudocostsdown[i];
                        }
                        vscore = (1-state.pseudocostmu)*Math.Min(v*vcostdown, (1-v)*vcostup)+state.pseudocostmu*Math.Max(v*vcostdown, (1-v)*vcostup);
                    }
                    vscore = Math.Max(vscore, 0);
                    
                    //
                    // Update best candidate
                    //
                    if( branchidx<0 || (double)(vscore)>(double)(branchscore) )
                    {
                        branchidx = i;
                        branchscore = vscore;
                    }
                }
                alglib.ap.assert(branchidx>=0, "BBGD: integrity check 982152 failed");
                
                //
                // Append new subproblems to the group
                //
                if( state.dotrace )
                {
                    alglib.ap.trace(System.String.Format("> branching {0,8:d}P on var {1,8:d}:", s.leafid, branchidx));
                }
                leaf0 = apserv.weakatomicfetchadd(ref state.nextleafid, 1, _params);
                leaf1 = apserv.weakatomicfetchadd(ref state.nextleafid, 1, _params);
                subproblemcopyasunsolved(s, leaf0, entry.bestsubproblem0, _params);
                subproblemcopyasunsolved(s, leaf1, entry.bestsubproblem1, _params);
                entry.bestsubproblem0.leafidx = 0;
                entry.bestsubproblem1.leafidx = 1;
                entry.bestsubproblem0.parentfdual = s.bestfdual;
                entry.bestsubproblem1.parentfdual = s.bestfdual;
                ablasf.rcopyv(n, s.bestxdual, entry.bestsubproblem0.x0, _params);
                ablasf.rcopyv(n, s.bestxdual, entry.bestsubproblem1.x0, _params);
                entry.bestsubproblem0.x0[branchidx] = (int)Math.Floor(entry.bestsubproblem0.x0[branchidx]);
                entry.bestsubproblem0.bndu[branchidx] = entry.bestsubproblem0.x0[branchidx];
                if( state.dotrace )
                {
                    alglib.ap.trace(System.String.Format(" creating {0,8:d}P (x<={1,0:E2})", entry.bestsubproblem0.leafid, entry.bestsubproblem0.bndu[branchidx]));
                }
                entry.bestsubproblem1.x0[branchidx] = (int)Math.Ceiling(entry.bestsubproblem1.x0[branchidx]);
                entry.bestsubproblem1.bndl[branchidx] = entry.bestsubproblem1.x0[branchidx];
                if( state.dotrace )
                {
                    alglib.ap.trace(System.String.Format(" and {0,8:d}P (x>={1,0:E2})", entry.bestsubproblem1.leafid, entry.bestsubproblem1.bndl[branchidx]));
                }
                if( state.dotrace )
                {
                    alglib.ap.trace("\n");
                }
                entry.branchvar = branchidx;
                entry.branchval = s.bestxdual[branchidx];
                
                //
                // Splitting on integer variable is done.
                //
                done = true;
            }
            
            //
            // Another option spatial splitting between worst and dual solutions (used for non-convex problems with multistarts)
            //
            if( (!done && s.hasdualsolution) && (double)(Math.Abs(s.bestfdual-s.worstfdual))>(double)(state.pdgap*apserv.rmaxabs2(state.ffdual, 1, _params)) )
            {
                
                //
                // Select variable with the highest distance between worst and dual solutions
                //
                branchidx = -1;
                branchscore = 0;
                vmid = 0;
                for(i=0; i<=n-1; i++)
                {
                    vscore = Math.Abs(s.bestxdual[i]-s.worstxdual[i])/state.s[i];
                    if( branchidx<0 || (double)(vscore)>(double)(branchscore) )
                    {
                        branchidx = i;
                        branchscore = vscore;
                        vmid = 0.5*(s.bestxdual[i]+s.worstxdual[i]);
                    }
                }
                alglib.ap.assert(branchidx>=0, "BBGD: integrity check 137104 failed");
                if( (double)(branchscore)>(double)(0) )
                {
                    
                    //
                    // Append new subproblems to the group
                    //
                    if( state.dotrace )
                    {
                        alglib.ap.trace(System.String.Format("> spatially branching {0,8:d}P on var {1,8:d} (|x_worst-x_dual|={2,0:E2}):", s.leafid, branchidx, Math.Abs(s.bestxdual[branchidx]-s.worstxdual[branchidx])));
                    }
                    leaf0 = apserv.weakatomicfetchadd(ref state.nextleafid, 1, _params);
                    leaf1 = apserv.weakatomicfetchadd(ref state.nextleafid, 1, _params);
                    subproblemcopyasunsolved(s, leaf0, entry.bestsubproblem0, _params);
                    subproblemcopyasunsolved(s, leaf1, entry.bestsubproblem1, _params);
                    entry.bestsubproblem0.leafidx = 0;
                    entry.bestsubproblem1.leafidx = 1;
                    entry.bestsubproblem0.parentfdual = s.bestfdual;
                    entry.bestsubproblem1.parentfdual = s.bestfdual;
                    if( (double)(s.bestxdual[branchidx])<=(double)(vmid) )
                    {
                        ablasf.rcopyv(n, s.bestxdual, entry.bestsubproblem0.x0, _params);
                    }
                    else
                    {
                        ablasf.rcopyv(n, s.worstxdual, entry.bestsubproblem0.x0, _params);
                    }
                    entry.bestsubproblem0.bndu[branchidx] = apserv.rcase2(state.isintegral[i], (int)Math.Floor(vmid), vmid, _params);
                    if( state.dotrace )
                    {
                        alglib.ap.trace(System.String.Format(" creating {0,8:d}P (x<={1,0:E2})", entry.bestsubproblem0.leafid, entry.bestsubproblem0.bndu[branchidx]));
                    }
                    if( (double)(s.bestxdual[branchidx])>=(double)(vmid) )
                    {
                        ablasf.rcopyv(n, s.bestxdual, entry.bestsubproblem1.x0, _params);
                    }
                    else
                    {
                        ablasf.rcopyv(n, s.worstxdual, entry.bestsubproblem1.x0, _params);
                    }
                    entry.bestsubproblem1.bndl[branchidx] = apserv.rcase2(state.isintegral[i], (int)Math.Floor(vmid)+1, vmid, _params);
                    if( state.dotrace )
                    {
                        alglib.ap.trace(System.String.Format(" and {0,8:d}P (x>={1,0:E2})", entry.bestsubproblem1.leafid, entry.bestsubproblem1.bndl[branchidx]));
                    }
                    entry.branchvar = branchidx;
                    entry.branchval = vmid;
                    
                    //
                    // Splitting on integer variable is done.
                    //
                    done = true;
                }
            }
            
            //
            // Done or not done
            //
            result = done;
            if( !done )
            {
                return result;
            }
            subproblemcopy(s, s.leafid, entry.parentsubproblem, _params);
            entrypreparex(entry, front, state, false, _params);
            return result;
        }


        /*************************************************************************
        Prepare the front entry, generates subproblem queue.
        Sets timers if timeout was specified.
        *************************************************************************/
        private static void entrypreparex(bbgdfrontentry entry,
            bbgdfront front,
            bbgdstate state,
            bool isroot,
            alglib.xparams _params)
        {
            int restartidx = 0;
            int leafidx = 0;
            bbgdsubproblem subproblem = null;
            bbgdfrontsubsolver subsolver = null;

            alglib.ap.assert((front.frontmode==ftroot || front.frontmode==ftbasic) || front.frontmode==ftdynamic, "BBGD: 776046 failed");
            entry.isroot = isroot;
            entry.entrystatus = streadytorun;
            entry.entrylock = 0;
            if( front.frontmode==ftroot || front.frontmode==ftbasic )
            {
                entry.rstate.ia = new int[4+1];
                entry.rstate.ba = new bool[1+1];
                entry.rstate.stage = -1;
            }
            entry.hastimeout = state.timeout>0;
            if( entry.hastimeout )
            {
                entry.timeout = (int)Math.Round(state.timeout-apserv.stimergetmsrunning(state.timerglobal, _params));
                apserv.stimerinit(entry.timerlocal, _params);
                apserv.stimerstart(entry.timerlocal, _params);
            }
            
            //
            // Generate subproblem queue
            //
            while( entry.spqueue.getlength()>0 )
            {
                entry.spqueue.pop_transfer(ref subproblem);
                alglib.smp.ae_shared_pool_recycle(state.sppool, ref subproblem);
            }
            for(leafidx=0; leafidx<=apserv.icase2(isroot, 0, 1, _params); leafidx++)
            {
                for(restartidx=0; restartidx<=state.nmultistarts-1; restartidx++)
                {
                    alglib.smp.ae_shared_pool_retrieve(state.sppool, ref subproblem);
                    if( leafidx==0 )
                    {
                        subproblemcopy(entry.bestsubproblem0, entry.bestsubproblem0.leafid, subproblem, _params);
                    }
                    else
                    {
                        subproblemcopy(entry.bestsubproblem1, entry.bestsubproblem1.leafid, subproblem, _params);
                    }
                    if( restartidx>0 )
                    {
                        subproblemrandomizex0(subproblem, state, _params);
                    }
                    entry.spqueue.append(subproblem);
                }
            }
            entry.maxsubsolvers = Math.Min(entry.spqueue.getlength(), state.maxsubsolvers);
            
            //
            // Clear subsolver list
            //
            while( entry.subsolvers.getlength()>0 )
            {
                entry.subsolvers.pop_transfer(ref subsolver);
                alglib.smp.ae_shared_pool_recycle(state.subsolverspool, ref subsolver);
            }
        }


        /*************************************************************************
        Prepare subsolver for the front entry and specific subproblem to solve.
        *************************************************************************/
        private static void entrypreparesubsolver(bbgdstate state,
            bbgdfront front,
            bbgdfrontentry entry,
            bbgdsubproblem subproblem,
            bool isroot,
            bbgdfrontsubsolver subsolver,
            alglib.xparams _params)
        {
            alglib.ap.assert(front.frontmode==ftdynamic, "BBGD: 415212 failed");
            subsolver.rstate.ia = new int[1+1];
            subsolver.rstate.ba = new bool[1+1];
            subsolver.rstate.stage = -1;
            subproblemcopy(subproblem, subproblem.leafid, subsolver.subproblem, _params);
            subsolver.subsolverstatus = streadytorun;
        }


        /*************************************************************************
        Run subproblem solver, prepare RComm fields
        *************************************************************************/
        private static bool entryrunnondynamicfront(bbgdfrontentry entry,
            bbgdstate state,
            alglib.xparams _params)
        {
            bool result = new bool();
            int n = 0;
            int i = 0;
            int leafidx = 0;
            int restartidx = 0;
            int terminationtype = 0;
            bool uselock = new bool();
            bool done = new bool();

            
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
            if( entry.rstate.stage>=0 )
            {
                n = entry.rstate.ia[0];
                i = entry.rstate.ia[1];
                leafidx = entry.rstate.ia[2];
                restartidx = entry.rstate.ia[3];
                terminationtype = entry.rstate.ia[4];
                uselock = entry.rstate.ba[0];
                done = entry.rstate.ba[1];
            }
            else
            {
                n = -909;
                i = 81;
                leafidx = 255;
                restartidx = 74;
                terminationtype = -788;
                uselock = true;
                done = true;
            }
            if( entry.rstate.stage==0 )
            {
                goto lbl_0;
            }
            
            //
            // Routine body
            //
            
            //
            // Init
            //
            uselock = false;
            n = entry.parentsubproblem.n;
            alglib.ap.assert(entry.entrystatus==streadytorun, "BBGD: integrity check 613322 failed");
            
            //
            // Handle various objective types
            //
            done = false;
            if( state.objtype==1 && state.nnlc==0 )
            {
                solveqpnode(entry, entry.commonsubsolver, state, entry.parentsubproblem.x0, entry.bestsubproblem0.bndl, entry.bestsubproblem0.bndu, entry.bestsubproblem0, uselock, _params);
                if( !entry.isroot )
                {
                    solveqpnode(entry, entry.commonsubsolver, state, entry.parentsubproblem.x0, entry.bestsubproblem1.bndl, entry.bestsubproblem1.bndu, entry.bestsubproblem1, uselock, _params);
                }
                if( entry.hastimeout && (double)(apserv.stimergetmsrunning(entry.timerlocal, _params))>(double)(entry.timeout) )
                {
                    entry.entrystatus = sttimeout;
                    result = false;
                    return result;
                }
                entry.entrystatus = stsolved;
                done = true;
            }
            if( done )
            {
                goto lbl_1;
            }
            
            //
            // Generic NLP subproblem is solved.
            //
            minnlc.minnlccreatebuf(entry.parentsubproblem.n, entry.parentsubproblem.x0, entry.commonsubsolver.nlpsubsolver, _params);
            minnlc.minnlcsetscale(entry.commonsubsolver.nlpsubsolver, state.s, _params);
            minnlc.minnlcsetlc2(entry.commonsubsolver.nlpsubsolver, state.rawa, state.rawal, state.rawau, state.lccnt, _params);
            minnlc.minnlcsetnlc2(entry.commonsubsolver.nlpsubsolver, state.nl, state.nu, state.nnlc, _params);
            minnlc.minnlcsetprotocolv2s(entry.commonsubsolver.nlpsubsolver, _params);
            if( !entry.isroot )
            {
                minnlc.minnlcsetcond3(entry.commonsubsolver.nlpsubsolver, state.epsf, state.epsx, state.nonrootmaxitsconst+state.nonrootmaxitslin*entry.parentsubproblem.n, _params);
                minnlc.minnlcsetfsqpadditsforctol(entry.commonsubsolver.nlpsubsolver, state.nonrootadditsforfeasibility, state.ctol, _params);
            }
            leafidx = 0;
        lbl_3:
            if( leafidx>apserv.icase2(entry.isroot, 0, 1, _params) )
            {
                goto lbl_5;
            }
            restartidx = 0;
        lbl_6:
            if( restartidx>state.nmultistarts-1 )
            {
                goto lbl_8;
            }
            
            //
            // Setup box constraints and best subproblem so far
            //
            if( leafidx==0 )
            {
                minnlc.minnlcsetbc(entry.commonsubsolver.nlpsubsolver, entry.bestsubproblem0.bndl, entry.bestsubproblem0.bndu, _params);
                minnlc.minnlcrestartfrom(entry.commonsubsolver.nlpsubsolver, entry.bestsubproblem0.x0, _params);
            }
            else
            {
                minnlc.minnlcsetbc(entry.commonsubsolver.nlpsubsolver, entry.bestsubproblem1.bndl, entry.bestsubproblem1.bndu, _params);
                minnlc.minnlcrestartfrom(entry.commonsubsolver.nlpsubsolver, entry.bestsubproblem1.x0, _params);
            }
            
            //
            // Solve NLP relaxation
            //
        lbl_9:
            if( !minnlc.minnlciteration(entry.commonsubsolver.nlpsubsolver, _params) )
            {
                goto lbl_10;
            }
            if( entry.commonsubsolver.nlpsubsolver.requesttype==-1 )
            {
                goto lbl_11;
            }
            entry.entrystatus = stwaitingforrcomm;
            entry.rstate.stage = 0;
            goto lbl_rcomm;
        lbl_0:
        lbl_11:
            if( entry.hastimeout && (double)(apserv.stimergetmsrunning(entry.timerlocal, _params))>(double)(entry.timeout) )
            {
                entry.entrystatus = sttimeout;
                result = false;
                return result;
            }
            goto lbl_9;
        lbl_10:
            minnlc.minnlcresultsbuf(entry.commonsubsolver.nlpsubsolver, ref entry.commonsubsolver.xsol, entry.commonsubsolver.nlprep, _params);
            
            //
            // Analyze solution
            //
            if( leafidx==0 )
            {
                analyzenlpsolutionandenforceintegrality(entry, entry.commonsubsolver.xsol, entry.commonsubsolver.nlprep, state, entry.bestsubproblem0, uselock, _params);
            }
            else
            {
                analyzenlpsolutionandenforceintegrality(entry, entry.commonsubsolver.xsol, entry.commonsubsolver.nlprep, state, entry.bestsubproblem1, uselock, _params);
            }
            
            //
            // Randomize initial position for possible restarts
            //
            if( state.nmultistarts>1 )
            {
                if( leafidx==0 )
                {
                    subproblemrandomizex0(entry.bestsubproblem0, state, _params);
                }
                else
                {
                    subproblemrandomizex0(entry.bestsubproblem1, state, _params);
                }
            }
            restartidx = restartidx+1;
            goto lbl_6;
        lbl_8:
            leafidx = leafidx+1;
            goto lbl_3;
        lbl_5:
            entry.entrystatus = stsolved;
            done = true;
        lbl_1:
            alglib.ap.assert(done, "BBGD: integrity check 155534 failed");
            result = false;
            return result;
            
            //
            // Saving state
            //
        lbl_rcomm:
            result = true;
            entry.rstate.ia[0] = n;
            entry.rstate.ia[1] = i;
            entry.rstate.ia[2] = leafidx;
            entry.rstate.ia[3] = restartidx;
            entry.rstate.ia[4] = terminationtype;
            entry.rstate.ba[0] = uselock;
            entry.rstate.ba[1] = done;
            return result;
        }


        /*************************************************************************
        Run subproblem solver
        *************************************************************************/
        private static bool subsolverrun(bbgdstate state,
            bbgdfront front,
            bbgdfrontentry entry,
            bbgdfrontsubsolver subsolver,
            alglib.xparams _params)
        {
            bool result = new bool();
            int i = 0;
            int terminationtype = 0;
            bool uselock = new bool();
            bool done = new bool();

            
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
            if( subsolver.rstate.stage>=0 )
            {
                i = subsolver.rstate.ia[0];
                terminationtype = subsolver.rstate.ia[1];
                uselock = subsolver.rstate.ba[0];
                done = subsolver.rstate.ba[1];
            }
            else
            {
                i = -838;
                terminationtype = 939;
                uselock = false;
                done = true;
            }
            if( subsolver.rstate.stage==0 )
            {
                goto lbl_0;
            }
            
            //
            // Routine body
            //
            
            //
            // Init
            //
            uselock = true;
            alglib.ap.assert((subsolver.subsolverstatus==streadytorun && subsolver.subproblem.leafidx>=0) && subsolver.subproblem.leafidx<=1, "BBGD: integrity check 589220 failed");
            
            //
            // Handle various objective types
            //
            done = false;
            if( state.objtype==1 && state.nnlc==0 )
            {
                if( subsolver.subproblem.leafidx==0 )
                {
                    solveqpnode(entry, subsolver, state, subsolver.subproblem.x0, subsolver.subproblem.bndl, subsolver.subproblem.bndu, entry.bestsubproblem0, uselock, _params);
                }
                else
                {
                    solveqpnode(entry, subsolver, state, subsolver.subproblem.x0, subsolver.subproblem.bndl, subsolver.subproblem.bndu, entry.bestsubproblem1, uselock, _params);
                }
                if( entry.hastimeout && (double)(apserv.stimergetmsrunning(entry.timerlocal, _params))>(double)(entry.timeout) )
                {
                    subsolver.subsolverstatus = sttimeout;
                    result = false;
                    return result;
                }
                subsolver.subsolverstatus = stsolved;
                done = true;
            }
            if( done )
            {
                goto lbl_1;
            }
            
            //
            // Generic NLP subproblem is solved.
            //
            minnlc.minnlccreatebuf(subsolver.subproblem.n, subsolver.subproblem.x0, subsolver.nlpsubsolver, _params);
            minnlc.minnlcsetscale(subsolver.nlpsubsolver, state.s, _params);
            minnlc.minnlcsetbc(subsolver.nlpsubsolver, subsolver.subproblem.bndl, subsolver.subproblem.bndu, _params);
            minnlc.minnlcsetlc2(subsolver.nlpsubsolver, state.rawa, state.rawal, state.rawau, state.lccnt, _params);
            minnlc.minnlcsetnlc2(subsolver.nlpsubsolver, state.nl, state.nu, state.nnlc, _params);
            minnlc.minnlcsetprotocolv2s(subsolver.nlpsubsolver, _params);
            if( subsolver.subproblem.leafidx>=0 )
            {
                minnlc.minnlcsetcond3(subsolver.nlpsubsolver, state.epsf, state.epsx, state.nonrootmaxitsconst+state.nonrootmaxitslin*subsolver.subproblem.n, _params);
                minnlc.minnlcsetfsqpadditsforctol(subsolver.nlpsubsolver, state.nonrootadditsforfeasibility, state.ctol, _params);
            }
            
            //
            // Solve NLP relaxation
            //
        lbl_3:
            if( !minnlc.minnlciteration(subsolver.nlpsubsolver, _params) )
            {
                goto lbl_4;
            }
            if( subsolver.nlpsubsolver.requesttype==-1 )
            {
                goto lbl_5;
            }
            subsolver.subsolverstatus = stwaitingforrcomm;
            subsolver.rstate.stage = 0;
            goto lbl_rcomm;
        lbl_0:
        lbl_5:
            if( entry.hastimeout && (double)(apserv.stimergetmsrunning(entry.timerlocal, _params))>(double)(entry.timeout) )
            {
                subsolver.subsolverstatus = sttimeout;
                result = false;
                return result;
            }
            goto lbl_3;
        lbl_4:
            minnlc.minnlcresultsbuf(subsolver.nlpsubsolver, ref subsolver.xsol, subsolver.nlprep, _params);
            
            //
            // Analyze solution
            //
            if( subsolver.subproblem.leafidx==0 )
            {
                analyzenlpsolutionandenforceintegrality(entry, subsolver.xsol, subsolver.nlprep, state, entry.bestsubproblem0, uselock, _params);
            }
            else
            {
                analyzenlpsolutionandenforceintegrality(entry, subsolver.xsol, subsolver.nlprep, state, entry.bestsubproblem1, uselock, _params);
            }
            subsolver.subsolverstatus = stsolved;
            done = true;
        lbl_1:
            alglib.ap.assert(done, "BBGD: integrity check 659230 failed");
            result = false;
            return result;
            
            //
            // Saving state
            //
        lbl_rcomm:
            result = true;
            subsolver.rstate.ia[0] = i;
            subsolver.rstate.ia[1] = terminationtype;
            subsolver.rstate.ba[0] = uselock;
            subsolver.rstate.ba[1] = done;
            return result;
        }


        /*************************************************************************
        Uses entry data to update global statistics in State:
        * primal bound
        * pseudocosts

        This update should be performed prior to feeding data from the entry  into
        State with EntryPushSolution() or EntryTryPushAndDive().
        *************************************************************************/
        private static void entryupdateglobalstats(bbgdfrontentry entry,
            bbgdstate state,
            alglib.xparams _params)
        {
            int k = 0;
            double v = 0;
            double vup = 0;
            double vdown = 0;

            alglib.ap.assert(entry.entrystatus==stsolved, "BBGD: integrity check 828957 failed");
            
            //
            // Analyze solutions #0 and #1 (if present)
            //
            if( entry.bestsubproblem0.hasprimalsolution )
            {
                alglib.ap.assert(entry.bestsubproblem0.n==state.n, "BBGD: integrity check 832958 failed");
                if( !state.hasprimalsolution || (double)(entry.bestsubproblem0.fprim)<(double)(state.fprim) )
                {
                    ablasf.rcopyallocv(entry.bestsubproblem0.n, entry.bestsubproblem0.xprim, ref state.xprim, _params);
                    state.fprim = entry.bestsubproblem0.fprim;
                    state.hprim = entry.bestsubproblem0.hprim;
                    state.hasprimalsolution = true;
                }
                state.repnprimalcandidates = state.repnprimalcandidates+1;
            }
            if( entry.bestsubproblem1.hasprimalsolution )
            {
                alglib.ap.assert(entry.bestsubproblem1.n==state.n, "BBGD: integrity check 832958 failed");
                if( !state.hasprimalsolution || (double)(entry.bestsubproblem1.fprim)<(double)(state.fprim) )
                {
                    ablasf.rcopyallocv(entry.bestsubproblem1.n, entry.bestsubproblem1.xprim, ref state.xprim, _params);
                    state.fprim = entry.bestsubproblem1.fprim;
                    state.hprim = entry.bestsubproblem1.hprim;
                    state.hasprimalsolution = true;
                }
                state.repnprimalcandidates = state.repnprimalcandidates+1;
            }
            
            //
            // Update pseudocosts
            //
            if( !entry.isroot )
            {
                
                //
                // Update pseudocosts
                //
                if( state.isintegral[entry.branchvar] )
                {
                    if( entry.bestsubproblem0.hasdualsolution )
                    {
                        k = entry.branchvar;
                        v = entry.branchval;
                        vdown = Math.Max(entry.bestsubproblem0.bestfdual-entry.bestsubproblem0.parentfdual, 0)/Math.Max(v-(int)Math.Floor(v), math.machineepsilon);
                        if( (double)(vdown)>(double)(0) )
                        {
                            state.pseudocostsdown[k] = (state.pseudocostsdown[k]*state.pseudocostscntdown[k]+vdown)/(state.pseudocostscntdown[k]+1);
                            state.pseudocostscntdown[k] = state.pseudocostscntdown[k]+1;
                            state.globalpseudocostdown = (state.globalpseudocostdown*state.globalpseudocostcntdown+vdown)/(state.globalpseudocostcntdown+1);
                            state.globalpseudocostcntdown = state.globalpseudocostcntdown+1;
                        }
                    }
                    if( entry.bestsubproblem1.hasdualsolution )
                    {
                        k = entry.branchvar;
                        v = entry.branchval;
                        vup = Math.Max(entry.bestsubproblem1.bestfdual-entry.bestsubproblem1.parentfdual, 0)/Math.Max((int)Math.Ceiling(v)-v, math.machineepsilon);
                        if( (double)(vup)>(double)(0) )
                        {
                            state.pseudocostsup[k] = (state.pseudocostsup[k]*state.pseudocostscntup[k]+vup)/(state.pseudocostscntup[k]+1);
                            state.pseudocostscntup[k] = state.pseudocostscntup[k]+1;
                            state.globalpseudocostup = (state.globalpseudocostup*state.globalpseudocostcntup+vup)/(state.globalpseudocostcntup+1);
                            state.globalpseudocostcntup = state.globalpseudocostcntup+1;
                        }
                    }
                }
            }
            
            //
            // Update subproblem counts
            //
            state.repnsubproblems = state.repnsubproblems+apserv.icase2(entry.isroot, 1, 2, _params)*state.nmultistarts;
            state.repntreenodes = state.repntreenodes+apserv.icase2(entry.isroot, 1, 2, _params);
        }


        /*************************************************************************
        Feeding solution from the entry to the end of the State.bbSubproblems[]
        array. This function does not regrow the sorted part of bbSubproblems (heap).

        If at least one subproblem was added, the flag variable is set to true.
        It is left unchanged otherwise.

        This function merely adds subproblems to the heap, it is assumed that they
        were already scanned for primal solutions, pseudocosts, etc.
        *************************************************************************/
        private static void entrypushsolution(bbgdfrontentry entry,
            bbgdstate state,
            ref bool setonupdate,
            alglib.xparams _params)
        {
            alglib.ap.assert(entry.entrystatus==stsolved, "BBGD: integrity check 863011 failed");
            pushsubproblemsolution(entry.bestsubproblem0, state, ref setonupdate, _params);
            pushsubproblemsolution(entry.bestsubproblem1, state, ref setonupdate, _params);
        }


        /*************************************************************************
        Tries to perform diving by pushing the worst leaf to the heap (if feasible
        and not fathomed) and configuring the entry to process the best leaf.

        If unsuccessful due to both leafs being infeasible/fathomed, returns
        False and does not change entry state or the heap. There is no need to call
        EntryPushSolution() in this case.

        If successful, the entry status is set to stReadyToRun and True is returned.

        This function merely adds subproblems to the heap, it is assumed that they
        were already scanned for primal solutions, pseudocosts, etc.
        *************************************************************************/
        private static bool entrytrypushanddive(bbgdfrontentry entry,
            bbgdfront front,
            bbgdstate state,
            alglib.xparams _params)
        {
            bool result = new bool();
            int eligibleleafidx = 0;
            double eligibleleafdual = 0;
            bool iseligibleleaf = new bool();
            bbgdsubproblem eligiblep = null;
            bool bdummy = new bool();

            alglib.ap.assert(entry.entrystatus==stsolved, "BBGD: integrity check 905205 failed");
            eligibleleafidx = -1;
            eligibleleafdual = math.maxrealnumber;
            alglib.smp.ae_shared_pool_retrieve(state.sppool, ref eligiblep);
            
            //
            // Analyze leaf 0
            //
            iseligibleleaf = entry.bestsubproblem0.hasdualsolution;
            iseligibleleaf = iseligibleleaf && (!state.hasprimalsolution || (double)(entry.bestsubproblem0.dualbound)<(double)(state.fprim-state.pdgap*apserv.rmaxabs2(state.fprim, 1, _params)));
            iseligibleleaf = iseligibleleaf && (eligibleleafidx<0 || (double)(entry.bestsubproblem0.dualbound)<(double)(eligibleleafdual));
            if( iseligibleleaf )
            {
                eligibleleafidx = 0;
                eligibleleafdual = entry.bestsubproblem0.dualbound;
                subproblemcopy(entry.bestsubproblem0, entry.bestsubproblem0.leafid, eligiblep, _params);
            }
            
            //
            // Analyze leaf 1
            //
            iseligibleleaf = entry.bestsubproblem1.hasdualsolution;
            iseligibleleaf = iseligibleleaf && (!state.hasprimalsolution || (double)(entry.bestsubproblem1.dualbound)<(double)(state.fprim-state.pdgap*apserv.rmaxabs2(state.fprim, 1, _params)));
            iseligibleleaf = iseligibleleaf && (eligibleleafidx<0 || (double)(entry.bestsubproblem1.dualbound)<(double)(eligibleleafdual));
            if( iseligibleleaf )
            {
                eligibleleafidx = 1;
                eligibleleafdual = entry.bestsubproblem1.dualbound;
                subproblemcopy(entry.bestsubproblem1, entry.bestsubproblem1.leafid, eligiblep, _params);
            }
            
            //
            // Exit if no leaf is eligible
            //
            if( eligibleleafidx<0 )
            {
                pushsubproblemsolution(entry.bestsubproblem0, state, ref bdummy, _params);
                pushsubproblemsolution(entry.bestsubproblem1, state, ref bdummy, _params);
                if( state.dotrace )
                {
                    alglib.ap.trace("> no eligible leaves to continue diving, retrieving problem from the heap\n");
                }
                alglib.smp.ae_shared_pool_recycle(state.sppool, ref eligiblep);
                result = false;
                return result;
            }
            if( state.dotrace )
            {
                alglib.ap.trace(System.String.Format("> diving into leaf {0,0:d} (subproblem {1,8:d}P)\n", eligibleleafidx, eligiblep.leafid));
            }
            result = true;
            
            //
            // Process eligible leaf, try to push ineligible one
            //
            // NOTE: this code has one small inefficiency - if eligible leaf does not
            //       need splitting (and hence diving), it decides to break the diving
            //       even if another one could be processed instead.
            //
            if( eligibleleafidx==0 )
            {
                pushsubproblemsolution(entry.bestsubproblem1, state, ref bdummy, _params);
            }
            else
            {
                pushsubproblemsolution(entry.bestsubproblem0, state, ref bdummy, _params);
            }
            if( !entryprepareleafs(entry, front, eligiblep, state, _params) )
            {
                if( eligibleleafidx==0 )
                {
                    pushsubproblemsolution(entry.bestsubproblem0, state, ref bdummy, _params);
                }
                else
                {
                    pushsubproblemsolution(entry.bestsubproblem1, state, ref bdummy, _params);
                }
                result = false;
            }
            alglib.smp.ae_shared_pool_recycle(state.sppool, ref eligiblep);
            return result;
        }


        /*************************************************************************
        Feeding solution of the subproblem to the end of the State.bbSubproblems[]
        array. This function does not regrow the sorted part of bbSubproblems (heap).

        Intended to be used by EntryPushSolution().
        *************************************************************************/
        private static void pushsubproblemsolution(bbgdsubproblem subproblem,
            bbgdstate state,
            ref bool setonupdate,
            alglib.xparams _params)
        {
            bbgdsubproblem p = null;

            if( !subproblem.hasdualsolution )
            {
                if( state.dotrace )
                {
                    alglib.ap.trace(System.String.Format(">> analyzing {0,8:d}P: infeasible (err={1,0:E2}), fathomed\n", subproblem.leafid, subproblem.besthdual));
                }
                return;
            }
            if( state.dotrace )
            {
                alglib.ap.trace(System.String.Format(">> analyzing {0,8:d}P (bestfdual={1,0:E12}, dualbound={2,0:E12}, fprim={3,0:E12})", subproblem.leafid, subproblem.bestfdual, subproblem.dualbound, subproblem.fprim));
            }
            if( state.hasprimalsolution && (double)(subproblem.dualbound)>=(double)(state.fprim-state.pdgap*apserv.rmaxabs2(state.fprim, 1, _params)) )
            {
                if( state.dotrace )
                {
                    alglib.ap.trace(", fathomed\n");
                }
                return;
            }
            if( state.dotrace )
            {
                alglib.ap.trace("\n");
            }
            alglib.smp.ae_shared_pool_retrieve(state.sppool, ref p);
            subproblemcopy(subproblem, subproblem.leafid, p, _params);
            state.bbsubproblems.append(p);
            setonupdate = true;
        }


        /*************************************************************************
        Quick lightweight presolve for a QP subproblem. Saves presolved problem to
        the Entry fields.

        Returns termination type: 0 for success, negative for failure (infeasibility
        detected)
        *************************************************************************/
        private static int qpquickpresolve(bbgdfrontentry entry,
            bbgdfrontsubsolver subsolver,
            double[] raws,
            double[] rawxorigin,
            double[] rawbndl,
            double[] rawbndu,
            sparse.sparsematrix rawa,
            bool isupper,
            double[] rawb,
            int n,
            sparse.sparsematrix rawsparsec,
            double[] rawcl,
            double[] rawcu,
            int lccnt,
            int[] qpordering,
            double eps,
            alglib.xparams _params)
        {
            int result = 0;
            int npsv = 0;
            int offs = 0;
            int dstrow = 0;
            int i = 0;
            int j = 0;
            int jj = 0;
            int j0 = 0;
            int j1 = 0;
            int ic = 0;
            int jc = 0;
            double v = 0;
            double vi = 0;
            double vf = 0;
            double cmin = 0;
            double cmax = 0;
            double vscl = 0;

            alglib.ap.assert(!isupper, "QPQuickPresolve: IsUpper=True is not implemented");
            result = 0;
            
            //
            // Copy box constraints to Subsolver.psvRawBndL/U
            //
            ablasf.rcopyallocv(n, rawbndl, ref subsolver.psvrawbndl, _params);
            ablasf.rcopyallocv(n, rawbndu, ref subsolver.psvrawbndu, _params);
            
            //
            // Analyze constraints to find forcing ones that fix variables at their bounds
            //
            for(i=0; i<=lccnt-1; i++)
            {
                j0 = rawsparsec.ridx[i];
                j1 = rawsparsec.ridx[i+1]-1;
                
                //
                // Compute row normalzation factor for scaled coordinates
                //
                vscl = 0.0;
                for(jj=j0; jj<=j1; jj++)
                {
                    v = rawsparsec.vals[jj]*raws[rawsparsec.idx[jj]];
                    vscl = vscl+v*v;
                }
                vscl = 1/apserv.coalesce(Math.Sqrt(vscl), 1, _params);
                
                //
                // Compute minimum and maximum row values
                //
                cmin = 0;
                cmax = 0;
                for(jj=j0; jj<=j1; jj++)
                {
                    j = rawsparsec.idx[jj];
                    v = rawsparsec.vals[jj];
                    if( v>0.0 )
                    {
                        cmin = cmin+v*subsolver.psvrawbndl[j];
                        cmax = cmax+v*subsolver.psvrawbndu[j];
                    }
                    if( v<0.0 )
                    {
                        cmin = cmin+v*subsolver.psvrawbndu[j];
                        cmax = cmax+v*subsolver.psvrawbndl[j];
                    }
                }
                if( math.isfinite(cmax) && math.isfinite(rawcl[i]) )
                {
                    if( (double)(cmax)<(double)(rawcl[i]-vscl*eps) )
                    {
                        
                        //
                        // constraint is infeasible beyond Eps
                        //
                        result = -3;
                        return result;
                    }
                    if( (double)(cmax)<(double)(rawcl[i]+vscl*eps) )
                    {
                        
                        //
                        // Constraint fixes its variables at values maximizing constraint value
                        //
                        for(jj=j0; jj<=j1; jj++)
                        {
                            j = rawsparsec.idx[jj];
                            v = rawsparsec.vals[jj];
                            if( v>0.0 )
                            {
                                subsolver.psvrawbndl[j] = subsolver.psvrawbndu[j];
                            }
                            if( v<0.0 )
                            {
                                subsolver.psvrawbndu[j] = subsolver.psvrawbndl[j];
                            }
                        }
                        continue;
                    }
                }
                if( math.isfinite(cmin) && math.isfinite(rawcu[i]) )
                {
                    if( (double)(cmin)>(double)(rawcu[i]+vscl*eps) )
                    {
                        
                        //
                        // constraint is infeasible beyond Eps
                        //
                        result = -3;
                        return result;
                    }
                    if( (double)(cmin)>(double)(rawcu[i]-vscl*eps) )
                    {
                        
                        //
                        // Constraint fixes its variables at values minimizing constraint value
                        //
                        for(jj=j0; jj<=j1; jj++)
                        {
                            j = rawsparsec.idx[jj];
                            v = rawsparsec.vals[jj];
                            if( v>0.0 )
                            {
                                subsolver.psvrawbndu[j] = subsolver.psvrawbndl[j];
                            }
                            if( v<0.0 )
                            {
                                subsolver.psvrawbndl[j] = subsolver.psvrawbndu[j];
                            }
                        }
                        continue;
                    }
                }
            }
            
            //
            // Analyze fixed vars, compress S, Origin, variable bounds and linear term
            //
            npsv = 0;
            ablasf.isetallocv(n+lccnt, -1, ref subsolver.psvpackxyperm, _params);
            ablasf.isetallocv(n+lccnt, -1, ref subsolver.psvunpackxyperm, _params);
            ablasf.rallocv(n, ref subsolver.psvs, _params);
            ablasf.rallocv(n, ref subsolver.psvxorigin, _params);
            ablasf.rallocv(n, ref subsolver.psvbndl, _params);
            ablasf.rallocv(n, ref subsolver.psvbndu, _params);
            ablasf.rallocv(n, ref subsolver.psvb, _params);
            ablasf.rallocv(n, ref subsolver.psvfixvals, _params);
            for(i=0; i<=n-1; i++)
            {
                if( (math.isfinite(subsolver.psvrawbndl[i]) && math.isfinite(subsolver.psvrawbndu[i])) && subsolver.psvrawbndl[i]>subsolver.psvrawbndu[i] )
                {
                    result = -3;
                    return result;
                }
                if( (!math.isfinite(subsolver.psvrawbndl[i]) || !math.isfinite(subsolver.psvrawbndu[i])) || subsolver.psvrawbndl[i]<subsolver.psvrawbndu[i] )
                {
                    subsolver.psvunpackxyperm[npsv] = i;
                    subsolver.psvpackxyperm[i] = npsv;
                    subsolver.psvs[npsv] = raws[i];
                    subsolver.psvxorigin[npsv] = rawxorigin[i];
                    subsolver.psvbndl[npsv] = subsolver.psvrawbndl[i];
                    subsolver.psvbndu[npsv] = subsolver.psvrawbndu[i];
                    subsolver.psvb[npsv] = rawb[i];
                    npsv = npsv+1;
                }
                else
                {
                    subsolver.psvfixvals[i] = subsolver.psvrawbndl[i];
                }
            }
            subsolver.npsv = npsv;
            if( npsv==0 )
            {
                return result;
            }
            
            //
            // Compress quadratic term
            //
            subsolver.psva.n = npsv;
            subsolver.psva.m = npsv;
            ablasf.iallocv(npsv+1, ref subsolver.psva.ridx, _params);
            offs = 0;
            dstrow = 0;
            subsolver.psva.ridx[0] = 0;
            for(i=0; i<=n-1; i++)
            {
                ablasf.igrowv(offs+npsv, ref subsolver.psva.idx, _params);
                ablasf.rgrowv(offs+npsv, ref subsolver.psva.vals, _params);
                j0 = rawa.ridx[i];
                j1 = rawa.uidx[i]-1;
                ic = subsolver.psvpackxyperm[i];
                if( ic<0 )
                {
                    
                    //
                    // I-th variable is fixed: products with this variable either update the linear term or the constant one (ignored for presolve).
                    //
                    // The diagonal term is implicitly ignored (JC>=0 is always evaluated to false). 
                    //
                    vi = subsolver.psvfixvals[i]-rawxorigin[i];
                    for(jj=j0; jj<=j1; jj++)
                    {
                        j = rawa.idx[jj];
                        v = rawa.vals[jj];
                        jc = subsolver.psvpackxyperm[j];
                        if( jc>=0 )
                        {
                            subsolver.psvb[jc] = subsolver.psvb[jc]+v*vi;
                        }
                    }
                }
                else
                {
                    
                    //
                    // I-th variable is non-fixed: products with this variable either update the quadratic term or the linear one
                    //
                    for(jj=j0; jj<=j1; jj++)
                    {
                        j = rawa.idx[jj];
                        v = rawa.vals[jj];
                        jc = subsolver.psvpackxyperm[j];
                        if( jc>=0 )
                        {
                            subsolver.psva.idx[offs] = jc;
                            subsolver.psva.vals[offs] = v;
                            offs = offs+1;
                        }
                        else
                        {
                            subsolver.psvb[ic] = subsolver.psvb[ic]+v*(subsolver.psvfixvals[j]-rawxorigin[j]);
                        }
                    }
                    subsolver.psva.ridx[dstrow+1] = offs;
                    dstrow = dstrow+1;
                }
            }
            sparse.sparsecreatecrsinplace(subsolver.psva, _params);
            
            //
            // Compress linear constraints
            //
            subsolver.psvlccnt = 0;
            if( lccnt>0 )
            {
                ablasf.rsetallocv(lccnt, Double.NegativeInfinity, ref subsolver.psvcl, _params);
                ablasf.rsetallocv(lccnt, Double.PositiveInfinity, ref subsolver.psvcu, _params);
                subsolver.psvsparsec.m = 0;
                subsolver.psvsparsec.n = npsv;
                ablasf.iallocv(lccnt+1, ref subsolver.psvsparsec.ridx, _params);
                subsolver.psvsparsec.ridx[0] = 0;
                for(i=0; i<=lccnt-1; i++)
                {
                    vf = 0.0;
                    offs = subsolver.psvsparsec.ridx[subsolver.psvlccnt];
                    ablasf.igrowv(offs+npsv, ref subsolver.psvsparsec.idx, _params);
                    ablasf.rgrowv(offs+npsv, ref subsolver.psvsparsec.vals, _params);
                    j0 = rawsparsec.ridx[i];
                    j1 = rawsparsec.ridx[i+1]-1;
                    for(jj=j0; jj<=j1; jj++)
                    {
                        j = rawsparsec.idx[jj];
                        v = rawsparsec.vals[jj];
                        jc = subsolver.psvpackxyperm[j];
                        if( jc>=0 )
                        {
                            subsolver.psvsparsec.idx[offs] = jc;
                            subsolver.psvsparsec.vals[offs] = v;
                            offs = offs+1;
                        }
                        else
                        {
                            vf = vf+subsolver.psvfixvals[j]*v;
                        }
                    }
                    if( offs==subsolver.psvsparsec.ridx[subsolver.psvlccnt] )
                    {
                        continue;
                    }
                    subsolver.psvsparsec.ridx[subsolver.psvlccnt+1] = offs;
                    if( math.isfinite(rawcl[i]) )
                    {
                        subsolver.psvcl[subsolver.psvlccnt] = rawcl[i]-vf;
                    }
                    if( math.isfinite(rawcu[i]) )
                    {
                        subsolver.psvcu[subsolver.psvlccnt] = rawcu[i]-vf;
                    }
                    subsolver.psvpackxyperm[n+i] = npsv+subsolver.psvlccnt;
                    subsolver.psvunpackxyperm[npsv+subsolver.psvlccnt] = n+i;
                    subsolver.psvlccnt = subsolver.psvlccnt+1;
                    subsolver.psvsparsec.m = subsolver.psvsparsec.m+1;
                }
                sparse.sparsecreatecrsinplace(subsolver.psvsparsec, _params);
            }
            
            //
            // Compress QP ordering:
            // * first, compute inverse of State.qpOrdering[] in Subsolver.tmpI[]
            // * map Subsolver.tmpI[] elements to packed indexes Subsolver.psvPackXYPerm[]
            // * compress Subsolver.tmpI[], skipping -1's (dropped vars/constraints)
            // * invert Subsolver.tmpI[], storing result into Subsolver.psvQPOrdering[]
            //
            ablasf.iallocv(n+lccnt, ref subsolver.tmpi, _params);
            for(i=0; i<=n+lccnt-1; i++)
            {
                subsolver.tmpi[qpordering[i]] = i;
            }
            for(i=0; i<=n+lccnt-1; i++)
            {
                subsolver.tmpi[i] = subsolver.psvpackxyperm[subsolver.tmpi[i]];
            }
            offs = 0;
            for(i=0; i<=n+lccnt-1; i++)
            {
                if( subsolver.tmpi[i]>=0 )
                {
                    subsolver.tmpi[offs] = subsolver.tmpi[i];
                    offs = offs+1;
                }
            }
            alglib.ap.assert(offs==npsv+subsolver.psvlccnt, "BBGD: 534739 failed");
            ablasf.iallocv(npsv+subsolver.psvlccnt, ref subsolver.psvqpordering, _params);
            for(i=0; i<=npsv+subsolver.psvlccnt-1; i++)
            {
                subsolver.psvqpordering[subsolver.tmpi[i]] = i;
            }
            return result;
        }


        /*************************************************************************
        Solve QP subproblem given by its bounds and initial point. Internally
        applies iterative refinement to produce highly accurate solutions (essential
        for proper functioning of the B&B solver)

        If UseLock=True, then SubproblemToUpdate is accessed by acquiring Entry.EntryLock
        *************************************************************************/
        private static void solveqpnode(bbgdfrontentry entry,
            bbgdfrontsubsolver subsolver,
            bbgdstate state,
            double[] x0,
            double[] bndl,
            double[] bndu,
            bbgdsubproblem subproblemtoupdate,
            bool uselock,
            alglib.xparams _params)
        {
            int i = 0;
            int n = 0;
            int terminationtype = 0;
            int tmpterminationtype = 0;
            int k = 0;
            int maxidx = 0;
            double fsol = 0;
            double hsol = 0;
            double mxsol = 0;
            double fcand = 0;
            double hcand = 0;
            double mxcand = 0;
            double stpnrm = 0;
            double v = 0;
            double trustrad = 0;
            bool applytrustrad = new bool();
            bool isintfeasible = new bool();

            alglib.ap.assert(state.objtype==1 && state.nnlc==0, "BBGD: integrity check 330714 failed");
            n = entry.parentsubproblem.n;
            ablasf.rallocv(Math.Max(n, state.lccnt), ref subsolver.tmp0, _params);
            
            //
            // Quick exit for infeasible with respect to box constraints
            //
            for(i=0; i<=n-1; i++)
            {
                if( (math.isfinite(bndl[i]) && math.isfinite(bndu[i])) && (double)(bndl[i])>(double)(bndu[i]) )
                {
                    return;
                }
            }
            
            //
            // Initial state: box constrain proposed X0
            //
            ablasf.rcopyallocv(n, x0, ref subsolver.xsol, _params);
            for(i=0; i<=n-1; i++)
            {
                if( math.isfinite(bndl[i]) )
                {
                    subsolver.xsol[i] = Math.Max(bndl[i], subsolver.xsol[i]);
                }
                if( math.isfinite(bndu[i]) )
                {
                    subsolver.xsol[i] = Math.Min(bndu[i], subsolver.xsol[i]);
                }
            }
            fsol = 0.5*sparse.sparsevsmv(state.obja, false, subsolver.xsol, _params)+ablasf.rdotv(n, subsolver.xsol, state.objb, _params)+state.objc0;
            optserv.unscaleandchecklc2violation(state.s, state.rawa, state.rawal, state.rawau, state.lcsrcidx, state.lccnt, subsolver.xsol, ref hsol, ref mxsol, ref maxidx, _params);
            for(i=0; i<=n-1; i++)
            {
                if( math.isfinite(bndl[i]) )
                {
                    v = Math.Max(bndl[i]-subsolver.xsol[i], 0.0);
                    hsol = hsol+v;
                    mxsol = Math.Max(mxsol, v);
                }
                if( math.isfinite(bndu[i]) )
                {
                    v = Math.Max(subsolver.xsol[i]-bndu[i], 0.0);
                    hsol = hsol+v;
                    mxsol = Math.Max(mxsol, v);
                }
            }
            
            //
            // Perform several refinement iterations, then analyze candidate
            //
            terminationtype = 0;
            applytrustrad = false;
            for(k=0; k<=maxqprfsits-1; k++)
            {
                
                //
                // Reformulate raw problem
                //
                //     min[0.5x'Ax+b'x+c]    subject to    AL<=Ax<=AU, BndL<=x<=BndU 
                //
                // as an SQP-style problem
                //
                //     min[0.5y'Ay+(A*xsol+b)'y+fsol]    subject to    AL-A*xsol<=Ay<=AU-A*xsol, BndL-xsol<=y<=BndU-xsol
                //
                //  with y=x-xsol.
                //
                ablasf.rcopyallocv(n, bndl, ref subsolver.wrkbndl, _params);
                ablasf.rcopyallocv(n, bndu, ref subsolver.wrkbndu, _params);
                if( applytrustrad )
                {
                    for(i=0; i<=n-1; i++)
                    {
                        subsolver.wrkbndl[i] = Math.Max(subsolver.wrkbndl[i], subsolver.xsol[i]-trustrad*state.s[i]);
                        subsolver.wrkbndu[i] = Math.Min(subsolver.wrkbndu[i], subsolver.xsol[i]+trustrad*state.s[i]);
                    }
                }
                sparse.sparsesmv(state.obja, false, subsolver.xsol, ref subsolver.wrkb, _params);
                ablasf.raddv(n, 1.0, state.objb, subsolver.wrkb, _params);
                ablasf.rcopyallocv(n, state.s, ref subsolver.wrks, _params);
                if( applytrustrad )
                {
                    ablasf.rmulv(n, Math.Min(trustrad, 1.0), subsolver.wrks, _params);
                }
                
                //
                // Solve SQP subproblem
                //
                tmpterminationtype = qpquickpresolve(entry, subsolver, state.s, subsolver.xsol, subsolver.wrkbndl, subsolver.wrkbndu, state.obja, false, subsolver.wrkb, n, state.rawa, state.rawal, state.rawau, state.lccnt, state.qpordering, state.epsx, _params);
                if( tmpterminationtype>=0 )
                {
                    if( subsolver.npsv>0 )
                    {
                        ipm2solver.ipm2init(subsolver.qpsubsolver, subsolver.psvs, subsolver.psvxorigin, subsolver.npsv, state.densedummy2, subsolver.psva, 1, false, state.densedummy2, subsolver.tmp0, 0, subsolver.psvb, 0.0, subsolver.psvbndl, subsolver.psvbndu, subsolver.psvsparsec, subsolver.psvlccnt, state.densedummy2, 0, subsolver.psvcl, subsolver.psvcu, false, false, _params);
                        ipm2solver.ipm2setcond(subsolver.qpsubsolver, state.epsx, state.epsx, state.epsx, _params);
                        ipm2solver.ipm2setmaxits(subsolver.qpsubsolver, maxipmits, _params);
                        ipm2solver.ipm2setordering(subsolver.qpsubsolver, subsolver.psvqpordering, _params);
                        ipm2solver.ipm2optimize(subsolver.qpsubsolver, true, ref subsolver.tmp1, ref subsolver.tmp2, ref subsolver.tmp3, ref tmpterminationtype, _params);
                    }
                    ablasf.rcopyallocv(n, subsolver.psvfixvals, ref subsolver.tmp0, _params);
                    for(i=0; i<=subsolver.npsv-1; i++)
                    {
                        subsolver.tmp0[subsolver.psvunpackxyperm[i]] = subsolver.tmp1[i];
                    }
                }
                else
                {
                    
                    //
                    // Presolver signalled infeasibility, stop
                    //
                    break;
                }
                terminationtype = apserv.icoalesce(terminationtype, tmpterminationtype, _params);
                
                //
                // Modify trust radius
                //
                ablasf.rcopyallocv(n, subsolver.tmp0, ref subsolver.tmp1, _params);
                ablasf.raddv(n, -1.0, subsolver.xsol, subsolver.tmp1, _params);
                stpnrm = ablasf.rsclnrminf(n, subsolver.tmp1, state.s, _params);
                if( !applytrustrad )
                {
                    applytrustrad = true;
                    trustrad = 1.0E20;
                }
                trustrad = Math.Min(trustrad, stpnrm);
                
                //
                // Evaluate proposed point using Markov filter
                //
                fcand = 0.5*sparse.sparsevsmv(state.obja, false, subsolver.tmp0, _params)+ablasf.rdotv(n, subsolver.tmp0, state.objb, _params)+state.objc0;
                optserv.unscaleandchecklc2violation(state.s, state.rawa, state.rawal, state.rawau, state.lcsrcidx, state.lccnt, subsolver.tmp0, ref hcand, ref mxcand, ref maxidx, _params);
                for(i=0; i<=n-1; i++)
                {
                    if( math.isfinite(bndl[i]) )
                    {
                        v = Math.Max(bndl[i]-subsolver.tmp0[i], 0.0);
                        hcand = hcand+v;
                        mxcand = Math.Max(mxcand, v);
                    }
                    if( math.isfinite(bndu[i]) )
                    {
                        v = Math.Max(subsolver.tmp0[i]-bndu[i], 0.0);
                        hcand = hcand+v;
                        mxcand = Math.Max(mxcand, v);
                    }
                }
                if( (double)(fcand)>=(double)(fsol) && (double)(hcand)>=(double)(hsol) )
                {
                    break;
                }
                ablasf.rcopyv(n, subsolver.tmp0, subsolver.xsol, _params);
                fsol = fcand;
                hsol = hcand;
                mxsol = mxcand;
                terminationtype = tmpterminationtype;
                if( (double)(stpnrm)<=(double)(state.epsx) )
                {
                    break;
                }
            }
            for(i=0; i<=n-1; i++)
            {
                if( math.isfinite(bndl[i]) )
                {
                    subsolver.xsol[i] = Math.Max(bndl[i], subsolver.xsol[i]);
                }
                if( math.isfinite(bndu[i]) )
                {
                    subsolver.xsol[i] = Math.Min(bndu[i], subsolver.xsol[i]);
                }
            }
            ablasf.rcopyallocv(n, subsolver.xsol, ref subsolver.tmp0, _params);
            analyzeqpsolutionandenforceintegrality(entry, subsolver.tmp0, terminationtype, state, subproblemtoupdate, uselock, ref isintfeasible, _params);
            
            //
            // Apply rounding heuristic to solutions that are box/linearly feasible, but not integer feasible
            //
            if( !isintfeasible && (double)(mxsol)<=(double)(state.ctol) )
            {
                
                //
                // Round the solution
                //
                for(i=0; i<=n-1; i++)
                {
                    if( state.isintegral[i] )
                    {
                        subsolver.xsol[i] = (int)Math.Round(subsolver.xsol[i]);
                        if( math.isfinite(bndl[i]) )
                        {
                            subsolver.xsol[i] = Math.Max(bndl[i], subsolver.xsol[i]);
                        }
                        if( math.isfinite(bndu[i]) )
                        {
                            subsolver.xsol[i] = Math.Min(bndu[i], subsolver.xsol[i]);
                        }
                    }
                }
                
                //
                // Reformulate raw problem as one centered around XSol and having fixed integer variables
                //
                ablasf.rcopyallocv(n, bndl, ref subsolver.wrkbndl, _params);
                ablasf.rcopyallocv(n, bndu, ref subsolver.wrkbndu, _params);
                for(i=0; i<=n-1; i++)
                {
                    if( state.isintegral[i] )
                    {
                        subsolver.wrkbndl[i] = subsolver.xsol[i];
                        subsolver.wrkbndu[i] = subsolver.xsol[i];
                    }
                }
                sparse.sparsesmv(state.obja, false, subsolver.xsol, ref subsolver.wrkb, _params);
                ablasf.raddv(n, 1.0, state.objb, subsolver.wrkb, _params);
                
                //
                // Solve SQP subproblem
                //
                tmpterminationtype = qpquickpresolve(entry, subsolver, state.s, subsolver.xsol, subsolver.wrkbndl, subsolver.wrkbndu, state.obja, false, subsolver.wrkb, n, state.rawa, state.rawal, state.rawau, state.lccnt, state.qpordering, state.epsx, _params);
                if( tmpterminationtype>=0 )
                {
                    terminationtype = 1;
                    if( subsolver.npsv>0 )
                    {
                        ipm2solver.ipm2init(subsolver.qpsubsolver, subsolver.psvs, subsolver.psvxorigin, subsolver.npsv, state.densedummy2, subsolver.psva, 1, false, state.densedummy2, subsolver.tmp0, 0, subsolver.psvb, 0.0, subsolver.psvbndl, subsolver.psvbndu, subsolver.psvsparsec, subsolver.psvlccnt, state.densedummy2, 0, subsolver.psvcl, subsolver.psvcu, false, false, _params);
                        ipm2solver.ipm2setcond(subsolver.qpsubsolver, state.epsx, state.epsx, state.epsx, _params);
                        ipm2solver.ipm2setmaxits(subsolver.qpsubsolver, maxipmits, _params);
                        ipm2solver.ipm2setordering(subsolver.qpsubsolver, subsolver.psvqpordering, _params);
                        ipm2solver.ipm2optimize(subsolver.qpsubsolver, true, ref subsolver.tmp1, ref subsolver.tmp2, ref subsolver.tmp3, ref terminationtype, _params);
                    }
                    ablasf.rcopyallocv(n, subsolver.psvfixvals, ref subsolver.tmp0, _params);
                    for(i=0; i<=subsolver.npsv-1; i++)
                    {
                        subsolver.tmp0[subsolver.psvunpackxyperm[i]] = subsolver.tmp1[i];
                    }
                    if( terminationtype>0 )
                    {
                        analyzeqpsolutionandenforceintegrality(entry, subsolver.tmp0, terminationtype, state, subproblemtoupdate, uselock, ref isintfeasible, _params);
                    }
                }
            }
        }


        /*************************************************************************
        Analyze solution of a QP relaxation, and send it to the subproblem,
        updating its best and worst primal/dual solutions.

        Can modify XSol.

        If UseLock=True, then Subproblem is accessed by acquiring Entry.EntryLock
        *************************************************************************/
        private static void analyzeqpsolutionandenforceintegrality(bbgdfrontentry entry,
            double[] xsol,
            int terminationtype,
            bbgdstate state,
            bbgdsubproblem subproblem,
            bool uselock,
            ref bool isintfeas,
            alglib.xparams _params)
        {
            int i = 0;
            int n = 0;
            double sumerr = 0;
            double maxerr = 0;
            int maxidx = 0;
            double f = 0;

            isintfeas = new bool();

            alglib.ap.assert(state.objtype==1, "BBGD: objType<>1 in AnalyzeQPSolutionAndEnforceIntegrality()");
            n = subproblem.n;
            optserv.unscaleandchecklc2violation(state.s, state.rawa, state.rawal, state.rawau, state.lcsrcidx, state.lccnt, xsol, ref sumerr, ref maxerr, ref maxidx, _params);
            maxerr = maxerr/Math.Max(ablasf.rsclnrminf(n, xsol, state.s, _params), 1);
            if( terminationtype>0 && (double)(maxerr)<=(double)(state.ctol) )
            {
                
                //
                // Analyze integrality
                //
                isintfeas = true;
                for(i=0; i<=n-1; i++)
                {
                    if( state.isintegral[i] )
                    {
                        isintfeas = isintfeas && (double)(Math.Abs(xsol[i]-(int)Math.Round(xsol[i])))<=(double)(state.ctol);
                    }
                }
                if( isintfeas )
                {
                    for(i=0; i<=n-1; i++)
                    {
                        if( state.isintegral[i] )
                        {
                            xsol[i] = (int)Math.Round(xsol[i]);
                        }
                    }
                }
                f = 0.5*sparse.sparsevsmv(state.obja, false, xsol, _params)+ablasf.rdotv(n, xsol, state.objb, _params)+state.objc0;
                optserv.unscaleandchecklc2violation(state.s, state.rawa, state.rawal, state.rawau, state.lcsrcidx, state.lccnt, xsol, ref sumerr, ref maxerr, ref maxidx, _params);
                maxerr = maxerr/Math.Max(ablasf.rsclnrminf(n, xsol, state.s, _params), 1);
                
                //
                // Update primal and dual solutions.
                // Use locks to protect access.
                //
                if( uselock )
                {
                    apserv.weakatomicacquirelock(ref entry.entrylock, 0, 1, _params);
                }
                if( !subproblem.hasdualsolution || (double)(f)<(double)(subproblem.bestfdual) )
                {
                    ablasf.rcopyallocv(n, xsol, ref subproblem.bestxdual, _params);
                    subproblem.bestfdual = f;
                    subproblem.besthdual = maxerr;
                    subproblem.bestdualisintfeas = isintfeas;
                }
                if( !subproblem.hasdualsolution || (double)(f)>(double)(subproblem.worstfdual) )
                {
                    ablasf.rcopyallocv(n, xsol, ref subproblem.worstxdual, _params);
                    subproblem.worstfdual = f;
                    subproblem.worsthdual = maxerr;
                }
                subproblem.hasdualsolution = true;
                subproblemrecomputedualbound(subproblem, _params);
                if( isintfeas && (!subproblem.hasprimalsolution || (double)(f)<(double)(subproblem.fprim)) )
                {
                    subproblem.hasprimalsolution = true;
                    ablasf.rcopyallocv(n, xsol, ref subproblem.xprim, _params);
                    subproblem.fprim = f;
                    subproblem.hprim = maxerr;
                }
                if( uselock )
                {
                    System.Threading.Thread.VolatileWrite(ref entry.entrylock, 0);
                }
            }
            else
            {
                if( uselock )
                {
                    apserv.weakatomicacquirelock(ref entry.entrylock, 0, 1, _params);
                }
                subproblem.besthdual = Math.Min(subproblem.besthdual, maxerr);
                isintfeas = false;
                if( uselock )
                {
                    System.Threading.Thread.VolatileWrite(ref entry.entrylock, 0);
                }
            }
        }


        /*************************************************************************
        Analyze solution of an NLP relaxation, and send it to the subproblem,
        updating its best and worst primal/dual solutions.

        Can modify XSol.
        *************************************************************************/
        private static void analyzenlpsolutionandenforceintegrality(bbgdfrontentry entry,
            double[] xsol,
            minnlc.minnlcreport rep,
            bbgdstate state,
            bbgdsubproblem subproblem,
            bool uselock,
            alglib.xparams _params)
        {
            int i = 0;
            int n = 0;
            bool isintfeas = new bool();

            n = subproblem.n;
            if( rep.terminationtype>0 && (double)(rep.sclfeaserr)<=(double)(state.ctol) )
            {
                
                //
                // Analyze integrality
                //
                isintfeas = true;
                for(i=0; i<=n-1; i++)
                {
                    if( state.isintegral[i] )
                    {
                        isintfeas = isintfeas && (double)(Math.Abs(xsol[i]-(int)Math.Round(xsol[i])))<=(double)(state.ctol);
                    }
                }
                if( isintfeas )
                {
                    for(i=0; i<=n-1; i++)
                    {
                        if( state.isintegral[i] )
                        {
                            xsol[i] = (int)Math.Round(xsol[i]);
                        }
                    }
                }
                
                //
                // Update primal and dual solutions
                // Use locks to protect access.
                //
                if( uselock )
                {
                    apserv.weakatomicacquirelock(ref entry.entrylock, 0, 1, _params);
                }
                if( !subproblem.hasdualsolution || (double)(rep.f)<(double)(subproblem.bestfdual) )
                {
                    ablasf.rcopyallocv(n, xsol, ref subproblem.bestxdual, _params);
                    subproblem.bestfdual = rep.f;
                    subproblem.besthdual = rep.sclfeaserr;
                    subproblem.bestdualisintfeas = isintfeas;
                }
                if( !subproblem.hasdualsolution || (double)(rep.f)>(double)(subproblem.worstfdual) )
                {
                    ablasf.rcopyallocv(n, xsol, ref subproblem.worstxdual, _params);
                    subproblem.worstfdual = rep.f;
                    subproblem.worsthdual = rep.sclfeaserr;
                }
                subproblem.hasdualsolution = true;
                subproblemrecomputedualbound(subproblem, _params);
                if( isintfeas && (!subproblem.hasprimalsolution || (double)(rep.f)<(double)(subproblem.fprim)) )
                {
                    subproblem.hasprimalsolution = true;
                    ablasf.rcopyallocv(n, xsol, ref subproblem.xprim, _params);
                    subproblem.fprim = rep.f;
                    subproblem.hprim = rep.sclfeaserr;
                }
                if( uselock )
                {
                    System.Threading.Thread.VolatileWrite(ref entry.entrylock, 0);
                }
            }
            else
            {
                
                //
                // Bad solution. Use locks to protect access.
                //
                if( uselock )
                {
                    apserv.weakatomicacquirelock(ref entry.entrylock, 0, 1, _params);
                }
                subproblem.besthdual = Math.Min(subproblem.besthdual, rep.sclfeaserr);
                if( uselock )
                {
                    System.Threading.Thread.VolatileWrite(ref entry.entrylock, 0);
                }
            }
        }


        /*************************************************************************
        Add sall subproblems that are outside of the heap to the heap.

        When the function finishes, we have bbSubproblemsHeapSize=len(bbSubproblems)-1.
        *************************************************************************/
        private static void growheap(bbgdstate state,
            alglib.xparams _params)
        {
            int cnt = 0;

            cnt = state.bbsubproblems.getlength();
            alglib.ap.assert(state.bbsubproblemsheapsize>=0 && state.bbsubproblemsheapsize<=cnt, "BBGD: integrity check 181334 failed");
            if( cnt>0 )
            {
                state.bbsubproblemsheapsize = subproblemheapgrow(state.bbsubproblems, 0, state.bbsubproblemsheapsize, cnt-state.bbsubproblemsheapsize, _params);
            }
        }


        /*************************************************************************
        Adds all subproblems that are outside of the heap to the heap, then remove
        one on top of the heap and move it to the end of the array. The heap is
        rebuilt after that.

        When the function finishes, we have bbSubproblemsHeapSize=len(bbSubproblems)-1.
        *************************************************************************/
        private static void growheapandpoptop(bbgdstate state,
            alglib.xparams _params)
        {
            int cnt = 0;

            cnt = state.bbsubproblems.getlength();
            alglib.ap.assert(cnt>0, "BBGD: integrity check 040311 failed");
            alglib.ap.assert(state.bbsubproblemsheapsize>=0 && state.bbsubproblemsheapsize<=cnt, "BBGD: integrity check 040312 failed");
            state.bbsubproblemsheapsize = subproblemheapgrow(state.bbsubproblems, 0, state.bbsubproblemsheapsize, cnt-state.bbsubproblemsheapsize, _params);
            state.bbsubproblemsheapsize = subproblemheappoptop(state.bbsubproblems, 0, state.bbsubproblemsheapsize, _params);
        }


        /*************************************************************************
        Grows subproblem heap having size HeapSize elements, located starting from
        element Offs of the SubproblemHeap array,  by  adding  AppendCnt  elements
        located immediately after the heap part.

        Returns new heap size.
        *************************************************************************/
        private static int subproblemheapgrow(ap.objarray subproblemheap,
            int offs,
            int heapsize,
            int appendcnt,
            alglib.xparams _params)
        {
            int result = 0;
            bbgdsubproblem pchild = null;
            bbgdsubproblem pparent = null;
            int ichild = 0;
            int iparent = 0;
            int newheapsize = 0;

            alglib.ap.assert(heapsize>=0 && appendcnt>=0, "BBGD: integrity check 984505 failed");
            alglib.ap.assert(subproblemheap.getlength()>=offs+heapsize+appendcnt, "BBGD: integrity check 985506 failed");
            
            //
            // Grow heap until all elements are in the heap
            //
            newheapsize = heapsize+appendcnt;
            while( heapsize<newheapsize )
            {
                ichild = heapsize;
                while( ichild>0 )
                {
                    iparent = (ichild-1)/2;
                    subproblemheap.get(offs+ichild, ref pchild);
                    subproblemheap.get(offs+iparent, ref pparent);
                    if( (double)(pparent.dualbound)<=(double)(pchild.dualbound) )
                    {
                        break;
                    }
                    subproblemheap.swap(offs+ichild, offs+iparent);
                    ichild = iparent;
                }
                heapsize = heapsize+1;
            }
            result = newheapsize;
            return result;
        }


        /*************************************************************************
        Removes subproblem on top of the heap having size HeapSize elements, located
        starting from the element Offs of the SubproblemHeap array, and moves it
        to the end of the array. The heap is resorted.

        Returns new heap size.
        *************************************************************************/
        private static int subproblemheappoptop(ap.objarray subproblemheap,
            int offs,
            int heapsize,
            alglib.xparams _params)
        {
            int result = 0;
            bbgdsubproblem pchild = null;
            bbgdsubproblem pchild2 = null;
            bbgdsubproblem pparent = null;
            int ichild = 0;
            int ichild2 = 0;
            int iparent = 0;

            alglib.ap.assert(heapsize>=1, "BBGD: integrity check 023510 failed");
            alglib.ap.assert(subproblemheap.getlength()>=offs+heapsize, "BBGD: integrity check 024510 failed");
            
            //
            // Pop top
            //
            subproblemheap.swap(offs+0, offs+heapsize-1);
            heapsize = heapsize-1;
            iparent = 0;
            ichild = 1;
            ichild2 = 2;
            while( ichild<heapsize )
            {
                subproblemheap.get(offs+iparent, ref pparent);
                subproblemheap.get(offs+ichild, ref pchild);
                
                //
                // only one child.
                //
                // swap and terminate (because this child
                // has no siblings due to heap structure)
                //
                if( ichild2>=heapsize )
                {
                    if( (double)(pparent.dualbound)>(double)(pchild.dualbound) )
                    {
                        subproblemheap.swap(offs+iparent, offs+ichild);
                    }
                    break;
                }
                
                //
                // Two children
                //
                subproblemheap.get(offs+ichild2, ref pchild2);
                if( (double)(pchild.dualbound)<(double)(pchild2.dualbound) )
                {
                    if( (double)(pparent.dualbound)>(double)(pchild.dualbound) )
                    {
                        subproblemheap.swap(offs+iparent, offs+ichild);
                        iparent = ichild;
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    if( (double)(pparent.dualbound)>(double)(pchild2.dualbound) )
                    {
                        subproblemheap.swap(offs+iparent, offs+ichild2);
                        iparent = ichild2;
                    }
                    else
                    {
                        break;
                    }
                }
                ichild = 2*iparent+1;
                ichild2 = 2*iparent+2;
            }
            result = heapsize;
            return result;
        }


    }
    public class mirbfvns
    {
        /*************************************************************************
        This object stores an RBF model, either in dense or sparse model format.
        *************************************************************************/
        public class mirbfmodel : apobject
        {
            public bool isdense;
            public int n;
            public int nf;
            public double[] vmodelbase;
            public double[] vmodelscale;
            public double[] multscale;
            public double[,] clinear;
            public double[,] mx0;
            public int nc;
            public double[,] centers;
            public double[,] crbf;
            public int[] cridx;
            public sparse.sparsematrix spcenters;
            public double[] spcoeffs;
            public mirbfmodel()
            {
                init();
            }
            public override void init()
            {
                vmodelbase = new double[0];
                vmodelscale = new double[0];
                multscale = new double[0];
                clinear = new double[0,0];
                mx0 = new double[0,0];
                centers = new double[0,0];
                crbf = new double[0,0];
                cridx = new int[0];
                spcenters = new sparse.sparsematrix();
                spcoeffs = new double[0];
            }
            public override alglib.apobject make_copy()
            {
                mirbfmodel _result = new mirbfmodel();
                _result.isdense = isdense;
                _result.n = n;
                _result.nf = nf;
                _result.vmodelbase = (double[])vmodelbase.Clone();
                _result.vmodelscale = (double[])vmodelscale.Clone();
                _result.multscale = (double[])multscale.Clone();
                _result.clinear = (double[,])clinear.Clone();
                _result.mx0 = (double[,])mx0.Clone();
                _result.nc = nc;
                _result.centers = (double[,])centers.Clone();
                _result.crbf = (double[,])crbf.Clone();
                _result.cridx = (int[])cridx.Clone();
                _result.spcenters = spcenters!=null ? (sparse.sparsematrix)spcenters.make_copy() : null;
                _result.spcoeffs = (double[])spcoeffs.Clone();
                return _result;
            }
        };


        /*************************************************************************
        This object stores a lightweight subsolver for an integer grid
        *************************************************************************/
        public class mirbfvnsnodesubsolver : apobject
        {
            public double trustrad;
            public bool sufficientcloudsize;
            public double basef;
            public double baseh;
            public double predf;
            public double predh;
            public double skrellen;
            public double maxh;
            public double[] successfhistory;
            public double[] successhhistory;
            public int historymax;
            public mirbfvnsnodesubsolver()
            {
                init();
            }
            public override void init()
            {
                successfhistory = new double[0];
                successhhistory = new double[0];
            }
            public override alglib.apobject make_copy()
            {
                mirbfvnsnodesubsolver _result = new mirbfvnsnodesubsolver();
                _result.trustrad = trustrad;
                _result.sufficientcloudsize = sufficientcloudsize;
                _result.basef = basef;
                _result.baseh = baseh;
                _result.predf = predf;
                _result.predh = predh;
                _result.skrellen = skrellen;
                _result.maxh = maxh;
                _result.successfhistory = (double[])successfhistory.Clone();
                _result.successhhistory = (double[])successhhistory.Clone();
                _result.historymax = historymax;
                return _result;
            }
        };


        /*************************************************************************
        This object stores temporaries for rbfMinimizeModel()
        *************************************************************************/
        public class rbfmmtemporaries : apobject
        {
            public optserv.nlpstoppingcriteria crit;
            public double[] bndlx;
            public double[] bndux;
            public double[] x0x;
            public double[] sx;
            public double[] scalingfactors;
            public nlcfsqp.minfsqpstate fsqpsolver;
            public optserv.smoothnessmonitor smonitor;
            public sparse.sparsematrix cx;
            public double[] clx;
            public double[] cux;
            public double[] tmp0;
            public double[] tmp1;
            public double[] tmp2;
            public double[] tmpnl;
            public double[] tmpnu;
            public int[] tmpi;
            public rbfmmtemporaries()
            {
                init();
            }
            public override void init()
            {
                crit = new optserv.nlpstoppingcriteria();
                bndlx = new double[0];
                bndux = new double[0];
                x0x = new double[0];
                sx = new double[0];
                scalingfactors = new double[0];
                fsqpsolver = new nlcfsqp.minfsqpstate();
                smonitor = new optserv.smoothnessmonitor();
                cx = new sparse.sparsematrix();
                clx = new double[0];
                cux = new double[0];
                tmp0 = new double[0];
                tmp1 = new double[0];
                tmp2 = new double[0];
                tmpnl = new double[0];
                tmpnu = new double[0];
                tmpi = new int[0];
            }
            public override alglib.apobject make_copy()
            {
                rbfmmtemporaries _result = new rbfmmtemporaries();
                _result.crit = crit!=null ? (optserv.nlpstoppingcriteria)crit.make_copy() : null;
                _result.bndlx = (double[])bndlx.Clone();
                _result.bndux = (double[])bndux.Clone();
                _result.x0x = (double[])x0x.Clone();
                _result.sx = (double[])sx.Clone();
                _result.scalingfactors = (double[])scalingfactors.Clone();
                _result.fsqpsolver = fsqpsolver!=null ? (nlcfsqp.minfsqpstate)fsqpsolver.make_copy() : null;
                _result.smonitor = smonitor!=null ? (optserv.smoothnessmonitor)smonitor.make_copy() : null;
                _result.cx = cx!=null ? (sparse.sparsematrix)cx.make_copy() : null;
                _result.clx = (double[])clx.Clone();
                _result.cux = (double[])cux.Clone();
                _result.tmp0 = (double[])tmp0.Clone();
                _result.tmp1 = (double[])tmp1.Clone();
                _result.tmp2 = (double[])tmp2.Clone();
                _result.tmpnl = (double[])tmpnl.Clone();
                _result.tmpnu = (double[])tmpnu.Clone();
                _result.tmpi = (int[])tmpi.Clone();
                return _result;
            }
        };


        /*************************************************************************
        This object stores temporaries for rbfMinimizeModel()
        *************************************************************************/
        public class mirbfvnstemporaries : apobject
        {
            public apserv.stimer localtimer;
            public hqrnd.hqrndstate localrng;
            public double[] glbbndl;
            public double[] glbbndu;
            public double[] fullx0;
            public double[] glbx0;
            public double[] glbtmp0;
            public double[] glbtmp1;
            public double[] glbtmp2;
            public double[,] glbxf;
            public double[,] glbsx;
            public double[,] ortdeltas;
            public double[] glbmultscale;
            public double[] glbvtrustregion;
            public mirbfmodel glbmodel;
            public rbfmmtemporaries buf;
            public double[] glbsk;
            public double[,] glbrandomprior;
            public double[] glbprioratx0;
            public double[] glbxtrial;
            public double[] glbs;
            public int[] mapfull2compact;
            public sparse.sparsematrix glba;
            public double[] glbal;
            public double[] glbau;
            public bool[] glbmask;
            public rbfmmtemporaries mmbuf;
            public int[] lclidxfrac;
            public int[] lcl2glb;
            public double[,] lclxf;
            public double[,] lclsx;
            public int[] nodeslist;
            public double[,] lclrandomprior;
            public double[] lclmultscale;
            public double[] lcls;
            public double[] lclx0;
            public mirbfmodel tmpmodel;
            public ipm2solver.ipm2state qpsubsolver;
            public bbgd.bbgdstate bbgdsubsolver;
            public double[] wrkbndl;
            public double[] wrkbndu;
            public sparse.sparsematrix diaga;
            public double[] linb;
            public mirbfvnstemporaries()
            {
                init();
            }
            public override void init()
            {
                localtimer = new apserv.stimer();
                localrng = new hqrnd.hqrndstate();
                glbbndl = new double[0];
                glbbndu = new double[0];
                fullx0 = new double[0];
                glbx0 = new double[0];
                glbtmp0 = new double[0];
                glbtmp1 = new double[0];
                glbtmp2 = new double[0];
                glbxf = new double[0,0];
                glbsx = new double[0,0];
                ortdeltas = new double[0,0];
                glbmultscale = new double[0];
                glbvtrustregion = new double[0];
                glbmodel = new mirbfmodel();
                buf = new rbfmmtemporaries();
                glbsk = new double[0];
                glbrandomprior = new double[0,0];
                glbprioratx0 = new double[0];
                glbxtrial = new double[0];
                glbs = new double[0];
                mapfull2compact = new int[0];
                glba = new sparse.sparsematrix();
                glbal = new double[0];
                glbau = new double[0];
                glbmask = new bool[0];
                mmbuf = new rbfmmtemporaries();
                lclidxfrac = new int[0];
                lcl2glb = new int[0];
                lclxf = new double[0,0];
                lclsx = new double[0,0];
                nodeslist = new int[0];
                lclrandomprior = new double[0,0];
                lclmultscale = new double[0];
                lcls = new double[0];
                lclx0 = new double[0];
                tmpmodel = new mirbfmodel();
                qpsubsolver = new ipm2solver.ipm2state();
                bbgdsubsolver = new bbgd.bbgdstate();
                wrkbndl = new double[0];
                wrkbndu = new double[0];
                diaga = new sparse.sparsematrix();
                linb = new double[0];
            }
            public override alglib.apobject make_copy()
            {
                mirbfvnstemporaries _result = new mirbfvnstemporaries();
                _result.localtimer = localtimer!=null ? (apserv.stimer)localtimer.make_copy() : null;
                _result.localrng = localrng!=null ? (hqrnd.hqrndstate)localrng.make_copy() : null;
                _result.glbbndl = (double[])glbbndl.Clone();
                _result.glbbndu = (double[])glbbndu.Clone();
                _result.fullx0 = (double[])fullx0.Clone();
                _result.glbx0 = (double[])glbx0.Clone();
                _result.glbtmp0 = (double[])glbtmp0.Clone();
                _result.glbtmp1 = (double[])glbtmp1.Clone();
                _result.glbtmp2 = (double[])glbtmp2.Clone();
                _result.glbxf = (double[,])glbxf.Clone();
                _result.glbsx = (double[,])glbsx.Clone();
                _result.ortdeltas = (double[,])ortdeltas.Clone();
                _result.glbmultscale = (double[])glbmultscale.Clone();
                _result.glbvtrustregion = (double[])glbvtrustregion.Clone();
                _result.glbmodel = glbmodel!=null ? (mirbfmodel)glbmodel.make_copy() : null;
                _result.buf = buf!=null ? (rbfmmtemporaries)buf.make_copy() : null;
                _result.glbsk = (double[])glbsk.Clone();
                _result.glbrandomprior = (double[,])glbrandomprior.Clone();
                _result.glbprioratx0 = (double[])glbprioratx0.Clone();
                _result.glbxtrial = (double[])glbxtrial.Clone();
                _result.glbs = (double[])glbs.Clone();
                _result.mapfull2compact = (int[])mapfull2compact.Clone();
                _result.glba = glba!=null ? (sparse.sparsematrix)glba.make_copy() : null;
                _result.glbal = (double[])glbal.Clone();
                _result.glbau = (double[])glbau.Clone();
                _result.glbmask = (bool[])glbmask.Clone();
                _result.mmbuf = mmbuf!=null ? (rbfmmtemporaries)mmbuf.make_copy() : null;
                _result.lclidxfrac = (int[])lclidxfrac.Clone();
                _result.lcl2glb = (int[])lcl2glb.Clone();
                _result.lclxf = (double[,])lclxf.Clone();
                _result.lclsx = (double[,])lclsx.Clone();
                _result.nodeslist = (int[])nodeslist.Clone();
                _result.lclrandomprior = (double[,])lclrandomprior.Clone();
                _result.lclmultscale = (double[])lclmultscale.Clone();
                _result.lcls = (double[])lcls.Clone();
                _result.lclx0 = (double[])lclx0.Clone();
                _result.tmpmodel = tmpmodel!=null ? (mirbfmodel)tmpmodel.make_copy() : null;
                _result.qpsubsolver = qpsubsolver!=null ? (ipm2solver.ipm2state)qpsubsolver.make_copy() : null;
                _result.bbgdsubsolver = bbgdsubsolver!=null ? (bbgd.bbgdstate)bbgdsubsolver.make_copy() : null;
                _result.wrkbndl = (double[])wrkbndl.Clone();
                _result.wrkbndu = (double[])wrkbndu.Clone();
                _result.diaga = diaga!=null ? (sparse.sparsematrix)diaga.make_copy() : null;
                _result.linb = (double[])linb.Clone();
                return _result;
            }
        };


        /*************************************************************************
        This object stores integer grid
        *************************************************************************/
        public class mirbfvnsgrid : apobject
        {
            public int nnodes;
            public double[,] nodesinfo;
            public int ptlistlength;
            public int[] ptlistheads;
            public int[] ptlistdata;
            public ap.objarray subsolvers;
            public int naddcols;
            public mirbfvnsgrid()
            {
                init();
            }
            public override void init()
            {
                nodesinfo = new double[0,0];
                ptlistheads = new int[0];
                ptlistdata = new int[0];
                subsolvers = new ap.objarray();
            }
            public override alglib.apobject make_copy()
            {
                mirbfvnsgrid _result = new mirbfvnsgrid();
                _result.nnodes = nnodes;
                _result.nodesinfo = (double[,])nodesinfo.Clone();
                _result.ptlistlength = ptlistlength;
                _result.ptlistheads = (int[])ptlistheads.Clone();
                _result.ptlistdata = (int[])ptlistdata.Clone();
                _result.subsolvers = subsolvers!=null ? (ap.objarray)subsolvers.make_copy() : null;
                _result.naddcols = naddcols;
                return _result;
            }
        };


        /*************************************************************************
        This object stores all known points
        *************************************************************************/
        public class mirbfvnsdataset : apobject
        {
            public int npoints;
            public int nvars;
            public int nnlc;
            public double[,] pointinfo;
            public mirbfvnsdataset()
            {
                init();
            }
            public override void init()
            {
                pointinfo = new double[0,0];
            }
            public override alglib.apobject make_copy()
            {
                mirbfvnsdataset _result = new mirbfvnsdataset();
                _result.npoints = npoints;
                _result.nvars = nvars;
                _result.nnlc = nnlc;
                _result.pointinfo = (double[,])pointinfo.Clone();
                return _result;
            }
        };


        /*************************************************************************
        This object stores MIRBF-VNS optimizer state.
        *************************************************************************/
        public class mirbfvnsstate : apobject
        {
            public int n;
            public optserv.nlpstoppingcriteria criteria;
            public int algomode;
            public int budget;
            public int maxneighborhood;
            public int batchsize;
            public bool expandneighborhoodonstart;
            public bool retrylastcut;
            public int convexityflag;
            public double ctol;
            public double epsf;
            public double quickepsf;
            public double epsx;
            public int adaptiveinternalparallelism;
            public int timeout;
            public double[] s;
            public double[] bndl;
            public double[] bndu;
            public bool[] hasbndl;
            public bool[] hasbndu;
            public double[] finitebndl;
            public double[] finitebndu;
            public bool[] isintegral;
            public bool[] isbinary;
            public sparse.sparsematrix rawa;
            public double[] rawal;
            public double[] rawau;
            public int[] lcsrcidx;
            public int lccnt;
            public bool haslinearlyconstrainedints;
            public int nnlc;
            public double[] nl;
            public double[] nu;
            public bool nomask;
            public bool[] hasmask;
            public sparse.sparsematrix varmask;
            public bool hasx0;
            public double[] x0;
            public int requesttype;
            public double[] reportx;
            public double reportf;
            public int querysize;
            public int queryfuncs;
            public int queryvars;
            public int querydim;
            public int queryformulasize;
            public double[] querydata;
            public double[] replyfi;
            public double[] replydj;
            public sparse.sparsematrix replysj;
            public double[] tmpx1;
            public double[] tmpc1;
            public double[] tmpf1;
            public double[] tmpg1;
            public double[,] tmpj1;
            public sparse.sparsematrix tmps1;
            public bool userterminationneeded;
            public int repnfev;
            public int repsubsolverits;
            public int repiterationscount;
            public int repterminationtype;
            public apserv.stimer timerglobal;
            public apserv.stimer timerprepareneighbors;
            public apserv.stimer timerproposetrial;
            public int explorativetrialcnt;
            public int explorativetrialtimems;
            public int localtrialsamplingcnt;
            public int localtrialsamplingtimems;
            public int localtrialrbfcnt;
            public int localtrialrbftimems;
            public int cutcnt;
            public int cuttimems;
            public int dbgpotentiallyparallelbatches;
            public int dbgsequentialbatches;
            public int dbgpotentiallyparallelcutrounds;
            public int dbgsequentialcutrounds;
            public bool prepareevaluationbatchparallelism;
            public bool expandcutgenerateneighborsparallelism;
            public bool doanytrace;
            public bool dotrace;
            public bool doextratrace;
            public bool dolaconictrace;
            public double[] xc;
            public double fc;
            public double mxc;
            public double hc;
            public int nodec;
            public double[] nodecproducedbycut;
            public mirbfvnsgrid grid;
            public mirbfvnsdataset dataset;
            public int nfrac;
            public int nint;
            public int[] xcneighbors;
            public int[] xcreachedfrom;
            public double[,] xcreachedbycut;
            public bool[] xcqueryflags;
            public int xcneighborscnt;
            public int xcpriorityneighborscnt;
            public int evalbatchsize;
            public double[,] evalbatchpoints;
            public int[] evalbatchnodeidx;
            public int[] evalbatchneighboridx;
            public bool outofbudget;
            public hqrnd.hqrndstate unsafeglobalrng;
            public double[] maskint;
            public double[] maskfrac;
            public int[] idxint;
            public int[] idxfrac;
            public int[] xuneighbors;
            public double[,] xucuts;
            public double[,] xupoints;
            public bool[] xuflags;
            public double[] xtrial;
            public double[] trialfi;
            public int[] tmpeb0;
            public double[] tmpeb1;
            public int[] tmpeb2;
            public mirbfvnstemporaries dummytmp;
            public double[,] densedummy2;
            public alglib.ap.nxpool rpool;
            public alglib.smp.shared_pool tmppool;
            public rcommstate rstate;
            public mirbfvnsstate()
            {
                init();
            }
            public override void init()
            {
                criteria = new optserv.nlpstoppingcriteria();
                s = new double[0];
                bndl = new double[0];
                bndu = new double[0];
                hasbndl = new bool[0];
                hasbndu = new bool[0];
                finitebndl = new double[0];
                finitebndu = new double[0];
                isintegral = new bool[0];
                isbinary = new bool[0];
                rawa = new sparse.sparsematrix();
                rawal = new double[0];
                rawau = new double[0];
                lcsrcidx = new int[0];
                nl = new double[0];
                nu = new double[0];
                hasmask = new bool[0];
                varmask = new sparse.sparsematrix();
                x0 = new double[0];
                reportx = new double[0];
                querydata = new double[0];
                replyfi = new double[0];
                replydj = new double[0];
                replysj = new sparse.sparsematrix();
                tmpx1 = new double[0];
                tmpc1 = new double[0];
                tmpf1 = new double[0];
                tmpg1 = new double[0];
                tmpj1 = new double[0,0];
                tmps1 = new sparse.sparsematrix();
                timerglobal = new apserv.stimer();
                timerprepareneighbors = new apserv.stimer();
                timerproposetrial = new apserv.stimer();
                xc = new double[0];
                nodecproducedbycut = new double[0];
                grid = new mirbfvnsgrid();
                dataset = new mirbfvnsdataset();
                xcneighbors = new int[0];
                xcreachedfrom = new int[0];
                xcreachedbycut = new double[0,0];
                xcqueryflags = new bool[0];
                evalbatchpoints = new double[0,0];
                evalbatchnodeidx = new int[0];
                evalbatchneighboridx = new int[0];
                unsafeglobalrng = new hqrnd.hqrndstate();
                maskint = new double[0];
                maskfrac = new double[0];
                idxint = new int[0];
                idxfrac = new int[0];
                xuneighbors = new int[0];
                xucuts = new double[0,0];
                xupoints = new double[0,0];
                xuflags = new bool[0];
                xtrial = new double[0];
                trialfi = new double[0];
                tmpeb0 = new int[0];
                tmpeb1 = new double[0];
                tmpeb2 = new int[0];
                dummytmp = new mirbfvnstemporaries();
                densedummy2 = new double[0,0];
                rpool = alglib.ap.nxpool.new_nrpool();
                tmppool = new alglib.smp.shared_pool();
                rstate = new rcommstate();
            }
            public override alglib.apobject make_copy()
            {
                mirbfvnsstate _result = new mirbfvnsstate();
                _result.n = n;
                _result.criteria = criteria!=null ? (optserv.nlpstoppingcriteria)criteria.make_copy() : null;
                _result.algomode = algomode;
                _result.budget = budget;
                _result.maxneighborhood = maxneighborhood;
                _result.batchsize = batchsize;
                _result.expandneighborhoodonstart = expandneighborhoodonstart;
                _result.retrylastcut = retrylastcut;
                _result.convexityflag = convexityflag;
                _result.ctol = ctol;
                _result.epsf = epsf;
                _result.quickepsf = quickepsf;
                _result.epsx = epsx;
                _result.adaptiveinternalparallelism = adaptiveinternalparallelism;
                _result.timeout = timeout;
                _result.s = (double[])s.Clone();
                _result.bndl = (double[])bndl.Clone();
                _result.bndu = (double[])bndu.Clone();
                _result.hasbndl = (bool[])hasbndl.Clone();
                _result.hasbndu = (bool[])hasbndu.Clone();
                _result.finitebndl = (double[])finitebndl.Clone();
                _result.finitebndu = (double[])finitebndu.Clone();
                _result.isintegral = (bool[])isintegral.Clone();
                _result.isbinary = (bool[])isbinary.Clone();
                _result.rawa = rawa!=null ? (sparse.sparsematrix)rawa.make_copy() : null;
                _result.rawal = (double[])rawal.Clone();
                _result.rawau = (double[])rawau.Clone();
                _result.lcsrcidx = (int[])lcsrcidx.Clone();
                _result.lccnt = lccnt;
                _result.haslinearlyconstrainedints = haslinearlyconstrainedints;
                _result.nnlc = nnlc;
                _result.nl = (double[])nl.Clone();
                _result.nu = (double[])nu.Clone();
                _result.nomask = nomask;
                _result.hasmask = (bool[])hasmask.Clone();
                _result.varmask = varmask!=null ? (sparse.sparsematrix)varmask.make_copy() : null;
                _result.hasx0 = hasx0;
                _result.x0 = (double[])x0.Clone();
                _result.requesttype = requesttype;
                _result.reportx = (double[])reportx.Clone();
                _result.reportf = reportf;
                _result.querysize = querysize;
                _result.queryfuncs = queryfuncs;
                _result.queryvars = queryvars;
                _result.querydim = querydim;
                _result.queryformulasize = queryformulasize;
                _result.querydata = (double[])querydata.Clone();
                _result.replyfi = (double[])replyfi.Clone();
                _result.replydj = (double[])replydj.Clone();
                _result.replysj = replysj!=null ? (sparse.sparsematrix)replysj.make_copy() : null;
                _result.tmpx1 = (double[])tmpx1.Clone();
                _result.tmpc1 = (double[])tmpc1.Clone();
                _result.tmpf1 = (double[])tmpf1.Clone();
                _result.tmpg1 = (double[])tmpg1.Clone();
                _result.tmpj1 = (double[,])tmpj1.Clone();
                _result.tmps1 = tmps1!=null ? (sparse.sparsematrix)tmps1.make_copy() : null;
                _result.userterminationneeded = userterminationneeded;
                _result.repnfev = repnfev;
                _result.repsubsolverits = repsubsolverits;
                _result.repiterationscount = repiterationscount;
                _result.repterminationtype = repterminationtype;
                _result.timerglobal = timerglobal!=null ? (apserv.stimer)timerglobal.make_copy() : null;
                _result.timerprepareneighbors = timerprepareneighbors!=null ? (apserv.stimer)timerprepareneighbors.make_copy() : null;
                _result.timerproposetrial = timerproposetrial!=null ? (apserv.stimer)timerproposetrial.make_copy() : null;
                _result.explorativetrialcnt = explorativetrialcnt;
                _result.explorativetrialtimems = explorativetrialtimems;
                _result.localtrialsamplingcnt = localtrialsamplingcnt;
                _result.localtrialsamplingtimems = localtrialsamplingtimems;
                _result.localtrialrbfcnt = localtrialrbfcnt;
                _result.localtrialrbftimems = localtrialrbftimems;
                _result.cutcnt = cutcnt;
                _result.cuttimems = cuttimems;
                _result.dbgpotentiallyparallelbatches = dbgpotentiallyparallelbatches;
                _result.dbgsequentialbatches = dbgsequentialbatches;
                _result.dbgpotentiallyparallelcutrounds = dbgpotentiallyparallelcutrounds;
                _result.dbgsequentialcutrounds = dbgsequentialcutrounds;
                _result.prepareevaluationbatchparallelism = prepareevaluationbatchparallelism;
                _result.expandcutgenerateneighborsparallelism = expandcutgenerateneighborsparallelism;
                _result.doanytrace = doanytrace;
                _result.dotrace = dotrace;
                _result.doextratrace = doextratrace;
                _result.dolaconictrace = dolaconictrace;
                _result.xc = (double[])xc.Clone();
                _result.fc = fc;
                _result.mxc = mxc;
                _result.hc = hc;
                _result.nodec = nodec;
                _result.nodecproducedbycut = (double[])nodecproducedbycut.Clone();
                _result.grid = grid!=null ? (mirbfvnsgrid)grid.make_copy() : null;
                _result.dataset = dataset!=null ? (mirbfvnsdataset)dataset.make_copy() : null;
                _result.nfrac = nfrac;
                _result.nint = nint;
                _result.xcneighbors = (int[])xcneighbors.Clone();
                _result.xcreachedfrom = (int[])xcreachedfrom.Clone();
                _result.xcreachedbycut = (double[,])xcreachedbycut.Clone();
                _result.xcqueryflags = (bool[])xcqueryflags.Clone();
                _result.xcneighborscnt = xcneighborscnt;
                _result.xcpriorityneighborscnt = xcpriorityneighborscnt;
                _result.evalbatchsize = evalbatchsize;
                _result.evalbatchpoints = (double[,])evalbatchpoints.Clone();
                _result.evalbatchnodeidx = (int[])evalbatchnodeidx.Clone();
                _result.evalbatchneighboridx = (int[])evalbatchneighboridx.Clone();
                _result.outofbudget = outofbudget;
                _result.unsafeglobalrng = unsafeglobalrng!=null ? (hqrnd.hqrndstate)unsafeglobalrng.make_copy() : null;
                _result.maskint = (double[])maskint.Clone();
                _result.maskfrac = (double[])maskfrac.Clone();
                _result.idxint = (int[])idxint.Clone();
                _result.idxfrac = (int[])idxfrac.Clone();
                _result.xuneighbors = (int[])xuneighbors.Clone();
                _result.xucuts = (double[,])xucuts.Clone();
                _result.xupoints = (double[,])xupoints.Clone();
                _result.xuflags = (bool[])xuflags.Clone();
                _result.xtrial = (double[])xtrial.Clone();
                _result.trialfi = (double[])trialfi.Clone();
                _result.tmpeb0 = (int[])tmpeb0.Clone();
                _result.tmpeb1 = (double[])tmpeb1.Clone();
                _result.tmpeb2 = (int[])tmpeb2.Clone();
                _result.dummytmp = dummytmp!=null ? (mirbfvnstemporaries)dummytmp.make_copy() : null;
                _result.densedummy2 = (double[,])densedummy2.Clone();
                _result.rpool = rpool!=null ? (alglib.ap.nxpool)rpool.make_copy() : null;
                _result.tmppool = tmppool!=null ? (alglib.smp.shared_pool)tmppool.make_copy() : null;
                _result.rstate = rstate!=null ? (rcommstate)rstate.make_copy() : null;
                return _result;
            }
        };




        public const int nodeunexplored = 0;
        public const int nodeinprogress = 1;
        public const int nodesolved = 2;
        public const int nodebad = 3;
        public const int ncolstatus = 0;
        public const int ncolneighborbegin = 1;
        public const int ncolneighborend = 2;
        public const int ncolfbest = 3;
        public const int ncolhbest = 4;
        public const int ncolmxbest = 5;
        public const int ncollastaccepted = 6;
        public const int maxprimalcandforcut = 10;
        public const int softmaxnodescoeff = 10;
        public const int safetyboxforbbgd = 5;
        public const int rbfcloudsizemultiplier = 4;
        public const int rbfminimizeitsperphase = 5;
        public const double rbfsubsolverepsx = 0.00001;
        public const double eta2 = 0.7;
        public const double gammadec = 0.5;
        public const double gammadec2 = 0.66;
        public const double gammadec3 = 0.05;
        public const double gammainc = 2.0;
        public const double gammainc2 = 4.0;
        public const double rbfpointunacceptablyfar = 10.0;
        public const double rbfpointtooclose = 0.01;
        public const double rbfsktooshort = 0.01;
        public const double habovezero = 50.0;
        public const int maxipmits = 200;


        /*************************************************************************
        MIRBFVNS solver initialization.
        --------------------------------------------------------------------------

          -- ALGLIB --
             Copyright 01.01.2025 by Bochkanov Sergey
        *************************************************************************/
        public static void mirbfvnscreatebuf(int n,
            double[] bndl,
            double[] bndu,
            double[] s,
            double[] x0,
            bool[] isintegral,
            bool[] isbinary,
            sparse.sparsematrix sparsea,
            double[] al,
            double[] au,
            int[] lcsrcidx,
            int lccnt,
            double[] nl,
            double[] nu,
            int nnlc,
            int algomode,
            int budget,
            int maxneighborhood,
            int batchsize,
            int timeout,
            int tracelevel,
            mirbfvnsstate state,
            alglib.xparams _params)
        {
            int i = 0;
            int j0 = 0;
            int j1 = 0;
            int jj = 0;

            alglib.ap.assert(n>=1, "MIRBFVNSCreateBuf: N<1");
            alglib.ap.assert(alglib.ap.len(x0)>=n, "MIRBFVNSCreateBuf: Length(X0)<N");
            alglib.ap.assert(apserv.isfinitevector(x0, n, _params), "MIRBFVNSCreateBuf: X contains infinite or NaN values");
            alglib.ap.assert(alglib.ap.len(bndl)>=n, "MIRBFVNSCreateBuf: Length(BndL)<N");
            alglib.ap.assert(alglib.ap.len(bndu)>=n, "MIRBFVNSCreateBuf: Length(BndU)<N");
            alglib.ap.assert(alglib.ap.len(s)>=n, "MIRBFVNSCreateBuf: Length(S)<N");
            alglib.ap.assert(alglib.ap.len(isintegral)>=n, "MIRBFVNSCreateBuf: Length(IsIntegral)<N");
            alglib.ap.assert(alglib.ap.len(isbinary)>=n, "MIRBFVNSCreateBuf: Length(IsBinary)<N");
            alglib.ap.assert(nnlc>=0, "MIRBFVNSCreateBuf: NNLC<0");
            alglib.ap.assert(alglib.ap.len(nl)>=nnlc, "MIRBFVNSCreateBuf: Length(NL)<NNLC");
            alglib.ap.assert(alglib.ap.len(nu)>=nnlc, "MIRBFVNSCreateBuf: Length(NU)<NNLC");
            alglib.ap.assert(budget>=0, "MIRBFVNSCreateBuf: Length(NU)<NNLC");
            alglib.ap.assert(timeout>=0, "MIRBFVNSCreateBuf: Timeout<0");
            alglib.ap.assert(((tracelevel==0 || tracelevel==1) || tracelevel==2) || tracelevel==3, "MIRBFVNSCreateBuf: unexpected trace level");
            alglib.ap.assert(algomode==0 || algomode==1, "MIRBFVNSCreateBuf: unexpected AlgoMode");
            initinternal(n, x0, 0, 0.0, state, _params);
            state.algomode = algomode;
            state.expandneighborhoodonstart = true;
            state.retrylastcut = true;
            state.budget = budget;
            state.maxneighborhood = maxneighborhood;
            state.batchsize = batchsize;
            state.timeout = timeout;
            state.dotrace = tracelevel>=2;
            state.doextratrace = tracelevel>=3;
            state.dolaconictrace = tracelevel==1;
            state.doanytrace = state.dotrace || state.dolaconictrace;
            for(i=0; i<=n-1; i++)
            {
                alglib.ap.assert(math.isfinite(bndl[i]) || Double.IsNegativeInfinity(bndl[i]), "MIRBFVNSCreateBuf: BndL contains NAN or +INF");
                alglib.ap.assert(math.isfinite(bndu[i]) || Double.IsPositiveInfinity(bndu[i]), "MIRBFVNSCreateBuf: BndL contains NAN or -INF");
                alglib.ap.assert(isintegral[i] || !isbinary[i], "MIRBFVNSCreateBuf: variable marked as binary but not integral");
                alglib.ap.assert(math.isfinite(s[i]), "MIRBFVNSCreateBuf: S contains infinite or NAN elements");
                alglib.ap.assert((double)(s[i])!=(double)(0), "MIRBFVNSCreateBuf: S contains zero elements");
                state.bndl[i] = bndl[i];
                state.hasbndl[i] = math.isfinite(bndl[i]);
                state.bndu[i] = bndu[i];
                state.hasbndu[i] = math.isfinite(bndu[i]);
                state.isintegral[i] = isintegral[i];
                state.isbinary[i] = isbinary[i];
                state.s[i] = Math.Abs(s[i]);
            }
            state.lccnt = lccnt;
            state.haslinearlyconstrainedints = false;
            if( lccnt>0 )
            {
                sparse.sparsecopytocrsbuf(sparsea, state.rawa, _params);
                ablasf.rcopyallocv(lccnt, al, ref state.rawal, _params);
                ablasf.rcopyallocv(lccnt, au, ref state.rawau, _params);
                ablasf.icopyallocv(lccnt, lcsrcidx, ref state.lcsrcidx, _params);
                for(i=0; i<=lccnt-1; i++)
                {
                    j0 = state.rawa.ridx[i];
                    j1 = state.rawa.ridx[i+1]-1;
                    for(jj=j0; jj<=j1; jj++)
                    {
                        state.haslinearlyconstrainedints = state.haslinearlyconstrainedints || isintegral[state.rawa.idx[jj]];
                    }
                }
            }
            state.nnlc = nnlc;
            ablasf.rallocv(nnlc, ref state.nl, _params);
            ablasf.rallocv(nnlc, ref state.nu, _params);
            for(i=0; i<=nnlc-1; i++)
            {
                alglib.ap.assert(math.isfinite(nl[i]) || Double.IsNegativeInfinity(nl[i]), "MIRBFVNSCreateBuf: NL[i] is +INF or NAN");
                alglib.ap.assert(math.isfinite(nu[i]) || Double.IsPositiveInfinity(nu[i]), "MIRBFVNSCreateBuf: NU[i] is -INF or NAN");
                state.nl[i] = nl[i];
                state.nu[i] = nu[i];
            }
        }


        /*************************************************************************
        Set tolerance for violation of nonlinear constraints

          -- ALGLIB --
             Copyright 01.01.2025 by Bochkanov Sergey
        *************************************************************************/
        public static void mirbfvnssetctol(mirbfvnsstate state,
            double ctol,
            alglib.xparams _params)
        {
            state.ctol = ctol;
        }


        /*************************************************************************
        Set subsolver stopping condition

          -- ALGLIB --
             Copyright 01.01.2025 by Bochkanov Sergey
        *************************************************************************/
        public static void mirbfvnssetepsf(mirbfvnsstate state,
            double epsf,
            alglib.xparams _params)
        {
            state.epsf = epsf;
        }


        /*************************************************************************
        Set subsolver stopping condition

          -- ALGLIB --
             Copyright 01.01.2025 by Bochkanov Sergey
        *************************************************************************/
        public static void mirbfvnssetepsx(mirbfvnsstate state,
            double epsx,
            alglib.xparams _params)
        {
            state.epsx = epsx;
        }


        /*************************************************************************
        Set variable mask

          -- ALGLIB --
             Copyright 01.01.2025 by Bochkanov Sergey
        *************************************************************************/
        public static void mirbfvnssetvariablemask(mirbfvnsstate state,
            bool[] hasmask,
            sparse.sparsematrix mask,
            alglib.xparams _params)
        {
            state.nomask = false;
            ablasf.bcopyallocv(1+state.nnlc, hasmask, ref state.hasmask, _params);
            sparse.sparsecopybuf(mask, state.varmask, _params);
        }


        /*************************************************************************
        Set adaptive internal parallelism:
        * +1 for 'favor parallelism', turn off when proved that serial is better
        *  0 for 'cautious parallelism', start serially, use SMP when proved that SMP is better
        * -1 for 'no adaptiveness', always start SMP when allowed to do so

          -- ALGLIB --
             Copyright 01.01.2025 by Bochkanov Sergey
        *************************************************************************/
        public static void mirbfvnssetadaptiveinternalparallelism(mirbfvnsstate state,
            int smpmode,
            alglib.xparams _params)
        {
            state.adaptiveinternalparallelism = smpmode;
        }


        /*************************************************************************


          -- ALGLIB --
             Copyright 01.01.2025 by Bochkanov Sergey
        *************************************************************************/
        public static bool mirbfvnsiteration(mirbfvnsstate state,
            alglib.xparams _params)
        {
            bool result = new bool();
            int n = 0;
            int nnlc = 0;
            int i = 0;
            int j = 0;
            int k = 0;
            int newneighbors = 0;
            int offs = 0;
            double v = 0;
            double v0 = 0;
            double v1 = 0;
            double lcerr = 0;
            bool bflag = new bool();

            
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
                nnlc = state.rstate.ia[1];
                i = state.rstate.ia[2];
                j = state.rstate.ia[3];
                k = state.rstate.ia[4];
                newneighbors = state.rstate.ia[5];
                offs = state.rstate.ia[6];
                bflag = state.rstate.ba[0];
                v = state.rstate.ra[0];
                v0 = state.rstate.ra[1];
                v1 = state.rstate.ra[2];
                lcerr = state.rstate.ra[3];
            }
            else
            {
                n = 359;
                nnlc = -58;
                i = -919;
                j = -909;
                k = 81;
                newneighbors = 255;
                offs = 74;
                bflag = false;
                v = 809.0;
                v0 = 205.0;
                v1 = -838.0;
                lcerr = 939.0;
            }
            if( state.rstate.stage==0 )
            {
                goto lbl_0;
            }
            if( state.rstate.stage==1 )
            {
                goto lbl_1;
            }
            
            //
            // Routine body
            //
            n = state.n;
            nnlc = state.nnlc;
            apserv.stimerinit(state.timerglobal, _params);
            apserv.stimerinit(state.timerprepareneighbors, _params);
            apserv.stimerinit(state.timerproposetrial, _params);
            apserv.stimerstart(state.timerglobal, _params);
            clearoutputs(state, _params);
            alglib.smp.ae_shared_pool_set_seed(state.tmppool, state.dummytmp);
            state.rpool.alloc_double(apserv.imax3(n, state.lccnt, 1+nnlc, _params));
            ablasf.rsetallocv(n, 0.0, ref state.maskint, _params);
            ablasf.rsetallocv(n, 0.0, ref state.maskfrac, _params);
            ablasf.isetallocv(n, -1, ref state.idxint, _params);
            ablasf.isetallocv(n, -1, ref state.idxfrac, _params);
            state.nfrac = 0;
            state.nint = 0;
            for(i=0; i<=n-1; i++)
            {
                if( state.isintegral[i] )
                {
                    state.maskint[i] = 1.0;
                    state.idxint[state.nint] = i;
                    state.nint = state.nint+1;
                }
                else
                {
                    state.maskfrac[i] = 1.0;
                    state.idxfrac[state.nfrac] = i;
                    state.nfrac = state.nfrac+1;
                }
            }
            ablasf.rcopyallocv(n, state.x0, ref state.xc, _params);
            ablasf.rallocv(n, ref state.tmpx1, _params);
            ablasf.rallocv(1+state.nnlc, ref state.tmpf1, _params);
            state.prepareevaluationbatchparallelism = state.adaptiveinternalparallelism==1 || state.adaptiveinternalparallelism==-1;
            state.expandcutgenerateneighborsparallelism = state.adaptiveinternalparallelism==1 || state.adaptiveinternalparallelism==-1;
            if( state.doanytrace )
            {
                alglib.ap.trace("> preparing initial point\n");
            }
            ablasf.rsetallocv(n, -Math.Sqrt(math.maxrealnumber), ref state.finitebndl, _params);
            ablasf.rsetallocv(n, Math.Sqrt(math.maxrealnumber), ref state.finitebndu, _params);
            for(i=0; i<=n-1; i++)
            {
                if( state.hasbndl[i] && state.hasbndu[i] )
                {
                    if( (double)(state.bndu[i])<(double)(state.bndl[i]) )
                    {
                        if( state.doanytrace )
                        {
                            alglib.ap.trace(System.String.Format(">> error: box constraint {0,0:d} is infeasible (bndU<bndL)\n", i));
                        }
                        state.repterminationtype = -3;
                        result = false;
                        return result;
                    }
                    if( state.isintegral[i] && (double)((int)Math.Floor(state.bndu[i]))<(double)(state.bndl[i]) )
                    {
                        if( state.doanytrace )
                        {
                            alglib.ap.trace(System.String.Format(">> error: box constraint {0,0:d} is incompatible with integrality constraints\n", i));
                        }
                        state.repterminationtype = -3;
                        result = false;
                        return result;
                    }
                }
                if( state.isintegral[i] )
                {
                    if( state.hasbndl[i] && (double)(state.bndl[i])!=(double)((int)Math.Round(state.bndl[i])) )
                    {
                        state.bndl[i] = (int)Math.Ceiling(state.bndl[i]);
                    }
                    if( state.hasbndu[i] && (double)(state.bndu[i])!=(double)((int)Math.Round(state.bndu[i])) )
                    {
                        state.bndu[i] = (int)Math.Floor(state.bndu[i]);
                    }
                }
                if( state.isbinary[i] )
                {
                    if( Double.IsNegativeInfinity(state.bndl[i]) || (double)(state.bndl[i])<(double)(0) )
                    {
                        state.bndl[i] = 0;
                    }
                    if( Double.IsPositiveInfinity(state.bndu[i]) || (double)(state.bndu[i])>(double)(1) )
                    {
                        state.bndu[i] = 1;
                    }
                }
                state.hasbndl[i] = state.hasbndl[i] || math.isfinite(state.bndl[i]);
                state.hasbndu[i] = state.hasbndu[i] || math.isfinite(state.bndu[i]);
                if( state.hasbndl[i] )
                {
                    state.finitebndl[i] = state.bndl[i];
                }
                if( state.hasbndu[i] )
                {
                    state.finitebndu[i] = state.bndu[i];
                }
            }
            bflag = prepareinitialpoint(state, state.xc, ref lcerr, _params);
            if( !bflag || (double)(lcerr)>(double)(state.ctol) )
            {
                if( state.doanytrace )
                {
                    alglib.ap.trace(">> error: box, linear and integrality constraints together are inconsistent; declaring infeasibility\n");
                }
                state.repterminationtype = -3;
                result = false;
                return result;
            }
            ablasf.rcopyallocv(n, state.xc, ref state.querydata, _params);
            ablasf.rallocv(1+nnlc, ref state.replyfi, _params);
            state.requesttype = 4;
            state.querysize = 1;
            state.queryfuncs = 1+nnlc;
            state.queryvars = n;
            state.querydim = 0;
            state.rstate.stage = 0;
            goto lbl_rcomm;
        lbl_0:
            state.repnfev = state.repnfev+1;
            if( !apserv.isfinitevector(state.replyfi, 1+nnlc, _params) )
            {
                if( state.doanytrace )
                {
                    alglib.ap.trace(">> error: the initial point has objective or one of nonlinear constraints equal to NAN/INF\n>> unable to restore, stop\n");
                }
                state.repterminationtype = -3;
                result = false;
                return result;
            }
            state.fc = state.replyfi[0];
            computeviolation2(state, state.xc, state.replyfi, ref state.hc, ref state.mxc, _params);
            datasetinitempty(state.dataset, state, _params);
            state.nodec = gridcreate(state.grid, state, state.xc, state.replyfi, state.hc, state.mxc, _params);
            ablasf.rsetallocv(n, 0.0, ref state.nodecproducedbycut, _params);
            if( state.doanytrace )
            {
                alglib.ap.trace(System.String.Format(">> done; the initial grid node N{0,0:d} is created\n", state.nodec));
            }
            state.repterminationtype = 0;
            state.outofbudget = false;
        lbl_2:
            if( false )
            {
                goto lbl_3;
            }
            ablasf.isetallocv(1, state.nodec, ref state.xcneighbors, _params);
            ablasf.isetallocv(1, state.nodec, ref state.xcreachedfrom, _params);
            ablasf.bsetallocv(1, false, ref state.xcqueryflags, _params);
            ablasf.rgrowrowsfixedcolsm(1, n, ref state.xcreachedbycut, _params);
            ablasf.rsetr(n, 0.0, state.xcreachedbycut, 0, _params);
            state.xcneighborscnt = 1;
            state.xcpriorityneighborscnt = 1;
            if( state.expandneighborhoodonstart )
            {
                expandneighborhood(state, _params);
            }
            if( state.dotrace )
            {
                alglib.ap.trace(System.String.Format("> starting search from node {0,0:d}", state.nodec));
                if( state.xcneighborscnt>1 )
                {
                    alglib.ap.trace(System.String.Format(" with expanded neighborhood of {0,0:d} nodes", state.xcneighborscnt-1));
                }
                alglib.ap.trace(System.String.Format(" (F={0,0:E6},H={1,0:E6})\n", gridgetfbest(state.grid, state, state.nodec, _params), gridgethbest(state.grid, state, state.nodec, _params)));
            }
        lbl_4:
            if( false )
            {
                goto lbl_5;
            }
            k = state.nodec;
            j = 0;
            for(i=0; i<=state.xcneighborscnt-1; i++)
            {
                if( gridisbetter(state.grid, state, k, state.xcneighbors[i], _params) )
                {
                    k = state.xcneighbors[i];
                    j = i;
                }
            }
            if( k!=state.nodec )
            {
                if( state.dotrace )
                {
                    alglib.ap.trace(System.String.Format(">> found better neighbor, switching to node {0,0:d} (F={1,0:E6},H={2,0:E6})\n", k, gridgetfbest(state.grid, state, k, _params), gridgethbest(state.grid, state, k, _params)));
                }
                state.nodec = k;
                ablasf.rcopyrv(n, state.xcreachedbycut, j, state.nodecproducedbycut, _params);
                goto lbl_5;
            }
            if( state.budget>0 && state.repnfev>=state.budget )
            {
                if( state.doanytrace )
                {
                    alglib.ap.trace("> iteration budget exhausted, stopping\n");
                }
                state.repterminationtype = 5;
                state.outofbudget = true;
                goto lbl_5;
            }
            if( state.timeout>0 && (double)(apserv.stimergetmsrunning(state.timerglobal, _params))>(double)(state.timeout) )
            {
                if( state.doanytrace )
                {
                    alglib.ap.trace("> time budget exhausted, stopping\n");
                }
                state.repterminationtype = 5;
                state.outofbudget = true;
                goto lbl_5;
            }
            prepareevaluationbatch(state, _params);
            if( state.evalbatchsize<=0 )
            {
                goto lbl_6;
            }
            state.requesttype = 4;
            state.querysize = state.evalbatchsize;
            state.queryfuncs = 1+nnlc;
            state.queryvars = n;
            state.querydim = 0;
            ablasf.rallocv(n*state.evalbatchsize, ref state.querydata, _params);
            offs = 0;
            for(i=0; i<=state.evalbatchsize-1; i++)
            {
                for(k=0; k<=n-1; k++)
                {
                    state.querydata[offs+k] = state.evalbatchpoints[i,k];
                }
                offs = offs+n;
            }
            ablasf.rallocv((1+nnlc)*state.evalbatchsize, ref state.replyfi, _params);
            state.rstate.stage = 1;
            goto lbl_rcomm;
        lbl_1:
            state.repnfev = state.repnfev+state.evalbatchsize;
            ablasf.rallocv(n, ref state.xtrial, _params);
            ablasf.rallocv(1+nnlc, ref state.trialfi, _params);
            for(i=0; i<=state.evalbatchsize-1; i++)
            {
                ablasf.rcopyrv(n, state.evalbatchpoints, i, state.xtrial, _params);
                ablasf.rcopyvx(1+nnlc, state.replyfi, i*(1+nnlc), state.trialfi, 0, _params);
                gridsendtrialpointto(state.grid, state, state.nodec, state.evalbatchnodeidx[i], state.xtrial, state.trialfi, _params);
            }
            if( state.dotrace )
            {
                if( gridgetbestlastacceptedinunsolvedneighborhood(state.grid, state, state.xcneighbors, state.xcneighborscnt, ref k, ref v, ref v0, ref v1, _params) )
                {
                    alglib.ap.trace(System.String.Format(">> improving neighborhood, {0,3:d} evaluations, best unsolved node is {1,6:d}: f={2,15:E6}, sum(viol)={3,0:E2}, max(viol)={4,0:E2}\n", state.evalbatchsize, k, v, v0, v1));
                }
                else
                {
                    gridgetbestinneighborhood(state.grid, state, state.xcneighbors, state.xcneighborscnt, ref v, ref v0, ref v1, _params);
                    alglib.ap.trace(System.String.Format(">> improving neighborhood, {0,3:d} evaluations, all nodes are  solved,  best:  f={1,15:E6}, sum(viol)={2,0:E2}, max(viol)={3,0:E2}\n", state.evalbatchsize, v, v0, v1));
                }
            }
            if( state.evalbatchsize<state.batchsize && (state.maxneighborhood==0 || state.xcneighborscnt-1<state.maxneighborhood) )
            {
                newneighbors = expandneighborhood(state, _params);
                if( state.dotrace )
                {
                    alglib.ap.trace(System.String.Format("> evaluation batch is not fully used ({0,0:d} out of {1,0:d}), expanding neighborhood: {2,0:d} new neighbor(s), |neighborhood|={3,0:d}\n", state.evalbatchsize, state.batchsize, newneighbors, state.xcneighborscnt));
                }
            }
            goto lbl_4;
        lbl_6:
            if( state.maxneighborhood>0 && state.xcneighborscnt-1>=state.maxneighborhood )
            {
                if( state.doanytrace )
                {
                    alglib.ap.trace(System.String.Format("> the neighborhood size exceeds limit ({0,0:d}+1), stopping\n", state.maxneighborhood));
                }
                state.repterminationtype = 2;
                goto lbl_5;
            }
            newneighbors = expandneighborhood(state, _params);
            if( newneighbors==0 )
            {
                if( state.doanytrace )
                {
                    alglib.ap.trace("> the integer grid was completely scanned, stopping\n");
                }
                state.repterminationtype = 1;
                goto lbl_5;
            }
            if( state.dotrace )
            {
                alglib.ap.trace(System.String.Format(">> expanding neighborhood, {0,0:d} new neighbor(s), |neighborhood|={1,0:d}\n", newneighbors, state.xcneighborscnt));
            }
            goto lbl_4;
        lbl_5:
            if( state.repterminationtype!=0 )
            {
                goto lbl_3;
            }
            goto lbl_2;
        lbl_3:
            gridoffloadbestpoint(state.grid, state, state.nodec, ref state.xc, ref k, ref state.fc, ref state.hc, ref state.mxc, _params);
            if( (double)(state.mxc)>(double)(state.ctol) )
            {
                state.repterminationtype = apserv.icase2(state.outofbudget, -33, -3, _params);
            }
            apserv.stimerstop(state.timerglobal, _params);
            if( state.doanytrace )
            {
                alglib.ap.trace("\n=== STOPPED ========================================================================================\n");
                alglib.ap.trace(System.String.Format("raw target:     {0,20:E12}\n", state.fc));
                alglib.ap.trace(System.String.Format("max.violation:  {0,20:E12}\n", state.mxc));
                alglib.ap.trace(System.String.Format("evaluations:    {0,6:d}\n", state.repnfev));
                alglib.ap.trace(System.String.Format("subsolver its:  {0,6:d}\n", state.repsubsolverits));
                alglib.ap.trace(System.String.Format("integral nodes: {0,6:d}\n", state.grid.nnodes));
                alglib.ap.trace(System.String.Format("total time:     {0,10:F1} ms (wall-clock)\n", apserv.stimergetms(state.timerglobal, _params)));
                alglib.ap.trace("\nDetailed time (wall-clock):\n");
                alglib.ap.trace(System.String.Format("gen neighbors:  {0,10:F1} ms (wall-clock)\n", apserv.stimergetms(state.timerprepareneighbors, _params)));
                alglib.ap.trace(System.String.Format("propose trial:  {0,10:F1} ms (wall-clock)\n", apserv.stimergetms(state.timerproposetrial, _params)));
                alglib.ap.trace("\nAdvanced statistics:\n");
                alglib.ap.trace("> neighborhood-generating cuts:\n");
                alglib.ap.trace(System.String.Format("avg.time:       {0,0:F1} ms\n", (double)state.cuttimems/(double)Math.Max(state.cutcnt, 1)));
                alglib.ap.trace(System.String.Format("count:          {0,0:d}\n", state.cutcnt));
                alglib.ap.trace(">> sequential and potentially parallel rounds:\n");
                alglib.ap.trace(System.String.Format("sequential:     {0,0:d}\n", state.dbgsequentialcutrounds));
                alglib.ap.trace(System.String.Format("parallel:       {0,0:d}\n", state.dbgpotentiallyparallelcutrounds));
                alglib.ap.trace("> integer node explorations and continuous subspace searches (callback time not included):\n");
                alglib.ap.trace(">> initial exploration:\n");
                alglib.ap.trace(System.String.Format("avg.time:       {0,0:F1} ms\n", (double)state.explorativetrialtimems/(double)Math.Max(state.explorativetrialcnt, 1)));
                alglib.ap.trace(System.String.Format("count:          {0,0:d}\n", state.explorativetrialcnt));
                alglib.ap.trace(">> random sampling around initial point:\n");
                alglib.ap.trace(System.String.Format("avg.time:       {0,0:F1} ms\n", (double)state.localtrialsamplingtimems/(double)Math.Max(state.localtrialsamplingcnt, 1)));
                alglib.ap.trace(System.String.Format("count:          {0,0:d}\n", state.localtrialsamplingcnt));
                alglib.ap.trace(">> surrogate model optimizations:\n");
                alglib.ap.trace(System.String.Format("avg.time:       {0,0:F1} ms\n", (double)state.localtrialrbftimems/(double)Math.Max(state.localtrialrbfcnt, 1)));
                alglib.ap.trace(System.String.Format("count:          {0,0:d}\n", state.localtrialrbfcnt));
                alglib.ap.trace(">> sequential and potentially parallel batches:\n");
                alglib.ap.trace(System.String.Format("sequential:     {0,0:d}\n", state.dbgsequentialbatches));
                alglib.ap.trace(System.String.Format("parallel:       {0,0:d}\n", state.dbgpotentiallyparallelbatches));
            }
            result = false;
            return result;
            
            //
            // Saving state
            //
        lbl_rcomm:
            result = true;
            state.rstate.ia[0] = n;
            state.rstate.ia[1] = nnlc;
            state.rstate.ia[2] = i;
            state.rstate.ia[3] = j;
            state.rstate.ia[4] = k;
            state.rstate.ia[5] = newneighbors;
            state.rstate.ia[6] = offs;
            state.rstate.ba[0] = bflag;
            state.rstate.ra[0] = v;
            state.rstate.ra[1] = v0;
            state.rstate.ra[2] = v1;
            state.rstate.ra[3] = lcerr;
            return result;
        }


        /*************************************************************************
        Clears output fields during initialization
        *************************************************************************/
        private static void clearoutputs(mirbfvnsstate state,
            alglib.xparams _params)
        {
            state.userterminationneeded = false;
            state.repnfev = 0;
            state.repsubsolverits = 0;
            state.repiterationscount = 0;
            state.repterminationtype = 0;
        }


        /*************************************************************************
        Internal initialization subroutine.
        Sets default NLC solver with default criteria.
        *************************************************************************/
        private static void initinternal(int n,
            double[] x,
            int solvermode,
            double diffstep,
            mirbfvnsstate state,
            alglib.xparams _params)
        {
            int i = 0;
            double[,] c = new double[0,0];
            int[] ct = new int[0];

            state.convexityflag = 0;
            optserv.critinitdefault(state.criteria, _params);
            state.adaptiveinternalparallelism = 0;
            state.timeout = 0;
            state.ctol = 1.0E-5;
            state.epsf = 1.0E-5;
            state.epsx = 1.0E-5;
            state.quickepsf = 0.01;
            state.n = n;
            state.userterminationneeded = false;
            ablasf.bsetallocv(n, false, ref state.isintegral, _params);
            ablasf.bsetallocv(n, false, ref state.isbinary, _params);
            state.bndl = new double[n];
            state.hasbndl = new bool[n];
            state.bndu = new double[n];
            state.hasbndu = new bool[n];
            state.s = new double[n];
            state.x0 = new double[n];
            state.xc = new double[n];
            for(i=0; i<=n-1; i++)
            {
                state.bndl[i] = Double.NegativeInfinity;
                state.hasbndl[i] = false;
                state.bndu[i] = Double.PositiveInfinity;
                state.hasbndu[i] = false;
                state.s[i] = 1.0;
                state.x0[i] = x[i];
                state.xc[i] = x[i];
            }
            state.hasx0 = true;
            state.lccnt = 0;
            state.nnlc = 0;
            state.nomask = true;
            clearoutputs(state, _params);
            state.explorativetrialcnt = 0;
            state.explorativetrialtimems = 0;
            state.localtrialsamplingcnt = 0;
            state.localtrialsamplingtimems = 0;
            state.localtrialrbfcnt = 0;
            state.localtrialrbftimems = 0;
            state.cutcnt = 0;
            state.cuttimems = 0;
            state.dbgpotentiallyparallelbatches = 0;
            state.dbgsequentialbatches = 0;
            state.dbgpotentiallyparallelcutrounds = 0;
            state.dbgsequentialcutrounds = 0;
            hqrnd.hqrndseed(8543, 7455, state.unsafeglobalrng, _params);
            state.rstate.ia = new int[6+1];
            state.rstate.ba = new bool[0+1];
            state.rstate.ra = new double[3+1];
            state.rstate.stage = -1;
        }


        /*************************************************************************
        Prepare initial point that is feasible with respect to integrality, box
        and linear constraints (but may potentially violate nonlinear ones).

        X contains current approximation that is replaced by a point satisfying
        constraints. Error in constraints is returned as a result.

        If it is impossible to satisfy box, integrality and linear constraints
        simultaneously, an integer+box feasible point is returned, and LCErr is
        set to an error at this point.

        Box and integrality constraints are assumed to be compatible.

        Returns True on success, False on failure.
        *************************************************************************/
        private static bool prepareinitialpoint(mirbfvnsstate state,
            double[] x,
            ref double lcerr,
            alglib.xparams _params)
        {
            bool result = new bool();
            int n = 0;
            int i = 0;

            lcerr = 0;

            n = state.n;
            result = true;
            lcerr = 0;
            for(i=0; i<=n-1; i++)
            {
                if( state.hasbndl[i] )
                {
                    x[i] = Math.Max(x[i], state.bndl[i]);
                }
                if( state.hasbndu[i] )
                {
                    x[i] = Math.Min(x[i], state.bndu[i]);
                }
                if( state.isintegral[i] )
                {
                    x[i] = (int)Math.Round(x[i]);
                }
            }
            if( !state.haslinearlyconstrainedints )
            {
                return result;
            }
            ablasf.rsetallocm(1, n, 0.0, ref state.xucuts, _params);
            ablasf.rsetallocm(1, n, 0.0, ref state.xupoints, _params);
            ablasf.bsetallocv(1, false, ref state.xuflags, _params);
            findnearestintegralsubjecttocut(state, x, state.xucuts, state.xupoints, state.xuflags, 0, false, _params);
            result = state.xuflags[0];
            lcerr = 0;
            if( result )
            {
                ablasf.rcopyrv(n, state.xupoints, 0, x, _params);
            }
            return result;
        }


        /*************************************************************************
        Given current neighborhood (one stored in  State.XCNeighbors[]),  prepares
        up to State.BatchSize evaluation requests, depending on neighbor priorities,
        statuses (unexplored or in progress) and other factors.

        The evaluation batch is stored into:
        * EvalBatchSize, >=0
        * EvalBatchPoints, array[EvalBatchSize,N]
        * EvalBatchNodeIdx, array[EvalBatchSize], node index in the grid
        * EvalBatchNeighborIdx, array[EvalBatchSize], node index in XCNeighbors[] array
        *************************************************************************/
        private static void prepareevaluationbatch(mirbfvnsstate state,
            alglib.xparams _params)
        {
            int i = 0;
            int j = 0;
            int k = 0;
            int cntu = 0;
            int st = 0;
            int n = 0;
            bool addednodec = new bool();
            int expectedexplorationcnt = 0;
            int expectedsamplingcnt = 0;
            int expectedrbfcnt = 0;
            double avgexplorationtime = 0;
            double avgsamplingtime = 0;
            double avgrbftime = 0;
            double expectedexplorationtime = 0;
            double expectedsamplingtime = 0;
            double expectedrbftime = 0;
            bool sufficienttime = new bool();
            bool sufficientcount = new bool();

            n = state.n;
            state.evalbatchsize = 0;
            addednodec = false;
            alglib.ap.assert(state.nodec==state.xcneighbors[0] && gridgetstatus(state.grid, state, state.xcneighbors[0], _params)!=nodeunexplored, "MIRBFVNS: 989642 failed");
            for(i=0; i<=state.xcpriorityneighborscnt-1; i++)
            {
                if( state.evalbatchsize<state.batchsize && gridgetstatus(state.grid, state, state.xcneighbors[i], _params)==nodeunexplored )
                {
                    ablasf.igrowappendv(state.evalbatchsize+1, ref state.evalbatchnodeidx, state.xcneighbors[i], _params);
                    ablasf.igrowappendv(state.evalbatchsize+1, ref state.evalbatchneighboridx, i, _params);
                    state.evalbatchsize = state.evalbatchsize+1;
                }
            }
            if( state.evalbatchsize<state.batchsize )
            {
                cntu = 0;
                for(i=state.xcpriorityneighborscnt; i<=state.xcneighborscnt-1; i++)
                {
                    if( gridgetstatus(state.grid, state, state.xcneighbors[i], _params)==nodeunexplored )
                    {
                        ablasf.igrowappendv(cntu+1, ref state.tmpeb0, state.xcneighbors[i], _params);
                        ablasf.igrowappendv(cntu+1, ref state.tmpeb2, i, _params);
                        cntu = cntu+1;
                    }
                }
                for(i=0; i<=cntu-2; i++)
                {
                    j = i+hqrnd.hqrnduniformi(state.unsafeglobalrng, cntu-i, _params);
                    k = state.tmpeb0[i];
                    state.tmpeb0[i] = state.tmpeb0[j];
                    state.tmpeb0[j] = k;
                    k = state.tmpeb2[i];
                    state.tmpeb2[i] = state.tmpeb2[j];
                    state.tmpeb2[j] = k;
                }
                i = 0;
                while( state.evalbatchsize<state.batchsize && i<cntu )
                {
                    ablasf.igrowappendv(state.evalbatchsize+1, ref state.evalbatchnodeidx, state.tmpeb0[i], _params);
                    ablasf.igrowappendv(state.evalbatchsize+1, ref state.evalbatchneighboridx, state.tmpeb2[i], _params);
                    state.evalbatchsize = state.evalbatchsize+1;
                    i = i+1;
                }
            }
            alglib.ap.assert(state.nodec==state.xcneighbors[0] && !apserv.ilinearsearchispresent(state.evalbatchnodeidx, 0, state.evalbatchsize, state.nodec, _params), "MIRBFVNS: 023353 failed");
            if( gridgetstatus(state.grid, state, state.nodec, _params)==nodeinprogress )
            {
                if( state.evalbatchsize<state.batchsize && (state.batchsize>1 || (double)(hqrnd.hqrnduniformr(state.unsafeglobalrng, _params))<(double)(0.5)) )
                {
                    ablasf.igrowappendv(state.evalbatchsize+1, ref state.evalbatchnodeidx, state.nodec, _params);
                    ablasf.igrowappendv(state.evalbatchsize+1, ref state.evalbatchneighboridx, 0, _params);
                    state.evalbatchsize = state.evalbatchsize+1;
                    addednodec = true;
                }
            }
            if( state.evalbatchsize<state.batchsize )
            {
                cntu = 0;
                for(i=apserv.icase2(addednodec, 1, 0, _params); i<=state.xcneighborscnt-1; i++)
                {
                    st = gridgetstatus(state.grid, state, state.xcneighbors[i], _params);
                    if( (st==nodeunexplored || st==nodesolved) || st==nodebad )
                    {
                        continue;
                    }
                    if( st==nodeinprogress )
                    {
                        ablasf.igrowappendv(cntu+1, ref state.tmpeb0, state.xcneighbors[i], _params);
                        ablasf.igrowappendv(cntu+1, ref state.tmpeb2, i, _params);
                        cntu = cntu+1;
                        continue;
                    }
                    alglib.ap.assert(false, "MIRBFVNS: 047402 failed");
                }
                for(i=0; i<=cntu-2; i++)
                {
                    j = i+hqrnd.hqrnduniformi(state.unsafeglobalrng, cntu-i, _params);
                    k = state.tmpeb0[i];
                    state.tmpeb0[i] = state.tmpeb0[j];
                    state.tmpeb0[j] = k;
                    k = state.tmpeb2[i];
                    state.tmpeb2[i] = state.tmpeb2[j];
                    state.tmpeb2[j] = k;
                }
                i = 0;
                while( state.evalbatchsize<state.batchsize && i<cntu )
                {
                    ablasf.igrowappendv(state.evalbatchsize+1, ref state.evalbatchnodeidx, state.tmpeb0[i], _params);
                    ablasf.igrowappendv(state.evalbatchsize+1, ref state.evalbatchneighboridx, state.tmpeb2[i], _params);
                    state.evalbatchsize = state.evalbatchsize+1;
                    i = i+1;
                }
            }
            if( state.evalbatchsize>0 )
            {
                ablasf.rgrowrowsfixedcolsm(state.evalbatchsize, n, ref state.evalbatchpoints, _params);
                ablasf.iallocv(2*state.evalbatchsize, ref state.tmpeb0, _params);
                avgexplorationtime = state.explorativetrialtimems/apserv.coalesce(state.explorativetrialcnt, 1, _params);
                avgsamplingtime = state.localtrialsamplingtimems/apserv.coalesce(state.localtrialsamplingcnt, 1, _params);
                avgrbftime = state.localtrialrbftimems/apserv.coalesce(state.localtrialrbfcnt, 1, _params);
                expectedexplorationtime = 0;
                expectedexplorationcnt = 0;
                expectedsamplingtime = 0;
                expectedsamplingcnt = 0;
                expectedrbftime = 0;
                expectedrbfcnt = 0;
                for(i=0; i<=state.evalbatchsize-1; i++)
                {
                    alglib.ap.assert(state.evalbatchnodeidx[i]==state.xcneighbors[state.evalbatchneighboridx[i]], "MIRBFVNS: 089649");
                    state.tmpeb0[2*i+0] = hqrnd.hqrnduniformi(state.unsafeglobalrng, 1000000, _params);
                    state.tmpeb0[2*i+1] = hqrnd.hqrnduniformi(state.unsafeglobalrng, 1000000, _params);
                    if( gridgetstatus(state.grid, state, state.evalbatchnodeidx[i], _params)==nodeunexplored )
                    {
                        expectedexplorationtime = expectedexplorationtime+avgexplorationtime;
                        expectedexplorationcnt = expectedexplorationcnt+1;
                    }
                    else
                    {
                        if( (double)(gridgetpointscountinnode(state.grid, state, state.evalbatchnodeidx[i], _params))<(double)(state.nfrac+1) )
                        {
                            expectedsamplingtime = expectedsamplingtime+avgsamplingtime;
                            expectedsamplingcnt = expectedsamplingcnt+1;
                        }
                        else
                        {
                            expectedrbftime = expectedrbftime+avgrbftime;
                            expectedrbfcnt = expectedrbfcnt+1;
                        }
                    }
                }
                sufficienttime = (double)(state.explorativetrialtimems+state.localtrialsamplingtimems+state.localtrialrbftimems)>(double)(apserv.adaptiveparallelismtimerequired(_params));
                sufficientcount = (double)(state.explorativetrialcnt+state.localtrialsamplingcnt+state.localtrialrbfcnt)>(double)(apserv.adaptiveparallelismcountrequired(_params));
                if( state.adaptiveinternalparallelism>=0 && (sufficienttime || sufficientcount) )
                {
                    state.prepareevaluationbatchparallelism = false;
                    state.prepareevaluationbatchparallelism = state.prepareevaluationbatchparallelism || ((double)(expectedexplorationtime)>=(double)(apserv.workerstartthresholdms(_params)) && expectedexplorationcnt>=2);
                    state.prepareevaluationbatchparallelism = state.prepareevaluationbatchparallelism || ((double)(expectedsamplingtime)>=(double)(apserv.workerstartthresholdms(_params)) && expectedsamplingcnt>=2);
                    state.prepareevaluationbatchparallelism = state.prepareevaluationbatchparallelism || ((double)(expectedrbftime)>=(double)(apserv.workerstartthresholdms(_params)) && expectedrbfcnt>=2);
                }
                apserv.stimerstartcond(state.timerproposetrial, state.doanytrace, _params);
                gridparallelproposelocaltrialpoint(state.grid, state.grid, state, state, 0, state.evalbatchsize, true, state.prepareevaluationbatchparallelism, _params);
                apserv.stimerstopcond(state.timerproposetrial, state.doanytrace, _params);
                if( state.prepareevaluationbatchparallelism )
                {
                    state.dbgpotentiallyparallelbatches = state.dbgpotentiallyparallelbatches+1;
                }
                else
                {
                    state.dbgsequentialbatches = state.dbgsequentialbatches+1;
                }
            }
        }


        /*************************************************************************
        Expands current neighborhood (one stored in State.XCNeighbors[]) with  new
        neighbors

        The function returns the number of added neighbors. Zero means that the
        entire integer grid was scanned.

        Changes XCNeighbors[], XCReachedFrom[], XCNeighborsCnt, XCQueryFlags[].

        Uses XUNeighbors for temporary storage.
        *************************************************************************/
        private static int expandneighborhood(mirbfvnsstate state,
            alglib.xparams _params)
        {
            int result = 0;
            int n = 0;
            int i = 0;
            int nncnt = 0;
            int unmarked = 0;
            int unmarkedi = 0;
            int unsolvedcnt = 0;
            int newunsolvedcnt = 0;
            int st = 0;
            double[] xc = new double[0];
            double[] unmarkedcut = new double[0];
            int idummy = 0;
            double fc = 0;
            double hc = 0;
            double mxc = 0;

            n = state.n;
            state.rpool.retrieve(ref xc);
            state.rpool.retrieve(ref unmarkedcut);
            alglib.ap.assert(alglib.ap.len(xc)>=n && alglib.ap.len(unmarkedcut)>=n, "MIRBFVNS: 944420");
            result = 0;
            unsolvedcnt = 0;
            while( unsolvedcnt<state.batchsize )
            {
                unmarked = -1;
                unmarkedi = -1;
                for(i=0; i<=state.xcneighborscnt-1; i++)
                {
                    if( !state.xcqueryflags[i] )
                    {
                        st = gridgetstatus(state.grid, state, state.xcneighbors[i], _params);
                        if( st==nodebad || st==nodeunexplored )
                        {
                            continue;
                        }
                        alglib.ap.assert(st==nodesolved || st==nodeinprogress, "MIRBFVNS: 964543");
                        if( unmarked<0 || gridisbetter(state.grid, state, unmarked, state.xcneighbors[i], _params) )
                        {
                            unmarked = state.xcneighbors[i];
                            unmarkedi = i;
                        }
                    }
                }
                if( unmarked<0 )
                {
                    break;
                }
                gridoffloadbestpoint(state.grid, state, state.nodec, ref xc, ref idummy, ref fc, ref hc, ref mxc, _params);
                ablasf.rcopyrv(n, state.xcreachedbycut, unmarkedi, unmarkedcut, _params);
                gridexpandcutgenerateneighbors(state.grid, state, xc, unmarkedcut, state.xcneighbors, state.xcneighborscnt, ref state.xuneighbors, ref state.xucuts, ref state.xupoints, ref nncnt, ref state.xuflags, _params);
                if( nncnt==0 )
                {
                    state.xcqueryflags[unmarkedi] = true;
                    continue;
                }
                newunsolvedcnt = 0;
                for(i=0; i<=nncnt-1; i++)
                {
                    if( gridneedsevals(state.grid, state, state.xuneighbors[i], _params) )
                    {
                        newunsolvedcnt = newunsolvedcnt+1;
                    }
                }
                if( result!=0 )
                {
                    if( state.maxneighborhood>0 && state.xcneighborscnt-1+nncnt>=state.maxneighborhood )
                    {
                        break;
                    }
                }
                state.xcqueryflags[unmarkedi] = true;
                for(i=0; i<=nncnt-1; i++)
                {
                    ablasf.igrowappendv(state.xcneighborscnt+1, ref state.xcneighbors, state.xuneighbors[i], _params);
                    ablasf.igrowappendv(state.xcneighborscnt+1, ref state.xcreachedfrom, unmarked, _params);
                    ablasf.bgrowappendv(state.xcneighborscnt+1, ref state.xcqueryflags, false, _params);
                    ablasf.rgrowrowsfixedcolsm(state.xcneighborscnt+1, n, ref state.xcreachedbycut, _params);
                    ablasf.rcopyrr(n, state.xucuts, i, state.xcreachedbycut, state.xcneighborscnt, _params);
                    state.xcneighborscnt = state.xcneighborscnt+1;
                }
                result = result+nncnt;
                unsolvedcnt = unsolvedcnt+newunsolvedcnt;
            }
            state.rpool.recycle(ref xc);
            state.rpool.recycle(ref unmarkedcut);
            return result;
        }


        /*************************************************************************
        Sum of violations of linear and nonlinear constraints. Linear ones are
        scaled
        *************************************************************************/
        private static void computeviolation2(mirbfvnsstate state,
            double[] x,
            double[] fi,
            ref double h,
            ref double mx,
            alglib.xparams _params)
        {
            int i = 0;
            double v = 0;
            double vmx = 0;
            double vs = 0;

            h = 0;
            mx = 0;

            optserv.unscaleandchecklc2violation(state.s, state.rawa, state.rawal, state.rawau, state.lcsrcidx, state.lccnt, x, ref vs, ref vmx, ref i, _params);
            h = vs;
            mx = vmx;
            for(i=0; i<=state.nnlc-1; i++)
            {
                if( math.isfinite(state.nl[i]) )
                {
                    v = Math.Max(state.nl[i]-fi[1+i], 0.0);
                    h = h+v;
                    mx = Math.Max(mx, v);
                }
                if( math.isfinite(state.nu[i]) )
                {
                    v = Math.Max(fi[1+i]-state.nu[i], 0.0);
                    h = h+v;
                    mx = Math.Max(mx, v);
                }
            }
        }


        /*************************************************************************
        Finds integer feasible point satisfying linear constraints that is nearest
        to X0, subject to integral cuts given by CutsTable[RowIdx,*]:
        * CutsTable[RowIdx,i]>0 means that the lower bound on variable i is set to X0[i]+CutsTable[RowIdx,i]
        * CutsTable[RowIdx,i]<0 means that the upper bound on variable i is set to X0[i]+CutsTable[RowIdx,i]
        * CutsTable[RowIdx,i]=0 means that bounds on variable i are unchanged.
        * CutsTable[RowIdx,i] must be integer and must be zero for fractional variables
        X0 is assumed to be feasible with respect to at least box constraints.

        This function does not modify the grid, merely solves MILP subproblem and
        returns its solution. If no feasible point satisfying cut can be found,
        False is returned and XN is left in an undefined state.

        XN must be preallocated array long enough to store the result
        *************************************************************************/
        private static void findnearestintegralsubjecttocut(mirbfvnsstate state,
            double[] x0,
            double[,] cutstable,
            double[,] resultstable,
            bool[] successflags,
            int rowidx,
            bool usesafetybox,
            alglib.xparams _params)
        {
            int n = 0;
            int vidx = 0;
            mirbfvnstemporaries buf = null;
            bool updatestats = new bool();

            n = state.n;
            updatestats = state.doanytrace || state.adaptiveinternalparallelism>=0;
            for(vidx=0; vidx<=n-1; vidx++)
            {
                alglib.ap.assert((cutstable[rowidx,vidx]==(int)Math.Round(cutstable[rowidx,vidx]) && (state.isintegral[vidx] || cutstable[rowidx,vidx]==0)) && (!state.isintegral[vidx] || x0[vidx]==(int)Math.Round(x0[vidx])), "MIRBFVNS: 075558");
            }
            successflags[rowidx] = true;
            for(vidx=0; vidx<=n-1; vidx++)
            {
                resultstable[rowidx,vidx] = x0[vidx]+cutstable[rowidx,vidx];
                if( (state.hasbndl[vidx] && cutstable[rowidx,vidx]<0) && resultstable[rowidx,vidx]<state.bndl[vidx] )
                {
                    successflags[rowidx] = false;
                }
                if( (state.hasbndu[vidx] && cutstable[rowidx,vidx]>0) && resultstable[rowidx,vidx]>state.bndu[vidx] )
                {
                    successflags[rowidx] = false;
                }
            }
            if( successflags[rowidx] && state.haslinearlyconstrainedints )
            {
                alglib.smp.ae_shared_pool_retrieve(state.tmppool, ref buf);
                apserv.stimerinit(buf.localtimer, _params);
                apserv.stimerstartcond(buf.localtimer, updatestats, _params);
                findnearestintegralsubjecttocutx(state, x0, cutstable, resultstable, successflags, rowidx, usesafetybox, buf, _params);
                apserv.stimerstopcond(buf.localtimer, updatestats, _params);
                if( updatestats )
                {
                    apserv.weakatomicfetchadd(ref state.cutcnt, 1, _params);
                    apserv.weakatomicfetchadd(ref state.cuttimems, apserv.stimergetmsint(buf.localtimer, _params), _params);
                }
                alglib.smp.ae_shared_pool_recycle(state.tmppool, ref buf);
            }
            else
            {
                if( updatestats )
                {
                    apserv.weakatomicfetchadd(ref state.cutcnt, 1, _params);
                }
            }
        }


        /*************************************************************************
        A version of FindNearestIntegralSubjectToCut() internally called by the
        function when we have a linearly constrained problem. A workhorse for
        diffucult problems.
        *************************************************************************/
        private static void findnearestintegralsubjecttocutx(mirbfvnsstate state,
            double[] x0,
            double[,] cutstable,
            double[,] resultstable,
            bool[] successflags,
            int rowidx,
            bool usesafetybox,
            mirbfvnstemporaries buf,
            alglib.xparams _params)
        {
            int n = 0;
            int i = 0;
            double v = 0;
            int bbgdgroupsize = 0;
            int nmultistarts = 0;
            int timeout = 0;
            double smallcoeff = 0;

            n = state.n;
            ablasf.rcopyallocv(n, state.bndl, ref buf.wrkbndl, _params);
            ablasf.rcopyallocv(n, state.bndu, ref buf.wrkbndu, _params);
            for(i=0; i<=n-1; i++)
            {
                if( cutstable[rowidx,i]>0 )
                {
                    buf.wrkbndl[i] = x0[i]+cutstable[rowidx,i];
                }
                if( cutstable[rowidx,i]<0 )
                {
                    buf.wrkbndu[i] = x0[i]+cutstable[rowidx,i];
                }
                if( state.isintegral[i] && usesafetybox )
                {
                    v = x0[i]-safetyboxforbbgd;
                    if( !math.isfinite(buf.wrkbndl[i]) || (double)(buf.wrkbndl[i])<(double)(v) )
                    {
                        buf.wrkbndl[i] = v;
                    }
                    v = x0[i]+safetyboxforbbgd;
                    if( !math.isfinite(buf.wrkbndu[i]) || (double)(buf.wrkbndu[i])>(double)(v) )
                    {
                        buf.wrkbndu[i] = v;
                    }
                }
            }
            buf.diaga.n = n;
            buf.diaga.m = n;
            ablasf.iallocv(n+1, ref buf.diaga.ridx, _params);
            ablasf.iallocv(n, ref buf.diaga.idx, _params);
            ablasf.rallocv(n, ref buf.diaga.vals, _params);
            ablasf.rallocv(n, ref buf.linb, _params);
            for(i=0; i<=n-1; i++)
            {
                buf.diaga.ridx[i] = i;
                buf.diaga.idx[i] = i;
                if( state.isintegral[i] )
                {
                    buf.diaga.vals[i] = 1.0;
                    buf.linb[i] = -(x0[i]+cutstable[rowidx,i]);
                }
                else
                {
                    smallcoeff = 1.0E-5/Math.Max(state.s[i]*state.s[i], 1);
                    buf.diaga.vals[i] = smallcoeff;
                    buf.linb[i] = -(x0[i]*smallcoeff);
                }
            }
            buf.diaga.ridx[n] = n;
            sparse.sparsecreatecrsinplace(buf.diaga, _params);
            bbgdgroupsize = 1;
            nmultistarts = 1;
            timeout = 0;
            bbgd.bbgdcreatebuf(n, buf.wrkbndl, buf.wrkbndu, state.s, x0, state.isintegral, state.isbinary, state.rawa, state.rawal, state.rawau, state.lcsrcidx, state.lccnt, state.nl, state.nu, 0, bbgdgroupsize, nmultistarts, timeout, 0, buf.bbgdsubsolver, _params);
            bbgd.bbgdsetctol(buf.bbgdsubsolver, state.ctol, _params);
            bbgd.bbgdsetquadraticobjective(buf.bbgdsubsolver, buf.diaga, false, buf.linb, 0.0, _params);
            bbgd.bbgdforceserial(buf.bbgdsubsolver, _params);
            bbgd.bbgdsetdiving(buf.bbgdsubsolver, 2, _params);
            bbgd.bbgdsetmaxprimalcandidates(buf.bbgdsubsolver, maxprimalcandforcut, _params);
            bbgd.bbgdsetsoftmaxnodes(buf.bbgdsubsolver, softmaxnodescoeff*n, _params);
            while( bbgd.bbgditeration(buf.bbgdsubsolver, _params) )
            {
                alglib.ap.assert(false, "MIRBFVNS: unexpected V2 request by BBGD working in MIQP mode");
            }
            ablasf.rcopyvr(n, buf.bbgdsubsolver.xc, resultstable, rowidx, _params);
            successflags[rowidx] = buf.bbgdsubsolver.repterminationtype>0;
        }


        /*************************************************************************
        Parallel version of FindNearestIntegralSubjectToCut().

        The half-range [R0,R1) of cuts from CutsTable[] is processed.

        IsRoot must be true on initial call (recursive calls set it to False).
        TryParallelism controls whether parallel processing is used or not.
        *************************************************************************/
        private static void parallelfindnearestintegralsubjecttocut(mirbfvnsstate state,
            double[] x0,
            double[,] cutstable,
            double[,] resultstable,
            bool[] successflags,
            int r0,
            int r1,
            bool usesafetybox,
            bool isroot,
            bool tryparallelism,
            alglib.xparams _params)
        {
            int rmid = 0;

            if( r1<=r0 )
            {
                return;
            }
            if( (isroot && tryparallelism) && r1-r0>=2 )
            {
                if( _trypexec_parallelfindnearestintegralsubjecttocut(state,x0,cutstable,resultstable,successflags,r0,r1,usesafetybox,isroot,tryparallelism, _params) )
                {
                    return;
                }
            }
            if( r1==r0+1 )
            {
                findnearestintegralsubjecttocut(state, x0, cutstable, resultstable, successflags, r0, usesafetybox, _params);
                return;
            }
            alglib.ap.assert(r1>r0+1, "MIRBFVNS: 705014 failed");
            rmid = r0+(r1-r0)/2;
            parallelfindnearestintegralsubjecttocut(state, x0, cutstable, resultstable, successflags, r0, rmid, usesafetybox, false, tryparallelism, _params);
            parallelfindnearestintegralsubjecttocut(state, x0, cutstable, resultstable, successflags, rmid, r1, usesafetybox, false, tryparallelism, _params);
        }


        /*************************************************************************
        Serial stub for GPL edition.
        *************************************************************************/
        public static bool _trypexec_parallelfindnearestintegralsubjecttocut(mirbfvnsstate state,
            double[] x0,
            double[,] cutstable,
            double[,] resultstable,
            bool[] successflags,
            int r0,
            int r1,
            bool usesafetybox,
            bool isroot,
            bool tryparallelism, alglib.xparams _params)
        {
            return false;
        }


        /*************************************************************************
        Initializes dataset in an empty state
        *************************************************************************/
        private static void datasetinitempty(mirbfvnsdataset dataset,
            mirbfvnsstate state,
            alglib.xparams _params)
        {
            dataset.npoints = 0;
            dataset.nvars = state.n;
            dataset.nnlc = state.nnlc;
            ablasf.rgrowrowsfixedcolsm(1, dataset.nvars+1+dataset.nnlc+2, ref dataset.pointinfo, _params);
        }


        /*************************************************************************
        Appends point to the dataset and returns its index
        *************************************************************************/
        private static int datasetappendpoint(mirbfvnsdataset dataset,
            double[] x,
            double[] fi,
            double h,
            double mx,
            alglib.xparams _params)
        {
            int result = 0;
            int i = 0;
            int n = 0;
            int nnlc = 0;
            int rowidx = 0;

            ablasf.rgrowrowsfixedcolsm(dataset.npoints+1, dataset.nvars+1+dataset.nnlc+2, ref dataset.pointinfo, _params);
            rowidx = dataset.npoints;
            n = dataset.nvars;
            nnlc = dataset.nnlc;
            for(i=0; i<=n-1; i++)
            {
                dataset.pointinfo[rowidx,i] = x[i];
            }
            for(i=0; i<=nnlc; i++)
            {
                dataset.pointinfo[rowidx,n+i] = fi[i];
            }
            dataset.pointinfo[rowidx,n+1+nnlc+0] = h;
            dataset.pointinfo[rowidx,n+1+nnlc+1] = mx;
            dataset.npoints = dataset.npoints+1;
            result = rowidx;
            return result;
        }


        /*************************************************************************
        Initializes an integer grid using an initial point. Returns a node index,
        which is likely to be zero in all implementations.

        Params:
            X           point
            Fi  objective/constraints
            H   sum of constraint violations
            MX  maximum of constraint violations
        *************************************************************************/
        private static int gridcreate(mirbfvnsgrid grid,
            mirbfvnsstate state,
            double[] x,
            double[] fi,
            double h,
            double mx,
            alglib.xparams _params)
        {
            int result = 0;
            int n = 0;
            int i = 0;
            int pointidx = 0;

            n = state.n;
            for(i=0; i<=n-1; i++)
            {
                if( state.isintegral[i] && (int)Math.Round(x[i])!=x[i] )
                {
                    alglib.ap.assert(false, "MIRBFVNS: 886456 failed");
                }
            }
            pointidx = datasetappendpoint(state.dataset, x, fi, h, mx, _params);
            grid.nnodes = 1;
            grid.naddcols = 7;
            ablasf.rgrowrowsfixedcolsm(1, n+grid.naddcols, ref grid.nodesinfo, _params);
            ablasf.rcopyvr(n, x, grid.nodesinfo, 0, _params);
            ablasf.rmergemulvr(n, state.maskint, grid.nodesinfo, 0, _params);
            grid.nodesinfo[0,n+ncolstatus] = apserv.icase2(state.nfrac==0, nodesolved, nodeinprogress, _params);
            grid.nodesinfo[0,n+ncolneighborbegin] = -1;
            grid.nodesinfo[0,n+ncolneighborend] = -1;
            grid.nodesinfo[0,n+ncolfbest] = fi[0];
            grid.nodesinfo[0,n+ncolhbest] = h;
            grid.nodesinfo[0,n+ncolmxbest] = mx;
            grid.nodesinfo[0,n+ncollastaccepted] = pointidx;
            result = 0;
            grid.ptlistlength = 0;
            ablasf.iallocv(2, ref grid.ptlistheads, _params);
            grid.ptlistheads[0] = -1;
            grid.ptlistheads[1] = 0;
            gridappendpointtolist(grid, pointidx, 0, _params);
            grid.subsolvers.clear();
            if( state.nfrac>0 )
            {
                gridappendnilsubsolver(grid, _params);
                gridinitnilsubsolver(grid, state, 0, fi[0], h, mx, _params);
            }
            return result;
        }


        /*************************************************************************
        Appends point index to the per-node points list
        *************************************************************************/
        private static void gridappendpointtolist(mirbfvnsgrid grid,
            int pointidx,
            int nodeidx,
            alglib.xparams _params)
        {
            int nextentry = 0;
            int listsize = 0;

            nextentry = grid.ptlistheads[2*nodeidx+0];
            listsize = grid.ptlistheads[2*nodeidx+1];
            ablasf.igrowappendv(2*grid.ptlistlength+1, ref grid.ptlistdata, pointidx, _params);
            ablasf.igrowappendv(2*grid.ptlistlength+2, ref grid.ptlistdata, nextentry, _params);
            grid.ptlistheads[2*nodeidx+0] = grid.ptlistlength;
            grid.ptlistheads[2*nodeidx+1] = listsize+1;
            grid.ptlistlength = grid.ptlistlength+1;
        }


        /*************************************************************************
        Return node status by its index
        *************************************************************************/
        private static int gridgetstatus(mirbfvnsgrid grid,
            mirbfvnsstate state,
            int nodeidx,
            alglib.xparams _params)
        {
            int result = 0;

            alglib.ap.assert(nodeidx>=0 && nodeidx<grid.nnodes, "MIRBFVNS: 905114");
            result = (int)Math.Round(grid.nodesinfo[nodeidx,state.n+ncolstatus]);
            return result;
        }


        /*************************************************************************
        Return true of the node needs further evaluations (unexplored or in progress).
        False is returned when no evaluations are needed (solved or bad).
        *************************************************************************/
        private static bool gridneedsevals(mirbfvnsgrid grid,
            mirbfvnsstate state,
            int nodeidx,
            alglib.xparams _params)
        {
            bool result = new bool();
            double k = 0;

            alglib.ap.assert(nodeidx>=0 && nodeidx<grid.nnodes, "MIRBFVNS: 039445");
            k = grid.nodesinfo[nodeidx,state.n+ncolstatus];
            alglib.ap.assert((((double)(k)==(double)(nodeunexplored) || (double)(k)==(double)(nodeinprogress)) || (double)(k)==(double)(nodesolved)) || (double)(k)==(double)(nodebad), "MIRBFVNS: 935449");
            result = (double)(k)==(double)(nodeunexplored) || (double)(k)==(double)(nodeinprogress);
            return result;
        }


        /*************************************************************************
        Scans integer grid for a node corresponding to a point. If no node is found,
        creates a new one in a nodeUnexplored state.
        *************************************************************************/
        private static int gridfindorcreatenode(mirbfvnsgrid grid,
            mirbfvnsstate state,
            double[] x,
            alglib.xparams _params)
        {
            int result = 0;
            int n = 0;
            int i = 0;
            int j = 0;
            double[] xm = new double[0];
            bool isequal = new bool();

            n = state.n;
            for(i=0; i<=n-1; i++)
            {
                if( state.isintegral[i] && (int)Math.Round(x[i])!=x[i] )
                {
                    alglib.ap.assert(false, "MIRBFVNS: 932513 failed");
                }
            }
            state.rpool.retrieve(ref xm);
            alglib.ap.assert(alglib.ap.len(xm)>=n, "MIRBFVNS: 932514");
            ablasf.rcopyv(n, x, xm, _params);
            ablasf.rmergemulv(n, state.maskint, xm, _params);
            for(i=0; i<=grid.nnodes-1; i++)
            {
                isequal = true;
                for(j=0; j<=n-1; j++)
                {
                    if( !(xm[j]==grid.nodesinfo[i,j]) )
                    {
                        isequal = false;
                        break;
                    }
                }
                if( isequal )
                {
                    result = i;
                    return result;
                }
            }
            ablasf.rgrowrowsfixedcolsm(grid.nnodes+1, n+grid.naddcols, ref grid.nodesinfo, _params);
            ablasf.rcopyvr(n, xm, grid.nodesinfo, grid.nnodes, _params);
            grid.nodesinfo[grid.nnodes,n+ncolstatus] = nodeunexplored;
            grid.nodesinfo[grid.nnodes,n+ncolneighborbegin] = -1;
            grid.nodesinfo[grid.nnodes,n+ncolneighborend] = -1;
            grid.nodesinfo[grid.nnodes,n+ncolfbest] = math.maxrealnumber;
            grid.nodesinfo[grid.nnodes,n+ncolhbest] = math.maxrealnumber;
            grid.nodesinfo[grid.nnodes,n+ncolmxbest] = math.maxrealnumber;
            grid.nodesinfo[grid.nnodes,n+ncollastaccepted] = -1;
            ablasf.igrowappendv(2*grid.nnodes+1, ref grid.ptlistheads, -1, _params);
            ablasf.igrowappendv(2*grid.nnodes+2, ref grid.ptlistheads, 0, _params);
            grid.nnodes = grid.nnodes+1;
            result = grid.nnodes-1;
            if( state.nfrac>0 )
            {
                gridappendnilsubsolver(grid, _params);
            }
            if( state.doextratrace )
            {
                alglib.ap.trace(System.String.Format("[{0,6:d}] >>> CREATING NODE: variables mask is [", grid.nnodes-1));
                apserv.tracerowautoprec(grid.nodesinfo, grid.nnodes-1, 0, n, _params);
                alglib.ap.trace("]\n");
            }
            state.rpool.recycle(ref xm);
            return result;
        }


        /*************************************************************************
        Scans integer grid for nodes similar to #NodeIdx.

        Here 'similar' means that:
        * there is a mask of relevant variables given by VarMask[], where True
          means that the variable is marked as a relevant
        * a node has all relevant integer variables equal to that of #NodeIdx
          (fractional ones and irrelevant integer ones are ignored)
          
        If PutFirst is True, then #NodeIdx is output first in the list.

        Node indexes are stored to nodeList[] array that is resized as needed,
        results count is stores to nodesCnt
        *************************************************************************/
        private static void gridfindnodeslike(mirbfvnsgrid grid,
            mirbfvnsstate state,
            int nodeidx,
            bool putfirst,
            bool[] varmask,
            ref int[] nodeslist,
            ref int nodescnt,
            alglib.xparams _params)
        {
            int n = 0;
            int i = 0;
            int j = 0;
            bool isequal = new bool();

            nodescnt = 0;

            n = state.n;
            nodescnt = 0;
            if( putfirst )
            {
                ablasf.igrowappendv(nodescnt+1, ref nodeslist, nodeidx, _params);
                nodescnt = nodescnt+1;
            }
            for(i=0; i<=grid.nnodes-1; i++)
            {
                if( putfirst && i==nodeidx )
                {
                    continue;
                }
                isequal = true;
                for(j=0; j<=n-1; j++)
                {
                    if( (state.isintegral[j] && varmask[j]) && !(grid.nodesinfo[nodeidx,j]==grid.nodesinfo[i,j]) )
                    {
                        isequal = false;
                        break;
                    }
                }
                if( isequal )
                {
                    ablasf.igrowappendv(nodescnt+1, ref nodeslist, i, _params);
                    nodescnt = nodescnt+1;
                }
            }
        }


        /*************************************************************************
        Appends nil subsolver to the end of the subsolver list; no integrity checks,
        internal function used by grid.
        *************************************************************************/
        private static void gridappendnilsubsolver(mirbfvnsgrid grid,
            alglib.xparams _params)
        {
            mirbfvnsnodesubsolver dummy = null;

            grid.subsolvers.append(dummy);
        }


        /*************************************************************************
        Replaces nil subsolver with the newly initialized one. Assumes NFrac>0.
        No integrity checks, internal function used by grid.
        *************************************************************************/
        private static void gridinitnilsubsolver(mirbfvnsgrid grid,
            mirbfvnsstate state,
            int nodeidx,
            double f,
            double h,
            double mx,
            alglib.xparams _params)
        {
            mirbfvnsnodesubsolver subsolver = null;

            grid.subsolvers.get(nodeidx, ref subsolver);
            alglib.ap.assert(!(subsolver!=null), "MIRBFVNS: 231513");
            subsolver = new mirbfvnsnodesubsolver();
            subsolver.trustrad = 1.0;
            subsolver.sufficientcloudsize = false;
            subsolver.maxh = 10*Math.Max(10, h);
            subsolver.historymax = apserv.iboundval(state.nfrac+1, 10, 10, _params);
            ablasf.rsetallocv(subsolver.historymax, 1.0E20, ref subsolver.successfhistory, _params);
            ablasf.rsetallocv(subsolver.historymax, 1.0E20, ref subsolver.successhhistory, _params);
            grid.subsolvers.rewrite(nodeidx, subsolver);
        }


        /*************************************************************************
        Returns information about best point in a neighborhood. Mostly used for
        debug purposes. If no neighbors are present, returns False and dummy values
        (MaxRealNumber).

        Returns objective, sum(violation) and max(violation) at the best point.
        *************************************************************************/
        private static bool gridgetbestinneighborhood(mirbfvnsgrid grid,
            mirbfvnsstate state,
            int[] neighbors,
            int neighborscnt,
            ref double fbest,
            ref double hbest,
            ref double mxbest,
            alglib.xparams _params)
        {
            bool result = new bool();
            int n = 0;
            int i = 0;
            int idummy = 0;
            double[] x0 = new double[0];
            double f = 0;
            double h = 0;
            double mx = 0;

            fbest = 0;
            hbest = 0;
            mxbest = 0;

            n = state.n;
            state.rpool.retrieve(ref x0);
            alglib.ap.assert(alglib.ap.len(x0)>=n, "MIRBFVNS: 212354");
            fbest = math.maxrealnumber;
            hbest = math.maxrealnumber;
            mxbest = math.maxrealnumber;
            result = false;
            for(i=0; i<=neighborscnt-1; i++)
            {
                if( gridgetstatus(grid, state, neighbors[i], _params)!=nodeunexplored )
                {
                    gridoffloadbestpoint(grid, state, neighbors[i], ref x0, ref idummy, ref f, ref h, ref mx, _params);
                    if( !result || isbetterpoint(fbest, hbest, mxbest, f, h, mx, state.ctol, _params) )
                    {
                        fbest = f;
                        hbest = h;
                        mxbest = mx;
                        result = true;
                    }
                }
            }
            state.rpool.recycle(ref x0);
            return result;
        }


        /*************************************************************************
        Returns information about best last accepted point in unsolved nodes in a
        neighborhood.

        Mostly used for debug purposes. If no neighbors are present, returns False
        and dummy values (MaxRealNumber, -1 for node index).

        Returns objective, sum(violation) and max(violation) at the best point as
        well as node index.
        *************************************************************************/
        private static bool gridgetbestlastacceptedinunsolvedneighborhood(mirbfvnsgrid grid,
            mirbfvnsstate state,
            int[] neighbors,
            int neighborscnt,
            ref int nodeidx,
            ref double fbest,
            ref double hbest,
            ref double mxbest,
            alglib.xparams _params)
        {
            bool result = new bool();
            int n = 0;
            int nnlc = 0;
            int i = 0;
            int st = 0;
            int pointidx = 0;
            double f = 0;
            double h = 0;
            double mx = 0;

            nodeidx = 0;
            fbest = 0;
            hbest = 0;
            mxbest = 0;

            n = state.n;
            nnlc = state.nnlc;
            nodeidx = -1;
            fbest = math.maxrealnumber;
            hbest = math.maxrealnumber;
            mxbest = math.maxrealnumber;
            result = false;
            for(i=0; i<=neighborscnt-1; i++)
            {
                st = (int)Math.Round(grid.nodesinfo[neighbors[i],state.n+ncolstatus]);
                alglib.ap.assert(((st==nodeunexplored || st==nodeinprogress) || st==nodesolved) || st==nodebad, "MIRBFVNS: 311248");
                if( st!=nodeinprogress )
                {
                    continue;
                }
                pointidx = (int)Math.Round(grid.nodesinfo[neighbors[i],n+ncollastaccepted]);
                f = state.dataset.pointinfo[pointidx,n];
                h = state.dataset.pointinfo[pointidx,n+1+nnlc+0];
                mx = state.dataset.pointinfo[pointidx,n+1+nnlc+1];
                if( !result || isbetterpoint(fbest, hbest, mxbest, f, h, mx, state.ctol, _params) )
                {
                    fbest = f;
                    hbest = h;
                    mxbest = mx;
                    nodeidx = neighbors[i];
                    result = true;
                }
            }
            return result;
        }


        /*************************************************************************
        Informally speaking, this function returns a list of node indexes that
        correspond to up to 2N neighbors of a given node, excluding nodes in a
        user-specified list.

        More precisely, the node is given by a central point of a neighborhood plus
        a cut that resulted in reaching a node. The actual node is a sum of XCentral
        and NodeCut.

        This function 'expands' the cut by generating many derivative cuts, querying
        for nearest neighbors corresponding to these cuts, and generating a list
        of both cuts and neighbors.

        This function creates new nodes with status=nodeUnexplored, if necessary
        *************************************************************************/
        private static void gridexpandcutgenerateneighbors(mirbfvnsgrid grid,
            mirbfvnsstate state,
            double[] xcentral,
            double[] nodecut,
            int[] excludelist,
            int excludecnt,
            ref int[] neighbornodes,
            ref double[,] cutsapplied,
            ref double[,] pointsfound,
            ref int nncnt,
            ref bool[] tmpsuccessflags,
            alglib.xparams _params)
        {
            int n = 0;
            int vidx = 0;
            int sidx = 0;
            int candcnt = 0;
            int candidx = 0;
            int newnodeidx = 0;
            double vshift = 0;
            double[] xm = new double[0];
            bool sufficienttime = new bool();
            bool sufficientcount = new bool();

            n = state.n;
            state.rpool.retrieve(ref xm);
            alglib.ap.assert(alglib.ap.len(xm)>=n, "MIRBFVNS: 937448");
            ablasf.rgrowrowsfixedcolsm(2*n, n, ref cutsapplied, _params);
            ablasf.rgrowrowsfixedcolsm(2*n, n, ref pointsfound, _params);
            ablasf.bsetallocv(2*n, false, ref tmpsuccessflags, _params);
            candcnt = 0;
            for(vidx=0; vidx<=n-1; vidx++)
            {
                if( !state.isintegral[vidx] )
                {
                    continue;
                }
                for(sidx=0; sidx<=1; sidx++)
                {
                    vshift = 1-2*sidx;
                    if( nodecut[vidx]>0 && vshift<0 )
                    {
                        continue;
                    }
                    if( nodecut[vidx]<0 && vshift>0 )
                    {
                        continue;
                    }
                    if( state.hasbndl[vidx] && xcentral[vidx]+nodecut[vidx]+vshift<state.bndl[vidx] )
                    {
                        continue;
                    }
                    if( state.hasbndu[vidx] && xcentral[vidx]+nodecut[vidx]+vshift>state.bndu[vidx] )
                    {
                        continue;
                    }
                    ablasf.rcopyvr(n, nodecut, cutsapplied, candcnt, _params);
                    cutsapplied[candcnt,vidx] = cutsapplied[candcnt,vidx]+vshift;
                    candcnt = candcnt+1;
                }
            }
            sufficienttime = (double)(state.cuttimems)>=(double)(apserv.adaptiveparallelismtimerequired(_params));
            sufficientcount = (double)(state.cutcnt)>=(double)(apserv.adaptiveparallelismcountrequired(_params));
            if( state.adaptiveinternalparallelism>=0 && (sufficienttime || sufficientcount) )
            {
                state.expandcutgenerateneighborsparallelism = (double)(state.cuttimems/apserv.coalesce(state.cutcnt, 1, _params)*candcnt)>=(double)(apserv.workerstartthresholdms(_params)) && candcnt>=2;
            }
            apserv.stimerstartcond(state.timerprepareneighbors, state.doanytrace, _params);
            parallelfindnearestintegralsubjecttocut(state, xcentral, cutsapplied, pointsfound, tmpsuccessflags, 0, candcnt, true, true, state.expandcutgenerateneighborsparallelism, _params);
            apserv.stimerstopcond(state.timerprepareneighbors, state.doanytrace, _params);
            if( state.expandcutgenerateneighborsparallelism )
            {
                state.dbgpotentiallyparallelcutrounds = state.dbgpotentiallyparallelcutrounds+1;
            }
            else
            {
                state.dbgsequentialcutrounds = state.dbgsequentialcutrounds+1;
            }
            nncnt = 0;
            for(candidx=0; candidx<=candcnt-1; candidx++)
            {
                if( !tmpsuccessflags[candidx] )
                {
                    continue;
                }
                ablasf.rcopyrv(n, pointsfound, candidx, xm, _params);
                newnodeidx = gridfindorcreatenode(grid, state, xm, _params);
                if( !apserv.ilinearsearchispresent(excludelist, 0, excludecnt, newnodeidx, _params) && !apserv.ilinearsearchispresent(neighbornodes, 0, nncnt, newnodeidx, _params) )
                {
                    ablasf.igrowappendv(nncnt+1, ref neighbornodes, newnodeidx, _params);
                    if( candidx>nncnt )
                    {
                        ablasf.rcopyrr(n, cutsapplied, candidx, cutsapplied, nncnt, _params);
                        ablasf.rcopyrr(n, pointsfound, candidx, pointsfound, nncnt, _params);
                    }
                    nncnt = nncnt+1;
                }
            }
            state.rpool.recycle(ref xm);
        }


        /*************************************************************************
        Parallel version of the gridProposeLocalTrialPoint().

        For each I in half-range [R0,R1) it loads:
        * node index from State.EvalBatchNodeIdx[I]
        * seeds from State.tmpEB0[2*I+0] and State.tmpEB0[2*I+1]
        and calls gridProposeLocalTrialPoint in parallel manner.

        IsRoot must be true on initial call (recursive calls set it to False).
        TryParallelism controls whether parallel processing is used or not.
        *************************************************************************/
        private static void gridparallelproposelocaltrialpoint(mirbfvnsgrid grid,
            mirbfvnsgrid sharedgrid,
            mirbfvnsstate state,
            mirbfvnsstate sharedstate,
            int r0,
            int r1,
            bool isroot,
            bool tryparallelism,
            alglib.xparams _params)
        {
            int rmid = 0;

            if( r1<=r0 )
            {
                return;
            }
            if( (isroot && tryparallelism) && r1-r0>=2 )
            {
                if( _trypexec_gridparallelproposelocaltrialpoint(grid,sharedgrid,state,sharedstate,r0,r1,isroot,tryparallelism, _params) )
                {
                    return;
                }
            }
            if( r1==r0+1 )
            {
                if( gridgetstatus(state.grid, state, state.evalbatchnodeidx[r0], _params)==nodeunexplored )
                {
                    gridproposetrialpointwhenexploringfrom(grid, sharedgrid, state, sharedstate, state.evalbatchnodeidx[r0], state.xcreachedfrom[state.evalbatchneighboridx[r0]], state.tmpeb0[2*r0+0], state.tmpeb0[2*r0+1], r0, _params);
                    return;
                }
                if( state.nomask )
                {
                    gridproposelocaltrialpointnomask(grid, sharedgrid, state, sharedstate, state.evalbatchnodeidx[r0], state.tmpeb0[2*r0+0], state.tmpeb0[2*r0+1], r0, _params);
                }
                else
                {
                    gridproposelocaltrialpointmasked(grid, sharedgrid, state, sharedstate, state.evalbatchnodeidx[r0], state.tmpeb0[2*r0+0], state.tmpeb0[2*r0+1], r0, _params);
                }
                return;
            }
            alglib.ap.assert(r1>r0+1, "MIRBFVNS: 190337 failed");
            rmid = r0+(r1-r0)/2;
            gridparallelproposelocaltrialpoint(grid, sharedgrid, state, sharedstate, r0, rmid, false, tryparallelism, _params);
            gridparallelproposelocaltrialpoint(grid, sharedgrid, state, sharedstate, rmid, r1, false, tryparallelism, _params);
        }


        /*************************************************************************
        Serial stub for GPL edition.
        *************************************************************************/
        public static bool _trypexec_gridparallelproposelocaltrialpoint(mirbfvnsgrid grid,
            mirbfvnsgrid sharedgrid,
            mirbfvnsstate state,
            mirbfvnsstate sharedstate,
            int r0,
            int r1,
            bool isroot,
            bool tryparallelism, alglib.xparams _params)
        {
            return false;
        }


        /*************************************************************************
        Having node with  status=nodeInProgress,  proposes  trial  point  for  the
        exploration. Raises an exception for nodes with statuses different from
        nodeInProgress, e.g. unexplored.

        Use gridProposeTrialPointWhenExploringFrom() to start exploration of an
        unexplored node.

        Grid and State are passed twice: first as a constant reference, second as
        a shared one. The idea is to separate potentially thread-unsafe accesses
        from read-only ones that are safe to do.

        The result is written into SharedState.EvalBatchPoints[], row EvalBatchIdx.
        *************************************************************************/
        private static void gridproposelocaltrialpointnomask(mirbfvnsgrid grid,
            mirbfvnsgrid sharedgrid,
            mirbfvnsstate state,
            mirbfvnsstate sharedstate,
            int nodeidx,
            int rngseedtouse0,
            int rngseedtouse1,
            int evalbatchidx,
            alglib.xparams _params)
        {
            mirbfvnsnodesubsolver subsolver = null;
            mirbfvnstemporaries buf = null;
            double f = 0;
            double h = 0;
            int nfrac = 0;
            int fulln = 0;
            int nnlc = 0;
            int k = 0;
            int i = 0;
            int j = 0;
            int jj = 0;
            int j0 = 0;
            int j1 = 0;
            int offs = 0;
            int ortbasissize = 0;
            int nextlistpos = 0;
            int npoints = 0;
            int candidx = 0;
            int lastacceptedidx = 0;
            int subsolverits = 0;
            double v = 0;
            double v0 = 0;
            double v1 = 0;
            double mindistinf = 0;
            double vmax = 0;
            bool updatestats = new bool();

            fulln = state.n;
            nfrac = state.nfrac;
            nnlc = state.nnlc;
            updatestats = state.doanytrace || state.adaptiveinternalparallelism>=0;
            alglib.ap.assert(state.nomask, "MIRBFVNS: 331952 failed");
            alglib.ap.assert((double)(rbfpointtooclose)<=(double)(rbfsktooshort) && (double)(rbfsktooshort)>(double)(0), "MIRBFVNS: integrity check 498747 for control parameters failed");
            alglib.ap.assert(nodeidx>=0 && nodeidx<grid.nnodes, "MIRBFVNS: 075437");
            alglib.ap.assert(gridgetstatus(grid, state, nodeidx, _params)==nodeinprogress, "MIRBFVNS: 076438");
            alglib.ap.assert(state.nfrac>0, "MIRBFVNS: 086602");
            sharedgrid.subsolvers.get(nodeidx, ref subsolver);
            alglib.ap.assert(subsolver!=null, "MIRBFVNS: 266533");
            alglib.smp.ae_shared_pool_retrieve(sharedstate.tmppool, ref buf);
            apserv.stimerinit(buf.localtimer, _params);
            apserv.stimerstartcond(buf.localtimer, updatestats, _params);
            hqrnd.hqrndseed(rngseedtouse0, rngseedtouse1, buf.localrng, _params);
            ablasf.isetallocv(fulln, -1, ref buf.mapfull2compact, _params);
            ablasf.rallocv(nfrac, ref buf.glbbndl, _params);
            ablasf.rallocv(nfrac, ref buf.glbbndu, _params);
            ablasf.rallocv(nfrac, ref buf.glbs, _params);
            ablasf.rsetallocv(nfrac, subsolver.trustrad, ref buf.glbvtrustregion, _params);
            for(i=0; i<=nfrac-1; i++)
            {
                j = state.idxfrac[i];
                buf.glbs[i] = state.s[j];
                buf.glbbndl[i] = state.bndl[j];
                buf.glbbndu[i] = state.bndu[j];
                buf.glbvtrustregion[i] = buf.glbvtrustregion[i]*buf.glbs[i];
                buf.mapfull2compact[j] = i;
            }
            npoints = 1;
            ablasf.rgrowrowsfixedcolsm(npoints, nfrac+1+nnlc, ref buf.glbxf, _params);
            ablasf.rgrowrowsfixedcolsm(npoints, nfrac, ref buf.glbsx, _params);
            lastacceptedidx = (int)Math.Round(grid.nodesinfo[nodeidx,fulln+ncollastaccepted]);
            ablasf.rallocv(nfrac, ref buf.glbx0, _params);
            for(j=0; j<=nfrac-1; j++)
            {
                buf.glbx0[j] = state.dataset.pointinfo[lastacceptedidx,state.idxfrac[j]];
                buf.glbxf[0,j] = buf.glbx0[j];
                buf.glbsx[0,j] = buf.glbx0[j]/buf.glbs[j];
            }
            for(j=0; j<=nnlc; j++)
            {
                buf.glbxf[0,nfrac+j] = state.dataset.pointinfo[lastacceptedidx,fulln+j];
            }
            ablasf.rallocv(fulln, ref buf.fullx0, _params);
            ablasf.rcopyrv(fulln, state.dataset.pointinfo, lastacceptedidx, buf.fullx0, _params);
            f = state.dataset.pointinfo[lastacceptedidx,fulln];
            h = state.dataset.pointinfo[lastacceptedidx,fulln+1+nnlc+0];
            subsolver.basef = f;
            subsolver.baseh = h;
            nextlistpos = grid.ptlistheads[2*nodeidx+0];
            alglib.ap.assert(nextlistpos>=0 && grid.ptlistheads[2*nodeidx+1]>0, "MIRBFVNS: 352426");
            while( nextlistpos>=0 && npoints<rbfcloudsizemultiplier*nfrac+1 )
            {
                candidx = grid.ptlistdata[2*nextlistpos+0];
                nextlistpos = grid.ptlistdata[2*nextlistpos+1];
                if( candidx==lastacceptedidx )
                {
                    continue;
                }
                ablasf.rgrowrowsfixedcolsm(npoints+1, nfrac+1+nnlc, ref buf.glbxf, _params);
                ablasf.rgrowrowsfixedcolsm(npoints+1, nfrac, ref buf.glbsx, _params);
                for(j=0; j<=nfrac-1; j++)
                {
                    buf.glbxf[npoints,j] = state.dataset.pointinfo[candidx,state.idxfrac[j]];
                    buf.glbsx[npoints,j] = buf.glbxf[npoints,j]/buf.glbs[j];
                }
                for(j=0; j<=nnlc; j++)
                {
                    buf.glbxf[npoints,nfrac+j] = state.dataset.pointinfo[candidx,fulln+j];
                }
                if( (double)(rdistinfrr(nfrac, buf.glbsx, npoints, buf.glbsx, 0, _params))>(double)(rbfpointunacceptablyfar*subsolver.trustrad) )
                {
                    continue;
                }
                mindistinf = math.maxrealnumber;
                for(i=0; i<=npoints-1; i++)
                {
                    mindistinf = Math.Min(mindistinf, rdistinfrr(nfrac, buf.glbsx, npoints, buf.glbsx, i, _params));
                }
                if( (double)(mindistinf)<(double)(rbfpointtooclose*subsolver.trustrad) )
                {
                    continue;
                }
                npoints = npoints+1;
            }
            subsolver.sufficientcloudsize = npoints>=nfrac+1;
            if( !subsolver.sufficientcloudsize )
            {
                ablasf.rallocv(nfrac, ref buf.glbtmp0, _params);
                ablasf.rallocv(nfrac, ref buf.glbtmp1, _params);
                ablasf.rallocv(nfrac, ref buf.glbtmp2, _params);
                ablasf.rallocm(npoints-1, nfrac, ref buf.ortdeltas, _params);
                ortbasissize = 0;
                for(i=0; i<=npoints-2; i++)
                {
                    ablasf.rcopyrv(nfrac, buf.glbxf, i, buf.glbtmp0, _params);
                    ablasf.raddrv(nfrac, -1, buf.glbxf, npoints-1, buf.glbtmp0, _params);
                    v0 = Math.Sqrt(ablasf.rdotv2(nfrac, buf.glbtmp0, _params));
                    if( (double)(v0)==(double)(0) )
                    {
                        continue;
                    }
                    ablas.rowwisegramschmidt(buf.ortdeltas, ortbasissize, nfrac, buf.glbtmp0, ref buf.glbtmp0, false, _params);
                    v1 = Math.Sqrt(ablasf.rdotv2(nfrac, buf.glbtmp0, _params));
                    if( (double)(v1)==(double)(0) )
                    {
                        continue;
                    }
                    ablasf.rmulv(nfrac, 1/v1, buf.glbtmp0, _params);
                    ablasf.rcopyvr(nfrac, buf.glbtmp0, buf.ortdeltas, ortbasissize, _params);
                    ortbasissize = ortbasissize+1;
                }
                ablasf.rsetv(nfrac, 0.0, buf.glbtmp1, _params);
                vmax = -1;
                for(k=0; k<=4; k++)
                {
                    for(i=0; i<=nfrac-1; i++)
                    {
                        v0 = buf.glbx0[i]-buf.glbvtrustregion[i];
                        v1 = buf.glbx0[i]+buf.glbvtrustregion[i];
                        if( math.isfinite(buf.glbbndl[i]) )
                        {
                            v0 = Math.Max(v0, buf.glbbndl[i]);
                        }
                        if( math.isfinite(buf.glbbndu[i]) )
                        {
                            v1 = Math.Min(v1, buf.glbbndu[i]);
                        }
                        buf.glbtmp0[i] = apserv.boundval(v0+(v1-v0)*hqrnd.hqrnduniformr(buf.localrng, _params), v0, v1, _params);
                    }
                    ablasf.rcopyv(nfrac, buf.glbtmp0, buf.glbtmp2, _params);
                    ablas.rowwisegramschmidt(buf.ortdeltas, ortbasissize, nfrac, buf.glbtmp2, ref buf.glbtmp2, false, _params);
                    v = ablasf.rdotv2(nfrac, buf.glbtmp2, _params);
                    if( (double)(v)>=(double)(vmax) )
                    {
                        ablasf.rcopyv(nfrac, buf.glbtmp0, buf.glbtmp1, _params);
                        vmax = v;
                    }
                }
                subsolver.skrellen = 0;
                for(i=0; i<=nfrac-1; i++)
                {
                    v = buf.glbtmp1[i];
                    sharedstate.evalbatchpoints[evalbatchidx,state.idxfrac[i]] = v;
                    subsolver.skrellen = Math.Max(subsolver.skrellen, Math.Abs(v-buf.glbx0[i])/buf.glbvtrustregion[i]);
                }
                for(i=0; i<=state.nint-1; i++)
                {
                    sharedstate.evalbatchpoints[evalbatchidx,state.idxint[i]] = grid.nodesinfo[nodeidx,state.idxint[i]];
                }
                apserv.stimerstopcond(buf.localtimer, updatestats, _params);
                if( updatestats )
                {
                    apserv.weakatomicfetchadd(ref sharedstate.localtrialsamplingcnt, 1, _params);
                    apserv.weakatomicfetchadd(ref sharedstate.localtrialsamplingtimems, apserv.stimergetmsint(buf.localtimer, _params), _params);
                }
            }
            else
            {
                hqrnd.hqrndnormalm(buf.localrng, 1+nnlc, nfrac+1, ref buf.glbrandomprior, _params);
                for(i=0; i<=nnlc; i++)
                {
                    ablasf.rmergedivvr(nfrac, buf.glbs, buf.glbrandomprior, i, _params);
                }
                ablasf.rallocv(1+nnlc, ref buf.glbprioratx0, _params);
                ablasf.rgemv(1+nnlc, nfrac, 1.0, buf.glbrandomprior, 0, buf.glbx0, 0.0, buf.glbprioratx0, _params);
                ablasf.rsetc(1+nnlc, 0.0, buf.glbrandomprior, nfrac, _params);
                for(i=0; i<=npoints-1; i++)
                {
                    for(j=0; j<=nnlc; j++)
                    {
                        buf.glbxf[i,nfrac+j] = buf.glbxf[i,nfrac+j]-(ablasf.rdotrr(nfrac, buf.glbrandomprior, j, buf.glbxf, i, _params)-buf.glbprioratx0[j]);
                    }
                }
                if( state.lccnt>0 )
                {
                    alglib.ap.assert(sparse.sparseiscrs(state.rawa, _params), "MIRBFVNS: 095629 failed");
                    ablasf.rsetallocv(state.lccnt, Double.NegativeInfinity, ref buf.glbal, _params);
                    ablasf.rsetallocv(state.lccnt, Double.PositiveInfinity, ref buf.glbau, _params);
                    buf.glba.m = state.lccnt;
                    buf.glba.n = nfrac;
                    ablasf.iallocv(state.lccnt+1, ref buf.glba.ridx, _params);
                    buf.glba.ridx[0] = 0;
                    for(i=0; i<=state.lccnt-1; i++)
                    {
                        v = 0.0;
                        offs = buf.glba.ridx[i];
                        ablasf.igrowv(offs+nfrac, ref buf.glba.idx, _params);
                        ablasf.rgrowv(offs+nfrac, ref buf.glba.vals, _params);
                        j0 = state.rawa.ridx[i];
                        j1 = state.rawa.ridx[i+1]-1;
                        for(jj=j0; jj<=j1; jj++)
                        {
                            j = state.rawa.idx[jj];
                            if( !state.isintegral[j] )
                            {
                                buf.glba.idx[offs] = buf.mapfull2compact[j];
                                buf.glba.vals[offs] = state.rawa.vals[jj];
                                offs = offs+1;
                            }
                            else
                            {
                                v = v+buf.fullx0[j]*state.rawa.vals[jj];
                            }
                        }
                        buf.glba.ridx[i+1] = offs;
                        if( math.isfinite(state.rawal[i]) )
                        {
                            buf.glbal[i] = state.rawal[i]-v;
                        }
                        if( math.isfinite(state.rawau[i]) )
                        {
                            buf.glbau[i] = state.rawau[i]-v;
                        }
                    }
                    sparse.sparsecreatecrsinplace(buf.glba, _params);
                }
                ablasf.rsetallocv(nfrac, 1/subsolver.trustrad, ref buf.glbmultscale, _params);
                ablasf.rmergedivv(nfrac, buf.glbs, buf.glbmultscale, _params);
                rbfinitmodel(buf.glbxf, buf.glbmultscale, npoints, nfrac, 1+nnlc, buf.glbmodel, _params);
                rbfaddlinearterm(buf.glbmodel, buf.glbrandomprior, _params);
                ablasf.rallocv(nfrac, ref buf.glbxtrial, _params);
                rbfminimizemodel(buf.glbmodel, buf.glbx0, buf.glbbndl, buf.glbbndu, buf.glbvtrustregion, subsolver.trustrad, state.ctol, rbfminimizeitsperphase, false, buf.glba, buf.glbal, buf.glbau, state.lccnt, state.nl, state.nu, state.nnlc, nfrac, buf.mmbuf, ref buf.glbxtrial, ref buf.glbsk, ref subsolver.predf, ref subsolver.predh, ref subsolverits, _params);
                subsolver.skrellen = 0;
                for(i=0; i<=nfrac-1; i++)
                {
                    subsolver.skrellen = Math.Max(subsolver.skrellen, Math.Abs(buf.glbsk[i]/buf.glbvtrustregion[i]));
                    sharedstate.evalbatchpoints[evalbatchidx,state.idxfrac[i]] = buf.glbxtrial[i];
                }
                for(i=0; i<=state.nint-1; i++)
                {
                    sharedstate.evalbatchpoints[evalbatchidx,state.idxint[i]] = grid.nodesinfo[nodeidx,state.idxint[i]];
                }
                apserv.stimerstopcond(buf.localtimer, updatestats, _params);
                apserv.weakatomicfetchadd(ref sharedstate.repsubsolverits, subsolverits, _params);
                if( updatestats )
                {
                    apserv.weakatomicfetchadd(ref sharedstate.localtrialrbfcnt, 1, _params);
                    apserv.weakatomicfetchadd(ref sharedstate.localtrialrbftimems, apserv.stimergetmsint(buf.localtimer, _params), _params);
                }
            }
            alglib.smp.ae_shared_pool_recycle(sharedstate.tmppool, ref buf);
        }


        /*************************************************************************
        Having node with  status=nodeInProgress,  proposes  trial  point  for  the
        exploration. Raises an exception for nodes with statuses different from
        nodeInProgress, e.g. unexplored.

        Use gridProposeTrialPointWhenExploringFrom() to start exploration of an
        unexplored node.

        Grid and State are passed twice: first as a constant reference, second as
        a shared one. The idea is to separate potentially thread-unsafe accesses
        from read-only ones that are safe to do.

        The result is written into SharedState.EvalBatchPoints[], row EvalBatchIdx.
        *************************************************************************/
        private static void gridproposelocaltrialpointmasked(mirbfvnsgrid grid,
            mirbfvnsgrid sharedgrid,
            mirbfvnsstate state,
            mirbfvnsstate sharedstate,
            int nodeidx,
            int rngseedtouse0,
            int rngseedtouse1,
            int evalbatchidx,
            alglib.xparams _params)
        {
            mirbfvnsnodesubsolver subsolver = null;
            mirbfvnstemporaries buf = null;
            double f = 0;
            double h = 0;
            int fidx = 0;
            int curnfrac = 0;
            int fulln = 0;
            int nnlc = 0;
            int k = 0;
            int i = 0;
            int j = 0;
            int jj = 0;
            int j0 = 0;
            int j1 = 0;
            int offs = 0;
            int nodescnt = 0;
            int nextlistpos = 0;
            int npoints = 0;
            int candidx = 0;
            int lastacceptedidx = 0;
            int subsolverits = 0;
            double v = 0;
            double v0 = 0;
            double v1 = 0;
            double mindistinf = 0;
            double prioratx0 = 0;
            bool updatestats = new bool();

            fulln = state.n;
            nnlc = state.nnlc;
            updatestats = state.dotrace || state.adaptiveinternalparallelism>=0;
            alglib.ap.assert(!state.nomask, "MIRBFVNS: 676607 failed");
            alglib.ap.assert((double)(rbfpointtooclose)<=(double)(rbfsktooshort) && (double)(rbfsktooshort)>(double)(0), "MIRBFVNS: integrity check 498747 for control parameters failed");
            alglib.ap.assert(nodeidx>=0 && nodeidx<grid.nnodes, "MIRBFVNS: 075437");
            alglib.ap.assert(gridgetstatus(grid, state, nodeidx, _params)==nodeinprogress, "MIRBFVNS: 076438");
            alglib.ap.assert(state.nfrac>0, "MIRBFVNS: 086602");
            sharedgrid.subsolvers.get(nodeidx, ref subsolver);
            alglib.ap.assert(subsolver!=null, "MIRBFVNS: 266533");
            alglib.smp.ae_shared_pool_retrieve(sharedstate.tmppool, ref buf);
            apserv.stimerinit(buf.localtimer, _params);
            apserv.stimerstartcond(buf.localtimer, updatestats, _params);
            hqrnd.hqrndseed(rngseedtouse0, rngseedtouse1, buf.localrng, _params);
            lastacceptedidx = (int)Math.Round(grid.nodesinfo[nodeidx,fulln+ncollastaccepted]);
            ablasf.rallocv(fulln, ref buf.fullx0, _params);
            ablasf.rcopyrv(fulln, state.dataset.pointinfo, lastacceptedidx, buf.fullx0, _params);
            f = state.dataset.pointinfo[lastacceptedidx,fulln];
            h = state.dataset.pointinfo[lastacceptedidx,fulln+1+nnlc+0];
            subsolver.basef = f;
            subsolver.baseh = h;
            ablasf.isetallocv(fulln, -1, ref buf.mapfull2compact, _params);
            ablasf.rallocv(state.nfrac, ref buf.glbbndl, _params);
            ablasf.rallocv(state.nfrac, ref buf.glbbndu, _params);
            ablasf.rallocv(state.nfrac, ref buf.glbs, _params);
            ablasf.rallocv(state.nfrac, ref buf.glbx0, _params);
            ablasf.rsetallocv(state.nfrac, subsolver.trustrad, ref buf.glbvtrustregion, _params);
            for(i=0; i<=state.nfrac-1; i++)
            {
                j = state.idxfrac[i];
                buf.glbs[i] = state.s[j];
                buf.glbbndl[i] = state.bndl[j];
                buf.glbbndu[i] = state.bndu[j];
                buf.glbvtrustregion[i] = buf.glbvtrustregion[i]*buf.glbs[i];
                buf.glbx0[i] = state.dataset.pointinfo[lastacceptedidx,state.idxfrac[i]];
                buf.mapfull2compact[j] = i;
            }
            ablasf.rsetallocv(state.nfrac, 1/subsolver.trustrad, ref buf.glbmultscale, _params);
            ablasf.rmergedivv(state.nfrac, buf.glbs, buf.glbmultscale, _params);
            if( state.lccnt>0 )
            {
                alglib.ap.assert(sparse.sparseiscrs(state.rawa, _params), "MIRBFVNS: 095629 failed");
                ablasf.rsetallocv(state.lccnt, Double.NegativeInfinity, ref buf.glbal, _params);
                ablasf.rsetallocv(state.lccnt, Double.PositiveInfinity, ref buf.glbau, _params);
                buf.glba.m = state.lccnt;
                buf.glba.n = state.nfrac;
                ablasf.iallocv(state.lccnt+1, ref buf.glba.ridx, _params);
                buf.glba.ridx[0] = 0;
                for(i=0; i<=state.lccnt-1; i++)
                {
                    v = 0.0;
                    offs = buf.glba.ridx[i];
                    ablasf.igrowv(offs+state.nfrac, ref buf.glba.idx, _params);
                    ablasf.rgrowv(offs+state.nfrac, ref buf.glba.vals, _params);
                    j0 = state.rawa.ridx[i];
                    j1 = state.rawa.ridx[i+1]-1;
                    for(jj=j0; jj<=j1; jj++)
                    {
                        j = state.rawa.idx[jj];
                        if( !state.isintegral[j] )
                        {
                            buf.glba.idx[offs] = buf.mapfull2compact[j];
                            buf.glba.vals[offs] = state.rawa.vals[jj];
                            offs = offs+1;
                        }
                        else
                        {
                            v = v+buf.fullx0[j]*state.rawa.vals[jj];
                        }
                    }
                    buf.glba.ridx[i+1] = offs;
                    if( math.isfinite(state.rawal[i]) )
                    {
                        buf.glbal[i] = state.rawal[i]-v;
                    }
                    if( math.isfinite(state.rawau[i]) )
                    {
                        buf.glbau[i] = state.rawau[i]-v;
                    }
                }
                sparse.sparsecreatecrsinplace(buf.glba, _params);
            }
            rbfinitemptysparsemodel(buf.glbmultscale, state.nfrac, buf.glbmodel, _params);
            subsolver.sufficientcloudsize = true;
            for(fidx=0; fidx<=nnlc; fidx++)
            {
                if( state.hasmask[fidx] )
                {
                    ablasf.bsetallocv(fulln, false, ref buf.glbmask, _params);
                    j0 = state.varmask.ridx[fidx];
                    j1 = state.varmask.ridx[fidx+1]-1;
                    for(jj=j0; jj<=j1; jj++)
                    {
                        buf.glbmask[state.varmask.idx[jj]] = true;
                    }
                }
                else
                {
                    ablasf.bsetallocv(fulln, true, ref buf.glbmask, _params);
                }
                ablasf.iallocv(state.nfrac, ref buf.lclidxfrac, _params);
                ablasf.iallocv(state.nfrac, ref buf.lcl2glb, _params);
                ablasf.rallocv(state.nfrac, ref buf.lcls, _params);
                ablasf.rallocv(state.nfrac, ref buf.lclmultscale, _params);
                curnfrac = 0;
                for(i=0; i<=state.nfrac-1; i++)
                {
                    if( buf.glbmask[state.idxfrac[i]] )
                    {
                        buf.lclidxfrac[curnfrac] = state.idxfrac[i];
                        buf.lcl2glb[curnfrac] = i;
                        buf.lcls[curnfrac] = buf.glbs[i];
                        buf.lclmultscale[curnfrac] = buf.glbmultscale[i];
                        curnfrac = curnfrac+1;
                    }
                }
                if( curnfrac==0 )
                {
                    rbfappendconstantmodel(buf.glbmodel, state.dataset.pointinfo[lastacceptedidx,fulln+fidx], _params);
                    continue;
                }
                npoints = 1;
                ablasf.rgrowrowsfixedcolsm(npoints, curnfrac+1, ref buf.lclxf, _params);
                ablasf.rgrowrowsfixedcolsm(npoints, curnfrac, ref buf.lclsx, _params);
                for(j=0; j<=curnfrac-1; j++)
                {
                    buf.lclxf[0,j] = state.dataset.pointinfo[lastacceptedidx,buf.lclidxfrac[j]];
                    buf.lclsx[0,j] = buf.lclxf[0,j]/state.s[buf.lclidxfrac[j]];
                }
                buf.lclxf[0,curnfrac] = state.dataset.pointinfo[lastacceptedidx,fulln+fidx];
                gridfindnodeslike(grid, state, nodeidx, true, buf.glbmask, ref buf.nodeslist, ref nodescnt, _params);
                alglib.ap.assert(nodescnt>=1 && buf.nodeslist[0]==nodeidx, "MIRBFVNS: 773613 failed");
                for(k=0; k<=nodescnt-1; k++)
                {
                    if( grid.ptlistheads[2*buf.nodeslist[k]+1]==0 )
                    {
                        continue;
                    }
                    nextlistpos = grid.ptlistheads[2*buf.nodeslist[k]+0];
                    alglib.ap.assert(nextlistpos>=0 && grid.ptlistheads[2*buf.nodeslist[k]+1]>0, "MIRBFVNS: 904319");
                    while( nextlistpos>=0 && npoints<rbfcloudsizemultiplier*curnfrac+1 )
                    {
                        candidx = grid.ptlistdata[2*nextlistpos+0];
                        nextlistpos = grid.ptlistdata[2*nextlistpos+1];
                        ablasf.rgrowrowsfixedcolsm(npoints+1, curnfrac+1, ref buf.lclxf, _params);
                        ablasf.rgrowrowsfixedcolsm(npoints+1, curnfrac, ref buf.lclsx, _params);
                        for(j=0; j<=curnfrac-1; j++)
                        {
                            buf.lclxf[npoints,j] = state.dataset.pointinfo[candidx,buf.lclidxfrac[j]];
                            buf.lclsx[npoints,j] = buf.lclxf[npoints,j]/state.s[buf.lclidxfrac[j]];
                        }
                        buf.lclxf[npoints,curnfrac] = state.dataset.pointinfo[candidx,fulln+fidx];
                        if( (double)(rdistinfrr(curnfrac, buf.lclsx, npoints, buf.lclsx, 0, _params))>(double)(rbfpointunacceptablyfar*subsolver.trustrad) )
                        {
                            continue;
                        }
                        mindistinf = math.maxrealnumber;
                        for(i=0; i<=npoints-1; i++)
                        {
                            mindistinf = Math.Min(mindistinf, rdistinfrr(curnfrac, buf.lclsx, npoints, buf.lclsx, i, _params));
                        }
                        if( (double)(mindistinf)<(double)(rbfpointtooclose*subsolver.trustrad) )
                        {
                            continue;
                        }
                        npoints = npoints+1;
                    }
                }
                subsolver.sufficientcloudsize = subsolver.sufficientcloudsize && npoints>=curnfrac+1;
                hqrnd.hqrndnormalm(buf.localrng, 1, curnfrac+1, ref buf.lclrandomprior, _params);
                ablasf.rmergedivvr(curnfrac, buf.lcls, buf.lclrandomprior, 0, _params);
                prioratx0 = ablasf.rdotrr(curnfrac, buf.lclxf, 0, buf.lclrandomprior, 0, _params);
                buf.lclrandomprior[0,curnfrac] = 0.0;
                for(i=0; i<=npoints-1; i++)
                {
                    buf.lclxf[i,curnfrac] = buf.lclxf[i,curnfrac]-(ablasf.rdotrr(curnfrac, buf.lclrandomprior, 0, buf.lclxf, i, _params)-prioratx0);
                }
                rbfinitmodel(buf.lclxf, buf.lclmultscale, npoints, curnfrac, 1, buf.tmpmodel, _params);
                rbfaddlinearterm(buf.tmpmodel, buf.lclrandomprior, _params);
                rbfappendmodel(buf.glbmodel, buf.tmpmodel, buf.lcl2glb, _params);
            }
            if( !subsolver.sufficientcloudsize )
            {
                subsolver.skrellen = 0;
                for(i=0; i<=state.nfrac-1; i++)
                {
                    v0 = buf.glbx0[i]-buf.glbvtrustregion[i];
                    v1 = buf.glbx0[i]+buf.glbvtrustregion[i];
                    if( math.isfinite(buf.glbbndl[i]) )
                    {
                        v0 = Math.Max(v0, buf.glbbndl[i]);
                    }
                    if( math.isfinite(buf.glbbndu[i]) )
                    {
                        v1 = Math.Min(v1, buf.glbbndu[i]);
                    }
                    v = apserv.boundval(v0+(v1-v0)*hqrnd.hqrnduniformr(buf.localrng, _params), v0, v1, _params);
                    sharedstate.evalbatchpoints[evalbatchidx,state.idxfrac[i]] = v;
                    subsolver.skrellen = Math.Max(subsolver.skrellen, Math.Abs(v-buf.glbx0[i])/buf.glbvtrustregion[i]);
                }
                for(i=0; i<=state.nint-1; i++)
                {
                    sharedstate.evalbatchpoints[evalbatchidx,state.idxint[i]] = grid.nodesinfo[nodeidx,state.idxint[i]];
                }
                apserv.stimerstopcond(buf.localtimer, updatestats, _params);
                if( updatestats )
                {
                    apserv.weakatomicfetchadd(ref sharedstate.localtrialsamplingcnt, 1, _params);
                    apserv.weakatomicfetchadd(ref sharedstate.localtrialsamplingtimems, apserv.stimergetmsint(buf.localtimer, _params), _params);
                }
            }
            else
            {
                ablasf.rallocv(state.nfrac, ref buf.glbxtrial, _params);
                rbfminimizemodel(buf.glbmodel, buf.glbx0, buf.glbbndl, buf.glbbndu, buf.glbvtrustregion, subsolver.trustrad, state.ctol, rbfminimizeitsperphase, false, buf.glba, buf.glbal, buf.glbau, state.lccnt, state.nl, state.nu, state.nnlc, state.nfrac, buf.mmbuf, ref buf.glbxtrial, ref buf.glbsk, ref subsolver.predf, ref subsolver.predh, ref subsolverits, _params);
                subsolver.skrellen = 0;
                for(i=0; i<=state.nfrac-1; i++)
                {
                    subsolver.skrellen = Math.Max(subsolver.skrellen, Math.Abs(buf.glbsk[i]/buf.glbvtrustregion[i]));
                    sharedstate.evalbatchpoints[evalbatchidx,state.idxfrac[i]] = buf.glbxtrial[i];
                }
                for(i=0; i<=state.nint-1; i++)
                {
                    sharedstate.evalbatchpoints[evalbatchidx,state.idxint[i]] = grid.nodesinfo[nodeidx,state.idxint[i]];
                }
                apserv.stimerstopcond(buf.localtimer, updatestats, _params);
                apserv.weakatomicfetchadd(ref sharedstate.repsubsolverits, subsolverits, _params);
                if( updatestats )
                {
                    apserv.weakatomicfetchadd(ref sharedstate.localtrialrbfcnt, 1, _params);
                    apserv.weakatomicfetchadd(ref sharedstate.localtrialrbftimems, apserv.stimergetmsint(buf.localtimer, _params), _params);
                }
            }
            alglib.smp.ae_shared_pool_recycle(sharedstate.tmppool, ref buf);
        }


        /*************************************************************************
        Propose initial values of fractional variables that are used to start
        exploration of node #NewNodeIdx from #ExploreFromNode.

        The function loads XTrial with fixed and fractional variables. Values of
        fractional variables are derived from that at #ExploreFromNode.
        *************************************************************************/
        private static void gridproposetrialpointwhenexploringfrom(mirbfvnsgrid grid,
            mirbfvnsgrid sharedgrid,
            mirbfvnsstate state,
            mirbfvnsstate sharedstate,
            int newnodeidx,
            int explorefromnode,
            int rngseedtouse0,
            int rngseedtouse1,
            int evalbatchidx,
            alglib.xparams _params)
        {
            double f = 0;
            double h = 0;
            double mx = 0;
            int fulln = 0;
            int nfrac = 0;
            int i = 0;
            int j = 0;
            int jj = 0;
            int j0 = 0;
            int j1 = 0;
            int offs = 0;
            int bestidx = 0;
            double v = 0;
            mirbfvnstemporaries buf = null;
            bool updatestats = new bool();
            int terminationtype = 0;

            fulln = state.n;
            nfrac = state.nfrac;
            updatestats = state.doanytrace || state.adaptiveinternalparallelism>=0;
            alglib.ap.assert(newnodeidx>=0 && newnodeidx<grid.nnodes, "MIRBFVNS: 087602");
            alglib.ap.assert(explorefromnode>=0 && explorefromnode<grid.nnodes, "MIRBFVNS: 088602");
            alglib.smp.ae_shared_pool_retrieve(sharedstate.tmppool, ref buf);
            apserv.stimerinit(buf.localtimer, _params);
            apserv.stimerstartcond(buf.localtimer, updatestats, _params);
            hqrnd.hqrndseed(rngseedtouse0, rngseedtouse1, buf.localrng, _params);
            if( state.nfrac==0 )
            {
                ablasf.rcopyrr(fulln, grid.nodesinfo, newnodeidx, sharedstate.evalbatchpoints, evalbatchidx, _params);
                apserv.weakatomicfetchadd(ref sharedstate.explorativetrialcnt, 1, _params);
                alglib.smp.ae_shared_pool_recycle(sharedstate.tmppool, ref buf);
                return;
            }
            gridoffloadbestpoint(grid, state, explorefromnode, ref buf.fullx0, ref bestidx, ref f, ref h, ref mx, _params);
            for(j=0; j<=fulln-1; j++)
            {
                buf.fullx0[j] = state.maskfrac[j]*buf.fullx0[j]+state.maskint[j]*grid.nodesinfo[newnodeidx,j];
            }
            ablasf.rcopyvr(fulln, buf.fullx0, sharedstate.evalbatchpoints, evalbatchidx, _params);
            if( state.lccnt==0 )
            {
                alglib.smp.ae_shared_pool_recycle(sharedstate.tmppool, ref buf);
                return;
            }
            ablasf.isetallocv(fulln, -1, ref buf.mapfull2compact, _params);
            ablasf.rallocv(nfrac, ref buf.glbbndl, _params);
            ablasf.rallocv(nfrac, ref buf.glbbndu, _params);
            ablasf.rallocv(nfrac, ref buf.glbs, _params);
            ablasf.rallocv(nfrac, ref buf.glbx0, _params);
            for(i=0; i<=nfrac-1; i++)
            {
                j = state.idxfrac[i];
                buf.glbs[i] = state.s[j];
                buf.glbx0[i] = buf.fullx0[j];
                buf.glbbndl[i] = state.bndl[j];
                buf.glbbndu[i] = state.bndu[j];
                buf.mapfull2compact[j] = i;
            }
            buf.diaga.n = nfrac;
            buf.diaga.m = nfrac;
            ablasf.iallocv(nfrac+1, ref buf.diaga.ridx, _params);
            ablasf.iallocv(nfrac, ref buf.diaga.idx, _params);
            ablasf.rallocv(nfrac, ref buf.diaga.vals, _params);
            for(i=0; i<=nfrac-1; i++)
            {
                buf.diaga.ridx[i] = i;
                buf.diaga.idx[i] = i;
                buf.diaga.vals[i] = 1.0/(state.s[i]*state.s[i]);
            }
            buf.diaga.ridx[nfrac] = nfrac;
            sparse.sparsecreatecrsinplace(buf.diaga, _params);
            ablasf.rsetallocv(nfrac, 0.0, ref buf.linb, _params);
            alglib.ap.assert(sparse.sparseiscrs(state.rawa, _params), "MIRBFVNS: 095629 failed");
            ablasf.rsetallocv(state.lccnt, Double.NegativeInfinity, ref buf.glbal, _params);
            ablasf.rsetallocv(state.lccnt, Double.PositiveInfinity, ref buf.glbau, _params);
            buf.glba.m = state.lccnt;
            buf.glba.n = nfrac;
            ablasf.iallocv(state.lccnt+1, ref buf.glba.ridx, _params);
            buf.glba.ridx[0] = 0;
            for(i=0; i<=state.lccnt-1; i++)
            {
                v = 0.0;
                offs = buf.glba.ridx[i];
                ablasf.igrowv(offs+nfrac, ref buf.glba.idx, _params);
                ablasf.rgrowv(offs+nfrac, ref buf.glba.vals, _params);
                j0 = state.rawa.ridx[i];
                j1 = state.rawa.ridx[i+1]-1;
                for(jj=j0; jj<=j1; jj++)
                {
                    j = state.rawa.idx[jj];
                    if( !state.isintegral[j] )
                    {
                        buf.glba.idx[offs] = buf.mapfull2compact[j];
                        buf.glba.vals[offs] = state.rawa.vals[jj];
                        offs = offs+1;
                    }
                    else
                    {
                        v = v+buf.fullx0[j]*state.rawa.vals[jj];
                    }
                }
                buf.glba.ridx[i+1] = offs;
                if( math.isfinite(state.rawal[i]) )
                {
                    buf.glbal[i] = state.rawal[i]-v;
                }
                if( math.isfinite(state.rawau[i]) )
                {
                    buf.glbau[i] = state.rawau[i]-v;
                }
            }
            sparse.sparsecreatecrsinplace(buf.glba, _params);
            ipm2solver.ipm2init(buf.qpsubsolver, buf.glbs, buf.glbx0, nfrac, state.densedummy2, buf.diaga, 1, false, state.densedummy2, buf.glbtmp0, 0, buf.linb, 0.0, buf.glbbndl, buf.glbbndu, buf.glba, state.lccnt, state.densedummy2, 0, buf.glbal, buf.glbau, false, false, _params);
            ipm2solver.ipm2setcond(buf.qpsubsolver, state.epsx, state.epsx, state.epsx, _params);
            ipm2solver.ipm2setmaxits(buf.qpsubsolver, maxipmits, _params);
            ipm2solver.ipm2optimize(buf.qpsubsolver, true, ref buf.glbtmp0, ref buf.glbtmp1, ref buf.glbtmp2, ref terminationtype, _params);
            if( terminationtype>0 )
            {
                for(i=0; i<=nfrac-1; i++)
                {
                    buf.fullx0[state.idxfrac[i]] = buf.glbtmp0[i];
                }
            }
            ablasf.rcopyvr(fulln, buf.fullx0, sharedstate.evalbatchpoints, evalbatchidx, _params);
            apserv.stimerstopcond(buf.localtimer, updatestats, _params);
            if( updatestats )
            {
                apserv.weakatomicfetchadd(ref sharedstate.explorativetrialcnt, 1, _params);
                apserv.weakatomicfetchadd(ref sharedstate.explorativetrialtimems, apserv.stimergetmsint(buf.localtimer, _params), _params);
            }
            alglib.smp.ae_shared_pool_recycle(sharedstate.tmppool, ref buf);
        }


        /*************************************************************************
        Having trial point corresponding to node #NodeIdx (it is not checked that
        the point actually belongs to the node) and objective/constraint values
        at that point, send information about trial to the node.

        Processing may involve updating node status (from unexplored to explored or
        solved), but generally does not involve re-running internal subsolver
        *************************************************************************/
        private static void gridsendtrialpointto(mirbfvnsgrid grid,
            mirbfvnsstate state,
            int centralnodeidx,
            int nodeidx,
            double[] xtrial,
            double[] replyfi,
            alglib.xparams _params)
        {
            int n = 0;
            int pointidx = 0;
            int st = 0;
            int i = 0;
            double h = 0;
            double mx = 0;
            double v0 = 0;
            double v1 = 0;
            double nodefbest = 0;
            double nodehbest = 0;
            double nodemxbest = 0;
            double centralfbest = 0;
            double centralhbest = 0;
            mirbfvnsnodesubsolver subsolver = null;
            double preddeltaf = 0;
            double preddeltah = 0;
            bool acceptablebymarkovfilter = new bool();
            bool sufficientdecreasef = new bool();
            bool sufficientdecreaseh = new bool();

            n = state.n;
            alglib.ap.assert(nodeidx>=0 && nodeidx<grid.nnodes, "MIRBFVNS: 116651");
            if( !apserv.isfinitevector(replyfi, 1+state.nnlc, _params) )
            {
                st = gridgetstatus(grid, state, nodeidx, _params);
                if( st==nodeunexplored )
                {
                    grid.nodesinfo[nodeidx,n+ncolstatus] = nodebad;
                }
                else
                {
                    alglib.ap.assert(st==nodeinprogress && state.nfrac>0, "MIRBFVNS: 175604");
                    grid.subsolvers.get(nodeidx, ref subsolver);
                    alglib.ap.assert(subsolver!=null, "MIRBFVNS: 412607");
                    if( state.doextratrace )
                    {
                        alglib.ap.trace(System.String.Format(">>> infinities detected, decreasing trust radius for node {0,0:d}\n", nodeidx));
                    }
                    subsolver.trustrad = 0.5*subsolver.trustrad;
                    if( (double)(subsolver.trustrad)<=(double)(state.epsx) )
                    {
                        grid.nodesinfo[nodeidx,n+ncolstatus] = nodesolved;
                    }
                }
                return;
            }
            computeviolation2(state, xtrial, replyfi, ref h, ref mx, _params);
            pointidx = datasetappendpoint(state.dataset, xtrial, replyfi, h, mx, _params);
            gridappendpointtolist(grid, pointidx, nodeidx, _params);
            st = gridgetstatus(grid, state, nodeidx, _params);
            if( st==nodeunexplored )
            {
                grid.nodesinfo[nodeidx,n+ncolstatus] = apserv.icase2(state.nfrac==0, nodesolved, nodeinprogress, _params);
                grid.nodesinfo[nodeidx,n+ncolfbest] = replyfi[0];
                grid.nodesinfo[nodeidx,n+ncolhbest] = h;
                grid.nodesinfo[nodeidx,n+ncolmxbest] = mx;
                grid.nodesinfo[nodeidx,n+ncollastaccepted] = pointidx;
                if( state.nfrac>0 )
                {
                    gridinitnilsubsolver(grid, state, nodeidx, replyfi[0], h, mx, _params);
                }
            }
            else
            {
                alglib.ap.assert(st==nodeinprogress && state.nfrac>0, "MIRBFVNS: 117652");
                if( isbetterpoint(grid.nodesinfo[nodeidx,n+ncolfbest], grid.nodesinfo[nodeidx,n+ncolhbest], grid.nodesinfo[nodeidx,n+ncolmxbest], replyfi[0], h, mx, state.ctol, _params) )
                {
                    grid.nodesinfo[nodeidx,n+ncolfbest] = replyfi[0];
                    grid.nodesinfo[nodeidx,n+ncolhbest] = h;
                    grid.nodesinfo[nodeidx,n+ncolmxbest] = mx;
                }
                grid.subsolvers.get(nodeidx, ref subsolver);
                alglib.ap.assert(subsolver!=null, "MIRBFVNS: 412607");
                if( subsolver.sufficientcloudsize )
                {
                    preddeltaf = subsolver.predf-subsolver.basef;
                    preddeltah = subsolver.predh-subsolver.baseh;
                    sufficientdecreasef = (double)(preddeltaf)<(double)(0) && (double)(replyfi[0])<(double)(subsolver.basef+eta2*preddeltaf);
                    sufficientdecreaseh = (double)(preddeltah)<(double)(0) && (double)(h)<(double)(subsolver.baseh+eta2*preddeltah);
                    acceptablebymarkovfilter = (double)(h)<=(double)(subsolver.maxh);
                    acceptablebymarkovfilter = acceptablebymarkovfilter && ((double)(replyfi[0])<(double)(subsolver.basef) || (double)(h)<(double)(subsolver.baseh));
                    acceptablebymarkovfilter = acceptablebymarkovfilter && (sufficientdecreasef || sufficientdecreaseh);
                    if( acceptablebymarkovfilter )
                    {
                        if( (double)(subsolver.skrellen)>(double)(rbfsktooshort) )
                        {
                            if( (double)(subsolver.predf)<(double)(subsolver.basef) )
                            {
                                if( (double)((subsolver.basef-replyfi[0])/(subsolver.basef-subsolver.predf))>(double)(eta2) )
                                {
                                    subsolver.trustrad = Math.Min(gammainc*subsolver.trustrad, gammainc2*(subsolver.skrellen*subsolver.trustrad));
                                }
                                else
                                {
                                    subsolver.trustrad = Math.Max(gammadec, gammadec2*subsolver.skrellen)*subsolver.trustrad;
                                }
                                if( state.doextratrace )
                                {
                                    alglib.ap.trace(System.String.Format("[{0,6:d}] >>> acceptable, predicted f-step, predDeltaF={1,0:E2}  ratio={2,0:E2}    predDeltaH={3,0:E2}  ratio={4,0:E2}, trustRad:={5,0:E2}\n", nodeidx, -(subsolver.basef-subsolver.predf), (subsolver.basef-replyfi[0])/(subsolver.basef-subsolver.predf), -(subsolver.baseh-subsolver.predh), (subsolver.baseh-h)/(subsolver.baseh-subsolver.predh), subsolver.trustrad));
                                }
                            }
                            else
                            {
                                if( (double)(subsolver.predh)<(double)(subsolver.baseh) && (double)((subsolver.baseh-h)/(subsolver.baseh-subsolver.predh))>(double)(eta2) )
                                {
                                    subsolver.trustrad = Math.Min(gammainc*subsolver.trustrad, gammainc2*(subsolver.skrellen*subsolver.trustrad));
                                }
                                else
                                {
                                    subsolver.trustrad = Math.Max(gammadec, gammadec2*subsolver.skrellen)*subsolver.trustrad;
                                }
                                if( state.doextratrace )
                                {
                                    alglib.ap.trace(System.String.Format("[{0,6:d}] >>> acceptable, predicted h-step, predDeltaF={1,0:E2}  ratio={2,0:E2}    predDeltaH={3,0:E2}  ratio={4,0:E2}, trustRad:={5,0:E2}\n", nodeidx, -(subsolver.basef-subsolver.predf), (subsolver.basef-replyfi[0])/(subsolver.basef-subsolver.predf), -(subsolver.baseh-subsolver.predh), (subsolver.baseh-h)/(subsolver.baseh-subsolver.predh), subsolver.trustrad));
                                }
                            }
                        }
                        else
                        {
                            subsolver.trustrad = Math.Min(10*rbfsktooshort, 0.1)*subsolver.trustrad;
                            if( state.doextratrace )
                            {
                                alglib.ap.trace(System.String.Format("[{0,6:d}] >>> acceptable, Sk is too short, decreasing trustRad:={1,0:E2}\n", nodeidx, subsolver.trustrad));
                            }
                        }
                        grid.nodesinfo[nodeidx,n+ncollastaccepted] = pointidx;
                        for(i=0; i<=subsolver.historymax-2; i++)
                        {
                            subsolver.successfhistory[i] = subsolver.successfhistory[i+1];
                        }
                        subsolver.successfhistory[subsolver.historymax-1] = Math.Abs(replyfi[0]-subsolver.basef);
                        for(i=0; i<=subsolver.historymax-2; i++)
                        {
                            subsolver.successhhistory[i] = subsolver.successhhistory[i+1];
                        }
                        subsolver.successhhistory[subsolver.historymax-1] = Math.Abs(h-subsolver.baseh);
                    }
                    else
                    {
                        if( state.doextratrace )
                        {
                            alglib.ap.trace(System.String.Format("[{0,6:d}] >>> unacceptable, DeltaF={1,0:E2}, DeltaH={2,0:E2}\n", nodeidx, -(subsolver.basef-replyfi[0]), -(subsolver.baseh-h)));
                        }
                        subsolver.trustrad = Math.Max(gammadec3, gammadec2*subsolver.skrellen)*subsolver.trustrad;
                    }
                }
                centralfbest = gridgetfbest(grid, state, centralnodeidx, _params);
                centralhbest = gridgethbest(grid, state, centralnodeidx, _params);
                nodefbest = gridgetfbest(grid, state, nodeidx, _params);
                nodehbest = gridgethbest(grid, state, nodeidx, _params);
                nodemxbest = gridgetmxbest(grid, state, nodeidx, _params);
                v0 = 0;
                v1 = 0;
                for(i=0; i<=subsolver.historymax-1; i++)
                {
                    v0 = v0+subsolver.successfhistory[i];
                    v1 = v1+subsolver.successhhistory[i];
                }
                if( (double)(subsolver.trustrad)<=(double)(state.epsx) )
                {
                    if( state.doextratrace )
                    {
                        alglib.ap.trace(System.String.Format("[{0,6:d}] >>> STOP: trust radius is small ({1,0:E2}), marking as solved (fbest={2,0:E6},maxv={3,0:E6})\n", nodeidx, subsolver.trustrad, nodefbest, nodemxbest));
                    }
                    grid.nodesinfo[nodeidx,n+ncolstatus] = nodesolved;
                }
                if( (double)(nodemxbest)<=(double)(state.ctol) )
                {
                    if( nodeidx==centralnodeidx )
                    {
                        if( (double)(Math.Abs(v0)*habovezero)<(double)(state.epsf*apserv.rmaxabs2(nodefbest, 1.0, _params)) )
                        {
                            if( state.doextratrace )
                            {
                                alglib.ap.trace(System.String.Format("[{0,6:d}] >>> STOP: central node feasible, objective change is small, marking as solved (fbest={1,0:E6},maxv={2,0:E6})\n", nodeidx, nodefbest, nodemxbest));
                            }
                            grid.nodesinfo[nodeidx,n+ncolstatus] = nodesolved;
                        }
                    }
                    else
                    {
                        if( (double)(nodefbest-Math.Abs(v0)*habovezero)>(double)(centralfbest) && (double)(Math.Abs(v0)*habovezero)<(double)(Math.Max(state.quickepsf, 2*state.epsf)*apserv.rmaxabs2(nodefbest, 1.0, _params)) )
                        {
                            if( state.doextratrace )
                            {
                                alglib.ap.trace(System.String.Format("[{0,6:d}] >>> STOP: neighbor node feasible and worse than central one, objective change is small, marking as solved (fbest={1,0:E6},maxv={2,0:E6})\n", nodeidx, nodefbest, nodemxbest));
                            }
                            grid.nodesinfo[nodeidx,n+ncolstatus] = nodesolved;
                        }
                    }
                }
                else
                {
                    if( nodeidx==centralnodeidx )
                    {
                        if( (double)(nodehbest-Math.Abs(v1)*habovezero)>(double)(0) && (double)(Math.Abs(v1)*habovezero)<(double)(state.epsf*apserv.rmaxabs2(nodehbest, 1.0, _params)) )
                        {
                            if( state.doextratrace )
                            {
                                alglib.ap.trace(System.String.Format("[{0,6:d}] >>> STOP: central node infeasible, constraint violation converged, marking as solved (fbest={1,0:E6},maxv={2,0:E6})\n", nodeidx, nodefbest, nodemxbest));
                            }
                            grid.nodesinfo[nodeidx,n+ncolstatus] = nodesolved;
                        }
                    }
                    else
                    {
                        if( (double)(nodehbest-Math.Abs(v1)*habovezero)>(double)(centralhbest) && (double)(Math.Abs(v1)*habovezero)<(double)(Math.Max(state.quickepsf, 2*state.epsf)*apserv.rmaxabs2(nodehbest, 1.0, _params)) )
                        {
                            if( state.doextratrace )
                            {
                                alglib.ap.trace(System.String.Format("[{0,6:d}] >>> STOP: neighbor node infeasible and worse than central one, constraint violation converged, marking as solved (fbest={1,0:E6},maxv={2,0:E6})\n", nodeidx, nodefbest, nodemxbest));
                            }
                            grid.nodesinfo[nodeidx,n+ncolstatus] = nodesolved;
                        }
                    }
                }
            }
        }


        /*************************************************************************
        Offloads best point for the grid node
        *************************************************************************/
        private static void gridoffloadbestpoint(mirbfvnsgrid grid,
            mirbfvnsstate state,
            int nodeidx,
            ref double[] x,
            ref int pointidx,
            ref double f,
            ref double h,
            ref double mx,
            alglib.xparams _params)
        {
            int n = 0;
            int st = 0;
            int candidx = 0;
            bool firstpoint = new bool();
            int nextlistpos = 0;
            double f1 = 0;
            double h1 = 0;
            double mx1 = 0;

            pointidx = 0;
            f = 0;
            h = 0;
            mx = 0;

            n = state.n;
            alglib.ap.assert(nodeidx>=0 && nodeidx<grid.nnodes, "MIRBFVNS: 116651");
            st = (int)Math.Round(grid.nodesinfo[nodeidx,n+ncolstatus]);
            alglib.ap.assert(st==nodesolved || st==nodeinprogress, "MIRBFVNS: 171713");
            alglib.ap.assert(grid.ptlistheads[2*nodeidx+0]>=0 && grid.ptlistheads[2*nodeidx+1]>0, "MIRBFVNS: 171714");
            ablasf.rallocv(n, ref x, _params);
            f = math.maxrealnumber;
            h = math.maxrealnumber;
            mx = math.maxrealnumber;
            firstpoint = true;
            nextlistpos = grid.ptlistheads[2*nodeidx+0];
            pointidx = -1;
            while( nextlistpos>=0 )
            {
                candidx = grid.ptlistdata[2*nextlistpos+0];
                nextlistpos = grid.ptlistdata[2*nextlistpos+1];
                f1 = state.dataset.pointinfo[candidx,n];
                h1 = state.dataset.pointinfo[candidx,n+1+state.nnlc+0];
                mx1 = state.dataset.pointinfo[candidx,n+1+state.nnlc+1];
                if( firstpoint || isbetterpoint(f, h, mx, f1, h1, mx1, state.ctol, _params) )
                {
                    ablasf.rcopyrv(n, state.dataset.pointinfo, candidx, x, _params);
                    pointidx = candidx;
                    f = f1;
                    h = h1;
                    mx = mx1;
                }
                firstpoint = false;
            }
        }


        /*************************************************************************
        Compares two points, with (objective,sumviolation,maxviolation)=(F,H,MX),
        and returns true if the second one is better. CTol is used to differentiate
        between feasible and infeasible points.

        The base can not be in 'bad' or 'unexplored' state
        *************************************************************************/
        private static bool isbetterpoint(double f0,
            double h0,
            double mx0,
            double f1,
            double h1,
            double mx1,
            double ctol,
            alglib.xparams _params)
        {
            bool result = new bool();

            result = false;
            if( mx0<=ctol && mx1<=ctol )
            {
                result = (double)(f1)<(double)(f0);
            }
            if( mx0<=ctol && mx1>ctol )
            {
                result = false;
            }
            if( mx0>ctol && mx1<=ctol )
            {
                result = true;
            }
            if( mx0>ctol && mx1>ctol )
            {
                result = (double)(h1)<(double)(h0-ctol);
            }
            return result;
        }


        /*************************************************************************
        Compares two grid nodes, returns if the second one is better.

        The base can not be in 'bad' or 'unexplored' state
        *************************************************************************/
        private static bool gridisbetter(mirbfvnsgrid grid,
            mirbfvnsstate state,
            int baseidx,
            int candidx,
            alglib.xparams _params)
        {
            bool result = new bool();
            int n = 0;
            int st0 = 0;
            int st1 = 0;
            double f0 = 0;
            double f1 = 0;
            double h0 = 0;
            double h1 = 0;
            double mx0 = 0;
            double mx1 = 0;

            n = state.n;
            result = false;
            alglib.ap.assert(((baseidx>=0 && baseidx<grid.nnodes) && candidx>=0) && candidx<grid.nnodes, "MIRBFVNS: 204716");
            st0 = (int)Math.Round(grid.nodesinfo[baseidx,n+ncolstatus]);
            st1 = (int)Math.Round(grid.nodesinfo[candidx,n+ncolstatus]);
            alglib.ap.assert((((st0==nodeinprogress || st0==nodesolved) || st0==nodebad) || st0==nodeunexplored) && (((st1==nodeinprogress || st1==nodesolved) || st1==nodebad) || st1==nodeunexplored), "MIRBFVNS: 209730");
            if( st0==nodebad || st0==nodeunexplored )
            {
                result = st1!=nodebad && st1!=nodeunexplored;
                return result;
            }
            if( st1==nodebad || st1==nodeunexplored )
            {
                return result;
            }
            f0 = grid.nodesinfo[baseidx,n+ncolfbest];
            f1 = grid.nodesinfo[candidx,n+ncolfbest];
            h0 = grid.nodesinfo[baseidx,n+ncolhbest];
            h1 = grid.nodesinfo[candidx,n+ncolhbest];
            mx0 = grid.nodesinfo[baseidx,n+ncolmxbest];
            mx1 = grid.nodesinfo[candidx,n+ncolmxbest];
            result = isbetterpoint(f0, h0, mx0, f1, h1, mx1, state.ctol, _params);
            return result;
        }


        /*************************************************************************
        Returns best objective for a node
        *************************************************************************/
        private static double gridgetpointscountinnode(mirbfvnsgrid grid,
            mirbfvnsstate state,
            int nodeidx,
            alglib.xparams _params)
        {
            double result = 0;

            alglib.ap.assert(nodeidx>=0 && nodeidx<grid.nnodes, "MIRBFVNS: 271747");
            result = grid.ptlistheads[2*nodeidx+1];
            return result;
        }


        /*************************************************************************
        Returns best objective for a node
        *************************************************************************/
        private static double gridgetfbest(mirbfvnsgrid grid,
            mirbfvnsstate state,
            int nodeidx,
            alglib.xparams _params)
        {
            double result = 0;
            int n = 0;

            n = state.n;
            alglib.ap.assert(nodeidx>=0 && nodeidx<grid.nnodes, "MIRBFVNS: 271747");
            result = grid.nodesinfo[nodeidx,n+ncolfbest];
            return result;
        }


        /*************************************************************************
        Returns best sum of violations for a node
        *************************************************************************/
        private static double gridgethbest(mirbfvnsgrid grid,
            mirbfvnsstate state,
            int nodeidx,
            alglib.xparams _params)
        {
            double result = 0;
            int n = 0;

            n = state.n;
            alglib.ap.assert(nodeidx>=0 && nodeidx<grid.nnodes, "MIRBFVNS: 271748");
            result = grid.nodesinfo[nodeidx,n+ncolhbest];
            return result;
        }


        /*************************************************************************
        Returns best max of violations for a node
        *************************************************************************/
        private static double gridgetmxbest(mirbfvnsgrid grid,
            mirbfvnsstate state,
            int nodeidx,
            alglib.xparams _params)
        {
            double result = 0;
            int n = 0;

            n = state.n;
            alglib.ap.assert(nodeidx>=0 && nodeidx<grid.nnodes, "MIRBFVNS: 031236");
            result = grid.nodesinfo[nodeidx,n+ncolmxbest];
            return result;
        }


        /*************************************************************************
        This function performs minimization of the RBF model of objective/constraints
        and returns minimum as well as predicted values at the minimum

          -- ALGLIB --
             Copyright 15.10.2024 by Bochkanov Sergey
        *************************************************************************/
        private static void rbfminimizemodel(mirbfmodel model,
            double[] x0,
            double[] bndl,
            double[] bndu,
            double[] trustregion,
            double trustradfactor,
            double ctol,
            int maxitsperphase,
            bool autoscalemodel,
            sparse.sparsematrix c,
            double[] cl,
            double[] cu,
            int lccnt,
            double[] nl,
            double[] nu,
            int nnlc,
            int n,
            rbfmmtemporaries buf,
            ref double[] xn,
            ref double[] sk,
            ref double predf,
            ref double predh,
            ref int subsolverits,
            alglib.xparams _params)
        {
            int i = 0;
            int j = 0;
            int jj = 0;
            int nnz = 0;
            int nx = 0;
            double multiplyby = 0;
            double v = 0;
            double vax = 0;
            double predsum = 0;
            bool usedensebfgs = new bool();

            predf = 0;
            predh = 0;
            subsolverits = 0;

            nx = n+2*lccnt+2*nnlc;
            multiplyby = 1.0;
            optserv.critinitdefault(buf.crit, _params);
            optserv.critsetcondv1(buf.crit, 0.0, rbfsubsolverepsx, maxitsperphase, _params);
            subsolverits = 0;
            ablasf.rallocv(n, ref xn, _params);
            ablasf.rallocv(n, ref sk, _params);
            usedensebfgs = false;
            ablasf.rallocv(nx, ref buf.bndlx, _params);
            ablasf.rallocv(nx, ref buf.bndux, _params);
            ablasf.rallocv(nx, ref buf.x0x, _params);
            ablasf.rallocv(nx, ref buf.sx, _params);
            for(i=0; i<=n-1; i++)
            {
                buf.bndlx[i] = x0[i]-trustregion[i];
                buf.bndux[i] = x0[i]+trustregion[i];
                if( math.isfinite(bndl[i]) && (double)(bndl[i])>(double)(buf.bndlx[i]) )
                {
                    buf.bndlx[i] = bndl[i];
                }
                if( math.isfinite(bndu[i]) && (double)(bndu[i])<(double)(buf.bndux[i]) )
                {
                    buf.bndux[i] = bndu[i];
                }
                buf.x0x[i] = x0[i];
                buf.sx[i] = trustregion[i];
            }
            for(i=n; i<=nx-1; i++)
            {
                buf.bndlx[i] = 0.0;
                buf.bndux[i] = Double.PositiveInfinity;
                buf.sx[i] = trustradfactor*multiplyby;
                buf.x0x[i] = 0.0;
            }
            ablasf.rallocv(lccnt+1, ref buf.clx, _params);
            ablasf.rallocv(lccnt+1, ref buf.cux, _params);
            sparse.sparsecreatecrsemptybuf(nx, buf.cx, _params);
            ablasf.iallocv(nx, ref buf.tmpi, _params);
            ablasf.rallocv(nx, ref buf.tmp0, _params);
            for(i=0; i<=lccnt-1; i++)
            {
                nnz = 0;
                vax = 0;
                for(jj=c.ridx[i]; jj<=c.ridx[i+1]-1; jj++)
                {
                    j = c.idx[jj];
                    v = c.vals[jj];
                    buf.tmpi[nnz] = j;
                    buf.tmp0[nnz] = v;
                    vax = vax+v*x0[j];
                    nnz = nnz+1;
                }
                buf.clx[i] = cl[i];
                if( math.isfinite(cl[i]) )
                {
                    buf.tmpi[nnz] = n+2*i+0;
                    buf.tmp0[nnz] = 1.0;
                    buf.x0x[n+2*i+0] = Math.Max(cl[i]-vax, 0.0);
                    nnz = nnz+1;
                }
                buf.cux[i] = cu[i];
                if( math.isfinite(cu[i]) )
                {
                    buf.tmpi[nnz] = n+2*i+1;
                    buf.tmp0[nnz] = -1.0;
                    buf.x0x[n+2*i+1] = Math.Max(vax-cu[i], 0.0);
                    nnz = nnz+1;
                }
                alglib.ap.assert(nnz<=nx, "RBF4OPT: integrity check 800519 failed");
                sparse.sparseappendcompressedrow(buf.cx, buf.tmpi, buf.tmp0, nnz, _params);
            }
            ablasf.iallocv(lccnt, ref buf.tmpi, _params);
            for(i=0; i<=lccnt-1; i++)
            {
                buf.tmpi[i] = i;
            }
            ablasf.rallocv(1+nnlc, ref buf.tmp0, _params);
            ablasf.rallocv((1+nnlc)*n, ref buf.tmp1, _params);
            rbfcomputemodel(model, buf.x0x, buf.tmp0, true, buf.tmp1, true, _params);
            ablasf.rsetallocv(1+nnlc, 1.0, ref buf.scalingfactors, _params);
            if( autoscalemodel )
            {
                for(i=0; i<=nnlc; i++)
                {
                    v = 0.0;
                    for(j=0; j<=n-1; j++)
                    {
                        v = Math.Max(v, Math.Abs(buf.tmp1[i*n+j]));
                    }
                    buf.scalingfactors[i] = 1/Math.Max(v, 1);
                }
            }
            ablasf.rcopyallocv(nnlc, nl, ref buf.tmpnl, _params);
            ablasf.rcopyallocv(nnlc, nu, ref buf.tmpnu, _params);
            for(i=0; i<=nnlc-1; i++)
            {
                if( math.isfinite(nl[i]) )
                {
                    buf.tmpnl[i] = buf.tmpnl[i]*buf.scalingfactors[i+1];
                    buf.x0x[n+2*lccnt+2*i+0] = Math.Max(buf.tmpnl[i]-buf.scalingfactors[i+1]*buf.tmp0[1+i], 0.0);
                }
                if( math.isfinite(nu[i]) )
                {
                    buf.tmpnu[i] = buf.tmpnu[i]*buf.scalingfactors[i+1];
                    buf.x0x[n+2*lccnt+2*i+1] = Math.Max(buf.scalingfactors[i+1]*buf.tmp0[1+i]-buf.tmpnu[i], 0.0);
                }
            }
            predsum = 0;
            if( lccnt+nnlc>0 )
            {
                nlcfsqp.minfsqpinitbuf(buf.bndlx, buf.bndux, buf.sx, buf.x0x, nx, buf.cx, buf.clx, buf.cux, buf.tmpi, lccnt, buf.tmpnl, buf.tmpnu, nnlc, buf.crit, usedensebfgs, buf.fsqpsolver, _params);
                nlcfsqp.minfsqpsetinittrustrad(buf.fsqpsolver, 1.0, _params);
                optserv.smoothnessmonitorinit(buf.smonitor, buf.sx, nx, 1+nnlc, false, _params);
                while( nlcfsqp.minfsqpiteration(buf.fsqpsolver, buf.smonitor, false, _params) )
                {
                    if( buf.fsqpsolver.xupdated )
                    {
                        continue;
                    }
                    if( buf.fsqpsolver.needfisj )
                    {
                        ablasf.rallocv(nx, ref buf.tmp0, _params);
                        ablasf.rallocv((1+nnlc)*n, ref buf.tmp1, _params);
                        ablasf.rcopyv(nx, buf.fsqpsolver.x, buf.tmp0, _params);
                        ablasf.rmergemulv(nx, buf.sx, buf.tmp0, _params);
                        ablasf.rmergemaxv(nx, buf.bndlx, buf.tmp0, _params);
                        ablasf.rmergeminv(nx, buf.bndux, buf.tmp0, _params);
                        rbfcomputemodel(model, buf.tmp0, buf.fsqpsolver.fi, true, buf.tmp1, true, _params);
                        ablasf.rsetallocv((1+nnlc)*nx, 0.0, ref buf.tmp2, _params);
                        buf.fsqpsolver.fi[0] = 0;
                        for(j=n; j<=nx-1; j++)
                        {
                            buf.fsqpsolver.fi[0] = buf.fsqpsolver.fi[0]+buf.tmp0[j];
                            buf.tmp2[j] = 1.0;
                        }
                        for(i=1; i<=nnlc; i++)
                        {
                            buf.fsqpsolver.fi[i] = buf.fsqpsolver.fi[i]*buf.scalingfactors[i];
                            ablasf.rcopyvx(n, buf.tmp1, i*n, buf.tmp2, i*nx, _params);
                            ablasf.rmulvx(n, buf.scalingfactors[i], buf.tmp2, i*nx, _params);
                            if( math.isfinite(nl[i-1]) )
                            {
                                j = n+2*lccnt+2*(i-1)+0;
                                buf.fsqpsolver.fi[i] = buf.fsqpsolver.fi[i]+buf.tmp0[j];
                                buf.tmp2[i*nx+j] = 1.0;
                            }
                            if( math.isfinite(nu[i-1]) )
                            {
                                j = n+2*lccnt+2*(i-1)+1;
                                buf.fsqpsolver.fi[i] = buf.fsqpsolver.fi[i]-buf.tmp0[j];
                                buf.tmp2[i*nx+j] = -1.0;
                            }
                        }
                        sparse.sparsecreatecrsfromdensev(buf.tmp2, 1+nnlc, nx, buf.fsqpsolver.sj, _params);
                        sparse.sparsemultiplycolsby(buf.fsqpsolver.sj, buf.sx, _params);
                        continue;
                    }
                    alglib.ap.assert(false, "RBF4OPT: integrity check 858514 failed");
                }
                alglib.ap.assert(buf.fsqpsolver.repterminationtype>0, "RBF4OPT: integrity check 860514 failed");
                subsolverits = subsolverits+buf.fsqpsolver.repiterationscount;
                ablasf.rcopyv(nx, buf.fsqpsolver.stepk.x, buf.x0x, _params);
                ablasf.rmergemulv(nx, buf.sx, buf.x0x, _params);
                ablasf.rmergemaxv(nx, buf.bndlx, buf.x0x, _params);
                ablasf.rmergeminv(nx, buf.bndux, buf.x0x, _params);
                predsum = 0;
                if( lccnt>0 )
                {
                    sparse.sparsemv(c, buf.x0x, ref buf.tmp0, _params);
                    for(i=0; i<=lccnt-1; i++)
                    {
                        if( math.isfinite(cl[i]) )
                        {
                            predsum = predsum+Math.Max(cl[i]-buf.tmp0[i], 0.0);
                        }
                        if( math.isfinite(cu[i]) )
                        {
                            predsum = predsum+Math.Max(buf.tmp0[i]-cu[i], 0.0);
                        }
                    }
                }
                ablasf.rallocv(1+nnlc, ref buf.tmp1, _params);
                rbfcomputemodel(model, buf.x0x, buf.tmp1, true, buf.tmp0, false, _params);
                for(i=0; i<=nnlc-1; i++)
                {
                    if( math.isfinite(nl[i]) )
                    {
                        predsum = predsum+Math.Max(buf.tmpnl[i]-buf.tmp1[1+i]*buf.scalingfactors[1+i], 0.0);
                    }
                    if( math.isfinite(nu[i]) )
                    {
                        predsum = predsum+Math.Max(buf.tmp1[1+i]*buf.scalingfactors[1+i]-buf.tmpnu[i], 0.0);
                    }
                }
            }
            ablasf.iallocv(nx, ref buf.tmpi, _params);
            ablasf.rallocv(nx, ref buf.tmp0, _params);
            nnz = 0;
            for(i=n; i<=nx-1; i++)
            {
                buf.tmpi[nnz] = i;
                buf.tmp0[nnz] = 1.0;
                nnz = nnz+1;
            }
            alglib.ap.assert(alglib.ap.len(buf.clx)>=lccnt+1 && alglib.ap.len(buf.cux)>=lccnt+1, "RBF4OPT: integrity check 889517 failed");
            sparse.sparseappendcompressedrow(buf.cx, buf.tmpi, buf.tmp0, nnz, _params);
            buf.clx[lccnt] = Double.NegativeInfinity;
            buf.cux[lccnt] = Math.Max(predsum, 0.1*ctol);
            ablasf.iallocv(lccnt+1, ref buf.tmpi, _params);
            for(i=0; i<=lccnt; i++)
            {
                buf.tmpi[i] = i;
            }
            nlcfsqp.minfsqpinitbuf(buf.bndlx, buf.bndux, buf.sx, buf.x0x, nx, buf.cx, buf.clx, buf.cux, buf.tmpi, lccnt+1, buf.tmpnl, buf.tmpnu, nnlc, buf.crit, usedensebfgs, buf.fsqpsolver, _params);
            nlcfsqp.minfsqpsetinittrustrad(buf.fsqpsolver, 1.0, _params);
            optserv.smoothnessmonitorinit(buf.smonitor, buf.sx, nx, 1+nnlc, false, _params);
            while( nlcfsqp.minfsqpiteration(buf.fsqpsolver, buf.smonitor, false, _params) )
            {
                if( buf.fsqpsolver.xupdated )
                {
                    continue;
                }
                if( buf.fsqpsolver.needfisj )
                {
                    ablasf.rallocv(nx, ref buf.tmp0, _params);
                    ablasf.rallocv((1+nnlc)*n, ref buf.tmp1, _params);
                    ablasf.rcopyv(nx, buf.fsqpsolver.x, buf.tmp0, _params);
                    ablasf.rmergemulv(nx, buf.sx, buf.tmp0, _params);
                    ablasf.rmergemaxv(nx, buf.bndlx, buf.tmp0, _params);
                    ablasf.rmergeminv(nx, buf.bndux, buf.tmp0, _params);
                    rbfcomputemodel(model, buf.tmp0, buf.fsqpsolver.fi, true, buf.tmp1, true, _params);
                    ablasf.rsetallocv((1+nnlc)*nx, 0.0, ref buf.tmp2, _params);
                    buf.fsqpsolver.fi[0] = buf.fsqpsolver.fi[0]*buf.scalingfactors[0];
                    ablasf.rcopyvx(n, buf.tmp1, 0, buf.tmp2, 0, _params);
                    ablasf.rmulvx(n, buf.scalingfactors[0], buf.tmp2, 0, _params);
                    for(i=1; i<=nnlc; i++)
                    {
                        buf.fsqpsolver.fi[i] = buf.fsqpsolver.fi[i]*buf.scalingfactors[i];
                        ablasf.rcopyvx(n, buf.tmp1, i*n, buf.tmp2, i*nx, _params);
                        ablasf.rmulvx(n, buf.scalingfactors[i], buf.tmp2, i*nx, _params);
                        if( math.isfinite(nl[i-1]) )
                        {
                            j = n+2*lccnt+2*(i-1)+0;
                            buf.fsqpsolver.fi[i] = buf.fsqpsolver.fi[i]+buf.tmp0[j];
                            buf.tmp2[i*nx+j] = 1.0;
                        }
                        if( math.isfinite(nu[i-1]) )
                        {
                            j = n+2*lccnt+2*(i-1)+1;
                            buf.fsqpsolver.fi[i] = buf.fsqpsolver.fi[i]-buf.tmp0[j];
                            buf.tmp2[i*nx+j] = -1.0;
                        }
                    }
                    sparse.sparsecreatecrsfromdensev(buf.tmp2, 1+nnlc, nx, buf.fsqpsolver.sj, _params);
                    sparse.sparsemultiplycolsby(buf.fsqpsolver.sj, buf.sx, _params);
                    continue;
                }
                alglib.ap.assert(false, "DFGM: integrity check 261738 failed");
            }
            subsolverits = subsolverits+buf.fsqpsolver.repiterationscount;
            ablasf.rcopyv(n, buf.fsqpsolver.stepk.x, buf.x0x, _params);
            ablasf.rmergemulv(nx, buf.sx, buf.x0x, _params);
            ablasf.rmergemaxv(nx, buf.bndlx, buf.x0x, _params);
            ablasf.rmergeminv(nx, buf.bndux, buf.x0x, _params);
            ablasf.rcopyv(n, buf.x0x, xn, _params);
            ablasf.rallocv(1+nnlc, ref buf.tmp1, _params);
            ablasf.rcopyv(n, xn, sk, _params);
            ablasf.raddv(n, -1.0, x0, sk, _params);
            rbfcomputemodel(model, xn, buf.tmp1, true, buf.tmp0, false, _params);
            predf = buf.tmp1[0];
            predh = 0;
            if( lccnt>0 )
            {
                sparse.sparsemv(c, xn, ref buf.tmp0, _params);
                for(i=0; i<=lccnt-1; i++)
                {
                    if( math.isfinite(cl[i]) )
                    {
                        predh = predh+Math.Max(cl[i]-buf.tmp0[i], 0.0);
                    }
                    if( math.isfinite(cu[i]) )
                    {
                        predh = predh+Math.Max(buf.tmp0[i]-cu[i], 0.0);
                    }
                }
            }
            for(i=0; i<=nnlc-1; i++)
            {
                if( math.isfinite(nl[i]) )
                {
                    predh = predh+Math.Max(nl[i]-buf.tmp1[1+i], 0.0);
                }
                if( math.isfinite(nu[i]) )
                {
                    predh = predh+Math.Max(buf.tmp1[1+i]-nu[i], 0.0);
                }
            }
        }


        /*************************************************************************
        This function performs initial construction of an RBF model.

          -- ALGLIB --
             Copyright 15.10.2024 by Bochkanov Sergey
        *************************************************************************/
        private static void rbfinitmodel(double[,] xf,
            double[] multscale,
            int nc,
            int n,
            int nf,
            mirbfmodel model,
            alglib.xparams _params)
        {
            int i = 0;
            int j = 0;
            int k = 0;
            int fidx = 0;
            double v = 0;
            double[,] rbfsys = new double[0,0];
            double[] sol = new double[0];
            double[] rhs = new double[0];
            double[] sol2 = new double[0];
            double[,] rrhs = new double[0,0];
            double[,] ssol = new double[0,0];

            alglib.ap.assert((nc>=1 && n>=1) && nf>=1, "RBF4OPT: 234603 failed");
            ablasf.rsetallocm(nc+n+1, nc+n+1, 0.0, ref rbfsys, _params);
            for(i=0; i<=nc-1; i++)
            {
                for(j=0; j<=i; j++)
                {
                    v = 0;
                    for(k=0; k<=n-1; k++)
                    {
                        v = v+math.sqr((xf[i,k]-xf[j,k])*multscale[k]);
                    }
                    v = v*Math.Sqrt(v);
                    rbfsys[i,j] = v;
                    rbfsys[j,i] = v;
                }
            }
            for(i=0; i<=n-1; i++)
            {
                for(j=0; j<=nc-1; j++)
                {
                    rbfsys[nc+i,j] = (xf[j,i]-xf[0,i])*multscale[i];
                    rbfsys[j,nc+i] = rbfsys[nc+i,j];
                }
            }
            for(j=0; j<=nc-1; j++)
            {
                rbfsys[nc+n,j] = 1.0;
                rbfsys[j,nc+n] = 1.0;
            }
            ablasf.rallocv(nf, ref model.vmodelbase, _params);
            ablasf.rallocv(nf, ref model.vmodelscale, _params);
            for(j=0; j<=nf-1; j++)
            {
                model.vmodelbase[j] = xf[0,n+j];
                v = 0;
                for(i=0; i<=nc-1; i++)
                {
                    v = v+math.sqr(xf[i,n+j]-model.vmodelbase[j]);
                }
                model.vmodelscale[j] = Math.Sqrt(apserv.coalesce(v, 1, _params)/nc);
            }
            model.isdense = true;
            model.n = n;
            model.nc = nc;
            model.nf = nf;
            ablasf.rgrowrowsfixedcolsm(nc, n, ref model.centers, _params);
            ablasf.rcopym(nc, n, xf, model.centers, _params);
            ablasf.rallocm(nf, n, ref model.mx0, _params);
            for(i=0; i<=nf-1; i++)
            {
                ablasf.rcopyrr(n, xf, 0, model.mx0, i, _params);
            }
            ablasf.rcopyallocv(n, multscale, ref model.multscale, _params);
            ablasf.rsetallocm(nf, nc+n+1, 0.0, ref rrhs, _params);
            ablasf.rallocm(nf, nc, ref model.crbf, _params);
            ablasf.rallocm(nf, n+1, ref model.clinear, _params);
            for(fidx=0; fidx<=nf-1; fidx++)
            {
                for(i=0; i<=nc-1; i++)
                {
                    rrhs[fidx,i] = (xf[i,n+fidx]-model.vmodelbase[fidx])/model.vmodelscale[fidx];
                }
            }
            rbfsolvecpdm(rbfsys, rrhs, nc, nf, n, 0.0, true, ref ssol, _params);
            ablasf.rcopym(nf, nc, ssol, model.crbf, _params);
            for(fidx=0; fidx<=nf-1; fidx++)
            {
                for(i=0; i<=n-1; i++)
                {
                    model.clinear[fidx,i] = ssol[fidx,nc+i];
                }
                model.clinear[fidx,n] = ssol[fidx,nc+n];
            }
        }


        /*************************************************************************
        This function performs initial construction of an empty sparse RBF model.

          -- ALGLIB --
             Copyright 15.10.2024 by Bochkanov Sergey
        *************************************************************************/
        private static void rbfinitemptysparsemodel(double[] multscale,
            int n,
            mirbfmodel model,
            alglib.xparams _params)
        {
            alglib.ap.assert(n>=1, "RBF4OPT: 980221 failed");
            model.isdense = false;
            model.n = n;
            model.nf = 0;
            ablasf.rcopyallocv(n, multscale, ref model.multscale, _params);
            ablasf.igrowappendv(1, ref model.cridx, 0, _params);
            sparse.sparsecreatecrsemptybuf(n, model.spcenters, _params);
        }


        /*************************************************************************
        This function appends a constant model to a sparse RBF model.

        The sparse model is given by Model parameter, the constant model is given
        by its value V.

          -- ALGLIB --
             Copyright 15.10.2024 by Bochkanov Sergey
        *************************************************************************/
        private static void rbfappendconstantmodel(mirbfmodel model,
            double v,
            alglib.xparams _params)
        {
            int n = 0;
            int nf = 0;
            int offs = 0;

            nf = model.nf;
            n = model.n;
            alglib.ap.assert(!model.isdense, "RBF4OPT: 102335 failed");
            ablasf.rgrowappendv(nf+1, ref model.vmodelbase, v, _params);
            ablasf.rgrowappendv(nf+1, ref model.vmodelscale, 0.0, _params);
            ablasf.rgrowrowsfixedcolsm(nf+1, n+1, ref model.clinear, _params);
            ablasf.rgrowrowsfixedcolsm(nf+1, n, ref model.mx0, _params);
            ablasf.rsetr(n+1, 0.0, model.clinear, nf, _params);
            ablasf.rsetr(n, 0.0, model.mx0, nf, _params);
            offs = model.cridx[model.nf];
            alglib.ap.assert(offs==model.spcenters.m, "RBF4OPT: 097316 failed");
            ablasf.igrowappendv(nf+2, ref model.cridx, offs, _params);
            model.nf = nf+1;
        }


        /*************************************************************************
        This function appends a dense RBF model to a potentially larger sparse one.

        The sparse model is given by Model parameter, the dense model is given by
        miniModel parameter, with miniModel.N<=Model.N. The mini2full[] array
        maps reduced subspace indexes in [0,miniModel.N) to a full space.

        The dense model must have NF=1.

        The function assumes (but does not check) that Model and miniModel have
        the same Model.multScale[]

          -- ALGLIB --
             Copyright 15.10.2024 by Bochkanov Sergey
        *************************************************************************/
        private static void rbfappendmodel(mirbfmodel model,
            mirbfmodel minimodel,
            int[] mini2full,
            alglib.xparams _params)
        {
            int i = 0;
            int j = 0;
            int n = 0;
            int mn = 0;
            int nf = 0;
            int offs = 0;

            nf = model.nf;
            n = model.n;
            mn = minimodel.n;
            alglib.ap.assert(minimodel.isdense && !model.isdense, "RBF4OPT: 061252 failed");
            alglib.ap.assert(minimodel.n<=model.n, "RBF4OPT: 061253 failed");
            alglib.ap.assert(minimodel.nf==1, "RBF4OPT: 061254 failed");
            ablasf.rgrowappendv(nf+1, ref model.vmodelbase, minimodel.vmodelbase[0], _params);
            ablasf.rgrowappendv(nf+1, ref model.vmodelscale, minimodel.vmodelscale[0], _params);
            ablasf.rgrowrowsfixedcolsm(nf+1, n+1, ref model.clinear, _params);
            ablasf.rgrowrowsfixedcolsm(nf+1, n, ref model.mx0, _params);
            ablasf.rsetr(n, 0.0, model.clinear, nf, _params);
            ablasf.rsetr(n, 0.0, model.mx0, nf, _params);
            for(i=0; i<=mn-1; i++)
            {
                model.clinear[nf,mini2full[i]] = minimodel.clinear[0,i];
                model.mx0[nf,mini2full[i]] = minimodel.mx0[0,i];
            }
            model.clinear[nf,n] = minimodel.clinear[0,minimodel.n];
            offs = model.cridx[model.nf];
            alglib.ap.assert(offs==model.spcenters.m, "RBF4OPT: 097316 failed");
            for(i=0; i<=minimodel.nc-1; i++)
            {
                sparse.sparseappendemptyrow(model.spcenters, _params);
                for(j=0; j<=mn-1; j++)
                {
                    sparse.sparseappendelement(model.spcenters, mini2full[j], minimodel.centers[i,j], _params);
                }
                ablasf.rgrowappendv(offs+1, ref model.spcoeffs, minimodel.crbf[0,i], _params);
                offs = offs+1;
            }
            ablasf.igrowappendv(nf+2, ref model.cridx, offs, _params);
            model.nf = nf+1;
        }


        /*************************************************************************
        This function modifies RBF model by adding linear function to its linear
        term.

            C       array[NF,N+1], one row per function, N columns for coefficients
                    before the x[i]-x0[i] term, one column for the constant term.

          -- ALGLIB --
             Copyright 15.10.2024 by Bochkanov Sergey
        *************************************************************************/
        private static void rbfaddlinearterm(mirbfmodel model,
            double[,] c,
            alglib.xparams _params)
        {
            int n = 0;
            int k = 0;
            int fidx = 0;
            double v = 0;

            alglib.ap.assert(model.isdense, "RBF4OPT: 133304");
            n = model.n;
            for(fidx=0; fidx<=model.nf-1; fidx++)
            {
                for(k=0; k<=n-1; k++)
                {
                    v = model.vmodelscale[fidx]*model.multscale[k];
                    if( v!=0 )
                    {
                        model.clinear[fidx,k] = model.clinear[fidx,k]+c[fidx,k]/v;
                    }
                }
                if( model.vmodelscale[fidx]!=0 )
                {
                    model.clinear[fidx,n] = model.clinear[fidx,n]+c[fidx,n]/model.vmodelscale[fidx];
                }
            }
        }


        /*************************************************************************
        This function computes RBF model at the required point. May return model
        value and its gradient.

          -- ALGLIB --
             Copyright 15.10.2024 by Bochkanov Sergey
        *************************************************************************/
        private static void rbfcomputemodel(mirbfmodel mmodel,
            double[] x,
            double[] f,
            bool needf,
            double[] g,
            bool needg,
            alglib.xparams _params)
        {
            int n = 0;
            int j = 0;
            int k = 0;
            int fidx = 0;
            int cc = 0;
            int c0 = 0;
            int c1 = 0;
            int kk = 0;
            int k0 = 0;
            int k1 = 0;
            double v = 0;
            double r = 0;
            double vf = 0;
            double vc = 0;

            n = mmodel.n;
            alglib.ap.assert(!needf || alglib.ap.len(f)>=mmodel.nf, "RBF4OPT: integrity check 419111 failed");
            alglib.ap.assert(!needg || alglib.ap.len(g)>=mmodel.nf*n, "RBF4OPT: integrity check 419112 failed");
            if( needf )
            {
                ablasf.rsetv(mmodel.nf, 0.0, f, _params);
            }
            if( needg )
            {
                ablasf.rsetv(mmodel.nf*n, 0.0, g, _params);
            }
            if( mmodel.isdense )
            {
                for(fidx=0; fidx<=mmodel.nf-1; fidx++)
                {
                    vf = 0;
                    for(j=0; j<=mmodel.nc-1; j++)
                    {
                        r = 0;
                        for(k=0; k<=n-1; k++)
                        {
                            v = (x[k]-mmodel.centers[j,k])*mmodel.multscale[k];
                            r = r+v*v;
                        }
                        r = Math.Sqrt(r);
                        vf = vf+mmodel.crbf[fidx,j]*(r*r*r);
                        if( needg )
                        {
                            for(k=0; k<=n-1; k++)
                            {
                                g[fidx*n+k] = g[fidx*n+k]+mmodel.crbf[fidx,j]*3*r*(x[k]-mmodel.centers[j,k])*mmodel.multscale[k]*mmodel.multscale[k];
                            }
                        }
                    }
                    for(k=0; k<=n-1; k++)
                    {
                        vf = vf+mmodel.clinear[fidx,k]*(x[k]-mmodel.mx0[fidx,k])*mmodel.multscale[k];
                        if( needg )
                        {
                            g[fidx*n+k] = g[fidx*n+k]+mmodel.clinear[fidx,k]*mmodel.multscale[k];
                        }
                    }
                    vf = vf+mmodel.clinear[fidx,n];
                    if( needf )
                    {
                        f[fidx] = vf*mmodel.vmodelscale[fidx]+mmodel.vmodelbase[fidx];
                    }
                    if( needg )
                    {
                        ablasf.rmulvx(n, mmodel.vmodelscale[fidx], g, fidx*n, _params);
                    }
                }
            }
            else
            {
                for(fidx=0; fidx<=mmodel.nf-1; fidx++)
                {
                    vf = 0;
                    c0 = mmodel.cridx[fidx];
                    c1 = mmodel.cridx[fidx+1]-1;
                    for(cc=c0; cc<=c1; cc++)
                    {
                        r = 0;
                        k0 = mmodel.spcenters.ridx[cc];
                        k1 = mmodel.spcenters.ridx[cc+1]-1;
                        for(kk=k0; kk<=k1; kk++)
                        {
                            k = mmodel.spcenters.idx[kk];
                            v = (x[k]-mmodel.spcenters.vals[kk])*mmodel.multscale[k];
                            r = r+v*v;
                        }
                        r = Math.Sqrt(r);
                        vc = mmodel.spcoeffs[cc];
                        vf = vf+vc*(r*r*r);
                        if( needg )
                        {
                            for(kk=k0; kk<=k1; kk++)
                            {
                                k = mmodel.spcenters.idx[kk];
                                g[fidx*n+k] = g[fidx*n+k]+vc*3*r*(x[k]-mmodel.spcenters.vals[kk])*mmodel.multscale[k]*mmodel.multscale[k];
                            }
                        }
                    }
                    for(k=0; k<=n-1; k++)
                    {
                        vf = vf+mmodel.clinear[fidx,k]*(x[k]-mmodel.mx0[fidx,k])*mmodel.multscale[k];
                        if( needg )
                        {
                            g[fidx*n+k] = g[fidx*n+k]+mmodel.clinear[fidx,k]*mmodel.multscale[k];
                        }
                    }
                    vf = vf+mmodel.clinear[fidx,n];
                    if( needf )
                    {
                        f[fidx] = vf*mmodel.vmodelscale[fidx]+mmodel.vmodelbase[fidx];
                    }
                    if( needg )
                    {
                        ablasf.rmulvx(n, mmodel.vmodelscale[fidx], g, fidx*n, _params);
                    }
                }
            }
        }


        /*************************************************************************
        This function solves RBF  system using conditionally positive definiteness
        if possible.

        INPUT PARAMETERS:
            A           array[NCenters+NX,NCenters], basis function matrix and
                        linear polynomial values
            B           array[NCenters], target values    
            NCenters    centers count
            NX          space dimensionality
            LambdaV     smoothing parameter, LambdaV>=0
            isCPD       whether basis is conditionally positive definite or not


          -- ALGLIB --
             Copyright 15.10.2024 by Bochkanov Sergey
        *************************************************************************/
        private static void rbfsolvecpdm(double[,] a,
            double[,] bb,
            int ncenters,
            int nrhs,
            int nx,
            double lambdav,
            bool iscpd,
            ref double[,] ssol,
            alglib.xparams _params)
        {
            int ncoeff = 0;
            int i = 0;
            int j = 0;
            int k = 0;
            double v = 0;
            double vv = 0;
            double mx = 0;
            double reg = 0;
            int ortbasissize = 0;
            double[,] q = new double[0,0];
            double[,] q1 = new double[0,0];
            double[,] r = new double[0,0];
            double[] c = new double[0];
            double[] y = new double[0];
            double[] z = new double[0];
            int[] ortbasismap = new int[0];
            double[] choltmp = new double[0];

            alglib.ap.assert((double)(lambdav)>=(double)(0), "DFGM: integrity check 854519 failed");
            alglib.ap.assert(iscpd, "DFGM: integrity check 855520 failed");
            ncoeff = ncenters+nx+1;
            reg = Math.Sqrt(math.machineepsilon);
            ablasf.rallocm(nx+1, nx+1, ref r, _params);
            ablasf.rallocm(nx+1, ncenters, ref q1, _params);
            ablasf.iallocv(nx+1, ref ortbasismap, _params);
            ablasf.rsetr(ncenters, 1/Math.Sqrt(ncenters), q1, 0, _params);
            r[0,0] = Math.Sqrt(ncenters);
            ortbasismap[0] = nx;
            ortbasissize = 1;
            ablasf.rallocv(ncenters, ref z, _params);
            for(k=0; k<=nx-1; k++)
            {
                for(j=0; j<=ncenters-1; j++)
                {
                    z[j] = a[ncenters+k,j];
                }
                v = Math.Sqrt(ablasf.rdotv2(ncenters, z, _params));
                ablas.rowwisegramschmidt(q1, ortbasissize, ncenters, z, ref y, true, _params);
                vv = Math.Sqrt(ablasf.rdotv2(ncenters, z, _params));
                if( (double)(vv)>(double)(Math.Sqrt(math.machineepsilon)*(v+1)) )
                {
                    ablasf.rcopymulvr(ncenters, 1/vv, z, q1, ortbasissize, _params);
                    ablasf.rcopyvc(ortbasissize, y, r, ortbasissize, _params);
                    r[ortbasissize,ortbasissize] = vv;
                    ortbasismap[ortbasissize] = k;
                    ortbasissize = ortbasissize+1;
                }
            }
            ablasf.rsetallocm(ncenters, ncenters, 0.0, ref q, _params);
            for(i=0; i<=ncenters-1; i++)
            {
                ablasf.rcopyrr(ncenters, a, i, q, i, _params);
            }
            ablasf.rallocm(nrhs, ncoeff, ref ssol, _params);
            ablasf.rcopym(nrhs, ncenters, bb, ssol, _params);
            for(i=0; i<=ncenters-1; i++)
            {
                q[i,i] = q[i,i]+lambdav;
            }
            ablasf.rallocv(ncenters, ref z, _params);
            for(i=0; i<=ncenters-1; i++)
            {
                ablasf.rcopyrv(ncenters, q, i, z, _params);
                ablas.rowwisegramschmidt(q1, ortbasissize, ncenters, z, ref y, false, _params);
                ablasf.rcopyvr(ncenters, z, q, i, _params);
            }
            for(i=0; i<=ncenters-1; i++)
            {
                ablasf.rcopycv(ncenters, q, i, z, _params);
                ablas.rowwisegramschmidt(q1, ortbasissize, ncenters, z, ref y, false, _params);
                ablasf.rcopyvc(ncenters, z, q, i, _params);
            }
            for(i=0; i<=nrhs-1; i++)
            {
                ablasf.rcopyrv(ncenters, ssol, i, z, _params);
                ablas.rowwisegramschmidt(q1, ortbasissize, ncenters, z, ref y, false, _params);
                ablasf.rcopyvr(ncenters, z, ssol, i, _params);
            }
            mx = 1.0;
            for(i=0; i<=ncenters-1; i++)
            {
                mx = Math.Max(mx, Math.Abs(q[i,i]));
            }
            for(i=0; i<=ncenters-1; i++)
            {
                ablasf.rcopyrv(ncenters, q, i, z, _params);
                for(j=0; j<=ortbasissize-1; j++)
                {
                    ablasf.raddrv(ncenters, mx*q1[j,i], q1, j, z, _params);
                }
                ablasf.rcopyvr(ncenters, z, q, i, _params);
            }
            for(i=0; i<=ncenters-1; i++)
            {
                q[i,i] = q[i,i]+reg*mx;
            }
            if( !trfac.spdmatrixcholeskyrec(q, 0, ncenters, false, ref choltmp, _params) )
            {
                alglib.ap.assert(false, "GENMOD: RBF solver failed due to extreme degeneracy");
            }
            ablas.rmatrixrighttrsm(nrhs, ncenters, q, 0, 0, false, false, 1, ssol, 0, 0, _params);
            ablas.rmatrixrighttrsm(nrhs, ncenters, q, 0, 0, false, false, 0, ssol, 0, 0, _params);
            ablasf.rallocv(ncenters, ref z, _params);
            ablasf.rallocv(ncenters, ref c, _params);
            for(i=0; i<=nrhs-1; i++)
            {
                ablasf.rcopyrv(ncenters, bb, i, z, _params);
                ablasf.rcopyrv(ncenters, ssol, i, c, _params);
                ablasf.rgemv(ncenters, ncenters, -1.0, a, 0, c, 1.0, z, _params);
                ablas.rowwisegramschmidt(q1, ortbasissize, ncenters, z, ref y, true, _params);
                ablas.rmatrixtrsv(ortbasissize, r, 0, 0, true, false, 0, y, 0, _params);
                for(j=0; j<=nx; j++)
                {
                    ssol[i,ncenters+j] = 0.0;
                }
                for(j=0; j<=ortbasissize-1; j++)
                {
                    ssol[i,ncenters+ortbasismap[j]] = y[j];
                }
            }
        }


        private static double rdistinfrr(int n,
            double[,] a,
            int i0,
            double[,] b,
            int i1,
            alglib.xparams _params)
        {
            double result = 0;
            int i = 0;
            double v = 0;

            result = 0;
            for(i=0; i<=n-1; i++)
            {
                v = a[i0,i]-b[i1,i];
                result = Math.Max(result, Math.Abs(v));
            }
            return result;
        }


    }
    public class minlpsolvers
    {
        /*************************************************************************
        This object stores nonlinear optimizer state.
        You should use functions provided by MinNLC subpackage to work  with  this
        object
        *************************************************************************/
        public class minlpsolverstate : apobject
        {
            public int n;
            public int algoidx;
            public optserv.nlpstoppingcriteria criteria;
            public double diffstep;
            public int convexityflag;
            public double pdgap;
            public double ctol;
            public double subsolverepsx;
            public double subsolverepsf;
            public int nmultistarts;
            public int timeout;
            public int bbgdgroupsize;
            public int mirbfvnsbudget;
            public int mirbfvnsmaxneighborhood;
            public int mirbfvnsbatchsize;
            public int mirbfvnsalgo;
            public int adaptiveinternalparallelism;
            public double[] s;
            public double[] bndl;
            public double[] bndu;
            public bool[] isintegral;
            public bool[] isbinary;
            public bool hasobjmask;
            public bool[] objmask;
            public opts.xlinearconstraints xlc;
            public sparse.sparsematrix nlcmask;
            public int nnlc;
            public double[] nl;
            public double[] nu;
            public bool[] hasnlcmask;
            public bool hasx0;
            public double[] x0;
            public int protocolversion;
            public bool issuesparserequests;
            public bool userterminationneeded;
            public double[] xc;
            public int repnfev;
            public int repnsubproblems;
            public int repntreenodes;
            public int repnnodesbeforefeasibility;
            public int repterminationtype;
            public double repf;
            public double reppdgap;
            public int tracelevel;
            public int requesttype;
            public double[] reportx;
            public double reportf;
            public int querysize;
            public int queryfuncs;
            public int queryvars;
            public int querydim;
            public int queryformulasize;
            public double[] querydata;
            public double[] replyfi;
            public double[] replydj;
            public sparse.sparsematrix replysj;
            public double[] tmpx1;
            public double[] tmpc1;
            public double[] tmpf1;
            public double[] tmpg1;
            public double[,] tmpj1;
            public sparse.sparsematrix tmps1;
            public bbgd.bbgdstate bbgdsubsolver;
            public mirbfvns.mirbfvnsstate mirbfvnssubsolver;
            public double[] rdummy;
            public bool[] tmpb1;
            public sparse.sparsematrix tmpsparse;
            public rcommstate rstate;
            public minlpsolverstate()
            {
                init();
            }
            public override void init()
            {
                criteria = new optserv.nlpstoppingcriteria();
                s = new double[0];
                bndl = new double[0];
                bndu = new double[0];
                isintegral = new bool[0];
                isbinary = new bool[0];
                objmask = new bool[0];
                xlc = new opts.xlinearconstraints();
                nlcmask = new sparse.sparsematrix();
                nl = new double[0];
                nu = new double[0];
                hasnlcmask = new bool[0];
                x0 = new double[0];
                xc = new double[0];
                reportx = new double[0];
                querydata = new double[0];
                replyfi = new double[0];
                replydj = new double[0];
                replysj = new sparse.sparsematrix();
                tmpx1 = new double[0];
                tmpc1 = new double[0];
                tmpf1 = new double[0];
                tmpg1 = new double[0];
                tmpj1 = new double[0,0];
                tmps1 = new sparse.sparsematrix();
                bbgdsubsolver = null;
                mirbfvnssubsolver = null;
                rdummy = new double[0];
                tmpb1 = new bool[0];
                tmpsparse = new sparse.sparsematrix();
                rstate = new rcommstate();
            }
            public override alglib.apobject make_copy()
            {
                minlpsolverstate _result = new minlpsolverstate();
                _result.n = n;
                _result.algoidx = algoidx;
                _result.criteria = criteria!=null ? (optserv.nlpstoppingcriteria)criteria.make_copy() : null;
                _result.diffstep = diffstep;
                _result.convexityflag = convexityflag;
                _result.pdgap = pdgap;
                _result.ctol = ctol;
                _result.subsolverepsx = subsolverepsx;
                _result.subsolverepsf = subsolverepsf;
                _result.nmultistarts = nmultistarts;
                _result.timeout = timeout;
                _result.bbgdgroupsize = bbgdgroupsize;
                _result.mirbfvnsbudget = mirbfvnsbudget;
                _result.mirbfvnsmaxneighborhood = mirbfvnsmaxneighborhood;
                _result.mirbfvnsbatchsize = mirbfvnsbatchsize;
                _result.mirbfvnsalgo = mirbfvnsalgo;
                _result.adaptiveinternalparallelism = adaptiveinternalparallelism;
                _result.s = (double[])s.Clone();
                _result.bndl = (double[])bndl.Clone();
                _result.bndu = (double[])bndu.Clone();
                _result.isintegral = (bool[])isintegral.Clone();
                _result.isbinary = (bool[])isbinary.Clone();
                _result.hasobjmask = hasobjmask;
                _result.objmask = (bool[])objmask.Clone();
                _result.xlc = xlc!=null ? (opts.xlinearconstraints)xlc.make_copy() : null;
                _result.nlcmask = nlcmask!=null ? (sparse.sparsematrix)nlcmask.make_copy() : null;
                _result.nnlc = nnlc;
                _result.nl = (double[])nl.Clone();
                _result.nu = (double[])nu.Clone();
                _result.hasnlcmask = (bool[])hasnlcmask.Clone();
                _result.hasx0 = hasx0;
                _result.x0 = (double[])x0.Clone();
                _result.protocolversion = protocolversion;
                _result.issuesparserequests = issuesparserequests;
                _result.userterminationneeded = userterminationneeded;
                _result.xc = (double[])xc.Clone();
                _result.repnfev = repnfev;
                _result.repnsubproblems = repnsubproblems;
                _result.repntreenodes = repntreenodes;
                _result.repnnodesbeforefeasibility = repnnodesbeforefeasibility;
                _result.repterminationtype = repterminationtype;
                _result.repf = repf;
                _result.reppdgap = reppdgap;
                _result.tracelevel = tracelevel;
                _result.requesttype = requesttype;
                _result.reportx = (double[])reportx.Clone();
                _result.reportf = reportf;
                _result.querysize = querysize;
                _result.queryfuncs = queryfuncs;
                _result.queryvars = queryvars;
                _result.querydim = querydim;
                _result.queryformulasize = queryformulasize;
                _result.querydata = (double[])querydata.Clone();
                _result.replyfi = (double[])replyfi.Clone();
                _result.replydj = (double[])replydj.Clone();
                _result.replysj = replysj!=null ? (sparse.sparsematrix)replysj.make_copy() : null;
                _result.tmpx1 = (double[])tmpx1.Clone();
                _result.tmpc1 = (double[])tmpc1.Clone();
                _result.tmpf1 = (double[])tmpf1.Clone();
                _result.tmpg1 = (double[])tmpg1.Clone();
                _result.tmpj1 = (double[,])tmpj1.Clone();
                _result.tmps1 = tmps1!=null ? (sparse.sparsematrix)tmps1.make_copy() : null;
                _result.bbgdsubsolver = bbgdsubsolver!=null ? (bbgd.bbgdstate)bbgdsubsolver.make_copy() : null;
                _result.mirbfvnssubsolver = mirbfvnssubsolver!=null ? (mirbfvns.mirbfvnsstate)mirbfvnssubsolver.make_copy() : null;
                _result.rdummy = (double[])rdummy.Clone();
                _result.tmpb1 = (bool[])tmpb1.Clone();
                _result.tmpsparse = tmpsparse!=null ? (sparse.sparsematrix)tmpsparse.make_copy() : null;
                _result.rstate = rstate!=null ? (rcommstate)rstate.make_copy() : null;
                return _result;
            }
        };


        /*************************************************************************
        This structure stores the optimization report.

        The following fields are set by all MINLP solvers:
        * f                         objective value at the solution
        * nfev                      number of value/gradient evaluations
        * terminationtype           termination type (see below)

        The BBGD solver additionally sets the following fields:
        * pdgap                     final primal-dual gap
        * ntreenodes                number of B&B tree nodes traversed
        * nsubproblems              total number of NLP relaxations solved; can be
                                    larger than ntreenodes because of restarts
        * nnodesbeforefeasibility   number of nodes evaluated before finding first
                                    integer feasible solution

        TERMINATION CODES

        TerminationType field contains completion code, which can be either FAILURE
        code or SUCCESS code.

        === FAILURE CODE ===
          -33   timed out, failed to find a feasible point within  time  limit  or
                iteration budget
          -8    internal integrity control detected  infinite  or  NAN  values  in
                function/gradient, recovery was impossible.  Abnormal  termination
                signaled.
          -3    integer infeasibility is signaled:
                * for convex problems: proved to be infeasible
                * for nonconvex problems: a primal feasible point  is  nonexistent
                  or too difficult to find


        === SUCCESS CODE ===
           2    successful solution:
                * for BBGD - entire tree was scanned
                * for MIVNS - either entire  integer  grid  was  scanned,  or  the
                  neighborhood size  based  condition  was  triggered  (in  future
                  versions other criteria may be introduced)
           5    a primal feasible point was found, but time or iteration limit was
                exhausted but we  failed  to  find  a  better  one  or  prove  its
                optimality; the best point so far is returned.

        *************************************************************************/
        public class minlpsolverreport : apobject
        {
            public double f;
            public int nfev;
            public int nsubproblems;
            public int ntreenodes;
            public int nnodesbeforefeasibility;
            public int terminationtype;
            public double pdgap;
            public minlpsolverreport()
            {
                init();
            }
            public override void init()
            {
            }
            public override alglib.apobject make_copy()
            {
                minlpsolverreport _result = new minlpsolverreport();
                _result.f = f;
                _result.nfev = nfev;
                _result.nsubproblems = nsubproblems;
                _result.ntreenodes = ntreenodes;
                _result.nnodesbeforefeasibility = nnodesbeforefeasibility;
                _result.terminationtype = terminationtype;
                _result.pdgap = pdgap;
                return _result;
            }
        };




        /*************************************************************************
                        MIXED INTEGER NONLINEAR PROGRAMMING SOLVER

        DESCRIPTION:
        The  subroutine  minimizes a function  F(x)  of N arguments subject to any
        combination of:
        * box constraints
        * linear equality/inequality/range constraints CL<=Ax<=CU
        * nonlinear equality/inequality/range constraints HL<=Hi(x)<=HU
        * integrality constraints on some variables

        REQUIREMENTS:
        * F(), H() are continuously differentiable on the  feasible  set  and  its
          neighborhood
        * starting point X0, which can be infeasible

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
             Copyright 01.01.2025 by Bochkanov Sergey
        *************************************************************************/
        public static void minlpsolvercreate(int n,
            double[] x,
            minlpsolverstate state,
            alglib.xparams _params)
        {
            alglib.ap.assert(n>=1, "MINLPSolverCreate: N<1");
            alglib.ap.assert(alglib.ap.len(x)>=n, "MINLPSolverCreate: Length(X)<N");
            alglib.ap.assert(apserv.isfinitevector(x, n, _params), "MINLPSolverCreate: X contains infinite or NaN values");
            initinternal(n, x, 0, 0.0, state, _params);
        }


        /*************************************************************************
        This function sets box constraints for the mixed integer optimizer.

        Box constraints are inactive by default.

        IMPORTANT: box constraints work in parallel with the integrality ones:
                   * a variable marked as integral is considered  having no bounds
                     until minlpsolversetbc() is called
                   * a  variable  with  lower  and  upper bounds set is considered
                     continuous   until    marked    as    integral    with    the
                     minlpsolversetintkth() function.

        INPUT PARAMETERS:
            State   -   structure stores algorithm state
            BndL    -   lower bounds, array[N].
                        If some (all) variables are unbounded, you may  specify  a
                        very small number or -INF, with the  latter  option  being
                        recommended.
            BndU    -   upper bounds, array[N].
                        If some (all) variables are unbounded, you may  specify  a
                        very large number or +INF, with the  latter  option  being
                        recommended.

        NOTE 1:  it is possible to specify  BndL[i]=BndU[i].  In  this  case  I-th
                 variable will be "frozen" at X[i]=BndL[i]=BndU[i].

          -- ALGLIB --
             Copyright 01.01.2025 by Bochkanov Sergey
        *************************************************************************/
        public static void minlpsolversetbc(minlpsolverstate state,
            double[] bndl,
            double[] bndu,
            alglib.xparams _params)
        {
            int i = 0;
            int n = 0;

            n = state.n;
            alglib.ap.assert(alglib.ap.len(bndl)>=n, "MINLPSolverSetBC: Length(BndL)<N");
            alglib.ap.assert(alglib.ap.len(bndu)>=n, "MINLPSolverSetBC: Length(BndU)<N");
            for(i=0; i<=n-1; i++)
            {
                alglib.ap.assert(math.isfinite(bndl[i]) || Double.IsNegativeInfinity(bndl[i]), "MINLPSolverSetBC: BndL contains NAN or +INF");
                alglib.ap.assert(math.isfinite(bndu[i]) || Double.IsPositiveInfinity(bndu[i]), "MINLPSolverSetBC: BndL contains NAN or -INF");
                state.bndl[i] = bndl[i];
                state.bndu[i] = bndu[i];
            }
        }


        /*************************************************************************
        This function sets two-sided linear constraints AL <= A*x <= AU with dense
        constraint matrix A.

        INPUT PARAMETERS:
            State   -   structure previously allocated with minlpsolvercreate() call.
            A       -   linear constraints, array[K,N]. Each row of  A  represents
                        one  constraint. One-sided  inequality   constraints, two-
                        sided inequality  constraints,  equality  constraints  are
                        supported (see below)
            AL, AU  -   lower and upper bounds, array[K];
                        * AL[i]=AU[i] => equality constraint Ai*x
                        * AL[i]<AU[i] => two-sided constraint AL[i]<=Ai*x<=AU[i]
                        * AL[i]=-INF  => one-sided constraint Ai*x<=AU[i]
                        * AU[i]=+INF  => one-sided constraint AL[i]<=Ai*x
                        * AL[i]=-INF, AU[i]=+INF => constraint is ignored
            K       -   number of equality/inequality constraints,  K>=0;  if  not
                        given, inferred from sizes of A, AL, AU.

          -- ALGLIB --
             Copyright 15.04.2024 by Bochkanov Sergey
        *************************************************************************/
        public static void minlpsolversetlc2dense(minlpsolverstate state,
            double[,] a,
            double[] al,
            double[] au,
            int k,
            alglib.xparams _params)
        {
            optserv.xlcsetlc2mixed(state.xlc, state.tmps1, 0, a, k, al, au, _params);
        }


        /*************************************************************************
        This  function  sets  two-sided linear  constraints  AL <= A*x <= AU  with
        a sparse constraining matrix A. Recommended for large-scale problems.

        This  function  overwrites  linear  (non-box)  constraints set by previous
        calls (if such calls were made).

        INPUT PARAMETERS:
            State   -   structure previously allocated with minlpsolvercreate() call.
            A       -   sparse matrix with size [K,N] (exactly!).
                        Each row of A represents one general linear constraint.
                        A can be stored in any sparse storage format.
            AL, AU  -   lower and upper bounds, array[K];
                        * AL[i]=AU[i] => equality constraint Ai*x
                        * AL[i]<AU[i] => two-sided constraint AL[i]<=Ai*x<=AU[i]
                        * AL[i]=-INF  => one-sided constraint Ai*x<=AU[i]
                        * AU[i]=+INF  => one-sided constraint AL[i]<=Ai*x
                        * AL[i]=-INF, AU[i]=+INF => constraint is ignored
            K       -   number  of equality/inequality constraints, K>=0.  If  K=0
                        is specified, A, AL, AU are ignored.

          -- ALGLIB --
             Copyright 15.04.2024 by Bochkanov Sergey
        *************************************************************************/
        public static void minlpsolversetlc2(minlpsolverstate state,
            sparse.sparsematrix a,
            double[] al,
            double[] au,
            int k,
            alglib.xparams _params)
        {
            optserv.xlcsetlc2mixed(state.xlc, a, k, state.tmpj1, 0, al, au, _params);
        }


        /*************************************************************************
        This  function  sets  two-sided linear  constraints  AL <= A*x <= AU  with
        a mixed constraining matrix A including a sparse part (first SparseK rows)
        and a dense part (last DenseK rows). Recommended for large-scale problems.

        This  function  overwrites  linear  (non-box)  constraints set by previous
        calls (if such calls were made).

        This function may be useful if constraint matrix includes large number  of
        both types of rows - dense and sparse. If you have just a few sparse rows,
        you  may  represent  them  in  dense  format  without losing  performance.
        Similarly, if you have just a few dense rows, you may store them in sparse
        format with almost same performance.

        INPUT PARAMETERS:
            State   -   structure previously allocated with minlpsolvercreate() call.
            SparseA -   sparse matrix with size [K,N] (exactly!).
                        Each row of A represents one general linear constraint.
                        A can be stored in any sparse storage format.
            SparseK -   number of sparse constraints, SparseK>=0
            DenseA  -   linear constraints, array[K,N], set of dense constraints.
                        Each row of A represents one general linear constraint.
            DenseK  -   number of dense constraints, DenseK>=0
            AL, AU  -   lower and upper bounds, array[SparseK+DenseK], with former
                        SparseK elements corresponding to sparse constraints,  and
                        latter DenseK elements corresponding to dense constraints;
                        * AL[i]=AU[i] => equality constraint Ai*x
                        * AL[i]<AU[i] => two-sided constraint AL[i]<=Ai*x<=AU[i]
                        * AL[i]=-INF  => one-sided constraint Ai*x<=AU[i]
                        * AU[i]=+INF  => one-sided constraint AL[i]<=Ai*x
                        * AL[i]=-INF, AU[i]=+INF => constraint is ignored
            K       -   number  of equality/inequality constraints, K>=0.  If  K=0
                        is specified, A, AL, AU are ignored.

          -- ALGLIB --
             Copyright 15.04.2024 by Bochkanov Sergey
        *************************************************************************/
        public static void minlpsolversetlc2mixed(minlpsolverstate state,
            sparse.sparsematrix sparsea,
            int ksparse,
            double[,] densea,
            int kdense,
            double[] al,
            double[] au,
            alglib.xparams _params)
        {
            optserv.xlcsetlc2mixed(state.xlc, sparsea, ksparse, densea, kdense, al, au, _params);
        }


        /*************************************************************************
        This function appends a two-sided linear constraint AL <= A*x <= AU to the
        matrix of dense constraints.

        INPUT PARAMETERS:
            State   -   structure previously allocated with minlpsolvercreate() call.
            A       -   linear constraint coefficient, array[N], right side is NOT
                        included.
            AL, AU  -   lower and upper bounds;
                        * AL=AU    => equality constraint Ai*x
                        * AL<AU    => two-sided constraint AL<=A*x<=AU
                        * AL=-INF  => one-sided constraint Ai*x<=AU
                        * AU=+INF  => one-sided constraint AL<=Ai*x
                        * AL=-INF, AU=+INF => constraint is ignored

          -- ALGLIB --
             Copyright 15.04.2024 by Bochkanov Sergey
        *************************************************************************/
        public static void minlpsolveraddlc2dense(minlpsolverstate state,
            double[] a,
            double al,
            double au,
            alglib.xparams _params)
        {
            optserv.xlcaddlc2dense(state.xlc, a, al, au, _params);
        }


        /*************************************************************************
        This function appends two-sided linear constraint  AL <= A*x <= AU  to the
        list of currently present sparse constraints.

        Constraint is passed in compressed format: as list of non-zero entries  of
        coefficient vector A. Such approach is more efficient than  dense  storage
        for highly sparse constraint vectors.

        INPUT PARAMETERS:
            State   -   structure previously allocated with minlpsolvercreate() call.
            IdxA    -   array[NNZ], indexes of non-zero elements of A:
                        * can be unsorted
                        * can include duplicate indexes (corresponding entries  of
                          ValA[] will be summed)
            ValA    -   array[NNZ], values of non-zero elements of A
            NNZ     -   number of non-zero coefficients in A
            AL, AU  -   lower and upper bounds;
                        * AL=AU    => equality constraint A*x
                        * AL<AU    => two-sided constraint AL<=A*x<=AU
                        * AL=-INF  => one-sided constraint A*x<=AU
                        * AU=+INF  => one-sided constraint AL<=A*x
                        * AL=-INF, AU=+INF => constraint is ignored

          -- ALGLIB --
             Copyright 19.07.2018 by Bochkanov Sergey
        *************************************************************************/
        public static void minlpsolveraddlc2(minlpsolverstate state,
            int[] idxa,
            double[] vala,
            int nnz,
            double al,
            double au,
            alglib.xparams _params)
        {
            optserv.xlcaddlc2(state.xlc, idxa, vala, nnz, al, au, _params);
        }


        /*************************************************************************
        This function appends two-sided linear constraint  AL <= A*x <= AU  to the
        list of currently present sparse constraints.

        Constraint vector A is  passed  as  a  dense  array  which  is  internally
        sparsified by this function.

        INPUT PARAMETERS:
            State   -   structure previously allocated with minlpsolvercreate() call.
            DA      -   array[N], constraint vector
            AL, AU  -   lower and upper bounds;
                        * AL=AU    => equality constraint A*x
                        * AL<AU    => two-sided constraint AL<=A*x<=AU
                        * AL=-INF  => one-sided constraint A*x<=AU
                        * AU=+INF  => one-sided constraint AL<=A*x
                        * AL=-INF, AU=+INF => constraint is ignored

          -- ALGLIB --
             Copyright 19.07.2018 by Bochkanov Sergey
        *************************************************************************/
        public static void minlpsolveraddlc2sparsefromdense(minlpsolverstate state,
            double[] da,
            double al,
            double au,
            alglib.xparams _params)
        {
            optserv.xlcaddlc2sparsefromdense(state.xlc, da, al, au, _params);
        }


        /*************************************************************************
        This function sets two-sided nonlinear constraints for MINLP optimizer.

        In fact, this function sets  only  constraints  COUNT  and  their  BOUNDS.
        Constraints  themselves  (constraint  functions)   are   passed   to   the
        MINLPSolverOptimize() method as callbacks.

        MINLPSolverOptimize() method accepts a user-defined vector function F[] and its
        Jacobian J[], where:
        * first element of F[] and first row of J[] correspond to the target
        * subsequent NNLC components of F[] (and rows of J[]) correspond  to  two-
          sided nonlinear constraints NL<=C(x)<=NU, where
          * NL[i]=NU[i] => I-th row is an equality constraint Ci(x)=NL
          * NL[i]<NU[i] => I-th tow is a  two-sided constraint NL[i]<=Ci(x)<=NU[i]
          * NL[i]=-INF  => I-th row is an one-sided constraint Ci(x)<=NU[i]
          * NU[i]=+INF  => I-th row is an one-sided constraint NL[i]<=Ci(x)
          * NL[i]=-INF, NU[i]=+INF => constraint is ignored

        NOTE: you may combine nonlinear constraints with linear/boundary ones.  If
              your problem has mixed constraints, you  may explicitly specify some
              of them as linear or box ones.
              It helps optimizer to handle them more efficiently.

        INPUT PARAMETERS:
            State   -   structure previously allocated with MINLPSolverCreate call.
            NL      -   array[NNLC], lower bounds, can contain -INF
            NU      -   array[NNLC], lower bounds, can contain +INF
            NNLC    -   constraints count, NNLC>=0

        NOTE 1: nonlinear constraints are satisfied only  approximately!   It   is
                possible that the algorithm will evaluate the function  outside of
                the feasible area!
                
        NOTE 2: algorithm scales variables  according  to the scale  specified by
                MINLPSolverSetScale()  function,  so it can handle problems with badly
                scaled variables (as long as we KNOW their scales).
                   
                However,  there  is  no  way  to  automatically  scale   nonlinear
                constraints. Inappropriate scaling  of nonlinear  constraints  may
                ruin convergence. Solving problem with  constraint  "1000*G0(x)=0"
                is NOT the same as solving it with constraint "0.001*G0(x)=0".
                   
                It means that YOU are  the  one who is responsible for the correct
                scaling of the nonlinear constraints Gi(x) and Hi(x). We recommend
                you to scale nonlinear constraints in such a way that the Jacobian
                rows have approximately unit magnitude  (for  problems  with  unit
                scale) or have magnitude approximately equal to 1/S[i] (where S is
                a scale set by MINLPSolverSetScale() function).

          -- ALGLIB --
             Copyright 05.01.2025 by Bochkanov Sergey
        *************************************************************************/
        public static void minlpsolversetnlc2(minlpsolverstate state,
            double[] nl,
            double[] nu,
            int nnlc,
            alglib.xparams _params)
        {
            int i = 0;

            alglib.ap.assert(nnlc>=0, "MINLPSolverSetNLC2: NNLC<0");
            alglib.ap.assert(alglib.ap.len(nl)>=nnlc, "MINLPSolverSetNLC2: Length(NL)<NNLC");
            alglib.ap.assert(alglib.ap.len(nu)>=nnlc, "MINLPSolverSetNLC2: Length(NU)<NNLC");
            state.nnlc = nnlc;
            ablasf.bsetallocv(nnlc, false, ref state.hasnlcmask, _params);
            sparse.sparsecreatecrsemptybuf(state.n, state.nlcmask, _params);
            ablasf.rallocv(nnlc, ref state.nl, _params);
            ablasf.rallocv(nnlc, ref state.nu, _params);
            for(i=0; i<=nnlc-1; i++)
            {
                alglib.ap.assert(math.isfinite(nl[i]) || Double.IsNegativeInfinity(nl[i]), "MINLPSolverSetNLC2: NL[i] is +INF or NAN");
                alglib.ap.assert(math.isfinite(nu[i]) || Double.IsPositiveInfinity(nu[i]), "MINLPSolverSetNLC2: NU[i] is -INF or NAN");
                state.nl[i] = nl[i];
                state.nu[i] = nu[i];
                sparse.sparseappendemptyrow(state.nlcmask, _params);
            }
        }


        /*************************************************************************
        This function APPENDS a two-sided nonlinear constraint to the list.

        In fact, this function adds constraint bounds.  A  constraints  itself  (a
        function) is passed to the MINLPSolverOptimize() method as a callback. See
        comments on  MINLPSolverSetNLC2()  for  more  information  about  callback
        structure.

        The function adds a two-sided nonlinear constraint NL<=C(x)<=NU, where
        * NL=NU => I-th row is an equality constraint Ci(x)=NL
        * NL<NU => I-th tow is a  two-sided constraint NL<=Ci(x)<=NU
        * NL=-INF  => I-th row is an one-sided constraint Ci(x)<=NU
        * NU=+INF  => I-th row is an one-sided constraint NL<=Ci(x)
        * NL=-INF, NU=+INF => constraint is ignored

        NOTE: you may combine nonlinear constraints with linear/boundary ones.  If
              your problem has mixed constraints, you  may explicitly specify some
              of them as linear or box ones. It helps the optimizer to handle them
              more efficiently.

        INPUT PARAMETERS:
            State   -   structure previously allocated with MINLPSolverCreate call.
            NL      -   lower bound, can be -INF
            NU      -   upper bound, can be +INF

        NOTE 1: nonlinear constraints are satisfied only  approximately!   It   is
                possible that the algorithm will evaluate the function  outside of
                the feasible area!
                
        NOTE 2: algorithm scales variables  according  to the scale  specified by
                MINLPSolverSetScale()  function,  so it can handle problems with badly
                scaled variables (as long as we KNOW their scales).
                   
                However,  there  is  no  way  to  automatically  scale   nonlinear
                constraints. Inappropriate scaling  of nonlinear  constraints  may
                ruin convergence. Solving problem with  constraint  "1000*G0(x)=0"
                is NOT the same as solving it with constraint "0.001*G0(x)=0".
                   
                It means that YOU are  the  one who is responsible for the correct
                scaling of the nonlinear constraints Gi(x) and Hi(x). We recommend
                you to scale nonlinear constraints in such a way that the Jacobian
                rows have approximately unit magnitude  (for  problems  with  unit
                scale) or have magnitude approximately equal to 1/S[i] (where S is
                a scale set by MINLPSolverSetScale() function).
                
        NOTE 3: use addnlc2masked() in order to specify variable  mask.  Masks are
                essential  for  derivative-free  optimization because they provide
                important information about relevant and irrelevant variables.

          -- ALGLIB --
             Copyright 05.01.2025 by Bochkanov Sergey
        *************************************************************************/
        public static void minlpsolveraddnlc2(minlpsolverstate state,
            double nl,
            double nu,
            alglib.xparams _params)
        {
            alglib.ap.assert(math.isfinite(nl) || Double.IsNegativeInfinity(nl), "MINLPSolverAddNLC2: NL is +INF or NAN");
            alglib.ap.assert(math.isfinite(nu) || Double.IsPositiveInfinity(nu), "MINLPSolverAddNLC2: NU is -INF or NAN");
            ablasf.rgrowappendv(state.nnlc+1, ref state.nl, nl, _params);
            ablasf.rgrowappendv(state.nnlc+1, ref state.nu, nu, _params);
            ablasf.bgrowappendv(state.nnlc+1, ref state.hasnlcmask, false, _params);
            sparse.sparseappendemptyrow(state.nlcmask, _params);
            state.nnlc = state.nnlc+1;
        }


        /*************************************************************************
        This function APPENDS a two-sided nonlinear constraint to the  list,  with
        the  variable   mask  being  specified  as  a  compressed  index  array. A
        variable mask is a set of variables actually appearing in the constraint.

        ----- ABOUT VARIABLE MASKS -----------------------------------------------

        Variable masks provide crucial information  for  derivative-free  solvers,
        greatly accelerating surrogate model construction. This  applies  to  both 
        continuous and integral variables, with results for binary variables being
        more pronounced.

        Up to 2x improvement in convergence speed has been observed for sufficiently
        sparse MINLP problems.

        NOTE: In order to unleash the full potential of variable  masking,  it  is
              important to provide masks for objective as well  as  all  nonlinear
              constraints.
              
              Even partial  information  matters,  i.e.  if you are 100% sure that
              your black-box  function  does  not  depend  on  some variables, but
              unsure about other ones, mark surely irrelevant variables, and  tell
              the solver that other ones may be relevant.
              
        NOTE: the solver is may behave unpredictably  if  some  relevant  variable
              is not included into the mask. Most likely it will fail to converge,
              although it sometimes possible to converge  to  solution  even  with
              incorrectly specified mask.

        NOTE: minlpsolversetobjectivemask() can be used to set  variable  mask for
              the objective.

        NOTE: Masks  are  ignored  by  branch-and-bound-type  solvers  relying  on
              analytic gradients.

        ----- ABOUT NONLINEAR CONSTRAINTS ----------------------------------------

        In fact, this function adds constraint bounds.  A  constraint   itself  (a
        function) is passed to the MINLPSolverOptimize() method as a callback. See
        comments on  MINLPSolverSetNLC2()  for  more  information  about  callback
        structure.

        The function adds a two-sided nonlinear constraint NL<=C(x)<=NU, where
        * NL=NU => I-th row is an equality constraint Ci(x)=NL
        * NL<NU => I-th tow is a  two-sided constraint NL<=Ci(x)<=NU
        * NL=-INF  => I-th row is an one-sided constraint Ci(x)<=NU
        * NU=+INF  => I-th row is an one-sided constraint NL<=Ci(x)
        * NL=-INF, NU=+INF => constraint is ignored

        NOTE: you may combine nonlinear constraints with linear/boundary ones.  If
              your problem has mixed constraints, you  may explicitly specify some
              of them as linear or box ones. It helps the optimizer to handle them
              more efficiently.

        INPUT PARAMETERS:
            State   -   structure previously allocated with MINLPSolverCreate call.
            NL      -   lower bound, can be -INF
            NU      -   upper bound, can be +INF
            VarIdx  -   array[NMSK], with potentially  unsorted  and  non-distinct
                        indexes (the function will sort and merge duplicates).  If
                        a variable index K appears in the list, it  means that the
                        constraint potentially depends  on  K-th  variable.  If  a
                        variable index K does NOT appear in  the  list,  it  means
                        that the constraint does NOT depend on K-th variable.
                        The array can have more than NMSK elements, in which  case
                        only leading NMSK will be used.
            NMSK    -   NMSK>=0, VarIdx[] size:
                        * NMSK>0 means that the constraint depends on up  to  NMSK
                          variables whose indexes are stored in VarIdx[]
                        * NMSK=0 means that the constraint is a constant function;
                          the solver may fail if it is not actually the case.

        NOTE 1: nonlinear constraints are satisfied only  approximately!   It   is
                possible that the algorithm will evaluate the function  outside of
                the feasible area!
                
        NOTE 2: algorithm scales variables  according  to the scale  specified by
                MINLPSolverSetScale()  function,  so it can handle problems with badly
                scaled variables (as long as we KNOW their scales).
                   
                However,  there  is  no  way  to  automatically  scale   nonlinear
                constraints. Inappropriate scaling  of nonlinear  constraints  may
                ruin convergence. Solving problem with  constraint  "1000*G0(x)=0"
                is NOT the same as solving it with constraint "0.001*G0(x)=0".
                   
                It means that YOU are  the  one who is responsible for the correct
                scaling of the nonlinear constraints Gi(x) and Hi(x). We recommend
                you to scale nonlinear constraints in such a way that the Jacobian
                rows have approximately unit magnitude  (for  problems  with  unit
                scale) or have magnitude approximately equal to 1/S[i] (where S is
                a scale set by MINLPSolverSetScale() function).

          -- ALGLIB --
             Copyright 05.01.2025 by Bochkanov Sergey
        *************************************************************************/
        public static void minlpsolveraddnlc2masked(minlpsolverstate state,
            double nl,
            double nu,
            int[] varidx,
            int nmsk,
            alglib.xparams _params)
        {
            alglib.ap.assert(math.isfinite(nl) || Double.IsNegativeInfinity(nl), "MINLPSolverAddNLC2Masked: NL is +INF or NAN");
            alglib.ap.assert(math.isfinite(nu) || Double.IsPositiveInfinity(nu), "MINLPSolverAddNLC2Masked: NU is -INF or NAN");
            alglib.ap.assert(nmsk>=0, "MINLPSolverAddNLC2Masked: NMSK<0");
            alglib.ap.assert(alglib.ap.len(varidx)>=nmsk, "MINLPSolverAddNLC2Masked: len(VarIdx)<NMSK");
            ablasf.rgrowappendv(state.nnlc+1, ref state.nl, nl, _params);
            ablasf.rgrowappendv(state.nnlc+1, ref state.nu, nu, _params);
            ablasf.bgrowappendv(state.nnlc+1, ref state.hasnlcmask, true, _params);
            ablasf.rsetallocv(nmsk, 1.0, ref state.rdummy, _params);
            sparse.sparseappendcompressedrow(state.nlcmask, varidx, state.rdummy, nmsk, _params);
            state.nnlc = state.nnlc+1;
        }


        /*************************************************************************
        This function sets stopping condition for the branch-and-bound  family  of
        solvers: a solver must when when the gap between primal and dual bounds is
        less than PDGap.

        The solver computes relative gap, equal to |Fprim-Fdual|/max(|Fprim|,1).

        This parameter is ignored by other types of solvers, e.g. MIVNS.

        INPUT PARAMETERS:
            State   -   structure stores algorithm state
            PDGap   -   >=0, tolerance. Zero value means that some default value
                        is automatically selected.

          -- ALGLIB --
             Copyright 01.01.2025 by Bochkanov Sergey
        *************************************************************************/
        public static void minlpsolversetpdgap(minlpsolverstate state,
            double pdgap,
            alglib.xparams _params)
        {
            alglib.ap.assert(math.isfinite(pdgap), "MINLPSolverSetPDGap: PDGap is not finite");
            alglib.ap.assert((double)(pdgap)>=(double)(0), "MINLPSolverSetPDGap: PDGap<0");
            state.pdgap = pdgap;
        }


        /*************************************************************************
        This function sets tolerance for nonlinear constraints;  points  violating
        constraints by no more than CTol are considered feasible.

        Depending on the specific algorithm  used,  constraint  violation  may  be
        checked against  internally  scaled/normalized  constraints  (some  smooth
        solvers renormalize constraints in such a way that they have roughly  unit
        gradient magnitudes) or against raw constraint values:
        * BBSYNC renormalizes constraints prior to comparing them with CTol
        * MIRBF-VNS checks violation against raw constraint values

        IMPORTANT: one  should  be  careful  when choosing tolerances and stopping
                   criteria.
                   
                   A solver stops  as  soon  as  stopping  criteria are triggered;
                   a feasibility check is  performed  after  that.  If  too  loose
                   stopping criteria are  used, the solver  may  fail  to  enforce
                   constraints  with  sufficient  accuracy  and  fail to recognize
                   solution as a feasible one.
                   
                   For example, stopping with EpsX=0.01 and checking CTol=0.000001
                   will almost surely result in problems. Ideally, CTol should  be
                   1-2 orders of magnitude more relaxed than stopping criteria.

        INPUT PARAMETERS:
            State   -   structure stores algorithm state
            CTol    -   >0, tolerance.

          -- ALGLIB --
             Copyright 01.01.2025 by Bochkanov Sergey
        *************************************************************************/
        public static void minlpsolversetctol(minlpsolverstate state,
            double ctol,
            alglib.xparams _params)
        {
            alglib.ap.assert(math.isfinite(ctol), "MINLPSolverSetCTol: CTol is not finite");
            alglib.ap.assert((double)(ctol)>(double)(0), "MINLPSolverSetCTol: CTol<=0");
            state.ctol = ctol;
        }


        /*************************************************************************
        This  function  tells  MINLP solver  to  use  an  objective-based stopping
        condition for an underlying subsolver, i.e. to stop subsolver if  relative
        change in objective between iterations is less than EpsF.

        Too tight EspF, as always, result in spending too much time in the solver.
        Zero value means that some default non-zero value will be used.

        Exact action of this condition as well as reaction  to  too  relaxed  EpsF
        depend on specific MINLP solver being used

        * BBSYNC. This condition controls SQP subsolver used to solve NLP (relaxed)
          subproblems arising during B&B  tree  search. Good  values are typically
          between 1E-6 and 1E-7.
          
          Too relaxed values may result in subproblems being  mistakenly  fathomed
          (feasible solutions not identified), too  large  constraint  violations,
          etc.

        * MIVNS. This condition controls RBF-based surrogate model subsolver  used
          to handle continuous variables. It is ignored for integer-only problems.
          
          The subsolver stops if total objective change in last  several  (between
          5 and 10) steps is less than EpsF. More than one step is used  to  check
          convergence because surrogate  model-based  solvers  usually  need  more
          stringent stopping criteria than SQP.
          
          Good values are relatively high, between 0.01 and 0.0001,  depending  on
          a  problem.  The  MIVNS  solver  is  designed to gracefully handle large
          values of EpsF - it will stop early, but it won't compromise feasibility
          (it will try to reduce constraint violations below CTol)  and  will  not
          drop promising integral nodes.

        INPUT PARAMETERS:
            State   -   solver structure
            EpsF    -   >0, stopping condition

          -- ALGLIB --
             Copyright 01.01.2025 by Bochkanov Sergey
        *************************************************************************/
        public static void minlpsolversetsubsolverepsf(minlpsolverstate state,
            double epsf,
            alglib.xparams _params)
        {
            alglib.ap.assert(math.isfinite(epsf), "MINLPSolverSetSubsolverEpsF: EpsF is not finite");
            alglib.ap.assert((double)(epsf)>=(double)(0), "MINLPSolverSetSubsolverEpsF: EpsF<0");
            state.subsolverepsf = epsf;
        }


        /*************************************************************************
        This  function  tells  MINLP solver to use a step-based stopping condition
        for an underlying subsolver, i.e. to stop subsolver  if  typical step size
        becomes less than EpsX.

        Too tight EspX, as always, result in spending too much time in the solver.
        Zero value means that some default non-zero value will be used.

        Exact action of this condition as well as reaction  to  too  relaxed  EpsX
        depend on specific MINLP solver being used

        * BBSYNC. This condition controls SQP subsolver used to solve NLP (relaxed)
          subproblems arising during B&B  tree  search. Good  values are typically
          between 1E-6 and 1E-7.
          
          Too relaxed values may result in subproblems being  mistakenly  fathomed
          (feasible solutions not identified), too  large  constraint  violations,
          etc.

        * MIVNS. This condition controls RBF-based surrogate model subsolver  used
          to handle continuous variables. It is ignored for integer-only problems.
          
          The subsolver stops if trust radius  for  a  surrogate  model  optimizer
          becomes less than EpsX.
          
          Good values are relatively high, between 0.01 and 0.0001,  depending  on
          a  problem.  The  MIVNS  solver  is  designed to gracefully handle large
          values of EpsX - it will stop early, but it won't compromise feasibility
          (it will try to reduce constraint violations below CTol)  and  will  not
          drop promising integral nodes.

        INPUT PARAMETERS:
            State   -   solver structure
            EpsX    -   >=0, stopping condition. Zero value means that some default
                        value will be used.

          -- ALGLIB --
             Copyright 01.01.2025 by Bochkanov Sergey
        *************************************************************************/
        public static void minlpsolversetsubsolverepsx(minlpsolverstate state,
            double epsx,
            alglib.xparams _params)
        {
            alglib.ap.assert(math.isfinite(epsx), "MINLPSolverSetSubsolverEpsX: EpsX is not finite");
            alglib.ap.assert((double)(epsx)>=(double)(0), "MINLPSolverSetSubsolverEpsX: EpsX<0");
            state.subsolverepsx = epsx;
        }


        /*************************************************************************
        This function controls adaptive internal parallelism, i.e. algorithm  used
        by  the  solver  to  adaptively  decide  whether parallel acceleration  of
        solver's internal calculations (B&B  code,  SQP,  parallel linear algebra)
        should be actually used or not.

        This  function  tells  the  solver  to  favor  parallelism,  i.e.  utilize
        multithreading (when allowed by the  user)  until  statistics  prove  that
        overhead from starting/stopping worker threads is too large.

        This way solver gets the best performance  on  problems  with  significant
        amount  of  internal  calculations  (large  QP/MIQP  subproblems,  lengthy
        surrogate model optimization sessions) from the very beginning. The  price
        is that problems with small solver overhead that does not justify internal
        parallelism (<1ms per iteration) will suffer slowdown for several  initial
        10-20 milliseconds until the solver proves that parallelism makes no sense

        Use  MINLPSolver.CautiousInternalParallelism()  to  avoid slowing down the
        solver on easy problems.

        NOTE: the internal parallelism is distinct from the callback  parallelism.
              The former is the ability to utilize parallelism to speed-up solvers
              own internal calculations,  while  the  latter  is  the  ability  to
              perform several callback evaluations at once. Aside from performance
              considerations, the internal parallelism is entirely transparent  to
              the user. The callback parallelism requries  the  user  to  write  a
              thread-safe, reentrant callback.

        NOTE: in order to use internal parallelism, adaptive or not, the user must
              activate it by   specifying  alglib::parallel  in  flags  or  global
              threading settings. ALGLIB for C++ must be compiled in the  OS-aware
              mode.

          -- ALGLIB --
             Copyright 01.01.2025 by Bochkanov Sergey
        *************************************************************************/
        public static void minlpsolverfavorinternalparallelism(minlpsolverstate state,
            alglib.xparams _params)
        {
            state.adaptiveinternalparallelism = 1;
        }


        /*************************************************************************
        This function controls adaptive internal parallelism, i.e. algorithm  used
        by  the  solver  to  adaptively  decide  whether parallel acceleration  of
        solver's internal calculations (B&B  code,  SQP,  parallel linear algebra)
        should be actually used or not.

        This function tells the solver  to  do calculations in the single-threaded
        mode until statistics  prove  that  iteration  cost  justified  activating
        multithreading.

        This way solver does not suffer slow-down on problems with small iteration
        overhead (<1ms per iteration), at the cost of spending  initial  10-20  ms
        in the single-threaded  mode  even  on  difficult  problems  that  justify
        parallelism usage.

        Use  MINLPSolver.FavorInternalParallelism() to use parallelism until it is
        proven to be useless.

        NOTE: the internal parallelism is distinct from the callback  parallelism.
              The former is the ability to utilize parallelism to speed-up solvers
              own internal calculations,  while  the  latter  is  the  ability  to
              perform several callback evaluations at once. Aside from performance
              considerations, the internal parallelism is entirely transparent  to
              the user. The callback parallelism requries  the  user  to  write  a
              thread-safe, reentrant callback.

        NOTE: in order to use internal parallelism, adaptive or not, the user must
              activate it by   specifying  alglib::parallel  in  flags  or  global
              threading settings. ALGLIB for C++ must be compiled in the  OS-aware
              mode.

          -- ALGLIB --
             Copyright 01.01.2025 by Bochkanov Sergey
        *************************************************************************/
        public static void minlpsolvercautiousinternalparallelism(minlpsolverstate state,
            alglib.xparams _params)
        {
            state.adaptiveinternalparallelism = 0;
        }


        /*************************************************************************
        This function controls adaptive internal parallelism, i.e. algorithm  used
        by  the  solver  to  adaptively  decide  whether parallel acceleration  of
        solver's internal calculations (B&B  code,  SQP,  parallel linear algebra)
        should be actually used or not.

        This function tells the solver to do calculations exactly as prescribed by
        the user: in the parallel mode when alglib::parallel flag  is  passed,  in
        the single-threaded mode otherwise. The solver  does  not  analyze  actual
        running times to decide whether parallelism is justified or not.

        NOTE: the internal parallelism is distinct from the callback  parallelism.
              The former is the ability to utilize parallelism to speed-up solvers
              own internal calculations,  while  the  latter  is  the  ability  to
              perform several callback evaluations at once. Aside from performance
              considerations, the internal parallelism is entirely transparent  to
              the user. The callback parallelism requries  the  user  to  write  a
              thread-safe, reentrant callback.

        NOTE: in order to use internal parallelism, adaptive or not, the user must
              activate it by   specifying  alglib::parallel  in  flags  or  global
              threading settings. ALGLIB for C++ must be compiled in the  OS-aware
              mode.

          -- ALGLIB --
             Copyright 01.01.2025 by Bochkanov Sergey
        *************************************************************************/
        public static void minlpsolvernoadaptiveinternalparallelism(minlpsolverstate state,
            alglib.xparams _params)
        {
            state.adaptiveinternalparallelism = -1;
        }


        /*************************************************************************
        This function marks K-th variable as an integral one.

        Unless box constraints are set for the variable, it is unconstrained (i.e.
        can take positive or  negative  values).  By  default  all  variables  are
        continuous.

        IMPORTANT: box constraints work in parallel with the integrality ones:
                   * a variable marked as integral is considered  having no bounds
                     until minlpsolversetbc() is called
                   * a  variable  with  lower  and  upper bounds set is considered
                     continuous   until    marked    as    integral    with    the
                     minlpsolversetintkth() function.

        INPUT PARAMETERS:
            State   -   structure stores algorithm state
            K       -   0<=K<N, variable index

          -- ALGLIB --
             Copyright 01.01.2025 by Bochkanov Sergey
        *************************************************************************/
        public static void minlpsolversetintkth(minlpsolverstate state,
            int k,
            alglib.xparams _params)
        {
            alglib.ap.assert(k>=0 && k<state.n, "MINLPSolverSetIntKth: K is outside of [0,N)");
            state.isintegral[k] = true;
            state.isbinary[k] = false;
        }


        /*************************************************************************
        This function sets variable  mask for the objective.  A variable  mask  is
        a set of variables actually appearing in the objective.

        If you want  to  set  variable  mask  for  a  nonlinear  constraint,   use
        addnlc2masked() or addnlc2maskeddense() to add  a constraint together with
        a constraint-specific mask.

        Variable masks provide crucial information  for  derivative-free  solvers,
        greatly accelerating surrogate model construction. This  applies  to  both 
        continuous and integral variables, with results for binary variables being
        more pronounced.

        Up to 2x improvement in convergence speed has been observed for sufficiently
        sparse MINLP problems.

        NOTE: In order to unleash the full potential of variable  masking,  it  is
              important to provide masks for objective as well  as  all  nonlinear
              constraints.
              
              Even partial  information  matters,  i.e.  if you are 100% sure that
              your black-box  function  does  not  depend  on  some variables, but
              unsure about other ones, mark surely irrelevant variables, and  tell
              the solver that other ones may be relevant.
              
        NOTE: the solver is may behave unpredictably  if  some  relevant  variable
              is not included into the mask. Most likely it will fail to converge,
              although it sometimes possible to converge  to  solution  even  with
              incorrectly specified mask.

        NOTE: Masks  are  ignored  by  branch-and-bound-type  solvers  relying  on
              analytic gradients.

        INPUT PARAMETERS:
            State   -   structure stores algorithm state
            ObjMask -   array[N],  I-th  element  is  False  if  I-th variable  is
                        irrelevant.

          -- ALGLIB --
             Copyright 01.01.2025 by Bochkanov Sergey
        *************************************************************************/
        public static void minlpsolversetobjectivemaskdense(minlpsolverstate state,
            bool[] objmask,
            alglib.xparams _params)
        {
            int i = 0;

            objmask = (bool[])objmask.Clone();

            alglib.ap.assert(alglib.ap.len(objmask)>=state.n, "MINLPSolverSetObjectiveMaskDense: len(ObjMask)<N");
            state.hasobjmask = false;
            for(i=0; i<=state.n-1; i++)
            {
                state.hasobjmask = state.hasobjmask || !objmask[i];
            }
            ablasf.bcopyallocv(state.n, objmask, ref state.objmask, _params);
        }


        /*************************************************************************
        This function sets scaling coefficients for the mixed integer optimizer.

        ALGLIB optimizers use scaling matrices to test stopping  conditions  (step
        size and gradient are scaled before comparison  with  tolerances).  Scales
        are also used by the finite difference variant of the optimizer - the step
        along I-th axis is equal to DiffStep*S[I]. Finally,  variable  scales  are
        used for preconditioning (i.e. to speed up the solver).

        The scale of the I-th variable is a translation invariant measure of:
        a) "how large" the variable is
        b) how large the step should be to make significant changes in the function

        INPUT PARAMETERS:
            State   -   structure stores algorithm state
            S       -   array[N], non-zero scaling coefficients
                        S[i] may be negative, sign doesn't matter.

          -- ALGLIB --
             Copyright 06.01.2025 by Bochkanov Sergey
        *************************************************************************/
        public static void minlpsolversetscale(minlpsolverstate state,
            double[] s,
            alglib.xparams _params)
        {
            int i = 0;

            alglib.ap.assert(alglib.ap.len(s)>=state.n, "MINLPSolver: Length(S)<N");
            for(i=0; i<=state.n-1; i++)
            {
                alglib.ap.assert(math.isfinite(s[i]), "MINLPSolver: S contains infinite or NAN elements");
                alglib.ap.assert((double)(s[i])!=(double)(0), "MINLPSolver: S contains zero elements");
                state.s[i] = Math.Abs(s[i]);
            }
        }


        /*************************************************************************
        This function tell the solver to use BBSYNC (Branch&Bound with Synchronous
        processing) mixed-integer nonlinear programming algorithm.

        The BBSYNC algorithm is an NLP-based branch-and-bound method with integral
        and spatial splits, supporting both convex  and  nonconvex  problems.  The
        algorithm combines parallelism support with deterministic  behavior  (i.e.
        the same branching decisions are performed with every paralell run).

        Non-convex (multiextremal) problems can be solved with  multiple  restarts
        from random points, which are activated by minlpsolversetmultistarts()

        IMPORTANT: contrary to the popular  misconception,  MINLP  is  not  easily
                   parallelizable. B&B trees often have  profiles  unsuitable  for
                   parallel processing (too short and/or too linear).  Spatial  or
                   integral splits adds some limited degree of parallelism (up  to
                   2x in the very best case), but in practice it often results  in
                   just a 1.5x speed-up at best  due  imbalanced  leaf  processing
                   times.  Furthermore ,  determinism  is  always  at   odds  with
                   efficiency.
                   
                   Achieving good parallel speed-up requires some amount of tuning
                   and having a 2x-3x speed-up is already a good result.
                   
                   On the other hand, setups using multiple  random  restarts  are
                   obviously highly parallelizable.

        INPUT PARAMETERS:
            State           -   structure that stores algorithm state
            
            GroupSize       -   >=1, group size. Up to GroupSize tree nodes can be
                                processed in the parallel manner.
                                
                                Increasing  this   parameter   makes   the  solver
                                less efficient serially (it always tries  to  fill
                                the batch with nodes, even if there  is  a  chance
                                that most of them will be  discarded  later),  but
                                increases its parallel potential.
                                
                                Parallel speed-up comes from two sources:
                                * callback parallelism (several  objective  values
                                  are computed concurrently), which is significant
                                  for problems with callbacks that take  more than
                                  1ms per evaluation
                                * internal parallelism, i.e. ability to do parallel
                                  sparse matrix factorization  and  other  solver-
                                  related tasks
                                By  default,  the  solver  runs  serially even for
                                GroupSize>1. Both kinds of parallelism have to  be
                                activated by the user, see ALGLIB Reference Manual
                                for more information.
                                
                                Recommended value, depending on callback cost  and
                                matrix factorization overhead, can be:
                                * 1 for 'easy' problems with cheap  callbacks  and
                                  small dimensions; also for problems with  nearly
                                  linear B&B trees.
                                * 2-3  for   problems   with  sufficiently  costly
                                  callbacks (or sufficiently high  linear  algebra
                                  overhead) that it makes sense to utilize limited
                                  parallelism.
                                * cores count - for difficult problems  with  deep
                                  and  wide   B&B trees  and  sufficiently  costly
                                  callbacks (or sufficiently high  linear  algebra
                                  overhead).

        NOTES: DETERMINISM

        Running with fixed GroupSize generally produces same results independently
        of whether parallelism is used or not. Changing  GroupSize  parameter  may
        change results in the following ways:

        * for problems that are solved to optimality  but have multiple solutions,
          different values of this parameter may  result  in  different  solutions
          being returned (but still with the same objective value)
          
        * while operating close to exhausting budget (either timeout or iterations
          limit), different GroupSize values may result in different  outcomes:  a
          solution being found, or budget being exhausted
          
        * finally, on difficult problems that are too hard to solve to  optimality
          but still allow finding primal feasible solutions changing GroupSize may
          result in different primal feasible solutions being returned.

          -- ALGLIB --
             Copyright 01.01.2025 by Bochkanov Sergey
        *************************************************************************/
        public static void minlpsolversetalgobbsync(minlpsolverstate state,
            int groupsize,
            alglib.xparams _params)
        {
            alglib.ap.assert(groupsize>=1, "MINLPSolverSetAlgoBBSYNC: GroupSize<1");
            state.algoidx = 0;
            state.bbgdgroupsize = groupsize;
        }


        /*************************************************************************
        This function  tell  the  solver  to  use  MIVNS  (Mixed-Integer  Variable
        Neighborhood Search) solver for  derivative-free  mixed-integer  nonlinear
        programming with expensive objective/constraints and non-relaxable integer
        variables.

        The solver is intended for moderately-sized problems, typically with  tens
        of variables.

        The algorithm has the following features:
        * it supports all-integer and mixed-integer problems with box, linear  and
          nonlinear equality and inequality  constraints
        * it makes no assumptions about problem convexity
        * it does not require derivative information. Although  it  still  assumes
          that objective/constraints are smooth wrt continuous variables, no  such
          assumptions are made regarding dependence on integer variables.
        * it efficiently uses limited computational budget and  scales  well  with
          larger budgets
        * it does not evaluate objective/constraints at points violating integrality
        * it also respects linear constraints in all intermediate points

        NOTE: In  particular,  if  your  task  uses integrality+sum-to-one set  of
              constraints to encode multiple choice options (e.g. [1,0,0], [0,1,0]
              or [0,0,1]), you can be sure that the algorithm will not ask for  an
              objective value at a point with fractional values like [0.1,0.5,0.4]
              or at one that is not a correct one-hot encoded value (e.g.  [1,1,0]
              which has two variables set to 1).

        The algorithm is intended for low-to-medium accuracy solution of otherwise
        intractable problems with expensive objective/constraints.

        It can solve any MINLP problem; however, it is optimized for the following
        problem classes:
        * limited variable count
        * expensive objective/constraints
        * nonrelaxable integer variables
        * no derivative information
        * problems where changes in integer variables lead to  structural  changes
          in the entire system. Speaking in other words, on  problems  where  each
          integer variable acts as an on/off or "choice"  switch  that  completely
          rewires the model - turning constraints, variables, or whole sub-systems
          on or off

        INPUT PARAMETERS:
            State           -   structure that stores algorithm state
            
            Budget          -   optimization  budget (function  evaluations).  The
                                solver will not stop  immediately  after  reaching
                                Budget evaluations, but  will  stop  shortly after
                                that (usually within 2N+1 evaluations). Zero value
                                means no limit.
                                
            MaxNeighborhood -   stopping condition for the solver.  The  algorithm
                                will stop as soon as there are  no  points  better
                                than the current candidate in a neighborhood whose
                                size is equal to or exceeds MaxNeighborhood.  Zero
                                means no stopping condition.
                                
                                Recommended neighborhood size is  proportional  to
                                the difference between integral variables count NI
                                and the number of linear equality  constraints  on
                                integral variables L (such constraints effectively
                                reduce problem dimensionality).
                                
                                The very minimal value for binary problems is NI-L,
                                which means that the solution can not be  improved
                                by flipping one of variables between 0 and 1.  The
                                very minimal value for non-binary integral vars is
                                twice as much (because  now  each  point  has  two
                                neighbors per  variable).  However,  such  minimal
                                values often result in an early termination.
                                
                                It is recommended to set this parameter to 5*N  or
                                10*N (ignoring LI) and to test how it  behaves  on
                                your problem.
                                
            BatchSize           >=1,   recommended  batch  size  for  neighborhood
                                exploration.   Up   to  BatchSize  nodes  will  be
                                evaluated at any  moment,  thus  up  to  BatchSize
                                objective evaluations can be performed in parallel.
                                
                                Increasing  this   parameter   makes   the  solver
                                slightly less efficient serially (it always  tries
                                to fill the batch with nodes, even if there  is  a
                                chance that most of them will be discarded later),
                                but greatly increases its parallel potential.
                                
                                Recommended values depend on the cores  count  and
                                on the limitations  of  the  objective/constraints
                                callback:
                                * 1 for serial execution, callback that can not be
                                  called  from   multiple   threads,   or   highly
                                  parallelized  expensive  callback that keeps all
                                  cores occupied
                                * small fixed value like 5  or  10,  if  you  need
                                  reproducible behavior independent from the cores
                                  count
                                * CORESCOUNT, 2*CORESCOUNT or some other  multiple
                                  of CORESCOUNT, if you want to utilize parallelism
                                  to the maximum extent
                                
                                Parallel speed-up comes from two sources:
                                * callback parallelism (several  objective  values
                                  are computed concurrently), which is significant
                                  for problems with callbacks that take  more than
                                  1ms per evaluation
                                * internal parallelism, i.e. ability to do parallel
                                  sparse matrix factorization  and  other  solver-
                                  related tasks
                                By  default,  the  solver  runs  serially even for
                                GroupSize>1. Both kinds of parallelism have to  be
                                activated by the user, see ALGLIB Reference Manual
                                for more information.

        NOTES: if no stopping criteria is specified (unlimited budget, no timeout,
               no  neighborhood  size  limit),  then  the  solver  will  run until
               enumerating all integer solutions.
               
        ===== ALGORITHM DESCRIPTION ==============================================

        A simplified description for an  all-integer  algorithm, omitting stopping
        criteria and various checks:

            MIVNS (ALL-INTEGER):
                1. Input: initial integral point, may be infeasible wrt  nonlinear
                   constraints, but is feasible wrt linear  ones.  Enforce  linear
                   feasibility, if needed.
                2. Generate initial neighborhood around the current point that  is
                   equal to the point itself. The point is marked as explored.
                3. Scan  neighborhood  for  a  better  point  (one  that  is  less
                   infeasible or has lower objective);  if  one  is found, make it
                   current and goto #2
                4. Scan neighborhood for an unexplored point (one with no objective
                   computed). If one if found, compute objective, mark the point as
                   explored, goto #3
                5. If there are no unexplored or better points in the neighborhood,
                   expand it: find a  point  that  was  not  used  for  expansion,
                   compute up to 2N its nearest integral neighbors,  add  them  to
                   the neighborhood and mark as unexplored. Goto #3. 
                   
            NOTE: A nearest integral neighbor is a nearest point that  differs  at
                  least by +1 or -1 in one  of  integral  variables  and  that  is
                  feasible with respect to box and  linear  constraints  (ignoring
                  nonlinear ones). For problems  with  difficult  constraint  sets
                  integral neighbors are found by solving MIQP subproblems.
                      
        The algorithm above systematically scans neighborhood  of  a  point  until
        either better point is found, an entire integer grid is enumerated, or one
        of stopping conditions is met.

        A mixed-integer version of the algorithm is more complex:
        * it still sees optimization space as a set of integer  nodes,  each  node
          having a subspace of continuous variables associated with it
        * after starting to explore a node, the algorithm runs an  RBF  surrogate-
          based subsolver for the node. It manages a dedicated subsolver for  each
          node in a neighborhood and adaptively divides its  computational  budget
          between subsolvers, switching to a node as soon as its  subsolver  shows
          better results than its competitors.
        * the algorithm remembers all previously evaluated points and reuses  them
          as much as possible
          
        ===== ALGORITHM SCALING WITH VARIABLES COUNT N ===========================

        A 'neighborhood scan' is a minimum number of function evaluations   needed
        to perform at least minimal evaluation of the immediate  neighborhood. For
        an N-dimensional problem with NI  integer variables and NF continuous ones
        we have ~NI nodes in an immediate neighborhood, and each  node  needs  ~NF
        evalutations to build at least linear model of the objective.

        Thus, a MIVNS neighborhood scan will need  about NI*NF=NI*(N-NI)=NF*(N-NF)
        objective evaluations.

        It is important to note that MIVNS  does  not  share  information  between
        nodes because it assumes that objective landscape can  drastically  change
        when jumping from node to node. That's why we need  NI*NF instead of NI+NF
        objective values.

        In practice, when started not too far away from the minimum, we can expect
        to get some improvement in 5-10 scans, and to get significant progress  in
        50-100 scans.

        For problems with NF being small or NI  being  small  we  have  scan  cost
        being proportional to variables count N, which allows us to  achieve  good
        progress using between 5N and 100N objective values.  However,  when  both
        NI and NF are close to N/2,  a  scan  needs  ~N^2  objective  evaluations,
        which results in a much worse scaling behavior.

          -- ALGLIB --
             Copyright 01.01.2025 by Bochkanov Sergey
        *************************************************************************/
        public static void minlpsolversetalgomivns(minlpsolverstate state,
            int budget,
            int maxneighborhood,
            int batchsize,
            alglib.xparams _params)
        {
            alglib.ap.assert(budget>=0, "MINLPSolverSetAlgoMIVNS: Budget<0");
            alglib.ap.assert(maxneighborhood>=0, "MINLPSolverSetAlgoMIVNS: MaxNeighborhood<0");
            alglib.ap.assert(batchsize>=1, "MINLPSolverSetAlgoMIVNS: BatchSize<1");
            state.algoidx = 1;
            state.mirbfvnsalgo = 0;
            state.mirbfvnsbudget = budget;
            state.mirbfvnsmaxneighborhood = maxneighborhood;
            state.mirbfvnsbatchsize = batchsize;
        }


        /*************************************************************************
        This function activates multiple random restarts (performed for each node,
        including root and child ones) that help to find global solutions to  non-
        convex problems.

        This parameter is used  by  branch-and-bound  solvers  and  is  presently
        ignored by derivative-free solvers.

        INPUT PARAMETERS:
            State           -   structure that stores algorithm state
            NMultistarts    -   >=1, number of random restarts:
                                * 1 means that no restarts performed, the solver
                                  assumes convexity
                                * >=1 means that solver assumes non-convexity and
                                  performs fixed amount of random restarts

          -- ALGLIB --
             Copyright 01.01.2025 by Bochkanov Sergey
        *************************************************************************/
        public static void minlpsolversetmultistarts(minlpsolverstate state,
            int nmultistarts,
            alglib.xparams _params)
        {
            alglib.ap.assert(nmultistarts>=1, "MINLPSolverSetMultistarts: NMultistarts<1");
            state.nmultistarts = nmultistarts;
        }


        /*************************************************************************
        This function activates timeout feature. The solver finishes after running
        for a specified amount of time (in seconds, fractions can  be  used)  with
        the best point so far.

        Depending on the situation, the following completion codes can be reported
        in rep.terminationtype:
        * -33 (failure), if timed out without finding a feasible point
        * 5 (partial success), if timed out after finding at least one feasible point

        The solver does not stop immediately after timeout was  triggered  because
        it needs some time for underlying subsolvers to react to  timeout  signal.
        Generally, about one additional subsolver iteration (which is usually  far
        less than one B&B split) will be performed prior to stopping.

        INPUT PARAMETERS:
            State           -   structure that stores algorithm state
            Timeout         -   >=0, timeout in seconds (floating point number):
                                * 0 means no timeout
                                * >=0 means stopping after specified number of
                                  seconds.

          -- ALGLIB --
             Copyright 01.01.2025 by Bochkanov Sergey
        *************************************************************************/
        public static void minlpsolversettimeout(minlpsolverstate state,
            double timeout,
            alglib.xparams _params)
        {
            alglib.ap.assert(math.isfinite(timeout) && (double)(timeout)>=(double)(0), "MINLPSolverSetTimeout: Timeout<0 or is infinite/NAN");
            state.timeout = (int)Math.Ceiling(1000*timeout);
        }


        /*************************************************************************


          -- ALGLIB --
             Copyright 01.01.2025 by Bochkanov Sergey
        *************************************************************************/
        public static bool minlpsolveriteration(minlpsolverstate state,
            alglib.xparams _params)
        {
            bool result = new bool();
            int n = 0;
            int i = 0;
            int k = 0;
            int originalrequesttype = 0;
            bool done = new bool();
            bool densejac = new bool();
            bool b = new bool();

            
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
                k = state.rstate.ia[2];
                originalrequesttype = state.rstate.ia[3];
                done = state.rstate.ba[0];
                densejac = state.rstate.ba[1];
                b = state.rstate.ba[2];
            }
            else
            {
                n = 359;
                i = -58;
                k = -919;
                originalrequesttype = -909;
                done = true;
                densejac = true;
                b = false;
            }
            if( state.rstate.stage==0 )
            {
                goto lbl_0;
            }
            if( state.rstate.stage==1 )
            {
                goto lbl_1;
            }
            
            //
            // Routine body
            //
            alglib.ap.assert(state.hasx0, "MINLPSolver: integrity check 500655 failed");
            n = state.n;
            clearoutputs(state, _params);
            optserv.xlcconverttosparse(state.xlc, _params);
            state.tracelevel = 0;
            if( ap.istraceenabled("MINLP", _params) )
            {
                state.tracelevel = 2;
            }
            if( ap.istraceenabled("MINLP.LACONIC", _params) )
            {
                state.tracelevel = 1;
            }
            
            //
            // Initial trace messages
            //
            if( state.tracelevel>0 )
            {
                alglib.ap.trace("\n\n");
                alglib.ap.trace("////////////////////////////////////////////////////////////////////////////////////////////////////\n");
                alglib.ap.trace("//  MINLP SOLVER STARTED                                                                          //\n");
                alglib.ap.trace("////////////////////////////////////////////////////////////////////////////////////////////////////\n");
                alglib.ap.trace(System.String.Format("N             = {0,6:d}\n", n));
                alglib.ap.trace(System.String.Format("cntLC         = {0,6:d}\n", state.xlc.nsparse+state.xlc.ndense));
                alglib.ap.trace(System.String.Format("cntNLC        = {0,6:d}\n", state.nnlc));
                k = 0;
                for(i=0; i<=n-1; i++)
                {
                    if( state.isintegral[i] )
                    {
                        k = k+1;
                    }
                }
                alglib.ap.trace(System.String.Format("nIntegral     = {0,6:d} vars", k));
                k = 0;
                for(i=0; i<=n-1; i++)
                {
                    if( state.isbinary[i] )
                    {
                        k = k+1;
                    }
                }
                if( k>0 )
                {
                    alglib.ap.trace(System.String.Format(" (incl. {0,0:d} binary ones)", k));
                }
                alglib.ap.trace("\n");
                if( state.algoidx==0 )
                {
                    alglib.ap.trace("> printing BBSYNC solver parameters:\n");
                    alglib.ap.trace(System.String.Format("GroupSize     = {0,6:d}\n", state.bbgdgroupsize));
                    alglib.ap.trace(System.String.Format("Multistarts   = {0,6:d}\n", state.nmultistarts));
                    if( state.timeout>0 )
                    {
                        alglib.ap.trace(System.String.Format("Timeout       = {0,0:F1}s\n", 0.001*state.timeout));
                    }
                    else
                    {
                        alglib.ap.trace("Timeout       = none\n");
                    }
                }
                if( state.algoidx==1 )
                {
                    alglib.ap.trace("> printing MIVNS solver parameters:\n");
                    if( state.mirbfvnsbudget>0 )
                    {
                        alglib.ap.trace(System.String.Format("Budget        = {0,6:d}\n", state.mirbfvnsbudget));
                    }
                    else
                    {
                        alglib.ap.trace("Budget        =    inf\n");
                    }
                    if( state.mirbfvnsmaxneighborhood>0 )
                    {
                        alglib.ap.trace(System.String.Format("MaxNeighbors  = {0,6:d}\n", state.mirbfvnsmaxneighborhood));
                    }
                    else
                    {
                        alglib.ap.trace("MaxNeighbors  =    inf\n");
                    }
                    alglib.ap.trace(System.String.Format("BatchSize     = {0,6:d}\n", state.mirbfvnsbatchsize));
                    if( state.timeout>0 )
                    {
                        alglib.ap.trace(System.String.Format("Timeout       = {0,0:F1}s\n", 0.001*state.timeout));
                    }
                    else
                    {
                        alglib.ap.trace("Timeout       = none\n");
                    }
                }
                alglib.ap.trace("\n");
            }
            
            //
            // Init the solver
            //
            done = false;
            if( state.algoidx==0 )
            {
                if( !(state.bbgdsubsolver!=null) )
                {
                    state.bbgdsubsolver = new bbgd.bbgdstate();
                }
                bbgd.bbgdcreatebuf(n, state.bndl, state.bndu, state.s, state.x0, state.isintegral, state.isbinary, state.xlc.effsparsea, state.xlc.effal, state.xlc.effau, state.xlc.lcsrcidx, state.xlc.nsparse+state.xlc.ndense, state.nl, state.nu, state.nnlc, state.bbgdgroupsize, state.nmultistarts, state.timeout, state.tracelevel, state.bbgdsubsolver, _params);
                if( (double)(state.pdgap)>(double)(0) )
                {
                    bbgd.bbgdsetpdgap(state.bbgdsubsolver, state.pdgap, _params);
                }
                if( (double)(state.ctol)>(double)(0) )
                {
                    bbgd.bbgdsetctol(state.bbgdsubsolver, state.ctol, _params);
                }
                if( (double)(state.subsolverepsx)>(double)(0) )
                {
                    bbgd.bbgdsetepsx(state.bbgdsubsolver, state.subsolverepsx, _params);
                }
                if( (double)(state.subsolverepsf)>(double)(0) )
                {
                    bbgd.bbgdsetepsf(state.bbgdsubsolver, state.subsolverepsf, _params);
                }
                done = true;
            }
            if( state.algoidx==1 )
            {
                if( !(state.mirbfvnssubsolver!=null) )
                {
                    state.mirbfvnssubsolver = new mirbfvns.mirbfvnsstate();
                }
                mirbfvns.mirbfvnscreatebuf(n, state.bndl, state.bndu, state.s, state.x0, state.isintegral, state.isbinary, state.xlc.effsparsea, state.xlc.effal, state.xlc.effau, state.xlc.lcsrcidx, state.xlc.nsparse+state.xlc.ndense, state.nl, state.nu, state.nnlc, state.mirbfvnsalgo, state.mirbfvnsbudget, state.mirbfvnsmaxneighborhood, state.mirbfvnsbatchsize, state.timeout, state.tracelevel, state.mirbfvnssubsolver, _params);
                mirbfvns.mirbfvnssetadaptiveinternalparallelism(state.mirbfvnssubsolver, state.adaptiveinternalparallelism, _params);
                if( (double)(state.ctol)>(double)(0) )
                {
                    mirbfvns.mirbfvnssetctol(state.mirbfvnssubsolver, state.ctol, _params);
                }
                if( (double)(state.subsolverepsf)>(double)(0) )
                {
                    mirbfvns.mirbfvnssetepsf(state.mirbfvnssubsolver, state.subsolverepsf, _params);
                }
                if( (double)(state.subsolverepsx)>(double)(0) )
                {
                    mirbfvns.mirbfvnssetepsx(state.mirbfvnssubsolver, state.subsolverepsx, _params);
                }
                b = state.hasobjmask;
                for(i=0; i<=state.nnlc-1; i++)
                {
                    b = b || state.hasnlcmask[i];
                }
                if( b )
                {
                    ablasf.ballocv(1+state.nnlc, ref state.tmpb1, _params);
                    sparse.sparsecreatecrsemptybuf(n, state.tmpsparse, _params);
                    sparse.sparseappendemptyrow(state.tmpsparse, _params);
                    state.tmpb1[0] = state.hasobjmask;
                    if( state.hasobjmask )
                    {
                        for(i=0; i<=n-1; i++)
                        {
                            if( state.objmask[i] )
                            {
                                sparse.sparseappendelement(state.tmpsparse, i, 1.0, _params);
                            }
                        }
                    }
                    if( state.nnlc>0 )
                    {
                        sparse.sparseappendmatrix(state.tmpsparse, state.nlcmask, _params);
                    }
                    for(i=0; i<=state.nnlc-1; i++)
                    {
                        state.tmpb1[1+i] = state.hasnlcmask[i];
                    }
                    mirbfvns.mirbfvnssetvariablemask(state.mirbfvnssubsolver, state.tmpb1, state.tmpsparse, _params);
                }
                done = true;
            }
            alglib.ap.assert(done, "MINLPSolvers: 891649 failed");
            
            //
            // Run the solver
            //
            done = false;
            if( state.algoidx!=0 )
            {
                goto lbl_2;
            }
        lbl_4:
            if( !bbgd.bbgditeration(state.bbgdsubsolver, _params) )
            {
                goto lbl_5;
            }
            
            //
            // Offload request
            //
            bbgd.bbgdoffloadrcommrequest(state.bbgdsubsolver, ref originalrequesttype, ref state.querysize, ref state.queryfuncs, ref state.queryvars, ref state.querydim, ref state.queryformulasize, ref state.querydata, _params);
            alglib.ap.assert(originalrequesttype==1, "MINLPSOLVERS: integrity check 328345 failed");
            state.requesttype = apserv.icase2(state.issuesparserequests, originalrequesttype, 2, _params);
            
            //
            // Initialize temporaries and prepare place for reply
            //
            densejac = (state.requesttype==2 || state.requesttype==3) || state.requesttype==5;
            ablasf.rallocv(n, ref state.tmpg1, _params);
            ablasf.rallocv(n, ref state.tmpx1, _params);
            ablasf.rallocv(1+state.nnlc, ref state.tmpf1, _params);
            if( densejac )
            {
                ablasf.rallocm(1+state.nnlc, n, ref state.tmpj1, _params);
                ablasf.rallocv(state.queryfuncs*state.queryvars*state.querysize, ref state.replydj, _params);
            }
            ablasf.rallocv(state.queryfuncs*state.querysize, ref state.replyfi, _params);
            
            //
            // RComm and copy back
            //
            state.rstate.stage = 0;
            goto lbl_rcomm;
        lbl_0:
            if( densejac )
            {
                sparse.sparsecreatecrsfromdensevbuf(state.replydj, state.querysize*state.queryfuncs, state.queryvars, state.replysj, _params);
            }
            bbgd.bbgdloadrcommreply(state.bbgdsubsolver, originalrequesttype, state.querysize, state.queryfuncs, state.queryvars, state.querydim, state.queryformulasize, state.replyfi, state.rdummy, state.replysj, _params);
            goto lbl_4;
        lbl_5:
            done = true;
        lbl_2:
            if( state.algoidx!=1 )
            {
                goto lbl_6;
            }
        lbl_8:
            if( !mirbfvns.mirbfvnsiteration(state.mirbfvnssubsolver, _params) )
            {
                goto lbl_9;
            }
            
            //
            // Offload request
            //
            alglib.ap.assert(state.mirbfvnssubsolver.requesttype==4, "MINLPSOLVERS: 993231 failed");
            state.requesttype = state.mirbfvnssubsolver.requesttype;
            state.querysize = state.mirbfvnssubsolver.querysize;
            state.queryfuncs = state.mirbfvnssubsolver.queryfuncs;
            state.queryvars = state.mirbfvnssubsolver.queryvars;
            state.querydim = state.mirbfvnssubsolver.querydim;
            ablasf.rcopyallocv(state.querysize*(state.queryvars+state.querydim), state.mirbfvnssubsolver.querydata, ref state.querydata, _params);
            
            //
            // Initialize temporaries and prepare place for reply
            //
            ablasf.rallocv(n, ref state.tmpx1, _params);
            ablasf.rallocv(1+state.nnlc, ref state.tmpf1, _params);
            ablasf.rallocv(state.queryfuncs*state.querysize, ref state.replyfi, _params);
            
            //
            // RComm and copy back
            //
            state.rstate.stage = 1;
            goto lbl_rcomm;
        lbl_1:
            ablasf.rcopyv(state.querysize*state.queryfuncs, state.replyfi, state.mirbfvnssubsolver.replyfi, _params);
            goto lbl_8;
        lbl_9:
            done = true;
        lbl_6:
            alglib.ap.assert(done, "MINLPSolvers: 926649 failed");
            
            //
            // Save results
            //
            done = false;
            if( state.algoidx==0 )
            {
                ablasf.rcopyallocv(n, state.bbgdsubsolver.xc, ref state.xc, _params);
                state.repnfev = state.bbgdsubsolver.repnfev;
                state.repnsubproblems = state.bbgdsubsolver.repnsubproblems;
                state.repntreenodes = state.bbgdsubsolver.repntreenodes;
                state.repnnodesbeforefeasibility = state.bbgdsubsolver.repnnodesbeforefeasibility;
                state.repterminationtype = state.bbgdsubsolver.repterminationtype;
                state.repf = state.bbgdsubsolver.repf;
                state.reppdgap = state.bbgdsubsolver.reppdgap;
                done = true;
            }
            if( state.algoidx==1 )
            {
                ablasf.rcopyallocv(n, state.mirbfvnssubsolver.xc, ref state.xc, _params);
                state.repnfev = state.mirbfvnssubsolver.repnfev;
                state.repnsubproblems = 0;
                state.repntreenodes = 0;
                state.repnnodesbeforefeasibility = 0;
                state.repterminationtype = state.mirbfvnssubsolver.repterminationtype;
                state.repf = state.mirbfvnssubsolver.fc;
                state.reppdgap = 0;
                done = true;
            }
            alglib.ap.assert(done, "MINLPSolvers: 944650 failed");
            result = false;
            return result;
            
            //
            // Saving state
            //
        lbl_rcomm:
            result = true;
            state.rstate.ia[0] = n;
            state.rstate.ia[1] = i;
            state.rstate.ia[2] = k;
            state.rstate.ia[3] = originalrequesttype;
            state.rstate.ba[0] = done;
            state.rstate.ba[1] = densejac;
            state.rstate.ba[2] = b;
            return result;
        }


        /*************************************************************************
        This subroutine  restarts  algorithm  from  new  point.  All  optimization
        parameters (including constraints) are left unchanged.

        This  function  allows  to  solve multiple  optimization  problems  (which
        must have  same number of dimensions) without object reallocation penalty.

        INPUT PARAMETERS:
            State   -   structure previously allocated with MINLPSolverCreate call.
            X       -   new starting point.

          -- ALGLIB --
             Copyright 28.11.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void minlpsolverrestartfrom(minlpsolverstate state,
            double[] x,
            alglib.xparams _params)
        {
            int n = 0;

            n = state.n;
            
            //
            // First, check for errors in the inputs
            //
            alglib.ap.assert(alglib.ap.len(x)>=n, "MINLPSolverRestartFrom: Length(X)<N");
            alglib.ap.assert(apserv.isfinitevector(x, n, _params), "MINLPSolverRestartFrom: X contains infinite or NaN values!");
            
            //
            // Set XC
            //
            ablasf.rcopyv(n, x, state.x0, _params);
            state.hasx0 = true;
            
            //
            // prepare RComm facilities
            //
            state.rstate.ia = new int[3+1];
            state.rstate.ba = new bool[2+1];
            state.rstate.stage = -1;
            clearoutputs(state, _params);
        }


        /*************************************************************************
        MINLPSolver results:  the  solution  found,  completion  codes  and  additional
        information.

        INPUT PARAMETERS:
            Solver  -   solver

        OUTPUT PARAMETERS:
            X       -   array[N], solution
            Rep     -   optimization report, contains information about completion
                        code, constraint violation at the solution and so on.
                        
                        rep.f contains objective value at the solution.
                        
                        You   should   check   rep.terminationtype  in  order   to
                        distinguish successful termination from unsuccessful one.
                        
                        More information about fields of this  structure  can  be
                        found in the comments on the minlpsolverreport datatype.
           
          -- ALGLIB --
             Copyright 18.01.2024 by Bochkanov Sergey
        *************************************************************************/
        public static void minlpsolverresults(minlpsolverstate state,
            ref double[] x,
            minlpsolverreport rep,
            alglib.xparams _params)
        {
            x = new double[0];

            minlpsolverresultsbuf(state, ref x, rep, _params);
        }


        /*************************************************************************
        NLC results

        Buffered implementation of MINLPSolverResults() which uses pre-allocated buffer
        to store X[]. If buffer size is  too  small,  it  resizes  buffer.  It  is
        intended to be used in the inner cycles of performance critical algorithms
        where array reallocation penalty is too large to be ignored.

          -- ALGLIB --
             Copyright 28.11.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void minlpsolverresultsbuf(minlpsolverstate state,
            ref double[] x,
            minlpsolverreport rep,
            alglib.xparams _params)
        {
            rep.f = state.repf;
            rep.nfev = state.repnfev;
            rep.nsubproblems = state.repnsubproblems;
            rep.ntreenodes = state.repntreenodes;
            rep.nnodesbeforefeasibility = state.repnnodesbeforefeasibility;
            rep.terminationtype = state.repterminationtype;
            rep.pdgap = state.reppdgap;
            if( state.repterminationtype>0 )
            {
                ablasf.rcopyallocv(state.n, state.xc, ref x, _params);
            }
            else
            {
                ablasf.rsetallocv(state.n, Double.NaN, ref x, _params);
            }
        }


        /*************************************************************************
        Set V2 reverse communication protocol with dense requests
        *************************************************************************/
        public static void minlpsolversetprotocolv2(minlpsolverstate state,
            alglib.xparams _params)
        {
            state.protocolversion = 2;
            state.issuesparserequests = false;
            state.rstate.ia = new int[3+1];
            state.rstate.ba = new bool[2+1];
            state.rstate.stage = -1;
        }


        /*************************************************************************
        Set V2 reverse communication protocol with sparse requests
        *************************************************************************/
        public static void minlpsolversetprotocolv2s(minlpsolverstate state,
            alglib.xparams _params)
        {
            state.protocolversion = 2;
            state.issuesparserequests = true;
            state.rstate.ia = new int[3+1];
            state.rstate.ba = new bool[2+1];
            state.rstate.stage = -1;
        }


        /*************************************************************************
        Clears output fields during initialization
        *************************************************************************/
        private static void clearoutputs(minlpsolverstate state,
            alglib.xparams _params)
        {
            state.userterminationneeded = false;
            state.repnfev = 0;
            state.repterminationtype = 0;
            state.repf = 0;
            state.reppdgap = math.maxrealnumber;
            state.repnsubproblems = 0;
            state.repntreenodes = 0;
            state.repnnodesbeforefeasibility = -1;
        }


        /*************************************************************************
        Internal initialization subroutine.
        Sets default NLC solver with default criteria.
        *************************************************************************/
        private static void initinternal(int n,
            double[] x,
            int solvermode,
            double diffstep,
            minlpsolverstate state,
            alglib.xparams _params)
        {
            int i = 0;
            double[,] c = new double[0,0];
            int[] ct = new int[0];

            state.protocolversion = 2;
            state.issuesparserequests = false;
            state.convexityflag = 0;
            
            //
            // Initialize other params
            //
            optserv.critinitdefault(state.criteria, _params);
            state.timeout = 0;
            state.pdgap = 0;
            state.ctol = 0;
            state.n = n;
            state.subsolverepsx = 0;
            state.subsolverepsf = 0;
            state.nmultistarts = 1;
            state.diffstep = diffstep;
            state.userterminationneeded = false;
            ablasf.bsetallocv(n, false, ref state.isintegral, _params);
            ablasf.bsetallocv(n, false, ref state.isbinary, _params);
            state.bndl = new double[n];
            state.bndu = new double[n];
            state.s = new double[n];
            state.x0 = new double[n];
            state.xc = new double[n];
            for(i=0; i<=n-1; i++)
            {
                state.bndl[i] = Double.NegativeInfinity;
                state.bndu[i] = Double.PositiveInfinity;
                state.s[i] = 1.0;
                state.x0[i] = x[i];
                state.xc[i] = x[i];
            }
            state.hasx0 = true;
            state.hasobjmask = false;
            
            //
            // Constraints
            //
            optserv.xlcinit(n, state.xlc, _params);
            sparse.sparsecreatecrsemptybuf(n, state.nlcmask, _params);
            state.nnlc = 0;
            
            //
            // Report fields
            //
            clearoutputs(state, _params);
            
            //
            // RComm
            //
            state.rstate.ia = new int[3+1];
            state.rstate.ba = new bool[2+1];
            state.rstate.stage = -1;
            
            //
            // Final setup
            //
            minlpsolvercautiousinternalparallelism(state, _params);
            minlpsolversetalgobbsync(state, 1, _params);
        }


    }
}

