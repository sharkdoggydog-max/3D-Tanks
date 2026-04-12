using Tanks.Core;
using UnityEngine;

namespace Tanks.UI
{
    public class GameHud : MonoBehaviour
    {
        private GUIStyle panelStyle;
        private GUIStyle textStyle;
        private GUIStyle titleStyle;

        private void OnGUI()
        {
            EnsureStyles();

            GUILayout.BeginArea(new Rect(16f, 16f, 320f, 180f), panelStyle);

            if (GameManager.Instance == null)
            {
                GUILayout.Label("Booting prototype...", titleStyle);
                GUILayout.EndArea();
                return;
            }

            GUILayout.Label($"State: {GameManager.Instance.CurrentState}", titleStyle);

            if (GameManager.Instance.PlayerHealth != null)
            {
                GUILayout.Label(
                    $"Player HP: {GameManager.Instance.PlayerHealth.CurrentHealth:0}/{GameManager.Instance.PlayerHealth.MaxHealth:0}",
                    textStyle);
            }

            GUILayout.Label($"Enemies Left: {GameManager.Instance.EnemyCount}", textStyle);
            GUILayout.Space(10f);
            GUILayout.Label("Move: W/S or Up/Down", textStyle);
            GUILayout.Label("Turn: A/D or Left/Right", textStyle);
            GUILayout.Label("Fire: Space or Left Mouse", textStyle);

            if (GameManager.Instance.CurrentState == GameState.Victory || GameManager.Instance.CurrentState == GameState.Defeat)
            {
                GUILayout.Space(10f);
                GUILayout.Label("Press R to restart", textStyle);
            }

            GUILayout.EndArea();
        }

        private void EnsureStyles()
        {
            if (panelStyle != null)
            {
                return;
            }

            panelStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(12, 12, 12, 12)
            };

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            textStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                normal = { textColor = Color.white }
            };
        }
    }
}
