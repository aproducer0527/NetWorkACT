using UnityEngine;

namespace Entities
{
    /// <summary>
    /// Player 和 Enemy 共用的运行时运动状态与基础计算。
    /// 具体参数由各自的 ScriptableObject 数据提供。
    /// </summary>
    public abstract class EntityBase : MonoBehaviour
    {
        /// <summary>当前世界速度，单位：米/秒。</summary>
        public Vector3 Velocity { get; protected set; }

        /// <summary>水平速度，不包含 Y 轴分量。</summary>
        public Vector3 HorizontalVelocity => new Vector3(Velocity.x, 0f, Velocity.z);

        /// <summary>垂直速度；正值向上，负值向下。</summary>
        public float VerticalVelocity => Velocity.y;

        /// <summary>最近一次地面检测的结果。</summary>
        public bool IsGrounded { get; protected set; }

        /// <summary>当前地面的法线；未着地时为世界上方向。</summary>
        public Vector3 GroundNormal { get; protected set; } = Vector3.up;

        /// <summary>只更新水平速度，保留现有垂直速度。</summary>
        protected void SetHorizontalVelocity(Vector3 horizontalVelocity)
        {
            Velocity = new Vector3(horizontalVelocity.x, Velocity.y, horizontalVelocity.z);
        }

        /// <summary>只更新垂直速度，保留现有水平速度。</summary>
        protected void SetVerticalVelocity(float verticalVelocity)
        {
            Velocity = new Vector3(Velocity.x, verticalVelocity, Velocity.z);
        }

        /// <summary>
        /// 根据外部地面检测结果更新着地状态。
        /// 检测位置、碰撞形状和地面层由 Player 或 Enemy 决定。
        /// </summary>
        protected void SetGroundContact(bool grounded, Vector3 groundNormal)
        {
            IsGrounded = grounded;
            GroundNormal = grounded && groundNormal.sqrMagnitude > 0f
                ? groundNormal.normalized
                : Vector3.up;
        }

        /// <summary>
        /// 将水平速度逐渐调整到目标方向与速度。
        /// worldDirection 可为零，也可传入长度不超过 1 的模拟摇杆输入。
        /// </summary>
        protected void AccelerateHorizontal(
            Vector3 worldDirection,
            float targetSpeed,
            float acceleration,
            float deltaTime)
        {
            if (deltaTime <= 0f)
                return;

            Vector3 direction = Vector3.ProjectOnPlane(worldDirection, Vector3.up);
            direction = Vector3.ClampMagnitude(direction, 1f);

            Vector3 targetVelocity = direction * Mathf.Max(0f, targetSpeed);
            Vector3 nextVelocity = Vector3.MoveTowards(
                HorizontalVelocity,
                targetVelocity,
                Mathf.Max(0f, acceleration) * deltaTime);

            SetHorizontalVelocity(nextVelocity);
        }

        /// <summary>在空中累计重力，并限制最大下落速度。</summary>
        protected void ApplyGravity(float gravity, float maxFallSpeed, float deltaTime)
        {
            if (IsGrounded || deltaTime <= 0f)
                return;

            float nextVelocity = Velocity.y - Mathf.Max(0f, gravity) * deltaTime;
            SetVerticalVelocity(Mathf.Max(nextVelocity, -Mathf.Max(0f, maxFallSpeed)));
        }

        /// <summary>根据期望高度计算起跳速度。成功起跳时返回 true。</summary>
        protected bool TryJump(float jumpHeight, float gravity)
        {
            if (!IsGrounded || jumpHeight <= 0f || gravity <= 0f)
                return false;

            SetVerticalVelocity(Mathf.Sqrt(2f * gravity * jumpHeight));
            SetGroundContact(false, Vector3.up);
            return true;
        }

        /// <summary>以指定角速度朝水平方向转身。</summary>
        protected void FaceDirection(Vector3 worldDirection, float degreesPerSecond, float deltaTime)
        {
            Vector3 flatDirection = Vector3.ProjectOnPlane(worldDirection, Vector3.up);
            if (flatDirection.sqrMagnitude <= 0.0001f || deltaTime <= 0f)
                return;

            Quaternion targetRotation = Quaternion.LookRotation(flatDirection, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                Mathf.Max(0f, degreesPerSecond) * deltaTime);
        }

        /// <summary>清除运行时速度和地面状态。</summary>
        protected void ResetMotion()
        {
            Velocity = Vector3.zero;
            SetGroundContact(false, Vector3.up);
        }
    }
}
