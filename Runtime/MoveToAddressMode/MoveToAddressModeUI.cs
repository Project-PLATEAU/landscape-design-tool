using UnityEngine;
using UnityEngine.UIElements;
using Cinemachine;
using Landscape2.Runtime.UiCommon;
using System.Collections.Generic;

namespace Landscape2.Runtime.MoveToAddressMode
{
    public class MoveToAddressModeUI : ISubComponent
    {
        private const string UIListContainer = "unity-content-container";
        private const string UIAdressButton = "List_Adress";
        private const string UIAdressList = "Panel_AdressList";

        private MoveToAddressMode moveToAddressMode;
        private LandscapeCamera landscapeCamera;
        private VisualElement root;
        private TextField textField;
        private bool isFindingAddress = false;
        private static bool isInputFieldFocused = false;
        private bool isMouseOverUI = false;
        private bool isErrorDialogOpen = false;
        private string lastInputText = "";
        private int buttonIndex = -1;
        private int buttonCount = 0;

        public MoveToAddressModeUI(VisualElement uiRoot, CinemachineVirtualCamera mainCamera, CinemachineVirtualCamera walkerCamera, LandscapeCamera landscapeCamera)
        {
            moveToAddressMode = new MoveToAddressMode(mainCamera, walkerCamera);
            this.landscapeCamera = landscapeCamera;
            root = uiRoot;

            root.RegisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.NoTrickleDown);
            root.RegisterCallback<PointerLeaveEvent>(OnPointerLeave, TrickleDown.NoTrickleDown);


            textField = this.root.Q<TextField>("AdressInputField");
            textField.RegisterCallback<FocusInEvent>(evt => OnFocusIn());
            textField.RegisterCallback<FocusOutEvent>(evt => OnFocusOut());
            textField.RegisterCallback<KeyDownEvent>(evt =>
            {
                if(evt.keyCode == KeyCode.UpArrow)
                {
                    ListControll(true);
                }
                else if(evt.keyCode == KeyCode.DownArrow)
                {
                    ListControll(false);
                }
                else if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                {
                    OnInputEnter();
                }
            }, TrickleDown.TrickleDown);

            textField.RegisterCallback<ChangeEvent<string>>(evt =>
            {
                buttonIndex = -1;
                OnDrawSuggest();
            });

            var resetButton = this.root.Q<Button>("ResetButton");
            resetButton.clicked += () =>
            {
                if(isErrorDialogOpen)
                    return;
                textField.value = "";
                textField.Focus();
            };

            var okButton = this.root.Q<Button>("OKButton");
            okButton.clicked += () =>
            {
                OnInputEnter();
            };

            var adList = this.root.Q<VisualElement>(UIAdressList);
            adList.style.display = DisplayStyle.None;
        }

        public static bool IsInputFieldFocused()
        {
            return isInputFieldFocused;
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            isMouseOverUI = true;
        }

        private void OnPointerLeave(PointerLeaveEvent evt)
        {
            isMouseOverUI = false;
        }

        private void OnFocusIn()
        {
            isInputFieldFocused = true;
            landscapeCamera.OnUserInputTrigger(isMouseOverUI);
        }
        private void OnFocusOut()
        {
            isInputFieldFocused = false;
            landscapeCamera.OnUserInputTrigger(isMouseOverUI);
        }

        private async void OnInputEnter()
        {
            var adList = this.root.Q<VisualElement>(UIAdressList);
            adList.style.display = DisplayStyle.None;
            buttonCount = 0;

            if (isFindingAddress || isErrorDialogOpen || moveToAddressMode == null || moveToAddressMode.IsFindingAddress())
            {
                return;
            }

            if( string.IsNullOrEmpty(textField.value))
            {
                // 空文字列の場合は何もしない
                return;
            }

            // フォーカス外す
            textField.Blur();

            // 住所検索
            isFindingAddress = true;
            Debug.Log("Search Address: " + textField.value);
            MoveToAddressMode.AddressMoveStatus status = await moveToAddressMode.MoveToAddress(textField.value, landscapeCamera);
            isFindingAddress = false;
            if (status != MoveToAddressMode.AddressMoveStatus.Success)
            {
                ShowErrorDialog(status);
            }
        }

