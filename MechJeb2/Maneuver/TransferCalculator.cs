// #define DEBUG

using System;
using System.Threading;
using UnityEngine;

namespace MuMech
{
	public class TransferCalculator
	{
		public int bestDate = 0;
		public int bestDuration = 0;
		public bool stop = false;

		int pendingJobs;

		// Original parameters, only used to check if parameters have changed
		public readonly Orbit originOrbit;
		public readonly Orbit destinationOrbit;

		protected readonly Orbit origin;
		protected readonly Orbit destination;

		protected int nextDateIndex;
		protected readonly int dateSamples;
		public readonly double minDepartureTime;
		public readonly double maxDepartureTime;
		public readonly double minTransferTime;
		public readonly double maxTransferTime;
		protected readonly int maxDurationSamples;

		public ManeuverParameters[,] computed;
#if DEBUG
		string[,] log;
#endif

		double arrivalDate;

		public ManeuverParameters result;


		public TransferCalculator(
				Orbit o, Orbit target,
				double min_departure_time,
				double max_transfer_time,
				double min_sampling_step):
			this(o, target, min_departure_time, min_departure_time + max_transfer_time, 3600, max_transfer_time,
			     Math.Min(1000, Math.Max(200, (int) (max_transfer_time / Math.Max(min_sampling_step, 60.0)))),
			     Math.Min(1000, Math.Max(200, (int) (max_transfer_time / Math.Max(min_sampling_step, 60.0)))))
		{
			StartThreads();
		}

		protected TransferCalculator(
			Orbit o, Orbit target,
			double min_departure_time,
			double max_departure_time,
			double min_transfer_time,
			double max_transfer_time,
			int width,
			int height)
		{
			originOrbit = o;
			destinationOrbit = target;

			origin = new Orbit();
			origin.UpdateFromOrbitAtUT(o, min_departure_time, o.referenceBody);
			destination = new Orbit();
			destination.UpdateFromOrbitAtUT(target, min_departure_time, target.referenceBody);
			maxDurationSamples = height;
			dateSamples = width;
			nextDateIndex = dateSamples;
			this.minDepartureTime = min_departure_time;
			this.maxDepartureTime = max_departure_time;
			this.minTransferTime = min_transfer_time;
			this.maxTransferTime = max_transfer_time;
			computed = new ManeuverParameters[dateSamples, maxDurationSamples];
			pendingJobs = 0;

#if DEBUG
			log = new string[dateSamples, maxDurationSamples];
#endif
		}

		protected void StartThreads()
		{

			if (pendingJobs != 0)
				throw new Exception("Computation threads have already been started");

			pendingJobs = Math.Max(1, Environment.ProcessorCount - 1);
			for (int job = 0; job < pendingJobs; job++)
				ThreadPool.QueueUserWorkItem(ComputeDeltaV);

//			pending_jobs = 1;
//			ComputeDeltaV(this);
		}

		private bool IsBetter(int date_index1, int duration_index1, int date_index2, int duration_index2)
		{
			return computed[date_index1, duration_index1].dV.sqrMagnitude > computed[date_index2, duration_index2].dV.sqrMagnitude;
		}

		void ComputeDeltaV(object args)
		{
			CelestialBody origin_planet = origin.referenceBody;

			for (int date_index = TakeDateIndex();
					date_index >= 0;
					date_index = TakeDateIndex())
			{
				double t0 = DateFromIndex(date_index);
				Vector3d V1_0 = origin_planet.orbit.getOrbitalVelocityAtUT(t0);
				Vector3d R1 = origin_planet.orbit.getRelativePositionAtUT(t0);

				int duration_samples = DurationSamplesForDate(date_index);
				for (int duration_index = 0; duration_index < duration_samples; duration_index++)
				{
					if (stop)
						break;

					double dt = DurationFromIndex(duration_index);
					double t1 = t0 + dt;

					Vector3d R2 = destination.getRelativePositionAtUT(t1);

					bool short_way = Vector3d.Dot(Vector3d.Cross(R1, R2), origin_planet.orbit.GetOrbitNormal()) > 0;
					try
					{
						Vector3d V1, V2;
#if DEBUG
						log[date_index, duration_index] = dt + ","
							+ R1.x + "," + R1.y + "," + R1.z + ","
								+ R2.x + "," + R2.y + "," + R2.z + ","
								+ V1_0.x + "," + V1_0.y + "," + V1_0.z;
#endif
						LambertSolver.Solve(R1, R2, dt, origin_planet.referenceBody, short_way, out V1, out V2);

#if DEBUG
						log[date_index, duration_index] += "," + V1.x + "," + V1.y + "," + V1.z;
#endif

						Vector3d soi_exit_velocity = V1 - V1_0;
						var maneuver = ComputeEjectionManeuver(soi_exit_velocity, origin, t0);
						if (!double.IsNaN(maneuver.dV.sqrMagnitude) && !double.IsNaN(maneuver.UT))
						{
							computed[date_index, duration_index] = maneuver;
						}
#if DEBUG
						log[date_index, duration_index] += "," + maneuver.dV.magnitude;
#endif
					}
					catch (Exception) {}
				}
			}

			JobFinished();
		}

