using TMPro;
using UnityEngine;

// The data types Auga's API signatures use (Auga/API.Common.cs in the Auga repo). Redirected to Auga.dll
// along with Auga.API, so the field names and types must match Auga's exactly; see AugaAPI.cs.
// Only Auga assigns these fields.
#pragma warning disable CS0649
namespace Auga
{
    internal enum RequirementWireState
    {
        Absent,
        Have,
        DontHave
    }

    internal class PlayerPanelTabData
    {
        public int Index;
        public TMP_Text TabTitle;
        public GameObject TabButtonGO;
        public GameObject ContentGO;
    }

    internal class WorkbenchTabData
    {
        public int Index;
        public TMP_Text TabTitle;
        public GameObject TabButtonGO;
        public GameObject RequirementsPanelGO;
        public GameObject ItemInfoGO;
    }
}
