// Таймеры вместо setTimeout: выполнить позже, повторять, плавно менять значение.
// Game.Render отменяет все таймеры прошлой сцены (CancelAll); долгие эффекты интерфейса — через Ui-версии.
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Run : MonoBehaviour
{
    static Run inst;
    static readonly List<Coroutine> scene = new List<Coroutine>();

    static Run I
    {
        get
        {
            if (inst == null)
            {
                var go = new GameObject("Run");
                DontDestroyOnLoad(go);
                inst = go.AddComponent<Run>();
            }
            return inst;
        }
    }

    // Таймеры сцены: отменяются при переходе в другую сцену
    public static void After(float seconds, Action action) => scene.Add(I.StartCoroutine(AfterCo(seconds, action)));

    // Повторять, пока tick возвращает true
    public static void Every(float seconds, Func<bool> tick) => scene.Add(I.StartCoroutine(EveryCo(seconds, tick)));

    // k от 0 до 1 за duration секунд
    public static void Tween(float duration, Action<float> step) => scene.Add(I.StartCoroutine(TweenCo(duration, step)));

    // Эффекты интерфейса, которые переживают смену сцены (затухание вспышки, музыка)
    public static void UiAfter(float seconds, Action action) => I.StartCoroutine(AfterCo(seconds, action));
    public static void UiTween(float duration, Action<float> step) => I.StartCoroutine(TweenCo(duration, step));

    public static void CancelAll()
    {
        foreach (var c in scene)
            if (c != null && inst != null) inst.StopCoroutine(c);
        scene.Clear();
    }

    static IEnumerator AfterCo(float seconds, Action action)
    {
        yield return new WaitForSeconds(seconds);
        Safe(action);
    }

    static IEnumerator EveryCo(float seconds, Func<bool> tick)
    {
        while (true)
        {
            yield return new WaitForSeconds(seconds);
            bool more = false;
            try { more = tick(); }
            catch (Exception e) { Debug.LogException(e); }
            if (!more) yield break;
        }
    }

    static IEnumerator TweenCo(float duration, Action<float> step)
    {
        float t = 0;
        while (t < duration)
        {
            Safe(() => step(t / duration));
            yield return null;
            t += Time.deltaTime;
        }
        Safe(() => step(1f));
    }

    static void Safe(Action a)
    {
        try { a(); }
        catch (Exception e) { Debug.LogException(e); }
    }
}