		private void JobFinished()
		{
			int remaining = Interlocked.Decrement(ref pendingJobs);
			if (remaining == 0)
			{
				for (int date_index = 0; date_index < dateSamples; date_index++)
				{
					int n = DurationSamplesForDate(date_index);
					for (int duration_index = 0; duration_index < n; duration_index++)
					{
						if (computed[bestDate, bestDuration] == null ||
							(computed[date_index, duration_index] != null
								&& IsBetter(bestDate, bestDuration, date_index, duration_index)))
						{
							bestDate = date_index;
							bestDuration = duration_index;
						}
					}
				}

				arrivalDate = DateFromIndex(bestDate) + DurationFromIndex(bestDuration);
				result = computed[bestDate, bestDuration];

				pendingJobs = -1;

#if DEBUG
				string dir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
				var f = System.IO.File.CreateText(dir + "/DeltaVWorking.csv");
				f.WriteLine(originOrbit.referenceBody.referenceBody.gravParameter);
				for (int date_index = 0; date_index < dateSamples; date_index++)
				{
					int n = DurationSamplesForDate(date_index);
					for (int duration_index = 0; duration_index < n; duration_index++)
					{
						f.WriteLine(log[date_index, duration_index]);
					}
				}
#endif
			}
		}

		public bool Finished { get { return pendingJobs == -1; } }

		public virtual int Progress { get
			{ return (int)(100 * (1 - Math.Sqrt((double)Math.Max(0, nextDateIndex) / dateSamples))); } }

		protected int TakeDateIndex() { return Interlocked.Decrement(ref nextDateIndex); }

		public virtual int DurationSamplesForDate(int date_index)
		{
			return (int)(maxDurationSamples * (maxDepartureTime - DateFromIndex(date_index)) / maxTransferTime);
		}
		public double DurationFromIndex(int index)
		{
			return minTransferTime + index * (maxTransferTime - minTransferTime) / maxDurationSamples;
		}
		public double DateFromIndex(int index)
		{
			return minDepartureTime + index * (maxDepartureTime - minDepartureTime) / dateSamples;
		}

		public ManeuverParameters OptimizedResult
		{
			get
			{
				return OptimizeEjection(result, origin, destination, arrivalDate, minDepartureTime);
			}
		}

		// Compute the two intersecting lines of a cone with a corner at (0,0,0) with a plane passing by (0,0,0)
		// If there is no intersection, return the line on the plane closest to the cone (assumes cone_angle > pi/2)
		static void IntersectConePlane(Vector3d cone_direction, double cone_angle, Vector3d plane_normal, out Vector3d intersect_1, out Vector3d intersect_2)
		{
			intersect_1 = Vector3d.zero;
			intersect_2 = Vector3d.zero;

			cone_direction.Normalize();
			plane_normal.Normalize();

			// Change the frame of reference:
			// x = cone_direction
			// y = plane_normal projected on the plane normal to cone_direction
			// z = x * y
			Vector3d x = cone_direction;
			Vector3d z = Vector3d.Cross(x, plane_normal).normalized;
			Vector3d y = Vector3d.Cross(z, x);

			// transition matrix from the temporary frame to the global frame
			Matrix3x3f M_i_tmp = new Matrix3x3f();
			for (int i = 0; i < 3; i++)
			{
				M_i_tmp[i, 0] = (float)x[i];
				M_i_tmp[i, 1] = (float)y[i];
				M_i_tmp[i, 2] = (float)z[i];
			}

			Vector3d n = M_i_tmp.transpose() * plane_normal;
			double cos_phi = -n.x / (n.y * Math.Tan(cone_angle));

			if (Math.Abs(cos_phi) <= 1.0f)
			{
				intersect_1.x = Math.Cos(cone_angle);
				intersect_1.y = Math.Sin(cone_angle) * cos_phi;
				intersect_1.z = Math.Sin(cone_angle) * Math.Sqrt(1 - cos_phi * cos_phi);
			}
			else
			{
				intersect_1.x = -n.y;
				intersect_1.y = n.x;
				intersect_1.z = 0.0;
			}

			intersect_2.x = intersect_1.x;
			intersect_2.y = intersect_1.y;
			intersect_2.z = -intersect_1.z;

			intersect_1 = M_i_tmp * intersect_1;
			intersect_2 = M_i_tmp * intersect_2;
		}

