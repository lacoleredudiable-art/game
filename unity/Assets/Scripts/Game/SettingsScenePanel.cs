using System;
using System.Globalization;
using System.IO;
using Dovus.Core.Equipment;
using Dovus.Core.Grammar;
using Dovus.Core.Tuning;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dovus.Game
{
    /// <summary>
    /// Ayar sahnesi: silah/skill seçimi, yavaş çekim, tutuş + vuruş karesi slider'ları, kaydet.
    /// </summary>
    public sealed class SettingsScenePanel : MonoBehaviour
    {
        public static bool BlocksGameplayInput { get; private set; }

        ManifestationDirector _director;
        HexagonInput _input;
        GameClock _clock;
        WeaponFeelApplier _applier;
        SkillMotor _skills;
        WeaponVisualRegistry _registry;
        Transform _gripRoot;
        Transform _playerRoot;
        RectTransform _content;
        Text _status;
        Text _weaponLabel;
        float _statusUntil;
        string _selectedWeaponKey = "kilic";
        bool _slowMo;
        float _touchUiScale = 1f;
        float _sliderRowHeight = 52f;
        GameObject _uiRoot;

        readonly (int verb, int adj, string label)[] _skillPresets =
        {
            (1, 1, "1-1"),
            (1, 2, "1-2"),
            (2, 1, "2-1"),
            (3, 1, "3-1"),
        };

        public Transform GripRoot => _gripRoot;

        public void SetCaptureUiVisible(bool visible)
        {
            if (_uiRoot != null)
                _uiRoot.SetActive(visible);
        }

        public void Configure(
            ManifestationDirector director,
            HexagonInput input,
            GameClock clock,
            WeaponFeelApplier applier,
            SkillMotor skills,
            FollowCamera follow = null,
            PrototypeTuning tuning = null,
            Transform gripSubject = null,
            Transform playerRoot = null)
        {
            _director = director;
            _input = input;
            _clock = clock;
            _applier = applier;
            _skills = skills;
            _gripRoot = gripSubject;
            _playerRoot = playerRoot;
            _registry = Resources.Load<WeaponVisualRegistry>("Animation/WeaponVisualRegistry");
            WeaponFeelStore.EnsureLoaded();
            BuildUi();
            SelectWeapon(_selectedWeaponKey);
            Debug.Log("[SettingsScene] Ayar paneli hazır.");
        }

        void BuildUi()
        {
            float dpi = Screen.dpi > 10f ? Screen.dpi : 160f;
            _touchUiScale = Mathf.Clamp(dpi / 160f, 1f, 2.6f);
            _sliderRowHeight = Mathf.Max(48f, 52f * _touchUiScale);

            var canvasGo = new GameObject("SettingsSceneCanvas");
            _uiRoot = canvasGo;
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 900;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f / _touchUiScale, 900f / _touchUiScale);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var card = new GameObject("Panel");
            card.transform.SetParent(canvasGo.transform, false);
            var cardRect = card.AddComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.01f, 0.08f);
            cardRect.anchorMax = new Vector2(0.34f, 0.92f);
            cardRect.offsetMin = Vector2.zero;
            cardRect.offsetMax = Vector2.zero;
            card.AddComponent<Image>().color = new Color(0.06f, 0.08f, 0.11f, 0.94f);

            var title = CreateLabel(card.transform, "AYAR SAHNESİ", 20);
            title.fontStyle = FontStyle.Bold;
            title.alignment = TextAnchor.UpperLeft;
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.04f, 0.92f);
            titleRect.anchorMax = new Vector2(0.96f, 0.99f);

            _weaponLabel = CreateLabel(card.transform, string.Empty, 15);
            var weaponRect = _weaponLabel.GetComponent<RectTransform>();
            weaponRect.anchorMin = new Vector2(0.04f, 0.86f);
            weaponRect.anchorMax = new Vector2(0.96f, 0.91f);

            BuildWeaponRow(card.transform, 0.78f, 0.85f);
            BuildSkillRow(card.transform, 0.70f, 0.77f);
            BuildActionRow(card.transform, 0.63f, 0.69f);

            var scrollGo = new GameObject("Scroll");
            scrollGo.transform.SetParent(card.transform, false);
            var scrollRect = scrollGo.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0.03f, 0.12f);
            scrollRect.anchorMax = new Vector2(0.97f, 0.62f);
            scrollRect.offsetMin = Vector2.zero;
            scrollRect.offsetMax = Vector2.zero;

            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollGo.transform, false);
            var viewportRect = viewport.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            viewport.AddComponent<RectMask2D>();

            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(viewport.transform, false);
            _content = contentGo.AddComponent<RectTransform>();
            _content.anchorMin = new Vector2(0f, 1f);
            _content.anchorMax = new Vector2(1f, 1f);
            _content.pivot = new Vector2(0.5f, 1f);
            var layout = contentGo.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 4f;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            contentGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.viewport = viewportRect;
            scroll.content = _content;
            scroll.horizontal = false;
            scroll.vertical = true;

            var saveRow = new GameObject("SaveRow");
            saveRow.transform.SetParent(card.transform, false);
            var saveRowRect = saveRow.AddComponent<RectTransform>();
            saveRowRect.anchorMin = new Vector2(0.04f, 0.03f);
            saveRowRect.anchorMax = new Vector2(0.96f, 0.10f);
            var saveHBox = saveRow.AddComponent<HorizontalLayoutGroup>();
            saveHBox.spacing = 6f;
            saveHBox.childForceExpandWidth = true;
            var resetGrip = CreateButton(saveRow.transform, "TUTUŞ SIFIR");
            resetGrip.onClick.AddListener(ResetGripTuning);
