using System;

namespace dxfInspect.Model
{
    /// <summary>
    /// Provides Description and Value Type lookup for DXF Group Codes.
    /// </summary>
    public static class DxfGroupCodeInfo
    {
        /// <summary>
        /// Returns a descriptive string for the given DXF group code, 
        /// based on the "DXF Group Codes in Numerical Order Reference."
        /// </summary>
        public static string GetDescription(int code)
        {
            // Negative codes (application codes)
            if (code == -5) return "APP: persistent reactor chain";
            if (code == -4) return "APP: conditional operator (used only with ssget)";
            if (code == -3) return "APP: extended data (XDATA) sentinel (fixed)";
            if (code == -2) return "APP: entity name reference (fixed)";
            if (code == -1) return "APP: entity name (transient, never saved) (fixed)";

            // 0 -> 9
            if (code == 0)  return "Entity type indicator (fixed)";
            if (code == 1)  return "Primary text value for an entity";
            if (code == 2)  return "Name (attribute tag, block name, etc.)";
            if (code == 3 || code == 4) return "Other text or name values";
            if (code == 5)  return "Entity handle (up to 16 hex digits) (fixed)";
            if (code == 6)  return "Linetype name (fixed)";
            if (code == 7)  return "Text style name (fixed)";
            if (code == 8)  return "Layer name (fixed)";
            if (code == 9)  return "Variable name identifier (HEADER section only)";

            // 10 -> 18
            if (code == 10) return "Primary point (X) for lines, circles, etc. (DXF)";
            if (code >= 11 && code <= 18)
                return "Other points (X value); context depends on entity";

            // 20, 30
            if (code == 20 || code == 30)
                return "Y or Z value of the primary point (DXF)";

            // 21 -> 28, 31 -> 37
            if ((code >= 21 && code <= 28) || (code >= 31 && code <= 37))
                return "Y or Z values of other points (DXF)";

            // 38, 39
            if (code == 38) return "Entity's elevation if nonzero";
            if (code == 39) return "Entity's thickness if nonzero (fixed)";

            // 40 -> 48
            if (code >= 40 && code <= 48)
                return "Double-precision float (e.g. text height, scale factors)";

            // 49
            if (code == 49)
                return "Repeated double-precision float (e.g. dash lengths in LTYPE)";

            // 50 -> 58
            if (code >= 50 && code <= 58)
                return "Angle data (degrees in DXF, radians in AutoLISP/ObjectARX)";

            // 60
            if (code == 60) return "Entity visibility flag (0=visible,1=invisible)";

            // 62
            if (code == 62) return "Color number (fixed)";

            // 66
            if (code == 66) return "\"Entities follow\" flag (fixed)";

            // 67
            if (code == 67) return "Space indicator—model or paper space (fixed)";

            // 68
            if (code == 68) return "APP: viewport on/off screen or isActive state";

            // 69
            if (code == 69) return "APP: viewport identification number";

            // 70 -> 78
            if (code >= 70 && code <= 78)
                return "Integer values (repeat counts, flag bits, modes)";

            // 90 -> 99
            if (code >= 90 && code <= 99)
                return "32-bit integer values";

            // 100
            if (code == 100)
                return "Subclass data marker (required for derived entity/object classes)";

            // 102
            if (code == 102)
                return "Control string ({...} or }), for application use";

            // 105
            if (code == 105)
                return "Object handle for DIMVAR symbol table entry";

            // 110,111,112
            if (code == 110) return "UCS origin (X); appears if code 72=1";
            if (code == 111) return "UCS X-axis (X); appears if code 72=1";
            if (code == 112) return "UCS Y-axis (X); appears if code 72=1";

            // 120 -> 122
            if (code >= 120 && code <= 122)
                return "UCS origin/X-axis/Y-axis (Y values) if code 72=1";

            // 130 -> 132
            if (code >= 130 && code <= 132)
                return "UCS origin/X-axis/Y-axis (Z values) if code 72=1";

            // 140 -> 149
            if (code >= 140 && code <= 149)
                return "Double-precision floats (points, elevation, DIMSTYLE, etc.)";

            // 170 -> 179
            if (code >= 170 && code <= 179)
                return "16-bit integer values (DIMSTYLE flags, etc.)";

            // 210
            if (code == 210) return "Extrusion direction (X) (fixed)";

            // 220, 230
            if (code == 220 || code == 230)
                return "Extrusion direction (Y or Z)";

            // 270 -> 279
            if (code >= 270 && code <= 279)
                return "16-bit integer values";

            // 280 -> 289
            if (code >= 280 && code <= 289)
                return "16-bit integer value";

            // 290 -> 299
            if (code >= 290 && code <= 299)
                return "Boolean flag values";

            // 300 -> 309
            if (code >= 300 && code <= 309)
                return "Arbitrary text strings";

            // 310 -> 319
            if (code >= 310 && code <= 319)
                return "Arbitrary binary chunks (hex string up to 254 chars)";

            // 320 -> 329
            if (code >= 320 && code <= 329)
                return "Arbitrary object handles (no translation on INSERT/XREF)";

            // 330 -> 339
            if (code >= 330 && code <= 339)
                return "Soft-pointer handle => references to objects in same DXF/drawing";

            // 340 -> 349
            if (code >= 340 && code <= 349)
                return "Hard-pointer handle => references to objects in same DXF/drawing";

            // 350 -> 359
            if (code >= 350 && code <= 359)
                return "Soft-owner handle => ownership links to objects in same DXF/drawing";

            // 360 -> 369
            if (code >= 360 && code <= 369)
                return "Hard-owner handle => ownership links to objects in same DXF/drawing";

            // 370 -> 379
            if (code >= 370 && code <= 379)
                return "Lineweight enum (16-bit). 370=common entity field";

            // 380 -> 389
            if (code >= 380 && code <= 389)
                return "PlotStyleName type enum (16-bit)";

            // 390 -> 399
            if (code >= 390 && code <= 399)
                return "PlotStyleName handle (string hex); basically a hard pointer";

            // 400 -> 409
            if (code >= 400 && code <= 409)
                return "16-bit integers";

            // 410 -> 419
            if (code >= 410 && code <= 419)
                return "String (e.g. PAPERSPACE name)";

            // 420 -> 427
            if (code >= 420 && code <= 427)
                return "32-bit int (e.g. True Color, RGBA mask)";

            // 430 -> 437
            if (code >= 430 && code <= 437)
                return "String (e.g. True Color name)";

            // 440 -> 447
            if (code >= 440 && code <= 447)
                return "32-bit int (e.g. transparency)";

            // 450 -> 459
            if (code >= 450 && code <= 459)
                return "Long integer values";

            // 460 -> 469
            if (code >= 460 && code <= 469)
                return "Double-precision floating-point values";

            // 470 -> 479
            if (code >= 470 && code <= 479)
                return "String values (various usage)";

            // 480 -> 481
            if (code >= 480 && code <= 481)
                return "Hard-pointer handle => references to other objects";

            // 999
            if (code == 999)
                return "Comment (string). Not in SAVEAS, but OPEN honors/ignores";

            // 1000 -> 1003
            if (code >= 1000 && code <= 1003)
                return "Extended data string values (ASCII up to 255 bytes)";

            // 1004
            if (code == 1004)
                return "Extended data chunk of bytes (up to 127 bytes, stored as hex)";

            // 1005
            if (code == 1005)
                return "Entity handle in extended data (up to 16 hex digits)";

            // 1010, 1020, 1030
            if (code == 1010)
                return "Point in extended data (X). Followed by 1020 (Y), 1030 (Z)";
            if (code == 1020 || code == 1030)
                return "Y or Z values of extended data point";

            // 1011, 1021, 1031
            if (code == 1011)
                return "3D world space position in extended data (X)";
            if (code == 1021 || code == 1031)
                return "Y or Z values of extended data world space position";

            // 1012, 1022, 1032
            if (code == 1012)
                return "3D world space displacement in extended data (X)";
            if (code == 1022 || code == 1032)
                return "Y or Z of extended data displacement";

            // 1013, 1023, 1033
            if (code == 1013)
                return "3D world space direction in extended data (X)";
            if (code == 1023 || code == 1033)
                return "Y or Z of extended data direction";

            // 1040, 1041, 1042
            if (code == 1040) return "Extended data double-precision float value";
            if (code == 1041) return "Extended data distance value";
            if (code == 1042) return "Extended data scale factor";

            // 1070
            if (code == 1070)
                return "Extended data 16-bit signed integer";

            // 1071
            if (code == 1071)
                return "Extended data 32-bit signed long";

            // Fallback if code not found in table
            return "Unknown or unspecified group code";
        }

