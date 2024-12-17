
namespace Dxf;

/// <summary>
/// 
/// </summary>
public class Dxf3Dsolid : DxfObject
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="version"></param>
    /// <param name="id"></param>
    public Dxf3Dsolid(DxfAcadVer version, int id)
        : base(version, id)
    {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public override string Create()
    {
        throw new NotImplementedException();
    }
}
