using System.Collections;
using UnityEngine;

public class RouletteSpinner : MonoBehaviour
{
    [Header("References")]
    public Transform wheel;      // The roulette wheel mesh (rotates)
    public Transform ball;       // The small ball (moves around the wheel)

    [Header("Ball Path Settings")]
    public float ballOuterRadius; // Where the ball starts
    public float ballInnerRadius; // Where it lands
    public float ballHeight;      // Height offset above the wheel

    [Header("Timing Settings")]
    public float spinDuration;     // Total time for a spin
    public int wheelExtraSpins;       // Extra full 360° turns for the wheel
    public int ballExtraSpins;       // How many laps the ball makes before landing

    [Header("Easing Curves")]
    public AnimationCurve wheelEase = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public AnimationCurve ballEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Debug / Test")]
    public bool autoTestOnPlay;
    public int previewTarget;

    // European roulette number order (clockwise, top view)
    private readonly int[] orderEU = new int[]
    {
        0, 32, 15, 19, 4, 21, 2, 25, 17, 34, 6, 27, 13, 36, 11, 30, 8, 23,
        10, 5, 24, 16, 33, 1, 20, 14, 31, 9, 22, 18, 29, 7, 28, 12, 35, 3, 26
    };

    private bool spinning = false;

    public bool IsSpinning => spinning;

    void Start()
    {
        if (autoTestOnPlay)
            SpinTo(previewTarget);
    }

    /// <summary>
    /// Spin the wheel and make the ball land on the selected number.
    /// </summary>
    public void SpinTo(int targetNumber)
    {
        if (!spinning)
            StartCoroutine(SpinToRoutine(targetNumber));
    }

    /// <summary>
    /// Coroutine so GameManager can "yield return" it.
    /// </summary>
    public IEnumerator SpinToRoutine(int targetNumber)
    {
        if (spinning) yield break;
        int index = IndexOfNumber(targetNumber);
        if (index < 0)
        {
            Debug.LogWarning("Invalid roulette number: " + targetNumber);
            yield break;
        }
        yield return StartCoroutine(SpinRoutine(index));
    }

    private int IndexOfNumber(int number)
    {
        for (int i = 0; i < orderEU.Length; i++)
        {
            if (orderEU[i] == number)
                return i;
        }
        return -1;
    }

    private IEnumerator SpinRoutine(int targetIndex)
    {
        if (wheel == null || ball == null)
        {
            Debug.LogWarning("Assign wheel and ball references!");
            yield break;
        }

        spinning = true;

        // Each pocket's angle size
        float perPocketDeg = 360f / orderEU.Length;

        // Ball always lands around world angle 0 (Z+ direction)
        float dropAngleWorldDeg = 0f;

        // Start at current wheel rotation
        float currentWheelY = wheel.eulerAngles.y;
        float targetPocketOffset = targetIndex * perPocketDeg;

        // Final rotation for the wheel (extra spins + align target pocket)
        float wheelFinalY = currentWheelY + (wheelExtraSpins * 360f) + Mathf.DeltaAngle(0f, dropAngleWorldDeg - targetPocketOffset);

        // ----- BALL SETTINGS -----
        // Give the ball a random starting angle for variation
        float randomStartOffset = Random.Range(0f, 360f);
        float ballStartAngle = randomStartOffset;

        // The ball rolls opposite direction (counter to wheel), making several full laps
        float ballEndAngle = dropAngleWorldDeg - (ballExtraSpins * 360f);

        // Store start wheel rotation
        Vector3 wheelEuler = wheel.eulerAngles;
        float t = 0f;

        // Animation loop
        while (t < spinDuration)
        {
            t += Time.deltaTime;
            float kWheel = wheelEase.Evaluate(Mathf.Clamp01(t / spinDuration));
            float kBall = ballEase.Evaluate(Mathf.Clamp01(t / spinDuration));

            // ----- WHEEL -----
            float newY = Mathf.LerpAngle(currentWheelY, wheelFinalY, kWheel);
            wheelEuler.y = newY;
            wheel.rotation = Quaternion.Euler(wheelEuler);

            // ----- BALL -----
            // Delay the inward fall — stays outer until ~70% of spin
            float fallStart = 0.7f;
            float fallT = Mathf.InverseLerp(fallStart, 1f, kBall);
            float radius = Mathf.Lerp(ballOuterRadius, ballInnerRadius, Mathf.Clamp01(fallT));

            // Move ball angle around the wheel
            float ballAngle = Mathf.LerpAngle(ballStartAngle, ballEndAngle, kBall);
            Vector3 pos = PolarOnPlane(transform.position, ballAngle, radius, ballHeight);
            ball.position = pos;

            yield return null;
        }

        // Snap to final exact position
        wheelEuler.y = wheelFinalY;
        wheel.rotation = Quaternion.Euler(wheelEuler);
        ball.position = PolarOnPlane(transform.position, dropAngleWorldDeg, ballInnerRadius, ballHeight);

        spinning = false;
    }

    /// <summary>
    /// Converts polar coordinates to a position on the XZ plane.
    /// </summary>
    private Vector3 PolarOnPlane(Vector3 center, float angleDeg, float radius, float y)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        float x = Mathf.Sin(rad) * radius;
        float z = Mathf.Cos(rad) * radius;
        return new Vector3(center.x + x, center.y + y, center.z + z);
    }

    // optional gizmo preview for editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, ballOuterRadius);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, ballInnerRadius);
    }
}
