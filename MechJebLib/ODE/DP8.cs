/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Collections.Generic;
using MechJebLib.Primitives;
using static System.Math;
using static MechJebLib.Utils.Statics;

// ReSharper disable CompareOfFloatsByEqualityOperator
namespace MechJebLib.ODE
{
    using IVPFunc = Action<IList<double>, double, IList<double>>;

    /// <summary>
    /// </summary>
    public class DP8 : AbstractRungeKutta
    {
        protected override int Order               => 8;
        protected override int Stages              => 12;
        protected override int ErrorEstimatorOrder => 7;

        #region IntegrationConstants

        private const double A0201 = 5.26001519587677318785587544488e-2;
        private const double A0301 = 1.97250569845378994544595329183e-2;
        private const double A0302 = 5.91751709536136983633785987549e-2;
        private const double A0401 = 2.95875854768068491816892993775e-2;
        private const double A0403 = 8.87627564304205475450678981324e-2;
        private const double A0501 = 2.41365134159266685502369798665e-1;
        private const double A0503 = -8.84549479328286085344864962717e-1;
        private const double A0504 = 9.24834003261792003115737966543e-1;
        private const double A0601 = 3.7037037037037037037037037037e-2;
        private const double A0604 = 1.70828608729473871279604482173e-1;
        private const double A0605 = 1.25467687566822425016691814123e-1;
        private const double A0701 = 3.7109375e-2;
        private const double A0704 = 1.70252211019544039314978060272e-1;
        private const double A0705 = 6.02165389804559606850219397283e-2;
        private const double A0706 = -1.7578125e-2;
        private const double A0801 = 3.70920001185047927108779319836e-2;
        private const double A0804 = 1.70383925712239993810214054705e-1;
        private const double A0805 = 1.07262030446373284651809199168e-1;
        private const double A0806 = -1.53194377486244017527936158236e-2;
        private const double A0807 = 8.27378916381402288758473766002e-3;
        private const double A0901 = 6.24110958716075717114429577812e-1;
        private const double A0904 = -3.36089262944694129406857109825;
        private const double A0905 = -8.68219346841726006818189891453e-1;
        private const double A0906 = 2.75920996994467083049415600797e1;
        private const double A0907 = 2.01540675504778934086186788979e1;
        private const double A0908 = -4.34898841810699588477366255144e1;
        private const double A1001 = 4.77662536438264365890433908527e-1;
        private const double A1004 = -2.48811461997166764192642586468e0;
        private const double A1005 = -5.90290826836842996371446475743e-1;
        private const double A1006 = 2.12300514481811942347288949897e1;
        private const double A1007 = 1.52792336328824235832596922938e1;
        private const double A1008 = -3.32882109689848629194453265587e1;
        private const double A1009 = -2.03312017085086261358222928593e-2;
        private const double A1101 = -9.3714243008598732571704021658e-1;
        private const double A1104 = 5.18637242884406370830023853209e0;
        private const double A1105 = 1.09143734899672957818500254654e0;
        private const double A1106 = -8.14978701074692612513997267357e0;
        private const double A1107 = -1.85200656599969598641566180701e1;
        private const double A1108 = 2.27394870993505042818970056734e1;
        private const double A1109 = 2.49360555267965238987089396762e0;
        private const double A1110 = -3.0467644718982195003823669022e0;
        private const double A1201 = 2.27331014751653820792359768449e0;
        private const double A1204 = -1.05344954667372501984066689879e1;
        private const double A1205 = -2.00087205822486249909675718444e0;
        private const double A1206 = -1.79589318631187989172765950534e1;
        private const double A1207 = 2.79488845294199600508499808837e1;
        private const double A1208 = -2.85899827713502369474065508674e0;
        private const double A1209 = -8.87285693353062954433549289258e0;
        private const double A1210 = 1.23605671757943030647266201528e1;
        private const double A1211 = 6.43392746015763530355970484046e-1;

        // interpolant:
        private const double A1401 = 0.056167502283047954;
        private const double A1407 = 0.25350021021662483;
        private const double A1408 = -0.2462390374708025;
        private const double A1409 = -0.12419142326381637;
        private const double A1410 = 0.15329179827876568;
        private const double A1411 = 0.00820105229563469;
        private const double A1412 = 0.007567897660545699;
        private const double A1413 = -0.008298;
        private const double A1501 = 0.03183464816350214;
        private const double A1506 = 0.028300909672366776;
        private const double A1507 = 0.053541988307438566;
        private const double A1508 = -0.05492374857139099;
        private const double A1511 = -0.00010834732869724932;
        private const double A1512 = 0.0003825710908356584;
        private const double A1513 = -0.00034046500868740456;
        private const double A1514 = 0.1413124436746325;
        private const double A1601 = -0.42889630158379194;
        private const double A1606 = -4.697621415361164;
        private const double A1607 = 7.683421196062599;
        private const double A1608 = 4.06898981839711;
        private const double A1609 = 0.3567271874552811;
        private const double A1613 = -0.0013990241651590145;
        private const double A1614 = 2.9475147891527724;
        private const double A1615 = -9.15095847217987;

