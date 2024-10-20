using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Image))]
public class MyJoystick : MonoBehaviour
{
    public class Boundary
    {
        public Vector2 min = Vector2.zero;
        public Vector2 max = Vector2.zero;
    }

    static private MyJoystick[] joysticks;
    static private bool enumeratedJoysticks = false;
    static private float tapTimeDelta = 0.3f;

    public bool touchPad;
    public Rect touchZone;
    public Vector2 deadZone = Vector2.zero;
    public bool normalize = false;
    public Vector2 position;
    public int tapCount;

    private int lastFingerId = -1;
    private float tapTimeWindow;
    private Vector2 fingerDownPos;

    private Image gui;
    private Rect defaultRect;
    public Boundary guiBoundary = new Boundary();
    private Vector2 guiTouchOffset;
    private Vector2 guiCenter;

    private RectTransform rectTransform;

    public Rect getBoundary()
    {
        return new Rect(guiBoundary.min.x, guiBoundary.min.y, (guiBoundary.max.x - guiBoundary.min.x), (guiBoundary.max.y - guiBoundary.min.y));
    }

    void Start()
    {
        gui = transform.GetComponent<Image>();
        rectTransform = gui.GetComponent<RectTransform>();

        defaultRect = new Rect(0, 0, rectTransform.sizeDelta.x, rectTransform.sizeDelta.y);

        defaultRect.x += transform.position.x * Screen.width;
        defaultRect.y += transform.position.y * Screen.height;

        Vector3 tempPos = new Vector3(0, 0, transform.position.z);
        transform.position = tempPos;

        if (touchPad)
        {
            if (gui.sprite)
                touchZone = defaultRect;
        }
        else
        {
            guiTouchOffset.x = defaultRect.width * 0.25f;
            guiTouchOffset.y = defaultRect.height * 0.25f;

            guiCenter.x = defaultRect.x + guiTouchOffset.x;
            guiCenter.y = defaultRect.y + guiTouchOffset.y;

            guiBoundary.min.x = defaultRect.x - guiTouchOffset.x;
            guiBoundary.max.x = defaultRect.x + guiTouchOffset.x;
            guiBoundary.min.y = defaultRect.y - guiTouchOffset.y;
            guiBoundary.max.y = defaultRect.y + guiTouchOffset.y;
        }
    }

    void Disable()
    {
        gameObject.SetActive(false);
        enumeratedJoysticks = false;
    }

    void ResetJoystick()
    {
        rectTransform.anchoredPosition = new Vector2(defaultRect.x, defaultRect.y);
        lastFingerId = -1;
        position = Vector2.zero;
        fingerDownPos = Vector2.zero;

        if (touchPad)
        {
            Color tempColor = gui.color;
            tempColor.a = 0.15f;
            gui.color = tempColor;
        }
    }

    public bool IsFingerDown()
    {
        return (lastFingerId != -1);
    }

    void LatchedFinger(int fingerId)
    {
        if (lastFingerId == fingerId)
            ResetJoystick();
    }

    void Update()
    {
        if (!enumeratedJoysticks)
        {
            joysticks = FindObjectsOfType(typeof(MyJoystick)) as MyJoystick[];
            enumeratedJoysticks = true;
        }

        var count = Input.touchCount;

        if (tapTimeWindow > 0)
            tapTimeWindow -= Time.deltaTime;
        else
            tapCount = 0;

        if (count == 0)
            ResetJoystick();
        else
        {
            for (int i = 0; i < count; i++)
            {
                Touch touch = Input.GetTouch(i);
                Vector2 guiTouchPos = touch.position - guiTouchOffset;

                bool shouldLatchFinger = false;
                if (touchPad)
                {
                    if (touchZone.Contains(touch.position))
                        shouldLatchFinger = true;
                }
                else
                {
                    // Заменяем HitTest на проверку RectTransform
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, touch.position, null, out Vector2 localPoint);
                    if (rectTransform.rect.Contains(localPoint))
                        shouldLatchFinger = true;
                }

                if (shouldLatchFinger && (lastFingerId == -1 || lastFingerId != touch.fingerId))
                {
                    if (touchPad)
                    {
                        Color tempColor = gui.color;
                        tempColor.a = 0.30f;
                        gui.color = tempColor;

                        lastFingerId = touch.fingerId;
                        fingerDownPos = touch.position;
                    }

                    lastFingerId = touch.fingerId;

                    if (tapTimeWindow > 0)
                        tapCount++;
                    else
                    {
                        tapCount = 1;
                        tapTimeWindow = tapTimeDelta;
                    }

                    foreach (MyJoystick j in joysticks)
                    {
                        if (j != this)
                            j.LatchedFinger(touch.fingerId);
                    }
                }

                if (lastFingerId == touch.fingerId)
                {
                    if (touch.tapCount > tapCount)
                        tapCount = touch.tapCount;

                    if (touchPad)
                    {
                        position.x = Mathf.Clamp((touch.position.x - fingerDownPos.x) / (touchZone.width / 2), -1, 1);
                        position.y = Mathf.Clamp((touch.position.y - fingerDownPos.y) / (touchZone.height / 2), -1, 1);
                    }
                    else
                    {
                        Vector2 clampedPos = new Vector2(
                            Mathf.Clamp(guiTouchPos.x, guiBoundary.min.x, guiBoundary.max.x),
                            Mathf.Clamp(guiTouchPos.y, guiBoundary.min.y, guiBoundary.max.y)
                        );
                        rectTransform.anchoredPosition = clampedPos;
                    }

                    if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                        ResetJoystick();
                }
            }
        }

        if (!touchPad)
        {
            position.x = (rectTransform.anchoredPosition.x + guiTouchOffset.x - guiCenter.x) / guiTouchOffset.x;
            position.y = (rectTransform.anchoredPosition.y + guiTouchOffset.y - guiCenter.y) / guiTouchOffset.y;
        }

        var absoluteX = Mathf.Abs(position.x);
        var absoluteY = Mathf.Abs(position.y);

        if (absoluteX < deadZone.x)
        {
            position.x = 0;
        }
        else if (normalize)
        {
            position.x = Mathf.Sign(position.x) * (absoluteX - deadZone.x) / (1 - deadZone.x);
        }

        if (absoluteY < deadZone.y)
        {
            position.y = 0;
        }
        else if (normalize)
        {
            position.y = Mathf.Sign(position.y) * (absoluteY - deadZone.y) / (1 - deadZone.y);
        }
    }
}