#if UNITY_EDITOR
            var bakeRegistry = CreateButton(saveRow.transform, "REGISTRY'YE YAZ");
            bakeRegistry.onClick.AddListener(BakeGripToRegistry);
#endif
            var copyJson = CreateButton(saveRow.transform, "KOPYALA");
            copyJson.onClick.AddListener(CopyGripJsonToClipboard);
            var save = CreateButton(saveRow.transform, "KAYDET");
            save.onClick.AddListener(SaveToDisk);

            _status = CreateLabel(card.transform, string.Empty, 13);
            var statusRect = _status.GetComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0.04f, 0.105f);
            statusRect.anchorMax = new Vector2(0.96f, 0.125f);
            _status.color = new Color(0.55f, 0.95f, 0.75f);
        }

        void BuildWeaponRow(Transform parent, float yMin, float yMax)
        {
            var row = new GameObject("Weapons");
            row.transform.SetParent(parent, false);
            var rect = row.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.03f, yMin);
            rect.anchorMax = new Vector2(0.97f, yMax);
            var h = row.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 4f;
            h.childForceExpandWidth = true;
            h.childControlHeight = true;
            foreach (string key in WeaponFeelStore.WeaponKeys)
            {
                string captured = key;
                var btn = CreateButton(row.transform, key.ToUpperInvariant());
                btn.onClick.AddListener(() => SelectWeapon(captured));
            }
        }

        void BuildSkillRow(Transform parent, float yMin, float yMax)
        {
            var row = new GameObject("Skills");
            row.transform.SetParent(parent, false);
            var rect = row.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.03f, yMin);
            rect.anchorMax = new Vector2(0.97f, yMax);
            var h = row.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 4f;
            h.childForceExpandWidth = true;
            foreach (var preset in _skillPresets)
            {
                var p = preset;
                var btn = CreateButton(row.transform, p.label);
                btn.onClick.AddListener(() => CastSkill(p.verb, p.adj));
            }
        }

        void BuildActionRow(Transform parent, float yMin, float yMax)
        {
            var row = new GameObject("Actions");
            row.transform.SetParent(parent, false);
            var rect = row.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.03f, yMin);
            rect.anchorMax = new Vector2(0.97f, yMax);
            var h = row.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 6f;
            h.childForceExpandWidth = true;
            var slow = CreateButton(row.transform, "YAVAŞ ×0.25");
            slow.onClick.AddListener(ToggleSlowMo);
            var strike = CreateButton(row.transform, "VURUŞ");
            strike.onClick.AddListener(TriggerBasicStrike);
            var back = CreateButton(row.transform, "OYUNA DÖN");
            back.onClick.AddListener(SceneFlow.LoadPrototype);
        }

        /// <summary>Play doğrulama / panel butonları.</summary>
        public void SelectWeapon(string key)
        {
            _selectedWeaponKey = key;
            EquipmentItem w = FindWeapon(key);
            if (w != null && _director != null)
            {
                EquipmentItem second = null;
                foreach (EquipmentItem other in _director.AvailableWeapons)
                {
                    if (other != null && other.Id != w.Id)
                    {
                        second = other;
                        break;
                    }
                }

                _director.SetWeaponLoadout(w, second);
            }

            _applier?.RefreshFromStore(key);
            RebuildGripSliders();
            if (_weaponLabel != null)
                _weaponLabel.text = "Silah: " + (w?.Name ?? key);
            LogGripProp(key);
        }

        EquipmentItem FindWeapon(string key)
        {
            if (_director == null)
                return null;
            foreach (EquipmentItem w in _director.AvailableWeapons)
            {
                if (string.Equals(w.AnimationsKey, key, StringComparison.OrdinalIgnoreCase))
                    return w;
            }

            return null;
        }

        void RebuildGripSliders()
        {
            for (int i = _content.childCount - 1; i >= 0; i--)
                Destroy(_content.GetChild(i).gameObject);

            WeaponVisualRegistry.PropEntry grip = FindPropEntry();
            if (grip == null)
            {
                AddHeader("Registry kaydı yok");
                return;
            }

            bool primaryRight = WeaponGripHands.PrimaryIsRight(_selectedWeaponKey);
            bool showRight = WeaponGripHands.UsesRightProp(grip);
            bool showLeft = WeaponGripHands.UsesLeftProp(grip);

            if (primaryRight && showRight)
            {
                AddHeader("TUTUŞ — SAĞ EL (aktif)");
                AddGripSliders(
                    () => grip.RightLocalPosition,
                    v => grip.RightLocalPosition = v,
                    () => grip.RightLocalEulerAngles,
                    v => grip.RightLocalEulerAngles = v,
                    () => grip.RightLocalScale,
                    v => grip.RightLocalScale = v);
            }
            else if (!primaryRight && showLeft)
            {
                AddHeader("TUTUŞ — SOL EL (aktif)");
                AddGripSliders(
                    () => grip.LeftLocalPosition,
                    v => grip.LeftLocalPosition = v,
                    () => grip.LeftLocalEulerAngles,
                    v => grip.LeftLocalEulerAngles = v,
                    () => grip.LeftLocalScale,
                    v => grip.LeftLocalScale = v);
            }

            if (primaryRight && showLeft)
            {
                AddHeader("TUTUŞ — SOL EL (registry)");
                AddGripSliders(
                    () => grip.LeftLocalPosition,
                    v => grip.LeftLocalPosition = v,
                    () => grip.LeftLocalEulerAngles,
                    v => grip.LeftLocalEulerAngles = v,
                    () => grip.LeftLocalScale,
                    v => grip.LeftLocalScale = v);
            }
            else if (!primaryRight && showRight)
            {
                AddHeader("TUTUŞ — SAĞ EL (kullanılmıyor)");
            }
            else if (!showRight && !showLeft)
            {
                AddHeader("TUTUŞ — prop yok");
            }

            WeaponFeelStore.WeaponEntry entry = WeaponFeelStore.Get(_selectedWeaponKey);
            AddHeader("VURUŞ KARESİ");
            AddFloat(
                "Bang (sn)",
                0.05f,
                0.55f,
                () => entry.hitBangSec,
                v =>
                {
                    entry.hitBangSec = v;
                    _applier?.RefreshFromStore(_selectedWeaponKey);
                },
                "0.000");
        }

        WeaponVisualRegistry.PropEntry FindPropEntry() =>
            _registry != null ? _registry.FindProps(_selectedWeaponKey) : null;

        void AddGripSliders(
            Func<Vector3> getPos,
            Action<Vector3> setPos,
            Func<Vector3> getEuler,
            Action<Vector3> setEuler,
            Func<Vector3> getScale,
            Action<Vector3> setScale)
        {
            AddFloat("Konum X", -0.3f, 0.3f, () => getPos().x, v =>
            {
                Vector3 p = getPos();
                setPos(new Vector3(v, p.y, p.z));
                TouchGrip();
            }, "0.000");
            AddFloat("Konum Y", -0.3f, 0.3f, () => getPos().y, v =>
            {
                Vector3 p = getPos();
                setPos(new Vector3(p.x, v, p.z));
                TouchGrip();
            }, "0.000");
            AddFloat("Konum Z", -0.3f, 0.3f, () => getPos().z, v =>
            {
                Vector3 p = getPos();
                setPos(new Vector3(p.x, p.y, v));
                TouchGrip();
            }, "0.000");
            AddFloat("Açı X", -180f, 180f, () => getEuler().x, v =>
            {
                Vector3 e = getEuler();
                setEuler(new Vector3(v, e.y, e.z));
                TouchGrip();
            }, "0.0");
            AddFloat("Açı Y", -180f, 180f, () => getEuler().y, v =>
            {
                Vector3 e = getEuler();
                setEuler(new Vector3(e.x, v, e.z));
                TouchGrip();
            }, "0.0");
            AddFloat("Açı Z", -180f, 180f, () => getEuler().z, v =>
            {
                Vector3 e = getEuler();
                setEuler(new Vector3(e.x, e.y, v));
                TouchGrip();
            }, "0.0");
            AddFloat("Ölçek X", 0.2f, 3f, () => getScale().x, v =>
            {
                Vector3 s = getScale();
                setScale(new Vector3(v, s.y, s.z));
                TouchGrip();
            }, "0.00");
            AddFloat("Ölçek Y", 0.2f, 3f, () => getScale().y, v =>
            {
                Vector3 s = getScale();
                setScale(new Vector3(s.x, v, s.z));
                TouchGrip();
            }, "0.00");
            AddFloat("Ölçek Z", 0.2f, 3f, () => getScale().z, v =>
            {
                Vector3 s = getScale();
                setScale(new Vector3(s.x, s.y, v));
                TouchGrip();
            }, "0.00");
        }

        void TouchGrip()
        {
            _applier?.RefreshFromStore(_selectedWeaponKey);
        }

        void ToggleSlowMo()
        {
            _slowMo = !_slowMo;
            if (_clock != null)
                _clock.SimulationScale = _slowMo ? 0.25f : 1f;
            ShowStatus(_slowMo ? "Yavaş çekim açık (×0.25)" : "Normal hız");
        }

        void CastSkill(int verb, int adj)
        {
            bool ok = _input != null && _input.TryDebugCastSkill(verb, adj);
            ShowStatus(ok ? $"Skill {verb}-{adj} gönderildi" : "Skill reddedildi (build?)");
        }

        void TriggerBasicStrike()
        {
            bool ok = _input != null && _input.TryDebugBasicStrike();
            if (ok)
                StartCoroutine(LogBasicStrikeAnimNextFrame());
            else
                ShowStatus("Düz vuruş reddedildi");
        }

        System.Collections.IEnumerator LogBasicStrikeAnimNextFrame()
        {
            yield return null;
            string state = !string.IsNullOrEmpty(_director?.LastAnimationState)
                ? _director.LastAnimationState
                : DescribeAnimatorState(_playerRoot);
            Debug.Log($"[SettingsScene] basicStrike ok=true animState={state}");
            ShowStatus($"Düz vuruş ({state})");
        }

        void LogGripProp(string weaponKey)
        {
            string right = FindHandPropName(_gripRoot, HumanBodyBones.RightHand);
            string left = FindHandPropName(_gripRoot, HumanBodyBones.LeftHand);
            Debug.Log($"[SettingsScene] weapon={weaponKey} dummyRightProp={right ?? "(yok)"} dummyLeftProp={left ?? "(yok)"}");
        }

        static string FindHandPropName(Transform root, HumanBodyBones bone)
        {
            if (root == null)
                return null;
            var anim = root.GetComponentInChildren<Animator>();
            if (anim == null)
                return null;
            Transform hand = anim.GetBoneTransform(bone);
            if (hand == null)
                return null;
            for (int i = 0; i < hand.childCount; i++)
            {
                Transform child = hand.GetChild(i);
                if (WeaponHandProps.IsWeaponPropRootName(child.name))
                    return child.name;
            }

            return null;
        }

        static string DescribeAnimatorState(Transform root)
        {
            if (root == null)
                return "?";
            var visual = root.GetComponent<ActorVisual>();
            Animator anim = visual != null ? visual.Animator : root.GetComponentInChildren<Animator>();
            if (anim == null)
                return "no-anim";
            AnimatorClipInfo[] clips = anim.GetCurrentAnimatorClipInfo(0);
            if (clips != null && clips.Length > 0 && clips[0].clip != null)
                return clips[0].clip.name;
            return anim.GetCurrentAnimatorStateInfo(0).shortNameHash.ToString();
        }

        void CopyGripJsonToClipboard()
        {
            if (_registry == null)
            {
                ShowStatus("Registry yüklenemedi");
                return;
            }

            string json = WeaponGripClipboardJson.SerializeRegistry(_registry);
            GUIUtility.systemCopyBuffer = json;
            try
            {
                File.WriteAllText(WeaponGripClipboardJson.ExportFilePath, json);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SettingsScene] export dosyası yazılamadı: " + e.Message);
            }

            ShowStatus("kopyalandı");
            Debug.Log($"[SettingsScene] grip JSON kopyalandı ({json.Length} char) → {WeaponGripClipboardJson.ExportFilePath}");
        }

        void SaveToDisk()
        {
            WeaponFeelStore.Save();
            ShowStatus("Kaydedildi: " + WeaponFeelStore.FilePath);
        }

        void ResetGripTuning()
        {
            WeaponVisualRegistry.PropEntry grip = FindPropEntry();
            if (grip != null)
            {
                grip.RightLocalPosition = Vector3.zero;
                grip.RightLocalEulerAngles = Vector3.zero;
                grip.RightLocalScale = Vector3.one;
                grip.LeftLocalPosition = Vector3.zero;
                grip.LeftLocalEulerAngles = Vector3.zero;
                grip.LeftLocalScale = Vector3.one;
            }

            _applier?.RefreshFromStore(_selectedWeaponKey);
            RebuildGripSliders();
            ShowStatus("Tutuş sıfırlandı (pos/rot 0, scale 1)");
        }

