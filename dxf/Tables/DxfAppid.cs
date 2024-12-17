
namespace Dxf;

/// <summary>
/// 
/// </summary>
public class DxfAppid : DxfObject
{
    /// <summary>
    /// 
    /// </summary>
    public string ApplicationName { get; set; }
    /// <summary>
    /// 
    /// </summary>
    public DxfAppidStandardFlags AppidStandardFlags { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="version"></param>
    /// <param name="id"></param>
    public DxfAppid(DxfAcadVer version, int id)
        : base(version, id)
    {
    }

    /// <summary>
    /// 
    /// </summary>
    public void Defaults()
    {
        ApplicationName = string.Empty;
        AppidStandardFlags = DxfAppidStandardFlags.Default;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public override string Create()
    {
        Reset();

        Add(0, DxfCodeName.AppId);

        if (Version > DxfAcadVer.AC1009)
        {
            Handle(Id);
            Subclass(DxfSubclassMarker.SymbolTableRecord);
            Subclass(DxfSubclassMarker.RegAppTableRecord);
        }

        Add(2, ApplicationName);
        Add(70, (int)AppidStandardFlags);

        return Build();
    }
}
