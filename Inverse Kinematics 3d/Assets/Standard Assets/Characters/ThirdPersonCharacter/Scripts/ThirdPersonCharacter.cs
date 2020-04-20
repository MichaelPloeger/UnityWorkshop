using UnityEngine;

namespace UnityStandardAssets.Characters.ThirdPerson
{
	[RequireComponent(typeof(Rigidbody))]
	[RequireComponent(typeof(CapsuleCollider))]
	[RequireComponent(typeof(Animator))]
	public class ThirdPersonCharacter : MonoBehaviour
	{
		[SerializeField] float m_MovingTurnSpeed = 360;
		[SerializeField] float m_StationaryTurnSpeed = 180;
		[SerializeField] float m_JumpPower = 12f;
		[Range(1f, 4f)][SerializeField] float m_GravityMultiplier = 2f;
		[SerializeField] float m_RunCycleLegOffset = 0.2f; //specific to the character in sample assets, will need to be modified to work with others
		[SerializeField] float m_MoveSpeedMultiplier = 1f;
		[SerializeField] float m_AnimSpeedMultiplier = 1f;
		[SerializeField] float m_GroundCheckDistance = 0.1f;

		Rigidbody m_Rigidbody;
		Animator m_Animator;
		bool m_IsGrounded;
		float m_OrigGroundCheckDistance;
		const float k_Half = 0.5f;
		float m_TurnAmount;
		float m_ForwardAmount;
		Vector3 m_GroundNormal;
		float m_CapsuleHeight;
		Vector3 m_CapsuleCenter;
		CapsuleCollider m_Capsule;
		bool m_Crouching;


		[Header("Feet Grounder")]
		private Vector3 rightFootPosition, leftFootPosition, leftFootTargetPosition, rightFootTargetPosition;
		private Quaternion leftFootIkRotation, rightFootIkRotation;
		private float lastPelvisPositionY, lastRightFootPositionY, lastLeftFootPositionY;

		public bool enableFeetIk = true;
		[Range(0, 2)] [SerializeField] private float heightFromGroundRaycast = 1.14f;
		[Range(0, 2)] [SerializeField] private float raycastDownDistance = 1.5f;
		[SerializeField] private LayerMask environmentLayer;
		[SerializeField] private float pelvisOffset = 0f;
		[Range(0, 1)] [SerializeField] private float pelvisUpAndDownSpeed = 0.28f;
		[Range(0, 1)] [SerializeField] private float feetToIkPostionSpeed = 0.5f;

		public string leftFootAnimVariableName = "LeftFootCurve";
		public string rightFootAnimVariableName = "RightFootCurve";
		public bool useProIkFeature = false;
		public bool showSolverDebug = true;


		void Start()
		{
			m_Animator = GetComponent<Animator>();
			m_Rigidbody = GetComponent<Rigidbody>();
			m_Capsule = GetComponent<CapsuleCollider>();
			m_CapsuleHeight = m_Capsule.height;
			m_CapsuleCenter = m_Capsule.center;

			m_Rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
			m_OrigGroundCheckDistance = m_GroundCheckDistance;
		}


		public void Move(Vector3 move, bool crouch, bool jump)
		{

			// convert the world relative moveInput vector into a local-relative
			// turn amount and forward amount required to head in the desired
			// direction.
			if (move.magnitude > 1f) move.Normalize();
			move = transform.InverseTransformDirection(move);
			CheckGroundStatus();
			move = Vector3.ProjectOnPlane(move, m_GroundNormal);
			m_TurnAmount = Mathf.Atan2(move.x, move.z);
			m_ForwardAmount = move.z;

			ApplyExtraTurnRotation();

			// control and velocity handling is different when grounded and airborne:
			if (m_IsGrounded)
			{
				HandleGroundedMovement(crouch, jump);
			}
			else
			{
				HandleAirborneMovement();
			}

			ScaleCapsuleForCrouching(crouch);
			PreventStandingInLowHeadroom();

			// send input and other state parameters to the animator
			UpdateAnimator(move);
		}


		void ScaleCapsuleForCrouching(bool crouch)
		{
			if (m_IsGrounded && crouch)
			{
				if (m_Crouching) return;
				m_Capsule.height = m_Capsule.height / 2f;
				m_Capsule.center = m_Capsule.center / 2f;
				m_Crouching = true;
			}
			else
			{
				Ray crouchRay = new Ray(m_Rigidbody.position + Vector3.up * m_Capsule.radius * k_Half, Vector3.up);
				float crouchRayLength = m_CapsuleHeight - m_Capsule.radius * k_Half;
				if (Physics.SphereCast(crouchRay, m_Capsule.radius * k_Half, crouchRayLength, Physics.AllLayers, QueryTriggerInteraction.Ignore))
				{
					m_Crouching = true;
					return;
				}
				m_Capsule.height = m_CapsuleHeight;
				m_Capsule.center = m_CapsuleCenter;
				m_Crouching = false;
			}
		}