#if UNITY_EDITOR
        const string RegistryAssetPath = "Assets/Resources/Animation/WeaponVisualRegistry.asset";

        void BakeGripToRegistry()
        {
            if (_registry == null)
            {
                ShowStatus("Registry yüklenemedi");
                return;
            }

            WeaponVisualRegistry asset = AssetDatabase.LoadAssetAtPath<WeaponVisualRegistry>(RegistryAssetPath);
            if (asset == null)
            {
                ShowStatus("Registry asset bulunamadı");
                return;
            }

            WeaponVisualRegistry.PropEntry live = FindPropEntry();
            WeaponVisualRegistry.PropEntry disk = asset.FindProps(_selectedWeaponKey);
            if (live == null || disk == null)
            {
                ShowStatus("Silah kaydı yok");
                return;
            }

            CopyGrip(live, disk);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            _applier?.RefreshFromStore(_selectedWeaponKey);
            ShowStatus($"Registry'ye yazıldı: {_selectedWeaponKey}");
        }

        static void CopyGrip(WeaponVisualRegistry.PropEntry from, WeaponVisualRegistry.PropEntry to)
        {
            to.RightLocalPosition = from.RightLocalPosition;
            to.RightLocalEulerAngles = from.RightLocalEulerAngles;
            to.RightLocalScale = from.RightLocalScale;
            to.LeftLocalPosition = from.LeftLocalPosition;
            to.LeftLocalEulerAngles = from.LeftLocalEulerAngles;
            to.LeftLocalScale = from.LeftLocalScale;
        }
