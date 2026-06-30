using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LFramework.Editor.Windows
{
    /// <summary>
    /// InjectionDebugWindow - 历史记录和导航
    /// 前进/后退功能
    /// </summary>
    public partial class InjectionDebugWindow
    {
        #region History Data

        private class HistoryEntry
        {
            public int TabIndex;
            public string SearchText;
            public DateTime Timestamp;

            public HistoryEntry(int tab, string search)
            {
                TabIndex = tab;
                SearchText = search ?? "";
                Timestamp = DateTime.Now;
            }

            public override bool Equals(object obj)
            {
                if (obj is HistoryEntry other)
                {
                    return TabIndex == other.TabIndex && SearchText == other.SearchText;
                }
                return false;
            }

            public override int GetHashCode()
            {
                return TabIndex.GetHashCode() ^ SearchText.GetHashCode();
            }
        }

        private List<HistoryEntry> _history = new List<HistoryEntry>();
        private int _historyIndex = -1;
        private bool _isNavigating = false;

        #endregion

        #region History UI

        private void DrawHistoryButtons()
        {
            // 后退按钮
            GUI.enabled = CanGoBack();
            if (GUILayout.Button("←", EditorStyles.toolbarButton, GUILayout.Width(25)))
            {
                GoBack();
            }
            GUI.enabled = true;

            // 前进按钮
            GUI.enabled = CanGoForward();
            if (GUILayout.Button("→", EditorStyles.toolbarButton, GUILayout.Width(25)))
            {
                GoForward();
            }
            GUI.enabled = true;
        }

        #endregion

        #region History Logic

        private void RecordHistory(int tabIndex, string searchText)
        {
            // 如果正在导航，不记录历史
            if (_isNavigating)
                return;

            var newEntry = new HistoryEntry(tabIndex, searchText);

            // 如果和当前记录相同，不添加
            if (_historyIndex >= 0 && _historyIndex < _history.Count)
            {
                if (_history[_historyIndex].Equals(newEntry))
                    return;
            }

            // 删除当前位置之后的所有历史
            if (_historyIndex < _history.Count - 1)
            {
                _history.RemoveRange(_historyIndex + 1, _history.Count - _historyIndex - 1);
            }

            // 添加新记录
            _history.Add(newEntry);
            _historyIndex = _history.Count - 1;

            // 限制历史记录数量
            if (_history.Count > 50)
            {
                _history.RemoveAt(0);
                _historyIndex--;
            }
        }

        private bool CanGoBack()
        {
            return _historyIndex > 0;
        }

        private bool CanGoForward()
        {
            return _historyIndex < _history.Count - 1;
        }

        private void GoBack()
        {
            if (!CanGoBack())
                return;

            _historyIndex--;
            ApplyHistoryEntry(_history[_historyIndex]);
        }

        private void GoForward()
        {
            if (!CanGoForward())
                return;

            _historyIndex++;
            ApplyHistoryEntry(_history[_historyIndex]);
        }

        private void ApplyHistoryEntry(HistoryEntry entry)
        {
            _isNavigating = true;

            _selectedTab = entry.TabIndex;
            _searchText = entry.SearchText;

            Repaint();

            _isNavigating = false;
        }

        private void OnTabChanged(int newTab)
        {
            RecordHistory(newTab, _searchText);
        }

        private void OnSearchChanged(string newSearch)
        {
            // 只有在搜索文本真正改变时才记录
            if (_searchText != newSearch)
            {
                RecordHistory(_selectedTab, newSearch);
            }
        }

        #endregion
    }
}