        private const double C2  = 0.526001519587677318785587544488e-01;
        private const double C3  = 0.789002279381515978178381316732e-01;
        private const double C4  = 0.118350341907227396726757197510;
        private const double C5  = 0.281649658092772603273242802490;
        private const double C6  = 0.333333333333333333333333333333;
        private const double C7  = 0.25;
        private const double C8  = 0.307692307692307692307692307692;
        private const double C9  = 0.651282051282051282051282051282;
        private const double C10 = 0.6;
        private const double C11 = 0.857142857142857142857142857142;

        // interpolant:
        private const double C13 = 1.0;
        private const double C14 = 0.1;
        private const double C15 = 0.2;
        private const double C16 = 0.777777777777777777777777777778;

        private const double E501 = 0.1312004499419488073250102996e-1;
        private const double E506 = -0.1225156446376204440720569753e+1;
        private const double E507 = -0.4957589496572501915214079952;
        private const double E508 = 0.1664377182454986536961530415e+1;
        private const double E509 = -0.3503288487499736816886487290;
        private const double E510 = 0.3341791187130174790297318841;
        private const double E511 = 0.8192320648511571246570742613e-1;
        private const double E512 = -0.2235530786388629525884427845e-1;

        private const double E301 = 0.244094488188976377952755905512;
        private const double E303 = 0.220588235294117647058823529412e-1;
        private const double E309 = 0.733846688281611857341361741547;

        /*
        private const double B10 = 11282082432.0 / 11282082432.0;
        private const double B11 = -32272833064.0 / 11282082432.0;
        private const double B12 = 34969693132.0 / 11282082432.0;
        private const double B13 = -13107642775.0 / 11282082432.0;
        private const double B14 = 157015080.0 / 11282082432.0;

        private const double B31 = -100 * -1323431896.0 / 32700410799.0;
        private const double B32 = -100 * 2074956840.0 / 32700410799.0;
        private const double B33 = -100 * -914128567.0 / 32700410799.0;
        private const double B34 = -100 * 15701508.0 / 32700410799.0;

        private const double B41 = 25.0 * -889289856.0 / 5641041216.0;
        private const double B42 = 25.0 * 2460397220.0 / 5641041216.0;
        private const double B43 = 25.0 * -1518414297.0 / 5641041216.0;
        private const double B44 = 25.0 * 94209048.0 / 5641041216.0;

        private const double B51 = -2187.0 * -259006536.0 / 199316789632.0;
        private const double B52 = -2187.0 * 687873124.0 / 199316789632.0;
        private const double B53 = -2187.0 * -451824525.0 / 199316789632.0;
        private const double B54 = -2187.0 * 52338360.0 / 199316789632.0;

        private const double B61 = 11.0 * -361440756.0 / 2467955532.0;
        private const double B62 = 11.0 * 946554244.0 / 2467955532.0;
        private const double B63 = 11.0 * -661884105.0 / 2467955532.0;
        private const double B64 = 11.0 * 106151040.0 / 2467955532.0;

        private const double B71 = 44764047.0 / 29380423.0;
        private const double B72 = -82437520.0 / 29380423.0;
        private const double B73 = 8293050.0 / 29380423.0;
        */

        #endregion

