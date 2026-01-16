using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Postica.Common;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.UIElements;


namespace Postica.BindingSystem
{
    internal class SplashScreen : EditorWindow
    {
        internal const string Legacy_FirstTimeKey = "[BS_SHOW_WELCOME]";
        private const string VersionFile = "Library/BS_Version.txt";

        private static DateTime RevealTime;
        private static bool IsUpdate;

        [InitializeOnLoadMethod]
        static void ShowFirstTime()
        {
            var lines = new List<string>();
            if(File.Exists(VersionFile))
            {
                lines = File.ReadAllLines(VersionFile).ToList();
                var version = lines[0];
                
                if (string.CompareOrdinal(version, BindSystem.Version) < 0)
                {
                    if (lines.Count > 0) lines[0] = BindSystem.Version;
                    else lines.Add(BindSystem.Version);

                    lines.RemoveAll(l => l == "show-welcome-update");
                    lines.Add("show-welcome-update");
                    
                    File.WriteAllLines(VersionFile, lines);
                    foreach(var method in TypeCache.GetMethodsWithAttribute<OnBindSystemUpgradeAttribute>())
                    {
                        try
                        {
                            var parameters = method.GetParameters();
                            if(parameters.Length >= 2 && parameters[0].ParameterType == typeof(string) && parameters[1].ParameterType == typeof(string))
                            {
                                method.Invoke(null, new object[] { version, BindSystem.Version });
                            }
                            else if(parameters.Length == 1 && parameters[0].ParameterType == typeof(string))
                            {
                                method.Invoke(null, new object[] { version });
                            }
                            else
                            {
                                method.Invoke(null, null);
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"Error invoking method {method.Name} with OnBindSystemUpgradeAttribute: {e.Message}");
                        }
                    }
                    IsUpdate = true;
                }

                if (lines.Contains("show-welcome-update") || lines.Contains("show-welcome"))
                {
                    EditorApplication.delayCall += DelayedStart;
                }

                return;
            }

            if (lines.Count > 0) lines[0] = BindSystem.Version;
            else lines.Add(BindSystem.Version);

            lines.Add("show-welcome");
            
            File.WriteAllLines(VersionFile, lines);

            if (PlayerPrefs.HasKey(Legacy_FirstTimeKey))
            {
                return;
            }

            EditorApplication.delayCall += DelayedStart;
        }

#if INTERNAL_TEST
        [MenuItem("Binding System/Test SplashScreen")]
#endif
        static void ClearFirstTime()
        {
            PlayerPrefs.DeleteKey(Legacy_FirstTimeKey);
            if(File.Exists(VersionFile))
            {
                File.Delete(VersionFile);
            }
            CompilationPipeline.RequestScriptCompilation();
        }

        static void DelayedStart()
        {
            RevealTime = DateTime.Now.AddSeconds(2);
            EditorApplication.update -= DelayedShowWelcome;
            EditorApplication.update += DelayedShowWelcome;
        }

        private static void DelayedShowWelcome()
        {
            if (RevealTime > DateTime.Now)
            {
                return;
            }
            EditorApplication.update -= DelayedShowWelcome;
            ShowWelcome();
        }

        static void ShowWelcome()
        {
            SplashScreen wnd = CreateInstance<SplashScreen>();
            wnd.ShowModal();
            // wnd.Show();
        }

        private void OnEnable()
        {
            minSize = maxSize = new Vector2(620, 540);

            Rect main = EditorGUIUtility.GetMainWindowPosition();
            Rect pos = position;
            float centerWidth = (main.width - pos.width) * 0.5f;
            float centerHeight = (main.height - pos.height) * 0.5f;
            pos.x = main.x + centerWidth;
            pos.y = main.y + centerHeight;
            position = pos;
        }
        
        private int _currentPanelIndex = -1;
        private List<VisualElement> _panels;
        private List<string> _alreadySavedSettings = new List<string>();

        public void CreateGUI()
        {
            if (File.Exists(VersionFile))
            {
                _alreadySavedSettings = new List<string>(File.ReadAllLines(VersionFile));
            }
            
            IsUpdate = _alreadySavedSettings.Contains("show-welcome-update");
            
            var removed = _alreadySavedSettings.RemoveAll(l => l == "show-welcome-update");
            removed += _alreadySavedSettings.RemoveAll(l => l == "show-welcome");
            
            if(removed > 0)
            {
                File.WriteAllLines(VersionFile, _alreadySavedSettings);
            }
            
            VisualElement root = rootVisualElement;

            // Find the BindingSystem Folder and from there these files
            var uxmlPath = BindingSystemIO.BuildLocalPath("Editor", "Splash", "SplashScreen.uxml");
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);
            VisualElement rootPanel = visualTree.Instantiate();
            root.Add(rootPanel);

