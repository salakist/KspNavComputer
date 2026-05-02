namespace KspNavComputer.Core.Bodies;

/// <summary>
/// Static registry of all 17 KSP 1.12.x stock celestial bodies.
/// Data sourced from KSP wiki / in-game orbital parameters.
/// All values SI: metres, seconds, m³/s².
/// Orbital elements referenced to the Kerbol inertial frame at epoch UT=0.
/// </summary>
public static class BodyDatabase
{
    // -------------------------------------------------------------------------
    // Kerbol (central star)
    // -------------------------------------------------------------------------
    public static readonly CelestialBody Kerbol = new(
        Name:                    "Kerbol",
        GravParam:               1.1723328e18,
        Radius:                  261_600_000,
        SphereOfInfluence:       double.PositiveInfinity,
        SiderealRotationPeriod:  432_000,
        Parent:                  null,
        Orbit:                   null
    );

    // -------------------------------------------------------------------------
    // Moho
    // -------------------------------------------------------------------------
    public static readonly CelestialBody Moho = new(
        Name:                    "Moho",
        GravParam:               1.6860938e11,
        Radius:                  250_000,
        SphereOfInfluence:       9_646_663,
        SiderealRotationPeriod:  1_210_000,
        Parent:                  Kerbol,
        Orbit: new OrbitalElements(
            SemiMajorAxis:               5_263_138_304,
            Eccentricity:                0.2,
            Inclination:                 7.0 * Math.PI / 180.0,
            LongitudeOfAscendingNode:    70.0 * Math.PI / 180.0,
            ArgumentOfPeriapsis:         15.0 * Math.PI / 180.0,
            MeanAnomalyAtEpoch:          3.14,
            Epoch:                       0
        )
    );

    // -------------------------------------------------------------------------
    // Eve
    // -------------------------------------------------------------------------
    public static readonly CelestialBody Eve = new(
        Name:                    "Eve",
        GravParam:               8.1717302e12,
        Radius:                  700_000,
        SphereOfInfluence:       85_109_365,
        SiderealRotationPeriod:  80_500,
        Parent:                  Kerbol,
        Orbit: new OrbitalElements(
            SemiMajorAxis:               9_832_684_544,
            Eccentricity:                0.01,
            Inclination:                 2.1 * Math.PI / 180.0,
            LongitudeOfAscendingNode:    15.0 * Math.PI / 180.0,
            ArgumentOfPeriapsis:         0.0,
            MeanAnomalyAtEpoch:          3.14,
            Epoch:                       0
        )
    );

    // -------------------------------------------------------------------------
    // Gilly  (moon of Eve)
    // -------------------------------------------------------------------------
    public static readonly CelestialBody Gilly = new(
        Name:                    "Gilly",
        GravParam:               8_289_449.8,
        Radius:                  13_000,
        SphereOfInfluence:       126_123.27,
        SiderealRotationPeriod:  28_255,
        Parent:                  Eve,
        Orbit: new OrbitalElements(
            SemiMajorAxis:               31_500_000,
            Eccentricity:                0.55,
            Inclination:                 12.0 * Math.PI / 180.0,
            LongitudeOfAscendingNode:    80.0 * Math.PI / 180.0,
            ArgumentOfPeriapsis:         10.0 * Math.PI / 180.0,
            MeanAnomalyAtEpoch:          0.9,
            Epoch:                       0
        )
    );

    // -------------------------------------------------------------------------
    // Kerbin
    // -------------------------------------------------------------------------
    public static readonly CelestialBody Kerbin = new(
        Name:                    "Kerbin",
        GravParam:               3.5316000e12,
        Radius:                  600_000,
        SphereOfInfluence:       84_159_286,
        SiderealRotationPeriod:  21_549.425,
        Parent:                  Kerbol,
        Orbit: new OrbitalElements(
            SemiMajorAxis:               13_599_840_256,
            Eccentricity:                0.0,
            Inclination:                 0.0,
            LongitudeOfAscendingNode:    0.0,
            ArgumentOfPeriapsis:         0.0,
            MeanAnomalyAtEpoch:          3.14,
            Epoch:                       0
        )
    );

    // -------------------------------------------------------------------------
    // Mun  (moon of Kerbin)
    // -------------------------------------------------------------------------
    public static readonly CelestialBody Mun = new(
        Name:                    "Mun",
        GravParam:               6.5138398e10,
        Radius:                  200_000,
        SphereOfInfluence:       2_429_559.1,
        SiderealRotationPeriod:  138_984.38,
        Parent:                  Kerbin,
        Orbit: new OrbitalElements(
            SemiMajorAxis:               12_000_000,
            Eccentricity:                0.0,
            Inclination:                 0.0,
            LongitudeOfAscendingNode:    0.0,
            ArgumentOfPeriapsis:         0.0,
            MeanAnomalyAtEpoch:          1.7,
            Epoch:                       0
        )
    );

