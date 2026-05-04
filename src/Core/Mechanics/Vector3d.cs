namespace KspNavComputer.Core.Mechanics;

/// <summary>
/// Minimal double-precision 3-D vector. Used throughout the orbital mechanics layer.
/// </summary>
public readonly record struct Vector3d(double X, double Y, double Z)
{
    public static readonly Vector3d Zero = new(0, 0, 0);

    public double MagnitudeSq => X * X + Y * Y + Z * Z;
    public double Magnitude   => Math.Sqrt(MagnitudeSq);

    public Vector3d Normalize()
    {
        double m = Magnitude;
        if (m < double.Epsilon) return Zero;
        return new(X / m, Y / m, Z / m);
    }

    public static Vector3d operator +(Vector3d a, Vector3d b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    public static Vector3d operator -(Vector3d a, Vector3d b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    public static Vector3d operator *(Vector3d v, double s)   => new(v.X * s,   v.Y * s,   v.Z * s);
    public static Vector3d operator *(double s,   Vector3d v) => v * s;
    public static Vector3d operator /(Vector3d v, double s)   => new(v.X / s,   v.Y / s,   v.Z / s);
    public static Vector3d operator -(Vector3d v)             => new(-v.X,      -v.Y,       -v.Z);

    public static double Dot(Vector3d a, Vector3d b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;

    public static Vector3d Cross(Vector3d a, Vector3d b) => new(
        a.Y * b.Z - a.Z * b.Y,
        a.Z * b.X - a.X * b.Z,
        a.X * b.Y - a.Y * b.X
    );
}
