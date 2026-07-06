using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace MDPro3.UI
{
    public class SidePanel : MonoBehaviour
    {
        [Header("Side Panel")]
        public RectTransform window;
        public CanvasGroup shadow;
        [HideInInspector]public bool showing;
        protected float width = 400f;

        protected SidePanel lastPanel;
        protected float transitionTime = 0.4f;
        protected float shadowAlpha = 0.75f;

        protected virtual void Awake()
        {
            if (window == null)
                window = GetComponent<RectTransform>();
            if (window == null && transform.childCount > 0)
                window = transform.GetChild(0).GetComponent<RectTransform>();
            if (window != null)
                width = window.rect.width;

            if (shadow != null)
            {
                shadow.alpha = 0f;
                shadow.blocksRaycasts = false;
                if (shadow.TryGetComponent<Button>(out var button))
                    button.onClick.AddListener(() => Hide());
            }
            Hide(true);

            if (TryGetComponent<CanvasGroup>(out var canvasGroup))
                canvasGroup.alpha = 1f;
        }

        protected virtual bool NeedResponse()
        {
            return Program.instance.ui_.currentSidePanel == this;
        }

        protected virtual void Update()
        {
            if (!showing) return;
            if (!NeedResponse()) return;

            if(UserInput.WasCancelPressed || UserInput.MouseRightDown)
                Hide();
        }

        public virtual void Show(bool takeOverInput = true)
        {
            showing = true;

            if (Program.instance != null && Program.instance.room != null && !Program.instance.room.showing)
                AudioManager.PlaySE("SE_MENU_SLIDE_01");
            gameObject.SetActive(true);
            if (window != null)
            {
                window.DOKill();
                window.DOAnchorPosX(0f, transitionTime)/*.SetEase(Ease.Linear)*/;
            }
            if (shadow != null)
            {
                shadow.DOKill();
                shadow.DOFade(shadowAlpha, transitionTime);
                shadow.blocksRaycasts = true;
            }

            if (takeOverInput && Program.instance != null)
            {
                lastPanel = Program.instance.ui_?.currentSidePanel;
                if (Program.instance.ui_ != null)
                    Program.instance.ui_.currentSidePanel = this;
            }
        }

        public virtual void Hide(bool instant = false, bool callLast = false)
        {
            showing = false;

            if (Program.instance != null && Program.instance.room != null && !Program.instance.room.showing)
                AudioManager.PlaySE("SE_MENU_SLIDE_02");
            if (window == null)
            {
                gameObject.SetActive(false);
                return;
            }
            window.DOKill();
            window.DOAnchorPosX(width + SafeAreaAdapter.GetSafeAreaRightOffset(), instant ? 0f : transitionTime)
                //.SetEase(Ease.Linear)
                .OnComplete(() =>
            {
                if (Program.instance?.ui_ != null && Program.instance.ui_.currentSidePanel == this)
                {
                    Program.instance.ui_.currentSidePanel = null;
                    if(lastPanel != null && callLast)
                        lastPanel.Show();
                }
                gameObject.SetActive(false);
            });
            if (shadow != null)
            {
                shadow.DOKill();
                shadow.blocksRaycasts = false;
                shadow.DOFade(0f, instant ? 0f : transitionTime);
            }
        }
    }
}
