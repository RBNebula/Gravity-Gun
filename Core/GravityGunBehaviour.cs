using System;
using GravityGun.Diagnostics;
using MineMogul.ToolImportApi.Game;
using UnityEngine;

namespace GravityGun.Core
{
    internal sealed class GravityGunBehaviour : MonoBehaviour
    {
        [Header("Targeting")]
        public LayerMask GrabbableLayer = ~0;
        public float RaycastDistance = 25f;
        public float MaxAllowedTargetDistance = 25f;
        public bool DisallowKinematicTargets = true;

        [Header("Pull")]
        public float HoldDistance = 3f;
        public float PullAcceleration = 12f;
        public float PullMaxSpeed = 20f;
        public float LaunchImpulse = 45f;
        public float ReacquireBlockAfterShoot = 0.35f;

        [Header("Audio")]
        public float PullTickInterval = 0.12f;

        private RuntimeImportedTool? _tool;
        private Rigidbody? _heldBody;
        private int _heldInstanceId = -1;
        private bool _wantsPull;
        private float _nextPullAudioTime;
        private float _reacquireBlockedUntilTime;

        private SoundDefinition? _acquireSfx;
        private SoundDefinition? _pullTickSfx;
        private SoundDefinition? _releaseSfx;
        private SoundDefinition? _shootSfx;
        private SoundDefinition? _failedShootSfx;

        private readonly GravityGunTargeting _targeting = new GravityGunTargeting();
        private readonly GravityGunTargetValidator _validator = new GravityGunTargetValidator();
        private readonly GravityGunPullMotor _motor = new GravityGunPullMotor();
        private readonly GravityGunAudioController _audio = new GravityGunAudioController();
        private readonly GravityGunVfxController _vfx = new GravityGunVfxController();

        private bool _loggedModelBinding;
        private bool _loggedReanchor;
        private Collider[]? _toolColliders;
        private Rigidbody? _toolRootRigidbody;
        private bool _physicsCacheReady;
        private bool _holdFxActive;
        private bool _vfxInitialized;
        private float _nextVfxInitAttemptTime;

        private PlayerController? Owner => _tool != null ? _tool.Owner : null;
        private GameObject? ViewModel => _tool != null ? _tool.ViewModel : null;
        private GameObject? WorldModel => _tool != null ? _tool.WorldModel : null;

        private void Awake()
        {
            _tool = GetComponent<RuntimeImportedTool>();
            if (_tool == null)
            {
                enabled = false;
            }
        }

        private void OnEnable()
        {
            _loggedReanchor = false;
            SetHeldCollisionState(Owner != null);
            SyncHeldPhysicsState();
            MaintainViewModelAnchor();
            LogModelBindingOnce();
            _holdFxActive = false;
            _vfxInitialized = false;
            _nextVfxInitAttemptTime = 0f;
            _vfx.SetHoldFxActive(false);
            _vfx.ClearTransientFx();
        }

        private void OnDisable()
        {
            ForceReleaseCurrentTarget(playReleaseSound: true);
            _holdFxActive = false;
            _vfx.SetHoldFxActive(false);
            _vfx.ClearTransientFx();
        }

        private void FixedUpdate()
        {
            _holdFxActive = false;

            if (Owner == null)
            {
                if (_heldInstanceId != -1)
                {
                    ClearHeldState();
                }
                return;
            }

            if (_wantsPull)
            {
                _wantsPull = false;

                if (Time.time < _reacquireBlockedUntilTime)
                {
                    return;
                }

                TryPickupTarget();

                if (IsValidTarget(_heldBody))
                {
                    PullHeldTarget(_heldBody!);
                    PlaySfx_PullTick(_heldBody!.worldCenterOfMass);
                    _holdFxActive = true;
                    return;
                }
            }

            if (_heldBody != null)
            {
                ReleaseHeld(playReleaseSound: true);
                return;
            }

            if (_heldInstanceId != -1)
            {
                ClearHeldState();
            }
        }

