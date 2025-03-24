using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using UnityEngine;

namespace Landscape2.Runtime.UiCommon
{
    public class TabUI
    {
        private readonly VisualElement tabRoot;
        private const string ClassNameTab = "tab";
        private const string ClassNameTabRoot = "tabs";
        private const string ClassNameTabSelected = "tab-selected";
        private const string ClassNameContent = "tab-content";
        private const string ClassNameContentRoot = "tab-contents";
        private const string ClassNameContentUnselected = "tab-content-unselected";
        private VisualElement selectedTab;
        private Dictionary<VisualElement, VisualElement> tabToContent;

        public TabUI(VisualElement tabRoot)
        {
            this.tabRoot = tabRoot;
            // var tabs = GetAllTabs().ToList();
            var tabsRoot = tabRoot.Q(className : ClassNameTabRoot);
            var tabs = tabsRoot.Children().Where(e => e.ClassListContains(ClassNameTab)).ToList();

            // タブとコンテンツを紐付けます。
            // タブの順番と、".tab-contents"の子の".tab-content"の順番が一致するものと過程して紐付けます。
            var contentsRoot = tabRoot.Q(className: ClassNameContentRoot);
            var contents = contentsRoot.Children().Where(e => e.ClassListContains(ClassNameContent)).ToList();
            tabToContent = new();
            for (int i = 0; i < tabs.Count; i++)
            {
                tabToContent.Add(tabs[i], contents[i]);
            }

            // タブクリック時のコールバックを追加
            tabs.ForEach(tab => tab.RegisterCallback<ClickEvent>(OnTabClicked));

            ChangeSelectedTab(tabs.First());
        }

        private void OnTabClicked(ClickEvent e)
        {
            var clickedTab = e.currentTarget as VisualElement;
            if (clickedTab == selectedTab) return;
            ChangeSelectedTab(clickedTab);
        }

        private UQueryBuilder<VisualElement> GetAllTabs()
        {
            return tabRoot.Query(className: ClassNameTab);
        }

        private void ChangeSelectedTab(VisualElement nextSelectedTab)
        {
            selectedTab = nextSelectedTab;
            foreach (var (tab, content) in tabToContent)
            {
                if (tab == nextSelectedTab)
                {
                    tab.AddToClassList(ClassNameTabSelected);
                    content.RemoveFromClassList(ClassNameContentUnselected);
                }
                else
                {
                    tab.RemoveFromClassList(ClassNameTabSelected);
                    content.AddToClassList(ClassNameContentUnselected);
                }
            }
        }
    }
}