    // -------------------------------------------------------------------------
    // Minmus  (moon of Kerbin)
    // -------------------------------------------------------------------------
    public static readonly CelestialBody Minmus = new(
        Name:                    "Minmus",
        GravParam:               1_765_800,
        Radius:                  60_000,
        SphereOfInfluence:       2_247_428.4,
        SiderealRotationPeriod:  40_400,
        Parent:                  Kerbin,
        Orbit: new OrbitalElements(
            SemiMajorAxis:               47_000_000,
            Eccentricity:                0.0,
            Inclination:                 6.0 * Math.PI / 180.0,
            LongitudeOfAscendingNode:    78.0 * Math.PI / 180.0,
            ArgumentOfPeriapsis:         38.0 * Math.PI / 180.0,
            MeanAnomalyAtEpoch:          0.9,
            Epoch:                       0
        )
    );

    // -------------------------------------------------------------------------
    // Duna
    // -------------------------------------------------------------------------
    public static readonly CelestialBody Duna = new(
        Name:                    "Duna",
        GravParam:               3.0136321e11,
        Radius:                  320_000,
        SphereOfInfluence:       47_921_949,
        SiderealRotationPeriod:  65_517.859,
        Parent:                  Kerbol,
        Orbit: new OrbitalElements(
            SemiMajorAxis:               20_726_155_264,
            Eccentricity:                0.051,
            Inclination:                 0.06 * Math.PI / 180.0,
            LongitudeOfAscendingNode:    135.5 * Math.PI / 180.0,
            ArgumentOfPeriapsis:         0.0,
            MeanAnomalyAtEpoch:          3.14,
            Epoch:                       0
        )
    );

    // -------------------------------------------------------------------------
    // Ike  (moon of Duna)
    // -------------------------------------------------------------------------
    public static readonly CelestialBody Ike = new(
        Name:                    "Ike",
        GravParam:               1.8568369e10,
        Radius:                  130_000,
        SphereOfInfluence:       1_049_598.9,
        SiderealRotationPeriod:  65_517.862,
        Parent:                  Duna,
        Orbit: new OrbitalElements(
            SemiMajorAxis:               3_200_000,
            Eccentricity:                0.03,
            Inclination:                 0.2 * Math.PI / 180.0,
            LongitudeOfAscendingNode:    0.0,
            ArgumentOfPeriapsis:         0.0,
            MeanAnomalyAtEpoch:          1.7,
            Epoch:                       0
        )
    );

    // -------------------------------------------------------------------------
    // Dres
    // -------------------------------------------------------------------------
    public static readonly CelestialBody Dres = new(
        Name:                    "Dres",
        GravParam:               2.1484489e10,
        Radius:                  138_000,
        SphereOfInfluence:       32_832_840,
        SiderealRotationPeriod:  34_800,
        Parent:                  Kerbol,
        Orbit: new OrbitalElements(
            SemiMajorAxis:               40_839_348_203,
            Eccentricity:                0.145,
            Inclination:                 5.0 * Math.PI / 180.0,
            LongitudeOfAscendingNode:    280.0 * Math.PI / 180.0,
            ArgumentOfPeriapsis:         90.0 * Math.PI / 180.0,
            MeanAnomalyAtEpoch:          3.14,
            Epoch:                       0
        )
    );

    // -------------------------------------------------------------------------
    // Jool
    // -------------------------------------------------------------------------
    public static readonly CelestialBody Jool = new(
        Name:                    "Jool",
        GravParam:               2.8252800e14,
        Radius:                  6_000_000,
        SphereOfInfluence:       2_455_985_185,
        SiderealRotationPeriod:  36_000,
        Parent:                  Kerbol,
        Orbit: new OrbitalElements(
            SemiMajorAxis:               68_773_560_320,
            Eccentricity:                0.05,
            Inclination:                 1.304 * Math.PI / 180.0,
            LongitudeOfAscendingNode:    52.0 * Math.PI / 180.0,
            ArgumentOfPeriapsis:         0.0,
            MeanAnomalyAtEpoch:          0.1,
            Epoch:                       0
        )
    );

    // -------------------------------------------------------------------------
    // Laythe  (moon of Jool)
    // -------------------------------------------------------------------------
    public static readonly CelestialBody Laythe = new(
        Name:                    "Laythe",
        GravParam:               1.9620000e12,
        Radius:                  500_000,
        SphereOfInfluence:       3_723_645.8,
        SiderealRotationPeriod:  52_980.879,
        Parent:                  Jool,
        Orbit: new OrbitalElements(
            SemiMajorAxis:               27_184_000,
            Eccentricity:                0.0,
            Inclination:                 0.0,
            LongitudeOfAscendingNode:    0.0,
            ArgumentOfPeriapsis:         0.0,
            MeanAnomalyAtEpoch:          3.14,
            Epoch:                       0
        )
    );

