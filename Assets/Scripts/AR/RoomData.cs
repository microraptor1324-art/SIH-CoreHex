using System;
using UnityEngine;

namespace ARMiningSimulator.AR
{
    /// <summary>
    /// Persistent data structure holding the measured dimensions of the trainee's room.
    /// Accessible globally so the Phase 3 Environment Generator can build the mining area
    /// strictly inside these measured physical boundaries.
    /// </summary>
    [Serializable]
    public class RoomData
    {
        public static RoomData Current { get; private set; } = new RoomData();

        public static event Action<RoomData> OnRoomDataUpdated;

        [SerializeField] private float _length;
        [SerializeField] private float _width;
        [SerializeField] private float _area;
        [SerializeField] private Vector3 _center;
        [SerializeField] private Quaternion _rotation;
        [SerializeField] private Vector3[] _corners;
        [SerializeField] private bool _isConfirmed;

        public float Length => _length;
        public float Width => _width;
        public float Area => _area;
        public Vector3 Center => _center;
        public Quaternion Rotation => _rotation;
        public Vector3[] Corners => _corners;
        public bool IsConfirmed => _isConfirmed;
        public bool IsValid => _length > 0.5f && _width > 0.5f && _area > 1.0f;

        public RoomData()
        {
            _length = 0f;
            _width = 0f;
            _area = 0f;
            _center = Vector3.zero;
            _rotation = Quaternion.identity;
            _corners = Array.Empty<Vector3>();
            _isConfirmed = false;
        }

        public RoomData(float length, float width, Vector3 center, Quaternion rotation, Vector3[] corners, bool confirmed = false)
        {
            _length = Mathf.Max(length, width);
            _width = Mathf.Min(length, width);
            _area = _length * _width;
            _center = center;
            _rotation = rotation;
            _corners = corners ?? Array.Empty<Vector3>();
            _isConfirmed = confirmed;
        }

        public static void SetCurrent(RoomData data)
        {
            Current = data ?? new RoomData();
            OnRoomDataUpdated?.Invoke(Current);
            Debug.Log($"[RoomData] Stored dimensions: Length={Current.Length:F2}m, Width={Current.Width:F2}m, Area={Current.Area:F2}m² (Confirmed: {Current.IsConfirmed})");
        }

        public static void Reset()
        {
            Current = new RoomData();
            OnRoomDataUpdated?.Invoke(Current);
        }
    }
}
