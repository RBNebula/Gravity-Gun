using GravityGunMod.Diagnostics;
using UnityEngine;

namespace GravityGunMod.Core
{
    internal sealed class GravityGunVfxController
    {
        private const int ArcCount = 3;
        private const int SegmentCount = 12;
        private const int ShootSegmentCount = 16;
        private const float ArcWidth = 0.0035f;
        private const float ShootArcWidth = 0.015f;
        private const float JitterAmplitude = 0.024f;
        private const float ShootJitterAmplitude = 0.05f;
        private const float JitterSpeed = 36f;
        private const float ShootJitterSpeed = 40f;
        private const float ProngOutwardOffset = 0.055f;
        private const float MuzzleForwardOffset = 0.0f;
        private const float ShootStartForwardOffset = 0.01f;
        private const float ShootBoltDuration = 0.09f;
        private const float ArcEmissionIntensity = 2.6f;

        private readonly Transform?[] _prongs = new Transform?[ArcCount];
        private readonly LineRenderer?[] _arcRenderers = new LineRenderer?[ArcCount];
        private readonly Vector3[][] _pointBuffers = new Vector3[ArcCount][];
        private readonly float[] _noiseSeeds = new float[ArcCount] { 7.13f, 19.73f, 37.01f };
        private readonly Vector3[] _shootPoints = new Vector3[ShootSegmentCount];

        private Transform? _viewModelRoot;
        private Transform? _pivot;
        private Transform? _muzzleCore;
        private LineRenderer? _shootRenderer;
        private bool _isInitialized;
        private bool _isActive;
        private bool _isShootActive;
        private float _shootEndTime;
        private Vector3 _shootTargetPoint;

        public bool Init(Transform? viewModelRoot)
        {
            _viewModelRoot = viewModelRoot;
            _isInitialized = false;
            _isActive = false;

            if (_viewModelRoot == null)
            {
                GravityGunLog.Warn("[GravityGunVFX] Init failed: viewModelRoot is null.");
                return false;
            }

            _pivot = FindByAliasRecursive(_viewModelRoot, "pivot");
            if (_pivot == null)
            {
                _pivot = FindByAliasRecursive(_viewModelRoot, "colladavisualscenegroup");
            }

            Transform fxSearchRoot = _pivot ?? _viewModelRoot;

            _muzzleCore = FindByAliasRecursive(fxSearchRoot, "fxmuzzlecore");
            _prongs[0] = FindByAliasRecursive(fxSearchRoot, "fxprong1");
            _prongs[1] = FindByAliasRecursive(fxSearchRoot, "fxprong2");
            _prongs[2] = FindByAliasRecursive(fxSearchRoot, "fxprong3");

            if (_muzzleCore == null)
            {
                _muzzleCore = FindByAliasRecursive(_viewModelRoot, "fxmuzzlecore");
            }
            if (_prongs[0] == null)
            {
                _prongs[0] = FindByAliasRecursive(_viewModelRoot, "fxprong1");
            }
            if (_prongs[1] == null)
            {
                _prongs[1] = FindByAliasRecursive(_viewModelRoot, "fxprong2");
            }
            if (_prongs[2] == null)
            {
                _prongs[2] = FindByAliasRecursive(_viewModelRoot, "fxprong3");
            }

            bool hasPivot = _pivot != null;
            bool hasMuzzle = _muzzleCore != null;
            bool hasProng1 = _prongs[0] != null;
            bool hasProng2 = _prongs[1] != null;
            bool hasProng3 = _prongs[2] != null;

            int rendererCount = _viewModelRoot.GetComponentsInChildren<Renderer>(true).Length;
            GravityGunLog.Info("[GravityGunVFX] Found pivot: " + hasPivot);
            GravityGunLog.Info("[GravityGunVFX] Found Muzzle: " + hasMuzzle);
            GravityGunLog.Info("[GravityGunVFX] Found Prongs: " + hasProng1 + " " + hasProng2 + " " + hasProng3);
            GravityGunLog.Info("[GravityGunVFX] Renderer count under viewmodel: " + rendererCount);
            GravityGunLog.Info("[GravityGunVFX] pivot path: " + GetTransformPathOrNull(_pivot));
            GravityGunLog.Info("[GravityGunVFX] muzzle path: " + GetTransformPathOrNull(_muzzleCore));
            GravityGunLog.Info("[GravityGunVFX] prong1 path: " + GetTransformPathOrNull(_prongs[0]));
            GravityGunLog.Info("[GravityGunVFX] prong2 path: " + GetTransformPathOrNull(_prongs[1]));
            GravityGunLog.Info("[GravityGunVFX] prong3 path: " + GetTransformPathOrNull(_prongs[2]));

            if (!hasMuzzle || !hasProng1 || !hasProng2 || !hasProng3)
            {
                GravityGunLog.Warn("[GravityGunVFX] Missing required FX anchors. Expected FX_Muzzle_Core, FX_Pr0ng_1/2/3 (pivot optional).");
                return false;
            }

            EnsureArcRenderers();
            EnsureShootRenderer();
            SetHoldFxActive(false);
            ClearTransientFx();
            _isInitialized = true;
            return true;
        }

