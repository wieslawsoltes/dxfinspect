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
        private long _dataSize;
        private long _totalDataSize;

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
            _dataSize = System.Text.Encoding.UTF8.GetByteCount(Data);
            _totalDataSize = _dataSize; // Initially set to own data size
        }
        
        public DxfTreeNodeViewModel? Parent { get; set; }
        
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

        public void UpdateTotalDataSize()
        {
            var newTotal = _dataSize;
            if (HasChildren)
            {
                foreach (var child in Children)
                {
                    child.UpdateTotalDataSize();
                    newTotal += child._totalDataSize;
                }
            }

            this.RaiseAndSetIfChanged(ref _totalDataSize, newTotal, nameof(TotalDataSize));
            this.RaisePropertyChanged(nameof(FormattedDataSize));
        }

        public long TotalDataSize => _totalDataSize;

        public string FormattedDataSize
        {
            get
            {
                var size = _totalDataSize;
                string[] sizes = { "B", "KB", "MB", "GB" };
                int order = 0;
                double calculatedSize = size;

                while (calculatedSize >= 1024 && order < sizes.Length - 1)
                {
                    order++;
                    calculatedSize /= 1024;
                }

                return $"{calculatedSize:0.##} {sizes[order]}";
            }
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
