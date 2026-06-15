namespace CoreAlign.Domain.Entities.GlassEnclosure;

public class RunGeometry3D
{
    public int? Z { get; private set; }
    public decimal? TiltDeg { get; private set; }
    public int? ArcRadiusMm { get; private set; }
    public decimal? ArcSweepDeg { get; private set; }

    protected RunGeometry3D() { }

    public RunGeometry3D(int? z, decimal? tiltDeg, int? arcRadiusMm, decimal? arcSweepDeg)
    {
        Z = z;
        TiltDeg = tiltDeg;
        ArcRadiusMm = arcRadiusMm;
        ArcSweepDeg = arcSweepDeg;
    }

    public static RunGeometry3D Empty => new();
}
