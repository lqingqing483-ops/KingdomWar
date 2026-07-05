using UnityEngine;
using System.Collections;

namespace KingdomWar.Game.Battle
{
    public class AnimationStateMachine
    {
        // 动画状态枚举
        public enum AnimationState
        {
            Idle,
            Walk,
            Attack,
            GetHit,
            Death
        }

        private Animator animator;
        private AnimationState currentState;
        private AnimationState previousState;
        private float stateStartTime;

        // 状态转换表
        private bool[,] stateTransitions = new bool[,]
        {
            // Idle, Walk, Attack, GetHit, Death
            { true,  true,  true,  true,  true },  // Idle
            { true,  true,  true,  true,  true },  // Walk
            { true,  true,  true,  true,  true },  // Attack
            { true,  true,  true,  false, true },  // GetHit
            { false, false, false, false, true }   // Death
        };

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="animator">动画控制器</param>
        public AnimationStateMachine(Animator animator)
        {
            this.animator = animator;
            currentState = AnimationState.Idle;
            previousState = AnimationState.Idle;
            stateStartTime = Time.time;
        }

        /// <summary>
        /// 更新状态机
        /// </summary>
        public void Update()
        {
            // 这里可以添加状态机的更新逻辑
            // 例如处理状态持续时间、状态转换条件等
        }

        /// <summary>
        /// 转换到指定状态
        /// </summary>
        /// <param name="newState">新状态</param>
        /// <returns>是否成功转换</returns>
        public bool TransitionToState(AnimationState newState)
        {
            // 检查状态转换是否有效
            if (!stateTransitions[(int)currentState, (int)newState])
            {
                return false;
            }

            // 记录前一个状态
            previousState = currentState;
            // 设置当前状态
            currentState = newState;
            // 记录状态开始时间
            stateStartTime = Time.time;

            // 触发相应的动画
            TriggerAnimation(newState);

            return true;
        }

        /// <summary>
        /// 触发动画
        /// </summary>
        /// <param name="state">动画状态</param>
        private void TriggerAnimation(AnimationState state)
        {
            if (animator == null)
                return;

            // 重置所有动画参数
            ResetAnimationParameters();

            // 根据状态设置相应的动画参数
            switch (state)
            {
                case AnimationState.Idle:
                    animator.SetBool("IsMoving", false);
                    animator.SetBool("Attack", false);
                    animator.SetBool("IsDead", false);
                    break;
                case AnimationState.Walk:
                    animator.SetBool("IsMoving", true);
                    animator.SetFloat("MoveSpeed", 1.0f);
                    animator.SetBool("Attack", false);
                    animator.SetBool("IsDead", false);
                    break;
                case AnimationState.Attack:
                    animator.SetBool("IsMoving", false);
                    animator.SetBool("Attack", true);
                    animator.SetBool("IsDead", false);
                    break;
                case AnimationState.GetHit:
                    animator.SetBool("GetHit", true);
                    break;
                case AnimationState.Death:
                    animator.SetBool("IsDead", true);
                    break;
            }
        }

        /// <summary>
        /// 重置动画参数
        /// </summary>
        private void ResetAnimationParameters()
        {
            if (animator == null)
                return;

            animator.SetBool("IsMoving", false);
            animator.SetFloat("MoveSpeed", 0f);
            animator.SetBool("Attack", false);
            animator.SetBool("GetHit", false);
            animator.SetBool("IsDead", false);
        }

        /// <summary>
        /// 获取当前状态
        /// </summary>
        public AnimationState CurrentState
        {
            get { return currentState; }
        }

        /// <summary>
        /// 获取前一个状态
        /// </summary>
        public AnimationState PreviousState
        {
            get { return previousState; }
        }

        /// <summary>
        /// 获取当前状态持续时间
        /// </summary>
        public float StateDuration
        {
            get { return Time.time - stateStartTime; }
        }

        /// <summary>
        /// 处理单位状态变化
        /// </summary>
        /// <param name="unitState">单位状态</param>
        /// <param name="isHit">是否受击</param>
        /// <param name="isDead">是否死亡</param>
        public void HandleUnitState(UnitState unitState, bool isHit, bool isDead)
        {
            // 优先处理死亡状态
            if (isDead)
            {
                TransitionToState(AnimationState.Death);
                return;
            }

            // 处理受击状态
            if (isHit)
            {
                TransitionToState(AnimationState.GetHit);
                return;
            }

            // 根据单位状态转换动画状态
            switch (unitState)
            {
                case UnitState.Idle:
                    TransitionToState(AnimationState.Idle);
                    break;
                case UnitState.Moving:
                    TransitionToState(AnimationState.Walk);
                    break;
                case UnitState.Attacking:
                    TransitionToState(AnimationState.Attack);
                    break;
                case UnitState.Dead:
                    TransitionToState(AnimationState.Death);
                    break;
            }
        }

        /// <summary>
        /// 设置移动速度
        /// </summary>
        /// <param name="speed">移动速度</param>
        public void SetMoveSpeed(float speed)
        {
            if (animator != null)
            {
                animator.SetFloat("MoveSpeed", speed);
            }
        }
    }
}
