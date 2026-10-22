// Фоновая отрисовка: большие картинки (фоны, персонажи) рисуются в отдельном потоке, чтобы смена сцены
// не подвисала. Главный поток только превращает готовые пиксели в текстуры (Pump — каждый кадр).
// Get возвращает текстуру, если она готова, иначе ставит её в очередь и возвращает null.
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public static class ArtQueue
{
    class Job
    {
        public string key;
        public int w, h;
        public Func<Color32[]> work;
        public Color32[] result;
    }

    static readonly object sync = new object();
    static readonly LinkedList<Job> pending = new LinkedList<Job>();
    static readonly Queue<Job> done = new Queue<Job>();
    static readonly HashSet<string> queued = new HashSet<string>();
    static readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
    static readonly AutoResetEvent signal = new AutoResetEvent(false);
    static Thread worker;

    public static Texture2D Get(string key, int w, int h, Func<Color32[]> work, bool urgent = false)
    {
        if (textures.TryGetValue(key, out var t) && t != null) return t;
        lock (sync)
        {
            if (!queued.Contains(key))
            {
                queued.Add(key);
                var job = new Job { key = key, w = w, h = h, work = work };
                // Нужное прямо сейчас — вперёд очереди, заготовки — в конец
                if (urgent) pending.AddFirst(job);
                else pending.AddLast(job);
                signal.Set();
            }
        }
        EnsureWorker();
        return null;
    }

    // Нарисовать сразу (главный поток) — когда ждать нельзя
    public static Texture2D GetNow(string key, int w, int h, Func<Color32[]> work)
    {
        if (textures.TryGetValue(key, out var t) && t != null) return t;
        t = Painter.MakeTexture(w, h, work());
        textures[key] = t;
        return t;
    }

    public static bool Ready(string key) => textures.ContainsKey(key);

    // Главный поток: готовые пиксели → текстуры (не больше max за кадр)
    public static void Pump(int max = 2)
    {
        for (int i = 0; i < max; i++)
        {
            Job job;
            lock (sync)
            {
                if (done.Count == 0) return;
                job = done.Dequeue();
            }
            if (job.result != null && !textures.ContainsKey(job.key))
                textures[job.key] = Painter.MakeTexture(job.w, job.h, job.result);
        }
    }

    static void EnsureWorker()
    {
        if (worker != null && worker.IsAlive) return;
        worker = new Thread(Loop) { IsBackground = true, Name = "ArtQueue", Priority = System.Threading.ThreadPriority.BelowNormal };
        worker.Start();
    }

    static void Loop()
    {
        while (true)
        {
            Job job = null;
            lock (sync)
            {
                if (pending.Count > 0)
                {
                    job = pending.First.Value;
                    pending.RemoveFirst();
                }
            }
            if (job == null)
            {
                signal.WaitOne(500);
                continue;
            }
            try
            {
                job.result = job.work();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            lock (sync) done.Enqueue(job);
        }
    }
}