        public void SetHoldFxActive(bool active)
        {
            _isActive = active;
            for (int i = 0; i < ArcCount; i++)
            {
                LineRenderer? lr = _arcRenderers[i];
                if (lr != null && lr.enabled != _isActive)
                {
                    lr.enabled = _isActive;
                }
            }
        }

        public void UpdateHoldArcs()
        {
            if (!_isInitialized || !_isActive || _muzzleCore == null)
            {
                return;
            }

            Vector3 corePosition = _muzzleCore.position;
            Vector3 end = corePosition + (_muzzleCore.forward * MuzzleForwardOffset);
            float t = Time.time * JitterSpeed;

            for (int i = 0; i < ArcCount; i++)
            {
                Transform? startAnchor = _prongs[i];
                LineRenderer? lr = _arcRenderers[i];
                Vector3[]? points = _pointBuffers[i];
                if (startAnchor == null || lr == null || points == null)
                {
                    continue;
                }

                Vector3 start = GetOffsetProngStart(startAnchor.position, corePosition);
                BuildArcPoints(start, end, points, _noiseSeeds[i], t, JitterAmplitude);
                lr.SetPositions(points);
            }
        }

        public void PlayShootBolt(Vector3 hitPoint)
        {
            if (!_isInitialized || _muzzleCore == null)
            {
                return;
            }

            EnsureShootRenderer();
            if (_shootRenderer == null)
            {
                return;
            }

            _shootTargetPoint = hitPoint;
            _isShootActive = true;
            _shootEndTime = Time.time + ShootBoltDuration;
            _shootRenderer.enabled = true;
            UpdateShootBolt();
        }

        public void UpdateTransientFx()
        {
            if (!_isInitialized)
            {
                if (_shootRenderer != null && _shootRenderer.enabled)
                {
                    _shootRenderer.enabled = false;
                }

                return;
            }

            if (!_isShootActive && _shootRenderer != null && _shootRenderer.enabled)
            {
                _shootRenderer.enabled = false;
            }

            UpdateShootBolt();
        }

        public void ClearTransientFx()
        {
            _isShootActive = false;
            _shootEndTime = 0f;

            if (_shootRenderer != null)
            {
                _shootRenderer.enabled = false;
            }
        }

        private static Vector3 GetOffsetProngStart(Vector3 prongPosition, Vector3 corePosition)
        {
            Vector3 radial = prongPosition - corePosition;
            if (radial.sqrMagnitude < 0.000001f)
            {
                return prongPosition;
            }

            return prongPosition + radial.normalized * ProngOutwardOffset;
        }

