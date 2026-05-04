using KspNavComputer.Core.Bodies;

namespace KspNavComputer.Core.Porkchop;

/// <summary>
/// Input parameters for <see cref="PorkchopComputer.AutoTofRange"/>.
/// </summary>
public record AutoTofRangeParameters(
    CelestialBody Origin,
    CelestialBody Destination,
    double Mu
);