        private void LateUpdate()
        {
            MaintainViewModelAnchor();
            UpdateFxForHoldingState();
        }

        public void SetAudio(
            SoundDefinition? acquire,
            SoundDefinition? pullTick,
            SoundDefinition? release,
            SoundDefinition? shoot,
            SoundDefinition? failedShoot)
        {
            _acquireSfx = acquire;
            _pullTickSfx = pullTick;
            _releaseSfx = release;
            _shootSfx = shoot;
            _failedShootSfx = failedShoot;
        }

        public string GetControlsText()
        {
            if (Singleton<KeybindManager>.Instance == null)
            {
                return "Drop - [Unbound]";
            }

            return "Drop - " + Singleton<KeybindManager>.Instance.GetBindingText(KeybindAction.DropTool) +
                   "\nPull Object - " + Singleton<KeybindManager>.Instance.GetBindingText(KeybindAction.SecondaryAttack) +
                   "\nLaunch Object - " + Singleton<KeybindManager>.Instance.GetBindingText(KeybindAction.PrimaryAttack) +
                   "\nRelease Object - " + Singleton<KeybindManager>.Instance.GetBindingText(KeybindAction.RotateObject);
        }

        public void PrimaryFire()
        {
            if (Owner == null || Owner.PlayerCamera == null)
            {
                return;
            }

            if (IsValidTarget(_heldBody))
            {
                FireHeld();
                return;
            }

            if (FireEmpty())
            {
                BlockReacquireAfterShoot();
                return;
            }

            PlaySfx_FailedShoot(Owner.PlayerCamera.transform.position);
        }

        public void SecondaryFireHeld()
        {
            if (Time.time < _reacquireBlockedUntilTime)
            {
                return;
            }

            _wantsPull = true;
        }

        public void Reload()
        {
            ForceReleaseCurrentTarget(playReleaseSound: true);
        }

        public void BeforeDropItem()
        {
            ForceReleaseCurrentTarget(playReleaseSound: true);
            _holdFxActive = false;
            _vfx.SetHoldFxActive(false);
            _vfx.ClearTransientFx();
            SetHeldCollisionState(false);
        }

        public void BeforeUnEquip()
        {
            ForceReleaseCurrentTarget(playReleaseSound: true);
            _holdFxActive = false;
            _vfx.SetHoldFxActive(false);
            _vfx.ClearTransientFx();
        }

        private bool TryAcquireTarget(out Rigidbody? rb, out Vector3 hitPoint)
        {
            return _targeting.TryGetSingleTarget(Owner, RaycastDistance, GrabbableLayer, out rb, out hitPoint);
        }

        private bool TryPickupTarget()
        {
            if (_heldBody != null)
            {
                return true;
            }

            if (!TryAcquireTarget(out Rigidbody? rb, out Vector3 hitPoint) || rb == null)
            {
                return false;
            }

            _heldBody = rb;
            _heldInstanceId = rb.GetInstanceID();
            PlaySfx_Acquire(hitPoint);
            return true;
        }

        private void ReleaseHeld(bool playReleaseSound)
        {
            if (_heldBody == null)
            {
                ClearHeldState();
                _vfx.SetHoldFxActive(false);
                return;
            }

            Vector3 p = _heldBody.worldCenterOfMass;
            _motor.ReleaseSingleTarget(_heldBody);
            if (playReleaseSound)
            {
                PlaySfx_Release(p);
            }

            ClearHeldState();
            _vfx.SetHoldFxActive(false);
        }

        private void FireHeld()
        {
            if (Owner == null || Owner.PlayerCamera == null || _heldBody == null)
            {
                return;
            }

            Vector3 releasePoint = _heldBody.worldCenterOfMass;
            _motor.LaunchSingleTarget(_heldBody, Owner.PlayerCamera.transform, LaunchImpulse);
            _motor.ReleaseSingleTarget(_heldBody);
            PlaySfx_Shoot(releasePoint);
            TryPlayShootBolt(releasePoint);
            PlaySfx_Release(releasePoint);
            ClearHeldState();
            BlockReacquireAfterShoot();
        }