        private async void OnDrawSuggest()
        {
            if (isFindingAddress || isErrorDialogOpen || moveToAddressMode == null || moveToAddressMode.IsFindingAddress())
            {
                return;
            }

            var adList = this.root.Q<VisualElement>(UIAdressList);
            if (string.IsNullOrEmpty(textField.value))
            {
                // 空文字列の場合は何もしない
                adList.style.display = DisplayStyle.None;
                buttonCount = 0;
                return;
            }

            string inputText = textField.value;
            // 空白除去
            inputText = inputText.Replace("　", "").Replace(" ", "");
            if (string.IsNullOrEmpty(inputText))
            {
                adList.style.display = DisplayStyle.None;
                buttonCount = 0;
                return;
            }
            if(inputText == lastInputText)
                return;

            lastInputText = inputText;
            // 住所候補の表示
            List<string> list = await moveToAddressMode.GetSuggestList(inputText);
            var adListContent = adList.Q<VisualElement>(UIListContainer);
            if (list.Count > 0)
            {
                adListContent.Clear();
                foreach (var addr in list)
                {
                    var elm = Resources.Load<VisualTreeAsset>(UIAdressButton).CloneTree();
                    var btn = elm.Q<Button>(UIAdressButton);
                    btn.clicked += () =>
                    {
                        textField.value = addr;
                        textField.Focus();
                    };

                    // フォーカス中はホバーと同じ色に変える
                    btn.RegisterCallback<FocusInEvent>(evt =>
                    {
                        btn.style.backgroundColor = new Color(238f / 255f, 238f / 255f, 238f / 255f, 1f);
                    });
                    btn.RegisterCallback<FocusOutEvent>(evt =>
                    {
                        btn.style.backgroundColor = Color.white;
                    });
                    btn.RegisterCallback<PointerEnterEvent>(evt =>
                    {
                        btn.style.backgroundColor = new Color(238f / 255f, 238f / 255f, 238f / 255f, 1f);
                    });
                    btn.RegisterCallback<PointerLeaveEvent>(evt =>
                    {
                        if (btn.focusController.focusedElement != btn)
                        {
                            btn.style.backgroundColor = Color.white;
                        }
                    });


                    btn.RegisterCallback<NavigationMoveEvent>(evt =>
                    {
                        if (evt.direction == NavigationMoveEvent.Direction.Up)
                        {
                            ListControll(true);
                        }
                        else if (evt.direction == NavigationMoveEvent.Direction.Down)
                        {
                            ListControll(false);
                        }
                        evt.StopPropagation();
                        btn.focusController.IgnoreEvent(evt);
                    }, TrickleDown.TrickleDown);

                    var adressLabel = elm.Q<Label>("AdressName");
                    adressLabel.text = addr;
                    adListContent.Add(elm);
                }
                buttonCount = list.Count;
                adList.style.display = DisplayStyle.Flex;

                var adListScroll = adList.Q<ScrollView>("Panel");
                adListScroll.scrollOffset = Vector2.zero;
            }
            else
            {
                if(adListContent.childCount > 0)
                {
                    adListContent.Clear();
                }
                buttonCount = 0;
                adList.style.display = DisplayStyle.None;
            }
        }

        private void ListControll(bool up)
        {
            if (buttonCount < 1)
                return;

            var adList = this.root.Q<VisualElement>(UIAdressList);
            var adListContent = adList.Q<VisualElement>(UIListContainer);
            if (up)
            {
                buttonIndex--;
                if (buttonIndex < 0)
                {
                    if (buttonIndex == -1)
                    {
                        textField.Focus();  // 入力欄にフォーカス
                        return;
                    }
                    else
                    {
                        buttonIndex = buttonCount - 1;
                    }
                }
            }
            else
            {
                buttonIndex++;
                if (buttonIndex >= buttonCount)
                {
                    buttonIndex = 0;
                }
            }

            // フォーカスしたボタンが表示されるようにスクロール
            var adListScroll = adList.Q<ScrollView>("Panel");
            var focusedBtn = adListScroll.contentContainer.ElementAt(buttonIndex);
            adListScroll.ScrollTo(focusedBtn);

            var btn = focusedBtn.Q<Button>(UIAdressButton);
            btn.Focus();
        }

        private void ShowErrorDialog(MoveToAddressMode.AddressMoveStatus status)
        {
            isErrorDialogOpen = true;
            textField.SetEnabled(false);
            if (status == MoveToAddressMode.AddressMoveStatus.NoGround)
            {
                ModalUI.ShowModal("住所移動エラー", "指定された住所は対象エリア外のため\n移動できません。\n別の住所を入力してください。", "OK", false, true, CloseErrorDialog);
            }
            else if (status == MoveToAddressMode.AddressMoveStatus.Failed)
            {
                ModalUI.ShowModal("住所移動エラー", "指定された住所が見つかりませんでした。\n住所の表記を確認して、再度入力してください。", "OK", false, true, CloseErrorDialog);
            }
            else if(status == MoveToAddressMode.AddressMoveStatus.NoData)
            {
                ModalUI.ShowModal("住所移動エラー", "住所データが読み込まれていません。", "OK", false, true, CloseErrorDialog);
            }
        }

        private void CloseErrorDialog()
        {
            isErrorDialogOpen = false;
            textField.SetEnabled(true);
        }

        public void Update(float deltaTime)
        {
        }

        public void OnEnable()
        {
        }

        public void OnDisable()
        {
        }

        public void Start()
        {
        }

        public void LateUpdate(float deltaTime)
        {
        }
    }
}
