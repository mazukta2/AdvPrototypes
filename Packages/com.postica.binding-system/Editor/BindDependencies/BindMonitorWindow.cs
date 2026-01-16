// filepath: Packages/com.postica.binding-system/Editor/BindDependencies/DataRefreshersMonitorWindow.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using Postica.BindingSystem;
using Postica.Common;
using Cursor = UnityEngine.UIElements.Cursor;

namespace Postica.BindingSystem.BindDependencies
{
    /// <summary>
    /// Real-time monitor window showing all data refreshers and their current values.
    /// Rows flash when values change to provide visual feedback.
    /// </summary>
    internal class BindMonitorWindow : EditorWindow
    {
        private const double ExecuteWarningThresholdPercentage = 0.001; // Warn if execution time exceeds 0.1% of frame time
        private const int MeasurementSampleCount = 100; // Number of samples to keep for moving average calculations
        
        [MenuItem("Window/Analysis/Bindings Monitor", priority = 800)]
        public static void Open()
        {
            var window = GetWindow<BindMonitorWindow>();
            var lite = EditorGUIUtility.isProSkin ? "" : "_lite";
            var icon = Resources.Load<Texture2D>($"_bsicons/bind{lite}_on_window");
            window.titleContent = new GUIContent("Bindings Monitor", icon);
        }

        private Toolbar _toolbar;
        private Footer _footer;
        private ToolbarToggle _pauseToggle;
        private ToolbarButton _resetButton;
        private ToolbarToggle _enableEditorTimeToggle;
        private ToolbarToggle _showInactiveBindsToggle;
        private ToolbarToggle _phasedOptionToggle;
        private Slider _updateIntervalSlider;
        private Label _messageLabel;
        private ToolbarSearchField _searchField;

        private MultiColumnListView _tableView;

        private InactiveDataUpdater _inactiveDataUpdater;

        private readonly List<StageRowData> _allStageRows = new();

        private readonly List<StageRowData> _filteredStageRows = new();

        [NonSerialized] private bool _isPaused;
        [NonSerialized] private DateTime _nextUpdate;
        [NonSerialized] private DateTime _nextFastSortUpdate;
        [NonSerialized] private readonly float _fastSortInterval = 2f;
        [NonSerialized] private string _searchText = "";

        [NonSerialized] private double _executionTimeThreshold;

        [SerializeField] private bool _showInactiveBinds;
        [SerializeField] private float _updateInterval;

        [NonSerialized] private bool _requiresConstantReordering;

        private bool CanMonitor => Application.isPlaying || _enableEditorTimeToggle?.value == true;

        public string StatusText
        {
            get => _footer.Status;
            set => _footer.Status = value;
        }

        private void OnEnable()
        {
            TrySetWindowIcon();

            BindingEngineInternal.IsProfilingEnabled = true;

            var targetFramerate = Application.targetFrameRate <= 0
                ? Screen.currentResolution.refreshRateRatio.value
                : Application.targetFrameRate;
            _executionTimeThreshold = ExecuteWarningThresholdPercentage * (1000.0 / targetFramerate);
        }

        private void TrySetWindowIcon()
        {
            if (titleContent?.image == null)
            {
                var lite = EditorGUIUtility.isProSkin ? "" : "_lite";
                var icon = Resources.Load<Texture2D>($"_bsicons/bind{lite}_on_window");
                if (icon)
                {
                    titleContent = new GUIContent("Bindings Monitor", icon);
                }
            }
        }

        private void OnDisable()
        {
            BindingEngineInternal.IsProfilingEnabled = false;
        }

        public void CreateGUI()
        {
            TrySetWindowIcon();
            minSize = new Vector2(900, 400);

            var root = rootVisualElement;
            root.style.flexDirection = FlexDirection.Column;

            BuildUI(root);
        }

        private void BuildUI(VisualElement root)
        {
            // Load styles
            LoadStyles(root);

            // Build Data
            BuildData();

            // Build UI
            BuildToolbar(root);
            BuildWarning(root);
            BuildTable(root);
            BuildFooter(root);
        }

        private void BuildWarning(VisualElement root)
        {
            root.Add(new HelpBox("The performance measurements are approximate and may vary based on system load and other factors, " +
                                 "such as Editor drawing, Game rendering, Garbage collection and others.",
                HelpBoxMessageType.Info).WithClass("bs-monitor-warning"));
        }

        private void BuildFooter(VisualElement root)
        {
            _footer = new Footer(this);
            root.Add(_footer);
        }

        private void Update()
        {
            // Editor mode warning
            if (!CanMonitor)
            {
                BindingEngineInternal.IsProfilingEnabled = false;
                rootVisualElement.AddToClassList("bs-monitor--inactive");
                rootVisualElement.Clear();
                BuildToolbar(rootVisualElement);
                _messageLabel ??= new Label().WithClass("bs-monitor__message");
                _messageLabel.AddToClassList("bs-monitor__message--warning");
                _messageLabel.text = "Enter Play Mode to monitor active bindings";
                rootVisualElement.Add(_messageLabel);
                return;
            }

            rootVisualElement.RemoveFromClassList("bs-monitor--inactive");

            if (_messageLabel?.panel != null)
            {
                rootVisualElement.Clear();
                BindingEngineInternal.IsProfilingEnabled = true;
                BuildUI(rootVisualElement);
            }

            // Refresh Draws

            if (DateTime.Now < _nextUpdate)
            {
                return;
            }

            _nextUpdate = DateTime.Now.AddSeconds(_updateIntervalSlider?.value ?? 0f);

            if (_isPaused)
            {
                StatusText = "Paused";
                return;
            }

            StatusText = "Active";

            if (_requiresConstantReordering && _tableView.sortedColumns.Any())
            {
                FastReorder();
            }

            foreach (var filteredStageRow in _filteredStageRows)
            {
                filteredStageRow.RefreshDraw();
            }

            _footer.Refresh();
        }

