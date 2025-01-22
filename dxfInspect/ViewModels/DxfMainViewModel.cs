using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Platform.Storage;
using dxfInspect.Model;
using dxfInspect.Services;
using ReactiveUI;

namespace dxfInspect.ViewModels;

public class MainViewModel : ReactiveObject
{
    private const double ParsingWeight = 0.7; // 70% for parsing
    private const double ViewModelWeight = 0.3; // 30% for view model creation

    private ObservableCollection<DxfTabViewModel> _tabs;
    private DxfTabViewModel? _selectedTab;
    private bool _isLoading;
    private double _loadingProgress;
    private string _currentSection = string.Empty;
    private string? _errorMessage;

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

    public bool IsLoading
    {
        get => _isLoading;
        private set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    public double LoadingProgress
    {
        get => _loadingProgress;
        private set => this.RaiseAndSetIfChanged(ref _loadingProgress, value);
    }

    public string CurrentSection
    {
        get => _currentSection;
        private set => this.RaiseAndSetIfChanged(ref _currentSection, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => this.RaiseAndSetIfChanged(ref _errorMessage, value);
    }

    public ICommand CloseTabCommand { get; }
    public ICommand OpenInNewTabCommand { get; }

    public async Task LoadDxfFileAsync(IStorageFile file)
    {
        IsLoading = true;
        ErrorMessage = null;
        LoadingProgress = 0;
        CurrentSection = string.Empty;

        try
        {
            await using var stream = await file.OpenReadAsync();
            var progress = new Progress<DxfParser.ParsingProgress>(p =>
            {
                LoadingProgress = p.ProgressPercentage * ParsingWeight;
                CurrentSection = $"{p.Stage}: {p.CurrentSection}";

                if (p.Error != null)
                {
                    ErrorMessage = $"Error parsing file: {p.Error.Message}";
                }
            });

            var sections = await DxfParser.ParseStreamAsync(stream, progress);

            CurrentSection = "Creating view model";
            LoadingProgress = ParsingWeight * 100; // Parsing complete

            // Increment reference count when adding new tab
            DxfRawTagCache.Instance.IncrementReferenceCount();
            await AddNewFileTabAsync(sections, file.Name);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load file: {ex.Message}";
            // Ensure we don't increment reference count if loading fails
            DxfRawTagCache.Instance.DecrementReferenceCount();
        }
        finally
        {
            LoadingProgress = 100;
            IsLoading = false;
        }
    }

    private async Task AddNewFileTabAsync(IList<DxfRawTag> sections, string fileName)
    {
        var treeViewModel = new DxfTreeViewModel();
        
        await Task.Run(() =>
        {
            var totalNodes = sections.Sum(s => CountNodes(s));
            var processedNodes = 0;

            void ReportProgress(int processed)
            {
                processedNodes = processed;
                var vmProgress = (double)processedNodes / totalNodes * 100;
                LoadingProgress = (ParsingWeight * 100) + (vmProgress * ViewModelWeight);
            }

            treeViewModel.LoadDxfData(sections, fileName, ReportProgress);
        });

        var tab = new DxfTabViewModel(fileName, treeViewModel);
        Tabs.Add(tab);
        SelectedTab = tab;
    }

    private int CountNodes(DxfRawTag tag)
    {
        var count = 1; // Count the current tag
        if (tag.Children != null)
        {
            foreach (var child in tag.Children)
            {
                count += CountNodes(child);
            }
        }
        return count;
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

        // Decrement reference count when closing a tab
        DxfRawTagCache.Instance.DecrementReferenceCount();
    }
}
