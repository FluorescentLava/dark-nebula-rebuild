using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

// Unity samples sensors on its main thread. Only numerical calibration runs on a worker.
public static class LevelMotionCalibration
{
    public static bool GyroscopeReady { get; private set; }
    public static bool IsCalibrating { get; private set; }
    public static Vector3 Gravity
    {
        get
        {
            Vector3 gravity = GyroscopeReady ? Input.gyro.gravity : Input.acceleration;
            return gravity.sqrMagnitude > .01f ? gravity : Input.acceleration;
        }
    }

    static LevelMotionCalibrationRunner runner;
    static bool samplingComplete;

    public static void Begin(Action<Quaternion> apply)
    {
        Cancel();
        if (runner == null)
        {
            var owner = new GameObject("Background motion calibration");
            owner.hideFlags = HideFlags.HideInHierarchy;
            UnityEngine.Object.DontDestroyOnLoad(owner);
            runner = owner.AddComponent<LevelMotionCalibrationRunner>();
        }
        runner.StartCoroutine(Calibrate(apply));
    }

    // Loading never waits for sampling or worker completion. Stop collecting
    // when the scene is ready so gameplay movement cannot bias the neutral pose.
    public static void EndSampling() { samplingComplete = true; }

    public static void Cancel()
    {
        if (runner != null) runner.StopAllCoroutines();
        samplingComplete = true;
        IsCalibrating = false;
    }

    public static IEnumerator Calibrate(Action<Quaternion> apply)
    {
        samplingComplete = false;
        IsCalibrating = true;
        GyroscopeReady = false;
        try
        {
            if (!SystemInfo.supportsAccelerometer && !SystemInfo.supportsGyroscope) yield break;
            if (SystemInfo.supportsGyroscope)
            {
                Input.gyro.updateInterval = 1f / 60f;
                Input.gyro.enabled = true;
            }
            float start = Time.realtimeSinceStartup;
            // Allow Android's sensor service to start before collecting its first window.
            while (!samplingComplete && Time.realtimeSinceStartup - start < .2f) yield return null;
            Vector3 result = Vector3.zero;
            double bestVariance = double.MaxValue;
            do
            {
                var samples = new double[32 * 3];
                int count = 0;
                bool useGyro = SystemInfo.supportsGyroscope && Input.gyro.gravity.sqrMagnitude > .01f;
                while (count < 32)
                {
                    Vector3 sample = useGyro ? Input.gyro.gravity : Input.acceleration;
                    samples[count * 3] = sample.x;
                    samples[count * 3 + 1] = sample.y;
                    samples[count * 3 + 2] = sample.z;
                    count++;
                    if (samplingComplete) break;
                    yield return new WaitForSecondsRealtime(.02f);
                }
                if (count < 32) Array.Resize(ref samples, count * 3);
                Task<double[]> work = Task.Run(() => Analyze(samples));
                while (!work.IsCompleted) yield return null;
                if (work.IsFaulted || work.IsCanceled) yield break;
                double[] statistics = work.Result;
                if (statistics[3] < bestVariance)
                {
                    bestVariance = statistics[3];
                    result = new Vector3((float)statistics[0], (float)statistics[1], (float)statistics[2]);
                    GyroscopeReady = useGyro;
                }
                if (samplingComplete || bestVariance < .001) break;
            } while (Time.realtimeSinceStartup - start < 4f);
            // Bound calibration work even when the device keeps moving.
            // The lowest-variance window defines the user's neutral holding position.
            if (result.sqrMagnitude > .01f)
            {
                result.Normalize();
                Vector3 up = Mathf.Abs(Vector3.Dot(result, Vector3.up)) > .99f ? Vector3.forward : Vector3.up;
                apply(Quaternion.LookRotation(result, up));
            }
        }
        finally { IsCalibrating = false; }
    }

    public static double[] Analyze(double[] samples)
    {
        int count = samples.Length / 3;
        if (count == 0) throw new ArgumentException("Calibration requires samples.");
        double x = 0, y = 0, z = 0, variance = 0;
        for (int i = 0; i < count; i++) { x += samples[i * 3]; y += samples[i * 3 + 1]; z += samples[i * 3 + 2]; }
        x /= count; y /= count; z /= count;
        for (int i = 0; i < count; i++)
        {
            double dx = samples[i * 3] - x, dy = samples[i * 3 + 1] - y, dz = samples[i * 3 + 2] - z;
            variance += dx * dx + dy * dy + dz * dz;
        }
        return new[] { x, y, z, variance / count };
    }
}

// Its lifetime covers the asynchronous scene change and does not own a screen.
public sealed class LevelMotionCalibrationRunner : MonoBehaviour { }