        public static string GetValueType(int code)
        {
            // -----------------------------------------------------------------
            // Following the "Group code value types" table exactly:
            // -----------------------------------------------------------------

            // 0-9 -> String
            if (code >= 0 && code <= 9)
                return "String";

            // 10-17, 20-27, 30-37 -> Double precision 3D point value
            if ((code >= 10 && code <= 17) ||
                (code >= 20 && code <= 27) ||
                (code >= 30 && code <= 37))
                return "Double precision 3D point value";

            // 38-59 -> Double-precision floating-point value
            if (code >= 38 && code <= 59)
                return "Double-precision floating-point value";

            // 60-79 -> 16-bit integer value
            if (code >= 60 && code <= 79)
                return "16-bit integer value";

            // 90-99 -> 32-bit integer value
            if (code >= 90 && code <= 99)
                return "32-bit integer value";

            // 100-102 -> String
            if (code >= 100 && code <= 102)
                return "String";

            // 105 -> String representing hexadecimal (hex) handle value
            if (code == 105)
                return "String representing hexadecimal (hex) handle value";

            // 110-119, 120-129, 130-139 -> Double precision floating-point value
            if ((code >= 110 && code <= 119) ||
                (code >= 120 && code <= 129) ||
                (code >= 130 && code <= 139))
                return "Double-precision floating-point value";

            // 140-149 -> Double precision scalar floating-point value
            if (code >= 140 && code <= 149)
                return "Double precision scalar floating-point value";

            // 160-169 -> 64-bit integer value
            if (code >= 160 && code <= 169)
                return "64-bit integer value";

            // 170-179 -> 16-bit integer value
            if (code >= 170 && code <= 179)
                return "16-bit integer value";

            // 210-239 -> Double-precision floating-point value
            if (code >= 210 && code <= 239)
                return "Double-precision floating-point value";

            // 270-279, 280-289 -> 16-bit integer value
            if ((code >= 270 && code <= 279) ||
                (code >= 280 && code <= 289))
                return "16-bit integer value";

            // 290-299 -> Boolean flag value
            if (code >= 290 && code <= 299)
                return "Boolean flag value";

            // 300-309 -> Arbitrary text string
            if (code >= 300 && code <= 309)
                return "Arbitrary text string";

            // 310-319 -> String representing hex value of binary chunk
            if (code >= 310 && code <= 319)
                return "String representing hex value of binary chunk";

            // 320-329 -> String representing hex handle value
            if (code >= 320 && code <= 329)
                return "String representing hex handle value";

            // 330-369 -> String representing hex object IDs
            if (code >= 330 && code <= 369)
                return "String representing hex object IDs";

            // 370-379, 380-389 -> 16-bit integer value
            if ((code >= 370 && code <= 379) ||
                (code >= 380 && code <= 389))
                return "16-bit integer value";

            // 390-399 -> String representing hex handle value
            if (code >= 390 && code <= 399)
                return "String representing hex handle value";

            // 400-409 -> 16-bit integer value
            if (code >= 400 && code <= 409)
                return "16-bit integer value";

            // 410-419 -> String
            if (code >= 410 && code <= 419)
                return "String";

            // 420-429 -> 32-bit integer value
            if (code >= 420 && code <= 429)
                return "32-bit integer value";

            // 430-439 -> String
            if (code >= 430 && code <= 439)
                return "String";

            // 440-449 -> 32-bit integer value
            if (code >= 440 && code <= 449)
                return "32-bit integer value";

            // 450-459 -> Long
            if (code >= 450 && code <= 459)
                return "Long";

            // 460-469 -> Double-precision floating-point value
            if (code >= 460 && code <= 469)
                return "Double-precision floating-point value";

            // 470-479 -> String
            if (code >= 470 && code <= 479)
                return "String";

            // 480-481 -> String representing a hex handle value
            if (code >= 480 && code <= 481)
                return "String representing a hex handle value";

            // 999 -> Comment (string)
            if (code == 999)
                return "Comment (string)";

            // 1000-1003 -> String
            if (code >= 1000 && code <= 1003)
                return "String";

            // 1004 -> String representing a hex value of binary chunk
            if (code == 1004)
                return "String representing a hex value of binary chunk";

            // 1005 -> String (same limits as indicated with 0-9 code range)
            if (code == 1005)
                return "String";

            // 1010-1013, 1020-1023, 1030-1033, 1040-1042 -> Double-precision floating-point value
            if ((code >= 1010 && code <= 1013) ||
                (code >= 1020 && code <= 1023) ||
                (code >= 1030 && code <= 1033) ||
                (code >= 1040 && code <= 1042))
                return "Double-precision floating-point value";

            // 1070 -> 16-bit integer value
            if (code == 1070)
                return "16-bit integer value";

            // 1071 -> 32-bit integer value
            if (code == 1071)
                return "32-bit integer value";

            // -----------------------------------------------------------------
            // If no match above, default:
            // -----------------------------------------------------------------
            return "Unknown";
        }
    }
}