		void PreventStandingInLowHeadroom()
		{
			// prevent standing up in crouch-only zones
			if (!m_Crouching)
			{
				Ray crouchRay = new Ray(m_Rigidbody.position + Vector3.up * m_Capsule.radius * k_Half, Vector3.up);
				float crouchRayLength = m_CapsuleHeight - m_Capsule.radius * k_Half;
				if (Physics.SphereCast(crouchRay, m_Capsule.radius * k_Half, crouchRayLength, Physics.AllLayers, QueryTriggerInteraction.Ignore))
				{
					m_Crouching = true;
				}
			}
		}


		void UpdateAnimator(Vector3 move)
		{
			// update the animator parameters
			m_Animator.SetFloat("Forward", m_ForwardAmount, 0.1f, Time.deltaTime);
			m_Animator.SetFloat("Turn", m_TurnAmount, 0.1f, Time.deltaTime);
			m_Animator.SetBool("Crouch", m_Crouching);
			m_Animator.SetBool("OnGround", m_IsGrounded);
			if (!m_IsGrounded)
			{
				m_Animator.SetFloat("Jump", m_Rigidbody.velocity.y);
			}

			// calculate which leg is behind, so as to leave that leg trailing in the jump animation
			// (This code is reliant on the specific run cycle offset in our animations,
			// and assumes one leg passes the other at the normalized clip times of 0.0 and 0.5)
			float runCycle =
				Mathf.Repeat(
					m_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime + m_RunCycleLegOffset, 1);
			float jumpLeg = (runCycle < k_Half ? 1 : -1) * m_ForwardAmount;
			if (m_IsGrounded)
			{
				m_Animator.SetFloat("JumpLeg", jumpLeg);
			}

			// the anim speed multiplier allows the overall speed of walking/running to be tweaked in the inspector,
			// which affects the movement speed because of the root motion.
			if (m_IsGrounded && move.magnitude > 0)
			{
				m_Animator.speed = m_AnimSpeedMultiplier;
			}
			else
			{
				// don't use that while airborne
				m_Animator.speed = 1;
			}
		}


		void HandleAirborneMovement()
		{
			// apply extra gravity from multiplier:
			Vector3 extraGravityForce = (Physics.gravity * m_GravityMultiplier) - Physics.gravity;
			m_Rigidbody.AddForce(extraGravityForce);

			m_GroundCheckDistance = m_Rigidbody.velocity.y < 0 ? m_OrigGroundCheckDistance : 0.01f;
		}


		void HandleGroundedMovement(bool crouch, bool jump)
		{
			// check whether conditions are right to allow a jump:
			if (jump && !crouch && m_Animator.GetCurrentAnimatorStateInfo(0).IsName("Grounded"))
			{
				// jump!
				m_Rigidbody.velocity = new Vector3(m_Rigidbody.velocity.x, m_JumpPower, m_Rigidbody.velocity.z);
				m_IsGrounded = false;
				m_Animator.applyRootMotion = false;
				m_GroundCheckDistance = 0.1f;
			}
		}

		void ApplyExtraTurnRotation()
		{
			// help the character turn faster (this is in addition to root rotation in the animation)
			float turnSpeed = Mathf.Lerp(m_StationaryTurnSpeed, m_MovingTurnSpeed, m_ForwardAmount);
			transform.Rotate(0, m_TurnAmount * turnSpeed * Time.deltaTime, 0);
		}


		public void OnAnimatorMove()
		{
			// we implement this function to override the default root motion.
			// this allows us to modify the positional speed before it's applied.
			if (m_IsGrounded && Time.deltaTime > 0)
			{
				Vector3 v = (m_Animator.deltaPosition * m_MoveSpeedMultiplier) / Time.deltaTime;

				// we preserve the existing y part of the current velocity.
				v.y = m_Rigidbody.velocity.y;
				m_Rigidbody.velocity = v;
			}
		}


		void CheckGroundStatus()
		{
			RaycastHit hitInfo;
#if UNITY_EDITOR
			// helper to visualise the ground check ray in the scene view
			Debug.DrawLine(transform.position + (Vector3.up * 0.1f), transform.position + (Vector3.up * 0.1f) + (Vector3.down * m_GroundCheckDistance));
#endif
			// 0.1f is a small offset to start the ray from inside the character
			// it is also good to note that the transform position in the sample assets is at the base of the character
			if (Physics.Raycast(transform.position + (Vector3.up * 0.1f), Vector3.down, out hitInfo, m_GroundCheckDistance))
			{
				m_GroundNormal = hitInfo.normal;
				m_IsGrounded = true;
				m_Animator.applyRootMotion = true;
			}
			else
			{
				m_IsGrounded = false;
				m_GroundNormal = Vector3.up;
				m_Animator.applyRootMotion = false;
			}
		}