        private void EnsureArcRenderers()
        {
            Material arcMaterial = CreateArcMaterial();
            int viewModelLayer = _viewModelRoot != null ? _viewModelRoot.gameObject.layer : 0;

            for (int i = 0; i < ArcCount; i++)
            {
                LineRenderer? existingRenderer = _arcRenderers[i];
                if (existingRenderer == null)
                {
                    GameObject go = new GameObject("GravityGun_InternalArc_" + (i + 1));
                    go.transform.SetParent(_viewModelRoot, false);
                    go.layer = viewModelLayer;
                    LineRenderer lr = go.AddComponent<LineRenderer>();
                    ConfigureArcRenderer(lr, arcMaterial);
                    _arcRenderers[i] = lr;
                }
                else if (existingRenderer.gameObject.layer != viewModelLayer)
                {
                    existingRenderer.gameObject.layer = viewModelLayer;
                }

                if (_pointBuffers[i] == null || _pointBuffers[i].Length != SegmentCount)
                {
                    _pointBuffers[i] = new Vector3[SegmentCount];
                }
            }
        }

        private void EnsureShootRenderer()
        {
            if (_viewModelRoot == null)
            {
                return;
            }

            int viewModelLayer = _viewModelRoot.gameObject.layer;
            if (_shootRenderer == null)
            {
                GameObject go = new GameObject("GravityGun_ShootBolt");
                go.transform.SetParent(_viewModelRoot, false);
                go.layer = viewModelLayer;
                _shootRenderer = go.AddComponent<LineRenderer>();
                ConfigureShootRenderer(_shootRenderer, CreateArcMaterial());
            }
            else if (_shootRenderer.gameObject.layer != viewModelLayer)
            {
                _shootRenderer.gameObject.layer = viewModelLayer;
            }
        }

        private void UpdateShootBolt()
        {
            if (!_isShootActive)
            {
                return;
            }

            if (_shootRenderer == null || _muzzleCore == null)
            {
                ClearTransientFx();
                return;
            }

            if (Time.time >= _shootEndTime)
            {
                ClearTransientFx();
                return;
            }

            Vector3 start = _muzzleCore.position + (_muzzleCore.forward * ShootStartForwardOffset);
            float t = Time.time * ShootJitterSpeed;
            BuildArcPoints(start, _shootTargetPoint, _shootPoints, 53.29f, t, ShootJitterAmplitude);
            _shootRenderer.SetPositions(_shootPoints);
        }

        private static Material CreateArcMaterial()
        {
            Shader? standard = Shader.Find("Standard");
            if (standard != null)
            {
                Material standardMat = new Material(standard)
                {
                    name = "GravityGunArcMaterial",
                    hideFlags = HideFlags.HideAndDontSave
                };

                standardMat.SetOverrideTag("RenderType", "Transparent");
                standardMat.SetFloat("_Mode", 2f);
                standardMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                standardMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                standardMat.SetInt("_ZWrite", 0);
                standardMat.DisableKeyword("_ALPHATEST_ON");
                standardMat.EnableKeyword("_ALPHABLEND_ON");
                standardMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                standardMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

                if (standardMat.HasProperty("_Color"))
                {
                    standardMat.SetColor("_Color", new Color(1f, 0.62f, 0.2f, 0.9f));
                }

                if (standardMat.HasProperty("_EmissionColor"))
                {
                    standardMat.SetColor("_EmissionColor", new Color(1f, 0.68f, 0.24f, 1f) * ArcEmissionIntensity);
                    standardMat.globalIlluminationFlags &= ~MaterialGlobalIlluminationFlags.EmissiveIsBlack;
                    standardMat.globalIlluminationFlags |= MaterialGlobalIlluminationFlags.RealtimeEmissive;
                    standardMat.EnableKeyword("_EMISSION");
                }

                return standardMat;
            }

            Shader? shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
            Material material = new Material(shader)
            {
                name = "GravityGunArcMaterial",
                hideFlags = HideFlags.HideAndDontSave
            };
            return material;
        }

