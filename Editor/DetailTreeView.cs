using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Profiling;
using Object = UnityEngine.Object;

namespace Yorozu.EditorTool
{
    internal class DetailTreeView : TreeView
    {
        internal DetailTreeView(TreeViewState state, MultiColumnHeader multiColumnHeader) : base(state, multiColumnHeader)
        {
            columnIndexForTreeFoldouts = 1;
            baseIndent -= 10f;
            showAlternatingRowBackgrounds = true;
            showBorder = true;
            
            multiColumnHeader.sortingChanged += OnSortingChanged;
            
            Reload();
        }

        /// <summary>
        /// ソート変更
        /// </summary>
        private void OnSortingChanged(MultiColumnHeader multiColumnHeader)
        {
            if (GetRows().Count <= 1)
                return;
            
            Sort(GetRows());
        }

        private void Sort(IList<TreeViewItem> rows)
        {
            var sortedColumns = multiColumnHeader.state.sortedColumns;
            if (multiColumnHeader.sortedColumnIndex == -1 ||
                sortedColumns.Length == 0
               )
                return;
            
            var cast = rootItem.children.Cast<DetailTreeViewItem>();
            var ascending = multiColumnHeader.IsSortedAscending(sortedColumns[0]);
            var items = InitialOrder(cast, ascending);
                                
            for (var i = 1; i < sortedColumns.Length; i++)
            {
                ascending = multiColumnHeader.IsSortedAscending(sortedColumns[i]);
                switch (sortedColumns[i])
                {
                    case 0:
                        items = ThenBy(items, i => i.displayName, ascending);
                        break;
                    case 1:
                        items = ThenBy(items, i => i.Size, ascending);
                        break;
                    case 2:
                        items = ThenBy(items, i => i.Path, ascending);
                        break;
                }
            }
            IOrderedEnumerable<T> ThenBy<T, TKey>(IOrderedEnumerable<T> source, Func<T, TKey> selector, bool ascending)
            {
                return @ascending ? source.ThenBy(selector) : source.ThenByDescending(selector);
            }

            rootItem.children = items.Cast<TreeViewItem>().ToList();
            rows.Clear();
            foreach (var item in rootItem.children)
            {
                rows.Add(item);
            }
            Repaint();
        }

        private IOrderedEnumerable<DetailTreeViewItem> InitialOrder(IEnumerable<DetailTreeViewItem> items, bool ascending)
        {
            switch (multiColumnHeader.state.sortedColumns[0])
            {
                case 1:
                    return Order(items, i => i.Size);
                case 2:
                    return Order(items, i => i.Path);
                default:
                    return Order(items, i => i.displayName);
            }
            
            IOrderedEnumerable<T> Order<T, TKey>(IEnumerable<T> source, Func<T, TKey> selector)
            {
                return @ascending ? source.OrderBy(selector) : source.OrderByDescending(selector);
            }
        }
        
        protected override TreeViewItem BuildRoot()
        {
            var root = AssetDependencyDetailWindow.SearchDependency();
            SetupDepthsFromParentsAndChildren(root);
            return root;
        }
        
        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            if (!root.hasChildren)
                return new List<TreeViewItem>();

            var rows = base.BuildRows(root);
            Sort(rows);
            return rows;
        }
        
        protected override void DoubleClickedItem(int id)
        {
            EditorGUIUtility.PingObject(EditorUtility.InstanceIDToObject(id));
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var item = (DetailTreeViewItem) args.item;

            for (var i = 0; i < args.GetNumVisibleColumns(); ++i)
            {
                var columnIndex = args.GetColumn(i);
                switch (columnIndex)
                {
                    case 0:
                        base.RowGUI(args);
                        break;
                    case 1:
                        EditorGUI.LabelField(args.GetCellRect(columnIndex), item.SizeText);
                        break;
                    case 2:
                        EditorGUI.LabelField(args.GetCellRect(columnIndex), item.Path);
                        break;
                }
            }
        }
    }
    
    internal class DetailMultiColumnHeader : MultiColumnHeader
    {
        internal static MultiColumnHeaderState Create()
        {
            var columns = new MultiColumnHeaderState.Column[]
            {
                new MultiColumnHeaderState.Column()
                {
                    headerContent = new GUIContent("Name"),
                    autoResize = true,
                    allowToggleVisibility = false,
                    width = 130,
                },
                new MultiColumnHeaderState.Column()
                {
                    headerContent = new GUIContent("Size"),
                    autoResize = true,
                    allowToggleVisibility = false,
                    width = 40,
                },
                new MultiColumnHeaderState.Column()
                {
                    headerContent = new GUIContent("Path"),
                    autoResize = true,
                    allowToggleVisibility = false,
                },
            };
         
            return new MultiColumnHeaderState(columns);
        }
        
        internal DetailMultiColumnHeader(MultiColumnHeaderState state) : base(state)
        {
            canSort = true;
            height = 24f;
        }

        protected override void AddColumnHeaderContextMenuItems(GenericMenu menu)
        {
        }
    }

    internal class DetailTreeViewItem : TreeViewItem
    {
        internal string SizeText;
        internal long Size;
        internal string Path;
        
        internal DetailTreeViewItem(string assetPath)
        {
            Path = assetPath;
            depth = 0;
            displayName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            icon = (Texture2D) AssetDatabase.GetCachedIcon(assetPath);
            
            var loadAsset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
            id = loadAsset.GetInstanceID();
            
            Size = Profiler.GetRuntimeMemorySizeLong(loadAsset);
            SizeText = SizeSuffix(Size);
        }
        
        private static readonly string[] SizeSuffixes = {"B", "KB", "MB", "GB"};

        private string SizeSuffix(long value, int decimalPlaces = 1)
        {
            if (value < 0)
            {
                return "-" + SizeSuffix(-value, decimalPlaces);
            }

            var i = 0;
            var dValue = (decimal)value;
            while (Math.Round(dValue, decimalPlaces) >= 1000 && i < SizeSuffixes.Length - 1)
            {
                dValue /= 1024;
                i++;
            }

            return string.Format("{0:n" + decimalPlaces + "} {1}", dValue, SizeSuffixes[i]);
        }
    }
}