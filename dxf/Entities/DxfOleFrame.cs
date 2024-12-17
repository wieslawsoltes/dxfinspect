
namespace Dxf;

/// <summary>
/// 
/// </summary>
public class DxfOleFrame : DxfObject
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="version"></param>
    /// <param name="id"></param>
    public DxfOleFrame(DxfAcadVer version, int id)
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
