using KspNavComputer.Core.Bodies;
using KspNavComputer.Core.Mechanics;

namespace KspNavComputer.Core.Transfer;

/// <summary>
/// Computes a porkchop (Δv / departure-date × TOF) grid.
///
/// The TOF range is auto-computed from the Hohmann transfer time using the
/// same formula as alexmoon's LWP:
///   hohmannTof = π · √(a³/μ)  where a = (rOrigin + rDest) / 2
///   minTof     = max(hohmannTof − T_dest, hohmannTof / 2)
///   maxTof     = minTof + min(2 · T_dest, hohmannTof)
///
/// Each grid cell calls <see cref="TransferComputer.Compute"/>.  Cells that
/// raise an exception or produce NaN/Infinity are stored as NaN.
/// </summary>
public static class PorkchopComputer
{
    // -------------------------------------------------------------------------
    // TOF range auto-computation
    // -------------------------------------------------------------------------

    /// <summary>
    /// Derives the min/max time-of-flight range for a porkchop grid using the
    /// same Hohmann-based heuristic as LWP.
    /// </summary>
    public static (double MinTof, double MaxTof) AutoTofRange(
        CelestialBody origin, CelestialBody destination, double mu)
    {
        double rOrigin = origin.Orbit!.SemiMajorAxis;
        double rDest   = destination.Orbit!.SemiMajorAxis;

        double hohmannA   = (rOrigin + rDest) / 2.0;
        double hohmannTof = Math.PI * Math.Sqrt(hohmannA * hohmannA * hohmannA / mu);

        // Destination's orbital period.
        double destPeriod = 2.0 * Math.PI
                          * Math.Sqrt(rDest * rDest * rDest / mu);

        double minTof = Math.Max(hohmannTof - destPeriod, hohmannTof / 2.0);
        double maxTof = minTof + Math.Min(2.0 * destPeriod, hohmannTof);

        return (minTof, maxTof);
    }

    // -------------------------------------------------------------------------
    // Grid computation
    // -------------------------------------------------------------------------

    /// <summary>
    /// Computes the Δv grid.  Each cell is the minimum total Δv for the
    /// corresponding (departure date, TOF) combination.
    /// </summary>
    public static PorkchopResult Compute(PorkchopParameters p)
    {
        if (p.Origin.Parent is null)
            throw new ArgumentException("Origin must orbit a central body.");

        double mu = p.Origin.Parent.GravParam;
        var (minTof, maxTof) = AutoTofRange(p.Origin, p.Destination, mu);

        int rows = p.GridRows;
        int cols = p.GridCols;
        var deltaVs = new double[rows * cols];

        // Build a canonical destination parking orbit for insertion burns.
        ParkingOrbit destinationOrbit = p.DestinationOrbit
            ?? new ParkingOrbit(Altitude: 0); // altitude 0 → won't be used

        double minDv     = double.MaxValue;
        double maxDv     = double.MinValue;
        int    optRow    = 0;
        int    optCol    = 0;

        double logSum   = 0.0;
        double logSumSq = 0.0;
        int    validN   = 0;

        double depRange = p.LatestDeparture - p.EarliestDeparture;
        double tofRange = maxTof - minTof;

        for (int row = 0; row < rows; row++)
        {
            double tof = minTof + (tofRange * row) / Math.Max(rows - 1, 1);

            for (int col = 0; col < cols; col++)
            {
                double depUT = p.EarliestDeparture
                             + (depRange * col) / Math.Max(cols - 1, 1);

                double dv;
                try
                {
                    var transferParams = new TransferParameters(
                        Origin:           p.Origin,
                        Destination:      p.Destination,
                        DepartureUT:      depUT,
                        TimeOfFlight:     tof,
                        OriginOrbit:      p.OriginOrbit,
                        DestinationOrbit: destinationOrbit,
                        TransferType:     p.TransferType,
                        NoInsertionBurn:  p.DestinationOrbit is null
                    );
                    var result = TransferComputer.Compute(transferParams);
                    dv = result.TotalDeltaV;

                    if (double.IsNaN(dv) || double.IsInfinity(dv) || dv <= 0.0)
                        dv = double.NaN;
                }
                catch
                {
                    dv = double.NaN;
                }

                deltaVs[row * cols + col] = dv;

                if (!double.IsNaN(dv))
                {
                    if (dv < minDv) { minDv = dv; optRow = row; optCol = col; }
                    if (dv > maxDv)   maxDv = dv;

                    double logDv = Math.Log(dv);
                    logSum   += logDv;
                    logSumSq += logDv * logDv;
                    validN++;
                }
            }
        }

        if (validN == 0)
        {
            minDv = 0; maxDv = 0;
        }

        double meanLog = validN > 0 ? logSum / validN : 0.0;
        double stdLog  = validN > 1
            ? Math.Sqrt(logSumSq / validN - meanLog * meanLog)
            : 0.0;

        return new PorkchopResult(
            DeltaVs:           deltaVs,
            Rows:              rows,
            Cols:              cols,
            EarliestDeparture: p.EarliestDeparture,
            LatestDeparture:   p.LatestDeparture,
            MinTof:            minTof,
            MaxTof:            maxTof,
            MinDeltaV:         minDv == double.MaxValue ? 0.0 : minDv,
            MaxDeltaV:         maxDv == double.MinValue ? 0.0 : maxDv,
            MeanLogDeltaV:     meanLog,
            StdLogDeltaV:      stdLog,
            OptimalRow:        optRow,
            OptimalCol:        optCol
        );
    }
}
