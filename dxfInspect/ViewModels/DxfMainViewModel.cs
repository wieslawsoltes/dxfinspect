using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using dxfInspect.Model;
using dxfInspect.Services;
using ReactiveUI;

namespace dxfInspect.ViewModels;

public class MainViewModel : ReactiveObject
{
    private ObservableCollection<DxfTabViewModel> _tabs;
    private DxfTabViewModel? _selectedTab;

    public MainViewModel()
    {
        _tabs = new ObservableCollection<DxfTabViewModel>();
        CloseTabCommand = ReactiveCommand.Create<DxfTabViewModel>(CloseTab);
        OpenInNewTabCommand = ReactiveCommand.Create<DxfTreeNodeViewModel>(OpenInNewTab);
    }

    public ObservableCollection<DxfTabViewModel> Tabs
    {
        get => _tabs;
        private set => this.RaiseAndSetIfChanged(ref _tabs, value);
    }

    public DxfTabViewModel? SelectedTab
    {
        get => _selectedTab;
        set => this.RaiseAndSetIfChanged(ref _selectedTab, value);
    }

    public ICommand CloseTabCommand { get; }
    public ICommand OpenInNewTabCommand { get; }

    public void AddNewFileTab(IList<DxfRawTag> sections, string fileName)
    {
        var treeViewModel = new DxfTreeViewModel();
        treeViewModel.LoadDxfData(sections, fileName);
        
        var tab = new DxfTabViewModel(fileName, treeViewModel);
        Tabs.Add(tab);
        SelectedTab = tab;
    }

    private void OpenInNewTab(DxfTreeNodeViewModel node)
    {
        var filteredViewModel = SelectedTab?.Content.CreateFilteredView(node);
        if (filteredViewModel != null && SelectedTab != null)
        {
            // Get the base filename without path
            string baseFileName = System.IO.Path.GetFileName(SelectedTab.Title.Split(" - ")[0]);
            
            // Create suffix using entity type and line range
            string entityType = node.Code == DxfParser.DxfCodeForType ? node.Data : $"Code {node.Code}";
            string lineRange = $"[{node.LineRange}]";
            
            var newTitle = $"{baseFileName} - {entityType} {lineRange}";
            var newTab = new DxfTabViewModel(newTitle, filteredViewModel);
            Tabs.Add(newTab);
            SelectedTab = newTab;
        }
    }

    private void CloseTab(DxfTabViewModel tab)
    {
        var index = Tabs.IndexOf(tab);
        if (tab == SelectedTab)
        {
            SelectedTab = index > 0 ? Tabs[index - 1] : (Tabs.Count > 1 ? Tabs[1] : null);
        }
        Tabs.Remove(tab);
    }
}