            var ussPath = BindingSystemIO.BuildLocalPath("Editor", "Splash", "SplashScreen.uss");
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(ussPath);
            root.styleSheets.Add(styleSheet);

            rootPanel.StretchToParentSize();
            root.schedule.Execute(StartAnimations).StartingIn(200);
        }

        private void MoveToNextPanel()
        {
            if(_currentPanelIndex >= 0)
            {
                HidePanel(_panels[_currentPanelIndex]);
                var panelName = _panels[_currentPanelIndex].name.Replace("panel--", "");
                if(!_alreadySavedSettings.Contains(panelName))
                {
                    _alreadySavedSettings.Add(panelName);
                    File.WriteAllLines(VersionFile, _alreadySavedSettings);
                }
            }
            while(++_currentPanelIndex < _panels.Count)
            {
                var panelName = _panels[_currentPanelIndex].name.Replace("panel--", "");
                if(!_alreadySavedSettings.Contains(panelName) || panelName.Equals("final", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }
            }
            if(_currentPanelIndex >= _panels.Count)
            {
                rootVisualElement.schedule.Execute(Close).ExecuteLater(1000);
                return;
            }
            ShowPanel(_panels[_currentPanelIndex]);
        }

        private void ShowPanel(VisualElement panel)
        {
            panel.WithoutClass("disclaimer--hidden", "disclaimer--not-displayed");
        }

        private void HidePanel(VisualElement panel)
        {
            panel.WithClass("disclaimer--hidden");
            panel.schedule.Execute(() => panel.WithClass("disclaimer--not-displayed")).StartingIn(1000);
        }
        
        private void StartAnimations(TimerState state)
        {
            var root = rootVisualElement;
            
            _panels = root.Query<VisualElement>(null, "disclaimer").ToList();
            foreach (var panel in _panels.ToArray())
            {
                panel.WithClass("disclaimer--not-displayed");
                if (!ProcessPanel(panel))
                {
                    _panels.Remove(panel);
                }
            }

            var background = root.Q("background");
            background.AddToClassList("background--moved");

            var banner = root.Q("banner");
            banner.AddToClassList("banner--start");

            root.schedule.Execute(MoveToNextPanel).ExecuteLater(2000);
        }

        private bool ProcessPanel(VisualElement panel)
        {
            switch (panel.name.Replace("panel--", ""))
            {
#if BS_LEGACY_SUPPORT
                case "auto-conversion":
                    var enableButton = panel.Q<Button>("enable-btn");
                    var cancelButton = panel.Q<Button>("cancel-btn");
                
                    enableButton.clicked += EnableButton_clicked;
                    cancelButton.clicked += CancelButton_clicked;
                    return true;
#endif
                case "phased-bind":
                    var enablePhasedButton = panel.Q<Button>("enable-btn");
                    var cancelPhasedButton = panel.Q<Button>("cancel-btn");

                    enablePhasedButton.clicked += EnablePhasedButton_clicked;
                    cancelPhasedButton.clicked += CancelPhasedButton_clicked;
                    return true;
                case "final":
                    var closeButton = panel.Q<Button>("close-btn");
                    closeButton.clicked += MoveToNextPanel;
                    
                    if (IsUpdate)
                    {
                        panel.Q<Label>("completeTitle").text = "Binding System Updated";
                        panel.Q<Label>("completeMessage").text = $"The Binding System has been updated to the latest version\n<b>{BindSystem.Version}</b>";
                    }
                    
                    return true;
            }
            return false;
        }
        
        private void EnableButton_clicked()
        {
            BindingSettings.Current.AutoFixSerializationUpgrade = true;
            MoveToNextPanel();
        }
        
        private void EnablePhasedButton_clicked()
        {
            BindingSettings.Current.UsePhasedBinding = true;
            MoveToNextPanel();
        }
        
        private void CancelPhasedButton_clicked()
        {
            BindingSettings.Current.UsePhasedBinding = false;
            EditorUtility.DisplayDialog("Binding System", "This feature can always be re-enabled in Binding Settings", "Ok");
            MoveToNextPanel();
        }

        private void CancelButton_clicked()
        {
            BindingSettings.Current.AutoFixSerializationUpgrade = false;
            EditorUtility.DisplayDialog("Binding System", "This feature can always be re-enabled in Binding Settings", "Ok");
            MoveToNextPanel();
        }
    }
}