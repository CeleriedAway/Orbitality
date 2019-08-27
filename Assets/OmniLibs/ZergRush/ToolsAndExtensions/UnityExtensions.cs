﻿#if UNITY_5_3_OR_NEWER

using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using TMPro;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;
using ZergRush;
using ZergRush.ReactiveCore;
using static UnityEngine.Mathf;

public class ConnectableObject : MonoBehaviour, IConnectionSink
{
    public Connections connections = new Connections();

    public IDisposable addConnection
    {
        set { connections.Add(value); }
    }

    Action<IDisposable> _connectionSink;
    public Action<IDisposable> connectionSink => _connectionSink ?? (_connectionSink = disp => connections.Add(disp));

    public void DisconnectAll()
    {
        connections.DisconnectAll();
    }

    protected virtual void OnDestroy()
    {
        DisconnectAll();
    }

    public void AddConnection(IDisposable connection)
    {
        connections.addConnection = connection;
    }
}

public static class UnityExtensions
{
    public static void AddConnection(this IConnectionSink sink, IConnectionSink dublicater, IDisposable connection)
    {
        sink.AddConnection(connection);
        dublicater.AddConnection(connection);
    }
    
    class UnityActionDisposable : IDisposable
    {
        public UnityEvent e;
        public UnityAction action;

        public void Dispose()
        {
            if (e != null)
            {
                e.RemoveListener(action);
                e = null;
                action = null;
            }
        }
    }

    class UnityActionDisposable<T> : IDisposable
    {
        public UnityEvent<T> e;
        public UnityAction<T> action;

        public void Dispose()
        {
            if (e != null)
            {
                e.RemoveListener(action);
                e = null;
                action = null;
            }
        }
    }

    public static IEventReader ClickStream(this Button button)
    {
        if (button == null)
        {
            Debug.LogError("button is null!!!");
            return new AbandonedReader();
        }

        return new AnonymousEventReader((Action reaction) =>
        {
            var ua = new UnityAction(reaction);
            button.onClick.AddListener(ua);
            return new UnityActionDisposable {action = ua, e = button.onClick};
        });
    }

    public static IDisposable Subscribe(this Button button, Action reaction)
    {
        if (button == null)
            return new EmptyDisposable();

        return button.ClickStream().Subscribe(reaction);
    }

    public static IEventReader PressedStream(this Button button)
    {
        if (button == null)
        {
            Debug.LogError("button is null!!!");
            return new AbandonedReader();
        }

        return new AnonymousEventReader((Action reaction) =>
        {
            var ua = new UnityAction(reaction);
            button.onClick.AddListener(ua);
            return new UnityActionDisposable {action = ua, e = button.onClick};
        });
    }

