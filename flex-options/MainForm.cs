
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace flex_options
{
    public partial class MainForm : Form
    {
        public MainForm() => InitializeComponent();
        protected override void OnLoad(EventArgs e)
        {
            int replaceIndex;
            DataGridViewComboBoxColumn cbCol;

            base.OnLoad(e);
            dataGridView.DataSource = Records; 
            dataGridView.Columns[nameof(Record.Description)].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            // Index column
            cbCol = new DataGridViewComboBoxColumn
            {
                Name = nameof(Record.Index),
                DataSource = new int[] {1,2,3},
                DataPropertyName = nameof(Record.Index),
            };
            replaceIndex = dataGridView.Columns[nameof(Record.Index)].Index;
            dataGridView.Columns.RemoveAt(replaceIndex);
            dataGridView.Columns.Insert(replaceIndex, cbCol);

            // Option column
            cbCol = new DataGridViewComboBoxColumn
            {
                Name = nameof(Record.Option),
                DataSource = Enum.GetNames<OptionsOne>(),
                DataPropertyName = nameof(Record.Option),
            };
            replaceIndex = dataGridView.Columns[nameof(Record.Option)].Index;
            dataGridView.Columns.RemoveAt(replaceIndex);
            dataGridView.Columns.Insert(replaceIndex, cbCol);
            dataGridView.CurrentCellChanged += (sender, e) =>
            {
                var cbCol = ((DataGridViewComboBoxColumn)dataGridView.Columns[nameof(Record.Option)]);
                if (dataGridView.CurrentCell is DataGridViewComboBoxCell cbCell && cbCell.ColumnIndex == cbCol.Index)
                {
                    var record = Records[dataGridView.CurrentCell.RowIndex];
                    cbCell.DataSource = record.AvailableOptions;
                }
            };
            dataGridView.DataError += (sender, e) =>
            {
                Debug.Fail("We don't expect this to happen anymore!");
            };
            // Consider 'not' allowing the record to be dirty after a CB select.
            dataGridView.CurrentCellDirtyStateChanged += (sender, e) =>
            {
                if(dataGridView.CurrentCell is DataGridViewComboBoxCell)
                {
                    if (dataGridView.IsCurrentCellDirty)
                    {
                        BeginInvoke(()=> dataGridView.EndEdit(DataGridViewDataErrorContexts.Commit));
                    }
                }
            };
            Records.Add(new Record());
            Records.Add(new Record());
            Records.Add(new Record());
        }
        BindingList<Record> Records { get; } = new BindingList<Record>();
    }

    enum OptionsOne { Select, Bumble, Twinkle, }
    enum OptionsTwo { Select, Whisker, Quibble, }
    enum OptionsThree { Select, Wobble, Flutter, }
    class Record : INotifyPropertyChanged
    {
        static int _recordCount = 1;
        public string Description { get; set; } = $"Record {_recordCount++}";

        // By initially setting index to '1' and Available options
        // to `OptionsOne` every record is born in sync with itself.
        public int Index
        {
            get => _index;
            set
            {
                if (!Equals(_index, value))
                {
                    _index = value;
                    switch (Index)
                    {
                        case 1: AvailableOptions = Enum.GetNames(typeof(OptionsOne)); break;
                        case 2: AvailableOptions = Enum.GetNames(typeof(OptionsTwo)); break;
                        case 3: AvailableOptions = Enum.GetNames(typeof(OptionsThree)); break;
                    }
                    if (Option is string currentOption && !AvailableOptions.Contains(currentOption))
                    {
                        Option = AvailableOptions[0];
                        OnPropertyChanged(nameof(Option));
                    }
                    OnPropertyChanged();
                }
            }
        }
        int _index = 1;

        [Browsable(false)]
        public string[] AvailableOptions { get; set; } = Enum.GetNames(typeof(OptionsOne));
        public string? Option
        {
            get => _option;
            set
            {
                if (!Equals(_option, value))
                {
                    if (Option is string currentOption && AvailableOptions.Contains(value))
                    {
                        _option = value;
                        OnPropertyChanged();
                    }
                }
            }
        }
        string? _option = $"{OptionsOne.Select}";

        public virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
