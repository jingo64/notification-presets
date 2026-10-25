using System;
using HarmonyLib;
using PavonisInteractive.TerraInvicta;
using PavonisInteractive.TerraInvicta.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace NotificationPresets.UI
{
    // Adds a second row of buttons (Early / Mid / Late game) above the game's own reset buttons
    // on the notification options page
    public static class PresetButtons
    {
        const string RowName = "NotificationPresetsRow";

        static GameObject row;
        static RectTransform scrollView;
        static float shift;

        public static void Build(NotificationsOptionsController controller)
        {
            var panel = controller.transform;
            if (panel.Find(RowName) != null)
                return;
            var buttons = panel.Find("Buttons");
            var template = buttons != null ? buttons.Find("ResetToGameDefaultsButton") : null;
            if (template == null)
            {
                Main.Error("Notification options buttons not found");
                return;
            }

            // The game's row sizes its buttons with a layout group, which didn't lay out our copy;
            // the buttons are placed with anchors instead
            row = Object.Instantiate(buttons.gameObject, panel, false);
            row.name = RowName;
            foreach (var group in row.GetComponents<LayoutGroup>())
                Object.DestroyImmediate(group);
            for (int i = row.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(row.transform.GetChild(i).gameObject);
            var rowRect = (RectTransform)row.transform;
            shift = ((RectTransform)buttons).rect.height;
            rowRect.anchoredPosition += new Vector2(0, shift);

            AddButton(template.gameObject, controller, Preset.Early, "Early game alerts", 0);
            AddButton(template.gameObject, controller, Preset.Mid, "Mid game alerts", 1);
            AddButton(template.gameObject, controller, Preset.Late, "Late game alerts", 2);

            // Make room above the new row
            var scroll = panel.Find("ScrollView Container");
            if (scroll != null)
            {
                scrollView = (RectTransform)scroll;
                scrollView.offsetMin += new Vector2(0, shift);
            }
            SetVisible(Main.enabled);
        }

        public static void SetVisible(bool visible)
        {
            if (row == null)
                return;
            if (row.activeSelf == visible)
                return;
            row.SetActive(visible);
            if (scrollView != null)
                scrollView.offsetMin += new Vector2(0, visible ? shift : -shift);
        }

        // Matches the game's row: 40 padding at the ends, 40 between buttons, 30 high
        const float Padding = 40f;
        const float ButtonHeight = 30f;

        static void AddButton(GameObject template, NotificationsOptionsController controller, Preset preset, string label, int index)
        {
            var clone = Object.Instantiate(template, row.transform, false);
            clone.name = "Preset" + preset;
            clone.SetActive(true);
            var rect = (RectTransform)clone.transform;
            rect.anchorMin = new Vector2(index / 3f, 0f);
            rect.anchorMax = new Vector2((index + 1) / 3f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = new Vector2(index == 0 ? Padding : Padding / 2f, 0f);
            rect.offsetMax = new Vector2(index == 2 ? -Padding : -Padding / 2f, ButtonHeight);
            foreach (var behaviour in clone.GetComponentsInChildren<MonoBehaviour>(true))
            {
                var typeName = behaviour.GetType().Name;
                if (typeName == "TooltipTrigger" || typeName == "RightClickHandler" || typeName == "RectTransformChangeListener")
                    Object.DestroyImmediate(behaviour);
            }
            var text = clone.GetComponentInChildren<TMP_Text>(true);
            if (text != null)
                text.text = label;
            var button = clone.GetComponent<Button>();
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(() => OnClick(controller, preset));
        }

        static void OnClick(NotificationsOptionsController controller, Preset preset)
        {
            var faction = GameControl.control.activePlayer;
            if (!TIGameState.Valid(faction))
                return;
            try
            {
                Presets.Apply(faction, preset);
                AudioManager.PlayOneShot("event:/SFX/UI_SFX/trig_SFX_GenericConfirm");
                controller.InitializeNotificationOptions();
            }
            catch (Exception e)
            {
                Main.Error("Applying preset failed: " + e);
            }
        }
    }

    [HarmonyPatch(typeof(NotificationsOptionsController), "Start")]
    static class NotificationsOptionsController_Start_Patch
    {
        static void Postfix(NotificationsOptionsController __instance)
        {
            try
            {
                PresetButtons.Build(__instance);
            }
            catch (Exception e)
            {
                Main.Error("Building preset buttons failed: " + e);
            }
        }
    }
}
