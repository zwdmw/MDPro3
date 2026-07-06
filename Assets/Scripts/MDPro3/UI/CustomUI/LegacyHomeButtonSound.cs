using UnityEngine;
using UnityEngine.EventSystems;

namespace MDPro3.UI
{
    public class LegacyHomeButtonSound : MonoBehaviour,
        IPointerClickHandler,
        IPointerEnterHandler,
        ISelectHandler,
        ISubmitHandler
    {
        [SerializeField] private string SoundLabelClick;
        [SerializeField] private string SoundLabelPointerEnter;
        [SerializeField] private string SoundLabelSelectedGamePad;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
                AudioManager.PlaySE(SoundLabelClick);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            AudioManager.PlaySE(SoundLabelPointerEnter);
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (eventData is AxisEventData || UserInput.NextSelectionIsAxis)
                AudioManager.PlaySE(SoundLabelSelectedGamePad);
        }

        public void OnSubmit(BaseEventData eventData)
        {
            AudioManager.PlaySE(SoundLabelClick);
        }
    }
}
