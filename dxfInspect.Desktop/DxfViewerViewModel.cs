using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Selection;
using Dxf;
using ReactiveUI;

public class DxfViewerViewModel : ReactiveObject
{
    private readonly HierarchicalTreeDataGridSource<DxfTreeNodeModel> _source;
    private bool _cellSelection;

    public DxfViewerViewModel()
    {
        _source = new HierarchicalTreeDataGridSource<DxfTreeNodeModel>(Array.Empty<DxfTreeNodeModel>())
        {
            Columns =
            {
                new HierarchicalExpanderColumn<DxfTreeNodeModel>(
                    new TextColumn<DxfTreeNodeModel, string>("Code", x => x.Code, new GridLength(80)),
                    x => x.Children,
                    x => x.HasChildren,
                    x => x.IsExpanded),
                new TextColumn<DxfTreeNodeModel, string>("Type", x => x.Type, new GridLength(120)),
                new TextColumn<DxfTreeNodeModel, string>("Data", x => x.Data, new GridLength(1, GridUnitType.Star))
            }
        };
    }

    public bool CellSelection
    {
        get => _cellSelection;
        set
        {
            if (_cellSelection != value)
            {
                _cellSelection = value;
                if (_cellSelection)
                    Source.Selection = new TreeDataGridCellSelectionModel<DxfTreeNodeModel>(Source);
                else
                    Source.Selection = new TreeDataGridRowSelectionModel<DxfTreeNodeModel>(Source);
                this.RaisePropertyChanged();
            }
        }
    }

    public ITreeDataGridSource<DxfTreeNodeModel> Source => _source;

    public void LoadDxfData(IList<DxfRawTag> sections)
    {
        var nodes = ConvertToTreeNodes(sections);
        _source.Items = nodes;
    }

    private static List<DxfTreeNodeModel> ConvertToTreeNodes(IList<DxfRawTag> sections)
    {
        var nodes = new List<DxfTreeNodeModel>();

        foreach (var section in sections.Where(s => s.IsEnabled))
        {
            var sectionNode = new DxfTreeNodeModel(
                section.GroupCode.ToString(),
                section.DataElement ?? string.Empty,
                "SECTION");

            if (section.Children != null)
            {
                AddChildNodes(sectionNode, section.Children);
            }

            nodes.Add(sectionNode);
        }

        return nodes;
    }

    private static void AddChildNodes(DxfTreeNodeModel parent, IList<DxfRawTag> children)
    {
        foreach (var child in children.Where(c => c.IsEnabled))
        {
            string type = child.GroupCode == DxfParser.DxfCodeForType ? child.DataElement ?? "TYPE" : "Value";
            
            var node = new DxfTreeNodeModel(
                child.GroupCode.ToString(),
                child.DataElement ?? string.Empty,
                type);

            if (child.Children != null)
            {
                AddChildNodes(node, child.Children);
            }

            parent.Children.Add(node);
        }
    }
}
