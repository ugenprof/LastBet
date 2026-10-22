// Фоновая музыка: одна зона играет по кругу, смена — плавным переходом (MUSIC_CROSSFADE_MS).
// Сцена, мелькнувшая на пару секунд, трек не меняет (MUSIC_SWITCH_DELAY_MS). Выключено/включено — запоминается.
// Треки — Kevin MacLeod (incompetech.com), CC BY 4.0: Resources/Music/CREDITS.txt.
using System.Collections;
using UnityEngine;

public class Music : MonoBehaviour
{
    const float VOLUME = 0.35f;            // MUSIC_VOLUME
    const float CROSSFADE = 4f;            // MUSIC_CROSSFADE_MS
    const float SWITCH_DELAY = 1.5f;       // MUSIC_SWITCH_DELAY_MS

    static Music inst;
    AudioSource a, b, click;
    string zone, target;
    int token;
    AudioClip clickClip;

    public static bool Muted { get; private set; }

    public static void Init()
    {
        if (inst != null) return;
        var go = new GameObject("Music");
        DontDestroyOnLoad(go);
        inst = go.AddComponent<Music>();
        inst.a = go.AddComponent<AudioSource>();
        inst.b = go.AddComponent<AudioSource>();
        inst.click = go.AddComponent<AudioSource>();
        foreach (var s in new[] { inst.a, inst.b })
        {
            s.loop = true;
            s.playOnAwake = false;
            s.volume = 0;
        }
        inst.clickClip = MakeClick();
        Muted = PlayerPrefs.GetInt("muted", 0) == 1;
    }

    // Зона сцены (null — не менять)
    public static void SetZone(string z)
    {
        if (inst == null || string.IsNullOrEmpty(z) || z == inst.target) return;
        inst.target = z;
        int t = ++inst.token;
        float delay = inst.zone != null ? SWITCH_DELAY : 0;
        inst.StartCoroutine(inst.SwitchLater(t, delay));
    }

    IEnumerator SwitchLater(int t, float delay)
    {
        if (delay > 0) yield return new WaitForSeconds(delay);
        if (t != token || target == zone) yield break;
        var track = Data.C.MUSIC_TRACKS.TryGetValue(target, out var tr) ? tr : null;
        var clip = Resources.Load<AudioClip>("Music/" + target);
        if (clip == null) yield break;
        zone = target;

        // Новая дорожка — на свободный источник, старая затихает
        var next = a.isPlaying && a.volume > 0 ? b : a;
        var old = next == a ? b : a;
        next.clip = clip;
        next.volume = 0;
        next.Play();
        float full = VOLUME * (track != null ? track.volume : 1f);
        float oldStart = old.volume, time = 0;
        while (time < CROSSFADE)
        {
            if (t != token) yield break;
            time += Time.deltaTime;
            float k = Mathf.Clamp01(time / CROSSFADE);
            next.volume = Muted ? 0 : full * k;
            old.volume = Muted ? 0 : oldStart * (1 - k);
            yield return null;
        }
        old.Stop();
        next.volume = Muted ? 0 : full;
    }

    public static void Toggle()
    {
        if (inst == null) return;
        Muted = !Muted;
        PlayerPrefs.SetInt("muted", Muted ? 1 : 0);
        var track = inst.zone != null && Data.C.MUSIC_TRACKS.TryGetValue(inst.zone, out var tr) ? tr : null;
        float full = VOLUME * (track != null ? track.volume : 1f);
        foreach (var s in new[] { inst.a, inst.b })
            if (s.isPlaying) s.volume = Muted ? 0 : (s.clip != null && s.clip.name == inst.zone ? full : 0);
    }

    // Тихий щелчок кнопки
    public static void Click()
    {
        if (inst == null || Muted) return;
        inst.click.PlayOneShot(inst.clickClip, 0.25f);
    }

    static AudioClip MakeClick()
    {
        int rate = 44100, n = rate / 25;
        var data = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / rate;
            data[i] = Mathf.Sin(2 * Mathf.PI * 1400 * t) * Mathf.Exp(-t * 90) * 0.6f;
        }
        var clip = AudioClip.Create("click", n, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
