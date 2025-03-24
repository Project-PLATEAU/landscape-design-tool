using System;
using Button = UnityEngine.UIElements.Button;

namespace Landscape2.Runtime.UiCommon
{
    /// <summary>
    /// ボタンを押したあと、一定時間ボタンのテキストが別のメッセージに置き換わるボタンです。
    /// 毎フレームUpdateの呼び出しが必要です。
    /// </summary>
    public class ButtonWithClickMessage
    {
        public Button Button { get; private set; }
        
        /// <summary> クリックで一定時間だけ表示されるボタンテキスト</summary>
        private string clickedMessage;
        
        /// <summary> 通常時のボタンテキスト </summary>
        private string buttonTextNormal;
        
        private float timeToResetButton;
        
        /// <summary>
        /// ボタンを押してから、ボタンのテキストを「記憶しました」や「復元しました」に変更する秒数。この秒数の後はもとに戻る。
        /// </summary>
        private const float TimeToChangeText = 3f;

        public ButtonWithClickMessage(Button button, string clickedMessage, Action onClicked)
        {
            this.Button = button;
            this.clickedMessage = clickedMessage;
            buttonTextNormal = button.text;
            
            button.clicked += () =>
            {
                onClicked();
                DisplayClickedMessage();
            };
        }

        public void Update(float deltaTime)
        {
            // ボタンを押してテキストが変わった後、一定時間でテキストを戻す
            timeToResetButton -= deltaTime;
            if (timeToResetButton is <= 0f and >= -100f)
            {
                Button.text = buttonTextNormal;
                timeToResetButton = -9999f;
            }
        }

        public string NormalButtonText
        {
            get => Button.text;
            set
            {
                Button.text = value;
                buttonTextNormal = value;
            }
        }

        private void DisplayClickedMessage()
        {
            Button.text = clickedMessage;
            timeToResetButton = TimeToChangeText;
        }
        
    }
}