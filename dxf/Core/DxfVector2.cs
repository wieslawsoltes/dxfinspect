
namespace Dxf;

/// <summary>
/// 
/// </summary>
public class DxfVector2
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
    /// <param name="x"></param>
    /// <param name="y"></param>
    public DxfVector2(double x, double y)
    {
        X = x;
        Y = y;
    }
}
