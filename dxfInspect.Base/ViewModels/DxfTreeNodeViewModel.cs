using System.Collections.ObjectModel;
using dxfInspect.Model;
using ReactiveUI;

namespace dxfInspect.ViewModels
{
    public class DxfTreeNodeViewModel : ReactiveObject
    {
        private bool _isExpanded;
        private DxfLineRange _lineRange;
        private int _endLine;

        public DxfTreeNodeViewModel(
            int startLine,
            int endLine,
            int code,
            string data,
            string type,
            string nodeKey,
            string originalGroupCodeLine,
            string originalDataLine,
            DxfRawTag rawTag)
        {
            StartLine = startLine;
            _endLine = endLine;
            _lineRange = new DxfLineRange(startLine, endLine);
            Code = code;
            Data = data;
            Type = type;
            NodeKey = nodeKey;
            OriginalGroupCodeLine = originalGroupCodeLine;
            OriginalDataLine = originalDataLine;
            RawTag = rawTag;
        }

        public int StartLine { get; }

        public int EndLine
        {
            get => _endLine;
            set => this.RaiseAndSetIfChanged(ref _endLine, value);
        }

        public DxfLineRange LineRange
        {
            get => _lineRange;
            private set => this.RaiseAndSetIfChanged(ref _lineRange, value);
        }

        public void UpdateLineRange(int startLine, int endLine)
        {
            _lineRange = new DxfLineRange(startLine, endLine);
            this.RaisePropertyChanged(nameof(LineRange));
        }

        public int Code { get; }
        public string Data { get; }
        public string Type { get; }
        public string NodeKey { get; }
        public string OriginalGroupCodeLine { get; }
        public string OriginalDataLine { get; }
        public DxfRawTag RawTag { get; }
        public ObservableCollection<DxfTreeNodeViewModel> Children { get; } = [];
        public bool HasChildren => Children.Count > 0;

        public bool IsExpanded
        {
            get => _isExpanded;
            set => this.RaiseAndSetIfChanged(ref _isExpanded, value);
        }

        public string CodeString => Code.ToString();

        public string GroupCodeDescription => DxfGroupCodeInfo.GetDescription(Code);

        public string GroupCodeValueType => DxfGroupCodeInfo.GetValueType(Code);
    }
}
