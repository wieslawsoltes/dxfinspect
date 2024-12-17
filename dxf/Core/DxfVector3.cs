
namespace Dxf;

/// <summary>
/// 
/// </summary>
public class DxfVector3
{
    /// <summary>
    /// 
    /// </summary>
    public double X { get; private set; }
    /// <summary>
    /// 
    /// </summary>
    public double Y { get; private set; }
    /// <summary>
    /// 
    /// </summary>
    public double Z { get; private set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    public DxfVector3(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
    }
}