        private void LoadStyles(VisualElement root)
        {
            var ussPath = BindingSystemIO.BuildLocalPath("Editor", "BindDependencies", "BindMonitorWindow.uss");
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(ussPath);
            if (styleSheet) root.styleSheets.Add(styleSheet);

            if (!EditorGUIUtility.isProSkin)
            {
                ussPath = BindingSystemIO.BuildLocalPath("Editor", "BindDependencies", "BindMonitorWindowLite.uss");
                styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(ussPath);
                if (styleSheet) root.styleSheets.Add(styleSheet);
            }
        }

        private void BuildData()
        {
            BindingEngineInternal.DataUpdateRegistered -= OnDataUpdateRegistered;
            BindingEngineInternal.DataUpdateRegistered += OnDataUpdateRegistered;

            BindingEngineInternal.DataUpdateUnregistered -= OnDataUpdateUnregistered;
            BindingEngineInternal.DataUpdateUnregistered += OnDataUpdateUnregistered;

            RefreshAllData();
        }

        private void OnDataUpdateRegistered(BindingEngineInternal.DataUpdater.StageData data)
        {
            RefreshAllData();
        }


        private void OnDataUpdateUnregistered(BindingEngineInternal.DataUpdater.StageData obj)
        {
            RefreshAllData();
        }

        private void RefreshAllData()
        {
            ClearAllRows();

            AddDataFromStage(BindingEngineInternal.PreUpdate);
            AddDataFromStage(BindingEngineInternal.PostUpdate);
            AddDataFromStage(BindingEngineInternal.PreLateUpdate);
            AddDataFromStage(BindingEngineInternal.PostLateUpdate);
            AddDataFromStage(BindingEngineInternal.PreRender);
            AddDataFromStage(BindingEngineInternal.PostRender);
            AddDataFromStage(BindingEngineInternal.PreFixedUpdate);
            AddDataFromStage(BindingEngineInternal.PostFixedUpdate);
            
            if (_enableEditorTimeToggle?.value == true)
            {
                AddDataFromStage(BindingEngineInternal.UpdateAtEditTime);
            }

            if (_showInactiveBinds)
            {
                // Collect all registered binds that are not currently active
                var inactiveBinds = BindingEngineInternal.AllProxies.Except(_allStageRows.Select(d => d.Proxy));
                _inactiveDataUpdater ??= new InactiveDataUpdater();
                _inactiveDataUpdater.Refresh(inactiveBinds);
                
                AddDataFromStage(_inactiveDataUpdater);
            }

            MergeCommonRows();
            UpdateFilteredRows();
        }

        private void MergeCommonRows()
        {
            // Merge the rows by their ids
            var mergedRows = new Dictionary<int, StageRowData>();
            foreach (var row in _allStageRows)
            {
                if (mergedRows.TryGetValue(row.Id, out var existingRow))
                {
                    MergeRow(existingRow, row);
                }
                else
                {
                    mergedRows[row.Id] = row;
                }
            }
            
            _allStageRows.Clear();
            _allStageRows.AddRange(mergedRows.Values);
        }

        private void MergeRow(StageRowData existingRow, StageRowData row)
        {
            existingRow.MergeWith(row);
            row.Dispose();
        }

        private void ClearAllRows()
        {
            if (_tableView != null)
            {
                _tableView.itemsSource = Array.Empty<StageRowData>();
                _tableView.RefreshItems();
            }

            foreach (var row in _allStageRows)
            {
                row?.Dispose();
            }

            _allStageRows.Clear();
        }

        private void AddDataFromStage(BindingEngineInternal.IDataUpdater stage)
        {
            foreach (var data in stage.AllDataUpdaters)
            {
                var proxy = BindingEngineInternal.GetProxyById(data.Id);
                var row = new StageRowData(this, data, proxy);

                _allStageRows.Add(row);
            }
        }

        private void UpdateFilteredRows()
        {
            _filteredStageRows.Clear();
            _filteredStageRows.AddRange(_allStageRows.Where(FilterBySearch));
            if (_tableView == null) return;

            _tableView.itemsSource = Array.Empty<StageRowData>();

            if (_tableView.sortedColumns.Any())
            {
                _filteredStageRows.Sort(ColumnSorter);
            }

            foreach (var stageRowData in _filteredStageRows)
            {
                stageRowData.Measures.Reset();
            }

            _tableView.itemsSource = _filteredStageRows;
            _tableView.RefreshItems();
        }

        private void FastReorder()
        {
            if (_tableView == null) return;
            if (!_tableView.sortedColumns.Any()) return;
            if (DateTime.Now < _nextFastSortUpdate) return;

            _nextFastSortUpdate = DateTime.Now.AddSeconds(_fastSortInterval);

            _filteredStageRows.Sort(ColumnSorter);
            _tableView.RefreshItems();
        }

        private int ColumnSorter(StageRowData x, StageRowData y)
        {
            _requiresConstantReordering = false;

            foreach (var column in _tableView.sortedColumns)
            {
                switch (column.columnName)
                {
                    case "Stages":
                        var result = string.Compare(x.Stages, y.Stages, StringComparison.OrdinalIgnoreCase);
                        if (result != 0) return column.direction == SortDirection.Ascending ? result : -result;
                        break;
                    case "Proxy":
                        var xProxyName = x.Proxy?.Source?.name ?? "";
                        var yProxyName = y.Proxy?.Source?.name ?? "";
                        result = string.Compare(xProxyName, yProxyName, StringComparison.OrdinalIgnoreCase);
                        if (result != 0) return column.direction == SortDirection.Ascending ? result : -result;

                        var xPath = x.Proxy?.RuntimePath ?? "";
                        var yPath = y.Proxy?.RuntimePath ?? "";
                        result = string.Compare(xPath, yPath, StringComparison.OrdinalIgnoreCase);
                        if (result != 0) return column.direction == SortDirection.Ascending ? result : -result;

                        var xValue = x.Proxy?.BindProxyPair.SourceValue;
                        var yValue = y.Proxy?.BindProxyPair.SourceValue;
                        result = string.Compare(ValueToString(xValue), ValueToString(yValue),
                            StringComparison.OrdinalIgnoreCase);
                        if (result != 0) return column.direction == SortDirection.Ascending ? result : -result;
                        break;
                    case "Target":
                        var xTargetName = x.Proxy?.BindData?.Source?.name ?? "";
                        var yTargetName = y.Proxy?.BindData?.Source?.name ?? "";
                        result = string.Compare(xTargetName, yTargetName, StringComparison.OrdinalIgnoreCase);
                        if (result != 0) return column.direction == SortDirection.Ascending ? result : -result;

                        var xTargetPath = x.Proxy?.BindData?.Path ?? "";
                        var yTargetPath = y.Proxy?.BindData?.Path ?? "";
                        result = string.Compare(xTargetPath, yTargetPath, StringComparison.OrdinalIgnoreCase);
                        if (result != 0) return column.direction == SortDirection.Ascending ? result : -result;

                        var xTargetValue = x.Proxy?.BindProxyPair.TargetValue;
                        var yTargetValue = y.Proxy?.BindProxyPair.TargetValue;
                        result = string.Compare(ValueToString(xTargetValue), ValueToString(yTargetValue),
                            StringComparison.OrdinalIgnoreCase);
                        if (result != 0) return column.direction == SortDirection.Ascending ? result : -result;
                        break;
                    case "Measurements":
                        _requiresConstantReordering = true;
                        // Average execution time
                        result = x.Measures.AvgExecutionTimeMs.CompareTo(y.Measures.AvgExecutionTimeMs);
                        if (result != 0) return column.direction == SortDirection.Ascending ? result : -result;

                        break;
                }
            }

            return 0;
        }

