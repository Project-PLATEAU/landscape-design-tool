using PlateauToolkit.Sandbox.Runtime.PlateauSandboxBuildings.Runtime;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace Landscape2.Runtime
{
    public interface IArrangementBuildingParameter<T> where T : class
    {
        public void SetParameter(T parameters);
    }

    public interface IArrangementBuildingParameterUI
    {
        public void SetTarget(PlateauSandboxBuilding building);
        public void Show(bool isShow);
    }
}