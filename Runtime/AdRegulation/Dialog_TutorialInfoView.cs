using UnityEngine;
using UnityEngine.UIElements;

namespace Landscape2.Runtime.AdRegulation
{
    public interface IDialog_TutorialInfoView
    {
        public void Set(string title, string desc);


        // 現在利用中のオーナー
        public object CurrentOwner { get; set; }
    }

    public class Dialog_TutorialInfoView : IDialog_TutorialInfoView
    {
        VisualElement root;

        Label Title { get; set; }
        Label Desc { get; set; }
        object IDialog_TutorialInfoView.CurrentOwner { get; set; } = null;

        public Dialog_TutorialInfoView(VisualElement root)
        {
            this.root = root;

            Title = root.Q<Label>("Title");
            Desc = root.Q<Label>("Desc");
        }

        void IDialog_TutorialInfoView.Set(string title, string desc)
        {
            Title.text = title;
            Desc.text = desc;

        }
    }
}
