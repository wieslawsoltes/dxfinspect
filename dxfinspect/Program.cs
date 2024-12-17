using System.Text;
using Dxf;

try
{
    switch (args.Length)
    {
        case 1:
        {
            var dxf = File.ReadAllText(args[0]);
            var sections = DxfParser.Parse(dxf);
            var fileName = Path.GetFileName(args[0]);
            var html = ToHtml(sections, fileName);
            Console.WriteLine(html);
            break;
        }
        case 2:
        {
            var dxf = File.ReadAllText(args[0]);
            var sections = DxfParser.Parse(dxf);
            var html = ToHtml(sections, args[0]);
            File.WriteAllText(args[1], html);
            break;
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
    Console.WriteLine(ex.StackTrace);
}

static string ToHtml(IList<DxfRawTag> sections, string fileName)
{
    var sb = new StringBuilder();

    sb.AppendLine("<html>");
    sb.AppendLine("<head>");
    sb.AppendFormat("<title>{0}</title>{1}", fileName, Environment.NewLine);
    sb.AppendLine("<meta charset=\"utf-8\"/>");
    sb.AppendLine("<style type=\"text/css\">");
    sb.AppendLine("body     { background-color:rgb(221,221,221); }");
    sb.AppendLine(".Table   { display:none; font-family:\"Courier New\"; font-size:10pt; border-collapse:collapse; margin:10px; float:none; }");
    sb.AppendLine(".Header  { display:table-row; font-weight:normal; text-align:left; background-color:rgb(255,30,102); }");
    sb.AppendLine(".Section { display:table-row; font-weight:normal; text-align:left; background-color:rgb(255,242,102); }");
    sb.AppendLine(".Other   { display:table-row; font-weight:normal; text-align:left; background-color:rgb(191,191,191); }");
    sb.AppendLine(".Row     { display:table-row; background-color:rgb(221,221,221); }");
    sb.AppendLine(".Cell    { display:table-cell; padding-left:5px; padding-right:5px; border:none; }");
    sb.AppendLine(".Line    { overflow:hidden; white-space:nowrap; text-overflow:ellipsis; width:60px; color:rgb(84,84,84); }");
    sb.AppendLine(".Code    { overflow:hidden; white-space:nowrap; text-overflow:ellipsis; width:50px; color:rgb(116,116,116); }");
    sb.AppendLine(".Data    { overflow:hidden; white-space:nowrap; text-overflow:ellipsis; width:300px; color:rgb(0,0,0); }");
    sb.AppendLine(".Toggle  { display:table; cursor:hand; font-family:\"Courier New\"; font-size:14pt; font-weight:normal; text-align:left; border-collapse:collapse; margin-left:10px; margin-top:10px; float:none; width:440px; background-color:rgb(191,191,191); }");
    sb.AppendLine("</style>");
    sb.AppendLine("<script type=\"text/javascript\"> function toggle_visibility(id) { var e = document.getElementById(id); if(e.style.display == 'table') e.style.display = 'none'; else e.style.display = 'table'; } </script>");
    sb.AppendLine("</head>");
    sb.AppendLine("<body>");

    var lineNumber = 0;
    for (var i = 0; i < sections.Count; i++)
    {
        var section = sections[i];

        if (!section.IsEnabled)
        {
            continue;
        }

        // section
        sb.AppendFormat("{3}<div class=\"Toggle\" onclick=\"toggle_visibility('{0}');\"><p>{1} {2}</p></div>{3}", i, section.DataElement, (section.Children != null) && (section.Children.Count > 0) && (section.Children[0].GroupCode == DxfParser.DxfCodeForName) ? section.Children[0].DataElement : "<Unknown>", Environment.NewLine);
        //sb.AppendFormat("<!-- SECTION i={0} -->{1}", i, Environment.NewLine);
        sb.AppendFormat("<div class=\"Table\" id=\"{0}\">{1}", i, Environment.NewLine);
        sb.AppendFormat("    <div class=\"Header\"><div class=\"Cell\"><p>LINE</p></div><div class=\"Cell\"><p>CODE</p></div><div class=\"Cell\"><p>DATA</p></div></div>{0}", Environment.NewLine);
        sb.AppendFormat("    <div class=\"{0}\"><div class=\"Cell\"><p class=\"Line\">{1}</p></div><div class=\"Cell\"><p class=\"Code\">{2}:</p></div><div class=\"Cell\"><p class=\"Data\">{3}</p></div></div>{4}", "Section", lineNumber += 2, section.GroupCode, section.DataElement, Environment.NewLine);

        if (section.Children != null)
        {
            for (var j = 0; j < section.Children.Count; j++)
            {
                var child = section.Children[j];
                if (child.IsEnabled)
                {
                    var isEntityWithType = child.GroupCode == DxfParser.DxfCodeForType;
                    if (isEntityWithType)
                    {
                        var other = child;

                        // entity with children (type)
                        //sb.AppendFormat("    <!-- OTHER j={0} -->{1}", j, Environment.NewLine);
                        sb.AppendFormat("    <div class=\"{0}\"><div class=\"Cell\"><p class=\"Line\">{1}</p></div><div class=\"Cell\"><p class=\"Code\">{2}:</p></div><div class=\"Cell\"><p class=\"Data\">{3}</p></div></div>{4}", "Other", lineNumber += 2, other.GroupCode, other.DataElement, Environment.NewLine);

                        if (other.Children != null)
                        {
                            for (var k = 0; k < other.Children.Count; k++)
                            {
                                var entity = other.Children[k];
                                if (entity.IsEnabled)
                                {
                                    // entity without type
                                    //sb.AppendFormat("        <!-- ENTITY k={0} -->{1}", k, Environment.NewLine);
                                    sb.AppendFormat("        <div class=\"Row\"><div class=\"Cell\"><p class=\"Line\">{0}</p></div><div class=\"Cell\"><p class=\"Code\">{1}:</p></div><div class=\"Cell\"><p class=\"Data\">{2}</p></div></div>{3}", lineNumber += 2, entity.GroupCode, entity.DataElement, Environment.NewLine);
                                }
                            }
                        }
                    }
                    else
                    {
                        var entity = child;

                        // entity without children (type)
                        //sb.AppendFormat("    <!-- ENTITY j={0} -->{1}", j, Environment.NewLine);
                        sb.AppendFormat("    <div class=\"Row\"><div class=\"Cell\"><p class=\"Line\">{0}</p></div><div class=\"Cell\"><p class=\"Code\">{1}:</p></div><div class=\"Cell\"><p class=\"Data\">{2}</p></div></div>{3}", lineNumber += 2, entity.GroupCode, entity.DataElement, Environment.NewLine);
                    }
                }
            }
        }

        sb.AppendLine("</div>");
    }

    sb.AppendFormat("{0}</body></html>", Environment.NewLine);

    return sb.ToString();
}
