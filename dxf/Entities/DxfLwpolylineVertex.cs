
namespace Dxf;

/// <summary>
/// 
/// </summary>
public class DxfLwpolylineVertex
{
    /// <summary>
    /// 
    /// </summary>
    public DxfVector2 Coordinates { get; private set; }
    /// <summary>
    /// 
    /// </summary>
    public double StartWidth { get; private set; }
    /// <summary>
    /// 
    /// </summary>
    public double EndWidth { get; private set; }
    /// <summary>
    /// 
    /// </summary>
    public double Bulge { get; private set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="coordinates"></param>
    /// <param name="startWidth"></param>
    /// <param name="endWidth"></param>
    /// <param name="bulge"></param>
    public DxfLwpolylineVertex(
        DxfVector2 coordinates, 
        double startWidth, 
        double endWidth, 
        double bulge)
    {
        Coordinates = coordinates;
        StartWidth = startWidth;
        EndWidth = endWidth;
        Bulge = bulge;
    }
}