        private bool FilterBySearch(StageRowData stageRowData)
        {
            if (string.IsNullOrWhiteSpace(_searchText)) return true;

            var search = _searchText.Trim().ToLower();
            if (stageRowData.Stages.Contains(search, StringComparison.OrdinalIgnoreCase)) return true;
            if (stageRowData.Proxy != null && FilterBySearch(stageRowData.Proxy)) return true;

            return false;
        }

        private bool FilterBySearch(BindProxy proxy)
        {
            if (string.IsNullOrWhiteSpace(_searchText)) return true;

            if (!proxy?.Source) return false;
            if (!proxy.BindData?.Source) return false;

            var search = _searchText.Trim().ToLower();
            if (proxy.Source.name.Contains(search, StringComparison.OrdinalIgnoreCase)) return true;
            if (proxy.RuntimePath.Contains(search, StringComparison.OrdinalIgnoreCase)) return true;
            if (proxy.BindData != null &&
                proxy.BindData.Value.Source.name.Contains(search, StringComparison.OrdinalIgnoreCase)) return true;
            if (proxy.BindData != null &&
                proxy.BindData.Value.Path.Contains(search, StringComparison.OrdinalIgnoreCase)) return true;

            return false;
        }

        private void BuildToolbar(VisualElement root)
        {
            if (_toolbar == null)
            {
                _toolbar = new Toolbar();
                _toolbar.AddToClassList("bs-monitor-toolbar");

                _enableEditorTimeToggle =
                    new ToolbarToggle()
                        { text = "Edit Time", value = false }.WithClass("bs-monitor-toolbar__edit-time");
                _enableEditorTimeToggle.RegisterValueChangedCallback(evt =>
                {
                    RefreshAllData();
                    Update();
                });
                _toolbar.Add(_enableEditorTimeToggle);
            }

            if (_toolbar.parent != root)
            {
                _toolbar.RemoveFromHierarchy();
                root.Add(_toolbar);
            }

            if (!CanMonitor)
            {
                _toolbar.Clear();
                _toolbar.Add(_enableEditorTimeToggle);
                return;
            }

            _showInactiveBindsToggle = new ToolbarToggle()
            {
                text = "Show Inactive",
                value = _showInactiveBinds,
                tooltip = "Show binds that are currently inactive (not updating)"
            }.WithClass("bs-monitor-toolbar__show-inactive");
            _showInactiveBindsToggle.RegisterValueChangedCallback(evt =>
            {
                _showInactiveBinds = evt.newValue;
                RefreshAllData();
            });

            _pauseToggle = new ToolbarToggle() { text = "Pause", value = false }.WithClass("bs-monitor-toolbar__pause");
            _pauseToggle.RegisterValueChangedCallback(evt =>
            {
                _isPaused = evt.newValue;
                root.EnableInClassList("bs-monitor--paused", _isPaused);
                BindingEngineInternal.IsProfilingEnabled = !_isPaused;
                StatusText = _isPaused ? "Paused" : "Active";
                _pauseToggle.text = _isPaused ? "Resume" : "Pause";
            });

            _resetButton = new ToolbarButton(ResetTable)
                { text = "Reset" }.WithClass("bs-monitor-toolbar__reset");

            _updateIntervalSlider = new Slider(0f, 2f)
            {
                value = _updateInterval,
                pageSize = 0.1f,
                showInputField = true,
            }.WithClass("bs-monitor-interval__input");
            _updateIntervalSlider.RegisterValueChangedCallback(evt => { _updateInterval = evt.newValue; });

            _searchField = new ToolbarSearchField().WithClass("bs-monitor-toolbar__search");
            _searchField.value = "";
            _searchField.RegisterValueChangedCallback(evt =>
            {
                _searchText = evt.newValue;
                UpdateFilteredRows();
            });

            _phasedOptionToggle = new ToolbarToggle()
            {
                text = "Phased Updates",
                value = BindSystem.Options.UsePhasedUpdates,
                tooltip = "Toggle phased updates for data refreshers"
            }.WithClass("bs-monitor-toolbar__phased-updates");
            _phasedOptionToggle.RegisterValueChangedCallback(evt =>
            {
                BindSystem.Options.UsePhasedUpdates = evt.newValue;
            });

            _toolbar.Add(_showInactiveBindsToggle);
            _toolbar.Add(_pauseToggle);
            _toolbar.Add(_resetButton);
            _toolbar.Add(new ToolbarSpacer().WithClass("bs-monitor-toolbar__spacer"));
            
            // if (Application.isPlaying)
            // {
            //     _toolbar.Add(_phasedOptionToggle);
            //     _toolbar.Add(new ToolbarSpacer().WithClass("bs-monitor-toolbar__spacer"));
            // }

            _toolbar.Add(new VisualElement { tooltip = "Update Interval (seconds)" }.WithClass("bs-monitor-interval")
                .WithChildren(new Label("Refresh Interval").WithClass("bs-monitor-interval__label"),
                    _updateIntervalSlider));
            _toolbar.Add(_searchField);
        }