		static double AngleAboutAxis(Vector3d v1, Vector3d v2, Vector3d axis)
		{
			axis.Normalize();

			v1 = (v1 - Vector3d.Dot(axis, v1) * axis).normalized;
			v2 = (v2 - Vector3d.Dot(axis, v2) * axis).normalized;

			return Math.Atan2(Vector3d.Dot(axis, Vector3d.Cross(v1, v2)), Vector3d.Dot(v1, v2));
		}

		static ManeuverParameters ComputeEjectionManeuver(Vector3d exit_velocity, Orbit initial_orbit, double UT_0)
		{
			double GM = initial_orbit.referenceBody.gravParameter;
			double C3 = exit_velocity.sqrMagnitude;
			double sma = -GM / C3;
			double Rpe = initial_orbit.semiMajorAxis;
			double ecc = 1 - Rpe / sma;
			double theta = Math.Acos(-1 / ecc);

			Vector3d intersect_1;
			Vector3d intersect_2;

			// intersect_1 is the optimal position on the orbit assuming the orbit is circular and the sphere of influence is infinite
			IntersectConePlane(exit_velocity, theta, initial_orbit.h, out intersect_1, out intersect_2);

			Vector3d eccvec = initial_orbit.GetEccVector();

			double true_anomaly = AngleAboutAxis(eccvec, intersect_1, initial_orbit.h);
			// GetMeanAnomalyAtTrueAnomaly expects degrees and returns radians
			double mean_anomaly = initial_orbit.GetMeanAnomalyAtTrueAnomaly(true_anomaly * 180 / Math.PI);

			double delta_mean_anomaly = MuUtils.ClampRadiansPi(mean_anomaly - initial_orbit.MeanAnomalyAtUT(UT_0));

			double UT = UT_0 + delta_mean_anomaly / initial_orbit.MeanMotion();

			Vector3d V_0 = initial_orbit.getOrbitalVelocityAtUT(UT);
			Vector3d pos = initial_orbit.getRelativePositionAtUT(UT);

			double final_vel = Math.Sqrt(C3 + 2 * GM / pos.magnitude);

			Vector3d x = pos.normalized;
			Vector3d z = Vector3d.Cross(x, exit_velocity).normalized;
			Vector3d y = Vector3d.Cross(z, x);

			theta = Math.PI / 2;
			double dtheta = 0.001;
			Orbit sample = new Orbit();

			double theta_err = Double.MaxValue;

			for (int iteration = 0 ; iteration < 50 ; ++iteration)
			{
				Vector3d V_1 = final_vel * (Math.Cos(theta) * x + Math.Sin(theta) * y);
				sample.UpdateFromStateVectors(pos, V_1, initial_orbit.referenceBody, UT);
				theta_err = AngleAboutAxis(exit_velocity, sample.getOrbitalVelocityAtUT(OrbitExtensions.NextTimeOfRadius(sample, UT, sample.referenceBody.sphereOfInfluence)), z);
				if (double.IsNaN(theta_err))
					return null;
				if (Math.Abs(theta_err) <= Math.PI / 1800)
					return new ManeuverParameters((V_1 - V_0).xzy, UT);

				V_1 = final_vel * (Math.Cos(theta + dtheta) * x + Math.Sin(theta + dtheta) * y);
				sample.UpdateFromStateVectors(pos, V_1, initial_orbit.referenceBody, UT);
				double theta_err_2 = AngleAboutAxis(exit_velocity, sample.getOrbitalVelocityAtUT(OrbitExtensions.NextTimeOfRadius(sample, UT, sample.referenceBody.sphereOfInfluence)), z);

				V_1 = final_vel * (Math.Cos(theta - dtheta) * x + Math.Sin(theta - dtheta) * y);
				sample.UpdateFromStateVectors(pos, V_1, initial_orbit.referenceBody, UT);
				double theta_err_3 = AngleAboutAxis(exit_velocity, sample.getOrbitalVelocityAtUT(OrbitExtensions.NextTimeOfRadius(sample, UT, sample.referenceBody.sphereOfInfluence)), z);

				double derr = MuUtils.ClampRadiansPi(theta_err_2 - theta_err_3) / (2 * dtheta);

				theta = MuUtils.ClampRadiansTwoPi(theta - theta_err / derr);

				// if theta > pi, replace with 2pi - theta, the function ejection_angle=f(theta) is symmetrc wrt theta=pi
				if (theta > Math.PI)
					theta = 2 * Math.PI - theta;
			}

			return null;
		}