    // -------------------------------------------------------------------------
    // Vall  (moon of Jool)
    // -------------------------------------------------------------------------
    public static readonly CelestialBody Vall = new(
        Name:                    "Vall",
        GravParam:               2.0748150e11,
        Radius:                  300_000,
        SphereOfInfluence:       2_406_401.4,
        SiderealRotationPeriod:  105_962.09,
        Parent:                  Jool,
        Orbit: new OrbitalElements(
            SemiMajorAxis:               43_152_000,
            Eccentricity:                0.0,
            Inclination:                 0.0,
            LongitudeOfAscendingNode:    0.0,
            ArgumentOfPeriapsis:         0.0,
            MeanAnomalyAtEpoch:          0.9,
            Epoch:                       0
        )
    );

    // -------------------------------------------------------------------------
    // Tylo  (moon of Jool)
    // -------------------------------------------------------------------------
    public static readonly CelestialBody Tylo = new(
        Name:                    "Tylo",
        GravParam:               2.8252800e12,
        Radius:                  600_000,
        SphereOfInfluence:       10_856_518,
        SiderealRotationPeriod:  211_926.36,
        Parent:                  Jool,
        Orbit: new OrbitalElements(
            SemiMajorAxis:               68_500_000,
            Eccentricity:                0.0,
            Inclination:                 0.025 * Math.PI / 180.0,
            LongitudeOfAscendingNode:    0.0,
            ArgumentOfPeriapsis:         0.0,
            MeanAnomalyAtEpoch:          3.14,
            Epoch:                       0
        )
    );

    // -------------------------------------------------------------------------
    // Bop  (moon of Jool)
    // -------------------------------------------------------------------------
    public static readonly CelestialBody Bop = new(
        Name:                    "Bop",
        GravParam:               2_486_834.9,
        Radius:                  65_000,
        SphereOfInfluence:       1_221_060.9,
        SiderealRotationPeriod:  544_507.43,
        Parent:                  Jool,
        Orbit: new OrbitalElements(
            SemiMajorAxis:               128_500_000,
            Eccentricity:                0.235,
            Inclination:                 15.0 * Math.PI / 180.0,
            LongitudeOfAscendingNode:    10.0 * Math.PI / 180.0,
            ArgumentOfPeriapsis:         25.0 * Math.PI / 180.0,
            MeanAnomalyAtEpoch:          0.9,
            Epoch:                       0
        )
    );

    // -------------------------------------------------------------------------
    // Pol  (moon of Jool)
    // -------------------------------------------------------------------------
    public static readonly CelestialBody Pol = new(
        Name:                    "Pol",
        GravParam:               721_702.08,
        Radius:                  44_000,
        SphereOfInfluence:       1_042_138.9,
        SiderealRotationPeriod:  901_902.62,
        Parent:                  Jool,
        Orbit: new OrbitalElements(
            SemiMajorAxis:               179_890_000,
            Eccentricity:                0.171,
            Inclination:                 4.25 * Math.PI / 180.0,
            LongitudeOfAscendingNode:    2.0 * Math.PI / 180.0,
            ArgumentOfPeriapsis:         15.0 * Math.PI / 180.0,
            MeanAnomalyAtEpoch:          0.9,
            Epoch:                       0
        )
    );

    // -------------------------------------------------------------------------
    // Eeloo
    // -------------------------------------------------------------------------
    public static readonly CelestialBody Eeloo = new(
        Name:                    "Eeloo",
        GravParam:               7.4410815e10,
        Radius:                  210_000,
        SphereOfInfluence:       119_082_940,
        SiderealRotationPeriod:  19_460,
        Parent:                  Kerbol,
        Orbit: new OrbitalElements(
            SemiMajorAxis:               90_118_820_000,
            Eccentricity:                0.26,
            Inclination:                 6.15 * Math.PI / 180.0,
            LongitudeOfAscendingNode:    50.0 * Math.PI / 180.0,
            ArgumentOfPeriapsis:         260.0 * Math.PI / 180.0,
            MeanAnomalyAtEpoch:          3.14,
            Epoch:                       0
        )
    );

    // -------------------------------------------------------------------------
    // Lookup
    // -------------------------------------------------------------------------
    private static readonly IReadOnlyDictionary<string, CelestialBody> _all =
        new Dictionary<string, CelestialBody>(StringComparer.OrdinalIgnoreCase)
        {
            { "Kerbol",  Kerbol  },
            { "Moho",    Moho    },
            { "Eve",     Eve     },
            { "Gilly",   Gilly   },
            { "Kerbin",  Kerbin  },
            { "Mun",     Mun     },
            { "Minmus",  Minmus  },
            { "Duna",    Duna    },
            { "Ike",     Ike     },
            { "Dres",    Dres    },
            { "Jool",    Jool    },
            { "Laythe",  Laythe  },
            { "Vall",    Vall    },
            { "Tylo",    Tylo    },
            { "Bop",     Bop     },
            { "Pol",     Pol     },
            { "Eeloo",   Eeloo   },
        };

    public static IReadOnlyDictionary<string, CelestialBody> All => _all;

    public static CelestialBody Get(string name)
    {
        if (!_all.TryGetValue(name, out var body))
            throw new ArgumentException($"Unknown body: '{name}'.");
        return body;
    }
}