        private void ResetTable()
        {
            if (_tableView == null) return;
            
            _tableView.sortColumnDescriptions.Clear();
            _searchField.value = "";
        }

        private void BuildTable(VisualElement root)
        {
            _tableView = new MultiColumnListView()
            {
                showAlternatingRowBackgrounds = AlternatingRowBackground.All,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                selectionType = SelectionType.None,
#if UNITY_6000_0_OR_NEWER
                sortingMode = ColumnSortingMode.Default,
#else
                sortingEnabled = true,
#endif
            }.WithClass("bs-monitor-table");

            _tableView.columnSortingChanged -= UpdateFilteredRows;
            _tableView.columnSortingChanged += UpdateFilteredRows;

            _tableView.columns.Add(MakeSimpleColumn("Stages", 80, r => r.Stages));
            // _tableView.columns.Add(MakeSimpleColumn("Id", 60, r => r.Id.ToString()));

            _tableView.columns.Add(MakeColumn<BindView>("Proxy", 300, (v, r) =>
            {
                v.SourceField.value = r.Proxy?.Source;
                v.PathField.value = r.Proxy?.RuntimePath ?? "<none>";

                var sourceValue = r.Proxy?.BindProxyPair.SourceValue;
                if (sourceValue?.GetType() != null && v.ValueType?.IsAssignableFrom(sourceValue.GetType()) == true)
                {
                    v.UpdateValueAction?.Invoke(r.Proxy?.BindProxyPair.SourceValue);
                    return;
                }

                (v.ValueField, v.UpdateValueAction) =
                    BuildOrRebuildValueField(sourceValue, v.UpdateValueAction, v.HighlightValueChange);
            }));

            _tableView.columns.Add(MakeColumn<Image>("M", 12, (v, r) =>
            {
                v.RemoveFromClassList("bs-monitor-cell__mode--" + nameof(BindMode.Read).ToLower());
                v.RemoveFromClassList("bs-monitor-cell__mode--" + nameof(BindMode.ReadWrite).ToLower());
                v.RemoveFromClassList("bs-monitor-cell__mode--" + nameof(BindMode.Write).ToLower());
                v.WithClass("bs-monitor-cell__mode",
                    "bs-monitor-cell__mode--" + r.Proxy?.BindData?.Mode.ToString().ToLower());
            }, resizable: false, sortable: false));

            _tableView.columns.Add(MakeColumn<BindView>("Target", 300, (v, r) =>
            {
                v.SourceField.value = r.Proxy?.BindData?.Source;
                v.PathField.value = r.Proxy?.BindData?.Path ?? "<none>";
                var targetValue = r.Proxy?.BindProxyPair.TargetValue;
                if (targetValue?.GetType() != null && v.ValueType?.IsAssignableFrom(targetValue.GetType()) == true)
                {
                    v.UpdateValueAction?.Invoke(r.Proxy?.BindProxyPair.TargetValue);
                    return;
                }

                (v.ValueField, v.UpdateValueAction) = BuildOrRebuildValueField(r.Proxy?.BindProxyPair.TargetValue,
                    v.UpdateValueAction, v.HighlightValueChange);
            }));

            _tableView.columns.Add(MakeColumn<StatusView>("Status", 100, (v, r) => v.Bind(r)));

            _tableView.columns.Add(MakeColumn<Label>("Measurements", 150, (label, r) =>
            {
                var callsString = Application.isPlaying
                    ? $"Calls: {r.Measures.CallCount} / {Time.frameCount}"
                    : $"Calls: {r.Measures.CallCount}";
                var avgTimeString = $"Avg Time: {r.Measures.AvgExecutionTimeMs:F4} ms";
                var callsPerFrame = $"Calls per Frame: {r.Measures.CallsPerFrame}";
                if (r.Measures.CallsPerFrame > 1)
                {
                    callsPerFrame = ("⚠ " + callsPerFrame).RT().Bold().Color(BindColors.Error);
                }

                var tooltip = "";
                if (r.Measures.AvgExecutionTimeMs > _executionTimeThreshold)
                {
                    avgTimeString = ("⚠ " + avgTimeString).RT().Bold().Color(BindColors.Debug);
                    tooltip = "High average execution time!";
                }

                label.text = $"{callsString}\n{avgTimeString}\n{callsPerFrame}";
#if BS_DEBUG
                label.text += $"\nPath: {r.Proxy?.RuntimePath}".RT().Color(BindColors.Debug);
#endif
                if (label.tooltip != tooltip) label.tooltip = tooltip;
            }));

            _tableView.itemsSource = _filteredStageRows;
            root.Add(_tableView);
        }

        private Column MakeSimpleColumn(string title, float width, Func<StageRowData, string> valueGetter)
        {
            return new Column()
            {
                name = title,
                title = title,
                sortable = true,
                resizable = true,
                width = width,
                makeCell = () => new Label().WithClass("bs-monitor-cell", "bs-monitor-data__" + title.ToLower()),
                bindCell = (v, i) =>
                {
                    if (v is not Label label) return;
                    if (i < 0 || i >= _filteredStageRows.Count) return;

                    label.text = valueGetter(_filteredStageRows[i]);
                    label.tooltip = label.text;
                }
            };
        }