        protected override void RKStep(IVPFunc f)
        {
            double h = Habs * Direction;

            K[1].CopyFrom(Dy);

            for (int i = 0; i < N; i++)
                Ynew[i] = Y[i] + h * (A0201 * Dy[i]);
            f(Ynew, T + C2 * h, K[2]);

            for (int i = 0; i < N; i++)
                Ynew[i] = Y[i] + h * (A0301 * K[1][i] + A0302 * K[2][i]);
            f(Ynew, T + C3 * h, K[3]);

            for (int i = 0; i < N; i++)
                Ynew[i] = Y[i] + h * (A0401 * K[1][i] + A0403 * K[3][i]);
            f(Ynew, T + C4 * h, K[4]);

            for (int i = 0; i < N; i++)
                Ynew[i] = Y[i] + h * (A0501 * K[1][i] + A0503 * K[3][i] + A0504 * K[4][i]);
            f(Ynew, T + C5 * h, K[5]);

            for (int i = 0; i < N; i++)
                Ynew[i] = Y[i] + h * (A0601 * K[1][i] + A0604 * K[4][i] + A0605 * K[5][i]);
            f(Ynew, T + C6 * h, K[6]);

            for (int i = 0; i < N; i++)
                Ynew[i] = Y[i] + h * (A0701 * K[1][i] + A0704 * K[4][i] + A0705 * K[5][i] + A0706 * K[6][i]);
            f(Ynew, T + C7 * h, K[7]);

            for (int i = 0; i < N; i++)
                Ynew[i] = Y[i] + h * (A0801 * K[1][i] + A0804 * K[4][i] + A0805 * K[5][i] + A0806 * K[6][i] + A0807 * K[7][i]);
            f(Ynew, T + C8 * h, K[8]);

            for (int i = 0; i < N; i++)
                Ynew[i] = Y[i] + h * (A0901 * K[1][i] + A0904 * K[4][i] + A0905 * K[5][i] + A0906 * K[6][i] + A0907 * K[7][i] + A0908 * K[8][i]);
            f(Ynew, T + C9 * h, K[9]);

            for (int i = 0; i < N; i++)
                Ynew[i] = Y[i] + h * (A1001 * K[1][i] + A1004 * K[4][i] + A1005 * K[5][i] + A1006 * K[6][i] + A1007 * K[7][i] + A1008 * K[8][i] +
                    A1009 * K[9][i]);
            f(Ynew, T + C10 * h, K[10]);

            for (int i = 0; i < N; i++)
                Ynew[i] = Y[i] + h * (A1101 * K[1][i] + A1104 * K[4][i] + A1105 * K[5][i] + A1106 * K[6][i] + A1107 * K[7][i] + A1108 * K[8][i] +
                    A1109 * K[9][i] + A1110 * K[10][i]);
            f(Ynew, T + C11 * h, K[11]);

            for (int i = 0; i < N; i++)
                Ynew[i] = Y[i] + h * (A1201 * K[1][i] + A1204 * K[4][i] + A1205 * K[5][i] + A1206 * K[6][i] + A1207 * K[7][i] + A1208 * K[8][i] +
                    A1209 * K[9][i] + A1210 * K[10][i] + A1211 * K[11][i]);
            f(Ynew, T + h, K[12]);

            K[12].CopyTo(Dynew);
        }

        protected override void InitInterpolant()
        {
            // intentionally left blank
        }

        // https://doi.org/10.1016/0898-1221(86)90025-8
        protected override void Interpolate(double x, Vn yout)
        {
            throw new NotImplementedException();

            /*
            double h = Habs * Direction;
            double s = (x - T) / h;
            double s2 = s * s;
            double s3 = s * s2;
            double s4 = s2 * s2;

            double bs1 = B10 + B11 * s + B12 * s2 + B13 * s3 + B14 * s4;
            double bs3 = s * (B31 + B32 * s + B33 * s2 + B34 * s3);
            double bs4 = s * (B41 + B42 * s + B43 * s2 + B44 * s3);
            double bs5 = s * (B51 + B52 * s + B53 * s2 + B54 * s3);
            double bs6 = s * (B61 + B62 * s + B63 * s2 + B64 * s3);
            double bs7 = (1.0 - s) * s * (B71 + B72 * s + B73 * s2);

            for (int i = 0; i < N; i++)
                yout[i] = Y[i] + h * s * (bs1 * K[1][i] + bs3 * K[3][i] + bs4 * K[4][i] + bs5 * K[5][i] + bs6 * K[6][i] + bs7 * K[7][i]);
                */
        }

        /*
         * def _estimate_error_norm(self, K, h, scale):
           err5 = np.dot(K.T, self.E5) / scale
           err3 = np.dot(K.T, self.E3) / scale
           err5_norm_2 = np.linalg.norm(err5)**2
           err3_norm_2 = np.linalg.norm(err3)**2
           if err5_norm_2 == 0 and err3_norm_2 == 0:
           return 0.0
           denom = err5_norm_2 + 0.01 * err3_norm_2
           return np.abs(h) * err5_norm_2 / np.sqrt(denom * len(scale))
         */

        protected override double ScaledErrorNorm()
        {
            var err3 = Vn.Rent(N);
            var err5 = Vn.Rent(N);

            for (int i = 0; i < N; i++)
            {
                err3[i] = K[4][i] - K[1][i] * E301 - K[3][i] * E303 - K[9][i] * E309;
                err5[i] = K[1][i] * E501 + K[6][i] * E506 + K[7][i] * E507 + K[8][i] * E508 + K[9][i] * E509 + K[10][i] * E510 + K[2][i] * E511 +
                    K[3][i] * E512;
            }

            double error5 = 0.0, error3 = 0.0;
            for (int i = 0; i < N; i++)
            {
                double scale = Atol + Rtol * Max(Abs(Y[i]), Abs(Ynew[i]));
                error5 += Powi(err5[i] / scale, 2);
                error3 += Powi(err3[i] / scale, 2);
            }

            double denom = error5 + 0.01 * error3;
            if (denom <= 0.0)
                denom = 1.0;
            return Habs * error5 * Sqrt(1.0 / (N * denom));
        }
    }
}
