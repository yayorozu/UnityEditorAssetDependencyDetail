using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.TreeViewExamples;
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
            
            Sort(rootItem, GetRows());
        }

        private void Sort(TreeViewItem root, IList<TreeViewItem> rows)
        {
            var sortedColumns = multiColumnHeader.state.sortedColumns;
            if (multiColumnHeader.sortedColumnIndex == -1 ||
                sortedColumns.Length == 0
               )
                return;
            
            var cast = rootItem.children.Cast<DetailTreeViewItem>();
            var ascending = multiColumnHeader.IsSortedAscending(sortedColumns[0]);
            var items = sortedColumns[0] == 1 ? 
                cast.Order(i => i.Size, ascending) : 
                cast.Order(i => i.displayName, ascending);
                                
            for (var i = 1; i < sortedColumns.Length; i++)
            {
                ascending = multiColumnHeader.IsSortedAscending(sortedColumns[i]);
                switch (sortedColumns[i])
                {
                    case 0:
                        items = items.ThenBy(i => i.displayName, ascending);
                        break;
                    case 1:
                        items = items.ThenBy(i => i.Size, ascending);
                        break;
                }
            }

            rootItem.children = items.Cast<TreeViewItem>().ToList();
            TreeToList(root, rows);
            Repaint();
        }
        
        private static void TreeToList (TreeViewItem root, IList<TreeViewItem> result)
        {
            if (root == null)
                throw new NullReferenceException("root");
            if (result == null)
                throw new NullReferenceException("result");

            result.Clear();
	
            if (root.children == null)
                return;

            var stack = new Stack<TreeViewItem>();
            for (var i = root.children.Count - 1; i >= 0; i--)
                stack.Push(root.children[i]);

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                result.Add(current);

                if (!current.hasChildren || current.children[0] == null) 
                    continue;
                
                for (var i = current.children.Count - 1; i >= 0; i--)
                {
                    stack.Push(current.children[i]);
                }
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
            ; 
            Sort(root, rows);
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
                    // Size
                    case 1:
                        EditorGUI.LabelField(args.GetCellRect(columnIndex), item.SizeText);
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
                },
                new MultiColumnHeaderState.Column()
                {
                    headerContent = new GUIContent("Size"),
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
        
        private string _path;
        
        internal DetailTreeViewItem(string assetPath)
        {
            _path = assetPath;
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