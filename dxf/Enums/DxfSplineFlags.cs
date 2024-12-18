﻿
namespace Dxf;

/// <summary>
/// Group code: 70
/// </summary>
[Flags]
public enum DxfSplineFlags : int
{
    /// <summary>
    /// 
    /// </summary>
    ClosedSpline = 1,
    /// <summary>
    /// 
    /// </summary>
    PeriodicSpline = 2,
    /// <summary>
    /// 
    /// </summary>
    RationalSpline = 4,
    /// <summary>
    /// 
    /// </summary>
    Planar = 8,
    /// <summary>
    /// 
    /// </summary>
    Linear = 16
}