        private static void ConfigureArcRenderer(LineRenderer lr, Material material)
        {
            lr.sharedMaterial = material;
            lr.useWorldSpace = true;
            lr.positionCount = SegmentCount;
            lr.widthMultiplier = ArcWidth;
            lr.textureMode = LineTextureMode.Stretch;
            lr.numCornerVertices = 0;
            lr.numCapVertices = 0;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            lr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            lr.startColor = new Color(0.9f, 0.42f, 0.1f, 0.97f);
            lr.endColor = new Color(1f, 0.62f, 0.2f, 0.9f);
            lr.enabled = false;
        }

        private static void ConfigureShootRenderer(LineRenderer lr, Material material)
        {
            lr.sharedMaterial = material;
            lr.useWorldSpace = true;
            lr.positionCount = ShootSegmentCount;
            lr.widthMultiplier = ShootArcWidth;
            lr.textureMode = LineTextureMode.Stretch;
            lr.numCornerVertices = 0;
            lr.numCapVertices = 0;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            lr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            lr.startColor = new Color(1f, 0.85f, 0.35f, 0.97f);
            lr.endColor = new Color(1f, 0.5f, 0.1f, 0.92f);
            lr.enabled = false;
        }

        private static void BuildArcPoints(Vector3 start, Vector3 end, Vector3[] points, float seed, float time, float jitterAmplitude)
        {
            Vector3 direction = end - start;
            float distance = direction.magnitude;
            if (distance < 0.0001f)
            {
                for (int i = 0; i < points.Length; i++)
                {
                    points[i] = start;
                }
                return;
            }

            Vector3 forward = direction / distance;
            Vector3 perpendicularA = Vector3.Cross(forward, Vector3.up);
            if (perpendicularA.sqrMagnitude < 0.0001f)
            {
                perpendicularA = Vector3.Cross(forward, Vector3.right);
            }
            perpendicularA.Normalize();
            Vector3 perpendicularB = Vector3.Cross(forward, perpendicularA).normalized;

            int last = points.Length - 1;
            for (int i = 0; i <= last; i++)
            {
                float lerp = last > 0 ? i / (float)last : 0f;
                Vector3 p = Vector3.Lerp(start, end, lerp);

                if (i > 0 && i < last)
                {
                    float envelope = Mathf.Sin(lerp * Mathf.PI);
                    float n1 = Mathf.PerlinNoise(seed + lerp * 3.17f, time + seed) - 0.5f;
                    float n2 = Mathf.PerlinNoise(seed + 11.71f + lerp * 4.93f, time * 1.13f + seed * 0.73f) - 0.5f;
                    p += (perpendicularA * n1 + perpendicularB * n2) * (jitterAmplitude * envelope * 2f);
                }

                points[i] = p;
            }
        }

        private static Transform? FindByAliasRecursive(Transform? root, string normalizedAlias)
        {
            if (root == null || string.IsNullOrEmpty(normalizedAlias))
            {
                return null;
            }

            string normalizedName = NormalizeName(root.name);
            if (normalizedName == normalizedAlias ||
                normalizedName.StartsWith(normalizedAlias, System.StringComparison.Ordinal))
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform? found = FindByAliasRecursive(root.GetChild(i), normalizedAlias);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static string NormalizeName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return string.Empty;
            }

            char[] buffer = new char[name.Length];
            int count = 0;
            for (int i = 0; i < name.Length; i++)
            {
                char c = char.ToLowerInvariant(name[i]);
                if (c == '_' || c == '-' || c == ' ' || c == '.')
                {
                    continue;
                }

                if (c == '0')
                {
                    c = 'o';
                }

                buffer[count++] = c;
            }

            return new string(buffer, 0, count);
        }

        private static string GetTransformPathOrNull(Transform? t)
        {
            if (t == null)
            {
                return "null";
            }

            string path = t.name;
            Transform? cursor = t.parent;
            while (cursor != null)
            {
                path = cursor.name + "/" + path;
                cursor = cursor.parent;
            }

            return path;
        }
    }
}