        private Column MakeColumn<T>(string title, float width, Action<T, StageRowData> updateAction,
            bool sortable = true, bool resizable = true)
            where T : VisualElement, new()
        {
            return new Column()
            {
                name = title,
                title = title,
                sortable = sortable,
                resizable = resizable,
                width = width,
                makeCell = () => new T().WithClass("bs-monitor-cell", "bs-monitor-data__" + title.ToLower()),
                bindCell = (v, i) =>
                {
                    if (v is not T view) return;
                    if (i < 0 || i >= _filteredStageRows.Count) return;

                    var item = _filteredStageRows[i];
                    item.Cells[title] = new CellData
                    {
                        View = view,
                        Update = () => updateAction(view, item)
                    };
                    
                    if (!EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
                    
                    if (view is BindView bindView)
                    {
                        bindView.RemoveHighlight();
                        bindView.CanHighlight = false;
                        updateAction(view, item);
                        bindView.CanHighlight = true;
                    }
                    else
                    {
                        updateAction(view, item);
                    }
                },
                unbindCell = (v, i) =>
                {
                    if (v is not T view) return;
                    if (i < 0 || i >= _filteredStageRows.Count) return;

                    var item = _filteredStageRows[i];
                    item.Cells.Clear();
                    // if (item.Cells.TryGetValue(title, out var cell) && cell.View == view)
                    // {
                    //     item.Cells.Remove(title);
                    // }
                }
            };
        }

        private string ValueToString(object value)
        {
            if (value == null) return "<null>";
            if (value is string str) return $"\"{str}\"";
            if (value is float f) return f.ToString("F3");
            if (value is double d) return d.ToString("F3");
            if (value is Vector2 v2) return $"({v2.x:F2}, {v2.y:F2})";
            if (value is Vector3 v3) return $"({v3.x:F2}, {v3.y:F2}, {v3.z:F2})";
            if (value is Color c) return $"RGBA({c.r:F2}, {c.g:F2}, {c.b:F2}, {c.a:F2})";
            return value.ToString();
        }

        // Build or rebuild a typed value field (ColorField, Toggle, etc.)
        private (VisualElement field, Action<object> SetFieldValue) BuildOrRebuildValueField(object value,
            Action<object> rebuildFieldView, Action onValueChanged)
        {
            // This method is slow because it recreates the field at every update.
            // TODO: Add ref parameters to reuse existing fields when type matches.
            switch (value)
            {
                // Null value explicit label
                case null:
                {
                    var nullLabel = new Label("<null>");
                    return (nullLabel, v =>
                    {
                        if (v == null)
                        {
                            nullLabel.text = "<null>";
                        }
                        else
                        {
                            rebuildFieldView?.Invoke(v);
                        }
                    });
                }
                // Unity Object
                case UnityEngine.Object uo:
                {
                    var field = new ObjectField { objectType = uo.GetType(), value = uo, allowSceneObjects = true };
                    field.SetEnabled(false);
                    field.RegisterValueChangedCallback(_ => onValueChanged?.Invoke());
                    return (field, v =>
                    {
                        if (v is Object u) field.value = u;
                        else if (v == null) field.value = null;
                        else rebuildFieldView?.Invoke(v);
                    });
                }
                case Color col:
                {
                    var field = new ColorField { value = col };
                    field.SetEnabled(false);
                    field.RegisterValueChangedCallback(_ => onValueChanged?.Invoke());
                    return (field, v =>
                    {
                        if (v is Color c) field.value = c;
                        else rebuildFieldView?.Invoke(v);
                    });
                }
                case bool b:
                {
                    var field = new Toggle { value = b };
                    field.SetEnabled(false);
                    field.RegisterValueChangedCallback(_ => onValueChanged?.Invoke());
                    return (field, v =>
                    {
                        if (v is bool bb) field.value = bb;
                        else rebuildFieldView?.Invoke(v);
                    });
                }
                case int i:
                {
                    var field = new IntegerField { value = i };
                    field.SetEnabled(false);
                    field.RegisterValueChangedCallback(_ => onValueChanged?.Invoke());
                    return (field, v =>
                    {
                        if (v is int ii) field.value = ii;
                        else rebuildFieldView?.Invoke(v);
                    });
                }
                case long l:
                {
                    var field = new LongField() { value = l };
                    field.SetEnabled(false);
                    field.RegisterValueChangedCallback(_ => onValueChanged?.Invoke());
                    return (field, v =>
                    {
                        if (v is long ii) field.value = ii;
                        else rebuildFieldView?.Invoke(v);
                    });
                }
                case float f:
                {
                    var field = new FloatField { value = f };
                    field.SetEnabled(false);
                    field.RegisterValueChangedCallback(_ => onValueChanged?.Invoke());
                    return (field, v =>
                    {
                        if (v is float ff) field.value = ff;
                        else rebuildFieldView?.Invoke(v);
                    });
                }
                case double d:
                {
                    var field = new DoubleField { value = d };
                    field.SetEnabled(false);
                    field.RegisterValueChangedCallback(_ => onValueChanged?.Invoke());
                    return (field, v =>
                    {
                        if (v is double dd) field.value = dd;
                        else rebuildFieldView?.Invoke(v);
                    });
                }
                case Vector2 v2:
                {
                    var field = new Vector2Field { value = v2 };
                    field.SetEnabled(false);
                    field.RegisterValueChangedCallback(_ => onValueChanged?.Invoke());
                    return (field, v =>
                    {
                        if (v is Vector2 vv2) field.value = vv2;
                        else rebuildFieldView?.Invoke(v);
                    });
                }
                case Vector3 v3:
                {
                    var field = new Vector3Field { value = v3 };
                    field.SetEnabled(false);
                    field.RegisterValueChangedCallback(_ => onValueChanged?.Invoke());
                    return (field, v =>
                    {
                        if (v is Vector3 vv3) field.value = vv3;
                        else rebuildFieldView?.Invoke(v);
                    });
                }
                case Vector4 v4:
                {
                    var field = new Vector4Field { value = v4 };
                    field.SetEnabled(false);
                    field.RegisterValueChangedCallback(_ => onValueChanged?.Invoke());
                    return (field, v =>
                    {
                        if (v is Vector4 vv4) field.value = vv4;
                        else rebuildFieldView?.Invoke(v);
                    });
                }
                case Enum e:
                {
                    var field = new EnumField(e);
                    field.SetEnabled(false);
                    field.RegisterValueChangedCallback(_ => onValueChanged?.Invoke());
                    return (field, v =>
                    {
                        if (v is Enum ee && ee.GetType() == e.GetType()) field.value = ee;
                        else rebuildFieldView?.Invoke(v);
                    });
                }
                case string s:
                {
                    var field = new TextField
                    {
                        value = s,
                        isReadOnly = true,
                        multiline = false
                    };
                    field.RegisterValueChangedCallback(_ => onValueChanged?.Invoke());
                    return (field, v =>
                    {
                        if (v is string ss) field.value = ss;
                        else rebuildFieldView?.Invoke(v);
                    });
                }
                case Gradient g:
                {
                    var field = new GradientField { value = g };
                    field.SetEnabled(false);
                    // field.RegisterValueChangedCallback(_ => onValueChanged?.Invoke());
                    return (field, v =>
                    {
                        if (v is Gradient gg)
                        {
                            if(GradientsAreEqual(field.value, gg)) return;
                            // To avoid a memory leak, we need to detach and reattach the field
                            var parent = field.parent;
                            var index = parent.IndexOf(field);
                            // But first destroy the background texture to avoid memory leak
                            var bgElement = field.Q(null, GradientField.backgroundUssClassName);
                            if (bgElement != null)
                            {
                                var bgTexture = bgElement.style.backgroundImage.value.texture;
                                if (bgTexture != null)
                                {
                                    DestroyImmediate(bgTexture);
                                    bgElement.style.backgroundImage = new StyleBackground((Texture2D)null);
                                }
                            }
                            field.RemoveFromHierarchy();
                            field.value = gg;
                            parent.Insert(index, field);
                            onValueChanged?.Invoke();
                        }
                        else
                        {
                            rebuildFieldView?.Invoke(v);
                        }
                    });
                }
            }

            // Fallback label
            var fallback = new Label(ValueToString(value));
            return (fallback, v =>
            {
                if (v == null || v.GetType() != value.GetType())
                {
                    rebuildFieldView?.Invoke(v);
                }
                else
                {
                    fallback.text = ValueToString(v);
                }
            });
        }

        private static bool GradientsAreEqual(Gradient a, Gradient b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;
            if (a.mode != b.mode) return false;
            if (a.colorKeys.Length != b.colorKeys.Length) return false;
            if (a.alphaKeys.Length != b.alphaKeys.Length) return false;

            for (int i = 0; i < a.colorKeys.Length; i++)
            {
                if (a.colorKeys[i].color != b.colorKeys[i].color) return false;
                if (Math.Abs(a.colorKeys[i].time - b.colorKeys[i].time) > 0.0001f) return false;
            }

            for (int i = 0; i < a.alphaKeys.Length; i++)
            {
                if (Math.Abs(a.alphaKeys[i].alpha - b.alphaKeys[i].alpha) > 0.0001f) return false;
                if (Math.Abs(a.alphaKeys[i].time - b.alphaKeys[i].time) > 0.0001f) return false;
            }

            return true;
        }

        private Type GetExpectedFieldType(object value)
        {
            return value switch
            {
                null => typeof(Label),
                Object => typeof(ObjectField),
                Color => typeof(ColorField),
                bool => typeof(Toggle),
                int => typeof(IntegerField),
                long => typeof(Label),
                float => typeof(FloatField),
                double => typeof(DoubleField),
                Vector2 => typeof(Vector2Field),
                Vector3 => typeof(Vector3Field),
                Vector4 => typeof(Vector4Field),
                Enum => typeof(EnumField),
                string => typeof(TextField),
                _ => typeof(Label)
            };
        }

        #region [  INTERNAL CLASSES  ]

        private class StageRowData : IDisposable
        {
            public readonly int Id;
            private readonly BindMonitorWindow _owner;
            public readonly BindingEngineInternal.IStageData StageData;
            public readonly BindProxy Proxy;
            
            public string Stages;

            public int MergedCount;
            public MeasuresData Measures;

            public readonly Dictionary<string, CellData> Cells = new();

            public double LastChangeTime;
            public VisualElement RowElement;
            public Label StatusLabel;

            public StageRowData(BindMonitorWindow owner, BindingEngineInternal.IStageData data, BindProxy proxy)
            {
                _owner = owner;
                StageData = data;
                Proxy = proxy;
                Stages = data.StageName;
                Id = data.Id;
                Measures = new MeasuresData(MeasurementSampleCount);
                MergedCount = 1;

                data.OnUpdate += DataUpdated;
            }

            public StageRowData(BindMonitorWindow owner, IBind bind, string stages)
            {
                _owner = owner;
                StageData = null;
                Proxy = null;
                Stages = stages;
                Id = bind is IDataRefresher dr ? dr.RefreshId.GetHashCode() : 0;
                Measures = new MeasuresData(MeasurementSampleCount);
            }

            private void DataUpdated(BindingEngineInternal.IStageData data)
            {
                Measures.TryUpdate(StageData);
            }

            public void RefreshDraw()
            {
                foreach (var cellData in Cells.Values)
                {
                    cellData.Update?.Invoke();
                }
            }
            
            public void MergeWith(StageRowData other)
            {
                if (other == null) return;
                if (other == this) return;
                Stages = Stages.Replace(other.Stages, "").Trim() + '\n' + other.Stages;
                other.StageData.OnUpdate -= other.DataUpdated;
                other.StageData.OnUpdate += DataUpdated;
            }

            public void Dispose()
            {
                Cells.Clear();
                StageData.OnUpdate -= DataUpdated;
            }
        }

        private struct CellData
        {
            public VisualElement View;
            public Action Update;
        }

        private struct MeasuresData
        {
            private const int MaxSamples = 100;
            
            private int _lastComputeFrame;

            public int CallsPerFrame;
            public int CallCount;
            public double AvgExecutionTimeMs;
            
            public Dictionary<int, MovingAverage> Averages;
            
            public int AvgMemoryUsage;
            
            public MeasuresData(int sampleCount = MaxSamples)
            {
                CallCount = 0;
                CallsPerFrame = 0;
                _lastComputeFrame = -1;
                AvgExecutionTimeMs = new MovingAverage(sampleCount);
                AvgMemoryUsage = 0;
                Averages = new Dictionary<int, MovingAverage>();
            }

            public bool TryUpdate(BindingEngineInternal.IStageData data)
            {
                CallCount = data.TotalExecutionsCount;
                if (CallCount <= 0) return false;

                if (!Application.isPlaying || _lastComputeFrame != Time.frameCount)
                {
                    CallsPerFrame = 0;
                    AvgExecutionTimeMs = 0;
                    _lastComputeFrame = Time.frameCount;
                }

                if (!Averages.TryGetValue(data.Id, out var avg))
                {
                    avg = new MovingAverage(MeasurementSampleCount);
                    Averages[data.Id] = avg;
                }
                
                AvgExecutionTimeMs += avg.AddSample(data.CurrentExecutionTimeMs);
                CallsPerFrame++;
                return true;
            }

            public void Reset()
            {
                CallCount = 0;
                CallsPerFrame = 0;
                _lastComputeFrame = -1;
                AvgExecutionTimeMs = 0;
                AvgMemoryUsage = 0;
                Averages.Clear();
            }
        }

        private class BindView : VisualElement
        {
            private VisualElement _valueField;
            public ObjectField SourceField;
            public TextField PathField;
            public VisualElement ValueContainer;
            public Action<object> UpdateValueAction;

            public Type ValueType;

            public bool CanHighlight;

            public VisualElement ValueField
            {
                get => _valueField;
                set
                {
                    if (_valueField == value) return;
                    _valueField?.RemoveFromHierarchy();
                    ValueContainer.Clear();
                    _valueField = value;
                    if (_valueField != null)
                    {
                        if (_valueField.GetType().TryGetGenericArguments(typeof(BaseField<>), out var args))
                        {
                            ValueType = args[0];
                        }

                        ValueContainer.Add(_valueField);
                    }
                    else
                    {
                        ValueType = null;
                    }
                }
            }

            public BindView()
            {
                AddToClassList("bs-monitor-bind");
                SourceField = new ObjectField { objectType = typeof(Object), allowSceneObjects = true };
                SourceField.AddToClassList("bs-monitor-bind__source");
                SourceField.RegisterValueChangedCallback(evt =>
                {
                    EnableInClassList("bs-monitor-bind--error", evt.newValue == null);
                });
                PathField = new TextField { isReadOnly = true, multiline = false };
                PathField.AddToClassList("bs-monitor-bind__path");
                ValueContainer = new VisualElement();
                ValueContainer.AddToClassList("bs-monitor-bind__value");

                Add(new VisualElement().WithClass("bs-monitor-bind__row", "bs-monitor-bind__first-row")
                    .WithChildren(SourceField, PathField));
                Add(new VisualElement().WithClass("bs-monitor-bind__row", "bs-monitor-bind__second-row")
                    .WithChildren(new Label("Value").WithClass("bs-monitor-bind__value-label"), ValueContainer));
            }

            public void HighlightValueChange()
            {
                if (!CanHighlight) return;

                AddToClassList("bs-monitor-bind--value-changed");
                schedule.Execute(() => RemoveFromClassList("bs-monitor-bind--value-changed")).ExecuteLater(300);
            }
            
            public void RemoveHighlight()
            {
                RemoveFromClassList("bs-monitor-bind--value-changed");
            }
        }

        private class StatusView : VisualElement
        {
            public Label StatusLabel;
            public VisualElement ActionsContainer;
            public Button FocusButton;
            public Button PauseButton;
            public Button ResumeButton;
            public Toggle OptimizeToggle;

            public StageRowData BoundData;

            public StatusView()
            {
                AddToClassList("bs-monitor-status");
                StatusLabel = new Label().WithClass("bs-monitor-status__status");
                ActionsContainer = new VisualElement().WithClass("bs-monitor-status__actions");
                FocusButton = new Button(FocusOnBind)
                {
                    focusable = false,
                    tooltip = "Focus on Bind in Hierarchy"
                }.WithClass("bs-monitor-status__action", "bs-monitor-status__action--focus");

                PauseButton = new Button(PauseUpdate)
                {
                    focusable = false,
                    tooltip = "Pause Updates for this Bind"
                }.WithClass("bs-monitor-status__action", "bs-monitor-status__action--pause");

                ResumeButton = new Button(ResumeUpdate)
                {
                    focusable = false,
                    tooltip = "Resume Updates for this Bind"
                }.WithClass("bs-monitor-status__action", "bs-monitor-status__action--resume");

                OptimizeToggle = new Toggle()
                {
                    focusable = false,
                    tooltip =
                        "Optimize updates for this Bind. Set value only if changed and other tricks are applied to improve performance." +
                        "\n<i>Note: On rare occasions this may cause some updates to actually decrease performance or skip updates entirely. Check on each bind if it's worth it.</i>"
                }.WithClass("unity-button", "bs-monitor-status__action", "bs-monitor-status__action--optimize");

                OptimizeToggle.RegisterValueChangedCallback(evt =>
                {
                    if (BoundData?.Proxy?.BindData == null) return;
                    BoundData.Proxy.GetBindData().TryEnableFlag(BindData.BitFlags.OptimizeUpdate, evt.newValue);
                    OptimizeToggle.EnableInClassList("bs-monitor-status__action--optimize-enabled",
                        BoundData.Proxy.GetBindData().Flags.HasFlag(BindData.BitFlags.OptimizeUpdate));
                });

                ActionsContainer.Add(
                    FocusButton.WithChildren(new Image().WithClass("bs-monitor-status__action__icon")));
                ActionsContainer.Add(
                    PauseButton.WithChildren(new Image().WithClass("bs-monitor-status__action__icon")));
                ActionsContainer.Add(
                    ResumeButton.WithChildren(new Image().WithClass("bs-monitor-status__action__icon")));
                ActionsContainer.Add(
                    OptimizeToggle.WithChildren(new Image().WithClass("bs-monitor-status__action__icon")));

                Add(StatusLabel);
                Add(ActionsContainer);
            }

            private void FocusOnBind()
            {
                BindDataDrawer.Focus(BoundData.Proxy?.Source, BoundData.Proxy?.Path);
            }

            private void PauseUpdate()
            {
                BoundData.StageData.IsPaused = true;
                EnableInClassList("bs-monitor-status--paused", true);
            }

            private void ResumeUpdate()
            {
                BoundData.StageData.IsPaused = false;
                EnableInClassList("bs-monitor-status--paused", false);
            }


            public void Bind(StageRowData data)
            {
                if (BoundData == data)
                {
                    return;
                }
                
                EnableInClassList("bs-monitor-status--phased", data.Proxy.BindProxyPair?.IsPhasedBind == true);

                RemoveFromClassList("bs-monitor-status--paused");
                BoundData = data;

                OptimizeToggle.value =
                    data.Proxy?.GetBindData().Flags.HasFlag(BindData.BitFlags.OptimizeUpdate) == true;

                var sd = data.StageData;
                var (frames, seconds) = (Mathf.Max(1, sd.UpdateFrameInterval), sd.UpdateTimeInterval);
                StatusLabel.text = (frames, seconds) switch
                {
                    (_, > 0) => Mathf.Approximately(seconds, 1)
                        ? "Every second"
                        : $"Every {seconds:F2} seconds",
                    (> 0, _) => frames == 1 ? "Every Frame" : $"Every {frames} frames",
                    _ => "Controlled"
                };
                StatusLabel.tooltip =
                    (frames, seconds) switch
                    {
                        (_, > 0) => $"Updates every {seconds:F2} seconds",
                        (> 0, _) => $"Updates every {frames} frames",
                        _ => "Controlled Trigger - does not update automatically"
                    };
            }
        }

        private class Footer : VisualElement
        {
            private readonly BindMonitorWindow _owner;
            private readonly Label _totalExecutionTimeLabel;
            private readonly Label _totalElementsLabel;
            private readonly Label _phasedEnabledLabel;
            private readonly Label _statusLabel;
            
            public string Status
            {
                get => _statusLabel.text;
                set
                {
                    var previousStatus = _statusLabel.text?.ToLower() ?? "ready";
                    _statusLabel.text = value?.ToUpper();
                    _statusLabel.RemoveFromClassList("bs-monitor-footer__status--" + previousStatus);
                    _statusLabel.AddToClassList("bs-monitor-footer__status--" + value?.ToLower() ?? "ready");
                }
            }

            public Footer(BindMonitorWindow owner)
            {
                _owner = owner;

                AddToClassList("bs-monitor-footer");
                _totalElementsLabel =
                    new Label().WithClass("bs-monitor-footer__info", "bs-monitor-footer__info--elements");
                _totalExecutionTimeLabel =
                    new Label().WithClass("bs-monitor-footer__info", "bs-monitor-footer__info--time");

                _phasedEnabledLabel = new Label("Phased Bindings")
                {
                    tooltip = "Phased Bindings are enabled in the Binding System settings. They help reduce binding updates when redundant to improve performance."
                }.WithClass("bs-monitor-footer__phased");
                _phasedEnabledLabel.schedule.Execute(() => _phasedEnabledLabel.EnableInClassList("bs-monitor-footer__phased--active", BindSystem.Options.UsePhasedUpdates)).Every(1000);
                _statusLabel = new Label("Ready").WithClass("bs-monitor-footer__status");

                Add(_totalElementsLabel);
                Add(_totalExecutionTimeLabel);
                Add(_phasedEnabledLabel);
                Add(_statusLabel);
            }

            public void Refresh()
            {
                var totalExecutionTime =
                    _owner._allStageRows.Sum(r => r.StageData.IsPaused ? 0 : r.Measures.AvgExecutionTimeMs);

                if (_owner._allStageRows.Count == _owner._filteredStageRows.Count)
                {
                    _totalElementsLabel.text = $"Elements: {_owner._allStageRows.Count}";
                    _totalExecutionTimeLabel.text = $"Total Exec Time: {totalExecutionTime:F4} ms";
                }
                else
                {
                    _totalElementsLabel.text =
                        $"Elements: {_owner._filteredStageRows.Count}/{_owner._allStageRows.Count} ({(_owner._filteredStageRows.Count / (float)_owner._allStageRows.Count):P1})";
                    var partialExecutionTime =
                        _owner._filteredStageRows.Sum(r => r.StageData.IsPaused ? 0 : r.Measures.AvgExecutionTimeMs);
                    _totalExecutionTimeLabel.text =
                        $"Total Exec Time: {partialExecutionTime:F4} ms\t({(partialExecutionTime / totalExecutionTime):P1})\t/ {totalExecutionTime:F4} ms";
                }
            }
        }

        public class InactiveDataUpdater : BindingEngineInternal.IDataUpdater
        {
            private List<SimpleStageData> _datas = new();
            
            public string Name => "Inactive";

            public IReadOnlyList<BindingEngineInternal.IStageData> AllDataUpdaters => _datas;

            public void Refresh(IEnumerable<BindProxy> proxies)
            {
                var index = 0;
                foreach (var proxy in proxies)
                {
                    if (_datas.Count <= index)
                    {
                        _datas.Add(new SimpleStageData("Inactive", proxy));
                    }
                    else
                    {
                        _datas[index].Bind(proxy);
                    }

                    index++;
                }
                
                while (_datas.Count > index)
                {
                    _datas.RemoveAt(_datas.Count - 1);
                }
            }
        }
        
        public class SimpleStageData : BindingEngineInternal.IStageData
        {
            public int Id { get; set; }

            public Object Context { get; set; }

            public string StageName { get; set; }

            public double CurrentExecutionTimeMs => 0;

            public int TotalExecutionsCount => 0;

            public bool IsPaused { get; set; }

            public int UpdateFrameInterval => 0;

            public float UpdateTimeInterval => 0;

            #pragma warning disable
            public event Action<BindingEngineInternal.IStageData> OnUpdate;
            #pragma warning restore

            public SimpleStageData(string stageName, BindProxy proxy)
            {
                Id = proxy.Id;
                Context = proxy.Source;
                StageName = stageName;
            }

            public SimpleStageData()
            {
                // For empty rows
            }
            
            public void Bind(BindProxy proxy)
            {
                Id = proxy.Id;
                Context = proxy.Source;
            }
        }

        #endregion
    }
}