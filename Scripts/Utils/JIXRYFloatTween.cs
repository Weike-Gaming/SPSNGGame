using Dreamteck.Splines;
using UnityEngine;

namespace Weike.Games.JIXRY
{
    public class JIXRYFloatTween : MonoBehaviour
    {
        [SerializeField] private SplineFollower follower = null;

        private Vector3 _startPos;
        private bool _doOnce = false;
        private bool _playAnimation = false;

        [SerializeField] private float verticalAmplitude = 0.9f;
        [SerializeField] private float horizontalAmplitude = 0.5f;
        [SerializeField] private float verticalDuration = 1.6f;
        [SerializeField] private float horizontalDuration = 2.0f;

        private float _verticalTimer = 0f;
        private float _horizontalTimer = 0f;

        private int _verticalDirection = 1;   // 1 = up, -1 = down
        private int _horizontalDirection = 1; // 1 = right, -1 = left

        private void Awake()
        {
            if(!_doOnce)
            {
                follower.onEndReached += OnSplineEndReached;
                _doOnce = true;
            }
        }

        private void OnDisable()
        {
            ResetProperties();
        }
        private void OnSplineEndReached(double d)
        {
            StartFloatTween();
        }

        public void StartFloatTween()
        {
            _playAnimation = true;
            follower.follow = false;
            follower.motion.applyRotation = false;
            _startPos = transform.position;      
        }

        private void Update()
        {
            if (!_playAnimation) return;
            float deltaTime = Time.deltaTime;

            // Vertical ping-pong
            _verticalTimer += deltaTime * _verticalDirection;
            if (_verticalTimer > verticalDuration)
            {
                _verticalTimer = verticalDuration;
                _verticalDirection *= -1;
            }
            else if (_verticalTimer < 0f)
            {
                _verticalTimer = 0f;
                _verticalDirection *= -1;
            }
            float verticalProgress = _verticalTimer / verticalDuration; // 0..1
            float newY = _startPos.y + verticalAmplitude * Mathf.Sin(verticalProgress * Mathf.PI);

            // Horizontal ping-pong
            _horizontalTimer += deltaTime * _horizontalDirection;
            if (_horizontalTimer > horizontalDuration)
            {
                _horizontalTimer = horizontalDuration;
                _horizontalDirection *= -1;
            }
            else if (_horizontalTimer < 0f)
            {
                _horizontalTimer = 0f;
                _horizontalDirection *= -1;
            }
            float horizontalProgress = _horizontalTimer / horizontalDuration; // 0..1
            float newX = _startPos.x + horizontalAmplitude * Mathf.Sin(horizontalProgress * Mathf.PI);

            // Apply position
            Vector3 pos = transform.position;
            pos.x = newX;
            pos.y = newY;
            transform.position = pos;
        }
        public void ResetProperties()
        {
            _playAnimation = false;
            _verticalTimer = 0;
            _horizontalTimer = 0;
        }
    }
}