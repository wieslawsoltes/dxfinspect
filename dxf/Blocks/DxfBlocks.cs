﻿
namespace Dxf;

/// <summary>
/// 
/// </summary>
public class DxfBlocks : DxfObject
{
    /// <summary>
    /// 
    /// </summary>
    public IList<DxfBlock> Blocks { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="version"></param>
    /// <param name="id"></param>
    public DxfBlocks(DxfAcadVer version, int id)
        : base(version, id)
    {
        Blocks = new List<DxfBlock>();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public override string Create()
    {
        Reset();

        Add(0, DxfCodeName.Section);
        Add(2, "BLOCKS");

        foreach (var block in Blocks)
        {
            Append(block.Create());
        }

        Add(0, DxfCodeName.EndSec);

        return Build();
    }
}