#endif

        void ShowStatus(string text)
        {
            if (_status == null)
                return;
            _status.text = text;
            _statusUntil = Time.unscaledTime + 2.5f;
        }

        void Update()
        {
            BlocksGameplayInput = false;
            if (_status != null && _status.text.Length > 0 && Time.unscaledTime > _statusUntil)
                _status.text = string.Empty;
        }

        void AddHeader(string text)
        {
            var go = new GameObject("Header");
            go.transform.SetParent(_content, false);
            go.AddComponent<LayoutElement>().preferredHeight = Mathf.Max(32f, 32f * _touchUiScale);
            var label = CreateLabel(go.transform, text, Mathf.RoundToInt(16f * _touchUiScale));
            label.fontStyle = FontStyle.Bold;
            label.color = new Color(0.45f, 0.92f, 1f);
        }

        void AddFloat(string label, float min, float max, Func<float> get, Action<float> set, string format)
        {
            var row = new GameObject("Row");
            row.transform.SetParent(_content, false);
            row.AddComponent<LayoutElement>().preferredHeight = _sliderRowHeight;
            var text = CreateLabel(row.transform, label, Mathf.RoundToInt(14f * _touchUiScale));
            var textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 0.5f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.offsetMin = new Vector2(4f, 0f);

            var sliderGo = new GameObject("Slider");
            sliderGo.transform.SetParent(row.transform, false);
            var sliderRect = sliderGo.AddComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0f, 0.05f);
            sliderRect.anchorMax = new Vector2(1f, 0.45f);
            sliderRect.offsetMin = new Vector2(4f, 0f);
            sliderRect.offsetMax = new Vector2(-4f, 0f);
            var slider = BuildSlider(sliderGo.transform);
            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = false;
            slider.SetValueWithoutNotify(Mathf.Clamp(get(), min, max));
            slider.onValueChanged.AddListener(v =>
            {
                set(v);
                text.text = $"{label}: {v.ToString(format, CultureInfo.InvariantCulture)}";
            });
            text.text = $"{label}: {get().ToString(format, CultureInfo.InvariantCulture)}";
        }

        static Slider BuildSlider(Transform parent)
        {
            var slider = parent.gameObject.AddComponent<Slider>();
            var bg = new GameObject("Bg");
            bg.transform.SetParent(parent, false);
            var bgRect = bg.AddComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0f, 0.25f);
            bgRect.anchorMax = new Vector2(1f, 0.75f);
            bg.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f);
            var fillArea = new GameObject("Fill");
            fillArea.transform.SetParent(parent, false);
            var fillRect = fillArea.AddComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0.25f);
            fillRect.anchorMax = new Vector2(1f, 0.75f);
            fillRect.offsetMin = new Vector2(8f, 0f);
            fillRect.offsetMax = new Vector2(-8f, 0f);
            var fill = new GameObject("FillInner");
            fill.transform.SetParent(fillArea.transform, false);
            var fillInner = fill.AddComponent<RectTransform>();
            fillInner.anchorMin = Vector2.zero;
            fillInner.anchorMax = new Vector2(0f, 1f);
            fillInner.sizeDelta = new Vector2(8f, 0f);
            fill.AddComponent<Image>().color = new Color(0.4f, 0.9f, 1f, 0.85f);
            slider.fillRect = fillInner;
            return slider;
        }

        Text CreateLabel(Transform parent, string text, int size)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var t = go.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.text = text;
            t.fontSize = size;
            t.color = Color.white;
            t.raycastTarget = false;
            return t;
        }

        Button CreateButton(Transform parent, string text)
        {
            var go = new GameObject("Btn_" + text);
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().preferredHeight = Mathf.Max(48f, 36f * _touchUiScale);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.35f, 0.9f, 1f, 0.18f);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var label = CreateLabel(go.transform, text, Mathf.RoundToInt(13f * _touchUiScale));
            label.alignment = TextAnchor.MiddleCenter;
            label.fontStyle = FontStyle.Bold;
            return btn;
        }
    }
}
