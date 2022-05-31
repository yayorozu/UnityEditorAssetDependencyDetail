using System;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Yorozu.EditorTool
{
    /// <summary>
    /// アセットの依存を調べて依存アセットのサイズ等を見る
    /// </summary>
    public class AssetDependencyDetailWindow : EditorWindow
    {
        [MenuItem("Tools/AssetDependencyDetail")]
        private static void ShowWindow()
        {
            var window = GetWindow<AssetDependencyDetailWindow>("AssetDependencyDetail");
            window.Show();
        }

        internal static AssetDependencyDetailWindow window;

        private Object _searchTarget;
        [NonSerialized]
        private bool _Initialized;
        [SerializeField]
        private MultiColumnHeaderState _columnHeaderState;
        [SerializeField]
        private TreeViewState _state;
        private DetailTreeView _treeView;

        private void OnEnable()
        {
            window = this;
        }

        private void OnDisable()
        {
            window = null;
        }

        private void InitIfNeeded()
        {
            if (_Initialized)
                return;

            _state ??= new TreeViewState();
            
            var headerState = DetailMultiColumnHeader.Create();
            if (MultiColumnHeaderState.CanOverwriteSerializedFields(_columnHeaderState, headerState))
                MultiColumnHeaderState.OverwriteSerializedFields(_columnHeaderState, headerState);
            _columnHeaderState = headerState;
            
            var multiColumnHeader = new DetailMultiColumnHeader(_columnHeaderState);
            multiColumnHeader.ResizeToFit();
            
            _treeView = new DetailTreeView(_state, multiColumnHeader);
            
            _Initialized = true;
        }

        private void OnGUI()
        {
            InitIfNeeded();

            using (var check = new EditorGUI.ChangeCheckScope())
            {
                _searchTarget = EditorGUILayout.ObjectField("Target", _searchTarget, typeof(Object), false);
                if (check.changed)
                {
                    if (_searchTarget != null)
                    {
                        _treeView.Reload();
                        Repaint();
                    }
                }
            }
            
            var rect = GUILayoutUtility.GetRect(0, 100000, 0, 100000);
            _treeView.OnGUI(rect);
        }

        internal static TreeViewItem SearchDependency()
        {
            var root = new TreeViewItem(0, -1, "root");
            if (window != null && window._searchTarget != null)
            {
                var path = AssetDatabase.GetAssetPath(window._searchTarget);
                var dependencies = AssetDatabase.GetDependencies(path);
                var depth2Dependencies = dependencies
                    .SelectMany(AssetDatabase.GetDependencies)
                    .Distinct();
                
                foreach (var assetPath in depth2Dependencies)
                {
                    root.AddChild(new DetailTreeViewItem(assetPath));
                }
            }
            
            return root;
        }
    }
}
