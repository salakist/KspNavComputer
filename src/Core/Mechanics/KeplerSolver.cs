using KspNavComputer.Core.Bodies;

namespace KspNavComputer.Core.Mechanics;

/// <summary>
/// Keplerian orbit propagation: solves Kepler's equation and converts orbital
/// elements to Cartesian state vectors (position + velocity) in the central-body
/// inertial frame (x toward vernal equinox, z toward celestial north).
/// </summary>
public static class KeplerSolver
{
    private const double Tolerance = 1e-10;
    private const int    MaxIterations = 100;

    // -------------------------------------------------------------------------
    // Kepler's equation solvers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Solves Kepler's equation M = E - e·sin(E) for eccentric anomaly E,
    /// using Newton-Raphson iteration.
    /// </summary>
    public static double SolveEccentricAnomaly(double meanAnomaly, double eccentricity)
    {
        // Normalise M to [0, 2π)
        double M = meanAnomaly % (2.0 * Math.PI);
        if (M < 0) M += 2.0 * Math.PI;

        // Initial guess
        double E = M < Math.PI ? M + eccentricity / 2.0 : M - eccentricity / 2.0;

        for (int i = 0; i < MaxIterations; i++)
        {
            double dE = (M - E + eccentricity * Math.Sin(E)) / (1.0 - eccentricity * Math.Cos(E));
            E += dE;
            if (Math.Abs(dE) < Tolerance) break;
        }
        return E;
    }

    /// <summary>
    /// Converts eccentric anomaly E to true anomaly ν.
    /// </summary>
    public static double TrueAnomalyFromEccentric(double E, double eccentricity)
    {
        double cosE = Math.Cos(E);
        double sinE = Math.Sin(E);
        return Math.Atan2(
            Math.Sqrt(1.0 - eccentricity * eccentricity) * sinE,
            cosE - eccentricity
        );
    }

    // -------------------------------------------------------------------------
    // State vector computation
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns the position (m) in the orbital plane (perifocal frame:
    /// x toward periapsis, y 90° ahead in orbit direction).
    /// </summary>
    public static Vector3d PositionInOrbitalPlane(OrbitalElements orbit, double trueAnomaly)
    {
        double e   = orbit.Eccentricity;
        double a   = orbit.SemiMajorAxis;
        double nu  = trueAnomaly;
        double p   = a * (1.0 - e * e);           // semi-latus rectum
        double r   = p / (1.0 + e * Math.Cos(nu));
        return new Vector3d(r * Math.Cos(nu), r * Math.Sin(nu), 0.0);
    }

    /// <summary>
    /// Returns the velocity (m/s) in the orbital plane (perifocal frame).
    /// </summary>
    public static Vector3d VelocityInOrbitalPlane(OrbitalElements orbit, double trueAnomaly, double gravParam)
    {
        double e  = orbit.Eccentricity;
        double a  = orbit.SemiMajorAxis;
        double nu = trueAnomaly;
        double p  = a * (1.0 - e * e);
        double h  = Math.Sqrt(gravParam * p);      // specific angular momentum
        double coeff = gravParam / h;
        return new Vector3d(
            -coeff * Math.Sin(nu),
             coeff * (e + Math.Cos(nu)),
             0.0
        );
    }

    /// <summary>
    /// Returns the full Cartesian state (position m, velocity m/s) in the
    /// central-body inertial frame at the given Universal Time.
    /// </summary>
    public static (Vector3d Position, Vector3d Velocity) StateAt(
        OrbitalElements orbit, double gravParam, double ut)
    {
        double n  = Math.Sqrt(gravParam / Math.Pow(orbit.SemiMajorAxis, 3)); // mean motion
        double M  = orbit.MeanAnomalyAtEpoch + n * (ut - orbit.Epoch);
        double E  = SolveEccentricAnomaly(M, orbit.Eccentricity);
        double nu = TrueAnomalyFromEccentric(E, orbit.Eccentricity);

        Vector3d rPerif = PositionInOrbitalPlane(orbit,  nu);
        Vector3d vPerif = VelocityInOrbitalPlane(orbit, nu, gravParam);

        // Rotate perifocal → inertial:  R_z(-Ω) · R_x(-i) · R_z(-ω)
        double omega  = orbit.ArgumentOfPeriapsis;
        double inc    = orbit.Inclination;
        double Omega  = orbit.LongitudeOfAscendingNode;

        return (
            RotatePerifocalToInertial(rPerif, omega, inc, Omega),
            RotatePerifocalToInertial(vPerif, omega, inc, Omega)
        );
    }

    // -------------------------------------------------------------------------
    // Rotation helper
    // -------------------------------------------------------------------------

    private static Vector3d RotatePerifocalToInertial(
        Vector3d v, double omega, double inc, double Omega)
    {
        double cosO = Math.Cos(Omega), sinO = Math.Sin(Omega);
        double coso = Math.Cos(omega), sino = Math.Sin(omega);
        double cosi = Math.Cos(inc),   sini = Math.Sin(inc);

        // Standard perifocal-to-inertial rotation matrix (row-by-row)
        double r11 =  cosO * coso - sinO * sino * cosi;
        double r12 = -cosO * sino - sinO * coso * cosi;
        double r13 =  sinO * sini;
        double r21 =  sinO * coso + cosO * sino * cosi;
        double r22 = -sinO * sino + cosO * coso * cosi;
        double r23 = -cosO * sini;
        double r31 =  sino * sini;
        double r32 =  coso * sini;
        double r33 =  cosi;

        return new Vector3d(
            r11 * v.X + r12 * v.Y + r13 * v.Z,
            r21 * v.X + r22 * v.Y + r23 * v.Z,
            r31 * v.X + r32 * v.Y + r33 * v.Z
        );
    }
}