        #region Workshop code
        private void FixedUpdate()
		{
			if (!enableFeetIk) return;
			if (m_Animator == null) return;

			AdjustFeetTarget(ref rightFootPosition, HumanBodyBones.RightFoot);
			AdjustFeetTarget(ref leftFootPosition, HumanBodyBones.LeftFoot);

			FeetPositionSolver(rightFootPosition, ref rightFootTargetPosition, ref rightFootIkRotation);
			FeetPositionSolver(leftFootPosition, ref leftFootTargetPosition, ref leftFootIkRotation);
		}

		private void OnAnimatorIK(int layerIndex)
		{
			if (!enableFeetIk || !m_IsGrounded) return;
			if (m_Animator == null) return;

			MovePelvisHeight();

			m_Animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 1);
			//Used for setting the feet correctly on any platform
			if (useProIkFeature)
			{
				//For this to work the walk animation needs to be changed
				m_Animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, m_Animator.GetFloat(rightFootAnimVariableName));
			}

			MoveFeetToIkPoint(AvatarIKGoal.RightFoot, rightFootTargetPosition, rightFootIkRotation, ref lastRightFootPositionY);


			m_Animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1);
			if (useProIkFeature)
			{
				m_Animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, m_Animator.GetFloat(leftFootAnimVariableName));
			}

			MoveFeetToIkPoint(AvatarIKGoal.LeftFoot, leftFootTargetPosition, leftFootIkRotation, ref lastLeftFootPositionY);
		}


		/// <summary>
		/// Adjust the feet to the Ik point
		/// </summary>
		/// <param name="foot">current foot</param>
		/// <param name="targetPositionHolder">holds the y needed for the foot</param>
		/// <param name="rotationIkHolder">the rotation needed for the foor</param>
		/// <param name="lastFootPostionY">the last position the foot was</param>
		private void MoveFeetToIkPoint(AvatarIKGoal foot, Vector3 targetPositionHolder, Quaternion rotationIkHolder, ref float lastFootPostionY) 
		{
			Vector3 targetIkPosition = m_Animator.GetIKPosition(foot);

			//check if targetPositionHolder is not equal to zero
			//set targetPositionHolder and targetIkPostion to local space 
			//add the targetPositionHolder.y to the targetIkPosition it has to be done in feetToIkPostionSpeed (mathf.Lerp)
			//Set lastfootposition
			//Set targetIkPostion to world space again and finally set the Ik rotation(rotationIkHolder) for the animator

			m_Animator.SetIKPosition(foot, targetIkPosition);
		}

		/// <summary>
		/// Adjust pelvis by the offset between the bodyposition and the highest foot
		/// </summary>
		private void MovePelvisHeight()
		{
			//These variables gonna be used for calculating the height and needs to be set in order to work
			if (rightFootTargetPosition == Vector3.zero || leftFootTargetPosition == Vector3.zero || lastPelvisPositionY == 0)
			{
				lastPelvisPositionY = m_Animator.bodyPosition.y;
				return;
			}

			//Calculate the offset of the footTargetPosition between the y position
			

			//Set new pelvisPosition using the offset and bodyposition from the animator. Set y position after in pelvisUpAnDownSpeed

			//Set the body position to the new pelvisposition, set lastPelvisPositionY

		}

		/// <summary>
		/// ReAdjusts the feet to the correct place
		/// </summary>
		/// <param name="footPosition">Current position of the foot</param>
		/// <param name="feetTargetPosition">current position of the feet target</param>
		/// <param name="feetIkRotations">Current rotations of the feet</param>
		private void FeetPositionSolver(Vector3 footPosition, ref Vector3 feetTargetPosition, ref Quaternion feetIkRotations)
		{
			//Visualisation for the ray;
			if (showSolverDebug)
				Debug.DrawLine(footPosition, footPosition + Vector3.down * (raycastDownDistance + heightFromGroundRaycast), Color.yellow);

			//Check if footPosition hits the ground with distance raycastDownDistance + heightFromGroundRaycast
			//if it does set feetIkPosition, override the y position to the correct hit, calculate foot rotation and set FeetIkRotations and return.

			//If not worked
			feetTargetPosition = Vector3.zero;

		}

		/// <summary>
		///	Sets the position to the foot and adjusts the height with the heightFromGroundRaycast
		/// </summary>
		/// <param name="feetPosition">position of the feet that needs to be changed</param>
		/// <param name="foot">foot bone</param>
		private void AdjustFeetTarget(ref Vector3 feetPosition, HumanBodyBones foot)
		{
			//Get the foot position from the animator
		}

		#endregion

	}
}