        private bool FireEmpty()
        {
            if (Owner == null || Owner.PlayerCamera == null)
            {
                return false;
            }

            if (!TryAcquireTarget(out Rigidbody? rb, out Vector3 hitPoint) || rb == null)
            {
                return false;
            }

            _motor.LaunchSingleTarget(rb, Owner.PlayerCamera.transform, LaunchImpulse);
            PlaySfx_Shoot(hitPoint);
            TryPlayShootBolt(hitPoint);
            return true;
        }

        private void UpdateFxForHoldingState()
        {
            if (!_vfxInitialized)
            {
                TryInitializeVfx();
            }

            if (!_vfxInitialized)
            {
                _vfx.SetHoldFxActive(false);
                _vfx.ClearTransientFx();
                return;
            }

            bool shouldShow = Owner != null && _holdFxActive && _heldBody != null;
            _vfx.SetHoldFxActive(shouldShow);
            if (shouldShow)
            {
                _vfx.UpdateHoldArcs();
            }

            _vfx.UpdateTransientFx();
        }

        private void PullHeldTarget(Rigidbody heldBody)
        {
            if (Owner == null || Owner.PlayerCamera == null)
            {
                return;
            }

            _motor.PullSingleTarget(heldBody, Owner.PlayerCamera.transform, HoldDistance, PullAcceleration, PullMaxSpeed);
        }

        private bool IsValidTarget(Rigidbody? rb)
        {
            return _validator.IsValidTarget(
                Owner,
                rb,
                _heldInstanceId,
                MaxAllowedTargetDistance,
                GrabbableLayer,
                DisallowKinematicTargets);
        }

        private void ForceReleaseCurrentTarget(bool playReleaseSound)
        {
            _holdFxActive = false;

            if (_heldBody == null)
            {
                if (_heldInstanceId != -1)
                {
                    ClearHeldState();
                }
                return;
            }

            ReleaseHeld(playReleaseSound);
        }

        private void ClearHeldState()
        {
            _heldBody = null;
            _heldInstanceId = -1;
            _nextPullAudioTime = 0f;
            _holdFxActive = false;
            _vfx.SetHoldFxActive(false);
        }

        private void BlockReacquireAfterShoot()
        {
            _wantsPull = false;
            _reacquireBlockedUntilTime = Time.time + ReacquireBlockAfterShoot;
        }

        private void SyncHeldPhysicsState()
        {
            EnsurePhysicsCache();
            if (_toolRootRigidbody == null)
            {
                return;
            }

            if (Owner != null)
            {
                ZeroVelocityIfDynamic(_toolRootRigidbody);
                _toolRootRigidbody.isKinematic = true;
                _toolRootRigidbody.useGravity = true;
                _toolRootRigidbody.interpolation = RigidbodyInterpolation.None;
            }
        }

        private void SetHeldCollisionState(bool held)
        {
            EnsurePhysicsCache();
            if (_toolColliders != null)
            {
                for (int i = 0; i < _toolColliders.Length; i++)
                {
                    Collider? c = _toolColliders[i];
                    if (c != null)
                    {
                        c.enabled = !held;
                    }
                }
            }

            if (_toolRootRigidbody != null)
            {
                _toolRootRigidbody.detectCollisions = !held;
                if (held)
                {
                    ZeroVelocityIfDynamic(_toolRootRigidbody);
                    _toolRootRigidbody.isKinematic = true;
                }
            }
        }

        private void EnsurePhysicsCache()
        {
            if (_physicsCacheReady)
            {
                return;
            }

            _toolColliders = GetComponentsInChildren<Collider>(true);
            _toolRootRigidbody = GetComponent<Rigidbody>();
            _physicsCacheReady = true;
        }

