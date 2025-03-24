using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using Landscape2.Runtime.UiCommon;

namespace Landscape2.Runtime
{
    public abstract class LineOfSightModeClass
    {
        public virtual void OnEnable(VisualElement element) { }
        public virtual void OnDisable() { }
        public virtual void OnSelect() { }
    }

    public enum LineOfSightType
    {
        main,
        viewPoint,
        landmark,
        analyzeViewPoint,
        analyzeLandmark
    }

    public class LineOfSight : ISubComponent, LandscapeInputActions.ILineOfSightActions
    {
        private LineOfSightModeClass currentMode;
        private ViewPoint viewPoint;
        private Landmark landmark;
        private AnalyzeViewPoint analyzeViewPoint;
        private AnalyzeLandmark analyzeLandmark;
        private LineOfSightDataComponent lineOfSightDataComponent;
        private LineOfSightSubscribeSaveSystem lineOfSightSubscribeSaveSystem;
        private LineOfSightUI lineOfSightUI;
        private VisualElement lineOfSightUIElement;
        private LandscapeInputActions.LineOfSightActions input;

        public LineOfSight(SaveSystem saveSystemInstance, VisualElement uiRootElement)
        {
            lineOfSightDataComponent = new LineOfSightDataComponent();
            lineOfSightUI = new LineOfSightUI();
            viewPoint = new ViewPoint(lineOfSightDataComponent);
            landmark = new Landmark(lineOfSightDataComponent);
            analyzeViewPoint = new AnalyzeViewPoint(lineOfSightDataComponent);
            analyzeLandmark = new AnalyzeLandmark(lineOfSightDataComponent);
            lineOfSightUIElement = uiRootElement;
            lineOfSightSubscribeSaveSystem = new LineOfSightSubscribeSaveSystem(
                saveSystemInstance,
                lineOfSightDataComponent,
                lineOfSightUI,
                viewPoint,
                landmark);
        }
        public void OnEnable()
        {
            input = new LandscapeInputActions.LineOfSightActions(new LandscapeInputActions());
            input.SetCallbacks(this);
            input.Enable();

            lineOfSightUI.OnEnable(this, viewPoint, landmark, analyzeViewPoint, analyzeLandmark, lineOfSightUIElement);
        }
        // クリック時の処理を判別するため
        public void SetMode(LineOfSightType mode)
        {
            if (currentMode != null)
            {
                currentMode.OnDisable();
            }
            if (mode == LineOfSightType.viewPoint)
            {
                currentMode = viewPoint;
            }
            else if (mode == LineOfSightType.landmark)
            {
                currentMode = landmark;
            }
            else if (mode == LineOfSightType.analyzeViewPoint)
            {
                currentMode = analyzeViewPoint;
            }
            else if (mode == LineOfSightType.analyzeLandmark)
            {
                currentMode = analyzeLandmark;
            }
            else if (mode == LineOfSightType.main)
            {
                currentMode = null;
            }
            if (currentMode != null)
            {
                currentMode.OnEnable(lineOfSightUIElement);
            }
        }
        public void OnSelect(InputAction.CallbackContext context)
        {
            if (context.performed && currentMode != null)
            {
                currentMode.OnSelect();
            }
        }
        public void Update(float deltaTime)
        {
            lineOfSightUI.Update();
        }
        public void OnDisable()
        {
            input.Disable();
        }
        public void Start()
        {
        }

        public void LateUpdate(float deltaTime)
        {
        }

    }
}
