using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Game.Actors;
using Dovus.Game.Boss;
using Dovus.Game.Casting;
using Dovus.Game.Config;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dovus.Game.Hud
{
    public sealed partial class VitalsHud : MonoBehaviour
    {
        void ApplyTuningLayout()
        {
            float topInset = HexagonLayoutScreen.SafeTopInsetPx();
            float leftInset = HexagonLayoutScreen.SafeLeftInsetPx();
            float margin = HexagonLayoutScreen.DpToPixels(_tuning.Hud.VitalsMarginDp);
            float w = HexagonLayoutScreen.DpToPixels(_tuning.Hud.VitalsBarWidthDp);
            float h = HexagonLayoutScreen.DpToPixels(_tuning.Hud.VitalsBarHeightDp);
            float bossW = HexagonLayoutScreen.DpToPixels(_tuning.Hud.VitalsBossBarWidthDp);
            float bossH = HexagonLayoutScreen.DpToPixels(_tuning.Hud.VitalsBossBarHeightDp);
            float spacing = HexagonLayoutScreen.DpToPixels(_tuning.Hud.VitalsBarSpacingDp);
            float pad = HexagonLayoutScreen.DpToPixels(_tuning.Hud.VitalsPanelPaddingDp);
            float headerH = HexagonLayoutScreen.DpToPixels(_tuning.Hud.VitalsHeaderHeightDp);
            Rect safe = HexagonLayoutScreen.SafeRectPx();
            bossW = Mathf.Min(bossW, safe.width - margin * 2f);

            bool sizeChanged =
                !Mathf.Approximately(_tuning.Hud.VitalsBarWidthDp, _appliedWidthDp) ||
                !Mathf.Approximately(_tuning.Hud.VitalsBarHeightDp, _appliedHeightDp) ||
                !Mathf.Approximately(_tuning.Hud.VitalsBossBarHeightDp, _appliedBossHDp) ||
                !Mathf.Approximately(_tuning.Hud.VitalsBossBarWidthDp, _appliedBossWDp) ||
                !Mathf.Approximately(_tuning.Hud.VitalsBarSpacingDp, _appliedSpacingDp) ||
                !Mathf.Approximately(_tuning.Hud.VitalsMarginDp, _appliedMarginDp);

            if (sizeChanged || true)
            {
                _appliedWidthDp = _tuning.Hud.VitalsBarWidthDp;
                _appliedHeightDp = _tuning.Hud.VitalsBarHeightDp;
                _appliedBossHDp = _tuning.Hud.VitalsBossBarHeightDp;
                _appliedBossWDp = _tuning.Hud.VitalsBossBarWidthDp;
                _appliedSpacingDp = _tuning.Hud.VitalsBarSpacingDp;
                _appliedMarginDp = _tuning.Hud.VitalsMarginDp;

                int rows = BarCount;
                float panelH = headerH + rows * h + (rows - 1) * spacing + pad * 2f;
                float panelW = w + pad * 2f;

                _playerRoot.anchoredPosition = new Vector2(leftInset + margin, -(topInset + margin));
                _playerPanel.anchoredPosition = Vector2.zero;
                _playerPanel.sizeDelta = new Vector2(panelW, panelH);

                if (_playerIdentity != null)
                {
                    RectTransform identity = _playerIdentity.rectTransform;
                    identity.anchorMin = identity.anchorMax = new Vector2(0f, 1f);
                    identity.pivot = new Vector2(0f, 1f);
                    identity.anchoredPosition = new Vector2(pad, -pad * 0.35f);
                    identity.sizeDelta = new Vector2(w, headerH);
                }
                float barsTop = pad + headerH;
                _playerBg.anchoredPosition = new Vector2(pad, -barsTop);
                _playerBg.sizeDelta = new Vector2(w, h);
                _manaBg.anchoredPosition = new Vector2(pad, -(barsTop + h + spacing));
                _manaBg.sizeDelta = new Vector2(w, h);
                if (_hasAlly && _allyBg != null)
                {
                    _allyBg.anchoredPosition = new Vector2(pad, -(barsTop + 2f * (h + spacing)));
                    _allyBg.sizeDelta = new Vector2(w, h);
                }

                PlayerStackBottomCanvasY = -(topInset + margin + panelH);

                HudTheme th = _theme;
                float nameH = HexagonLayoutScreen.DpToPixels(th.BossNameDp + VitalsHudDefaults.BossNameDpPadding);
                _bossRoot.anchoredPosition = new Vector2(0f, -(topInset + margin * 0.5f));
                _bossName.rectTransform.anchoredPosition = Vector2.zero;
                _bossName.rectTransform.sizeDelta = new Vector2(bossW, nameH);
                _bossBg.anchoredPosition = new Vector2(-bossW * 0.5f, -nameH);
                _bossBg.sizeDelta = new Vector2(bossW, bossH);
                float poiseGap = HexagonLayoutScreen.DpToPixels(VitalsHudDefaults.PoiseBarGapDp);
                float poiseH = Mathf.Max(VitalsHudDefaults.PoiseBarMinHeightDp, bossH * VitalsHudDefaults.BossPoiseHeightMult);
                if (_poiseBg != null)
                {
                    _poiseBg.anchoredPosition = new Vector2(-bossW * 0.5f, -(nameH + bossH + poiseGap));
                    _poiseBg.sizeDelta = new Vector2(bossW, poiseH);
                }
                // Cast barı poise barının altında; etiketi barın üstünde durur.
                float castLabelH = HexagonLayoutScreen.DpToPixels(th.CastLabelDp + VitalsHudDefaults.CastLabelDpPadding);
                float castH = HexagonLayoutScreen.DpToPixels(th.CastBarHeightDp);
                float castTop = nameH + bossH + poiseGap + poiseH + castLabelH;
                if (_castRoot != null)
                {
                    _castRoot.anchoredPosition = new Vector2(0f, -castTop);
                    _castRoot.sizeDelta = new Vector2(bossW * th.CastBarWidthFrac, castH);
                }
                BossStackBottomCanvasY = -(topInset + margin * 0.5f + castTop + castH);
            }

            if (_appliedBossColor != _tuning.Hud.BossVitalsColor)
            {
                _appliedBossColor = _tuning.Hud.BossVitalsColor;
                _bossFill.color = _appliedBossColor;
            }

            // Oyuncu HP: mevcut soft rose tonu
            Color hpColor = _theme.PlayerHpColor;
            if (_appliedPlayerColor != hpColor)
            {
                _appliedPlayerColor = hpColor;
                _playerFill.color = hpColor;
            }

            Color manaColor = _theme.PlayerManaColor;
            if (_appliedManaColor != manaColor)
            {
                _appliedManaColor = manaColor;
                if (_manaFill != null)
                    _manaFill.color = _appliedManaColor;
            }
        }
    }
}
