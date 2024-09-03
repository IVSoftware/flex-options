I was able to easily reproduce your `DataError` exception and the root cause is that whenever the `DataGridView` refreshes, it tries to synchronize the displayed value with one of the available items in the combo box. But if you change the available options in the combo box, it can no longer find the displayed value and throws the `DataError` exception. 

As I understand it, You'd like to have different options for different index values, something like this for example:

```
    enum OptionsOne { Select, Bumble, Twinkle, }
    enum OptionsTwo { Select, Whisker, Quibble, }
    enum OptionsThree { Select, Wobble, Flutter, }
```

[![different options][1]][1]
___

To handle the `Refresh` issue, you can experiment with using binding to suppress any change to the `Option` value if the incoming value isn't a currently available option. Here's an example for the bound record class:

```
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
```

[![successful selection][2]][2]

___

A secondary issue is that you might change the `Index` value, and now the currently-selected value is no longer valid, so another aspect of this binding is that the record will reset to the safe "common" value of `Select` if that occurs. 

[![successful redirection][3]][3]

___

The only thing that remains to be done is making sure the available selections track the selected item. This can be done in a handler for the `CurrentCellChanged` event as shown in this initialization routine:

```
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
```

___

One more subtlety. In your original code, changing a CB selection is going to make the record dirty and uncommitted. By handling the `CurrentCellDirtyStateChanged` event you can go ahead and commit immediately if desired.



  [1]: https://i.sstatic.net/2B6aCpM6.png
  [2]: https://i.sstatic.net/AhdcjM8J.png
  [3]: https://i.sstatic.net/yr6AUP80.png