		class OptimizerData
		{
			public Orbit initial_orbit;
			public double original_UT;
			public double UT_arrival;
			public Vector3d pos_arrival;
		}

		static void DistanceToTarget(double[] x, double[] fi, object obj)
		{
			OptimizerData data = (OptimizerData)obj;

			double t = data.original_UT + x[3];
			Vector3d DV = new Vector3d(x[0], x[1], x[2]);

			Orbit orbit = new Orbit();
			orbit.UpdateFromStateVectors(data.initial_orbit.getRelativePositionAtUT(t), data.initial_orbit.getOrbitalVelocityAtUT(t) + DV.xzy, data.initial_orbit.referenceBody, t);
			orbit.StartUT = t;

			var pars = new PatchedConics.SolverParameters();
			Vector3d pos;

			while(true)
			{
				Orbit next_orbit = new Orbit();
				PatchedConics.CalculatePatch(orbit, next_orbit, orbit.StartUT, pars, null);

				if (orbit.EndUT > data.UT_arrival)
				{
					pos = orbit.getTruePositionAtUT(data.UT_arrival);
					Vector3d err = pos - data.pos_arrival;

					fi[0] = err.x;
					fi[1] = err.y;
					fi[2] = err.z;
					return;
				}

				// As of 0.25 CalculatePatch fails if the orbit does not change SoI
				if (next_orbit.referenceBody == null)
				{
					next_orbit.UpdateFromOrbitAtUT(orbit, orbit.StartUT + orbit.period, orbit.referenceBody);
				}
				orbit = next_orbit;
			}
		}

		public static ManeuverParameters OptimizeEjection(ManeuverParameters original_maneuver, Orbit initial_orbit, Orbit target, double UT_arrival, double earliest_UT)
		{
			while(true)
			{
				alglib.minlmstate state;
				alglib.minlmreport rep;

				double[] x = new double[4];
				double[] scale = new double[4];

				x[0] = original_maneuver.dV.x;
				x[1] = original_maneuver.dV.y;
				x[2] = original_maneuver.dV.z;
				x[3] = 0;

				scale[0] = scale[1] = scale[2] = original_maneuver.dV.magnitude;
				scale[3] = initial_orbit.period;

				OptimizerData data = new OptimizerData();
				data.initial_orbit = initial_orbit;
				data.original_UT = original_maneuver.UT;
				data.UT_arrival = UT_arrival;
				data.pos_arrival = target.getTruePositionAtUT(UT_arrival);

				alglib.minlmcreatev(4, x, 0.01, out state);
				double epsx = 1e-4; // stop if |(x(k+1)-x(k)) ./ scale| <= EpsX
				double epsf = 0.01; // stop if |F(k+1)-F(k)| <= EpsF*max{|F(k)|,|F(k+1)|,1}
				alglib.minlmsetcond(state, 0, epsf, epsx, 50);
				alglib.minlmsetscale(state, scale);
				alglib.minlmoptimize(state, DistanceToTarget, null, data);
				alglib.minlmresults(state, out x, out rep);

				Debug.Log("Transfer calculator: termination type=" + rep.terminationtype);
				Debug.Log("Transfer calculator: iteration count=" + rep.iterationscount);

				// try again in one orbit if the maneuver node is in the past
				if (original_maneuver.UT + x[3] < earliest_UT)
				{
					Debug.Log("Transfer calculator: maneuver is " + (earliest_UT - original_maneuver.UT - x[3]) + " s too early, trying again in " + initial_orbit.period + " s");
					original_maneuver.UT += initial_orbit.period;
				}
				else
					return new ManeuverParameters(new Vector3d(x[0], x[1], x[2]), original_maneuver.UT + x[3]);
			}
		}
	}

	public class AllGraphTransferCalculator : TransferCalculator
	{
		public AllGraphTransferCalculator(
			Orbit o, Orbit target,
			double min_departure_time,
			double max_departure_time,
			double min_transfer_time,
			double max_transfer_time,
			int width,
			int height) : base(o, target, min_departure_time, max_departure_time, min_transfer_time, max_transfer_time, width, height)
		{
			StartThreads();
		}

		public override int DurationSamplesForDate(int date_index)
		{
			return maxDurationSamples;
		}

		public override int Progress { get { return Math.Min(100, (int)(100 * (1 - (double)nextDateIndex / dateSamples))); } }
	}
}

