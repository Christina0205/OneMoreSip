using UnityEngine;

namespace OneMoreSip
{
    /// <summary>
    /// A gray NPC that patrols left/right and catches the player when they are within
    /// the 3-unit detection range AND are showing a visible bottle or are drinking.
    /// There is no fail state - being caught is feedback only.
    /// </summary>
    public class NPCDetection : MonoBehaviour
    {
        public const float DetectionRange = 3f;
        public const float PatrolSpeed = 2f;

        private float _minX, _maxX;
        private int _dir = 1;
        private float _lastCatchMessage = -10f;

        public void Init(float minX, float maxX)
        {
            _minX = minX;
            _maxX = maxX;
        }

        private void Update()
        {
            Patrol();
            CheckCatch();
        }

        private void Patrol()
        {
            Vector3 pos = transform.position;
            pos.x += _dir * PatrolSpeed * Time.deltaTime;
            if (pos.x >= _maxX) { pos.x = _maxX; _dir = -1; }
            else if (pos.x <= _minX) { pos.x = _minX; _dir = 1; }
            transform.position = pos;
        }

        private void CheckCatch()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.Player == null || gm.Bottle == null) return;

            float dist = Mathf.Abs(transform.position.x - gm.Player.transform.position.x);
            if (dist > DetectionRange) return;

            bool caught = gm.Bottle.VisibleBottle || gm.Bottle.IsDrinking;
            if (caught && Time.time - _lastCatchMessage > 1.5f)
            {
                _lastCatchMessage = Time.time;
                gm.UI.ShowNpcMessage("Caught: your bottle was not hidden.", 2f);
            }
        }
    }
}
