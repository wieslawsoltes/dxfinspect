
namespace Dxf;

/// <summary>
/// 
/// </summary>
public class DxfClasses : DxfObject
{
    /// <summary>
    /// 
    /// </summary>
    public IList<DxfClass> Classes { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="version"></param>
    /// <param name="id"></param>
    public DxfClasses(DxfAcadVer version, int id)
        : base(version, id)
    {
        Classes = new List<DxfClass>();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public override string Create()
    {
        Reset();

        Add(0, DxfCodeName.Section);
        Add(2, "CLASSES");

        foreach(var cls in Classes)
        {
            Append(cls.Create());
        }

        Add(0, DxfCodeName.EndSec);

        return Build();
    }
}