    public static void SetPointerEventListener(this EventTrigger trigger, EventTriggerType eventType, Action action)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = eventType;
        entry.callback.AddListener(eventData => action());
        trigger.triggers.Add(entry);
    }

    public static IDisposable OnPointerDown(this EventTrigger self, Action act)
    {
        var entry = new EventTrigger.Entry {eventID = EventTriggerType.PointerDown};

        entry.callback.AddListener(data => { act(); });

        self.triggers.Add(entry);
        return new AnonymousDisposable(() => self.triggers.Remove(entry));
    }
    public static IDisposable OnPointerUp(this EventTrigger self, Action act)
    {
        var entry = new EventTrigger.Entry {eventID = EventTriggerType.PointerUp};

        entry.callback.AddListener(data => { act(); });

        self.triggers.Add(entry);
        return new AnonymousDisposable(() => self.triggers.Remove(entry));
    }


    public static void SetLayerRecursively(this GameObject go, int layerId)
    {
        go.layer = layerId;
        foreach (Transform c in go.transform)
        {
            SetLayerRecursively(c.gameObject, layerId);
        }
    }

    public static void StopAllCoroutinesRecursively(this GameObject obj)
    {
        obj.GetComponentsInChildren<MonoBehaviour>().ForEach(monoBeh => monoBeh.StopAllCoroutines());
    }

    static public T GetOrAddComponent<T>(this GameObject obj) where T : Component
    {
        var component = obj.GetComponent<T>();
        if (component == null)
        {
            component = obj.AddComponent<T>();
        }

        return component;
    }

    static public void RemoveComponent<T>(this GameObject obj) where T : Component
    {
        var component = obj.GetComponent<T>();
        if (component != null)
        {
            GameObject.Destroy(component);
        }
    }

    static public void RemoveComponentImmediate<T>(this GameObject obj) where T : Component
    {
        var component = obj.GetComponent<T>();
        if (component != null)
        {
            GameObject.DestroyImmediate(component);
        }
    }


    public static ICell<bool> IsPressed(this GameObject go)
    {
        var cell = new Cell<bool>();
        var trigger = go.GetOrAddComponent<EventTrigger>();
        trigger.SetPointerEventListener(EventTriggerType.PointerDown, () => cell.value = true);
        trigger.SetPointerEventListener(EventTriggerType.PointerUp, () => cell.value = false);
        trigger.SetPointerEventListener(EventTriggerType.Cancel, () => cell.value = false);
        return cell;
    }

    public static IDisposable RunCoroutineWhile(this MonoBehaviour self, Func<IEnumerator> coro, ICell<bool> condition)
    {
        Coroutine currentCoro = null;
        return condition.Bind(val =>
        {
            if (val) currentCoro = self.StartCoroutine(coro());
            else if (currentCoro != null)
            {
                self.StopCoroutine(currentCoro);
                currentCoro = null;
            }
        });
    }

    public static Color WithAlpha(this Color c, float alpha)
    {
        c.a = alpha;
        return c;
    }

    public static void MakeUniqueMaterial(this Image img)
    {
        img.material = UnityEngine.Object.Instantiate(img.material) as Material;
    }

    public static T MakeInstance<T>(this GameObject obj)
        where T : Component
    {
        return (UnityEngine.Object.Instantiate(obj) as GameObject).GetComponent<T>();
    }

    public static GameObject MakeInstance(this GameObject obj)
    {
        return (UnityEngine.Object.Instantiate(obj) as GameObject);
    }

    public static void ResetTransform(this GameObject obj)
    {
        ResetTransform(obj.transform);
    }

    public static void ResetTransform(this Component c)
    {
        c.transform.localPosition = Vector3.zero;
        c.transform.localRotation = Quaternion.identity;
        c.transform.localScale = Vector3.one;
    }

    public static void SetPositionX(this Transform t, float x)
    {
        var position = t.position;
        position.x = x;
        t.position = position;
    }

    public static void SetLocalPositionX(this Transform t, float x)
    {
        var position = t.localPosition;
        position.x = x;
        t.localPosition = position;
    }

    public static void SetAnchoredPositionX(this RectTransform t, float x)
    {
        var position = t.anchoredPosition;
        position.x = x;
        t.anchoredPosition = position;
    }

    public static void SetAnchoredPositionY(this RectTransform t, float y)
    {
        var position = t.anchoredPosition;
        position.y = y;
        t.anchoredPosition = position;
    }

    public static void SetPositionY(this Transform t, float y)
    {
        var position = t.position;
        position.y = y;
        t.position = position;
    }

    public static void SetLocalPositionY(this Transform t, float y)
    {
        var position = t.localPosition;
        position.y = y;
        t.localPosition = position;
    }


    public static void SetPositionZ(this Transform t, float z)
    {
        var position = t.position;
        position.z = z;
        t.position = position;
    }

    public static void SetLocalPositionZ(this Transform t, float z)
    {
        var position = t.localPosition;
        position.z = z;
        t.localPosition = position;
    }

    public static void SetActiveSafe(this GameObject obj, bool active)
    {
        if (!obj) return;
        if (obj.activeSelf == active) return;
        obj.SetActive(active);
    }

    public static void SetActiveSafe(this Component obj, bool active)
    {
        if (!obj) return;
        if (obj.gameObject.activeSelf == active) return;
        obj.gameObject.SetActive(active);
    }

    public static Sprite LoadSprite(string name)
    {
        Sprite result = null;
        WWW www = new WWW(WWW.EscapeURL("file://" + Application.dataPath + "/" + name));
        while (!www.isDone)
        {
        }

        if (www.texture != null)
            result = Sprite.Create(www.texture, new Rect(0, 0, www.texture.width, www.texture.height),
                new Vector2(0.5f, 0.5f));
        else
            Debug.Log("#Failed loading sprite " + name);

        return result;
    }

    public static void SetTransparency(this Graphic img, float transparency)
    {
        var color = img.color;
        color.a = transparency;
        img.color = color;
    }

    public static void RestartCurrentStateWIthRandomSpeedAndShift(this Animator animator)
    {
        animator.speed = UnityEngine.Random.Range(0.8f, 1.2f);
        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        animator.Play(state.fullPathHash, -1, UnityEngine.Random.Range(0f, 2f));
    }

    public static Coroutine ExecuteWithDelay(this MonoBehaviour self, float delay, Action action)
    {
        if (delay <= 0)
        {
            action();
            return null;
        }

        return self.StartCoroutine(DelayCoro(action, delay));
    }

    public static IEnumerable<T> FlagsToList<T>(int mask)
    {
        if (typeof(T).IsSubclassOf(typeof(Enum)) == false)
            throw new ArgumentException();

        return Enum.GetValues(typeof(T))
            .Cast<int>()
            .Where(Mathf.IsPowerOfTwo)
            .Where(m => ((int) mask & (int) m) != 0)
            .Cast<T>();
    }

    static IEnumerator DelayCoro(Action action, float delay)
    {
        yield return new WaitForSeconds(delay);
        action();
    }

    public static float EaseOut(float t)
    {
        return -t * (t - 2);
    }

    static public float EaseIn(float t)
    {
        return t * t * t;
    }

    static public float EaseElasticOut(float t)
    {
        var p = 0.3F;
        return Mathf.Pow(2, -10 * t) * Mathf.Sin((t - p / 4) * (2 * Mathf.PI) / p) + 1;
    }

    static public float EaseOut(float from, float to, float t)
    {
        return Mathf.Lerp(from, to, -t * (t - 2));
    }

    static public float EaseIn(float from, float to, float t)
    {
        return Mathf.Lerp(from, to, t * t * t);
    }

    public static float TenPower(int power)
    {
        return Mathf.Pow(10, power);
    }

    public static IDisposable SetTextContent<T>(this Text text, ICell<T> val)
    {
        return val.Bind(v => text.text = v.ToString());
    }
    
    public static IDisposable SetTextContent<T>(this TextMeshProUGUI text, ICell<T> val)
    {
        return val.Bind(v => text.text = v.ToString());
    }

    public static IDisposable SetFill(this Image image, ICell<float> val)
    {
        return val.Bind(v => image.fillAmount = v);
    }

    public static IDisposable SetActive(this MonoBehaviour go, ICell<bool> val)
    {
        return val.Bind(go.SetActiveSafe);
    }

    public static void SetVisible(this Image image, bool val)
    {
        image.SetActiveSafe(val);
    }
    
    public static void SetVisibility(this CanvasGroup @group, bool visible, float fadeDuration = 0, float delay = 0, float baseAlfa = 1)
    {
        @group.DOKill();
        if (visible)
        {
            @group.SetActiveSafe(true);
            @group.alpha = 0;
            @group.DOFade(baseAlfa, fadeDuration).SetDelay(delay);
        }
        else
        {
            @group.DOFade(0, fadeDuration).SetDelay(delay).onComplete += () => @group.SetActiveSafe(false);
        }
    }
    
    public static void SetVisibility(this Image image, bool visible, float fadeDuration = 0, float baseAlfa = 1)
    {
        image.DOKill();
        if (visible)
        {
            image.DOKill(false);
            image.SetActiveSafe(true);
            image.SetTransparent(true);
            image.DOFade(baseAlfa, fadeDuration);
        }
        else
        {
            image.DOKill(false);
            image.DOFade(0, fadeDuration).onComplete += () => image.SetVisible(false);
        }
    }

    public static IDisposable SetVisible(this Image image, ICell<bool> val, float fadeDuration = 0)
    {
        
        if (fadeDuration != 0)
        {
            image.SetVisible(val.value);
            return val.ListenUpdates(v => { image.SetVisibility(v, fadeDuration, image.color.a); });
        }
        else
        {
            return val.Bind(image.SetVisible);
        }
    }
    
    public static IDisposable SetVisible(this CanvasGroup image, ICell<bool> val, float fadeDuration = 0, float targetOpacity = 1.0f)
    {
        if (fadeDuration != 0)
        {
            image.SetActiveSafe(val.value);
            return val.ListenUpdates(v => { image.SetVisibility(v, fadeDuration, targetOpacity); });
        }
        else
        {
            return val.Bind(image.SetActiveSafe);
        }
    }

    public static IDisposable SetActive(this Transform go, ICell<bool> val)
    {
        return val.Bind(go.SetActiveSafe);
    }

    public static IDisposable SetActive(this GameObject go, ICell<bool> val)
    {
        return val.Bind(go.SetActiveSafe);
    }

    public static IDisposable SetOpacity(this Image image, ICell<float> val)
    {
        return val.Bind(v =>
        {
            var c = image.color;
            c.a = v;
            image.color = c;
        });
    }

    public static IDisposable SetVisibility(this MonoBehaviour obj, ICell<bool> val)
    {
        return val.Bind(obj.SetActiveSafe);
    }

    // Return true if object is stopped
    public static bool StopObject(this Rigidbody rb, float breakForcePower, float velocityLimit)
    {
        if (rb.velocity.magnitude < velocityLimit) return true;
        rb.AddForce(-rb.velocity.normalized * breakForcePower);
        return false;
    }

    public static Vector3 ProjectVectorOnPlane(this Vector3 planeNormal, Vector3 vec)
    {
        return vec - planeNormal.normalized * (Vector3.Dot(vec, planeNormal) / planeNormal.magnitude);
    }

    public static Vector3 HeightVec(this Vector3 planeNormal, Vector3 vec)
    {
        return planeNormal.normalized * (Vector3.Dot(vec, planeNormal) / planeNormal.magnitude);
    }

    public static Vector2 BottomCenter(this RectTransform rt)
    {
        return rt.anchoredPosition - rt.rect.size.WithX(0) / 2;
    }

    public static Vector2 TopCenter(this RectTransform rt)
    {
        return rt.anchoredPosition + rt.rect.size.WithX(0) / 2;
    }

    public static Vector2 LeftCenter(this RectTransform rt)
    {
        return rt.anchoredPosition - rt.rect.size.WithY(0) / 2;
    }

    public static Vector2 RightCenter(this RectTransform rt)
    {
        return rt.anchoredPosition + rt.rect.size.WithY(0) / 2;
    }

    public static float HorizontalFovFromVerticalFov(float verticalFovAngleDegree, float aspect)
    {
        return 2 * Mathf.Atan(aspect * Mathf.Tan(verticalFovAngleDegree / 2 * Mathf.Deg2Rad)) * Mathf.Rad2Deg;
    }

    public static float ZoomFov(float fovAngle, float zoomValue)
    {
        return Atan(Tan(fovAngle * Deg2Rad) * zoomValue) * Rad2Deg;
    }

    public static float VerticalFovFromHorizontalFov(float horizantalFovAngleInDegrees, float aspect)
    {
        return 2 * Mathf.Atan(1 / aspect * Mathf.Tan(horizantalFovAngleInDegrees / 2 * Mathf.Deg2Rad)) * Mathf.Rad2Deg;
    }

    public static Vector2 DivideComponentVise(this Vector2 item, Vector2 otherItem)
    {
        return new Vector2(item.x / otherItem.x, item.y / otherItem.y);
    }

    public static Vector3 DivideComponentVise(this Vector3 item, Vector3 otherItem)
    {
        return new Vector3(item.x / otherItem.x, item.y / otherItem.y, item.z / otherItem.z);
    }

    public static Vector2 MultiplyComponentVise(this Vector2 item, Vector2 otherItem)
    {
        return new Vector2(item.x * otherItem.x, item.y * otherItem.y);
    }

    public static Vector3 MultiplyComponentVise(this Vector3 item, Vector3 otherItem)
    {
        return new Vector3(item.x * otherItem.x, item.y * otherItem.y, item.z * otherItem.z);
    }

    public static void SetVelocityMagnitude(this Rigidbody rb, float mg)
    {
        var velocity = rb.velocity;
        if (velocity == Vector3.zero) velocity = rb.transform.forward * 0.0001f;
        rb.AddForce(velocity.normalized * (mg - velocity.magnitude), ForceMode.VelocityChange);
    }

    public static Vector3 YToZero(this Vector3 vec)
    {
        vec.y = 0;
        return vec;
    }
    public static Vector3 SwapYZ(this Vector3 vec)
    {
        return new Vector3(vec.x, vec.z, vec.y);
    }


    public static Vector3 YToValue(this Vector3 vec, float y)
    {
        vec.y = y;
        return vec;
    }

    public static Vector3 WithX(this Vector3 vec, float x)
    {
        vec.x = x;
        return vec;
    }

    public static Vector3 WithY(this Vector3 vec, float y)
    {
        vec.y = y;
        return vec;
    }

    public static Vector2 WithY(this Vector2 vec, float y)
    {
        vec.y = y;
        return vec;
    }

    public static Vector2 WithX(this Vector2 vec, float x)
    {
        vec.x = x;
        return vec;
    }

    public static Vector3 WithZ(this Vector3 vec, float z)
    {
        vec.z = z;
        return vec;
    }

    public static Vector3 ToVolume(this Vector2 vec)
    {
        return new Vector3(vec.x, 0, vec.y);
    }

    public static Vector3 PingPong(this Vector3 point, Vector3 size)
    {
        point.x = Mathf.PingPong(point.x, size.x);
        point.y = Mathf.PingPong(point.y, size.y);
        point.z = Mathf.PingPong(point.z, size.z);
        return point;
    }

    public static Vector3 RestrictInBox(Vector3 point, Bounds bounds)
    {
        var min = bounds.min;
        var max = bounds.max;
        return PingPong(point - min, max - min) + min;
    }

    public static Vector3 RandomPosInRange(Vector3 initial, float range)
    {
        return (initial + UnityEngine.Random.onUnitSphere * range);
    }

    public static void PlaySimpleAnimation(this GameObject obj, AnimationClip clip)
    {
        var anim = obj.GetOrAddComponent<Animation>();
        anim.AddClip(clip, "default");
        anim.Play("default");
    }
    
    public static void PlaySimpleAnimation(this MonoBehaviour obj, AnimationClip clip)
    {
        PlaySimpleAnimation(obj.gameObject, clip);
    }
    
    public static void PlaySimpleAnimation(this Component obj, AnimationClip clip)
    {
        PlaySimpleAnimation(obj.gameObject, clip);
    }
}

#endif