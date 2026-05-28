using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Переключает визуальные скины мяча (дочерние объекты под Skin) и сообщает об изменении.
/// </summary>
[DisallowMultipleComponent]
public sealed class BallSkinController : MonoBehaviour
{
    public event Action<BallSkinId> SkinChanged;

    [Header("Связи")]
    [SerializeField] Transform skinRoot;
    [SerializeField] BallSkinId defaultSkin = BallSkinId.Defolt;

    readonly Dictionary<BallSkinId, GameObject> _skinObjects = new();
    BallSkinId _activeSkin = BallSkinId.Defolt;

    public BallSkinId ActiveSkin => _activeSkin;

    void Awake()
    {
        if (skinRoot == null)
            skinRoot = transform.Find("Skin");

        CacheSkins();
        SetSkin(defaultSkin, invokeEvent: false);
    }

    public void SetSkin(BallSkinId skinId, bool invokeEvent = true)
    {
        if (_skinObjects.Count == 0)
            CacheSkins();

        if (!_skinObjects.ContainsKey(skinId))
            skinId = BallSkinId.Defolt;

        _activeSkin = skinId;

        foreach (KeyValuePair<BallSkinId, GameObject> pair in _skinObjects)
        {
            if (pair.Value == null)
                continue;

            pair.Value.SetActive(pair.Key == skinId);
        }

        if (invokeEvent)
            SkinChanged?.Invoke(_activeSkin);
    }

    public void SetSkinByObjectName(string skinObjectName)
    {
        if (string.IsNullOrWhiteSpace(skinObjectName))
            return;

        if (TryParseSkinName(skinObjectName, out BallSkinId skinId))
            SetSkin(skinId);
    }

    void CacheSkins()
    {
        _skinObjects.Clear();

        if (skinRoot == null)
            return;

        for (int i = 0; i < skinRoot.childCount; i++)
        {
            Transform child = skinRoot.GetChild(i);
            if (child == null)
                continue;

            if (!TryParseSkinName(child.name, out BallSkinId skinId))
                continue;

            _skinObjects[skinId] = child.gameObject;
        }
    }

    public static bool TryParseSkinName(string objectName, out BallSkinId skinId)
    {
        skinId = BallSkinId.Defolt;
        if (string.IsNullOrWhiteSpace(objectName))
            return false;

        string normalized = objectName.Trim();
        if (normalized.Equals("gnom", StringComparison.OrdinalIgnoreCase))
        {
            skinId = BallSkinId.Gnom;
            return true;
        }

        if (normalized.Equals("My_ball", StringComparison.OrdinalIgnoreCase))
        {
            skinId = BallSkinId.MyBall;
            return true;
        }

        return Enum.TryParse(normalized, ignoreCase: true, out skinId);
    }
}
