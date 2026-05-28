using UnityEngine;

/// <summary>Применяет сохранённый скин к мячу игрока при появлении.</summary>
public sealed class BallSkinSelectionApplier : MonoBehaviour
{
    [SerializeField] BallSpawner ballSpawner;

    void Awake()
    {
        if (ballSpawner == null)
            ballSpawner = GetComponent<BallSpawner>();

        if (ballSpawner == null)
            ballSpawner = BallSpawner.Instance;
    }

    void OnEnable()
    {
        if (ballSpawner == null)
            return;

        ballSpawner.ThrowableBallReady += HandleThrowableBallReady;

        if (ballSpawner.CurrentThrowableBall != null)
            ApplyToBall(ballSpawner.CurrentThrowableBall);
    }

    void OnDisable()
    {
        if (ballSpawner != null)
            ballSpawner.ThrowableBallReady -= HandleThrowableBallReady;
    }

    void HandleThrowableBallReady(SlingshotShooter ball) => ApplyToBall(ball);

    public static void ApplyToBall(SlingshotShooter ball)
    {
        if (ball == null || ball.gameObject.name.Contains("Bot"))
            return;

        if (!ball.TryGetComponent(out BallSkinController skinController))
            return;

        skinController.SetSkin(BallSkinSelectionStorage.SelectedSkin);
    }
}
