#nullable enable

using System.Collections.Generic;
using UnityEngine;

namespace CavalryFight.Services.Combat
{
    /// <summary>
    /// シーン内の矢を追跡するサービス
    /// </summary>
    /// <remarks>
    /// FindObjectsOfTypeを避けるため、矢が自身を登録/解除します。
    /// </remarks>
    public class ArrowTrackerService : IArrowTrackerService
    {
        private readonly List<Transform> _activeArrows = new();

        /// <summary>
        /// アクティブな矢のリスト（読み取り専用）
        /// </summary>
        public IReadOnlyList<Transform> ActiveArrows => _activeArrows;

        /// <summary>
        /// サービスを初期化します
        /// </summary>
        public void Initialize()
        {
            // 特別な初期化は不要
        }

        /// <summary>
        /// サービスを破棄します
        /// </summary>
        public void Dispose()
        {
            _activeArrows.Clear();
        }

        /// <summary>
        /// 矢を登録します
        /// </summary>
        public void RegisterArrow(Transform arrow)
        {
            if (arrow != null && !_activeArrows.Contains(arrow))
            {
                _activeArrows.Add(arrow);
            }
        }

        /// <summary>
        /// 矢を解除します
        /// </summary>
        public void UnregisterArrow(Transform arrow)
        {
            _activeArrows.Remove(arrow);
        }

        /// <summary>
        /// 指定位置に最も近い矢を取得します
        /// </summary>
        public bool TryGetNearestArrow(Vector3 position, float maxDistance, out Transform? nearestArrow, out float distance)
        {
            nearestArrow = null;
            distance = float.MaxValue;

            // nullエントリを除去（破壊された矢）
            _activeArrows.RemoveAll(a => a == null);

            foreach (var arrow in _activeArrows)
            {
                float dist = Vector3.Distance(arrow.position, position);
                if (dist < maxDistance && dist < distance)
                {
                    distance = dist;
                    nearestArrow = arrow;
                }
            }

            return nearestArrow != null;
        }
    }
}
