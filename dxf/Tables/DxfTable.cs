
namespace Dxf;

/// <summary>
/// 
/// </summary>
/// <typeparam name="T"></typeparam>
public class DxfTable<T>
{
    /// <summary>
    /// 
    /// </summary>
    public int Id { get; set; }
    /// <summary>
    /// 
    /// </summary>
    public IList<T> Items { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public DxfTable()
    {
        Items = new List<T>();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    public DxfTable(int id)
        : this()
    {
        Id = id;
    }
}
