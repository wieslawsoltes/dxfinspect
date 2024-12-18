﻿
namespace Dxf;

/// <summary>
/// Group code: 281
/// </summary>
public enum DxfViewRenderMode : byte
{
    /// <summary>
    /// 
    /// </summary>
    Optimized2D = 0,
    /// <summary>
    /// 
    /// </summary>
    Wireframe = 1,
    /// <summary>
    /// 
    /// </summary>
    HiddenLine = 2,
    /// <summary>
    /// 
    /// </summary>
    FlatShaded= 3,
    /// <summary>
    /// 
    /// </summary>
    GouraudShaded = 4,
    /// <summary>
    /// 
    /// </summary>
    FlatShadedWithWireframe = 5,
    /// <summary>
    /// 
    /// </summary>
    GouraudShadedWithWireframe = 6
}