        private void MaintainViewModelAnchor()
        {
            if (Owner == null || Owner.ViewModelContainer == null)
            {
                return;
            }

            Transform anchor = Owner.ViewModelContainer;
            if (transform.parent != anchor)
            {
                transform.SetParent(anchor, worldPositionStays: false);
                _loggedReanchor = false;
            }

            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            Rigidbody? rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                ZeroVelocityIfDynamic(rb);
                rb.isKinematic = true;
                rb.position = anchor.position;
                rb.rotation = anchor.rotation;
            }

            if (!_loggedReanchor)
            {
                GravityGunLog.Info("MaintainViewModelAnchor applied. Parent='" + anchor.name + "'.");
                _loggedReanchor = true;
            }
        }

        private void LogModelBindingOnce()
        {
            if (_loggedModelBinding)
            {
                return;
            }

            _loggedModelBinding = true;

            string worldName = WorldModel != null ? WorldModel.name : "null";
            string viewName = ViewModel != null ? ViewModel.name : "null";
            int viewChildren = ViewModel != null ? ViewModel.transform.childCount : -1;
            int viewRenderers = ViewModel != null ? ViewModel.GetComponentsInChildren<MeshRenderer>(true).Length : 0;
            int viewSkinned = ViewModel != null ? ViewModel.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length : 0;
            int viewEnabledRenderers = 0;
            if (ViewModel != null)
            {
                Renderer[] renderers = ViewModel.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i] != null && renderers[i].enabled)
                    {
                        viewEnabledRenderers++;
                    }
                }
            }
            bool viewIsChild = ViewModel != null && ViewModel.transform.IsChildOf(transform);
            bool worldIsChild = WorldModel != null && WorldModel.transform.IsChildOf(transform);
            bool ownerNull = Owner == null;

            GravityGunLog.Info(
                "Tool binding check: toolGo=" + gameObject.name +
                ", ownerNull=" + ownerNull +
                ", world=" + worldName +
                " (childOfTool=" + worldIsChild + ")" +
                ", view=" + viewName +
                " (childOfTool=" + viewIsChild + ")" +
                ", viewChildren=" + viewChildren +
                ", viewMeshRenderers=" + viewRenderers +
                ", viewSkinnedRenderers=" + viewSkinned +
                ", viewEnabledRenderers=" + viewEnabledRenderers);
        }

        private void TryPlayShootBolt(Vector3 hitPoint)
        {
            if (!_vfxInitialized)
            {
                TryInitializeVfx();
            }

            if (_vfxInitialized)
            {
                _vfx.PlayShootBolt(hitPoint);
            }
        }

        private static void ZeroVelocityIfDynamic(Rigidbody rb)
        {
            if (rb.isKinematic)
            {
                return;
            }

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        private void TryInitializeVfx()
        {
            if (Time.time < _nextVfxInitAttemptTime)
            {
                return;
            }

            if (ViewModel == null)
            {
                _nextVfxInitAttemptTime = Time.time + 1f;
                return;
            }

            _vfxInitialized = _vfx.Init(ViewModel.transform);
            if (!_vfxInitialized)
            {
                _nextVfxInitAttemptTime = Time.time + 1f;
            }
        }

        private void PlaySfx_Acquire(Vector3 point)
        {
            _audio.PlayAcquire(_acquireSfx, point);
        }

        private void PlaySfx_PullTick(Vector3 point)
        {
            _audio.TryPlayPullTick(_pullTickSfx, point, PullTickInterval, ref _nextPullAudioTime);
        }

        private void PlaySfx_Release(Vector3 point)
        {
            _audio.PlayRelease(_releaseSfx, point);
        }

        private void PlaySfx_Shoot(Vector3 point)
        {
            _audio.PlayShoot(_shootSfx, point);
        }

        private void PlaySfx_FailedShoot(Vector3 point)
        {
            _audio.PlayFailedShoot(_failedShootSfx, point);
        }
    }
}
